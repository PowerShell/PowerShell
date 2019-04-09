// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// This cmdlet resumes the jobs that are Job2. Errors are added for each Job that is not Job2.
    /// </summary>
#if !CORECLR
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsLifecycle.Resume, "Job", SupportsShouldProcess = true, DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210611")]
#endif
    [OutputType(typeof(Job))]
    public class ResumeJobCommand : JobCmdletBase, IDisposable
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
        /// Specifies whether to delay returning from the cmdlet until all jobs reach a running state.
        /// This could take significant time due to workflow throttling.
        /// </summary>
        [Parameter(ParameterSetName = ParameterAttribute.AllParameterSets)]
        public SwitchParameter Wait { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Resume the Job.
        /// </summary>
        protected override void ProcessRecord()
        {
            // List of jobs to resume
            List<Job> jobsToResume = null;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    {
                        jobsToResume = FindJobsMatchingByName(true, false, true, false);
                    }

                    break;

                case InstanceIdParameterSet:
                    {
                        jobsToResume = FindJobsMatchingByInstanceId(true, false, true, false);
                    }

                    break;

                case SessionIdParameterSet:
                    {
                        jobsToResume = FindJobsMatchingBySessionId(true, false, true, false);
                    }

                    break;

                case StateParameterSet:
                    {
                        jobsToResume = FindJobsMatchingByState(false);
                    }

                    break;

                case FilterParameterSet:
                    {
                        jobsToResume = FindJobsMatchingByFilter(false);
                    }

                    break;

                default:
                    {
                        jobsToResume = CopyJobsToList(_jobs, false, false);
                    }

                    break;
            }

            _allJobsToResume.AddRange(jobsToResume);

            // Blue: 151804 When resuming a single suspended workflow job, Resume-job cmdlet doesn't wait for the job to be in running state
            // Setting Wait to true so that this cmdlet will wait for the running job state.
            if (_allJobsToResume.Count == 1)
                Wait = true;

            foreach (Job job in jobsToResume)
            {
                var job2 = job as Job2;

                // If the job is not Job2, the resume operation is not supported.
                if (job2 == null)
                {
                    WriteError(new ErrorRecord(PSTraceSource.NewNotSupportedException(RemotingErrorIdStrings.JobResumeNotSupported, job.Id), "Job2OperationNotSupportedOnJob", ErrorCategory.InvalidType, (object)job));
                    continue;
                }

                string targetString = PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemovePSJobWhatIfTarget, job.Command, job.Id);
                if (ShouldProcess(targetString, VerbsLifecycle.Resume))
                {
                    _cleanUpActions.Add(job2, HandleResumeJobCompleted);
                    job2.ResumeJobCompleted += HandleResumeJobCompleted;

                    lock (_syncObject)
                    {
                        if (!_pendingJobs.Contains(job2.InstanceId))
                        {
                            _pendingJobs.Add(job2.InstanceId);
                        }
                    }

                    job2.ResumeJobAsync();
                }
            }
        }

        private bool _warnInvalidState = false;
        private readonly HashSet<Guid> _pendingJobs = new HashSet<Guid>();
        private readonly ManualResetEvent _waitForJobs = new ManualResetEvent(false);
        private readonly Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>> _cleanUpActions =
            new Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>>();
        private readonly List<ErrorRecord> _errorsToWrite = new List<ErrorRecord>();
        private readonly List<Job> _allJobsToResume = new List<Job>();
        private readonly object _syncObject = new object();
        private bool _needToCheckForWaitingJobs;

        private void HandleResumeJobCompleted(object sender, AsyncCompletedEventArgs eventArgs)
        {
            Job job = sender as Job;

            if (eventArgs.Error != null && eventArgs.Error is InvalidJobStateException)
            {
                _warnInvalidState = true;
            }

            var parentJob = job as ContainerParentJob;
            if (parentJob != null && parentJob.ExecutionError.Count > 0)
            {
                foreach (
                    var e in
                        parentJob.ExecutionError.Where(e => e.FullyQualifiedErrorId == "ContainerParentJobResumeAsyncError")
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

                parentJob.ExecutionError.Clear();
            }

            bool releaseWait = false;
            lock (_syncObject)
            {
                if (_pendingJobs.Contains(job.InstanceId))
                {
                    _pendingJobs.Remove(job.InstanceId);
                }

                if (_needToCheckForWaitingJobs && _pendingJobs.Count == 0)
                    releaseWait = true;
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
            bool jobsPending = false;
            lock (_syncObject)
            {
                _needToCheckForWaitingJobs = true;
                if (_pendingJobs.Count > 0)
                    jobsPending = true;
            }

            if (Wait && jobsPending)
                _waitForJobs.WaitOne();

            if (_warnInvalidState) WriteWarning(RemotingErrorIdStrings.ResumeJobInvalidJobState);
            foreach (var e in _errorsToWrite) WriteError(e);
            foreach (var j in _allJobsToResume) WriteObject(j);
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
                pair.Key.ResumeJobCompleted -= pair.Value;
            }

            _waitForJobs.Dispose();
        }
        #endregion Dispose
    }
}
