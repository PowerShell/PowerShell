/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Linq;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Management.Automation.PerformanceData;
using System.Threading;
using Dbg = System.Diagnostics.Debug;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.Activities.Hosting;
using Microsoft.PowerShell.Activities;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// Workflow Job type implementation. For use with the WorkflowJobSourceAdapter.
    /// </summary>
    public class PSWorkflowJob : Job2, IJobDebugger
    {
        #region Public Accessors

        /// <summary>
        /// Delegate action on workflow idling
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is needed for taking the decision for evicting the workflows.")]
        public Action<PSWorkflowJob, ReadOnlyCollection<BookmarkInfo>> OnIdle { get; set; }

        /// <summary>
        /// Delegate function on workflow persist idle action
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is needed for taking the decision for evicting the workflows.")]
        public Func<PSWorkflowJob, ReadOnlyCollection<BookmarkInfo>, bool, PSPersistableIdleAction> OnPersistableIdleAction { get; set; }

        /// <summary>
        /// Delegate action on workflow unloaded
        /// </summary>
        public Action<PSWorkflowJob> OnUnloaded { get; set; }

        /// <summary>
        /// Signaled when job reaches running state
        /// </summary>
        internal WaitHandle Running { get { return this.JobRunning; } }

        /// <summary>
        /// Signaled when job finishes suspending or aboring
        /// </summary>
        internal WaitHandle SuspendedOrAborted { get { return this.JobSuspendedOrAborted; } }

        /// <summary>
        /// Job input parameters
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal Dictionary<string, object> WorkflowParameters { get { return _workflowParameters; } }

        /// <summary>
        /// Job input parameters including system provided parameters
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal Dictionary<string, object> PSWorkflowCommonParameters { get { return _psWorkflowCommonParameters; } }

        /// <summary>
        /// Job metadata collection
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal Dictionary<string, object> JobMetadata { get { return _jobMetadata; } set { _jobMetadata = value; } }

        /// <summary>
        /// Job invoker metadata collection
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal Dictionary<string, object> PrivateMetadata { get { return _privateMetadata; } }

        /// <summary>
        /// Workflow instance for the job
        /// </summary>
        public PSWorkflowInstance PSWorkflowInstance
        {
            get { return _workflowInstance; }
            internal set { _workflowInstance = value; }
        }

        /// <summary>
        /// Workflow debugger
        /// </summary>
        public Debugger PSWorkflowDebugger
        {
            get
            {
                return (_workflowInstance != null) ? _workflowInstance.PSWorkflowDebugger : null;
            }
        }

        /// <summary>
        /// Workflow job definition
        /// </summary>
        internal WorkflowJobDefinition PSWorkflowJobDefinition
        {
            get { return _definition; }
        }

        internal Dictionary<string, object> JobCreationContext
        {
            get { return _jobCreationContext; }
        }

        #endregion

        #region Private Members

        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private const string ClassNameTrace = "PSWorkflowJob";
        private static readonly Tracer StructuredTracer = new Tracer();
        private readonly object _syncObject = new object();
        private readonly object _resumeErrorSyncObject = new object();
        private bool _isDisposed;
        private string _statusMessage = string.Empty;
        private string _location = string.Empty;
        private readonly WorkflowJobDefinition _definition;
        private readonly PSWorkflowRuntime _runtime;
        private JobState _previousState;
        private PSWorkflowInstance _workflowInstance;
        private static readonly PSPerfCountersMgr _perfCountersMgr = PSPerfCountersMgr.Instance;
        private readonly Dictionary<Guid, Exception> _resumeErrors = new Dictionary<Guid, Exception>();
        private readonly Dictionary<string, object> _jobCreationContext;

        /// <summary>
        /// Input parameters to the workflow instance.
        /// </summary>

        private Dictionary<string, object> _workflowParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, object> _psWorkflowCommonParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, object> _jobMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, object> _privateMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Holds the collection of input objects recieved from the pipeline
        private PSDataCollection<PSObject> _inputCollection;

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

        private bool _isAsync = true;

        #endregion Private Members

        #region Private Methods

        /// <summary>
        /// Provide validation of constructor parameter that could cause NullReferenceException.
        /// </summary>
        /// <param name="specification">JobInvocationInfo for construction.</param>
        /// <returns>specification parameter if not null.</returns>
        private static JobInvocationInfo Validate(JobInvocationInfo specification)
        {
            if (specification == null)
            {
                throw new ArgumentNullException("specification");
            }

            if (specification.Definition == null)
            {
                throw new ArgumentException(Resources.UninitializedSpecification, "specification");
            }

            if (string.IsNullOrEmpty(specification.Definition.Command))
            {
                throw new ArgumentException(Resources.UninitializedSpecification, "specification");
            }

            return specification;
        }

        private void InitializeWithWorkflow(PSWorkflowInstance instance, bool closeStreams = false)
        {
            Dbg.Assert(instance.Streams != null, "Workflow Instance has no stream data.");
            _tracer.WriteMessage(ClassNameTrace, "InitializeWithWorkflow", WorkflowGuidForTraces, this, "Setting streams");
            Output = instance.Streams.OutputStream ?? new PSDataCollection<PSObject>();
            Progress = instance.Streams.ProgressStream ?? new PSDataCollection<ProgressRecord>();
            Warning = instance.Streams.WarningStream ?? new PSDataCollection<WarningRecord>();
            Error = instance.Streams.ErrorStream ?? new PSDataCollection<ErrorRecord>();
            Debug = instance.Streams.DebugStream ?? new PSDataCollection<DebugRecord>();
            Verbose = instance.Streams.VerboseStream ?? new PSDataCollection<VerboseRecord>();
            Information = instance.Streams.InformationStream ?? new PSDataCollection<InformationRecord>();

            if (!closeStreams) return;

            Output.Complete();
            Progress.Complete();
            Warning.Complete();
            Error.Complete();
            Debug.Complete();
            Verbose.Complete();
            Information.Complete();
        }

        private bool _starting;
        private void DoStartJobLogic(object state)
        {
            if (_isDisposed) return;
            Dbg.Assert(state == null, "State is never used");
            _tracer.WriteMessage(ClassNameTrace, "DoStartJobLogic", WorkflowGuidForTraces, this, "BEGIN");
            StructuredTracer.BeginJobLogic(InstanceId);
            lock (SyncRoot)
            {
                AssertValidState(JobState.NotStarted);
                if (_starting || _suspending || _resuming) return;
                _starting = true;
            }

            // Do Not set job state from within the lock.
            // This can cause a deadlock because events are raised.
            DoSetJobState(JobState.Running);

            Dbg.Assert(_workflowInstance != null, "PSWorkflowInstance should have been populated by the adapter");
            Dbg.Assert(_workflowInstance.Id != Guid.Empty, "Workflow has not been loaded before StartJob");

            _tracer.WriteMessage(ClassNameTrace, "DoStartJobLogic", WorkflowGuidForTraces, this, "ready to start");

            _workflowInstance.ExecuteInstance();

            // This message could appear after completion traces in some cases.
            StructuredTracer.WorkflowExecutionStarted(_workflowInstance.Id, string.Empty);
            _tracer.WriteMessage(ClassNameTrace, "DoStartJobLogic", WorkflowGuidForTraces, this, "END");
        }

        private bool _stopCalled;
        private void DoStopJob()
        {
            if (_isDisposed || IsFinishedState(JobStateInfo.State) || JobState.Stopping == JobStateInfo.State || _stopCalled) return;

            bool cancel = false;
            bool waitForRunning = false;
            bool waitForSuspendorAbort = false;

            lock (SyncRoot)
            {
                if (_isDisposed || IsFinishedState(JobStateInfo.State) || JobState.Stopping == JobStateInfo.State || _stopCalled) return;

                if (_suspending)
                    waitForSuspendorAbort = true;
                else if (_starting || _resuming)
                    waitForRunning = true;

                _tracer.WriteMessage(ClassNameTrace, "DoStopJob", WorkflowGuidForTraces, this, "BEGIN");

                _stopCalled = true;

                if (JobStateInfo.State == JobState.Running)
                {
                    cancel = true;
                }
            }

            // actual stop logic goes here
            if (cancel)
            {
                if (waitForSuspendorAbort)
                    JobSuspendedOrAborted.WaitOne();
                else if (waitForRunning)
                    JobRunning.WaitOne();

                // Do not set job state from within the lock.
                // This can cause a deadlock because events are raised.
                DoSetJobState(JobState.Stopping);

                StructuredTracer.CancellingWorkflowExecution(_workflowInstance.Id);
                _workflowInstance.StopInstance();
            }
            else
            {
                // Do not set job state from within the lock.
                // This can cause a deadlock because events are raised.
                DoSetJobState(JobState.Stopping);

                _workflowInstance.State = JobState.Stopped;
                _workflowInstance.PerformTaskAtTerminalState();
                DoSetJobState(JobState.Stopped);
            }
            StructuredTracer.WorkflowExecutionCancelled(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsPerSec);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);

            _tracer.WriteMessage(ClassNameTrace, "DoStopJob", WorkflowGuidForTraces, this, "END");
        }

        private bool _suspending = false;

        /// <summary>
        /// DoSuspendJob returns a bool, but
        /// in the case of a SuspendJob call, we do not
        /// want to return until the job is in the correct state.
        /// The boolean return value is used by the WorkflowManager
        /// for Shutdown purposes.
        /// </summary>
        /// <returns></returns>
        internal bool DoSuspendJob()
        {
            bool waitNeeded = true;

            if (_isDisposed)
            {
                return waitNeeded;
            }

            bool waitForRunning = false;
            bool notStarted = false;

            lock (SyncRoot)
            {
                if (_isDisposed || JobStateInfo.State == JobState.Suspending || JobStateInfo.State == JobState.Suspended ||
                    _suspending)
                {
                    return waitNeeded;
                }

                if (this.IsSuspendable != null && IsSuspendable.HasValue && IsSuspendable.Value == false)
                {
                    _tracer.WriteMessage(ClassNameTrace, "DoSuspendJob", WorkflowGuidForTraces, this,
                                         "The job is not suspendable.");
                    throw new InvalidOperationException(Resources.ErrorMessageForPersistence);
                }

                if (_starting)
                    waitForRunning = true;

                _tracer.WriteMessage(ClassNameTrace, "DoSuspendJob", WorkflowGuidForTraces, this, "BEGIN");
                _suspending = true;

                if (JobStateInfo.State != JobState.Running && JobStateInfo.State != JobState.NotStarted)
                {
                    _tracer.WriteMessage(ClassNameTrace, "DoSuspendJob", WorkflowGuidForTraces, this, "InvalidJobState");
                    throw new InvalidJobStateException(JobStateInfo.State, Resources.SuspendNotValidState);
                }
                if (JobStateInfo.State == JobState.NotStarted)
                {
                    notStarted = true;
                }
            }

            if (waitForRunning)
                JobRunning.WaitOne();

            // Do not set job state from within the lock.
            // This can cause a deadlock because events are raised.
            DoSetJobState(JobState.Suspending);

            lock (SyncRoot)
            {
                _resuming = false;
            }

            _workflowInstance.SuspendInstance(notStarted);

            _tracer.WriteMessage(ClassNameTrace, "DoSuspendJob", WorkflowGuidForTraces, this, "END");
            return waitNeeded;
        }

        /// <summary>
        /// Do Set Job State
        /// </summary>
        /// <param name="state"></param>
        /// <param name="reason"></param>
        /// <returns>returns false if state tranition is not possible. Return value is required to update SQM perf counters</returns>
        private bool DoSetJobState(JobState state, Exception reason = null)
        {
            if (IsFinishedState(_previousState) || _isDisposed) return false;

            lock (_syncObject)
            {
                if (IsFinishedState(_previousState) || _isDisposed) return false;

                // State should not be transitioned to suspended from stopping 
                if (_previousState == JobState.Stopping && state == JobState.Suspended) return false;

                // State should not be transitioned to suspended from suspended 
                if (_previousState == JobState.Suspended && state == JobState.Suspended) return false;

                if (state != _previousState && StructuredTracer.IsEnabled)
                {
                    StructuredTracer.JobStateChanged(Id, InstanceId, state.ToString(), _previousState.ToString());
                }

#if DEBUG
                switch (_previousState)
                {
                    case JobState.Running:
                        Dbg.Assert(state != JobState.NotStarted && state != JobState.Blocked,
                                   "WorkflowJob invalid state transition from Running.");
                        break;
                    case JobState.Stopping:
                        //Dbg.Assert(
                        //    state == JobState.Stopped || state == JobState.Failed || state == JobState.Completed,
                        //    "WorkflowJob invalid state transition from Stopping.");
                        break;
                    case JobState.Stopped:
                        Dbg.Assert(false, "WorkflowJob should never transition after Stopped state.");
                        break;
                    case JobState.Suspending:
                        Dbg.Assert(
                            state == JobState.Suspended || state == JobState.Completed || state == JobState.Failed ||
                            state == JobState.Stopped || state == JobState.Stopping,
                            "WorkflowJob invalid state transition from Suspending.");
                        break;
                    case JobState.Suspended:
                        Dbg.Assert(
                            state == JobState.Running || state == JobState.Stopping || state == JobState.Stopped ||
                            state == JobState.Completed || state == JobState.Failed,
                            "WorkflowJob invalid state transition from Suspended.");
                        break;
                    case JobState.Failed:
                        Dbg.Assert(false, "WorkflowJob should never transition after Failed state.");
                        break;
                    case JobState.Completed:
                        Dbg.Assert(false, "WorkflowJob should never transition after Completed state.");
                        break;
                    case JobState.Disconnected:
                        Dbg.Assert(false, "WorkflowJob should never be in a disconnected state");
                        break;
                    case JobState.Blocked:
                        Dbg.Assert(false, "WorkflowJob should never be in a blocked state");
                        break;
                    default:
                        break;
                }
#endif

                _previousState = state;
                // Update JobMetadata
                if (_workflowInstance != null)
                {
                    _workflowInstance.PSWorkflowContext.JobMetadata.Remove(Constants.JobMetadataStateReason);
                    if (reason != null)
                    {
                        if (StructuredTracer.IsEnabled)
                        {
                            StructuredTracer.JobError(Id, InstanceId, Tracer.GetExceptionString(reason));
                        }
                        _workflowInstance.PSWorkflowContext.JobMetadata.Add(Constants.JobMetadataStateReason, reason);
                    }
                }
            }

            _tracer.WriteMessage(ClassNameTrace, "DoSetJobState", WorkflowGuidForTraces, this, "Setting state to {0}, Setting Reason to exception: {1}", state.ToString(), reason == null ? null : reason.ToString());
            _workflowInstance.State = state;

            SetJobState(state, reason);
            _tracer.WriteMessage(ClassNameTrace, "DoSetJobState", WorkflowGuidForTraces, this, "Done setting state");
            return true;
        }

        /// <summary>
        /// Create necessary dictionaries for WorkflowManager consumption based on StartParameters.
        /// </summary>
        private void SortStartParameters(DynamicActivity dynamicActivity, CommandParameterCollection parameters)
        {
            bool selfRemoting = dynamicActivity != null && dynamicActivity.Properties.Any(x => x.Name.Equals(Constants.ComputerName, StringComparison.CurrentCultureIgnoreCase));
            bool takesPSPrivateMetadata = dynamicActivity != null && dynamicActivity.Properties.Contains(Constants.PrivateMetadata);
            _jobMetadata.Add(Constants.WorkflowTakesPrivateMetadata, takesPSPrivateMetadata);

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    _tracer.WriteMessage(ClassNameTrace, "SortStartParameters", WorkflowGuidForTraces, this, "Found parameter; {0}; {1}", parameter.Name,
                        parameter.Value == null ? null : parameter.Value.ToString());
                    switch (parameter.Name)
                    {
                        case Constants.ComputerName:
                            if (selfRemoting)
                            {
                                // If we're self-remoting, location becomes the default computer
                                // and the PSComputerNames is passed in as an argument instead
                                // of going to the ubiquitious parameters
                                _location = Constants.DefaultComputerName;
                                string parameterName = dynamicActivity.Properties.First(x => x.Name.Equals(Constants.ComputerName, StringComparison.CurrentCultureIgnoreCase)).Name;
                                _workflowParameters[parameterName] = LanguagePrimitives.ConvertTo<string[]>(parameter.Value);
                            }
                            else
                            {
                                // Set _location before adding parameter.
                                var computer = parameter.Value as string;
                                _location = computer;
                                string[] computerNames = LanguagePrimitives.ConvertTo<string[]>(parameter.Value);
                                _psWorkflowCommonParameters[parameter.Name] = computerNames;
                            }
                            break;
                        case Constants.PrivateMetadata:
                            Hashtable privateData = parameter.Value as Hashtable;
                            if (privateData != null)
                            {
                                IDictionaryEnumerator enumerator = privateData.GetEnumerator();
                                while (enumerator.MoveNext())
                                {
                                    _privateMetadata.Add(enumerator.Key.ToString(), enumerator.Value);
                                }

                                // Make the combined object available within the workflow as well...
                                if (takesPSPrivateMetadata)
                                {
                                    _workflowParameters.Add(parameter.Name, parameter.Value);
                                }
                            }
                            break;

                        case Constants.PSInputCollection:
                            {
                                // Remove the input collection so we can properly pass it to the workflow job
                                object baseObject = parameter.Value is PSObject
                                                        ? ((PSObject)parameter.Value).BaseObject
                                                        : parameter.Value;

                                if (baseObject is PSDataCollection<PSObject>)
                                {
                                    _inputCollection = baseObject as PSDataCollection<PSObject>;
                                }
                                else
                                {
                                    var inputCollection = new PSDataCollection<PSObject>();
                                    var e = LanguagePrimitives.GetEnumerator(baseObject);
                                    if (e != null)
                                    {
                                        while (e.MoveNext())
                                        {
                                            inputCollection.Add(PSObject.AsPSObject(e.Current));
                                        }
                                    }
                                    else
                                    {
                                        inputCollection.Add(PSObject.AsPSObject(parameter.Value));
                                    }
                                    _inputCollection = inputCollection;
                                }
                            }
                            break;

                        case Constants.PSParameterCollection:
                            // Remove this one from the parameter collecton...
                            break;
                        case Constants.PSRunningTime:
                        case Constants.PSElapsedTime:
                        case Constants.ConnectionRetryCount:
                        case Constants.ActionRetryCount:
                        case Constants.ConnectionRetryIntervalSec:
                        case Constants.ActionRetryIntervalSec:
                            _psWorkflowCommonParameters.Add(parameter.Name, parameter.Value);
                            break;

                        case Constants.Persist:
                        case Constants.Credential:
                        case Constants.Port:
                        case Constants.UseSSL:
                        case Constants.ConfigurationName:
                        case Constants.ApplicationName:
                        case Constants.ConnectionURI:
                        case Constants.SessionOption:
                        case Constants.Authentication:
                        case Constants.AuthenticationLevel:
                        case Constants.CertificateThumbprint:
                        case Constants.AllowRedirection:
                        case Constants.Verbose:
                        case Constants.Debug:
                        case Constants.ErrorAction:
                        case Constants.WarningAction:
                        case Constants.InformationAction:
                        case Constants.PSWorkflowErrorAction:
                        case Constants.PSSuspendOnError:
                        case Constants.PSSenderInfo:
                        case Constants.ModulePath:
                        case Constants.PSCurrentDirectory:
                            // Note: We don't add ErrorVariable, WarningVariable, OutVariable, or OutBuffer
                            // here because they are interpreted by PowerShell in the function generated over
                            // the workflow definition.
                            _psWorkflowCommonParameters.Add(parameter.Name, parameter.Value);
                            break;
                        default:
                            _workflowParameters.Add(parameter.Name, parameter.Value);
                            break;
                    }
                }
            }

            // Add in the workflow command name...
            _psWorkflowCommonParameters.Add("WorkflowCommandName", _definition.Command);
        }


        private void HandleMyStateChanged(object sender, JobStateEventArgs e)
        {
            _tracer.WriteMessage(ClassNameTrace, "HandleMyStateChanged", WorkflowGuidForTraces, this,
                                 "NewState: {0}; OldState: {1}", e.JobStateInfo.State.ToString(),
                                 e.PreviousJobStateInfo.State.ToString());
            bool unloadStreams = false;
            if (e.PreviousJobStateInfo.State == JobState.NotStarted)
            {
                PSBeginTime = DateTime.Now;
            }

            switch (e.JobStateInfo.State)
            {
                case JobState.Running:
                    {
                        lock (SyncRoot)
                        {
                            _suspending = false;
                            _resuming = false;
                            wfSuspendInProgress = false;
                        }

                        lock (_syncObject)
                        {
                            JobRunning.Set();

                            // Do not create the event if it doesn't already exist. Suspend may never be called.
                            if (_jobSuspendedOrAborted != null)
                                JobSuspendedOrAborted.Reset();

                            // Clear the message indicating that the job was suspended or that the job was queued
                            // for resume.  The job is now running, and that status is no longer valid.
                            _statusMessage = String.Empty;
                        }
                    }
                    break;

                case JobState.Suspended:
                    {
                        lock (SyncRoot)
                        {
                            _suspending = false;
                            _resuming = false;
                            wfSuspendInProgress = false;
                        }

                        PSEndTime = DateTime.Now;
                        lock (_syncObject)
                        {
                            JobSuspendedOrAborted.Set();
                            JobRunning.Reset();
                        }
                        unloadStreams = true;
                    }
                    break;
                case JobState.Failed:
                case JobState.Completed:
                case JobState.Stopped:
                    {
                        PSEndTime = DateTime.Now;

                        lock (_syncObject)
                        {
                            StructuredTracer.EndJobLogic(InstanceId);
                            JobSuspendedOrAborted.Set();

                            // Do not reset JobRunning when the state is terminal.
                            // No thread should wait on a job transitioning again to
                            // JobState.Running.
                            JobRunning.Set();
                        }
                        unloadStreams = true;
                    }
                    break;
            }

            if (!unloadStreams || !_unloadStreamsOnPersistentState) return;

            _tracer.WriteMessage(ClassNameTrace, "HandleMyStateChanged", WorkflowGuidForTraces, this,
                                 "BEGIN Unloading streams from memory");
            SelfUnloadJobStreams();
            _tracer.WriteMessage(ClassNameTrace, "HandleMyStateChanged", WorkflowGuidForTraces, this,
                                 "END Unloading streams from memory");
        }

        private void DoStartJobAsync(object state)
        {
            _tracer.WriteMessage(ClassNameTrace, "DoStartJobAsync", WorkflowGuidForTraces, this, "");
            Dbg.Assert(state == null, "State is never used");
            var asyncOp = AsyncOperationManager.CreateOperation(null);

            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            workerDelegate.BeginInvoke(
                asyncOp,
                ActionType.Start,
                string.Empty,
                string.Empty,
                false,
                null,
                null);
        }

        private void AssertValidState(JobState expectedState)
        {
            AssertNotDisposed();
            lock (SyncRoot)
            {
                if (JobStateInfo.State != expectedState)
                {
                    throw new InvalidJobStateException(JobStateInfo.State, Resources.JobCannotBeStarted);
                }
            }
        }

        private bool _resuming;
        private void DoResumeJob(object state)
        {
            if (_isDisposed) return;
            var tuple = state as Tuple<string, ManualResetEvent>;
            string label = tuple != null ? tuple.Item1 ?? String.Empty : String.Empty;

            if (string.IsNullOrEmpty(label) == false)
            {
                DoLabeledResumeJob(label);
                return;
            }

            lock (SyncRoot)
            {
                if (_isDisposed || JobStateInfo.State == JobState.Running || _resuming) return;
                _tracer.WriteMessage(ClassNameTrace, "DoResumeJob", WorkflowGuidForTraces, this, "BEGIN");

                if (JobStateInfo.State != JobState.Suspended)
                {
                    _tracer.WriteMessage(ClassNameTrace, "DoResumeJob", WorkflowGuidForTraces, this,
                                            "InvalidJobState");
                    throw new InvalidJobStateException(JobStateInfo.State, Resources.ResumeNotValidState);
                }

                // this will avoid the race codition between two resume requests and both are trying to load instance and loadstreams.
                _resuming = true;
            }

            _workflowInstance.DoLoadInstanceForReactivation();

            // load the streams before resuming
            LoadJobStreams();

            // Do not set state within the lock.
            // This can cause a deadlock because events are raised.
            DoSetJobState(JobState.Running);

            // actual logic of resuming a job
            StructuredTracer.WorkflowResuming(_workflowInstance.Id);
            _workflowInstance.ResumeInstance(label);

            lock (SyncRoot)
            {
                // once the resume instance is called then we can allow the subsequent suspend requests
                _suspending = false;
                wfSuspendInProgress = false;

            }

            StructuredTracer.WorkflowResumed(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "DoResumeJob", WorkflowGuidForTraces, this, "END");
        }

        private void DoLabeledResumeJob(string label)
        {
            bool waitNeeded = false;

            lock (SyncRoot)
            {
                if (_isDisposed) return;
                _tracer.WriteMessage(ClassNameTrace, "DoLabeledResumeJob", WorkflowGuidForTraces, this, "BEGIN");

                if (wfSuspendInProgress)
                {
                    waitNeeded = true;
                }

                listOfLabels.Add(label);
            }

            try
            {
                if (waitNeeded)
                    JobSuspendedOrAborted.WaitOne();

                lock (SyncRoot)
                {
                    wfSuspendInProgress = false;

                    if (JobStateInfo.State != JobState.Suspended && JobStateInfo.State != JobState.Running)
                    {
                        _tracer.WriteMessage(ClassNameTrace, "DoLabeledResumeJob", WorkflowGuidForTraces, this, "InvalidJobState");
                        throw new InvalidJobStateException(JobStateInfo.State, Resources.ResumeNotValidState);
                    }

                    if (JobStateInfo.State != JobState.Running && !_resuming)
                        _workflowInstance.DoLoadInstanceForReactivation();

                    // this needs to be done here because this call may throw
                    // invalid bookmark exception, which should be happened before
                    // setting the job state to running.
                    _workflowInstance.ValidateIfLabelExists(label);

                    _resuming = true;
                }

                // load the streams before resuming
                LoadJobStreams();

                // Do not set state within the lock.
                // This can cause a deadlock because events are raised.
                DoSetJobState(JobState.Running);

                // actual logic of resuming a job
                StructuredTracer.WorkflowResuming(_workflowInstance.Id);
                _workflowInstance.ResumeInstance(label);

                lock (SyncRoot)
                {
                    // once the resume instance is called then we can allow the subsequent suspend requests
                    _suspending = false;
                }
            }
            finally
            {
                lock (SyncRoot)
                {
                    listOfLabels.Remove(label);
                }
            }

            StructuredTracer.WorkflowResumed(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "DoLabeledResumeJob", WorkflowGuidForTraces, this, "END");
        }

        private void DoResumeJobCatchException(object state)
        {
            var tuple = state as Tuple<object, Guid>;
            Dbg.Assert(tuple != null, "ResumeJob with exception handling should have tuple.");
            try
            {
                DoResumeJob(tuple.Item1);
            }
            catch (Exception e)
            {
                lock (_resumeErrorSyncObject)
                {
                    if (!_resumeErrors.ContainsKey(tuple.Item2))
                        _resumeErrors.Add(tuple.Item2, e);
                }
            }
            finally
            {
                // Blue: 79098
                // There was a race condition between ResumeJob(label) and DoResumeJobCatchException() in setting and accessing the resume errors.
                // ManualResetEvent should be set here after adding resume errors if any.
                var eventTuple = tuple.Item1 as Tuple<string, ManualResetEvent>;
                if (eventTuple != null && eventTuple.Item2 != null)
                {
                    eventTuple.Item2.Set();
                }
            }
        }

        /// <summary>
        /// DoResumeBookmark
        /// </summary>
        /// <param name="bookmark">The Bookmark which needs to be resumed.</param>
        /// <param name="state">The state, which will be passed to the activity, which gets resumed.</param>
        protected virtual void DoResumeBookmark(Bookmark bookmark, object state)
        {
            if (_isDisposed) return;

            lock (SyncRoot)
            {
                if (_isDisposed) return;

                if (this.IsFinishedState(JobStateInfo.State) || JobStateInfo.State == JobState.Stopping)
                {
                    _tracer.WriteMessage(ClassNameTrace, "DoResumeBookmark", WorkflowGuidForTraces, this, "InvalidJobState to resume a bookmark");
                    throw new InvalidJobStateException(JobStateInfo.State, Resources.ResumeNotValidState);
                }
            }

            // Do Not set job state from within the lock.
            // This can cause a deadlock because events are raised.
            //
            DoSetJobState(JobState.Running);

            _workflowInstance.ResumeBookmark(bookmark, state);
        }

        private bool DoTerminateJob(string reason, bool suppressError = false)
        {
            if (_isDisposed) return false;
            bool terminated = false;
            lock (SyncRoot)
            {
                if (_isDisposed) return false;
                _tracer.WriteMessage(ClassNameTrace, "DoTerminateJob", WorkflowGuidForTraces, this, "BEGIN");
                if (JobStateInfo.State == JobState.Running || JobStateInfo.State == JobState.NotStarted)
                {
                    _tracer.WriteMessage("trying to terminate running workflow job");
                    _workflowInstance.CheckForTerminalAction();
                    _workflowInstance.TerminateInstance(reason, suppressError);
                    terminated = true;
                }
                else if (JobStateInfo.State == JobState.Suspended)
                {
                    _tracer.WriteMessage("Trying to load and terminate suspended workflow");
                    _workflowInstance.DoLoadInstanceForReactivation();
                    _workflowInstance.TerminateInstance(reason, suppressError);
                    terminated = true;
                }
            }

            _tracer.WriteMessage(ClassNameTrace, "DoTerminateJob", WorkflowGuidForTraces, this, "END");
            return terminated;
        }

        internal bool DoAbortJob(string reason)
        {
            if (_isDisposed) return false;

            bool waitForRunning = false;

            lock (SyncRoot)
            {
                if (_isDisposed) return false;

                if (_isDisposed || JobStateInfo.State == JobState.Suspending || JobStateInfo.State == JobState.Suspended || _suspending)
                    return false;

                if (_starting)
                    waitForRunning = true;

                _tracer.WriteMessage(ClassNameTrace, "DoAbortJob", WorkflowGuidForTraces, this, "BEGIN");
                _suspending = true;

                if (JobStateInfo.State != JobState.Running && JobStateInfo.State != JobState.NotStarted)
                {
                    _tracer.WriteMessage(ClassNameTrace, "DoAbortJob", WorkflowGuidForTraces, this, "InvalidJobState");
                    throw new InvalidJobStateException(JobStateInfo.State, Resources.SuspendNotValidState);
                }
            }

            // Do not set job state from within lock.
            // This can cause a deadlock because events are raised.
            DoSetJobState(JobState.Suspending);

            lock (SyncRoot)
            {
                _resuming = false;
            }

            if (waitForRunning)
                JobRunning.WaitOne();

            _workflowInstance.CheckForTerminalAction();
            _workflowInstance.AbortInstance(reason);

            _tracer.WriteMessage(ClassNameTrace, "DoAbortJob", WorkflowGuidForTraces, this, "END");

            return true;
        }

        private void OnWorkflowCompleted(object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowCompleted", WorkflowGuidForTraces, this, "BEGIN");
            DoSetJobState(JobState.Completed);
            StructuredTracer.WorkflowExecutionFinished(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.SucceededWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.SucceededWorkflowJobsPerSec);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowCompleted", WorkflowGuidForTraces, this, "END");
        }

        private void OnWorkflowAborted(Exception e, object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowAborted", WorkflowGuidForTraces, this, "BEGIN");

            DoSetJobState(JobState.Suspended, e);

            StructuredTracer.WorkflowExecutionAborted(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.FailedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                   PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                   PSWorkflowPerformanceCounterIds.FailedWorkflowJobsPerSec);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowAborted", WorkflowGuidForTraces, this, "END");
        }

        private void OnWorkflowFaulted(Exception e, object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowFaulted", WorkflowGuidForTraces, this, "BEGIN");
            StructuredTracer.WorkflowExecutionError(_workflowInstance.Id, Tracer.GetExceptionString(e) + Environment.NewLine + e);
            DoSetJobState(JobState.Failed, e);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.FailedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.FailedWorkflowJobsPerSec);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowFaulted", WorkflowGuidForTraces, this, "END");
        }

        private void OnWorkflowStopped(object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowStopped", WorkflowGuidForTraces, this, "BEGIN");
            DoSetJobState(JobState.Stopped);
            StructuredTracer.WorkflowExecutionCancelled(_workflowInstance.Id);
            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsPerSec);
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
            _perfCountersMgr.UpdateCounterByValue(
               PSWorkflowPerformanceCounterSetInfo.CounterSetId,
               PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowStopped", WorkflowGuidForTraces, this, "END");
        }

        private void OnWorkflowSuspended(object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowSuspended", WorkflowGuidForTraces, this, "BEGIN");

            // CheckStopping() was not thread safe.
            // Now DoSetJobState handles the invalid state transion from stopping to suspended
            if (DoSetJobState(JobState.Suspended))
            {
                StructuredTracer.WorkflowUnloaded(_workflowInstance.Id);
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.SuspendedWorkflowJobsCount);
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.SuspendedWorkflowJobsPerSec);
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount);
                _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec);
            }
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowSuspended", WorkflowGuidForTraces, this, "END");
        }

        private void OnWorkflowIdle(ReadOnlyCollection<BookmarkInfo> bookmarks, object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowIdle", WorkflowGuidForTraces, this, "BEGIN");

            if (this.OnIdle != null) OnIdle(this, bookmarks);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowIdle", WorkflowGuidForTraces, this, "END");
        }

        private PSPersistableIdleAction OnWorkflowPersistableIdleAction(ReadOnlyCollection<BookmarkInfo> bookmarks, bool externalSuspendRequest, object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowPersistIdleAction", WorkflowGuidForTraces, this, "BEGIN");

            PSPersistableIdleAction rc = PSPersistableIdleAction.NotDefined;

            if (this.OnPersistableIdleAction != null)
                rc = this.OnPersistableIdleAction(this, bookmarks, externalSuspendRequest);

            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowPersistIdleAction", WorkflowGuidForTraces, this, "END");

            return rc;
        }

        private void OnWorkflowUnloaded(object sender)
        {
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowUnloaded", WorkflowGuidForTraces, this, "BEGIN");
            if (this.OnUnloaded != null) OnUnloaded(this);
            _tracer.WriteMessage(ClassNameTrace, "OnWorkflowUnloaded", WorkflowGuidForTraces, this, "END");
        }

        /// <summary>
        /// Unloads the streams of the job. 
        /// </summary>
        /// <remarks>
        /// To be called from this class only</remarks>
        private void SelfUnloadJobStreams()
        {
            if (_hasMoreDataOnDisk) return;
            lock (SyncRoot)
            {
                if (_hasMoreDataOnDisk) return;

                Dbg.Assert(JobStateInfo.State == JobState.Stopped ||
                           JobStateInfo.State == JobState.Completed ||
                           JobStateInfo.State == JobState.Failed ||
                           JobStateInfo.State == JobState.Suspended, "Job state is incorrect when unload is called");
                UnloadJobStreams();
            }
        }

        #endregion Private Methods

        #region Internal Accessors

        internal Guid WorkflowGuid { get { return _workflowInstance.Id; } }
        private Guid WorkflowGuidForTraces { get { if (_workflowInstance != null) return _workflowInstance.Id; return Guid.Empty; } }

        internal bool WorkflowInstanceLoaded { get; set; }

        internal bool SynchronousExecution { get; set; }

        internal bool? IsSuspendable = null;

        internal PSLanguageMode? SourceLanguageMode { get; set; }

        #endregion Internal Accessors

        #region Internal Methods

        private bool _unloadStreamsOnPersistentState;
        internal void EnableStreamUnloadOnPersistentState()
        {
            _unloadStreamsOnPersistentState = true;
        }

        /// <summary>
        /// Helper function to check if job is finished
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal bool IsFinishedState(JobState state)
        {
            return (state == JobState.Completed || state == JobState.Failed || state == JobState.Stopped);
        }

        internal void LoadWorkflow(CommandParameterCollection commandParameterCollection, Activity activity, string xaml)
        {
            _tracer.WriteMessage(ClassNameTrace, "LoadWorkflow", WorkflowGuidForTraces, this, "BEGIN");
            Dbg.Assert(_workflowInstance == null, "LoadWorkflow() should only be called once by the adapter");

            // If activity hasn't been cached, we can't generate it from _definition.
            if (activity == null)
            {
                bool windowsWorkflow;
                activity = DefinitionCache.Instance.GetActivityFromCache(_definition, out windowsWorkflow);

                if (activity == null)
                {
                    // The workflow cannot be run.
                    throw new InvalidOperationException(Resources.ActivityNotCached);
                }
            }

            string workflowXaml;
            string runtimeAssemblyPath;

            if (string.IsNullOrEmpty(xaml))
            {
                workflowXaml = DefinitionCache.Instance.GetWorkflowXaml(_definition);
                runtimeAssemblyPath = DefinitionCache.Instance.GetRuntimeAssemblyPath(_definition);
            }
            else
            {
                workflowXaml = xaml;
                runtimeAssemblyPath = null;
            }

            _location = null;
            SortStartParameters(activity as DynamicActivity, commandParameterCollection);

            // Set location if ComputerName wasn't specified in the parameters.
            if (string.IsNullOrEmpty(_location)) _location = Constants.DefaultComputerName;
            if (_jobMetadata.ContainsKey(Constants.JobMetadataLocation))
                _jobMetadata.Remove(Constants.JobMetadataLocation);
            _jobMetadata.Add(Constants.JobMetadataLocation, _location);

            if (_jobMetadata.ContainsKey(Constants.WorkflowJobCreationContext))
                _jobMetadata.Remove(Constants.WorkflowJobCreationContext);
            _jobMetadata.Add(Constants.WorkflowJobCreationContext, _jobCreationContext);

            PSWorkflowDefinition definition = new PSWorkflowDefinition(activity, workflowXaml, runtimeAssemblyPath, _definition.RequiredAssemblies);
            PSWorkflowContext metadatas = new PSWorkflowContext(_workflowParameters, _psWorkflowCommonParameters, _jobMetadata, _privateMetadata);

            _workflowInstance = _runtime.Configuration.CreatePSWorkflowInstance(definition, metadatas, _inputCollection, this);
            this.ConfigureWorkflowHandlers();

            // Create a WorkflowApplication instance.
            _tracer.WriteMessage(ClassNameTrace, "LoadWorkflow", WorkflowGuidForTraces, this, "Calling instance loader");

            #if DEBUG
            try
            {
                _workflowInstance.CreateInstance();
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("Cannot create unknown type", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Capture environment to help diagnose: MSFT:246456
                    PSObject inputObject = new PSObject();
                    inputObject.Properties.Add(
                        new PSNoteProperty("activity", activity));
                    inputObject.Properties.Add(
                        new PSNoteProperty("workflowXaml", workflowXaml));
                    inputObject.Properties.Add(
                        new PSNoteProperty("runtimeAssemblyPath", runtimeAssemblyPath));
                    inputObject.Properties.Add(
                        new PSNoteProperty("_definition.RequiredAssemblies", _definition.RequiredAssemblies));

                    string tempPath = System.IO.Path.GetTempFileName();
                    System.Management.Automation.PowerShell.Create().AddCommand("Export-CliXml").
                        AddParameter("InputObject", inputObject).
                        AddParameter("Depth", 10).
                        AddParameter("Path", tempPath).Invoke();

                    throw new Exception("Bug MSFT:246456 detected. Please capture " + tempPath + ", open a new issue " +
                        "at https://github.com/PowerShell/PowerShell/issues/new and attach the file.");
                }
                else
                {
                    throw;
                }
            }
            #else
            _workflowInstance.CreateInstance();
            #endif

            InitializeWithWorkflow(_workflowInstance);
            WorkflowInstanceLoaded = true;
            _tracer.WriteMessage(ClassNameTrace, "LoadWorkflow", WorkflowGuidForTraces, this, "END");
        }

        internal void ConfigureWorkflowHandlers()
        {
            _workflowInstance.OnCompletedDelegate = OnWorkflowCompleted;
            _workflowInstance.OnSuspenedDelegate = OnWorkflowSuspended;
            _workflowInstance.OnStoppedDelegate = OnWorkflowStopped;
            _workflowInstance.OnAbortedDelegate = OnWorkflowAborted;
            _workflowInstance.OnFaultedDelegate = OnWorkflowFaulted;
            _workflowInstance.OnIdleDelegate = OnWorkflowIdle;
            _workflowInstance.OnPersistableIdleActionDelegate = OnWorkflowPersistableIdleAction;
            _workflowInstance.OnUnloadedDelegate = OnWorkflowUnloaded;
        }

        internal void RestoreFromWorkflowInstance(PSWorkflowInstance instance)
        {
            _tracer.WriteMessage(ClassNameTrace, "RestoreFromWorkflowInstance", WorkflowGuidForTraces, this, "BEGIN");
            Dbg.Assert(instance != null, "cannot restore a workflow job with null workflow instance");

            object data;
            Exception reason = null;

            if (instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataStateReason, out data))
                reason = data as Exception;
            else
                reason = instance.Error;

            // igorse: restore all of the job metadata
            _workflowParameters = instance.PSWorkflowContext.WorkflowParameters;
            _psWorkflowCommonParameters = instance.PSWorkflowContext.PSWorkflowCommonParameters;
            _jobMetadata = instance.PSWorkflowContext.JobMetadata;
            _privateMetadata = instance.PSWorkflowContext.PrivateMetadata;

            if (instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataLocation, out data)) _location = data as string;
            if (instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataStatusMessage, out data)) _statusMessage = data as string;
            if (instance.State == JobState.Suspended)
            {
                // Status message cannot be set publicly.
                _statusMessage = Resources.SuspendedJobRecoveredFromPreviousSession;
            }

            // indicate that this job has results on disk. This will ensure
            // that the results are not loaded when doing a get-job
            lock (_syncObject)
            {
                _hasMoreDataOnDisk = true;
            }

            Dbg.Assert(instance.JobStateRetrieved,
                       "Cannot set job state when job state is not retrieved or when there is an error in retrival");
            // igorse: set job state when job is fully restored as StateChanged event will fire
            DoSetJobState(instance.State, reason);
            _tracer.WriteMessage(ClassNameTrace, "RestoreFromWorkflowInstance", WorkflowGuidForTraces, this, "END");
        }

        #endregion Internal Methods

        #region Async helpers

        private enum ActionType
        {
            Start = 0,
            Stop = 1,
            Suspend = 2,
            Resume = 3,
            Abort = 4,
            Terminate = 5
        }

        private class AsyncCompleteContainer
        {
            internal AsyncCompletedEventArgs EventArgs;
            internal ActionType Action;
        }

        private delegate void JobActionWorkerDelegate(AsyncOperation asyncOp, ActionType action, string reason, string label, bool suppressError);
        private void JobActionWorker(AsyncOperation asyncOp, ActionType action, string reason, string label, bool suppressError)
        {
            Exception exception = null;

#pragma warning disable 56500
            try
            {
                switch (action)
                {
                    case ActionType.Start:
                        DoStartJobLogic(null);
                        break;

                    case ActionType.Stop:
                        DoStopJob();
                        break;

                    case ActionType.Suspend:
                        DoSuspendJob();
                        break;

                    case ActionType.Resume:
                        DoResumeJob(label);
                        break;

                    case ActionType.Abort:
                        DoAbortJob(reason);
                        break;

                    case ActionType.Terminate:
                        DoTerminateJob(reason, suppressError);
                        break;
                }
            }
            catch (Exception e)
            {
                // Called on a background thread, need to include any exception in
                // event arguments.
                exception = e;
            }
#pragma warning restore 56500

            var eventArgs = new AsyncCompletedEventArgs(exception, false, asyncOp.UserSuppliedState);

            var container = new AsyncCompleteContainer { EventArgs = eventArgs, Action = action };

            // End the task. The asyncOp object is responsible 
            // for marshaling the call.
            asyncOp.PostOperationCompleted(JobActionAsyncCompleted, container);
        }

        private void JobActionAsyncCompleted(object operationState)
        {
            if (_isDisposed) return;
            var container = operationState as AsyncCompleteContainer;
            Dbg.Assert(container != null, "AsyncCompleteContainer cannot be null");
            _tracer.WriteMessage(ClassNameTrace, "JobActionAsyncCompleted", WorkflowGuidForTraces, this, "operation: {0}", container.Action.ToString());
            try
            {
                switch (container.Action)
                {
                    case ActionType.Start:
                        if (container.EventArgs.Error == null) JobRunning.WaitOne();
                        OnStartJobCompleted(container.EventArgs);
                        break;
                    case ActionType.Stop:
                    case ActionType.Terminate:
                        if (container.EventArgs.Error == null) Finished.WaitOne();
                        OnStopJobCompleted(container.EventArgs);
                        break;
                    case ActionType.Suspend:
                        if (container.EventArgs.Error == null) JobSuspendedOrAborted.WaitOne();
                        OnSuspendJobCompleted(container.EventArgs);
                        break;
                    case ActionType.Abort:
                        if (container.EventArgs.Error == null) JobSuspendedOrAborted.WaitOne();
                        OnSuspendJobCompleted(container.EventArgs);
                        break;
                    case ActionType.Resume:
                        if (container.EventArgs.Error == null) JobRunning.WaitOne();
                        OnResumeJobCompleted(container.EventArgs);
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                // An object disposed exception can be thrown by any of the above WaitOne() statements.
                // To otherwise prevent this, locking could be done. That would perf implications for every
                // iteration of this code, and would open a possibility for deadlocks through multiple lock
                // objects.
            }
        }

        #endregion Async helpers

        #region Constructors

        /// <summary>
        /// Construct a PSWorkflowJob.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="specification">JobInvocationInfo representing the command this job
        /// will invoke.</param>
        internal PSWorkflowJob(PSWorkflowRuntime runtime, JobInvocationInfo specification)
            : base(Validate(specification).Command)
        {
            Dbg.Assert(runtime != null, "runtime must not be null.");
            // If specification is null, ArgumentNullException would be raised from
            // the static validate method.
            StartParameters = specification.Parameters;
            _definition = WorkflowJobDefinition.AsWorkflowJobDefinition(specification.Definition);

            _runtime = runtime;
            CommonInit();
        }

        /// <summary>
        /// Construct a PSWorkflowJob.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="specification">JobInvocationInfo representing the command this job will invoke.</param>
        /// <param name="JobInstanceId"></param>
        /// <param name="creationContext"></param>
        internal PSWorkflowJob(PSWorkflowRuntime runtime, JobInvocationInfo specification, Guid JobInstanceId, Dictionary<string, object> creationContext)
            : base(Validate(specification).Command, specification.Definition.Name, JobInstanceId)
        {
            Dbg.Assert(runtime != null, "runtime must not be null.");
            // If specification is null, ArgumentNullException would be raised from
            // the static validate method.
            StartParameters = specification.Parameters;
            _definition = WorkflowJobDefinition.AsWorkflowJobDefinition(specification.Definition);
            _jobCreationContext = creationContext;

            _runtime = runtime;
            CommonInit();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        internal PSWorkflowJob(PSWorkflowRuntime runtime, string command, string name)
            : base(command, name)
        {
            Dbg.Assert(runtime != null, "runtime must not be null.");

            _runtime = runtime;
            CommonInit();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="token"></param>
        internal PSWorkflowJob(PSWorkflowRuntime runtime, string command, string name, JobIdentifier token)
            : base(command, name, token)
        {
            Dbg.Assert(runtime != null, "runtime must not be null.");

            _runtime = runtime;
            CommonInit();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="instanceId"></param>
        internal PSWorkflowJob(PSWorkflowRuntime runtime, string command, string name, Guid instanceId)
            : base(command, name, instanceId)
        {
            Dbg.Assert(runtime != null, "runtime must not be null.");

            _runtime = runtime;
            CommonInit();
        }

        private void CommonInit()
        {
            PSJobTypeName = WorkflowJobSourceAdapter.AdapterTypeName;
            StateChanged += HandleMyStateChanged;
            _tracer.WriteMessage(ClassNameTrace, "CommonInit", WorkflowGuidForTraces, this, "Construction/initialization");

        }

        #endregion Constructors

        #region Overrides of Job

        /// <summary>
        /// 
        /// </summary>
        public override void StopJob()
        {
            AssertNotDisposed();
            DoStopJob();
            Finished.WaitOne();
        }

        /// <summary>
        /// Success status of the command execution.
        /// </summary>
        public override string StatusMessage
        {
            get { return _statusMessage; }
        }

        /// <summary>
        /// Indicates that more data is available in this result object for reading.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (_hasMoreDataOnDisk
                   || Output.IsOpen || Output.Count > 0
                   || Error.IsOpen || Error.Count > 0
                   || Verbose.IsOpen || Verbose.Count > 0
                   || Debug.IsOpen || Debug.Count > 0
                   || Warning.IsOpen || Warning.Count > 0
                   || Progress.IsOpen || Progress.Count > 0
                   || Information.IsOpen || Information.Count > 0
                   );
            }
        }

        private bool _hasMoreDataOnDisk;

        /// <summary>
        /// Indicates a location where this job is running.
        /// </summary>
        public override string Location
        {
            get { return _location; }
        }

        #endregion

        #region Overrides of Job2

        /// <summary>
        /// Implementation of this method will allow the delayed loadig of streams.
        /// </summary>
        protected override void DoLoadJobStreams()
        {
            lock (_syncObject)
            {
                bool closeStreams = IsFinishedState(JobStateInfo.State);
                InitializeWithWorkflow(PSWorkflowInstance, closeStreams);
                _hasMoreDataOnDisk = false;
            }
        }

        /// <summary>
        /// Unloads job streams information. Enables jobs to
        /// clear stream information from memory
        /// </summary>
        protected override void DoUnloadJobStreams()
        {
            if (_workflowInstance == null) return;

            lock (_syncObject)
            {
                if (_workflowInstance == null) return;

                // if persisting the streams was necessary and
                // there was an error it means that the streams
                // contain objects that are not serializable
                // In such a case we should not dispose the
                // streams from memory
                if (!_workflowInstance.SaveStreamsIfNecessary()) return;

                _hasMoreDataOnDisk = true;
                _workflowInstance.DisposeStreams();
            }
        }

        /// <summary>
        /// start a job. The job will be started with the parameters
        /// specified in StartParameters
        /// </summary>
        /// <remarks>It is redudant to have a method named StartJob
        /// on a job class. However, this is done so as to avoid
        /// an FxCop violation "CA1716:IdentifiersShouldNotMatchKeywords"
        /// Stop and Resume are reserved keyworks in C# and hence cannot
        /// be used as method names. Therefore to be consistent it has
        /// been decided to use *Job in the name of the methods</remarks>
        public override void StartJob()
        {
            AssertValidState(JobState.NotStarted);
            _isAsync = false;
            _runtime.JobManager.SubmitOperation(this, DoStartJobLogic, null, JobState.NotStarted);
            JobRunning.WaitOne();
        }

        /// <summary>
        /// Start a job asynchronously
        /// </summary>
        public override void StartJobAsync()
        {
#pragma warning disable 56500
            try
            {
                // Assert here and catch it for event arguments to reduce the number
                // of error operations in the throttle manager.
                AssertValidState(JobState.NotStarted);
                _runtime.JobManager.SubmitOperation(this, DoStartJobAsync, null, JobState.NotStarted);
            }
            catch (Exception e)
            {
                // Catching all exceptions is valid here because
                // Transfering exception with event arguments.
                OnStartJobCompleted(new AsyncCompletedEventArgs(e, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Stop a job asynchronously
        /// </summary>
        public override void StopJobAsync()
        {
            AssertNotDisposed();
            _tracer.WriteMessage(ClassNameTrace, "StopJobAsync", WorkflowGuidForTraces, this, "");
            var asyncOp = AsyncOperationManager.CreateOperation(null);

            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            workerDelegate.BeginInvoke(
                asyncOp,
                ActionType.Stop,
                string.Empty,
                string.Empty,
                false,
                null,
                null);
        }

        /// <summary>
        /// Suspend a job
        /// </summary>
        public override void SuspendJob()
        {
            AssertNotDisposed();

            DoSuspendJob();

            // DoSuspendJob returns a bool, but
            // in the case of a SuspendJob call, we do not
            // want to return until the job is in the correct state.
            // The boolean return value is used by the WorkflowManager
            // for Shutdown purposes.
            JobSuspendedOrAborted.WaitOne();
        }

        /// <summary>
        /// Asynchronously suspend a job
        /// </summary>
        public override void SuspendJobAsync()
        {
            AssertNotDisposed();
            _tracer.WriteMessage(ClassNameTrace, "SuspendJobAsync", WorkflowGuidForTraces, this, "");
            var asyncOp = AsyncOperationManager.CreateOperation(null);

            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            workerDelegate.BeginInvoke(
                asyncOp,
                ActionType.Suspend,
                string.Empty,
                string.Empty,
                false,
                null,
                null);
        }

        /// <summary>
        /// Stop Job
        /// </summary>
        /// <param name="force">True to force stop</param>
        /// <param name="reason">Reason for forced stop</param>
        public override void StopJob(bool force, string reason)
        {
            StopJob(force, reason, false);
        }

        /// <summary>
        /// Stop Job
        /// </summary>
        /// <param name="force">True to force stop</param>
        /// <param name="reason">Reason for forced stop</param>
        /// <param name="suppressError">Suppress error for forced stop</param>
        public void StopJob(bool force, string reason, bool suppressError)
        {
            AssertNotDisposed();

            if (force)
            {
                if (DoTerminateJob(reason, suppressError))
                    Finished.WaitOne();
            }
            else
            {
                DoStopJob();
                Finished.WaitOne();
            }
        }

        /// <summary>
        /// Stop Job Asynchronously
        /// </summary>
        /// <param name="force">True to force stop</param>
        /// <param name="reason">Reason for forced stop</param>
        public override void StopJobAsync(bool force, string reason)
        {
            StopJobAsync(force, reason, false);
        }

        /// <summary>
        /// Stop job asynchronously
        /// </summary>
        /// <param name="force">True to force stop</param>
        /// <param name="reason">Reason for forced stop</param>
        /// <param name="suppressError">Suppress error for forced stop</param>
        public void StopJobAsync(bool force, string reason, bool suppressError)
        {
            AssertNotDisposed();
            _tracer.WriteMessage(ClassNameTrace, "StopJobAsync", WorkflowGuidForTraces, this, "");

            var asyncOp = AsyncOperationManager.CreateOperation(null);
            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);

            ActionType actionType = ActionType.Stop;

            if (force)
            {
                actionType = ActionType.Terminate;
            }

            workerDelegate.BeginInvoke(
                asyncOp,
                actionType,
                reason,
                string.Empty,
                suppressError,
                null,
                null);
        }

        /// <summary>
        /// Suspend Job
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJob(bool force, string reason)
        {
            AssertNotDisposed();

            if (force)
            {
                DoAbortJob(reason);
                JobSuspendedOrAborted.WaitOne();
            }
            else
            {
                DoSuspendJob();
                JobSuspendedOrAborted.WaitOne();
            }
        }

        /// <summary>
        /// Suspend Job Asynchronously
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJobAsync(bool force, string reason)
        {
            AssertNotDisposed();
            _tracer.WriteMessage(ClassNameTrace, "SuspendJobAsync", WorkflowGuidForTraces, this, "");

            var asyncOp = AsyncOperationManager.CreateOperation(null);
            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            ActionType actionType = ActionType.Suspend;

            if (force)
            {
                actionType = ActionType.Abort;
            }

            workerDelegate.BeginInvoke(
                    asyncOp,
                    actionType,
                    reason,
                    string.Empty,
                    false,
                    null,
                    null);
        }

        /// <summary>
        /// Resume a suspended job
        /// </summary>
        public override void ResumeJob()
        {
            ResumeJob(null);
        }

        /// <summary>
        /// Resume a suspended job
        /// </summary>
        /// <param name="label"></param>
        public virtual void ResumeJob(string label)
        {
            AssertNotDisposed();
            var errorId = Guid.NewGuid();
            var waitHandle = new ManualResetEvent(false);
            _statusMessage = Resources.JobQueuedForResume;
            _runtime.JobManager.SubmitOperation(this, DoResumeJobCatchException, new Tuple<object, Guid>(new Tuple<string, ManualResetEvent>(label, waitHandle), errorId), JobState.Suspended);
            waitHandle.WaitOne();
            waitHandle.Dispose();

            lock (_resumeErrorSyncObject)
            {
                Exception exception;
                if (_resumeErrors.TryGetValue(errorId, out exception))
                {
                    _resumeErrors.Remove(errorId);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// ResumeBookmark
        /// </summary>
        /// <param name="bookmark"></param>
        /// <param name="state"></param>
        public void ResumeBookmark(Bookmark bookmark, object state)
        {
            if (bookmark == null) throw new ArgumentNullException("bookmark");

            DoResumeBookmark(bookmark, state);
        }

        /// <summary>
        /// ResumeBookmark
        /// </summary>
        /// <param name="bookmark"></param>
        /// <param name="supportDisconnectedStreams"></param>
        /// <param name="streams"></param>
        public void ResumeBookmark(Bookmark bookmark, bool supportDisconnectedStreams, PowerShellStreams<PSObject, PSObject> streams)
        {
            if (bookmark == null) throw new ArgumentNullException("bookmark");
            if (streams == null) throw new ArgumentNullException("streams");

            PSResumableActivityContext arguments = new PSResumableActivityContext(streams);
            arguments.SupportDisconnectedStreams = supportDisconnectedStreams;
            arguments.Failed = false;
            arguments.Error = null;

            DoResumeBookmark(bookmark, arguments);
        }

        /// <summary>
        /// ResumeBookmark
        /// </summary>
        /// <param name="bookmark"></param>
        /// <param name="supportDisconnectedStreams"></param>
        /// <param name="streams"></param>
        /// <param name="exception"></param>
        public void ResumeBookmark(Bookmark bookmark, bool supportDisconnectedStreams, PowerShellStreams<PSObject, PSObject> streams, Exception exception)
        {
            if (bookmark == null) throw new ArgumentNullException("bookmark");
            if (streams == null) throw new ArgumentNullException("streams");
            if (exception == null) throw new ArgumentNullException("exception");

            PSResumableActivityContext arguments = new PSResumableActivityContext(streams);
            arguments.SupportDisconnectedStreams = supportDisconnectedStreams;
            arguments.Failed = true;
            arguments.Error = exception;

            DoResumeBookmark(bookmark, arguments);
        }

        /// <summary>
        /// GetPersistableIdleAction
        /// </summary>
        /// <param name="bookmarks"></param>
        /// <param name="externalSuspendRequest"></param>
        /// <returns></returns>
        public PSPersistableIdleAction GetPersistableIdleAction(ReadOnlyCollection<BookmarkInfo> bookmarks, bool externalSuspendRequest)
        {
            PSPersistableIdleAction defaultValue = this.PSWorkflowInstance.GetPersistableIdleAction(bookmarks, externalSuspendRequest);

            if (defaultValue == PSPersistableIdleAction.Suspend && listOfLabels.Count > 0)
            {
                // Labeled resumption is in progress so we cannot allow the suspension
                return PSPersistableIdleAction.None;
            }


            lock (_syncObject)
            {
                if (defaultValue == PSPersistableIdleAction.Suspend && listOfLabels.Count > 0)
                {
                    // Labeled resumption is in progress so we cannot allow the suspension
                    return PSPersistableIdleAction.None;
                }
                
                if (defaultValue == PSPersistableIdleAction.Suspend)
                    wfSuspendInProgress = true;

                return defaultValue;
            }
        }

        private bool wfSuspendInProgress = false;
        private List<string> listOfLabels = new List<string>();


        /// <summary>
        /// Resume a suspended job asynchronously.
        /// </summary>
        public override void ResumeJobAsync()
        {
#pragma warning disable 56500
            try
            {
                AssertNotDisposed();
                _runtime.JobManager.SubmitOperation(this, DoResumeJobAsync, null, JobState.Suspended);
            }
            catch (Exception e)
            {
                // Catching all exceptions is valid here because
                // Transfering exception with event arguments.
                OnResumeJobCompleted(new AsyncCompletedEventArgs(e, false, null));
            }
#pragma warning restore 56500
        }

        private void DoResumeJobAsync(object state)
        {
            _tracer.WriteMessage(ClassNameTrace, "DoResumeJobAsync", WorkflowGuidForTraces, this, "");
            var label = state as string;
            var asyncOp = AsyncOperationManager.CreateOperation(null);

            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            workerDelegate.BeginInvoke(
                asyncOp,
                ActionType.Resume,
                string.Empty,
                label,
                false,
                null,
                null);
        }

        /// <summary>
        /// Resume a suspended job asynchronously.
        /// </summary>
        /// <param name="label"></param>
        public virtual void ResumeJobAsync(string label)
        {
#pragma warning disable 56500
            try
            {
                AssertNotDisposed();
                _runtime.JobManager.SubmitOperation(this, DoResumeJobAsync, label, JobState.Suspended);
            }
            catch (Exception e)
            {
                // Catching all exceptions is valid here because
                // Transfering exception with event arguments.
                OnResumeJobCompleted(new AsyncCompletedEventArgs(e, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Unblock a blocked job
        /// </summary>
        public override void UnblockJob()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Unblock a blocked job asynchronously
        /// </summary>
        public override void UnblockJobAsync()
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            lock (SyncRoot)
            {
                if (_isDisposed) return;
                _isDisposed = true;
            }

            if (disposing)
            {
                try
                {
                    // WorkflowInstance should be the first one to be disposed
                    //
                    if (_workflowInstance != null)
                        _workflowInstance.Dispose();

                    StateChanged -= HandleMyStateChanged;

                    if (_jobRunning != null)
                        _jobRunning.Close();

                    if (_jobSuspendedOrAborted != null)
                        _jobSuspendedOrAborted.Dispose();

                    _tracer.Dispose();
                }
                finally
                {
                    base.Dispose(true);
                }
            }
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PSWorkflowJob");
            }
        }

        /// <summary>
        /// Check and add StateChanged event handler
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="expectedState"></param>
        /// <returns></returns>
        internal bool CheckAndAddStateChangedEventHandler(EventHandler<JobStateEventArgs> handler, JobState expectedState)
        {
            lock(SyncRoot)
            {
                // Event Handler should be added 
                // if JobState is not changed after job submitted to the Workflow job manager queue, OR
                // if job is already running due to other labelled resume operation or other start job operation. 
                //
                if (JobStateInfo.State == expectedState || JobStateInfo.State == JobState.Running)
                {
                    Dbg.Assert(expectedState == JobState.NotStarted || expectedState == JobState.Suspended, "Only Start and Resume operations are serviced by PSWorkflowJobManager.StartOperationsFromQueue");
                    StateChanged += handler;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion IDisposable

        #region IJobDebugger

        /// <summary>
        /// Job Debugger
        /// </summary>
        public Debugger Debugger
        {
            get
            {
                return PSWorkflowDebugger;
            }
        }

        /// <summary>
        /// True if job is synchronous and can be debugged.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }
            set { _isAsync = value; }
        }

        #endregion
    }
}
