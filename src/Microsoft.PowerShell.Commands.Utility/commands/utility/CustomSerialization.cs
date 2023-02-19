// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This class provides functionality for serializing a PSObject.
    /// </summary>
    internal class CustomSerialization
    {
        #region constructor
        /// <summary>
        /// Depth of serialization.
        /// </summary>
        private readonly int _depth;

        /// <summary>
        /// XmlWriter to be used for writing.
        /// </summary>
        private readonly XmlWriter _writer;

        /// <summary>
        /// Whether type information should be included in the xml.
        /// </summary>
        private readonly bool _notypeinformation;

        /// <summary>
        /// CustomerSerializer used for formatting the output for _writer.
        /// </summary>
        private CustomInternalSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSerialization"/> class.
        /// </summary>
        /// <param name="writer">
        /// writer to be used for serialization.
        /// </param>
        /// <param name="notypeinformation">
        /// should the type information to be shown.
        /// </param>
        /// <param name="depth">
        /// depth to be used for serialization. If this value is specified,
        /// depth from types.xml is not used.
        /// </param>
        internal CustomSerialization(XmlWriter writer, bool notypeinformation, int depth)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(writer));
            }

            if (depth < 1)
            {
                throw PSTraceSource.NewArgumentException(nameof(writer), Serialization.DepthOfOneRequired);
            }

            _depth = depth;
            _writer = writer;
            _notypeinformation = notypeinformation;
            _serializer = null;
        }

        /// <summary>
        /// Default depth of serialization.
        /// </summary>
        public static int MshDefaultSerializationDepth { get; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSerialization"/> class.
        /// </summary>
        /// <param name="writer">
        /// writer to be used for serialization.
        /// </param>
        /// <param name="notypeinformation">
        /// should the type information to be shown.
        /// </param>
        internal CustomSerialization(XmlWriter writer, bool notypeinformation)
            : this(writer, notypeinformation, MshDefaultSerializationDepth)
        {
        }

        #endregion constructor

        #region public methods

        private bool _firstCall = true;

        /// <summary>
        /// Serializes passed in object.
        /// </summary>
        /// <param name="source">
        /// Object to be serialized.
        /// </param>
        internal void Serialize(object source)
        {
            // Write the root element tag before writing first object.
            if (_firstCall)
            {
                _firstCall = false;
                Start();
            }

            _serializer = new CustomInternalSerializer
                               (
                                   _writer,
                                   _notypeinformation,
                                   true
                                );
            _serializer.WriteOneObject(source, null, _depth);
            _serializer = null;
        }

        /// <summary>
        /// Serializes passed in object.
        /// </summary>
        /// <param name="source">
        /// Object to be serialized.
        /// </param>
        internal void SerializeAsStream(object source)
        {
            _serializer = new CustomInternalSerializer
                               (
                                   _writer,
                                   _notypeinformation,
                                   true
                                );
            _serializer.WriteOneObject(source, null, _depth);
            _serializer = null;
        }

        /// <summary>
        /// Writes the start of root element.
        /// </summary>
        private void Start()
        {
            CustomInternalSerializer.WriteStartElement(_writer, CustomSerializationStrings.RootElementTag);
        }

        /// <summary>
        /// Write the end of root element.
        /// </summary>
        internal void Done()
        {
            if (_firstCall)
            {
                _firstCall = false;
                Start();
            }

            _writer.WriteEndElement();
            _writer.Flush();
        }

        /// <summary>
        /// Flush the writer.
        /// </summary>
        internal void DoneAsStream()
        {
            _writer.Flush();
        }

        internal void Stop()
        {
            CustomInternalSerializer serializer = _serializer;
            serializer?.Stop();
        }

        #endregion
    }

    /// <summary>
    /// This internal helper class provides methods for serializing mshObject.
    /// </summary>
    internal class
    CustomInternalSerializer
    {
        #region constructor

        /// <summary>
        /// Xml writer to be used.
        /// </summary>
        private readonly XmlWriter _writer;

        /// <summary>
        /// Check first call for every pipeline object to write Object tag else property tag.
        /// </summary>
        private bool _firstcall;

        /// <summary>
        /// Should the type information to be shown.
        /// </summary>
        private readonly bool _notypeinformation;

        /// <summary>
        /// Check object call.
        /// </summary>
        private bool _firstobjectcall = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomInternalSerializer"/> class.
        /// </summary>
        /// <param name="writer">
        /// Xml writer to be used.
        /// </param>
        /// <param name="notypeinformation">
        /// Xml writer to be used.
        /// </param>
        /// <param name="isfirstcallforObject">
        /// Check first call for every pipeline object to write Object tag else property tag.
        /// </param>
        internal CustomInternalSerializer(XmlWriter writer, bool notypeinformation, bool isfirstcallforObject)
        {
            Dbg.Assert(writer != null, "caller should validate the parameter");

            _writer = writer;
            _notypeinformation = notypeinformation;
            _firstcall = isfirstcallforObject;
        }

        #endregion

        #region Stopping

        private bool _isStopping = false;

        /// <summary>
        /// Called from a separate thread will stop the serialization process.
        /// </summary>
        internal void Stop()
        {
            _isStopping = true;
        }

        private void CheckIfStopping()
        {
            if (_isStopping)
            {
                throw PSTraceSource.NewInvalidOperationException(Serialization.Stopping);
            }
        }

        #endregion Stopping

        /// <summary>
        /// This writes one object.
        /// </summary>
        /// <param name="source">
        /// source to be serialized.
        /// </param>
        /// <param name="property">
        /// name of property. If null, name attribute is not written.
        /// </param>
        /// <param name="depth">
        /// depth to which this object should be serialized.
        /// </param>
        internal void WriteOneObject(object source, string property, int depth)
        {
            Dbg.Assert(depth >= 0, "depth should always be greater or equal to zero");

            CheckIfStopping();

            if (source == null)
            {
                WriteNull(property);
                return;
            }

            if (HandlePrimitiveKnownType(source, property))
            {
                return;
            }

            if (HandlePrimitiveKnownTypePSObject(source, property, depth))
            {
                return;
            }

            // Note: We don't use containers in depth calculation. i.e even if the
            // current depth is zero, we serialize the container. All contained items will
            // get serialized with depth zero.
            if (HandleKnownContainerTypes(source, property, depth))
            {
                return;
            }

            PSObject mshSource = PSObject.AsPSObject(source);
            // If depth is zero, complex type should be serialized as string.
            if (depth == 0 || SerializeAsString(mshSource))
            {
                HandlePSObjectAsString(mshSource, property, depth);
                return;
            }

            HandleComplexTypePSObject(mshSource, property, depth);
            return;
        }

        /// <summary>
        /// Serializes Primitive Known Types.
        /// </summary>
        /// <returns>
        /// true if source is handled, else false.
        /// </returns>
        private bool HandlePrimitiveKnownType(object source, string property)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            // Check if source is of primitive known type
            TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(source.GetType());
            if (pktInfo != null)
            {
                WriteOnePrimitiveKnownType(_writer, property, source, pktInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serializes PSObject whose base objects are of primitive known type.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="property"></param>
        /// <param name="depth"></param>
        /// <returns>
        /// true if source is handled, else false.
        /// </returns>
        private bool HandlePrimitiveKnownTypePSObject(object source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            bool sourceHandled = false;
            PSObject moSource = source as PSObject;
            if (moSource != null && !moSource.ImmediateBaseObjectIsEmpty)
            {
                // Check if baseObject is primitive known type
                object baseObject = moSource.ImmediateBaseObject;
                TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(baseObject.GetType());
                if (pktInfo != null)
                {
                    WriteOnePrimitiveKnownType(_writer, property, baseObject, pktInfo);
                    sourceHandled = true;
                }
            }

            return sourceHandled;
        }

        private bool HandleKnownContainerTypes(object source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            ContainerType ct = ContainerType.None;
            PSObject mshSource = source as PSObject;
            IEnumerable enumerable = null;
            IDictionary dictionary = null;

            // If passed in object is PSObject with no baseobject, return false.
            if (mshSource != null && mshSource.ImmediateBaseObjectIsEmpty)
            {
                return false;
            }

            // Check if source (or baseobject in mshSource) is known container type
            GetKnownContainerTypeInfo(mshSource != null ? mshSource.ImmediateBaseObject : source, out ct,
                                      out dictionary, out enumerable);

            if (ct == ContainerType.None)
                return false;

            WriteStartOfPSObject(mshSource ?? PSObject.AsPSObject(source), property, true);
            switch (ct)
            {
                case ContainerType.Dictionary:
                    {
                        WriteDictionary(dictionary, depth);
                    }

                    break;
                case ContainerType.Stack:
                case ContainerType.Queue:
                case ContainerType.List:
                case ContainerType.Enumerable:
                    {
                        WriteEnumerable(enumerable, depth);
                    }

                    break;
                default:
                    {
                        Dbg.Assert(false, "All containers should be handled in the switch");
                    }

                    break;
            }

            // An object which is original enumerable becomes an PSObject
            // with arraylist on deserialization. So on roundtrip it will show up
            // as List.
            // We serialize properties of enumerable and on deserialization mark the object
            // as Deserialized. So if object is marked deserialized, we should write properties.
            // Note: we do not serialize the properties of IEnumerable if depth is zero.
            if (depth != 0 && (ct == ContainerType.Enumerable || (mshSource != null && mshSource.IsDeserialized)))
            {
                // Note:Depth is the depth for serialization of baseObject.
                // Depth for serialization of each property is one less.
                WritePSObjectProperties(PSObject.AsPSObject(source), depth);
            }

            // If source is PSObject, serialize notes
            if (mshSource != null)
            {
                // Serialize instanceMembers
                PSMemberInfoCollection<PSMemberInfo> instanceMembers = mshSource.InstanceMembers;
                if (instanceMembers != null)
                {
                    WriteMemberInfoCollection(instanceMembers, depth, true);
                }
            }

            _writer.WriteEndElement();

            return true;
        }

        /// <summary>
        /// Checks if source is known container type and returns appropriate information.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ct"></param>
        /// <param name="dictionary"></param>
        /// <param name="enumerable"></param>
        private static void GetKnownContainerTypeInfo(
            object source, out ContainerType ct, out IDictionary dictionary, out IEnumerable enumerable)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            ct = ContainerType.None;
            dictionary = null;
            enumerable = null;

            dictionary = source as IDictionary;
            if (dictionary != null)
            {
                ct = ContainerType.Dictionary;
                return;
            }

            if (source is Stack)
            {
                ct = ContainerType.Stack;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "Stack is enumerable");
            }
            else if (source is Queue)
            {
                ct = ContainerType.Queue;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "Queue is enumerable");
            }
            else if (source is IList)
            {
                ct = ContainerType.List;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "IList is enumerable");
            }
            else
            {
                Type gt = source.GetType();
                if (gt.GetTypeInfo().IsGenericType)
                {
                    if (DerivesFromGenericType(gt, typeof(Stack<>)))
                    {
                        ct = ContainerType.Stack;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Stack is enumerable");
                    }
                    else if (DerivesFromGenericType(gt, typeof(Queue<>)))
                    {
                        ct = ContainerType.Queue;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Queue is enumerable");
                    }
                    else if (DerivesFromGenericType(gt, typeof(List<>)))
                    {
                        ct = ContainerType.List;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Queue is enumerable");
                    }
                }
            }

            // Check if type is IEnumerable
            if (ct == ContainerType.None)
            {
                enumerable = LanguagePrimitives.GetEnumerable(source);
                if (enumerable != null)
                {
                    ct = ContainerType.Enumerable;
                }
            }
        }

        /// <summary>
        /// Checks if derived is of type baseType or a type derived from baseType.
        /// </summary>
        /// <param name="derived"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        private static bool DerivesFromGenericType(Type derived, Type baseType)
        {
            Dbg.Assert(derived != null, "caller should validate the parameter");
            Dbg.Assert(baseType != null, "caller should validate the parameter");
            while (derived != null)
            {
                if (derived.GetTypeInfo().IsGenericType)
                    derived = derived.GetGenericTypeDefinition();

                if (derived == baseType)
                {
                    return true;
                }

                derived = derived.GetTypeInfo().BaseType;
            }

            return false;
        }

        #region Write PSObject

        /// <summary>
        /// Serializes an PSObject whose baseobject is of primitive type.
        /// and which has notes.
        /// </summary>
        /// <param name="source">
        /// Source from which notes are written.
        /// </param>
        /// <param name="primitive">
        /// primitive object which is written as base object. In most cases it
        /// is same source.ImmediateBaseObject. When PSObject is serialized as string,
        /// it can be different. <see cref="HandlePSObjectAsString"/> for more info.
        /// </param>
        /// <param name="pktInfo">
        /// TypeSerializationInfo for the primitive.
        /// </param>
        /// <param name="property"></param>
        /// <param name="depth"></param>
        private void WritePrimitiveTypePSObjectWithNotes(
            PSObject source,
            object primitive,
            TypeSerializationInfo pktInfo,
            string property,
            int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            // Write start of PSObject. Since baseobject is primitive known
            // type, we do not need TypeName information.
            WriteStartOfPSObject(source, property, source.ToStringFromDeserialization != null);

            if (pktInfo != null)
            {
                WriteOnePrimitiveKnownType(_writer, null, primitive, pktInfo);
            }

            // Serialize instanceMembers
            PSMemberInfoCollection<PSMemberInfo> instanceMembers = source.InstanceMembers;
            if (instanceMembers != null)
            {
                WriteMemberInfoCollection(instanceMembers, depth, true);
            }

            _writer.WriteEndElement();
        }

        private void HandleComplexTypePSObject(PSObject source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            WriteStartOfPSObject(source, property, true);

            // Figure out what kind of object we are dealing with
            bool isEnum = false;
            bool isPSObject = false;

            if (!source.ImmediateBaseObjectIsEmpty)
            {
                isEnum = source.ImmediateBaseObject is Enum;
                isPSObject = source.ImmediateBaseObject is PSObject;
            }

            if (isEnum)
            {
                object baseObject = source.ImmediateBaseObject;
                foreach (PSPropertyInfo prop in source.Properties)
                {
                    WriteOneObject(System.Convert.ChangeType(baseObject, Enum.GetUnderlyingType(baseObject.GetType()), System.Globalization.CultureInfo.InvariantCulture), prop.Name, depth);
                }
            }
            else if (isPSObject)
            {
                if (_firstobjectcall)
                {
                    _firstobjectcall = false;
                    WritePSObjectProperties(source, depth);
                }
                else
                {
                    WriteOneObject(source.ImmediateBaseObject, null, depth);
                }
            }
            else
            {
                WritePSObjectProperties(source, depth);
            }

            _writer.WriteEndElement();
        }

        /// <summary>
        /// Writes start element, attributes and typeNames for PSObject.
        /// </summary>
        /// <param name="mshObject"></param>
        /// <param name="property"></param>
        /// <param name="writeTNH">
        /// if true, TypeName information is written, else not.
        /// </param>
        private void WriteStartOfPSObject(
            PSObject mshObject,
            string property,
            bool writeTNH)
        {
            Dbg.Assert(mshObject != null, "caller should validate the parameter");

            if (property != null)
            {
                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(_writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(_writer, CustomSerializationStrings.Properties);
                }
            }

            object baseObject = mshObject.BaseObject;
            if (!_notypeinformation)
                WriteAttribute(_writer, CustomSerializationStrings.TypeAttribute, baseObject.GetType().ToString());
        }

        #region membersets

        /// <summary>
        /// Returns true if PSObject has notes.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>
        /// </returns>
        private static bool PSObjectHasNotes(PSObject source)
        {
            if (source.InstanceMembers != null && source.InstanceMembers.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serialize member set. This method serializes without writing.
        /// enclosing tags and attributes.
        /// </summary>
        /// <param name="me">
        /// Enumerable containing members
        /// </param>
        /// <param name="depth"></param>
        /// <param name="writeEnclosingMemberSetElementTag">
        /// if this is true, write an enclosing "<memberset></memberset>" tag.
        /// </param>
        /// <returns></returns>
        private void WriteMemberInfoCollection(
            PSMemberInfoCollection<PSMemberInfo> me, int depth, bool writeEnclosingMemberSetElementTag)
        {
            Dbg.Assert(me != null, "caller should validate the parameter");

            foreach (PSMemberInfo info in me)
            {
                if (!info.ShouldSerialize)
                {
                    continue;
                }

                if (!(info is PSPropertyInfo property))
                {
                    continue;
                }

                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, info.Name);
                if (!_notypeinformation)
                    WriteAttribute(_writer, CustomSerializationStrings.TypeAttribute, info.GetType().ToString());
                _writer.WriteString(property.Value.ToString());
                _writer.WriteEndElement();
            }
        }

        #endregion membersets

        #region properties

        /// <summary>
        /// Serializes properties of PSObject.
        /// </summary>
        private void WritePSObjectProperties(PSObject source, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the information");

            depth = GetDepthOfSerialization(source, depth);

            // Depth available for each property is one less
            --depth;
            Dbg.Assert(depth >= 0, "depth should be greater or equal to zero");
            if (source.GetSerializationMethod(null) == SerializationMethod.SpecificProperties)
            {
                PSMemberInfoInternalCollection<PSPropertyInfo> specificProperties = new();
                foreach (string propertyName in source.GetSpecificPropertiesToSerialize(null))
                {
                    PSPropertyInfo property = source.Properties[propertyName];
                    if (property != null)
                    {
                        specificProperties.Add(property);
                    }
                }

                SerializeProperties(specificProperties, CustomSerializationStrings.Properties, depth);
                return;
            }

            foreach (PSPropertyInfo prop in source.Properties)
            {
                Dbg.Assert(prop != null, "propertyCollection should only have member of type PSProperty");
                object value = AutomationNull.Value;
                // PSObject throws GetValueException if it cannot
                // get value for a property.
                try
                {
                    value = prop.Value;
                }
                catch (GetValueException)
                {
                    WritePropertyWithNullValue(_writer, prop, depth);
                    continue;
                }
                // Write the property
                if (value == null)
                {
                    WritePropertyWithNullValue(_writer, prop, depth);
                }
                else
                {
                    WriteOneObject(value, prop.Name, depth);
                }
            }
        }

        /// <summary>
        /// Serializes properties from collection.
        /// </summary>
        /// <param name="propertyCollection">
        /// Collection of properties to serialize.
        /// </param>
        /// <param name="name">
        /// Name for enclosing element tag.
        /// </param>
        /// <param name="depth">
        /// Depth to which each property should be serialized.
        /// </param>
        private void SerializeProperties(
            PSMemberInfoInternalCollection<PSPropertyInfo> propertyCollection, string name, int depth)
        {
            Dbg.Assert(propertyCollection != null, "caller should validate the parameter");
            if (propertyCollection.Count == 0)
                return;

            foreach (PSMemberInfo info in propertyCollection)
            {
                PSPropertyInfo prop = info as PSPropertyInfo;

                Dbg.Assert(prop != null, "propertyCollection should only have member of type PSProperty");

                object value = AutomationNull.Value;
                // PSObject throws GetValueException if it cannot
                // get value for a property.
                try
                {
                    value = prop.Value;
                }
                catch (GetValueException)
                {
                    continue;
                }
                // Write the property
                WriteOneObject(value, prop.Name, depth);
            }
        }

        #endregion base properties

        #endregion WritePSObject

        #region enumerable and dictionary

        /// <summary>
        /// Serializes IEnumerable.
        /// </summary>
        /// <param name="enumerable">
        /// Enumerable which is serialized.
        /// </param>
        /// <param name="depth"></param>
        private void WriteEnumerable(IEnumerable enumerable, int depth)
        {
            Dbg.Assert(enumerable != null, "caller should validate the parameter");

            IEnumerator enumerator = null;
            try
            {
                enumerator = enumerable.GetEnumerator();
                enumerator.Reset();
            }
            catch (Exception)
            {
                enumerator = null;
            }

            // AD has incorrect implementation of IEnumerable where they returned null
            // for GetEnumerator instead of empty enumerator
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
                        else
                        {
                            item = enumerator.Current;
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    WriteOneObject(item, null, depth);
                }
            }
        }

        /// <summary>
        /// Serializes IDictionary.
        /// </summary>
        /// <param name="dictionary">Dictionary which is serialized.</param>
        /// <param name="depth"></param>
        private void WriteDictionary(IDictionary dictionary, int depth)
        {
            IDictionaryEnumerator dictionaryEnum = null;
            try
            {
                dictionaryEnum = (IDictionaryEnumerator)dictionary.GetEnumerator();
            }
            catch (Exception)
            {
            }

            if (dictionaryEnum != null)
            {
                while (dictionaryEnum.MoveNext())
                {
                    // Write Key
                    WriteOneObject(dictionaryEnum.Key, CustomSerializationStrings.DictionaryKey, depth);
                    // Write Value
                    WriteOneObject(dictionaryEnum.Value, CustomSerializationStrings.DictionaryValue, depth);
                }
            }
        }

        #endregion enumerable and dictionary

        #region serialize as string

        private void HandlePSObjectAsString(PSObject source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the information");

            bool hasNotes = PSObjectHasNotes(source);
            string value = GetStringFromPSObject(source);

            if (value != null)
            {
                TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(value.GetType());
                Dbg.Assert(pktInfo != null, "TypeSerializationInfo should be present for string");
                if (hasNotes)
                {
                    WritePrimitiveTypePSObjectWithNotes(source, value, pktInfo, property, depth);
                }
                else
                {
                    WriteOnePrimitiveKnownType(_writer, property, source.BaseObject, pktInfo);
                }
            }
            else
            {
                if (hasNotes)
                {
                    WritePrimitiveTypePSObjectWithNotes(source, null, null, property, depth);
                }
                else
                {
                    WriteNull(property);
                }
            }
        }

        /// <summary>
        /// Gets the string from PSObject using the information from
        /// types.ps1xml. This string is used for serializing the PSObject.
        /// </summary>
        /// <param name="source">
        /// PSObject to be converted to string.
        /// </param>
        /// <returns>
        /// string value to use for serializing this PSObject.
        /// </returns>
        private static string GetStringFromPSObject(PSObject source)
        {
            Dbg.Assert(source != null, "caller should have validated the information");

            // check if we have a well known string serialization source
            PSPropertyInfo serializationProperty = source.GetStringSerializationSource(null);
            string result = null;
            if (serializationProperty != null)
            {
                object val = serializationProperty.Value;
                if (val != null)
                {
                    try
                    {
                        // if we have a string serialization value, return it
                        result = val.ToString();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                try
                {
                    // fall back value
                    result = source.ToString();
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the information the PSObject
        /// and returns true if this object should be serialized as string.
        /// </summary>
        /// <param name="source">PSObject to be serialized.</param>
        /// <returns>True if the object needs to be serialized as a string.</returns>
        private static bool SerializeAsString(PSObject source)
        {
            return source.GetSerializationMethod(null) == SerializationMethod.String;
        }

        #endregion serialize as string

        /// <summary>
        /// Compute the serialization depth for an PSObject instance subtree.
        /// </summary>
        /// <param name="source">PSObject whose serialization depth has to be computed.</param>
        /// <param name="depth">Current depth.</param>
        /// <returns></returns>
        private static int GetDepthOfSerialization(PSObject source, int depth)
        {
            if (source == null)
                return depth;

            // get the depth from the PSObject
            // NOTE: we assume that the depth out of the PSObject is > 0
            // else we consider it not set in types.ps1xml
            int objectLevelDepth = source.GetSerializationDepth(null);
            if (objectLevelDepth <= 0)
            {
                // no override at the type level
                return depth;
            }

            return objectLevelDepth;
        }

        /// <summary>
        /// Writes null.
        /// </summary>
        /// <param name="property"></param>
        private void WriteNull(string property)
        {
            if (property != null)
            {
                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(_writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(_writer, CustomSerializationStrings.Properties);
                }
            }

            _writer.WriteEndElement();
        }

        #region known type serialization

        private void WritePropertyWithNullValue(
            XmlWriter writer, PSPropertyInfo source, int depth)
        {
            WriteStartElement(writer, CustomSerializationStrings.Properties);
            WriteAttribute(writer, CustomSerializationStrings.NameAttribute, ((PSPropertyInfo)source).Name);
            if (!_notypeinformation)
                WriteAttribute(writer, CustomSerializationStrings.TypeAttribute, ((PSPropertyInfo)source).TypeNameOfValue);
            writer.WriteEndElement();
        }

        private void WriteObjectString(
            XmlWriter writer, string property, object source, TypeSerializationInfo entry)
        {
            if (property != null)
            {
                WriteStartElement(writer, CustomSerializationStrings.Properties);
                WriteAttribute(writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(writer, CustomSerializationStrings.Properties);
                }
            }

            if (!_notypeinformation)
                WriteAttribute(writer, CustomSerializationStrings.TypeAttribute, source.GetType().ToString());

            writer.WriteString(source.ToString());
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes an item or property in Monad namespace.
        /// </summary>
        /// <param name="writer">The XmlWriter stream to which the object is serialized.</param>
        /// <param name="property">Name of property. Pass null for item.</param>
        /// <param name="source">Object to be written.</param>
        /// <param name="entry">Serialization information about source.</param>
        private void WriteOnePrimitiveKnownType(
            XmlWriter writer, string property, object source, TypeSerializationInfo entry)
        {
            WriteObjectString(writer, property, source, entry);
        }

        #endregion known type serialization

        #region misc

        /// <summary>
        /// Writes start element in Monad namespace.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="elementTag">Tag of element.</param>
        internal static void WriteStartElement(XmlWriter writer, string elementTag)
        {
            writer.WriteStartElement(elementTag);
        }

        /// <summary>
        /// Writes attribute in monad namespace.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="name">Name of attribute.</param>
        /// <param name="value">Value of attribute.</param>
        internal static void WriteAttribute(XmlWriter writer, string name, string value)
        {
            writer.WriteAttributeString(name, value);
        }

        #endregion misc
    }
}
