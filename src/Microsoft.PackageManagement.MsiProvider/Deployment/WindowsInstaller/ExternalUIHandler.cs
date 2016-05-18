//---------------------------------------------------------------------
// <copyright file="ExternalUIHandler.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Microsoft.Deployment.WindowsInstaller.ExternalUIHandler and related delegates,
// with related parts of Installer class.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Defines a callback function that the installer calls for progress notification and error messages.
    /// </summary>
    internal delegate MessageResult ExternalUIHandler(
        InstallMessage messageType,
        string message,
        MessageButtons buttons,
        MessageIcon icon,
        MessageDefaultButton defaultButton);

    /// <summary>
    /// [MSI 3.1] Defines a callback function that the installer calls for record-based progress notification and error messages.
    /// </summary>
    internal delegate MessageResult ExternalUIRecordHandler(
        InstallMessage messageType,
        Record messageRecord,
        MessageButtons buttons,
        MessageIcon icon,
        MessageDefaultButton defaultButton);

    internal delegate int NativeExternalUIHandler(IntPtr context, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);

    internal delegate int NativeExternalUIRecordHandler(IntPtr context, int messageType, int recordHandle);

    internal class ExternalUIProxy
    {
        private ExternalUIHandler handler;

        internal ExternalUIProxy(ExternalUIHandler handler)
        {
            this.handler = handler;
        }

        public ExternalUIHandler Handler
        {
            get { return this.handler; }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public int ProxyHandler(IntPtr contextPtr, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message)
        {
            try
            {
                int msgType   = messageType & 0x7F000000;
                int buttons   = messageType & 0x0000000F;
                int icon      = messageType & 0x000000F0;
                int defButton = messageType & 0x00000F00;

                return (int) this.handler(
                        (InstallMessage) msgType,
                        message,
                        (MessageButtons) buttons,
                        (MessageIcon) icon,
                        (MessageDefaultButton) defButton);
            }
            catch
            {
                return (int) MessageResult.Error;
            }
        }
    }

    internal class ExternalUIRecordProxy
    {
        private ExternalUIRecordHandler handler;

        internal ExternalUIRecordProxy(ExternalUIRecordHandler handler)
        {
            this.handler = handler;
        }

        public ExternalUIRecordHandler Handler
        {
            get { return this.handler; }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public int ProxyHandler(IntPtr contextPtr, int messageType, int recordHandle)
        {
            try
            {
                int msgType   = messageType & 0x7F000000;
                int buttons   = messageType & 0x0000000F;
                int icon      = messageType & 0x000000F0;
                int defButton = messageType & 0x00000F00;

                Record msgRec = (recordHandle != 0 ? Record.FromHandle((IntPtr) recordHandle, false) : null);
                using (msgRec)
                {
                    return (int) this.handler(
                        (InstallMessage) msgType,
                        msgRec,
                        (MessageButtons) buttons,
                        (MessageIcon) icon,
                        (MessageDefaultButton) defButton);
                }
            }
            catch
            {
                return (int) MessageResult.Error;
            }
        }
    }

    internal static partial class Installer
    {
        private static IList externalUIHandlers = ArrayList.Synchronized(new ArrayList());

        /// <summary>
        /// Enables an external user-interface handler. This external UI handler is called before the
        /// normal internal user-interface handler. The external UI handler has the option to suppress
        /// the internal UI by returning a non-zero value to indicate that it has handled the messages.
        /// </summary>
        /// <param name="uiHandler">A callback delegate that handles the UI messages</param>
        /// <param name="messageFilter">Specifies which messages to handle using the external message handler.
        /// If the external handler returns a non-zero result, then that message will not be sent to the UI,
        /// instead the message will be logged if logging has been enabled.</param>
        /// <returns>The previously set external handler, or null if there was no previously set handler</returns>
        /// <remarks><p>
        /// To restore the previous UI handler, a second call is made to SetExternalUI using the
        /// ExternalUIHandler returned by the first call to SetExternalUI and specifying
        /// <see cref="InstallLogModes.None"/> as the message filter.
        /// </p><p>
        /// The external user interface handler does not have full control over the external user
        /// interface unless <see cref="SetInternalUI(InstallUIOptions)"/> is called with the uiLevel parameter set to
        /// <see cref="InstallUIOptions.Silent"/>. If SetInternalUI is not called, the internal user
        /// interface level defaults to <see cref="InstallUIOptions.Basic"/>. As a result, any message not
        /// handled by the external user interface handler is handled by Windows Installer. The initial
        /// "Preparing to install..." dialog always appears even if the external user interface
        /// handler handles all messages.
        /// </p><p>
        /// SetExternalUI should only be called from a bootstrapping application. You cannot call
        /// it from a custom action
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetexternalui.asp">MsiSetExternalUI</a>
        /// </p></remarks>
        internal static ExternalUIHandler SetExternalUI(ExternalUIHandler uiHandler, InstallLogModes messageFilter)
        {
            NativeExternalUIHandler nativeHandler = null;
            if (uiHandler != null)
            {
                nativeHandler = new ExternalUIProxy(uiHandler).ProxyHandler;
                Installer.externalUIHandlers.Add(nativeHandler);
            }
            NativeExternalUIHandler oldNativeHandler = NativeMethods.MsiSetExternalUI(nativeHandler, (uint) messageFilter, IntPtr.Zero);
            if (oldNativeHandler != null && oldNativeHandler.Target is ExternalUIProxy)
            {
                Installer.externalUIHandlers.Remove(oldNativeHandler);
                return ((ExternalUIProxy) oldNativeHandler.Target).Handler;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// [MSI 3.1] Enables a record-based external user-interface handler. This external UI handler is called
        /// before the normal internal user-interface handler. The external UI handler has the option to suppress
        /// the internal UI by returning a non-zero value to indicate that it has handled the messages.
        /// </summary>
        /// <param name="uiHandler">A callback delegate that handles the UI messages</param>
        /// <param name="messageFilter">Specifies which messages to handle using the external message handler.
        /// If the external handler returns a non-zero result, then that message will not be sent to the UI,
        /// instead the message will be logged if logging has been enabled.</param>
        /// <returns>The previously set external handler, or null if there was no previously set handler</returns>
        /// <remarks><p>
        /// To restore the previous UI handler, a second call is made to SetExternalUI using the
        /// ExternalUIHandler returned by the first call to SetExternalUI and specifying
        /// <see cref="InstallLogModes.None"/> as the message filter.
        /// </p><p>
        /// The external user interface handler does not have full control over the external user
        /// interface unless <see cref="SetInternalUI(InstallUIOptions)"/> is called with the uiLevel parameter set to
        /// <see cref="InstallUIOptions.Silent"/>. If SetInternalUI is not called, the internal user
        /// interface level defaults to <see cref="InstallUIOptions.Basic"/>. As a result, any message not
        /// handled by the external user interface handler is handled by Windows Installer. The initial
        /// "Preparing to install..." dialog always appears even if the external user interface
        /// handler handles all messages.
        /// </p><p>
        /// SetExternalUI should only be called from a bootstrapping application. You cannot call
        /// it from a custom action
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetexternaluirecord.asp">MsiSetExternalUIRecord</a>
        /// </p></remarks>
        internal static ExternalUIRecordHandler SetExternalUI(ExternalUIRecordHandler uiHandler, InstallLogModes messageFilter)
        {
            NativeExternalUIRecordHandler nativeHandler = null;
            if (uiHandler != null)
            {
                nativeHandler = new ExternalUIRecordProxy(uiHandler).ProxyHandler;
                Installer.externalUIHandlers.Add(nativeHandler);
            }
            NativeExternalUIRecordHandler oldNativeHandler;
            uint ret = NativeMethods.MsiSetExternalUIRecord(nativeHandler, (uint) messageFilter, IntPtr.Zero, out oldNativeHandler);
            if (ret != 0)
            {
                Installer.externalUIHandlers.Remove(nativeHandler);
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            if (oldNativeHandler != null && oldNativeHandler.Target is ExternalUIRecordProxy)
            {
                Installer.externalUIHandlers.Remove(oldNativeHandler);
                return ((ExternalUIRecordProxy) oldNativeHandler.Target).Handler;
            }
            else
            {
                return null;
            }
        }
    }
}
