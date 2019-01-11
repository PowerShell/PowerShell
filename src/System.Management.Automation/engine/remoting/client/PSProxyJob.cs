// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    #region Workflow Hosting API

    /// <summary>
    /// Class that will serve as the API for hosting and executing
    /// workflows in PowerShell. This class will have a behavior
    /// similar to how the Runspace and PowerShell APIs behave in
    /// the remoting scenario. The objects on the client side act
    /// as proxies to the real objects on the server.
    /// </summary>
    public sealed class PSJobProxy : Job2
    {
        #region Constructors

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        internal PSJobProxy(string command)
            : base(command)
        {
            _tracer.WriteMessage(ClassNameTrace, "ctor", _remoteJobInstanceId, this,
                "Constructing proxy job", null);
            StateChanged += HandleMyStateChange;
        }

        #endregion Constructors

        #region Overrides of Job

        ///<summary>
        /// Success status of the command execution.
        /// </summary>
        public override string StatusMessage
        {
            get { return _remoteJobStatusMessage; }
        }

        /// <summary>
        /// Indicates that more data is available in this
        /// result object for reading.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                // moreData is initially set to true, and it
                // will remain so until the async result
                // object has completed execution.

                if (_moreData && IsFinishedState(JobStateInfo.State))
                {
                    bool atleastOneChildHasMoreData = ChildJobs.Any(t => t.HasMoreData);

                    bool parentHasMoreData = (CollectionHasMoreData(Output)
                     || CollectionHasMoreData(Error)
                     || CollectionHasMoreData(Verbose)
                     || CollectionHasMoreData(Debug)
                     || CollectionHasMoreData(Warning)
                     || CollectionHasMoreData(Progress));

                    _moreData = parentHasMoreData || atleastOneChildHasMoreData;
                }

                return _moreData;
            }
        }

        /// <summary>
        /// This is the location string from the remote job.
        /// </summary>
        public override string Location
        {
            get { return _remoteJobLocation; }
        }

        #endregion

        #region Overrides of Job2

        /// <summary>
        /// Start a job. The job will be started with the parameters
        /// specified in StartParameters.
        /// </summary>
        /// <exception cref="InvalidJobStateException">Thrown if the job
        ///  is already running, if there is no runspace or runspace pool
        ///  assigned.</exception>
        /// <exception cref="NotSupportedException">Thrown if the job is
        ///  otherwise started, finished, or suspended.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if job is
        /// disposed.</exception>
        public override void StartJob()
        {
            StartJob(null, null, null);
        }

        /// <summary>
        /// Start a job asynchronously.
        /// </summary>
        /// <remarks>When a job is started all the data in the
        /// job streams from a previous invocation will be cleared</remarks>
        public override void StartJobAsync()
        {
            StartJobAsync(null, null, null);
        }

        /// <summary>
        /// Stop a job synchronously. In order to be consistent, this method
        /// should be used in place of StopJob which was introduced in the
        /// v2 Job API.
        /// </summary>
        /// <exception cref="InvalidJobStateException">Thrown if job is blocked.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if job is disposed.</exception>
        public override void StopJob()
        {
            try
            {
                // The call is asynchronous because Receive-Job is called
                // in the same invocation. Return once the job state changed
                // event is received, and the job state is updated to a
                // Finished state.
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Stop);
                else
                    DoStopAsync();

                Finished.WaitOne();
            }
            catch (Exception error)
            {
                _tracer.WriteMessage(ClassNameTrace, "StopJob", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                throw;
            }
        }

        /// <summary>
        /// Stop a job asynchronously.
        /// </summary>
        public override void StopJobAsync()
        {
#pragma warning disable 56500
            try
            {
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Stop);
                else
                    DoStopAsync();
            }
            catch (Exception error)
            {
                // Exception transferred using event arguments.
                _tracer.WriteMessage(ClassNameTrace, "StopJobAsync", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                OnStopJobCompleted(new AsyncCompletedEventArgs(error, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJob(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyJobControlNotSupported);
        }

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJobAsync(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyJobControlNotSupported);
        }

        /// <summary>
        /// Suspend a job.
        /// </summary>
        /// <exception cref="InvalidJobStateException">Throws if the job is not in
        /// a running or suspended state.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if job is
        /// disposed.</exception>
        public override void SuspendJob()
        {
            try
            {
                // The call is asynchronous because Receive-Job is called
                // in the same invocation. Return once the job state changed
                // event is received, and the job state is updated to a
                // Finished or Suspended state.
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Suspend);
                else
                    DoSuspendAsync();

                JobSuspendedOrFinished.WaitOne();
            }
            catch (Exception error)
            {
                _tracer.WriteMessage(ClassNameTrace, "SuspendJob", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously suspend a job.
        /// </summary>
        public override void SuspendJobAsync()
        {
#pragma warning disable 56500
            try
            {
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Suspend);
                else
                    DoSuspendAsync();
            }
            catch (Exception error)
            {
                // Exception transferred using event arguments.
                _tracer.WriteMessage(ClassNameTrace, "SuspendJobAsync", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                OnSuspendJobCompleted(new AsyncCompletedEventArgs(error, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// SuspendJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJob(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyJobControlNotSupported);
        }

        /// <summary>
        /// SuspendJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJobAsync(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyJobControlNotSupported);
        }

        /// <summary>
        /// Resume a suspended job.
        /// </summary>
        /// <exception cref="InvalidJobStateException">Throws if the job
        /// is not in a suspended or running state.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if job is
        /// disposed.</exception>
        public override void ResumeJob()
        {
            try
            {
                // The call is asynchronous because Receive-Job is called
                // in the same invocation. Return once the job state changed
                // event is received, and the job state is updated to a
                // Finished or Running state.
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Resume);
                else
                    DoResumeAsync();

                JobRunningOrFinished.WaitOne();
            }
            catch (Exception error)
            {
                _tracer.WriteMessage(ClassNameTrace, "ResumeJob", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                throw;
            }
        }

        /// <summary>
        /// Resume a suspended job asynchronously.
        /// </summary>
        public override void ResumeJobAsync()
        {
#pragma warning disable 56500
            try
            {
                if (ShouldQueueOperation())
                    _pendingOperations.Enqueue(QueueOperation.Resume);
                else
                    DoResumeAsync();
            }
            catch (Exception error)
            {
                // Exception transferred using event arguments.
                _tracer.WriteMessage(ClassNameTrace, "ResumeJobAsync", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                OnResumeJobCompleted(new AsyncCompletedEventArgs(error, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Unblock a blocked job.
        /// </summary>
        /// <exception cref="NotSupportedException">Unblock job is not supported on PSJobProxy.</exception>
        public override void UnblockJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyUnblockJobNotSupported);
        }

        /// <summary>
        /// Unblock a blocked job asynchronously.
        /// </summary>
        public override void UnblockJobAsync()
        {
            OnUnblockJobCompleted(new AsyncCompletedEventArgs(PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyUnblockJobNotSupported), false, null));
        }

        #endregion

        #region Additional State Management Methods

        /// <summary>
        /// Start execution of the workflow with the
        /// specified input. This input will serve as
        /// input to the underlying pipeline.
        /// </summary>
        /// <param name="input">collection of input
        /// objects</param>
        public void StartJobAsync(PSDataCollection<object> input)
        {
            StartJobAsync(null, null, input);
        }

        /// <summary>
        /// Start execution of the job with the
        /// specified input. This input will serve as
        /// input to the underlying pipeline.
        /// </summary>
        /// <param name="input"></param>
        /// <remarks>Not sure if this method is needed. This has
        /// been added just to be in sync with the PowerShell
        /// APIs</remarks>
        public void StartJob(PSDataCollection<object> input)
        {
            StartJob(null, null, input);
        }

        /// <summary>
        /// Start execution of the workflow with the
        /// specified input. This input will serve as
        /// input to the underlying pipeline.
        /// Because the number of child jobs is unknown before starting
        /// the job, delegates may be indicated to ensure that no events will be missed
        /// after the child job is created if data begins streaming back immediately.
        /// </summary>
        /// <param name="dataAdded">Delegate used to subscribe to data added events on the child jobs.</param>
        /// <param name="stateChanged">Delegate used to subscribe to state changed events on the child jobs.</param>
        /// <param name="input">collection of input
        /// objects</param>
        public void StartJob(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, PSDataCollection<object> input)
        {
            try
            {
                // The call is asynchronous because Receive-Job is called
                // in the same invocation. Return once the job object is returned and
                // used to initialize the proxy.
                DoStartAsync(dataAdded, stateChanged, input);
                _jobInitializedWaitHandle.WaitOne();
            }
            catch (Exception error)
            {
                _tracer.WriteMessage(ClassNameTrace, "StartJob", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                throw;
            }
        }

        /// <summary>
        /// Start asynchronous execution of the workflow with the
        /// specified input. This input will serve as
        /// input to the underlying pipeline.
        /// Because the number of child jobs is unknown before starting
        /// the job, delegates may be indicated to ensure that no events will be missed
        /// after the child job is created if data begins streaming back immediately.
        /// </summary>
        /// <param name="dataAdded">Delegate used to subscribe to data added events on the child jobs.</param>
        /// <param name="stateChanged">Delegate used to subscribe to state changed events on the child jobs.</param>
        /// <param name="input">collection of input
        /// objects</param>
        public void StartJobAsync(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, PSDataCollection<object> input)
        {
#pragma warning disable 56500
            try
            {
                DoStartAsync(dataAdded, stateChanged, input);
            }
            catch (Exception error)
            {
                // Exception transferred using event arguments.
                _tracer.WriteMessage(ClassNameTrace, "StartJobAsync", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                OnStartJobCompleted(new AsyncCompletedEventArgs(error, false, null));
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Removes the job.
        /// If remoteJob is true, the job output that has been transferred to this
        /// client object will be preserved.
        /// </summary>
        /// <param name="removeRemoteJob">Indicates whether the remove operation should
        /// be applied to the remote or local job.</param>
        /// <param name="force">Force will stop the job on the server before
        /// attempting removal. Default value is false.</param>
        /// <exception cref="InvalidOperationException">Thrown if the job is not in
        /// a completed state.</exception>
        public void RemoveJob(bool removeRemoteJob, bool force)
        {
            if (!removeRemoteJob)
            {
                Dispose();
                return;
            }

            lock (SyncRoot)
            {
                AssertNotDisposed();
            }

            try
            {
                DoRemove(force);
                lock (SyncRoot)
                {
                    // If _removeCalled has not been set to true, do not wait.
                    // There should be an exception during DoRemove in most cases when
                    // this is true.
                    if (!_removeCalled)
                    {
                        Diagnostics.Assert(false, "remove called is false after calling remove. No exception was thrown.");
                        return;
                    }
                }

                RemoveComplete.WaitOne();
            }
            catch (Exception error)
            {
                _tracer.WriteMessage(ClassNameTrace, "RemoveJob", _remoteJobInstanceId, this, "Error", null);
                _tracer.TraceException(error);
                throw;
            }
        }

        /// <summary>
        /// Removes the job.
        /// If remoteJob is true, the job output that has been transferred to this
        /// client object will be preserved.
        /// </summary>
        /// <param name="removeRemoteJob">Indicates whether the remove operation should
        /// be applied to the remote or local job.</param>
        /// <exception cref="InvalidOperationException">Thrown if the job is not in
        /// a completed state.</exception>
        public void RemoveJob(bool removeRemoteJob)
        {
            RemoveJob(removeRemoteJob, false);
        }

        /// <summary>
        /// Removes the job on the remote server.
        /// The job output that has been transferred to this client object will be
        /// preserved.
        /// </summary>
        /// <param name="removeRemoteJob">Indicates whether the remove operation should
        /// be applied to the remote or local job.</param>
        /// <param name="force">Force will stop the job on the server before
        /// attempting removal.</param>
        public void RemoveJobAsync(bool removeRemoteJob, bool force)
        {
            if (!removeRemoteJob)
            {
                Dispose();
                OnRemoveJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            var asyncOp = AsyncOperationManager.CreateOperation(force);
            var workerDelegate = new JobActionWorkerDelegate(JobActionWorker);
            workerDelegate.BeginInvoke(asyncOp, ActionType.Remove, null, null);
        }

        /// <summary>
        /// Removes the job on the remote server.
        /// The job output that has been transferred to this client object will be
        /// preserved.
        /// </summary>
        /// <param name="removeRemoteJob">Indicates whether the remove operation should
        /// be applied to the remote or local job.</param>
        public void RemoveJobAsync(bool removeRemoteJob)
        {
            RemoveJobAsync(removeRemoteJob, false);
        }

        /// <summary>
        /// This event should be raised whenever the asynchronous removal of
        /// a server side job is completed.
        /// </summary>
        public event EventHandler<AsyncCompletedEventArgs> RemoveJobCompleted;

        /// <summary>
        /// Method to raise the event when removing a
        /// server side job is completed.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        private void OnRemoveJobCompleted(AsyncCompletedEventArgs eventArgs)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<AsyncCompletedEventArgs> handler = RemoveJobCompleted;

#pragma warning disable 56500
            try
            {
                if (handler != null)
                {
                    handler(this, eventArgs);
                }
            }
            catch (Exception exception)
            {
                // errors in the handlers are not errors in the operation
                // silently ignore them
                _tracer.TraceException(exception);
            }
#pragma warning restore 56500
        }

        #endregion Additional State Management Methods

        #region Additional Properties

        /// <summary>
        /// If set, the remote job will be removed when it has been completed and the data has been received.
        /// This can only be set prior to a job being started.
        /// </summary>
        public bool RemoveRemoteJobOnCompletion
        {
            get
            {
                return _removeRemoteJobOnCompletion;
            }

            set
            {
                AssertChangesCanBeAccepted();
                _removeRemoteJobOnCompletion = value;
            }
        }

        private bool _removeRemoteJobOnCompletion;

        /// <summary>
        /// The instance ID of the remote job that this proxy interacts with.
        /// </summary>
        public Guid RemoteJobInstanceId
        {
            get { return _remoteJobInstanceId; }
        }

        /// <summary>
        /// Runspace in which this job will be executed.
        /// </summary>
        /// <remarks>At any point of time only a runspace or a
        /// runspacepool may be specified</remarks>
        public Runspace Runspace
        {
            get
            {
                return _runspace;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                lock (SyncRoot)
                {
                    AssertChangesCanBeAccepted();
                    _runspacePool = null;
                    _runspace = value;
                }
            }
        }

        /// <summary>
        /// RunspacePool in which this job will be executed.
        /// </summary>
        public RunspacePool RunspacePool
        {
            get
            {
                return _runspacePool;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                lock (SyncRoot)
                {
                    AssertChangesCanBeAccepted();
                    _runspace = null;
                    _runspacePool = value;
                }
            }
        }

        #endregion Additional Properties

        #region Proxy Factory Static Methods

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspace">Runspace containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <param name="dataAdded">Handler to subscribe to any child job data added events.</param>
        /// <param name="stateChanged">Handler to subscribe to any child job state changed events.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter,
            EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged)
        {
            return Create(runspace, filter, dataAdded, stateChanged, true);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspace">Runspace containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <param name="receiveImmediately">If true, the data streaming will start immediately. If false,
        /// the user must call "ReceiveJob()" to start data streaming.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter, bool receiveImmediately)
        {
            return Create(runspace, filter, null, null, receiveImmediately);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspace">Runspace containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter)
        {
            return Create(runspace, filter, null, null, true);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspace">Runspace containing the jobs to base the proxy on.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(Runspace runspace)
        {
            return Create(runspace, null, null, null, true);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspacePool">RunspacePool containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <param name="dataAdded">Handler to subscribe to any child job data added events.</param>
        /// <param name="stateChanged">Handler to subscribe to any child job state changed events.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter,
            EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged)
        {
            return Create(runspacePool, filter, dataAdded, stateChanged, true);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspacePool">RunspacePool containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <param name="receiveImmediately">If true, the data streaming will start immediately. If false,
        /// the user must call "ReceiveJob()" to start data streaming.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter, bool receiveImmediately)
        {
            return Create(runspacePool, filter, null, null, receiveImmediately);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspacePool">RunspacePool containing the jobs to base the proxy on.</param>
        /// <param name="filter">Hashtable to use for the Get-Job -filter command.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter)
        {
            return Create(runspacePool, filter, null, null, true);
        }

        /// <summary>
        /// Queries the runspace for jobs and constructs a collection of job proxies to interact with them.
        /// </summary>
        /// <param name="runspacePool">RunspacePool containing the jobs to base the proxy on.</param>
        /// <returns>A collection of job proxies that represent the jobs collected based on the filter.</returns>
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool)
        {
            return Create(runspacePool, null, null, null, true);
        }

        private static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter,
            EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, bool connectImmediately)
        {
            if (runspacePool == null)
                throw new PSArgumentNullException("runspacePool");
            return Create(null, runspacePool, filter, dataAdded, stateChanged, connectImmediately);
        }

        private static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter,
            EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, bool connectImmediately)
        {
            if (runspace == null)
                throw new PSArgumentNullException("runspace");
            return Create(runspace, null, filter, dataAdded, stateChanged, connectImmediately);
        }

        private static ICollection<PSJobProxy> Create(Runspace runspace, RunspacePool runspacePool, Hashtable filter,
            EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, bool connectImmediately)
        {
            // run command
            Collection<PSObject> jobResults;

            using (PowerShell ps = PowerShell.Create())
            {
                Dbg.Assert(runspacePool == null ^ runspace == null, "Either a runspace or a runspacepool should be used, not both.");

                if (runspacePool == null)
                {
                    ps.Runspace = runspace;
                }
                else
                {
                    ps.RunspacePool = runspacePool;
                }

                ps.AddCommand("Get-Job");
                if (filter != null) ps.AddParameter("Filter", filter);

                jobResults = ps.Invoke();
            }

            Collection<PSJobProxy> jobs = new Collection<PSJobProxy>();
            foreach (var deserializedJob in jobResults)
            {
                if (!Deserializer.IsDeserializedInstanceOfType(deserializedJob, typeof(Job)))
                {
                    // Do not create proxies if jobs are live.
                    continue;
                }

                string command = string.Empty;
                if (!TryGetJobPropertyValue(deserializedJob, "Command", out command))
                {
                    Dbg.Assert(false, "Job object did not contain command when creating proxy.");
                }

                PSJobProxy job = new PSJobProxy(command);

                job.InitializeExistingJobProxy(deserializedJob, runspace, runspacePool);
                // Events will be registered and states will be set by ReceiveJob when it is called.
                // This may be later, if connectImmediately is set to false.

                job._receiveIsValidCall = true;
                if (connectImmediately)
                {
                    // This will make receive invalid in the future, sets the state to running.
                    job.ReceiveJob(dataAdded, stateChanged);
                }
                else
                {
                    Dbg.Assert(dataAdded == null, "DataAdded cannot be specified if not connecting immediately");
                    Dbg.Assert(stateChanged == null, "StateChanged cannot be specified if not connecting immediately");
                }

                jobs.Add(job);
            }

            return jobs;
        }

        #endregion Proxy Factory Static Methods

        /// <summary>
        /// Will begin streaming data for a job object created by the "create" method that is in a not started state.
        /// </summary>
        public void ReceiveJob()
        {
            ReceiveJob(null, null);
        }

        /// <summary>
        /// Will begin streaming data for a job object created by the "create" method that is in a not started state.
        /// </summary>
        /// <param name="dataAdded">Delegate used to subscribe to data added events on the child jobs.</param>
        /// <param name="stateChanged">Delegate used to subscribe to state changed events on the child jobs.</param>
        public void ReceiveJob(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged)
        {
            lock (SyncRoot)
            {
                if (!_receiveIsValidCall)
                {
                    // Receive may only be called once, and is only a valid call after a proxy job has been created in
                    // a non-streaming state.
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.JobProxyReceiveInvalid);
                }

                Dbg.Assert(JobStateInfo.State == JobState.NotStarted, "ReceiveJob is only valid on a not started PSJobProxy");
                Dbg.Assert(_receivePowerShell.Commands.Commands.Count == 0, "ReceiveJob is only valid if internal pipeline is unused.");
                Dbg.Assert(_receivePowerShell.InvocationStateInfo.State == PSInvocationState.NotStarted, "ReceiveJob is only valid if internal pipeline has not been started");
                Dbg.Assert(_dataAddedHandler == null, "DataAdded cannot be specified before calling ReceiveJob");
                Dbg.Assert(_stateChangedHandler == null, "StateChanged cannot be specified before calling ReceiveJob");

                _receiveIsValidCall = false;
                _dataAddedHandler = dataAdded;
                _stateChangedHandler = stateChanged;
            }

            RegisterChildEvents();

            // Set job states after events have been registered.
            // All jobs begin in the running state. States will be updated when state changed
            // event information is gotten by Receive-Job. See DataAddedToOutput.
            ValidateAndDoSetJobState(JobState.Running);
            foreach (PSChildJobProxy child in ChildJobs)
            {
                Dbg.Assert(child != null, "Child should always be PSChildJobProxy");
                child.DoSetJobState(JobState.Running);
            }

            AssignRunspaceOrRunspacePool(_receivePowerShell);
            AddReceiveJobCommandToPowerShell(_receivePowerShell, false);
            _receivePowerShell.AddParameter("InstanceId", _remoteJobInstanceId);
            _receivePowerShell.BeginInvoke((PSDataCollection<PSObject>)null, _receivePowerShellOutput, null,
                                               CleanupReceivePowerShell, null);
        }

        #region Internal Methods

        internal void InitializeJobProxy(PSCommand command, Runspace runspace, RunspacePool runspacePool)
        {
            Dbg.Assert(command != null, "Command cannot be null");
            _tracer.WriteMessage(ClassNameTrace, "InitializeJobProxy", _remoteJobInstanceId, this,
                "Initializing Job Proxy.", null);
            _pscommand = command.Clone();

            var paramCol = new CommandParameterCollection();
            foreach (var parameter in _pscommand.Commands[0].Parameters)
            {
                paramCol.Add(parameter);
            }

            // The StartParameters may be edited or overwritten by the user.
            // The parameters will need to be added back to the command object before
            // execution.
            StartParameters = new List<CommandParameterCollection> { paramCol };
            _pscommand.Commands[0].Parameters.Clear();

            CommonInit(runspace, runspacePool);
        }

        internal void InitializeExistingJobProxy(PSObject o, Runspace runspace, RunspacePool runspacePool)
        {
            Dbg.Assert(o != null, "deserialized job cannot be null");
            _tracer.WriteMessage(ClassNameTrace, "InitializeExistingJobProxy", _remoteJobInstanceId, this,
                                 "Initializing job proxy for existing job.", null);
            _pscommand = null;
            _startCalled = true;
            _jobInitialized = true;
            CommonInit(runspace, runspacePool);

            PopulateJobProperties(o);
            // Do not set child job states, or subscribe to child events. This will
            // be done in ReceiveJob whenever it is called.
            // We need to wait until then because event handlers may not be specified until then.

            // Construct the parent job's start parameters specific for SM+
            List<Hashtable> psParamCollection = new List<Hashtable>();
            object psPrivateMetadata = null;
            foreach (PSChildJobProxy job in ChildJobs)
            {
                // Each child job should have StartParameters.Count = 1.
                if (job.StartParameters.Count == 0) continue;
                Hashtable childJobCol = new Hashtable();
                foreach (CommandParameter p in job.StartParameters[0])
                {
                    if (psPrivateMetadata == null && p.Name.Equals("PSPrivateMetadata", StringComparison.OrdinalIgnoreCase))
                    {
                        psPrivateMetadata = p.Value;
                    }

                    childJobCol.Add(p.Name, p.Value);
                }

                psParamCollection.Add(childJobCol);
            }

            CommandParameterCollection newStartParameters = new CommandParameterCollection();
            newStartParameters.Add(new CommandParameter("PSParameterCollection", psParamCollection));
            if (psPrivateMetadata != null)
            {
                newStartParameters.Add(new CommandParameter("PSPrivateMetadata", psPrivateMetadata));
            }

            StartParameters.Add(newStartParameters);
        }

        private void CommonInit(Runspace runspace, RunspacePool runspacePool)
        {
            _runspacePool = runspacePool;
            _runspace = runspace;

            _receivePowerShell.InvocationStateChanged += ReceivePowerShellInvocationStateChanged;

            // Output buffer other than the PSJobProxy output is needed.
            // If the proxy's output buffer is used, data added there will raise events
            // to the consumer before we have re-distributed the output.
            _receivePowerShellOutput.DataAdded += DataAddedToOutput;
            _receivePowerShell.Streams.Error.DataAdded += DataAddedToError;
            _receivePowerShell.Streams.Debug.DataAdded += DataAddedToDebug;
            _receivePowerShell.Streams.Verbose.DataAdded += DataAddedToVerbose;
            _receivePowerShell.Streams.Warning.DataAdded += DataAddedToWarning;
            _receivePowerShell.Streams.Progress.DataAdded += DataAddedToProgress;
            _receivePowerShell.Streams.Information.DataAdded += DataAddedToInformation;
        }

        /// <summary>
        /// Helper to do error checking for getting a property of type T from a PSobject.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        internal static bool TryGetJobPropertyValue<T>(PSObject o, string propertyName, out T propertyValue)
        {
            propertyValue = default(T);
            PSPropertyInfo propertyInfo = o.Properties[propertyName];
            if (propertyInfo == null || !(propertyInfo.Value is T)) return false;
            propertyValue = (T)propertyInfo.Value;
            return true;
        }

        #endregion Internal Methods

        #region Async helpers

        private enum ActionType
        {
            Remove
        }

        private class AsyncCompleteContainer
        {
            internal AsyncCompletedEventArgs EventArgs;
            internal ActionType Action;
        }

        private delegate void JobActionWorkerDelegate(AsyncOperation asyncOp, ActionType action);
        private void JobActionWorker(AsyncOperation asyncOp, ActionType action)
        {
            Exception exception = null;

#pragma warning disable 56500
            try
            {
                switch (action)
                {
                    case ActionType.Remove:
                        DoRemove(asyncOp.UserSuppliedState);
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
            var container = operationState as AsyncCompleteContainer;
            Dbg.Assert(container != null, "AsyncComplete Container is null; not passed properly.");
            _tracer.WriteMessage(ClassNameTrace, "JobActionAsyncCompleted", _remoteJobInstanceId, this,
                "Async operation {0} completed", container.Action.ToString());
            switch (container.Action)
            {
                case ActionType.Remove:
                    if (container.EventArgs.Error == null) RemoveComplete.WaitOne();
                    OnRemoveJobCompleted(container.EventArgs);
                    break;
            }
        }

        #endregion Async helpers

        #region Private Methods

        private bool ShouldQueueOperation()
        {
            bool queueThisOperation;
            lock (_inProgressSyncObject)
            {
                if (!_inProgress)
                {
                    _inProgress = true;
                    queueThisOperation = false;
                }
                else
                {
                    queueThisOperation = true;
                }
            }

            return queueThisOperation;
        }

        private void ProcessQueue()
        {
            bool queueWorkItem = false;
            lock (_inProgressSyncObject)
            {
                if (!_pendingOperations.IsEmpty && !_workerCreated)
                {
                    queueWorkItem = true;
                    _workerCreated = true;
                    _inProgress = true;
                }
                else
                    _inProgress = false;
            }

            if (queueWorkItem)
            {
                ThreadPool.QueueUserWorkItem(ProcessQueueWorker);
            }
        }

        private void ProcessQueueWorker(object state)
        {
            // If this operation has been queued to the thread pool, processing rights
            // already belong here. Release only when finished.
            while (true)
            {
                QueueOperation nextOperation;
                lock (_inProgressSyncObject)
                {
                    if (!_pendingOperations.TryDequeue(out nextOperation))
                    {
                        _inProgress = false;
                        _workerCreated = false;
                        break;
                    }
                }

                switch (nextOperation)
                {
                    case QueueOperation.Stop:
#pragma warning disable 56500
                        try
                        {
                            DoStopAsync();
                            Finished.WaitOne();
                        }
                        catch (Exception e)
                        {
                            // Transfer exception via event arguments.
                            OnStopJobCompleted(new AsyncCompletedEventArgs(e, false, null));
                        }

                        break;
                    case QueueOperation.Suspend:
                        try
                        {
                            DoSuspendAsync();
                            JobSuspendedOrFinished.WaitOne();
                        }
                        catch (Exception e)
                        {
                            // Transfer exception via event arguments.
                            OnSuspendJobCompleted(new AsyncCompletedEventArgs(e, false, null));
                        }

                        break;
                    case QueueOperation.Resume:
                        try
                        {
                            DoResumeAsync();
                            JobRunningOrFinished.WaitOne();
                        }
                        catch (Exception e)
                        {
                            // Transfer exception via event arguments.
                            OnResumeJobCompleted(new AsyncCompletedEventArgs(e, false, null));
                        }
#pragma warning restore 56500
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if there is more data in the specified collection.
        /// </summary>
        /// <typeparam name="T">Type of the collection</typeparam>
        /// <param name="collection">Collection to check.</param>
        /// <returns>True if the collection has more data.</returns>
        private static bool CollectionHasMoreData<T>(PSDataCollection<T> collection)
        {
            return (collection.IsOpen || collection.Count > 0);
        }

        /// <summary>
        /// Worker method which starts the job.
        /// </summary>
        private void DoStartAsync(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, PSDataCollection<object> input)
        {
            // Checks disposed, and whether the start operation is valid.
            AssertJobCanBeStartedAndSetStateToRunning();
            Dbg.Assert(_inProgress == false, "Start should always be able to obtain processing rights if it is a valid call.");
            lock (_inProgressSyncObject) { _inProgress = true; }

            // Set input and handlers. Should only be done after we've confirmed that this
            // call to StartJob should proceed, which isn't until now.
            lock (SyncRoot)
            {
                Dbg.Assert(_dataAddedHandler == null, "DataAdded cannot be specified before StartJob is called");
                Dbg.Assert(_stateChangedHandler == null, "StateChanged cannot be specified before StartJob is called");
                _dataAddedHandler = dataAdded;
                _stateChangedHandler = stateChanged;
            }

            _tracer.WriteMessage(ClassNameTrace, "DoStartAsync", _remoteJobInstanceId, this,
                                    "Starting command invocation.", null);
            s_structuredTracer.BeginProxyJobExecution(InstanceId);

            DoStartPrepare();

            _receivePowerShell.BeginInvoke(input, _receivePowerShellOutput, null, CleanupReceivePowerShell, null);
        }

        private void DoStartPrepare()
        {
            // Either a runspace or a runspacepool has to be set
            // before the job can be started
            if (_runspacePool == null && _runspace == null)
            {
                throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.RunspaceAndRunspacePoolNull);
            }

            AssignRunspaceOrRunspacePool(_receivePowerShell);

            // set the parameters. the user may have changed them.
            bool found = false;
            if (StartParameters != null && StartParameters.Count > 0)
            {
                foreach (var parameter in StartParameters[0])
                {
                    _pscommand.Commands[0].Parameters.Add(parameter);
                    if (string.Compare(parameter.Name, "AsJob", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (!(parameter.Value is bool) || !(bool)parameter.Value)
                        {
                            // If the AsJob Parameter has been passed and explicitly set to false, we should
                            // not proceed with the operation. This is an error.
                            throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.JobProxyAsJobMustBeTrue);
                        }

                        found = true;
                    }
                }
            }

            // If the user has overwritten the parameter collection, we should add AsJob back again.
            if (!found)
            {
                _pscommand.Commands[0].Parameters.Add("AsJob", true);
            }

            // set the commands for the powershell
            _receivePowerShell.Commands = _pscommand;

            // Setup receive-job data streaming.
            // Pipe the invocation with as job to Receive-Job, because we do not yet have the job's
            // instance ID.
            // Use the WriteJob parameter to cause Receive-Job to output the job object.
            AddReceiveJobCommandToPowerShell(_receivePowerShell, true);
        }

        /// <summary>
        /// Worker method which stops the job.
        /// </summary>
        private void DoStopAsync()
        {
            if (!AssertStopJobIsValidAndSetToStopping())
            {
                OnStopJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            _receivePowerShell.Stop();
            _receivePowerShell.Commands.Clear();
            _receivePowerShell.GenerateNewInstanceId();
            _receivePowerShell.AddCommand("Stop-Job").AddParameter("InstanceId", _remoteJobInstanceId).AddParameter("PassThru");
            AddReceiveJobCommandToPowerShell(_receivePowerShell, false);
            _receivePowerShell.BeginInvoke((PSDataCollection<PSObject>)null, _receivePowerShellOutput, null, CleanupReceivePowerShell, null);
        }

        /// <summary>
        /// Worker method which suspends the job.
        /// </summary>
        private void DoSuspendAsync()
        {
            if (!AssertSuspendJobIsValidAndSetToSuspending())
            {
                OnSuspendJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            _receivePowerShell.Stop();
            _receivePowerShell.Commands.Clear();
            _receivePowerShell.GenerateNewInstanceId();
            _receivePowerShell.AddCommand("Suspend-Job").AddParameter("InstanceId", _remoteJobInstanceId).AddParameter("Wait");
            AddReceiveJobCommandToPowerShell(_receivePowerShell, false);
            _receivePowerShell.BeginInvoke((PSDataCollection<PSObject>)null, _receivePowerShellOutput, null, CleanupReceivePowerShell, null);
        }

        /// <summary>
        /// Worker method to resume the job.
        /// </summary>
        private void DoResumeAsync()
        {
            AssertResumeJobIsValidAndSetToRunning();

            // If the job state was not set to running, the job should not be resumed. (True if job is in finished state.)
            if (JobStateInfo.State != JobState.Running)
            {
                OnResumeJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            _receivePowerShell.Stop();
            _receivePowerShell.Commands.Clear();
            _receivePowerShell.GenerateNewInstanceId();
            _receivePowerShell.AddCommand("Resume-Job").AddParameter("InstanceId", _remoteJobInstanceId).AddParameter("Wait");
            AddReceiveJobCommandToPowerShell(_receivePowerShell, false);
            _receivePowerShell.BeginInvoke((PSDataCollection<PSObject>)null, _receivePowerShellOutput, null, CleanupReceivePowerShell, null);
        }

        /// <summary>
        /// Worker method to remove the remote job object.
        /// </summary>
        /// <param name="state">State information indicates the "force" parameter.</param>
        private void DoRemove(object state)
        {
            AssertNotDisposed();
            _tracer.WriteMessage(ClassNameTrace, "DoRemove", _remoteJobInstanceId, this, "Start", null);
            Dbg.Assert(state is bool, "State should be boolean indicating 'force'");

            // Do not check RemoveCalled outside of the lock. We do not want to wait on RemoveComplete unless this lock
            // has been released by the thread that continues.
            if (_isDisposed || _remoteJobRemoved) return;
            lock (SyncRoot)
            {
                if (_isDisposed || _remoteJobRemoved || _removeCalled) return;
                // Only return here if the job has not been started.
                if (_remoteJobInstanceId == Guid.Empty && !_startCalled) return;
                AssertRemoveJobIsValid();
                Dbg.Assert(!_removeCalled, "removecalled should only be modified within base locks");
                _removeCalled = true;
                RemoveComplete.Reset();
            }

            // _receivePowerShell will never be null and so
            // there is no need to do a null check
            Dbg.Assert(_receivePowerShell != null, "ReceivePowerShell should not be null");

            try
            {
                // Ensure that the job has been initialized, if it has been started. If
                // the instance ID is Guid.Empty after this, there was an error, and the
                // job should not be on the server. If it is, we cannot reliably connect to
                // remove it.
                // Do this within the Try/Finally block so that the wait handle will be set
                // on exit.
                _jobInitializedWaitHandle.WaitOne();
                if (_remoteJobInstanceId == Guid.Empty) return;

                // Stop the receive-job command if it's in progress.
                _receivePowerShell.Stop();

                // Remove the job with Remove-Job.
                using (PowerShell powershell = PowerShell.Create())
                {
                    // Either the runspace or runspace pool will be
                    // not null at this point. This is because of
                    // the following:
                    // 1. The job state is running
                    // 2. The job was started using either a runspace
                    //    or a runspace pool
                    // 3. Once a job has been started, the values cannot
                    //    be modified until the job reaches a terminal
                    //    state
                    // Therefore it is not required to validate the above
                    // here
                    Dbg.Assert((_runspace != null && _runspacePool == null) ||
                               (_runspace == null && _runspacePool != null),
                               "Either the runspace or a runspacepool should not be null");

                    AssignRunspaceOrRunspacePool(powershell);

                    // set the commands for the powershell
                    powershell.Commands.AddCommand("Remove-Job").AddParameter("InstanceId", _remoteJobInstanceId);

                    if ((bool)state) powershell.AddParameter("Force", true).AddParameter("ErrorAction", ActionPreference.SilentlyContinue);

                    try
                    {
                        _tracer.WriteMessage(ClassNameTrace, "DoRemove", _remoteJobInstanceId, this,

                                    "Invoking Remove-Job", null);
                        powershell.Invoke();
                    }
                    catch (Exception e)
                    {
                        // since this is third party code call out - it is ok
                        // to catch Exception. In all other cases the specific
                        // exception must be caught
                        _tracer.WriteMessage(ClassNameTrace, "DoRemove", _remoteJobInstanceId, this,
                                             "Setting job state to failed since invoking Remove-Job failed.", null);
                        DoSetJobState(JobState.Failed, e);
                        throw;
                    }

                    if (powershell.Streams.Error != null && powershell.Streams.Error.Count > 0)
                    {
                        throw powershell.Streams.Error[0].Exception;
                    }
                }

                _tracer.WriteMessage(ClassNameTrace, "DoRemove", _remoteJobInstanceId, this,
                                     "Completed Invoking Remove-Job", null);
                lock (SyncRoot)
                {
                    _remoteJobRemoved = true;
                }

                if (!IsFinishedState(JobStateInfo.State))
                {
                    DoSetJobState(JobState.Stopped);
                }
            }
            catch (Exception)
            {
                lock (SyncRoot)
                {
                    Dbg.Assert(!_remoteJobRemoved,
                               "Should never allow DoRemove to be called again if proxy job has removed its server job.");
                    _removeCalled = false;
                }

                throw;
            }
            finally
            {
                RemoveComplete.Set();
            }
        }

        private void AddReceiveJobCommandToPowerShell(PowerShell powershell, bool writeJob)
        {
            powershell.AddCommand("Receive-Job").AddParameter("Wait").AddParameter("WriteEvents").AddParameter("Verbose").AddParameter("Debug");
            if (writeJob)
                powershell.AddParameter("WriteJobInResults");
            if (RemoveRemoteJobOnCompletion)
                powershell.AddParameter("AutoRemoveJob");
        }

        private void CleanupReceivePowerShell(IAsyncResult asyncResult)
        {
#pragma warning disable 56500
            try
            {
                _receivePowerShell.EndInvoke(asyncResult);

                // set the state based on the previous computed state
                _tracer.WriteMessage(ClassNameTrace, "CleanupReceivePowerShell", Guid.Empty, this,
                                    "Setting job state to {0} from computed stated", _computedJobState.ToString());
                ValidateAndDoSetJobState(_computedJobState);
            }
            catch (PipelineStoppedException e)
            {
                // Raised if the pipeline was stopped.
                // Pipeline stopped indicates that the command was interrupted, and it should
                // not be used to indicate that a job failed.
                _tracer.TraceException(e);
            }
            catch (PSRemotingDataStructureException e)
            {
                // Raised if the pipeline was stopped. (remote case)
                // Pipeline stopped indicates that the command was interrupted, and it should
                // not be used to indicate that a job failed.
                _tracer.TraceException(e);
            }
            catch (RemoteException e)
            {
                if (Deserializer.IsInstanceOfType(e.SerializedRemoteException, typeof(PipelineStoppedException)))
                {
                    // Raised if the pipeline was stopped. (remote case)
                    // Pipeline stopped indicates that the command was interrupted, and it should
                    // not be used to indicate that a job failed.
                    _tracer.TraceException(e);
                }
                else
                {
                    _tracer.TraceException(e);

                    DoSetJobState(JobState.Failed, e);
                }
            }
            catch (Exception e)
            {
                // EndInvoke may throw any exception that the command execution could throw
                // Since this is third party code, we will catch and trace the exception.
                // Exception raised to the user using JobStateInfo.Reason.
                _tracer.WriteMessage(ClassNameTrace, "CleanupReceivePowerShell", _remoteJobInstanceId, this,
                                     "Exception calling receivePowerShell.EndInvoke", null);
                _tracer.TraceException(e);

                DoSetJobState(JobState.Failed, e);

                // This throw would cause the client application to crash if an exception is thrown in the command invocation.
            }
#pragma warning restore 56500
        }

        /// <summary>
        /// Assigns either a runspace or runspacepool to the specified powershell
        /// instance.
        /// </summary>
        /// <param name="powershell">powershell instance to which the set has to
        /// happen</param>
        private void AssignRunspaceOrRunspacePool(PowerShell powershell)
        {
            Dbg.Assert(_runspacePool == null ^ _runspace == null, "Either a runspace or a runspacepool should be assigned to the proxy job");

            if (_runspacePool == null)
            {
                powershell.Runspace = _runspace;
            }
            else
            {
                powershell.RunspacePool = _runspacePool;
            }
        }

        private void HandleMyStateChange(object sender, JobStateEventArgs e)
        {
            switch (e.JobStateInfo.State)
            {
                case JobState.Running:
                    {
                        lock (SyncRoot)
                        {
                            if (e.PreviousJobStateInfo.State == JobState.NotStarted)
                            {
                                PSBeginTime = DateTime.Now;
                            }

                            JobRunningOrFinished.Set();
                            JobSuspendedOrFinished.Reset();
                            OnResumeJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                        }
                    }

                    break;

                case JobState.Suspended:
                    {
                        lock (SyncRoot)
                        {
                            PSEndTime = DateTime.Now;

                            JobSuspendedOrFinished.Set();
                            JobRunningOrFinished.Reset();
                            OnSuspendJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                        }
                    }

                    break;
                case JobState.Failed:
                case JobState.Completed:
                case JobState.Stopped:
                    {
                        lock (SyncRoot)
                        {
                            // In a finished state, further state transitions will not occur.
                            PSEndTime = DateTime.Now;

                            // Release any thread waiting for Start, Suspend, Resume
                            JobRunningOrFinished.Set();
                            OnResumeJobCompleted(new AsyncCompletedEventArgs(e.JobStateInfo.Reason, false, null));

                            JobSuspendedOrFinished.Set();
                            OnSuspendJobCompleted(new AsyncCompletedEventArgs(e.JobStateInfo.Reason, false, null));

                            _jobInitializedWaitHandle.Set();
                            OnStartJobCompleted(new AsyncCompletedEventArgs(e.JobStateInfo.Reason, false, null));

                            OnStopJobCompleted(new AsyncCompletedEventArgs(e.JobStateInfo.Reason, false, null));
                        }
                    }

                    break;
            }

            ProcessQueue();
        }

        /// <summary>
        /// Event handler for InvocationStateChanged on the powershell
        /// object running receive-job.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Argument describing this event.</param>
        private void ReceivePowerShellInvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            _tracer.WriteMessage(ClassNameTrace, "ReceivePowerShellInvocationStateChanged", _remoteJobInstanceId, this,
                "receivePowerShell state changed to {0}", e.InvocationStateInfo.State.ToString());

            switch (e.InvocationStateInfo.State)
            {
                case PSInvocationState.Running:
                    {
                        return;
                    }

                case PSInvocationState.Completed:
                    // Job State in this case is set in CleanupReceivePowerShell.
                    break;

                case PSInvocationState.Failed:
                    {
                        var newState = JobState.Failed;
                        var reason = e.InvocationStateInfo.Reason == null
                                         ? string.Empty
                                         : e.InvocationStateInfo.Reason.ToString();
                        _tracer.WriteMessage(ClassNameTrace, "ReceivePowerShellInvocationStateChanged", _remoteJobInstanceId, this,
                            "Setting job state to {0} old state was {1} and reason is {2}.", newState.ToString(), JobStateInfo.State.ToString(), reason);
                        DoSetJobState(newState, e.InvocationStateInfo.Reason);
                    }

                    break;

                case PSInvocationState.Stopped:
                    break;
            }
        }

        /// <summary>
        /// Assigns job properties and creates child job tree.
        /// </summary>
        /// <param name="o">Deserialized job object representing the remote job for this proxy.</param>
        private void PopulateJobProperties(PSObject o)
        {
            Dbg.Assert(Deserializer.IsDeserializedInstanceOfType(o, typeof(Job)),
                       "Cannot populate job members unless o is a deserialized job.");

            TryGetJobPropertyValue(o, "InstanceId", out _remoteJobInstanceId);
            TryGetJobPropertyValue(o, "StatusMessage", out _remoteJobStatusMessage);
            TryGetJobPropertyValue(o, "Location", out _remoteJobLocation);

            s_structuredTracer.ProxyJobRemoteJobAssociation(InstanceId, _remoteJobInstanceId);

            string name;
            TryGetJobPropertyValue(o, "Name", out name);
            Name = name;

            // Create proxy jobs for each child job.
            // The remote child jobs will be started and managed by their parent. They are provided as
            // children on this job for access to the state, properties and for direct management.
            PSObject remoteChildrenObject;
            if (!TryGetJobPropertyValue(o, "ChildJobs", out remoteChildrenObject)) return; // No ChildJobs property
            var remoteChildren = remoteChildrenObject.BaseObject as ArrayList;
            Dbg.Assert(remoteChildren != null, "ChildJobs property had unexpected type.");
            foreach (PSObject job in remoteChildren.Cast<PSObject>().Where(job => !(job.BaseObject is string)))
            {
                Guid childJobInstanceId;
                if (!TryGetJobPropertyValue(job, "InstanceId", out childJobInstanceId))
                {
                    Dbg.Assert(false, "ChildJobs should be serialized to include InstanceID, cannot interact with them otherwise.");
                    continue;
                }

                var childProxyJob = new PSChildJobProxy(Command, job); // All have the same workflow name.
                _childJobsMapping.Add(childJobInstanceId, childProxyJob);

                // Set DataAddedCount for each child to match parent.
                // This may otherwise not be available to set until data arrives.
                childProxyJob.Output.DataAddedCount = Output.DataAddedCount;
                childProxyJob.Error.DataAddedCount = Error.DataAddedCount;
                childProxyJob.Progress.DataAddedCount = Progress.DataAddedCount;
                childProxyJob.Warning.DataAddedCount = Warning.DataAddedCount;
                childProxyJob.Verbose.DataAddedCount = Verbose.DataAddedCount;
                childProxyJob.Debug.DataAddedCount = Debug.DataAddedCount;
                childProxyJob.Information.DataAddedCount = Information.DataAddedCount;

                // Now see if there are start parameters we can populate.
                PSObject childJobStartParametersObject;
                if (TryGetJobPropertyValue(job, "StartParameters", out childJobStartParametersObject))
                {
                    PopulateStartParametersOnChild(childJobStartParametersObject, childProxyJob);
                }

                ChildJobs.Add(childProxyJob);
            }
        }

        private void PopulateStartParametersOnChild(PSObject childJobStartParametersObject, PSChildJobProxy childProxyJob)
        {
            ArrayList childJobStartParameters = childJobStartParametersObject.BaseObject as ArrayList;
            if (childJobStartParameters != null)
            {
                List<CommandParameterCollection> listComParCol = new List<CommandParameterCollection>();
                // check childjobstartparameters--was a List<CommandParameterCollection>
                foreach (PSObject paramCollection in childJobStartParameters.Cast<PSObject>().Where(paramCollection => !(paramCollection.BaseObject is string)))
                {
                    ArrayList parameterCollection = paramCollection.BaseObject as ArrayList;
                    if (parameterCollection != null)
                    {
                        CommandParameterCollection newComParCol = new CommandParameterCollection();
                        foreach (PSObject deserializedCommandParameter in parameterCollection.Cast<PSObject>().Where(deserializedCommandParameter => !(deserializedCommandParameter.BaseObject is string)))
                        {
                            string parameterName;
                            object parameterValue;
                            if (TryGetJobPropertyValue(deserializedCommandParameter, "Name", out parameterName) && TryGetJobPropertyValue(deserializedCommandParameter, "Value", out parameterValue))
                            {
                                CommandParameter cp = new CommandParameter(parameterName, parameterValue);
                                newComParCol.Add(cp);
                            }
                        }

                        listComParCol.Add(newComParCol);
                    }
                }

                childProxyJob.StartParameters = listComParCol;
            }
        }

        private void RegisterChildEvents()
        {
            if (_childEventsRegistered) return;
            lock (SyncRoot)
            {
                if (_childEventsRegistered) return;
                _childEventsRegistered = true;

                // if the data added handler was provided, use it to subscribe to child events.
                if (_dataAddedHandler != null)
                {
                    foreach (PSChildJobProxy job in ChildJobs)
                    {
                        Dbg.Assert(job != null, "ChildJob that is not child job proxy exists");
                        job.JobDataAdded += _dataAddedHandler;
                    }
                }

                // we need to register for our handler to compute parent proxy's state
                // after the passed in handlers are registered. This will ensure that
                // our handler is invoked last
                foreach (var job in ChildJobs)
                {
                    job.StateChanged += HandleChildProxyJobStateChanged;
                }

                // likewise for state changed
                if (_stateChangedHandler != null)
                {
                    foreach (PSChildJobProxy job in ChildJobs)
                    {
                        Dbg.Assert(job != null, "ChildJob that is not child job proxy exists");
                        job.StateChanged += _stateChangedHandler;
                    }
                }
            }
        }

        private void UnregisterChildEvents()
        {
            lock (SyncRoot)
            {
                // Check inside the lock only, if the lock is held by the register method
                // the childEventsRegistered may be altered before the lock is released.
                if (!_childEventsRegistered) return;

                // if the data added handler was provided, use it to unsubscribe from child events.
                if (_dataAddedHandler != null)
                {
                    foreach (PSChildJobProxy job in ChildJobs)
                    {
                        Dbg.Assert(job != null, "ChildJob that is not child job proxy exists");
                        job.JobDataAdded -= _dataAddedHandler;
                    }
                }

                // likewise for state changed
                if (_stateChangedHandler != null)
                {
                    foreach (PSChildJobProxy job in ChildJobs)
                    {
                        Dbg.Assert(job != null, "ChildJob that is not child job proxy exists");
                        job.StateChanged -= _stateChangedHandler;
                    }
                }

                foreach (var job in ChildJobs)
                {
                    job.StateChanged -= HandleChildProxyJobStateChanged;
                }

                _childEventsRegistered = false;
            }
        }

        private void HandleChildProxyJobStateChanged(object sender, JobStateEventArgs e)
        {
            JobState computedJobState;
            if (!ContainerParentJob.ComputeJobStateFromChildJobStates(ClassNameTrace, e, ref _blockedChildJobsCount,
                                                                      ref _suspendedChildJobsCount,
                                                                      ref _suspendingChildJobsCount,
                                                                      ref _finishedChildJobsCount,
                                                                      ref _failedChildJobsCount,
                                                                      ref _stoppedChildJobsCount,
                                                                      ChildJobs.Count,
                                                                      out computedJobState))
                return;
            if (computedJobState == JobState.Suspending) return; // Ignore for proxy job

            _tracer.WriteMessage(ClassNameTrace, "HandleChildProxyJobStateChanged", Guid.Empty, this,
                                 "storing job state to {0}", computedJobState.ToString());
            // we need to store the state and set it only when invocation
            // state changed for _receivePowerShell is received. This is to
            // enable consumers from disposing the proxy job on its state
            // changed handler
            _computedJobState = computedJobState;
        }

        /// <summary>
        /// Check if changes to the jobs properties can be accepted.
        /// </summary>
        private void AssertChangesCanBeAccepted()
        {
            lock (SyncRoot)
            {
                AssertNotDisposed();

                if (JobStateInfo.State != JobState.NotStarted)
                {
                    throw new InvalidJobStateException(JobStateInfo.State);
                }
            }
        }

        private void AssertJobCanBeStartedAndSetStateToRunning()
        {
            lock (SyncRoot)
            {
                AssertNotDisposed();

                // only a job which is not started or which is in a terminal
                // state can be restarted
                if (JobStateInfo.State != JobState.NotStarted && !IsFinishedState(JobStateInfo.State))
                {
                    throw new InvalidJobStateException(JobStateInfo.State, StringUtil.Format(PowerShellStrings.JobCannotBeStartedWhenRunning));
                }

                if (_startCalled)
                {
                    throw PSTraceSource.NewNotSupportedException(PowerShellStrings.JobCanBeStartedOnce);
                }

                _startCalled = true;
            }

            _tracer.WriteMessage(ClassNameTrace, "AssertJobCanBeStartedAndSetStateToRunning", _remoteJobInstanceId, this,
                "Setting job state to running", null);
            ValidateAndDoSetJobState(JobState.Running);
        }

        private bool AssertStopJobIsValidAndSetToStopping()
        {
            lock (SyncRoot)
            {
                AssertNotDisposed();

                if (JobStateInfo.State == JobState.NotStarted)
                {
                    throw new InvalidJobStateException(JobState.NotStarted);
                }

                if (IsFinishedState(JobStateInfo.State))
                    return false;
            }

            DoSetJobState(JobState.Stopping);
            return true;
        }

        private bool AssertSuspendJobIsValidAndSetToSuspending()
        {
            lock (SyncRoot)
            {
                AssertNotDisposed();

                // Valid states: Running, Suspended, Suspending
                if (JobStateInfo.State != JobState.Suspended
                    && JobStateInfo.State != JobState.Suspending
                    && JobStateInfo.State != JobState.Running)
                {
                    throw new InvalidJobStateException(JobStateInfo.State);
                }
            }

            if (JobStateInfo.State != JobState.Running) return false;
            DoSetJobState(JobState.Suspending);
            return true;
        }

        private void AssertResumeJobIsValidAndSetToRunning()
        {
            lock (SyncRoot)
            {
                AssertNotDisposed();
                // Valid states: Running, Suspended, Suspending
                if (JobStateInfo.State != JobState.Suspended
                    && JobStateInfo.State != JobState.Suspending
                    && JobStateInfo.State != JobState.Running)
                {
                    throw new InvalidJobStateException(JobStateInfo.State);
                }
            }

            if (JobStateInfo.State != JobState.Running) ValidateAndDoSetJobState(JobState.Running);
            foreach (PSChildJobProxy job in ChildJobs)
            {
                Dbg.Assert(job != null, "child job is not PSChildJobProxy");
                if (!IsFinishedState(job.JobStateInfo.State))
                    job.DoSetJobState(JobState.Running);
            }
        }

        private void AssertRemoveJobIsValid()
        {
            if (JobStateInfo.State == JobState.NotStarted || _remoteJobInstanceId == Guid.Empty)
            {
                throw new InvalidJobStateException(JobStateInfo.State);
            }
        }

        /// <summary>
        /// Assert if the object is not yet disposed and if so
        /// throw an ObjectDisposedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if
        /// the object has already been disposed</exception>
        /// <remarks>Method is not thread-safe. Caller has to
        /// ensure thread safety</remarks>
        private new void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw PSTraceSource.NewObjectDisposedException("PSJobProxy");
            }
        }

        private void ValidateAndDoSetJobState(JobState state, Exception reason = null)
        {
            // Do not set the state to running if the job is in the process of suspending or stopping.
            // This can happen if Stop or Suspend is called during job initialization.
            if ((_previousState == JobState.Stopping || _previousState == JobState.Suspending) && state == JobState.Running)
                return;
            DoSetJobState(state, reason);
        }

        private void DoSetJobState(JobState state, Exception reason = null)
        {
            // If the object is disposed, the user does not care about state.
            if (_isDisposed) return;

            lock (SyncRoot)
            {
                if (_previousState == state) return; // Do not set the state if the desired state is accurate.
                _previousState = state;
            }

#if DEBUG
            switch (JobStateInfo.State)
            {
                case JobState.Running:
                    Dbg.Assert(state != JobState.NotStarted && state != JobState.Blocked,
                               "JobProxy invalid state transition from Running.");
                    break;
                case JobState.Stopping:
                    Dbg.Assert(
                        state == JobState.Stopped || state == JobState.Failed || state == JobState.Completed,
                        "JobProxy invalid state transition from Stopping.");
                    break;
                case JobState.Stopped:
                    Dbg.Assert(false, "JobProxy should never transition after Stopped state.");
                    break;
                case JobState.Suspending:
                    Dbg.Assert(
                        state == JobState.Suspended || state == JobState.Completed || state == JobState.Failed ||
                        state == JobState.Stopped || state == JobState.Stopping,
                        "JobProxy invalid state transition from Suspending.");
                    break;
                case JobState.Suspended:
                    Dbg.Assert(
                        state == JobState.Running || state == JobState.Stopping || state == JobState.Stopped,
                        "JobProxy invalid state transition from Suspended.");
                    break;
                case JobState.Failed:
                    Dbg.Assert(false, "JobProxy should never transition after Failed state.");
                    break;
                case JobState.Completed:
                    Dbg.Assert(false, "JobProxy should never transition after Completed state.");
                    break;
                case JobState.Disconnected:
                    Dbg.Assert(state != JobState.NotStarted && state != JobState.Blocked,
                               "JobProxy invalid state transition from Disconnected.");
                    break;
                case JobState.Blocked:
                    Dbg.Assert(false, "JobProxy should never be in a blocked state");
                    break;
                default:
                    break;
            }
#endif

            try
            {
                _tracer.WriteMessage(ClassNameTrace, "DoSetJobState", _remoteJobInstanceId, this,
                                                 "BEGIN Set job state to {0} and call event handlers", state.ToString());
                s_structuredTracer.EndProxyJobExecution(InstanceId);
                s_structuredTracer.BeginProxyJobEventHandler(InstanceId);
                SetJobState(state, reason);
                s_structuredTracer.EndProxyJobEventHandler(InstanceId);
                _tracer.WriteMessage(ClassNameTrace, "DoSetJobState", _remoteJobInstanceId, this,
                                                 "END Set job state to {0} and call event handlers", state.ToString());
            }
            catch (ObjectDisposedException)
            {
                _tracer.WriteMessage(ClassNameTrace, "DoSetJobState", _remoteJobInstanceId, this,
                    "Caught object disposed exception", null);
            }
        }

        #endregion Private Methods

        #region Private methods for Output management

        private T GetRecord<T>(object sender)
        {
            lock (SyncRoot)
            {
                var collection = sender as PSDataCollection<T>;
                Dbg.Assert(collection != null, "Sender cannot be null");

                T data = collection.ReadAndRemoveAt0();

                Dbg.Assert(data != null, "DataAdded should be raised for each object added");
                return data;
            }
        }

        private void DataAddedToOutput(object sender, DataAddedEventArgs e)
        {
            var newObject = GetRecord<PSObject>(sender);

            if (!_jobInitialized)
            {
                // If the job is not initialized, then this should be the first object received after
                // invoking a command. This condition should be met when StartJob is called.

                // Note: These events will always come in sequence, So this is not thread safe.
                _jobInitialized = true;

                // Because we have added the AsJob parameter we expect
                // the command to write out a job, if it does not
                // the command fails.)
                // If the command writes an error before any output, the proxy will use this
                // as an indication of a failed command execution.
                if (!Deserializer.IsDeserializedInstanceOfType(newObject, typeof(Job)))
                {
                    _tracer.WriteMessage(ClassNameTrace, "DataAddedToOutput", _remoteJobInstanceId, this,
                                         "Setting job state to failed. Command did not return a job object.",
                                         null);
                    Exception reason = (_receivePowerShell.Streams.Error.Count == 0 ||
                                        _receivePowerShell.Streams.Error[0].Exception == null)
                                           ? PSTraceSource.NewNotSupportedException(PowerShellStrings.CommandDoesNotWriteJob)
                                           : _receivePowerShell.Streams.Error[0].Exception;
                    DoSetJobState(JobState.Failed, reason);
                    _jobInitializedWaitHandle.Set();
                    OnStartJobCompleted(new AsyncCompletedEventArgs(reason, false, null));
                    return;
                }

                PopulateJobProperties(newObject);

                RegisterChildEvents();

                // Child Job States must be set after events are subscribed.
                // All jobs start in the running state. Receive-Job will return
                // a persistent job state event for each child job sometime after
                // the job object is returned. This will update the job state of
                // the child in a subsequent call to DataAddedToOutput.
                foreach (PSChildJobProxy child in ChildJobs)
                {
                    Dbg.Assert(child != null, "Child should always be PSChildJobProxy");
                    child.DoSetJobState(JobState.Running);
                }

                // Release any thread waiting start.
                _jobInitializedWaitHandle.Set();
                _tracer.WriteMessage(ClassNameTrace, "DataAddedToOutput", Guid.Empty, this,
                                     "BEGIN Invoke StartJobCompleted event", null);
                OnStartJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                _tracer.WriteMessage(ClassNameTrace, "DataAddedToOutput", Guid.Empty, this,
                                     "END Invoke StartJobCompleted event", null);
                ProcessQueue();
                return;
            }

            if (newObject.Properties[RemotingConstants.EventObject] != null)
            {
                PSPropertyInfo guidProperty = newObject.Properties[RemotingConstants.SourceJobInstanceId];
                Guid sourceJobId = guidProperty != null ? (Guid)guidProperty.Value : Guid.Empty;

                if (guidProperty == null || sourceJobId == Guid.Empty)
                {
                    Diagnostics.Assert(false, "We should not get guidProperty as null or an empty source job id in non interop scenarios");
                    return;
                }

                if (!_childJobsMapping.ContainsKey(sourceJobId))
                {
                    if (sourceJobId != _remoteJobInstanceId)
                    {
                        Diagnostics.Assert(false,
                                           "We should not get an unidentified source job id in non interop scenarios");
                    }

                    return;
                }

                // If the event args are job state, the event is a Job's StateChanged event.
                var jobStateEventArgs = newObject.BaseObject as JobStateEventArgs ??
                                        Microsoft.PowerShell.DeserializingTypeConverter.RehydrateJobStateEventArgs(newObject);
                if (jobStateEventArgs != null)
                {
                    // If the child job sent the event, then change the proxy child's state.
                    // If the current job raised the event, the state change will come when the
                    // receive call returns.
                    _tracer.WriteMessage(ClassNameTrace, "DataAddedToOutput", Guid.Empty, this,
                                         "Updating child job {0} state to {1} ", sourceJobId.ToString(),
                                         jobStateEventArgs.JobStateInfo.State.ToString());
                    ((PSChildJobProxy)_childJobsMapping[sourceJobId]).DoSetJobState(
                        jobStateEventArgs.JobStateInfo.State, jobStateEventArgs.JobStateInfo.Reason);
                    _tracer.WriteMessage(ClassNameTrace, "DataAddedToOutput", Guid.Empty, this,
                                         "Finished updating child job {0} state to {1} ", sourceJobId.ToString(),
                                         jobStateEventArgs.JobStateInfo.State.ToString());
                }

                return;
            }

            SortOutputObject(newObject);
        }

        private void SortOutputObject(PSObject newObject)
        {
            PSPropertyInfo guidProperty = newObject.Properties[RemotingConstants.SourceJobInstanceId];
            Guid sourceJobId = guidProperty != null ? (Guid)guidProperty.Value : Guid.Empty;
            if (guidProperty == null || sourceJobId == Guid.Empty || !_childJobsMapping.ContainsKey(sourceJobId))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                Output.Add(newObject);
                return;
            }

            // make sure the tag reflects client side job.
            newObject.Properties.Remove(RemotingConstants.SourceJobInstanceId);
            newObject.Properties.Add(new PSNoteProperty(RemotingConstants.SourceJobInstanceId,
                                                        ((PSChildJobProxy)_childJobsMapping[sourceJobId]).InstanceId));
            ((PSChildJobProxy)_childJobsMapping[sourceJobId]).Output.Add(newObject);
        }

        private void DataAddedToError(object sender, DataAddedEventArgs e)
        {
            var newError = GetRecord<ErrorRecord>(sender);

            Dbg.Assert(newError != null, "DataAdded should be raised once for each piece of data.");

            SortError(newError);
        }

        private void SortError(ErrorRecord record)
        {
            // var newJobError = record as RemotingErrorRecord;
            // if (newJobError == null || !_childJobsMapping.ContainsKey(newJobError.OriginInfo.InstanceID))
            Guid id = Guid.Empty;
            string computerName = string.Empty;
            if (record.ErrorDetails != null)
            {
                record.ErrorDetails.RecommendedAction = RemoveIdentifierInformation(record.ErrorDetails.RecommendedAction, out id, out computerName);
            }

            if (id == Guid.Empty || !_childJobsMapping.ContainsKey(id))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteError(record);
                return;
            }

            // make sure the tag reflects client side job.
            // newJobError.OriginInfo.InstanceID = ((PSChildJobProxy)_childJobsMapping[newJobError.OriginInfo.InstanceID]).InstanceId;
            var oi = new OriginInfo(null, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[id]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[id]).WriteError(new RemotingErrorRecord(record, oi));
        }

        private void DataAddedToProgress(object sender, DataAddedEventArgs e)
        {
            var newRecord = GetRecord<ProgressRecord>(sender);

            Dbg.Assert(newRecord != null, "DataAdded should be raised once for each piece of data.");

            SortProgress(newRecord);
        }

        private void SortProgress(ProgressRecord newRecord)
        {
            // var newJobRecord = newRecord as RemotingProgressRecord;
            // if (newJobRecord == null || !_childJobsMapping.ContainsKey(newJobRecord.OriginInfo.InstanceID))
            Guid id;
            string computerName;
            newRecord.CurrentOperation = RemoveIdentifierInformation(newRecord.CurrentOperation, out id, out computerName);
            if (id == Guid.Empty || !_childJobsMapping.ContainsKey(id))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteProgress(newRecord);
                return;
            }

            // make sure the tag reflects client side job.
            // newJobRecord.OriginInfo.InstanceID = ((PSChildJobProxy)_childJobsMapping[newJobRecord.OriginInfo.InstanceID]).InstanceId;
            var oi = new OriginInfo(computerName, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[id]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[id]).WriteProgress(new RemotingProgressRecord(newRecord, oi));
        }

        private void DataAddedToDebug(object sender, DataAddedEventArgs e)
        {
            var record = GetRecord<DebugRecord>(sender);

            Dbg.Assert(record != null, "DataAdded should be raised once for each piece of data.");

            SortDebug(record);
        }

        private void SortDebug(DebugRecord record)
        {
            Guid remoteJobInstanceId;
            string computerName;
            string message = RemoveIdentifierInformation(record.Message, out remoteJobInstanceId, out computerName);
            if (remoteJobInstanceId == Guid.Empty || !_childJobsMapping.ContainsKey(remoteJobInstanceId))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteDebug(message);
                return;
            }

            // make sure the tag reflects client side job.
            var originInfo = new OriginInfo(computerName, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).Debug.Add(new RemotingDebugRecord(message, originInfo));
        }

        private void DataAddedToWarning(object sender, DataAddedEventArgs e)
        {
            var record = GetRecord<WarningRecord>(sender);

            Dbg.Assert(record != null, "DataAdded should be raised once for each piece of data.");

            SortWarning(record);
        }

        private void SortWarning(WarningRecord record)
        {
            Guid remoteJobInstanceId;
            string computerName;
            string message = RemoveIdentifierInformation(record.Message, out remoteJobInstanceId, out computerName);
            if (remoteJobInstanceId == Guid.Empty || !_childJobsMapping.ContainsKey(remoteJobInstanceId))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteWarning(message);
                return;
            }

            // make sure the tag reflects client side job.
            var originInfo = new OriginInfo(computerName, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).Warning.Add(new RemotingWarningRecord(message, originInfo));
        }

        private void DataAddedToVerbose(object sender, DataAddedEventArgs e)
        {
            var record = GetRecord<VerboseRecord>(sender);

            Dbg.Assert(record != null, "DataAdded should be raised once for each piece of data.");

            SortVerbose(record);
        }

        private void SortVerbose(VerboseRecord record)
        {
            Guid remoteJobInstanceId;
            string computerName;
            string message = RemoveIdentifierInformation(record.Message, out remoteJobInstanceId, out computerName);
            if (remoteJobInstanceId == Guid.Empty || !_childJobsMapping.ContainsKey(remoteJobInstanceId))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteVerbose(message);
                return;
            }

            // make sure the tag reflects client side job.
            var originInfo = new OriginInfo(computerName, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).Verbose.Add(new RemotingVerboseRecord(message, originInfo));
        }

        private void DataAddedToInformation(object sender, DataAddedEventArgs e)
        {
            var record = GetRecord<InformationRecord>(sender);

            Dbg.Assert(record != null, "DataAdded should be raised once for each piece of data.");

            SortInformation(record);
        }

        private void SortInformation(InformationRecord record)
        {
            Guid remoteJobInstanceId;
            string computerName;
            record.Source = RemoveIdentifierInformation(record.Source, out remoteJobInstanceId, out computerName);
            if (remoteJobInstanceId == Guid.Empty || !_childJobsMapping.ContainsKey(remoteJobInstanceId))
            {
                // If there is no child job matching the source, add it to the parent's collection so that it is not lost.
                WriteInformation(record);
                return;
            }

            // make sure the tag reflects client side job.
            var originInfo = new OriginInfo(computerName, Guid.Empty, ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).InstanceId);
            ((PSChildJobProxy)_childJobsMapping[remoteJobInstanceId]).Information.Add(new RemotingInformationRecord(record, originInfo));
        }

        private static string RemoveIdentifierInformation(string message, out Guid jobInstanceId, out string computerName)
        {
            jobInstanceId = Guid.Empty;
            computerName = string.Empty;

            if (!string.IsNullOrEmpty(message))
            {
                string[] parts = message.Split(Utils.Separators.Colon, 3);

                if (parts.Length == 3)
                {
                    if (!Guid.TryParse(parts[0], out jobInstanceId))
                        jobInstanceId = Guid.Empty;
                    computerName = parts[1];
                    return parts[2];
                }
            }

            return message;
        }

        #endregion Output management

        #region IDisposable Overrides

        /// <summary>
        /// Dispose all managed resources.
        /// </summary>
        /// <param name="disposing">True when being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_isDisposed) return;

            lock (SyncRoot)
            {
                if (_isDisposed) return;

                _isDisposed = true;
            }

            if (_receivePowerShell != null)
            {
                _receivePowerShell.Stop();
                _receivePowerShell.InvocationStateChanged -= ReceivePowerShellInvocationStateChanged;
                _receivePowerShell.Streams.Error.DataAdded -= DataAddedToError;
                _receivePowerShell.Streams.Warning.DataAdded -= DataAddedToWarning;
                _receivePowerShell.Streams.Verbose.DataAdded -= DataAddedToVerbose;
                _receivePowerShell.Streams.Progress.DataAdded -= DataAddedToProgress;
                _receivePowerShell.Streams.Debug.DataAdded -= DataAddedToDebug;
                _receivePowerShell.Streams.Information.DataAdded -= DataAddedToInformation;
                _receivePowerShell.Dispose();
            }

            UnregisterChildEvents();
            StateChanged -= HandleMyStateChange;

            _receivePowerShellOutput.DataAdded -= DataAddedToOutput;
            if (_receivePowerShellOutput != null) _receivePowerShellOutput.Dispose();

            if (_removeComplete != null) _removeComplete.Dispose();
            if (_jobRunningOrFinished != null) _jobRunningOrFinished.Dispose();
            _jobInitializedWaitHandle.Dispose();
            if (_jobSuspendedOrFinished != null) _jobSuspendedOrFinished.Dispose();

            if (ChildJobs != null && ChildJobs.Count > 0)
            {
                foreach (var job in ChildJobs)
                {
                    // Child Job events are unregistered by UnregisterChildEvents above.
                    job.Dispose();
                }
            }

            _tracer.Dispose();
        }

        #endregion IDisposable Overrides

        #region Private Members

        private enum QueueOperation
        { Stop, Suspend, Resume }

        private ConcurrentQueue<QueueOperation> _pendingOperations = new ConcurrentQueue<QueueOperation>();

        private ManualResetEvent _removeComplete;
        private ManualResetEvent RemoveComplete
        {
            get
            {
                if (_removeComplete == null)
                {
                    lock (SyncRoot)
                    {
                        if (_removeComplete == null)
                        {
                            // this assert is required so that a wait handle
                            // is not created after the object is disposed
                            // which will result in a leak
                            AssertNotDisposed();
                            _removeComplete = new ManualResetEvent(false);
                        }
                    }
                }

                return _removeComplete;
            }
        }

        private ManualResetEvent _jobRunningOrFinished;
        private ManualResetEvent JobRunningOrFinished
        {
            get
            {
                if (_jobRunningOrFinished == null)
                {
                    lock (SyncRoot)
                    {
                        if (_jobRunningOrFinished == null)
                        {
                            // this assert is required so that a wait handle
                            // is not created after the object is disposed
                            // which will result in a leak
                            AssertNotDisposed();
                            _jobRunningOrFinished = new ManualResetEvent(false);
                        }
                    }
                }

                return _jobRunningOrFinished;
            }
        }

        private readonly ManualResetEvent _jobInitializedWaitHandle = new ManualResetEvent(false);

        private ManualResetEvent _jobSuspendedOrFinished;
        private ManualResetEvent JobSuspendedOrFinished
        {
            get
            {
                if (_jobSuspendedOrFinished == null)
                {
                    lock (SyncRoot)
                    {
                        if (_jobSuspendedOrFinished == null)
                        {
                            // this assert is required so that a wait handle
                            // is not created after the object is disposed
                            // which will result in a leak
                            AssertNotDisposed();
                            _jobSuspendedOrFinished = new ManualResetEvent(false);
                        }
                    }
                }

                return _jobSuspendedOrFinished;
            }
        }

        private PSCommand _pscommand;
        private Runspace _runspace;
        private RunspacePool _runspacePool;
        private EventHandler<JobDataAddedEventArgs> _dataAddedHandler;
        private EventHandler<JobStateEventArgs> _stateChangedHandler;
        private const string ResBaseName = "PowerShellStrings";
        private Guid _remoteJobInstanceId = Guid.Empty;
        private string _remoteJobStatusMessage = string.Empty;
        private string _remoteJobLocation = string.Empty;
        private readonly Hashtable _childJobsMapping = new Hashtable();
        private readonly PowerShell _receivePowerShell = PowerShell.Create();
        private readonly PSDataCollection<PSObject> _receivePowerShellOutput = new PSDataCollection<PSObject>();
        private bool _moreData = true;
        private JobState _previousState = JobState.NotStarted;
        private JobState _computedJobState;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static Tracer s_structuredTracer = new Tracer();
        private bool _jobInitialized;
        private bool _removeCalled;
        private bool _startCalled;
        private bool _receiveIsValidCall;
        private bool _isDisposed;
        private bool _remoteJobRemoved;
        private bool _childEventsRegistered;
        private object _inProgressSyncObject = new object();
        private bool _inProgress = false;
        private bool _workerCreated = false;
        private const string ClassNameTrace = "PSJobProxy";
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

        #endregion Private Members
    }

    /// <summary>
    /// Job class used for children of PSJobProxy jobs.
    /// </summary>
    public sealed class PSChildJobProxy : Job2
    {
        internal PSChildJobProxy(string command, PSObject o)
            : base(command)
        {
            PSJobProxy.TryGetJobPropertyValue(o, "StatusMessage", out _statusMessage);
            PSJobProxy.TryGetJobPropertyValue(o, "Location", out _location);

            string name;
            PSJobProxy.TryGetJobPropertyValue(o, "Name", out name);
            Name = name;

            Output.DataAdded += OutputAdded;
            Error.DataAdded += ErrorAdded;
            Warning.DataAdded += WarningAdded;
            Verbose.DataAdded += VerboseAdded;
            Progress.DataAdded += ProgressAdded;
            Debug.DataAdded += DebugAdded;
            Information.DataAdded += InformationAdded;
        }

        internal void AssignDisconnectedState()
        {
            DoSetJobState(JobState.Disconnected);
        }

        /// <summary>
        /// Method to raise the event when this job has data added.
        /// </summary>
        /// <param name="eventArgs">argument describing
        /// an exception that is associated with the event</param>
        private void OnJobDataAdded(JobDataAddedEventArgs eventArgs)
        {
#pragma warning disable 56500
            try
            {
                _tracer.WriteMessage(ClassNameTrace, "OnJobDataAdded", Guid.Empty, this, "BEGIN call event handlers");
                JobDataAdded.SafeInvoke(this, eventArgs);
                _tracer.WriteMessage(ClassNameTrace, "OnJobDataAdded", Guid.Empty, this, "END call event handlers");
            }
            catch (Exception exception)
            {
                // errors in the handlers are not errors in the operation
                // silently ignore them
                _tracer.WriteMessage(ClassNameTrace, "OnJobDataAdded", Guid.Empty, this, "END Exception thrown in JobDataAdded handler");
                _tracer.TraceException(exception);
            }
#pragma warning restore 56500
        }

        private void OutputAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Output, e.Index));
        }

        private void ErrorAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Error, e.Index));
        }

        private void WarningAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Warning, e.Index));
        }

        private void VerboseAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Verbose, e.Index));
        }

        private void ProgressAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Progress, e.Index));
        }

        private void DebugAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Debug, e.Index));
        }

        private void InformationAdded(object sender, DataAddedEventArgs e)
        {
            OnJobDataAdded(new JobDataAddedEventArgs(this, PowerShellStreamType.Information, e.Index));
        }

        private static Tracer s_structuredTracer = new Tracer();
        internal void DoSetJobState(JobState state, Exception reason = null)
        {
            if (_disposed) return;

#if DEBUG
            switch (JobStateInfo.State)
            {
                case JobState.Running:
                    Dbg.Assert(state != JobState.NotStarted && state != JobState.Blocked,
                               "ChildJobProxy invalid state transition from Running.");
                    break;
                case JobState.Stopping:
                    Dbg.Assert(
                        state == JobState.Stopped || state == JobState.Failed || state == JobState.Completed,
                        "ChildJobProxy invalid state transition from Stopping.");
                    break;
                case JobState.Stopped:
                    Dbg.Assert(false, "ChildJobProxy should never transition after Stopped state.");
                    break;
                case JobState.Suspending:
                    Dbg.Assert(
                        state == JobState.Suspended || state == JobState.Completed || state == JobState.Failed ||
                        state == JobState.Stopped || state == JobState.Stopping,
                        "ChildJobProxy invalid state transition from Suspending.");
                    break;
                case JobState.Suspended:
                    Dbg.Assert(
                        state == JobState.Running || state == JobState.Stopping || state == JobState.Stopped,
                        "ChildJobProxy invalid state transition from Suspended.");
                    break;
                case JobState.Failed:
                    Dbg.Assert(false, "ChildJobProxy should never transition after Failed state.");
                    break;
                case JobState.Completed:
                    Dbg.Assert(false, "ChildJobProxy should never transition after Completed state.");
                    break;
                case JobState.Disconnected:
                    Dbg.Assert(state != JobState.NotStarted && state != JobState.Blocked,
                               "ChildJobProxy invalid state transition from Disconnected.");
                    break;
                case JobState.Blocked:
                    Dbg.Assert(false, "ChildJobProxy should never be in a blocked state");
                    break;
                default:
                    break;
            }
#endif

            try
            {
                Dbg.Assert(state != JobStateInfo.State, "Setting job state should always change the state.");
                _tracer.WriteMessage("PSChildJobProxy", "DoSetJobState", Guid.Empty, this,
                                                 "BEGIN Set job state to {0} and call event handlers", state.ToString());
                s_structuredTracer.BeginProxyChildJobEventHandler(InstanceId);
                SetJobState(state, reason);
                s_structuredTracer.EndProxyJobEventHandler(InstanceId);
                _tracer.WriteMessage("PSChildJobProxy", "DoSetJobState", Guid.Empty, this,
                                                 "END Set job state to {0} and call event handlers", state.ToString());
            }
            catch (ObjectDisposedException)
            {
                // If the object is disposed, the user will not need the state.
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            lock (_syncObject)
            {
                if (_disposed) return;
                _disposed = true;

                Output.DataAdded -= OutputAdded;
                Error.DataAdded -= ErrorAdded;
                Warning.DataAdded -= WarningAdded;
                Verbose.DataAdded -= VerboseAdded;
                Progress.DataAdded -= ProgressAdded;
                Debug.DataAdded -= DebugAdded;
                Information.DataAdded -= InformationAdded;
            }

            base.Dispose(disposing);
        }

        #region Control method overrides

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void StartJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void StartJobAsync()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJob(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void StopJobAsync()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJobAsync(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void SuspendJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void SuspendJobAsync()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// SuspendJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJob(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// SuspendJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJobAsync(bool force, string reason)
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void ResumeJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void ResumeJobAsync()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void UnblockJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void UnblockJobAsync()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void StopJob()
        {
            throw PSTraceSource.NewNotSupportedException(PowerShellStrings.ProxyChildJobControlNotSupported);
        }

        #endregion Control method overrides

        #region Public properties

        /// <summary>
        /// This event will be raised whenever data has been added to one of the job object's
        /// 6 collections. The event arguments include the job itself, the data type, indicating
        /// which collection has data added, and the index.
        /// </summary>
        public event EventHandler<JobDataAddedEventArgs> JobDataAdded;

        /// <summary>
        /// Status message.
        /// </summary>
        public override string StatusMessage
        {
            get { return _statusMessage; }
        }

        /// <summary>
        /// Indicates the job has or can have more data on one or more data collection.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (Output.IsOpen || Output.Count > 0
                     || Error.IsOpen || Error.Count > 0
                     || Verbose.IsOpen || Verbose.Count > 0
                     || Debug.IsOpen || Debug.Count > 0
                     || Warning.IsOpen || Warning.Count > 0
                     || Progress.IsOpen || Progress.Count > 0
                     || Information.IsOpen || Information.Count > 0
                     );
            }
        }

        /// <summary>
        /// The location of the job.
        /// </summary>
        public override string Location
        {
            get { return _location; }
        }

        #endregion Public properties

        #region Private members

        private string _statusMessage;
        private string _location;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly object _syncObject = new object();
        private bool _disposed = false;
        private const string ClassNameTrace = "PSChildJobProxy";

        #endregion
    }

    /// <summary>
    /// Event arguments that indicate data has been added to a child job.
    /// </summary>
    public sealed class JobDataAddedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="job">The job that contains the data that is added.</param>
        /// <param name="dataType">The type of data that this event is raised for.</param>
        /// <param name="index">
        /// Index at which the data is added.
        /// </param>
        internal JobDataAddedEventArgs(Job job, PowerShellStreamType dataType, int index)
        {
            SourceJob = job;
            DataType = dataType;
            Index = index;
        }

        /// <summary>
        /// The job that contains the PSDataCollection which is the sender.
        /// </summary>
        public Job SourceJob { get; }

        /// <summary>
        /// Identifies the type of the sending collection as one of the six collections
        /// associated with a job.
        /// If data type = output, sender is PSDataCollection of PSObject, Error is of ErrorRecord, etc.
        /// </summary>
        public PowerShellStreamType DataType { get; }

        /// <summary>
        /// Index at which the data is added.
        /// </summary>
        public int Index { get; }
    }

    /// <summary>
    /// Job data is added to one of these streams. Each
    /// type of data implies a different type of object.
    /// </summary>
    public enum PowerShellStreamType
    {
        /// <summary>
        /// PSObject.
        /// </summary>
        Input = 0,

        /// <summary>
        /// PSObject.
        /// </summary>
        Output = 1,

        /// <summary>
        /// ErrorRecord.
        /// </summary>
        Error = 2,

        /// <summary>
        /// WarningRecord.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// VerboseRecord.
        /// </summary>
        Verbose = 4,

        /// <summary>
        /// DebugRecord.
        /// </summary>
        Debug = 5,

        /// <summary>
        /// ProgressRecord.
        /// </summary>
        Progress = 6,

        /// <summary>
        /// InformationRecord.
        /// </summary>
        Information = 7
    }

    #endregion Workflow Hosting API
}
