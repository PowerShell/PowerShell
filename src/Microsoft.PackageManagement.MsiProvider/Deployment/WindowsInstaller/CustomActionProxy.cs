//---------------------------------------------------------------------
// <copyright file="CustomActionProxy.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// Managed-code portion of the custom action proxy.
    /// </summary>
    internal static class CustomActionProxy
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int InvokeCustomAction32(int sessionHandle, string entryPoint,
            int remotingDelegatePtr)
        {
            return CustomActionProxy.InvokeCustomAction(sessionHandle, entryPoint, new IntPtr(remotingDelegatePtr));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int InvokeCustomAction64(int sessionHandle, string entryPoint,
            long remotingDelegatePtr)
        {
            return CustomActionProxy.InvokeCustomAction(sessionHandle, entryPoint, new IntPtr(remotingDelegatePtr));
        }

        /// <summary>
        /// Invokes a managed custom action method.
        /// </summary>
        /// <param name="sessionHandle">Integer handle to the installer session.</param>
        /// <param name="entryPoint">Name of the custom action entrypoint. This must
        /// either map to an entrypoint definition in the <c>customActions</c>
        /// config section, or be an explicit entrypoint of the form:
        /// &quot;AssemblyName!Namespace.Class.Method&quot;</param>
        /// <param name="remotingDelegatePtr">Pointer to a delegate used to
        /// make remote API calls, if this custom action is running out-of-proc.</param>
        /// <returns>The value returned by the custom action method,
        /// or ERROR_INSTALL_FAILURE if the custom action could not be invoked.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int InvokeCustomAction(int sessionHandle, string entryPoint,
            IntPtr remotingDelegatePtr)
        {
            Session session = null;
            string assemblyName, className, methodName;
            MethodInfo method;

            try
            {
                MsiRemoteInvoke remotingDelegate = (MsiRemoteInvoke)
                    Marshal.GetDelegateForFunctionPointer(
                        remotingDelegatePtr, typeof(MsiRemoteInvoke));
                RemotableNativeMethods.RemotingDelegate = remotingDelegate;

                sessionHandle = RemotableNativeMethods.MakeRemoteHandle(sessionHandle);
                session = new Session((IntPtr) sessionHandle, false);
                if (string.IsNullOrWhiteSpace(entryPoint))
                {
                    throw new ArgumentNullException("entryPoint");
                }

                if (!CustomActionProxy.FindEntryPoint(
                    session,
                    entryPoint,
                    out assemblyName,
                    out className,
                    out methodName))
                {
                    return (int) ActionResult.Failure;
                }
                session.Log("Calling custom action {0}!{1}.{2}", assemblyName, className, methodName);

                method = CustomActionProxy.GetCustomActionMethod(
                    session,
                    assemblyName,
                    className,
                    methodName);
                if (method == null)
                {
                    return (int) ActionResult.Failure;
                }
            }
            catch (Exception ex)
            {
                if (session != null)
                {
                    try
                    {
                        session.Log("Exception while loading custom action:");
                        session.Log(ex.ToString());
                    }
                    catch (Exception) { }
                }
                return (int) ActionResult.Failure;
            }

            try
            {
                // Set the current directory to the location of the extracted files.
                Environment.CurrentDirectory =
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                object[] args = new object[] { session };
                if (DebugBreakEnabled(new string[] { entryPoint, methodName }))
                {
                    string message = String.Format(CultureInfo.InvariantCulture,
                        "To debug your custom action, attach to process ID {0} (0x{0:x}) and click OK; otherwise, click Cancel to fail the custom action.",
                        System.Diagnostics.Process.GetCurrentProcess().Id
                        );

                    MessageResult button = NativeMethods.MessageBox(
                        IntPtr.Zero,
                        message,
                        "Custom Action Breakpoint",
                        (int)MessageButtons.OKCancel | (int)MessageIcon.Asterisk | (int)(MessageBoxStyles.TopMost | MessageBoxStyles.ServiceNotification)
                        );

                    if (MessageResult.Cancel == button)
                    {
                        return (int)ActionResult.UserExit;
                    }
                }

                ActionResult result = (ActionResult) method.Invoke(null, args);
                session.Close();
                return (int) result;
            }
            catch (InstallCanceledException)
            {
                return (int) ActionResult.UserExit;
            }
            catch (Exception ex)
            {
                session.Log("Exception thrown by custom action:");
                session.Log(ex.ToString());
                return (int) ActionResult.Failure;
            }
        }

        /// <summary>
        /// Checks the "MMsiBreak" environment variable for any matching custom action names.
        /// </summary>
        /// <param name="names">List of names to search for in the environment
        /// variable string.</param>
        /// <returns>True if a match was found, else false.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool DebugBreakEnabled(string[] names)
        {
            string mmsibreak = Environment.GetEnvironmentVariable("MMsiBreak");
            if (mmsibreak != null)
            {
                foreach (string breakName in mmsibreak.Split(',', ';'))
                {
                    foreach (string name in names)
                    {
                        if (breakName == name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Locates and parses an entrypoint mapping in CustomAction.config.
        /// </summary>
        /// <param name="session">Installer session handle, just used for logging.</param>
        /// <param name="entryPoint">Custom action entrypoint name: the key value
        /// in an item in the <c>customActions</c> section of the config file.</param>
        /// <param name="assemblyName">Returned display name of the assembly from
        /// the entrypoint mapping.</param>
        /// <param name="className">Returned class name of the entrypoint mapping.</param>
        /// <param name="methodName">Returned method name of the entrypoint mapping.</param>
        /// <returns>True if the entrypoint was found, false if not or if some error
        /// occurred.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool FindEntryPoint(
            Session session,
            string entryPoint,
            out string assemblyName,
            out string className,
            out string methodName)
        {
            assemblyName = null;
            className = null;
            methodName = null;

            string fullEntryPoint;
            if (entryPoint.IndexOf('!') > 0)
            {
                fullEntryPoint = entryPoint;
            }
            else
            {
                IDictionary config;
                try
                {
                    config = (IDictionary) ConfigurationManager.GetSection("customActions");
                }
                catch (ConfigurationException cex)
                {
                    session.Log("Error: missing or invalid customActions config section.");
                    session.Log(cex.ToString());
                    return false;
                }
                fullEntryPoint = (string) config[entryPoint];
                if (fullEntryPoint == null)
                {
                    session.Log(
                        "Error: custom action entry point '{0}' not found " +
                        "in customActions config section.",
                        entryPoint);
                    return false;
                }
            }

            int assemblySplit = fullEntryPoint.IndexOf('!');
            int methodSplit = fullEntryPoint.LastIndexOf('.');
            if (assemblySplit < 0 || methodSplit < 0 || methodSplit < assemblySplit)
            {
                session.Log("Error: invalid custom action entry point:" + entryPoint);
                return false;
            }

            assemblyName = fullEntryPoint.Substring(0, assemblySplit);
            className = fullEntryPoint.Substring(assemblySplit + 1, methodSplit - assemblySplit - 1);
            methodName = fullEntryPoint.Substring(methodSplit + 1);
            return true;
        }

        /// <summary>
        /// Uses reflection to load the assembly and class and find the method.
        /// </summary>
        /// <param name="session">Installer session handle, just used for logging.</param>
        /// <param name="assemblyName">Display name of the assembly containing the
        /// custom action method.</param>
        /// <param name="className">Fully-qualified name of the class containing the
        /// custom action method.</param>
        /// <param name="methodName">Name of the custom action method.</param>
        /// <returns>The method, or null if not found.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static MethodInfo GetCustomActionMethod(
            Session session,
            string assemblyName,
            string className,
            string methodName)
        {
            Assembly customActionAssembly;
            Type customActionClass = null;
            Exception caughtEx = null;
            try
            {
                customActionAssembly = AppDomain.CurrentDomain.Load(assemblyName);
                customActionClass = customActionAssembly.GetType(className, true, true);
            }
            catch (IOException ex) { caughtEx = ex; }
            catch (BadImageFormatException ex) { caughtEx = ex; }
            catch (TypeLoadException ex) { caughtEx = ex; }
            catch (ReflectionTypeLoadException ex) { caughtEx = ex; }
            catch (SecurityException ex) { caughtEx = ex; }
            if (caughtEx != null)
            {
                session.Log("Error: could not load custom action class " + className + " from assembly: " + assemblyName);
                session.Log(caughtEx.ToString());
                return null;
            }

            MethodInfo[] methods = customActionClass.GetMethods(
                BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (method.Name == methodName &&
                    CustomActionProxy.MethodHasCustomActionSignature(method))
                {
                    return method;
                }
            }
            session.Log("Error: custom action method \"" + methodName +
                "\" is missing or has the wrong signature.");
            return null;
        }

        /// <summary>
        /// Checks if a method has the right return and parameter types
        /// for a custom action, and that it is marked by a CustomActionAttribute.
        /// </summary>
        /// <param name="method">Method to be checked.</param>
        /// <returns>True if the method is a valid custom action, else false.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool MethodHasCustomActionSignature(MethodInfo method)
        {
            if (method.ReturnType == typeof(ActionResult) &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(Session))
            {
                object[] methodAttribs = method.GetCustomAttributes(false);
                foreach (object attrib in methodAttribs)
                {
                    if (attrib is CustomActionAttribute)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
