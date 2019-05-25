// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.Commands
{
    using Extensions;

    internal static class CIMHelper
    {
        internal static class ClassNames
        {
            internal const string OperatingSystem = "Win32_OperatingSystem";
            internal const string PageFileUsage = "Win32_PageFileUsage";
            internal const string Bios = "Win32_BIOS";
            internal const string BaseBoard = "Win32_BaseBoard";
            internal const string ComputerSystem = "Win32_ComputerSystem";
            internal const string Keyboard = "Win32_Keyboard";
            internal const string DeviceGuard = "Win32_DeviceGuard";
            internal const string HotFix = "Win32_QuickFixEngineering";
            internal const string MicrosoftNetworkAdapter = "MSFT_NetAdapter";
            internal const string NetworkAdapter = "Win32_NetworkAdapter";
            internal const string NetworkAdapterConfiguration = "Win32_NetworkAdapterConfiguration";
            internal const string Processor = "Win32_Processor";
            internal const string PhysicalMemory = "Win32_PhysicalMemory";
            internal const string TimeZone = "Win32_TimeZone";
        }

        internal const string DefaultNamespace = @"root\cimv2";
        internal const string DeviceGuardNamespace = @"root\Microsoft\Windows\DeviceGuard";
        internal const string MicrosoftNetworkAdapterNamespace = "root/StandardCimv2";
        internal const string DefaultQueryDialect = "WQL";

        internal static Dictionary<string, CIMHelper.QueryInfo> QueryableProperties = new Dictionary<string, CIMHelper.QueryInfo>(){
            // Bios properties
            { "BiosCharacteristics", new CIMHelper.QueryInfo("BiosCharacteristics", "BiosCharacteristics", CIMHelper.ClassNames.Bios) },
            { "BiosBIOSVersion", new CIMHelper.QueryInfo("BiosBIOSVersion", "BIOSVersion", CIMHelper.ClassNames.Bios) },
            { "BiosBuildNumber", new CIMHelper.QueryInfo("BiosBuildNumber", "BuildNumber", CIMHelper.ClassNames.Bios) },
            { "BiosCaption", new CIMHelper.QueryInfo("BiosCaption", "Caption", CIMHelper.ClassNames.Bios) },
            { "BiosCodeSet", new CIMHelper.QueryInfo("BiosCodeSet", "CodeSet", CIMHelper.ClassNames.Bios) },
            { "BiosCurrentLanguage", new CIMHelper.QueryInfo("BiosCurrentLanguage", "CurrentLanguage", CIMHelper.ClassNames.Bios) },
            { "BiosDescription", new CIMHelper.QueryInfo("BiosDescription", "Description", CIMHelper.ClassNames.Bios) },
            { "BiosEmbeddedControllerMajorVersion", new CIMHelper.QueryInfo("BiosEmbeddedControllerMajorVersion", "EmbeddedControllerMajorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosEmbeddedControllerMinorVersion", new CIMHelper.QueryInfo("BiosEmbeddedControllerMinorVersion", "EmbeddedControllerMinorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosIdentificationCode", new CIMHelper.QueryInfo("BiosIdentificationCode", "IdentificationCode", CIMHelper.ClassNames.Bios) },
            { "BiosInstallableLanguages", new CIMHelper.QueryInfo("BiosInstallableLanguages", "InstallableLanguages", CIMHelper.ClassNames.Bios) },
            { "BiosInstallDate", new CIMHelper.QueryInfo("BiosInstallDate", "InstallDate", CIMHelper.ClassNames.Bios) },
            { "BiosLanguageEdition", new CIMHelper.QueryInfo("BiosLanguageEdition", "LanguageEdition", CIMHelper.ClassNames.Bios) },
            { "BiosListOfLanguages", new CIMHelper.QueryInfo("BiosListOfLanguages", "ListOfLanguages", CIMHelper.ClassNames.Bios) },
            { "BiosManufacturer", new CIMHelper.QueryInfo("BiosManufacturer", "Manufacturer", CIMHelper.ClassNames.Bios) },
            { "BiosName", new CIMHelper.QueryInfo("BiosName", "Name", CIMHelper.ClassNames.Bios) },
            { "BiosOtherTargetOS", new CIMHelper.QueryInfo("BiosOtherTargetOS", "OtherTargetOS", CIMHelper.ClassNames.Bios) },
            { "BiosPrimaryBIOS", new CIMHelper.QueryInfo("BiosPrimaryBIOS", "PrimaryBIOS", CIMHelper.ClassNames.Bios) },
            { "BiosReleaseDate", new CIMHelper.QueryInfo("BiosReleaseDate", "ReleaseDate", CIMHelper.ClassNames.Bios) },
            { "BiosSerialNumber", new CIMHelper.QueryInfo("BiosSerialNumber", "SerialNumber", CIMHelper.ClassNames.Bios) },
            { "BiosSMBIOSBIOSVersion", new CIMHelper.QueryInfo("BiosSMBIOSBIOSVersion", "SMBIOSBIOSVersion", CIMHelper.ClassNames.Bios) },
            { "BiosSMBIOSMajorVersion", new CIMHelper.QueryInfo("BiosSMBIOSMajorVersion", "SMBIOSMajorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosSMBIOSMinorVersion", new CIMHelper.QueryInfo("BiosSMBIOSMinorVersion", "SMBIOSMinorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosSMBIOSPresent", new CIMHelper.QueryInfo("BiosSMBIOSPresent", "SMBIOSPresent", CIMHelper.ClassNames.Bios) },
            { "BiosSoftwareElementState", new CIMHelper.QueryInfo("BiosSoftwareElementState", "SoftwareElementState", CIMHelper.ClassNames.Bios) },
            { "BiosStatus", new CIMHelper.QueryInfo("BiosStatus", "Status", CIMHelper.ClassNames.Bios) },
            { "BiosSystemBiosMajorVersion", new CIMHelper.QueryInfo("BiosSystemBiosMajorVersion", "SystemBiosMajorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosSystemBiosMinorVersion", new CIMHelper.QueryInfo("BiosSystemBiosMinorVersion", "SystemBiosMinorVersion", CIMHelper.ClassNames.Bios) },
            { "BiosTargetOperatingSystem", new CIMHelper.QueryInfo("BiosTargetOperatingSystem", "TargetOperatingSystem", CIMHelper.ClassNames.Bios) },
            { "BiosVersion", new CIMHelper.QueryInfo("BiosVersion", "Version", CIMHelper.ClassNames.Bios) },
            // Operating system properties
            { "OsName", new CIMHelper.QueryInfo("OsName", "Name", CIMHelper.ClassNames.OperatingSystem) },
            { "OsBootDevice", new CIMHelper.QueryInfo("OsBootDevice", "BootDevice", CIMHelper.ClassNames.OperatingSystem) },
            { "OsBuildNumber", new CIMHelper.QueryInfo("OsBuildNumber", "BuildNumber", CIMHelper.ClassNames.OperatingSystem) },
            { "OsBuildType", new CIMHelper.QueryInfo("OsBuildType", "BuildType", CIMHelper.ClassNames.OperatingSystem) },
            { "OsCodeSet", new CIMHelper.QueryInfo("OsCodeSet", "CodeSet", CIMHelper.ClassNames.OperatingSystem) },
            { "OsCountryCode", new CIMHelper.QueryInfo("OsCountryCode", "CountryCode", CIMHelper.ClassNames.OperatingSystem) },
            { "OsCSDVersion", new CIMHelper.QueryInfo("OsCSDVersion", "CSDVersion", CIMHelper.ClassNames.OperatingSystem) },
            { "OsCurrentTimeZone", new CIMHelper.QueryInfo("OsCurrentTimeZone", "CurrentTimeZone", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDataExecutionPreventionAvailable", new CIMHelper.QueryInfo("OsDataExecutionPreventionAvailable", "DataExecutionPrevention_Available", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDataExecutionPrevention32BitApplications", new CIMHelper.QueryInfo("OsDataExecutionPrevention32BitApplications", "DataExecutionPrevention_32BitApplications", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDataExecutionPreventionDrivers", new CIMHelper.QueryInfo("OsDataExecutionPreventionDrivers", "DataExecutionPrevention_Drivers", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDataExecutionPreventionSupportPolicy", new CIMHelper.QueryInfo("OsDataExecutionPreventionSupportPolicy", "DataExecutionPrevention_SupportPolicy", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDebug", new CIMHelper.QueryInfo("OsDebug", "Debug", CIMHelper.ClassNames.OperatingSystem) },
            { "OsDistributed", new CIMHelper.QueryInfo("OsDistributed", "Distributed", CIMHelper.ClassNames.OperatingSystem) },
            { "OsEncryptionLevel", new CIMHelper.QueryInfo("OsEncryptionLevel", "EncryptionLevel", CIMHelper.ClassNames.OperatingSystem) },
            { "OsForegroundApplicationBoost", new CIMHelper.QueryInfo("OsForegroundApplicationBoost", "ForegroundApplicationBoost", CIMHelper.ClassNames.OperatingSystem) }
        };

        internal struct QueryInfo {
            public QueryInfo(string psObjectPropertyName, string property, string wmiClass)
            {
                this.psObjectPropertyName = psObjectPropertyName;
                this.wmiPropertyName = property;
                this.wmiClass = wmiClass;
            }

            public string psObjectPropertyName;
            public string wmiPropertyName;
            public string wmiClass;

            public int HashCode() {
                return (wmiPropertyName + wmiClass).GetHashCode();
            }
        }

        /// <summary>
        /// Create a WQL query string to retrieve requested properties from
        /// the specified WMI class.
        /// </summary>
        /// <param name="from">A string containing the WMI class name.</param>
        /// <param name="requestedProperties">A set of properties to query.</param>
        /// <returns>
        /// A string containing the WQL query string
        /// </returns>
        internal static string WqlQueryProperties(string from, List<string> requestedProperties)
        {
            var wmiPropertiesToQuery = requestedProperties
            .Where(property => CIMHelper.QueryableProperties.ContainsKey(property) && CIMHelper.QueryableProperties[property].wmiClass == from)
            .Select(property => CIMHelper.QueryableProperties[property].wmiPropertyName)
            .ToList();
            return wmiPropertiesToQuery.Count != 0 ? "SELECT " + string.Join(",", wmiPropertiesToQuery) + " from " + from : null;
        }

        /// <summary>
        /// Retrieve a new object of type T, whose properties and fields are
        /// populated from an instance of the named WMI class. If the CIM
        /// query results in multiple instances, only the first instance is
        /// returned.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be created. Must be a default-constructable
        /// reference type.
        /// </typeparam>
        /// <param name="session">
        /// The CIM session to be queried.
        /// </param>
        /// <param name="nameSpace">
        /// A string containing the namespace to run the query against
        /// </param>
        /// <param name="wmiClassName">
        /// A string containing the name of the WMI class from which to populate
        /// the resultant object.
        /// </param>
        /// <param name="requestedProperties">
        /// A set containing the properties to query
        /// </param>
        /// <returns>
        /// A new object of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T GetFirst<T>(CimSession session, string nameSpace, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            if (string.IsNullOrEmpty(wmiClassName))
                throw new ArgumentException("String argument may not be null or empty", "wmiClassName");

            try
            {
                var type = typeof(T);
                var binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                T rv = new T();

                using (var instance = session.QueryFirstInstance(nameSpace, CIMHelper.WqlQueryProperties(wmiClassName, requestedProperties)))
                {
                    if (instance != null) {
                        SetObjectDataMembers(rv, binding, instance);
                    }
                }

                return rv;
            }
            catch (Exception /*ex*/)
            {
                // on any error fall through to the null return below
            }

            return null;
        }

        /// <summary>
        /// Retrieve an array of new objects of type T, whose properties and fields are
        /// populated from an instance of the specified WMI class on the specified CIM
        /// session.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be created. Must be a default-constructable
        /// reference type.
        /// </typeparam>
        /// <param name="session">
        /// The CIM session to be queried.
        /// </param>
        /// <param name="nameSpace">
        /// A string containing the namespace to run the query against
        /// </param>
        /// <param name="wmiClassName">
        /// A string containing the name of the WMI class from which to populate
        /// the resultant array elements.
        /// </param>
        /// <param name="requestedProperties">
        /// A set containing the properties to query
        /// </param>
        /// <returns>
        /// An array of new objects of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T[] GetAll<T>(CimSession session, string nameSpace, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            if (string.IsNullOrEmpty(wmiClassName))
                throw new ArgumentException("String argument may not be null or empty", "wmiClassName");

            var rv = new List<T>();

            try
            {
                var instances = session.QueryInstances(nameSpace, CIMHelper.WqlQueryProperties(wmiClassName, requestedProperties));

                if (instances != null)
                {
                    var type = typeof(T);
                    var binding = BindingFlags.Public | BindingFlags.Instance;

                    foreach (var instance in instances)
                    {
                        T objT = new T();

                        using (instance)
                        {
                            SetObjectDataMembers(objT, binding, instance);
                        }

                        rv.Add(objT);
                    }
                }
            }
            catch (Exception /*ex*/)
            {
                // on any error we'll just fall through to the return below
            }

            return rv.ToArray();
        }

        /// <summary>
        /// Retrieve an array of new objects of type T, whose properties and fields are
        /// populated from an instance of the specified WMI class on the specified CIM
        /// session.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be created. Must be a default-constructable
        /// reference type.
        /// </typeparam>
        /// <param name="session">
        /// The CIM session to be queried.
        /// </param>
        /// <param name="wmiClassName">
        /// A string containing the name of the WMI class from which to populate
        /// the resultant array elements.
        /// </param>
        /// <param name="requestedProperties">
        /// A set containing the properties to query
        /// </param>
        /// <returns>
        /// An array of new objects of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T[] GetAll<T>(CimSession session, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            return GetAll<T>(session, DefaultNamespace, wmiClassName, requestedProperties);
        }

        internal static void SetObjectDataMember(object obj, BindingFlags binding, CimProperty cimProperty)
        {
            var type = obj.GetType();

            var pi = type.GetProperty(cimProperty.Name, binding);

            if (pi != null && pi.CanWrite)
            {
                pi.SetValue(obj, cimProperty.Value, null);
            }
            else
            {
                var fi = type.GetField(cimProperty.Name, binding);

                if (fi != null && !fi.IsInitOnly)
                {
                    fi.SetValue(obj, cimProperty.Value);
                }
            }
        }

        internal static void SetObjectDataMembers(object obj, BindingFlags binding, CimInstance instance)
        {
            foreach (var wmiProp in instance.CimInstanceProperties)
                SetObjectDataMember(obj, binding, wmiProp);
        }

        /// <summary>
        /// Escape any backslash (\) characters in a path with an additional
        /// backslash, allowing the path to be used within a WMI query.
        /// </summary>
        /// <param name="path">
        /// A string that may contain backslash characters.
        /// </param>
        /// <returns>
        /// A new string in which any backslash characters have been "escaped"
        /// by prefacing then with an additional backslash
        /// </returns>
        internal static string EscapePath(string path)
        {
            return string.Join(@"\\", path.Split('\\'));
        }
    }
}

namespace Extensions
{
    using Microsoft.PowerShell.Commands;

    internal static class CIMExtensions
    {
        /// <summary>
        /// An "overload" of the
        /// <see cref="Microsoft.Management.Infrastructure.CimSession"/>.QueryInstances
        /// method that takes only the namespace and query string as a parameters.
        /// </summary>
        /// <param name="session">The CimSession to be queried.</param>
        /// <param name="nameSpace">A string containing the namespace to run the query against.</param>
        /// <param name="query">A string containing the query to be run.</param>
        /// <returns>
        /// An IEnumerable interface that can be used to enumerate the instances
        /// </returns>
        internal static IEnumerable<CimInstance> QueryInstances(this CimSession session, string nameSpace, string query)
        {
            return session.QueryInstances(nameSpace, CIMHelper.DefaultQueryDialect, query);
        }

        /// <summary>
        /// Execute a CIM query and return only the first instance in the result.
        /// </summary>
        /// <param name="session">The CimSession to be queried.</param>
        /// <param name="nameSpace">A string containing the namespace to run the query against.</param>
        /// <param name="query">A string containing the query to be run.</param>
        /// <returns>
        /// A <see cref="Microsoft.Management.Infrastructure.CimInstance"/> object
        /// representing the first instance in a query result if successful, null
        /// otherwise.
        /// </returns>
        internal static CimInstance QueryFirstInstance(this CimSession session, string nameSpace, string query)
        {
            if (query == null)
                return null;

            try
            {
                var instances = session.QueryInstances(nameSpace, query);
                var enumerator = instances.GetEnumerator();

                if (enumerator.MoveNext())
                    return enumerator.Current;
            }
            catch (Exception /*ex*/)
            {
                // on any error, fall through to the null return below
            }

            return null;
        }

        /// <summary>
        /// Execute a CIM query and return only the first instance in the result.
        /// </summary>
        /// <param name="session">The CimSession to be queried.</param>
        /// <param name="query">A string containing the query to be run.</param>
        /// <returns>
        /// A <see cref="Microsoft.Management.Infrastructure.CimInstance"/> object
        /// representing the first instance in a query result if successful, null
        /// otherwise.
        /// </returns>
        internal static CimInstance QueryFirstInstance(this CimSession session, string query)
        {
            return session.QueryFirstInstance(CIMHelper.DefaultNamespace, query);
        }

        internal static T GetFirst<T>(this CimSession session, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            return session.GetFirst<T>(CIMHelper.DefaultNamespace, wmiClassName, requestedProperties);
        }

        internal static T GetFirst<T>(this CimSession session, string wmiNamespace, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            return CIMHelper.GetFirst<T>(session, wmiNamespace, wmiClassName, requestedProperties);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiClassName, requestedProperties);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiNamespace, string wmiClassName, List<string> requestedProperties) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiNamespace, wmiClassName, requestedProperties);
        }
    }
}
