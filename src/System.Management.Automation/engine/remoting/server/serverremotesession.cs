// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// By design, on the server side, each remote connection is represented by
    /// a ServerRemoteSession object, which contains one instance of this class.
    ///
    /// This class holds 4 pieces of information.
    /// 1. Client capability: This is the capability received during the negotiation process.
    /// 2. Server capability: This comes from default parameters.
    /// 3. Client configuration: This holds the remote session related configuration parameters that
    ///    the client sent to the server. This parameters can be changed and resent after the connection
    ///    is established.
    /// 4. Server configuration: this holds the server sider configuration parameters.
    ///
    /// All these together define the connection level parameters.
    /// </summary>
    internal sealed class ServerRemoteSessionContext
    {
        #region Constructors

        /// <summary>
        /// The constructor instantiates a server capability object and a server configuration
        /// using default values.
        /// </summary>
        internal ServerRemoteSessionContext()
        {
            ServerCapability = RemoteSessionCapability.CreateServerCapability();
        }

        #endregion Constructors

        /// <summary>
        /// This property represents the capability that the server receives from the client.
        /// </summary>
        internal RemoteSessionCapability ClientCapability { get; set; }

        /// <summary>
        /// This property is the server capability generated on the server side.
        /// </summary>
        internal RemoteSessionCapability ServerCapability { get; set; }

        /// <summary>
        /// True if negotiation from client is succeeded...in which case ClientCapability
        /// is the capability that server agreed with.
        /// </summary>
        internal bool IsNegotiationSucceeded { get; set; }
    }

    /// <summary>
    /// This class is designed to be the server side controller of a remote connection.
    ///
    /// It contains a static entry point that the PowerShell server process will get into
    /// the server mode. At this entry point, a runspace configuration is passed in. This runspace
    /// configuration is used to instantiate a server side runspace.
    ///
    /// This class controls a remote connection by using a Session data structure handler, which
    /// in turn contains a Finite State Machine, and a transport mechanism.
    /// </summary>
    internal sealed class ServerRemoteSession : RemoteSession
    {
        [TraceSourceAttribute("ServerRemoteSession", "ServerRemoteSession")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("ServerRemoteSession", "ServerRemoteSession");

        private readonly PSSenderInfo _senderInfo;
        private readonly string _configProviderId;
        private readonly string _initParameters;
        private string _initScriptForOutOfProcRS;
        private PSSessionConfiguration _sessionConfigProvider;

        // used to apply quotas on command and session transportmanagers.
        private int? _maxRecvdObjectSize;
        private int? _maxRecvdDataSizeCommand;

        private ServerRunspacePoolDriver _runspacePoolDriver;
        private readonly PSRemotingCryptoHelperServer _cryptoHelper;

        // Specifies an optional endpoint configuration for out-of-proc session use.
        // Creates a pushed remote runspace session created with this configuration name.
        private string _configurationName;

        // Specifies an optional .pssc configuration file path for out-of-proc session use.
        // The .pssc file is used to configure the runspace for the endpoint session.
        private string _configurationFile;

        // Specifies an initial location of the powershell session.
        private string _initialLocation;

        #region Events
        /// <summary>
        /// Raised when session is closed.
        /// </summary>
        internal EventHandler<RemoteSessionStateMachineEventArgs> Closed;
        #endregion

        #region Constructors

        /// <summary>
        /// This constructor instantiates a ServerRemoteSession object and
        /// a ServerRemoteSessionDataStructureHandler object.
        /// </summary>
        /// <param name="senderInfo">
        /// Details about the user creating this session.
        /// </param>
        /// <param name="configurationProviderId">
        /// The resource URI for which this session is being created
        /// </param>
        /// <param name="initializationParameters">
        /// Initialization Parameters xml passed by WSMan API. This data is read from the config
        /// xml.
        /// </param>
        /// <param name="transportManager">
        /// The transport manager this session should use to send/receive data
        /// </param>
        /// <returns></returns>
        internal ServerRemoteSession(PSSenderInfo senderInfo,
            string configurationProviderId,
            string initializationParameters,
            AbstractServerSessionTransportManager transportManager)
        {
            Dbg.Assert(transportManager != null, "transportManager cannot be null.");

            // let input,output and error from native commands redirect as we have
            // to send (or receive) them back to client for action.
            NativeCommandProcessor.IsServerSide = true;
            _senderInfo = senderInfo;
            _configProviderId = configurationProviderId;
            _initParameters = initializationParameters;
            _cryptoHelper = (PSRemotingCryptoHelperServer)transportManager.CryptoHelper;
            _cryptoHelper.Session = this;

            Context = new ServerRemoteSessionContext();
            SessionDataStructureHandler = new ServerRemoteSessionDSHandlerImpl(this, transportManager);
            BaseSessionDataStructureHandler = SessionDataStructureHandler;
            SessionDataStructureHandler.CreateRunspacePoolReceived += HandleCreateRunspacePool;
            SessionDataStructureHandler.NegotiationReceived += HandleNegotiationReceived;
            SessionDataStructureHandler.SessionClosing += HandleSessionDSHandlerClosing;
            SessionDataStructureHandler.PublicKeyReceived += HandlePublicKeyReceived;
            transportManager.Closing += HandleResourceClosing;

            // update the quotas from sessionState..start with default size..and
            // when Custom Session Configuration is loaded (during runspace creation) update this.
            transportManager.ReceivedDataCollection.MaximumReceivedObjectSize =
                BaseTransportManager.MaximumReceivedObjectSize;

            // session transport manager can receive unlimited data..however each object is limited
            // by maxRecvdObjectSize. this is to allow clients to use a session for an unlimited time..
            // also the messages that can be sent to a session are limited and very controlled.
            // However a command transport manager can be restricted to receive only a fixed amount of data
            // controlled by maxRecvdDataSizeCommand..This is because commands can accept any number of input
            // objects
            transportManager.ReceivedDataCollection.MaximumReceivedDataSize = null;
        }

        #endregion Constructors

        #region Creation Factory

        /// <summary>
        /// Creates a server remote session for the supplied <paramref name="configurationProviderId"/>
        /// and <paramref name="transportManager"/>.
        /// </summary>
        /// <param name="senderInfo"></param>
        /// <param name="configurationProviderId"></param>
        /// <param name="initializationParameters">
        /// Initialization Parameters xml passed by WSMan API. This data is read from the config
        /// xml.
        /// </param>
        /// <param name="transportManager"></param>
        /// <param name="initialCommand">Optional initial command used for OutOfProc sessions.</param>
        /// <param name="configurationName">Optional configuration endpoint name for OutOfProc sessions.</param>
        /// <param name="configurationFile">Optional configuration file (.pssc) path for OutOfProc sessions.</param>
        /// <param name="initialLocation">Optional configuration initial location of the powershell session.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// InitialSessionState provider with <paramref name="configurationProviderId"/> does
        /// not exist on the remote server.
        /// </exception>
        /*
                  <InitializationParameters>
                    <Param Name="PSVersion" Value="2.0" />
                    <Param Name="ApplicationBase" Value="<folder path>" />
                    ...
                  </InitializationParameters>
        */
        internal static ServerRemoteSession CreateServerRemoteSession(
            PSSenderInfo senderInfo,
            string configurationProviderId,
            string initializationParameters,
            AbstractServerSessionTransportManager transportManager,
            string initialCommand,
            string configurationName,
            string configurationFile,
            string initialLocation)
        {
            Dbg.Assert(
                (senderInfo != null) && (senderInfo.UserInfo != null),
                "senderInfo and userInfo cannot be null.");

            s_trace.WriteLine("Finding InitialSessionState provider for id : {0}", configurationProviderId);

            if (string.IsNullOrEmpty(configurationProviderId))
            {
                throw PSTraceSource.NewInvalidOperationException("RemotingErrorIdStrings.NonExistentInitialSessionStateProvider", configurationProviderId);
            }

            const string shellPrefix = System.Management.Automation.Remoting.Client.WSManNativeApi.ResourceURIPrefix;
            int index = configurationProviderId.IndexOf(shellPrefix, StringComparison.OrdinalIgnoreCase);
            senderInfo.ConfigurationName = (index == 0) ? configurationProviderId.Substring(shellPrefix.Length) : string.Empty;
            ServerRemoteSession result = new ServerRemoteSession(
                senderInfo,
                configurationProviderId,
                initializationParameters,
                transportManager)
            {
                _initScriptForOutOfProcRS = initialCommand,
                _configurationName = configurationName,
                _configurationFile = configurationFile,
                _initialLocation = initialLocation
            };

            // start state machine.
            RemoteSessionStateMachineEventArgs startEventArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.CreateSession);
            result.SessionDataStructureHandler.StateMachine.RaiseEvent(startEventArg);

            return result;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// This indicates the remote session object is Client, Server or Listener.
        /// </summary>
        internal override RemotingDestination MySelf
        {
            get
            {
                return RemotingDestination.Server;
            }
        }

        /// <summary>
        /// This is the data dispatcher for the whole remote connection.
        /// This dispatcher is registered with the server side input queue's InputDataReady event.
        /// When the input queue has received data from client, it calls the InputDataReady listeners.
        ///
        /// This dispatcher distinguishes the negotiation packet as a special case. For all other data,
        /// it dispatches the data through Finite State Machines DoMessageReceived handler by raising the event
        /// MessageReceived. The FSM's DoMessageReceived handler further dispatches to the receiving
        /// components: such as runspace or pipeline which have their own data dispatching methods.
        /// </summary>
        /// <param name="sender">
        /// This parameter is not used by the method, in this implementation.
        /// </param>
        /// <param name="dataEventArg">
        /// This parameter contains the remote data received from client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="dataEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="dataEventArg"/> does not contain remote data.
        /// </exception>
        /// <exception cref="PSRemotingDataStructureException">
        /// If the destination of the data is not for server.
        /// </exception>
        internal void DispatchInputQueueData(object sender, RemoteDataEventArgs dataEventArg)
        {
            if (dataEventArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(dataEventArg));
            }

            RemoteDataObject<PSObject> rcvdData = dataEventArg.ReceivedData;

            if (rcvdData == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(dataEventArg));
            }

            RemotingDestination destination = rcvdData.Destination;

            if ((destination & MySelf) != MySelf)
            {
                // this packet is not target for me.
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.RemotingDestinationNotForMe, MySelf, destination);
            }

            RemotingTargetInterface targetInterface = rcvdData.TargetInterface;
            RemotingDataType dataType = rcvdData.DataType;

            RemoteSessionStateMachineEventArgs messageReceivedArg = null;

            switch (targetInterface)
            {
                case RemotingTargetInterface.Session:
                    {
                        switch (dataType)
                        {
                            // TODO: Directly calling an event handler in StateMachine bypassing the StateMachine's
                            // loop of ProcessEvents()..This is needed as connection is already established and the
                            // following message does not change state. An ideal solution would be to process
                            // non-session messages in this class rather than by StateMachine.
                            case RemotingDataType.CreateRunspacePool:
                                messageReceivedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.MessageReceived);
                                if (SessionDataStructureHandler.StateMachine.CanByPassRaiseEvent(messageReceivedArg))
                                {
                                    messageReceivedArg.RemoteData = rcvdData;
                                    SessionDataStructureHandler.StateMachine.DoMessageReceived(this, messageReceivedArg);
                                }
                                else
                                {
                                    SessionDataStructureHandler.StateMachine.RaiseEvent(messageReceivedArg);
                                }

                                break;

                            case RemotingDataType.CloseSession:
                                SessionDataStructureHandler.RaiseDataReceivedEvent(dataEventArg);
                                break;

                            case RemotingDataType.SessionCapability:
                                SessionDataStructureHandler.RaiseDataReceivedEvent(dataEventArg);
                                break;

                            case RemotingDataType.PublicKey:
                                SessionDataStructureHandler.RaiseDataReceivedEvent(dataEventArg);
                                break;

                            default:
                                Dbg.Assert(false, "Should never reach here");
                                break;
                        }
                    }

                    break;

                // TODO: Directly calling an event handler in StateMachine bypassing the StateMachine's
                // loop of ProcessEvents()..This is needed as connection is already established and the
                // following message does not change state. An ideal solution would be to process
                // non-session messages in this class rather than by StateMachine.
                case RemotingTargetInterface.RunspacePool:
                case RemotingTargetInterface.PowerShell:
                    // GETBACK
                    messageReceivedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.MessageReceived);
                    if (SessionDataStructureHandler.StateMachine.CanByPassRaiseEvent(messageReceivedArg))
                    {
                        messageReceivedArg.RemoteData = rcvdData;
                        SessionDataStructureHandler.StateMachine.DoMessageReceived(this, messageReceivedArg);
                    }
                    else
                    {
                        SessionDataStructureHandler.StateMachine.RaiseEvent(messageReceivedArg);
                    }

                    break;
            }
        }

        /// <summary>
        /// Have received a public key from the other side
        /// Import or take other action based on the state.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">event arguments which contains the
        /// remote public key</param>
        private void HandlePublicKeyReceived(object sender, RemoteDataEventArgs<string> eventArgs)
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established ||
                SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyRequested || // this is only for legacy clients
                SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyExchanged)
            {
                string remotePublicKey = eventArgs.Data;

                bool ret = _cryptoHelper.ImportRemotePublicKey(remotePublicKey);

                RemoteSessionStateMachineEventArgs args = null;
                if (!ret)
                {
                    // importing remote public key failed
                    // set state to closed
                    args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceiveFailed);

                    SessionDataStructureHandler.StateMachine.RaiseEvent(args);
                }

                args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceived);
                SessionDataStructureHandler.StateMachine.RaiseEvent(args);
            }
        }

        /// <summary>
        /// Start the key exchange process.
        /// </summary>
        internal override void StartKeyExchange()
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established)
            {
                // send using data structure handler
                SessionDataStructureHandler.SendRequestForPublicKey();

                RemoteSessionStateMachineEventArgs eventArgs =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyRequested);
                SessionDataStructureHandler.StateMachine.RaiseEvent(eventArgs);
            }
        }

        /// <summary>
        /// Complete the Key exchange process.
        /// </summary>
        internal override void CompleteKeyExchange()
        {
            _cryptoHelper.CompleteKeyExchange();
        }

        /// <summary>
        /// Send an encrypted session key to the client.
        /// </summary>
        internal void SendEncryptedSessionKey()
        {
            Dbg.Assert(SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyReceived,
                "Sever must be in EstablishedAndKeyReceived state before you can attempt to send encrypted session key");

            string encryptedSessionKey = null;
            bool ret = _cryptoHelper.ExportEncryptedSessionKey(out encryptedSessionKey);

            RemoteSessionStateMachineEventArgs args = null;
            if (!ret)
            {
                args =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeySendFailed);
                SessionDataStructureHandler.StateMachine.RaiseEvent(args);
            }

            SessionDataStructureHandler.SendEncryptedSessionKey(encryptedSessionKey);

            // complete the key exchange process
            CompleteKeyExchange();

            args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeySent);
            SessionDataStructureHandler.StateMachine.RaiseEvent(args);
        }

        #endregion Overrides

        #region Properties

        /// <summary>
        /// This property returns the ServerRemoteSessionContext object created inside
        /// this object's constructor.
        /// </summary>
        internal ServerRemoteSessionContext Context { get; }

        /// <summary>
        /// This property returns the ServerRemoteSessionDataStructureHandler object created inside
        /// this object's constructor.
        /// </summary>
        internal ServerRemoteSessionDataStructureHandler SessionDataStructureHandler { get; }

        #endregion

        #region Private/Internal Methods

        /// <summary>
        /// Let the session clear its resources.
        /// </summary>
        /// <param name="reasonForClose"></param>
        internal void Close(RemoteSessionStateMachineEventArgs reasonForClose)
        {
            Closed.SafeInvoke(this, reasonForClose);
            if (_runspacePoolDriver != null)
                _runspacePoolDriver.Closed -= HandleResourceClosing;
        }

        /// <summary>
        /// ExecutesConnect. expects client capability and connect_runspacepool PSRP
        /// messages in connectData.
        /// If negotiation is successful and max and min runspaces in connect_runspacepool
        /// match the associated runspace pool parameters, it builds up server capability
        /// and runspace_initinfo in connectResponseData.
        /// This is a version of Connect that executes the whole connect algorithm in one single
        /// hop.
        /// This algorithm is being executed synchronously without associating with state machine.
        /// </summary>
        /// <param name="connectData"></param>
        /// <param name="connectResponseData"></param>
        /// The operation is being outside the statemachine because of multiple reasons associated with design simplicity
        /// - Support automatic disconnect and let wsman server stack take care of connection state
        /// - The response data should not travel in transports output stream but as part of connect response
        /// - We want this operation to be synchronous
        internal void ExecuteConnect(byte[] connectData, out byte[] connectResponseData)
        {
            connectResponseData = null;
            Fragmentor fragmentor = new Fragmentor(int.MaxValue, null);
            Fragmentor defragmentor = fragmentor;

            int totalDataLen = connectData.Length;

            if (totalDataLen < FragmentedRemoteObject.HeaderLength)
            {
                // raise exception
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // TODO: Follow up on comment from Krishna regarding having the serialization/deserialization separate for this
            // operation. This could be integrated as helper functions in fragmentor/serializer components
            long fragmentId = FragmentedRemoteObject.GetFragmentId(connectData, 0);
            bool sFlag = FragmentedRemoteObject.GetIsStartFragment(connectData, 0);
            bool eFlag = FragmentedRemoteObject.GetIsEndFragment(connectData, 0);
            int blobLength = FragmentedRemoteObject.GetBlobLength(connectData, 0);

            if (blobLength > totalDataLen - FragmentedRemoteObject.HeaderLength)
            {
                // raise exception
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            if (!sFlag || !eFlag)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // if session is not in expected state
            RemoteSessionState currentState = SessionDataStructureHandler.StateMachine.State;
            if (currentState != RemoteSessionState.Established &&
                currentState != RemoteSessionState.EstablishedAndKeyExchanged)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnServerStateValidation);
            }

            // process first message
            MemoryStream serializedStream = new MemoryStream();
            serializedStream.Write(connectData, FragmentedRemoteObject.HeaderLength, blobLength);

            serializedStream.Seek(0, SeekOrigin.Begin);
            RemoteDataObject<PSObject> capabilityObject = RemoteDataObject<PSObject>.CreateFrom(serializedStream, defragmentor);

            if (capabilityObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            if ((capabilityObject.Destination != RemotingDestination.Server) || (capabilityObject.DataType != RemotingDataType.SessionCapability))
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // process second message
            int secondFragmentLength = totalDataLen - FragmentedRemoteObject.HeaderLength - blobLength;
            if (secondFragmentLength < FragmentedRemoteObject.HeaderLength)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            byte[] secondFragment = new byte[secondFragmentLength];
            Array.Copy(connectData, FragmentedRemoteObject.HeaderLength + blobLength, secondFragment, 0, secondFragmentLength);

            fragmentId = FragmentedRemoteObject.GetFragmentId(secondFragment, 0);
            sFlag = FragmentedRemoteObject.GetIsStartFragment(secondFragment, 0);
            eFlag = FragmentedRemoteObject.GetIsEndFragment(secondFragment, 0);
            blobLength = FragmentedRemoteObject.GetBlobLength(secondFragment, 0);

            if (blobLength != secondFragmentLength - FragmentedRemoteObject.HeaderLength)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            if (!sFlag || !eFlag)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // process second message
            serializedStream = new MemoryStream();
            serializedStream.Write(secondFragment, FragmentedRemoteObject.HeaderLength, blobLength);

            serializedStream.Seek(0, SeekOrigin.Begin);
            RemoteDataObject<PSObject> connectRunspacePoolObject = RemoteDataObject<PSObject>.CreateFrom(serializedStream, defragmentor);

            if (connectRunspacePoolObject == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnServerStateValidation);
            }

            if ((connectRunspacePoolObject.Destination != RemotingDestination.Server) || (connectRunspacePoolObject.DataType != RemotingDataType.ConnectRunspacePool))
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // We have the two objects required for validating the connect operation
            RemoteSessionCapability clientCapability;
            try
            {
                clientCapability = RemotingDecoder.GetSessionCapability(capabilityObject.Data);
            }
            catch (PSRemotingDataStructureException)
            {
                // this will happen if expected properties are not
                // received for session capability
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            try
            {
                RunServerNegotiationAlgorithm(clientCapability, true);
            }
            catch (PSRemotingDataStructureException)
            {
                throw;
            }

            // validate client connect_runspacepool request
            int clientRequestedMinRunspaces = -1;
            int clientRequestedMaxRunspaces = -1;
            bool clientRequestedRunspaceCount = false;
            if (connectRunspacePoolObject.Data.Properties[RemoteDataNameStrings.MinRunspaces] != null && connectRunspacePoolObject.Data.Properties[RemoteDataNameStrings.MaxRunspaces] != null)
            {
                try
                {
                    clientRequestedMinRunspaces = RemotingDecoder.GetMinRunspaces(connectRunspacePoolObject.Data);
                    clientRequestedMaxRunspaces = RemotingDecoder.GetMaxRunspaces(connectRunspacePoolObject.Data);
                    clientRequestedRunspaceCount = true;
                }
                catch (PSRemotingDataStructureException)
                {
                    throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
                }
            }

            // these should be positive and max should be greater than min
            if (clientRequestedRunspaceCount &&
                (clientRequestedMinRunspaces == -1 || clientRequestedMaxRunspaces == -1 || clientRequestedMinRunspaces > clientRequestedMaxRunspaces))
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            if (_runspacePoolDriver == null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnServerStateValidation);
            }

            // currently only one runspace pool per session is allowed. make sure this ID in connect message is the same
            if (connectRunspacePoolObject.RunspacePoolId != _runspacePoolDriver.InstanceId)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnInputValidation);
            }

            // we currently dont support adjusting runspace count on a connect operation.
            // there is a potential conflict here where in the runspace pool driver is still yet to process a queued
            // setMax or setMinrunspaces request.
            // TODO: resolve this.. probably by letting the runspace pool consume all messages before we execute this.
            if (clientRequestedRunspaceCount
                && (_runspacePoolDriver.RunspacePool.GetMaxRunspaces() != clientRequestedMaxRunspaces)
                && (_runspacePoolDriver.RunspacePool.GetMinRunspaces() != clientRequestedMinRunspaces))
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnMismatchedRunspacePoolProperties);
            }

            // all client messages are validated
            // now build up the server capabilities and connect response messages to be piggybacked on connect response
            RemoteDataObject capability = RemotingEncoder.GenerateServerSessionCapability(Context.ServerCapability, _runspacePoolDriver.InstanceId);
            RemoteDataObject runspacepoolInitData = RemotingEncoder.GenerateRunspacePoolInitData(_runspacePoolDriver.InstanceId,
                                                                                               _runspacePoolDriver.RunspacePool.GetMaxRunspaces(),
                                                                                               _runspacePoolDriver.RunspacePool.GetMinRunspaces());

            // having this stream operating separately will result in out of sync fragment Ids. but this is still OK
            // as this is executed only when connecting from a new client that does not have any previous fragments context.
            // no problem even if fragment Ids in this response and the sessiontransport stream clash (interfere) and its guaranteed
            // that the fragments in connect response are always complete (enclose a complete object).
            SerializedDataStream stream = new SerializedDataStream(4 * 1024); //Each message with fragment headers cannot cross 4k
            stream.Enter();
            capability.Serialize(stream, fragmentor);
            stream.Exit();
            stream.Enter();
            runspacepoolInitData.Serialize(stream, fragmentor);
            stream.Exit();
            byte[] outbuffer = stream.Read();
            Dbg.Assert(outbuffer != null, "connect response data should be serialized");
            stream.Dispose();
            // we are done
            connectResponseData = outbuffer;

            // enqueue a connect event in state machine to let session do any other post-connect operation
            // Do this outside of the synchronous connect operation, as otherwise connect can easily get deadlocked
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                (object state) =>
                {
                    RemoteSessionStateMachineEventArgs startEventArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.ConnectSession);
                    SessionDataStructureHandler.StateMachine.RaiseEvent(startEventArg);
                }));

            _runspacePoolDriver.DataStructureHandler.ProcessConnect();
            return;
        }

        // pass on application private data when session is connected from new client
        internal void HandlePostConnect() => _runspacePoolDriver?.SendApplicationPrivateDataToClient();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="createRunspaceEventArg"></param>
        /// <exception cref="InvalidOperationException">
        /// 1. InitialSessionState cannot be null.
        /// 2. Non existent InitialSessionState provider for the shellID
        /// </exception>
        private void HandleCreateRunspacePool(object sender, RemoteDataEventArgs createRunspaceEventArg)
        {
            if (createRunspaceEventArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(createRunspaceEventArg));
            }

            RemoteDataObject<PSObject> rcvdData = createRunspaceEventArg.ReceivedData;
            Dbg.Assert(rcvdData != null, "rcvdData must be non-null");

            _senderInfo.ApplicationArguments = RemotingDecoder.GetApplicationArguments(rcvdData.Data);

            // Get Initial Session State from custom session config suppliers
            // like Exchange.
            ConfigurationDataFromXML configurationData =
                PSSessionConfiguration.LoadEndPointConfiguration(_configProviderId, _initParameters);
            // used by Out-Of-Proc (IPC) runspace.
            configurationData.InitializationScriptForOutOfProcessRunspace = _initScriptForOutOfProcRS;
            // start with data from configuration XML and then override with data
            // from EndPointConfiguration type.
            _maxRecvdObjectSize = configurationData.MaxReceivedObjectSizeMB;
            _maxRecvdDataSizeCommand = configurationData.MaxReceivedCommandSizeMB;

            if (string.IsNullOrEmpty(configurationData.ConfigFilePath))
            {
                _sessionConfigProvider = configurationData.CreateEndPointConfigurationInstance();
            }
            else
            {
                System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(_senderInfo.UserInfo.WindowsIdentity);
                Func<string, bool> validator = (role) => windowsPrincipal.IsInRole(role);
                _sessionConfigProvider = new DISCPowerShellConfiguration(configurationData.ConfigFilePath, validator);
            }

            // exchange of ApplicationArguments and ApplicationPrivateData is be done as early as possible
            // (i.e. to let the _sessionConfigProvider bail out if it can't comply with client's versioning request)
            PSPrimitiveDictionary applicationPrivateData = _sessionConfigProvider.GetApplicationPrivateData(_senderInfo);

            InitialSessionState rsSessionStateToUse = null;

            if (configurationData.SessionConfigurationData != null)
            {
                // Use the provided WinRM endpoint runspace configuration information.
                try
                {
                    rsSessionStateToUse =
                        _sessionConfigProvider.GetInitialSessionState(configurationData.SessionConfigurationData, _senderInfo, _configProviderId);
                }
                catch (NotImplementedException)
                {
                    rsSessionStateToUse = _sessionConfigProvider.GetInitialSessionState(_senderInfo);
                }
            }
            else if (!string.IsNullOrEmpty(_configurationFile))
            {
                // Use the optional _configurationFile parameter to create the endpoint runspace configuration.
                // This parameter is only used by Out-Of-Proc transports (not WinRM transports).
                var discConfiguration = new Remoting.DISCPowerShellConfiguration(
                    configFile: _configurationFile,
                    roleVerifier: null,
                    validateFile: true);
                rsSessionStateToUse = discConfiguration.GetInitialSessionState(_senderInfo);
            }
            else
            {
                // Create a runspace configuration based on the provided PSSessionConfiguration provider.
                // This can be either a 'default' configuration, or third party configuration PSSessionConfiguration provider object.
                // So far, only Exchange provides a custom PSSessionConfiguration provider implementation.
                rsSessionStateToUse = _sessionConfigProvider.GetInitialSessionState(_senderInfo);
            }

            if (rsSessionStateToUse == null)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.InitialSessionStateNull, _configProviderId);
            }

            rsSessionStateToUse.ThrowOnRunspaceOpenError = true;

            // this might throw if the sender info is already present
            rsSessionStateToUse.Variables.Add(
                new SessionStateVariableEntry(RemoteDataNameStrings.SenderInfoPreferenceVariable,
                _senderInfo,
                Remoting.PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.PSSenderInfoDescription),
                ScopedItemOptions.ReadOnly));

            // Get client PS version from PSSenderInfo.
            Version psClientVersion = null;
            if (_senderInfo.ApplicationArguments != null && _senderInfo.ApplicationArguments.ContainsKey("PSversionTable"))
            {
                var value = PSObject.Base(_senderInfo.ApplicationArguments["PSversionTable"]) as PSPrimitiveDictionary;
                if (value != null && value.ContainsKey("PSVersion"))
                {
                    psClientVersion = PSObject.Base(value["PSVersion"]) as Version;
                }
            }

            if (!string.IsNullOrEmpty(configurationData.EndPointConfigurationTypeName))
            {
                // user specified a type to load for configuration..use the values from this type.
                _maxRecvdObjectSize = _sessionConfigProvider.GetMaximumReceivedObjectSize(_senderInfo);
                _maxRecvdDataSizeCommand = _sessionConfigProvider.GetMaximumReceivedDataSizePerCommand(_senderInfo);
            }

            SessionDataStructureHandler.TransportManager.ReceivedDataCollection.MaximumReceivedObjectSize = _maxRecvdObjectSize;
            // MaximumReceivedDataSize is not set for session transport manager...see the constructor
            // for more info.

            Guid clientRunspacePoolId = rcvdData.RunspacePoolId;
            int minRunspaces = RemotingDecoder.GetMinRunspaces(rcvdData.Data);
            int maxRunspaces = RemotingDecoder.GetMaxRunspaces(rcvdData.Data);
            PSThreadOptions threadOptions = RemotingDecoder.GetThreadOptions(rcvdData.Data);
            ApartmentState apartmentState = RemotingDecoder.GetApartmentState(rcvdData.Data);
            HostInfo hostInfo = RemotingDecoder.GetHostInfo(rcvdData.Data);

            if (_runspacePoolDriver != null)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.RunspaceAlreadyExists,
                    _runspacePoolDriver.InstanceId);
            }

#if !UNIX
            bool isAdministrator = _senderInfo.UserInfo.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#else
            bool isAdministrator = false;
#endif

            ServerRunspacePoolDriver tmpDriver = new ServerRunspacePoolDriver(
                clientRunspacePoolId,
                minRunspaces,
                maxRunspaces,
                threadOptions,
                apartmentState,
                hostInfo,
                rsSessionStateToUse,
                applicationPrivateData,
                configurationData,
                this.SessionDataStructureHandler.TransportManager,
                isAdministrator,
                Context.ServerCapability,
                psClientVersion,
                _configurationName,
                _initialLocation);

            // attach the necessary event handlers and start the driver.
            Interlocked.Exchange(ref _runspacePoolDriver, tmpDriver);
            _runspacePoolDriver.Closed += HandleResourceClosing;
            _runspacePoolDriver.Start();
        }

        /// <summary>
        /// This handler method runs the negotiation algorithm. It decides if the negotiation is successful,
        /// or fails.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="negotiationEventArg">
        /// This parameter contains the client negotiation capability packet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="negotiationEventArg"/> is null.
        /// </exception>
        private void HandleNegotiationReceived(object sender, RemoteSessionNegotiationEventArgs negotiationEventArg)
        {
            if (negotiationEventArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(negotiationEventArg));
            }

            try
            {
                Context.ClientCapability = negotiationEventArg.RemoteSessionCapability;
                // This will throw if there is an error running the algorithm
                RunServerNegotiationAlgorithm(negotiationEventArg.RemoteSessionCapability, false);

                // Send server's capability to client.
                RemoteSessionStateMachineEventArgs sendingNegotiationArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSending);
                SessionDataStructureHandler.StateMachine.RaiseEvent(sendingNegotiationArg);

                // if negotiation succeeded change the state to neg. completed.
                RemoteSessionStateMachineEventArgs negotiationCompletedArg =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationCompleted);
                SessionDataStructureHandler.StateMachine.RaiseEvent(negotiationCompletedArg);
            }
            catch (PSRemotingDataStructureException dse)
            {
                // Before setting to negotiation failed..send servers capability...that
                // way client can communicate differently if it wants to.
                RemoteSessionStateMachineEventArgs sendingNegotiationArg =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSending);
                SessionDataStructureHandler.StateMachine.RaiseEvent(sendingNegotiationArg);

                RemoteSessionStateMachineEventArgs negotiationFailedArg =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationFailed,
                        dse);
                SessionDataStructureHandler.StateMachine.RaiseEvent(negotiationFailedArg);
            }
        }

        /// <summary>
        /// Handle session closing event to close runspace pool drivers this session is hosting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleSessionDSHandlerClosing(object sender, EventArgs eventArgs)
        {
            _runspacePoolDriver?.Close();

            // dispose the session configuration object..this will let them
            // clean their resources.
            if (_sessionConfigProvider != null)
            {
                _sessionConfigProvider.Dispose();
                _sessionConfigProvider = null;
            }
        }

        /// <summary>
        /// This handles closing of any resource used by this session.
        /// Resources used are RunspacePoolDriver, TransportManager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleResourceClosing(object sender, EventArgs args)
        {
            RemoteSessionStateMachineEventArgs closeSessionArgs = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);
            closeSessionArgs.RemoteData = null;
            SessionDataStructureHandler.StateMachine.RaiseEvent(closeSessionArgs);
        }

        /// <summary>
        /// This is the server side remote session capability negotiation algorithm.
        /// </summary>
        /// <param name="clientCapability">
        /// This is the client capability that the server received from client.
        /// </param>
        /// <param name="onConnect">
        /// If the negotiation is on a connect (and not create)
        /// </param>
        /// <returns>
        /// The method returns true if the capability negotiation is successful.
        /// Otherwise, it returns false.
        /// </returns>
        /// <exception cref="PSRemotingDataStructureException">
        /// 1. PowerShell server does not support the PSVersion {1} negotiated by the client.
        ///    Make sure the client is compatible with the build {2} of PowerShell.
        /// 2. PowerShell server does not support the SerializationVersion {1} negotiated by the client.
        ///    Make sure the client is compatible with the build {2} of PowerShell.
        /// </exception>
        private bool RunServerNegotiationAlgorithm(RemoteSessionCapability clientCapability, bool onConnect)
        {
            Dbg.Assert(clientCapability != null, "Client capability cache must be non-null");

            Version clientProtocolVersion = clientCapability.ProtocolVersion;
            Version serverProtocolVersion = Context.ServerCapability.ProtocolVersion;

            if (onConnect)
            {
                bool connectSupported = false;

                // Win10 server can support reconstruct/reconnect for all 2.x protocol versions
                // that support reconstruct/reconnect, Protocol 2.2+
                // Major protocol version differences (2.x -> 3.x) are not supported.
                // A reconstruct can only be initiated by a client that understands disconnect (2.2+),
                // so we only need to check major versions from client and this server for compatibility.
                if (clientProtocolVersion.Major == RemotingConstants.ProtocolVersion.Major)
                {
                    if (clientProtocolVersion.Minor == RemotingConstants.ProtocolVersionWin8RTM.Minor)
                    {
                        // Report that server is Win8 version to the client
                        // Protocol: 2.2
                        connectSupported = true;
                        serverProtocolVersion = RemotingConstants.ProtocolVersionWin8RTM;
                        Context.ServerCapability.ProtocolVersion = serverProtocolVersion;
                    }
                    else if (clientProtocolVersion.Minor > RemotingConstants.ProtocolVersionWin8RTM.Minor)
                    {
                        // All other minor versions are supported and the server returns its full capability
                        // Protocol: 2.3, 2.4, 2.5 ...
                        connectSupported = true;
                    }
                }

                if (!connectSupported)
                {
                    // Throw for protocol versions 2.x that don't support disconnect/reconnect.
                    // Protocol: < 2.2
                    PSRemotingDataStructureException reasonOfFailure =
                        new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerConnectFailedOnNegotiation,
                            RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME,
                            clientProtocolVersion,
                            PSVersionInfo.GitCommitId,
                            RemotingConstants.ProtocolVersion);
                    throw reasonOfFailure;
                }
            }
            else
            {
                // Win10 server can support Win8 client
                if (clientProtocolVersion == RemotingConstants.ProtocolVersionWin8RTM &&
                    (
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin10RTM)
                    ))
                {
                    // - report that server is Win8 version to the client
                    serverProtocolVersion = RemotingConstants.ProtocolVersionWin8RTM;
                    Context.ServerCapability.ProtocolVersion = serverProtocolVersion;
                }

                // Win8, Win10 server can support Win7 client
                if (clientProtocolVersion == RemotingConstants.ProtocolVersionWin7RTM &&
                    (
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin8RTM) ||
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin10RTM)
                    ))
                {
                    // - report that server is Win7 version to the client
                    serverProtocolVersion = RemotingConstants.ProtocolVersionWin7RTM;
                    Context.ServerCapability.ProtocolVersion = serverProtocolVersion;
                }

                // Win7, Win8, Win10 server can support Win7 RC client
                if (clientProtocolVersion == RemotingConstants.ProtocolVersionWin7RC &&
                    (
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin7RTM) ||
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin8RTM) ||
                        (serverProtocolVersion == RemotingConstants.ProtocolVersionWin10RTM)
                    ))
                {
                    // - report that server is RC version to the client
                    serverProtocolVersion = RemotingConstants.ProtocolVersionWin7RC;
                    Context.ServerCapability.ProtocolVersion = serverProtocolVersion;
                }

                if (!((clientProtocolVersion.Major == serverProtocolVersion.Major) &&
                      (clientProtocolVersion.Minor >= serverProtocolVersion.Minor)))
                {
                    PSRemotingDataStructureException reasonOfFailure =
                        new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerNegotiationFailed,
                            RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME,
                            clientProtocolVersion,
                            PSVersionInfo.GitCommitId,
                            RemotingConstants.ProtocolVersion);
                    throw reasonOfFailure;
                }
            }

            // PSVersion Check
            Version clientPSVersion = clientCapability.PSVersion;
            Version serverPSVersion = Context.ServerCapability.PSVersion;
            if (!((clientPSVersion.Major == serverPSVersion.Major) &&
                  (clientPSVersion.Minor >= serverPSVersion.Minor)))
            {
                PSRemotingDataStructureException reasonOfFailure =
                    new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerNegotiationFailed,
                        RemoteDataNameStrings.PSVersion,
                        clientPSVersion,
                        PSVersionInfo.GitCommitId,
                        RemotingConstants.ProtocolVersion);
                throw reasonOfFailure;
            }

            // SerializationVersion check
            Version clientSerVersion = clientCapability.SerializationVersion;
            Version serverSerVersion = Context.ServerCapability.SerializationVersion;
            if (!((clientSerVersion.Major == serverSerVersion.Major) &&
                  (clientSerVersion.Minor >= serverSerVersion.Minor)))
            {
                PSRemotingDataStructureException reasonOfFailure =
                    new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerNegotiationFailed,
                        RemoteDataNameStrings.SerializationVersion,
                        clientSerVersion,
                        PSVersionInfo.GitCommitId,
                        RemotingConstants.ProtocolVersion);
                throw reasonOfFailure;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientRunspacePoolId"></param>
        /// <returns></returns>
        internal ServerRunspacePoolDriver GetRunspacePoolDriver(Guid clientRunspacePoolId)
        {
            if (_runspacePoolDriver == null)
            {
                return null;
            }

            if (_runspacePoolDriver.InstanceId == clientRunspacePoolId)
            {
                return _runspacePoolDriver;
            }

            return null;
        }

        /// <summary>
        /// Used by Command Session to apply quotas on the command transport manager.
        /// This method is here because ServerRemoteSession knows about InitialSessionState.
        /// </summary>
        /// <param name="cmdTransportManager">
        /// Command TransportManager to apply the quota on.
        /// </param>
        internal void ApplyQuotaOnCommandTransportManager(AbstractServerTransportManager cmdTransportManager)
        {
            Dbg.Assert(cmdTransportManager != null, "cmdTransportManager cannot be null");
            cmdTransportManager.ReceivedDataCollection.MaximumReceivedDataSize = _maxRecvdDataSizeCommand;
            cmdTransportManager.ReceivedDataCollection.MaximumReceivedObjectSize = _maxRecvdObjectSize;
        }

        #endregion
    }
}
