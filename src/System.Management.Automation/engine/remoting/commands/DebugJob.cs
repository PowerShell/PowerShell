// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    [Cmdlet(VerbsDiagnostic.Debug, "Job", SupportsShouldProcess = true, DefaultParameterSetName = DefaultParameterSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=330208")]
    public sealed class DebugJobCommand : PSRemoteDebugCmdlet
    {
        #region Parameters

        /// <summary>
        /// The Job object to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = DefaultParameterSetName)]
        public Job Job { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// The message to display when the debugger is first attached to the job.
        /// </summary>
        protected override string DebuggingStartedMessage => string.Format(CultureInfo.InvariantCulture, DebuggerStrings.DebuggingJob, Job.Name);

        /// <summary>
        /// End processing. Do work.
        /// </summary>
        protected override void EndProcessing()
        {
            switch (ParameterSetName)
            {
                case NameParameterSetName:
                    Job = GetJobByName(Name);
                    break;

                case IdParameterSetName:
                    Job = GetJobById(Id);
                    break;

                case InstanceIdParameterSetName:
                    Job = GetJobByInstanceId(InstanceId);
                    break;
            }

            Debug.Assert(Job != null);

            if (!ShouldProcess(Job.Name, VerbsDiagnostic.Debug))
            {
                return;
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

            base.EndProcessing();
        }

        /// <summary>
        /// Starts the job debugging session.
        /// </summary>
        protected override void StartDebugging()
        {
            InitDebuggingSignal();

            Job.StateChanged += Job_StateChanged;

            // Set up host script debugger to debug the job.
            Debugger.DebugJob(Job, disableBreakAll: NoInitialBreak);
        }

        /// <summary>
        /// Adds event handlers to the job and debugger for output data processing.
        /// </summary>
        protected override void AddDataEventHandlers()
        {
            if (Job.ChildJobs.Count == 0)
            {
                // No child jobs, monitor this job's results collection.
                Job.Results.DataAdding += Results_DataAdding;
            }
            else
            {
                // Monitor each child job's results collections.
                foreach (var childJob in Job.ChildJobs)
                {
                    childJob.Results.DataAdding += Results_DataAdding;
                }
            }
        }

        /// <summary>
        /// Terminate the job on exception.
        /// </summary>
        /// <param name="_"></param>
        protected override void HandleDataProcessingException(Exception _)
        {
            // Terminate job on exception.
            if (Job != null && !Job.IsFinished)
            {
                Job.StopJob();
            }
        }

        /// <summary>
        /// Removes event handlers from the job and debugger that were added for output data processing.
        /// </summary>
        protected override void RemoveDataEventHandlers()
        {
            if (Job.ChildJobs.Count == 0)
            {
                // Remove single job DataAdding event handler.
                Job.Results.DataAdding -= Results_DataAdding;
            }
            else
            {
                // Remove each child job's DataAdding event handler.
                foreach (var childJob in Job.ChildJobs)
                {
                    childJob.Results.DataAdding -= Results_DataAdding;
                }
            }
        }

        /// <summary>
        /// Stop the job debugging session.
        /// </summary>
        protected override void StopDebugging()
        {
            Job.StateChanged -= Job_StateChanged;

            if (Job != null)
            {
                Debugger?.StopDebugJob(Job);
            }
        }

        #endregion Overrides

        #region Private methods

        /// <summary>
        /// Check for debuggable job. Job must implement IJobDebugger and also
        /// must be running or in Debug stopped state.
        /// </summary>
        /// <returns></returns>
        private bool CheckForDebuggableJob()
        {
            bool debuggableJobFound = GetJobDebuggable(Job);
            if (!debuggableJobFound)
            {
                // Assume passed in job is a container job and check child jobs.
                foreach (var cJob in Job.ChildJobs)
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

        private bool GetJobDebuggable(Job job)
        {
            if (job is IJobDebugger)
            {
                return ((job.JobStateInfo.State == JobState.Running) ||
                        (job.JobStateInfo.State == JobState.AtBreakpoint));
            }

            return false;
        }

        private void Job_StateChanged(object sender, JobStateEventArgs stateChangedArgs)
        {
            // If we are leaving a breakpoint, allow the output buffer to be written
            if (stateChangedArgs.PreviousJobStateInfo.State == JobState.AtBreakpoint)
            {
                SetDebuggingSignal();
            }
            
            var job = sender as Job;
            if (job.IsFinished)
            {
                CloseOutput();
            }
        }

        private void Results_DataAdding(object sender, DataAddingEventArgs dataAddingArgs)
        {
            AddOutput(dataAddingArgs.ItemAdded as PSStreamObject);
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

        #endregion Private methods
    }
}
