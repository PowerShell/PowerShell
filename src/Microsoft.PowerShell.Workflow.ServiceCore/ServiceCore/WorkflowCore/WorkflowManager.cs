/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.WSMan;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Activities;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Management.Automation;
    using System.Management.Automation.Tracing;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Management.Automation.Runspaces;
    using System.Management.Automation.PerformanceData;
    using System.Threading;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Provides interface to upper layers of M3P for calling the Workflow core functionality.
    /// Throttles the number of jobs run simultaneously. Used to control the number of workflows that will execute simultaneously
    /// </summary>
    public sealed class PSWorkflowJobManager: IDisposable
    {
        #region Private Members

        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static readonly Tracer StructuredTracer = new Tracer();

        private readonly PSWorkflowRuntime _runtime;
        /// <summary>
        /// The class name.
        /// </summary>
        private const string Facility = "WorkflowManager : ";
        private LockObjectsCollection lockObjects = new LockObjectsCollection();

        #endregion Private Members

        #region Job Throttle related Private Members

        private readonly ConcurrentQueue<Tuple<Action<object>, object, JobState>> _pendingQueue = new ConcurrentQueue<Tuple<Action<object>, object, JobState>>();
        private static readonly PSPerfCountersMgr PerfCountersMgr = PSPerfCountersMgr.Instance;
        private readonly int _throttleLimit;
        private int _inProgressCount;

        private const int WaitInterval = 5 * 1000;

        // Timer to call WSManPluginOperationComplete on zero active sessions and no jobs are inprogress or in pending queue
        private Timer _shutdownTimer;

        // Plugin/Endpoint process is created during the first shell session creation, so the default value of _activeSessionsCount is 1
        private int _activeSessionsCount = 1;

        /// <summary>
        /// this is set to a date in the past so that the first time
        /// GC happens correctly
        /// </summary>
        private static DateTime _lastGcTime = new DateTime(2011, 1, 1);
        private const int GcDelayInMinutes = 5;
        private static int _workflowsBeforeGc;
        private static int _gcStatus = NotInProgress;
        private const int InProgress = 1;
        private const int NotInProgress = 0;
        private static readonly Tracer etwTracer = new Tracer();

        /// <summary>
        /// if these many workflows are run without a GC
        /// in between them, then we will force a GC
        /// </summary>
        private const int WorkflowLimitBeforeGc = 25 * 5;

        #endregion Job Throttle related Private Members

        // For testing purpose ONLY
        internal static bool TestMode = false;
        // For testing purpose ONLY
        internal static long ObjectCounter = 0;

        private bool _isDisposed;
        private bool _isDisposing;
        private readonly object _syncObject = new object();

        #region WorkflowManager Management

        /// <summary>
        /// Construct a workflow manager
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="throttleLimit"></param>
        public PSWorkflowJobManager(PSWorkflowRuntime runtime, int throttleLimit)
        {
            if (runtime == null) 
            {
                throw new ArgumentNullException("runtime");
            }

            if (TestMode)
            {
                System.Threading.Interlocked.Increment(ref ObjectCounter);
            }
            
            _runtime = runtime;
            _throttleLimit = throttleLimit;

            if (PSWorkflowSessionConfiguration.IsWorkflowTypeEndpoint)
            {
                _shutdownTimer = new Timer(_runtime.Configuration.WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec);
                _shutdownTimer.Elapsed += ShutdownWaitTimerElapsed;
                _shutdownTimer.AutoReset = false;

                WSManServerChannelEvents.ActiveSessionsChanged += OnWSManServerActiveSessionsChangedEvent;
            }
        }


        /// <summary>
        /// Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            lock (_syncObject)
            {
                if (_isDisposed)
                    return;

                _isDisposing = true;

                // To release the job throttling thread
                _waitForJobs.Value.Set();

                ShutdownWorkflowManager();

                // Set _waitForJobs to free the service thread
                _waitForJobs.Value.Dispose();

                if (PSWorkflowSessionConfiguration.IsWorkflowTypeEndpoint)
                {
                    WSManServerChannelEvents.ActiveSessionsChanged -= OnWSManServerActiveSessionsChangedEvent;
                }

                if (_shutdownTimer != null)
                {
                    _shutdownTimer.Elapsed -= ShutdownWaitTimerElapsed;
                    _shutdownTimer.Close();
                    _shutdownTimer.Dispose();
                    _shutdownTimer = null;
                }

                _isDisposed = true;
            }
        }
        
        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PSWorkflowJobManager");
            }
        }

        /// <summary>
        /// LoadJob
        /// </summary>
        /// <param name="storedInstanceId"></param>
        /// <returns></returns>
        public PSWorkflowJob LoadJob(PSWorkflowId storedInstanceId)
        {
            AssertNotDisposed();

            if (storedInstanceId == null) throw new ArgumentNullException("storedInstanceId");

            if(LoadJobWithIdentifier(storedInstanceId))
            {
                return Get(null, storedInstanceId.Guid);
            }

            return null;
        }

        internal bool LoadJobWithIdentifier(PSWorkflowId storedInstanceId)
        {
            PSWorkflowInstance instance = _runtime.Configuration.CreatePSWorkflowInstance(storedInstanceId);

            try
            {
                instance.InstanceStore.Load(WorkflowStoreComponents.JobState | WorkflowStoreComponents.Metadata);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);

                instance.JobStateRetrieved = false;
                instance.PSWorkflowContext = new PSWorkflowContext();
            }

            if (!instance.JobStateRetrieved)
            {
                instance.RemoveInstance();
                return false;
            }

            string command;
            string name;
            Guid instanceId;
            if (!WorkflowJobSourceAdapter.GetJobInfoFromMetadata(instance, out command, out name, out instanceId))
            {
                instance.RemoveInstance();
                return false;
            }

            if (instance.Timer != null)
            {
                if (instance.Timer.CheckIfTimerHasReachedAlready(WorkflowTimerType.ElapsedTimer))
                {
                    instance.RemoveInstance();
                    return false;
                }

                // starting the elapsed timer immediately.
                instance.Timer.StartTimer(WorkflowTimerType.ElapsedTimer);
            }

            if (_wfJobTable.ContainsKey(instanceId))
            {
                return true;
            }

            lock (lockObjects.GetLockObject(instanceId))
            {
                if (_wfJobTable.ContainsKey(instanceId))
                {
                    return true;
                }

                PSWorkflowJob newjob = new PSWorkflowJob(_runtime, command, name, instanceId);
                newjob.PSWorkflowInstance = instance;
                instance.PSWorkflowJob = newjob;
                newjob.RestoreFromWorkflowInstance(instance);
                newjob.WorkflowInstanceLoaded = true;
                newjob.ConfigureWorkflowHandlers();
                if (!_wfJobTable.ContainsKey(newjob.InstanceId))
                {
                    AddJob(newjob);
                }
                return true;
            }
        }

        private readonly object _servicingThreadSyncObject = new object();
        private bool _needToStartServicingThread = true;

        /// <summary>
        /// Indicates whether the dedicated thread to start jobs is required
        /// The thread needs to started only the first time
        /// </summary>
        /// <remarks>This property helps in delay creation of
        /// the thread</remarks>
        private bool NeedToStartServicingThread
        {
            get
            {
                if (_needToStartServicingThread)
                {
                    lock(_servicingThreadSyncObject)
                    {
                        if (_needToStartServicingThread)
                        {
                            _needToStartServicingThread = false;
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private readonly Lazy<AutoResetEvent> _waitForJobs = new Lazy<AutoResetEvent>(() => new AutoResetEvent(false));
        
        private void CheckAndStartServicingThread()
        {
            // check to see if waking the servicing thread at 
            // this point will actually lead to servicing. If
            // so wake it up
            if (_inProgressCount < _throttleLimit && _pendingQueue.Count > 0)
                _waitForJobs.Value.Set();

            if (PSWorkflowSessionConfiguration.IsWorkflowTypeEndpoint)
            {
                lock (_syncObject)
                {
                    // Start the shutdown timer when there is no inprogress job, no pending job requests and no active sessions
                    // Otherwise disable it
                    if (_activeSessionsCount == 0 && _inProgressCount == 0 && _pendingQueue.Count == 0)
                    {
                        _shutdownTimer.Enabled = true;
                    }
                    else
                    {
                        _shutdownTimer.Enabled = false;
                    }
                }
            }

            if (!NeedToStartServicingThread) return;

            Thread servicingThread = new Thread(StartOperationsFromQueue)
                                         {Name = "Job Throttling Thread"};
            servicingThread.IsBackground = true;
            servicingThread.Start();
        }

        private void WaitTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer waitTimer = sender as Timer;
            Debug.Assert(waitTimer != null, "Sender was not send correctly");

            if (_inProgressCount == 0 && _pendingQueue.Count == 0)
            {
                // at the end of wait time if there are no jobs
                // then run garbage collection. This need not be
                // perfectly thread safe hence these lockless
                // checks - to minimize overhead
                RunGarbageCollection(false);
            }

            // cleanup
            waitTimer.Elapsed -= WaitTimerElapsed;
            waitTimer.Dispose();
        }

        /// <summary>
        /// PerformWSManPluginReportCompletion
        /// </summary>
        [DllImport("pwrshplugin.dll", EntryPoint = "PerformWSManPluginReportCompletion", SetLastError = false)]
        private static extern void PerformWSManPluginReportCompletion();

        private void ShutdownWaitTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer waitTimer = sender as Timer;
            Debug.Assert(waitTimer != null, "Sender was not send correctly");
            Debug.Assert(waitTimer.Equals(_shutdownTimer), "Sender was not send correctly");

            bool callWSManPluginReportCompletion = false;

            lock(_syncObject)
            {
                if (_activeSessionsCount == 0 && _inProgressCount == 0 && _pendingQueue.Count == 0)
                {
                    callWSManPluginReportCompletion = true;
                }
            }

            if(callWSManPluginReportCompletion)
            {
                Tracer.WriteMessage("Calling WSManPluginReportCompletion as there are no active sessions and no inprogress/pending jobs");
                // Endpoint will not be shutdown if there is any active session
                PerformWSManPluginReportCompletion();
            }
        }

        /// <summary>
        /// Handles the wsman server shutting down event.
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="eventArgs">arguments describing the event</param>
        private void OnWSManServerActiveSessionsChangedEvent(object sender, ActiveSessionsChangedEventArgs eventArgs)
        {
            if (eventArgs != null)
            {
                lock (_syncObject)
                {
                    _activeSessionsCount = eventArgs.ActiveSessionsCount;

                    if (_shutdownTimer != null)
                    {
                        // Start the shutdown timer when there is no inprogress job, no pending job requests and no active sessions
                        // Otherwise disable it
                        if (_activeSessionsCount == 0 && _inProgressCount == 0 && _pendingQueue.Count == 0)
                        {
                            _shutdownTimer.Enabled = true;
                        }
                        else
                        {
                            _shutdownTimer.Enabled = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Contains the logic for running garbage collection
        /// </summary>
        /// <param name="force">if true, time check will not be
        /// done and a forced garbage collection will take place</param>
        /// <remarks>This method ensures that garbage collection
        /// runs from only one thread at a time</remarks>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        private static void RunGarbageCollection(bool force)
        {
            if (Interlocked.CompareExchange(ref _gcStatus, InProgress, NotInProgress) != NotInProgress)
                return;

            // if GC is run based on time elapsed, then we want atleast a 5 minute gap between 2 GC runs
            if (force || DateTime.Compare(DateTime.Now, _lastGcTime.AddMinutes(GcDelayInMinutes)) >= 0)
            {
                _lastGcTime = DateTime.Now;
                Interlocked.Exchange(ref _workflowsBeforeGc, 0);

                etwTracer.BeginRunGarbageCollection();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                etwTracer.EndRunGarbageCollection();
            }

            Interlocked.CompareExchange(ref _gcStatus, NotInProgress, InProgress);
        }

        private void StartOperationsFromQueue()
        {
            do
            {
                Tuple<Action<object>, object, JobState> tuple;
                while (_inProgressCount < _throttleLimit && _pendingQueue.TryDequeue(out tuple))
                {
                    Action<object> operationDelegate = tuple.Item1;

                    // decrement the current queue length perf counter
                    PerfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.WaitingWorkflowJobsCount,
                        -1);


                    var wfJob = tuple.Item1.Target as PSWorkflowJob;
                    Debug.Assert(wfJob != null, "Target cannot be null");

                    // Increment in progress job count only when OnJobStateChanged Event handler is registered.
                    // Event handler will be added if job's current is state is either expected state or running.
                    // OnJobStateChanged handler decrements the in progress job count when job is Suspended, Completed, Failed or Stopped.
                    //
                    if (wfJob.CheckAndAddStateChangedEventHandler(OnJobStateChanged, tuple.Item3))
                        Interlocked.Increment(ref _inProgressCount);

                    // increment the current running workflow jobs counter
                    PerfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.RunningWorkflowJobsCount);
                    PerfCountersMgr.UpdateCounterByValue(
                        PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                        PSWorkflowPerformanceCounterIds.RunningWorkflowJobsPerSec);
                    operationDelegate(tuple.Item2);
                    operationDelegate = null;
                    tuple = null;
                }

                // there are no more requests to service however there
                // can be running jobs. When everything is complete and
                // _inProgressCount is 0 and there no further requests, 
                // this method will still be called. At that point fire
                // the wait timer
                if (_inProgressCount == 0 && _pendingQueue.Count == 0)
                {
                    Timer waitTimer = new Timer();

                    waitTimer.Elapsed += WaitTimerElapsed;
                    waitTimer.Interval = WaitInterval;
                    waitTimer.AutoReset = false;
                    waitTimer.Enabled = true;
                }

                try
                {
                    // we have reached this point either because there are
                    // no pending jobs or because we have hit the number of
                    // jobs that can be currently run. This thread will now
                    // sleep until thawed by an appropriate action
                    _waitForJobs.Value.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    // ObjectDisposedException will be thrown when _waitForJobs.Value is disposed.
                }
            } while (!_isDisposed && !_isDisposing);
        }

        private void OnJobStateChanged(object sender, JobStateEventArgs eventArgs)
        {
            switch (eventArgs.JobStateInfo.State)
            {
                case JobState.Suspended:
                case JobState.Stopped:
                case JobState.Failed:
                case JobState.Completed:
                    {
                        Job2 job = sender as Job2;
                        Debug.Assert(job != null, "Job instance need to passed when StateChanged event is raised");
                        job.StateChanged -= OnJobStateChanged;

                        // start more operations from queue if required
                        Interlocked.Decrement(ref _inProgressCount);
                        // decrement the workflow jobs being serviced perf counter
                        PerfCountersMgr.UpdateCounterByValue(
                            PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                            PSWorkflowPerformanceCounterIds.RunningWorkflowJobsCount,
                            -1);

                        if (Interlocked.Increment(ref _workflowsBeforeGc) >= WorkflowLimitBeforeGc)
                        {
                            RunGarbageCollection(true);
                        }

                        CheckAndStartServicingThread();
                    }
                    break;
            }
        }

        /// <summary>
        /// SubmitOperation
        /// </summary>
        /// <param name="job"></param>
        /// <param name="operationHandler"></param>
        /// <param name="state"></param>
        /// <param name="expectedState"></param>
        internal void SubmitOperation(Job2 job, Action<object> operationHandler, object state, JobState expectedState)
        {
            Debug.Assert(job != null, "Null job passed");
            Debug.Assert(operationHandler != null, "Null delegate passed");

            if (job == null) throw new ArgumentNullException("job");

            // Adding OnJobStateChanged event handler here caused negative _inProgressCount when jobs are stopped before they got picked up by servicing thread.
            _pendingQueue.Enqueue(Tuple.Create(operationHandler, state, expectedState));
            PerfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.WaitingWorkflowJobsCount);
            CheckAndStartServicingThread();
        }

        /// <summary>
        /// Create a workflow job by providing the activity-tree representing the workflow.
        /// </summary>
        /// <param name="jobInstanceId"></param>
        /// <param name="workflow"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public PSWorkflowJob CreateJob(Guid jobInstanceId, Activity workflow, string command, string name, Dictionary<string, object> parameters)
        {
            return CreateJob(jobInstanceId, workflow, command, name, parameters, null);
        }

        /// <summary>
        /// Create a workflow job by providing the xaml representing the workflow.
        /// </summary>
        /// <param name="jobInstanceId"></param>
        /// <param name="workflowXaml"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public PSWorkflowJob CreateJob(Guid jobInstanceId, string workflowXaml, string command, string name, Dictionary<string, object> parameters)
        {
            return CreateJob(jobInstanceId, workflowXaml, command, name, parameters, null);
        }

        /// <summary>
        /// Create a workflow job by providing the activity-tree representing the workflow.
        /// </summary>
        /// <param name="jobInstanceId"></param>
        /// <param name="workflow"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="creationContext"></param>
        /// <returns></returns>
        public PSWorkflowJob CreateJob(Guid jobInstanceId, Activity workflow, string command, string name, Dictionary<string, object> parameters, Dictionary<string, object> creationContext)
        {
            return CreateJobInternal(jobInstanceId, workflow, command, name, parameters, null, creationContext);
        }

        /// <summary>
        /// Create a workflow job by providing the xaml representing the workflow.
        /// </summary>
        /// <param name="jobInstanceId"></param>
        /// <param name="workflowXaml"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="creationContext"></param>
        /// <returns></returns>
        public PSWorkflowJob CreateJob(Guid jobInstanceId, string workflowXaml, string command, string name, Dictionary<string, object> parameters, Dictionary<string, object> creationContext)
        {
            Activity workflow = Microsoft.PowerShell.Commands.ImportWorkflowCommand.ConvertXamlToActivity(workflowXaml);
            return CreateJobInternal(jobInstanceId, workflow, command, name, parameters, workflowXaml, creationContext);
        }

        internal PSWorkflowJob CreateJobInternal(Guid jobInstanceId, Activity workflow, string command, string name, Dictionary<string, object> parameters, string xaml, Dictionary<string, object> creationContext)
        {
            AssertNotDisposed();

            if (jobInstanceId == Guid.Empty)
                throw new ArgumentNullException("jobInstanceId");

            if (workflow == null)
                throw new ArgumentNullException("workflow");

            if (command == null)
                throw new ArgumentNullException("command");

            if (name == null)
                throw new ArgumentNullException("name");

            if (_wfJobTable.ContainsKey(jobInstanceId))
            {
                ArgumentException exception = new ArgumentException(Resources.DuplicateInstanceId);
                Tracer.TraceException(exception);
                throw exception;
            }

            lock (lockObjects.GetLockObject(jobInstanceId))
            {

                if (_wfJobTable.ContainsKey(jobInstanceId))
                {
                    ArgumentException exception = new ArgumentException(Resources.DuplicateInstanceId);
                    Tracer.TraceException(exception);
                    throw exception;
                }

                JobDefinition definition = new JobDefinition(typeof(WorkflowJobSourceAdapter), command, name);

                var parameterDictionary = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);

                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> param in parameters)
                    {
                        parameterDictionary.Add(param.Key, param.Value);
                    }
                }

                string[] computerNames = null;
                bool gotComputerNames = false;
                object value;
                if (parameterDictionary.Count != 0 && parameterDictionary.TryGetValue(Constants.ComputerName, out value))
                {
                    if (LanguagePrimitives.TryConvertTo(value, CultureInfo.InvariantCulture, out computerNames))
                        gotComputerNames = computerNames != null;
                }

                if (gotComputerNames)
                {
                    if (computerNames.Length > 1)
                        throw new ArgumentException(Resources.OneComputerNameAllowed);

                    parameterDictionary.Remove(Constants.ComputerName);
                }

                var childSpecification = new JobInvocationInfo(definition, parameterDictionary);
                childSpecification.Command = command;

                // If actual computernames were specified, then set the PSComputerName parameter.
                if (gotComputerNames)
                {
                    var computerNameParameter = new CommandParameter(Constants.ComputerName, computerNames);
                    childSpecification.Parameters[0].Add(computerNameParameter);
                }

                // Job objects will be disposed of on parent job removal.
                var childJob = new PSWorkflowJob(_runtime, childSpecification, jobInstanceId, creationContext);
                childJob.JobMetadata = CreateJobMetadataWithNoParentDefined(childJob, computerNames);

                childJob.LoadWorkflow(childSpecification.Parameters[0], workflow, xaml);
                this.AddJob(childJob);

                return childJob;
            }
        }

        private static Dictionary<string, object> CreateJobMetadataWithNoParentDefined(Job job, string[] parentComputers)
        {
            var jobMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                  {
                                      {Constants.JobMetadataInstanceId, job.InstanceId},
                                      {Constants.JobMetadataSessionId, job.Id},
                                      {Constants.JobMetadataName, job.Name},
                                      {Constants.JobMetadataCommand, job.Command},
                                      {Constants.JobMetadataStateReason, job.JobStateInfo.Reason},
                                      {Constants.JobMetadataStatusMessage, job.StatusMessage},
                                      {Constants.JobMetadataUserName, Environment.UserName},
                                      {Constants.JobMetadataPid, CurrentProcessId},
                                      // This is needed because the jobs created by runtime APIs are not visible from the PS Console
                                      // although they were using default store, because they don't have the parent metadata.
                                      // Afer assigning the parent metadata these jobs will be visible from console if APIs are using
                                      // default store.
                                      {Constants.JobMetadataParentInstanceId, Guid.NewGuid()},
                                      {Constants.JobMetadataParentSessionId, 0}, // no need to worry about '0' because it will be re assigned when loaded into the session.
                                      {Constants.JobMetadataParentName, job.Name},
                                      {Constants.JobMetadataParentCommand, job.Command} 
                                  };

            return jobMetadata;
        }


        /// <summary>
        /// CreateJob
        /// </summary>
        /// <param name="jobInvocationInfo"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        internal ContainerParentJob CreateJob(JobInvocationInfo jobInvocationInfo, Activity activity)
        {
            if (jobInvocationInfo == null)
                throw new ArgumentNullException("jobInvocationInfo");

            if (jobInvocationInfo.Definition == null)
                throw new ArgumentException(Resources.NewJobDefinitionNull, "jobInvocationInfo");

            if (jobInvocationInfo.Command == null)
                throw new ArgumentException(Resources.NewJobDefinitionNull, "jobInvocationInfo");

            DynamicActivity dynamicActivity = activity as DynamicActivity;

            Debug.Assert(dynamicActivity != null, "Passed workflow must be a DynamicActivity");

            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Creating Workflow job with definition: {0}", jobInvocationInfo.Definition.InstanceId));

            // Create parent job. All PSWorkflowJob objects will be a child of some ContainerParentJob
            // This job will be disposed of when RemoveJob is called.
            ContainerParentJob newJob = new ContainerParentJob(
                jobInvocationInfo.Command, 
                jobInvocationInfo.Name,
                WorkflowJobSourceAdapter.AdapterTypeName);

            foreach (CommandParameterCollection commandParameterCollection in jobInvocationInfo.Parameters)
            {
                var parameterDictionary = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
                foreach (CommandParameter param in commandParameterCollection)
                {
                    parameterDictionary.Add(param.Name, param.Value);
                }

                string[] computerNames = null;
                bool gotComputerNames = false;
                object value;
                if (parameterDictionary.Count != 0 && parameterDictionary.TryGetValue(Constants.ComputerName, out value))
                {
                    if (LanguagePrimitives.TryConvertTo(value, CultureInfo.InvariantCulture, out computerNames))
                        gotComputerNames = computerNames != null;
                }

                StructuredTracer.ParentJobCreated(newJob.InstanceId);

                bool isComputerNameExists = false;

                if (dynamicActivity != null && dynamicActivity.Properties.Any(x => x.Name.Equals(Constants.ComputerName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    isComputerNameExists = true;
                }

                dynamicActivity = null;

                if (isComputerNameExists)
                {
                    var childSpecification = new JobInvocationInfo(jobInvocationInfo.Definition, parameterDictionary);
                    childSpecification.Command = newJob.Command;

                    // If actual computernames were specified, then set the PSComputerName parameter.
                    if (gotComputerNames)
                    {
                        var computerNameParameter = new CommandParameter(Constants.ComputerName, computerNames);
                        childSpecification.Parameters[0].Add(computerNameParameter);
                    }

                    // Job objects will be disposed of on parent job removal.
                    var childJob = new PSWorkflowJob(_runtime, childSpecification);
                    childJob.JobMetadata = CreateJobMetadata(childJob, newJob.InstanceId, newJob.Id, newJob.Name, newJob.Command, computerNames);

                    childJob.LoadWorkflow(commandParameterCollection, activity, null);
                    this.AddJob(childJob);
                    newJob.AddChildJob(childJob);
                    StructuredTracer.ChildWorkflowJobAddition(childJob.InstanceId, newJob.InstanceId);
                    StructuredTracer.WorkflowJobCreated(newJob.InstanceId, childJob.InstanceId, childJob.WorkflowGuid);
                }
                else
                {
                    // Remove array of computerNames from collection.
                    parameterDictionary.Remove(Constants.ComputerName);

                    if (gotComputerNames)
                    {
                        foreach (var computerName in computerNames)
                        {
                            CreateChildJob(jobInvocationInfo, activity, newJob, commandParameterCollection, parameterDictionary, computerName, computerNames);
                        }
                    }
                    else
                    {
                        CreateChildJob(jobInvocationInfo, activity, newJob, commandParameterCollection, parameterDictionary, null, computerNames);
                    }
                }
            }

            StructuredTracer.JobCreationComplete(newJob.InstanceId, jobInvocationInfo.InstanceId);
            Tracer.TraceJob(newJob);

            return newJob;
        }

        private void CreateChildJob(JobInvocationInfo specification, Activity activity, ContainerParentJob newJob, CommandParameterCollection commandParameterCollection, Dictionary<string, object> parameterDictionary, string computerName, string[] computerNames)
        {
            if (!string.IsNullOrEmpty(computerName))
            {
                string[] childTargetComputerList = { computerName };

                // Set the target computer for this child job...
                parameterDictionary[Constants.ComputerName] = childTargetComputerList;
            }

            var childSpecification = new JobInvocationInfo(specification.Definition, parameterDictionary);

            // Job objects will be disposed of on parent job removal.
            var childJob = new PSWorkflowJob(_runtime, childSpecification);
            childJob.JobMetadata = CreateJobMetadata(childJob, newJob.InstanceId, newJob.Id, newJob.Name, newJob.Command, computerNames);

            // Remove the parameter from the collection...
            for (int index = 0; index < commandParameterCollection.Count; index++)
            {
                if (string.Equals(commandParameterCollection[index].Name, Constants.ComputerName, StringComparison.OrdinalIgnoreCase))
                {
                    commandParameterCollection.RemoveAt(index);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(computerName))
            {
                var computerNameParameter = new CommandParameter(Constants.ComputerName, computerName);
                commandParameterCollection.Add(computerNameParameter);
            }

            this.AddJob(childJob);
            childJob.LoadWorkflow(commandParameterCollection, activity, null);
            newJob.AddChildJob(childJob);
            StructuredTracer.ChildWorkflowJobAddition(childJob.InstanceId, newJob.InstanceId);
            Tracer.TraceJob(childJob);
            StructuredTracer.WorkflowJobCreated(newJob.InstanceId, childJob.InstanceId, childJob.WorkflowGuid);
        }

        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static Dictionary<string, object> CreateJobMetadata(Job job, Guid parentInstanceId, int parentSessionId,
        string parentName, string parentCommand, string[] parentComputers)
        {
            var jobMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                  {
                                      {Constants.JobMetadataInstanceId, job.InstanceId},
                                      {Constants.JobMetadataSessionId, job.Id},
                                      {Constants.JobMetadataName, job.Name},
                                      {Constants.JobMetadataCommand, job.Command},
                                      {Constants.JobMetadataStateReason, job.JobStateInfo.Reason},
                                      {Constants.JobMetadataStatusMessage, job.StatusMessage},
                                      {Constants.JobMetadataParentInstanceId, parentInstanceId},
                                      {Constants.JobMetadataParentSessionId, parentSessionId},
                                      {Constants.JobMetadataParentName, parentName},
                                      {Constants.JobMetadataParentCommand, parentCommand},
                                      {Constants.JobMetadataUserName, Environment.UserName},
                                      {Constants.JobMetadataPid, CurrentProcessId}
                                  };

            return jobMetadata;
        }

        /// <summary>
        /// ShutdownWorkflowManager is responsible suspending all the workflow and if suspend doesn't happen within timeout then calling the force suspend.
        /// </summary>
        /// <param name="timeout">The shutdown timeout in milliseconds.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void ShutdownWorkflowManager(int timeout = 500)
        {
            // Return if this is already disposed.
            if (_isDisposed)
                return;

            if (timeout <= 0)
                throw new ArgumentException(Resources.ForceSuspendTimeout);


            List<WaitHandle> forceHandles = new List<WaitHandle>();

            foreach (var job in GetJobs())
            {
                // non thread safe check to avoid straight forward exclusions
                if (job.JobStateInfo.State == JobState.Running || job.JobStateInfo.State == JobState.Suspending)
                {
                    try
                    {
                        if (job.DoAbortJob(Resources.ShutdownAbort)) // this will do the thread safe verification
                            forceHandles.Add(job.SuspendedOrAborted);
                        else
                            forceHandles.Add(job.Finished);
                    }
                    catch (Exception exception)
                    {
                        // To continue in aborting the remaining jobs, it is safe to discard all the exceptions during shutdown.
                        Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                               "Shutting down workflow manager: suspend forcefully : Exception details: {0}",
                               exception));

                    }
                }
            }

            if (forceHandles.Count > 0)
            {
                WaitHandle.WaitAll(forceHandles.ToArray(), timeout);
                forceHandles.Clear();
            }


            // cleanup
            foreach (var job in GetJobs())
            {
                UnloadJob(job.InstanceId);
            }

            _wfJobTable.Clear();
        }

        #endregion WorkflowManager Management

        #region Searching Workflows

        /// <summary>
        /// Returns the workflow job currently loaded in memory with provided id.
        /// This function DOES NOT try load the job from the store if not found in memory
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public PSWorkflowJob GetJob(Guid instanceId)
        {
            return Get(instanceId, null);
        }

        /// <summary>
        /// Loads and returns all workflow jobs.
        /// </summary>
        /// <returns>Returns the collection workflow jobs.</returns>
        public IEnumerable<PSWorkflowJob> GetJobs()
        {
            AssertNotDisposed();
            Tracer.WriteMessage(Facility + "Getting all the workflow instances.");
            return _wfJobTable.Values;
        }

        /// <summary>
        /// Loads and returns all workflow jobs based on provided filters.
        /// </summary>
        /// <param name="type">Represent which type of data needs to be used to apply filter.</param>
        /// <param name="filters">Filters represents the key value pair of conditions.</param>
        /// <returns>Returns the collection of workflow instances.</returns>
        internal IEnumerable<Job2> GetJobs(WorkflowFilterTypes type, Dictionary<string, object> filters)
        {
            Tracer.WriteMessage(Facility + "Getting workflow instances based on filters");
            return GetJobs(_wfJobTable.Values, type, filters);
        }

        /// <summary>
        /// Loads and returns all workflow jobs based on provided filters.
        /// </summary>
        /// <param name="searchList">PSWorkflowJob search list</param>
        /// <param name="type">Represent which type of data needs to be used to apply filter.</param>
        /// <param name="filters">Filters represents the key value pair of conditions.</param>
        /// <returns>Returns the collection of workflow instances.</returns>
        internal IEnumerable<Job2> GetJobs(ICollection<PSWorkflowJob> searchList, WorkflowFilterTypes type, IDictionary<String, object> filters)
        {
            List<Job2> selectedJobs = new List<Job2>();
            var filters2 = new Dictionary<string, object>(filters, StringComparer.CurrentCultureIgnoreCase);

            // do a quick search on the basic v2 parameters
            List<Job2> narrowedList = WorkflowJobSourceAdapter.SearchJobsOnV2Parameters(searchList, filters2);

            // we have already searched based on Id, InstanceId, Name and Command
            // these can now be removed from the list of items to search
            string[] toRemove = {
                                 Constants.JobMetadataSessionId, 
                                 Constants.JobMetadataInstanceId,
                                 Constants.JobMetadataName, 
                                 Constants.JobMetadataCommand
                                };

            foreach (var key in toRemove.Where(filters2.ContainsKey))
            {
                filters2.Remove(key);
            }

            if (filters2.Count == 0)
                return narrowedList;

            foreach (var job in narrowedList)
            {                
                // we intend to find jobs that match ALL criteria in filters
                // NOT ANY criteria
                bool computerNameMatch = true;
                bool credentialMatch = true;
                object value;
                bool filterMatch = true;

                foreach (KeyValuePair<string, object> filter in filters2)
                {
                    string key = filter.Key;
                    if (!SearchAllFilterTypes((PSWorkflowJob)job, type, key, out value))
                    {
                        computerNameMatch = credentialMatch = filterMatch = false;
                        break;
                    }

                    // if guid is passed as a string try to convert and use the same                    
                    if (value is Guid)
                    {
                        Guid filterValueAsGuid;
                        LanguagePrimitives.TryConvertTo(filter.Value, CultureInfo.InvariantCulture,
                                                                         out filterValueAsGuid);
                        if (filterValueAsGuid == (Guid)value)
                            continue;
                        filterMatch = false;
                        break;
                    }

                    // PSComputerName needs to be special cased because it is actually an array.
                    if (key.Equals(Constants.ComputerName, StringComparison.OrdinalIgnoreCase))
                    {
                        string[] instanceComputers = value as string[];
                        if (instanceComputers == null)
                        {
                            string instanceComputer = value as string;
                            if (instanceComputer == null) break;

                            instanceComputers = new string[] { instanceComputer };
                        }

                        object[] filterComputers = filter.Value as object[];
                        if (filterComputers == null)
                        {
                            string computer = filter.Value as string;
                            if (computer == null) break;

                            filterComputers = new object[] { computer };
                        }

                        foreach(var instanceComputer in instanceComputers)
                        {
                            computerNameMatch = false;
                            // for every computer we want atleast one filter
                            // match
                            foreach(var filterComputer in filterComputers)
                            {
                                string stringFilter = filterComputer as string;
                                if (stringFilter == null) break;
                                WildcardPattern filterPattern = new WildcardPattern(stringFilter, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

                                if (!filterPattern.IsMatch(instanceComputer)) continue;
                                computerNameMatch = true;
                                break;                                
                            }
                            if (!computerNameMatch) break;
                        }
                        if (!computerNameMatch) break;
                        continue;
                    }

                    if ((key.Equals(Constants.Credential, StringComparison.OrdinalIgnoreCase)))
                    {
                        credentialMatch = false;
                        // PSCredential is a special case because it may be stored as a PSObject.
                        object credentialObject = filter.Value;
                        PSCredential credential = credentialObject as PSCredential;

                        if (credential == null)
                        {
                            PSObject credPsObject = credentialObject as PSObject;
                            if (credPsObject == null) break;

                            credential = credPsObject.BaseObject as PSCredential;
                            if (credential == null) break;
                        }

                        Debug.Assert(credential != null);
                        credentialMatch = WorkflowUtils.CompareCredential(credential, value as PSCredential);
                        continue;
                    }

                    if ((filter.Value is string || filter.Value is WildcardPattern) && value is string)
                    {
                        // at this point we are guaranteed that the key exists somewhere                    
                        WildcardPattern pattern;
                        string stringValue = filter.Value as string;
                        if (stringValue != null)
                            pattern = new WildcardPattern(stringValue,
                                                          WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                        else pattern = (WildcardPattern)filter.Value;

                        // if the pattern is a match, matched is still true, and don't check
                        // to see if the selected dictionary contains the actual key value pair.
                        if (!pattern.IsMatch((string)value))
                        {
                            // if the pattern is not a match, this doesn't match the given filter.
                            filterMatch = false;
                            break;
                        }
                        continue;
                    }

                    if (value != null && value.Equals(filter.Value)) continue;
                    filterMatch= false;
                    break;
                } // end of foreach - filters

                bool matched = computerNameMatch && credentialMatch && filterMatch;

                if (matched)
                {
                    selectedJobs.Add(job);
                }
            } //end of outer foreach    

            return selectedJobs;
        }

        private static bool SearchAllFilterTypes(PSWorkflowJob job, WorkflowFilterTypes type, string key, out object value)
        {
            PSWorkflowContext metadata = job.PSWorkflowInstance.PSWorkflowContext;
            value = null;
            if (metadata == null)
                return false;
            var searchTable = new Dictionary<WorkflowFilterTypes, Dictionary<string, object>>
                                  {
                                      {WorkflowFilterTypes.WorkflowSpecificParameters, metadata.WorkflowParameters},
                                      {WorkflowFilterTypes.JobMetadata, metadata.JobMetadata},
                                      {WorkflowFilterTypes.CommonParameters, metadata.PSWorkflowCommonParameters},
                                      {WorkflowFilterTypes.PrivateMetadata, metadata.PrivateMetadata}
                                  };
            foreach(var filter in searchTable.Keys)
            {
                object searchResult;
                if (!type.HasFlag(filter) || !SearchOneFilterType(searchTable[filter], key, out searchResult)) continue;
                value = searchResult;
                return true;
            }

            return false;

        }
        private static bool SearchOneFilterType(IDictionary<string, object> tableToSearch, string key, out object value)
        {
            value = null;
            if (tableToSearch == null)
                return false;

            if (tableToSearch.ContainsKey(key))
            {
                tableToSearch.TryGetValue(key, out value);
                return true;
            }

            return false;
        }

        #endregion Searching Workflows

        #region WorkflowInstanceTable

        /// <summary>
        /// The dictionary of workflow instances.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, PSWorkflowJob> _wfJobTable =
            new ConcurrentDictionary<Guid, PSWorkflowJob>();

        /// <summary>
        /// Add a job to the manager.
        /// </summary>
        /// <param name="job"></param>
        private void AddJob(PSWorkflowJob job)
        {
            Debug.Assert(job != null, "Null job passed");
            if (_wfJobTable.ContainsKey(job.InstanceId))
            {
                ArgumentException exception = new ArgumentException(Resources.DuplicateInstanceId);
                Tracer.TraceException(exception);
                throw exception;
            }

            _wfJobTable.TryAdd(job.InstanceId, job);
        }

        /// <summary>
        /// Retrieves job from the manager.
        /// </summary>
        /// <param name="jobInstanceId">The job instance Id.</param>
        /// <param name="workflowInstanceId">The workflow instance Id.</param>
        /// <returns>Returns the job.</returns>
        private PSWorkflowJob Get(Guid? jobInstanceId, Guid? workflowInstanceId)
        {
            AssertNotDisposed();

            PSWorkflowJob jobInstance = null;

            if (jobInstanceId.HasValue)
            {
                _wfJobTable.TryGetValue(jobInstanceId.Value, out jobInstance);
            }

            if (workflowInstanceId.HasValue)
            {
                jobInstance = _wfJobTable.Values.SingleOrDefault(job => job.PSWorkflowInstance.Id == workflowInstanceId.Value);
            }

            if (jobInstance == null)
            {
                return null;
            }

            return jobInstance;
        }

        internal void CleanUpWorkflowJobTable()
        {
            List<PSWorkflowJob> wfJobs = new List<PSWorkflowJob>(_wfJobTable.Values);
            foreach (var job in wfJobs)
            {
                if (job.PSWorkflowInstance.PSWorkflowContext.JobMetadata != null &&
                    job.PSWorkflowInstance.PSWorkflowContext.JobMetadata.Count != 0) continue;
                // If the job metadata is not found, this instance is not recoverable. Don't bother checking it
                // and remove it from the table.
                PSWorkflowJob removedJob;
                _wfJobTable.TryRemove(job.InstanceId, out removedJob);
                if (removedJob != null)
                    removedJob.Dispose();
            }
        }

        internal void ClearWorkflowManagerInstanceTable()
        {
            foreach(var job in _wfJobTable.Values)
            {
                job.Dispose();
            }
            _wfJobTable.Clear();
        }

        internal void RemoveChildJob(Job2 childWorkflowJob)
        {
            if (WorkflowJobSourceAdapter.GetInstance().GetJobManager() == this)
            {
                WorkflowJobSourceAdapter.GetInstance().RemoveChildJob(childWorkflowJob);
            }
            else
            {
                this.RemoveJob(childWorkflowJob.InstanceId);
            }
        }


        /// <summary>
        /// Remove the workflow with provided job instance id.
        /// </summary>
        /// <param name="instanceId">The job instance id.</param>
        public void RemoveJob(Guid instanceId)
        {
            AssertNotDisposed();

            Tracer.WriteMessage(Facility + "Removing job instance with id: " + instanceId);
            PSWorkflowJob job;
            _wfJobTable.TryGetValue(instanceId, out job);
            if (job == null) return;

            lock (lockObjects.GetLockObject(instanceId))
            {
                _wfJobTable.TryGetValue(instanceId, out job);
                if (job == null) return;

                try
                {
                    if (!job.IsFinishedState(job.JobStateInfo.State))
                    {
                        try
                        {
                            job.StopJob(true, "Remove");
                        }
                        catch (ObjectDisposedException)
                        {
                            Tracer.WriteMessage(Facility, "RemoveJob", job.PSWorkflowInstance.Id, "Workflow Job is already disposed. so removing it.");
                        }
                    }
                }
                finally
                {
                    job.PSWorkflowInstance.RemoveInstance();
                    job.Dispose();
                    _wfJobTable.TryRemove(instanceId, out job);

                    lockObjects.RemoveLockObject(instanceId);
                }
            }

            StructuredTracer.WorkflowDeletedFromDisk(instanceId, string.Empty);
            StructuredTracer.WorkflowCleanupPerformed(instanceId);
        }

        /// <summary>
        /// Unload/Forget the workflow with provided job instance id.
        /// This method is used to dispose unloaded idle workflows.
        /// </summary>
        /// <param name="instanceId">The job instance id.</param>
        public void UnloadJob(Guid instanceId)
        {
            AssertNotDisposed();

            lock (lockObjects.GetLockObject(instanceId))
            {
                Tracer.WriteMessage(Facility + "Forgetting job instance with id: " + instanceId);

                PSWorkflowJob job = GetJob(instanceId);

                if (job == null)
                    return;

                job.Dispose(); // igorse: why do we call Dispose before removing from _wfJobTable?

                _wfJobTable.TryRemove(job.InstanceId, out job);
            }
        }

        private void DoUnloadJob(object state)
        {
            Collection<object> col = state as Collection<object>;
            Debug.Assert(col != null, "The state object should be of type collection<object>");

            Debug.Assert(col[0].GetType() == typeof(Guid), "The first element in the collection should be of type Guid");
            Debug.Assert(col[1].GetType() == typeof(ManualResetEvent), "The second element in the collection should be of type ManualResetEvent");

            Guid instanceId = (Guid)col[0];
            ManualResetEvent handle = (ManualResetEvent)col[1];

            UnloadJob(instanceId);
            handle.Set();
        }

        /// <summary>
        /// UnloadAllJobs
        /// </summary>
        public void UnloadAllJobs()
        {
            AssertNotDisposed();

            List<WaitHandle> waitHandles = new List<WaitHandle>();

            foreach (KeyValuePair<Guid, PSWorkflowJob> job in _wfJobTable)
            {
                ManualResetEvent handle = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(DoUnloadJob, new Collection<object> { job.Key, handle });
                waitHandles.Add(handle);
            }

            if (waitHandles.Count > 0)
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    foreach (WaitHandle handle in waitHandles)
                        handle.WaitOne();
                }
                else
                {
                    WaitHandle.WaitAll(waitHandles.ToArray());
                }

                // now disposing all wait handles
                foreach (WaitHandle handle in waitHandles)
                    handle.Dispose();
            }
        }

        #endregion WorkflowInstanceTable

    }

    /// <summary>
    /// Provides list of workflow filter types.
    /// </summary>
    [Flags]
    internal enum WorkflowFilterTypes
    {
        /// <summary>
        /// Empty flags - not used, would indicate to search no collections.
        /// </summary>
        None = 0,

        /// <summary>
        /// Filters will be applicable to job metadata collection.
        /// </summary>
        JobMetadata = 1,

        /// <summary>
        /// Filters will be applicable to caller private metadata collection.
        /// </summary>
        PrivateMetadata = 2,

        /// <summary>
        /// Filters will be applicable to workflow specific parameters defined by the workflow author.
        /// </summary>
        WorkflowSpecificParameters = 4,

        /// <summary>
        /// Filters will be applicable to common parameters on all workflows.
        /// </summary>
        CommonParameters = 8,

        /// <summary>
        /// Use all filters.
        /// </summary>
        All = JobMetadata | PrivateMetadata | WorkflowSpecificParameters | CommonParameters
    }

    internal class LockObjectsCollection
    {
        private object syncLock = new object();
        private Dictionary<Guid, object> lockObjects = new Dictionary<Guid, object>();

        internal object GetLockObject(Guid id)
        {
            lock (syncLock)
            {
                if(lockObjects.ContainsKey(id) == false)
                {
                    lockObjects.Add(id, new object());
                }

                return lockObjects[id];
            }
        }

        internal void RemoveLockObject(Guid id)
        {
            if (lockObjects.ContainsKey(id) == false)
                return;
                
            lock (syncLock)
            {
                if (lockObjects.ContainsKey(id) == false)
                    return;

                lockObjects.Remove(id);
            }
        }
    }
}
