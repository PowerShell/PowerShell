// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Implements ServerRemoteSessionDataStructureHandler.
    /// </summary>
    internal sealed class ClientRemoteSessionDSHandlerImpl : ClientRemoteSessionDataStructureHandler, IDisposable
    {
        [TraceSource("CRSDSHdlerImpl", "ClientRemoteSessionDSHandlerImpl")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("CRSDSHdlerImpl", "ClientRemoteSessionDSHandlerImpl");

        private const string resBaseName = "remotingerroridstrings";

        private readonly BaseClientSessionTransportManager _transportManager;
        private readonly ClientRemoteSessionDSHandlerStateMachine _stateMachine;
        private readonly ClientRemoteSession _session;
        private readonly RunspaceConnectionInfo _connectionInfo;
        // used for connection redirection.
        private Uri _redirectUri;
        private int _maxUriRedirectionCount;
        private bool _isCloseCalled;
        private readonly object _syncObject = new object();
        private readonly PSRemotingCryptoHelper _cryptoHelper;

        private readonly ClientRemoteSession.URIDirectionReported _uriRedirectionHandler;

        internal override BaseClientSessionTransportManager TransportManager
        {
            get
            {
                return _transportManager;
            }
        }

        internal override BaseClientCommandTransportManager CreateClientCommandTransportManager(
            System.Management.Automation.Runspaces.Internal.ClientRemotePowerShell cmd,
            bool noInput)
        {
            BaseClientCommandTransportManager cmdTransportMgr =
                _transportManager.CreateClientCommandTransportManager(_connectionInfo, cmd, noInput);
            // listen to data ready events.
            cmdTransportMgr.DataReceived += DispatchInputQueueData;

            return cmdTransportMgr;
        }

        #region constructors

        /// <summary>
        /// Creates an instance of ClientRemoteSessionDSHandlerImpl.
        /// </summary>
        internal ClientRemoteSessionDSHandlerImpl(ClientRemoteSession session,
            PSRemotingCryptoHelper cryptoHelper,
            RunspaceConnectionInfo connectionInfo,
            ClientRemoteSession.URIDirectionReported uriRedirectionHandler)
        {
            Dbg.Assert(_maxUriRedirectionCount >= 0, "maxUriRedirectionCount cannot be less than 0.");

            if (session == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(session));
            }

            _session = session;

            // Create state machine
            _stateMachine = new ClientRemoteSessionDSHandlerStateMachine();
            _stateMachine.StateChanged += HandleStateChanged;

            _connectionInfo = connectionInfo;

            // Create transport manager
            _cryptoHelper = cryptoHelper;
            _transportManager = _connectionInfo.CreateClientSessionTransportManager(
                _session.RemoteRunspacePoolInternal.InstanceId,
                _session.RemoteRunspacePoolInternal.Name,
                cryptoHelper);

            _transportManager.DataReceived += DispatchInputQueueData;
            _transportManager.WSManTransportErrorOccured += HandleTransportError;
            _transportManager.CloseCompleted += HandleCloseComplete;
            _transportManager.DisconnectCompleted += HandleDisconnectComplete;
            _transportManager.ReconnectCompleted += HandleReconnectComplete;

            _transportManager.RobustConnectionNotification += HandleRobustConnectionNotification;

            WSManConnectionInfo wsmanConnectionInfo = _connectionInfo as WSManConnectionInfo;
            if (wsmanConnectionInfo != null)
            {
                // only WSMan transport supports redirection

                // store the uri redirection handler and authmechanism
                // for uri redirection.
                _uriRedirectionHandler = uriRedirectionHandler;
                _maxUriRedirectionCount = wsmanConnectionInfo.MaximumConnectionRedirectionCount;
            }
        }

        #endregion constructors

        #region create

        /// <summary>
        /// Makes a create call asynchronously.
        /// </summary>
        internal override void CreateAsync()
        {
            // errors are reported through WSManTransportErrorOccured event on
            // the transport manager.
            _transportManager.CreateCompleted += HandleCreateComplete;
            _transportManager.CreateAsync();
        }

        /// <summary>
        /// This callback is called on complete of async connect call.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleCreateComplete(object sender, EventArgs args)
        {
            // This is a no-op at the moment..as we dont need to inform anything to
            // state machine here..StateMachine must already have reached NegotiationSent
            // state and waiting for Negotiation Received which will happen only from
            // DataReceived event.
        }

        private void HandleConnectComplete(object sender, EventArgs args)
        {
            // No-OP. Once the negotiation messages are exchanged and the session gets into established state,
            // it will take care of spawning the receive operation on the connected session
            // There is however a caveat.
            // A rouge remote server if it does not send the required negotiation data in the Connect Response,
            // then the state machine can never get into the established state and the runspace can never get into a opened state
            // Living with this for now.
        }

        #endregion create

        #region disconnect
        internal override void DisconnectAsync()
        {
            _transportManager.DisconnectAsync();
        }

        private void HandleDisconnectComplete(object sender, EventArgs args)
        {
            // Set statemachine event
            RemoteSessionStateMachineEventArgs disconnectCompletedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.DisconnectCompleted);
            StateMachine.RaiseEvent(disconnectCompletedArg);
        }
        #endregion disconnect

        #region RobustConnection events

        private void HandleRobustConnectionNotification(object sender, ConnectionStatusEventArgs e)
        {
            RemoteSessionStateMachineEventArgs eventArgument = null;
            switch (e.Notification)
            {
                case ConnectionStatus.AutoDisconnectStarting:
                    eventArgument = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.RCDisconnectStarted);
                    break;

                case ConnectionStatus.AutoDisconnectSucceeded:
                    eventArgument = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.DisconnectCompleted,
                        new RuntimeException(
                            StringUtil.Format(RemotingErrorIdStrings.RCAutoDisconnectingError,
                                _session.RemoteRunspacePoolInternal.ConnectionInfo.ComputerName)));
                    break;

                case ConnectionStatus.InternalErrorAbort:
                    eventArgument = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.FatalError);
                    break;
            }

            if (eventArgument != null)
            {
                StateMachine.RaiseEvent(eventArgument);
            }
        }

        #endregion

        #region reconnect
        internal override void ReconnectAsync()
        {
            _transportManager.ReconnectAsync();
        }

        private void HandleReconnectComplete(object sender, EventArgs args)
        {
            // Set statemachine event
            RemoteSessionStateMachineEventArgs reconnectCompletedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.ReconnectCompleted);
            StateMachine.RaiseEvent(reconnectCompletedArg);
        }
        #endregion reconnect

        #region close

        /// <summary>
        /// Close the connection asynchronously.
        /// </summary>
        internal override void CloseConnectionAsync()
        {
            lock (_syncObject)
            {
                if (_isCloseCalled)
                {
                    return;
                }

                _transportManager.CloseAsync();
                _isCloseCalled = true;
            }
        }

        private void HandleCloseComplete(object sender, EventArgs args)
        {
            // This event gets raised only when the connection is closed successfully.

            RemoteSessionStateMachineEventArgs closeCompletedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.CloseCompleted);
            _stateMachine.RaiseEvent(closeCompletedArg);
        }

        #endregion close

        #region negotiation

        /// <summary>
        /// Sends the negotiation package asynchronously.
        /// </summary>
        internal override void SendNegotiationAsync(RemoteSessionState sessionState)
        {
            // This state change is made before the call to CreateAsync to ensure the state machine
            // is prepared for a NegotiationReceived response.  Otherwise a race condition can
            // occur when the transport NegotiationReceived arrives too soon, breaking the session.
            // This race condition was observed for OutOfProc transport when reusing the OutOfProc process.
            // this will change StateMachine to NegotiationSent.
            RemoteSessionStateMachineEventArgs negotiationSendCompletedArg =
                new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSendCompleted);
            _stateMachine.RaiseEvent(negotiationSendCompletedArg);

            if (sessionState == RemoteSessionState.NegotiationSending)
            {
                _transportManager.CreateAsync();
            }
            else if (sessionState == RemoteSessionState.NegotiationSendingOnConnect)
            {
                _transportManager.ConnectCompleted += HandleConnectComplete;
                _transportManager.ConnectAsync();
            }
            else
            {
                Dbg.Assert(false, "SendNegotiationAsync called in unexpected session state");
            }
        }

        internal override event EventHandler<RemoteSessionNegotiationEventArgs> NegotiationReceived;

        #endregion negotiation

        #region state change

        /// <summary>
        /// This event indicates that the connection state has changed.
        /// </summary>
        internal override event EventHandler<RemoteSessionStateEventArgs> ConnectionStateChanged;

        private void HandleStateChanged(object sender, RemoteSessionStateEventArgs arg)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arg));
            }

            // Enqueue session related negotiation packets first
            if ((arg.SessionStateInfo.State == RemoteSessionState.NegotiationSending) || (arg.SessionStateInfo.State == RemoteSessionState.NegotiationSendingOnConnect))
            {
                HandleNegotiationSendingStateChange();
            }

            // this will enable top-layers to enqueue any packets during NegotiationSending and
            // during other states.
            ConnectionStateChanged.SafeInvoke(this, arg);

            if ((arg.SessionStateInfo.State == RemoteSessionState.NegotiationSending) || (arg.SessionStateInfo.State == RemoteSessionState.NegotiationSendingOnConnect))
            {
                SendNegotiationAsync(arg.SessionStateInfo.State);
            }

            // once session is established.. start receiving data (if not already done and only apples to wsmanclientsessionTM)
            if (arg.SessionStateInfo.State == RemoteSessionState.Established)
            {
                WSManClientSessionTransportManager tm = _transportManager as WSManClientSessionTransportManager;
                if (tm != null)
                {
                    tm.AdjustForProtocolVariations(_session.ServerProtocolVersion);
                    tm.StartReceivingData();
                }
            }

            // Close the transport manager only after powershell's close their transports
            // Powershell's close their transport using the ConnectionStateChanged event notification.
            if (arg.SessionStateInfo.State == RemoteSessionState.ClosingConnection)
            {
                CloseConnectionAsync();
            }

            // process disconnect
            if (arg.SessionStateInfo.State == RemoteSessionState.Disconnecting)
            {
                DisconnectAsync();
            }

            // process reconnect
            if (arg.SessionStateInfo.State == RemoteSessionState.Reconnecting)
            {
                ReconnectAsync();
            }
        }

        /// <summary>
        /// Clubbing negotiation packet + runspace creation and then doing transportManager.ConnectAsync().
        /// This will save us 2 network calls by doing all the work in one network call.
        /// </summary>
        private void HandleNegotiationSendingStateChange()
        {
            RemoteSessionCapability clientCapability = _session.Context.ClientCapability;
            Dbg.Assert(clientCapability.RemotingDestination == RemotingDestination.Server, "Expected clientCapability.RemotingDestination == RemotingDestination.Server");

            // Encode and send the negotiation reply
            RemoteDataObject data = RemotingEncoder.GenerateClientSessionCapability(
                                        clientCapability, _session.RemoteRunspacePoolInternal.InstanceId);
            RemoteDataObject<PSObject> dataAsPSObject = RemoteDataObject<PSObject>.CreateFrom(
                data.Destination, data.DataType, data.RunspacePoolId, data.PowerShellId, (PSObject)data.Data);
            _transportManager.DataToBeSentCollection.Add<PSObject>(dataAsPSObject);
        }

        #endregion state change

        internal override ClientRemoteSessionDSHandlerStateMachine StateMachine
        {
            get
            {
                return _stateMachine;
            }
        }

        #region URI Redirection

        /// <summary>
        /// Transport reported an error saying that uri is redirected. This method
        /// will perform the redirection to the new URI by doing the following:
        /// 1. Close the current transport manager to clean resources
        /// 2. Raise a warning that URI is getting redirected.
        /// 3. Using the new URI, ask the same transport manager to redirect
        /// Step 1 is performed here. Step2-3 is performed in another method.
        /// </summary>
        /// <param name="newURIString"></param>
        /// <exception cref="ArgumentNullException">
        /// newURIString is a null reference.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// uriString is empty.
        /// The scheme specified in uriString is invalid.
        /// uriString contains too many slashes.
        /// The password specified in uriString is invalid.
        /// The host name specified in uriString is invalid.
        /// </exception>
        private void PerformURIRedirection(string newURIString)
        {
            _redirectUri = new Uri(newURIString);

            // make sure connection is not closed while we are handling the redirection.
            lock (_syncObject)
            {
                // if connection is closed by the user..no need to redirect
                if (_isCloseCalled)
                {
                    return;
                }

                // clear our current close complete & Error handlers
                _transportManager.CloseCompleted -= HandleCloseComplete;
                _transportManager.WSManTransportErrorOccured -= HandleTransportError;

                // perform other steps only after transport manager is closed.
                _transportManager.CloseCompleted += HandleTransportCloseCompleteForRedirection;
                // Handle errors happened while redirecting differently..We need to reset the
                // original handlers in this case.
                _transportManager.WSManTransportErrorOccured += HandleTransportErrorForRedirection;

                _transportManager.PrepareForRedirection();
            }
        }

        private void HandleTransportCloseCompleteForRedirection(object source, EventArgs args)
        {
            _transportManager.CloseCompleted -= HandleTransportCloseCompleteForRedirection;
            _transportManager.WSManTransportErrorOccured -= HandleTransportErrorForRedirection;

            // reattach the close complete and error handlers
            _transportManager.CloseCompleted += HandleCloseComplete;
            _transportManager.WSManTransportErrorOccured += HandleTransportError;

            PerformURIRedirectionStep2(_redirectUri);
        }

        private void HandleTransportErrorForRedirection(object sender, TransportErrorOccuredEventArgs e)
        {
            _transportManager.CloseCompleted -= HandleTransportCloseCompleteForRedirection;
            _transportManager.WSManTransportErrorOccured -= HandleTransportErrorForRedirection;

            // reattach the close complete and error handlers
            _transportManager.CloseCompleted += HandleCloseComplete;
            _transportManager.WSManTransportErrorOccured += HandleTransportError;

            HandleTransportError(sender, e);
        }

        /// <summary>
        /// This is step 2 of URI redirection. This is called after the current transport manager
        /// is closed. This is usually called from the close complete callback.
        /// </summary>
        /// <param name="newURI"></param>
        private void PerformURIRedirectionStep2(System.Uri newURI)
        {
            Dbg.Assert(newURI != null, "Uri cannot be null");
            lock (_syncObject)
            {
                // if connection is closed by the user..no need to redirect
                if (_isCloseCalled)
                {
                    return;
                }

                // raise warning to report the redirection
                _uriRedirectionHandler?.Invoke(newURI);

                // start a new connection
                _transportManager.Redirect(newURI, _connectionInfo);
            }
        }

        #endregion

        #region data handling

        /// <summary>
        /// Handler which handles transport errors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void HandleTransportError(object sender, TransportErrorOccuredEventArgs e)
        {
            Dbg.Assert(e != null, "HandleTransportError expects non-null eventargs");
            // handle uri redirections
            PSRemotingTransportRedirectException redirectException = e.Exception as PSRemotingTransportRedirectException;
            if ((redirectException != null) && (_maxUriRedirectionCount > 0))
            {
                Exception exception = null;

                try
                {
                    // honor max redirection count given by the user.
                    _maxUriRedirectionCount--;
                    PerformURIRedirection(redirectException.RedirectLocation);
                    return;
                }
                catch (ArgumentNullException argumentException)
                {
                    exception = argumentException;
                }
                catch (UriFormatException uriFormatException)
                {
                    exception = uriFormatException;
                }
                // if we are here, there must be an exception constructing a uri
                if (exception != null)
                {
                    PSRemotingTransportException newException =
                        new PSRemotingTransportException(PSRemotingErrorId.RedirectedURINotWellFormatted, RemotingErrorIdStrings.RedirectedURINotWellFormatted,
                            _session.Context.RemoteAddress.OriginalString,
                            redirectException.RedirectLocation);
                    newException.TransportMessage = e.Exception.TransportMessage;
                    e.Exception = newException;
                }
            }

            RemoteSessionEvent sessionEvent = RemoteSessionEvent.ConnectFailed;

            switch (e.ReportingTransportMethod)
            {
                case TransportMethodEnum.CreateShellEx:
                    sessionEvent = RemoteSessionEvent.ConnectFailed;
                    break;
                case TransportMethodEnum.SendShellInputEx:
                case TransportMethodEnum.CommandInputEx:
                    sessionEvent = RemoteSessionEvent.SendFailed;
                    break;
                case TransportMethodEnum.ReceiveShellOutputEx:
                case TransportMethodEnum.ReceiveCommandOutputEx:
                    sessionEvent = RemoteSessionEvent.ReceiveFailed;
                    break;
                case TransportMethodEnum.CloseShellOperationEx:
                    sessionEvent = RemoteSessionEvent.CloseFailed;
                    break;
                case TransportMethodEnum.DisconnectShellEx:
                    sessionEvent = RemoteSessionEvent.DisconnectFailed;
                    break;
                case TransportMethodEnum.ReconnectShellEx:
                    sessionEvent = RemoteSessionEvent.ReconnectFailed;
                    break;
            }

            RemoteSessionStateMachineEventArgs errorArgs =
                new RemoteSessionStateMachineEventArgs(sessionEvent, e.Exception);
            _stateMachine.RaiseEvent(errorArgs);
        }

        /// <summary>
        /// Dispatches data when it arrives from the input queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dataArg">
        /// arg which contains the data received from input queue
        /// </param>
        internal void DispatchInputQueueData(object sender, RemoteDataEventArgs dataArg)
        {
            if (dataArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataArg));
            }

            RemoteDataObject<PSObject> rcvdData = dataArg.ReceivedData;

            if (rcvdData == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(dataArg));
            }

            RemotingDestination destination = rcvdData.Destination;

            if ((destination & RemotingDestination.Client) != RemotingDestination.Client)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.RemotingDestinationNotForMe, RemotingDestination.Client, destination);
            }

            RemotingTargetInterface targetInterface = rcvdData.TargetInterface;
            switch (targetInterface)
            {
                case RemotingTargetInterface.Session:
                    {
                        // Messages for session can cause statemachine state to change.
                        // These messages are first processed by Sessiondata structure handler and depending
                        // on the type of message, appropriate event is raised in state machine
                        ProcessSessionMessages(dataArg);
                        break;
                    }

                case RemotingTargetInterface.RunspacePool:
                case RemotingTargetInterface.PowerShell:
                    // Non Session messages do not change the state of the statemachine.
                    // However instead of forwarding them to Runspace/pipeline here, an
                    // event is raised in state machine which verified that state is
                    // suitable for accepting these messages. if state is suitable statemachine
                    // will call DoMessageForwading which will forward the messages appropriately
                    RemoteSessionStateMachineEventArgs msgRcvArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.MessageReceived, null);
                    if (StateMachine.CanByPassRaiseEvent(msgRcvArg))
                    {
                        ProcessNonSessionMessages(dataArg.ReceivedData);
                    }
                    else
                    {
                        StateMachine.RaiseEvent(msgRcvArg);
                    }

                    break;
                default:
                    {
                        Dbg.Assert(false, "we should not be encountering this");
                    }

                    break;
            }
        }

        // TODO: If this is not used remove this
        // internal override event EventHandler<RemoteDataEventArgs> DataReceived;

        /// <summary>
        /// This processes the object received from transport which are
        /// targeted for session.
        /// </summary>
        /// <param name="arg">
        /// argument contains the data object
        /// </param>
        private void ProcessSessionMessages(RemoteDataEventArgs arg)
        {
            if (arg == null || arg.ReceivedData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arg));
            }

            RemoteDataObject<PSObject> rcvdData = arg.ReceivedData;

            RemotingTargetInterface targetInterface = rcvdData.TargetInterface;
            Dbg.Assert(targetInterface == RemotingTargetInterface.Session, "targetInterface must be Session");

            RemotingDataType dataType = rcvdData.DataType;

            switch (dataType)
            {
                case RemotingDataType.CloseSession:
                    PSRemotingDataStructureException reasonOfClose = new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerRequestedToCloseSession);
                    RemoteSessionStateMachineEventArgs closeSessionArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close, reasonOfClose);
                    _stateMachine.RaiseEvent(closeSessionArg);
                    break;

                case RemotingDataType.SessionCapability:
                    RemoteSessionCapability capability = null;
                    try
                    {
                        capability = RemotingDecoder.GetSessionCapability(rcvdData.Data);
                    }
                    catch (PSRemotingDataStructureException dse)
                    {
                        // this will happen if expected properties are not
                        // received for session capability
                        throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientNotFoundCapabilityProperties,
                            dse.Message, PSVersionInfo.GitCommitId, RemotingConstants.ProtocolVersion);
                    }

                    RemoteSessionStateMachineEventArgs capabilityArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationReceived);
                    capabilityArg.RemoteSessionCapability = capability;
                    _stateMachine.RaiseEvent(capabilityArg);

                    RemoteSessionNegotiationEventArgs negotiationArg = new RemoteSessionNegotiationEventArgs(capability);
                    NegotiationReceived.SafeInvoke(this, negotiationArg);
                    break;

                case RemotingDataType.EncryptedSessionKey:
                    {
                        string encryptedSessionKey = RemotingDecoder.GetEncryptedSessionKey(rcvdData.Data);
                        EncryptedSessionKeyReceived.SafeInvoke(this, new RemoteDataEventArgs<string>(encryptedSessionKey));
                    }

                    break;

                case RemotingDataType.PublicKeyRequest:
                    {
                        PublicKeyRequestReceived.SafeInvoke(this, new RemoteDataEventArgs<string>(string.Empty));
                    }

                    break;

                default:
                    {
                        throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ReceivedUnsupportedAction, dataType);
                    }
            }
        }

        /// <summary>
        /// This processes the object received from transport which are
        /// not targeted for session.
        /// </summary>
        /// <param name="rcvdData">
        /// received data.
        /// </param>
        internal void ProcessNonSessionMessages(RemoteDataObject<PSObject> rcvdData)
        {
            // TODO: Consider changing to Dbg.Assert()
            if (rcvdData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(rcvdData));
            }

            RemotingTargetInterface targetInterface = rcvdData.TargetInterface;

            Guid clientRunspacePoolId;
            RemoteRunspacePoolInternal runspacePool;

            switch (targetInterface)
            {
                case RemotingTargetInterface.Session:

                    Dbg.Assert(false,
                        "The session remote data is handled my session data structure handler, not here");
                    break;

                case RemotingTargetInterface.RunspacePool:
                    clientRunspacePoolId = rcvdData.RunspacePoolId;
                    runspacePool = _session.GetRunspacePool(clientRunspacePoolId);

                    if (runspacePool != null)
                    {
                        // GETBACK
                        runspacePool.DataStructureHandler.ProcessReceivedData(rcvdData);
                    }
                    else
                    {
                        // The runspace pool may have been removed on the client side,
                        // so, we should just ignore the message.
                        s_trace.WriteLine(@"Client received data for Runspace (id: {0}),
                            but the Runspace cannot be found", clientRunspacePoolId);
                    }

                    break;

                case RemotingTargetInterface.PowerShell:
                    clientRunspacePoolId = rcvdData.RunspacePoolId;
                    runspacePool = _session.GetRunspacePool(clientRunspacePoolId);

                    // GETBACK
                    runspacePool.DataStructureHandler.DispatchMessageToPowerShell(rcvdData);
                    break;

                default:

                    break;
            }
        }

        #endregion data handling

        #region IDisposable

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            _transportManager.Dispose();
        }

        #endregion IDisposable

        #region Key Exchange

        internal override event EventHandler<RemoteDataEventArgs<string>> EncryptedSessionKeyReceived;

        internal override event EventHandler<RemoteDataEventArgs<string>> PublicKeyRequestReceived;
        /// <summary>
        /// Send the specified local public key to the remote end.
        /// </summary>
        /// <param name="localPublicKey">Local public key as a string.</param>
        internal override void SendPublicKeyAsync(string localPublicKey)
        {
            _transportManager.DataToBeSentCollection.Add<object>(
                RemotingEncoder.GenerateMyPublicKey(_session.RemoteRunspacePoolInternal.InstanceId,
                    localPublicKey, RemotingDestination.Server));
        }

        /// <summary>
        /// Raise the public key received event.
        /// </summary>
        /// <param name="receivedData">Received data.</param>
        /// <remarks>This method is a hook to be called
        /// from the transport manager</remarks>
        internal override void RaiseKeyExchangeMessageReceived(RemoteDataObject<PSObject> receivedData)
        {
            ProcessSessionMessages(new RemoteDataEventArgs(receivedData));
        }

        #endregion Key Exchange
    }
}
