// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet suspends the jobs that are Job2. Errors are added for each Job that is not Job2.
    /// </summary>
#if !CORECLR
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsLifecycle.Suspend, "Job", SupportsShouldProcess = true, DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210613")]
    [OutputType(typeof(Job))]
#endif
    public class SuspendJobCommand : JobCmdletBase, IDisposable
    {
        #region Parameters
        /// <summary>
        /// Specifies the Jobs objects which need to be
        /// suspended.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = JobParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Job[] Job
        {
            get
            {
                return _jobs;
            }

            set
            {
                _jobs = value;
            }
        }

        private Job[] _jobs;

        /// <summary>
        /// </summary>
        public override string[] Command
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// If state of the job is running , this will forcefully suspend it.
        /// </summary>
        [Parameter(ParameterSetName = RemoveJobCommand.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.JobParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.NameParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.SessionIdParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.FilterParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.StateParameterSet)]
        [Alias("F")]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force = false;

        /// <summary>
        /// </summary>
        [Parameter()]
        public SwitchParameter Wait
        {
            get
            {
                return _wait;
            }

            set
            {
                _wait = value;
            }
        }

        private bool _wait = false;

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Suspend the Job.
        /// </summary>
        protected override void ProcessRecord()
        {
            // List of jobs to suspend
            List<Job> jobsToSuspend = null;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    {
                        jobsToSuspend = FindJobsMatchingByName(true, false, true, false);
                    }

                    break;

                case InstanceIdParameterSet:
                    {
                        jobsToSuspend = FindJobsMatchingByInstanceId(true, false, true, false);
                    }

                    break;

                case SessionIdParameterSet:
                    {
                        jobsToSuspend = FindJobsMatchingBySessionId(true, false, true, false);
                    }

                    break;

                case StateParameterSet:
                    {
                        jobsToSuspend = FindJobsMatchingByState(false);
                    }

                    break;

                case FilterParameterSet:
                    {
                        jobsToSuspend = FindJobsMatchingByFilter(false);
                    }

                    break;

                default:
                    {
                        jobsToSuspend = CopyJobsToList(_jobs, false, false);
                    }

                    break;
            }

            _allJobsToSuspend.AddRange(jobsToSuspend);

            foreach (Job job in jobsToSuspend)
            {
                var job2 = job as Job2;

                // If the job is not Job2, the suspend operation is not supported.
                if (job2 == null)
                {
                    WriteError(
                        new ErrorRecord(
                            PSTraceSource.NewNotSupportedException(RemotingErrorIdStrings.JobSuspendNotSupported, job.Id),
                            "Job2OperationNotSupportedOnJob", ErrorCategory.InvalidType, (object)job));
                    continue;
                }

                string targetString =
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemovePSJobWhatIfTarget,
                                                                   job.Command, job.Id);
                if (ShouldProcess(targetString, VerbsLifecycle.Suspend))
                {
                    if (_wait)
                    {
                        _cleanUpActions.Add(job2, HandleSuspendJobCompleted);
                    }
                    else
                    {
                        if (job2.IsFinishedState(job2.JobStateInfo.State) || job2.JobStateInfo.State == JobState.Stopping)
                        {
                            _warnInvalidState = true;
                            continue;
                        }

                        if (job2.JobStateInfo.State == JobState.Suspending || job2.JobStateInfo.State == JobState.Suspended)
                            continue;

                        job2.StateChanged += noWait_Job2_StateChanged;
                    }

                    job2.SuspendJobCompleted += HandleSuspendJobCompleted;

                    lock (_syncObject)
                    {
                        if (!_pendingJobs.Contains(job2.InstanceId))
                        {
                            _pendingJobs.Add(job2.InstanceId);
                        }
                    }

                    // there could be possibility that the job gets completed before or after the
                    // subscribing to nowait_job2_statechanged event so checking it again.
                    if (!_wait && (job2.IsFinishedState(job2.JobStateInfo.State) || job2.JobStateInfo.State == JobState.Suspending || job2.JobStateInfo.State == JobState.Suspended))
                    {
                        this.ProcessExecutionErrorsAndReleaseWaitHandle(job2);
                    }

                    job2.SuspendJobAsync(_force, RemotingErrorIdStrings.ForceSuspendJob);
                }
            }
        }

        private bool _warnInvalidState = false;
        private readonly HashSet<Guid> _pendingJobs = new HashSet<Guid>();
        private readonly ManualResetEvent _waitForJobs = new ManualResetEvent(false);
        private readonly Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>> _cleanUpActions =
            new Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>>();
        private readonly List<ErrorRecord> _errorsToWrite = new List<ErrorRecord>();
        private readonly List<Job> _allJobsToSuspend = new List<Job>();
        private readonly object _syncObject = new object();
        private bool _needToCheckForWaitingJobs;

        private void noWait_Job2_StateChanged(object sender, JobStateEventArgs e)
        {
            Job job = sender as Job;

            switch (e.JobStateInfo.State)
            {
                case JobState.Completed:
                case JobState.Stopped:
                case JobState.Failed:
                case JobState.Suspended:
                case JobState.Suspending:
                    this.ProcessExecutionErrorsAndReleaseWaitHandle(job);
                    break;
            }
        }

        private void HandleSuspendJobCompleted(object sender, AsyncCompletedEventArgs eventArgs)
        {
            Job job = sender as Job;

            if (eventArgs.Error != null && eventArgs.Error is InvalidJobStateException)
            {
                _warnInvalidState = true;
            }

            this.ProcessExecutionErrorsAndReleaseWaitHandle(job);
        }

        private void ProcessExecutionErrorsAndReleaseWaitHandle(Job job)
        {
            bool releaseWait = false;
            lock (_syncObject)
            {
                if (_pendingJobs.Contains(job.InstanceId))
                {
                    _pendingJobs.Remove(job.InstanceId);
                }
                else
                {
                    // there could be a possibility of race condition where this function is getting called twice
                    // so if job doesn't present in the _pendingJobs then just return
                    return;
                }

                if (_needToCheckForWaitingJobs && _pendingJobs.Count == 0)
                    releaseWait = true;
            }

            if (!_wait)
            {
                job.StateChanged -= noWait_Job2_StateChanged;
                Job2 job2 = job as Job2;
                if (job2 != null)
                    job2.SuspendJobCompleted -= HandleSuspendJobCompleted;
            }

            var parentJob = job as ContainerParentJob;
            if (parentJob != null && parentJob.ExecutionError.Count > 0)
            {
                foreach (
                    var e in
                        parentJob.ExecutionError.Where(static e => e.FullyQualifiedErrorId == "ContainerParentJobSuspendAsyncError")
                    )
                {
                    if (e.Exception is InvalidJobStateException)
                    {
                        // if any errors were invalid job state exceptions, warn the user.
                        // This is to support Get-Job | Resume-Job scenarios when many jobs
                        // are Completed, etc.
                        _warnInvalidState = true;
                    }
                    else
                    {
                        _errorsToWrite.Add(e);
                    }
                }
            }

            // end processing has been called
            // set waithandle if this is the last one
            if (releaseWait)
                _waitForJobs.Set();
        }

        /// <summary>
        /// End Processing.
        /// </summary>
        protected override void EndProcessing()
        {
            bool haveToWait = false;
            lock (_syncObject)
            {
                _needToCheckForWaitingJobs = true;
                if (_pendingJobs.Count > 0)
                    haveToWait = true;
            }

            if (haveToWait)
                _waitForJobs.WaitOne();

            if (_warnInvalidState) WriteWarning(RemotingErrorIdStrings.SuspendJobInvalidJobState);
            foreach (var e in _errorsToWrite) WriteError(e);
            foreach (var j in _allJobsToSuspend) WriteObject(j);
            base.EndProcessing();
        }

        /// <summary>
        /// </summary>
        protected override void StopProcessing()
        {
            _waitForJobs.Set();
        }

        #endregion Overrides

        #region Dispose

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var pair in _cleanUpActions)
            {
                pair.Key.SuspendJobCompleted -= pair.Value;
            }

            _waitForJobs.Dispose();
        }
        #endregion Dispose
    }
}
