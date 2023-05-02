// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

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

        /// <summary>
        /// Create a WQL query string to retrieve all properties from
        /// the specified WMI class.
        /// </summary>
        /// <param name="from">A string containing the WMI class name.</param>
        /// <returns>
        /// A string containing the WQL query string
        /// </returns>
        internal static string WqlQueryAll(string from)
        {
            return "SELECT * from " + from;
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
        /// <returns>
        /// A new object of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T GetFirst<T>(CimSession session, string nameSpace, string wmiClassName) where T : class, new()
        {
            ArgumentException.ThrowIfNullOrEmpty(wmiClassName);

            try
            {
                var type = typeof(T);
                const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                T rv = new();

                using (var instance = session.QueryFirstInstance(nameSpace, CIMHelper.WqlQueryAll(wmiClassName)))
                {
                    SetObjectDataMembers(rv, binding, instance);
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
        /// <returns>
        /// An array of new objects of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T[] GetAll<T>(CimSession session, string nameSpace, string wmiClassName) where T : class, new()
        {
            ArgumentException.ThrowIfNullOrEmpty(wmiClassName);
            
            var rv = new List<T>();

            try
            {
                var instances = session.QueryInstances(nameSpace, CIMHelper.WqlQueryAll(wmiClassName));

                if (instances != null)
                {
                    var type = typeof(T);
                    const BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;

                    foreach (var instance in instances)
                    {
                        T objT = new();

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
        /// <returns>
        /// An array of new objects of type T if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method matches property and field names of type T with identically
        /// named properties in the WMI class instance. The WMI property is converted
        /// to the type of T's property or field.
        /// </remarks>
        internal static T[] GetAll<T>(CimSession session, string wmiClassName) where T : class, new()
        {
            return GetAll<T>(session, DefaultNamespace, wmiClassName);
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

        internal static T GetFirst<T>(this CimSession session, string wmiClassName) where T : class, new()
        {
            return session.GetFirst<T>(CIMHelper.DefaultNamespace, wmiClassName);
        }

        internal static T GetFirst<T>(this CimSession session, string wmiNamespace, string wmiClassName) where T : class, new()
        {
            return CIMHelper.GetFirst<T>(session, wmiNamespace, wmiClassName);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiClassName) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiClassName);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiNamespace, string wmiClassName) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiNamespace, wmiClassName);
        }
    }
}
