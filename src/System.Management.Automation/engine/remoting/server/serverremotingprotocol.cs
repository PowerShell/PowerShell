// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

using System.Management.Automation.Remoting.Server;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This abstract class defines the server side data structure handler that a remote connection has
    /// at the remote session level.
    /// There are two other data structure handler levels:
    /// 1) at the runspace level,
    /// 2) at the pipeline level.
    ///
    /// This session level data structure handler defines what can be done at the session level.
    /// </summary>
    internal abstract class ServerRemoteSessionDataStructureHandler : BaseSessionDataStructureHandler
    {
        #region Constructors

        /// <summary>
        /// Constructor does no special initialization.
        /// </summary>
        internal ServerRemoteSessionDataStructureHandler()
        {
        }

        #endregion Constructors

        #region Abstract_API

        /// <summary>
        /// Makes a connect call asynchronously.
        /// </summary>
        internal abstract void ConnectAsync();

        /// <summary>
        /// Send capability negotiation asynchronously.
        /// </summary>
        internal abstract void SendNegotiationAsync();

        /// <summary>
        /// This event indicates that a client's capability negotiation packet has been received.
        /// </summary>
        internal abstract event EventHandler<RemoteSessionNegotiationEventArgs> NegotiationReceived;

        /// <summary>
        /// Close the connection asynchronously.
        /// </summary>
        /// <param name="reasonForClose">
        /// Message describing why the session is closing
        /// </param>
        internal abstract void CloseConnectionAsync(Exception reasonForClose);

        /// <summary>
        /// Event that raised when session datastructure handler is closing.
        /// </summary>
        internal abstract event EventHandler<EventArgs> SessionClosing;

        /// <summary>
        /// This event indicates a request for creating a new runspace pool
        /// has been received on the server side.
        /// </summary>
        internal abstract event EventHandler<RemoteDataEventArgs> CreateRunspacePoolReceived;

        /// <summary>
        /// A reference to the Finite State Machine.
        /// </summary>
        internal abstract ServerRemoteSessionDSHandlerStateMachine StateMachine
        {
            get;
        }

        /// <summary>
        /// Transport manager used by this data structure handler.
        /// </summary>
        internal abstract AbstractServerSessionTransportManager TransportManager
        {
            get;
        }

        /// <summary>
        /// This method is used by the client data dispatching mechanism.
        /// </summary>
        /// <param name="arg">
        /// This parameter contains the remote data from the client.
        /// </param>
        internal abstract void RaiseDataReceivedEvent(RemoteDataEventArgs arg); // this is the API the Transport calls

        internal abstract event EventHandler<RemoteDataEventArgs<string>> PublicKeyReceived;

        internal abstract void SendRequestForPublicKey();

        internal abstract void SendEncryptedSessionKey(string encryptedSessionKey);

        #endregion Abstract_API
    }
}
