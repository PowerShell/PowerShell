// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class implements a Finite State Machine (FSM) to control the remote connection on the server side.
    /// There is a similar but not identical FSM on the client side for this connection.
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
    internal class ServerRemoteSessionDSHandlerStateMachine
    {
        [TraceSourceAttribute("ServerRemoteSessionDSHandlerStateMachine", "ServerRemoteSessionDSHandlerStateMachine")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("ServerRemoteSessionDSHandlerStateMachine", "ServerRemoteSessionDSHandlerStateMachine");

        private readonly ServerRemoteSession _session;
        private readonly object _syncObject;

        private readonly Queue<RemoteSessionStateMachineEventArgs> _processPendingEventsQueue
            = new Queue<RemoteSessionStateMachineEventArgs>();

        // whether some thread is actively processing events
        // in a loop. If this is set then other threads
        // should simply add to the queue and not attempt
        // at processing the events in the queue. This will
        // guarantee that events will always be serialized
        // and processed
        private bool _eventsInProcess = false;

        private readonly EventHandler<RemoteSessionStateMachineEventArgs>[,] _stateMachineHandle;
        private RemoteSessionState _state;

        /// <summary>
        /// Timer used for key exchange.
        /// </summary>
        private Timer _keyExchangeTimer;

        #region Constructors

        /// <summary>
        /// This constructor instantiates a FSM object for the server side to control the remote connection.
        /// It initializes the event handling matrix with event handlers.
        /// It sets the initial state of the FSM to be Idle.
        /// </summary>
        /// <param name="session">
        /// This is the remote session object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        internal ServerRemoteSessionDSHandlerStateMachine(ServerRemoteSession session)
        {
            if (session == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(session));
            }

            _session = session;
            _syncObject = new object();

            _stateMachineHandle = new EventHandler<RemoteSessionStateMachineEventArgs>[(int)RemoteSessionState.MaxState, (int)RemoteSessionEvent.MaxEvent];

            for (int i = 0; i < _stateMachineHandle.GetLength(0); i++)
            {
                _stateMachineHandle[i, (int)RemoteSessionEvent.FatalError] += DoFatalError;

                _stateMachineHandle[i, (int)RemoteSessionEvent.Close] += DoClose;
                _stateMachineHandle[i, (int)RemoteSessionEvent.CloseFailed] += DoCloseFailed;
                _stateMachineHandle[i, (int)RemoteSessionEvent.CloseCompleted] += DoCloseCompleted;

                _stateMachineHandle[i, (int)RemoteSessionEvent.NegotiationTimeout] += DoNegotiationTimeout;

                _stateMachineHandle[i, (int)RemoteSessionEvent.SendFailed] += DoSendFailed;

                _stateMachineHandle[i, (int)RemoteSessionEvent.ReceiveFailed] += DoReceiveFailed;
                _stateMachineHandle[i, (int)RemoteSessionEvent.ConnectSession] += DoConnect;
            }

            _stateMachineHandle[(int)RemoteSessionState.Idle, (int)RemoteSessionEvent.CreateSession] += DoCreateSession;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationPending, (int)RemoteSessionEvent.NegotiationReceived] += DoNegotiationReceived;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationReceived, (int)RemoteSessionEvent.NegotiationSending] += DoNegotiationSending;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSending, (int)RemoteSessionEvent.NegotiationSendCompleted] += DoNegotiationCompleted;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSent, (int)RemoteSessionEvent.NegotiationCompleted] += DoEstablished;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationSent, (int)RemoteSessionEvent.NegotiationPending] += DoNegotiationPending;

            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.MessageReceived] += DoMessageReceived;

            _stateMachineHandle[(int)RemoteSessionState.NegotiationReceived, (int)RemoteSessionEvent.NegotiationFailed] += DoNegotiationFailed;

            _stateMachineHandle[(int)RemoteSessionState.Connecting, (int)RemoteSessionEvent.ConnectFailed] += DoConnectFailed;

            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeyReceived] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeyRequested] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.Established, (int)RemoteSessionEvent.KeyReceiveFailed] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.KeyReceived] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.KeySent] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyRequested, (int)RemoteSessionEvent.KeyReceiveFailed] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyReceived, (int)RemoteSessionEvent.KeySendFailed] += DoKeyExchange;
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyReceived, (int)RemoteSessionEvent.KeySent] += DoKeyExchange;

            // with connect, a new client can negotiate a key change to a server that has already negotiated key exchange with a previous client
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyExchanged, (int)RemoteSessionEvent.KeyReceived] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyExchanged, (int)RemoteSessionEvent.KeyRequested] += DoKeyExchange; //
            _stateMachineHandle[(int)RemoteSessionState.EstablishedAndKeyExchanged, (int)RemoteSessionEvent.KeyReceiveFailed] += DoKeyExchange; //

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

            // Initially, set state to Idle
            SetState(RemoteSessionState.Idle, null);
        }

        #endregion Constructors

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
                    _state == RemoteSessionState.EstablishedAndKeySent || // server session will never be in this state.. TODO- remove this
                    _state == RemoteSessionState.EstablishedAndKeyReceived ||
                    _state == RemoteSessionState.EstablishedAndKeyExchanged)
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
        /// <param name="fsmEventArg">
        /// This parameter contains the event to be raised.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        internal void RaiseEvent(RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            // make sure only one thread is processing events.
            lock (_syncObject)
            {
                s_trace.WriteLine("Event received : {0}", fsmEventArg.StateEvent);
                _processPendingEventsQueue.Enqueue(fsmEventArg);

                if (_eventsInProcess)
                {
                    return;
                }

                _eventsInProcess = true;
            }

            ProcessEvents();

            // currently server state machine doesn't raise events
            // this will allow server state machine to raise events.
            // RaiseStateMachineEvents();
        }

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

                RaiseEventPrivate(eventArgs);
            } while (_eventsInProcess);
        }

        /// <summary>
        /// This is the private version of raising a FSM event.
        /// It can only be called by the dedicated thread that processes the event queue.
        /// It calls the event handler
        /// in the right position of the event handling matrix.
        /// </summary>
        /// <param name="fsmEventArg">
        /// The parameter contains the actual FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter is null.
        /// </exception>
        private void RaiseEventPrivate(RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            if (fsmEventArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
            }

            EventHandler<RemoteSessionStateMachineEventArgs> handler = _stateMachineHandle[(int)_state, (int)fsmEventArg.StateEvent];
            if (handler != null)
            {
                s_trace.WriteLine("Before calling state machine event handler: state = {0}, event = {1}", _state, fsmEventArg.StateEvent);

                handler(this, fsmEventArg);

                s_trace.WriteLine("After calling state machine event handler: state = {0}, event = {1}", _state, fsmEventArg.StateEvent);
            }
        }

        #region Event Handlers

        /// <summary>
        /// This is the handler for Start event of the FSM. This is the beginning of everything
        /// else. From this moment on, the FSM will proceeds step by step to eventually reach
        /// Established state or Closed state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoCreateSession(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.CreateSession, "StateEvent must be CreateSession");
                Dbg.Assert(_state == RemoteSessionState.Idle, "DoCreateSession cannot only be called in Idle state");

                DoNegotiationPending(sender, fsmEventArg);
            }
        }

        /// <summary>
        /// This is the handler for NegotiationPending event.
        /// NegotiationPending state can be in reached in the following cases
        /// 1. From Idle to NegotiationPending (during startup)
        /// 2. From Negotiation(Response)Sent to NegotiationPending.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoNegotiationPending(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert((_state == RemoteSessionState.Idle) || (_state == RemoteSessionState.NegotiationSent),
                    "DoNegotiationPending can only occur when the state is Idle or NegotiationSent.");

                SetState(RemoteSessionState.NegotiationPending, null);
            }
        }

        /// <summary>
        /// This is the handler for the NegotiationReceived event.
        /// It sets the new state to be NegotiationReceived.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="fsmEventArg"/> is not NegotiationReceived event or it does not hold the
        /// client negotiation packet.
        /// </exception>
        private void DoNegotiationReceived(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationReceived, "StateEvent must be NegotiationReceived");
                Dbg.Assert(fsmEventArg.RemoteSessionCapability != null, "RemoteSessioncapability must be non-null");
                Dbg.Assert(_state == RemoteSessionState.NegotiationPending, "state must be in NegotiationPending state");

                if (fsmEventArg.StateEvent != RemoteSessionEvent.NegotiationReceived)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                if (fsmEventArg.RemoteSessionCapability == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                SetState(RemoteSessionState.NegotiationReceived, null);
            }
        }

        /// <summary>
        /// This is the handler for NegotiationSending event.
        /// It sets the new state to be NegotiationSending, and sends the server side
        /// negotiation packet by queuing it on the output queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoNegotiationSending(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            if (fsmEventArg == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
            }

            Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationSending, "Event must be NegotiationSending");
            Dbg.Assert(_state == RemoteSessionState.NegotiationReceived, "State must be NegotiationReceived");

            SetState(RemoteSessionState.NegotiationSending, null);

            _session.SessionDataStructureHandler.SendNegotiationAsync();
        }

        /// <summary>
        /// This is the handler for NegotiationSendCompleted event.
        /// It sets the new state to be NegotiationSent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoNegotiationCompleted(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(_state == RemoteSessionState.NegotiationSending, "State must be NegotiationSending");
                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationSendCompleted, "StateEvent must be NegotiationSendCompleted");

                SetState(RemoteSessionState.NegotiationSent, null);
            }
        }

        /// <summary>
        /// This is the handler for the NegotiationCompleted event.
        /// It sets the new state to be Established. It turns off the negotiation timeout timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoEstablished(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(_state == RemoteSessionState.NegotiationSent, "State must be NegotiationReceived");
                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationCompleted, "StateEvent must be NegotiationCompleted");

                if (fsmEventArg.StateEvent != RemoteSessionEvent.NegotiationCompleted)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                if (_state != RemoteSessionState.NegotiationSent)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                SetState(RemoteSessionState.Established, null);
            }
        }

        /// <summary>
        /// This is the handler for MessageReceived event. It dispatches the data to various components
        /// that uses the data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="fsmEventArg"/> does not contain remote data.
        /// </exception>
        internal void DoMessageReceived(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                if (fsmEventArg.RemoteData == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                Dbg.Assert(_state == RemoteSessionState.Established ||
                           _state == RemoteSessionState.EstablishedAndKeyExchanged ||
                           _state == RemoteSessionState.EstablishedAndKeyReceived ||
                           _state == RemoteSessionState.EstablishedAndKeySent,  // server session will never be in this state.. TODO- remove this
                           "State must be Established or EstablishedAndKeySent or EstablishedAndKeyReceived or EstablishedAndKeyExchanged");

                RemotingTargetInterface targetInterface = fsmEventArg.RemoteData.TargetInterface;
                RemotingDataType dataType = fsmEventArg.RemoteData.DataType;

                Guid clientRunspacePoolId;
                ServerRunspacePoolDriver runspacePoolDriver;
                // string errorMessage = null;

                RemoteDataEventArgs remoteDataForSessionArg = null;

                switch (targetInterface)
                {
                    case RemotingTargetInterface.Session:
                        {
                            switch (dataType)
                            {
                                // GETBACK
                                case RemotingDataType.CreateRunspacePool:
                                    remoteDataForSessionArg = new RemoteDataEventArgs(fsmEventArg.RemoteData);
                                    _session.SessionDataStructureHandler.RaiseDataReceivedEvent(remoteDataForSessionArg);
                                    break;

                                default:
                                    Dbg.Assert(false, "Should never reach here");
                                    break;
                            }
                        }

                        break;

                    case RemotingTargetInterface.RunspacePool:
                        // GETBACK
                        clientRunspacePoolId = fsmEventArg.RemoteData.RunspacePoolId;
                        runspacePoolDriver = _session.GetRunspacePoolDriver(clientRunspacePoolId);

                        if (runspacePoolDriver != null)
                        {
                            runspacePoolDriver.DataStructureHandler.ProcessReceivedData(fsmEventArg.RemoteData);
                        }
                        else
                        {
                            s_trace.WriteLine(@"Server received data for Runspace (id: {0}),
                                but the Runspace cannot be found", clientRunspacePoolId);

                            PSRemotingDataStructureException reasonOfFailure = new
                                PSRemotingDataStructureException(RemotingErrorIdStrings.RunspaceCannotBeFound,
                                    clientRunspacePoolId);
                            RemoteSessionStateMachineEventArgs runspaceNotFoundArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.FatalError, reasonOfFailure);
                            RaiseEvent(runspaceNotFoundArg);
                        }

                        break;

                    case RemotingTargetInterface.PowerShell:
                        clientRunspacePoolId = fsmEventArg.RemoteData.RunspacePoolId;
                        runspacePoolDriver = _session.GetRunspacePoolDriver(clientRunspacePoolId);

                        runspacePoolDriver.DataStructureHandler.DispatchMessageToPowerShell(fsmEventArg.RemoteData);
                        break;

                    default:
                        s_trace.WriteLine("Server received data unknown targetInterface: {0}", targetInterface);

                        PSRemotingDataStructureException reasonOfFailure2 = new PSRemotingDataStructureException(RemotingErrorIdStrings.ReceivedUnsupportedRemotingTargetInterfaceType, targetInterface);
                        RemoteSessionStateMachineEventArgs unknownTargetArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.FatalError, reasonOfFailure2);
                        RaiseEvent(unknownTargetArg);
                        break;
                }
            }
        }

        /// <summary>
        /// This is the handler for ConnectFailed event. In this implementation, this should never
        /// happen. This is because the IO channel is stdin/stdout/stderr redirection.
        /// Therefore, the connection is a dummy operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="fsmEventArg"/> does not contain ConnectFailed event.
        /// </exception>
        private void DoConnectFailed(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.ConnectFailed, "StateEvent must be ConnectFailed");

                if (fsmEventArg.StateEvent != RemoteSessionEvent.ConnectFailed)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                Dbg.Assert(_state == RemoteSessionState.Connecting, "session State must be Connecting");

                // This method should not be called in this implementation.
                throw PSTraceSource.NewInvalidOperationException();
            }
        }

        /// <summary>
        /// This is the handler for FatalError event. It directly calls the DoClose, which
        /// is the Close event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> does not contains FatalError event.
        /// </exception>
        private void DoFatalError(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.FatalError, "StateEvent must be FatalError");

                if (fsmEventArg.StateEvent != RemoteSessionEvent.FatalError)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fsmEventArg));
                }

                DoClose(this, fsmEventArg);
            }
        }

        /// <summary>
        /// Handle connect event - this is raised when a new client tries to connect to an existing session
        /// No changes to state. Calls into the session to handle any post connect operations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg"></param>
        private void DoConnect(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            Dbg.Assert(_state != RemoteSessionState.Idle, "session should not be in idle state when SessionConnect event is queued");
            if ((_state != RemoteSessionState.Closed) && (_state != RemoteSessionState.ClosingConnection))
            {
                _session.HandlePostConnect();
            }
        }

        /// <summary>
        /// This is the handler for Close event. It closes the connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoClose(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

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
                    case RemoteSessionState.EstablishedAndKeySent:  // server session will never be in this state.. TODO- remove this
                    case RemoteSessionState.EstablishedAndKeyReceived:
                    case RemoteSessionState.EstablishedAndKeyExchanged:
                    case RemoteSessionState.NegotiationReceived:
                    case RemoteSessionState.NegotiationSent:
                    case RemoteSessionState.NegotiationSending:
                        SetState(RemoteSessionState.ClosingConnection, fsmEventArg.Reason);
                        _session.SessionDataStructureHandler.CloseConnectionAsync(fsmEventArg.Reason);
                        break;

                    case RemoteSessionState.Idle:
                    case RemoteSessionState.UndefinedState:
                    default:
                        Exception forcedCloseException = new PSRemotingTransportException(fsmEventArg.Reason, RemotingErrorIdStrings.ForceClosed);
                        SetState(RemoteSessionState.Closed, forcedCloseException);
                        break;
                }

                CleanAll();
            }
        }

        /// <summary>
        /// This is the handler for CloseFailed event.
        /// It simply force the new state to be Closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoCloseFailed(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.CloseFailed, "StateEvent must be CloseFailed");

                RemoteSessionState stateBeforeTransition = _state;

                SetState(RemoteSessionState.Closed, fsmEventArg.Reason);

                // ignore
                CleanAll();
            }
        }

        /// <summary>
        /// This is the handler for CloseCompleted event. It sets the new state to be Closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoCloseCompleted(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.CloseCompleted, "StateEvent must be CloseCompleted");

                SetState(RemoteSessionState.Closed, fsmEventArg.Reason);
                // Close the session only after changing the state..this way
                // state machine will not process anything.
                _session.Close(fsmEventArg);
                CleanAll();
            }
        }

        /// <summary>
        /// This is the handler for NegotiationFailed event.
        /// It raises a Close event to trigger the connection to be shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoNegotiationFailed(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationFailed, "StateEvent must be NegotiationFailed");

                RemoteSessionStateMachineEventArgs closeArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);

                RaiseEventPrivate(closeArg);
            }
        }

        /// <summary>
        /// This is the handler for NegotiationTimeout event.
        /// If the connection is already Established, it ignores this event.
        /// Otherwise, it raises a Close event to trigger a close of the connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoNegotiationTimeout(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.NegotiationTimeout, "StateEvent must be NegotiationTimeout");

                if (_state == RemoteSessionState.Established)
                {
                    // ignore
                    return;
                }

                RemoteSessionStateMachineEventArgs closeArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);

                RaiseEventPrivate(closeArg);
            }
        }

        /// <summary>
        /// This is the handler for SendFailed event.
        /// This is an indication that the wire layer IO is no longer connected. So it raises
        /// a Close event to trigger a connection shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoSendFailed(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.SendFailed, "StateEvent must be SendFailed");

                RemoteSessionStateMachineEventArgs closeArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);

                RaiseEventPrivate(closeArg);
            }
        }

        /// <summary>
        /// This is the handler for ReceivedFailed event.
        /// This is an indication that the wire layer IO is no longer connected. So it raises
        /// a Close event to trigger a connection shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsmEventArg">
        /// This parameter contains the FSM event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="fsmEventArg"/> is null.
        /// </exception>
        private void DoReceiveFailed(object sender, RemoteSessionStateMachineEventArgs fsmEventArg)
        {
            using (s_trace.TraceEventHandlers())
            {
                if (fsmEventArg == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(fsmEventArg));
                }

                Dbg.Assert(fsmEventArg.StateEvent == RemoteSessionEvent.ReceiveFailed, "StateEvent must be ReceivedFailed");

                RemoteSessionStateMachineEventArgs closeArg = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.Close);

                RaiseEventPrivate(closeArg);
            }
        }

        /// <summary>
        /// This method contains all the logic for handling the state machine
        /// for key exchange. All the different scenarios are covered in this.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Event args.</param>
        private void DoKeyExchange(object sender, RemoteSessionStateMachineEventArgs eventArgs)
        {
            // There are corner cases with disconnect that can result in client receiving outdated key exchange packets
            // ***TODO*** Deal with this on the client side. Key exchange packets should have additional information
            // that identify the context of negotiation. Just like callId in SetMax and SetMinRunspaces messages
            Dbg.Assert(_state >= RemoteSessionState.Established,
                "Key Receiving can only be raised after reaching the Established state");

            switch (eventArgs.StateEvent)
            {
                case RemoteSessionEvent.KeyReceived:
                    {
                        // does the server ever start key exchange process??? This may not be required
                        if (_state == RemoteSessionState.EstablishedAndKeyRequested)
                        {
                            // reset the timer
                            Timer tmp = Interlocked.Exchange(ref _keyExchangeTimer, null);
                            tmp?.Dispose();
                        }

                        // the key import would have been done
                        // set state accordingly
                        SetState(RemoteSessionState.EstablishedAndKeyReceived, eventArgs.Reason);

                        // you need to send an encrypted session key to the client
                        _session.SendEncryptedSessionKey();
                    }

                    break;

                case RemoteSessionEvent.KeySent:
                    {
                        if (_state == RemoteSessionState.EstablishedAndKeyReceived)
                        {
                            // key exchange is now complete
                            SetState(RemoteSessionState.EstablishedAndKeyExchanged, eventArgs.Reason);
                        }
                    }

                    break;

                case RemoteSessionEvent.KeyRequested:
                    {
                        if ((_state == RemoteSessionState.Established) || (_state == RemoteSessionState.EstablishedAndKeyExchanged))
                        {
                            // the key has been sent set state accordingly
                            SetState(RemoteSessionState.EstablishedAndKeyRequested, eventArgs.Reason);

                            // start the timer
                            _keyExchangeTimer = new Timer(HandleKeyExchangeTimeout, null, BaseTransportManager.ServerDefaultKeepAliveTimeoutMs, Timeout.Infinite);
                        }
                    }

                    break;

                case RemoteSessionEvent.KeyReceiveFailed:
                    {
                        if ((_state == RemoteSessionState.Established) || (_state == RemoteSessionState.EstablishedAndKeyExchanged))
                        {
                            return;
                        }

                        DoClose(this, eventArgs);
                    }

                    break;

                case RemoteSessionEvent.KeySendFailed:
                    {
                        DoClose(this, eventArgs);
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
            Dbg.Assert(_state == RemoteSessionState.EstablishedAndKeyRequested, "timeout should only happen when waiting for a key");

            Timer tmp = Interlocked.Exchange(ref _keyExchangeTimer, null);
            tmp?.Dispose();

            PSRemotingDataStructureException exception =
                new PSRemotingDataStructureException(RemotingErrorIdStrings.ServerKeyExchangeFailed);

            RaiseEvent(new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceiveFailed, exception));
        }

        #endregion Event Handlers

        /// <summary>
        /// This method is designed to be a cleanup routine after the connection is closed.
        /// It can also be used for graceful shutdown of the server process, which is not currently
        /// implemented.
        /// </summary>
        private static void CleanAll()
        {
        }

        /// <summary>
        /// Set the FSM state to a new state.
        /// </summary>
        /// <param name="newState">
        /// The new state.
        /// </param>
        /// <param name="reason">
        /// Optional parameter that can provide additional information. This is currently not used.
        /// </param>
        private void SetState(RemoteSessionState newState, Exception reason)
        {
            RemoteSessionState oldState = _state;
            if (newState != oldState)
            {
                _state = newState;
                s_trace.WriteLine("state machine state transition: from state {0} to state {1}", oldState, _state);
            }
            // TODO: else should we close the session here?
        }
    }
}
