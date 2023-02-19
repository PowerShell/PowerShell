// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell;

using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    /// <summary>
    /// Deals with ManagementObject objects.
    /// This is a base class that interacts with other entities.
    /// </summary>
    internal abstract class BaseWMIAdapter : Adapter
    {
        #region WMIMethodCacheEntry

        /// <summary>
        /// Method information is cached for every unique ManagementClassPath created/used.
        /// This structure stores information such as MethodDefinition as displayed
        /// by Get-Member cmdlet, original MethodData and computed method information such
        /// as whether a method is static etc.
        /// </summary>
        internal class WMIMethodCacheEntry : CacheEntry
        {
            public string Name { get; }

            public string ClassPath { get; }

            public MethodInformation MethodInfoStructure { get; }

            public string MethodDefinition { get; }

            internal WMIMethodCacheEntry(string n, string cPath, MethodData mData)
            {
                Name = n;
                ClassPath = cPath;
                MethodInfoStructure = ManagementObjectAdapter.GetMethodInformation(mData);
                MethodDefinition = ManagementObjectAdapter.GetMethodDefinition(mData);
            }
        }

        #endregion

        #region WMIParameterInformation

        internal class WMIParameterInformation : ParameterInformation
        {
            public string Name { get; }

            public WMIParameterInformation(string name, Type ty) : base(ty, true, null, false)
            {
                Name = name;
            }
        }

        #endregion

        #region Member related Overrides

        /// <summary>
        /// Returns the TypeNameHierarchy using the __Derivation SystemProperties
        /// and dotnetBaseType.
        /// </summary>
        /// <param name="managementObj"></param>
        /// <param name="dotnetBaseType"></param>
        /// <param name="shouldIncludeNamespace"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetTypeNameHierarchyFromDerivation(ManagementBaseObject managementObj,
            string dotnetBaseType, bool shouldIncludeNamespace)
        {
            StringBuilder type = new StringBuilder(200);
            // give the typename based on NameSpace and Class
            type.Append(dotnetBaseType);
            type.Append('#');
            if (shouldIncludeNamespace)
            {
                type.Append(managementObj.SystemProperties["__NAMESPACE"].Value);
                type.Append('\\');
            }

            type.Append(managementObj.SystemProperties["__CLASS"].Value);
            yield return type.ToString();

            // Win8: 186792: PSTypeNames does not include full WMI class derivation
            // From MSDN: __Derivation; Data type: CIM_STRING array
            // Access type: Read-only for both instances and classes
            // Class hierarchy of the current class or instance. The first element is
            // the immediate parent class, the next is its parent, and so on; the last element
            // is the base class.
            PropertyData derivationData = managementObj.SystemProperties["__Derivation"];
            if (derivationData != null)
            {
                Dbg.Assert(derivationData.IsArray, "__Derivation must be a string array as per MSDN documentation");

                // give the typenames based on NameSpace + __Derivation
                string[] typeHierarchy = PropertySetAndMethodArgumentConvertTo(derivationData.Value, typeof(string[]), CultureInfo.InvariantCulture) as string[];
                if (typeHierarchy != null)
                {
                    foreach (string t in typeHierarchy)
                    {
                        type.Clear();
                        type.Append(dotnetBaseType);
                        type.Append('#');
                        if (shouldIncludeNamespace)
                        {
                            type.Append(managementObj.SystemProperties["__NAMESPACE"].Value);
                            type.Append('\\');
                        }

                        type.Append(t);
                        yield return type.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the TypeNameHierarchy out of an ManagementBaseObject.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        /// <remarks>
        /// TypeName is of the format ObjectType#__Namespace\\__Class
        /// </remarks>
        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            ManagementBaseObject managementObj = obj as ManagementBaseObject;

            bool isLoopStarted = false;
            foreach (string baseType in GetDotNetTypeNameHierarchy(obj))
            {
                if (!isLoopStarted)
                {
                    isLoopStarted = true;
                    // Win8: 186792 Return the hierarchy using the __Derivation property as well
                    // as NameSpace + Class.
                    foreach (string typeFromDerivation in GetTypeNameHierarchyFromDerivation(managementObj, baseType, true))
                    {
                        yield return typeFromDerivation;
                    }

                    // without namespace
                    foreach (string typeFromDerivation in GetTypeNameHierarchyFromDerivation(managementObj, baseType, false))
                    {
                        yield return typeFromDerivation;
                    }
                }

                yield return baseType;
            }
        }

        /// <summary>
        /// Returns null if memberName is not a member in the adapter or
        /// the corresponding PSMemberInfo.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="memberName">Name of the member to be retrieved.</param>
        /// <returns>The PSMemberInfo corresponding to memberName from obj.</returns>
        protected override T GetMember<T>(object obj, string memberName)
        {
            tracer.WriteLine("Getting member with name {0}", memberName);

            if (!(obj is ManagementBaseObject mgmtObject))
            {
                return null;
            }

            PSProperty property = DoGetProperty(mgmtObject, memberName);

            if (typeof(T).IsAssignableFrom(typeof(PSProperty)) && property != null)
            {
                return property as T;
            }

            if (typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                T returnValue = GetManagementObjectMethod<T>(mgmtObject, memberName);
                // We only return a method if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (returnValue != null && property == null)
                {
                    return returnValue;
                }
            }

            return null;
        }

        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            if (obj is ManagementBaseObject wmiObject)
            {
                return GetFirstOrDefaultProperty<T>(wmiObject, predicate)
                    ?? GetFirstOrDefaultMethod<T>(wmiObject, predicate);
            }

            return null;
        }

        /// <summary>
        /// Retrieves all the members available in the object.
        /// The adapter implementation is encouraged to cache all properties/methods available
        /// in the first call to GetMember and GetMembers so that subsequent
        /// calls can use the cache.
        /// In the case of the .NET adapter that would be a cache from the .NET type to
        /// the public properties and fields available in that type.
        /// In the case of the DirectoryEntry adapter, this could be a cache of the objectClass
        /// to the properties available in it.
        /// </summary>
        /// <param name="obj">Object to get all the member information from.</param>
        /// <returns>All members in obj.</returns>
        protected override PSMemberInfoInternalCollection<T> GetMembers<T>(object obj)
        {
            // obj should never be null
            Diagnostics.Assert(obj != null, "Input object is null");

            ManagementBaseObject wmiObject = (ManagementBaseObject)obj;
            PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();
            AddAllProperties<T>(wmiObject, returnValue);
            AddAllMethods<T>(wmiObject, returnValue);
            return returnValue;
        }

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, object[] arguments)
        {
            ManagementObject mgmtObject = method.baseObject as ManagementObject;
            Diagnostics.Assert(mgmtObject != null,
                "Object is not of ManagementObject type");

            WMIMethodCacheEntry methodEntry = (WMIMethodCacheEntry)method.adapterData;

            return AuxillaryInvokeMethod(mgmtObject, methodEntry, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="method">The return of GetMember.</param>
        /// <returns></returns>
        protected override Collection<string> MethodDefinitions(PSMethod method)
        {
            WMIMethodCacheEntry methodEntry = (WMIMethodCacheEntry)method.adapterData;
            Collection<string> returnValue = new Collection<string>();
            returnValue.Add(methodEntry.MethodDefinition);

            return returnValue;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            ManagementBaseObject mObj = property.baseObject as ManagementBaseObject;
            try
            {
                ManagementClass objClass = CreateClassFrmObject(mObj);
                return (bool)objClass.GetPropertyQualifierValue(property.Name, "Write");
            }
            catch (ManagementException)
            {
                // A property that lacks the Write qualifier may still be writeable.
                // The provider implementation may allow any properties in the provider
                // classes to be changed, whether the Write qualifier is present or not.
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // A property that lacks the Write qualifier may still be writeable.
                // The provider implementation may allow any properties in the provider
                // classes to be changed, whether the Write qualifier is present or not.
                return true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // A property that lacks the Write qualifier may still be writeable.
                // The provider implementation may allow any properties in the provider
                // classes to be changed, whether the Write qualifier is present or not.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            return true;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous DoGetProperty.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the property.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            PropertyData pd = property.adapterData as PropertyData;
            // GetDotNetType will never return null
            Type dotNetType = GetDotNetType(pd);
            string typeName;

            // Display Embedded object type name to
            // assist users in passing appropriate
            // object
            if (pd.Type == CimType.Object)
            {
                typeName = GetEmbeddedObjectTypeName(pd);

                if (pd.IsArray)
                {
                    typeName += "[]";
                }
            }
            else
            {
                typeName = forDisplay ? ToStringCodeMethods.Type(dotNetType) : dotNetType.ToString();
            }

            return typeName;
        }

        /// <summary>
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            PropertyData pd = property.adapterData as PropertyData;
            return pd.Value;
        }
        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// This method will only set the property on a particular instance. If you want
        /// to update the WMI store, call Put().
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            if (!(property.baseObject is ManagementBaseObject mObj))
            {
                throw new SetValueInvocationException("CannotSetNonManagementObjectMsg",
                    null,
                    ExtendedTypeSystem.CannotSetNonManagementObject,
                    property.Name, property.baseObject.GetType().FullName,
                    typeof(ManagementBaseObject).FullName);
            }

            if (!PropertyIsSettable(property))
            {
                throw new SetValueException("ReadOnlyWMIProperty",
                        null,
                        ExtendedTypeSystem.ReadOnlyProperty,
                        property.Name);
            }

            PropertyData pd = property.adapterData as PropertyData;

            if ((convertIfPossible) && (setValue != null))
            {
                // Convert only if value is not null
                Type paramType = GetDotNetType(pd);
                setValue = PropertySetAndMethodArgumentConvertTo(
                    setValue, paramType, CultureInfo.InvariantCulture);
            }

            pd.Value = setValue;
            return;
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected override string PropertyToString(PSProperty property)
        {
            StringBuilder returnValue = new StringBuilder();
            // if (PropertyIsStatic(property))
            // {
            //    returnValue.Append("static ");
            // }

            returnValue.Append(PropertyType(property, forDisplay: true));
            returnValue.Append(' ');
            returnValue.Append(property.Name);
            returnValue.Append(" {");
            if (PropertyIsGettable(property))
            {
                returnValue.Append("get;");
            }

            if (PropertyIsSettable(property))
            {
                returnValue.Append("set;");
            }

            returnValue.Append('}');
            return returnValue.ToString();
        }

        /// <summary>
        /// Returns an array with the property attributes.
        /// </summary>
        /// <param name="property">Property we want the attributes from.</param>
        /// <returns>An array with the property attributes.</returns>
        protected override AttributeCollection PropertyAttributes(PSProperty property)
        {
            return null;
        }

        #endregion

        #region Private/Internal Methods

        /// <summary>
        /// Retrieves the table for instance methods.
        /// </summary>
        /// <param name="wmiObject">Object containing methods to load in typeTable.</param>
        /// <param name="staticBinding">Controls what methods are adapted.</param>
        protected static CacheTable GetInstanceMethodTable(ManagementBaseObject wmiObject,
            bool staticBinding)
        {
            lock (s_instanceMethodCacheTable)
            {
                CacheTable typeTable = null;

                // unique identifier for identifying this ManagementObject's type
                ManagementPath classPath = wmiObject.ClassPath;
                string key = string.Create(CultureInfo.InvariantCulture, $"{classPath.Path}#{staticBinding}");

                typeTable = (CacheTable)s_instanceMethodCacheTable[key];
                if (typeTable != null)
                {
                    tracer.WriteLine("Returning method information from internal cache");
                    return typeTable;
                }

                tracer.WriteLine("Method information not found in internal cache. Constructing one");

                try
                {
                    // try to populate method table..if there is any exception
                    // generating the method metadata..suppress the exception
                    // but dont store the info in the cache. This is to allow
                    // for method look up again in future (after the wmi object
                    // is fixed)
                    typeTable = new CacheTable();
                    // Construct a ManagementClass object for this object to get the member metadata
                    ManagementClass mgmtClass = wmiObject as ManagementClass ?? CreateClassFrmObject(wmiObject);
                    PopulateMethodTable(mgmtClass, typeTable, staticBinding);
                    s_instanceMethodCacheTable[key] = typeTable;
                }
                catch (ManagementException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                }

                return typeTable;
            }
        }

        /// <summary>
        /// Populates methods of a ManagementClass in a CacheTable.
        /// </summary>
        /// <param name="mgmtClass">Class to get the method info from.</param>
        /// <param name="methodTable">Cachetable to update.</param>
        /// <param name="staticBinding">Controls what methods are adapted.</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ManagementException"></exception>
        private static void PopulateMethodTable(ManagementClass mgmtClass, CacheTable methodTable, bool staticBinding)
        {
            Dbg.Assert(mgmtClass != null, "ManagementClass cannot be null in this method");
            MethodDataCollection mgmtMethods = mgmtClass.Methods;
            if (mgmtMethods != null)
            {
                ManagementPath classPath = mgmtClass.ClassPath;
                // new operation will never fail
                foreach (MethodData mdata in mgmtMethods)
                {
                    // is method static
                    bool isStatic = IsStaticMethod(mdata);
                    if (isStatic == staticBinding)
                    {
                        // a method is added depending on
                        // whether staticBinding is requested or not.
                        string methodName = mdata.Name;
                        WMIMethodCacheEntry mCache = new WMIMethodCacheEntry(methodName, classPath.Path, mdata);
                        methodTable.Add(methodName, mCache);
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a ManagementClass object from the supplied mgmtBaseObject.
        /// ManagementObject has scope, options, path which need to be carried over to the ManagementClass for
        /// retrieving method/property/parameter metadata.
        /// </summary>
        /// <param name="mgmtBaseObject"></param>
        /// <returns></returns>
        private static ManagementClass CreateClassFrmObject(ManagementBaseObject mgmtBaseObject)
        {
            // Construct a ManagementClass object for this object to get the member metadata
            ManagementClass mgmtClass = mgmtBaseObject as ManagementClass;

            // try to use the actual object sent to this method..otherwise construct one
            if (mgmtClass == null)
            {
                mgmtClass = new ManagementClass(mgmtBaseObject.ClassPath);

                // inherit ManagementObject properties
                ManagementObject mgmtObject = mgmtBaseObject as ManagementObject;
                if (mgmtObject != null)
                {
                    mgmtClass.Scope = mgmtObject.Scope;
                    mgmtClass.Options = mgmtObject.Options;
                }
            }

            return mgmtClass;
        }

        /// <summary>
        /// Gets the object type associated with a CimType:object.
        /// </summary>
        /// <param name="pData">PropertyData representing a parameter.</param>
        /// <returns>
        /// typeof(object)#EmbeddedObjectTypeName if one found
        /// typeof(object) otherwise
        /// </returns>
        /// <remarks>
        /// This helps users of WMI in identifying exactly what type
        /// the underlying WMI provider will accept.
        /// </remarks>
        protected static string GetEmbeddedObjectTypeName(PropertyData pData)
        {
            string result = typeof(object).FullName;

            if (pData == null)
            {
                return result;
            }

            try
            {
                string cimType = (string)pData.Qualifiers["cimtype"].Value;
                result = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}#{1}",
                    typeof(ManagementObject).FullName,
                    cimType.Replace("object:", string.Empty));
            }
            catch (ManagementException)
            {
            }
            catch (System.Runtime.InteropServices.COMException)
            {
            }

            return result;
        }

        /// <summary>
        /// Gets the dotnet type of a given PropertyData.
        /// </summary>
        /// <param name="pData">PropertyData input.</param>
        /// <returns>A string representing dotnet type.</returns>
        protected static Type GetDotNetType(PropertyData pData)
        {
            Diagnostics.Assert(pData != null,
                "Input PropertyData should not be null");

            tracer.WriteLine("Getting DotNet Type for CimType : {0}", pData.Type);

            string retValue;
            switch (pData.Type)
            {
                case CimType.SInt8:
                    retValue = typeof(sbyte).FullName;
                    break;
                case CimType.UInt8:
                    retValue = typeof(byte).FullName;
                    break;
                case CimType.SInt16:
                    retValue = typeof(System.Int16).FullName;
                    break;
                case CimType.UInt16:
                    retValue = typeof(System.UInt16).FullName;
                    break;
                case CimType.SInt32:
                    retValue = typeof(System.Int32).FullName;
                    break;
                case CimType.UInt32:
                    retValue = typeof(System.UInt32).FullName;
                    break;
                case CimType.SInt64:
                    retValue = typeof(System.Int64).FullName;
                    break;
                case CimType.UInt64:
                    retValue = typeof(System.UInt64).FullName;
                    break;
                case CimType.Real32:
                    retValue = typeof(Single).FullName;
                    break;
                case CimType.Real64:
                    retValue = typeof(double).FullName;
                    break;
                case CimType.Boolean:
                    retValue = typeof(bool).FullName;
                    break;
                case CimType.String:
                    retValue = typeof(string).FullName;
                    break;
                case CimType.DateTime:
                    // this is actually a string
                    retValue = typeof(string).FullName;
                    break;
                case CimType.Reference:
                    // this is actually a string
                    retValue = typeof(string).FullName;
                    break;
                case CimType.Char16:
                    retValue = typeof(char).FullName;
                    break;
                case CimType.Object:
                default:
                    retValue = typeof(object).FullName;
                    break;
            }

            if (pData.IsArray)
            {
                retValue += "[]";
            }

            return Type.GetType(retValue);
        }

        /// <summary>
        /// Checks whether a given MethodData is static or not.
        /// </summary>
        /// <param name="mdata"></param>
        /// <returns>
        /// true, if static
        /// </returns>
        /// <remarks>
        /// This method relies on the qualifier "static"
        /// </remarks>
        protected static bool IsStaticMethod(MethodData mdata)
        {
            try
            {
                QualifierData staticQualifier = mdata.Qualifiers["static"];
                if (staticQualifier == null)
                    return false;

                bool result = false;
                LanguagePrimitives.TryConvertTo<bool>(staticQualifier.Value, out result);

                return result;
            }
            catch (ManagementException)
            {
            }
            catch (System.Runtime.InteropServices.COMException)
            {
            }

            return false;
        }

        private object AuxillaryInvokeMethod(ManagementObject obj, WMIMethodCacheEntry mdata, object[] arguments)
        {
            // Evaluate method and arguments
            object[] verifiedArguments;

            MethodInformation[] methods = new MethodInformation[1];
            methods[0] = mdata.MethodInfoStructure;

            // This will convert Null Strings to Empty Strings
            GetBestMethodAndArguments(mdata.Name, methods, arguments, out verifiedArguments);

            ParameterInformation[] parameterList = mdata.MethodInfoStructure.parameters;

            // GetBestMethodAndArguments should fill verifiedArguments with
            // correct values (even if some values are not specified)
            tracer.WriteLine("Parameters found {0}. Arguments supplied {0}",
                parameterList.Length, verifiedArguments.Length);

            Diagnostics.Assert(parameterList.Length == verifiedArguments.Length,
                "The number of parameters and arguments should match");

            // we should not cache inParameters as we are updating
            // inParameters object with argument values..Caching will
            // have side effects in this scenario like we have to clear
            // the values once the method is invoked.
            // Also caching MethodData occupies lot of memory compared to
            // caching string.
            ManagementClass mClass = CreateClassFrmObject(obj);
            ManagementBaseObject inParameters = mClass.GetMethodParameters(mdata.Name);

            for (int i = 0; i < parameterList.Length; i++)
            {
                // this cast should always succeed
                WMIParameterInformation pInfo = (WMIParameterInformation)parameterList[i];

                // Should not convert null input arguments
                // GetBestMethodAndArguments converts null strings to empty strings
                // and also null ints to 0. But WMI providers do not like these
                // conversions. So dont convert input arguments if they are null.
                // We could have done this in the base adapter but the change would be
                // costly for other adapters which dont mind the conversion.
                if ((i < arguments.Length) && (arguments[i] == null))
                {
                    verifiedArguments[i] = null;
                }

                inParameters[pInfo.Name] = verifiedArguments[i];
            }

            return InvokeManagementMethod(obj, mdata.Name, inParameters);
        }

        /// <summary>
        /// Decode parameter information from the supplied object.
        /// </summary>
        /// <param name="parameters">A ManagementBaseObject describing the parameters.</param>
        /// <param name="parametersList">A sorted list to store parameter information.</param>
        /// <remarks>
        /// Should not throw exceptions
        /// </remarks>
        internal static void UpdateParameters(ManagementBaseObject parameters,
            SortedList<int, WMIParameterInformation> parametersList)
        {
            // ManagementObject class do not populate parameters when there are none.
            if (parameters == null)
                return;

            foreach (PropertyData data in parameters.Properties)
            {
                // parameter position..
                int location = -1;
                WMIParameterInformation pInfo = new WMIParameterInformation(data.Name, GetDotNetType(data));

                try
                {
                    location = (int)data.Qualifiers["ID"].Value;
                }
                catch (ManagementException)
                {
                    // If there is an exception accessing location
                    // add the parameter to the end.
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // If there is an exception accessing location
                    // add the parameter to the end.
                }

                if (location < 0)
                {
                    location = parametersList.Count;
                }

                parametersList[location] = pInfo;
            }
        }

        /// <summary>
        /// Gets WMI method information.
        /// </summary>
        /// <param name="mData"></param>
        /// <returns></returns>
        /// <remarks>
        /// Decodes only input parameters.
        /// </remarks>
        internal static MethodInformation GetMethodInformation(MethodData mData)
        {
            Diagnostics.Assert(mData != null, "MethodData should not be null");

            // Get Method parameters
            var parameters = new SortedList<int, WMIParameterInformation>();
            UpdateParameters(mData.InParameters, parameters);

            // parameters is never null
            WMIParameterInformation[] pInfos = new WMIParameterInformation[parameters.Count];
            if (parameters.Count > 0)
            {
                parameters.Values.CopyTo(pInfos, 0);
            }

            MethodInformation returnValue = new MethodInformation(false, true, pInfos);

            return returnValue;
        }

        internal static string GetMethodDefinition(MethodData mData)
        {
            // gather parameter information for this method.
            // input and output parameters reside in 2 different groups..
            // we dont know the order they appear on the arguments line..
            var parameters = new SortedList<int, WMIParameterInformation>();
            UpdateParameters(mData.InParameters, parameters);

            StringBuilder inParameterString = new StringBuilder();

            if (parameters.Count > 0)
            {
                for (int i = 0; i < parameters.Values.Count; i++)
                {
                    WMIParameterInformation parameter = parameters.Values[i];
                    string typeName = parameter.parameterType.ToString();

                    PropertyData pData = mData.InParameters.Properties[parameter.Name];
                    if (pData.Type == CimType.Object)
                    {
                        typeName = GetEmbeddedObjectTypeName(pData);

                        if (pData.IsArray)
                        {
                            typeName += "[]";
                        }
                    }

                    inParameterString.Append(typeName);
                    inParameterString.Append(' ');
                    inParameterString.Append(parameter.Name);
                    inParameterString.Append(", ");
                }
            }

            if (inParameterString.Length > 2)
            {
                inParameterString.Remove(inParameterString.Length - 2, 2);
            }

            tracer.WriteLine("Constructing method definition for method {0}", mData.Name);
            StringBuilder builder = new StringBuilder();

            builder.Append("System.Management.ManagementBaseObject ");
            builder.Append(mData.Name);
            builder.Append('(');
            builder.Append(inParameterString);
            builder.Append(')');

            string returnValue = builder.ToString();
            tracer.WriteLine("Definition constructed: {0}", returnValue);

            return returnValue;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        /// <param name="wmiObject">Object to get all the property information from.</param>
        /// <param name="members">Collection where the members will be added.</param>
        protected abstract void AddAllProperties<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members) where T : PSMemberInfo;

        /// <summary>
        /// Adds method information of the ManagementObject. This is done by accessing
        /// the ManagementClass corresponding to this ManagementObject. All the method
        /// information is cached for a particular ManagementObject.
        /// </summary>
        /// <typeparam name="T">PSMemberInfo</typeparam>
        /// <param name="wmiObject">Object for which the members need to be retrieved.</param>
        /// <param name="members">Method information is added to this.</param>
        protected abstract void AddAllMethods<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members) where T : PSMemberInfo;

        protected abstract object InvokeManagementMethod(ManagementObject wmiObject,
            string methodName, ManagementBaseObject inParams);

        /// <summary>
        /// Get a method object given method name.
        /// </summary>
        /// <typeparam name="T">PSMemberInfo</typeparam>
        /// <param name="wmiObject">Object for which the method is required.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns>
        /// PsMemberInfo if method exists.
        /// Null otherwise.
        /// </returns>
        protected abstract T GetManagementObjectMethod<T>(ManagementBaseObject wmiObject,
            string methodName) where T : PSMemberInfo;

        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        /// <param name="wmiObject">Object to retrieve the PSProperty from.</param>
        /// <param name="propertyName">Name of the property to be retrieved.</param>
        /// <returns>The PSProperty corresponding to propertyName from obj.</returns>
        protected abstract PSProperty DoGetProperty(ManagementBaseObject wmiObject,
            string propertyName);

        /// <summary>
        /// Returns the first property whose name matches the specified <see cref="MemberNamePredicate"/>
        /// </summary>
        protected abstract T GetFirstOrDefaultProperty<T>(ManagementBaseObject wmiObject, MemberNamePredicate predicate) where T : PSMemberInfo;

        /// <summary>
        /// Returns the first method whose name matches the specified <see cref="MemberNamePredicate"/>
        /// </summary>
        protected abstract T GetFirstOrDefaultMethod<T>(ManagementBaseObject wmiObject, MemberNamePredicate predicate) where T : PSMemberInfo;

        #endregion

        #region Private Data

        private static readonly HybridDictionary s_instanceMethodCacheTable = new HybridDictionary();

        #endregion
    }

    /// <summary>
    /// Deals with ManagementClass objects.
    /// Adapts only static methods and SystemProperties of a
    /// ManagementClass object.
    /// </summary>
    internal class ManagementClassAdapter : BaseWMIAdapter
    {
        protected override void AddAllProperties<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members)
        {
            if (wmiObject.SystemProperties != null)
            {
                foreach (PropertyData property in wmiObject.SystemProperties)
                {
                    members.Add(new PSProperty(property.Name, this, wmiObject, property) as T);
                }
            }
        }

        protected override PSProperty DoGetProperty(ManagementBaseObject wmiObject, string propertyName)
        {
            if (wmiObject.SystemProperties != null)
            {
                foreach (PropertyData property in wmiObject.SystemProperties)
                {
                    if (propertyName.Equals(property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return new PSProperty(property.Name, this, wmiObject, property);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes method represented by <paramref name="mdata"/> using supplied arguments.
        /// </summary>
        /// <param name="wmiObject">ManagementObject on which the method is invoked.</param>
        /// <param name="methodName">Method data.</param>
        /// <param name="inParams">Method arguments.</param>
        /// <returns></returns>
        protected override object InvokeManagementMethod(ManagementObject wmiObject,
            string methodName, ManagementBaseObject inParams)
        {
            tracer.WriteLine("Invoking class method: {0}", methodName);

            ManagementClass mClass = wmiObject as ManagementClass;

            try
            {
                return mClass.InvokeMethod(methodName, inParams, null);
            }
            catch (Exception e)
            {
                throw new MethodInvocationException(
                    "WMIMethodException",
                    e,
                    ExtendedTypeSystem.WMIMethodInvocationException,
                    methodName, e.Message);
            }
        }

        /// <summary>
        /// Adds method information of the ManagementClass. Only static methods are added for
        /// an object of type ManagementClass.
        /// </summary>
        /// <typeparam name="T">PSMemberInfo</typeparam>
        /// <param name="wmiObject">Object for which the members need to be retrieved.</param>
        /// <param name="members">Method information is added to this.</param>
        protected override void AddAllMethods<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members)
        {
            Diagnostics.Assert((wmiObject != null) && (members != null),
                "Input arguments should not be null.");

            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return;
            }

            CacheTable table;
            table = GetInstanceMethodTable(wmiObject, true);

            foreach (WMIMethodCacheEntry methodEntry in table.memberCollection)
            {
                if (members[methodEntry.Name] == null)
                {
                    tracer.WriteLine("Adding method {0}", methodEntry.Name);
                    members.Add(new PSMethod(methodEntry.Name, this, wmiObject, methodEntry) as T);
                }
            }
        }

        /// <summary>
        /// Returns method information for a ManagementClass method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wmiObject"></param>
        /// <param name="methodName"></param>
        /// <returns>
        /// PSMethod if method exists and is static. Null otherwise.
        /// </returns>
        protected override T GetManagementObjectMethod<T>(ManagementBaseObject wmiObject, string methodName)
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return null;
            }

            CacheTable typeTable = GetInstanceMethodTable(wmiObject, true);
            WMIMethodCacheEntry method = (WMIMethodCacheEntry)typeTable[methodName];

            if (method == null)
            {
                return null;
            }

            return new PSMethod(method.Name, this, wmiObject, method) as T;
        }

        protected override T GetFirstOrDefaultProperty<T>(ManagementBaseObject wmiObject, MemberNamePredicate predicate)
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSProperty)))
            {
                return null;
            }

            if (wmiObject.SystemProperties != null)
            {
                foreach (PropertyData property in wmiObject.SystemProperties)
                {
                    if (predicate(property.Name))
                    {
                        return new PSProperty(property.Name, this, wmiObject, property) as T;
                    }
                }
            }

            return null;
        }

        protected override T GetFirstOrDefaultMethod<T>(ManagementBaseObject wmiObject, MemberNamePredicate predicate)
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return null;
            }

            CacheTable table = GetInstanceMethodTable(wmiObject, true);
            foreach (WMIMethodCacheEntry methodEntry in table.memberCollection)
            {
                if (predicate(methodEntry.Name))
                {
                    return new PSMethod(methodEntry.Name, this, wmiObject, methodEntry) as T;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Deals with ManagementObject objects.
    /// This class do not adapt static methods.
    /// </summary>
    internal class ManagementObjectAdapter : ManagementClassAdapter
    {
        protected override void AddAllProperties<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members)
        {
            // Add System properties
            base.AddAllProperties(wmiObject, members);

            if (wmiObject.Properties != null)
            {
                foreach (PropertyData property in wmiObject.Properties)
                {
                    members.Add(new PSProperty(property.Name, this, wmiObject, property) as T);
                }
            }
        }

        protected override PSProperty DoGetProperty(ManagementBaseObject wmiObject, string propertyName)
        {
            PropertyData adapterData = null;

            // First check whether we have any Class properties by this name
            PSProperty returnValue = base.DoGetProperty(wmiObject, propertyName);

            if (returnValue != null)
            {
                return returnValue;
            }

            try
            {
                adapterData = wmiObject.Properties[propertyName];
                return new PSProperty(adapterData.Name, this, wmiObject, adapterData);
            }
            catch (ManagementException)
            {
            }
            catch (Exception e)
            {
                // TODO: Bug 251457. This is a workaround to unblock partners and find out the root cause.
                Tracing.PSEtwLogProvider provider = new Tracing.PSEtwLogProvider();

                provider.WriteEvent(PSEventId.Engine_Health,
                                    PSChannel.Analytic,
                                    PSOpcode.Exception,
                                    PSLevel.Informational,
                                    PSTask.None,
                                    PSKeyword.UseAlwaysOperational,
                                    string.Create(CultureInfo.InvariantCulture, $"ManagementBaseObjectAdapter::DoGetProperty::PropertyName:{propertyName}, Exception:{e.Message}, StackTrace:{e.StackTrace}"),
                                    string.Empty,
                                    string.Empty);
                // ignore the exception.
            }

            return null;
        }

        /// <summary>
        /// Invokes method represented by <paramref name="mdata"/> using supplied arguments.
        /// </summary>
        /// <param name="obj">ManagementObject on which the method is invoked.</param>
        /// <param name="methodName">Method data.</param>
        /// <param name="inParams">Method arguments.</param>
        /// <returns></returns>
        protected override object InvokeManagementMethod(ManagementObject obj, string methodName, ManagementBaseObject inParams)
        {
            tracer.WriteLine("Invoking class method: {0}", methodName);

            try
            {
                ManagementBaseObject robj = obj.InvokeMethod(methodName, inParams, null);
                return robj;
            }
            catch (Exception e)
            {
                throw new MethodInvocationException(
                    "WMIMethodException",
                    e,
                    ExtendedTypeSystem.WMIMethodInvocationException,
                    methodName, e.Message);
            }
        }

        /// <summary>
        /// Adds method information of the ManagementObject. Only instance methods are added for
        /// a ManagementObject.
        /// </summary>
        /// <typeparam name="T">PSMemberInfo</typeparam>
        /// <param name="wmiObject">Object for which the members need to be retrieved.</param>
        /// <param name="members">Method information is added to this.</param>
        protected override void AddAllMethods<T>(ManagementBaseObject wmiObject,
            PSMemberInfoInternalCollection<T> members)
        {
            Diagnostics.Assert((wmiObject != null) && (members != null),
                "Input arguments should not be null.");

            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return;
            }

            CacheTable table;
            table = GetInstanceMethodTable(wmiObject, false);

            foreach (WMIMethodCacheEntry methodEntry in table.memberCollection)
            {
                if (members[methodEntry.Name] == null)
                {
                    tracer.WriteLine("Adding method {0}", methodEntry.Name);
                    members.Add(new PSMethod(methodEntry.Name, this, wmiObject, methodEntry) as T);
                }
            }
        }

        /// <summary>
        /// Returns method information for a ManagementObject method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wmiObject"></param>
        /// <param name="methodName"></param>
        /// <returns>
        /// PSMethod if method exists and is not static. Null otherwise.
        /// </returns>
        protected override T GetManagementObjectMethod<T>(ManagementBaseObject wmiObject, string methodName)
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return null;
            }

            CacheTable typeTable;
            WMIMethodCacheEntry method;

            typeTable = GetInstanceMethodTable(wmiObject, false);
            method = (WMIMethodCacheEntry)typeTable[methodName];

            if (method == null)
            {
                return null;
            }

            return new PSMethod(method.Name, this, wmiObject, method) as T;
        }
    }
}
