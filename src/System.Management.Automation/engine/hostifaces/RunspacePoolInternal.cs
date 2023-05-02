// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Management.Automation.Tracing;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;
using PSHost = System.Management.Automation.Host.PSHost;

namespace System.Management.Automation.Runspaces.Internal
{
    /// <summary>
    /// Class which supports pooling local powerShell runspaces.
    /// </summary>
    internal class RunspacePoolInternal
    {
        #region Private data

        protected int maxPoolSz;
        protected int minPoolSz;
        // we need total active runspaces to avoid lock() statements everywhere
        protected int totalRunspaces;
        protected List<Runspace> runspaceList = new List<Runspace>(); // info of all the runspaces in the pool.
        protected Stack<Runspace> pool; // stack of runspaces that are available.
        protected Queue<GetRunspaceAsyncResult> runspaceRequestQueue; // request queue.
        // let requesters request on the runspaceRequestQueue..internally
        // pool services on this queue.
        protected Queue<GetRunspaceAsyncResult> ultimateRequestQueue;
        protected RunspacePoolStateInfo stateInfo;
        protected InitialSessionState _initialSessionState;
        protected PSHost host;
        protected Guid instanceId;
        private bool _isDisposed;
        protected bool isServicingRequests;
        protected object syncObject = new object();

        private static readonly TimeSpan s_defaultCleanupPeriod = new TimeSpan(0, 15, 0);   // 15 minutes.
        private TimeSpan _cleanupInterval;
        private readonly Timer _cleanupTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor which creates a RunspacePool using the
        /// supplied <paramref name="configuration"/>, <paramref name="minRunspaces"/>
        /// and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
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
        public RunspacePoolInternal(int minRunspaces,
                int maxRunspaces,
                PSHost host)
            : this(minRunspaces, maxRunspaces)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            this.host = host;
            pool = new Stack<Runspace>();
            runspaceRequestQueue = new Queue<GetRunspaceAsyncResult>();
            ultimateRequestQueue = new Queue<GetRunspaceAsyncResult>();
            _initialSessionState = InitialSessionState.CreateDefault();
        }

        /// <summary>
        /// Constructor which creates a RunspacePool using the
        /// supplied <paramref name="configuration"/>, <paramref name="minRunspaces"/>
        /// and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="initialSessionState">
        /// InitialSessionState to use when creating a new Runspace.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
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
        public RunspacePoolInternal(int minRunspaces,
                int maxRunspaces,
                InitialSessionState initialSessionState,
                PSHost host)
            : this(minRunspaces, maxRunspaces)
        {
            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(initialSessionState));
            }

            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            _initialSessionState = initialSessionState.Clone();
            this.host = host;
            ThreadOptions = initialSessionState.ThreadOptions;
            this.ApartmentState = initialSessionState.ApartmentState;
            pool = new Stack<Runspace>();
            runspaceRequestQueue = new Queue<GetRunspaceAsyncResult>();
            ultimateRequestQueue = new Queue<GetRunspaceAsyncResult>();
        }

        /// <summary>
        /// Constructor for doing common initialization between
        /// this class and its derivatives.
        /// </summary>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        protected RunspacePoolInternal(int minRunspaces, int maxRunspaces)
        {
            if (maxRunspaces < 1)
            {
                throw PSTraceSource.NewArgumentException(nameof(maxRunspaces), RunspacePoolStrings.MaxPoolLessThan1);
            }

            if (minRunspaces < 1)
            {
                throw PSTraceSource.NewArgumentException(nameof(minRunspaces), RunspacePoolStrings.MinPoolLessThan1);
            }

            if (minRunspaces > maxRunspaces)
            {
                throw PSTraceSource.NewArgumentException(nameof(minRunspaces), RunspacePoolStrings.MinPoolGreaterThanMaxPool);
            }

            maxPoolSz = maxRunspaces;
            minPoolSz = minRunspaces;
            stateInfo = new RunspacePoolStateInfo(RunspacePoolState.BeforeOpen, null);
            instanceId = Guid.NewGuid();
            PSEtwLog.SetActivityIdForCurrentThread(instanceId);

            _cleanupInterval = s_defaultCleanupPeriod;
            _cleanupTimer = new Timer(new TimerCallback(CleanupCallback), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal RunspacePoolInternal() { }

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
                return instanceId;
            }
        }

        /// <summary>
        /// Gets a boolean which describes if the runspace pool is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        /// <summary>
        /// Gets State of the current runspace pool.
        /// </summary>
        public RunspacePoolStateInfo RunspacePoolStateInfo
        {
            get
            {
                return stateInfo;
            }
        }

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        ///
        /// Local runspace pool is created with application private data set to an empty <see cref="PSPrimitiveDictionary"/>.
        /// </summary>
        internal virtual PSPrimitiveDictionary GetApplicationPrivateData()
        {
            if (_applicationPrivateData == null)
            {
                lock (this.syncObject)
                {
                    _applicationPrivateData ??= new PSPrimitiveDictionary();
                }
            }

            return _applicationPrivateData;
        }

        internal virtual void PropagateApplicationPrivateData(Runspace runspace)
        {
            runspace.SetApplicationPrivateData(this.GetApplicationPrivateData());
        }

        private PSPrimitiveDictionary _applicationPrivateData;

        /// <summary>
        /// Gets the InitialSessionState object that this pool uses
        /// to create the runspaces.
        /// </summary>
        public InitialSessionState InitialSessionState
        {
            get
            {
                return _initialSessionState;
            }
        }

        /// <summary>
        /// The connection associated with this runspace pool.
        /// </summary>
        public virtual RunspaceConnectionInfo ConnectionInfo
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Specifies how often unused runspaces are disposed.
        /// </summary>
        public TimeSpan CleanupInterval
        {
            get
            {
                return _cleanupInterval;
            }

            set
            {
                lock (this.syncObject)
                {
                    _cleanupInterval = value;
                }
            }
        }

        /// <summary>
        /// Returns runspace pool availability.
        /// </summary>
        public virtual RunspacePoolAvailability RunspacePoolAvailability
        {
            get
            {
                return (stateInfo.State == RunspacePoolState.Opened) ?
                    RunspacePoolAvailability.Available :
                    RunspacePoolAvailability.None;
            }
        }

        #endregion

        #region events

        /// <summary>
        /// Event raised when RunspacePoolState changes.
        /// </summary>
        public event EventHandler<RunspacePoolStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event raised when one of the runspaces in the pool forwards an event to this instance.
        /// </summary>
        public event EventHandler<PSEventArgs> ForwardEvent;

        /// <summary>
        /// Event raised when a new Runspace is created by the pool.
        /// </summary>
        internal event EventHandler<RunspaceCreatedEventArgs> RunspaceCreated;

        #endregion events

        #region Disconnect-Connect Methods

        /// <summary>
        /// Synchronously disconnect runspace pool.
        /// </summary>
        public virtual void Disconnect()
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Asynchronously disconnect runspace pool.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual IAsyncResult BeginDisconnect(AsyncCallback callback, object state)
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Wait for BeginDisconnect to complete.
        /// </summary>
        /// <param name="asyncResult"></param>
        public virtual void EndDisconnect(IAsyncResult asyncResult)
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Synchronously connect runspace pool.
        /// </summary>
        public virtual void Connect()
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Asynchronously connect runspace pool.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Wait for BeginConnect to complete.
        /// </summary>
        /// <param name="asyncResult"></param>
        public virtual void EndConnect(IAsyncResult asyncResult)
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Creates an array of PowerShell objects that are in the Disconnected state for
        /// all currently disconnected running commands associated with this runspace pool.
        /// </summary>
        /// <returns></returns>
        public virtual Collection<PowerShell> CreateDisconnectedPowerShells(RunspacePool runspacePool)
        {
            throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceDisconnectConnectNotSupported);
        }

        /// <summary>
        /// Returns RunspacePool capabilities.
        /// </summary>
        /// <returns>RunspacePoolCapability.</returns>
        public virtual RunspacePoolCapability GetCapabilities()
        {
            return RunspacePoolCapability.Default;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the runspace state on a runspace pool with a single
        /// runspace.
        /// This is currently supported *only* for remote runspaces.
        /// </summary>
        /// <returns>True if successful.</returns>
        internal virtual bool ResetRunspaceState()
        {
            throw new PSNotSupportedException();
        }

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
        internal virtual bool SetMaxRunspaces(int maxRunspaces)
        {
            bool isSizeIncreased = false;

            lock (pool)
            {
                if (maxRunspaces < this.minPoolSz)
                {
                    return false;
                }

                if (maxRunspaces > this.maxPoolSz)
                {
                    isSizeIncreased = true;
                }
                else
                {
                    // since maxrunspaces limit is decreased
                    // destroy unwanted runspaces from the top
                    // of the pool.
                    while (pool.Count > maxRunspaces)
                    {
                        Runspace rsToDestroy = pool.Pop();
                        DestroyRunspace(rsToDestroy);
                    }
                }

                maxPoolSz = maxRunspaces;
            }

            // pool size is incremented.. check if we can release
            // some requests.
            if (isSizeIncreased)
            {
                EnqueueCheckAndStartRequestServicingThread(null, false);
            }

            return true;
        }

        /// <summary>
        /// Retrieves the maximum number of runspaces the pool maintains.
        /// </summary>
        /// <returns>
        /// The maximum number of runspaces in the pool
        /// </returns>
        public int GetMaxRunspaces()
        {
            return maxPoolSz;
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
        internal virtual bool SetMinRunspaces(int minRunspaces)
        {
            lock (pool)
            {
                if ((minRunspaces < 1) || (minRunspaces > this.maxPoolSz))
                {
                    return false;
                }

                minPoolSz = minRunspaces;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the minimum number of runspaces the pool maintains.
        /// </summary>
        /// <returns>
        /// The minimum number of runspaces in the pool
        /// </returns>
        public int GetMinRunspaces()
        {
            return minPoolSz;
        }

        /// <summary>
        /// Retrieves the number of runspaces available at the time of calling
        /// this method.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the RunspacePool failed or has been closed
        /// </exception>
        /// <returns>
        /// The number of available runspace in the pool.
        /// </returns>
        internal virtual int GetAvailableRunspaces()
        {
            // Dont allow state changes while we get the count
            lock (syncObject)
            {
                if (stateInfo.State == RunspacePoolState.Opened)
                {
                    // Win8: 169492 RunspacePool can report that there are negative runspaces available.
                    // totalRunspaces represents all the runspaces that were ever created by ths RunspacePool
                    // pool.Count represents the runspaces that are currently available
                    // maxPoolSz represents the total capacity w.r.t runspaces for this RunspacePool
                    // Once the RunspacePool allocates a runspace to a consumer, RunspacePool cannot reclaim the
                    // runspace until the consumer released the runspace back to the pool. A SetMaxRunspaces()
                    // call can arrive before the runspace is released..It is bad to make SetMaxRunspaces()
                    // wait for the consumers to release runspaces, so we let SetMaxRunspaces() go by changing
                    // maxPoolSz. Because of this there may be cases where maxPoolSz - totalRunspaces will become
                    // less than 0.
                    int unUsedCapacity = (maxPoolSz - totalRunspaces) < 0 ? 0 : (maxPoolSz - totalRunspaces);
                    return (pool.Count + unUsedCapacity);
                }
                else if (stateInfo.State == RunspacePoolState.Disconnected)
                {
                    throw new InvalidOperationException(RunspacePoolStrings.CannotWhileDisconnected);
                }
                else if (stateInfo.State != RunspacePoolState.BeforeOpen && stateInfo.State != RunspacePoolState.Opening)
                {
                    throw new InvalidOperationException(HostInterfaceExceptionsStrings.RunspacePoolNotOpened);
                }
                else
                {
                    return maxPoolSz;
                }
            }
        }

        /// <summary>
        /// Opens the runspacepool synchronously. RunspacePool must
        /// be opened before it can be used.
        /// </summary>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen
        /// </exception>
        public virtual void Open()
        {
            CoreOpen(false, null, null);
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
            return CoreOpen(true, callback, state);
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
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            RunspacePoolAsyncResult rsAsyncResult = asyncResult as RunspacePoolAsyncResult;

            if ((rsAsyncResult == null) ||
                (rsAsyncResult.OwnerId != instanceId) ||
                (!rsAsyncResult.IsAssociatedWithAsyncOpen))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginOpen");
            }

            rsAsyncResult.EndInvoke();
        }

        /// <summary>
        /// Closes the RunspacePool and cleans all the internal
        /// resources. This will close all the runspaces in the
        /// runspacepool and release all the async operations
        /// waiting for a runspace. If the pool is already closed
        /// or broken or closing this will just return.
        /// </summary>
        public virtual void Close()
        {
            CoreClose(false, null, null);
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
        public virtual IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            return CoreClose(true, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginClose to complete.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginClose
        /// on this runspacepool instance.
        /// </exception>
        /// <remarks>
        /// TODO: Behavior if EndClose is called multiple times.
        /// </remarks>
        public virtual void EndClose(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            RunspacePoolAsyncResult rsAsyncResult = asyncResult as RunspacePoolAsyncResult;

            if ((rsAsyncResult == null) ||
                (rsAsyncResult.OwnerId != instanceId) ||
                (rsAsyncResult.IsAssociatedWithAsyncOpen))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginClose");
            }

            rsAsyncResult.EndInvoke();
        }

        /// <summary>
        /// Gets a Runspace from the pool. If no free runspace is available
        /// and if max pool size is not reached, a new runspace is created.
        /// Otherwise this will block a runspace is released and available.
        /// </summary>
        /// <returns>
        /// An opened Runspace.
        /// </returns>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot perform operation because RunspacePool is
        /// not in the opened state.
        /// </exception>
        public Runspace GetRunspace()
        {
            AssertPoolIsOpen();
            // Get the runspace asynchronously.
            GetRunspaceAsyncResult asyncResult = (GetRunspaceAsyncResult)BeginGetRunspace(null, null);
            // Wait for async operation to complete.
            asyncResult.AsyncWaitHandle.WaitOne();

            // throw the exception that occurred while
            // processing the async operation
            if (asyncResult.Exception != null)
            {
                throw asyncResult.Exception;
            }

            return asyncResult.Runspace;
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
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Runspool is not in Opened state.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot release the runspace to this pool as the runspace
        /// doesn't belong to this pool.
        /// </exception>
        public void ReleaseRunspace(Runspace runspace)
        {
            if (runspace == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runspace));
            }

            AssertPoolIsOpen();

            bool isRunspaceReleased = false;
            bool destroyRunspace = false;

            // check if the runspace is owned by the pool
            lock (runspaceList)
            {
                if (!runspaceList.Contains(runspace))
                {
                    throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.RunspaceNotBelongsToPool);
                }
            }

            // Release this runspace only if it is in valid state and is
            // owned by this pool.
            if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
            {
                lock (pool)
                {
                    if (pool.Count < maxPoolSz)
                    {
                        isRunspaceReleased = true;
                        pool.Push(runspace);
                    }
                    else
                    {
                        // this runspace is not going to be pooled as maxPoolSz is reduced.
                        // so release the runspace and destroy it.
                        isRunspaceReleased = true;
                        destroyRunspace = true;
                    }
                }
            }
            else
            {
                destroyRunspace = true;
                isRunspaceReleased = true;
            }

            if (destroyRunspace)
            {
                // Destroying a runspace might be costly.
                // so doing this outside of the lock.
                DestroyRunspace(runspace);
            }

            // it is important to release lock on Pool so that
            // other threads can service requests.
            if (isRunspaceReleased)
            {
                // service any pending runspace requests.
                EnqueueCheckAndStartRequestServicingThread(null, false);
            }
        }

        /// <summary>
        /// Dispose off the current runspace pool.
        /// </summary>
        /// <param name="disposing">
        /// true to release all the internal resources.
        /// </param>
        public virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Close();
                    _cleanupTimer.Dispose();
                    _initialSessionState = null;
                    host = null;
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// The value of this property is propagated to all the Runspaces in this pool;
        /// it determines whether a new thread is create when a pipeline is executed.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        internal PSThreadOptions ThreadOptions { get; set; } = PSThreadOptions.Default;

        /// <summary>
        /// The value of this property is propagated to all the Runspaces in this pool.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        internal ApartmentState ApartmentState { get; set; } = Runspace.DefaultApartmentState;

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
            AssertPoolIsOpen();

            GetRunspaceAsyncResult asyncResult = new GetRunspaceAsyncResult(this.InstanceId,
                callback, state);

            // Enqueue and start servicing thread in one go..saving multiple locks.
            EnqueueCheckAndStartRequestServicingThread(asyncResult, true);

            return asyncResult;
        }

        /// <summary>
        /// Cancels the pending asynchronous BeginGetRunspace operation.
        /// </summary>
        /// <param name="asyncResult">
        /// </param>
        internal void CancelGetRunspace(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            GetRunspaceAsyncResult grsAsyncResult =
                asyncResult as GetRunspaceAsyncResult;

            if ((grsAsyncResult == null) || (grsAsyncResult.OwnerId != instanceId))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginGetRunspace");
            }

            grsAsyncResult.IsActive = false;
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
        /// <remarks>
        /// TODO: Behavior if EndGetRunspace is called multiple times.
        /// </remarks>
        internal Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            GetRunspaceAsyncResult grsAsyncResult =
                asyncResult as GetRunspaceAsyncResult;

            if ((grsAsyncResult == null) || (grsAsyncResult.OwnerId != instanceId))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginGetRunspace");
            }

            grsAsyncResult.EndInvoke();
            return grsAsyncResult.Runspace;
        }

        /// <summary>
        /// Opens the runspacepool synchronously / asynchronously.
        /// Runspace pool must be opened before it can be used.
        /// </summary>
        /// <param name="isAsync">
        /// true to open asynchronously
        /// </param>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginOpen completes.
        /// </param>
        /// <param name="asyncState">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// asyncResult object to monitor status of the async
        /// open operation. This is returned only if <paramref name="isAsync"/>
        /// is true.
        /// </returns>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot open RunspacePool because RunspacePool is not in
        /// the BeforeOpen state.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// There is not enough memory available to start this asynchronously.
        /// </exception>
        protected virtual IAsyncResult CoreOpen(bool isAsync, AsyncCallback callback,
            object asyncState)
        {
            lock (syncObject)
            {
                AssertIfStateIsBeforeOpen();

                stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Opening, null);
            }

            // only one thread will reach here, so no
            // need to lock.
            RaiseStateChangeEvent(stateInfo);

            if (isAsync)
            {
                AsyncResult asyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, true);
                // Open pool in another thread
                ThreadPool.QueueUserWorkItem(new WaitCallback(OpenThreadProc), asyncResult);
                return asyncResult;
            }

            // open the runspace synchronously
            OpenHelper();
            return null;
        }

        /// <summary>
        /// Creates a Runspace + opens it synchronously and
        /// pushes it into the stack.
        /// </summary>
        /// <remarks>
        /// Caller to make sure this is thread safe.
        /// </remarks>
        protected void OpenHelper()
        {
            try
            {
                PSEtwLog.SetActivityIdForCurrentThread(this.InstanceId);
                // Create a Runspace and store it in the pool
                // for future use. This will validate whether
                // a runspace can be created + opened successfully
                Runspace rs = CreateRunspace();
                pool.Push(rs);
            }
            catch (Exception exception)
            {
                SetStateToBroken(exception);
                // rethrow the exception
                throw;
            }

            bool shouldRaiseEvents = false;
            // RunspacePool might be closed while we are still opening
            // we should not change state from closed to opened..
            lock (syncObject)
            {
                if (stateInfo.State == RunspacePoolState.Opening)
                {
                    // Change state to opened and notify the user.
                    stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Opened, null);
                    shouldRaiseEvents = true;
                }
            }

            if (shouldRaiseEvents)
            {
                RaiseStateChangeEvent(stateInfo);
            }
        }

        private void SetStateToBroken(Exception reason)
        {
            bool shouldRaiseEvents = false;
            lock (syncObject)
            {
                if ((stateInfo.State == RunspacePoolState.Opening) ||
                    (stateInfo.State == RunspacePoolState.Opened) ||
                    (stateInfo.State == RunspacePoolState.Disconnecting) ||
                    (stateInfo.State == RunspacePoolState.Disconnected) ||
                    (stateInfo.State == RunspacePoolState.Connecting))
                {
                    stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Broken, null);
                    shouldRaiseEvents = true;
                }
            }

            if (shouldRaiseEvents)
            {
                RunspacePoolStateInfo stateInfo = new RunspacePoolStateInfo(this.stateInfo.State,
                    reason);
                RaiseStateChangeEvent(stateInfo);
            }
        }

        /// <summary>
        /// Starting point for asynchronous thread.
        /// </summary>
        /// <remarks>
        /// asyncResult object
        /// </remarks>
        protected void OpenThreadProc(object o)
        {
            Dbg.Assert(o is AsyncResult, "OpenThreadProc expects AsyncResult");
            // Since this is an internal method, we can safely cast the
            // object to AsyncResult object.
            AsyncResult asyncObject = (AsyncResult)o;
            // variable to keep track of exceptions.
            Exception exception = null;

            try
            {
                OpenHelper();
            }
            catch (Exception e)
            {
                // report non-severe exceptions to the user via the
                // asyncresult object
                exception = e;
            }
            finally
            {
                asyncObject.SetAsCompleted(exception);
            }
        }

        /// <summary>
        /// Closes the runspacepool synchronously / asynchronously.
        /// </summary>
        /// <param name="isAsync">
        /// true to close asynchronously
        /// </param>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginClose completes.
        /// </param>
        /// <param name="asyncState">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// asyncResult object to monitor status of the async
        /// open operation. This is returned only if <paramref name="isAsync"/>
        /// is true.
        /// </returns>
        private IAsyncResult CoreClose(bool isAsync, AsyncCallback callback, object asyncState)
        {
            lock (syncObject)
            {
                if ((stateInfo.State == RunspacePoolState.Closed) ||
                    (stateInfo.State == RunspacePoolState.Broken) ||
                    (stateInfo.State == RunspacePoolState.Closing) ||
                    (stateInfo.State == RunspacePoolState.Disconnecting) ||
                    (stateInfo.State == RunspacePoolState.Disconnected))
                {
                    if (isAsync)
                    {
                        RunspacePoolAsyncResult asyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                        asyncResult.SetAsCompleted(null);
                        return asyncResult;
                    }
                    else
                    {
                        return null;
                    }
                }

                stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Closing, null);
            }

            // only one thread will reach here.
            RaiseStateChangeEvent(stateInfo);

            if (isAsync)
            {
                RunspacePoolAsyncResult asyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                // Open pool in another thread
                ThreadPool.QueueUserWorkItem(new WaitCallback(CloseThreadProc), asyncResult);
                return asyncResult;
            }

            // open the runspace synchronously
            CloseHelper();
            return null;
        }

        private void CloseHelper()
        {
            try
            {
                InternalClearAllResources();
            }
            finally
            {
                stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Closed, null);
                RaiseStateChangeEvent(stateInfo);
            }
        }

        private void CloseThreadProc(object o)
        {
            Dbg.Assert(o is AsyncResult, "CloseThreadProc expects AsyncResult");
            // Since this is an internal method, we can safely cast the
            // object to AsyncResult object.
            AsyncResult asyncObject = (AsyncResult)o;
            // variable to keep track of exceptions.
            Exception exception = null;

            try
            {
                CloseHelper();
            }
            catch (Exception e)
            {
                // report non-severe exceptions to the user via the
                // asyncresult object
                exception = e;
            }
            finally
            {
                asyncObject.SetAsCompleted(exception);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Raise state changed event based on the StateInfo
        /// object.
        /// </summary>
        /// <param name="stateInfo">State information object.</param>
        protected void RaiseStateChangeEvent(RunspacePoolStateInfo stateInfo)
        {
            StateChanged.SafeInvoke(this,
                new RunspacePoolStateChangedEventArgs(stateInfo));
        }

        /// <summary>
        /// Checks if the Pool is open to honour requests.
        /// If not throws an exception.
        /// </summary>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot perform operation because RunspacePool is
        /// not in the opened state.
        /// </exception>
        internal void AssertPoolIsOpen()
        {
            lock (syncObject)
            {
                if (stateInfo.State != RunspacePoolState.Opened)
                {
                    string message = StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolState, RunspacePoolState.Opened, stateInfo.State);
                    throw new InvalidRunspacePoolStateException(message,
                                     stateInfo.State, RunspacePoolState.Opened);
                }
            }
        }

        /// <summary>
        /// Creates a new Runspace and initializes it by calling Open()
        /// </summary>
        /// <returns>
        /// An opened Runspace.
        /// </returns>
        /// <remarks>
        /// TODO: Exceptions thrown here need to be documented.
        /// </remarks>
        protected Runspace CreateRunspace()
        {
            Dbg.Assert(_initialSessionState != null, "_initialSessionState should not be null");
            // TODO: exceptions thrown here need to be documented
            // runspace.Open() did not document all the exceptions.
            Runspace result = RunspaceFactory.CreateRunspaceFromSessionStateNoClone(host, _initialSessionState);

            result.ThreadOptions = this.ThreadOptions == PSThreadOptions.Default ? PSThreadOptions.ReuseThread : this.ThreadOptions;
            result.ApartmentState = this.ApartmentState;

            this.PropagateApplicationPrivateData(result);

            result.Open();

            // Enforce the system lockdown policy if one is defined.
            Utils.EnforceSystemLockDownLanguageMode(result.ExecutionContext);

            result.Events.ForwardEvent += OnRunspaceForwardEvent; // this must be done after open since open initializes the ExecutionContext

            lock (runspaceList)
            {
                runspaceList.Add(result);
                totalRunspaces = runspaceList.Count;
            }

            // Start/Reset the cleanup timer to release idle runspaces in the pool.
            lock (this.syncObject)
            {
                _cleanupTimer.Change(CleanupInterval, CleanupInterval);
            }

            // raise the RunspaceCreated event and let callers handle it.
            RunspaceCreated.SafeInvoke(this, new RunspaceCreatedEventArgs(result));

            return result;
        }

        /// <summary>
        /// Cleans/Closes the runspace.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to be closed/cleaned
        /// </param>
        protected void DestroyRunspace(Runspace runspace)
        {
            Dbg.Assert(runspace != null, "Runspace cannot be null");
            runspace.Events.ForwardEvent -= OnRunspaceForwardEvent; // this must be done after open since open initializes the ExecutionContext
            runspace.Close();
            runspace.Dispose();
            lock (runspaceList)
            {
                runspaceList.Remove(runspace);
                totalRunspaces = runspaceList.Count;
            }
        }

        /// <summary>
        /// Cleans the pool closing the runspaces that are idle.
        /// This method is called as part of a timer callback.
        /// This method will make sure at least minPoolSz number
        /// of Runspaces are active.
        /// </summary>
        /// <param name="state"></param>
        protected void CleanupCallback(object state)
        {
            Dbg.Assert((this.stateInfo.State != RunspacePoolState.Disconnected &&
                        this.stateInfo.State != RunspacePoolState.Disconnecting &&
                        this.stateInfo.State != RunspacePoolState.Connecting),
                       "Local RunspacePool cannot be in disconnect/connect states");

            bool isCleanupTimerChanged = false;
            // Clean up the pool only if more runspaces
            // than minimum requested are present.
            while (totalRunspaces > minPoolSz)
            {
                // if the pool is closing just return..
                if (this.stateInfo.State == RunspacePoolState.Closing)
                {
                    return;
                }

                // This is getting run on a threadpool thread
                // it is ok to take the hit on locking and unlocking.
                // this will release request threads depending
                // on thread scheduling
                Runspace runspaceToDestroy = null;
                lock (pool)
                {
                    if (pool.Count == 0)
                    {
                        break; // break from while
                    }

                    runspaceToDestroy = pool.Pop();
                }

                // Stop the clean up timer only when we are about to clean runspaces.
                // It will be restarted when a new runspace is created.
                if (!isCleanupTimerChanged)
                {
                    lock (this.syncObject)
                    {
                        _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        isCleanupTimerChanged = true;
                    }
                }

                // destroy runspace outside of the lock
                DestroyRunspace(runspaceToDestroy);
                continue;
            }
        }

        /// <summary>
        /// Close all the runspaces in the pool.
        /// </summary>
        private void InternalClearAllResources()
        {
            string message = StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolState, RunspacePoolState.Opened, stateInfo.State);
            Exception invalidStateException = new InvalidRunspacePoolStateException(message,
                 stateInfo.State, RunspacePoolState.Opened);
            GetRunspaceAsyncResult runspaceRequester;

            // clear the request queue first..this way waiting threads
            // are immediately notified.
            lock (runspaceRequestQueue)
            {
                while (runspaceRequestQueue.Count > 0)
                {
                    runspaceRequester = runspaceRequestQueue.Dequeue();
                    runspaceRequester.SetAsCompleted(invalidStateException);
                }
            }

            lock (ultimateRequestQueue)
            {
                while (ultimateRequestQueue.Count > 0)
                {
                    runspaceRequester = ultimateRequestQueue.Dequeue();
                    runspaceRequester.SetAsCompleted(invalidStateException);
                }
            }

            // close all the runspaces
            List<Runspace> runspaceListCopy = new List<Runspace>();

            lock (runspaceList)
            {
                runspaceListCopy.AddRange(runspaceList);
                runspaceList.Clear();
            }

            // Start from the most recent runspace.
            for (int index = runspaceListCopy.Count - 1; index >= 0; index--)
            {
                // close runspaces suppress exceptions
                try
                {
                    // this will release pipelines executing in the
                    // runspace.
                    runspaceListCopy[index].Close();
                    runspaceListCopy[index].Dispose();
                }
                catch (InvalidRunspaceStateException)
                {
                }
            }

            lock (pool)
            {
                pool.Clear();
            }

            // dont release pool/runspacelist/runspaceRequestQueue/ultimateRequestQueue as they
            // might be accessed in lock() statements from another thread.
        }

        /// <summary>
        /// If <paramref name="requestToEnqueue"/> is not null, enqueues the request.
        /// Checks if a thread pool thread is queued to service pending requests
        /// for runspace. If a thread is not queued, queues one.
        /// </summary>
        /// <param name="requestToEnqueue">
        /// Used by calling threads to queue a request before checking and starting
        /// servicing thread.
        /// </param>
        /// <param name="useCallingThread">
        /// uses calling thread to assign available runspaces (if any) to runspace
        /// requesters.
        /// </param>
        protected void EnqueueCheckAndStartRequestServicingThread(GetRunspaceAsyncResult requestToEnqueue,
            bool useCallingThread)
        {
            bool shouldStartServicingInSameThread = false;
            lock (runspaceRequestQueue)
            {
                if (requestToEnqueue != null)
                {
                    runspaceRequestQueue.Enqueue(requestToEnqueue);
                }

                // if a thread is already servicing requests..just return.
                if (isServicingRequests)
                {
                    return;
                }

                if ((runspaceRequestQueue.Count + ultimateRequestQueue.Count) > 0)
                {
                    // we have requests pending..check if a runspace is available to
                    // service the requests.
                    lock (pool)
                    {
                        if ((pool.Count > 0) || (totalRunspaces < maxPoolSz))
                        {
                            isServicingRequests = true;
                            if ((useCallingThread) && (ultimateRequestQueue.Count == 0))
                            {
                                shouldStartServicingInSameThread = true;
                            }
                            else
                            {
                                // release a async result object using a thread pool thread.
                                // this way the calling thread will not block.
                                ThreadPool.QueueUserWorkItem(new WaitCallback(ServicePendingRequests), false);
                            }
                        }
                    }
                }
            }

            // only one thread will be here if any..
            // This will allow us to release lock.
            if (shouldStartServicingInSameThread)
            {
                ServicePendingRequests(true);
            }
        }

        /// <summary>
        /// Releases any readers in the reader queue waiting for
        /// Runspace.
        /// </summary>
        /// <param name="useCallingThreadState">
        /// This is of type object..because this method is called from a ThreadPool
        /// Thread.
        /// true, if calling thread should be used to assign a runspace.
        /// </param>
        protected void ServicePendingRequests(object useCallingThreadState)
        {
            // Check if the pool is closed or closing..if so return.
            if ((stateInfo.State == RunspacePoolState.Closed) || (stateInfo.State == RunspacePoolState.Closing))
            {
                return;
            }

            Dbg.Assert((this.stateInfo.State != RunspacePoolState.Disconnected &&
                        this.stateInfo.State != RunspacePoolState.Disconnecting &&
                        this.stateInfo.State != RunspacePoolState.Connecting),
                       "Local RunspacePool cannot be in disconnect/connect states");

            bool useCallingThread = (bool)useCallingThreadState;
            GetRunspaceAsyncResult runspaceRequester = null;

            try
            {
                while (true)
                {
                    lock (ultimateRequestQueue)
                    {
                        while (ultimateRequestQueue.Count > 0)
                        {
                            // if the pool is closing just return..
                            if (this.stateInfo.State == RunspacePoolState.Closing)
                            {
                                return;
                            }

                            Runspace result;
                            lock (pool)
                            {
                                if (pool.Count > 0)
                                {
                                    result = pool.Pop();
                                }
                                else if (totalRunspaces >= maxPoolSz)
                                {
                                    // no runspace is available..
                                    return;
                                }
                                else
                                {
                                    // TODO: how to handle exceptions if runspace
                                    // creation fails.
                                    // Create a new runspace..since the max limit is
                                    // not reached.
                                    result = CreateRunspace();
                                }
                            }

                            // Dequeue a runspace request
                            runspaceRequester = ultimateRequestQueue.Dequeue();
                            // if the runspace is not active send the runspace back to
                            // the pool and process other requests
                            if (!runspaceRequester.IsActive)
                            {
                                lock (pool)
                                {
                                    pool.Push(result);
                                }
                                // release the runspace requester
                                runspaceRequester.Release();
                                continue;
                            }
                            // release readers waiting for runspace on a thread pool
                            // thread.
                            runspaceRequester.Runspace = result;
                            // release the async operation on a thread pool thread.
                            if (useCallingThread)
                            {
                                // call DoComplete outside of the lock..as the
                                // DoComplete handler may handle the runspace
                                // in the same thread thereby blocking future
                                // servicing requests.
                                goto endOuterWhile;
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(runspaceRequester.DoComplete));
                            }
                        }
                    }

                    lock (runspaceRequestQueue)
                    {
                        if (runspaceRequestQueue.Count == 0)
                        {
                            break;
                        }

                        // copy requests from one queue to another and start
                        // processing the other queue
                        while (runspaceRequestQueue.Count > 0)
                        {
                            ultimateRequestQueue.Enqueue(runspaceRequestQueue.Dequeue());
                        }
                    }
                }
            endOuterWhile:;
            }
            finally
            {
                lock (runspaceRequestQueue)
                {
                    isServicingRequests = false;
                    // check if any new runspace request has arrived..
                    EnqueueCheckAndStartRequestServicingThread(null, false);
                }
            }

            if ((useCallingThread) && (runspaceRequester != null))
            {
                // call DoComplete outside of the lock and finally..as the
                // DoComplete handler may handle the runspace in the same
                // thread thereby blocking future servicing requests.
                runspaceRequester.DoComplete(null);
            }
        }

        /// <summary>
        /// Throws an exception if the runspace state is not
        /// BeforeOpen.
        /// </summary>
        protected void AssertIfStateIsBeforeOpen()
        {
            if (stateInfo.State != RunspacePoolState.BeforeOpen)
            {
                // Call fails if RunspacePoolState is not BeforeOpen.
                InvalidRunspacePoolStateException e =
                    new InvalidRunspacePoolStateException
                    (
                        StringUtil.Format(RunspacePoolStrings.CannotOpenAgain,
                            new object[] { stateInfo.State.ToString() }
                        ),
                        stateInfo.State,
                        RunspacePoolState.BeforeOpen
                    );
                throw e;
            }
        }

        /// <summary>
        /// Raises the ForwardEvent event.
        /// </summary>
        protected virtual void OnForwardEvent(PSEventArgs e)
        {
            this.ForwardEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Forward runspace events to the pool's event queue.
        /// </summary>
        private void OnRunspaceForwardEvent(object sender, PSEventArgs e)
        {
            if (e.ForwardEvent)
            {
                OnForwardEvent(e);
            }
        }

        #endregion
    }
}
