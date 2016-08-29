/*
 * Copyright (c) 2010 Microsoft Corporation. All rights reserved
 */
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using Microsoft.PowerShell.Activities;
using System.Management.Automation.Tracing;
using System.Management.Automation.PerformanceData;

namespace Microsoft.PowerShell.Workflow
{
    #region Connection

    /// <summary>
    /// class the contains a remote connection (runspace) and associated
    /// data for managing the same
    /// </summary>
    /// <remarks>the only reason this class has an internal scope is for
    /// test purposes. Else this can be a private class inside 
    /// connection manager</remarks>
    internal class Connection
    {
        private Runspace _runspace;
        private bool _busy;
        private bool _idle;
        private readonly object _syncObject = new object();
        private static readonly EventArgs EventArgs = new EventArgs();
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly Tracer _structuredTracer = new Tracer();
        private static readonly PSPerfCountersMgr _perfCountersMgr = PSPerfCountersMgr.Instance;
        private bool _readyForDisconnect = false;
        private bool _readyForReconnect = false;
        private ConnectionManager _manager;

        /// <summary>
        /// retry interval cannot be 0, so initial value is 1
        /// </summary>
        private uint _retryInterval = 1;

        internal uint RetryAttempt { get; set; }
        internal uint RetryCount { get; set; }
        internal GetRunspaceAsyncResult AsyncResult { get; set; }
        internal WSManConnectionInfo ConnectionInfo { get; set; }
        
        internal event EventHandler CloseCompleted;
        internal event EventHandler OpenCompleted;
        internal event EventHandler DisconnectCompleted;
        internal event EventHandler ReconnectCompleted;

        private const int Open = 1;
        private const int Close = 2;
        private const int Disconnect = 3;
        private const int Reconnect = 4;

        internal Connection(ConnectionManager manager)
        {
            _manager = manager;
            _tracer.WriteMessage("PSW Conn: Creating new connection");
        }

        internal bool DisconnectedIntentionally { get; set; }

        internal Runspace Runspace
        {
            get { return _runspace; }
        }

        internal Guid InstanceId
        {
            get { return _instanceId; }
        }

        internal bool Idle
        {
            get
            {
                lock (_syncObject)
                {
                    return _idle;
                }
            }
            set
            {
                lock (_syncObject)
                {
                    _idle = value;
                }
            }
        }
        internal bool ReadyForDisconnect
        {
            get
            {
                lock (_syncObject)
                {
                    return _readyForDisconnect;
                }
            }

            set
            {
                lock (_syncObject)
                {
                    _readyForDisconnect = value;

                    if (_readyForDisconnect)
                        _readyForReconnect = false;
                }
            }
        }

        internal bool ReadyForReconnect
        {
            get
            {
                lock (_syncObject)
                {
                    return _readyForReconnect;
                }
            }
            set
            {
                lock (_syncObject)
                {
                    _readyForReconnect = value;

                    if (_readyForReconnect)
                        _readyForDisconnect = false;
                }
            }
        }

        internal bool Busy 
        {
            get
            {
                lock (_syncObject)
                {
                    return _busy;
                }
            }
            set
            {
                lock (_syncObject)
                {
                    _busy = value;

                    if (_busy)
                    {
                        _idle = false;
                    }

                    // connection is set to busy by the connection manager
                    // when it is assigning the connection to a new activity
                    // then the connection should be marked as not ready for
                    // disconnect since the activity has to signal the same
                    // when it is not busy we do not want to disconnect idle
                    // connections, hence it needs to be marked as not ready
                    // for disconnect
                    _readyForDisconnect = false;
                    _readyForReconnect = false;
                }
            }
        }

        internal uint RetryInterval
        {
            get { return _retryInterval; }
            set 
            {
                _retryInterval = value;
                if (_retryInterval == 0) _retryInterval = 1;
            }
        }

        private bool CheckAndReconnectAfterCrashOrShutdown(WSManConnectionInfo connectionInfo)
        {
            bool connectAsyncSucceeded = false;

            RunCommandsArguments args = this.AsyncResult.AsyncState as RunCommandsArguments;
            if (args != null)
            {
                Guid runspaceId = args.ImplementationContext.DisconnectedRunspaceInstanceId;

                ActivityImplementationContext implementationContext = args.ImplementationContext;
                System.Management.Automation.PowerShell currentPowerShellInstance = implementationContext.PowerShellInstance;
                PSActivityContext psActivityContext = args.PSActivityContext;

                // Non-empty guid represents that args.ImplementationContext.EnableRemotingActivityAutoResume is true
                if (!runspaceId.Equals(Guid.Empty))
                {
                    Runspace[] runspaces = null;

                    try
                    {
                        // Queries the server for disconnected runspaces. 
                        // Each runspace object in the returned array is in the Disconnected state and can be
                        // connected to the server by calling the Connect() or ConnectAsync() methods on the runspace.
                        runspaces = Runspace.GetRunspaces(connectionInfo);
                    }
                    catch (System.Management.Automation.RuntimeException)
                    {
                        _tracer.WriteMessage("PSW Conn: Disconnected Remote Runspace is not available. Runspace Instance Id: " + runspaceId.ToString());
                    }

                    if (runspaces != null)
                    {
                        // In local machine case, remote runspace with the specific id will be not be available after machine shutdown.
                        // Remoterunspace on local machine may be available if only current process is crashed or suspended forcefully.
                        // OpenAsync() will takes care of create new remote runspace if disconnected runspace is not available.

                        Runspace foundRunspace = runspaces.FirstOrDefault(currentRunspace => currentRunspace.InstanceId.Equals(runspaceId));

                        if (foundRunspace != null && foundRunspace.RunspaceStateInfo.State == RunspaceState.Disconnected)
                        {
                            System.Management.Automation.PowerShell disconnectedPowerShell = null;

                            try
                            {
                                // When disconnectedPowerShell is not available PSInvalidOperationException will be thrown and finally block takes care of releasing the runspace.
                                // And OpenAsync() will create a new remote runspace if disconnected runspace is not available.
                                disconnectedPowerShell = foundRunspace.CreateDisconnectedPowerShell();

                                // Add stream handler and connect to disconnected remote runspace
                                PSActivity.AddHandlersToStreams(disconnectedPowerShell, args);
                                disconnectedPowerShell.ConnectAsync(args.Output, PSActivity.PowerShellInvocationCallback, args);


                                // Assign the disconnected powershell to ImplementationContext and dispose the existing powershell instance.
                                implementationContext.PowerShellInstance = disconnectedPowerShell;

                                // Since we are disposing the current command, remove this command from the list
                                // of running commands.
                                lock (psActivityContext.runningCommands)
                                {
                                    RetryCount retryCount = psActivityContext.runningCommands[currentPowerShellInstance];
                                    psActivityContext.runningCommands.Remove(currentPowerShellInstance);
                                    psActivityContext.runningCommands[disconnectedPowerShell] = retryCount;
                                }
                                currentPowerShellInstance.Dispose();

                                _runspace = foundRunspace;
                                _runspace.StateChanged += RunspaceStateChanged;

                                connectAsyncSucceeded = true;
                            }
                            catch (PSInvalidOperationException)
                            {
                                _tracer.WriteMessage("PSW Conn: Disconnected PowerShell is not available. Runspace Instance Id: " + foundRunspace.InstanceId.ToString());
                            }
                            finally
                            {
                                // release the runspace if reconnect did not happen.
                                if (connectAsyncSucceeded == false && foundRunspace != null)
                                {
                                    try
                                    {
                                        // Remove stream handlers on disconnectedPowerShell
                                        // Non null disconnectedPowerShell represents that handlers were added on disconnectedPowerShell and an exception happened during ConnectAsync.
                                        if (disconnectedPowerShell != null)
                                            PSActivity.RemoveHandlersFromStreams(disconnectedPowerShell, args);

                                        foundRunspace.Close();
                                        foundRunspace.Dispose();
                                    }
                                    catch (Exception)
                                    {
                                        // Runspace.Close can throw exceptions when Server process has exited or runspace is invalid.
                                        // Ignoring all exceptions.
                                        //
                                        _tracer.WriteMessage("PSW Conn: Disconnected PowerShell is not available. Runspace Instance Id: " + foundRunspace.InstanceId.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return connectAsyncSucceeded;
        }

        internal void OpenAsync()
        {
            // Always create runspace with WSManConnectionInfo.EnableNetworkAccess set to true.
            WSManConnectionInfo copyConnectionInfo = ConnectionInfo.Copy();
            copyConnectionInfo.EnableNetworkAccess = true;

            // If the remote runspace is not available after crash/shutdown, 
            // activity task resume will fall back to reexecuting in a new runspace is assigned for the connection
            if (!CheckAndReconnectAfterCrashOrShutdown(copyConnectionInfo))
            {
                _runspace = RunspaceFactory.CreateRunspace(copyConnectionInfo, null, LocalRunspaceProvider.SharedTypeTable);
                _runspace.StateChanged += RunspaceStateChanged;
                _tracer.WriteMessage("PSW Conn: Calling OpenAsync on runspace");
                _runspace.OpenAsync();
            }
        }

        internal void CloseAsync()
        {
            _tracer.WriteMessage("PSW Conn: Calling CloseAsync on runspace");

            // at this point the runspace can be in a closed or broken state
            // if so then close has to simply raise the event
            // if it was not in a terminal state when checking but happens
            // later but before CloseAsync() is called then the other 
            // call which caused the state change will raise an event
            if (_runspace.RunspaceStateInfo.State == RunspaceState.Broken ||
                _runspace.RunspaceStateInfo.State == RunspaceState.Closed)
            {
                RaiseEvents(Close);
            }
            else
                _runspace.CloseAsync();
        }

        internal void DisconnectAsync()
        {
            bool disconnect = false;

            if (_readyForDisconnect)
            {
                lock (_syncObject)
                {
                    if (_readyForDisconnect)
                    {
                        _readyForDisconnect=false;
                        _readyForReconnect = false;
                        disconnect = true;
                    }
                }
                if (disconnect)
                {
                    _tracer.WriteMessage("PSW Conn: Calling Disconnect Async");
                    _manager.DisconnectCalled();
                    _runspace.DisconnectAsync();
                    return;
                }                
            }
            RaiseEvents(Disconnect);
        }        

        internal void ReconnectAsync()
        {
            bool reconnect = false;

            if (_readyForReconnect)
            {
                lock (_syncObject)
                {
                    if (_readyForReconnect)
                    {
                        _readyForReconnect = false;
                        _readyForDisconnect = false;
                        reconnect = true;
                    }
                }

                if (reconnect)
                {
                    _tracer.WriteMessage("PSW Conn: Calling reconnect async");
                    _manager.ReconnectCalled();
                    _runspace.ConnectAsync();
                    return;
                }
            }
            RaiseEvents(Reconnect);
        }

        private void RunspaceStateChanged(object sender, RunspaceStateEventArgs e)
        {
            _structuredTracer.RunspaceStateChanged(this._instanceId.ToString(), e.RunspaceStateInfo.State.ToString(), string.Empty);
            _tracer.WriteMessage("PSW Conn: runspace state" + e.RunspaceStateInfo.State.ToString());
            switch (e.RunspaceStateInfo.State)
            {
                case RunspaceState.Opening:
                    return;

                case RunspaceState.Closing:
                    return;

                case RunspaceState.Disconnecting:
                    {
                        ReadyForDisconnect = false;
                    }
                    return;

                case RunspaceState.Opened:
                    {
                        // runspace opened successfully, assign it to the asyncresult                        
                        // reset retry counter
                        RetryAttempt = 0;
                        ReadyForReconnect = false;
                        AsyncResult.Connection = this;
                        // this SetAsCompleted will result in the callback to the
                        // activity happening in the WinRM thread
                        AsyncResult.SetAsCompleted(null);
                        RaiseEvents(Open);
                        RaiseEvents(Reconnect);
                    }
                    break;

                case RunspaceState.Broken:
                    {
                        // Dispose the broken runspace before retry
                        DisposeRunspace();

                        // connection attempt failed, retry
                        if (RetryCount > 0 && RetryAttempt < RetryCount)
                        {
                            RetryAttempt++;

                            LogRetryAttempt(RetryAttempt, RetryCount);

                            Timer timer = new Timer { AutoReset = false, Enabled = false, Interval = _retryInterval * 1000 };
                            timer.Elapsed += RetryTimerElapsed;
                            timer.Start();                            
                        }
                        else
                        {
                            // all retries have failed, end the asyncresult with an exception
                            // message
                            Busy = false;

                            lock (_syncObject)
                            {
                                if (AsyncResult != null)
                                {
                                    AsyncResult.Connection = null;
                                    AsyncResult.SetAsCompleted(e.RunspaceStateInfo.Reason);
                                }
                            }

                            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "Disposing broken connection to {0}", _runspace.ConnectionInfo.ComputerName));

                            RaiseEvents(Open);
                            RaiseEvents(Disconnect);
                            RaiseEvents(Close);
                            RaiseEvents(Reconnect);
                        }
                    }
                    break;

                case RunspaceState.Disconnected:
                    {
                        ReadyForReconnect = true;
                        RaiseEvents(Disconnect);
                    }
                    break;

                case RunspaceState.Closed:
                    {
                        DisposeRunspace();
                        RaiseEvents(Close);
                    }
                    break;
            }
        }

        private void LogRetryAttempt(uint retryAttempt, uint retryTotal)
        {
            RunCommandsArguments args = this.AsyncResult.AsyncState as RunCommandsArguments;
            if (args != null)
            {
                PSActivityContext psActivityContext = args.PSActivityContext;

                // Write / Log that an activity retry was required.
                if (psActivityContext.progress != null)
                {
                    string progressActivity = ((System.Activities.Activity)psActivityContext.ActivityObject).DisplayName;

                    if (string.IsNullOrEmpty(progressActivity))
                        progressActivity = psActivityContext.ActivityType.Name;

                    string retryMessage = String.Format(CultureInfo.CurrentCulture, Resources.RetryingConnection, retryAttempt, retryTotal);

                    ProgressRecord progressRecord = new ProgressRecord(0, progressActivity, retryMessage);
                    lock (psActivityContext.progress)
                    {
                        psActivityContext.progress.Add(progressRecord);
                    }
                }
            }
        }

        private void RaiseEvents(int eventType)
        {
            switch (eventType)
            {
                case Open:
                    {
                        if (OpenCompleted != null) OpenCompleted(this, EventArgs);
                    }
                    break;
                case Close:
                    {
                        if (CloseCompleted != null) CloseCompleted(this, EventArgs);
                    }
                    break;
                case Disconnect:
                    {
                        if (DisconnectCompleted != null) DisconnectCompleted(this, EventArgs);
                    }
                    break;
                case Reconnect:
                    {
                        if (ReconnectCompleted != null) ReconnectCompleted(this, EventArgs);
                    }
                    break;
            }
        }

        private void DisposeRunspace()
        {
            _runspace.StateChanged -= RunspaceStateChanged;
            _tracer.WriteMessage("PSW Conn: disposing runspace");
            _runspace.Dispose();
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingConnectionsDisposedCount);
        }

        private void RetryTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // specified retry interval timeout has elapsed, try again
            // dispose the timer
            Timer timer = sender as Timer;
            if (timer != null) timer.Dispose();

            // retry the connection again
            OpenAsync();
        }
    }

    #endregion Connection

    #region GetRunspaceAsyncResult

    /// <summary>
    /// AsyncResult object returned by ConnectionManager
    /// </summary>
    /// <remarks>the only reason this class has an internal scope is for
    /// test purposes. Else this can be a private class inside 
    /// connection manager</remarks>
    internal class GetRunspaceAsyncResult : ConnectionAsyncResult
    {
        internal GetRunspaceAsyncResult(object state, AsyncCallback callback, Guid ownerId) :
            base(state, callback, ownerId)
        {
        }

        internal Connection Connection { get; set; }
    }

    #endregion GetRunspaceAsyncResult

    #region ThrottleOperation

    internal abstract class ThrottleOperation
    {
        internal event EventHandler OperationComplete;
        private static readonly EventArgs EventArgs = new EventArgs();

        internal void RaiseOperationComplete()
        {
            if (OperationComplete != null) OperationComplete(this, EventArgs);
        }

        internal virtual void DoOperation()
        {
            throw new NotImplementedException();
        }
    }

    internal class OpenOperation : ThrottleOperation
    {
        private readonly Connection _connection;

        internal OpenOperation(Connection connection)
        {
            _connection = connection;
            _connection.OpenCompleted += HandleOpenCompleted;
        }

        private void HandleOpenCompleted(object sender, EventArgs eventArgs)
        {
            _connection.OpenCompleted -= HandleOpenCompleted;
            RaiseOperationComplete();
        }

        internal override void DoOperation()
        {
            _connection.OpenAsync();
        }
    }

    internal class CloseOperation : ThrottleOperation
    {
        private readonly Connection _connection;
        internal Connection Connection
        {
            get { return _connection; }
        }

        internal CloseOperation(Connection connection)
        {
            _connection = connection;
            _connection.CloseCompleted += HandleCloseCompleted;
        }

        private void HandleCloseCompleted(object sender, EventArgs eventArgs)
        {
            _connection.CloseCompleted -= HandleCloseCompleted;
            RaiseOperationComplete();
        }

        internal override void DoOperation()
        {
            _connection.CloseAsync();
        }
    }

    internal class CloseOneAndOpenAnotherOperation : ThrottleOperation
    {
        private readonly Connection _connectionToClose;
        private readonly Connection _connectionToOpen;

        internal CloseOneAndOpenAnotherOperation(Connection toClose, Connection toOpen)
        {
            _connectionToClose = toClose;
            _connectionToOpen = toOpen;

            _connectionToClose.CloseCompleted += HandleCloseCompleted;
            _connectionToOpen.OpenCompleted += HandleOpenCompleted;
        }

        private void HandleOpenCompleted(object sender, EventArgs eventArgs)
        {
            _connectionToOpen.OpenCompleted -= HandleOpenCompleted;
            RaiseOperationComplete();
        }

        private void HandleCloseCompleted(object sender, EventArgs eventArgs)
        {
            _connectionToClose.CloseCompleted -= HandleCloseCompleted;
            _connectionToOpen.OpenAsync();
        }

        internal override void DoOperation()
        {
            _connectionToClose.CloseAsync();
        }
    }

    internal class DisconnectOperation : ThrottleOperation
    {
        private readonly Connection _connection;

        internal DisconnectOperation(Connection connection)
        {
            _connection = connection;
            _connection.DisconnectCompleted += HandleDisconnectCompleted;
        }

        private void HandleDisconnectCompleted(object sender, EventArgs eventArgs)
        {
            _connection.DisconnectCompleted -= HandleDisconnectCompleted;
            RaiseOperationComplete();
        }

        internal override void  DoOperation()
        {
            _connection.DisconnectAsync();
        }
    }

    internal class ReconnectOperation : ThrottleOperation
    {
        private readonly Connection _connection;

        internal ReconnectOperation(Connection connection)
        {
            _connection = connection;
            _connection.ReconnectCompleted += HandleReconnectCompleted;
        }

        private void HandleReconnectCompleted(object sender, EventArgs eventArgs)
        {
            _connection.ReconnectCompleted -= HandleReconnectCompleted;
            RaiseOperationComplete();
        }
        
        internal override void DoOperation()
        {
            if (_connection.Runspace.RunspaceStateInfo.State != RunspaceState.Disconnected)
            {
                EventHandler<RunspaceStateEventArgs> HandleRunspaceStateChanged = null;
                HandleRunspaceStateChanged =
                    delegate(object sender, RunspaceStateEventArgs e)
                        {
                            if (e.RunspaceStateInfo.State == RunspaceState.Disconnected)
                            {
                                _connection.ReconnectAsync();
                                _connection.Runspace.StateChanged -= HandleRunspaceStateChanged;
                            }
                        };

                _connection.Runspace.StateChanged += HandleRunspaceStateChanged;

                if (_connection.Runspace.RunspaceStateInfo.State == RunspaceState.Disconnected)
                {
                    _connection.ReconnectAsync();
                    _connection.Runspace.StateChanged -= HandleRunspaceStateChanged;
                }

                return;
            }

            _connection.ReconnectAsync();
        }
    }

    #endregion ThrottleOperation

    internal class ConnectionManager : RunspaceProvider, IDisposable
    {
        #region RequestInfo

        private class RequestInfo
        {
            private WSManConnectionInfo _connectionInfo;
            private uint _retryCount;
            private GetRunspaceAsyncResult _asyncResult;
            private uint _retryInterval;

            internal WSManConnectionInfo ConnectionInfo
            {
                get { return _connectionInfo; }
                set { _connectionInfo = value; }
            }

            internal uint RetryCount
            {
                get { return _retryCount; }
                set { _retryCount = value; }
            }

            internal GetRunspaceAsyncResult AsyncResult
            {
                get { return _asyncResult; }
                set { _asyncResult = value; }
            }

            internal uint RetryInterval
            {
                get { return _retryInterval; }
                set { _retryInterval = value; }
            }
        }

        #endregion RequestInfo

        #region Private members

        // this is the connection pool indexed based on a given machine
        // and a given session configuration
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<Guid,Connection>>> _connectionPool =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>>>();

        // this is the list of computers to which a cleanup is requested
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Tuple<WaitCallback, object>>> _cleanupComputers =
            new ConcurrentDictionary<string, ConcurrentQueue<Tuple<WaitCallback, object>>>();

        private readonly int _idleTimeout;
        private readonly int _maxOutBoundConnections;
        private readonly int _maxConnectedSessions;
        private readonly int _maxDisconnectedSessions;

        private int _isServicing;
        private int _isServicingCallbacks;
        private int _isServicingCleanups;
        private const int Servicing = 1;
        private const int NotServicing = 0;
        private readonly Timer _timer;
        private readonly ConcurrentQueue<RequestInfo> _inComingRequests = new ConcurrentQueue<RequestInfo>();

        private readonly ConcurrentQueue<GetRunspaceAsyncResult> _callbacks =
            new ConcurrentQueue<GetRunspaceAsyncResult>();

        private int _timerFired;
        private const int TimerFired = 1;
        private const int TimerReset = 0;
        private bool _isDisposed;
        private bool _isDisposing; 
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly object _syncObject = new object();
        private static readonly PSPerfCountersMgr _perfCountersMgr = PSPerfCountersMgr.Instance;


        /// <summary>
        /// The servicing thread will release control of processing and the timer
        /// thread will take control and process based on this wait handle
        /// </summary>
        private readonly ManualResetEvent _servicingThreadRelease = new ManualResetEvent(false);

        /// <summary>
        /// The timer thread will release control of processing and the servicing thread
        /// will take control and process based on this wait handle
        /// </summary>
        private readonly ManualResetEvent _timerThreadRelease = new ManualResetEvent(true);

        /// <summary>
        /// This list is assumed to accessed by only the servicing thread and
        /// hence is not designed to be thread safe
        /// </summary>
        private readonly List<RequestInfo> _pendingRequests = new List<RequestInfo>();

        /// <summary>
        /// number of sessions in the connected state
        /// </summary>
        /// <remarks>this is made static in the interest of time
        /// should not be static</remarks>
        //internal static int ConnectedSessionCount = 0;
        private int _connectedSessionCount = 0;

        /// <summary>
        /// number of sessions in the disconnected state
        /// </summary>
        /// <remarks>this is made static in the interest of time
        /// should not be static</remarks>
        //internal static int _disconnectedSessionCount = 0;
        private int _disconnectedSessionCount = 0;

        private int _createdConnections = 0;

        /// <summary>
        /// if we need to check whether runspaces need to be
        /// disconnected
        /// </summary>
        private int _checkForDisconnect = 0;
        private const int CheckForDisconnect = 1;
        private const int DoNotCheckForDisconnect = 0;

        /// <summary>
        /// Map of timers for each machine
        /// </summary>
        private readonly ConcurrentDictionary<Timer, string> _timerMap = new ConcurrentDictionary<Timer, string>();

        #endregion Private Members

        #region Constructors

        internal ConnectionManager(int idleTimeout, int maxOutBoundConnections, int throttleLimit, int maxConnectedSessions, int maxDisconnectedSessions)
        {
            _idleTimeout = idleTimeout;
            _maxOutBoundConnections = maxOutBoundConnections;
            _throttleLimit = throttleLimit;
            _maxConnectedSessions = maxConnectedSessions;
            _maxDisconnectedSessions = maxDisconnectedSessions;

            _timer = new Timer {AutoReset = true, Interval = _idleTimeout, Enabled=false};
            _timer.Elapsed += HandleTimerElapsed;
            _timer.Start();
        }

        #endregion Constructors

        #region Interface Methods

        /// <summary>
        /// Get runspace for the specified connection info to be
        /// used for running commands
        /// </summary>
        /// <param name="connectionInfo">connection info to use</param>
        /// <param name="retryCount">retry count </param>
        /// <param name="retryInterval">retry interval in ms</param>
        /// <returns>remote runspace to use</returns>
        public override Runspace GetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval)
        {
            IAsyncResult asyncResult = BeginGetRunspace(connectionInfo, retryCount, retryInterval, null, null);

            return EndGetRunspace(asyncResult);
        }

        /// <summary>
        /// Begin for obtaining a runspace for the specified ConnectionInfo
        /// </summary>
        /// <param name="connectionInfo">connection info to be used for remote connections</param>
        /// <param name="retryCount">number of times to retry</param>
        /// <param name="callback">optional user defined callback</param>
        /// <param name="state">optional user specified state</param>
        /// <param name="retryInterval">time in milliseconds before the next retry has to be attempted</param>
        /// <returns>async result</returns>
        public override IAsyncResult BeginGetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval, AsyncCallback callback, object state)
        {
            GetRunspaceAsyncResult result = new GetRunspaceAsyncResult(state, callback, Guid.Empty);

            // Create a Request Object and queue the same
            RequestInfo requestInfo = new RequestInfo
                                          {
                                              ConnectionInfo = connectionInfo,
                                              RetryCount = retryCount,
                                              AsyncResult = result,
                                              RetryInterval = retryInterval
                                          };

            _tracer.WriteMessage("PSW ConnMgr: New incoming request for runspace queued");
            _inComingRequests.Enqueue(requestInfo);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingPendingRequestsQueueLength);

            // start servicing thread if required
            CheckAndStartRequiredThreads();

            return result;
        }

        /// <summary>
        /// End for obtaining a runspace for the specified connection info
        /// </summary>
        /// <param name="asyncResult">async result to end on</param>
        /// <returns>remote runspace to invoke commands on</returns>
        public override Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            GetRunspaceAsyncResult result = asyncResult as GetRunspaceAsyncResult;

            if (result == null)
                throw new ArgumentException(Resources.InvalidAsyncResultSpecified, "asyncResult");

            AssertNotDisposed();

            // this will throw an exception when a runspace is not successfully
            // available
            result.EndInvoke();

            Debug.Assert(result.Connection != null, "EndInvoke() should throw an exception if connection is null");

            _tracer.WriteMessage("PSW ConnMgr: Request serviced and runspace returned");
            Runspace runspace = result.Connection != null ? result.Connection.Runspace : null;

            return runspace;            
        }

        /// <summary>
        /// Release the runspace once the activity is done using the same
        /// </summary>
        /// <param name="runspace">runspace to release</param>
        public override void ReleaseRunspace(Runspace runspace)
        {
            AssertNotDisposed();

            Connection connection = GetConnectionForRunspace(runspace);
            _tracer.WriteMessage("PSW ConnMgr: Runspace released");

            // at this point connection will be not null
            // since GetConnectionForRunspace() should have
            // handled all cases and raised an exception
            Debug.Assert(connection != null, "GetConnectionForRunspace has not handled all cases and raised an exception");

            connection.Busy = false;
            connection.AsyncResult = null;

            CheckAndStartRequiredThreads();
        }

        /// <summary>
        /// Request a cleanup to the destination specified in the
        /// connection info. This means no runspaces will be held
        /// to the specified connection info.
        /// </summary>
        /// <param name="connectionInfo">connection info to which
        /// cleanup is desired</param>
        ///<param name="callback">callback to invoke</param>
        /// <param name="state">caller specified state</param>
        public override void RequestCleanup(WSManConnectionInfo connectionInfo, WaitCallback callback, object state)
        {
            if (_isDisposed || _isDisposing)
            {
                if (callback != null)
                {
                    callback(state);
                }

                return;
            }

            // add this computer to the list of cleanup computers
            string computerName = connectionInfo.ComputerName;

            ConcurrentQueue<Tuple<WaitCallback, object>> callbacks =
                _cleanupComputers.GetOrAdd(computerName, new ConcurrentQueue<Tuple<WaitCallback, object>>());

            var tuple = new Tuple<WaitCallback, object>(callback, state);
            callbacks.Enqueue(tuple);

            CheckAndStartCleanupThread();
        }

        /// <summary>
        /// Checks to see if the provider intentionally disconnected a runspace
        /// or it went into disconnected state due to network issues
        /// </summary>
        /// <param name="runspace">runspace that needs to be checked</param>
        /// <returns>true - when intentionally disconnected
        ///          false - disconnected due to network issues</returns>
        public override bool IsDisconnectedByRunspaceProvider(Runspace runspace)
        {
            Connection connection = GetConnectionForRunspace(runspace);
            return connection.DisconnectedIntentionally;
        }

        internal void DisconnectCalled()
        {
            lock (_syncObject)
            {
                _disconnectedSessionCount++;
                _connectedSessionCount--;
            }
        }
        internal void ReconnectCalled()
        {
            lock (_syncObject)
            {
                _connectedSessionCount++;
                _disconnectedSessionCount--;
            }
        }

        #endregion Interface Methods

        #region Private Methods

        private static void ThrowInvalidRunspaceException(Runspace runspace)
        {
            throw new ArgumentException(Resources.InvalidRunspaceSpecified, "runspace");
        }

        /// <summary>
        /// Handle the idle timer event
        /// </summary>
        /// <param name="sender">unused</param>
        /// <param name="e">unused</param>
        private void HandleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // ensure that the servicing thread is done before proceeding
            if (Interlocked.CompareExchange(ref _timerFired, TimerFired, TimerReset) == TimerFired)
            {
                _tracer.WriteMessage("PSW ConnMgr: Another timer thread is already servicing return");
                return;
            }

            if (_isDisposed || _isDisposing)
            {
                return;
            }

            try
            {
                _tracer.WriteMessage("PSW ConnMgr: Timer fired");

                _servicingThreadRelease.WaitOne();
                _timerThreadRelease.Reset();

                _tracer.WriteMessage("PSW ConnMgr: Timer servicing started");

                // when the timer elapses mark all unused connections to be closed
                Collection<string> toRemoveComputer = new Collection<string>();

                foreach (string computername in _connectionPool.Keys)
                {
                    ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> table = _connectionPool[computername];

                    Collection<string> toRemoveConfig = new Collection<string>();
                    foreach (string configName in table.Keys)
                    {
                        ConcurrentDictionary<Guid, Connection> connections = table[configName];
                        Collection<Connection> toRemoveConnections = new Collection<Connection>();

                        lock (_syncObject)
                        {
                            foreach (Connection connection in connections.Values)
                            {
                                // if connection has been marked idle the last 
                                // time remove resources
                                if (connection.Idle)
                                {
                                    toRemoveConnections.Add(connection);
                                }
                                else if (!connection.Busy)
                                {
                                    connection.Idle = true;
                                }
                            }
                        }

                        // remove all connections that need to be removed
                        foreach (Connection connection in toRemoveConnections)
                        {
                            Connection removeConnection;
                            _createdConnections--;
                            // remove connection from the table before attempting to 
                            // close it. This will ensure that this connection is not
                            // assigned by mistake
                            connections.TryRemove(connection.InstanceId, out removeConnection);
                            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "Closing idle connection to {0}", connection.Runspace.ConnectionInfo.ComputerName));
                            SubmitOperation(new CloseOperation(connection));
                        }

                        // if there are no more entries in connections it needs
                        // to be removed from the table
                        if (connections.Count == 0)
                        {
                            toRemoveConfig.Add(configName);
                        }

                    } // go through all configurations for a specified computer

                    foreach (string configName in toRemoveConfig)
                    {
                        ConcurrentDictionary<Guid, Connection> removedConnections;
                        table.TryRemove(configName, out removedConnections);
                    }

                    // if there are no more entries in table it needs to be removed
                    // from the connection pool
                    if (table.Keys.Count == 0)
                    {
                        toRemoveComputer.Add(computername);
                    }

                } // go through all computernames

                // remove unwanted computernames from the connectionpool
                foreach (string computerName in toRemoveComputer)
                {
                    ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> removedTable;
                    _connectionPool.TryRemove(computerName, out removedTable);
                }

                _tracer.WriteMessage("PSW ConnMgr: Timer servicing completed");

                Interlocked.CompareExchange(ref _timerFired, TimerReset, TimerFired);
                _timerThreadRelease.Set();
            }
            catch (ObjectDisposedException)
            { 
                // Ignoring this exception
            }
            finally
            {
                CheckAndStartRequiredThreads();
            }
        }

        private void TraceThreadPoolInfo(string message)
        {
#if DEBUG
            int maxWorkerThreads, maxCompletionPortThreads, availableWorkerThreads, availableCompletionPortThreads,
                minWorkerThreads, minCompletionPortThreads;

            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);

            _tracer.WriteMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                               "PSW ConnMgr: {0} minwt:{1} mincpt:{2} wt:{3} ct:{4} awt:{5} act:{6}",
                                               message, minWorkerThreads, minCompletionPortThreads, maxWorkerThreads,
                                               maxCompletionPortThreads, availableWorkerThreads,
                                               availableCompletionPortThreads));
#else
            _tracer.WriteMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                               "PSW ConnMgr: {0}",
                                               message));
#endif
        }

        /// <summary>
        /// Checks to see if a thread is already servicing and if not starts one
        /// </summary>
        private void CheckAndStartConnectionServicingThread()
        {
            if (Interlocked.CompareExchange(ref _isServicing, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("QueueUserWorkItem Connection Servicing thread");
            ThreadPool.QueueUserWorkItem(ServiceRequests);
        }

        /// <summary>
        /// Checks to see if a thread is already servicing callbacks 
        /// and if not starts one
        /// </summary>
        private void CheckAndStartCallbackServicingThread()
        {
            if (Interlocked.CompareExchange(ref _isServicingCallbacks, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("Callback thread");
            ThreadPool.QueueUserWorkItem(ServiceCallbacks);
        }

        /// <summary>
        /// Method that services the callbacks for all
        /// successfully assigned runspaces
        /// </summary>
        /// <param name="state">unused</param>
        private void ServiceCallbacks(object state)
        {
            if (_isDisposed || _isDisposing)
            {
                return;
            }

            GetRunspaceAsyncResult result;
            while (_callbacks.TryDequeue(out result))
            {
                result.SetAsCompleted(null);
            }

            // release servicing callbacks
            Interlocked.CompareExchange(ref _isServicingCallbacks, NotServicing, Servicing);
        }

        internal void AddToPendingCallback(GetRunspaceAsyncResult asyncResult)
        {
            _callbacks.Enqueue(asyncResult);
            CheckAndStartCallbackServicingThread();
        }

        private void CheckAndStartRequiredThreads()
        {
            if (_isDisposed || _isDisposing)
            {
                return;
            }

            CheckAndStartConnectionServicingThread();
            CheckAndStartDisconnectReconnectThread();
            CheckAndStartThrottleManagerThread();
            CheckAndStartCleanupThread();
        }

        /// <summary>
        /// Method that contains the core servicing logic
        /// </summary>
        /// <param name="state">not used</param>
        private void ServiceRequests(object state)
        {
            try
            {
                TraceThreadPoolInfo("Starting servicing thread");

                // ensure that idle timer thread has completed execution
                // before proceeding
                _timerThreadRelease.WaitOne();
                _servicingThreadRelease.Reset();

                if (NeedToReturnFromServicing())
                {
                    return;
                }

                Collection<RequestInfo> toRemove = new Collection<RequestInfo>();

                // service all pending requests
                foreach (RequestInfo info in _pendingRequests)
                {
                    if (ServiceOneRequest(info)) toRemove.Add(info);

                    if (NeedToReturnFromServicing())
                    {
                        return;
                    }
                }

                // remove serviced requests from pending queues
                foreach (RequestInfo info in toRemove)
                {
                    _pendingRequests.Remove(info);
                    _perfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.PSRemotingForcedToWaitRequestsQueueLength,
                        -1);

                }

                // if timer has fired, return
                if (NeedToReturnFromServicing())
                {
                    return;
                }

                // start servicing the incoming requests
                RequestInfo requestInfo;
                while (_inComingRequests.TryDequeue(out requestInfo))
                {
                    _perfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.PSRemotingPendingRequestsQueueLength,
                        -1);
                    if (!ServiceOneRequest(requestInfo))
                    {
                        // the request could not be serviced now
                        // add it to the pending queue
                        _pendingRequests.Add(requestInfo);
                        _perfCountersMgr.UpdateCounterByValue(
                            PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                            PSWorkflowPerformanceCounterIds.PSRemotingForcedToWaitRequestsQueueLength);
                    }

                    if (NeedToReturnFromServicing())
                    {
                        return;
                    }

                } // service incoming requests  
            }
            catch (ObjectDisposedException)
            {
                //
            }
            finally
            {
                // set servicing as complete
                SafelyReturnFromServicing();

                // Try to start the servicing thread again if there are any _inComingRequests.
                // This is required to fix the race condition between CheckAndStartRequiredThreads() and ServiceRequests() methods.
                //
                if (_inComingRequests.Count > 0)
                {
                    CheckAndStartConnectionServicingThread();
                }
            }
        }

        /// <summary>
        /// Services one single request
        /// </summary>
        /// <param name="requestInfo">request to service</param>
        /// <returns>true, if successfully serviced, false otherwise</returns>
        private bool ServiceOneRequest(RequestInfo requestInfo)
        {
            string computerName = requestInfo.ConnectionInfo.ComputerName;
            string configName = requestInfo.ConnectionInfo.ShellUri;

            // if a cleanup is requested for a specified computer
            // do not service the request until it is good to do so
            if (_cleanupComputers.ContainsKey(computerName))
                return false;

            int existingConnections = 0;
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount);

            // check if there is a table for this computername
            ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> table =
                _connectionPool.GetOrAdd(computerName,
                                         new ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>>());

            // count the number of existing connections to this computer
            existingConnections = table.Keys.Sum(key => table[key].Count);

            // check if there is a connection collection for the specified configuration
            ConcurrentDictionary<Guid, Connection> connections = table.GetOrAdd(configName,
                                                                                new ConcurrentDictionary
                                                                                    <Guid, Connection>());

            bool activityResumption = false;
            RunCommandsArguments args = requestInfo.AsyncResult.AsyncState as RunCommandsArguments;

            if (args != null && args.ImplementationContext != null)
                activityResumption = !args.ImplementationContext.DisconnectedRunspaceInstanceId.Equals(Guid.Empty);

            // Existing connections should not be used when remote activity is resuming as they have a valid remote runspace
            // Remote activity resume operation has to reconnect to the disconnected runspace
            if (!activityResumption)
            {
            // table is available, check if there are any free runspaces
            // if the collections was newly created, then this loop will 
            // be skipped)
                foreach (Connection connection in
                    connections.Values.Where(connection => !connection.Busy).Where(connection => ValidateConnection(requestInfo, connection)))
                {
                    // the assumption is connected session count and
                    // disconnected session count apply only to running
                    // pipelines. So a connection that is not busy will
                    // not be in the disconnected state
                    //Debug.Assert(connection.Runspace.RunspaceStateInfo.State != RunspaceState.Disconnected,
                    //             "A not Busy connection should not be in the disconnected state");
                    _tracer.WriteMessage("PSW ConnMgr: Assigning existing connection to request");
                    AssignConnection(requestInfo, connection);
                    _perfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount,
                        -1);
                    return true;
                }
            }

            if (existingConnections < _maxOutBoundConnections)
            {
                _tracer.WriteMessage("PSW ConnMgr: Creating new connection to service request");
                // the number of connections hasn't maxed out for this
                // computer - a new connection can be created
                Connection connection = CreateConnection(requestInfo, connections);

                // when the open succeeds within the specified retry attempts
                // the connection will be assigned.
                // submit the connection to the pending operations queue instead
                // of opening it directly
                SubmitOperation(new OpenOperation(connection));
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount,
                    -1);
                return true;
            }

            // when the existing connections are maxed out, there 
            // are one of two choices
            //      1. all connections are busy, in which case
            //         the request cannot be serviced
            //      2. there are some idle connections, which
            //         can be closed and a new one created

            Connection potentialConnection = null;

            // find the first available idle connection
            ConcurrentDictionary<Guid, Connection> removeConnections=null;

            foreach (var key in table.Keys)
            {
                removeConnections = table[key];

                potentialConnection = removeConnections.Values.Where(connection => !connection.Busy).FirstOrDefault();

                if (potentialConnection != null) break;
            }

            if (potentialConnection == null)
            {
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount,
                    -1);
                return false;  /* case 1 */
            }

            // it is possible that the potential connection in fact
            // is a good match, if so return the same
            // Existing connections should not be used when remote activity is resuming as they have a valid remote runspace
            // Remote activity resume operation has to reconnect to the disconnected runspace
            if (!activityResumption && ValidateConnection(requestInfo, potentialConnection))
            {
                _tracer.WriteMessage("PSW ConnMgr: Assigning potential connection to service request");
                AssignConnection(requestInfo, potentialConnection);
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount,
                    -1);
                return true;
            }

            // there is a potential connection which can be closed,
            // close the same and create a new one 
            removeConnections.TryRemove(potentialConnection.InstanceId, out potentialConnection);

            Debug.Assert(potentialConnection != null, "Trying to remove an element not in the dictionary");
            _tracer.WriteMessage("PSW ConnMgr: Closing potential connection and creating a new one to service request");

            // Create the connection object which will be returned after
            // the potential connection is closed
            _createdConnections--;
            Connection newConnection = CreateConnection(requestInfo, connections);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingConnectionsClosedReopendCount);
            SubmitOperation(new CloseOneAndOpenAnotherOperation(potentialConnection, newConnection));
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount,
                    -1);
            return true;
        }

        private Connection CreateConnection(RequestInfo requestInfo, ConcurrentDictionary<Guid, Connection> connections)
        {
            Connection connection = new Connection(this)
            {
                ConnectionInfo = requestInfo.ConnectionInfo,
                RetryCount = requestInfo.RetryCount,
                RetryInterval = requestInfo.RetryInterval,
                RetryAttempt = 0,
                AsyncResult = requestInfo.AsyncResult,
                Busy = true
            };

            // the busy status should be set before adding to the
            // collection so that this connection is accounted in
            // the total and is not assigned to another request
            connections.TryAdd(connection.InstanceId, connection);

            _createdConnections++;
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.PSRemotingConnectionsCreatedCount);
            return connection;
        }

        /// <summary>
        /// Do everything required so that servicing thread can return
        /// </summary>
        private void SafelyReturnFromServicing()
        {
            if (_isDisposed || _isDisposing)
            {
                return;
            }

            CheckAndStartRequiredThreads();

            try
            {
                _tracer.WriteMessage("PSW ConnMgr: Safely returning from servicing");
                Interlocked.CompareExchange(ref _isServicing, NotServicing, Servicing);
                _servicingThreadRelease.Set();
            }
            catch (ObjectDisposedException)
            {
                // Ignoring ObjectDisposedException
            }
        }

        /// <summary>
        /// Check if the servicing thread need to stop processing requests
        /// </summary>
        /// <returns>true if processing needs to stop</returns>
        private bool NeedToReturnFromServicing()
        {
            if(_isDisposed || _isDisposing)
            {
                _tracer.WriteMessage("PSW ConnMgr: Returning from servicing as ConnMgr is disposed or being disposed");
                return true;
            }

            // servicing thread has to return if the timer
            // has been fired
            if (_timerFired == TimerFired)
            {
                _tracer.WriteMessage("PSW ConnMgr: Returning from servicing since timer fired");
                return true;
            }

            if (_createdConnections >= _maxConnectedSessions)
            {
                _tracer.WriteMessage("PSW ConnMgr: Setting check for runspaces disconnect flag");
                Interlocked.CompareExchange(ref _checkForDisconnect, CheckForDisconnect, DoNotCheckForDisconnect);
            }

            return false;
        }

        private static bool ValidateConnection(RequestInfo requestInfo, Connection connection)
        {
            if (connection.Runspace.RunspaceStateInfo.State != RunspaceState.Opened ) return false;

            WSManConnectionInfo connectionInfo = requestInfo.ConnectionInfo;

            // when validation is called the Connection will have a runspace populated
            WSManConnectionInfo connectionInfo2 = connection.Runspace.OriginalConnectionInfo as WSManConnectionInfo;

            // Runspace.OriginalConnectionInfo is null for disconnected runspace after process crash
            if(connectionInfo2 == null)
                connectionInfo2 = connection.Runspace.ConnectionInfo as WSManConnectionInfo;

            if (connectionInfo2 == null) return false;
            
            // check URI related stuff
            if (!WorkflowUtils.CompareConnectionUri(connectionInfo, connectionInfo2))
            {
                return false;
            }

            // compare shell URI
            if (!WorkflowUtils.CompareShellUri(connectionInfo.ShellUri, connectionInfo2.ShellUri))
            {
                return false;
            }

            // check authentication
            if (!WorkflowUtils.CompareAuthentication(connectionInfo.AuthenticationMechanism, connectionInfo2.AuthenticationMechanism))
            {
                return false;
            }

            // check credentials if present
            if(!WorkflowUtils.CompareCredential(connectionInfo.Credential, connectionInfo2.Credential))
            {
                return false;
            }

            //check certificate thumbprint
            if (!WorkflowUtils.CompareCertificateThumbprint(connectionInfo.CertificateThumbprint, connectionInfo2.CertificateThumbprint))
            {
                return false;
            }

            //check proxy settings 
            if (!WorkflowUtils.CompareProxySettings(connectionInfo, connectionInfo2))
            {
                return false;
            }

            //check rest of wsman settings 
            if (!WorkflowUtils.CompareOtherWSManSettings(connectionInfo, connectionInfo2))
            {
                return false;
            }

            // check open timeout
            if (connectionInfo2.IdleTimeout < connectionInfo.IdleTimeout)
                return false;

            return true;
        }

        /// <summary>
        /// Service the request using the available connection
        /// </summary>
        /// <param name="requestInfo">request to service</param>
        /// <param name="connection">connection to use for servicing</param>
        private void AssignConnection(RequestInfo requestInfo, Connection connection)
        {
            IAsyncResult asyncResult = requestInfo.AsyncResult;
            GetRunspaceAsyncResult result = asyncResult as GetRunspaceAsyncResult;
            Debug.Assert(result != null, "IAsyncResult should be GetRunspaceAsyncResult");

            connection.Busy = true;

            connection.AsyncResult = result;
            result.Connection = connection;

            AddToPendingCallback(result);
        }

        /// <summary>
        /// Find the connection object given a runspace
        /// </summary>
        /// <param name="runspace">runspace whose connection
        /// needs to be found</param>
        /// <returns>Connection if found, null otherwise</returns>
        private Connection GetConnectionForRunspace(Runspace runspace)
        {
            if (runspace == null)
                throw new ArgumentNullException("runspace");

            // OriginalConnectionInfo will be null for reconnected remoterunspace after crash or shutdown
            WSManConnectionInfo connectionInfo = runspace.ConnectionInfo as WSManConnectionInfo;

            if (connectionInfo == null)
            {
                _tracer.WriteMessage("PSW ConnMgr: Incoming connectioninfo is null");
                // throw an exception here
                ThrowInvalidRunspaceException(runspace);
            }

            string computername = connectionInfo.ComputerName;
            string configname = connectionInfo.ShellUri;

            ConcurrentDictionary<string, ConcurrentDictionary<Guid,Connection>> table;
            ConcurrentDictionary<Guid,Connection> connections;

            if (!_connectionPool.TryGetValue(computername, out table))
            {
                // invalid runspace specified, throw an exception here
                _tracer.WriteMessage("PSW ConnMgr: Cannot find table for computername " + computername);
                ThrowInvalidRunspaceException(runspace);
            }

            if (!table.TryGetValue(configname, out connections))
            {
                // invalid runspace specified, throw an exception here
                _tracer.WriteMessage("PSW ConnMgr: Cannot find list for config " + configname);
                ThrowInvalidRunspaceException(runspace);
            }

            foreach (Connection connection in
                 connections.Values.Where(connection => connection.Runspace != null).Where(connection => connection.Runspace.InstanceId == runspace.InstanceId))
            {
                return connection;
            }

            // if this point is reached, then there is no match
            // invalid runspace specified, throw an exception 
            _tracer.WriteMessage("PSW ConnMgr: Cannot find the actual connection object");
            ThrowInvalidRunspaceException(runspace);

            return null;
        }

        /// <summary>
        /// Check and start a method which will do cleanups to
        /// the specified computer
        /// </summary>
        private void CheckAndStartCleanupThread()
        {
            if (Interlocked.CompareExchange(ref _isServicingCleanups, Servicing, NotServicing) != NotServicing)
                return;

            TraceThreadPoolInfo("Cleanup thread");

            ThreadPool.QueueUserWorkItem(ServiceCleanupRequests);
        }

        /// <summary>
        /// Worker method for servicing cleanups to requested computers
        /// </summary>
        /// <param name="state"></param>
        private void ServiceCleanupRequests(object state)
        {

            foreach (string computerName in _cleanupComputers.Keys)
            {
                ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> table;
                _connectionPool.TryGetValue(computerName, out table);

                if (table == null)
                {
                    RaiseCallbacksAfterCleanup(computerName);
                    continue;
                }

                foreach (string configName in table.Keys)
                {
                    ConcurrentDictionary<Guid, Connection> connections;
                    table.TryGetValue(configName, out connections);

                    if (connections == null) continue;
                    lock (_syncObject)
                    {
                        foreach (CloseOperation closeOperation in
                            connections.Values.Where(connection => !connection.Busy).Select(connection => new CloseOperation(connection)))
                        {
                            closeOperation.OperationComplete += HandleCloseOperationComplete;
                            SubmitOperation(closeOperation);
                        }
                    }
                }
            }

            Interlocked.CompareExchange(ref _isServicingCleanups, NotServicing, Servicing);
        }

        /// <summary>
        /// Handles OperationComplete of close operations i.e
        /// when connections to a specified computer is closed
        /// </summary>
        /// <param name="sender">the CloseOperation object
        /// that initiated this event</param>
        /// <param name="e">event parameters</param>
        private void HandleCloseOperationComplete(object sender, EventArgs e)
        {
            // the specified connection was successfully closed
            CloseOperation closeOperation = sender as CloseOperation;
            Debug.Assert(closeOperation != null, "CloseOperation object need to be returned in OperationComplete event");
            closeOperation.OperationComplete -= HandleCloseOperationComplete;

            Connection connection = closeOperation.Connection;
            WSManConnectionInfo connectionInfo = connection.Runspace.ConnectionInfo as WSManConnectionInfo;
            if (connectionInfo == null) return;

            string computerName = connectionInfo.ComputerName;
            string configName = connectionInfo.ShellUri;

            ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> table;
            _connectionPool.TryGetValue(computerName, out table);
            if (table == null) return;

            ConcurrentDictionary<Guid, Connection> connections;
            table.TryGetValue(configName, out connections);
            if (connections == null) return;

            // remove the connection from the table for this configName
            Connection removedConnection;
            connections.TryRemove(connection.InstanceId, out removedConnection);
            if (connections.Count != 0) return;

            // if there are no more connections to this configName
            // remove the table for the configName
            ConcurrentDictionary<Guid, Connection> removedConnections;
            table.TryRemove(configName, out removedConnections);
            if (table.Count != 0) return;

            // if there are no more tables for any specified configNames
            // for this computerName, remove the table for the computerName
            ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> removedTable;
            _connectionPool.TryRemove(computerName, out removedTable);

            if (removedTable != null)
            {
                // if this thread is the one that removed the table, then 
                // make all the callbacks in this thread
                RaiseCallbacksAfterCleanup(computerName);
            }
        }

        /// <summary>
        /// Raise all the callbacks to the specified computer
        /// after the requested cleanup
        /// </summary>
        /// <param name="computerName">computer to which the
        /// callback needs to be raised</param>
        private void RaiseCallbacksAfterCleanup(string computerName)
        {
            bool alreadyAdded = _timerMap.Values.Contains(computerName, StringComparer.OrdinalIgnoreCase);

            // before making the callbacks fire the timer
            Timer timer = new Timer();
            _timerMap.TryAdd(timer, computerName);

            timer.Elapsed += HandleCleanupWaitTimerElapsed;

            ConcurrentQueue<Tuple<WaitCallback, object>> waitCallbacks;
            if (!_cleanupComputers.TryGetValue(computerName, out waitCallbacks))
            {
                _tracer.WriteMessage("PSW ConnMgr: Cannot find specified computer in _waitCallbacks dictionary: " + computerName);
                return;
            }

            Tuple<WaitCallback, object> tuple;
            Collection<Tuple<WaitCallback, object>> callbacks = new Collection<Tuple<WaitCallback, object>>();

            int highest = 0;
            bool calledFromActivity = false;

            while (waitCallbacks.TryDequeue(out tuple))
            {
                if (tuple.Item2 != null)
                {
                    RunCommandsArguments args = tuple.Item2 as RunCommandsArguments;

                    if (args != null)
                    {
                        if (highest < args.CleanupTimeout)
                        {
                            highest = args.CleanupTimeout;
                        }

                        calledFromActivity = true;
                    }
                }

                callbacks.Add(tuple);
            }

            if (highest != 0)
            {
                timer.Interval = highest * 1000;
                timer.AutoReset = false;
                timer.Enabled = true;

                foreach (Tuple<WaitCallback, object> t in callbacks)
                {
                    if (t.Item1 != null)
                        t.Item1(t.Item2);
                }
            }
            else
            {
                if (!alreadyAdded)
                {
                    HandleCleanupWaitTimerElapsed(timer, null);
                }
                else
                {
                    string unused;
                    _timerMap.TryRemove(timer, out unused);
                }
            }

            if (!calledFromActivity)
            {
                foreach(var callbackTuple in callbacks)
                {
                    if (callbackTuple.Item1 != null)
                        callbackTuple.Item1(callbackTuple.Item2);
                }
            }
      }

        /// <summary>
        /// Timer elapsed handler for the specified computer. Once
        /// the timer elapses, new connections to the specified
        /// computer will be allowed
        /// </summary>
        /// <param name="sender">timer that generated the event</param>
        /// <param name="e">event arguments</param>
        private void HandleCleanupWaitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Timer timer = sender as Timer;
            Debug.Assert(timer != null, "Sender cannot be null");

            string computerName;
            _timerMap.TryGetValue(timer, out computerName);
            if (!string.IsNullOrEmpty(computerName))
            {
                ConcurrentQueue<Tuple<WaitCallback, object>> removedCallbacks;
                _cleanupComputers.TryRemove(computerName, out removedCallbacks);
            }

            // remove entry from the timer map and do cleanup
            _timerMap.TryRemove(timer, out computerName);

            timer.Elapsed -= HandleCleanupWaitTimerElapsed;
            timer.Dispose();

            CheckAndStartRequiredThreads();
        }

        #endregion Private Methods

        #region Test Helper Methods

        internal bool IsConnectionPoolEmpty()
        {
            return _connectionPool.Keys.Count > 0;
        }

        internal IEnumerable GetConnectionEnumerator()
        {
            return new ConnectionEnumerator(_connectionPool);
        }
        #endregion Test Helper Methods

        #region Dispose

        /// <summary>
        /// Dispose the connection manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the connection manager
        /// </summary>
        /// <param name="isDisposing"></param>
        protected void Dispose(bool isDisposing)
        {
            if (_isDisposed || !isDisposing)
                return;

            lock (_syncObject)
            {
                if (_isDisposed)
                    return;

                _isDisposing = true;

                // set these values to servicing so that a thread is
                // not started
                Interlocked.CompareExchange(ref _isServicing, Servicing, NotServicing);
                Interlocked.CompareExchange(ref _isServicingCallbacks, Servicing, NotServicing);
                Interlocked.CompareExchange(ref _isServicingCleanups, Servicing, NotServicing);
                Interlocked.CompareExchange(ref _timerFired, TimerFired, TimerReset);

                // close and clear all connections in connection pool
                ClearAll();
                
                // This should be done after closing all connections.
                // Throttling thread executes the close operations
                Interlocked.CompareExchange(ref _isOperationsServiced, Servicing, NotServicing);

                _timer.Elapsed -= HandleTimerElapsed;
                _timer.Dispose();

                var objectDisposedException = new ObjectDisposedException("ConnectionManager");
                GetRunspaceAsyncResult item;
                while (_callbacks.TryDequeue(out item))
                {
                    item.SetAsCompleted(objectDisposedException);
                }
                _cleanupComputers.Clear();

                RequestInfo info;
                while (_inComingRequests.Count > 0)
                {
                    _inComingRequests.TryDequeue(out info);
                    info.AsyncResult.SetAsCompleted(objectDisposedException);
                }

                foreach(var reqInfo in _pendingRequests)
                {
                    reqInfo.AsyncResult.SetAsCompleted(objectDisposedException);
                }
                _pendingRequests.Clear();

                ThrottleOperation operation;
                while (_pendingQueue.Count > 0)
                {
                    _pendingQueue.TryDequeue(out operation);
                }

                _timerMap.Clear();
                _servicingThreadRelease.Close();
                _timerThreadRelease.Close();
                _testHelperCloseDone.Close();
                _tracer.Dispose();

                _isDisposed = true;
            }
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ConnectionManager");
            }
        }
        #endregion Dispose

        #region Throttling

        /// <summary>
        /// queue of operations to be used for throttling
        /// </summary>
        private readonly ConcurrentQueue<ThrottleOperation> _pendingQueue = new ConcurrentQueue<ThrottleOperation>();

        /// <summary>
        /// Count of operations in progress
        /// </summary>
        private int _inProgressCount = 0;

        /// <summary>
        /// throttle limit - includes open/close/connect/disconnect operations
        /// </summary>
        private readonly int _throttleLimit;

        /// <summary>
        /// is the queue of operations being serviced
        /// </summary>
        private int _isOperationsServiced;

        /// <summary>
        /// submit an operation to the queue
        /// </summary>
        /// <param name="operation">operation to submit</param>
        private void SubmitOperation(ThrottleOperation operation)
        {
            _pendingQueue.Enqueue(operation);
            CheckAndStartThrottleManagerThread();
        }

        private void CheckAndStartThrottleManagerThread()
        {
            // if not already set, set flag that pending operations queue is being serviced
            // else another thread is already servicing so return
            if (Interlocked.CompareExchange(ref _isOperationsServiced, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("Queuing user workitem Running operations in throttle queue");

            ThreadPool.QueueUserWorkItem(StartOperationsFromQueue);
        }

        /// <summary>
        /// Start operations upto throttle limit from the queue
        /// </summary>
        /// <remarks>this method is thread safe. It starts all pending
        /// operations in the first calling thread. The assumption here
        /// is that starting a few operations isn't expensive and so
        /// the calling thread is not blocked for a long period of time</remarks>
        private void StartOperationsFromQueue(object state)
        {
            if (_isDisposed)
            {
                return;
            }

            TraceThreadPoolInfo("Running operations in throttle queue");

            // at this point, there will be only one thread which will be
            // servicing operations from the pending operations queue
            ThrottleOperation operation;

            while (_inProgressCount < _throttleLimit && _pendingQueue.TryDequeue(out operation))
            {
                operation.OperationComplete += HandleOperationComplete;
                Interlocked.Increment(ref _inProgressCount);
                operation.DoOperation();
            }

            _tracer.WriteMessage("PSW ConnMgr: Done throttling");
            // set flag that pending operations queue is not being serviced
            Interlocked.CompareExchange(ref _isOperationsServiced, NotServicing, Servicing);
        }

        private void HandleOperationComplete (Object sender, EventArgs e)
        {
            ThrottleOperation operation = sender as ThrottleOperation;
            Debug.Assert(operation != null, "OperationComplete event does not pass ThrottleOperation as sender");
            operation.OperationComplete -= HandleOperationComplete;
            Interlocked.Decrement(ref _inProgressCount);
            CheckAndStartRequiredThreads();            
        }

        #endregion Throttling

        #region Connection Enumerator

        private class ConnectionEnumerator : IEnumerator, IEnumerable
        {
            private Connection _currentConnection;
            private ConcurrentDictionary<Guid, Connection> _currentConnections;
            private ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>> _currentTable;
            private readonly IEnumerator _tableEnumerator;
            private IEnumerator _configEnumerator;
            private IEnumerator _connectionEnumerator;

            private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>>>
                _connectionPool;

            internal ConnectionEnumerator(ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>>> connectionPool)
            {
                Debug.Assert(connectionPool != null, "ConnectionPool cannot be null");
                _connectionPool = connectionPool;
                _tableEnumerator = _connectionPool.Keys.GetEnumerator();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (_connectionEnumerator != null && _connectionEnumerator.MoveNext())
                    {
                        Guid key = (Guid) _connectionEnumerator.Current;
                        _currentConnection = null;
                        _currentConnections.TryGetValue(key, out _currentConnection);
                        if (_currentConnection != null)
                            return true;
                        else
                            continue;
                    }

                    if (_configEnumerator != null && _configEnumerator.MoveNext())
                    {
                        string configName = (string)_configEnumerator.Current;
                        _currentTable.TryGetValue(configName, out _currentConnections);
                        _connectionEnumerator = _currentConnections.Keys.GetEnumerator();

                        Debug.Assert(_connectionEnumerator != null, "Enumerator should not be null");

                        continue;
                    }

                    if (_tableEnumerator.MoveNext())
                    {
                        string tableName = (string)_tableEnumerator.Current;
                        _connectionPool.TryGetValue(tableName, out _currentTable);
                        _configEnumerator = _currentTable.Keys.GetEnumerator();
                        Debug.Assert(_configEnumerator != null, "Enumerator should not be null");
                        continue;
                    }

                    break;
                }

                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Reset()
            {
                _tableEnumerator.Reset();
                _currentTable = (ConcurrentDictionary<string, ConcurrentDictionary<Guid, Connection>>)_tableEnumerator.Current;
                _configEnumerator = _currentTable.Keys.GetEnumerator();
                _currentConnections = (ConcurrentDictionary<Guid, Connection>)_configEnumerator.Current;
                _connectionEnumerator = _currentConnections.Keys.GetEnumerator();
                Guid key = (Guid)_connectionEnumerator.Current;
                _currentConnections.TryGetValue(key, out _currentConnection);
            }

            /// <summary>
            /// 
            /// </summary>
            public object Current
            {
                get { return _currentConnection; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator()
            {
                return this;
            }
        }

        #endregion Connection Enumerator

        #region Disconnect/Reconnect

        private int _isReconnectServicing;
        private const long NotMarked = 0;
        private const long Marked = 1;
        private long _newConnectionMarked = NotMarked;

        private void CheckAndStartDisconnectReconnectThread()
        {
            if (_checkForDisconnect != CheckForDisconnect) return;

            if (Interlocked.CompareExchange(ref _isReconnectServicing, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("Queuing user workitem disconnect/reconnect worker");           
            ThreadPool.QueueUserWorkItem(DisconnectReconnectWorker);
        }

        /// <summary>
        /// Disconnect and reconnect to different runspaces as necessary
        /// so as to help the connection manager scale and run a large
        /// number of commands on a large number of remote nodes
        /// </summary>
        /// <param name="state">unused</param>
        private void DisconnectReconnectWorker(object state)
        {
            if (_isDisposed || _isDisposing)
            {
                return;
            }

            TraceThreadPoolInfo("Running disconnect/reconnect worker");

            Interlocked.CompareExchange(ref _newConnectionMarked, NotMarked, Marked);

            while (Interlocked.CompareExchange(ref _newConnectionMarked, Marked, NotMarked) == Marked)
            {
                foreach (Connection connection in
                    (new ConnectionEnumerator(_connectionPool)))
                {
                    if (_disconnectedSessionCount > _maxDisconnectedSessions) break;

                    if (!connection.Busy) continue;
                    if (!connection.ReadyForDisconnect) continue;

                    connection.DisconnectedIntentionally = true;
                    // add a disconnect operation to the queue of operations
                    SubmitOperation(new DisconnectOperation(connection));
                }

                // at this point all relevant connections have been disconnected
                // now connect to every remote connection for a specified period of time

                // only connections which were initially marked for disconnected
                // would have been disconnected
                foreach (Connection connection in
                    (new ConnectionEnumerator(_connectionPool)))
                {
                    if (_connectedSessionCount > _maxConnectedSessions) break;

                    if (!connection.ReadyForReconnect) continue;

                    connection.DisconnectedIntentionally = false;
                    SubmitOperation(new ReconnectOperation(connection));
                }
            }

            _tracer.WriteMessage("PSW ConnMgr: Exiting disconnect reconnect worker");
            // reset flag that disconnect/reconnect thread is running
            Interlocked.CompareExchange(ref _isReconnectServicing, NotServicing, Servicing);
        }
       
        /// <summary>
        /// Callback to indicate that this runspace been initiated with
        /// a pipeline and can be disconnected
        /// </summary>
        /// <param name="runspace">runspace that needs to be marked as
        /// ready for disconnect</param>
        public override void ReadyForDisconnect(Runspace runspace)
        {
            Connection connection = GetConnectionForRunspace(runspace);
            _tracer.WriteMessage("PSW ConnMgr: Runspace marked as ready for disconnect");

            // at this point connection will be not null
            // since GetConnectionForRunspace() should have
            // handled all cases and raised an exception
            Debug.Assert(connection != null, "GetConnectionForRunspace has not handled all cases and raised an exception");

            if (connection.Busy)
            {
                // mark the connection as ready for disconnect
                // the thread which is servicing disconnect and reconnect
                // will take care of disconnecting and reconnecting the same
                lock (_syncObject)
                {
                    if (!connection.Busy)
                        return;

                    connection.ReadyForDisconnect = true;
                    Interlocked.CompareExchange(ref _newConnectionMarked, NotMarked, Marked);
                }

                CheckAndStartRequiredThreads();
            }
        }       

        #endregion Disconnect/Reconnect

        #region Test Helpers

        private readonly ManualResetEvent _testHelperCloseDone = new ManualResetEvent(false);
        internal void ClearAll()
        {
            if (_connectionPool.Count > 0)
            {
                _testHelperCloseDone.Reset();

                foreach (CloseOperation operation in
                    from Connection connection in new ConnectionEnumerator(_connectionPool)
                    select new CloseOperation(connection))
                {
                    operation.OperationComplete += OperationComplete;
                    SubmitOperation(operation);
                }

                _testHelperCloseDone.WaitOne();

                _connectionPool.Clear();
            }
        }

        private void OperationComplete(object sender, EventArgs e)
        {
            CloseOperation closeOperation = sender as CloseOperation;
            Debug.Assert(closeOperation != null, "CloseOperation not returned as sender");
            closeOperation.OperationComplete -= OperationComplete;

            if (_pendingQueue.Count == 0)
            {
                _testHelperCloseDone.Set();
            }
        }

        #endregion Test Helpers
    }
}
