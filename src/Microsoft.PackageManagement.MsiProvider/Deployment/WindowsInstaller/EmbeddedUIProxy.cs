//---------------------------------------------------------------------
// <copyright file="EmbeddedUIProxy.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Managed-code portion of the embedded UI proxy.
    /// </summary>
    internal static class EmbeddedUIProxy
    {
        private static IEmbeddedUI uiInstance;
        private static string uiClass;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool DebugBreakEnabled(string method)
        {
            return CustomActionProxy.DebugBreakEnabled(new string[] { method, EmbeddedUIProxy.uiClass + "." + method } );
        }

        /// <summary>
        /// Initializes managed embedded UI by loading the UI class and invoking its Initialize method.
        /// </summary>
        /// <param name="sessionHandle">Integer handle to the installer session.</param>
        /// <param name="uiClass">Name of the class that implements the embedded UI. This must
        /// be of the form: &quot;AssemblyName!Namespace.Class&quot;</param>
        /// <param name="internalUILevel">On entry, contains the current UI level for the installation. After this
        /// method returns, the installer resets the UI level to the returned value of this parameter.</param>
        /// <returns>0 if the embedded UI was successfully loaded and initialized,
        /// ERROR_INSTALL_USEREXIT if the user canceled the installation during initialization,
        /// or ERROR_INSTALL_FAILURE if the embedded UI could not be initialized.</returns>
        /// <remarks>
        /// Due to interop limitations, the successful resulting UILevel is actually returned
        /// as the high-word of the return value instead of via a ref parameter.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int Initialize(int sessionHandle, string uiClass, int internalUILevel)
        {
            Session session = null;

            try
            {
                session = new Session((IntPtr) sessionHandle, false);

                if (string.IsNullOrWhiteSpace(uiClass))
                {
                    throw new ArgumentNullException("uiClass");
                }

                EmbeddedUIProxy.uiInstance = EmbeddedUIProxy.InstantiateUI(session, uiClass);
            }
            catch (Exception ex)
            {
                if (session != null)
                {
                    try
                    {
                        session.Log("Exception while loading embedded UI:");
                        session.Log(ex.ToString());
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (EmbeddedUIProxy.uiInstance == null)
            {
                return (int) ActionResult.Failure;
            }

            try
            {
                string resourcePath = Path.GetDirectoryName(EmbeddedUIProxy.uiInstance.GetType().Assembly.Location);
                InstallUIOptions uiOptions = (InstallUIOptions) internalUILevel;
                if (EmbeddedUIProxy.DebugBreakEnabled("Initialize"))
                {
                    System.Diagnostics.Debugger.Launch();
                }

                if (EmbeddedUIProxy.uiInstance.Initialize(session, resourcePath, ref uiOptions))
                {
                    // The embedded UI initialized and the installation should continue
                    // with internal UI reset according to options.
                    return ((int) uiOptions) << 16;
                }
                else
                {
                    // The embedded UI did not initialize but the installation should still continue
                    // with internal UI reset according to options.
                    return (int) uiOptions;
                }
            }
            catch (InstallCanceledException)
            {
                // The installation was canceled by the user.
                return (int) ActionResult.UserExit;
            }
            catch (Exception ex)
            {
                // An unhandled exception causes the installation to fail immediately.
                session.Log("Exception thrown by embedded UI initialization:");
                session.Log(ex.ToString());
                return (int) ActionResult.Failure;
            }
        }

        /// <summary>
        /// Passes a progress message to the UI class.
        /// </summary>
        /// <param name="messageType">Installer message type and message box options.</param>
        /// <param name="recordHandle">Handle to a record containing message data.</param>
        /// <returns>Return value returned by the UI class.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static int ProcessMessage(int messageType, int recordHandle)
        {
            if (EmbeddedUIProxy.uiInstance != null)
            {
                try
                {
                    int msgType = messageType & 0x7F000000;
                    int buttons = messageType & 0x0000000F;
                    int icon = messageType & 0x000000F0;
                    int defButton = messageType & 0x00000F00;

                    Record msgRec = (recordHandle != 0 ? Record.FromHandle((IntPtr) recordHandle, false) : null);
                    using (msgRec)
                    {
                        if (EmbeddedUIProxy.DebugBreakEnabled("ProcessMessage"))
                        {
                            System.Diagnostics.Debugger.Launch();
                        }

                        return (int) EmbeddedUIProxy.uiInstance.ProcessMessage(
                            (InstallMessage) msgType,
                            msgRec,
                            (MessageButtons) buttons,
                            (MessageIcon) icon,
                            (MessageDefaultButton) defButton);
                    }
                }
                catch (Exception)
                {
                    // Ignore it... just hope future messages will not throw exceptions.
                }
            }

            return 0;
        }

        /// <summary>
        /// Passes a shutdown message to the UI class.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void Shutdown()
        {
            if (EmbeddedUIProxy.uiInstance != null)
            {
                try
                {
                    if (EmbeddedUIProxy.DebugBreakEnabled("Shutdown"))
                    {
                        System.Diagnostics.Debugger.Launch();
                    }

                    EmbeddedUIProxy.uiInstance.Shutdown();
                }
                catch (Exception)
                {
                    // Nothing to do at this point... the installation is done anyway.
                }

                EmbeddedUIProxy.uiInstance = null;
            }
        }

        /// <summary>
        /// Instantiates a UI class from a given assembly and class name.
        /// </summary>
        /// <param name="session">Installer session, for logging.</param>
        /// <param name="uiClass">Name of the class that implements the embedded UI. This must
        /// be of the form: &quot;AssemblyName!Namespace.Class&quot;</param>
        /// <returns>Interface on the UI class for handling UI messages.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static IEmbeddedUI InstantiateUI(Session session, string uiClass)
        {
            int assemblySplit = uiClass.IndexOf('!');
            if (assemblySplit < 0)
            {
                session.Log("Error: invalid embedded UI assembly and class:" + uiClass);
                return null;
            }

            string assemblyName = uiClass.Substring(0, assemblySplit);
            EmbeddedUIProxy.uiClass = uiClass.Substring(assemblySplit + 1);

            Assembly uiAssembly;
            try
            {
                uiAssembly = AppDomain.CurrentDomain.Load(assemblyName);

                // This calls out to CustomActionProxy.DebugBreakEnabled() directly instead
                // of calling EmbeddedUIProxy.DebugBreakEnabled() because we don't compose a
                // class.method name for this breakpoint.
                if (CustomActionProxy.DebugBreakEnabled(new string[] { "EmbeddedUI" }))
                {
                    System.Diagnostics.Debugger.Launch();
                }

                return (IEmbeddedUI) uiAssembly.CreateInstance(EmbeddedUIProxy.uiClass);
            }
            catch (Exception ex)
            {
                session.Log("Error: could not load embedded UI class " + EmbeddedUIProxy.uiClass + " from assembly: " + assemblyName);
                session.Log(ex.ToString());
                return null;
            }
        }
    }
}
