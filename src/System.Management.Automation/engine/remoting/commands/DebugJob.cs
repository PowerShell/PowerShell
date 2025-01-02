// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet takes a Job object and checks to see if it is debuggable.  If it
    /// is debuggable then it breaks into the job debugger in step mode.  If it is not
    /// debuggable then it is treated as a parent job and each child job is checked if
    /// it is debuggable and if it is will break into its job debugger in step mode.
    /// For multiple debuggable child jobs, each job execution will be halted and the
    /// debugger will step to each job execution point sequentially.
    ///
    /// When a job is debugged its output data is written to host and the executing job
    /// script will break into the host debugger, in step mode, at the next stoppable
    /// execution point.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsDiagnostic.Debug, "Job", SupportsShouldProcess = true, DefaultParameterSetName = DebugJobCommand.JobParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=330208")]
    public sealed class DebugJobCommand : PSCmdlet
    {
        #region Strings

        private const string JobParameterSet = "JobParameterSet";
        private const string JobNameParameterSet = "JobNameParameterSet";
        private const string JobIdParameterSet = "JobIdParameterSet";
        private const string JobInstanceIdParameterSet = "JobInstanceIdParameterSet";

        #endregion

        #region Private members

        private Job _job;
        private Debugger _debugger;
        private PSDataCollection<PSStreamObject> _debugCollection;

        #endregion

        #region Parameters

        /// <summary>
        /// The Job object to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = DebugJobCommand.JobParameterSet)]
        public Job Job
        {
            get;
            set;
        }

        /// <summary>
        /// The Job object name to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugJobCommand.JobNameParameterSet)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The Job object Id to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugJobCommand.JobIdParameterSet)]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// The Job object InstanceId to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugJobCommand.JobInstanceIdParameterSet)]
        public Guid InstanceId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag that tells PowerShell to automatically perform a BreakAll when the debugger is attached to the remote target.
        /// </summary>
        [Parameter]
        public SwitchParameter BreakAll { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// End processing.  Do work.
        /// </summary>
        protected override void EndProcessing()
        {
            switch (ParameterSetName)
            {
                case DebugJobCommand.JobParameterSet:
                    _job = Job;
                    break;

                case DebugJobCommand.JobNameParameterSet:
                    _job = GetJobByName(Name);
                    break;

                case DebugJobCommand.JobIdParameterSet:
                    _job = GetJobById(Id);
                    break;

                case DebugJobCommand.JobInstanceIdParameterSet:
                    _job = GetJobByInstanceId(InstanceId);
                    break;
            }

            if (!ShouldProcess(_job.Name, VerbsDiagnostic.Debug))
            {
                return;
            }

            Runspace runspace = LocalRunspace.DefaultRunspace;
            if (runspace == null || runspace.Debugger == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(RemotingErrorIdStrings.CannotDebugJobNoHostDebugger),
                        "DebugJobNoHostDebugger",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if ((runspace.Debugger.DebugMode == DebugModes.Default) || (runspace.Debugger.DebugMode == DebugModes.None))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(RemotingErrorIdStrings.CannotDebugJobInvalidDebuggerMode),
                        "DebugJobWrongDebugMode",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (this.Host == null || this.Host.UI == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(RemotingErrorIdStrings.CannotDebugJobNoHostUI),
                        "DebugJobNoHostAvailable",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (!CheckForDebuggableJob())
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(DebuggerStrings.NoDebuggableJobsFound),
                        "DebugJobNoDebuggableJobsFound",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            // Set up host script debugger to debug the job.
            _debugger = runspace.Debugger;
            _debugger.DebugJob(_job, breakAll: BreakAll);

            // Blocking call.  Send job output to host UI while debugging and wait for Job completion.
            WaitAndReceiveJobOutput();
        }

        /// <summary>
        /// Stop processing.
        /// </summary>
        protected override void StopProcessing()
        {
            // Cancel job debugging.
            Debugger debugger = _debugger;
            if ((debugger != null) && (_job != null))
            {
                debugger.StopDebugJob(_job);
            }

            // Unblock the data collection.
            PSDataCollection<PSStreamObject> debugCollection = _debugCollection;
            debugCollection?.Complete();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Check for debuggable job.  Job must implement IJobDebugger and also
        /// must be running or in Debug stopped state.
        /// </summary>
        /// <returns></returns>
        private bool CheckForDebuggableJob()
        {
            // Check passed in job object.
            bool debuggableJobFound = GetJobDebuggable(_job);

            if (!debuggableJobFound)
            {
                // Assume passed in job is a container job and check child jobs.
                foreach (var cJob in _job.ChildJobs)
                {
                    debuggableJobFound = GetJobDebuggable(cJob);
                    if (debuggableJobFound)
                    {
                        break;
                    }
                }
            }

            return debuggableJobFound;
        }

        private static bool GetJobDebuggable(Job job)
        {
            if (job is IJobDebugger)
            {
                return ((job.JobStateInfo.State == JobState.Running) ||
                        (job.JobStateInfo.State == JobState.AtBreakpoint));
            }

            return false;
        }

        private void WaitAndReceiveJobOutput()
        {
            _debugCollection = new PSDataCollection<PSStreamObject>();
            _debugCollection.BlockingEnumerator = true;

            try
            {
                AddEventHandlers();

                // This call blocks (blocking enumerator) until the job completes
                // or this command is cancelled.
                foreach (var streamItem in _debugCollection)
                {
                    streamItem?.WriteStreamObject(this);
                }
            }
            catch (Exception)
            {
                // Terminate job on exception.
                if (!_job.IsFinishedState(_job.JobStateInfo.State))
                {
                    _job.StopJob();
                }

                throw;
            }
            finally
            {
                RemoveEventHandlers();
                _debugCollection = null;
            }
        }

        private void HandleJobStateChangedEvent(object sender, JobStateEventArgs stateChangedArgs)
        {
            Job job = sender as Job;
            if (job.IsFinishedState(stateChangedArgs.JobStateInfo.State))
            {
                _debugCollection.Complete();
            }
        }

        private void HandleResultsDataAdding(object sender, DataAddingEventArgs dataAddingArgs)
        {
            if (_debugCollection.IsOpen)
            {
                PSStreamObject streamObject = dataAddingArgs.ItemAdded as PSStreamObject;
                if (streamObject != null)
                {
                    try
                    {
                        _debugCollection.Add(streamObject);
                    }
                    catch (PSInvalidOperationException) { }
                }
            }
        }

        private void HandleDebuggerNestedDebuggingCancelledEvent(object sender, EventArgs e)
        {
            StopProcessing();
        }

        private void AddEventHandlers()
        {
            _job.StateChanged += HandleJobStateChangedEvent;
            _debugger.NestedDebuggingCancelledEvent += HandleDebuggerNestedDebuggingCancelledEvent;

            if (_job.ChildJobs.Count == 0)
            {
                // No child jobs, monitor this job's results collection.
                _job.Results.DataAdding += HandleResultsDataAdding;
            }
            else
            {
                // Monitor each child job's results collections.
                foreach (var childJob in _job.ChildJobs)
                {
                    childJob.Results.DataAdding += HandleResultsDataAdding;
                }
            }
        }

        private void RemoveEventHandlers()
        {
            _job.StateChanged -= HandleJobStateChangedEvent;
            _debugger.NestedDebuggingCancelledEvent -= HandleDebuggerNestedDebuggingCancelledEvent;

            if (_job.ChildJobs.Count == 0)
            {
                // Remove single job DataAdding event handler.
                _job.Results.DataAdding -= HandleResultsDataAdding;
            }
            else
            {
                // Remove each child job's DataAdding event handler.
                foreach (var childJob in _job.ChildJobs)
                {
                    childJob.Results.DataAdding -= HandleResultsDataAdding;
                }
            }
        }

        private Job GetJobByName(string name)
        {
            // Search jobs in job repository.
            List<Job> jobs1 = new List<Job>();
            WildcardPattern pattern =
                WildcardPattern.Get(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

            foreach (Job job in JobRepository.Jobs)
            {
                if (pattern.IsMatch(job.Name))
                {
                    jobs1.Add(job);
                }
            }

            // Search jobs in job manager.
            List<Job2> jobs2 = JobManager.GetJobsByName(name, this, false, false, false, null);

            int jobCount = jobs1.Count + jobs2.Count;
            if (jobCount == 1)
            {
                return (jobs1.Count > 0) ? jobs1[0] : jobs2[0];
            }

            if (jobCount > 1)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.FoundMultipleJobsWithName, name)),
                        "DebugJobFoundMultipleJobsWithName",
                        ErrorCategory.InvalidOperation,
                        this)
                    );

                return null;
            }

            ThrowTerminatingError(
                new ErrorRecord(
                    new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.CannotFindJobWithName, name)),
                    "DebugJobCannotFindJobWithName",
                    ErrorCategory.InvalidOperation,
                    this)
                );

            return null;
        }

        private Job GetJobById(int id)
        {
            // Search jobs in job repository.
            List<Job> jobs1 = new List<Job>();
            foreach (Job job in JobRepository.Jobs)
            {
                if (job.Id == id)
                {
                    jobs1.Add(job);
                }
            }

            // Search jobs in job manager.
            Job job2 = JobManager.GetJobById(id, this, false, false, false);

            if ((jobs1.Count == 0) && (job2 == null))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.CannotFindJobWithId, id)),
                        "DebugJobCannotFindJobWithId",
                        ErrorCategory.InvalidOperation,
                        this)
                    );

                return null;
            }

            if ((jobs1.Count > 1) ||
                (jobs1.Count == 1) && (job2 != null))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.FoundMultipleJobsWithId, id)),
                        "DebugJobFoundMultipleJobsWithId",
                        ErrorCategory.InvalidOperation,
                        this)
                    );

                return null;
            }

            return (jobs1.Count > 0) ? jobs1[0] : job2;
        }

        private Job GetJobByInstanceId(Guid instanceId)
        {
            // Search jobs in job repository.
            foreach (Job job in JobRepository.Jobs)
            {
                if (job.InstanceId == instanceId)
                {
                    return job;
                }
            }

            // Search jobs in job manager.
            Job2 job2 = JobManager.GetJobByInstanceId(instanceId, this, false, false, false);
            if (job2 != null)
            {
                return job2;
            }

            ThrowTerminatingError(
                new ErrorRecord(
                    new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.CannotFindJobWithInstanceId, instanceId)),
                    "DebugJobCannotFindJobWithInstanceId",
                    ErrorCategory.InvalidOperation,
                    this)
                );

            return null;
        }

        #endregion
    }
}
