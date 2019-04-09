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
    /// This cmdlet stops the asynchronously invoked remote operations.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Job", SupportsShouldProcess = true, DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113413")]
    [OutputType(typeof(Job))]
    public class StopJobCommand : JobCmdletBase, IDisposable
    {
        #region Parameters
        /// <summary>
        /// Specifies the Jobs objects which need to be
        /// removed.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = RemoveJobCommand.JobParameterSet)]
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
        /// Pass the Job object through the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        /// <summary>
        /// </summary>
        public override string[] Command
        {
            get
            {
                return null;
            }
        }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Stop the Job.
        /// </summary>
        protected override void ProcessRecord()
        {
            // List of jobs to stop
            List<Job> jobsToStop = null;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    {
                        jobsToStop = FindJobsMatchingByName(true, false, true, false);
                    }

                    break;

                case InstanceIdParameterSet:
                    {
                        jobsToStop = FindJobsMatchingByInstanceId(true, false, true, false);
                    }

                    break;

                case SessionIdParameterSet:
                    {
                        jobsToStop = FindJobsMatchingBySessionId(true, false, true, false);
                    }

                    break;

                case StateParameterSet:
                    {
                        jobsToStop = FindJobsMatchingByState(false);
                    }

                    break;

                case FilterParameterSet:
                    {
                        jobsToStop = FindJobsMatchingByFilter(false);
                    }

                    break;

                default:
                    {
                        jobsToStop = CopyJobsToList(_jobs, false, false);
                    }

                    break;
            }

            _allJobsToStop.AddRange(jobsToStop);

            foreach (Job job in jobsToStop)
            {
                if (this.Stopping) return;
                if (job.IsFinishedState(job.JobStateInfo.State))
                {
                    continue;
                }

                string targetString =
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemovePSJobWhatIfTarget,
                                                                   job.Command, job.Id);
                if (ShouldProcess(targetString, VerbsLifecycle.Stop))
                {
                    Job2 job2 = job as Job2;
                    // if it is a Job2, then async is supported
                    // stop the job asynchronously
                    if (job2 != null)
                    {
                        _cleanUpActions.Add(job2, HandleStopJobCompleted);
                        job2.StopJobCompleted += HandleStopJobCompleted;

                        lock (_syncObject)
                        {
                            if (!job2.IsFinishedState(job2.JobStateInfo.State) &&
                                !_pendingJobs.Contains(job2.InstanceId))
                            {
                                _pendingJobs.Add(job2.InstanceId);
                            }
                        }

                        job2.StopJobAsync();
                    }
                    else
                    {
                        job.StopJob();
                    }
                }
            }
        }

        /// <summary>
        /// Wait for all the stop jobs to be completed.
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

            foreach (var e in _errorsToWrite) WriteError(e);
            if (_passThru)
            {
                foreach (var job in _allJobsToStop) WriteObject(job);
            }
        }

        /// <summary>
        /// </summary>
        protected override void StopProcessing()
        {
            _waitForJobs.Set();
        }

        #endregion Overrides

        #region Private Methods

        private void HandleStopJobCompleted(object sender, AsyncCompletedEventArgs eventArgs)
        {
            Job job = sender as Job;

            if (eventArgs.Error != null)
            {
                _errorsToWrite.Add(new ErrorRecord(eventArgs.Error, "StopJobError", ErrorCategory.ReadError, job));
            }

            var parentJob = job as ContainerParentJob;
            if (parentJob != null && parentJob.ExecutionError.Count > 0)
            {
                foreach (
                    var e in
                        parentJob.ExecutionError.Where(
                            e => e.FullyQualifiedErrorId == "ContainerParentJobStopAsyncError"))
                {
                    _errorsToWrite.Add(e);
                }
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

        #endregion Private Methods

        #region Private Members

        private readonly HashSet<Guid> _pendingJobs = new HashSet<Guid>();
        private readonly ManualResetEvent _waitForJobs = new ManualResetEvent(false);
        private readonly Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>> _cleanUpActions =
            new Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>>();

        private readonly List<Job> _allJobsToStop = new List<Job>();
        private readonly List<ErrorRecord> _errorsToWrite = new List<ErrorRecord>();

        private readonly object _syncObject = new object();
        private bool _needToCheckForWaitingJobs;

        #endregion Private Members

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
                pair.Key.StopJobCompleted -= pair.Value;
            }

            _waitForJobs.Dispose();
        }
        #endregion Dispose
    }
}
