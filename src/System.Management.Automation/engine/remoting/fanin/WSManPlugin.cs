// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// ----------------------------------------------------------------------
//  Contents:  Entry points for managed PowerShell plugin worker used to
//  host powershell in a WSMan service.
// ----------------------------------------------------------------------

using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Remoting.WSMan;
using System.Management.Automation.Tracing;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Consolidation of constants for uniformity.
    /// </summary>
    internal static class WSManPluginConstants
    {
        internal const int ExitCodeSuccess = 0x00000000;
        internal const int ExitCodeFailure = 0x00000001;

        internal const string CtrlCSignal = "powershell/signal/crtl_c";

        // The following are the only supported streams in PowerShell remoting.
        // see WSManNativeApi.cs. These are duplicated here to save on
        // Marshalling time.
        internal const string SupportedInputStream = "stdin";
        internal const string SupportedOutputStream = "stdout";
        internal const string SupportedPromptResponseStream = "pr";
        internal const string PowerShellStartupProtocolVersionName = "protocolversion";
        internal const string PowerShellStartupProtocolVersionValue = "2.0";
        internal const string PowerShellOptionPrefix = "PS_";

        internal const int WSManPluginParamsGetRequestedLocale = 5;
        internal const int WSManPluginParamsGetRequestedDataLocale = 6;
    }

    /// <summary>
    /// Definitions of HRESULT error codes that are passed to the client.
    /// 0x8054.... means that it is a PowerShell HRESULT. The PowerShell facility
    /// is 84 (0x54).
    /// </summary>
    internal enum WSManPluginErrorCodes : int
    {
        NullPluginContext = -2141976624, // 0x805407D0
        PluginContextNotFound = -2141976623, // 0x805407D1

        NullInvalidInput = -2141975624, // 0x80540BB8
        NullInvalidStreamSets = -2141975623, // 0x80540BB9
        SessionCreationFailed = -2141975622, // 0x80540BBA
        NullShellContext = -2141975621, // 0x80540BBB
        InvalidShellContext = -2141975620, // 0x80540BBC
        InvalidCommandContext = -2141975619, // 0x80540BBD
        InvalidInputStream = -2141975618, // 0x80540BBE
        InvalidInputDatatype = -2141975617, // 0x80540BBF
        InvalidOutputStream = -2141975616, // 0x80540BC0
        InvalidSenderDetails = -2141975615, // 0x80540BC1
        ShutdownRegistrationFailed = -2141975614, // 0x80540BC2
        ReportContextFailed = -2141975613, // 0x80540BC3
        InvalidArgSet = -2141975612, // 0x80540BC4
        ProtocolVersionNotMatch = -2141975611, // 0x80540BC5
        OptionNotUnderstood = -2141975610, // 0x80540BC6
        ProtocolVersionNotFound = -2141975609, // 0x80540BC7

        ManagedException = -2141974624, // 0x80540FA0
        PluginOperationClose = -2141974623, // 0x80540FA1
        PluginConnectNoNegotiationData = -2141974622, // 0x80540FA2
        PluginConnectOperationFailed = -2141974621, // 0x80540FA3

        NoError = 0,
        OutOfMemory = -2147024882  // 0x8007000E
    }

    /// <summary>
    /// Class that holds plugin + shell context information used to handle
    /// shutdown notifications.
    ///
    /// Explicit destruction and release of the IntPtrs is not required because
    /// their lifetime is managed by WinRM.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class WSManPluginOperationShutdownContext // TODO: Rename to OperationShutdownContext when removing the MC++ module.
    {
        #region Internal Members

        internal IntPtr pluginContext;
        internal IntPtr shellContext;
        internal IntPtr commandContext;
        internal bool isReceiveOperation;
        internal bool isShuttingDown;

        #endregion

        #region Constructors

        internal WSManPluginOperationShutdownContext(
            IntPtr plgContext,
            IntPtr shContext,
            IntPtr cmdContext,
            bool isRcvOp)
        {
            pluginContext = plgContext;
            shellContext = shContext;
            commandContext = cmdContext;
            isReceiveOperation = isRcvOp;
            isShuttingDown = false;
        }

        #endregion
    }

    /// <summary>
    /// Represents the logical grouping of all actions required to handle the
    /// lifecycle of shell sessions through the WinRM plugin.
    /// </summary>
    internal class WSManPluginInstance
    {
        #region Private Members

        private Dictionary<IntPtr, WSManPluginShellSession> _activeShellSessions;
        private object _syncObject;
        private static Dictionary<IntPtr, WSManPluginInstance> s_activePlugins = new Dictionary<IntPtr, WSManPluginInstance>();

        /// <summary>
        /// Enables dependency injection after the static constructor is called.
        /// This may be overridden in unit tests to enable different behavior.
        /// It is static because static instances of this class use the facade. Otherwise,
        /// it would be passed in via a parameterized constructor.
        /// </summary>
        internal static IWSManNativeApiFacade wsmanPinvokeStatic = new WSManNativeApiFacade();

        #endregion

        #region Constructor and Destructor

        internal WSManPluginInstance()
        {
            _activeShellSessions = new Dictionary<IntPtr, WSManPluginShellSession>();
            _syncObject = new System.Object();
        }

        /// <summary>
        /// Static constructor to listen to unhandled exceptions
        /// from the AppDomain and log the errors
        /// Note: It is not necessary to instantiate IWSManNativeApi here because it is not used.
        /// </summary>
        static WSManPluginInstance()
        {
            // NOTE - the order is important here:
            // because handler from WindowsErrorReporting is going to terminate the process
            // we want it to fire last

#if !CORECLR
            // Register our remoting handler for crashes
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(WSManPluginInstance.UnhandledExceptionHandler);
#endif
        }

        #endregion

        /// <summary>
        /// Create a new shell in the plugin context.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="extraInfo"></param>
        /// <param name="startupInfo"></param>
        /// <param name="inboundShellInformation"></param>
        internal void CreateShell(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            string extraInfo,
            WSManNativeApi.WSManShellStartupInfo_UnToMan startupInfo,
            WSManNativeApi.WSManData_UnToMan inboundShellInformation)
        {
            if (requestDetails == null)
            {
                // Nothing can be done because requestDetails are required to report operation complete
                PSEtwLog.LogAnalyticInformational(PSEventId.ReportOperationComplete,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    "null",
                    Convert.ToString(WSManPluginErrorCodes.NullInvalidInput, CultureInfo.InvariantCulture),
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "requestDetails",
                        "WSManPluginShell"),
                    string.Empty);
                return;
            }

            if ((requestDetails.senderDetails == null) ||
                (requestDetails.operationInfo == null))
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullInvalidInput,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "requestDetails",
                        "WSManPluginShell"));
                return;
            }

            if (startupInfo == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullInvalidInput,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "startupInfo",
                        "WSManPluginShell"));
                return;
            }

            if ((0 == startupInfo.inputStreamSet.streamIDsCount) || (0 == startupInfo.outputStreamSet.streamIDsCount))
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullInvalidStreamSets,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidStreamSet,
                        WSManPluginConstants.SupportedInputStream,
                        WSManPluginConstants.SupportedOutputStream));
                return;
            }

            if (string.IsNullOrEmpty(extraInfo))
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullInvalidInput,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "extraInfo",
                        "WSManPluginShell"));
                return;
            }

            WSManPluginInstance.SetThreadProperties(requestDetails);

            // check if protocolversion option is honored
            if (!EnsureOptionsComply(requestDetails))
            {
                return;
            }

            int result = WSManPluginConstants.ExitCodeSuccess;
            WSManPluginShellSession mgdShellSession;
            WSManPluginOperationShutdownContext context;
            byte[] convertedBase64 = null;

            try
            {
                PSSenderInfo senderInfo = GetPSSenderInfo(requestDetails.senderDetails);

                // inbound shell information is already verified by pwrshplugin.dll.. so no need
                // to verify here.
                WSManPluginServerTransportManager serverTransportMgr;

                if (Platform.IsWindows)
                {
                    serverTransportMgr = new WSManPluginServerTransportManager(BaseTransportManager.DefaultFragmentSize, new PSRemotingCryptoHelperServer());
                }
                else
                {
                    serverTransportMgr = new WSManPluginServerTransportManager(BaseTransportManager.DefaultFragmentSize, null);
                }

                PSEtwLog.LogAnalyticInformational(PSEventId.ServerCreateRemoteSession,
                    PSOpcode.Connect, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    requestDetails.ToString(), senderInfo.UserInfo.Identity.Name, requestDetails.resourceUri);
                ServerRemoteSession remoteShellSession = ServerRemoteSession.CreateServerRemoteSession(senderInfo,
                    requestDetails.resourceUri,
                    extraInfo,
                    serverTransportMgr);

                if (remoteShellSession == null)
                {
                    WSManPluginInstance.ReportWSManOperationComplete(
                        requestDetails,
                        WSManPluginErrorCodes.SessionCreationFailed);
                    return;
                }

                context = new WSManPluginOperationShutdownContext(pluginContext, requestDetails.unmanagedHandle, IntPtr.Zero, false);
                if (context == null)
                {
                    ReportOperationComplete(requestDetails, WSManPluginErrorCodes.OutOfMemory);
                    return;
                }

                // Create a shell session wrapper to track and service future interactions.
                mgdShellSession = new WSManPluginShellSession(requestDetails, serverTransportMgr, remoteShellSession, context);
                AddToActiveShellSessions(mgdShellSession);
                mgdShellSession.SessionClosed += new EventHandler<EventArgs>(HandleShellSessionClosed);

                if (inboundShellInformation != null)
                {
                    if ((uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT != inboundShellInformation.Type)
                    {
                        // only text data is supported
                        ReportOperationComplete(
                            requestDetails,
                            WSManPluginErrorCodes.InvalidInputDatatype,
                            StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginInvalidInputDataType,
                                "WSMAN_DATA_TYPE_TEXT"));
                        DeleteFromActiveShellSessions(requestDetails.unmanagedHandle);
                        return;
                    }
                    else
                    {
                        convertedBase64 = ServerOperationHelpers.ExtractEncodedXmlElement(
                            inboundShellInformation.Text,
                            WSManNativeApi.PS_CREATION_XML_TAG);
                    }
                }

                // now report the shell context to WSMan.
                PSEtwLog.LogAnalyticInformational(PSEventId.ReportContext,
                    PSOpcode.Connect, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    requestDetails.ToString(), requestDetails.ToString());
                result = wsmanPinvokeStatic.WSManPluginReportContext(requestDetails.unmanagedHandle, 0, requestDetails.unmanagedHandle);

                if (WSManPluginConstants.ExitCodeSuccess != result)
                {
                    ReportOperationComplete(
                        requestDetails,
                        WSManPluginErrorCodes.ReportContextFailed,
                        StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginReportContextFailed));
                    DeleteFromActiveShellSessions(requestDetails.unmanagedHandle);
                    return;
                }
            }
            catch (System.Exception e)
            {
                PSEtwLog.LogOperationalError(PSEventId.TransportError,
                    PSOpcode.Connect, PSTask.None, PSKeyword.UseAlwaysOperational, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000",
                    Convert.ToString(WSManPluginErrorCodes.ManagedException, CultureInfo.InvariantCulture), e.Message, e.StackTrace);

                DeleteFromActiveShellSessions(requestDetails.unmanagedHandle);
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ManagedException,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginManagedException,
                        e.Message));
                return;
            }

            bool isRegisterWaitForSingleObjectSucceeded = true;

            // always synchronize calls to OperationComplete once notification handle is registered.. else duplicate OperationComplete calls are bound to happen
            lock (mgdShellSession.shellSyncObject)
            {
                mgdShellSession.registeredShutdownNotification = 1;

                // Wrap the provided handle so it can be passed to the registration function
                EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                if (Platform.IsWindows)
                {
                    SafeWaitHandle safeWaitHandle = new SafeWaitHandle(requestDetails.shutdownNotificationHandle, false); // Owned by WinRM
                    eventWaitHandle.SafeWaitHandle = safeWaitHandle;
                }
                else
                {
                    // On non-windows platforms the shutdown notification is done through a callback instead of a windows event handle.
                    // Register the callback and this will then signal the event. Note, the gch object is deleted in the shell shutdown
                    // notification that will always come in to shut down the operation.

                    GCHandle gch = GCHandle.Alloc(eventWaitHandle);
                    IntPtr p = GCHandle.ToIntPtr(gch);

                    wsmanPinvokeStatic.WSManPluginRegisterShutdownCallback(
                                                           requestDetails.unmanagedHandle,
                                                           WSManPluginManagedEntryWrapper.workerPtrs.UnmanagedStruct.wsManPluginShutdownCallbackNative,
                                                           p);
                }

                mgdShellSession.registeredShutDownWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                                 eventWaitHandle,
                                 new WaitOrTimerCallback(WSManPluginManagedEntryWrapper.PSPluginOperationShutdownCallback),
                                 context,
                                 -1, // INFINITE
                                 true); // TODO: Do I need to worry not being able to set missing WT_TRANSFER_IMPERSONATION?
                if (mgdShellSession.registeredShutDownWaitHandle == null)
                {
                    isRegisterWaitForSingleObjectSucceeded = false;
                }
            }

            if (!isRegisterWaitForSingleObjectSucceeded)
            {
                mgdShellSession.registeredShutdownNotification = 0;
                WSManPluginInstance.ReportWSManOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ShutdownRegistrationFailed);
                DeleteFromActiveShellSessions(requestDetails.unmanagedHandle);
                return;
            }

            try
            {
                if (convertedBase64 != null)
                {
                    mgdShellSession.SendOneItemToSessionHelper(convertedBase64, WSManPluginConstants.SupportedInputStream);
                }
            }
            catch (System.Exception e)
            {
                PSEtwLog.LogOperationalError(PSEventId.TransportError,
                    PSOpcode.Connect, PSTask.None, PSKeyword.UseAlwaysOperational, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000",
                    Convert.ToString(WSManPluginErrorCodes.ManagedException, CultureInfo.InvariantCulture), e.Message, e.StackTrace);

                if (Interlocked.Exchange(ref mgdShellSession.registeredShutdownNotification, 0) == 1)
                {
                    // unregister callback.. wait for any ongoing callbacks to complete.. nothing much we could do if this fails
                    bool ignore = mgdShellSession.registeredShutDownWaitHandle.Unregister(null);
                    mgdShellSession.registeredShutDownWaitHandle = null;

                    // this will called OperationComplete
                    PerformCloseOperation(context);
                }

                return;
            }

            return;
        }

        /// <summary>
        /// This gets called on a thread pool thread once Shutdown wait handle is notified.
        /// </summary>
        /// <param name="context"></param>
        internal void CloseShellOperation(
            WSManPluginOperationShutdownContext context)
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.ServerCloseOperation,
                    PSOpcode.Disconnect, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                ((IntPtr)context.shellContext).ToString(),
                ((IntPtr)context.commandContext).ToString(),
                context.isReceiveOperation.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(context.shellContext);
            if (mgdShellSession == null)
            {
                // this should never be the case. this will protect the service.
                // Dbg.Assert(false, "context.shellContext not matched");
                return;
            }

            // update the internal data store only if this is not receive operation.
            if (!context.isReceiveOperation)
            {
                DeleteFromActiveShellSessions(context.shellContext);
            }

            System.Exception reasonForClose = new System.Exception(RemotingErrorIdStrings.WSManPluginOperationClose);
            mgdShellSession.CloseOperation(context, reasonForClose);
        }

        internal void CloseCommandOperation(
            WSManPluginOperationShutdownContext context)
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.ServerCloseOperation,
                PSOpcode.Disconnect, PSTask.None,
                PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                context.shellContext.ToString(),
                context.commandContext.ToString(),
                context.isReceiveOperation.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(context.shellContext);
            if (mgdShellSession == null)
            {
                // this should never be the case. this will protect the service.
                // Dbg.Assert(false, "context.shellContext not matched");
                return;
            }

            mgdShellSession.CloseCommandOperation(context);
        }

        /// <summary>
        /// Adds shell session to activeShellSessions store and returns the id
        /// at which the session is added.
        /// </summary>
        /// <param name="newShellSession"></param>
        private void AddToActiveShellSessions(
            WSManPluginShellSession newShellSession)
        {
            int count = -1;
            lock (_syncObject)
            {
                IntPtr key = newShellSession.creationRequestDetails.unmanagedHandle;
                Dbg.Assert(IntPtr.Zero != key, "NULL handles should not be provided");

                if (!_activeShellSessions.ContainsKey(key))
                {
                    _activeShellSessions.Add(key, newShellSession);

                    // trigger an event outside the lock
                    count = _activeShellSessions.Count;
                }
            }

            if (-1 != count)
            {
                // Raise session count changed event
                WSManServerChannelEvents.RaiseActiveSessionsChangedEvent(new ActiveSessionsChangedEventArgs(count));
            }
        }

        /// <summary>
        /// Retrieves a WSManPluginShellSession if matched.
        /// </summary>
        /// <param name="key">Shell context (WSManPluginRequest.unmanagedHandle).</param>
        /// <returns>Null WSManPluginShellSession if not matched. The object if matched.</returns>
        private WSManPluginShellSession GetFromActiveShellSessions(
            IntPtr key)
        {
            lock (_syncObject)
            {
                WSManPluginShellSession result;
                _activeShellSessions.TryGetValue(key, out result);
                return result;
            }
        }

        /// <summary>
        /// Removes a WSManPluginShellSession from tracking.
        /// </summary>
        /// <param name="keyToDelete">IntPtr of a WSManPluginRequest structure.</param>
        private void DeleteFromActiveShellSessions(
            IntPtr keyToDelete)
        {
            int count = -1;
            lock (_syncObject)
            {
                if (_activeShellSessions.Remove(keyToDelete))
                {
                    // trigger an event outside the lock
                    count = _activeShellSessions.Count;
                }
            }

            if (-1 != count)
            {
                // Raise session count changed event
                WSManServerChannelEvents.RaiseActiveSessionsChangedEvent(new ActiveSessionsChangedEventArgs(count));
            }
        }

        /// <summary>
        /// Triggers a shell close from an event handler.
        /// </summary>
        /// <param name="source">Shell context.</param>
        /// <param name="e"></param>
        private void HandleShellSessionClosed(
            object source,
            EventArgs e)
        {
            DeleteFromActiveShellSessions((IntPtr)source);
        }

        /// <summary>
        /// Helper function to validate incoming values.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="shellContext"></param>
        /// <param name="inputFunctionName"></param>
        /// <returns></returns>
        private bool validateIncomingContexts(
            WSManNativeApi.WSManPluginRequest requestDetails,
            IntPtr shellContext,
            string inputFunctionName)
        {
            if (requestDetails == null)
            {
                // Nothing can be done because requestDetails are required to report operation complete
                PSEtwLog.LogAnalyticInformational(PSEventId.ReportOperationComplete,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    "null",
                    Convert.ToString(WSManPluginErrorCodes.NullInvalidInput, CultureInfo.InvariantCulture),
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "requestDetails",
                        inputFunctionName),
                    string.Empty);
                return false;
            }

            if (IntPtr.Zero == shellContext)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullShellContext,
                        "ShellContext",
                        inputFunctionName));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create a new command in the shell context.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandLine"></param>
        /// <param name="arguments"></param>
        internal void CreateCommand(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            string commandLine,
            WSManNativeApi.WSManCommandArgSet arguments)
        {
            if (!validateIncomingContexts(requestDetails, shellContext, "WSManRunShellCommandEx"))
            {
                return;
            }

            SetThreadProperties(requestDetails);

            PSEtwLog.LogAnalyticInformational(PSEventId.ServerCreateCommandSession,
                    PSOpcode.Connect, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                ((IntPtr)shellContext).ToString(), requestDetails.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(shellContext);
            if (mgdShellSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidShellContext));
                return;
            }

            mgdShellSession.CreateCommand(pluginContext, requestDetails, flags, commandLine, arguments);
        }

        internal void StopCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            IntPtr shellContext,
            IntPtr commandContext)
        {
            if (requestDetails == null)
            {
                // Nothing can be done because requestDetails are required to report operation complete
                PSEtwLog.LogAnalyticInformational(PSEventId.ReportOperationComplete,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    "null",
                    Convert.ToString(WSManPluginErrorCodes.NullInvalidInput, CultureInfo.InvariantCulture),
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "requestDetails",
                        "StopCommand"),
                    string.Empty);
                return;
            }

            SetThreadProperties(requestDetails);

            PSEtwLog.LogAnalyticInformational(PSEventId.ServerStopCommand,
                    PSOpcode.Disconnect, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    ((IntPtr)shellContext).ToString(),
                    ((IntPtr)commandContext).ToString(),
                    requestDetails.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(shellContext);
            if (mgdShellSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidShellContext));
                return;
            }

            WSManPluginCommandSession mgdCommandSession = mgdShellSession.GetCommandSession(commandContext);
            if (mgdCommandSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidCommandContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidCommandContext));
                return;
            }

            mgdCommandSession.Stop(requestDetails);
        }

        internal void Shutdown()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManPluginShutdown,
                    PSOpcode.ShuttingDown, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic);

            // all active shells should be closed at this point
            Dbg.Assert(_activeShellSessions.Count == 0, "All active shells should be closed");

            // raise shutting down notification
            WSManServerChannelEvents.RaiseShuttingDownEvent();
        }

        /// <summary>
        /// Connect.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="inboundConnectInformation"></param>
        internal void ConnectShellOrCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            WSManNativeApi.WSManData_UnToMan inboundConnectInformation)
        {
            if (!validateIncomingContexts(requestDetails, shellContext, "ConnectShellOrCommand"))
            {
                return;
            }

            // TODO... What does this mean from a new client that has specified diff locale from original client?
            SetThreadProperties(requestDetails);
            // TODO.. Add new ETW events and log
            /*etwTracer.AnalyticChannel.WriteInformation(PSEventId.ServerReceivedData,
                    PSOpcode.Open, PSTask.None,
                ((IntPtr)shellContext).ToString(), ((IntPtr)commandContext).ToString(), ((IntPtr)requestDetails).ToString());*/

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(shellContext);
            if (mgdShellSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidShellContext));
                return;
            }

            if (IntPtr.Zero == commandContext)
            {
                mgdShellSession.ExecuteConnect(requestDetails, flags, inboundConnectInformation);
                return;
            }

            // this connect is on a command
            WSManPluginCommandSession mgdCmdSession = mgdShellSession.GetCommandSession(commandContext);
            if (mgdCmdSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidCommandContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidCommandContext));
                return;
            }

            mgdCmdSession.ExecuteConnect(requestDetails, flags, inboundConnectInformation);
        }

        /// <summary>
        /// Send data to the shell / command specified.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="stream"></param>
        /// <param name="inboundData"></param>
        internal void SendOneItemToShellOrCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            string stream,
            WSManNativeApi.WSManData_UnToMan inboundData)
        {
            if (!validateIncomingContexts(requestDetails, shellContext, "SendOneItemToShellOrCommand"))
            {
                return;
            }

            SetThreadProperties(requestDetails);

            PSEtwLog.LogAnalyticInformational(PSEventId.ServerReceivedData,
                    PSOpcode.Open, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                ((IntPtr)shellContext).ToString(), ((IntPtr)commandContext).ToString(), requestDetails.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(shellContext);
            if (mgdShellSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidShellContext)
                    );
                return;
            }

            if (IntPtr.Zero == commandContext)
            {
                // the data is destined for shell (runspace) session. so let shell handle it
                mgdShellSession.SendOneItemToSession(requestDetails, flags, stream, inboundData);
                return;
            }

            // the data is destined for command.
            WSManPluginCommandSession mgdCmdSession = mgdShellSession.GetCommandSession(commandContext);
            if (mgdCmdSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidCommandContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidCommandContext));
                return;
            }

            mgdCmdSession.SendOneItemToSession(requestDetails, flags, stream, inboundData);
        }

        /// <summary>
        /// Unlock the shell / command specified so that the shell / command
        /// starts sending data to the client.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="streamSet"></param>
        internal void EnableShellOrCommandToSendDataToClient(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            WSManNativeApi.WSManStreamIDSet_UnToMan streamSet)
        {
            if (!validateIncomingContexts(requestDetails, shellContext, "EnableShellOrCommandToSendDataToClient"))
            {
                return;
            }

            SetThreadProperties(requestDetails);

            PSEtwLog.LogAnalyticInformational(PSEventId.ServerClientReceiveRequest,
                    PSOpcode.Open, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    ((IntPtr)shellContext).ToString(),
                    ((IntPtr)commandContext).ToString(),
                    requestDetails.ToString());

            WSManPluginShellSession mgdShellSession = GetFromActiveShellSessions(shellContext);
            if (mgdShellSession == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidShellContext,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidShellContext)
                    );
                return;
            }

            WSManPluginOperationShutdownContext ctxtToReport = new WSManPluginOperationShutdownContext(pluginContext, shellContext, IntPtr.Zero, true);
            if (ctxtToReport == null)
            {
                ReportOperationComplete(requestDetails, WSManPluginErrorCodes.OutOfMemory);
                return;
            }

            if (IntPtr.Zero == commandContext)
            {
                // the instruction is destined for shell (runspace) session. so let shell handle it
                if (mgdShellSession.EnableSessionToSendDataToClient(requestDetails, flags, streamSet, ctxtToReport))
                {
                    return;
                }
            }
            else
            {
                // the instruction is destined for command
                ctxtToReport.commandContext = commandContext;
                WSManPluginCommandSession mgdCmdSession = mgdShellSession.GetCommandSession(commandContext);

                if (mgdCmdSession == null)
                {
                    ReportOperationComplete(
                        requestDetails,
                        WSManPluginErrorCodes.InvalidCommandContext,
                        StringUtil.Format(
                            RemotingErrorIdStrings.WSManPluginInvalidCommandContext));
                    return;
                }

                if (mgdCmdSession.EnableSessionToSendDataToClient(requestDetails, flags, streamSet, ctxtToReport))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Used to create PSPrincipal object from senderDetails struct.
        /// </summary>
        /// <param name="senderDetails"></param>
        /// <returns></returns>
        private PSSenderInfo GetPSSenderInfo(
            WSManNativeApi.WSManSenderDetails senderDetails)
        {
            // senderDetails will not be null.
            Dbg.Assert(senderDetails != null, "senderDetails cannot be null");

            // Construct PSIdentity
            PSCertificateDetails psCertDetails = null;
            // Construct Certificate Details
            if (senderDetails.certificateDetails != null)
            {
                psCertDetails = new PSCertificateDetails(
                    senderDetails.certificateDetails.subject,
                    senderDetails.certificateDetails.issuerName,
                    senderDetails.certificateDetails.issuerThumbprint);
            }

            // Construct PSPrincipal
            PSIdentity psIdentity = new PSIdentity(senderDetails.authenticationMechanism, true, senderDetails.senderName, psCertDetails);

            // For Virtual and RunAs accounts WSMan specifies the client token via an environment variable and
            // senderDetails.clientToken should not be used.
            IntPtr clientToken = GetRunAsClientToken();
            clientToken = (clientToken != IntPtr.Zero) ? clientToken : senderDetails.clientToken;
            WindowsIdentity windowsIdentity = null;
            if (clientToken != IntPtr.Zero)
            {
                try
                {
                    windowsIdentity = new WindowsIdentity(clientToken, senderDetails.authenticationMechanism);
                }
                // Suppress exceptions..So windowsIdentity = null in these cases
                catch (ArgumentException)
                {
                    // userToken is 0.
                    // -or-
                    // userToken is duplicated and invalid for impersonation.
                }
                catch (System.Security.SecurityException)
                {
                    // The caller does not have the correct permissions.
                    // -or-
                    // A Win32 error occurred.
                }
            }

            PSPrincipal userPrincipal = new PSPrincipal(psIdentity, windowsIdentity);
            PSSenderInfo result = new PSSenderInfo(userPrincipal, senderDetails.httpUrl);
            return result;
        }

        private const string WSManRunAsClientTokenName = "__WINRM_RUNAS_CLIENT_TOKEN__";
        /// <summary>
        /// Helper method to retrieve the WSMan client token from the __WINRM_RUNAS_CLIENT_TOKEN__
        /// environment variable, which is set in the WSMan layer for Virtual or RunAs accounts.
        /// </summary>
        /// <returns>ClientToken IntPtr.</returns>
        private IntPtr GetRunAsClientToken()
        {
            string clientTokenStr = System.Environment.GetEnvironmentVariable(WSManRunAsClientTokenName);
            if (clientTokenStr != null)
            {
                // Remove the token value from the environment variable
                System.Environment.SetEnvironmentVariable(WSManRunAsClientTokenName, null);

                int clientTokenInt;
                if (int.TryParse(clientTokenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out clientTokenInt))
                {
                    return new IntPtr(clientTokenInt);
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Was private. Made protected internal for easier testing.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <returns></returns>
        protected internal bool EnsureOptionsComply(
            WSManNativeApi.WSManPluginRequest requestDetails)
        {
            WSManNativeApi.WSManOption[] options = requestDetails.operationInfo.optionSet.options;
            bool isProtocolVersionDeclared = false;

            for (int i = 0; i < options.Length; i++) // What about requestDetails.operationInfo.optionSet.optionsCount? It is a hold over from the C++ API. Safer is Length.
            {
                WSManNativeApi.WSManOption option = options[i];

                if (string.Equals(option.name, WSManPluginConstants.PowerShellStartupProtocolVersionName, StringComparison.Ordinal))
                {
                    if (!EnsureProtocolVersionComplies(requestDetails, option.value))
                    {
                        return false;
                    }

                    isProtocolVersionDeclared = true;
                }

                if (0 == string.Compare(option.name, 0, WSManPluginConstants.PowerShellOptionPrefix, 0, WSManPluginConstants.PowerShellOptionPrefix.Length, StringComparison.Ordinal))
                {
                    if (option.mustComply)
                    {
                        ReportOperationComplete(
                            requestDetails,
                            WSManPluginErrorCodes.OptionNotUnderstood,
                            StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginOptionNotUnderstood,
                                option.name,
                                System.Management.Automation.PSVersionInfo.GitCommitId,
                                WSManPluginConstants.PowerShellStartupProtocolVersionValue));
                        return false;
                    }
                }
            }

            if (!isProtocolVersionDeclared)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ProtocolVersionNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginProtocolVersionNotFound,
                        WSManPluginConstants.PowerShellStartupProtocolVersionName,
                        System.Management.Automation.PSVersionInfo.GitCommitId,
                        WSManPluginConstants.PowerShellStartupProtocolVersionValue));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that the protocol version is in the correct syntax and supported.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="clientVersionString"></param>
        /// <returns></returns>
        protected internal bool EnsureProtocolVersionComplies(
            WSManNativeApi.WSManPluginRequest requestDetails,
            string clientVersionString)
        {
            if (string.Equals(clientVersionString, WSManPluginConstants.PowerShellStartupProtocolVersionValue, StringComparison.Ordinal))
            {
                return true;
            }

            // Check if major versions are equal and server's minor version is smaller..
            // if so client's version is supported by the server. The understanding is
            // that minor version changes do not break the protocol.
            System.Version clientVersion = Utils.StringToVersion(clientVersionString);
            System.Version serverVersion = Utils.StringToVersion(WSManPluginConstants.PowerShellStartupProtocolVersionValue);

            if ((clientVersion != null) && (serverVersion != null) &&
                (clientVersion.Major == serverVersion.Major) &&
                (clientVersion.Minor >= serverVersion.Minor))
            {
                return true;
            }

            ReportOperationComplete(
                requestDetails,
                WSManPluginErrorCodes.ProtocolVersionNotMatch,
                StringUtil.Format(
                    RemotingErrorIdStrings.WSManPluginProtocolVersionNotMatch,
                    WSManPluginConstants.PowerShellStartupProtocolVersionValue,
                    System.Management.Automation.PSVersionInfo.GitCommitId,
                    clientVersionString));
            return false;
        }

        /// <summary>
        /// Static func to take care of unmanaged to managed transitions.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="extraInfo"></param>
        /// <param name="startupInfo"></param>
        /// <param name="inboundShellInformation"></param>
        internal static void PerformWSManPluginShell(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            string extraInfo,
            IntPtr startupInfo, // WSMAN_SHELL_STARTUP_INFO*
            IntPtr inboundShellInformation) // WSMAN_DATA*
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                lock (s_activePlugins)
                {
                    pluginToUse = GetFromActivePlugins(pluginContext);
                    if (pluginToUse == null)
                    {
                        // create a new plugin
                        WSManPluginInstance mgdPlugin = new WSManPluginInstance();
                        AddToActivePlugins(pluginContext, mgdPlugin);
                        pluginToUse = mgdPlugin;
                    }
                }
            }

            // Marshal the incoming pointers into managed types prior to the call
            WSManNativeApi.WSManPluginRequest requestDetailsInstance = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);
            WSManNativeApi.WSManShellStartupInfo_UnToMan startupInfoInstance = WSManNativeApi.WSManShellStartupInfo_UnToMan.UnMarshal(startupInfo);
            WSManNativeApi.WSManData_UnToMan inboundShellInfo = WSManNativeApi.WSManData_UnToMan.UnMarshal(inboundShellInformation);

            pluginToUse.CreateShell(pluginContext, requestDetailsInstance, flags, extraInfo, startupInfoInstance, inboundShellInfo);
        }

        internal static void PerformWSManPluginCommand(
            IntPtr pluginContext,
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
            IntPtr arguments) // WSMAN_COMMAND_ARG_SET*
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.PluginContextNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginContextNotFound));
                return;
            }

            // Marshal the incoming pointers into managed types prior to the call
            WSManNativeApi.WSManPluginRequest request = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);
            WSManNativeApi.WSManCommandArgSet argSet = WSManNativeApi.WSManCommandArgSet.UnMarshal(arguments);

            pluginToUse.CreateCommand(pluginContext, request, flags, shellContext, commandLine, argSet);
        }

        internal static void PerformWSManPluginConnect(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr inboundConnectInformation)
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.PluginContextNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginContextNotFound));
                return;
            }

            // Marshal the incoming pointers into managed types prior to the call
            WSManNativeApi.WSManPluginRequest request = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);
            WSManNativeApi.WSManData_UnToMan connectInformation = WSManNativeApi.WSManData_UnToMan.UnMarshal(inboundConnectInformation);

            pluginToUse.ConnectShellOrCommand(request, flags, shellContext, commandContext, connectInformation);
        }

        internal static void PerformWSManPluginSend(
            IntPtr pluginContext,
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            IntPtr commandContext, // PVOID
            string stream,
            IntPtr inboundData) // WSMAN_DATA*
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.PluginContextNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginContextNotFound));
                return;
            }

            // Marshal the incoming pointers into managed types prior to the call
            WSManNativeApi.WSManPluginRequest request = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);
            WSManNativeApi.WSManData_UnToMan data = WSManNativeApi.WSManData_UnToMan.UnMarshal(inboundData);

            pluginToUse.SendOneItemToShellOrCommand(request, flags, shellContext, commandContext, stream, data);
        }

        internal static void PerformWSManPluginReceive(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr streamSet) // WSMAN_STREAM_ID_SET*
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.PluginContextNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginContextNotFound));
                return;
            }

            // Marshal the incoming pointers into managed types prior to the call
            WSManNativeApi.WSManPluginRequest request = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);
            WSManNativeApi.WSManStreamIDSet_UnToMan streamIdSet = WSManNativeApi.WSManStreamIDSet_UnToMan.UnMarshal(streamSet);

            pluginToUse.EnableShellOrCommandToSendDataToClient(pluginContext, request, flags, shellContext, commandContext, streamIdSet);
        }

        internal static void PerformWSManPluginSignal(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            IntPtr commandContext, // PVOID
            string code)
        {
            WSManNativeApi.WSManPluginRequest request = WSManNativeApi.WSManPluginRequest.UnMarshal(requestDetails);

            // Close Command
            if (IntPtr.Zero != commandContext)
            {
                if (!string.Equals(code, WSManPluginConstants.CtrlCSignal, StringComparison.Ordinal))
                {
                    // Close operations associated with this command..
                    WSManPluginOperationShutdownContext cmdCtxt = new WSManPluginOperationShutdownContext(pluginContext, shellContext, commandContext, false);
                    if (cmdCtxt != null)
                    {
                        PerformCloseOperation(cmdCtxt);
                    }
                    else
                    {
                        ReportOperationComplete(request, WSManPluginErrorCodes.OutOfMemory);
                        return;
                    }
                }
                else
                {
                    // we got crtl_c (stop) message from client. so stop powershell
                    WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

                    if (pluginToUse == null)
                    {
                        ReportOperationComplete(
                            request,
                            WSManPluginErrorCodes.PluginContextNotFound,
                            StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginContextNotFound));
                        return;
                    }

                    // this will ReportOperationComplete by itself..
                    // so we just here.
                    pluginToUse.StopCommand(request, shellContext, commandContext);
                    return;
                }
            }

            ReportOperationComplete(request, WSManPluginErrorCodes.NoError);
        }

        /// <summary>
        /// Close the operation specified by the supplied context.
        /// </summary>
        /// <param name="context"></param>
        internal static void PerformCloseOperation(
            WSManPluginOperationShutdownContext context)
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(context.pluginContext);

            if (pluginToUse == null)
            {
                return;
            }

            if (IntPtr.Zero == context.commandContext)
            {
                // this is targeted at shell
                pluginToUse.CloseShellOperation(context);
            }
            else
            {
                // shutdown is targeted at command
                pluginToUse.CloseCommandOperation(context);
            }
        }

        /// <summary>
        /// Performs deinitialization during shutdown.
        /// </summary>
        /// <param name="pluginContext"></param>
        internal static void PerformShutdown(
            IntPtr pluginContext)
        {
            WSManPluginInstance pluginToUse = GetFromActivePlugins(pluginContext);

            if (pluginToUse == null)
            {
                return;
            }

            pluginToUse.Shutdown();
        }

        private static WSManPluginInstance GetFromActivePlugins(IntPtr pluginContext)
        {
            lock (s_activePlugins)
            {
                WSManPluginInstance result = null;
                s_activePlugins.TryGetValue(pluginContext, out result);
                return result;
            }
        }

        private static void AddToActivePlugins(IntPtr pluginContext, WSManPluginInstance plugin)
        {
            lock (s_activePlugins)
            {
                if (!s_activePlugins.ContainsKey(pluginContext))
                {
                    s_activePlugins.Add(pluginContext, plugin);
                    return;
                }
            }
        }

        #region Utilities

        /// <summary>
        /// Report operation complete to WSMan and supply a reason (if any)
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        internal static void ReportWSManOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode)
        {
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null in operation complete.");

            PSEtwLog.LogAnalyticInformational(PSEventId.ReportOperationComplete,
                PSOpcode.Close, PSTask.None,
                PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                (requestDetails.unmanagedHandle).ToString(),
                Convert.ToString(errorCode, CultureInfo.InvariantCulture),
                string.Empty,
                string.Empty);

            ReportOperationComplete(requestDetails.unmanagedHandle, errorCode);
        }

        /// <summary>
        /// Extract message from exception (if any) and report operation complete with it to WSMan.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="reasonForClose"></param>
        internal static void ReportWSManOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            Exception reasonForClose)
        {
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null in operation complete.");

            WSManPluginErrorCodes error = WSManPluginErrorCodes.NoError;
            string errorMessage = string.Empty;
            string stackTrace = string.Empty;

            if (reasonForClose != null)
            {
                error = WSManPluginErrorCodes.ManagedException;
                errorMessage = reasonForClose.Message;
                stackTrace = reasonForClose.StackTrace;
            }

            PSEtwLog.LogAnalyticInformational(PSEventId.ReportOperationComplete,
                PSOpcode.Close, PSTask.None,
                PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                requestDetails.ToString(),
                Convert.ToString(error, CultureInfo.InvariantCulture),
                errorMessage,
                stackTrace);

            if (reasonForClose != null)
            {
                // report operation complete to wsman with the error message (if any).
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ManagedException,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginManagedException,
                        reasonForClose.Message));
            }
            else
            {
                ReportOperationComplete(
                    requestDetails.unmanagedHandle,
                    WSManPluginErrorCodes.NoError);
            }
        }

        /// <summary>
        /// Sets thread properties like UI Culture, Culture etc..This is needed as code is transitioning from
        /// unmanaged heap to managed heap...and thread properties are not set correctly during this
        /// transition.
        /// Currently WSMan provider supplies only UI Culture related data..so only UI Culture is set.
        /// </summary>
        /// <param name="requestDetails"></param>
        internal static void SetThreadProperties(
            WSManNativeApi.WSManPluginRequest requestDetails)
        {
            // requestDetails cannot not be null.
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null");

            // IntPtr nativeLocaleData = IntPtr.Zero;
            WSManNativeApi.WSManDataStruct outputStruct = new WSManNativeApi.WSManDataStruct();
            int hResult = wsmanPinvokeStatic.WSManPluginGetOperationParameters(
                requestDetails.unmanagedHandle,
                WSManPluginConstants.WSManPluginParamsGetRequestedLocale,
                outputStruct);
            // ref nativeLocaleData);
            bool retrievingLocaleSucceeded = (0 == hResult);
            WSManNativeApi.WSManData_UnToMan localeData = WSManNativeApi.WSManData_UnToMan.UnMarshal(outputStruct); // nativeLocaleData

            // IntPtr nativeDataLocaleData = IntPtr.Zero;
            hResult = wsmanPinvokeStatic.WSManPluginGetOperationParameters(
                requestDetails.unmanagedHandle,
                WSManPluginConstants.WSManPluginParamsGetRequestedDataLocale,
                outputStruct);
            // ref nativeDataLocaleData);
            bool retrievingDataLocaleSucceeded = ((int)WSManPluginErrorCodes.NoError == hResult);
            WSManNativeApi.WSManData_UnToMan dataLocaleData = WSManNativeApi.WSManData_UnToMan.UnMarshal(outputStruct); // nativeDataLocaleData

            // Set the UI Culture
            try
            {
                if (retrievingLocaleSucceeded && ((uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT == localeData.Type))
                {
                    CultureInfo uiCultureToUse = new CultureInfo(localeData.Text);
                    Thread.CurrentThread.CurrentUICulture = uiCultureToUse;
                }
            }
            // ignore if there is any exception constructing the culture..
            catch (ArgumentException)
            {
            }

            // Set the Culture
            try
            {
                if (retrievingDataLocaleSucceeded && ((uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT == dataLocaleData.Type))
                {
                    CultureInfo cultureToUse = new CultureInfo(dataLocaleData.Text);
                    Thread.CurrentThread.CurrentCulture = cultureToUse;
                }
            }
            // ignore if there is any exception constructing the culture..
            catch (ArgumentException)
            {
            }
        }

#if !CORECLR
        /// <summary>
        /// Handle any unhandled exceptions that get raised in the AppDomain
        /// This will log the exception into Crimson logs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal static void UnhandledExceptionHandler(
            object sender,
            UnhandledExceptionEventArgs args)
        {
            // args can never be null.
            Exception exception = (Exception)args.ExceptionObject;

            PSEtwLog.LogOperationalError(PSEventId.AppDomainUnhandledException,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    exception.GetType().ToString(), exception.Message,
                    exception.StackTrace);

            PSEtwLog.LogAnalyticError(PSEventId.AppDomainUnhandledException_Analytic,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    exception.GetType().ToString(), exception.Message,
                    exception.StackTrace);
        }
#endif

        /// <summary>
        /// Alternate wrapper for WSManPluginOperationComplete. TODO: Needed? I could easily use the handle instead and get rid of this? It is only for easier refactoring...
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage">Pre-formatted localized string.</param>
        /// <returns></returns>
        internal static void ReportOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode,
            string errorMessage)
        {
            if (requestDetails != null)
            {
                ReportOperationComplete(requestDetails.unmanagedHandle, errorCode, errorMessage);
            }
            // else cannot report if requestDetails is null.
        }

        /// <summary>
        /// Wrapper for WSManPluginOperationComplete. It performs validation prior to making the call.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        internal static void ReportOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode)
        {
            if (requestDetails != null &&
                IntPtr.Zero != requestDetails.unmanagedHandle)
            {
                wsmanPinvokeStatic.WSManPluginOperationComplete(
                    requestDetails.unmanagedHandle,
                    0,
                    (int)errorCode,
                    null);
            }
            // else cannot report if requestDetails is null.
        }

        /// <summary>
        /// Wrapper for WSManPluginOperationComplete. It performs validation prior to making the call.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        internal static void ReportOperationComplete(
            IntPtr requestDetails,
            WSManPluginErrorCodes errorCode,
            string errorMessage = "")
        {
            if (IntPtr.Zero == requestDetails)
            {
                // cannot report if requestDetails is null.
                return;
            }

            wsmanPinvokeStatic.WSManPluginOperationComplete(
                requestDetails,
                0,
                (int)errorCode,
                errorMessage);

            return;
        }

        #endregion
    }
}
