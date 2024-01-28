// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// ----------------------------------------------------------------------
//  Contents:  Entry points for managed PowerShell plugin worker used to
//  host powershell in a WSMan service.
// ----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Tracing;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Abstract class that defines common functionality for WinRM Plugin API Server Sessions.
    /// </summary>
    internal abstract class WSManPluginServerSession : IDisposable
    {
        private readonly object _syncObject;

        protected bool isClosed;
        protected bool isContextReported;
        // used to keep track of last error..this will be used
        // for reporting operation complete to WSMan.
        protected Exception lastErrorReported;

        // request context passed by WSMan while creating a shell or command.
        internal WSManNativeApi.WSManPluginRequest creationRequestDetails;
        // request context passed by WSMan while sending Plugin data.
        internal WSManNativeApi.WSManPluginRequest sendRequestDetails;
        internal WSManPluginOperationShutdownContext shutDownContext;
        // tracker used in conjunction with WSMan API to identify a particular
        // shell context.
        internal RegisteredWaitHandle registeredShutDownWaitHandle;
        internal WSManPluginServerTransportManager transportMgr;
        internal int registeredShutdownNotification;

        // event that gets raised when session is closed.."source" will provide
        // IntPtr for "creationRequestDetails" which can be used to free
        // the context.
        internal event EventHandler<EventArgs> SessionClosed;

        // Track whether Dispose has been called.
        private bool _disposed = false;

        protected WSManPluginServerSession(
            WSManNativeApi.WSManPluginRequest creationRequestDetails,
            WSManPluginServerTransportManager transportMgr)
        {
            _syncObject = new object();
            this.creationRequestDetails = creationRequestDetails;
            this.transportMgr = transportMgr;

            transportMgr.PrepareCalled += this.HandlePrepareFromTransportManager;
            transportMgr.WSManTransportErrorOccured += this.HandleTransportError;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param> True when called from Dispose(), False when called from Finalize().
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    // Close(false);
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                Close(false);

                // Note disposing has been done.
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WSManPluginServerSession"/> class.
        /// </summary>
        ~WSManPluginServerSession()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        internal void SendOneItemToSession(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            string stream,
            WSManNativeApi.WSManData_UnToMan inboundData)
        {
            if ((!string.Equals(stream, WSManPluginConstants.SupportedInputStream, StringComparison.Ordinal)) &&
                (!string.Equals(stream, WSManPluginConstants.SupportedPromptResponseStream, StringComparison.Ordinal)))
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidInputStream,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidInputStream,
                        WSManPluginConstants.SupportedInputStream));
                return;
            }

            if (inboundData == null)
            {
                // no data is supplied..just ignore.
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NoError);
                return;
            }

            if (inboundData.Type != (uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_BINARY)
            {
                // only binary data is supported
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidInputDatatype,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidInputStream,
                        "WSMAN_DATA_TYPE_BINARY"));
                return;
            }

            lock (_syncObject)
            {
                if (isClosed)
                {
                    WSManPluginInstance.ReportWSManOperationComplete(requestDetails, lastErrorReported);
                    return;
                }
                // store the send request details..because the operation complete
                // may happen from a different thread.
                sendRequestDetails = requestDetails;
            }

            SendOneItemToSessionHelper(inboundData.Data, stream);

            // report operation complete.
            ReportSendOperationComplete();
        }

        internal void SendOneItemToSessionHelper(
            byte[] data,
            string stream)
        {
            transportMgr.ProcessRawData(data, stream);
        }

        internal bool EnableSessionToSendDataToClient(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            WSManNativeApi.WSManStreamIDSet_UnToMan streamSet,
            WSManPluginOperationShutdownContext ctxtToReport)
        {
            if (isClosed)
            {
                WSManPluginInstance.ReportWSManOperationComplete(requestDetails, lastErrorReported);
                return false;
            }

            if ((streamSet == null) ||
                (streamSet.streamIDsCount != 1))
            {
                // only "stdout" is the supported output stream.
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidOutputStream,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidOutputStream,
                        WSManPluginConstants.SupportedOutputStream));
                return false;
            }

            if (!string.Equals(streamSet.streamIDs[0], WSManPluginConstants.SupportedOutputStream, StringComparison.Ordinal))
            {
                // only "stdout" is the supported output stream.
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.InvalidOutputStream,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginInvalidOutputStream,
                        WSManPluginConstants.SupportedOutputStream));
                return false;
            }

            return transportMgr.EnableTransportManagerSendDataToClient(requestDetails, ctxtToReport);
        }

        /// <summary>
        /// Report session context to WSMan..this will let WSMan send ACK to
        /// client and client can send data.
        /// </summary>
        internal void ReportContext()
        {
            int result = 0;
            bool isRegisterWaitForSingleObjectFailed = false;

            lock (_syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                if (!isContextReported)
                {
                    isContextReported = true;
                    PSEtwLog.LogAnalyticInformational(PSEventId.ReportContext,
                        PSOpcode.Connect, PSTask.None,
                        PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                        creationRequestDetails.ToString(), creationRequestDetails.ToString());

                    // TO BE FIXED - As soon as this API is called, WinRM service will send CommandResponse back and Signal is expected anytime
                    // If Signal comes and executes before registering the notification handle, cleanup will be messed
                    result = WSManNativeApi.WSManPluginReportContext(creationRequestDetails.unmanagedHandle, 0, creationRequestDetails.unmanagedHandle);
                    if (Platform.IsWindows && (result == WSManPluginConstants.ExitCodeSuccess))
                    {
                        registeredShutdownNotification = 1;

                        // Wrap the provided handle so it can be passed to the registration function
                        SafeWaitHandle safeWaitHandle = new SafeWaitHandle(creationRequestDetails.shutdownNotificationHandle, false); // Owned by WinRM
                        EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                        eventWaitHandle.SafeWaitHandle = safeWaitHandle;

                        // Register shutdown notification handle
                        this.registeredShutDownWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                            eventWaitHandle,
                            new WaitOrTimerCallback(WSManPluginManagedEntryWrapper.PSPluginOperationShutdownCallback),
                            shutDownContext,
                            -1, // INFINITE
                            true); // TODO: Do I need to worry not being able to set missing WT_TRANSFER_IMPERSONATION?
                        if (this.registeredShutDownWaitHandle == null)
                        {
                            isRegisterWaitForSingleObjectFailed = true;
                            registeredShutdownNotification = 0;
                        }
                    }
                }
            }

            if ((result != WSManPluginConstants.ExitCodeSuccess) || (isRegisterWaitForSingleObjectFailed))
            {
                string errorMessage;
                if (isRegisterWaitForSingleObjectFailed)
                {
                    errorMessage = StringUtil.Format(RemotingErrorIdStrings.WSManPluginShutdownRegistrationFailed);
                }
                else
                {
                    errorMessage = StringUtil.Format(RemotingErrorIdStrings.WSManPluginReportContextFailed);
                }

                // Report error and close the session
                Exception mgdException = new InvalidOperationException(errorMessage);
                Close(mgdException);
            }
        }

        /// <summary>
        /// Added to provide derived classes with the ability to send event notifications.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected internal void SafeInvokeSessionClosed(object sender, EventArgs eventArgs)
        {
            SessionClosed.SafeInvoke(sender, eventArgs);
        }

        // handle transport manager related errors
        internal void HandleTransportError(object sender, TransportErrorOccuredEventArgs eventArgs)
        {
            Exception reasonForClose = null;
            if (eventArgs != null)
            {
                reasonForClose = eventArgs.Exception;
            }

            Close(reasonForClose);
        }

        // handle prepare from transport by reporting context to WSMan.
        internal void HandlePrepareFromTransportManager(object sender, EventArgs eventArgs)
        {
            ReportContext();
            ReportSendOperationComplete();
            transportMgr.PrepareCalled -= this.HandlePrepareFromTransportManager;
        }

        internal void Close(bool isShuttingDown)
        {
            if (Interlocked.Exchange(ref registeredShutdownNotification, 0) == 1)
            {
                // release the shutdown notification handle.
                if (registeredShutDownWaitHandle != null)
                {
                    registeredShutDownWaitHandle.Unregister(null);
                    registeredShutDownWaitHandle = null;
                }
            }

            // Delete the context only if isShuttingDown != true. isShuttingDown will
            // be true only when the method is called from RegisterWaitForSingleObject
            // handler..in which case the context will be freed from the callback.
            if (shutDownContext != null)
            {
                shutDownContext = null;
            }

            transportMgr.WSManTransportErrorOccured -= this.HandleTransportError;

            // We should not use request details again after so releasing the resource.
            // Remember not to free this memory as this memory is allocated and owned by WSMan.
            creationRequestDetails = null;
            // if already disposing..no need to let finalizer thread
            // put resources to clean this object.
            // System.GC.SuppressFinalize(this); // TODO: This is already called in Dispose().
        }

        // close current session and transport manager because of an exception
        internal void Close(Exception reasonForClose)
        {
            lastErrorReported = reasonForClose;
            WSManPluginOperationShutdownContext context = new WSManPluginOperationShutdownContext(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, false);
            CloseOperation(context, reasonForClose);
        }

        // Report Operation Complete using the send request details.
        internal void ReportSendOperationComplete()
        {
            lock (_syncObject)
            {
                if (sendRequestDetails != null)
                {
                    // report and clear the send request details
                    WSManPluginInstance.ReportWSManOperationComplete(sendRequestDetails, lastErrorReported);
                    sendRequestDetails = null;
                }
            }
        }

        #region Pure virtual methods

        internal abstract void CloseOperation(WSManPluginOperationShutdownContext context, Exception reasonForClose);

        internal abstract void ExecuteConnect(
            WSManNativeApi.WSManPluginRequest requestDetails, // in
            int flags, // in
            WSManNativeApi.WSManData_UnToMan inboundConnectInformation); // in optional

        #endregion
    }

    /// <summary>
    /// </summary>
    internal sealed class WSManPluginShellSession : WSManPluginServerSession
    {
        #region Private Members

        private readonly Dictionary<IntPtr, WSManPluginCommandSession> _activeCommandSessions;
        private readonly ServerRemoteSession _remoteSession;

        #endregion

        #region Internally Visible Members

        internal object shellSyncObject;

        #endregion

        #region Constructor
        internal WSManPluginShellSession(
            WSManNativeApi.WSManPluginRequest creationRequestDetails,
            WSManPluginServerTransportManager transportMgr,
            ServerRemoteSession remoteSession,
            WSManPluginOperationShutdownContext shutDownContext)
            : base(creationRequestDetails, transportMgr)
        {
            _remoteSession = remoteSession;
            _remoteSession.Closed +=
                new EventHandler<RemoteSessionStateMachineEventArgs>(this.HandleServerRemoteSessionClosed);

            _activeCommandSessions = new Dictionary<IntPtr, WSManPluginCommandSession>();
            this.shellSyncObject = new object();
            this.shutDownContext = shutDownContext;
        }
        #endregion

        /// <summary>
        /// Main Routine for Connect on a Shell.
        /// Calls in server remotesessions ExecuteConnect to run the Connect algorithm
        /// This call is synchronous. i.e WSManOperationComplete will be called before the routine completes.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="inboundConnectInformation"></param>
        internal override void ExecuteConnect(
            WSManNativeApi.WSManPluginRequest requestDetails, // in
            int flags, // in
            WSManNativeApi.WSManData_UnToMan inboundConnectInformation) // in optional
        {
            if (inboundConnectInformation == null)
            {
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.NullInvalidInput,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginNullInvalidInput,
                        "inboundConnectInformation",
                        "WSManPluginShellConnect"));
                return;
            }

            // not registering shutdown event as this is a synchronous operation.

            IntPtr responseXml = IntPtr.Zero;
            try
            {
                byte[] inputData;
                byte[] outputData;

                // Retrieve the string (Base64 encoded)
                inputData = ServerOperationHelpers.ExtractEncodedXmlElement(
                    inboundConnectInformation.Text,
                    WSManNativeApi.PS_CONNECT_XML_TAG);

                // this will raise exceptions on failure
                try
                {
                    _remoteSession.ExecuteConnect(inputData, out outputData);

                    // construct Xml to send back
                    string responseData = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "<{0} xmlns=\"{1}\">{2}</{0}>",
                        WSManNativeApi.PS_CONNECTRESPONSE_XML_TAG,
                        WSManNativeApi.PS_XML_NAMESPACE,
                        Convert.ToBase64String(outputData));

                    // TODO: currently using OperationComplete to report back the responseXml. This will need to change to use WSManReportObject
                    // that is currently internal.
                    WSManPluginInstance.ReportOperationComplete(requestDetails, WSManPluginErrorCodes.NoError, responseData);
                }
                catch (PSRemotingDataStructureException ex)
                {
                    WSManPluginInstance.ReportOperationComplete(requestDetails, WSManPluginErrorCodes.PluginConnectOperationFailed, ex.Message);
                }
            }
            catch (OutOfMemoryException)
            {
                WSManPluginInstance.ReportOperationComplete(requestDetails, WSManPluginErrorCodes.OutOfMemory);
            }
            finally
            {
                if (responseXml != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(responseXml);
                }
            }

            return;
        }

        // Create a new command in the shell context.
        internal void CreateCommand(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            string commandLine,
            WSManNativeApi.WSManCommandArgSet arguments)
        {
            try
            {
                // inbound cmd information is already verified.. so no need to verify here.
                WSManPluginCommandTransportManager serverCmdTransportMgr = new WSManPluginCommandTransportManager(transportMgr);
                serverCmdTransportMgr.Initialize();

                // Apply quota limits on the command transport manager
                _remoteSession.ApplyQuotaOnCommandTransportManager(serverCmdTransportMgr);

                WSManPluginCommandSession mgdCmdSession = new WSManPluginCommandSession(requestDetails, serverCmdTransportMgr, _remoteSession);
                AddToActiveCmdSessions(mgdCmdSession);
                mgdCmdSession.SessionClosed += this.HandleCommandSessionClosed;

                mgdCmdSession.shutDownContext = new WSManPluginOperationShutdownContext(
                    pluginContext,
                    creationRequestDetails.unmanagedHandle,
                    mgdCmdSession.creationRequestDetails.unmanagedHandle,
                    false);

                do
                {
                    if (!mgdCmdSession.ProcessArguments(arguments))
                    {
                        WSManPluginInstance.ReportOperationComplete(
                            requestDetails,
                            WSManPluginErrorCodes.InvalidArgSet,
                            StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginInvalidArgSet,
                                "WSManPluginCommand"));
                        break;
                    }

                    // Report plugin context to WSMan
                    mgdCmdSession.ReportContext();
                } while (false);
            }
            catch (System.Exception e)
            {
                // if there is an exception creating remote session send the message to client.
                WSManPluginInstance.ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ManagedException,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginManagedException,
                        e.Message));
            }
        }

        // Closes the command and clears associated resources
        internal void CloseCommandOperation(
            WSManPluginOperationShutdownContext context)
        {
            WSManPluginCommandSession mgdCmdSession = GetCommandSession(context.commandContext);
            if (mgdCmdSession == null)
            {
                // this should never be the case. this will protect the service.
                return;
            }

            // update the internal data store only if this is not receive operation.
            if (!context.isReceiveOperation)
            {
                DeleteFromActiveCmdSessions(mgdCmdSession.creationRequestDetails.unmanagedHandle);
            }

            mgdCmdSession.CloseOperation(context, null);
        }

        // adds command session to active command Sessions store and returns the id
        // at which the session is added.
        private void AddToActiveCmdSessions(
            WSManPluginCommandSession newCmdSession)
        {
            lock (shellSyncObject)
            {
                if (isClosed)
                {
                    return;
                }

                IntPtr key = newCmdSession.creationRequestDetails.unmanagedHandle;
                Dbg.Assert(key != IntPtr.Zero, "NULL handles should not be provided");

                if (!_activeCommandSessions.ContainsKey(key))
                {
                    _activeCommandSessions.Add(key, newCmdSession);
                    return;
                }
            }
        }

        private void DeleteFromActiveCmdSessions(
            IntPtr keyToDelete)
        {
            lock (shellSyncObject)
            {
                if (isClosed)
                {
                    return;
                }

                _activeCommandSessions.Remove(keyToDelete);
            }
        }

        // closes all the active command sessions.
        private void CloseAndClearCommandSessions(
            Exception reasonForClose)
        {
            Collection<WSManPluginCommandSession> copyCmdSessions = new Collection<WSManPluginCommandSession>();
            lock (shellSyncObject)
            {
                Dictionary<IntPtr, WSManPluginCommandSession>.Enumerator cmdEnumerator = _activeCommandSessions.GetEnumerator();
                while (cmdEnumerator.MoveNext())
                {
                    copyCmdSessions.Add(cmdEnumerator.Current.Value);
                }

                _activeCommandSessions.Clear();
            }

            // close the command sessions outside of the lock
            IEnumerator<WSManPluginCommandSession> cmdSessionEnumerator = copyCmdSessions.GetEnumerator();
            while (cmdSessionEnumerator.MoveNext())
            {
                WSManPluginCommandSession cmdSession = cmdSessionEnumerator.Current;
                // we are not interested in session closed events anymore as we are initiating the close
                // anyway/
                cmdSession.SessionClosed -= this.HandleCommandSessionClosed;
                cmdSession.Close(reasonForClose);
            }

            copyCmdSessions.Clear();
        }

        // returns the command session instance for a given command id.
        // null if not found.
        internal WSManPluginCommandSession GetCommandSession(
            IntPtr cmdContext)
        {
            lock (shellSyncObject)
            {
                WSManPluginCommandSession result = null;
                _activeCommandSessions.TryGetValue(cmdContext, out result);
                return result;
            }
        }

        private void HandleServerRemoteSessionClosed(
            object sender,
            RemoteSessionStateMachineEventArgs eventArgs)
        {
            Exception reasonForClose = null;
            if (eventArgs != null)
            {
                reasonForClose = eventArgs.Reason;
            }

            Close(reasonForClose);
        }

        private void HandleCommandSessionClosed(
            object source,
            EventArgs e)
        {
            // command context is passed as "source" parameter
            DeleteFromActiveCmdSessions((IntPtr)source);
        }

        internal override void CloseOperation(
            WSManPluginOperationShutdownContext context,
            Exception reasonForClose)
        {
            // let command sessions to close.
            lock (shellSyncObject)
            {
                if (isClosed)
                {
                    return;
                }

                if (!context.isReceiveOperation)
                {
                    isClosed = true;
                }
            }

            WSManPluginInstance.SetThreadProperties(creationRequestDetails);

            bool isRcvOpShuttingDown = (context.isShuttingDown) && (context.isReceiveOperation);
            bool isRcvOp = context.isReceiveOperation;
            bool isShuttingDown = context.isShuttingDown;

            // close the pending send operation if any
            ReportSendOperationComplete();
            // close the shell's transport manager after commands handled the operation
            transportMgr.DoClose(isRcvOpShuttingDown, reasonForClose);

            if (!isRcvOp)
            {
                // Initiate close on the active command sessions and then clear the internal
                // Command Session dictionary
                CloseAndClearCommandSessions(reasonForClose);
                // raise session closed event and let dependent code to release resources.
                // null check is not performed here because the handler will take care of this.
                base.SafeInvokeSessionClosed(creationRequestDetails.unmanagedHandle, EventArgs.Empty);
                // Send Operation Complete to WSMan service
                WSManPluginInstance.ReportWSManOperationComplete(creationRequestDetails, reasonForClose);
                // let base class release its resources
                base.Close(isShuttingDown);
            }
            // TODO: Do this.Dispose(); here?
        }
    }

    internal sealed class WSManPluginCommandSession : WSManPluginServerSession
    {
        #region Private Members

        private readonly ServerRemoteSession _remoteSession;

        #endregion

        #region Internally Visible Members

        internal object cmdSyncObject;

        #endregion

        #region Constructor

        internal WSManPluginCommandSession(
            WSManNativeApi.WSManPluginRequest creationRequestDetails,
            WSManPluginServerTransportManager transportMgr,
            ServerRemoteSession remoteSession)
            : base(creationRequestDetails, transportMgr)
        {
            _remoteSession = remoteSession;
            cmdSyncObject = new object();
        }

        #endregion

        internal bool ProcessArguments(
            WSManNativeApi.WSManCommandArgSet arguments)
        {
            if (arguments.argsCount != 1)
            {
                return false;
            }

            byte[] convertedBase64 = Convert.FromBase64String(arguments.args[0]);
            transportMgr.ProcessRawData(convertedBase64, WSManPluginConstants.SupportedInputStream);

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="requestDetails"></param>
        internal void Stop(
            WSManNativeApi.WSManPluginRequest requestDetails)
        {
            // stop the command..command will be stopped if we raise ClosingEvent on
            // transport manager.
            transportMgr.PerformStop();
            WSManPluginInstance.ReportWSManOperationComplete(requestDetails, null);
        }

        internal override void CloseOperation(
            WSManPluginOperationShutdownContext context,
            Exception reasonForClose)
        {
            // let command sessions to close.
            lock (cmdSyncObject)
            {
                if (isClosed)
                {
                    return;
                }

                if (!context.isReceiveOperation)
                {
                    isClosed = true;
                }
            }

            WSManPluginInstance.SetThreadProperties(creationRequestDetails);

            bool isRcvOp = context.isReceiveOperation;
            // only one thread will be here.
            bool isRcvOpShuttingDown = (context.isShuttingDown) &&
                (context.isReceiveOperation) &&
                (context.commandContext == creationRequestDetails.unmanagedHandle);

            bool isCmdShuttingDown = (context.isShuttingDown) &&
                (!context.isReceiveOperation) &&
                (context.commandContext == creationRequestDetails.unmanagedHandle);

            // close the pending send operation if any
            ReportSendOperationComplete();
            // close the shell's transport manager first..so we wont send data.
            transportMgr.DoClose(isRcvOpShuttingDown, reasonForClose);

            if (!isRcvOp)
            {
                // raise session closed event and let dependent code to release resources.
                // null check is not performed here because Managed C++ will take care of this.
                base.SafeInvokeSessionClosed(creationRequestDetails.unmanagedHandle, EventArgs.Empty);
                // Send Operation Complete to WSMan service
                WSManPluginInstance.ReportWSManOperationComplete(creationRequestDetails, reasonForClose);
                // let base class release its resources
                this.Close(isCmdShuttingDown);
            }
        }

        /// <summary>
        /// Main routine for connect on a command/pipeline.. Currently NO-OP
        /// will be enhanced later to support intelligent connect... like ending input streams on pipelines
        /// that are still waiting for input data.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="inboundConnectInformation"></param>
        internal override void ExecuteConnect(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            WSManNativeApi.WSManData_UnToMan inboundConnectInformation)
        {
            WSManPluginInstance.ReportOperationComplete(
                requestDetails,
                WSManPluginErrorCodes.NoError);
            return;
        }
    }
}
