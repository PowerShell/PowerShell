// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class implements a Finite State Machine (FSM) to control the remote connection on the client side.
    /// There is a similar but not identical FSM on the server side for this connection.
    ///
    /// The FSM's states and events are defined to be the same for both the client FSM and the server FSM.
    /// This design allows the client and server FSM's to
    /// be as similar as possible, so that the complexity of maintaining them is minimized.
    ///
    /// This FSM only controls the remote connection state. States related to runspace and pipeline are managed by runspace
    /// pipeline themselves.
    ///
    /// This FSM defines an event handling matrix, which is filled by the event handlers.
    /// The state transitions can only be performed by these event handlers, which are private
    /// to this class. The event handling is done by a single thread, which makes this
    /// implementation solid and thread safe.
    ///
    /// This implementation of the FSM does not allow the remote session to be reused for a connection
    /// after it is been closed. This design decision is made to simplify the implementation.
    /// However, the design can be easily modified to allow the reuse of the remote session
    /// to reconnect after the connection is closed.
    /// </summary>
    internal sealed class ClientRemoteSessionDSHandlerStateMachine
    {
        [TraceSourceAttribute("CRSessionFSM", "CRSessionFSM")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("CRSessionFSM", "CRSessionFSM");

        /// <summary>
        /// Event handling matrix. It defines what action to take when an event occur.
        /// [State,Event]=>Action.
        /// </summary>
        private readonly EventHandler<RemoteSessionStateMachineEventArgs>[,] _stateMachineHandle;
        private readonly Queue<RemoteSessionStateEventArgs> _clientRemoteSessionStateChangeQueue;

        /// <summary>
        /// Current state of session.
        /// </summary>
        private RemoteSessionState _state;

        private readonly Queue<RemoteSessionStateMachineEventArgs> _processPendingEventsQueue
            = new Queue<RemoteSessionStateMachineEventArgs>();

        // all events raised through the state machine
        // will be queued in this
        private readonly object _syncObject = new object();
        // object for synchronizing access to the above
        // queue

        private bool _eventsInProcess = false;
        // whether some thread is actively processing events
        // in a loop. If this is set then other threads
        // should simply add to the queue and not attempt
        // at processing the events in the queue. This will
        // guarantee that events will always be serialized
        // and processed

        /// <summary>
        /// Timer to be used for key exchange.
        /// </summary>
        private Timer _keyExchangeTimer;

        /// <summary>
        /// Indicates that the client has previously completed the session key exchange.
        /// </summary>
        private bool _keyExchanged = false;

        /// <summary>
        /// This is to queue up a disconnect request when a key exchange is in process
        /// the session will be disconnect once the exchange is complete
        /// intermediate disconnect requests are tracked by this flag.
        /// </summary>
        private bool _pendingDisconnect = false;

        /// <summary>
        /// Processes events in the queue. If there are no
        /// more events to process, then sets eventsInProcess
        /// variable to false. This will ensure that another
        /// thread which raises an event can then take control
        /// of processing the events.
        /// </summary>
        private void ProcessEvents()
        {
            RemoteSessionStateMachineEventArgs eventArgs = null;

            do
            {
                lock (_syncObject)
                {
                    if (_processPendingEventsQueue.Count == 0)
                    {
                        _eventsInProcess = false;
                        break;
                    }

                    eventArgs = _processPendingEventsQueue.Dequeue();
                }

                try
                {
                    RaiseEventPrivate(eventArgs);
                }
                catch (Exception ex)
                {
                    HandleFatalError(ex);
                }

                try
                {
                    RaiseStateMachineEvents();
                }
                catch (Exception ex)
                {
                    HandleFatalError(ex);
                }
            } while (_eventsInProcess);
        }

        private void HandleFatalError(Exception ex)
        {
            // Event handlers should not throw exceptions.  But if they do we need to
            // handle them here to prevent the state machine from not responding when there are pending
            // events to process.

            // Enqueue a fatal error event if such an exception occurs; clear all existing events.. we are going to terminate the session
            PSRemotingDataStructureException fatalError = new PSRemotingDataStructureException(ex,
                        RemotingErrorIdStrings.FatalErrorCausingClose);

            RemoteSessionStateMachineEventArgs closeEvent =
                new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close, fatalError);

            RaiseEvent(closeEvent, true);
        }

        /// <summary>
        /// Raises the StateChanged events which are queued
        /// All StateChanged events will be raised once the
        /// processing of the State Machine events are
        /// complete.
        /// </summary>
        private void RaiseStateMachineEvents()
        {
            RemoteSessionStateEventArgs queuedEventArg = null;

            while (_clientRemoteSessionStateChangeQueue.Count > 0)
            {
                queuedEventArg = _clientRemoteSessionStateChangeQueue.Dequeue();

                StateChanged.SafeInvoke(this, queuedEventArg);
            }
        }

        /// <summary>
        /// Unique identifier for this state machine. Used
        /// in tracing.
        /// </summary>
        private readonly Guid _id;

        /// <summary>
        /// Handler to be used in cases, where setting the state is the
        /// only task being performed. This method also asserts
        /// if the specified event is valid for the current state of
        /// the state machine.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">Event args.</param>
        private void SetStateHandler(object sender, RemoteSessionStateMachineEventArgs eventArgs)
        {
            switch (eventArgs.StateEvent)
            {
                case RemoteSessionEvent.NegotiationCompleted:
                    {
                        Dbg.Assert(_state == RemoteSessionState.NegotiationReceived,
                            "State can be set to Established only when current state is NegotiationReceived");
                        SetState(RemoteSessionState.Established, null);
                    }

                    break;

                case RemoteSessionEvent.NegotiationReceived:
                    {
                        Dbg.Assert(eventArgs.RemoteSessionCapability != null,
                            "State can be set to NegotiationReceived only when RemoteSessionCapability is not null");
                        if (eventArgs.RemoteSessionCapability == null)
                        {
                            throw PSTraceSource.NewArgumentException(nameof(eventArgs));
                        }

                        SetState(RemoteSessionState.NegotiationReceived, null);
                    }

                    break;

                case RemoteSessionEvent.NegotiationSendCompleted:
                    {
                        Dbg.Assert((_state == RemoteSessionState.NegotiationSending) || (_state == RemoteSessionState.NegotiationSendingOnConnect),
                            "Negotiating send can be completed only when current state is NegotiationSending");

                        SetState(RemoteSessionState.NegotiationSent, null);
                    }

                    break;

                case RemoteSessionEvent.ConnectFailed:
                    {
                        Dbg.Assert(_state == RemoteSessionState.Connecting,
                            "A ConnectFailed event can be raised only when the current state is Connecting");

                        SetState(RemoteSessionState.ClosingConnection, eventArgs.Reason);
                    }

                    break;

                case RemoteSessionEvent.CloseFailed:
                    {
                        SetState(RemoteSessionState.Closed, eventArgs.Reason);
                    }

                    break;

                case RemoteSessionEvent.CloseCompleted:
                    {
                        SetState(RemoteSessionState.Closed, eventArgs.Reason);
                    }

                    break;

                case RemoteSessionEvent.KeyRequested:
                    {
                        Dbg.Assert(_state == RemoteSessionState.Established,
                            "Server can request a key only after the client reaches the Established state");

                        if (_state == RemoteSessionState.Established)
                        {
                            SetState(RemoteSessionState.EstablishedAndKeyRequested, eventArgs.Reason);
                        }
                    }

                    break;

                case RemoteSessionEvent.KeyReceived:
                    {
                        Dbg.Assert(_state == RemoteSessionState.EstablishedAndKeySent,
                            "Key Receiving can only be raised after reaching the Established state");

                        if (_state == RemoteSessionState.EstablishedAndKeySent)
                        {
                            Timer tmp = Interlocked.Exchange(ref _keyExchangeTimer, null);
                            tmp?.Dispose();

                            _keyExchanged = true;
                            SetState(RemoteSessionState.Established, eventArgs.Reason);

                            if (_pendingDisconnect)
                            {
                                // session key exchange is complete, if there is a disconnect pending, process it now
                                _pendingDisconnect = false;
                                DoDisconnect(sender, eventArgs);
                            }
                        }
                    }

                    break;

                case RemoteSessionEvent.KeySent:
                    {
                        Dbg.Assert(_state >= RemoteSessionState.Established,
                            "Client can send a public key only after reaching the Established state");

                        Dbg.Assert(!_keyExchanged, "Client should do key exchange only once");

                        if (_state == RemoteSessionState.Established ||
                            _state == RemoteSessionState.EstablishedAndKeyRequested)
                        {
                            SetState(RemoteSessionState.EstablishedAndKeySent, eventArgs.Reason);

                            // start the timer and wait
                            _keyExchangeTimer = new Timer(HandleKeyExchangeTimeout, null, BaseTransportManager.ClientDefaultOperationTimeoutMs, Timeout.Infinite);
                        }
                    }

                    break;
                case RemoteSessionEvent.DisconnectCompleted:
                    {
                        Dbg.Assert(_state == RemoteSessionState.Disconnecting || _state == RemoteSessionState.RCDisconnecting,
                            "DisconnectCompleted event received while state machine is in wrong state");

                        if (_state == RemoteSessionState.Disconnecting || _state == RemoteSessionState.RCDisconnecting)
                        {
                            SetState(RemoteSessionState.Disconnected, eventArgs.Reason);
                        }
                    }

                    break;
                case RemoteSessionEvent.DisconnectFailed:
                    {
                        Dbg.Assert(_state == RemoteSessionState.Disconnecting,
                           "DisconnectCompleted event received while state machine is in wrong state");

                        if (_state == RemoteSessionState.Disconnecting)
                        {
                            SetState(RemoteSessionState.Disconnected, eventArgs.Reason); // set state to disconnected even TODO. Put some ETW event describing the disconnect process failure
                        }
                    }

                    break;
                case RemoteSessionEvent.ReconnectCompleted:
                    {
                        Dbg.Assert(_state == RemoteSessionState.Reconnecting,
                            "ReconnectCompleted event received while state machine is in wrong state");

                        if (_state == RemoteSessionState.Reconnecting)
                        {
                            SetState(RemoteSessionState.Established, eventArgs.Reason);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the timeout for key exchange.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        private void HandleKeyExchangeTimeout(object sender)
        {
            Dbg.Assert(_state == RemoteSessionState.EstablishedAndKeySent, "timeout should only happen when waiting for a key");

            Timer tmp = Interlocked.Exchange(ref _keyExchangeTimer, null);
            tmp?.Dispose();

            PSRemotingDataStructureException exception =
                new PSRemotingDataStructureException(RemotingErrorIdStrings.ClientKeyExchangeFailed);

            RaiseEvent(new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceiveFailed, exception));
        }

        /// <summary>
        /// Handler to be used in cases, where raising an event to
        /// the state needs to be performed. This method also
        /// asserts if the specified event is valid for
        /// the current state of the state machine.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">Event args.</param>
        private void SetStateToClosedHandler(object sender, RemoteSessionStateMachineEventArgs eventArgs)
        {
            Dbg.Assert(_state == RemoteSessionState.NegotiationReceived &&
                            eventArgs.StateEvent == RemoteSessionEvent.NegotiationFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.SendFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.ReceiveFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.NegotiationTimeout ||
                            eventArgs.StateEvent == RemoteSessionEvent.KeySendFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.KeyReceiveFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.KeyRequestFailed ||
                            eventArgs.StateEvent == RemoteSessionEvent.ReconnectFailed,
                        "An event to close the state machine can be raised only on the following conditions: " +
            "1. Negotiation failed 2. Send failed 3. Receive failed 4. Negotiation timedout 5. Key send failed 6. key receive failed 7. key exchange failed 8. Reconnection failed");

            // if there is a NegotiationTimeout event raised, it
            // shouldn't matter if we are currently in the
            // Established state
            if (eventArgs.StateEvent == RemoteSessionEvent.NegotiationTimeout &&
                State == RemoteSessionState.Established)
            {
                return;
            }

            // raise an event to close the state machine
            RaiseEvent(new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close,
                            eventArgs.Reason));
        }

        #region constructor
        /// <summary>
        /// Creates an instance of ClientRemoteSessionDSHandlerStateMachine.
        /// </summary>
        internal ClientRemoteSessionDSHandlerStateMachine()
        {
            _clientRemoteSessionStateChangeQueue = new Queue<RemoteSessionStateEventArgs>();

            // Initialize the state machine event handling matrix
            _stateMachineHandle = new EventHandler<RemoteSessionStateMachineEventArgs>[(int)RemoteSessionState.MaxState, (int)RemoteSessionEvent.MaxEvent];
            for (int i = 0; i < _stateMachineHandle.GetLength(0); i++)
            {
                _stateMachineHandle[i, (int)RemoteSessionEvent.FatalError] += DoFatal;

                _stateMachineHandle[i, (int)RemoteSessionEvent.Close] += DoClose;
                _stateMachineHandle[i, (int)RemoteSessionEvent.CloseFailed] += SetStateHandler;
                _stateMachineHandle[i, (int)RemoteSessionEvent.CloseCompleted] += SetStateHandler;

                _stateMachineHandle[i, (int)RemoteSessionEvent.NegotiationTimeout] += SetStateToClosedHandler;

                _stateMachineHandle[i, (int)RemoteSessionEvent.SendFailed] += SetStateToClosedHandler;

                _stateMachineHandle[i, (int)RemoteSessionEvent.ReceiveFailed] += SetStateToClosedHandler;
                _stateMachineHandle[i, (int)RemoteSessionEvent.CreateSession] += DoCreateSession;
                _stateMachineHandle[i, (int)RemoteSessionEvent.ConnectSession] += DoConnectSession;
            }

            _stateMachineHandle[(int)RemoteSessionState.Idle, (int)RemoteSessionEvent.NegotiationSending] += DoNegotiationSending;

            _stateMachineHandle[(int)RemoteSessionState.Idle, (int)RemoteSessionEvent.NegotiationSendingOnConnect] += DoNegotiationSending;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSending, (int)RemoteSessionEvent.NegotiationSendCompleted] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSendingOnConnect, (int)RemoteSessionEvent.NegotiationSendCompleted] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSent, (int)RemoteSessionEvent.NegotiationReceived] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationReceived, (int)RemoteSessionEvent.NegotiationCompleted] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationReceived, (int)RemoteSessionEvent.NegotiationFailed] += SetStateToClosedHandler;

            _stateMachineHandle[(int)RemoteSessionState.Connecting, (int)RemoteSessionEvent.ConnectFailed] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.ClosingConnection, (int)RemoteSessionEvent.CloseCompleted] += SetStateHandler;

            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.DisconnectStart] += DoDisconnect;
            _stateMachineHandle[(int)RemoteSessionState.Disconnecting, (int)RemoteSessionEvent.DisconnectCompleted] += SetStateHandler;
            _stateMachineHandle[(int)RemoteSessionState.Disconnecting, (int)RemoteSessionEvent.DisconnectFailed] += SetStateHandler; // dont close
            _stateMachineHandle[(int)RemoteSessionState.Disconnected, (int)RemoteSessionEvent.ReconnectStart] += DoReconnect;
            _stateMachineHandle[(int)RemoteSessionState.Reconnecting, (int)RemoteSessionEvent.ReconnectCompleted] += SetStateHandler;
            _stateMachineHandle[(int)RemoteSessionState.Reconnecting, (int)RemoteSessionEvent.ReconnectFailed] += SetStateToClosedHandler;

            _stateMachineHandle[(int)RemoteSessionState.Disconnecting, (int)RemoteSessionEvent.RCDisconnectStarted] += DoRCDisconnectStarted;
            _stateMachineHandle[(int)RemoteSessionState.Disconnected, (int)RemoteSessionEvent.RCDisconnectStarted] += DoRCDisconnectStarted;
            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.RCDisconnectStarted] += DoRCDisconnectStarted;
            _stateMachineHandle[(int)RemoteSessionState.RCDisconnecting, (int)RemoteSessionEvent.DisconnectCompleted] += SetStateHandler;

            // Disconnect during key exchange process
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeySent, (int)RemoteSessionEvent.DisconnectStart] += DoDisconnectDuringKeyExchange;
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.DisconnectStart] += DoDisconnectDuringKeyExchange;

            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeyRequested] += SetStateHandler; //
            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeySent] += SetStateHandler; //
            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeySendFailed] += SetStateToClosedHandler; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeySent, (int)RemoteSessionEvent.KeyReceived] += SetStateHandler; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.KeySent] += SetStateHandler; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeySent, (int)RemoteSessionEvent.KeyReceiveFailed] += SetStateToClosedHandler; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.KeySendFailed] += SetStateToClosedHandler;

            // TODO: All these are potential unexpected state transitions.. should have a way to track these calls..
            // should at least put a dbg assert in this handler
            for (int i = 0; i < _stateMachineHandle.GetLength(0); i++)
            {
                for (int j = 0; j < _stateMachineHandle.GetLength(1); j++)
                {
                    if (_stateMachineHandle[i, j] == null)
                    {
                        _stateMachineHandle[i, j] += DoClose;
                    }
                }
            }

            _id = Guid.NewGuid();

            // initialize the state to be Idle: this means it is ready to start connecting.
            SetState(RemoteSessionState.Idle, null);
        }

        #endregion constructor

        /// <summary>
        /// Helper method used by dependents to figure out if the RaiseEvent
        /// method can be short-circuited. This will be useful in cases where
        /// the dependent code wants to take action immediately instead of
        /// going through state machine.
        /// </summary>
        /// <param name="arg"></param>
        internal bool CanByPassRaiseEvent(RemoteSessionStateMachineEventArgs arg)
        {
            if (arg.StateEvent == RemoteSessionEvent.MessageReceived)
            {
                if (_state == RemoteSessionState.Established ||
                    _state == RemoteSessionState.EstablishedAndKeyReceived ||   // TODO - Client session would never get into this state... to be removed
                    _state == RemoteSessionState.EstablishedAndKeySent ||
                    _state == RemoteSessionState.Disconnecting ||               // There can be input data until disconnect has been completed
                    _state == RemoteSessionState.Disconnected)                  // Data can arrive while state machine is transitioning to disconnected
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This method is used by all classes to raise a FSM event.
        /// The method will queue the event. The event queue will be handled in
        /// a thread safe manner by a single dedicated thread.
        /// </summary>
        /// <param name="arg">
        /// This parameter contains the event to be raised.
        /// </param>
        /// <param name="clearQueuedEvents">
        /// optional bool indicating whether to clear currently queued events
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        internal void RaiseEvent(RemoteSessionStateMachineEventArgs arg, bool clearQueuedEvents = false)
        {
            lock (_syncObject)
            {
                s_trace.WriteLine("Event received : {0} for {1}", arg.StateEvent, _id);
                if (clearQueuedEvents)
                {
                    _processPendingEventsQueue.Clear();
                }

                _processPendingEventsQueue.Enqueue(arg);

                if (!_eventsInProcess)
                {
                    _eventsInProcess = true;
                }
                else
                {
                    return;
                }
            }

            ProcessEvents();
        }

        /// <summary>
        /// This is the private version of raising a FSM event.
        /// It can only be called by the dedicated thread that processes the event queue.
        /// It calls the event handler
        /// in the right position of the event handling matrix.
        /// </summary>
        /// <param name="arg">
        /// The parameter contains the actual FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        private void RaiseEventPrivate(RemoteSessionStateMachineEventArgs arg)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arg));
            }

            EventHandler<RemoteSessionStateMachineEventArgs> handler = _stateMachineHandle[(int)State, (int)arg.StateEvent];
            if (handler != null)
            {
                s_trace.WriteLine("Before calling state machine event handler: state = {0}, event = {1}, id = {2}", State, arg.StateEvent, _id);

                handler(this, arg);

                s_trace.WriteLine("After calling state machine event handler: state = {0}, event = {1}, id = {2}", State, arg.StateEvent, _id);
            }
        }

        /// <summary>
        /// This is a readonly property available to all other classes. It gives the FSM state.
        /// Other classes can query for this state. Only the FSM itself can change the state.
        /// </summary>
        internal RemoteSessionState State
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// This event indicates that the FSM state changed.
        /// </summary>
        internal event EventHandler<RemoteSessionStateEventArgs> StateChanged;

        #region Event Handlers

        /// <summary>
        /// This is the handler for CreateSession event of the FSM. This is the beginning of everything
        /// else. From this moment on, the FSM will proceeds step by step to eventually reach
        /// Established state or Closed state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If the parameter <paramref name="arg"/> is null.
        /// </exception>
        private void DoCreateSession(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            using (s_trace.TraceEventHandlers())
            {
                Dbg.Assert(_state == RemoteSessionState.Idle,
                    "State needs to be idle to start connection");
                Dbg.Assert(_state != RemoteSessionState.ClosingConnection ||
                           _state != RemoteSessionState.Closed,
                    "Reconnect after connection is closing or closed is not allowed");

                if (State == RemoteSessionState.Idle)
                {
                    // we are short-circuiting the state by going directly to NegotiationSending..
                    // This will save 1 network trip just to establish a connection.
                    // Raise the event for sending the negotiation
                    RemoteSessionStateMachineEventArgs sendingArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSending);
                    RaiseEvent(sendingArg);
                }
            }
        }

        /// <summary>
        /// This is the handler for ConnectSession event of the FSM. This is the beginning of everything
        /// else. From this moment on, the FSM will proceeds step by step to eventually reach
        /// Established state or Closed state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="PSArgumentNullException">
        /// If the parameter <paramref name="arg"/> is null.
        /// </exception>
        private void DoConnectSession(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            using (s_trace.TraceEventHandlers())
            {
                Dbg.Assert(_state == RemoteSessionState.Idle,
                    "State needs to be idle to start connection");

                if (State == RemoteSessionState.Idle)
                {
                    // We need to send negotiation and connect algorithm related info
                    // Change state to let other DSHandlers add appropriate messages to be piggybacked on transport's Create payload
                    RemoteSessionStateMachineEventArgs sendingArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.NegotiationSendingOnConnect);
                    RaiseEvent(sendingArg);
                }
            }
        }

        /// <summary>
        /// This is the handler for NegotiationSending event.
        /// It sets the new state to be NegotiationSending and
        /// calls data structure handler to send the negotiation packet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="arg"/> is null.
        /// </exception>
        private void DoNegotiationSending(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            if (arg.StateEvent == RemoteSessionEvent.NegotiationSending)
            {
                SetState(RemoteSessionState.NegotiationSending, null);
            }
            else if (arg.StateEvent == RemoteSessionEvent.NegotiationSendingOnConnect)
            {
                SetState(RemoteSessionState.NegotiationSendingOnConnect, null);
            }
            else
            {
                Dbg.Assert(false, "NegotiationSending called on wrong event");
            }
        }

        private void DoDisconnectDuringKeyExchange(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            // set flag to indicate Disconnect request queue up
            _pendingDisconnect = true;
        }

        private void DoDisconnect(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            SetState(RemoteSessionState.Disconnecting, null);
        }

        private void DoReconnect(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            SetState(RemoteSessionState.Reconnecting, null);
        }

        private void DoRCDisconnectStarted(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            if (State != RemoteSessionState.Disconnecting &&
                State != RemoteSessionState.Disconnected)
            {
                SetState(RemoteSessionState.RCDisconnecting, null);
            }
        }

        /// <summary>
        /// This is the handler for Close event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="arg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="arg"/> does not contain remote data.
        /// </exception>
        private void DoClose(object sender, RemoteSessionStateMachineEventArgs arg)
        {
            using (s_trace.TraceEventHandlers())
            {
                RemoteSessionState oldState = _state;

                switch (oldState)
                {
                    case RemoteSessionState.ClosingConnection:
                    case RemoteSessionState.Closed:
                        // do nothing
                        break;

                    case RemoteSessionState.Connecting:
                    case RemoteSessionState.Connected:
                    case RemoteSessionState.Established:
                    case RemoteSessionState.EstablishedAndKeyReceived:  // TODO - Client session would never get into this state... to be removed
                    case RemoteSessionState.EstablishedAndKeySent:
                    case RemoteSessionState.NegotiationReceived:
                    case RemoteSessionState.NegotiationSent:
                    case RemoteSessionState.NegotiationSending:
                    case RemoteSessionState.Disconnected:
                    case RemoteSessionState.Disconnecting:
                    case RemoteSessionState.Reconnecting:
                    case RemoteSessionState.RCDisconnecting:
                        SetState(RemoteSessionState.ClosingConnection, arg.Reason);
                        break;

                    case RemoteSessionState.Idle:
                    case RemoteSessionState.UndefinedState:
                    default:
                        PSRemotingTransportException forceClosedException = new PSRemotingTransportException(arg.Reason, RemotingErrorIdStrings.ForceClosed);
                        SetState(RemoteSessionState.Closed, forceClosedException);
                        break;
                }

                CleanAll();
            }
        }

        /// <summary>
        /// Handles a fatal error message. Throws a well defined error message,
        /// which contains the reason for the fatal error as an inner exception.
        /// This way the internal details are not surfaced to the user.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void DoFatal(object sender, RemoteSessionStateMachineEventArgs eventArgs)
        {
            PSRemotingDataStructureException fatalError =
                new PSRemotingDataStructureException(eventArgs.Reason, RemotingErrorIdStrings.FatalErrorCausingClose);

            RemoteSessionStateMachineEventArgs closeEvent =
                new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close, fatalError);

            RaiseEvent(closeEvent);
        }

        #endregion Event Handlers

        private static void CleanAll()
        {
        }

        /// <summary>
        /// Sets the state of the state machine. Since only
        /// one thread can be manipulating the state at a time
        /// the state is not synchronized.
        /// </summary>
        /// <param name="newState">New state of the state machine.</param>
        /// <param name="reason">reason why the state machine is set
        /// to the new state</param>
        private void SetState(RemoteSessionState newState, Exception reason)
        {
            RemoteSessionState oldState = _state;

            if (newState != oldState)
            {
                _state = newState;
                s_trace.WriteLine("state machine state transition: from state {0} to state {1}", oldState, _state);

                RemoteSessionStateInfo stateInfo = new RemoteSessionStateInfo(_state, reason);
                RemoteSessionStateEventArgs sessionStateEventArg = new RemoteSessionStateEventArgs(stateInfo);

                _clientRemoteSessionStateChangeQueue.Enqueue(sessionStateEventArg);
            }
        }
    }
}
