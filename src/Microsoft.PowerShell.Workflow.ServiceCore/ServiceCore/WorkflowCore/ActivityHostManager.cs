/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Management.Automation;
using Microsoft.PowerShell.Activities;
using System.Management.Automation.Tracing;
using System.Activities;
using System.Management.Automation.PerformanceData;
using System.Management.Automation.Remoting;

namespace Microsoft.PowerShell.Workflow
{
    internal class ConnectionAsyncResult : IAsyncResult
    {
        #region Private Members

        private readonly object _state;
        private readonly AsyncCallback _callback;
        private bool _isCompleted;
        private ManualResetEvent _completedWaitHandle;
        private readonly object _syncObject = new object();
        private Exception _exception;
        private readonly Guid _ownerId;
        #endregion Private Members

        #region Internal

        internal ConnectionAsyncResult(object state, AsyncCallback callback, Guid ownerId)
        {
            _state = state;
            _ownerId = ownerId;
            _callback = callback;
        }

        internal Object State
        {
            get { return _state; }
        }

        internal AsyncCallback Callback
        {
            get { return _callback; }
        }

        internal ActivityInvoker Invoker { get; set; }

        /// <summary>
        /// this method is not thread safe
        /// </summary>
        internal void SetAsCompleted(Exception exception)
        {
            if (_isCompleted) return;

            lock (_syncObject)
            {
                if (_isCompleted) return;

                _isCompleted = true;
                _exception = exception;
                if (_completedWaitHandle != null) _completedWaitHandle.Set();
            }

            // invoke callback if available
            if (null != _callback) 
                _callback(this);
        }

        internal Guid OwnerId
        {
            get { return _ownerId; }
        }

        internal void EndInvoke()
        {
            AsyncWaitHandle.WaitOne();
            AsyncWaitHandle.Close();
            _completedWaitHandle = null; // allow early GC

            if (null != _exception) throw _exception;
        }

        #endregion Internal

        #region Overrides

        /// <summary>
        /// Whether the operation represented by this method is 
        /// completed
        /// </summary>
        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        /// <summary>
        /// Wait Handle for the operation
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if(null == _completedWaitHandle)
                {
                    lock(_syncObject)
                    {
                        if (null == _completedWaitHandle)
                        {
                            _completedWaitHandle = new ManualResetEvent(_isCompleted);
                        }
                    }
                }

                return _completedWaitHandle;
            }
        }

        /// <summary>
        /// Optional user specified state
        /// </summary>
        public object AsyncState
        {
            get { return _state; }
        }

        /// <summary>
        /// Whether this operation completed synchronously
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return _completedSynchronously; }
            set { _completedSynchronously = value; }
        }

        private bool _completedSynchronously;

        #endregion Overrides
    }

    /// <summary>
    /// Activity Host Manager which will spawn a set of activity
    /// host processes until all of them are used after which
    /// it will start queueing requests
    /// </summary>
    /// <remarks>Whether this class needs to remain public should be 
    /// evaluated</remarks>
    internal sealed class PSOutOfProcessActivityController : PSActivityHostController
    {
        #region Private Members

        /// <summary>
        /// Stack of available host processes
        /// </summary>
        private readonly Collection<ActivityHostProcess> _hostProcesses = new Collection<ActivityHostProcess>();

        /// <summary>
        /// Queue of requests
        /// </summary>
        private readonly ConcurrentQueue<ActivityInvoker> _requests =
            new ConcurrentQueue<ActivityInvoker>();
        private int _isServicing;
        private const int Servicing = 1;
        private const int NotServicing = 0;
        private const int MinActivityHosts = 1;
        private int _busyHosts;
        private readonly Tracer _structuredTracer = new Tracer();
        private readonly PSWorkflowConfigurationProvider _configuration;
        private static readonly PSPerfCountersMgr PerfCountersMgr = PSPerfCountersMgr.Instance;


        /// <summary>
        /// List of requests that need to be invoked again due to process crash
        /// </summary>
        private readonly ConcurrentQueue<ActivityInvoker> _failedRequests = new ConcurrentQueue<ActivityInvoker>();

        #endregion Private Members

        #region Internal Methods

        internal void RunPowerShellInActivityHost(System.Management.Automation.PowerShell powershell, PSDataCollection<PSObject> input,
            PSDataCollection<PSObject> output, PSActivityEnvironment policy, ConnectionAsyncResult asyncResult)
        {
            ActivityInvoker invoker =
                new ActivityInvoker
                    {
                        Input = input,
                        Output = output,
                        Policy = policy,
                        PowerShell = powershell,
                        AsyncResult = asyncResult
                    };

            _requests.Enqueue(invoker);
            CheckAndStartServicingThread();
        }        

        /// <summary>
        /// This is a helper method which should only be called
        /// from test code to reset stuff
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is called from the tests")]
        internal void Reset()
        {
            foreach (ActivityHostProcess process in _hostProcesses)
            {
                process.Dispose();
            }
            _hostProcesses.Clear();

            InitializeActivityHostProcesses();
        }

        #endregion Internal Methods

        #region Constructors

        internal PSOutOfProcessActivityController(PSWorkflowRuntime runtime)
            : base(runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException("runtime");

            Debug.Assert(runtime.Configuration != null, "For now only expecting PSWorkflowConfigurationProvider");

            this._configuration = runtime.Configuration;
            InitializeActivityHostProcesses();
        }

        #endregion Constructors

        #region Private Methods

        private void InitializeActivityHostProcesses()
        {
            // create the minimum number of hosts
            for (int i = 0; i < MinActivityHosts; i++)
            {
                ActivityHostProcess process = CreateNewActivityHostProcess();
                _hostProcesses.Add(process);
            }                        
        }

        private ActivityHostProcess CreateNewActivityHostProcess()
        {
            ActivityHostProcess process = new ActivityHostProcess(_configuration.ActivityProcessIdleTimeoutSec, _configuration.LanguageMode);
            process.ProcessCrashed += ProcessCrashed;
            process.Finished += ProcessFinished;
            process.OnProcessIdle += ProcessIdle;

            PerfCountersMgr.UpdateCounterByValue(
                 PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                 PSWorkflowPerformanceCounterIds.ActivityHostMgrProcessesPoolSize);
            return process;
        }

        private void ProcessCrashed(object sender, ActivityHostCrashedEventArgs e)
        {
            // the process crashed, we need to not use it anymore
            ActivityHostProcess process = sender as ActivityHostProcess;
            Debug.Assert(process != null, "ActivityHostProcess did not raise event correctly");
            Debug.Assert(process.Busy, "When ProcessCrashed is raised busy should not be reset");
            process.MarkForRemoval = true;                

            if (e.FailureOnSetup)
            {
                // the request needs to be processed again
                _failedRequests.Enqueue(e.Invoker);
                PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrFailedRequestsPerSec);
                PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrFailedRequestsQueueLength);
            }

            // Below call is added to fix the race condition:
            // When OOP host process is crashed, ProcessCrashed() method sets the process as MarkForRemoval, context switch happened at this time
            // and another process has finished and started the servicing thread, which checks the above process as MarkForRemoval and disposes it. 
            // ProcessFinished event handler is unregistered from the process and ActivityHostProcess.HandleTransportError will not be able to raise
            // process finished event, resulting inconsistent _busyHost count.
            //
            DecrementHostCountAndStartThreads();
        }

        private void ProcessFinished(object sender, EventArgs e)
        {     
            DecrementHostCountAndStartThreads();
        }

        private void DecrementHostCountAndStartThreads()
        {
            Interlocked.Decrement(ref _busyHosts);
    
            PerfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.ActivityHostMgrBusyProcessesCount,
                -1);

            CheckAndStartServicingThread();
        }

        private void ProcessIdle(object sender, EventArgs e)
        {
            // Only service thread should access the hostProcess collection
            // Start servicing thread if it is not running already
            //
            CheckAndStartServicingThread();
        }

        /// <summary>
        /// Handler method which runs a specific command in the specified process
        /// </summary>
        /// <param name="state"></param>
        private static void RunPowerShellInActivityHostWorker(object state)
        {
            // Tuple object with HostProcess and ActivityInvoker is added to ThreadPool.QueueUserWorkItem as part of RunInProcess()
            // Back reference on ActivityHostProcess in ActivityInvoker is removed
            //
            Tuple<ActivityHostProcess, ActivityInvoker> tupleProcessAndInvoker = state as Tuple<ActivityHostProcess, ActivityInvoker>;
            tupleProcessAndInvoker.Item1.PrepareAndRun(tupleProcessAndInvoker.Item2);
        }

        /// <summary>
        /// Checks to see if a thread is servicing requests. If not
        /// starts one
        /// </summary>
        private void CheckAndStartServicingThread()
        {
            if (Interlocked.CompareExchange(ref _isServicing, Servicing, NotServicing) == NotServicing)
            {
                ThreadPool.QueueUserWorkItem(ServiceRequests);
            }
        }

        /// <summary>
        /// Method which performs the actual servicing of requests
        /// </summary>
        /// <param name="state"></param>
        private void ServiceRequests(object state)
        {
            bool isFailedRequest = false;
            while (Interlocked.CompareExchange(ref _busyHosts, _configuration.MaxActivityProcesses, _configuration.MaxActivityProcesses) < _configuration.MaxActivityProcesses)
            {
                // if there are any processes marked for removal
                // remove them
                List<ActivityHostProcess> toRemove = _hostProcesses.Where(process => process.MarkForRemoval).ToList();

                foreach(var process in toRemove)
                {
                    SafelyDisposeProcess(process);
                }                

                ActivityInvoker invoker;
                // first service previously failed request
                // and then a queued request
                if (_failedRequests.Count > 0)
                {
                    _failedRequests.TryDequeue(out invoker);
                    isFailedRequest = true;
                }
                else
                {
                    _requests.TryDequeue(out invoker);
                    isFailedRequest = false;
                }

                if (invoker == null) break;

                if (invoker.IsCancelled) continue;

                if (isFailedRequest)
                {
                    PerfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.ActivityHostMgrFailedRequestsQueueLength,
                        -1);
                }
                else
                {
                    PerfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.ActivityHostMgrPendingRequestsQueueLength,
                        -1);
                }

                bool processed = false;
                foreach (ActivityHostProcess process in _hostProcesses.Where(process => !process.Busy))
                {
                    // we have found the first free process available use it
                    processed = true;
                    RunInProcess(invoker, process);
                    break;
                }

                // if there weren't enough processes, then we create one more
                if (processed) continue;

                ActivityHostProcess hostProcess = CreateNewActivityHostProcess();
                _hostProcesses.Add(hostProcess);                
                RunInProcess(invoker, hostProcess);
            }

            // we are all done, set servicing to false
            Interlocked.CompareExchange(ref _isServicing, NotServicing, Servicing);

            // Try to start the servicing thread again if there are any pending requests and activity host processes are available.
            // This is required to fix the race condition between CheckAndStartServicingThread() and ServiceRequests() methods.
            //
            if((_failedRequests.Count > 0 || _requests.Count > 0) &&
                Interlocked.CompareExchange(ref _busyHosts, _configuration.MaxActivityProcesses, _configuration.MaxActivityProcesses) < _configuration.MaxActivityProcesses)
            {
                CheckAndStartServicingThread();
            }
        }

        
        /// <summary>
        /// Unregisters all wait handles and disposes a process
        /// </summary>
        /// <param name="process"></param>
        private void SafelyDisposeProcess(ActivityHostProcess process)
        {
            process.Finished -= ProcessFinished;
            process.ProcessCrashed -= ProcessCrashed;
            process.OnProcessIdle -= ProcessIdle;

            process.Dispose();

            _hostProcesses.Remove(process);

            PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrProcessesPoolSize,
                    -1);
        }

        /// <summary>
        /// Method called by servicing thread. This method will run the command in the 
        /// specified process on a separate thread
        /// </summary>
        /// <param name="invoker"></param>
        /// <param name="process"></param>
        private void RunInProcess(ActivityInvoker invoker, ActivityHostProcess process)
        {
            if (invoker.IsCancelled)
            {
                return;
            }

            process.Busy = true;
            Interlocked.Increment(ref _busyHosts);
            PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrBusyProcessesCount);

            // Adding Tuple object with HostProcess and ActivityInvoker to ThreadPool.QueueUserWorkItem
            // Back reference on ActivityHostProcess in ActivityInvoker is removed
            //
            Tuple<ActivityHostProcess, ActivityInvoker> tupleProcessAndInvoker = new Tuple<ActivityHostProcess, ActivityInvoker>(process, invoker);
            ThreadPool.QueueUserWorkItem(RunPowerShellInActivityHostWorker, tupleProcessAndInvoker);
        }

        #endregion Private Methods

        #region PSActivityHostManager Overrides

        /// <summary>
        /// Begin invocation of command specified in activity
        /// </summary>
        /// <param name="command">pipeline of command to execute</param>
        /// <param name="input">input collection</param>
        /// <param name="output">output collection</param>
        /// <param name="policy">policy to use for the activity</param>
        /// <param name="callback">optional callback</param>
        /// <param name="state">optional caller specified state</param>
        /// <returns>IAsyncResult</returns>
        internal IAsyncResult BeginInvokePowerShell(System.Management.Automation.PowerShell command, 
            PSDataCollection<PSObject> input, PSDataCollection<PSObject> output, PSActivityEnvironment policy, 
                AsyncCallback callback, object state)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            ConnectionAsyncResult result = new ConnectionAsyncResult(state, callback, command.InstanceId);

            _structuredTracer.OutOfProcessRunspaceStarted(command.ToString());

            ActivityInvoker invoker =
                new ActivityInvoker
                {
                    Input = input,
                    Output = output,
                    Policy = policy,
                    PowerShell = command,
                    AsyncResult = result
                };

            result.Invoker = invoker;

            _requests.Enqueue(invoker);
            PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrIncomingRequestsPerSec);
            PerfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrPendingRequestsQueueLength);
            CheckAndStartServicingThread();
            return result;
        }

        /// <summary>
        /// Block until operation is complete on the current thread
        /// </summary>
        /// <param name="asyncResult">IAsyncResult to block on</param>
        internal void EndInvokePowerShell(IAsyncResult asyncResult)
        {
            ConnectionAsyncResult result = asyncResult as ConnectionAsyncResult;

            if (result == null) 
            {
                throw new PSInvalidOperationException(Resources.AsyncResultNotValid);
            }

            result.EndInvoke();
        }

        /// <summary>
        /// Cancels an already started execution
        /// </summary>
        /// <param name="asyncResult">async result to cancel</param>
        internal void CancelInvokePowerShell(IAsyncResult asyncResult)
        {
            ConnectionAsyncResult result = asyncResult as ConnectionAsyncResult;
            if (result != null)
            {
                result.Invoker.StopPowerShell();
            }
        }

        #endregion PSActivityHostManager Overrides
    }

    #region ActivityInvoker

    internal class ActivityInvoker
    {
        internal PSDataCollection<PSObject> Input { get; set; }
        internal PSDataCollection<PSObject> Output { get; set; }
        internal System.Management.Automation.PowerShell PowerShell { get; set; }
        internal PSActivityEnvironment Policy { get; set; }
        internal ConnectionAsyncResult AsyncResult { get; set; }
        private readonly object _syncObject = new object();
        private bool _invoked;
        private bool _cancelled;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        internal void InvokePowerShell(Runspace runspace)
        {
            IAsyncResult result = null;

            if (_cancelled)
                return;

            Exception invokeException = null;

            try
            {
                // Acquiring sync object to fix a race condition between BeginInvoke() and Stop() operations on powershell object.
                // Invoked flag is set only when begin invoke is succeeded, so that if this activity is cancelled, powershell needs to be stopped in StopPowerShell().
                //
                lock (_syncObject)
                {
                    if (_cancelled)
                        return;

                    _tracer.WriteMessage("State of runspace passed to invoker ", runspace.RunspaceStateInfo.State.ToString());

                    // at this point we assume we have a clean runspace to
                    // run the command
                    // if the runspace is broken then the invocation will
                    // result in an error either ways
                    PowerShell.Runspace = runspace;

                    // Temporary fix:
                    // If we are about to run this out of proc, we need to complete any open
                    // input. Otherwise, the out-of-proc PowerShell will hang expecting input.
                    // However, this has a bug that it will break a third command trying to
                    // use the same input collection. Nana to resolve with final fix.
                    if ((Input != null) && Input.EnumeratorNeverBlocks && Input.IsOpen)
                    {
                        Input.Complete();
                    }

                    _tracer.WriteMessage("BEGIN invocation of command out of proc");
                    result = Output == null
                                    ? PowerShell.BeginInvoke(Input)
                                    : PowerShell.BeginInvoke(Input, Output);

                    // _invoked should bet set here as powershell object can be disposed before runspace assignment due to cancelling an activity
                    // 
                    _invoked = true;
                } // end of lock

                // wait until PowerShell Execution Completes
                PowerShell.EndInvoke(result);
                _tracer.WriteMessage("END invocation of command out of proc");
            }
            catch (Exception exception)
            {
                // Since the callback is internally handled there should
                // not be any exceptions. Fix the same
                // ignore any exceptions caused in the callback invocation
                _tracer.WriteMessage("Running powershell in activity host threw exception");
                _tracer.TraceException(exception);
                invokeException = exception;

                // transport errors are centrally handled for queue and process management
                if (exception is PSRemotingTransportException) throw;
            }
            finally
            {
                AsyncResult.SetAsCompleted(invokeException);
            }
        }

        internal void StopPowerShell()
        {
            bool needToStop = false;

            lock (_syncObject)
            {
                _cancelled = true;

                if (_invoked)
                {
                    needToStop = true;
                }
            }

            if (needToStop)
            {
                PowerShell.Stop();
            }

            AsyncResult.SetAsCompleted(null);
        }

        internal bool IsCancelled
        {
            get
            {
                lock (_syncObject)
                {
                    return _cancelled;
                }
            }
        }
    }

    #endregion ActivityInvoker

}
