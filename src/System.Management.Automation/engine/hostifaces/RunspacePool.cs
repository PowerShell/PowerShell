// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces.Internal;
using System.Runtime.Serialization;
using System.Threading;

using PSHost = System.Management.Automation.Host.PSHost;

namespace System.Management.Automation.Runspaces
{
    #region Exceptions
    /// <summary>
    /// Exception thrown when state of the runspace pool is different from
    /// expected state of runspace pool.
    /// </summary>
    [Serializable]
    public class InvalidRunspacePoolStateException : SystemException
    {
        /// <summary>
        /// Creates a new instance of InvalidRunspacePoolStateException class.
        /// </summary>
        public InvalidRunspacePoolStateException()
        : base
        (
            StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolStateGeneral)
        )
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidRunspacePoolStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public InvalidRunspacePoolStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidRunspacePoolStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidRunspacePoolStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidRunspacePoolStateException
        /// with a specified error message and current and expected state.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="currentState">Current state of runspace pool.</param>
        /// <param name="expectedState">Expected state of the runspace pool.</param>
        internal InvalidRunspacePoolStateException
        (
            string message,
            RunspacePoolState currentState,
            RunspacePoolState expectedState
        )
            : base(message)
        {
            _expectedState = expectedState;
            _currentState = currentState;
        }

        #region ISerializable Members

        // No need to implement GetObjectData
        // if all fields are static or [NonSerialized]

        /// <summary>
        /// Initializes a new instance of the InvalidRunspacePoolStateException
        /// class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds
        /// the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains
        /// contextual information about the source or destination.
        /// </param>
        protected
        InvalidRunspacePoolStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        /// <summary>
        /// Access CurrentState of the runspace pool.
        /// </summary>
        /// <remarks>
        /// This is the state of the runspace pool when exception was thrown.
        /// </remarks>
        public RunspacePoolState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        /// <summary>
        /// Expected state of runspace pool by the operation which has thrown
        /// this exception.
        /// </summary>
        public RunspacePoolState ExpectedState
        {
            get
            {
                return _expectedState;
            }
        }

        /// <summary>
        /// Converts the current to an InvalidRunspaceStateException.
        /// </summary>
        internal InvalidRunspaceStateException ToInvalidRunspaceStateException()
        {
            InvalidRunspaceStateException exception = new InvalidRunspaceStateException(
                RunspaceStrings.InvalidRunspaceStateGeneral,
                this);
            exception.CurrentState = RunspacePoolStateToRunspaceState(this.CurrentState);
            exception.ExpectedState = RunspacePoolStateToRunspaceState(this.ExpectedState);
            return exception;
        }

        /// <summary>
        /// Converts a RunspacePoolState to a RunspaceState.
        /// </summary>
        private static RunspaceState RunspacePoolStateToRunspaceState(RunspacePoolState state)
        {
            switch (state)
            {
                case RunspacePoolState.BeforeOpen:
                    return RunspaceState.BeforeOpen;

                case RunspacePoolState.Opening:
                    return RunspaceState.Opening;

                case RunspacePoolState.Opened:
                    return RunspaceState.Opened;

                case RunspacePoolState.Closed:
                    return RunspaceState.Closed;

                case RunspacePoolState.Closing:
                    return RunspaceState.Closing;

                case RunspacePoolState.Broken:
                    return RunspaceState.Broken;

                case RunspacePoolState.Disconnecting:
                    return RunspaceState.Disconnecting;

                case RunspacePoolState.Disconnected:
                    return RunspaceState.Disconnected;

                case RunspacePoolState.Connecting:
                    return RunspaceState.Connecting;

                default:
                    Diagnostics.Assert(false, "Unexpected RunspacePoolState");
                    return 0;
            }
        }

        /// <summary>
        /// State of the runspace pool when exception was thrown.
        /// </summary>
        [NonSerialized]
        private readonly RunspacePoolState _currentState = 0;

        /// <summary>
        /// State of the runspace pool expected in method which throws this exception.
        /// </summary>
        [NonSerialized]
        private readonly RunspacePoolState _expectedState = 0;
    }
    #endregion

    #region State
    /// <summary>
    /// Defines various states of a runspace pool.
    /// </summary>
    public enum RunspacePoolState
    {
        /// <summary>
        /// Beginning state upon creation.
        /// </summary>
        BeforeOpen = 0,
        /// <summary>
        /// A RunspacePool is being created.
        /// </summary>
        Opening = 1,
        /// <summary>
        /// The RunspacePool is created and valid.
        /// </summary>
        Opened = 2,
        /// <summary>
        /// The RunspacePool is closed.
        /// </summary>
        Closed = 3,
        /// <summary>
        /// The RunspacePool is being closed.
        /// </summary>
        Closing = 4,
        /// <summary>
        /// The RunspacePool has been disconnected abnormally.
        /// </summary>
        Broken = 5,

        /// <summary>
        /// The RunspacePool is being disconnected.
        /// </summary>
        Disconnecting = 6,

        /// <summary>
        /// The RunspacePool has been disconnected.
        /// </summary>
        Disconnected = 7,

        /// <summary>
        /// The RunspacePool is being connected.
        /// </summary>
        Connecting = 8,
    }

    /// <summary>
    /// Event arguments passed to runspacepool state change handlers
    /// <see cref="RunspacePool.StateChanged"/> event.
    /// </summary>
    public sealed class RunspacePoolStateChangedEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="state">
        /// state to raise the event with.
        /// </param>
        internal RunspacePoolStateChangedEventArgs(RunspacePoolState state)
        {
            RunspacePoolStateInfo = new RunspacePoolStateInfo(state, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="stateInfo"></param>
        internal RunspacePoolStateChangedEventArgs(RunspacePoolStateInfo stateInfo)
        {
            RunspacePoolStateInfo = stateInfo;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the stateinfo of RunspacePool when this event occurred.
        /// </summary>
        public RunspacePoolStateInfo RunspacePoolStateInfo { get; }

        #endregion

        #region Private Data

        #endregion
    }

    /// <summary>
    /// Event arguments passed to RunspaceCreated event of RunspacePool.
    /// </summary>
    internal sealed class RunspaceCreatedEventArgs : EventArgs
    {
        #region Private Data

        #endregion

        #region Constructors

        /// <summary>
        /// </summary>
        /// <param name="runspace"></param>
        internal RunspaceCreatedEventArgs(Runspace runspace)
        {
            Runspace = runspace;
        }

        #endregion

        #region Internal Properties

        internal Runspace Runspace { get; }

        #endregion
    }

    #endregion

    #region RunspacePool Availability

    /// <summary>
    /// Defines runspace pool availability.
    /// </summary>
    public enum RunspacePoolAvailability
    {
        /// <summary>
        /// RunspacePool is not in the Opened state.
        /// </summary>
        None = 0,

        /// <summary>
        /// RunspacePool is Opened and available to accept commands.
        /// </summary>
        Available = 1,

        /// <summary>
        /// RunspacePool on the server is connected to another
        /// client and is not available to this client for connection
        /// or running commands.
        /// </summary>
        Busy = 2
    }

    #endregion

    #region RunspacePool Capabilities

    /// <summary>
    /// Defines runspace capabilities.
    /// </summary>
    public enum RunspacePoolCapability
    {
        /// <summary>
        /// No additional capabilities beyond a default runspace.
        /// </summary>
        Default = 0x0,

        /// <summary>
        /// Runspacepool and remoting layer supports disconnect/connect feature.
        /// </summary>
        SupportsDisconnect = 0x1
    }

    #endregion

    #region AsyncResult

    /// <summary>
    /// Encapsulated the AsyncResult for pool's Open/Close async operations.
    /// </summary>
    internal sealed class RunspacePoolAsyncResult : AsyncResult
    {
        #region Private Data

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ownerId">
        /// Instance Id of the pool creating this instance
        /// </param>
        /// <param name="callback">
        /// Callback to call when the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the "callback" with.
        /// </param>
        /// <param name="isCalledFromOpenAsync">
        /// true if AsyncResult monitors Async Open.
        /// false otherwise
        /// </param>
        internal RunspacePoolAsyncResult(Guid ownerId, AsyncCallback callback, object state,
            bool isCalledFromOpenAsync)
            : base(ownerId, callback, state)
        {
            IsAssociatedWithAsyncOpen = isCalledFromOpenAsync;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// True if AsyncResult monitors Async Open.
        /// false otherwise.
        /// </summary>
        internal bool IsAssociatedWithAsyncOpen { get; }

        #endregion
    }

    /// <summary>
    /// Encapsulated the results of a RunspacePool.BeginGetRunspace method.
    /// </summary>
    internal sealed class GetRunspaceAsyncResult : AsyncResult
    {
        #region Private Data

        private bool _isActive;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ownerId">
        /// Instance Id of the pool creating this instance
        /// </param>
        /// <param name="callback">
        /// Callback to call when the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the "callback" with.
        /// </param>
        internal GetRunspaceAsyncResult(Guid ownerId, AsyncCallback callback, object state)
            : base(ownerId, callback, state)
        {
            _isActive = true;
        }

        #endregion

        #region Internal Methods/Properties

        /// <summary>
        /// Gets the runspace that is assigned to the async operation.
        /// </summary>
        /// <remarks>
        /// This can be null if the async Get operation is not completed.
        /// </remarks>
        internal Runspace Runspace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this operation
        /// is active or not.
        /// </summary>
        internal bool IsActive
        {
            get
            {
                lock (SyncObject)
                {
                    return _isActive;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _isActive = value;
                }
            }
        }

        /// <summary>
        /// Marks the async operation as completed and releases
        /// waiting threads.
        /// </summary>
        /// <param name="state">
        /// This is not used
        /// </param>
        /// <remarks>
        /// This method is called from a thread pool thread to release
        /// the async operation.
        /// </remarks>
        internal void DoComplete(object state)
        {
            SetAsCompleted(null);
        }

        #endregion
    }

    #endregion

    #region RunspacePool

    /// <summary>
    /// Public interface which supports pooling PowerShell Runspaces.
    /// </summary>
    public sealed class RunspacePool : IDisposable
    {
        #region Private Data

        private readonly RunspacePoolInternal _internalPool;
        private readonly object _syncObject = new object();

        private event EventHandler<RunspacePoolStateChangedEventArgs> InternalStateChanged = null;

        private event EventHandler<PSEventArgs> InternalForwardEvent = null;

        private event EventHandler<RunspaceCreatedEventArgs> InternalRunspaceCreated = null;

        #endregion

        #region Internal Constructor

        /// <summary>
        /// Constructor which creates a RunspacePool using the
        /// supplied <paramref name="configuration"/>,
        /// <paramref name="minRunspaces"/> and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Host is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        internal RunspacePool(int minRunspaces, int maxRunspaces, PSHost host)
        {
            // Currently we support only Local Runspace Pool..
            // this needs to be changed once remote runspace pool
            // is implemented

            _internalPool = new RunspacePoolInternal(minRunspaces, maxRunspaces, host);
        }

        /// <summary>
        /// Constructor which creates a RunspacePool using the
        /// supplied <paramref name="initialSessionState"/>,
        /// <paramref name="minRunspaces"/> and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="initialSessionState">
        /// InitialSessionState object to use when creating a new Runspace.
        /// </param>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// initialSessionState is null.
        /// Host is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        internal RunspacePool(int minRunspaces, int maxRunspaces,
            InitialSessionState initialSessionState, PSHost host)
        {
            // Currently we support only Local Runspace Pool..
            // this needs to be changed once remote runspace pool
            // is implemented

            _internalPool = new RunspacePoolInternal(minRunspaces,
                maxRunspaces, initialSessionState, host);
        }

        /// <summary>
        /// Construct a runspace pool object.
        /// </summary>
        /// <param name="minRunspaces">Min runspaces.</param>
        /// <param name="maxRunspaces">Max runspaces.</param>
        /// <param name="typeTable">TypeTable.</param>
        /// <param name="host">Host.</param>
        /// <param name="applicationArguments">App arguments.</param>
        /// <param name="connectionInfo">Connection information.</param>
        /// <param name="name">Session name.</param>
        internal RunspacePool(
            int minRunspaces,
            int maxRunspaces,
            TypeTable typeTable,
            PSHost host,
            PSPrimitiveDictionary applicationArguments,
            RunspaceConnectionInfo connectionInfo,
            string name = null)
        {
            _internalPool = new RemoteRunspacePoolInternal(
                minRunspaces,
                maxRunspaces,
                typeTable,
                host,
                applicationArguments,
                connectionInfo,
                name);

            IsRemote = true;
        }

        /// <summary>
        /// Creates a runspace pool object in a disconnected state that is
        /// ready to connect to a remote runspace pool session specified by
        /// the instanceId parameter.
        /// </summary>
        /// <param name="isDisconnected">Indicates whether the shell/runspace pool is disconnected.</param>
        /// <param name="instanceId">Identifies a remote runspace pool session to connect to.</param>
        /// <param name="name">Friendly name for runspace pool.</param>
        /// <param name="connectCommands">Runspace pool running commands information.</param>
        /// <param name="connectionInfo">Connection information of remote server.</param>
        /// <param name="host">PSHost object.</param>
        /// <param name="typeTable">TypeTable used for serialization/deserialization of remote objects.</param>
        internal RunspacePool(
            bool isDisconnected,
            Guid instanceId,
            string name,
            ConnectCommandInfo[] connectCommands,
            RunspaceConnectionInfo connectionInfo,
            PSHost host,
            TypeTable typeTable)
        {
            // Disconnect-Connect semantics are currently only supported in WSMan transport.
            if (connectionInfo is not WSManConnectionInfo)
            {
                throw new NotSupportedException();
            }

            _internalPool = new RemoteRunspacePoolInternal(instanceId, name, isDisconnected, connectCommands,
                connectionInfo, host, typeTable);

            IsRemote = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get unique id for this instance of runspace pool. It is primarily used
        /// for logging purposes.
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return _internalPool.InstanceId;
            }
        }

        /// <summary>
        /// Gets a boolean which describes if the runspace pool is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return _internalPool.IsDisposed;
            }
        }

        /// <summary>
        /// Gets State of the current runspace pool.
        /// </summary>
        public RunspacePoolStateInfo RunspacePoolStateInfo
        {
            get
            {
                return _internalPool.RunspacePoolStateInfo;
            }
        }

        /// <summary>
        /// Gets the InitialSessionState object that this pool uses
        /// to create the runspaces.
        /// </summary>
        public InitialSessionState InitialSessionState
        {
            get
            {
                return _internalPool.InitialSessionState;
            }
        }

        /// <summary>
        /// Connection information for remote RunspacePools, null for local RunspacePools.
        /// </summary>
        public RunspaceConnectionInfo ConnectionInfo
        {
            get
            {
                return _internalPool.ConnectionInfo;
            }
        }

        /// <summary>
        /// Specifies how often unused runspaces are disposed.
        /// </summary>
        public TimeSpan CleanupInterval
        {
            get { return _internalPool.CleanupInterval; }

            set { _internalPool.CleanupInterval = value; }
        }

        /// <summary>
        /// Returns runspace pool availability.
        /// </summary>
        public RunspacePoolAvailability RunspacePoolAvailability
        {
            get { return _internalPool.RunspacePoolAvailability; }
        }

        #endregion

        #region events

        /// <summary>
        /// Event raised when RunspacePoolState changes.
        /// </summary>
        public event EventHandler<RunspacePoolStateChangedEventArgs> StateChanged
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = (InternalStateChanged == null);
                    InternalStateChanged += value;
                    if (firstEntry)
                    {
                        // call any event handlers on this object, replacing the
                        // internalPool sender with 'this' since receivers
                        // are expecting a RunspacePool.
                        _internalPool.StateChanged += OnStateChanged;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalStateChanged -= value;
                    if (InternalStateChanged == null)
                    {
                        _internalPool.StateChanged -= OnStateChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Handle internal Pool state changed events.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnStateChanged(object source, RunspacePoolStateChangedEventArgs args)
        {
            if (ConnectionInfo is NewProcessConnectionInfo)
            {
                NewProcessConnectionInfo connectionInfo = ConnectionInfo as NewProcessConnectionInfo;
                if (connectionInfo.Process != null &&
                    (args.RunspacePoolStateInfo.State == RunspacePoolState.Opened ||
                     args.RunspacePoolStateInfo.State == RunspacePoolState.Broken))
                {
                    connectionInfo.Process.RunspacePool = this;
                }
            }

            // call any event handlers on this, replacing the
            // internalPool sender with 'this' since receivers
            // are expecting a RunspacePool
            InternalStateChanged.SafeInvoke(this, args);
        }

        /// <summary>
        /// Event raised when one of the runspaces in the pool forwards an event to this instance.
        /// </summary>
        internal event EventHandler<PSEventArgs> ForwardEvent
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = InternalForwardEvent == null;

                    InternalForwardEvent += value;

                    if (firstEntry)
                    {
                        _internalPool.ForwardEvent += OnInternalPoolForwardEvent;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalForwardEvent -= value;

                    if (InternalForwardEvent == null)
                    {
                        _internalPool.ForwardEvent -= OnInternalPoolForwardEvent;
                    }
                }
            }
        }

        /// <summary>
        /// Pass thru of the ForwardEvent event from the internal pool.
        /// </summary>
        private void OnInternalPoolForwardEvent(object sender, PSEventArgs e)
        {
            OnEventForwarded(e);
        }

        /// <summary>
        /// Raises the ForwardEvent event.
        /// </summary>
        private void OnEventForwarded(PSEventArgs e)
        {
            InternalForwardEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Event raised when a new Runspace is created by the pool.
        /// </summary>
        internal event EventHandler<RunspaceCreatedEventArgs> RunspaceCreated
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = (InternalRunspaceCreated == null);
                    InternalRunspaceCreated += value;
                    if (firstEntry)
                    {
                        // call any event handlers on this object, replacing the
                        // internalPool sender with 'this' since receivers
                        // are expecting a RunspacePool.
                        _internalPool.RunspaceCreated += OnRunspaceCreated;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalRunspaceCreated -= value;
                    if (InternalRunspaceCreated == null)
                    {
                        _internalPool.RunspaceCreated -= OnRunspaceCreated;
                    }
                }
            }
        }

        /// <summary>
        /// Handle internal Pool RunspaceCreated events.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnRunspaceCreated(object source, RunspaceCreatedEventArgs args)
        {
            // call any event handlers on this, replacing the
            // internalPool sender with 'this' since receivers
            // are expecting a RunspacePool
            InternalRunspaceCreated.SafeInvoke(this, args);
        }

        #endregion events

        #region Public static methods.

        /// <summary>
        /// Queries the server for disconnected runspace pools and creates an array of runspace
        /// pool objects associated with each disconnected runspace pool on the server.  Each
        /// runspace pool object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace pool.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo)
        {
            return GetRunspacePools(connectionInfo, null, null);
        }

        /// <summary>
        /// Queries the server for disconnected runspace pools and creates an array of runspace
        /// pool objects associated with each disconnected runspace pool on the server.  Each
        /// runspace pool object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace pool.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host)
        {
            return GetRunspacePools(connectionInfo, host, null);
        }

        /// <summary>
        /// Queries the server for disconnected runspace pools and creates an array of runspace
        /// pool objects associated with each disconnected runspace pool on the server.  Each
        /// runspace pool object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace pool.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <param name="typeTable">TypeTable object.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            return RemoteRunspacePoolInternal.GetRemoteRunspacePools(connectionInfo, host, typeTable);
        }

        #endregion

        #region Public Disconnect-Connect API

        /// <summary>
        /// Disconnects the runspace pool synchronously.  Runspace pool must be in Opened state.
        /// </summary>
        public void Disconnect()
        {
            _internalPool.Disconnect();
        }

        /// <summary>
        /// Disconnects the runspace pool asynchronously.  Runspace pool must be in Opened state.
        /// </summary>
        /// <param name="callback">An AsyncCallback to call once the BeginClose completes.</param>
        /// <param name="state">A user supplied state to call the callback with.</param>
        public IAsyncResult BeginDisconnect(AsyncCallback callback, object state)
        {
            return _internalPool.BeginDisconnect(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginDisconnect to complete.
        /// </summary>
        /// <param name="asyncResult">Asynchronous call result object.</param>
        public void EndDisconnect(IAsyncResult asyncResult)
        {
            _internalPool.EndDisconnect(asyncResult);
        }

        /// <summary>
        /// Connects the runspace pool synchronously.  Runspace pool must be in disconnected state.
        /// </summary>
        public void Connect()
        {
            _internalPool.Connect();
        }

        /// <summary>
        /// Connects the runspace pool asynchronously.  Runspace pool must be in disconnected state.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            return _internalPool.BeginConnect(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginConnect to complete.
        /// </summary>
        /// <param name="asyncResult">Asynchronous call result object.</param>
        public void EndConnect(IAsyncResult asyncResult)
        {
            _internalPool.EndConnect(asyncResult);
        }

        /// <summary>
        /// Creates an array of PowerShell objects that are in the Disconnected state for
        /// all currently disconnected running commands associated with this runspace pool.
        /// </summary>
        /// <returns></returns>
        public Collection<PowerShell> CreateDisconnectedPowerShells()
        {
            return _internalPool.CreateDisconnectedPowerShells(this);
        }

        /// <summary>
        /// Returns RunspacePool capabilities.
        /// </summary>
        /// <returns>RunspacePoolCapability.</returns>
        public RunspacePoolCapability GetCapabilities()
        {
            return _internalPool.GetCapabilities();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the maximum number of Runspaces that can be active concurrently
        /// in the pool. All requests above that number remain queued until
        /// runspaces become available.
        /// </summary>
        /// <param name="maxRunspaces">
        /// The maximum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of runspaces to a number smaller than
        /// the minimum runspaces.
        /// </remarks>
        public bool SetMaxRunspaces(int maxRunspaces)
        {
            return _internalPool.SetMaxRunspaces(maxRunspaces);
        }

        /// <summary>
        /// Retrieves the maximum number of runspaces the pool maintains.
        /// </summary>
        /// <returns>
        /// The maximum number of runspaces in the pool
        /// </returns>
        public int GetMaxRunspaces()
        {
            return _internalPool.GetMaxRunspaces();
        }

        /// <summary>
        /// Sets the minimum number of Runspaces that the pool maintains
        /// in anticipation of new requests.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of idle runspaces to a number smaller than
        /// 1 or greater than maximum number of active runspaces.
        /// </remarks>
        public bool SetMinRunspaces(int minRunspaces)
        {
            return _internalPool.SetMinRunspaces(minRunspaces);
        }

        /// <summary>
        /// Retrieves the minimum number of runspaces the pool maintains.
        /// </summary>
        /// <returns>
        /// The minimum number of runspaces in the pool
        /// </returns>
        public int GetMinRunspaces()
        {
            return _internalPool.GetMinRunspaces();
        }

        /// <summary>
        /// Retrieves the number of runspaces available at the time of calling
        /// this method.
        /// </summary>
        /// <returns>
        /// The number of available runspace in the pool.
        /// </returns>
        public int GetAvailableRunspaces()
        {
            return _internalPool.GetAvailableRunspaces();
        }

        /// <summary>
        /// Opens the runspacepool synchronously. RunspacePool must
        /// be opened before it can be used.
        /// </summary>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen
        /// </exception>
        public void Open()
        {
            _internalPool.Open();
        }

        /// <summary>
        /// Opens the RunspacePool asynchronously. RunspacePool must
        /// be opened before it can be used.
        /// To get the exceptions that might have occurred, call
        /// EndOpen.
        /// </summary>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginOpen completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An AsyncResult object to monitor the state of the async
        /// operation.
        /// </returns>
        public IAsyncResult BeginOpen(AsyncCallback callback, object state)
        {
            return _internalPool.BeginOpen(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginOpen to complete.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginOpen
        /// on this runspacepool instance.
        /// </exception>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen.
        /// </exception>
        /// <remarks>
        /// TODO: Behavior if EndOpen is called multiple times.
        /// </remarks>
        public void EndOpen(IAsyncResult asyncResult)
        {
            _internalPool.EndOpen(asyncResult);
        }

        /// <summary>
        /// Closes the RunspacePool and cleans all the internal
        /// resources. This will close all the runspaces in the
        /// runspacepool and release all the async operations
        /// waiting for a runspace. If the pool is already closed
        /// or broken or closing this will just return.
        /// </summary>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot close the RunspacePool because RunspacePool is
        /// in Closing state.
        /// </exception>
        public void Close()
        {
            _internalPool.Close();
        }

        /// <summary>
        /// Closes the RunspacePool asynchronously and cleans all the internal
        /// resources. This will close all the runspaces in the
        /// runspacepool and release all the async operations
        /// waiting for a runspace. If the pool is already closed
        /// or broken or closing this will just return.
        /// </summary>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginClose completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An AsyncResult object to monitor the state of the async
        /// operation.
        /// </returns>
        public IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            return _internalPool.BeginClose(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginClose to complete.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginClose
        /// on this runspacepool instance.
        /// </exception>
        public void EndClose(IAsyncResult asyncResult)
        {
            _internalPool.EndClose(asyncResult);
        }

        /// <summary>
        /// Dispose the current runspacepool.
        /// </summary>
        public void Dispose()
        {
            _internalPool.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        ///
        /// Local runspace pool is created with application private data set to an empty <see cref="PSPrimitiveDictionary"/>.
        ///
        /// Remote runspace pool gets its application private data from the server (when creating the remote runspace pool)
        /// Calling this method on a remote runspace pool will block until the data is received from the server.
        /// The server will send application private data before reaching <see cref="RunspacePoolState.Opened"/> state.
        ///
        /// Runspaces that are part of a <see cref="RunspacePool"/> inherit application private data from the pool.
        /// </summary>
        public PSPrimitiveDictionary GetApplicationPrivateData()
        {
            return _internalPool.GetApplicationPrivateData();
        }

        #endregion

        #region Internal API

        /// <summary>
        /// This property determines whether a new thread is created for each invocation.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// An attempt to change this property was made after opening the RunspacePool
        /// </exception>
        public PSThreadOptions ThreadOptions
        {
            get
            {
                return _internalPool.ThreadOptions;
            }

            set
            {
                if (this.RunspacePoolStateInfo.State != RunspacePoolState.BeforeOpen)
                {
                    throw new InvalidRunspacePoolStateException(RunspacePoolStrings.ChangePropertyAfterOpen);
                }

                _internalPool.ThreadOptions = value;
            }
        }

        /// <summary>
        /// ApartmentState of the thread used to execute commands within this RunspacePool.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// An attempt to change this property was made after opening the RunspacePool
        /// </exception>
        public ApartmentState ApartmentState
        {
            get
            {
                return _internalPool.ApartmentState;
            }

            set
            {
                if (this.RunspacePoolStateInfo.State != RunspacePoolState.BeforeOpen)
                {
                    throw new InvalidRunspacePoolStateException(RunspacePoolStrings.ChangePropertyAfterOpen);
                }

                _internalPool.ApartmentState = value;
            }
        }

        /// <summary>
        /// Gets Runspace asynchronously from the runspace pool. The caller
        /// will get notified with the runspace using <paramref name="callback"/>
        /// </summary>
        /// <param name="callback">
        /// A AsyncCallback to call once the runspace is available.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An IAsyncResult object to track the status of the Async operation.
        /// </returns>
        internal IAsyncResult BeginGetRunspace(
            AsyncCallback callback, object state)
        {
            return _internalPool.BeginGetRunspace(callback, state);
        }

        /// <summary>
        /// Cancels the pending asynchronous BeginGetRunspace operation.
        /// </summary>
        /// <param name="asyncResult">
        /// </param>
        internal void CancelGetRunspace(IAsyncResult asyncResult)
        {
            _internalPool.CancelGetRunspace(asyncResult);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginGetRunspace to complete.
        /// </summary>
        /// <param name="asyncResult">
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginGetRunspace
        /// on this runspacepool instance.
        /// </exception>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen.
        /// </exception>
        internal Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            return _internalPool.EndGetRunspace(asyncResult);
        }

        /// <summary>
        /// Releases a Runspace to the pool. If pool is closed, this
        /// will be a no-op.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to release to the pool.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="runspace"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot release the runspace to this pool as the runspace
        /// doesn't belong to this pool.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// Only opened runspaces can be released back to the pool.
        /// </exception>
        internal void ReleaseRunspace(Runspace runspace)
        {
            _internalPool.ReleaseRunspace(runspace);
        }

        /// <summary>
        /// Indicates whether the RunspacePool is a remote one.
        /// </summary>
        internal bool IsRemote { get; } = false;

        /// <summary>
        /// RemoteRunspacePoolInternal associated with this
        /// runspace pool.
        /// </summary>
        internal RemoteRunspacePoolInternal RemoteRunspacePoolInternal
        {
            get
            {
                if (_internalPool is RemoteRunspacePoolInternal)
                {
                    return (RemoteRunspacePoolInternal)_internalPool;
                }
                else
                {
                    return null;
                }
            }
        }

        internal void AssertPoolIsOpen()
        {
            _internalPool.AssertPoolIsOpen();
        }

        #endregion
    }

    #endregion
}
