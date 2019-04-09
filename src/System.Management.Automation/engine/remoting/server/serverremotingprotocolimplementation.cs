// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Remoting.Server;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class is an implementation of the abstract class ServerRemoteSessionDataStructureHandler.
    /// </summary>
    internal class ServerRemoteSessionDSHandlerImpl : ServerRemoteSessionDataStructureHandler
    {
        private AbstractServerSessionTransportManager _transportManager;
        private ServerRemoteSessionDSHandlerStateMachine _stateMachine;
        private ServerRemoteSession _session;

        internal override AbstractServerSessionTransportManager TransportManager
        {
            get
            {
                return _transportManager;
            }
        }

        #region Constructors

        /// <summary>
        /// Constructs a ServerRemoteSession handler using the supplied transport manager. The
        /// supplied transport manager will be used to send and receive data from the remote
        /// client.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transportManager"></param>
        internal ServerRemoteSessionDSHandlerImpl(ServerRemoteSession session,
            AbstractServerSessionTransportManager transportManager)
        {
            Dbg.Assert(session != null, "session cannot be null.");
            Dbg.Assert(transportManager != null, "transportManager cannot be null.");

            _session = session;
            _stateMachine = new ServerRemoteSessionDSHandlerStateMachine(session);
            _transportManager = transportManager;
            _transportManager.DataReceived += session.DispatchInputQueueData;
        }

        #endregion Constructors

        #region Overrides

        /// <summary>
        /// Calls the transport layer connect to make a connection to the listener.
        /// </summary>
        internal override void ConnectAsync()
        {
            // for the WSMan implementation, this is a no-op..and statemachine is coded accordingly
            // to move to negotiation pending.
        }

        /// <summary>
        /// This method sends the server side capability negotiation packet to the client.
        /// </summary>
        internal override void SendNegotiationAsync()
        {
            RemoteSessionCapability serverCapability = _session.Context.ServerCapability;
            RemoteDataObject data = RemotingEncoder.GenerateServerSessionCapability(serverCapability,
                Guid.Empty);

            RemoteSessionStateMachineEventArgs negotiationSendCompletedArg =
                new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSendCompleted);
            _stateMachine.RaiseEvent(negotiationSendCompletedArg);

            RemoteDataObject<PSObject> dataToBeSent = RemoteDataObject<PSObject>.CreateFrom(
                data.Destination, data.DataType, data.RunspacePoolId, data.PowerShellId, (PSObject)data.Data);
            // send data to client..flush is not true as we expect to send state changed
            // information (from runspace creation)
            _transportManager.SendDataToClient<PSObject>(dataToBeSent, false);
        }

        /// <summary>
        /// This event indicates that the client capability negotiation packet has been received.
        /// </summary>
        internal override event EventHandler<RemoteSessionNegotiationEventArgs> NegotiationReceived;

        /// <summary>
        /// Event that raised when session datastructure handler is closing.
        /// </summary>
        internal override event EventHandler<EventArgs> SessionClosing;

        internal override event EventHandler<RemoteDataEventArgs<string>> PublicKeyReceived;

        /// <summary>
        /// Send the encrypted session key to the client side.
        /// </summary>
        /// <param name="encryptedSessionKey">encrypted session key
        /// as a string</param>
        internal override void SendEncryptedSessionKey(string encryptedSessionKey)
        {
            _transportManager.SendDataToClient<object>(RemotingEncoder.GenerateEncryptedSessionKeyResponse(
                Guid.Empty, encryptedSessionKey), true);
        }

        /// <summary>
        /// Send request to the client for sending a public key.
        /// </summary>
        internal override void SendRequestForPublicKey()
        {
            _transportManager.SendDataToClient<object>(
                RemotingEncoder.GeneratePublicKeyRequest(Guid.Empty), true);
        }

        /// <summary>
        /// Raise the public key received event.
        /// </summary>
        /// <param name="receivedData">Received data.</param>
        /// <remarks>This method is a hook to be called
        /// from the transport manager</remarks>
        internal override void RaiseKeyExchangeMessageReceived(RemoteDataObject<PSObject> receivedData)
        {
            RaiseDataReceivedEvent(new RemoteDataEventArgs(receivedData));
        }

        /// <summary>
        /// This method calls the transport level call to close the connection to the listener.
        /// </summary>
        /// <param name="reasonForClose">
        /// Message describing why the session is closing
        /// </param>
        /// <exception cref="PSRemotingTransportException">
        /// If the transport call fails.
        /// </exception>
        internal override void CloseConnectionAsync(Exception reasonForClose)
        {
            // Raise the closing event
            SessionClosing.SafeInvoke(this, EventArgs.Empty);

            _transportManager.Close(reasonForClose);

            RemoteSessionStateMachineEventArgs closeCompletedArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.CloseCompleted);
            _stateMachine.RaiseEvent(closeCompletedArg);
        }

        /// <summary>
        /// This event indicates that the client has requested to create a new runspace pool
        /// on the server side.
        /// </summary>
        internal override event EventHandler<RemoteDataEventArgs> CreateRunspacePoolReceived;

        /// <summary>
        /// A reference to the FSM object.
        /// </summary>
        internal override ServerRemoteSessionDSHandlerStateMachine StateMachine
        {
            get
            {
                return _stateMachine;
            }
        }

        /// <summary>
        /// This method is used by the input queue dispatching mechanism.
        /// It examines the data and takes appropriate actions.
        /// </summary>
        /// <param name="dataArg">
        /// The received client data.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        internal override void RaiseDataReceivedEvent(RemoteDataEventArgs dataArg)
        {
            if (dataArg == null)
            {
                throw PSTraceSource.NewArgumentNullException("dataArg");
            }

            RemoteDataObject<PSObject> rcvdData = dataArg.ReceivedData;

            RemotingTargetInterface targetInterface = rcvdData.TargetInterface;
            RemotingDataType dataType = rcvdData.DataType;

            Dbg.Assert(targetInterface == RemotingTargetInterface.Session, "targetInterface must be Session");

            switch (dataType)
            {
                case RemotingDataType.CreateRunspacePool:
                    {
                        // At this point, the negotiation is complete, so
                        // need to import the clients public key
                        CreateRunspacePoolReceived.SafeInvoke(this, dataArg);
                    }

                    break;

                case RemotingDataType.CloseSession:
                    PSRemotingDataStructureException reasonOfClose = new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientRequestedToCloseSession);
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
                        throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerNotFoundCapabilityProperties,
                            dse.Message, PSVersionInfo.GitCommitId, RemotingConstants.ProtocolVersion);
                    }

                    RemoteSessionStateMachineEventArgs capabilityArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationReceived);
                    capabilityArg.RemoteSessionCapability = capability;
                    _stateMachine.RaiseEvent(capabilityArg);

                    if (NegotiationReceived != null)
                    {
                        RemoteSessionNegotiationEventArgs negotiationArg = new RemoteSessionNegotiationEventArgs(capability);
                        negotiationArg.RemoteData = rcvdData;
                        NegotiationReceived.SafeInvoke(this, negotiationArg);
                    }

                    break;

                case RemotingDataType.PublicKey:
                    {
                        string remotePublicKey = RemotingDecoder.GetPublicKey(rcvdData.Data);
                        PublicKeyReceived.SafeInvoke(this, new RemoteDataEventArgs<string>(remotePublicKey));
                    }

                    break;

                default:
                    throw new PSRemotingDataStructureException(RemotingErrorIdStrings.ReceivedUnsupportedAction, dataType);
            }
        }

        #endregion Overrides
    }
}

