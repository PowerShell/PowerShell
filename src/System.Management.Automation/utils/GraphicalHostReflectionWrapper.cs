// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Management.Automation;
    using System.Reflection;

    /// <summary>
    /// Helper to access Microsoft.PowerShell.GraphicalHost.dll (which references on WPF) using reflection, since
    /// we do not want System.Management.Automation.dll or Microsoft.PowerShell.Commands.Utility.dll to reference WPF.
    /// Microsoft.PowerShell.GraphicalHost.dll contains:
    ///    1) out-gridview window implementation (the actual cmdlet is in Microsoft.PowerShell.Commands.Utility.dll)
    ///    2) show-command window implementation (the actual cmdlet is in Microsoft.PowerShell.Commands.Utility.dll)
    ///    3) the help window used in the System.Management.Automation.dll's get-help cmdlet when -ShowWindow is specified.
    /// </summary>
    internal sealed class GraphicalHostReflectionWrapper
    {
        /// <summary>
        /// Initialized in GetGraphicalHostReflectionWrapper with the Microsoft.PowerShell.GraphicalHost.dll assembly.
        /// </summary>
        private Assembly _graphicalHostAssembly;

        /// <summary>
        /// A type in Microsoft.PowerShell.GraphicalHost.dll we want to invoke members on.
        /// </summary>
        private Type _graphicalHostHelperType;

        /// <summary>
        /// An object in Microsoft.PowerShell.GraphicalHost.dll of type graphicalHostHelperType.
        /// </summary>
        private object _graphicalHostHelperObject;

        /// <summary>
        /// Prevents a default instance of the GraphicalHostReflectionWrapper class from being created.
        /// </summary>
        private GraphicalHostReflectionWrapper()
        {
        }

        /// <summary>
        /// Retrieves a wrapper used to invoke members of the type with name <paramref name="graphicalHostHelperTypeName"/>
        /// in Microsoft.PowerShell.GraphicalHost.dll.
        /// </summary>
        /// <param name="parentCmdlet">The cmdlet requesting the wrapper (used to throw terminating errors).</param>
        /// <param name="graphicalHostHelperTypeName">The type name we want to invoke members from.</param>
        /// <returns>
        /// wrapper used to invoke members of the type with name <paramref name="graphicalHostHelperTypeName"/>
        /// in Microsoft.PowerShell.GraphicalHost.dll
        /// </returns>
        /// <exception cref="RuntimeException">When it was not possible to load Microsoft.PowerShell.GraphicalHost.dlly.</exception>
        internal static GraphicalHostReflectionWrapper GetGraphicalHostReflectionWrapper(PSCmdlet parentCmdlet, string graphicalHostHelperTypeName)
        {
            return GetGraphicalHostReflectionWrapper(parentCmdlet, graphicalHostHelperTypeName, parentCmdlet.CommandInfo.Name);
        }

        /// <summary>
        /// Retrieves a wrapper used to invoke members of the type with name <paramref name="graphicalHostHelperTypeName"/>
        /// in Microsoft.PowerShell.GraphicalHost.dll.
        /// </summary>
        /// <param name="parentCmdlet">The cmdlet requesting the wrapper (used to throw terminating errors).</param>
        /// <param name="graphicalHostHelperTypeName">The type name we want to invoke members from.</param>
        /// <param name="featureName">Used for error messages.</param>
        /// <returns>
        /// wrapper used to invoke members of the type with name <paramref name="graphicalHostHelperTypeName"/>
        /// in Microsoft.PowerShell.GraphicalHost.dll
        /// </returns>
        /// <exception cref="RuntimeException">When it was not possible to load Microsoft.PowerShell.GraphicalHost.dlly.</exception>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Assembly.Load has been found to throw unadvertised exceptions")]
        internal static GraphicalHostReflectionWrapper GetGraphicalHostReflectionWrapper(PSCmdlet parentCmdlet, string graphicalHostHelperTypeName, string featureName)
        {
            GraphicalHostReflectionWrapper returnValue = new();

            if (IsInputFromRemoting(parentCmdlet))
            {
                ErrorRecord error = new ErrorRecord(
                    new NotSupportedException(StringUtil.Format(HelpErrors.RemotingNotSupportedForFeature, featureName)),
                    "RemotingNotSupported",
                    ErrorCategory.InvalidOperation,
                    parentCmdlet);

                parentCmdlet.ThrowTerminatingError(error);
            }

            // Prepare the full assembly name.
            AssemblyName smaAssemblyName = typeof(PSObject).Assembly.GetName();
            AssemblyName graphicalHostAssemblyName = new();
            graphicalHostAssemblyName.Name = "Microsoft.PowerShell.GraphicalHost";
            graphicalHostAssemblyName.Version = smaAssemblyName.Version;
            graphicalHostAssemblyName.CultureInfo = new CultureInfo(string.Empty); // Neutral culture
            graphicalHostAssemblyName.SetPublicKeyToken(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });

            try
            {
                returnValue._graphicalHostAssembly = Assembly.Load(graphicalHostAssemblyName);
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                // This exception is thrown if the Microsoft.PowerShell.GraphicalHost.dll could not be found (was not installed).
                string errorMessage = StringUtil.Format(
                        HelpErrors.GraphicalHostAssemblyIsNotFound,
                        featureName,
                        fileNotFoundEx.Message);

                parentCmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        new NotSupportedException(errorMessage, fileNotFoundEx),
                        "ErrorLoadingAssembly",
                        ErrorCategory.ObjectNotFound,
                        graphicalHostAssemblyName));
            }
            catch (Exception e)
            {
                parentCmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        e,
                        "ErrorLoadingAssembly",
                        ErrorCategory.ObjectNotFound,
                        graphicalHostAssemblyName));
            }

            returnValue._graphicalHostHelperType = returnValue._graphicalHostAssembly.GetType(graphicalHostHelperTypeName);

            Diagnostics.Assert(returnValue._graphicalHostHelperType != null, "the type should exist in Microsoft.PowerShell.GraphicalHost");
            ConstructorInfo constructor = returnValue._graphicalHostHelperType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Array.Empty<Type>(),
                null);

            if (constructor != null)
            {
                returnValue._graphicalHostHelperObject = constructor.Invoke(Array.Empty<object>());
                Diagnostics.Assert(returnValue._graphicalHostHelperObject != null, "the constructor does not throw anything");
            }

            return returnValue;
        }

        /// <summary>
        /// Used to escape characters that are not friendly to WPF binding.
        /// </summary>
        /// <param name="propertyName">Property name to be used in binding.</param>
        /// <returns>String with escaped characters.</returns>
        internal static string EscapeBinding(string propertyName)
        {
            return propertyName.Replace("/", " ").Replace(".", " ");
        }

        /// <summary>
        /// Calls an instance method with name <paramref name="methodName"/> passing the <paramref name="arguments"/>
        /// </summary>
        /// <param name="methodName">Name of the method to call.</param>
        /// <param name="arguments">Arguments to call the method with.</param>
        /// <returns>The method return value.</returns>
        internal object CallMethod(string methodName, params object[] arguments)
        {
            Diagnostics.Assert(_graphicalHostHelperObject != null, "there should be a constructor in order to call an instance method");
            MethodInfo method = _graphicalHostHelperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Diagnostics.Assert(method != null, "method " + methodName + " exists in graphicalHostHelperType is verified by caller");
            return method.Invoke(_graphicalHostHelperObject, arguments);
        }

        /// <summary>
        /// Calls a static method with name <paramref name="methodName"/> passing the <paramref name="arguments"/>
        /// </summary>
        /// <param name="methodName">Name of the method to call.</param>
        /// <param name="arguments">Arguments to call the method with.</param>
        /// <returns>The method return value.</returns>
        internal object CallStaticMethod(string methodName, params object[] arguments)
        {
            MethodInfo method = _graphicalHostHelperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Diagnostics.Assert(method != null, "method " + methodName + " exists in graphicalHostHelperType is verified by caller");
            return method.Invoke(null, arguments);
        }

        /// <summary>
        /// Gets the value of an instance property with name <paramref name="propertyName"/>
        /// </summary>
        /// <param name="propertyName">Name of the instance property to get the value from.</param>
        /// <returns>The value of an instance property with name <paramref name="propertyName"/></returns>
        internal object GetPropertyValue(string propertyName)
        {
            Diagnostics.Assert(_graphicalHostHelperObject != null, "there should be a constructor in order to get an instance property value");
            PropertyInfo property = _graphicalHostHelperType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            Diagnostics.Assert(property != null, "property " + propertyName + " exists in graphicalHostHelperType is verified by caller");
            return property.GetValue(_graphicalHostHelperObject, Array.Empty<object>());
        }

        /// <summary>
        /// Gets the value of a static property with name <paramref name="propertyName"/>
        /// </summary>
        /// <param name="propertyName">Name of the static property to get the value from.</param>
        /// <returns>The value of a static property with name <paramref name="propertyName"/></returns>
        internal object GetStaticPropertyValue(string propertyName)
        {
            PropertyInfo property = _graphicalHostHelperType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static);
            Diagnostics.Assert(property != null, "property " + propertyName + " exists in graphicalHostHelperType is verified by caller");
            return property.GetValue(null, Array.Empty<object>());
        }

        /// <summary>
        /// Returns true if the <paramref name="parentCmdlet"/> is being run remotely.
        /// </summary>
        /// <param name="parentCmdlet">Cmdlet we want to see if is running remotely.</param>
        /// <returns>True if the <paramref name="parentCmdlet"/> is being run remotely.</returns>
        private static bool IsInputFromRemoting(PSCmdlet parentCmdlet)
        {
            Diagnostics.Assert(parentCmdlet.SessionState != null, "SessionState should always be available.");

            PSVariable senderInfo = parentCmdlet.SessionState.PSVariable.Get("PSSenderInfo");
            return senderInfo != null;
        }
    }
}
