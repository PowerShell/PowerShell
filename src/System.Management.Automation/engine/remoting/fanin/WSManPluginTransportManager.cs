// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// ----------------------------------------------------------------------
//  Contents:  Entry points for managed PowerShell plugin worker used to
//  host powershell in a WSMan service.
// ----------------------------------------------------------------------

using System.Threading;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Remoting.Server;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Remoting
{
    internal class WSManPluginServerTransportManager : AbstractServerSessionTransportManager
    {
        private WSManNativeApi.WSManPluginRequest _requestDetails;

        // the following variables are used to block thread from sending
        // data to the client until the client sends a receive request.
        private bool _isRequestPending;
        private readonly object _syncObject;
        private readonly ManualResetEvent _waitHandle;
        private readonly Dictionary<Guid, WSManPluginServerTransportManager> _activeCmdTransportManagers;
        private bool _isClosed;
        // used to keep track of last error..this will be used
        // for reporting operation complete to WSMan.
        private Exception _lastErrorReported;
        // used with RegisterWaitForSingleObject. This object needs to be freed
        // upon close
        private WSManPluginOperationShutdownContext _shutDownContext;

        // tracker used in conjunction with WSMan API to identify a particular
        // shell context.
        private RegisteredWaitHandle _registeredShutDownWaitHandle;

        // event that gets raised when Prepare is called. Respective Session
        // object can use this callback to ReportContext to client.
        public event EventHandler<EventArgs> PrepareCalled;

        #region Constructor

        internal WSManPluginServerTransportManager(
            int fragmentSize,
            PSRemotingCryptoHelper cryptoHelper)
            : base(fragmentSize, cryptoHelper)
        {
            _syncObject = new object();
            _activeCmdTransportManagers = new Dictionary<Guid, WSManPluginServerTransportManager>();
            _waitHandle = new ManualResetEvent(false);
        }

        #endregion

        #region Inherited_from_AbstractServerSessionTransportManager
        internal override void Close(
            Exception reasonForClose)
        {
            DoClose(false, reasonForClose);
        }

        /// <summary>
        /// </summary>
        /// <param name="isShuttingDown">true if the method is called from RegisterWaitForSingleObject
        /// callback. This boolean is used to decide whether to UnregisterWait or
        /// UnregisterWaitEx</param>
        /// <param name="reasonForClose"></param>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "The WSManPluginReceiveResult return value is not documented and is not needed in this case.")]
        internal void DoClose(
            bool isShuttingDown,
            Exception reasonForClose)
        {
            if (_isClosed)
            {
                return;
            }

            lock (_syncObject)
            {
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;
                _lastErrorReported = reasonForClose;

                if (!_isRequestPending)
                {
                    // release threads blocked on the sending data to client if any
                    _waitHandle.Set();
                }
            }

            // only one thread will reach here

            // let everyone know that we are about to close
            try
            {
                RaiseClosingEvent();

                foreach (var cmdTransportKvp in _activeCmdTransportManagers)
                {
                    cmdTransportKvp.Value.Close(reasonForClose);
                }

                _activeCmdTransportManagers.Clear();

                if (_registeredShutDownWaitHandle != null)
                {
                    // This will not wait for the callback to complete.
                    _registeredShutDownWaitHandle.Unregister(null);
                    _registeredShutDownWaitHandle = null;
                }

                // Delete the context only if isShuttingDown != true. isShuttingDown will
                // be true only when the method is called from RegisterWaitForSingleObject
                // handler..in which case the context will be freed from the callback.
                if (_shutDownContext != null)
                {
                    _shutDownContext = null;
                }

                // This might happen when client did not send a receive request
                // but the server is closing
                if (_requestDetails != null)
                {
                    // Notify that no more data is being sent on this transport.
                    WSManNativeApi.WSManPluginReceiveResult(
                        _requestDetails.unmanagedHandle,
                        (int)WSManNativeApi.WSManFlagReceive.WSMAN_FLAG_RECEIVE_RESULT_NO_MORE_DATA,
                        WSManPluginConstants.SupportedOutputStream,
                        IntPtr.Zero,
                        WSManNativeApi.WSMAN_COMMAND_STATE_DONE,
                        0);

                    WSManPluginInstance.ReportWSManOperationComplete(_requestDetails, reasonForClose);
                    // We should not use request details again after reporting operation complete
                    // so releasing the resource. Remember not to free this memory as this memory
                    // is allocated and owned by WSMan.
                    _requestDetails = null;
                }
            }
            finally
            {
                // dispose resources
                _waitHandle.Dispose();
            }
        }

        /// <summary>
        /// Used by powershell DS handler. notifies transport that powershell is back to running state
        /// no payload.
        /// </summary>
        internal override void ReportExecutionStatusAsRunning()
        {
            if (_isClosed)
            {
                return;
            }

            int result = (int)WSManPluginErrorCodes.NoError;

            // there should have been a receive request in place already

            lock (_syncObject)
            {
                if (!_isClosed)
                {
                    result = WSManNativeApi.WSManPluginReceiveResult(
                        _requestDetails.unmanagedHandle,
                        0,
                        null,
                        IntPtr.Zero,
                        WSManNativeApi.WSMAN_COMMAND_STATE_RUNNING,
                        0);
                }
            }

            if (result != (int)WSManPluginErrorCodes.NoError)
            {
                ReportError(result, "WSManPluginReceiveResult");
            }
        }

        /// <summary>
        /// If flush is true, data will be sent immediately to the client. This is accomplished
        /// by using WSMAN_FLAG_RECEIVE_FLUSH flag provided by WSMan API.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flush"></param>
        /// <param name="reportAsPending"></param>
        /// <param name="reportAsDataBoundary"></param>
        protected override void SendDataToClient(
            byte[] data,
            bool flush,
            bool reportAsPending,
            bool reportAsDataBoundary)
        {
            if (_isClosed)
            {
                return;
            }

            // double-check locking mechanism is used here to avoid entering into lock
            // every time data is sent..entering/exiting from lock is costly.
            if (!_isRequestPending)
            {
                // Dont send data until we have received request from client.
                // The following blocks the calling thread. The thread is
                // unblocked once a request from client arrives.
                _waitHandle.WaitOne();
                _isRequestPending = true;
                // at this point request must be pending..so dispose waitHandle
                _waitHandle.Dispose();
            }

            int result = (int)WSManPluginErrorCodes.NoError;
            // at this point we have pending request from client. so it is safe
            // to send data to client using WSMan API.
            using (WSManNativeApi.WSManData_ManToUn dataToBeSent = new WSManNativeApi.WSManData_ManToUn(data))
            {
                lock (_syncObject)
                {
                    if (!_isClosed)
                    {
                        int flags = 0;
                        if (flush)
                            flags |= (int)WSManNativeApi.WSManFlagReceive.WSMAN_FLAG_RECEIVE_FLUSH;
                        if (reportAsDataBoundary)
                            // currently assigning hardcoded value for this flag, this is a new change in wsman.h and needs to be replaced with the actual definition once
                            // modified wsman.h is in public headers
                            flags |= (int)WSManNativeApi.WSManFlagReceive.WSMAN_FLAG_RECEIVE_RESULT_DATA_BOUNDARY;

                        result = WSManNativeApi.WSManPluginReceiveResult(
                            _requestDetails.unmanagedHandle,
                            flags,
                            WSManPluginConstants.SupportedOutputStream,
                            dataToBeSent,
                            reportAsPending ? WSManNativeApi.WSMAN_COMMAND_STATE_PENDING : null,
                            0);
                    }
                }
            }

            if (result != (int)WSManPluginErrorCodes.NoError)
            {
                ReportError(result, "WSManPluginReceiveResult");
            }
        }

        internal override void Prepare()
        {
            // let the base class prepare itself.
            base.Prepare();
            // raise PrepareCalled event and let dependent code to ReportContext.
            // null check is not performed here because Managed C++ will take care of this.
            PrepareCalled(this, EventArgs.Empty);
        }

        /// <summary>
        /// </summary>
        /// <param name="powerShellCmdId"></param>
        /// <returns></returns>
        internal override AbstractServerTransportManager GetCommandTransportManager(
            Guid powerShellCmdId)
        {
            return _activeCmdTransportManagers[powerShellCmdId];
        }

        // Used by command transport manager to manage cmd transport manager instances by session.
        internal void ReportTransportMgrForCmd(
            Guid cmdId,
            WSManPluginServerTransportManager transportManager)
        {
            lock (_syncObject)
            {
                if (_isClosed)
                {
                    return;
                }

                if (!_activeCmdTransportManagers.ContainsKey(cmdId))
                {
                    _activeCmdTransportManagers.Add(cmdId, transportManager);
                }
            }
        }

        internal override void RemoveCommandTransportManager(
            Guid cmdId)
        {
            lock (_syncObject)
            {
                if (_isClosed)
                {
                    return;
                }

                _activeCmdTransportManagers.Remove(cmdId);
            }
        }

        #endregion
        internal bool EnableTransportManagerSendDataToClient(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginOperationShutdownContext ctxtToReport)
        {
            _shutDownContext = ctxtToReport;
            bool isRegisterWaitForSingleObjectSucceeded = true;
            lock (_syncObject)
            {
                if (_isRequestPending)
                {
                    // if a request is already pending..ignore this.
                    WSManPluginInstance.ReportWSManOperationComplete(
                        requestDetails,
                        WSManPluginErrorCodes.NoError);
                    return false;
                }

                if (_isClosed)
                {
                    WSManPluginInstance.ReportWSManOperationComplete(requestDetails, _lastErrorReported);
                    return false;
                }

                _isRequestPending = true;
                _requestDetails = requestDetails;

                if (Platform.IsWindows)
                {
                    // Wrap the provided handle so it can be passed to the registration function
                    SafeWaitHandle safeWaitHandle = new SafeWaitHandle(requestDetails.shutdownNotificationHandle, false); // Owned by WinRM
                    EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                    eventWaitHandle.SafeWaitHandle = safeWaitHandle;

                    _registeredShutDownWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                            eventWaitHandle,
                            new WaitOrTimerCallback(WSManPluginManagedEntryWrapper.PSPluginOperationShutdownCallback),
                            _shutDownContext,
                            -1, // INFINITE
                            true); // TODO: Do I need to worry not being able to set missing WT_TRANSFER_IMPERSONATION?
                    if (_registeredShutDownWaitHandle == null)
                    {
                        isRegisterWaitForSingleObjectSucceeded = false;
                    }
                }
                // release thread waiting to send data to the client.
                _waitHandle.Set();
            }

            if (!isRegisterWaitForSingleObjectSucceeded)
            {
                WSManPluginInstance.PerformCloseOperation(ctxtToReport);
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ShutdownRegistrationFailed,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginShutdownRegistrationFailed));
                return false;
            }

            return true;
        }

        // This will either RaiseClosingEvent or calls DoClose()
        // RaiseClosingEvent will be called if Client has already put a receive request,
        // Otherwise DoClose() is called.
        // This is to make sure server sends all the data it has to Client w.r.t stopping
        // a command like StateChangedInfo etc.
        internal void PerformStop()
        {
            if (_isRequestPending)
            {
                RaiseClosingEvent();
            }
            else
            {
                DoClose(false, null);
            }
        }
    }

    internal class WSManPluginCommandTransportManager : WSManPluginServerTransportManager
    {
        private readonly WSManPluginServerTransportManager _serverTransportMgr;
        private System.Guid _cmdId;

        // Create Cmd Transport Manager for this sessn transport manager
        internal WSManPluginCommandTransportManager(WSManPluginServerTransportManager srvrTransportMgr)
            : base(srvrTransportMgr.Fragmentor.FragmentSize, srvrTransportMgr.CryptoHelper)
        {
            _serverTransportMgr = srvrTransportMgr;
            this.TypeTable = srvrTransportMgr.TypeTable;
        }

        internal void Initialize()
        {
            this.PowerShellGuidObserver += OnPowershellGuidReported;
            this.MigrateDataReadyEventHandlers(_serverTransportMgr);
        }

        private void OnPowershellGuidReported(object src, System.EventArgs args)
        {
            _cmdId = (System.Guid)src;
            _serverTransportMgr.ReportTransportMgrForCmd(_cmdId, this);
            this.PowerShellGuidObserver -= this.OnPowershellGuidReported;
        }
    }
}
