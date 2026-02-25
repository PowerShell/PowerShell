// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// ----------------------------------------------------------------------
//  Contents:  Entry points for managed PowerShell plugin worker used to
//  host powershell in a WSMan service.
// ----------------------------------------------------------------------

using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
using System.Globalization;

namespace System.Management.Automation.Remoting
{
    // TODO: Does this comment still apply?
    // The following complex delegate + native function pointer model is used
    // because of a problem with GCRoot. GCRoots cannot hold reference to the
    // AppDomain that created it. In the IIS hosting scenario, there may be
    // cases where multiple AppDomains exist in the same hosting process. In such
    // cases if GCRoot is used, CLR will pick up the first AppDomain in the list
    // to get the managed handle. Delegates are not just function pointers, they
    // also contain a reference to the AppDomain that created it. However the catch
    // is that delegates must be marshalled into their respective unmanaged function
    // pointers (otherwise we end up storing the delegate into a GCRoot).

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="extraInfo">PCWSTR.</param>
    /// <param name="startupInfo">WSMAN_SHELL_STARTUP_INFO*.</param>
    /// <param name="inboundShellInformation">WSMAN_DATA*.</param>
    internal delegate void WSManPluginShellDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        [MarshalAs(UnmanagedType.LPWStr)] string extraInfo,
        IntPtr startupInfo,
        IntPtr inboundShellInformation);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="shellContext">PVOID.</param>
    internal delegate void WSManPluginReleaseShellContextDelegate(
        IntPtr pluginContext,
        IntPtr shellContext);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandContext">PVOID optional.</param>
    /// <param name="inboundConnectInformation">WSMAN_DATA* optional.</param>
    internal delegate void WSManPluginConnectDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        IntPtr shellContext,
        IntPtr commandContext,
        IntPtr inboundConnectInformation);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandLine">PCWSTR.</param>
    /// <param name="arguments">WSMAN_COMMAND_ARG_SET*.</param>
    internal delegate void WSManPluginCommandDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        IntPtr shellContext,
        [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
        IntPtr arguments);

    /// <summary>
    /// Delegate that is passed to native layer for callback on operation shutdown notifications.
    /// </summary>
    /// <param name="shutdownContext">IntPtr.</param>
    internal delegate void WSManPluginOperationShutdownDelegate(
           IntPtr shutdownContext);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandContext">PVOID.</param>
    internal delegate void WSManPluginReleaseCommandContextDelegate(
        IntPtr pluginContext,
        IntPtr shellContext,
        IntPtr commandContext);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandContext">PVOID.</param>
    /// <param name="stream">PCWSTR.</param>
    /// <param name="inboundData">WSMAN_DATA*.</param>
    internal delegate void WSManPluginSendDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        IntPtr shellContext,
        IntPtr commandContext,
        [MarshalAs(UnmanagedType.LPWStr)] string stream,
        IntPtr inboundData);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandContext">PVOID optional.</param>
    /// <param name="streamSet">WSMAN_STREAM_ID_SET* optional.</param>
    internal delegate void WSManPluginReceiveDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        IntPtr shellContext,
        IntPtr commandContext,
        IntPtr streamSet);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
    /// <param name="flags">DWORD.</param>
    /// <param name="shellContext">PVOID.</param>
    /// <param name="commandContext">PVOID optional.</param>
    /// <param name="code">PCWSTR.</param>
    internal delegate void WSManPluginSignalDelegate(
        IntPtr pluginContext,
        IntPtr requestDetails,
        int flags,
        IntPtr shellContext,
        IntPtr commandContext,
        [MarshalAs(UnmanagedType.LPWStr)] string code);

    /// <summary>
    /// Callback that handles shell shutdown notification events.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="timedOut"></param>
    internal delegate void WaitOrTimerCallbackDelegate(
        IntPtr state,
        bool timedOut);

    /// <summary>
    /// </summary>
    /// <param name="pluginContext">PVOID.</param>
    internal delegate void WSManShutdownPluginDelegate(
        IntPtr pluginContext);

    /// <summary>
    /// </summary>
    internal sealed class WSManPluginEntryDelegates : IDisposable
    {
        #region Private Members

        // Holds the delegate pointers in a structure that has identical layout to the native structure.
        private readonly WSManPluginEntryDelegatesInternal _unmanagedStruct = new WSManPluginEntryDelegatesInternal();

        internal WSManPluginEntryDelegatesInternal UnmanagedStruct
        {
            get { return _unmanagedStruct; }
        }

        // Flag: Has Dispose already been called?
        private bool _disposed = false;

        /// <summary>
        /// GC handle which prevents garbage collector from collecting this delegate.
        /// </summary>
        private GCHandle _pluginShellGCHandle;
        private GCHandle _pluginReleaseShellContextGCHandle;
        private GCHandle _pluginCommandGCHandle;
        private GCHandle _pluginReleaseCommandContextGCHandle;
        private GCHandle _pluginSendGCHandle;
        private GCHandle _pluginReceiveGCHandle;
        private GCHandle _pluginSignalGCHandle;
        private GCHandle _pluginConnectGCHandle;
        private GCHandle _shutdownPluginGCHandle;
        private GCHandle _WSManPluginOperationShutdownGCHandle;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes the delegate struct for later use.
        /// </summary>
        internal WSManPluginEntryDelegates()
        {
            populateDelegates();
        }

        #endregion

        #region IDisposable Methods

        /// <summary>
        /// Internal implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Free any unmanaged objects here.
            this.CleanUpDelegates();

            _disposed = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WSManPluginEntryDelegates"/> class.
        /// </summary>
        ~WSManPluginEntryDelegates()
        {
            Dispose(false);
        }

        #endregion IDisposable Methods

        /// <summary>
        /// Creates delegates and populates the managed version of the
        /// structure that will be passed to unmanaged callers.
        /// </summary>
        private void populateDelegates()
        {
            // if a delegate is re-located by a garbage collection, it will not affect
            // the underlaying managed callback, so Alloc is used to add a reference
            // to the delegate, allowing relocation of the delegate, but preventing
            // disposal. Using GCHandle without pinning reduces fragmentation potential
            // of the managed heap.
            {
                WSManPluginShellDelegate pluginShell = new WSManPluginShellDelegate(WSManPluginManagedEntryWrapper.WSManPluginShell);
                _pluginShellGCHandle = GCHandle.Alloc(pluginShell);
                // marshal the delegate to a unmanaged function pointer so that AppDomain reference is stored correctly.
                // Populate the outgoing structure so the caller has access to the entry points
                _unmanagedStruct.wsManPluginShellCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginShell);
            }
            {
                WSManPluginReleaseShellContextDelegate pluginReleaseShellContext = new WSManPluginReleaseShellContextDelegate(WSManPluginManagedEntryWrapper.WSManPluginReleaseShellContext);
                _pluginReleaseShellContextGCHandle = GCHandle.Alloc(pluginReleaseShellContext);
                _unmanagedStruct.wsManPluginReleaseShellContextCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginReleaseShellContext);
            }
            {
                WSManPluginCommandDelegate pluginCommand = new WSManPluginCommandDelegate(WSManPluginManagedEntryWrapper.WSManPluginCommand);
                _pluginCommandGCHandle = GCHandle.Alloc(pluginCommand);
                _unmanagedStruct.wsManPluginCommandCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginCommand);
            }
            {
                WSManPluginReleaseCommandContextDelegate pluginReleaseCommandContext = new WSManPluginReleaseCommandContextDelegate(WSManPluginManagedEntryWrapper.WSManPluginReleaseCommandContext);
                _pluginReleaseCommandContextGCHandle = GCHandle.Alloc(pluginReleaseCommandContext);
                _unmanagedStruct.wsManPluginReleaseCommandContextCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginReleaseCommandContext);
            }
            {
                WSManPluginSendDelegate pluginSend = new WSManPluginSendDelegate(WSManPluginManagedEntryWrapper.WSManPluginSend);
                _pluginSendGCHandle = GCHandle.Alloc(pluginSend);
                _unmanagedStruct.wsManPluginSendCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginSend);
            }
            {
                WSManPluginReceiveDelegate pluginReceive = new WSManPluginReceiveDelegate(WSManPluginManagedEntryWrapper.WSManPluginReceive);
                _pluginReceiveGCHandle = GCHandle.Alloc(pluginReceive);
                _unmanagedStruct.wsManPluginReceiveCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginReceive);
            }
            {
                WSManPluginSignalDelegate pluginSignal = new WSManPluginSignalDelegate(WSManPluginManagedEntryWrapper.WSManPluginSignal);
                _pluginSignalGCHandle = GCHandle.Alloc(pluginSignal);
                _unmanagedStruct.wsManPluginSignalCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginSignal);
            }
            {
                WSManPluginConnectDelegate pluginConnect = new WSManPluginConnectDelegate(WSManPluginManagedEntryWrapper.WSManPluginConnect);
                _pluginConnectGCHandle = GCHandle.Alloc(pluginConnect);
                _unmanagedStruct.wsManPluginConnectCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginConnect);
            }
            {
                WSManShutdownPluginDelegate shutdownPlugin = new WSManShutdownPluginDelegate(WSManPluginManagedEntryWrapper.ShutdownPlugin);
                _shutdownPluginGCHandle = GCHandle.Alloc(shutdownPlugin);
                _unmanagedStruct.wsManPluginShutdownPluginCallbackNative = Marshal.GetFunctionPointerForDelegate(shutdownPlugin);
            }

            if (!Platform.IsWindows)
            {
                WSManPluginOperationShutdownDelegate pluginShutDownDelegate = new WSManPluginOperationShutdownDelegate(WSManPluginManagedEntryWrapper.WSManPSShutdown);
                _WSManPluginOperationShutdownGCHandle = GCHandle.Alloc(pluginShutDownDelegate);
                _unmanagedStruct.wsManPluginShutdownCallbackNative = Marshal.GetFunctionPointerForDelegate(pluginShutDownDelegate);
            }
        }

        /// <summary>
        /// </summary>
        private void CleanUpDelegates()
        {
            // Free GCHandles so that the memory they point to may be unpinned (garbage collected)
            if (_pluginShellGCHandle.IsAllocated)
            {
                _pluginShellGCHandle.Free();
                _pluginReleaseShellContextGCHandle.Free();
                _pluginCommandGCHandle.Free();
                _pluginReleaseCommandContextGCHandle.Free();
                _pluginSendGCHandle.Free();
                _pluginReceiveGCHandle.Free();
                _pluginSignalGCHandle.Free();
                _pluginConnectGCHandle.Free();
                _shutdownPluginGCHandle.Free();
                if (!Platform.IsWindows)
                {
                    _WSManPluginOperationShutdownGCHandle.Free();
                }
            }
        }

        /// <summary>
        /// Structure definition to match the native one.
        /// NOTE: The layout of this structure must be IDENTICAL between here and PwrshPluginWkr_Ptrs in pwrshplugindefs.h!
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class WSManPluginEntryDelegatesInternal
        {
            /// <summary>
            /// WsManPluginShutdownPluginCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginShutdownPluginCallbackNative;

            /// <summary>
            /// WSManPluginShellCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginShellCallbackNative;

            /// <summary>
            /// WSManPluginReleaseShellContextCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginReleaseShellContextCallbackNative;

            /// <summary>
            /// WSManPluginCommandCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginCommandCallbackNative;

            /// <summary>
            /// WSManPluginReleaseCommandContextCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginReleaseCommandContextCallbackNative;

            /// <summary>
            /// WSManPluginSendCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginSendCallbackNative;

            /// <summary>
            /// WSManPluginReceiveCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginReceiveCallbackNative;

            /// <summary>
            /// WSManPluginSignalCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginSignalCallbackNative;

            /// <summary>
            /// WSManPluginConnectCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginConnectCallbackNative;

            /// <summary>
            /// WSManPluginCommandCallbackNative.
            /// </summary>
            internal IntPtr wsManPluginShutdownCallbackNative;
        }
    }

    /// <summary>
    /// Class containing the public static managed entry functions that are callable from outside
    /// the module.
    /// </summary>
    public sealed class WSManPluginManagedEntryWrapper
    {
        /// <summary>
        /// Constructor is private because it only contains static members and properties.
        /// </summary>
        private WSManPluginManagedEntryWrapper() { }

        /// <summary>
        /// Immutable container that holds the delegates and their unmanaged pointers.
        /// </summary>
        internal static readonly WSManPluginEntryDelegates workerPtrs = new WSManPluginEntryDelegates();

        #region Managed Entry Points

        /// <summary>
        /// Called only once after the assembly is loaded..This is used to perform
        /// various initializations.
        /// </summary>
        /// <param name="wkrPtrs">IntPtr to WSManPluginEntryDelegates.WSManPluginEntryDelegatesInternal.</param>
        /// <returns>0 = Success, 1 = Failure.</returns>
        public static int InitPlugin(
            IntPtr wkrPtrs)
        {
            if (wkrPtrs == IntPtr.Zero)
            {
                return WSManPluginConstants.ExitCodeFailure;
            }

            Marshal.StructureToPtr<WSManPluginEntryDelegates.WSManPluginEntryDelegatesInternal>(workerPtrs.UnmanagedStruct, wkrPtrs, false);
            return WSManPluginConstants.ExitCodeSuccess;
        }

        /// <summary>
        /// Called only once during shutdown. This is used to perform various deinitializations.
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        public static void ShutdownPlugin(
            IntPtr pluginContext)
        {
            WSManPluginInstance.PerformShutdown(pluginContext);
            workerPtrs?.Dispose();
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandContext">PVOID optional.</param>
        /// <param name="inboundConnectInformation">WSMAN_DATA* optional.</param>
        public static void WSManPluginConnect(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr inboundConnectInformation)
        {
            if (pluginContext == IntPtr.Zero)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "pluginContext",
                        "WSManPluginConnect")
                    );
                return;
            }

            WSManPluginInstance.PerformWSManPluginConnect(pluginContext, requestDetails, flags, shellContext, commandContext, inboundConnectInformation);
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="extraInfo">PCWSTR.</param>
        /// <param name="startupInfo">WSMAN_SHELL_STARTUP_INFO*.</param>
        /// <param name="inboundShellInformation">WSMAN_DATA*.</param>
        public static void WSManPluginShell(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string extraInfo,
            IntPtr startupInfo,
            IntPtr inboundShellInformation)
        {
            if (pluginContext == IntPtr.Zero)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "pluginContext",
                        "WSManPluginShell")
                    );
                return;
            }

#if(DEBUG)
            // In debug builds, allow remote runspaces to wait for debugger attach
            if (Environment.GetEnvironmentVariable("__PSRemoteRunspaceWaitForDebugger", EnvironmentVariableTarget.Machine) != null)
            {
                bool debuggerAttached = false;
                while (!debuggerAttached)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
#endif

            WSManPluginInstance.PerformWSManPluginShell(pluginContext, requestDetails, flags, extraInfo, startupInfo, inboundShellInformation);
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="shellContext">PVOID.</param>
        public static void WSManPluginReleaseShellContext(
            IntPtr pluginContext,
            IntPtr shellContext)
        {
            // NO-OP..as our plugin does not own the memory related
            // to shellContext and so there is nothing to release
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandLine">PCWSTR.</param>
        /// <param name="arguments">WSMAN_COMMAND_ARG_SET* optional.</param>
        public static void WSManPluginCommand(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
            IntPtr arguments)
        {
            if (pluginContext == IntPtr.Zero)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "Plugin Context",
                        "WSManPluginCommand")
                    );
                return;
            }

            WSManPluginInstance.PerformWSManPluginCommand(pluginContext, requestDetails, flags, shellContext, commandLine, arguments);
        }

        /// <summary>
        /// Operation shutdown notification that was registered with the native layer for each of the shellCreate operations.
        /// </summary>
        /// <param name="shutdownContext">IntPtr.</param>
        public static void WSManPSShutdown(
            IntPtr shutdownContext)
        {
            GCHandle gch = GCHandle.FromIntPtr(shutdownContext);
            EventWaitHandle eventHandle = (EventWaitHandle)gch.Target;
            eventHandle.Set();
            gch.Free();
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandContext">PVOID.</param>
        public static void WSManPluginReleaseCommandContext(
            IntPtr pluginContext,
            IntPtr shellContext,
            IntPtr commandContext)
        {
            // NO-OP..as our plugin does not own the memory related
            // to commandContext and so there is nothing to release.
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandContext">PVOID.</param>
        /// <param name="stream">PCWSTR.</param>
        /// <param name="inboundData">WSMAN_DATA*.</param>
        public static void WSManPluginSend(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            [MarshalAs(UnmanagedType.LPWStr)] string stream,
            IntPtr inboundData)
        {
            if (pluginContext == IntPtr.Zero)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "Plugin Context",
                        "WSManPluginSend")
                    );
                return;
            }

            WSManPluginInstance.PerformWSManPluginSend(pluginContext, requestDetails, flags, shellContext, commandContext, stream, inboundData);
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandContext">PVOID optional.</param>
        /// <param name="streamSet">WSMAN_STREAM_ID_SET* optional.</param>
        public static void WSManPluginReceive(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr streamSet)
        {
            if (pluginContext == IntPtr.Zero)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "Plugin Context",
                        "WSManPluginReceive")
                    );
                return;
            }

            WSManPluginInstance.PerformWSManPluginReceive(pluginContext, requestDetails, flags, shellContext, commandContext, streamSet);
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginContext">PVOID.</param>
        /// <param name="requestDetails">WSMAN_PLUGIN_REQUEST*.</param>
        /// <param name="flags">DWORD.</param>
        /// <param name="shellContext">PVOID.</param>
        /// <param name="commandContext">PVOID optional.</param>
        /// <param name="code">PCWSTR.</param>
        public static void WSManPluginSignal(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            [MarshalAs(UnmanagedType.LPWStr)] string code)
        {
            if ((pluginContext == IntPtr.Zero) || (shellContext == IntPtr.Zero))
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullPluginContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullPluginContext,
                        "Plugin Context",
                        "WSManPluginSignal")
                    );
                return;
            }

            WSManPluginInstance.PerformWSManPluginSignal(pluginContext, requestDetails, flags, shellContext, commandContext, code);
        }

        /// <summary>
        /// Callback used to register with thread pool to notify when a plugin operation shuts down.
        /// Conforms to:
        ///     public delegate void WaitOrTimerCallback( Object state, bool timedOut )
        /// </summary>
        /// <param name="operationContext">PVOID.</param>
        /// <param name="timedOut">BOOLEAN.</param>
        /// <returns></returns>
        public static void PSPluginOperationShutdownCallback(
            object operationContext,
            bool timedOut)
        {
            if (operationContext == null)
            {
                return;
            }

            WSManPluginOperationShutdownContext context = (WSManPluginOperationShutdownContext)operationContext;
            context.isShuttingDown = true;

            WSManPluginInstance.PerformCloseOperation(context);
        }

        #endregion
    }

    /// <summary>
    /// This is a thin wrapper around WSManPluginManagedEntryWrapper.InitPlugin()
    /// so that it can be called from native COM code in a non-static context.
    ///
    /// This was done to get around an FXCop error: AvoidStaticMembersInComVisibleTypes.
    /// </summary>
    public sealed class WSManPluginManagedEntryInstanceWrapper : IDisposable
    {
        #region IDisposable

        // Flag: Has Dispose already been called?
        private bool _disposed = false;

        /// <summary>
        /// Internal implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Free any unmanaged objects here.
            _initDelegateHandle.Free();

            _disposed = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WSManPluginManagedEntryInstanceWrapper"/> class.
        /// </summary>
        ~WSManPluginManagedEntryInstanceWrapper()
        {
            Dispose(false);
        }

        #endregion

        #region Delegate Handling

        /// <summary>
        /// Matches signature for WSManPluginManagedEntryWrapper.InitPlugin.
        /// </summary>
        /// <param name="wkrPtrs"></param>
        /// <returns></returns>
        private delegate int InitPluginDelegate(
            IntPtr wkrPtrs);

        /// <summary>
        /// Prevents the delegate object from being garbage collected so it can be passed to the native code.
        /// </summary>
        private GCHandle _initDelegateHandle;

        /// <summary>
        /// Entry point for native code that cannot call static methods.
        /// </summary>
        /// <returns>A function pointer for the static entry point for the WSManPlugin initialization function.</returns>
        public IntPtr GetEntryDelegate()
        {
            InitPluginDelegate initDelegate = new InitPluginDelegate(WSManPluginManagedEntryWrapper.InitPlugin);
            _initDelegateHandle = GCHandle.Alloc(initDelegate);
            return Marshal.GetFunctionPointerForDelegate(initDelegate);
        }

        #endregion
    }
}
