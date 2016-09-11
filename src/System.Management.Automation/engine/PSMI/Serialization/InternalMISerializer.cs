/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This internal helper class provides methods for serializing mshObject.
    /// </summary>
    internal class InternalMISerializer
    {
        //TODO, insivara : Is this the correct namespace
        private const string PowerShellRemotingProviderNamespace = "root/Microsoft/Windows/Powershellv4";

        #region Constructor

        //TODO, insivara : Depth implementation will come in a later checkin
        private int _depth;

        internal InternalMISerializer(int depth)
        {
            _depth = depth;
            // TODO, insivara : Revisit this
            _typeTable = new TypeTable();
        }

        #endregion Constructor

        #region Properties

        internal CimInstance CimInstance { get; set; }

        /// Used by Remoting infrastructure. This TypeTable instance
        /// will be used by Serializer if ExecutionContext is not
        /// available (to get the ExecutionContext's TypeTable)
        private TypeTable _typeTable;

        private Collection<CollectionEntry<PSPropertyInfo>> _allPropertiesCollection;
        private Collection<CollectionEntry<PSPropertyInfo>> AllPropertiesCollection
        {
            get {
                return _allPropertiesCollection ??
                       (_allPropertiesCollection = PSObject.GetPropertyCollection(PSMemberViewTypes.All, _typeTable));
            }
        }

        // Similar to the property by the same name in serialization.cs
        private bool? _canUseDefaultRunspaceInThreadSafeManner;

        private bool CanUseDefaultRunspaceInThreadSafeManner
        {
            get
            {
                // can use default runspace in a thread safe manner only if
                // 1. we have a default runspace
                // 2. we recognize the type of current runspace and current pipeline
                // 3. the pipeline executes on the same thread as this method

                // we don't return "true" for
                // 1. we have a default runspace
                // 2. no currently executing pipeline
                // to avoid a race condition where a pipeline is started
                // after this property getter did all the checks

                if (!_canUseDefaultRunspaceInThreadSafeManner.HasValue)
                {
                    _canUseDefaultRunspaceInThreadSafeManner = false;

                    RunspaceBase runspace = Runspace.DefaultRunspace as RunspaceBase;
                    if (runspace != null)
                    {
                        Pipeline currentPipeline = runspace.GetCurrentlyRunningPipeline();
                        LocalPipeline localPipeline = currentPipeline as LocalPipeline;
                        if ((localPipeline != null) && (localPipeline.NestedPipelineExecutionThread != null))
                        {
                            _canUseDefaultRunspaceInThreadSafeManner =
                                (localPipeline.NestedPipelineExecutionThread.ManagedThreadId
                                == Threading.Thread.CurrentThread.ManagedThreadId);
                        }
                    }
                }

                return _canUseDefaultRunspaceInThreadSafeManner.Value;
            }
        }

        #endregion Properties

        internal CimInstance Serialize(object o)
        {
            return CreateCimInstanceForOneTopLevelObject(o);
        }

        /// <summary>
        /// This writes one object.
        /// </summary>
        /// <param name="source">
        /// source to be serialized.
        /// </param>
        internal CimInstance CreateCimInstanceForOneTopLevelObject
        (
            object source
        )
        {
            CimInstance result;
            CreateCimInstanceForOneObject(source, null, _depth, out result);
            return result;
        }

        private void CreateCimInstanceForOneObject
        (
            object source,
            string property,
            int depth,
            out CimInstance result
        )
        {
            // To avoid compiler error
            result = CreateNullCimInstance();

            Dbg.Assert(depth >= 0, "depth should always be greater or equal to zero");

            if (source == null)
            {
                //TODO, insivara : TO BE IMPLEMENTED
                //TODO, insivara : Return Null CimInstance
                return;
            }

            if (HandlePrimitiveKnownTypeByConvertingToPSObject(source, property, depth, out result))
            {
                return;
            }

            if (HandleKnownContainerTypes(source, property, depth, out result))
            {
                return;
            }

            HandleComplexTypePSObject(source, property, depth, out result);

            //TODO, insivara : Depth implementation
            return;
        }

        /// <summary>
        /// Handles primitive known type by first converting it to a PSObject.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="property"></param>
        /// <param name="depth"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool HandlePrimitiveKnownTypeByConvertingToPSObject(
            object source,
            string property,
            int depth,
            out CimInstance result
        )
        {
            // To avoid compiler error
            result = CreateNullCimInstance();

            Dbg.Assert(source != null, "caller should validate the parameter");
            //Check if source is of primitive known type
            MITypeSerializationInfo pktInfo = KnownMITypes.GetTypeSerializationInfo(source.GetType());
            if (pktInfo != null)
            {
                PSObject pktInfoPSObject = PSObject.AsPSObject(source);
                return HandlePrimitiveKnownTypePSObject(pktInfoPSObject, property, depth, out result);
            }
            return false;
        }

        /// <summary>
        /// Serializes PSObject whose base objects are of primitive known type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="property"></param>
        /// <param name="depth"></param>
        /// <param name="result"></param>
        /// <returns>
        /// true if source is handled, else false.
        /// </returns>
        private bool HandlePrimitiveKnownTypePSObject
        (
            object source,
            string property,
            int depth,
            out CimInstance result
        )
        {
            // To avoid compiler error
            result = CreateNullCimInstance();

            Dbg.Assert(source != null, "caller should validate the parameter");

            bool sourceHandled = false;
            PSObject moSource = source as PSObject;
            if (moSource != null && !moSource.immediateBaseObjectIsEmpty)
            {
                //Check if baseObject is primitive known type
                object baseObject = moSource.ImmediateBaseObject;
                MITypeSerializationInfo pktInfo = KnownMITypes.GetTypeSerializationInfo(baseObject.GetType());
                if (pktInfo != null)
                {
                    CreateCimInstanceForPrimitiveTypePSObject(moSource, baseObject, pktInfo, property, depth, out result);
                    sourceHandled = true;
                }
            }
            return sourceHandled;
        }

        private bool HandleKnownContainerTypes
        (
            object source,
            string property,
            int depth,
            out CimInstance result
        )
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            result = CreateNullCimInstance();
            ContainerType ct = ContainerType.None;
            PSObject mshSource = source as PSObject;
            IEnumerable enumerable = null;
            IDictionary dictionary = null;

            //If passed in object is PSObject with no baseobject, return false.
            if (mshSource != null && mshSource.immediateBaseObjectIsEmpty)
            {
                return false;
            }

            //Check if source (or baseobject in mshSource) is known container type
            SerializationUtilities.GetKnownContainerTypeInfo(mshSource != null ? mshSource.ImmediateBaseObject : source, out ct,
                                      out dictionary, out enumerable);

            if (ct == ContainerType.None)
                return false;

            result = CreateCimInstanceForPSObject(cimClassName: "PS_Object",
                                                         psObj: mshSource ?? PSObject.AsPSObject(source),
                                                         writeToString: false);

            List<CimInstance> listOfCimInstances = null;
            switch (ct)
            {
                case ContainerType.Dictionary:
                    WriteDictionary(dictionary, depth, out listOfCimInstances);
                    break;
                case ContainerType.Stack:
                    WriteEnumerable(enumerable, property, depth, out listOfCimInstances);
                    break;
                case ContainerType.Queue:
                    WriteEnumerable(enumerable, property, depth, out listOfCimInstances);
                    break;
                case ContainerType.List:
                    WriteEnumerable(enumerable, property, depth, out listOfCimInstances);
                    break;
                case ContainerType.Enumerable:
                    WriteEnumerable(enumerable, property, depth, out listOfCimInstances);
                    break;
                default:
                    Dbg.Assert(false, "All containers should be handled in the switch");
                    break;
            }

            CimInstance[] instanceArray = listOfCimInstances.ToArray();
            CimProperty valueProperty = CimProperty.Create("Value", instanceArray, Microsoft.Management.Infrastructure.CimType.InstanceArray, CimFlags.Property);
            result.CimInstanceProperties.Add(valueProperty);
            return true;
        }

        private void HandleComplexTypePSObject
           (
           object source,
           string property,
           int depth,
           out CimInstance result
           )
        {
            List<CimInstance> listOfCimInstancesProperties = null;
            Dbg.Assert(source != null, "caller should validate the parameter");
            PSObject mshSource = PSObject.AsPSObject(source);

            // Figure out what kind of object we are dealing with
            bool isErrorRecord = false;
            bool isInformationalRecord = false;
            bool isEnum = false;
            bool isPSObject = false;
            //TODO, insivara : To be implemented
            //bool isCimInstance = false;

            if (!mshSource.immediateBaseObjectIsEmpty)
            {
                ErrorRecord errorRecord = mshSource.ImmediateBaseObject as ErrorRecord;
                if (errorRecord == null)
                {
                    InformationalRecord informationalRecord = mshSource.ImmediateBaseObject as InformationalRecord;
                    if (informationalRecord == null)
                    {
                        isEnum = mshSource.ImmediateBaseObject is Enum;
                        isPSObject = mshSource.ImmediateBaseObject is PSObject;
                    }
                    else
                    {
                        informationalRecord.ToPSObjectForRemoting(mshSource);
                        isInformationalRecord = true;
                    }
                }
                else
                {
                    errorRecord.ToPSObjectForRemoting(mshSource);
                    isErrorRecord = true;
                }
            }

            bool writeToString = true;
            if (mshSource.ToStringFromDeserialization == null)
            // continue to write ToString from deserialized objects, but...
            {
                if (mshSource.immediateBaseObjectIsEmpty) // ... don't write ToString for property bags
                {
                    writeToString = false;
                }
            }

            // This will create a CimInstance for PS_Object and populate the typenames. 
            result = CreateCimInstanceForPSObject(cimClassName: "PS_Object",
                                                  psObj: mshSource,
                                                  writeToString: writeToString);

            PSMemberInfoInternalCollection<PSPropertyInfo> specificPropertiesToSerialize =
                SerializationUtilities.GetSpecificPropertiesToSerialize(mshSource, AllPropertiesCollection, _typeTable);

            if (isEnum)
            {
                CimInstance enumCimInstance = CreateCimInstanceForEnum(mshSource, depth, property != null);
                CimProperty p = CimProperty.Create("Value", enumCimInstance,
                                                   Microsoft.Management.Infrastructure.CimType.Reference,
                                                   Microsoft.Management.Infrastructure.CimFlags.Property);
                result.CimInstanceProperties.Add(p);
            }
            else if (isPSObject)
            {
                CimInstance psObjectCimInstance;
                CreateCimInstanceForOneObject(mshSource.ImmediateBaseObject, property, depth, out psObjectCimInstance);
                CimProperty valueProperty = CimProperty.Create("Value", psObjectCimInstance,
                                                               Microsoft.Management.Infrastructure.CimType.Reference,
                                                               Microsoft.Management.Infrastructure.CimFlags.Property);
                result.CimInstanceProperties.Add(valueProperty);
            }
            else if (isErrorRecord || isInformationalRecord)
            {
                // nothing to do
            }
            else
            {
                CreateCimInstanceForPSObjectProperties(mshSource, depth, specificPropertiesToSerialize, out listOfCimInstancesProperties);
            }

            //TODO, insivara : Implement serialization of CimInstance 
            //if (isCimInstance)
            //{
            //    CimInstance cimInstance = mshSource.ImmediateBaseObject as CimInstance;
            //    PrepareCimInstanceForSerialization(mshSource, cimInstance);
            //}

            //TODO, insivara : ExtendedProperties implementation will be done in a subsequent checkin
            //SerializeExtendedProperties(mshSource, depth, specificPropertiesToSerialize, out listOfCimInstancesExtendedProperties);

            if (listOfCimInstancesProperties != null && listOfCimInstancesProperties.Count > 0)
            {
                CimInstance[] referenceArray = listOfCimInstancesProperties.ToArray();
                CimProperty properties = CimProperty.Create("Properties", referenceArray,
                                                            Microsoft.Management.Infrastructure.CimType.ReferenceArray,
                                                            Microsoft.Management.Infrastructure.CimFlags.Property);
                result.CimInstanceProperties.Add(properties);
            }
        }

        /// <summary>
        /// Serializes an PSObject whose baseobject is of primitive type.
        /// </summary>
        /// <param name="source">
        /// source from which notes are written
        /// </param>
        /// <param name="primitive">
        /// primitive object which is written as base object. In most cases it
        /// is same source.ImmediateBaseObject. When PSObject is serialized as string,
        /// </param>
        /// <param name="pktInfo">
        /// TypeSerializationInfo for the primitive. 
        /// </param>
        /// <param name="property"></param>
        /// <param name="depth"></param>
        /// <param name="result"></param>
        private void CreateCimInstanceForPrimitiveTypePSObject
        (
            PSObject source,
            object primitive,
            MITypeSerializationInfo pktInfo,
            string property,
            int depth,
            out CimInstance result
        )
        {
            // To avoid compiler error
            result = CreateNullCimInstance();

            Dbg.Assert(source != null, "Caller should validate source != null");

            string toStringValue = SerializationUtilities.GetToStringForPrimitiveObject(source);
            bool hasModifiedTypesCollection = false;
            //hasModifiedTypesCollection = PSObjectHasModifiedTypesCollection(source);

            bool hasNotes = false;
            //hasNotes = PSObjectHasNotes(source);

            bool hasModifiedToString = (toStringValue != null);

            if (hasNotes || hasModifiedTypesCollection || hasModifiedToString)
            {
                //TODO, insivara : TO BE IMPLEMENTED
                //WritePrimitiveTypePSObjectWithNotes(
                //    source,
                //    primitive,
                //    hasModifiedTypesCollection,
                //    toStringValue,
                //    pktInfo,
                //    streamName,
                //    property,
                //    depth);
                //return;
            }
            else
            {
                if (primitive != null)
                {
                    CreateCimInstanceForOnePrimitiveKnownType(this, property, primitive, pktInfo, out result);
                    return;
                }
                else
                {
                    //TODO, insivara : Return Null CimInstance
                    return;
                }
            }
        }

        /// <summary>
        /// Writes an item or property in Monad namespace
        /// </summary>
        /// <param name="serializer">The serializer to which the object is serialized.</param>
        /// <param name="property">name of property. Pass null for item</param>
        /// <param name="source">object to be written</param>
        /// <param name="entry">serialization information about source</param>
        /// <param name="result"></param>
        private static void CreateCimInstanceForOnePrimitiveKnownType
        (
            InternalMISerializer serializer,
            string property,
            object source,
            MITypeSerializationInfo entry,
            out CimInstance result
        )
        {
            Dbg.Assert(serializer != null, "caller should have validated the information");
            Dbg.Assert(source != null, "caller should have validated the information");
            Dbg.Assert(entry != null, "caller should have validated the information");

            if (entry != null && entry.Serializer == null)
            {
                // we are not using GetToString, because we assume that
                // ToString() for primitive types never throws
                string value = Convert.ToString(source, CultureInfo.InvariantCulture);
                Dbg.Assert(value != null, "ToString shouldn't return null for primitive types");
                result = CreateRawStringCimInstance(property, value, entry);
            }
            else
            {
                result = entry.Serializer(property, source, entry);
            }
        }

        #region membersets
        #endregion membersets

        #region properties

        #endregion properties

        # region enumerable and dictionary

        /// <summary>
        /// Serializes IEnumerable
        /// </summary>
        /// <param name="enumerable">
        /// enumerable which is serialized
        /// </param>
        /// <param name="property"></param>
        /// <param name="enumerableInstances">
        /// </param>
        /// <param name="depth"></param>
        private void WriteEnumerable
        (
            IEnumerable enumerable,
            string property,
            int depth,
            out List<CimInstance> enumerableInstances
        )
        {
            enumerableInstances = new List<CimInstance>();

            Dbg.Assert(enumerable != null, "caller should validate the parameter");

            IEnumerator enumerator = null;
            try
            {
                enumerator = enumerable.GetEnumerator();
                try
                {
                    enumerator.Reset();
                }
                catch (System.NotSupportedException)
                {
                    //ignore exceptions thrown when the enumerator doesn't support Reset() method as in  win8:948569 
                }
            }
            catch (Exception exception)
            {
                // Catch-all OK. This is a third-party call-out.
                CommandProcessorBase.CheckForSevereException(exception);

                PSEtwLog.LogAnalyticWarning(
                    PSEventId.Serializer_EnumerationFailed, PSOpcode.Exception, PSTask.Serialization,
                    PSKeyword.Serializer | PSKeyword.UseAlwaysAnalytic,
                    enumerable.GetType().AssemblyQualifiedName,
                    exception.ToString());

                enumerator = null;
            }

            if (enumerator != null)
            {
                while (true)
                {
                    object item = null;
                    try
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                        item = enumerator.Current;
                    }
                    catch (Exception exception)
                    {
                        // Catch-all OK. This is a third-party call-out.
                        CommandProcessorBase.CheckForSevereException(exception);

                        PSEtwLog.LogAnalyticWarning(
                            PSEventId.Serializer_EnumerationFailed, PSOpcode.Exception, PSTask.Serialization,
                            PSKeyword.Serializer | PSKeyword.UseAlwaysAnalytic,
                            enumerable.GetType().AssemblyQualifiedName,
                            exception.ToString());

                        break;
                    }
                    CimInstance enumerableInstance;
                    CreateCimInstanceForOneObject(item, property, depth, out enumerableInstance);
                    enumerableInstances.Add(enumerableInstance);
                }
            }
        }

        private void WriteDictionary(IDictionary dictionary, int depth, out List<CimInstance> listOfCimInstances)
        {
            listOfCimInstances = new List<CimInstance>();
            Dbg.Assert(dictionary != null, "caller should validate the parameter");

            IDictionaryEnumerator dictionaryEnum = null;
            try
            {
                dictionaryEnum = dictionary.GetEnumerator();
            }
            catch (Exception exception) // ignore non-severe exceptions
            {
                // Catch-all OK. This is a third-party call-out.
                CommandProcessorBase.CheckForSevereException(exception);

                PSEtwLog.LogAnalyticWarning(
                    PSEventId.Serializer_EnumerationFailed, PSOpcode.Exception, PSTask.Serialization,
                    PSKeyword.Serializer | PSKeyword.UseAlwaysAnalytic,
                    dictionary.GetType().AssemblyQualifiedName,
                    exception.ToString());
            }

            if (dictionaryEnum != null)
            {
                while (true)
                {
                    object key = null;
                    object value = null;
                    try
                    {
                        if (!dictionaryEnum.MoveNext())
                        {
                            break;
                        }
                        else
                        {
                            key = dictionaryEnum.Key;
                            value = dictionaryEnum.Value;
                        }
                    }
                    catch (Exception exception)
                    {
                        // Catch-all OK. This is a third-party call-out.
                        CommandProcessorBase.CheckForSevereException(exception);

                        PSEtwLog.LogAnalyticWarning(
                            PSEventId.Serializer_EnumerationFailed, PSOpcode.Exception, PSTask.Serialization,
                            PSKeyword.Serializer | PSKeyword.UseAlwaysAnalytic,
                            dictionary.GetType().AssemblyQualifiedName,
                            exception.ToString());

                        break;
                    }

                    Dbg.Assert(key != null, "Dictionary keys should never be null");
                    if (key == null) break;
                    CimInstance dictionaryEntryInstance = CreateCimInstanceForDictionaryEntry(key, value, depth);
                    listOfCimInstances.Add(dictionaryEntryInstance);
                }
            }
        }

        private CimInstance CreateCimInstanceForDictionaryEntry(object key, object value, int depth)
        {
            //TODO, insivara: Update class name here
            CimInstance dictionaryEntryCimInstance = CreateCimInstance("PSObject_DictionaryEntry");
            CimInstance keyCimInstance, valueCimInstance;
            CreateCimInstanceForOneObject(key, null, depth, out keyCimInstance);
            CreateCimInstanceForOneObject(value, null, depth, out valueCimInstance);
            CimProperty keyProperty = CimProperty.Create("Key", keyCimInstance,
                                                         Microsoft.Management.Infrastructure.CimType.Instance,
                                                         Microsoft.Management.Infrastructure.CimFlags.Property);
            CimProperty valueProperty = CimProperty.Create("Value", valueCimInstance,
                                                         Microsoft.Management.Infrastructure.CimType.Instance,
                                                         Microsoft.Management.Infrastructure.CimFlags.Property);
            dictionaryEntryCimInstance.CimInstanceProperties.Add(keyProperty);
            dictionaryEntryCimInstance.CimInstanceProperties.Add(valueProperty);
            return dictionaryEntryCimInstance;
        }

        #endregion enumerable and dictionary

        # region Serialization Delegates

        /// <summary>
        /// Creates CimInstance for a primitive type 
        /// </summary>
        /// <param name="property">name of property. pass null for item</param>
        /// <param name="source">value</param>
        /// <param name="entry">serialization information about source</param>
        internal static CimInstance CreateCimInstanceForPrimitiveType(string property, object source, MITypeSerializationInfo entry)
        {
            CimInstance c;
            if (property != null)
            {
                c = CreateCimInstanceWhenPropertyNameExists(property, source, entry);
            }
            else
            {
                c = CreateCimInstance(entry.CimClassName);
                CimProperty valueProperty = CimProperty.Create("Value", source, entry.CimType, CimFlags.Property);
                c.CimInstanceProperties.Add(valueProperty);
            }
            return c;
        }

        /// <summary>
        /// Creates CimInstance for a string 
        /// </summary>
        /// <param name="property">name of property. pass null for item</param>
        /// <param name="source">string value to write</param>
        /// <param name="entry">serialization information about source</param>
        internal static CimInstance CreateCimInstanceForString(string property, object source, MITypeSerializationInfo entry)
        {
            CimInstance c;
            String value = InternalSerializer.EncodeString((String)source);
            if (property != null)
            {
                c = CreateCimInstanceWhenPropertyNameExists(property, value, entry);
            }
            else
            {
                c = CreateCimInstance(entry.CimClassName);
                CimProperty valueProperty = CimProperty.Create("Value", value, entry.CimType, CimFlags.Property);
                c.CimInstanceProperties.Add(valueProperty);
            }
            return c;
        }

        #endregion

        #region Serialization Helper Methods

        private static CimProperty CreateTypeNamesProperty(IEnumerable<string> types)
        {
            return CimProperty.Create("TypeNames", types, Microsoft.Management.Infrastructure.CimType.StringArray, CimFlags.Property);
        }

        internal static CimProperty CreateCimProperty(string propertyName, object propertyValue, Microsoft.Management.Infrastructure.CimType cimType)
        {
            return CimProperty.Create(propertyName, propertyValue, cimType, CimFlags.Property);
        }

        internal static CimInstance CreateCimInstance(string cimClassName)
        {
            CimInstance cimInstance = new CimInstance(cimClassName, PowerShellRemotingProviderNamespace);
            return cimInstance;
        }

        internal static CimInstance CreateCimInstanceForPSObject(string cimClassName, PSObject psObj, bool writeToString)
        {
            CimInstance cimInstance = new CimInstance(cimClassName, PowerShellRemotingProviderNamespace);
            CimProperty typeProperty = CreateTypeNamesProperty(psObj.TypeNames);
            cimInstance.CimInstanceProperties.Add(typeProperty);

            if (writeToString)
            {
                CimProperty toStringProperty = CimProperty.Create("ToString",
                                                                  SerializationUtilities.GetToString(psObj),
                                                                  Microsoft.Management.Infrastructure.CimType.String,
                                                                  Microsoft.Management.Infrastructure.CimFlags.Property);
                cimInstance.CimInstanceProperties.Add(toStringProperty);
            }
            return cimInstance;
        }

        private static CimInstance CreateRawStringCimInstance(string property, string value, MITypeSerializationInfo entry)
        {
            CimInstance c;
            if (property != null)
            {
                c = CreateCimInstanceWhenPropertyNameExists(property, value, entry);
            }
            else
            {
                c = CreateCimInstance(entry.CimClassName);
                CimProperty p1 = CimProperty.Create("Value", value, entry.CimType, CimFlags.Property);
                c.CimInstanceProperties.Add(p1);
            }
            return c;
        }

        private static CimInstance CreateCimInstanceWhenPropertyNameExists(string property, object source, MITypeSerializationInfo entry)
        {
            CimInstance innerInstance = CreateCimInstance(entry.CimClassName);
            CimProperty valueProperty = CimProperty.Create("Value", source, entry.CimType, CimFlags.Property);
            innerInstance.CimInstanceProperties.Add(valueProperty);
            CimInstance c = CreateCimInstance("PS_ObjectProperty");
            CimProperty name = CimProperty.Create("Name", property,
                                                  Microsoft.Management.Infrastructure.CimType.String,
                                                  CimFlags.Property);
            c.CimInstanceProperties.Add(name);
            CimProperty outerInstanceValueProperty = CimProperty.Create("Value", innerInstance,
                                                                        Microsoft.Management.Infrastructure.CimType.Reference,
                                                                        CimFlags.Property);
            c.CimInstanceProperties.Add(outerInstanceValueProperty);

            return c;
        }

        private CimInstance CreateCimInstanceForEnum(PSObject mshSource, int depth, bool serializeAsString)
        {
            CimInstance enumCimInstance = null;
            object baseObject = mshSource.ImmediateBaseObject;
            CreateCimInstanceForOneObject(source: System.Convert.ChangeType(baseObject, Enum.GetUnderlyingType(baseObject.GetType()), System.Globalization.CultureInfo.InvariantCulture),
                                          property: null,
                                          depth: depth,
                                          result: out enumCimInstance);
            return enumCimInstance;
        }

        /// <summary>
        /// Serializes properties of PSObject
        /// </summary>
        private void CreateCimInstanceForPSObjectProperties
        (
            PSObject source,
            int depth,
            IEnumerable<PSPropertyInfo> specificPropertiesToSerialize,
            out List<CimInstance> listOfCimInstances
        )
        {
            listOfCimInstances = new List<CimInstance>();

            Dbg.Assert(source != null, "caller should validate the information");

            //TODO, insivara : Depth implementation will come later

            if (specificPropertiesToSerialize != null)
            {
                SerializeProperties(specificPropertiesToSerialize, depth, out listOfCimInstances);
            }
            else
            {
                if (source.ShouldSerializeAdapter())
                {
                    IEnumerable<PSPropertyInfo> adapterCollection = null;
                    adapterCollection = source.GetAdaptedProperties();
                    if (adapterCollection != null)
                    {
                        SerializeProperties(adapterCollection, depth, out listOfCimInstances);
                    }
                }
            }
        }

        /// <summary>
        /// Serializes properties from collection
        /// </summary>
        /// <param name="propertyCollection">
        /// Collection of properties to serialize
        /// </param>
        /// <param name="depth">
        /// depth to which each property should be 
        /// serialized
        /// </param>    
        /// <param name="listOfCimInstances">
        /// list of CimInstances for the properties 
        /// serialized
        /// </param>
        private void SerializeProperties
        (
            IEnumerable<PSPropertyInfo> propertyCollection,
            int depth,
            out List<CimInstance> listOfCimInstances
        )
        {
            listOfCimInstances = new List<CimInstance>();
            Dbg.Assert(propertyCollection != null, "caller should validate the parameter");

            foreach (PSMemberInfo info in propertyCollection)
            {
                PSProperty prop = info as PSProperty;
                if (prop == null)
                {
                    continue;
                }

                bool success;
                object value = SerializationUtilities.GetPropertyValueInThreadSafeManner(prop, this.CanUseDefaultRunspaceInThreadSafeManner, out success);
                if (success)
                {
                    CimInstance propertyInstance = null;
                    CreateCimInstanceForOneObject(value, prop.Name, depth, out propertyInstance);
                    listOfCimInstances.Add(propertyInstance);
                }
            }
        }

        private static CimInstance CreateNullCimInstance()
        {
            return new CimInstance("Null");
        }

        #endregion 
    }
}