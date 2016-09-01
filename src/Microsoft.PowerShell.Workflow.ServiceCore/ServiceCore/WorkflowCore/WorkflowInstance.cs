/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Activities;
    using System.Management.Automation;
    using System.Management.Automation.Tracing;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;
    using System.Activities.Tracking;
    using System.IO;
    using System.Reflection;
    using Microsoft.PowerShell.Activities;
    using Microsoft.PowerShell.Commands;
    using System.Timers;
    using System.Collections.ObjectModel;
    using System.Activities.Hosting;
    using Dbg = System.Diagnostics.Debug;
    using System.Security.Principal;
    using System.Management.Automation.Language;
    using System.Runtime.Serialization;
    using System.Linq;

    /// <summary>
    /// Specifies the action that occurs when a workflow becomes idle when persistence is allowed.
    /// </summary>
    public enum PSPersistableIdleAction
    {
        /// <summary>
        /// No or null action is defined so fall back to default.
        /// </summary>
        NotDefined = 0,

        /// <summary>
        /// Specifies that no action is taken.
        /// </summary>
        None = 1,

        /// <summary>
        /// Specifies that the System.Activities.WorkflowApplication should persist the workflow.
        /// </summary>
        Persist = 2,

        /// <summary>
        /// Specifies that the System.Activities.WorkflowApplication should persist and unload the workflow.
        /// The job will remain in running state because async operation (out of proc or remote operation) is in progress.
        /// The System.Activities.WorkflowApplication will be loaded when async operation gets completed.
        /// </summary>
        Unload = 3,

        /// <summary>
        /// Specifies that the System.Activities.WorkflowApplication should persist and unload the workflow and Job is marked as suspended.
        /// </summary>
        Suspend = 4,

    }

    /// <summary>
    /// This class encapsulate the guid to avoid the confusion be job instance id and workflow instance id.
    /// </summary>
    public sealed class PSWorkflowId
    {
        private Guid _guid;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PSWorkflowId()
        {
            _guid = new Guid();
        }

        /// <summary>
        /// Constructor which takes guid.
        /// </summary>
        /// <param name="value"></param>
        public PSWorkflowId(Guid value)
        {
            _guid = value;
        }

        /// <summary>
        /// NewWorkflowGuid
        /// </summary>
        /// <returns></returns>
        public static PSWorkflowId NewWorkflowGuid()
        {
            return new PSWorkflowId(Guid.NewGuid());
        }

        /// <summary>
        /// Gets Guid
        /// </summary>
        public Guid Guid
        {
            get
            {
                return _guid;
            }
        }
    }

    /// <summary>
    /// WorkflowOnHandledErrorAction
    /// </summary>
    public enum WorkflowUnhandledErrorAction
    {
        /// <summary>
        /// Suspend
        /// </summary>
        Suspend = 0,

        /// <summary>
        /// Stop
        /// </summary>
        Stop = 1,

        /// <summary>
        /// Terminate
        /// </summary>
        Terminate = 2,
    };

    /// <summary>
    /// Possible workflow instance creation modes.
    /// </summary>
    internal enum WorkflowInstanceCreationMode
    {
        /// <summary>
        /// Workflow instance created normally.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Workflow instance created normally.
        /// </summary>
        AfterCrashOrShutdown = 1,
    };

    /// <summary>
    /// PSWorkflowRemoteActivityState
    /// </summary>
    public sealed class PSWorkflowRemoteActivityState
    {
        // the remote runspace instance ids collection
        private Dictionary<string, Dictionary<int, Tuple<object, string>>> _remoteRunspaceIdCollection;
        private readonly PSWorkflowInstanceStore _store;
        private bool _internalUnloaded = false;
        // sync object is required to synchronize the access to _remoteRunspaceIdCollection from multiple parallel tasks execution
        private object _syncObject = new object();
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// PSWorkflowRemoteActivityState constructor
        /// </summary>
        /// <param name="store"></param>
        internal PSWorkflowRemoteActivityState(PSWorkflowInstanceStore store)
        {
            _store = store;
            _remoteRunspaceIdCollection = new Dictionary<string, Dictionary<int, Tuple<object, string>>>();
        }

        /// <summary>
        /// Creates a PSWorkflowRemoteActivityState for a workflow instance based on deserialized object
        /// </summary>
        /// <param name="store"></param>
        /// <param name="deserializedRemoteActivityState"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the PowerShell Workflow extensibility.")]
        public PSWorkflowRemoteActivityState(PSWorkflowInstanceStore store, Dictionary<string, Dictionary<int, Tuple<object, string>>> deserializedRemoteActivityState)
        {
            _store = store;
            
            if (deserializedRemoteActivityState == null) 
                throw new ArgumentNullException("deserializedRemoteActivityState");

            _remoteRunspaceIdCollection = deserializedRemoteActivityState;
        }

        /// <summary>
        /// Retrieves RemoteActivityState object to be serialized
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the PowerShell Workflow extensibility.")]
        public Dictionary<string, Dictionary<int, Tuple<object, string>>> GetSerializedData()
        {
            lock (_syncObject)
            {
                // Get the copy of current _remoteRunspaceIdCollection for serialization
                return new Dictionary<string, Dictionary<int, Tuple<object, string>>>(_remoteRunspaceIdCollection);
            }
        }

        /// <summary>
        /// InternalUnloaded property
        /// </summary>
        internal bool InternalUnloaded
        {
            get
            {
                lock (_syncObject)
                {
                    return _internalUnloaded;
                }
            }
            set
            {
                lock (_syncObject)
                {
                    _internalUnloaded = value;
                }
            }
        }

        /// <summary>
        /// GetRemoteActivityRunspaceEntry
        /// </summary>
        /// <param name="activityId"></param>
        /// <param name="taskId"></param>
        /// <returns>task entry object</returns>
        internal object GetRemoteActivityRunspaceEntry(string activityId, int taskId)
        {
            object taskRunspaceId = null;

            lock (_syncObject)
            {
                Dictionary<int, Tuple<object, string>> runspaceIdCollection;
                if (_remoteRunspaceIdCollection.TryGetValue(activityId, out runspaceIdCollection))
                {
                    if (runspaceIdCollection != null)
                    {
                        Tuple<object, string> taskTuple;
                        runspaceIdCollection.TryGetValue(taskId, out taskTuple);

                        if (taskTuple != null)
                            taskRunspaceId = taskTuple.Item1;
                    }
                }
            }

            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "RemoteActivityState: runspace entry for taskid: {0}, taskEntry: {1}", taskId, taskRunspaceId));
            return taskRunspaceId;
        }

        /// <summary>
        /// SetRemoteActivityRunspaceEntry
        /// </summary>
        /// <param name="activityId"></param>
        /// <param name="taskId"></param>
        /// <param name="taskState">task state can be "NotStarted", Guid, or "Completed"</param>
        /// <param name="computerName">computer name of the task</param>
        internal void SetRemoteActivityRunspaceEntry(string activityId, int taskId, object taskState, string computerName)
        {
            lock (_syncObject)
            {
                Dictionary<int, Tuple<object, string>> activityTasksRunspaceIdCollection;
                _remoteRunspaceIdCollection.TryGetValue(activityId, out activityTasksRunspaceIdCollection);

                if (activityTasksRunspaceIdCollection == null)
                {
                    activityTasksRunspaceIdCollection = new Dictionary<int, Tuple<object, string>>();
                    _remoteRunspaceIdCollection.Add(activityId, activityTasksRunspaceIdCollection);
                }
                else
                {
                    // computerName will be passed as null during runspace instance id assignment and task completion
                    // So retrieving it from task entry.
                    if (computerName == null)
                    {
                        Tuple<object, string> taskTuple;
                        activityTasksRunspaceIdCollection.TryGetValue(taskId, out taskTuple);
                        if (taskTuple != null)
                        {
                            computerName = taskTuple.Item2;
                        }
                    }

                    // Remove the taskid entry if exists
                    activityTasksRunspaceIdCollection.Remove(taskId);
                }

                _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "RemoteActivityState.SetRemoteActivityRunspaceEntry: runspace entry for taskid: {0}, taskState: {1}, computerName: {2}", taskId, taskState, computerName));

                activityTasksRunspaceIdCollection.Add(taskId, new Tuple<object, string>(taskState, computerName));
            }

            if (InternalUnloaded)
            {
                _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "RemoteActivityState.SetRemoteActivityRunspaceEntry: persisting the Streams and RemoteActivityState"));
                
                // Streams and ActivityState needs to be persisted when workflow application is already unloaded internally
                // Streams will have the results from task completion
                _store.Save(WorkflowStoreComponents.ActivityState | WorkflowStoreComponents.Streams);
            }
        }

        /// <summary>
        /// RemoteActivityResumeRequired
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="duringResumeBookmark"></param>
        /// <returns></returns>
        internal bool RemoteActivityResumeRequired(PSActivity activity, bool duringResumeBookmark)
        {
            bool activityResumeRequired = false;
            if (activity is PSRemotingActivity)
            {
                lock (_syncObject)
                {
                    if (_remoteRunspaceIdCollection.ContainsKey(activity.Id))
                    {
                        // if all commands are completed remove the activity instance id's entry from  remoteRunspaceCollection
                        // otherwise restart the activity execution to reconnect to managed nodes.
                        Dictionary<int, Tuple<object, string>> commandRunspaceIdCollection;
                        _remoteRunspaceIdCollection.TryGetValue(activity.Id, out commandRunspaceIdCollection);
                        if (commandRunspaceIdCollection != null)
                        {
                            if (commandRunspaceIdCollection.Any(o => o.Value.Item1 is Guid))
                            {
                                activityResumeRequired = true;
                            }
                            else if (commandRunspaceIdCollection.Any(o => (o.Value.Item1 is string && o.Value.Item1.ToString().Equals("notstarted"))))
                            {
                                activityResumeRequired = true;
                            }
                        }

                        // This method is first time called from OnResumeBookmark, if no resume required it's entry
                        // needs to be removed fromt the collection as activity execution has finished.
                        // When an activity entry is removed, remoteActivityState will be persisted 
                        // as part of whole workflow application persistence at the end of activity completion
                        if (activityResumeRequired == false)
                            _remoteRunspaceIdCollection.Remove(activity.Id);
                    }
                    else if (duringResumeBookmark)
                    {
                        // Activity needs to be restarted/resumed if the process got crashed/terminated just after activity is bookmarked and 
                        // in this case there will be no entry for this activity id in _remoteRunspaceIdCollection
                        activityResumeRequired = true;
                    }
                }
            }

            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "RemoteActivityState.RemoteActivityResumeRequired returning activityResumeRequired: {0}", activityResumeRequired));
            return activityResumeRequired;
        }
    }

    /// <summary>
    /// Collects all the information related to a workflow instance.
    /// </summary>
    internal class PSWorkflowApplicationInstance : PSWorkflowInstance
    {
        #region Private Members

        /// <summary>
        /// Tracer initialization.
        /// </summary>
        private readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        
        /// <summary>
        /// Workflow Application executes the workflow and it is part of CLR 4.0.
        /// </summary>
        private WorkflowApplication workflowApplication;

        /// <summary>
        /// The Guid, which represents the unique instance of a workflow.
        /// </summary>
        private Guid id;        

        /// <summary>
        /// Workflow definition.
        /// </summary>
        private PSWorkflowDefinition _definition;

        /// <summary>
        /// Workflow output, which represent all streams from the workflow.
        /// </summary>
        private PowerShellStreams<PSObject, PSObject> _streams;

        /// <summary>
        /// Workflow error exception, which represent the terminating error workflow.
        /// </summary>
        private Exception _errorException;

        /// <summary>
        /// the remote runspace instance ids collection
        /// </summary>
        private PSWorkflowRemoteActivityState _remoteActivityState;

        /// <summary>
        /// Workflow metadatas.
        /// </summary>
        private PSWorkflowContext _metadatas;

        /// <summary>
        /// Workflow debugger.
        /// </summary>
        private PSWorkflowDebugger _debugger;

        /// <summary>
        /// Workflow timers.
        /// </summary>
        private PSWorkflowTimer _timers;

        /// <summary>
        /// The workflow creation mode.
        /// </summary>
        private WorkflowInstanceCreationMode creationMode;

        private Dictionary<string, PSActivityContext> asyncExecutionCollection;

        private bool PersistAfterNextPSActivity;
        private bool suspendAtNextCheckpoint = false;


        private readonly PSWorkflowInstanceStore _stores;
        private static readonly Tracer _structuredTracer = new Tracer();
        private PSWorkflowJob _job;

        private bool errorExceptionLoadCalled = false;

        private bool _suppressTerminateError;

        private void HandleWorkflowApplicationCompleted(WorkflowApplicationCompletedEventArgs e)
        {
            if (Disposed)
                return;

            _structuredTracer.Correlate();

            if (e.CompletionState == ActivityInstanceState.Closed)
            {
                if (System.Threading.Interlocked.CompareExchange(ref terminalStateHandled, Handled, NotHandled) == Handled)
                    return;

                try
                {
                    Tracer.WriteMessage("Workflow Application is completed and is in closed state.");

                    Tracer.WriteMessage("Flatting out the PSDataCollection returned outputs.");

                    foreach (KeyValuePair<string, object> outvariable in e.Outputs)
                    {
                        if (outvariable.Value != null)
                        {
                            if (outvariable.Value is PSDataCollection<PSObject>)
                            {
                                PSDataCollection<PSObject> outCollection = outvariable.Value as PSDataCollection<PSObject>;

                                Dbg.Assert(outCollection != null, "outCollection should not be NULL");

                                foreach (PSObject obj in outCollection)
                                {
                                    Streams.OutputStream.Add(obj);
                                }
                                outCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<ErrorRecord>)
                            {
                                PSDataCollection<ErrorRecord> errorCollection = outvariable.Value as PSDataCollection<ErrorRecord>;

                                Dbg.Assert(errorCollection != null, "errorCollection should not be NULL");

                                foreach (ErrorRecord obj in errorCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                errorCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<WarningRecord>)
                            {
                                PSDataCollection<WarningRecord> warningCollection = outvariable.Value as PSDataCollection<WarningRecord>;

                                Dbg.Assert(warningCollection != null, "warningCollection should not be NULL");

                                foreach (WarningRecord obj in warningCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                warningCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<ProgressRecord>)
                            {
                                PSDataCollection<ProgressRecord> progressCollection = outvariable.Value as PSDataCollection<ProgressRecord>;

                                Dbg.Assert(progressCollection != null, "progressCollection should not be NULL");

                                foreach (ProgressRecord obj in progressCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                progressCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<VerboseRecord>)
                            {
                                PSDataCollection<VerboseRecord> verboseCollection = outvariable.Value as PSDataCollection<VerboseRecord>;

                                Dbg.Assert(verboseCollection != null, "verboseCollection should not be NULL");

                                foreach (VerboseRecord obj in verboseCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                verboseCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<DebugRecord>)
                            {
                                PSDataCollection<DebugRecord> debugCollection = outvariable.Value as PSDataCollection<DebugRecord>;

                                Dbg.Assert(debugCollection != null, "debugCollection should not be NULL");

                                foreach (DebugRecord obj in debugCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                debugCollection.Clear();
                            }
                            else if (outvariable.Value is PSDataCollection<InformationRecord>)
                            {
                                PSDataCollection<InformationRecord> informationCollection = outvariable.Value as PSDataCollection<InformationRecord>;

                                Dbg.Assert(informationCollection != null, "informationCollection should not be NULL");

                                foreach (InformationRecord obj in informationCollection)
                                {
                                    Streams.OutputStream.Add(PSObject.AsPSObject(obj));
                                }
                                informationCollection.Clear();
                            }
                            else
                            {
                                // pass the object as is to the output stream
                                this.Streams.OutputStream.Add(PSObject.AsPSObject(outvariable.Value));
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Dbg.Assert(false, "An exception is raised after a workflow completed successfully. This is most likely a case of exception eating, check the exception details");
                    Tracer.TraceException(exception);
                }

                State = JobState.Completed;
                this.PerformTaskAtTerminalState();

                // do all cleanups here to save memory
                PerformCleanupAtTerminalState();

                if (this.OnCompleted != null)
                    this.OnCompleted(this);
            }

            if (e.CompletionState == ActivityInstanceState.Faulted)
            {
                HandleWorkflowApplicationFaultedState(e.TerminationException);
            }

            if (e.CompletionState == ActivityInstanceState.Canceled)
            {
                this.HandleWorkflowApplicationCanceled();
            }
        }

        private void HandleWorkflowApplicationCanceled()
        {
            if (System.Threading.Interlocked.CompareExchange(ref terminalStateHandled, Handled, NotHandled) == Handled)
                return;

            Tracer.WriteMessage("Workflow Application is completed in Canceled state.");

            State = JobState.Stopped;
            this.PerformTaskAtTerminalState();

            // do all cleanups here to save memory
            PerformCleanupAtTerminalState();

            if (this.OnStopped != null)
                this.OnStopped(this);
        }

        private ErrorRecord GetInnerErrorRecord(Exception exception)
        {
            IContainsErrorRecord nestedException = exception as IContainsErrorRecord;
            if (nestedException == null) return null;
            return nestedException.ErrorRecord;
        }

        private void HandleWorkflowApplicationAborted(WorkflowApplicationAbortedEventArgs e)
        {
            if (Disposed)
                return;

            _structuredTracer.Correlate();
            Tracer.WriteMessage("Workflow Application is completed in Aborted state.");

            // if the suspend in progress and there is some error it result in the aborted event
            // explicit faulting the workflow.
            if (this.callSuspendDelegate)
            {
                this.callSuspendDelegate = false;
                HandleWorkflowApplicationFaultedState(e.Reason);
                return;
            }


            // if there is an exception in the canceling the workflow the WF application throws abort event instead on canceled event.
            if (_job.JobStateInfo.State == JobState.Stopping)
            {
                HandleWorkflowApplicationCanceled();
                return;
            }

            this.Error = e.Reason;

            // do all cleanups here to save memory
            PerformCleanupAtTerminalState();

            if (this.OnAborted != null)
                this.OnAborted(e.Reason, this);
        }

        private UnhandledExceptionAction HandleWorkflowApplicationUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs e)
        {
            if (Disposed)
                return UnhandledExceptionAction.Terminate;

            _structuredTracer.Correlate();
            Tracer.WriteMessage("Workflow Application is completed in Unhandled exception state.");

            if (PSWorkflowContext.PSWorkflowCommonParameters.ContainsKey(Constants.PSWorkflowErrorAction) && PSWorkflowContext.PSWorkflowCommonParameters[Constants.PSWorkflowErrorAction] != null)
            {
                WorkflowUnhandledErrorAction action = (WorkflowUnhandledErrorAction)PSWorkflowContext.PSWorkflowCommonParameters[Constants.PSWorkflowErrorAction];

                switch (action)
                {
                    case WorkflowUnhandledErrorAction.Stop:
                        return UnhandledExceptionAction.Cancel;

                    case WorkflowUnhandledErrorAction.Suspend:
                        return UnhandledExceptionAction.Abort;

                    case WorkflowUnhandledErrorAction.Terminate:
                        return UnhandledExceptionAction.Terminate;
                }
            }

            return UnhandledExceptionAction.Terminate;
        }

        private void HandlePersistence(object state)
        {
            if (Disposed)
                return;

            // this lock will ensure the synchronization between the persistence and the force suspend (abort) method.
            // if persistence is going on then it will hold the execution of force suspend
            lock (ReactivateSync)
            {
                if (Disposed)
                    return;

                // if force suspend or abort has already been called IsTerminalStateAction will be true and then
                // there is no need to do the persistence.
                if (IsTerminalStateAction)
                    return;


                if (_job.JobStateInfo.State == JobState.Running && this.workflowApplication != null)
                {
                    try
                    {
                        Tracer.WriteMessage("PSWorkflowApplicationInstance", "HandlePersistence", id, "Persisting the workflow.");
                        this.workflowApplication.Persist();

                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                        SafelyHandleFaultedState(e);
                        return;
                    }

                    try
                    {
                        foreach (BookmarkInfo b in this.workflowApplication.GetBookmarks())
                        {
                            if (b.BookmarkName.Contains(PSActivity.PSPersistBookmarkPrefix))
                                this.workflowApplication.ResumeBookmark(b.BookmarkName, null);
                        }
                    }
                    catch (Exception e)
                    {
                        // this may occur in the race condition if there are many persistable workflow and then you tried to shutdown them.
                        Tracer.WriteMessage("PSWorkflowApplicationInstance", "HandlePersistence", id, "There has been exception while persisting the workflow in the background thread.");
                        Tracer.TraceException(e);
                    }
                }
            }
        }

        private void SafelyHandleFaultedState(Exception exception)
        {
            try
            {
                HandleWorkflowApplicationFaultedState(exception);
            }
            catch (Exception e)
            {
                // this may occur in the race condition if there is a persistable workflow which is getting stopped and at the same persist is called.
                Tracer.WriteMessage("PSWorkflowApplicationInstance", "SafelyHandleFaultedState", id, "There has been exception while marking the workflow in faulted state in the background thread.");
                Tracer.TraceException(e);
            }
        }

        private bool callSuspendDelegate = false;

        private PersistableIdleAction HandleWorkflowApplicationPersistableIdle(WorkflowApplicationIdleEventArgs e)
        {
            if (Disposed)
                return PersistableIdleAction.None;

            _structuredTracer.Correlate();

            PSPersistableIdleAction action = PSPersistableIdleAction.NotDefined;

            if (this.OnPersistableIdleAction != null)
                action = this.OnPersistableIdleAction(e.Bookmarks, this.suspendAtNextCheckpoint, this);
            
            if(action == PSPersistableIdleAction.NotDefined) // fall back default handler
                action = _job.GetPersistableIdleAction(e.Bookmarks, this.suspendAtNextCheckpoint);

            switch (action)
            {
                case PSPersistableIdleAction.None:
                    return PersistableIdleAction.None;

                case PSPersistableIdleAction.Persist:
                    System.Threading.ThreadPool.QueueUserWorkItem(this.HandlePersistence);
                    return PersistableIdleAction.None;

                case PSPersistableIdleAction.Unload:
                    if (Runtime.Configuration.PSWorkflowApplicationPersistUnloadTimeoutSec <= 0)
                    {
                        this.StartPersistUnloadWithZeroSeconds();
                        return PersistableIdleAction.Unload;
                    }
                    else
                    {
                        this.StartPersistUnloadTimer(Runtime.Configuration.PSWorkflowApplicationPersistUnloadTimeoutSec);
                        return PersistableIdleAction.None;
                    }

                case PSPersistableIdleAction.Suspend:
                    this.callSuspendDelegate = true;
                    return PersistableIdleAction.Unload;
            }

            return PersistableIdleAction.None;
        }

        private void HandleWorkflowApplicationIdle(WorkflowApplicationIdleEventArgs e)
        {
            if (Disposed)
                return;

            _structuredTracer.Correlate();
            Tracer.WriteMessage("Workflow Application is idle.");

            // there might be a possibility that stop job is being called by wfApp is Idle handling it properly so all Async execution .
            if (_job.JobStateInfo.State == JobState.Stopping)
            {
                this.StopBookMarkedWorkflow();
                return;
            }

            if (this.OnIdle != null)
                this.OnIdle(e.Bookmarks, this);
        }

        private void HandleWorkflowApplicationUnloaded(WorkflowApplicationEventArgs e)
        {
            if (Disposed)
                return;

            _structuredTracer.Correlate();
            Tracer.WriteMessage("Workflow Application is unloaded.");

            if (this.callSuspendDelegate)
            {
                this.callSuspendDelegate = false;
                // suspend logic
                if (this.OnSuspended != null)
                {
                    this.OnSuspended(this);
                }
            }

            if (this.OnUnloaded != null)
                this.OnUnloaded(this);
        }

        private void SubscribeWorkflowApplicationEvents()
        {
            this.workflowApplication.Completed += HandleWorkflowApplicationCompleted;
            this.workflowApplication.Aborted += HandleWorkflowApplicationAborted;
            this.workflowApplication.OnUnhandledException += HandleWorkflowApplicationUnhandledException;
            this.workflowApplication.PersistableIdle += HandleWorkflowApplicationPersistableIdle;
            this.workflowApplication.Idle += HandleWorkflowApplicationIdle;
            this.workflowApplication.Unloaded += HandleWorkflowApplicationUnloaded;
        }

        private void DisposeWorkflowApplication()
        {
            if (this.workflowApplication != null)
            {
                this.workflowApplication.Completed -= HandleWorkflowApplicationCompleted;
                this.workflowApplication.Aborted -= HandleWorkflowApplicationAborted;
                this.workflowApplication.OnUnhandledException -= HandleWorkflowApplicationUnhandledException;
                this.workflowApplication.PersistableIdle -= HandleWorkflowApplicationPersistableIdle;
                this.workflowApplication.Idle -= HandleWorkflowApplicationIdle;
                this.workflowApplication.Unloaded -= HandleWorkflowApplicationUnloaded;
                this.workflowApplication = null;
            }
        }

        private void OnSuspendUnloadComplete(IAsyncResult asyncResult)
        {
            if (Disposed) return;

            try
            {
                WorkflowApplication wfa = this.workflowApplication;
                if (wfa != null)
                {
                    wfa.EndUnload(asyncResult);
                }
            }
            catch (Exception e)
            {
                Tracer.WriteMessage("PSWorkflowInstance", "DoSuspendInstance", id, "Not able to unload workflow application in a given timeout.");
                Tracer.TraceException(e);

                HandleWorkflowApplicationFaultedState(e); 
                
                return;
            }

            this.ConfigureTimerOnUnload();
            this.DisposeWorkflowApplication();

            if (this.OnSuspended != null)
                this.OnSuspended(this);
        }

        private int terminalStateHandled = NotHandled;
        private const int NotHandled = 0;
        private const int Handled = 1;
        private void HandleWorkflowApplicationFaultedState(Exception e)
        {
            if (System.Threading.Interlocked.CompareExchange(ref terminalStateHandled, Handled, NotHandled) == Handled)
                return;

            Tracer.WriteMessage("Workflow Application is completed in Faulted state.");

            // there might be possible of race condition here in case of Winrm shutdown. if the activity is 
            // executing on loop back winrm process so there might be possibility that the winrm shuts down the 
            // activity process fist and causing the workflow to fail in the M3P process.
            // in order to avoid those situations we will ignore the remote exception if the shutdown is in progress
            if (WorkflowJobSourceAdapter.GetInstance().IsShutdownInProgress)
            {
                if (e.GetType() == typeof(RemoteException))
                {
                    Tracer.WriteMessage("PSWorkflowApplicationInstance", "HandleWorkflowApplicationFaultedState", id, "Since we are in shuting down mode so ignoring the remote exception");
                    Tracer.TraceException(e);

                    // no need to proceed any further
                    return;
                }
            }

            // before we start handling the faulted state we need to cancel
            // all running activities. Activities may be running if there is a 
            // parallel statement involved            
            StopAllAsyncExecutions();

            State = JobState.Failed;
            Exception reason = null;

            if (!_suppressTerminateError)
            {
                reason = e;

                ErrorRecord failureErrorRecord = GetInnerErrorRecord(reason);
                if (failureErrorRecord != null)
                {
                    reason = failureErrorRecord.Exception;

                    if (PSWorkflowJob.SynchronousExecution && Streams.ErrorStream.IsOpen)
                    {
                        // If we're running synchronously from the commandline, we want to see the error
                        // record from the workflow without the Receive-Job decoration. Receive-Job will
                        // decorate the reason, so do not send it as the reason.
                        // Note that if the error collection is closed, we do not want to lose this data, so
                        // pass the exception despite the bad decoration.
                        Streams.ErrorStream.Add(failureErrorRecord);
                        reason = null;
                    }
                }
                else
                {
                    // No error record, a raw exception was thrown. See if we have location
                    // information that we can wrap it in.
                    HostSettingCommandMetadata commandMetadata = null;

                    if (_paramDefaults != null)
                        commandMetadata = _paramDefaults.HostCommandMetadata;

                    if (commandMetadata != null)
                    {
                        ScriptPosition scriptStart = new ScriptPosition(
                            commandMetadata.CommandName,
                            commandMetadata.StartLineNumber,
                            commandMetadata.StartColumnNumber,
                            null);
                        ScriptPosition scriptEnd = new ScriptPosition(
                            commandMetadata.CommandName,
                            commandMetadata.EndLineNumber,
                            commandMetadata.EndColumnNumber,
                            null);
                        ScriptExtent extent = new ScriptExtent(scriptStart, scriptEnd);

                        reason = new JobFailedException(reason, extent);
                    }
                }
            }

            this.Error = reason;

            this.PerformTaskAtTerminalState();

            // do all cleanups here to save memory
            PerformCleanupAtTerminalState();

            if (this.OnFaulted != null)
                this.OnFaulted(reason, this);
        }

        private void CheckDisposed()
        {
            if (Disposed)
            {
                Debug.Assert(Disposed, "PSWorkflowInstance has been disposed " + Id);
            }
        }

        private void PerformCleanupAtTerminalState()
        {
            DisposeWorkflowApplication();
            ConfigureTimerOnUnload();
            if (_streams != null)
                UnregisterHandlersForDataAdding(_streams);
        }

        #endregion Private Members

        #region Internal Members

        /// <summary>
        /// Workflow instance constructor.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="definition">The workflow definition.</param>
        /// <param name="metadata">The metadata which includes parameters etc.</param>
        /// <param name="pipelineInput">This is input coming from pipeline, which is added to the input stream.</param>
        /// <param name="job"></param>
        internal PSWorkflowApplicationInstance(
                                        PSWorkflowRuntime runtime, 
                                        PSWorkflowDefinition definition,
                                        PSWorkflowContext metadata,
                                        PSDataCollection<PSObject> pipelineInput,                                        
                                        PSWorkflowJob job)
        {
            Tracer.WriteMessage("Creating Workflow instance.");
            InitializePSWorkflowApplicationInstance(runtime);
            this._definition = definition;
            this._metadatas = metadata;
            this._streams = new PowerShellStreams<PSObject, PSObject>(pipelineInput);
            RegisterHandlersForDataAdding(_streams);
            this._timers = new PSWorkflowTimer(this);           
            this.creationMode = WorkflowInstanceCreationMode.Normal;

            _job = job;
            _stores = Runtime.Configuration.CreatePSWorkflowInstanceStore(this);

            this._remoteActivityState = new PSWorkflowRemoteActivityState(_stores);
        }

        private void InitializePSWorkflowApplicationInstance(PSWorkflowRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException("runtime");

            this.PersistAfterNextPSActivity = false;
            this.suspendAtNextCheckpoint = false;
            Runtime = runtime;

            this.asyncExecutionCollection = new Dictionary<string, PSActivityContext>();
            this.ForceDisableStartOrEndPersistence = false;

            if (Runtime.Configuration.PSWorkflowApplicationPersistUnloadTimeoutSec > 0)
            {
                PersistUnloadTimer = new Timer(Convert.ToDouble(Runtime.Configuration.PSWorkflowApplicationPersistUnloadTimeoutSec * 1000));
                PersistUnloadTimer.Elapsed += new ElapsedEventHandler(PersistUnloadTimer_Elapsed);
                PersistUnloadTimer.AutoReset = false;
            }

            this._debugger = new PSWorkflowDebugger(this);
        }

        private void RegisterHandlersForDataAdding(PowerShellStreams<PSObject,PSObject> streams)
        {
            streams.OutputStream.DataAdding += HandleOutputDataAdding;
            streams.ErrorStream.DataAdding += HandleErrorDataAdding;
            streams.DebugStream.DataAdding += HandleInformationalDataAdding;
            streams.VerboseStream.DataAdding += HandleInformationalDataAdding;
            streams.WarningStream.DataAdding += HandleInformationalDataAdding;
            streams.ProgressStream.DataAdding += HandleProgressDataAdding;
            streams.InformationStream.DataAdding += HandleInformationDataAdding;
        }

        private void UnregisterHandlersForDataAdding(PowerShellStreams<PSObject, PSObject> streams)
        {
            if (streams.OutputStream != null)
                streams.OutputStream.DataAdding -= HandleOutputDataAdding;
            if (streams.ErrorStream != null)
                streams.ErrorStream.DataAdding -= HandleErrorDataAdding;
            if (streams.DebugStream != null)
                streams.DebugStream.DataAdding -= HandleInformationalDataAdding;
            if (streams.VerboseStream != null)
                streams.VerboseStream.DataAdding -= HandleInformationalDataAdding;
            if (streams.WarningStream != null)
                streams.WarningStream.DataAdding -= HandleInformationalDataAdding;
            if (streams.ProgressStream != null)
                streams.ProgressStream.DataAdding -= HandleProgressDataAdding;
            if (streams.InformationStream != null)
                streams.InformationStream.DataAdding -= HandleInformationDataAdding;
        }

        private const string LocalHost = "localhost";
        private void HandleOutputDataAdding(object sender, DataAddingEventArgs e)
        {
            PSObject psObject = (PSObject) e.ItemAdded;

            if (psObject == null) return;

            PSActivity.AddIdentifierInfoToOutput(psObject, JobInstanceId, LocalHost);
        }

        private void HandleErrorDataAdding(object sender, DataAddingEventArgs e)
        {
            ErrorRecord errorRecord = (ErrorRecord)e.ItemAdded;
            if (errorRecord == null) return;

            PSActivity.AddIdentifierInfoToErrorRecord(errorRecord, LocalHost, JobInstanceId);
        }

        private void HandleProgressDataAdding(object sender, DataAddingEventArgs e)
        {
            ProgressRecord progressRecord = (ProgressRecord)e.ItemAdded;
            if (progressRecord == null) return;

            progressRecord.CurrentOperation = PSActivity.AddIdentifierInfoToString(JobInstanceId, LocalHost,
                                                                                   progressRecord.CurrentOperation);
        }

        private void HandleInformationDataAdding(object sender, DataAddingEventArgs e)
        {
            InformationRecord informationRecord = (InformationRecord)e.ItemAdded;
            if (informationRecord == null) return;

            informationRecord.Source = PSActivity.AddIdentifierInfoToString(JobInstanceId, LocalHost,
                                                                                   informationRecord.Source);
        }

        private void HandleInformationalDataAdding(object sender, DataAddingEventArgs e)
        {
            InformationalRecord informationalRecord = (InformationalRecord)e.ItemAdded;
            if (informationalRecord == null) return;

            informationalRecord.Message = PSActivity.AddIdentifierInfoToString(JobInstanceId, LocalHost,
                                                                               informationalRecord.Message);
        }

        private Guid JobInstanceId
        {
            get
            {
                return _job != null ? _job.InstanceId : Guid.Empty;
            }
        }

        /// <summary>
        /// Workflow instance constructor for shutdown or crashed workflows.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="instanceId"></param>
        internal PSWorkflowApplicationInstance(PSWorkflowRuntime runtime, PSWorkflowId instanceId)
        {
            Tracer.WriteMessage("Creating Workflow instance after crash and shutdown workflow.");
            InitializePSWorkflowApplicationInstance(runtime);
            this._definition = null;
            this._metadatas = null;
            this._streams = null;
            this._timers = null;
            this.id = instanceId.Guid;
            this.creationMode = WorkflowInstanceCreationMode.AfterCrashOrShutdown;

            _stores = Runtime.Configuration.CreatePSWorkflowInstanceStore(this);
            this._remoteActivityState = null;            
        }

        /// <summary>
        /// Gets the Guid of workflow instance.
        /// </summary>
        internal override Guid Id
        {
            get { return this.id; }
        }


        /// <summary>
        /// Load instance for resuming the workflow
        /// </summary>
        internal override void DoLoadInstanceForReactivation()
        {
            CheckDisposed();

            try
            {
                lock (SyncLock)
                {
                    Tracer.WriteMessage("Loading for Workflow resumption.");

                    if (this.PSWorkflowDefinition.Workflow == null)
                    {
                        ArgumentException exception = new ArgumentException(Resources.NoWorkflowProvided);
                        Tracer.TraceException(exception);
                        throw exception;
                    }

                    this.workflowApplication = new WorkflowApplication(this.PSWorkflowDefinition.Workflow);
                    this.wfAppNeverLoaded = false;
                    SubscribeWorkflowApplicationEvents();
                    this.ConfigureAllExtensions();

                    // loading the workflow context from the store
                    this.workflowApplication.Load(this.id);

                    SetInternalUnloaded(false);

                    Tracer.WriteMessage("Workflow is loaded for reactivation, Guid = " + this.id.ToString("D", CultureInfo.CurrentCulture));
                }
            }
            catch(Exception e)
            {
                Tracer.WriteMessage("PSWorkflowApplicationInstance", "DoLoadInstanceForReactivation", id, "There has been an exception while loading the workflow state from persistence store.");
                Tracer.TraceException(e);
                var invalidOpException = new InvalidOperationException(Resources.WorkflowInstanceIncompletelyPersisted, e);
                HandleWorkflowApplicationFaultedState(invalidOpException);
                throw invalidOpException;
            }
        }

        /// <summary>
        /// PerformTaskAtTerminalState
        /// </summary>
        internal override void PerformTaskAtTerminalState()
        {

            // Cleanup
            if (this.PSWorkflowDefinition != null && this.PSWorkflowDefinition.Workflow != null)
            {
                this.PSWorkflowDefinition.Workflow = null;
            }


            if (this.CheckForStartOrEndPersistence())
            {
                try
                {
                    //Serializing Data to disk
                    _stores.Save(WorkflowStoreComponents.Metadata
                                | WorkflowStoreComponents.Streams
                                | WorkflowStoreComponents.TerminatingError
                                | WorkflowStoreComponents.Timer
                                | WorkflowStoreComponents.JobState
                                | WorkflowStoreComponents.ActivityState
                    );
                }
                catch (Exception e)
                {
                    Tracer.WriteMessage("Serialization exception occurred while saving workflow to persistence store");
                    Tracer.TraceException(e);
                    // Trace details
                    if (Streams.ErrorStream != null && Streams.ErrorStream.IsOpen)
                    {
                        Streams.ErrorStream.Add(new ErrorRecord(e, "Workflow_Serialization_Error", ErrorCategory.ParserError, null));
                    }
                    else
                    {
                        Tracer.WriteMessage("Error stream is not in Open state");
                    }
                }
            }

            Streams.CloseAll();

            // Stopping the timers
            if (Timer != null)
            {
                Timer.StopTimer(WorkflowTimerType.RunningTimer);
            }

            _structuredTracer.EndWorkflowExecution(_job.InstanceId);
        }
        
        /// <summary>
        /// Save streams if they are not already persisted
        /// </summary>
        /// <returns>false, if persistence was attempted and there
        /// was an error
        /// true - otherwise</returns>
        internal override bool SaveStreamsIfNecessary()
        {
            // if there was start and end persistence, there is 
            // no need to persist again, simply return
            if (CheckForStartOrEndPersistence())
                return true;

            try
            {
                _stores.Save(WorkflowStoreComponents.Streams);
            }
            catch (Exception e)
            {
                // it is fine to eat an exception here
                // the user explicitly disabled persist and
                // we are doing a best effort here
                // any failure means we will simply return
                // the caller will handle this scenario
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }

        #endregion Internal Members

        #region Public Members

        /// <summary>
        /// PSWorkflowJob
        /// </summary>
        public override PSWorkflowJob PSWorkflowJob
        {
            get { return _job; }

            protected internal set
            {
                _job = value;
            }
        }

        /// <summary>
        /// Gets the Guid of workflow instance.
        /// </summary>
        public override PSWorkflowId InstanceId
        {
            get { return new PSWorkflowId(this.id); }
        }

        /// <summary>
        /// Gets the Creation Context of workflow job.
        /// </summary>
        public override Dictionary<string, object> CreationContext
        {
            get 
            {
                Dictionary<string, object> creationContext = null;

                object jobCreationContext = null;                
                if (this.PSWorkflowContext.JobMetadata.TryGetValue(Constants.WorkflowJobCreationContext, out jobCreationContext))
                {
                    creationContext = jobCreationContext as Dictionary<string, object>;
                }

                return creationContext;
            }
        }

        /// <summary>
        /// InstanceStore
        /// </summary>
        public override PSWorkflowInstanceStore InstanceStore
        {
            get
            {
                return _stores;
            }
        }

        /// <summary>
        /// Gets the definition of workflow.
        /// </summary>
        public override PSWorkflowDefinition PSWorkflowDefinition
        {
            get
            {
                if (this._definition == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                {
                    lock (SyncLock)
                    {
                        if (this._definition == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                        {
                            try
                            {
                                _stores.Load(WorkflowStoreComponents.Definition);
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the workflow definition");

                                Tracer.TraceException(e);

                                this._definition = new PSWorkflowDefinition(null, string.Empty, string.Empty);


                            }
                        }
                    }
                }

                return this._definition;
            }

            set
            {
                CheckDisposed();
                this._definition = value;
            }
        }

        /// <summary>
        /// Gets the streams of workflow.
        /// </summary>
        public override PowerShellStreams<PSObject, PSObject> Streams
        {
            get
            {
                if (this._streams == null && 
                    (_streamsDisposed || this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown))
                {
                    lock (SyncLock)
                    {
                        if (this._streams == null &&
                            (_streamsDisposed || this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown))
                        {
                            try
                            {
                                _stores.Load(WorkflowStoreComponents.Streams);
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the workflow streams");

                                Tracer.TraceException(e);

                                this._streams = new PowerShellStreams<PSObject, PSObject>(null);
                                Tracer.WriteMessage("Marking the job to the faulted state.");
                            }
                            RegisterHandlersForDataAdding(_streams);
                        }
                    }
                }
                return this._streams;
            }

            set
            {
                CheckDisposed();
                if (_streams == value)
                    return;

                if (_streams != null)
                {
                    UnregisterHandlersForDataAdding(_streams);
                    _streams.Dispose();
                }
                this._streams = value;
                RegisterHandlersForDataAdding(_streams);
            }
        }

        /// <summary>
        /// Gets the remote runspace instance ids collection.
        /// </summary>
        public override PSWorkflowRemoteActivityState RemoteActivityState
        {
            get
            {
                if (this._remoteActivityState == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                {
                    lock (SyncLock)
                    {
                        if (this._remoteActivityState == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                        {
                            try
                            {
                                _stores.Load(WorkflowStoreComponents.ActivityState);
                                
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the RemoteActivityState");

                                Tracer.TraceException(e);

                                this.RemoteActivityState = new PSWorkflowRemoteActivityState(_stores);

                                Tracer.WriteMessage("Marking the job to the faulted state.");
                            }
                        }
                    }
                }

                return this._remoteActivityState;
            }

            set
            {
                CheckDisposed();
                this._remoteActivityState = value;
            }
        }

        /// <summary>
        /// Gets the streams of workflow.
        /// </summary>
        public override Exception Error
        {
            get
            {
                if (this._errorException == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown && errorExceptionLoadCalled == false)
                {
                    lock (SyncLock)
                    {
                        if (this._errorException == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown && errorExceptionLoadCalled == false)
                        {
                            try
                            {
                                errorExceptionLoadCalled = true;
                                _stores.Load(WorkflowStoreComponents.TerminatingError);
                                
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the workflow terminating error");

                                Tracer.TraceException(e);

                                this._errorException = null;

                                Tracer.WriteMessage("Marking the job to the faulted state.");
                            }
                        }
                    }
                }

                return this._errorException;
            }

            set
            {
                CheckDisposed();
                this._errorException = value;
            }
        }

        /// <summary>
        /// Gets the timers of workflow.
        /// </summary>
        public override PSWorkflowTimer Timer
        {
            get
            {
                if (this._timers == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                {
                    lock (SyncLock)
                    {
                        if (this._timers == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                        {
                            try
                            {
                                _stores.Load(WorkflowStoreComponents.Timer);
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the workflow timer");

                                Tracer.TraceException(e);

                                this._timers = null;

                                Tracer.WriteMessage("Marking the job to the faulted state.");
                            }
                        }
                    }
                }

                return this._timers;
            }

            set
            {
                CheckDisposed();
                this._timers = value;
            }
        }

        /// <summary>
        /// Gets the metadatas of workflow.
        /// </summary>
        public override PSWorkflowContext PSWorkflowContext
        {
            get 
            {
                if (this._metadatas == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                {
                    lock (SyncLock)
                    {
                        if (this._metadatas == null && this.creationMode == WorkflowInstanceCreationMode.AfterCrashOrShutdown)
                        {
                            try
                            {
                                _stores.Load(WorkflowStoreComponents.Metadata);
                            }
                            catch (Exception e)
                            {
                                Tracer.WriteMessage("Exception occurred while loading the workflow metadata");

                                Tracer.TraceException(e);

                                this._metadatas = new PSWorkflowContext();

                            }
                        }
                    }
                }
 
                return this._metadatas; 
            }

            set
            {
                CheckDisposed();
                this._metadatas = value;
            }

        }

        /// <summary>
        /// PSWorkflowDebugger
        /// </summary>
        internal override PSWorkflowDebugger PSWorkflowDebugger
        {
            get { return this._debugger; }
        }

        /// <summary>
        /// Dispose the streams to save memory
        /// </summary>
        public override void DisposeStreams()
        {
            if (_streams == null) return;
            lock (SyncLock)
            {
                if (_streams == null) return;
                _streamsDisposed = true;
                UnregisterHandlersForDataAdding(_streams);
                _streams.Dispose();
                _streams = null;
            }
        }

        private bool _streamsDisposed;

        #endregion Public Members

        #region Protected override members

        /// <summary>
        /// DoStopInstance
        /// </summary>
        protected override void DoStopInstance()
        {
            // This lock required to wait for any inprogress workflow reactivation
            lock (ReactivateSync)
            {
                // this flag ensures that the workflow application will not be loaded again since we are stopping the workflow instance
                IsTerminalStateAction = true;
                // this flag ensures that we don't unload the wfApplication instance since we are stopping the workflow.
                PersistIdleTimerInProgressOrTriggered = false;
            }

            if (this.workflowApplication != null)
            {
                this.workflowApplication.Cancel();
                return;
            }
            else
            {
                this.StopBookMarkedWorkflow();
            }
        }

        /// <summary>
        /// DoAbortInstance
        /// </summary>
        /// <param name="reason">Reason for aborting workflow.</param>
        protected override void DoAbortInstance(string reason)
        {
            if (Disposed)
                return;

            this.workflowApplication.Abort(reason);
        }

        /// <summary>
        /// DoTerminateInstance
        /// </summary>
        /// <param name="reason">Reason message for termination</param>
        protected override void DoTerminateInstance(string reason)
        {
            // TimeoutException happens when workflowapplication can not be terminated in 30 sec. This happens on CTL VMs.
            // if that timeout exception happens force stop job hangs on Finished event.
            // Specifying timeout value to max value
            this.workflowApplication.Terminate(reason, TimeSpan.MaxValue);
        }

        /// <summary>
        /// DoTerminateInstance
        /// </summary>
        /// <param name="reason">Reason message for termination</param>
        /// <param name="suppressError">Suppress error for termination</param>
        protected override void DoTerminateInstance(string reason, bool suppressError)
        {
            _suppressTerminateError = suppressError;
            DoTerminateInstance(reason);
        }

        /// <summary>
        /// DoResumeInstance
        /// </summary>
        protected override void DoResumeInstance(string label)
        {
            Tracer.WriteMessage("Trying to resume workflow");

            this.IsTerminalStateAction = false;
            this.suspendAtNextCheckpoint = false;

            string prefix = string.IsNullOrEmpty(label) ? PSActivity.PSBookmarkPrefix : PSActivity.PSSuspendBookmarkPrefix + label;

            ReadOnlyCollection<BookmarkInfo> bookmarkInfos = this.workflowApplication.GetBookmarks();
            if (bookmarkInfos.Count > 0)
            {
                foreach (BookmarkInfo bookmarkInfo in bookmarkInfos)
                {
                    if (bookmarkInfo.BookmarkName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        Bookmark bookmark = new Bookmark(bookmarkInfo.BookmarkName);
                        this.workflowApplication.ResumeBookmark(bookmark, ActivityOnResumeAction.Restart);
                    }
                }
            }
            else
            {
                this.workflowApplication.Run();
            }

            this.StartTimerOnResume();
            Tracer.WriteMessage("Workflow resumed");
        }

        /// <summary>
        /// DoSuspendInstance
        /// </summary>
        /// <param name="notStarted"></param>
        protected override void DoSuspendInstance(bool notStarted)
        {
            if (notStarted && this.CheckForStartOrEndPersistence())
            {
                this.workflowApplication.BeginUnload(OnSuspendUnloadComplete, null);
            }
            else
            {
                this.suspendAtNextCheckpoint = true;
            }
        }

        /// <summary>
        /// DoExecuteInstance
        /// </summary>
        protected override void DoExecuteInstance()
        {
            if (Disposed)
                return;

            Tracer.WriteMessage("Starting workflow execution");
            _structuredTracer.BeginWorkflowExecution(_job.InstanceId);
            
            try
            {
                this.workflowApplication.Run();
            }
            catch (Exception e)
            {
                HandleWorkflowApplicationFaultedState(e);
                return;
            }

            this.StartTimers();
            Tracer.WriteMessage("Workflow application started execution");
        }
        
        /// <summary>
        /// DoResumeBookmark
        /// </summary>
        /// <param name="bookmark"></param>
        /// <param name="state"></param>
        protected override void DoResumeBookmark(Bookmark bookmark, object state)
        {
            if (bookmark == null)
                throw new ArgumentNullException("bookmark");

            ReactivateWorkflowInternal(bookmark, state, true);
        }

        /// <summary>
        /// Loads the xaml to create an executable activity.
        /// </summary>
        protected override void DoCreateInstance()
        {
            CheckDisposed();
            _structuredTracer.LoadingWorkflowForExecution(this.id);
            lock (SyncLock)
            {
                Tracer.WriteMessage("Loading Workflow");

                if (this.PSWorkflowDefinition.Workflow == null)
                {
                    ArgumentException exception = new ArgumentException(Resources.NoWorkflowProvided);
                    Tracer.TraceException(exception);
                    throw exception;
                }

                this.workflowApplication = this.PSWorkflowContext.WorkflowParameters == null
                          ? new WorkflowApplication(this.PSWorkflowDefinition.Workflow)
                          : new WorkflowApplication(this.PSWorkflowDefinition.Workflow, this.PSWorkflowContext.WorkflowParameters);
                this.wfAppNeverLoaded = false;

                this.id = this.workflowApplication.Id;
                SubscribeWorkflowApplicationEvents();

                this.ConfigureAllExtensions();
                this.SetupTimers();

                this.PersistBeforeExecution();

                SetInternalUnloaded(false);

                Tracer.WriteMessage("Workflow is loaded, Guid = " + this.id.ToString("D", CultureInfo.CurrentCulture));
            }
            _structuredTracer.WorkflowLoadedForExecution(this.id);
        }
        
        /// <summary>
        /// Remove
        /// </summary>
        protected override void DoRemoveInstance()
        {
            CheckDisposed();
            _stores.Delete();
        }
        
        /// <summary>
        /// DoPersistInstance
        /// </summary>
        protected override void DoPersistInstance()
        {
            if (_job.JobStateInfo.State == JobState.Completed || _job.JobStateInfo.State == JobState.Stopped || _job.JobStateInfo.State == JobState.Failed)
            {
                // when job is reached to a terminal state so we just need to persist the required information
                _stores.Save(WorkflowStoreComponents.Metadata
                    | WorkflowStoreComponents.Streams
                    | WorkflowStoreComponents.TerminatingError
                    | WorkflowStoreComponents.JobState
                );
            }
            else
            {
                // if job has not reached the terminal state then the persistence will happen after the completion of currently running activity.
                this.PersistAfterNextPSActivity = true;
            }
        }

        /// <summary>
        /// DoGetPersistableIdleAction
        /// </summary>
        /// <param name="bookmarks"></param>
        /// <param name="externalSuspendRequest"></param>
        /// <returns></returns>
        protected override PSPersistableIdleAction DoGetPersistableIdleAction(ReadOnlyCollection<BookmarkInfo> bookmarks, bool externalSuspendRequest)
        {
            if (bookmarks.Count == 0)
                return PSPersistableIdleAction.None;

            Collection<BookmarkInfo> bks = new Collection<BookmarkInfo>();
            foreach (BookmarkInfo bk in bookmarks)
                bks.Add(bk);

            // Check if this is a suspend request
            if (VerifyRequest(bks, PSActivity.PSSuspendBookmarkPrefix))
                return PSPersistableIdleAction.Suspend;

            // Check if this is a persist request
            if (VerifyRequest(bks, PSActivity.PSPersistBookmarkPrefix))
            {
                if (externalSuspendRequest)
                    return PSPersistableIdleAction.Suspend;

                return PSPersistableIdleAction.Persist;
            }

            if (bks.Count > 0)
                return PSPersistableIdleAction.Unload;

            return PSPersistableIdleAction.None;
        }

        // This will return true when all bookmarks are of type same prefix. (like suspend or persist)
        // We may have branches (i.e.: in a parallel statement) where bookmarks are
        // being used for other things.
        private bool VerifyRequest(Collection<BookmarkInfo> bookmarks, string prefix)
        {
            if ((bookmarks == null) || (bookmarks.Count <= 0))
                return false;

            Collection<BookmarkInfo> toRemove = new Collection<BookmarkInfo>();
            bool rc = true;
            foreach (BookmarkInfo bookmark in bookmarks)
            {
                if (!bookmark.BookmarkName.Contains(prefix))
                {
                    rc = false;
                }
                else
                {
                    toRemove.Add(bookmark);
                }
            }

            foreach (BookmarkInfo r in toRemove)
            {
                bookmarks.Remove(r);
            }

            return rc;
        }


        /// <summary>
        /// Dispose 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing || Disposed)
                return;

            lock (SyncLock)
            {
                if (Disposed)
                    return;

                Disposed = true;

                lock (ReactivateSync)
                {
                    this.IsTerminalStateAction = true;
                    this.PersistIdleTimerInProgressOrTriggered = false;
                    SetInternalUnloaded(false);
                }

                this.ConfigureTimerOnUnload();
                
                // saving the workflow application handle into the temporary variable
                // then unregistering the workflow application handle
                // temporary variable will be used to call abort if workflow is in running state
                WorkflowApplication wf = this.workflowApplication;
                DisposeWorkflowApplication();

                if (this._job.JobStateInfo.State == JobState.Running && wf != null)
                {
                    try
                    {
                        wf.Abort("Disposing the job");
                    }
                    catch (Exception)
                    {
                        // We are not re-throwing any exception during Dispose
                    }
                }
                
                if (_paramDefaults != null)
                {
                    _paramDefaults.Dispose();
                    _paramDefaults = null;
                }

                if (_streams != null)
                {
                    UnregisterHandlersForDataAdding(_streams);
                    _streams.Dispose();
                }

                if (_timers != null)
                    _timers.Dispose();

                if (_debugger != null)
                    _debugger.Dispose();

                DisposePersistUnloadTimer();

                base.Dispose(disposing);
            }
        }

        #endregion Protected override members

        #region Private Members

        private void SetupTimers()
        {
            if (Timer != null && PSWorkflowContext.PSWorkflowCommonParameters != null)
            {
                // Running time
                if (PSWorkflowContext.PSWorkflowCommonParameters.ContainsKey(Constants.PSRunningTime))
                {
                    int timeInSeconds = Convert.ToInt32(PSWorkflowContext.PSWorkflowCommonParameters[Constants.PSRunningTime], CultureInfo.CurrentCulture);

                    if (timeInSeconds > 0)
                    {
                        Timer.SetupTimer(WorkflowTimerType.RunningTimer, TimeSpan.FromSeconds(timeInSeconds));
                    }
                }

                // Elapsed time
                if (PSWorkflowContext.PSWorkflowCommonParameters.ContainsKey(Constants.PSElapsedTime))
                {
                    int timeInSeconds = Convert.ToInt32(PSWorkflowContext.PSWorkflowCommonParameters[Constants.PSElapsedTime], CultureInfo.CurrentCulture);

                    if (timeInSeconds > 0)
                    {
                        Timer.SetupTimer(WorkflowTimerType.ElapsedTimer, TimeSpan.FromSeconds(timeInSeconds));
                    }
                }

                // Persist Interval
                bool psPersistValue = true;
                if (PSWorkflowContext.PSWorkflowCommonParameters.ContainsKey(Constants.Persist))
                {
                    psPersistValue = Convert.ToBoolean(PSWorkflowContext.PSWorkflowCommonParameters[Constants.Persist], CultureInfo.CurrentCulture);
                }
            }
        }

        private HostParameterDefaults _paramDefaults;
        private void ConfigureAllExtensions()
        {
            // declaring instance store
            this.workflowApplication.InstanceStore = _stores.CreateInstanceStore();

            var IOParticipant = _stores.CreatePersistenceIOParticipant();
            if (IOParticipant != null)
                this.workflowApplication.Extensions.Add(IOParticipant);

            // adding the tracking participants
            this.workflowApplication.Extensions.Add(this.GetTrackingParticipant());

            // adding the custom extensions
            IEnumerable<object> extensions = this.Runtime.Configuration.CreateWorkflowExtensions();
            if (extensions != null)
            {
                foreach (object extension in extensions)
                {
                    this.workflowApplication.Extensions.Add(extension);
                }
            }

            // adding the custom extension creation functions
            IEnumerable<Func<object>> extensionFunctions = this.Runtime.Configuration.CreateWorkflowExtensionCreationFunctions<object>();
            if (extensionFunctions != null)
            {
                foreach(Func<object> extensionFunc in extensionFunctions)
                {
                    this.workflowApplication.Extensions.Add<object>(extensionFunc);
                }
            }
            
            _paramDefaults = new HostParameterDefaults();

            if (this.PSWorkflowContext.PSWorkflowCommonParameters != null)
            {
                foreach (KeyValuePair<string, object> param in this.PSWorkflowContext.PSWorkflowCommonParameters)
                {
                    if (param.Key != Constants.PSRunningTime && param.Key != Constants.PSElapsedTime)
                    {
                        _paramDefaults.Parameters.Add(param.Key, param.Value);
                    }
                }
            }

            if (this.PSWorkflowContext.PrivateMetadata != null)
            {
                _paramDefaults.Parameters[Constants.PrivateMetadata] = this.PSWorkflowContext.PrivateMetadata;
            }

            // Job related parameters
            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataName))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataName)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataName];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataInstanceId))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataInstanceId)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataInstanceId];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataSessionId))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataSessionId)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataSessionId];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataCommand))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataCommand)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataCommand];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentName))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataParentName)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataParentName];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentInstanceId))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataParentInstanceId)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataParentInstanceId];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentSessionId))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataParentSessionId)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataParentSessionId];
            }

            if (this.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentCommand))
            {
                _paramDefaults.Parameters[TranslateMetaDataName(Constants.JobMetadataParentCommand)] = 
                    this.PSWorkflowContext.JobMetadata[Constants.JobMetadataParentCommand];
            }

            _paramDefaults.Parameters["WorkflowInstanceId"] = this.InstanceId;
          
            _paramDefaults.Parameters["Input"] = this.Streams.InputStream;
            _paramDefaults.Parameters["Result"] = this.Streams.OutputStream;
            _paramDefaults.Parameters["PSError"] = this.Streams.ErrorStream;
            _paramDefaults.Parameters["PSWarning"] = this.Streams.WarningStream;
            _paramDefaults.Parameters["PSProgress"] = this.Streams.ProgressStream;
            _paramDefaults.Parameters["PSVerbose"] = this.Streams.VerboseStream;
            _paramDefaults.Parameters["PSDebug"] = this.Streams.DebugStream;
            _paramDefaults.Parameters["PSInformation"] = this.Streams.InformationStream;

            // Assign PSActivityHost here
            _paramDefaults.Runtime = Runtime;

            _paramDefaults.JobInstanceId = _job.InstanceId;

            // Assign PSHostPersistDelegate here
            Func<bool> hostDelegate = this.CheckForPersistenceAfterPSActivity;
            _paramDefaults.HostPersistenceDelegate = hostDelegate;

            Action<object> activateDelegate = this.ReactivateWorkflow;
            _paramDefaults.ActivateDelegate = activateDelegate;

            _paramDefaults.AsyncExecutionCollection = this.asyncExecutionCollection;
            _paramDefaults.RemoteActivityState = this.RemoteActivityState;

            System.Activities.Hosting.SymbolResolver resolver = new System.Activities.Hosting.SymbolResolver();
            resolver.Add("ParameterDefaults", _paramDefaults);

            this.workflowApplication.Extensions.Add(resolver);
            this.workflowApplication.Extensions.Add(_paramDefaults);
        }


        private void PersistBeforeExecution()
        {
            if (this.CheckForStartOrEndPersistence())
            {
                try
                {
                    // For robustness
//                    _structuredTracer.PersistingWorkflow(this.id, (_runtime.Configuration as PSWorkflowConfigurationProvider).InstanceStorePath);
                    this.workflowApplication.Persist();
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                    throw;
                }
            }
        }
        
        private void StartTimers()
        {
            if (Timer != null)
            {
                Timer.StartTimer(WorkflowTimerType.RunningTimer);
                Timer.StartTimer(WorkflowTimerType.ElapsedTimer);
            }
        }

        private void ConfigureTimerOnUnload()
        {
            if (Timer != null)
            {
                Timer.StopTimer(WorkflowTimerType.RunningTimer);
            }
        }

        private void StartTimerOnResume()
        {

            if (Timer != null)
            {
                Timer.StartTimer(WorkflowTimerType.RunningTimer);
            }
        }

        /// <summary>
        /// Construct the workflow tracking participant.
        /// </summary>
        /// <returns>Returns the workflow tracking participant.</returns>
        private PSWorkflowTrackingParticipant GetTrackingParticipant()
        {
            const String all = "*";
            PSWorkflowTrackingParticipant participant = new PSWorkflowTrackingParticipant(this._debugger)
            {
                // Create a tracking profile to subscribe for tracking records
                // In this sample the profile subscribes for CustomTrackingRecords,
                // workflow instance records and activity state records
                TrackingProfile = new TrackingProfile()
                {
                    Name = "WorkflowTrackingProfile",
                    Queries = 
                    {
                        new CustomTrackingQuery() 
                        {
                         Name = all,
                         ActivityName = all
                        },
                        new WorkflowInstanceQuery()
                        {
                            // Limit workflow instance tracking records for started and completed workflow states
                            States = { 
                                WorkflowInstanceStates.Started, 
                                WorkflowInstanceStates.Completed, 
                                WorkflowInstanceStates.Persisted, 
                                WorkflowInstanceStates.UnhandledException 
                            },
                        },
                        new ActivityStateQuery()
                        {
                            // Subscribe for track records from all activities for all states
                            ActivityName = all,
                            States = { all },

                            // Extract workflow variables and arguments as a part of the activity tracking record
                            // VariableName = "*" allows for extraction of all variables in the scope
                            // of the activity
                            Variables = 
                            {                                
                                { all }   
                            },

                            Arguments =
                            {
                                { all }
                            }
                        }   
                    }
                }
            };

            return participant;
        }

        private bool CheckForStartOrEndPersistence()
        {
            // This is for the unit testing
            // Inside the unit test the force disable attribute can be set to true.
            if (ForceDisableStartOrEndPersistence == true)
                return false;

            if (this.PSWorkflowContext != null && this.PSWorkflowContext.PSWorkflowCommonParameters != null && this.PSWorkflowContext.PSWorkflowCommonParameters.ContainsKey(Constants.Persist))
            {
                bool? value = this.PSWorkflowContext.PSWorkflowCommonParameters[Constants.Persist] as bool?;
                if (value != null && value == false)
                    return false;
            }

            return true;
        }

        private bool CheckForPersistenceAfterPSActivity()
        {
            bool value = this.PersistAfterNextPSActivity;
            this.PersistAfterNextPSActivity = false;

            return value;
        }

        #endregion Private Members

        #region Persist and Reactivation

        internal bool InternalUnloaded;
        
        private void SetInternalUnloaded(bool value)
        {
            InternalUnloaded = value;

            if (this.RemoteActivityState != null)
                this.RemoteActivityState.InternalUnloaded = value;
        }

        private bool wfAppNeverLoaded = true;

        private bool PersistIdleTimerInProgressOrTriggered = false;
        private bool IsTerminalStateAction = false;

        private Timer PersistUnloadTimer;
        private object ReactivateSync = new object();

        private void ReactivateWorkflow(object state)
        {
            Debug.Assert(state != null, "State not passed correctly to ReactivateWorkflow");
            Bookmark bookmark = state as Bookmark;
            Debug.Assert(bookmark != null, "Bookmark not passed correctly to ReactivateWorkflow");

            ReactivateWorkflowInternal(bookmark, null, false);
        }

        private void ReactivateWorkflowInternal(Bookmark bookmark, object state, bool validateBookmark)
        {
            if (Disposed || IsTerminalStateAction)
                return;

            CheckAndLoadInstanceForReactivation();

            Debug.Assert(this.workflowApplication != null, "PSWorkflowApplicationInstance is not initialized properly");
            if (this.workflowApplication == null)
                return;

            if (validateBookmark && this.CheckIfBookmarkExistInCollection(bookmark.Name, this.workflowApplication.GetBookmarks()) == false)
            {
                Tracer.WriteMessage("PSWorkflowInstance", "ReactivateWorkflowInternal", this.id,"Invalid bookmark: '{0}'.", bookmark.Name);
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidBookmark,bookmark.Name));
            }

            try
            {
                this.workflowApplication.ResumeBookmark(bookmark, state);
            }
            catch (Exception e)
            {
                // there should not be any exception if any then writing to the logs
                Tracer.TraceException(e);
                HandleWorkflowApplicationFaultedState(e);
            }
        }

        private void CheckAndLoadInstanceForReactivation()
        {
            if (Disposed || IsTerminalStateAction || (PersistIdleTimerInProgressOrTriggered == false && this.wfAppNeverLoaded == false))
                return;

            lock (ReactivateSync)
            {
                if (Disposed || IsTerminalStateAction || (PersistIdleTimerInProgressOrTriggered == false && this.wfAppNeverLoaded == false))
                    return;

                if (this.InternalUnloaded || this.wfAppNeverLoaded)
                {
                    this.DoLoadInstanceForReactivation();
                    SetInternalUnloaded(false);
                }

                PersistIdleTimerInProgressOrTriggered = false;
            }
        }

        private bool CheckIfBookmarkExistInCollection(string bookmarkName, ReadOnlyCollection<BookmarkInfo> bookmarks)
        {
            foreach (BookmarkInfo bookmark in bookmarks)
            {
                if (bookmarkName == bookmark.BookmarkName)
                    return true;
            }

            return false;
        }

        private void StopBookMarkedWorkflow()
        {
            // Workflow is currently in booked marked state
            // we will try to cancel all async operations 
            // and then perform the terminal tasks related to stop workflow

            if (Disposed)
                return;

            if (System.Threading.Interlocked.CompareExchange(ref terminalStateHandled, Handled, NotHandled) == Handled)
                return;

            lock (ReactivateSync)
            {
                if (Disposed)
                    return;

                IsTerminalStateAction = true;

                StopAllAsyncExecutions();

                Tracer.WriteMessage("Workflow is in Canceled state.");

                State = JobState.Stopped;
                this.PerformTaskAtTerminalState();

                // do all cleanups here to save memory
                PerformCleanupAtTerminalState();

                if (this.OnStopped != null)
                    this.OnStopped(this);
            }
        }

        private void StopAllAsyncExecutions()
        {
            if (Disposed)
                return;

            if (this.asyncExecutionCollection != null && this.asyncExecutionCollection.Count > 0)
            {
                // This is not a full fix:
                // there could be a race condition where we have taken the copy but context is removed from by activity and disposed in that case contextinstance.cancel will throw
                // there could be one more possibility that is if copy has been made and after that activity adds a new context and will not get canceled and streams will get closed so async execution might throw
                // the solution is make every these operation on async execution collection thread safe

                foreach (PSActivityContext psActivityContextInstance in this.asyncExecutionCollection.Values.ToList())
                {
                    if (psActivityContextInstance != null)
                    {
                        psActivityContextInstance.IsCanceled = true;
                        Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity: Executing cancel request."));

                        psActivityContextInstance.Cancel();
                    }
                }

                this.asyncExecutionCollection.Clear();
            }
        }

        private void StartPersistUnloadWithZeroSeconds()
        {
            lock (ReactivateSync)
            {
                if (IsTerminalStateAction)
                    return;

                PersistIdleTimerInProgressOrTriggered = true;
                SetInternalUnloaded(true);
            }
        }

        private void StartPersistUnloadTimer(int delaySeconds)
        {
            // return if persist idle is in progress
            if (PersistIdleTimerInProgressOrTriggered)
                return;

            lock (ReactivateSync)
            {
                // return if persist idle is in progress
                if (PersistIdleTimerInProgressOrTriggered)
                    return;

                if (IsTerminalStateAction)
                    return;

                PersistIdleTimerInProgressOrTriggered = true;
                PersistUnloadTimer.Enabled = true;
            }
        }

        private void DisposePersistUnloadTimer()
        {
            if (PersistUnloadTimer == null)
                return;

            lock (ReactivateSync)
            {
                if (PersistUnloadTimer == null)
                    return;

                PersistUnloadTimer.Elapsed -= new ElapsedEventHandler(PersistUnloadTimer_Elapsed);
                PersistUnloadTimer.Dispose();
                PersistUnloadTimer = null;
            }
        }

        private void PersistUnloadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (PersistIdleTimerInProgressOrTriggered == false || IsTerminalStateAction)
                return;

            lock (ReactivateSync)
            {
                if (IsTerminalStateAction)
                    return;

                if (PersistIdleTimerInProgressOrTriggered)
                {
                    SetInternalUnloaded(true);

                    // Check for the race condition.
                    // There could be possibility workflow application is unloaded because of terminal state is reached.
                    if (this.workflowApplication != null)
                    {
                        try
                        {
                            this.OnUnloadComplete(this.workflowApplication.BeginUnload(null, null));
                        }
                        catch (Exception exp)
                        {
                            Tracer.WriteMessage("PSWorkflowInstance", "PersistUnloadTimer_Elapsed", id, "Got an exception while unloading the workflow Application.");
                            Tracer.TraceException(exp);
                            return;
                        }
                    }
                }
            }
        }
        private void OnUnloadComplete(IAsyncResult result)
        {
            if (Disposed) return;

            try
            {
                this.workflowApplication.EndUnload(result);
            }
            catch (Exception e)
            {
                Tracer.WriteMessage("PSWorkflowInstance", "PersistUnloadTimer_Elapsed", id, "Not able to unload workflow application in a given timeout.");
                Tracer.TraceException(e);
                return;
            }

            this.DisposeWorkflowApplication();
        }

        /// <summary>
        /// CheckForTerminalAction
        /// </summary>
        internal override void CheckForTerminalAction()
        {
            lock (ReactivateSync)
            {
                IsTerminalStateAction = true;
                PersistIdleTimerInProgressOrTriggered = false;
                if (this.InternalUnloaded)
                {
                    this.DoLoadInstanceForReactivation();
                    SetInternalUnloaded(false);
                }
            }
        }

        /// <summary>
        /// Validate if the label exists.
        /// </summary>
        /// <param name="label"></param>
        internal override void ValidateIfLabelExists(string label)
        {
            string prefix = PSActivity.PSSuspendBookmarkPrefix + label;

            ReadOnlyCollection<BookmarkInfo> bookmarkInfos = this.workflowApplication.GetBookmarks();

            bool found = false;
            foreach (BookmarkInfo bookmarkInfo in bookmarkInfos)
            {
                if (bookmarkInfo.BookmarkName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }

            if (!found)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidLabel, label));
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Helper method to translate internal Workflow metadata names to 
        /// public Workflow variable names.
        /// </summary>
        /// <param name="metaDataName">Metadata key string</param>
        /// <returns>Public workflow variable name</returns>
        internal static string TranslateMetaDataName(string metaDataName)
        {
            string rtnName;

            switch (metaDataName)
            {
                case Constants.JobMetadataName:
                    rtnName = "JobName";
                    break;

                case Constants.JobMetadataInstanceId:
                    rtnName = "JobInstanceId";
                    break;

                case Constants.JobMetadataSessionId:
                    rtnName = "JobId";
                    break;

                case Constants.JobMetadataCommand:
                    rtnName = "JobCommandName";
                    break;

                case Constants.JobMetadataParentName:
                    rtnName = "ParentJobName";
                    break;

                case Constants.JobMetadataParentInstanceId:
                    rtnName = "ParentJobInstanceId";
                    break;

                case Constants.JobMetadataParentSessionId:
                    rtnName = "ParentJobId";
                    break;

                case Constants.JobMetadataParentCommand:
                    rtnName = "ParentCommandName";
                    break;

                case Constants.JobMetadataLocation:
                    rtnName = "PSComputerName";
                    break;

                default:
                    rtnName = metaDataName;
                    break;
            }

            return rtnName;
        }

        #endregion
    }
}
