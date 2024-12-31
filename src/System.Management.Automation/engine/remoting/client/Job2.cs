// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    #region PowerShell v3 Job Extensions

    /// <summary>
    /// New base class for a job that provides extended state
    /// management functionality on the job. Since the existing
    /// Job class is an abstract class and there are existing
    /// implementations of the same, it is required to have a
    /// new class that will have the extended functionality. This
    /// is to ensure that backwards compatibility is maintained
    ///
    /// However, this class will derive from the existing Job
    /// class. The option of deprecating the existing class was
    /// considered as well. In order to maintain backwards
    /// compatibility of PowerShell job cmdlets they will have
    /// to work with the old interface and hence deprecating
    /// the Job class did not add any benefit rather than
    /// deriving from the same.
    /// </summary>
    /// <remarks>The following are some of the notes about
    /// why the asynchronous operations are provided this way
    /// in this class. There are two possible options in which
    /// asynchronous support can be provided:
    ///     1. Classical pattern (Begin and End)
    ///     2. Event based pattern
    ///
    /// Although the PowerShell API uses the classical pattern
    /// and we would like the Job API and PowerShell API to be
    /// as close as possible, the classical pattern is inherently
    /// complex to use.</remarks>
    public abstract class Job2 : Job
    {
        #region Private Members

        /// <summary>
        /// These are the parameters that can be used by a job
        /// implementation when they want to specify parameters
        /// to start a job.
        /// </summary>
        private List<CommandParameterCollection> _parameters;

        /// <summary>
        /// Object that will be used for thread synchronization.
        /// </summary>
        private readonly object _syncobject = new object();

        private const int StartJobOperation = 1;
        private const int StopJobOperation = 2;
        private const int SuspendJobOperation = 3;
        private const int ResumeJobOperation = 4;
        private const int UnblockJobOperation = 5;

        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        #endregion Private Members

        #region Properties

        /// <summary>
        /// Parameters to be used to start a job.
        /// This is a property because CommandParameterCollection
        /// does not have a public constructor. Hence the
        /// infrastructure creates an instance and provides
        /// it for the implementations to use.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<CommandParameterCollection> StartParameters
        {
            get
            {
                if (_parameters == null)
                {
                    lock (_syncobject)
                    {
                        _parameters ??= new List<CommandParameterCollection>();
                    }
                }

                return _parameters;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                lock (_syncobject)
                {
                    _parameters = value;
                }
            }
        }

        /// <summary>
        /// </summary>
        protected object SyncRoot
        {
            get { return syncObject; }
        }

        #endregion Properties

        #region Protected Methods

        /// <summary>
        /// Default no argument constructor.
        /// </summary>
        protected Job2() : base() { }

        /// <summary>
        /// Constructor which will initialize the job
        /// with the associated command string.
        /// </summary>
        /// <param name="command">string representation
        /// of the command the job is running</param>
        protected Job2(string command) : base(command) { }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        protected Job2(string command, string name)
            : base(command, name)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        /// <param name="childJobs">Child jobs of this job object.</param>
        protected Job2(string command, string name, IList<Job> childJobs)
            : base(command, name, childJobs)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        /// <param name="token">JobIdentifier token used to assign Id and InstanceId.</param>
        protected Job2(string command, string name, JobIdentifier token)
            : base(command, name, token)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="instanceId">Instance ID to allow job identification across sessions.</param>
        protected Job2(string command, string name, Guid instanceId)
            : base(command, name, instanceId)
        {
        }

        /// <summary>
        /// There is an internal method in Job which is not made
        /// public. In order to make this available to someone
        /// implementing a job it has to be added here. If the
        /// original method is made public it has changes of
        /// colliding with some implementation which may have
        /// added that method.
        /// </summary>
        /// <param name="state">State of the job.</param>
        /// <param name="reason">exception associated with the
        /// job entering this state</param>
        protected new void SetJobState(JobState state, Exception reason)
        {
            base.SetJobState(state, reason);
        }

        #endregion Protected Methods

        #region State Management

        /// <summary>
        /// Start a job. The job will be started with the parameters
        /// specified in StartParameters.
        /// </summary>
        /// <remarks>It is redundant to have a method named StartJob
        /// on a job class. However, this is done so as to avoid
        /// an FxCop violation "CA1716:IdentifiersShouldNotMatchKeywords"
        /// Stop and Resume are reserved keyworks in C# and hence cannot
        /// be used as method names. Therefore to be consistent it has
        /// been decided to use *Job in the name of the methods</remarks>
        public abstract void StartJob();

        /// <summary>
        /// Start a job asynchronously.
        /// </summary>
        public abstract void StartJobAsync();

        /// <summary>
        /// Event to be raise when the start job activity is completed.
        /// This event should not be raised for
        /// synchronous operation.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> StartJobCompleted;

        /// <summary>
        /// Method which can be extended or called by derived
        /// classes to raise the event when start of
        /// the job is completed.
        /// </summary>
        /// <param name="eventArgs">arguments describing
        /// an exception that is associated with the event</param>
        protected virtual void OnStartJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            RaiseCompletedHandler(StartJobOperation, eventArgs);
        }

        /// <summary>
        /// Method which can be extended or called by derived
        /// classes to raise the event when stopping a
        /// job is completed.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        protected virtual void OnStopJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            RaiseCompletedHandler(StopJobOperation, eventArgs);
        }

        /// <summary>
        /// Method which can be extended or called by derived
        /// classes to raise the event when suspending a
        /// job is completed.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        protected virtual void OnSuspendJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            RaiseCompletedHandler(SuspendJobOperation, eventArgs);
        }

        /// <summary>
        /// Method which can be extended or called by derived
        /// classes to raise the event when resuming a
        /// suspended job is completed.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        protected virtual void OnResumeJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            RaiseCompletedHandler(ResumeJobOperation, eventArgs);
        }

        /// <summary>
        /// Method which can be extended or called by derived
        /// classes to raise the event when unblocking a
        /// blocked job is completed.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        protected virtual void OnUnblockJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            RaiseCompletedHandler(UnblockJobOperation, eventArgs);
        }

        /// <summary>
        /// Raises the appropriate event based on the operation
        /// and the associated event arguments.
        /// </summary>
        /// <param name="operation">operation for which the event
        /// needs to be raised</param>
        /// <param name="eventArgs"></param>
        private void RaiseCompletedHandler(int operation, AsyncCompletedEventArgs eventArgs)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<AsyncCompletedEventArgs> handler = null;

            switch (operation)
            {
                case StartJobOperation:
                    {
                        handler = StartJobCompleted;
                    }

                    break;
                case StopJobOperation:
                    {
                        handler = StopJobCompleted;
                    }

                    break;
                case SuspendJobOperation:
                    {
                        handler = SuspendJobCompleted;
                    }

                    break;
                case ResumeJobOperation:
                    {
                        handler = ResumeJobCompleted;
                    }

                    break;
                case UnblockJobOperation:
                    {
                        handler = UnblockJobCompleted;
                    }

                    break;
                default:
                    {
                        Dbg.Assert(false, "this condition should not be hit, check the value of operation that you passed");
                    }

                    break;
            }
#pragma warning disable 56500
            try
            {
                handler?.Invoke(this, eventArgs);
            }
            catch (Exception exception)
            {
                // errors in the handlers are not errors in the operation
                // silently ignore them
                _tracer.TraceException(exception);
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Stop a job asynchronously.
        /// </summary>
        public abstract void StopJobAsync();

        /// <summary>
        /// Event to be raised when the asynchronous stopping of a job
        /// is completed.This event should not be raised for
        /// synchronous operation.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> StopJobCompleted;

        /// <summary>
        /// Suspend a job.
        /// </summary>
        public abstract void SuspendJob();

        /// <summary>
        /// Asynchronously suspend a job.
        /// </summary>
        public abstract void SuspendJobAsync();

        /// <summary>
        /// This event should be raised whenever the asynchronous suspend of
        /// a job is completed. This event should not be raised for
        /// synchronous operation.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> SuspendJobCompleted;

        /// <summary>
        /// Resume a suspended job.
        /// </summary>
        public abstract void ResumeJob();

        /// <summary>
        /// Resume a suspended job asynchronously.
        /// </summary>
        public abstract void ResumeJobAsync();

        /// <summary>
        /// This event should be raised whenever the asynchronous resume of
        /// a suspended job is completed. This event should not be raised for
        /// synchronous operation.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> ResumeJobCompleted;

        /// <summary>
        /// Unblock a blocked job.
        /// </summary>
        public abstract void UnblockJob();

        /// <summary>
        /// Unblock a blocked job asynchronously.
        /// </summary>
        public abstract void UnblockJobAsync();

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public abstract void StopJob(bool force, string reason);

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public abstract void StopJobAsync(bool force, string reason);

        /// <summary>
        /// SuspendJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public abstract void SuspendJob(bool force, string reason);

        /// <summary>
        /// SuspendJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public abstract void SuspendJobAsync(bool force, string reason);

        /// <summary>
        /// This event should be raised whenever the asynchronous unblock
        /// of a blocked job is completed. This event should not be raised for
        /// synchronous operation.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> UnblockJobCompleted;

        #endregion State Management
    }

    /// <summary>
    /// Specifies the various thread options that can be used
    /// for the ThreadBasedJob.
    /// </summary>
    public enum JobThreadOptions
    {
        /// <summary>
        /// Use the default behavior, which is to use a
        /// ThreadPoolThread.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use a thread pool thread.
        /// </summary>
        UseThreadPoolThread = 1,

        /// <summary>
        /// Create a new thread everything and reuse.
        /// </summary>
        UseNewThread = 2,
    }

    /*/// <summary>
    /// This job will provide asynchronous behavior by running
    /// the user specified script block in a separate process.
    /// There will be options for running the scriptblock
    /// in a new process or an existing process.
    /// </summary>
    /// <remarks>Jobs for the out-of-process activity manager
    /// can be implemented using this interface</remarks>
    public abstract class ProcessBasedJob : Job2
    {
        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void StartAsync()
        {
            throw new NotImplementedException();
        }
    }*/

    /// <summary>
    /// Top level container job.
    /// </summary>
    public sealed class ContainerParentJob : Job2
    {
        #region Private Members

        private const string TraceClassName = "ContainerParentJob";

        private bool _moreData = true;
        private readonly object _syncObject = new object();
        private int _isDisposed = 0;

        private const int DisposedTrue = 1;
        private const int DisposedFalse = 0;
        // This variable is set to true if at least one child job failed.

        // count of number of child jobs which have finished
        private int _finishedChildJobsCount = 0;

        // count of number of child jobs which are blocked
        private int _blockedChildJobsCount = 0;

        // count of number of child jobs which are suspended
        private int _suspendedChildJobsCount = 0;

        // count of number of child jobs which are suspending
        private int _suspendingChildJobsCount = 0;

        // count of number of child jobs which failed
        private int _failedChildJobsCount = 0;

        // count of number of child jobs which stopped
        private int _stoppedChildJobsCount = 0;

        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly PSDataCollection<ErrorRecord> _executionError = new PSDataCollection<ErrorRecord>();

        private PSEventManager _eventManager;

        internal PSEventManager EventManager
        {
            get
            {
                return _eventManager;
            }

            set
            {
                _tracer.WriteMessage("Setting event manager for Job ", InstanceId);
                _eventManager = value;
            }
        }

        private ManualResetEvent _jobRunning;

        private ManualResetEvent JobRunning
        {
            get
            {
                if (_jobRunning == null)
                {
                    lock (_syncObject)
                    {
                        if (_jobRunning == null)
                        {
                            // this assert is required so that a wait handle
                            // is not created after the object is disposed
                            // which will result in a leak
                            AssertNotDisposed();
                            _jobRunning = new ManualResetEvent(false);
                        }
                    }
                }

                return _jobRunning;
            }
        }

        private ManualResetEvent _jobSuspendedOrAborted;

        private ManualResetEvent JobSuspendedOrAborted
        {
            get
            {
                if (_jobSuspendedOrAborted == null)
                {
                    lock (_syncObject)
                    {
                        if (_jobSuspendedOrAborted == null)
                        {
                            // this assert is required so that a wait handle
                            // is not created after the object is disposed
                            // which will result in a leak
                            AssertNotDisposed();
                            _jobSuspendedOrAborted = new ManualResetEvent(false);
                        }
                    }
                }

                return _jobSuspendedOrAborted;
            }
        }

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Create a container parent job with the
        /// specified command string and name.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for display.</param>
        public ContainerParentJob(string command, string name)
            : base(command, name)
        {
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the
        /// specified command string.
        /// </summary>
        /// <param name="command">Command string.</param>
        public ContainerParentJob(string command)
            : base(command)
        {
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the
        /// specified command string.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="jobId">JobIdentifier token that allows reuse of an Id and Instance Id.</param>
        public ContainerParentJob(string command, string name, JobIdentifier jobId)
            : base(command, name, jobId)
        {
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the
        /// specified command string.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="instanceId">Instance ID to allow job identification across sessions.</param>
        public ContainerParentJob(string command, string name, Guid instanceId)
            : base(command, name, instanceId)
        {
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the
        /// specified command string.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="jobId">JobIdentifier token that allows reuse of an Id and Instance Id.</param>
        /// <param name="jobType">Job type name.</param>
        public ContainerParentJob(string command, string name, JobIdentifier jobId, string jobType)
            : base(command, name, jobId)
        {
            PSJobTypeName = jobType;
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the
        /// specified command string.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="instanceId">Instance ID to allow job identification across sessions.</param>
        /// <param name="jobType">Job type name.</param>
        public ContainerParentJob(string command, string name, Guid instanceId, string jobType)
            : base(command, name, instanceId)
        {
            PSJobTypeName = jobType;
            StateChanged += HandleMyStateChanged;
        }

        /// <summary>
        /// Create a container parent job with the specified command, name,
        /// job type strings.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="name">Friendly name for the job.</param>
        /// <param name="jobType">Job type name.</param>
        public ContainerParentJob(string command, string name, string jobType)
            : base(command, name)
        {
            PSJobTypeName = jobType;
            StateChanged += HandleMyStateChanged;
        }

        #endregion Constructors

        internal PSDataCollection<ErrorRecord> ExecutionError { get { return _executionError; } }

        #region Public Methods

        /// <summary>
        /// Add a child job to the parent job.
        /// </summary>
        /// <param name="childJob">Child job to add.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the job is disposed.</exception>
        /// <exception cref="ArgumentNullException">Thrown if child being added is null.</exception>
        public void AddChildJob(Job2 childJob)
        {
            AssertNotDisposed();

            ArgumentNullException.ThrowIfNull(childJob);

            _tracer.WriteMessage(TraceClassName, "AddChildJob", Guid.Empty, childJob, "Adding Child to Parent with InstanceId : ", InstanceId.ToString());

            JobStateInfo childJobStateInfo;
            lock (childJob.syncObject)
            {
                // Store job's state and subscribe to State Changed event. Locking here will
                // ensure that the jobstateinfo we get is the state before any state changed events are handled by ContainerParentJob.
                childJobStateInfo = childJob.JobStateInfo;
                childJob.StateChanged += HandleChildJobStateChanged;
            }

            ChildJobs.Add(childJob);
            ParentJobStateCalculation(new JobStateEventArgs(childJobStateInfo, new JobStateInfo(JobState.NotStarted)));
        }

        /// <summary>
        /// Indicates if more data is available.
        /// </summary>
        /// <remarks>
        /// This has more data if any of the child jobs have more data.
        /// </remarks>
        public override bool HasMoreData
        {
            get
            {
                // moreData is initially set to true, and it
                // will remain so until the async result
                // object has completed execution.
                if (_moreData && IsFinishedState(JobStateInfo.State))
                {
                    bool atleastOneChildHasMoreData = false;

                    for (int i = 0; i < ChildJobs.Count; i++)
                    {
                        if (ChildJobs[i].HasMoreData)
                        {
                            atleastOneChildHasMoreData = true;
                            break;
                        }
                    }

                    _moreData = atleastOneChildHasMoreData;
                }

                return _moreData;
            }
        }

        /// <summary>
        /// Message indicating status of the job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return ConstructStatusMessage();
            }
        }

        /// <summary>
        /// Starts all jobs.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if job is disposed.</exception>
        public override void StartJob()
        {
            AssertNotDisposed();
            _tracer.WriteMessage(TraceClassName, "StartJob", Guid.Empty, this, "Entering method", null);
            s_structuredTracer.BeginContainerParentJobExecution(InstanceId);

            // If parent contains no child jobs then this method will not respond.  Throw error in this case.
            if (ChildJobs.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNoChildJobs);
            }

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // If there is only one child job, call the synchronous method on the child to avoid use of another thread.
            // If there are multiple, we can run them in parallel using the asynchronous versions.
            if (ChildJobs.Count == 1)
            {
                Job2 child = ChildJobs[0] as Job2;
                Dbg.Assert(child != null, "Job is null after initial null check");
#pragma warning disable 56500
                try
                {
                    _tracer.WriteMessage(TraceClassName, "StartJob", Guid.Empty, this,
                        "Single child job synchronously, child InstanceId: {0}", child.InstanceId.ToString());
                    child.StartJob();
                    JobRunning.WaitOne();
                }
                catch (Exception e)
                {
                    // These exceptions are thrown by third party code. Adding them here to the collection
                    // of execution errors to present consistent behavior of the object.

                    ExecutionError.Add(new ErrorRecord(e, "ContainerParentJobStartError",
                                                       ErrorCategory.InvalidResult, child));
                    _tracer.WriteMessage(TraceClassName, "StartJob", Guid.Empty, this,
                        "Single child job threw exception, child InstanceId: {0}", child.InstanceId.ToString());
                    _tracer.TraceException(e);
                }
#pragma warning restore 56500
                return;
            }

            var completed = new AutoResetEvent(false);
            // Count of StartJobCompleted events from children.
            var startedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = (object sender, AsyncCompletedEventArgs e) =>
            {
                var childJob = sender as Job2;
                Dbg.Assert(childJob != null,
                           "StartJobCompleted only available on Job2");
                _tracer.WriteMessage(TraceClassName, "StartJob-Handler", Guid.Empty, this,
                    "Finished starting child job asynchronously, child InstanceId: {0}", childJob.InstanceId.ToString());
                if (e.Error != null)
                {
                    ExecutionError.Add(
                        new ErrorRecord(e.Error,
                                        "ContainerParentJobStartError",
                                        ErrorCategory.InvalidResult,
                                        childJob));
                    _tracer.WriteMessage(TraceClassName, "StartJob-Handler", Guid.Empty, this,
                        "Child job asynchronously had error, child InstanceId: {0}", childJob.InstanceId.ToString());
                    _tracer.TraceException(e.Error);
                }

                Interlocked.Increment(ref startedChildJobsCount);
                if (startedChildJobsCount == ChildJobs.Count)
                {
                    _tracer.WriteMessage(TraceClassName, "StartJob-Handler", Guid.Empty, this,
                        "Finished starting all child jobs asynchronously", null);
                    JobRunning.WaitOne();
                    completed.Set();
                }
            };

            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.StartJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "StartJob", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());

                // This child job is created to run synchronously and so can be debugged.  Set
                // the IJobDebugger.IsAsync accordingly.
                ScriptDebugger.SetDebugJobAsync(job as IJobDebugger, false);
                job.StartJobAsync();
            }

            completed.WaitOne();
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.StartJobCompleted -= eventHandler;
            }

            /*
            if (ExecutionError.Count > 0)
            {
                // Check to see expected behavior if one child job fails to start.
            }

            if (ExecutionError.Count == 1)
            {
                throw ExecutionError[0];
            } */
            _tracer.WriteMessage(TraceClassName, "StartJob", Guid.Empty, this, "Exiting method", null);
        }

        private static readonly Tracer s_structuredTracer = new Tracer();

        /// <summary>
        /// Starts all child jobs asynchronously.
        /// When all child jobs are started, StartJobCompleted event is raised.
        /// </summary>
        public override void StartJobAsync()
        {
            if (_isDisposed == DisposedTrue)
            {
                OnStartJobCompleted(new AsyncCompletedEventArgs(new ObjectDisposedException(TraceClassName), false, null));
                return;
            }

            _tracer.WriteMessage(TraceClassName, "StartJobAsync", Guid.Empty, this, "Entering method", null);
            s_structuredTracer.BeginContainerParentJobExecution(InstanceId);
            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // Count of StartJobCompleted events from children.
            var startedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = null;
            eventHandler = (sender, e) =>
             {
                 var childJob = sender as Job2;
                 Dbg.Assert(childJob != null, "StartJobCompleted only available on Job2");
                 _tracer.WriteMessage(TraceClassName, "StartJobAsync-Handler", Guid.Empty, this,
                     "Finished starting child job asynchronously, child InstanceId: {0}", childJob.InstanceId.ToString());
                 if (e.Error != null)
                 {
                     ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobStartAsyncError",
                         ErrorCategory.InvalidResult, childJob));
                     _tracer.WriteMessage(TraceClassName, "StartJobAsync-Handler", Guid.Empty, this,
                        "Child job asynchronously had error, child InstanceId: {0}", childJob.InstanceId.ToString());
                     _tracer.TraceException(e.Error);
                 }

                 Interlocked.Increment(ref startedChildJobsCount);
                 Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
                 childJob.StartJobCompleted -= eventHandler;

                 if (startedChildJobsCount == ChildJobs.Count)
                 {
                     _tracer.WriteMessage(TraceClassName, "StartJobAsync-Handler", Guid.Empty, this,
                        "Finished starting all child jobs asynchronously", null);

                     JobRunning.WaitOne();
                     // There may be multiple exceptions raised. They
                     // are stored in the Error stream of this job object, which is otherwise
                     // unused.
                     OnStartJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                 }
             };

            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");
                job.StartJobCompleted += eventHandler;

                _tracer.WriteMessage(TraceClassName, "StartJobAsync", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                job.StartJobAsync();
            }

            _tracer.WriteMessage(TraceClassName, "StartJobAsync", Guid.Empty, this, "Exiting method", null);
        }

        /// <summary>
        /// Resume all jobs.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if job is disposed.</exception>
        public override void ResumeJob()
        {
            AssertNotDisposed();
            _tracer.WriteMessage(TraceClassName, "ResumeJob", Guid.Empty, this, "Entering method", null);

            // If parent contains no child jobs then this method will not respond.  Throw error in this case.
            if (ChildJobs.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNoChildJobs);
            }

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // If there is only one child job, call the synchronous method on the child to avoid use of another thread.
            // If there are multiple, we can run them in parallel using the asynchronous versions.
            if (ChildJobs.Count == 1)
            {
                Job2 child = ChildJobs[0] as Job2;
                Dbg.Assert(child != null, "Job is null after initial null check");
#pragma warning disable 56500
                try
                {
                    _tracer.WriteMessage(TraceClassName, "ResumeJob", Guid.Empty, this,
                        "Single child job synchronously, child InstanceId: {0}", child.InstanceId.ToString());
                    child.ResumeJob();
                    JobRunning.WaitOne();
                }
                catch (Exception e)
                {
                    // These exceptions are thrown by third party code. Adding them here to the collection
                    // of execution errors to present consistent behavior of the object.

                    ExecutionError.Add(new ErrorRecord(e, "ContainerParentJobResumeError",
                                                       ErrorCategory.InvalidResult, child));
                    _tracer.WriteMessage(TraceClassName, "ResumeJob", Guid.Empty, this,
                        "Single child job threw exception, child InstanceId: {0}", child.InstanceId.ToString());
                    _tracer.TraceException(e);
                }
#pragma warning restore 56500
                return;
            }

            var completed = new AutoResetEvent(false);
            // Count of ResumeJobCompleted events from children.
            var resumedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = null;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                eventHandler = (object sender, AsyncCompletedEventArgs e) =>
                                            {
                                                var childJob = sender as Job2;
                                                Dbg.Assert(childJob != null, "ResumeJobCompleted only available on Job2");
                                                _tracer.WriteMessage(TraceClassName, "ResumeJob-Handler", Guid.Empty, this,
                                                    "Finished resuming child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                                                if (e.Error != null)
                                                {
                                                    ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobResumeError",
                                                        ErrorCategory.InvalidResult, job));
                                                    _tracer.WriteMessage(TraceClassName, "ResumeJob-Handler", Guid.Empty, this,
                                                        "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                                                    _tracer.TraceException(e.Error);
                                                }

                                                Interlocked.Increment(ref resumedChildJobsCount);
                                                if (resumedChildJobsCount == ChildJobs.Count)
                                                {
                                                    _tracer.WriteMessage(TraceClassName, "ResumeJob-Handler", Guid.Empty, this,
                                                        "Finished resuming all child jobs asynchronously", null);
                                                    JobRunning.WaitOne();
                                                    completed.Set();
                                                }
                                            };
                job.ResumeJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "ResumeJob", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                job.ResumeJobAsync();
            }

            completed.WaitOne();
            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.ResumeJobCompleted -= eventHandler;
            }

            _tracer.WriteMessage(TraceClassName, "ResumeJob", Guid.Empty, this, "Exiting method", null);

            // Errors are taken from the Error collection by the cmdlet for ContainerParentJob.
        }

        /// <summary>
        /// Resume all jobs asynchronously.
        /// </summary>
        public override void ResumeJobAsync()
        {
            if (_isDisposed == DisposedTrue)
            {
                OnResumeJobCompleted(new AsyncCompletedEventArgs(new ObjectDisposedException(TraceClassName), false, null));
                return;
            }

            _tracer.WriteMessage(TraceClassName, "ResumeJobAsync", Guid.Empty, this, "Entering method", null);
            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // Count of ResumeJobCompleted events from children.
            var resumedChildJobsCount = 0;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                EventHandler<AsyncCompletedEventArgs> eventHandler = null;
                eventHandler = (sender, e) =>
                                        {
                                            var childJob = sender as Job2;
                                            Dbg.Assert(childJob != null, "ResumeJobCompleted only available on Job2");
                                            _tracer.WriteMessage(TraceClassName, "ResumeJobAsync-Handler", Guid.Empty, this,
                                                "Finished resuming child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                                            if (e.Error != null)
                                            {
                                                ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobResumeAsyncError",
                                                    ErrorCategory.InvalidResult, job));
                                                _tracer.WriteMessage(TraceClassName, "ResumeJobAsync-Handler", Guid.Empty, this,
                                                    "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                                                _tracer.TraceException(e.Error);
                                            }

                                            Interlocked.Increment(ref resumedChildJobsCount);
                                            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
                                            childJob.ResumeJobCompleted -= eventHandler;
                                            if (resumedChildJobsCount == ChildJobs.Count)
                                            {
                                                _tracer.WriteMessage(TraceClassName, "ResumeJobAsync-Handler", Guid.Empty, this,
                                                    "Finished resuming all child jobs asynchronously", null);

                                                JobRunning.WaitOne();
                                                // There may be multiple exceptions raised. They
                                                // are stored in the Error stream of this job object, which is otherwise
                                                // unused.
                                                OnResumeJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                                            }
                                        };
                job.ResumeJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "ResumeJobAsync", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                job.ResumeJobAsync();
            }

            _tracer.WriteMessage(TraceClassName, "ResumeJobAsync", Guid.Empty, this, "Exiting method", null);
        }

        /// <summary>
        /// Suspends all jobs.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if job is disposed.</exception>
        public override void SuspendJob()
        {
            SuspendJobInternal(null, null);
        }

        /// <summary>
        /// Suspends all jobs forcefully.
        /// </summary>
        /// <param name="force">Force flag for suspending forcefully.</param>
        /// <param name="reason">Reason for doing forceful suspend.</param>
        public override void SuspendJob(bool force, string reason)
        {
            SuspendJobInternal(force, reason);
        }

        /// <summary>
        /// Suspends all jobs asynchronously.
        /// When all jobs have been suspended, SuspendJobCompleted is raised.
        /// </summary>
        public override void SuspendJobAsync()
        {
            SuspendJobAsyncInternal(null, null);
        }

        /// <summary>
        /// Suspends all jobs asynchronously with force flag.
        /// When all jobs have been suspended, SuspendJobCompleted is raised.
        /// </summary>
        /// <param name="force">Force flag for suspending forcefully.</param>
        /// <param name="reason">Reason for doing forceful suspend.</param>
        public override void SuspendJobAsync(bool force, string reason)
        {
            SuspendJobAsyncInternal(force, reason);
        }

        /// <summary>
        /// Stop all child jobs.
        /// </summary>
        public override void StopJob()
        {
            StopJobInternal(null, null);
        }

        /// <summary>
        /// Stops all child jobs asynchronously.
        /// Once all child jobs are stopped, StopJobCompleted event is raised.
        /// </summary>
        public override void StopJobAsync()
        {
            StopJobAsyncInternal(null, null);
        }

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJob(bool force, string reason)
        {
            StopJobInternal(force, reason);
        }

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJobAsync(bool force, string reason)
        {
            StopJobAsyncInternal(force, reason);
        }

        /// <summary>
        /// Unblock all child jobs.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if job is disposed.</exception>
        public override void UnblockJob()
        {
            AssertNotDisposed();
            _tracer.WriteMessage(TraceClassName, "UnblockJob", Guid.Empty, this, "Entering method", null);

            // If parent contains no child jobs then this method will not respond.  Throw error in this case.
            if (ChildJobs.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNoChildJobs);
            }

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // If there is only one child job, call the synchronous method on the child to avoid use of another thread.
            // If there are multiple, we can run them in parallel using the asynchronous versions.
            if (ChildJobs.Count == 1)
            {
                Job2 child = ChildJobs[0] as Job2;
                Dbg.Assert(child != null, "Job is null after initial null check");
#pragma warning disable 56500
                try
                {
                    _tracer.WriteMessage(TraceClassName, "UnblockJob", Guid.Empty, this,
                        "Single child job synchronously, child InstanceId: {0}", child.InstanceId.ToString());
                    child.UnblockJob();
                }
                catch (Exception e)
                {
                    // These exceptions are thrown by third party code. Adding them here to the collection
                    // of execution errors to present consistent behavior of the object.

                    ExecutionError.Add(new ErrorRecord(e, "ContainerParentJobUnblockError",
                        ErrorCategory.InvalidResult, child));
                    _tracer.WriteMessage(TraceClassName, "UnblockJob", Guid.Empty, this,
                        "Single child job threw exception, child InstanceId: {0}", child.InstanceId.ToString());
                    _tracer.TraceException(e);
                }
#pragma warning restore 56500
                return;
            }

            var completed = new AutoResetEvent(false);
            // count of UnblockJobCompleted events from children.
            int unblockedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = null;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                eventHandler = (object sender, AsyncCompletedEventArgs e) =>
                                        {
                                            var childJob = sender as Job2;
                                            Dbg.Assert(childJob != null, "UnblockJobCompleted only available on Job2");
                                            _tracer.WriteMessage(TraceClassName, "UnblockJob-Handler", Guid.Empty, this,
                                                "Finished unblock child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                                            if (e.Error != null)
                                            {
                                                ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobUnblockError",
                                                    ErrorCategory.InvalidResult, childJob));
                                                _tracer.WriteMessage(TraceClassName, "UnblockJob-Handler", Guid.Empty, this,
                                                    "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                                                _tracer.TraceException(e.Error);
                                            }

                                            Interlocked.Increment(ref unblockedChildJobsCount);
                                            if (unblockedChildJobsCount == ChildJobs.Count)
                                            {
                                                _tracer.WriteMessage(TraceClassName, "UnblockJob-Handler", Guid.Empty, this,
                                                    "Finished unblock all child jobs asynchronously", null);
                                                completed.Set();
                                            }
                                        };
                job.UnblockJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "UnblockJob", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                job.UnblockJobAsync();
            }

            completed.WaitOne();
            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.UnblockJobCompleted -= eventHandler;
            }

            _tracer.WriteMessage(TraceClassName, "UnblockJob", Guid.Empty, this, "Exiting method", null);

            // Errors are taken from the Error collection by the cmdlet for ContainerParentJob.
        }

        /// <summary>
        /// Unblock all child jobs asynchronously.
        /// Once all child jobs are unblocked, UnblockJobCompleted event is raised.
        /// </summary>
        public override void UnblockJobAsync()
        {
            if (_isDisposed == DisposedTrue)
            {
                OnUnblockJobCompleted(new AsyncCompletedEventArgs(new ObjectDisposedException(TraceClassName), false, null));
                return;
            }

            _tracer.WriteMessage(TraceClassName, "UnblockJobAsync", Guid.Empty, this, "Entering method", null);
            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // count of UnblockJobCompleted events from children.
            int unblockedChildJobsCount = 0;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                EventHandler<AsyncCompletedEventArgs> eventHandler = null;
                eventHandler = (sender, e) =>
                                        {
                                            var childJob = sender as Job2;
                                            Dbg.Assert(childJob != null, "UnblockJobCompleted only available on Job2");
                                            _tracer.WriteMessage(TraceClassName, "UnblockJobAsync-Handler", Guid.Empty, this,
                                                "Finished unblock child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                                            if (e.Error != null)
                                            {
                                                ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobUnblockError",
                                                    ErrorCategory.InvalidResult, childJob));
                                                _tracer.WriteMessage(TraceClassName, "UnblockJobAsync-Handler", Guid.Empty, this,
                                                    "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                                                _tracer.TraceException(e.Error);
                                            }

                                            Interlocked.Increment(ref unblockedChildJobsCount);
                                            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
                                            childJob.UnblockJobCompleted -= eventHandler;
                                            if (unblockedChildJobsCount == ChildJobs.Count)
                                            {
                                                _tracer.WriteMessage(TraceClassName, "UnblockJobAsync-Handler", Guid.Empty, this,
                                                    "Finished unblock all child jobs asynchronously", null);

                                                // State change is handled elsewhere.
                                                // There may be multiple exceptions raised. They
                                                // are stored in the Error stream of this job object, which is otherwise
                                                // unused.
                                                OnUnblockJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                                            }
                                        };
                job.UnblockJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "UnblockJobAsync", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                job.UnblockJobAsync();
            }

            _tracer.WriteMessage(TraceClassName, "UnblockJobAsync", Guid.Empty, this, "Exiting method", null);
        }

        #endregion Public Methods

        #region finish logic

        /// <summary>
        /// Internal synchronous SuspendJob, calls appropriate version if Force is specified.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        private void SuspendJobInternal(bool? force, string reason)
        {
            AssertNotDisposed();
            _tracer.WriteMessage(TraceClassName, "SuspendJob", Guid.Empty, this, "Entering method", null);

            // If parent contains no child jobs then this method will not respond.  Throw error in this case.
            if (ChildJobs.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNoChildJobs);
            }

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // If there is only one child job, call the synchronous method on the child to avoid use of another thread.
            // If there are multiple, we can run them in parallel using the asynchronous versions.
            if (ChildJobs.Count == 1)
            {
                Job2 child = ChildJobs[0] as Job2;
                Dbg.Assert(child != null, "Job is null after initial null check");
#pragma warning disable 56500
                try
                {
                    _tracer.WriteMessage(TraceClassName, "SuspendJob", Guid.Empty, this,
                                         "Single child job synchronously, child InstanceId: {0} force: {1}", child.InstanceId.ToString(), force.ToString());
                    if (force.HasValue)
                        child.SuspendJob(force.Value, reason);
                    else
                        child.SuspendJob();
                    JobSuspendedOrAborted.WaitOne();
                }
                catch (Exception e)
                {
                    // These exceptions are thrown by third party code. Adding them here to the collection
                    // of execution errors to present consistent behavior of the object.

                    ExecutionError.Add(new ErrorRecord(e, "ContainerParentJobSuspendError",
                                                       ErrorCategory.InvalidResult, child));
                    _tracer.WriteMessage(TraceClassName, "SuspendJob", Guid.Empty, this,
                        "Single child job threw exception, child InstanceId: {0} force: {1}", child.InstanceId.ToString(), force.ToString());
                    _tracer.TraceException(e);
                }
#pragma warning restore 56500
                return;
            }

            AutoResetEvent completed = new AutoResetEvent(false);
            var suspendedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = null;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                eventHandler = (object sender, AsyncCompletedEventArgs e) =>
                {
                    var childJob = sender as Job2;
                    Dbg.Assert(childJob != null,
                                "SuspendJobCompleted only available on Job2");
                    _tracer.WriteMessage(TraceClassName, "SuspendJob-Handler", Guid.Empty, this,
                        "Finished suspending child job asynchronously, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                    if (e.Error != null)
                    {
                        ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobSuspendError",
                            ErrorCategory.InvalidResult, job));
                        _tracer.WriteMessage(TraceClassName, "SuspendJob-Handler", Guid.Empty, this,
                            "Child job asynchronously had error, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                        _tracer.TraceException(e.Error);
                    }

                    Interlocked.Increment(ref suspendedChildJobsCount);
                    if (suspendedChildJobsCount == ChildJobs.Count)
                    {
                        _tracer.WriteMessage(TraceClassName, "SuspendJob-Handler", Guid.Empty, this,
                            "Finished suspending all child jobs asynchronously", null);
                        JobSuspendedOrAborted.WaitOne();
                        completed.Set();
                    }
                };
                job.SuspendJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "SuspendJob", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                if (force.HasValue)
                    job.SuspendJobAsync(force.Value, reason);
                else
                    job.SuspendJobAsync();
            }

            completed.WaitOne();
            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.SuspendJobCompleted -= eventHandler;
            }

            _tracer.WriteMessage(TraceClassName, "SuspendJob", Guid.Empty, this, "Exiting method", null);

            // Errors are taken from the Error collection by the cmdlet for ContainerParentJob.
        }

        /// <summary>
        /// Internal SuspendJobAsync. Calls appropriate method if Force is specified.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        private void SuspendJobAsyncInternal(bool? force, string reason)
        {
            if (_isDisposed == DisposedTrue)
            {
                OnSuspendJobCompleted(new AsyncCompletedEventArgs(new ObjectDisposedException(TraceClassName), false, null));
                return;
            }

            _tracer.WriteMessage(TraceClassName, "SuspendJobAsync", Guid.Empty, this, "Entering method", null);
            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // Count of SuspendJobCompleted events from children.
            var suspendedChildJobsCount = 0;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                EventHandler<AsyncCompletedEventArgs> eventHandler = null;
                eventHandler = (sender, e) =>
                {
                    var childJob = sender as Job2;
                    Dbg.Assert(childJob != null, "SuspendJobCompleted only available on Job2");
                    _tracer.WriteMessage(TraceClassName, "SuspendJobAsync-Handler", Guid.Empty, this,
                        "Finished suspending child job asynchronously, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                    if (e.Error != null)
                    {
                        ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobSuspendAsyncError",
                            ErrorCategory.InvalidResult, job));
                        _tracer.WriteMessage(TraceClassName, "SuspendJobAsync-Handler", Guid.Empty, this,
                            "Child job asynchronously had error, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                        _tracer.TraceException(e.Error);
                    }

                    Interlocked.Increment(ref suspendedChildJobsCount);
                    Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
                    childJob.SuspendJobCompleted -= eventHandler;
                    if (suspendedChildJobsCount == ChildJobs.Count)
                    {
                        _tracer.WriteMessage(TraceClassName, "SuspendJobAsync-Handler", Guid.Empty, this,
                                "Finished suspending all child jobs asynchronously", null);

                        JobSuspendedOrAborted.WaitOne();
                        // There may be multiple exceptions raised. They
                        // are stored in the Error stream of this job object, which is otherwise
                        // unused.
                        OnSuspendJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                    }
                };
                job.SuspendJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "SuspendJobAsync", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0} force: {1}", job.InstanceId.ToString(), force.ToString());
                if (force.HasValue)
                    job.SuspendJobAsync(force.Value, reason);
                else
                    job.SuspendJobAsync();
            }

            _tracer.WriteMessage(TraceClassName, "SuspendJobAsync", Guid.Empty, this, "Exiting method", null);
        }

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        private void StopJobInternal(bool? force, string reason)
        {
            AssertNotDisposed();
            _tracer.WriteMessage(TraceClassName, "StopJob", Guid.Empty, this, "Entering method", null);

            // If parent contains no child jobs then this method will not respond.  Throw error in this case.
            if (ChildJobs.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNoChildJobs);
            }

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // If there is only one child job, call the synchronous method on the child to avoid use of another thread.
            // If there are multiple, we can run them in parallel using the asynchronous versions.
            if (ChildJobs.Count == 1)
            {
                Job2 child = ChildJobs[0] as Job2;
                Dbg.Assert(child != null, "Job is null after initial null check");
#pragma warning disable 56500
                try
                {
                    _tracer.WriteMessage(TraceClassName, "StopJob", Guid.Empty, this,
                                         "Single child job synchronously, child InstanceId: {0}", child.InstanceId.ToString());
                    if (force.HasValue)
                        child.StopJob(force.Value, reason);
                    else
                        child.StopJob();
                    Finished.WaitOne();
                }
                catch (Exception e)
                {
                    // These exceptions are thrown by third party code. Adding them here to the collection
                    // of execution errors to present consistent behavior of the object.

                    ExecutionError.Add(new ErrorRecord(e, "ContainerParentJobStopError",
                                                       ErrorCategory.InvalidResult, child));
                    _tracer.WriteMessage(TraceClassName, "StopJob", Guid.Empty, this,
                                        "Single child job threw exception, child InstanceId: {0}", child.InstanceId.ToString());
                    _tracer.TraceException(e);
                }
#pragma warning restore 56500
                return;
            }

            AutoResetEvent completed = new AutoResetEvent(false);
            // Count of StopJobCompleted events from children.
            var stoppedChildJobsCount = 0;
            EventHandler<AsyncCompletedEventArgs> eventHandler = null;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                eventHandler = (object sender, AsyncCompletedEventArgs e) =>
                {
                    var childJob = sender as Job2;
                    Dbg.Assert(childJob != null,
                                "StopJobCompleted only available on Job2");
                    _tracer.WriteMessage(TraceClassName, "StopJob-Handler", Guid.Empty, this,
                        "Finished stopping child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                    if (e.Error != null)
                    {
                        ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobStopError",
                            ErrorCategory.InvalidResult, job));
                        _tracer.WriteMessage(TraceClassName, "StopJob-Handler", Guid.Empty, this,
                            "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                        _tracer.TraceException(e.Error);
                    }

                    Interlocked.Increment(ref stoppedChildJobsCount);
                    if (stoppedChildJobsCount == ChildJobs.Count)
                    {
                        _tracer.WriteMessage(TraceClassName, "StopJob-Handler", Guid.Empty, this,
                        "Finished stopping all child jobs asynchronously", null);
                        Finished.WaitOne();
                        completed.Set();
                    }
                };
                job.StopJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "StopJob", Guid.Empty, this,
                     "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                if (force.HasValue)
                    job.StopJobAsync(force.Value, reason);
                else
                    job.StopJobAsync();
            }

            completed.WaitOne();
            Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                job.StopJobCompleted -= eventHandler;
            }

            _tracer.WriteMessage(TraceClassName, "StopJob", Guid.Empty, this, "Exiting method", null);

            // Errors are taken from the Error collection by the cmdlet for ContainerParentJob.
        }

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        private void StopJobAsyncInternal(bool? force, string reason)
        {
            if (_isDisposed == DisposedTrue)
            {
                OnStopJobCompleted(new AsyncCompletedEventArgs(new ObjectDisposedException(TraceClassName), false, null));
                return;
            }

            _tracer.WriteMessage(TraceClassName, "StopJobAsync", Guid.Empty, this, "Entering method", null);

            foreach (Job2 job in this.ChildJobs)
            {
                if (job == null) throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.JobActionInvalidWithNullChild);
            }

            // count of StopJobCompleted events from children.
            int stoppedChildJobsCount = 0;
            foreach (Job2 job in ChildJobs)
            {
                Dbg.Assert(job != null, "Job is null after initial null check");

                EventHandler<AsyncCompletedEventArgs> eventHandler = null;
                eventHandler = (sender, e) =>
                {
                    var childJob = sender as Job2;
                    Dbg.Assert(childJob != null, "StopJobCompleted only available on Job2");
                    _tracer.WriteMessage(TraceClassName, "StopJobAsync-Handler", Guid.Empty, this,
                        "Finished stopping child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                    if (e.Error != null)
                    {
                        ExecutionError.Add(new ErrorRecord(e.Error, "ContainerParentJobStopAsyncError",
                            ErrorCategory.InvalidResult, childJob));
                        _tracer.WriteMessage(TraceClassName, "StopJobAsync-Handler", Guid.Empty, this,
                            "Child job asynchronously had error, child InstanceId: {0}", job.InstanceId.ToString());
                        _tracer.TraceException(e.Error);
                    }

                    Interlocked.Increment(ref stoppedChildJobsCount);
                    Dbg.Assert(eventHandler != null, "Event handler magically disappeared");
                    childJob.StopJobCompleted -= eventHandler;
                    if (stoppedChildJobsCount == ChildJobs.Count)
                    {
                        _tracer.WriteMessage(TraceClassName, "StopJobAsync-Handler", Guid.Empty, this,
                                "Finished stopping all child jobs asynchronously", null);

                        Finished.WaitOne();
                        // There may be multiple exceptions raised. They
                        // are stored in the Error stream of this job object, which is otherwise
                        // unused.
                        OnStopJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                    }
                };
                job.StopJobCompleted += eventHandler;
                _tracer.WriteMessage(TraceClassName, "StopJobAsync", Guid.Empty, this,
                    "Child job asynchronously, child InstanceId: {0}", job.InstanceId.ToString());
                if (force.HasValue)
                    job.StopJobAsync(force.Value, reason);
                else
                    job.StopJobAsync();
            }

            _tracer.WriteMessage(TraceClassName, "StopJobAsync", Guid.Empty, this, "Exiting method", null);
        }

        private void HandleMyStateChanged(object sender, JobStateEventArgs e)
        {
            _tracer.WriteMessage(TraceClassName, "HandleMyStateChanged", Guid.Empty, this,
                                 "NewState: {0}; OldState: {1}", e.JobStateInfo.State.ToString(),
                                 e.PreviousJobStateInfo.State.ToString());

            switch (e.JobStateInfo.State)
            {
                case JobState.Running:
                    {
                        lock (_syncObject)
                        {
                            JobRunning.Set();

                            // Do not create the event if it doesn't already exist. Suspend may never be called.
                            if (_jobSuspendedOrAborted != null)
                                JobSuspendedOrAborted.Reset();
                        }
                    }

                    break;

                case JobState.Suspended:
                    {
                        lock (_syncObject)
                        {
                            JobSuspendedOrAborted.Set();
                            JobRunning.Reset();
                        }
                    }

                    break;
                case JobState.Failed:
                case JobState.Completed:
                case JobState.Stopped:
                    {
                        lock (_syncObject)
                        {
                            JobSuspendedOrAborted.Set();

                            // Do not reset JobRunning when the state is terminal.
                            // No thread should wait on a job transitioning again to
                            // JobState.Running.
                            JobRunning.Set();
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the StateChanged event from each of the child job objects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleChildJobStateChanged(object sender, JobStateEventArgs e)
        {
            ParentJobStateCalculation(e);
        }

        private void ParentJobStateCalculation(JobStateEventArgs e)
        {
            JobState computedState;
            if (ComputeJobStateFromChildJobStates("ContainerParentJob", e, ref _blockedChildJobsCount, ref _suspendedChildJobsCount, ref _suspendingChildJobsCount,
                    ref _finishedChildJobsCount, ref _failedChildJobsCount, ref _stoppedChildJobsCount, ChildJobs.Count, out computedState))
            {
                if (computedState != JobStateInfo.State)
                {
                    if (JobStateInfo.State == JobState.NotStarted && computedState == JobState.Running)
                    {
                        PSBeginTime = DateTime.Now;
                    }

                    if (!IsFinishedState(JobStateInfo.State) && IsPersistentState(computedState))
                    {
                        PSEndTime = DateTime.Now;
                    }

                    SetJobState(computedState);
                }

                if (_finishedChildJobsCount == ChildJobs.Count)
                {
                    s_structuredTracer.EndContainerParentJobExecution(InstanceId);
                }
            }
        }

        /// <summary>
        /// Handles the StateChanged event from each of the child job objects.
        /// </summary>
        /// <param name="traceClassName"></param>
        /// <param name="e"></param>
        /// <param name="blockedChildJobsCount"></param>
        /// <param name="suspendedChildJobsCount"></param>
        /// <param name="suspendingChildJobsCount"></param>
        /// <param name="finishedChildJobsCount"></param>
        /// <param name="stoppedChildJobsCount"></param>
        /// <param name="childJobsCount"></param>
        /// <param name="computedJobState"></param>
        /// <param name="failedChildJobsCount"></param>
        /// <returns>True if the job state needs to be modified, false otherwise.</returns>
        internal static bool ComputeJobStateFromChildJobStates(string traceClassName, JobStateEventArgs e,
            ref int blockedChildJobsCount, ref int suspendedChildJobsCount, ref int suspendingChildJobsCount, ref int finishedChildJobsCount,
                ref int failedChildJobsCount, ref int stoppedChildJobsCount, int childJobsCount,
                    out JobState computedJobState)
        {
            computedJobState = JobState.NotStarted;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                if (e.JobStateInfo.State == JobState.Blocked)
                {
                    // increment count of blocked child jobs
                    Interlocked.Increment(ref blockedChildJobsCount);

                    // if any of the child job is blocked, we set state to blocked
                    tracer.WriteMessage(traceClassName, ": JobState is Blocked, at least one child job is blocked.");
                    computedJobState = JobState.Blocked;
                    return true;
                }

                if (e.PreviousJobStateInfo.State == JobState.Blocked)
                {
                    // check if any of the child jobs were unblocked
                    // in which case we need to check if the parent
                    // job needs to be unblocked as well
                    Interlocked.Decrement(ref blockedChildJobsCount);

                    if (blockedChildJobsCount == 0)
                    {
                        tracer.WriteMessage(traceClassName, ": JobState is unblocked, all child jobs are unblocked.");
                        computedJobState = JobState.Running;
                        return true;
                    }

                    return false;
                }

                if (e.PreviousJobStateInfo.State == JobState.Suspended)
                {
                    // decrement count of suspended child jobs
                    // needed to determine when all incomplete child jobs are suspended for parent job state.
                    Interlocked.Decrement(ref suspendedChildJobsCount);
                }

                if (e.PreviousJobStateInfo.State == JobState.Suspending)
                {
                    // decrement count of suspending child jobs
                    // needed to determine when all incomplete child jobs are suspended for parent job state.
                    Interlocked.Decrement(ref suspendingChildJobsCount);
                }

                if (e.JobStateInfo.State == JobState.Suspended)
                {
                    // increment count of suspended child jobs.
                    Interlocked.Increment(ref suspendedChildJobsCount);

                    // We know that at least one child is suspended. If all jobs are either complete or suspended, set the state.
                    if (suspendedChildJobsCount + finishedChildJobsCount == childJobsCount)
                    {
                        tracer.WriteMessage(traceClassName, ": JobState is suspended, all child jobs are suspended.");
                        computedJobState = JobState.Suspended;
                        return true;
                    }

                    // Job state should continue to be running unless:
                    // at least one child is suspended
                    // AND
                    // all child jobs are either suspended or finished.
                    return false;
                }

                if (e.JobStateInfo.State == JobState.Suspending)
                {
                    // increment count of suspending child jobs.
                    Interlocked.Increment(ref suspendingChildJobsCount);

                    // We know that at least one child is suspended. If all jobs are either complete or suspended, set the state.
                    if (suspendedChildJobsCount + finishedChildJobsCount + suspendingChildJobsCount == childJobsCount)
                    {
                        tracer.WriteMessage(traceClassName, ": JobState is suspending, all child jobs are in suspending state.");
                        computedJobState = JobState.Suspending;
                        return true;
                    }

                    // Job state should continue to be running unless:
                    // at least one child is suspended, suspending
                    // AND
                    // all child jobs are either suspended or finished.
                    return false;
                }

                // Ignore state changes which are not resulting in state change to finished.
                // State will be Running once at least one child is running.
                if ((e.JobStateInfo.State != JobState.Completed && e.JobStateInfo.State != JobState.Failed) && e.JobStateInfo.State != JobState.Stopped)
                {
                    if (e.JobStateInfo.State == JobState.Running)
                    {
                        computedJobState = JobState.Running;
                        return true;
                    }

                    // if the job state is Suspended, we have already returned.
                    // if the job state is NotStarted, do not set the state.
                    // if the job state is blocked, we have already returned.
                    return false;
                }

                if (e.JobStateInfo.State == JobState.Failed)
                {
                    // If any of the child job failed, we set status to failed
                    // we can set it right now and
                    Interlocked.Increment(ref failedChildJobsCount);
                }

                // If stop has not been called, but a child has been stopped, the parent should
                // reflect the stopped state.
                if (e.JobStateInfo.State == JobState.Stopped)
                {
                    Interlocked.Increment(ref stoppedChildJobsCount);
                }

                bool allChildJobsFinished = false;

                int finishedChildJobsCountNew = Interlocked.Increment(ref finishedChildJobsCount);

                // We are done
                if (finishedChildJobsCountNew == childJobsCount)
                {
                    allChildJobsFinished = true;
                }

                if (allChildJobsFinished)
                {
                    // if any child job failed, set status to failed
                    // If stop was called set, status to stopped
                    // else completed);
                    if (failedChildJobsCount > 0)
                    {
                        tracer.WriteMessage(traceClassName, ": JobState is failed, at least one child job failed.");
                        computedJobState = JobState.Failed;
                        return true;
                    }

                    if (stoppedChildJobsCount > 0)
                    {
                        tracer.WriteMessage(traceClassName, ": JobState is stopped, stop is called.");
                        computedJobState = JobState.Stopped;
                        return true;
                    }

                    tracer.WriteMessage(traceClassName, ": JobState is completed.");
                    computedJobState = JobState.Completed;
                    return true;
                }

                // If not all jobs are finished, one child job may be suspended, even though this job did not finish.
                // At this point, we know finishedChildJobsCountNew != childJobsCount
                if (suspendedChildJobsCount + finishedChildJobsCountNew == childJobsCount)
                {
                    tracer.WriteMessage(traceClassName, ": JobState is suspended, all child jobs are suspended.");
                    computedJobState = JobState.Suspended;
                    return true;
                }

                // If not all jobs are finished, one child job may be suspending, even though this job did not finish.
                // At this point, we know finishedChildJobsCountNew != childJobsCount and finishChildJobsCount + suspendedChilJobsCout != childJobsCount
                if (suspendingChildJobsCount + suspendedChildJobsCount + finishedChildJobsCountNew == childJobsCount)
                {
                    tracer.WriteMessage(traceClassName, ": JobState is suspending, all child jobs are in suspending state.");
                    computedJobState = JobState.Suspending;
                    return true;
                }
            }

            return false;
        }
        #endregion finish logic

        /// <summary>
        /// Release all the resources.
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.CompareExchange(ref _isDisposed, DisposedTrue, DisposedFalse) == DisposedTrue) return;

            try
            {
                UnregisterAllJobEvents();
                _executionError.Dispose();
                StateChanged -= HandleMyStateChanged;

                foreach (Job job in ChildJobs)
                {
                    _tracer.WriteMessage("Disposing child job with id : " + job.Id);
                    job.Dispose();
                }

                _jobRunning?.Dispose();
                _jobSuspendedOrAborted?.Dispose();
            }
            finally
            {
                base.Dispose(true);
            }
        }

        private string ConstructLocation()
        {
            if (ChildJobs == null || ChildJobs.Count == 0)
                return string.Empty;
            string location = ChildJobs.Select(static (job) => job.Location).Aggregate((s1, s2) => s1 + ',' + s2);
            return location;
        }

        private string ConstructStatusMessage()
        {
            if (ChildJobs == null || ChildJobs.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < ChildJobs.Count; i++)
            {
                if (!string.IsNullOrEmpty(ChildJobs[i].StatusMessage))
                {
                    sb.Append(ChildJobs[i].StatusMessage);
                }

                if (i < (ChildJobs.Count - 1))
                {
                    sb.Append(',');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Computers on which this job is running.
        /// </summary>
        public override string Location
        {
            get
            {
                return ConstructLocation();
            }
        }

        private void UnregisterJobEvent(Job job)
        {
            string sourceIdentifier = job.InstanceId + ":StateChanged";

            _tracer.WriteMessage("Unregistering StateChanged event for job ", job.InstanceId);
            foreach (PSEventSubscriber subscriber in
                EventManager.Subscribers.Where(subscriber => string.Equals(subscriber.SourceIdentifier, sourceIdentifier, StringComparison.OrdinalIgnoreCase)))
            {
                EventManager.UnsubscribeEvent(subscriber);
                break;
            }
        }

        private void UnregisterAllJobEvents()
        {
            if (EventManager == null)
            {
                _tracer.WriteMessage("No events subscribed, skipping event unregistrations");
                return;
            }

            foreach (var job in ChildJobs)
            {
                UnregisterJobEvent(job);
            }

            UnregisterJobEvent(this);
            _tracer.WriteMessage("Setting event manager to null");
            EventManager = null;
        }
    }

    /// <summary>
    /// Container exception for jobs that can map errors and exceptions
    /// to specific lines in their input.
    /// </summary>
    public class JobFailedException : SystemException
    {
        /// <summary>
        /// Creates a new JobFailedException.
        /// </summary>
        public JobFailedException()
        {
        }

        /// <summary>
        /// Creates a new JobFailedException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public JobFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new JobFailedException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The actual exception that caused this error.</param>
        public JobFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new JobFailedException.
        /// </summary>
        /// <param name="innerException">The actual exception that caused this error.</param>
        /// <param name="displayScriptPosition">A ScriptExtent that describes where this error originated from.</param>
        public JobFailedException(Exception innerException, ScriptExtent displayScriptPosition)
        {
            _reason = innerException;
            _displayScriptPosition = displayScriptPosition;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="serializationInfo">Serialization info.</param>
        /// <param name="streamingContext">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected JobFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The actual exception that caused this error.
        /// </summary>
        public Exception Reason { get { return _reason; } }

        private readonly Exception _reason;

        /// <summary>
        /// The user-focused location from where this error originated.
        /// </summary>
        public ScriptExtent DisplayScriptPosition { get { return _displayScriptPosition; } }

        private readonly ScriptExtent _displayScriptPosition;

        /// <summary>
        /// Returns the reason for this exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return Reason.Message;
            }
        }
    }

    #endregion PowerShell v3 Job Extensions
}
