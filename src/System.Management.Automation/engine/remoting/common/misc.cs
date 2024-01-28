// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Remoting;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal sealed class RemoteSessionNegotiationEventArgs : EventArgs
    {
        #region Constructors

        internal RemoteSessionNegotiationEventArgs(RemoteSessionCapability remoteSessionCapability)
        {
            Dbg.Assert(remoteSessionCapability != null, "caller should validate the parameter");

            if (remoteSessionCapability == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(remoteSessionCapability));
            }

            RemoteSessionCapability = remoteSessionCapability;
        }

        #endregion Constructors

        /// <summary>
        /// Data from network converted to type RemoteSessionCapability.
        /// </summary>
        internal RemoteSessionCapability RemoteSessionCapability { get; }

        /// <summary>
        /// Actual data received from the network.
        /// </summary>
        internal RemoteDataObject<PSObject> RemoteData { get; set; }
    }

    /// <summary>
    /// This event arg is designed to contain generic data received from the other side of the connection.
    /// It can be used for both the client side and for the server side.
    /// </summary>
    internal sealed class RemoteDataEventArgs : EventArgs
    {
        #region Constructors

        internal RemoteDataEventArgs(RemoteDataObject<PSObject> receivedData)
        {
            Dbg.Assert(receivedData != null, "caller should validate the parameter");

            if (receivedData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(receivedData));
            }

            ReceivedData = receivedData;
        }

        #endregion Constructors

        /// <summary>
        /// Received data.
        /// </summary>
        public RemoteDataObject<PSObject> ReceivedData { get; }
    }

    /// <summary>
    /// This event arg contains data received and is used to pass information
    /// from a data structure handler to its object.
    /// </summary>
    /// <typeparam name="T">type of data that's associated</typeparam>
    internal sealed class RemoteDataEventArgs<T> : EventArgs
    {
        #region Private Members

        #endregion Private Members

        #region Properties

        /// <summary>
        /// The data contained within this event.
        /// </summary>
        internal T Data { get; }

        #endregion Properties

        #region Constructor

        internal RemoteDataEventArgs(object data)
        {
            // Dbg.Assert(data != null, "data passed should not be null");

            Data = (T)data;
        }

        #endregion Constructor
    }

    /// <summary>
    /// This defines the various states a remote connection can be in.
    /// </summary>
    internal enum RemoteSessionState
    {
        /// <summary>
        /// Undefined state.
        /// </summary>
        UndefinedState = 0,

        /// <summary>
        /// This is the state a connect start with. When a connection is closed,
        /// the connection will eventually come back to this Idle state.
        /// </summary>
        Idle = 1,

        /// <summary>
        /// A connection operation has been initiated.
        /// </summary>
        Connecting = 2,

        /// <summary>
        /// A connection operation has completed successfully.
        /// </summary>
        Connected = 3,

        /// <summary>
        /// The capability negotiation message is in the process being sent on a create operation.
        /// </summary>
        NegotiationSending = 4,

        /// <summary>
        /// The capability negotiation message is in the process being sent on a connect operation.
        /// </summary>
        NegotiationSendingOnConnect = 5,

        /// <summary>
        /// The capability negotiation message is sent successfully from a sender point of view.
        /// </summary>
        NegotiationSent = 6,

        /// <summary>
        /// A capability negotiation message is received.
        /// </summary>
        NegotiationReceived = 7,

        /// <summary>
        /// Used by server to wait for negotiation from client.
        /// </summary>
        NegotiationPending = 8,

        /// <summary>
        /// The connection is in the progress of getting closed.
        /// </summary>
        ClosingConnection = 9,

        /// <summary>
        /// The connection is closed completely.
        /// </summary>
        Closed = 10,

        /// <summary>
        /// The capability negotiation has been successfully completed.
        /// </summary>
        Established = 11,

        /// <summary>
        /// Have sent a public key to the remote end,
        /// awaiting a response.
        /// </summary>
        /// <remarks>Applicable only to client</remarks>
        EstablishedAndKeySent = 12,

        /// <summary>
        /// Have received a public key from the remote
        /// end, need to send a response.
        /// </summary>
        /// <remarks>Applicable only to server</remarks>
        EstablishedAndKeyReceived = 13,

        /// <summary>
        /// For Server - Have sent a request to the remote end to
        /// send a public key
        /// for Client - have received a PK request from server.
        /// </summary>
        /// <remarks>Applicable to both client and server</remarks>
        EstablishedAndKeyRequested = 14,

        /// <summary>
        /// Key exchange complete. This can mean
        ///      (a) Sent an encrypted session key to the
        ///          remote end in response to receiving
        ///          a public key - this is for the server
        ///      (b) Received an encrypted session key from
        ///          remote end after sending a public key -
        ///          this is for the client.
        /// </summary>
        EstablishedAndKeyExchanged = 15,

        /// <summary>
        /// </summary>
        Disconnecting = 16,

        /// <summary>
        /// </summary>
        Disconnected = 17,

        /// <summary>
        /// </summary>
        Reconnecting = 18,

        /// <summary>
        /// A disconnect operation initiated by the WinRM robust connection
        /// layer and *not* by the user.
        /// </summary>
        RCDisconnecting = 19,

        /// <summary>
        /// Number of states.
        /// </summary>
        MaxState = 20
    }

    /// <summary>
    /// This defines the internal events that the finite state machine for the connection
    /// uses to take action and perform state transitions.
    /// </summary>
    internal enum RemoteSessionEvent
    {
        InvalidEvent = 0,
        CreateSession = 1,
        ConnectSession = 2,
        NegotiationSending = 3,
        NegotiationSendingOnConnect = 4,
        NegotiationSendCompleted = 5,
        NegotiationReceived = 6,
        NegotiationCompleted = 7,
        NegotiationPending = 8,
        Close = 9,
        CloseCompleted = 10,
        CloseFailed = 11,
        ConnectFailed = 12,
        NegotiationFailed = 13,
        NegotiationTimeout = 14,
        SendFailed = 15,
        ReceiveFailed = 16,
        FatalError = 17,
        MessageReceived = 18,
        KeySent = 19,
        KeySendFailed = 20,
        KeyReceived = 21,
        KeyReceiveFailed = 22,
        KeyRequested = 23,
        KeyRequestFailed = 24,
        DisconnectStart = 25,
        DisconnectCompleted = 26,
        DisconnectFailed = 27,
        ReconnectStart = 28,
        ReconnectCompleted = 29,
        ReconnectFailed = 30,
        RCDisconnectStarted = 31,
        MaxEvent = 32
    }

    /// <summary>
    /// This is a wrapper class for RemoteSessionState.
    /// </summary>
    internal sealed class RemoteSessionStateInfo
    {
        #region Constructors

        internal RemoteSessionStateInfo(RemoteSessionState state)
            : this(state, null)
        {
        }

        internal RemoteSessionStateInfo(RemoteSessionState state, Exception reason)
        {
            State = state;
            Reason = reason;
        }

        internal RemoteSessionStateInfo(RemoteSessionStateInfo sessionStateInfo)
        {
            State = sessionStateInfo.State;
            Reason = sessionStateInfo.Reason;
        }

        #endregion Constructors

        #region Public_Properties

        /// <summary>
        /// State of the connection.
        /// </summary>
        internal RemoteSessionState State { get; }

        /// <summary>
        /// If the connection is closed, this provides reason why it had happened.
        /// </summary>
        internal Exception Reason { get; }

        #endregion Public_Properties
    }

    /// <summary>
    /// This is the event arg that contains the state information.
    /// </summary>
    internal sealed class RemoteSessionStateEventArgs : EventArgs
    {
        #region Constructors

        internal RemoteSessionStateEventArgs(RemoteSessionStateInfo remoteSessionStateInfo)
        {
            Dbg.Assert(remoteSessionStateInfo != null, "caller should validate the parameter");

            if (remoteSessionStateInfo == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(remoteSessionStateInfo));
            }

            SessionStateInfo = remoteSessionStateInfo;
        }

        #endregion Constructors

        #region Public_Properties

        /// <summary>
        /// State information about the connection.
        /// </summary>
        public RemoteSessionStateInfo SessionStateInfo { get; }

        #endregion Public_Properties
    }

    internal sealed class RemoteSessionStateMachineEventArgs : EventArgs
    {
        #region Constructors

        internal RemoteSessionStateMachineEventArgs(RemoteSessionEvent stateEvent)
            : this(stateEvent, null)
        {
        }

        internal RemoteSessionStateMachineEventArgs(RemoteSessionEvent stateEvent, Exception reason)
        {
            StateEvent = stateEvent;
            Reason = reason;
        }

        #endregion Constructors

        internal RemoteSessionEvent StateEvent { get; }

        internal Exception Reason { get; }

        internal RemoteSessionCapability RemoteSessionCapability { get; set; }

        internal RemoteDataObject<PSObject> RemoteData { get; set; }
    }

    /// <summary>
    /// Defines the various types of remoting behaviour that a cmdlet may
    /// desire when used in a context that supports ambient / automatic remoting.
    /// </summary>
    public enum RemotingCapability
    {
        /// <summary>
        /// In the presence of ambient remoting, this command should
        /// still be run locally.
        /// </summary>
        None,

        /// <summary>
        /// In the presence of ambient remoting, this command should
        /// be run on the target computer using PowerShell Remoting.
        /// </summary>
        PowerShell,

        /// <summary>
        /// In the presence of ambient remoting, this command supports
        /// its own form of remoting which can be used instead to target
        /// the remote computer.
        /// </summary>
        SupportedByCommand,

        /// <summary>
        /// In the presence of ambient remoting, the command assumes
        /// all responsibility for targeting the remote computer;
        /// PowerShell Remoting is not supported.
        /// </summary>
        OwnedByCommand
    }

    /// <summary>
    /// Controls or overrides the remoting behavior, during invocation, of a
    /// command that supports ambient remoting.
    /// </summary>
    public enum RemotingBehavior
    {
        /// <summary>
        /// In the presence of ambient remoting, run this command locally.
        /// </summary>
        None,

        /// <summary>
        /// In the presence of ambient remoting, run this command on the target
        /// computer using PowerShell Remoting.
        /// </summary>
        PowerShell,

        /// <summary>
        /// In the presence of ambient remoting, and a command that declares
        /// 'SupportedByCommand' remoting capability, run this command on the
        /// target computer using the command's custom remoting facilities.
        /// </summary>
        Custom
    }
}
