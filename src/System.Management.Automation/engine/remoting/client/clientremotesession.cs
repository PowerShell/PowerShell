// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class is designed to contains the pertinent information about a Remote Connection,
    /// such as remote computer name, remote user name etc.
    /// It is also used to access remote connection capability and configuration information.
    /// Currently the session is identified by the InstanceId of the runspacePool associated with it
    /// This can change in future if we start supporting multiple runspacePools per session.
    /// </summary>
    internal class ClientRemoteSessionContext
    {
        #region properties

        /// <summary>
        /// Remote computer address in URI format.
        /// </summary>
        internal Uri RemoteAddress { get; set; }

        /// <summary>
        /// User credential to be used on the remote computer.
        /// </summary>
        internal PSCredential UserCredential { get; set; }

        /// <summary>
        /// Capability information for the client side.
        /// </summary>
        internal RemoteSessionCapability ClientCapability { get; set; }

        /// <summary>
        /// Capability information received from the server side.
        /// </summary>
        internal RemoteSessionCapability ServerCapability { get; set; }

        /// <summary>
        /// This is the shellName which identifies the PowerShell configuration to launch
        /// on remote machine.
        /// </summary>
        internal string ShellName { get; set; }

        #endregion Public_Properties
    }

    /// <summary>
    /// This abstract class defines the client view of the remote connection.
    /// </summary>
    internal abstract class ClientRemoteSession : RemoteSession
    {
        [TraceSourceAttribute("CRSession", "ClientRemoteSession")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("CRSession", "ClientRemoteSession");

        #region Public_Method_API

        /// <summary>
        /// Client side user calls this function to create a new remote session.
        /// User needs to register event handler to ConnectionEstablished and ConnectionClosed to
        /// monitor the actual connection state.
        /// </summary>
        public abstract void CreateAsync();

        /// <summary>
        /// This event handler is raised when the state of session changes.
        /// </summary>
        public abstract event EventHandler<RemoteSessionStateEventArgs> StateChanged;

        /// <summary>
        /// Close the connection to the remote computer in an asynchronous manner.
        /// Client side user can register an event handler with ConnectionClosed to monitor
        /// the connection state.
        /// </summary>
        public abstract void CloseAsync();

        /// <summary>
        /// Disconnects the remote session in an asynchronous manner.
        /// </summary>
        public abstract void DisconnectAsync();

        /// <summary>
        /// Reconnects the remote session in an asynchronous manner.
        /// </summary>
        public abstract void ReconnectAsync();

        /// <summary>
        /// Connects to an existing remote session
        /// User needs to register event handler to ConnectionEstablished and ConnectionClosed to
        /// monitor the actual connection state.
        /// </summary>
        public abstract void ConnectAsync();

        #endregion Public_Method_API

        #region Public_Properties

        internal ClientRemoteSessionContext Context { get; } = new ClientRemoteSessionContext();

        #endregion Public_Properties

        #region URI Redirection

        /// <summary>
        /// Delegate used to report connection URI redirections to the application.
        /// </summary>
        /// <param name="newURI">
        /// New URI to which the connection is being redirected to.
        /// </param>
        internal delegate void URIDirectionReported(Uri newURI);

        #endregion

        /// <summary>
        /// ServerRemoteSessionDataStructureHandler instance for this session.
        /// </summary>
        internal ClientRemoteSessionDataStructureHandler SessionDataStructureHandler { get; set; }

        protected Version _serverProtocolVersion;
        /// <summary>
        /// Protocol version negotiated by the server.
        /// </summary>
        internal Version ServerProtocolVersion
        {
            get
            {
                return _serverProtocolVersion;
            }
        }

        private RemoteRunspacePoolInternal _remoteRunspacePool;

        /// <summary>
        /// Remote runspace pool if used, for this session.
        /// </summary>
        internal RemoteRunspacePoolInternal RemoteRunspacePoolInternal
        {
            get
            {
                return _remoteRunspacePool;
            }

            set
            {
                Dbg.Assert(_remoteRunspacePool == null, @"RunspacePool should be
                        attached only once to the session");
                _remoteRunspacePool = value;
            }
        }

        /// <summary>
        /// Get the runspace pool with the matching id.
        /// </summary>
        /// <param name="clientRunspacePoolId">
        /// Id of the runspace to get
        /// </param>
        /// <returns></returns>
        internal RemoteRunspacePoolInternal GetRunspacePool(Guid clientRunspacePoolId)
        {
            if (_remoteRunspacePool != null)
            {
                if (_remoteRunspacePool.InstanceId.Equals(clientRunspacePoolId))
                    return _remoteRunspacePool;
            }

            return null;
        }
    }

    /// <summary>
    /// Remote Session Implementation.
    /// </summary>
    internal class ClientRemoteSessionImpl : ClientRemoteSession, IDisposable
    {
        [TraceSourceAttribute("CRSessionImpl", "ClientRemoteSessionImpl")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("CRSessionImpl", "ClientRemoteSessionImpl");

        private PSRemotingCryptoHelperClient _cryptoHelper = null;

        #region Constructors

        /// <summary>
        /// Creates a new instance of ClientRemoteSessionImpl.
        /// </summary>
        /// <param name="rsPool">
        /// The RunspacePool object this session should map to.
        /// </param>
        /// <param name="uriRedirectionHandler">
        /// </param>
        internal ClientRemoteSessionImpl(RemoteRunspacePoolInternal rsPool,
                                       URIDirectionReported uriRedirectionHandler)
        {
            Dbg.Assert(rsPool != null, "RunspacePool cannot be null");
            base.RemoteRunspacePoolInternal = rsPool;
            Context.RemoteAddress = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<Uri>(rsPool.ConnectionInfo,
                "ConnectionUri", null);
            _cryptoHelper = new PSRemotingCryptoHelperClient();
            _cryptoHelper.Session = this;
            Context.ClientCapability = RemoteSessionCapability.CreateClientCapability();
            Context.UserCredential = rsPool.ConnectionInfo.Credential;

            // shellName validation is not performed on the client side.
            // This is recommended by the WinRS team: for the reason that the rules may change in the future.
            Context.ShellName = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<string>(rsPool.ConnectionInfo,
                "ShellUri", string.Empty);

            MySelf = RemotingDestination.Client;
            // Create session data structure handler for this session
            SessionDataStructureHandler = new ClientRemoteSessionDSHandlerImpl(this,
                _cryptoHelper,
                rsPool.ConnectionInfo,
                uriRedirectionHandler);
            BaseSessionDataStructureHandler = SessionDataStructureHandler;
            _waitHandleForConfigurationReceived = new ManualResetEvent(false);

            // Register handlers for various ClientSessiondata structure handler events
            SessionDataStructureHandler.NegotiationReceived += HandleNegotiationReceived;
            SessionDataStructureHandler.ConnectionStateChanged += HandleConnectionStateChanged;
            SessionDataStructureHandler.EncryptedSessionKeyReceived += HandleEncryptedSessionKeyReceived;
            SessionDataStructureHandler.PublicKeyRequestReceived += HandlePublicKeyRequestReceived;
        }

        #endregion Constructors

        #region connect/close

        /// <summary>
        /// Creates a Remote Session Asynchronously.
        /// </summary>
        public override void CreateAsync()
        {
            // Raise a CreateSession event in StateMachine. This start the process of connection and negotiation to a new remote session
            RemoteSessionStateMachineEventArgs startArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.CreateSession);
            SessionDataStructureHandler.StateMachine.RaiseEvent(startArg);
        }

        /// <summary>
        /// Connects to a existing Remote Session Asynchronously by executing a Connect negotiation algorithm.
        /// </summary>
        public override void ConnectAsync()
        {
            // Raise the connectsession event in statemachine. This start the process of connection and negotiation to an existing remote session
            RemoteSessionStateMachineEventArgs startArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.ConnectSession);
            SessionDataStructureHandler.StateMachine.RaiseEvent(startArg);
        }

        /// <summary>
        /// Closes Session Connection Asynchronously.
        /// </summary>
        /// <remarks>
        /// Caller should register for ConnectionClosed event to get notified
        /// </remarks>
        public override void CloseAsync()
        {
            RemoteSessionStateMachineEventArgs closeArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);
            SessionDataStructureHandler.StateMachine.RaiseEvent(closeArg);
        }

        /// <summary>
        /// Temporarily suspends connection to a connected remote session.
        /// </summary>
        public override void DisconnectAsync()
        {
            RemoteSessionStateMachineEventArgs startDisconnectArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.DisconnectStart);
            SessionDataStructureHandler.StateMachine.RaiseEvent(startDisconnectArg);
        }

        /// <summary>
        /// Restores connection to a disconnected remote session. Negotiation has already been performed before.
        /// </summary>
        public override void ReconnectAsync()
        {
            RemoteSessionStateMachineEventArgs startReconnectArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.ReconnectStart);
            SessionDataStructureHandler.StateMachine.RaiseEvent(startReconnectArg);
        }

        /// <summary>
        /// This event handler is raised when the state of session changes.
        /// </summary>
        public override event EventHandler<RemoteSessionStateEventArgs> StateChanged;

        /// <summary>
        /// Handles changes in data structure handler state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg">
        /// Event argument which contains the new state
        /// </param>
        private void HandleConnectionStateChanged(object sender, RemoteSessionStateEventArgs arg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (arg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(arg));
                }

                if (arg.SessionStateInfo.State == RemoteSessionState.EstablishedAndKeyReceived) // TODO - Client session would never get into this state... to be removed
                {
                    // send the public key
                    StartKeyExchange();
                }

                if (arg.SessionStateInfo.State == RemoteSessionState.ClosingConnection)
                {
                    // when the connection is being closed we need to
                    // complete the key exchange process to release
                    // the lock under which the key exchange is happening
                    // if we fail to release the lock, then when
                    // transport manager is closing it will try to
                    // acquire the lock again leading to a deadlock
                    CompleteKeyExchange();
                }

                StateChanged.SafeInvoke(this, arg);
            }
        }

        #endregion connect/closed

        #region KeyExchange

        /// <summary>
        /// Start the key exchange process.
        /// </summary>
        internal override void StartKeyExchange()
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established ||
                SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyRequested)
            {
                // Start the key sending process
                string localPublicKey = null;
                bool ret = false;
                RemoteSessionStateMachineEventArgs eventArgs = null;
                Exception exception = null;

                try
                {
                    ret = _cryptoHelper.ExportLocalPublicKey(out localPublicKey);
                }
                catch (PSCryptoException cryptoException)
                {
                    ret = false;
                    exception = cryptoException;
                }

                if (!ret)
                {
                    // we need to complete the key exchange
                    // since the crypto helper will be waiting on it
                    CompleteKeyExchange();

                    // exporting local public key failed
                    // set state to Closed
                    eventArgs = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeySendFailed,
                                        exception);

                    SessionDataStructureHandler.StateMachine.RaiseEvent(eventArgs);
                }
                else
                {
                    // send using data structure handler
                    eventArgs = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeySent);
                    SessionDataStructureHandler.StateMachine.RaiseEvent(eventArgs);

                    SessionDataStructureHandler.SendPublicKeyAsync(localPublicKey);
                }
            }
        }

        /// <summary>
        /// Complete the key exchange process.
        /// </summary>
        internal override void CompleteKeyExchange()
        {
            _cryptoHelper.CompleteKeyExchange();
        }

        /// <summary>
        /// Handles an encrypted session key received from the other side.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">arguments that contain the remote
        /// public key</param>
        private void HandleEncryptedSessionKeyReceived(object sender, RemoteDataEventArgs<string> eventArgs)
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeySent)
            {
                string encryptedSessionKey = eventArgs.Data;

                bool ret = _cryptoHelper.ImportEncryptedSessionKey(encryptedSessionKey);

                RemoteSessionStateMachineEventArgs args = null;
                if (!ret)
                {
                    // importing remote public key failed
                    // set state to closed
                    args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceiveFailed);

                    SessionDataStructureHandler.StateMachine.RaiseEvent(args);
                }

                // complete the key exchange process
                CompleteKeyExchange();

                args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceived);
                SessionDataStructureHandler.StateMachine.RaiseEvent(args);
            }
        }

        /// <summary>
        /// Handles a request for public key from the server.
        /// </summary>
        /// <param name="sender">Send of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event, unused.</param>
        private void HandlePublicKeyRequestReceived(object sender, RemoteDataEventArgs<string> eventArgs)
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established)
            {
                RemoteSessionStateMachineEventArgs args =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyRequested);
                SessionDataStructureHandler.StateMachine.RaiseEvent(args);

                StartKeyExchange();
            }
        }

        #endregion KeyExchange

        // TODO:Review Configuration Story
        #region configuration

        private ManualResetEvent _waitHandleForConfigurationReceived;

        #endregion configuration

        #region negotiation

        /// <summary>
        /// Examines the negotiation packet received from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void HandleNegotiationReceived(object sender, RemoteSessionNegotiationEventArgs arg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (arg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(arg));
                }

                if (arg.RemoteSessionCapability == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(arg));
                }

                Context.ServerCapability = arg.RemoteSessionCapability;

                try
                {
                    // This will throw if there is an error running the algorithm
                    RunClientNegotiationAlgorithm(Context.ServerCapability);

                    RemoteSessionStateMachineEventArgs negotiationCompletedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationCompleted);
                    SessionDataStructureHandler.StateMachine.RaiseEvent(negotiationCompletedArg);
                }
                catch (PSRemotingDataStructureException dse)
                {
                    RemoteSessionStateMachineEventArgs negotiationFailedArg =
                        new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationFailed,
                            dse);
                    SessionDataStructureHandler.StateMachine.RaiseEvent(negotiationFailedArg);
                }
            }
        }

        /// <summary>
        /// Verifies the negotiation packet received from the server.
        /// </summary>
        /// <param name="serverRemoteSessionCapability">
        /// Capabilities of remote session
        /// </param>
        /// <returns>
        /// The method returns true if the capability negotiation is successful.
        /// Otherwise, it returns false.
        /// </returns>
        /// <exception cref="PSRemotingDataStructureException">
        /// 1. PowerShell client does not support the PSVersion {1} negotiated by the server.
        ///    Make sure the server is compatible with the build {2} of PowerShell.
        /// 2. PowerShell client does not support the SerializationVersion {1} negotiated by the server.
        ///    Make sure the server is compatible with the build {2} of PowerShell.
        /// </exception>
        private bool RunClientNegotiationAlgorithm(RemoteSessionCapability serverRemoteSessionCapability)
        {
            Dbg.Assert(serverRemoteSessionCapability != null, "server capability cache must be non-null");

            // ProtocolVersion check
            Version serverProtocolVersion = serverRemoteSessionCapability.ProtocolVersion;
            _serverProtocolVersion = serverProtocolVersion;
            Version clientProtocolVersion = Context.ClientCapability.ProtocolVersion;

            if (
                clientProtocolVersion.Equals(serverProtocolVersion)
                || (clientProtocolVersion == RemotingConstants.ProtocolVersionWin7RTM &&
                    serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RC)
                || (clientProtocolVersion == RemotingConstants.ProtocolVersionWin8RTM &&
                    (serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RC ||
                     serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RTM
                     ))
                || (clientProtocolVersion == RemotingConstants.ProtocolVersionWin10RTM &&
                    (serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RC ||
                     serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RTM ||
                     serverProtocolVersion == RemotingConstants.ProtocolVersionWin8RTM
                     ))
                 )
            {
                // passed negotiation check
            }
            else
            {
                PSRemotingDataStructureException reasonOfFailure =
                    new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientNegotiationFailed,
                        RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME,
                        serverProtocolVersion,
                        PSVersionInfo.GitCommitId,
                        RemotingConstants.ProtocolVersion);
                throw reasonOfFailure;
            }

            // PSVersion check
            Version serverPSVersion = serverRemoteSessionCapability.PSVersion;
            Version clientPSVersion = Context.ClientCapability.PSVersion;
            if (!clientPSVersion.Equals(serverPSVersion))
            {
                PSRemotingDataStructureException reasonOfFailure =
                    new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientNegotiationFailed,
                        RemoteDataNameStrings.PSVersion,
                        serverPSVersion.ToString(),
                        PSVersionInfo.GitCommitId,
                        RemotingConstants.ProtocolVersion);
                throw reasonOfFailure;
            }

            // Serialization Version check
            Version serverSerVersion = serverRemoteSessionCapability.SerializationVersion;
            Version clientSerVersion = Context.ClientCapability.SerializationVersion;
            if (!clientSerVersion.Equals(serverSerVersion))
            {
                PSRemotingDataStructureException reasonOfFailure =
                    new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientNegotiationFailed,
                        RemoteDataNameStrings.SerializationVersion,
                        serverSerVersion.ToString(),
                        PSVersionInfo.GitCommitId,
                        RemotingConstants.ProtocolVersion);
                throw reasonOfFailure;
            }

            return true;
        }

        #endregion negotiation

        internal override RemotingDestination MySelf { get; }

        #region IDisposable

        /// <summary>
        /// Public method for dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all managed resources.</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_waitHandleForConfigurationReceived != null)
                {
                    _waitHandleForConfigurationReceived.Dispose();
                    _waitHandleForConfigurationReceived = null;
                }

                ((ClientRemoteSessionDSHandlerImpl)SessionDataStructureHandler).Dispose();
                SessionDataStructureHandler = null;
                _cryptoHelper.Dispose();
                _cryptoHelper = null;
            }
        }

        #endregion IDisposable
    }
}
