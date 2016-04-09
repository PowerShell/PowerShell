//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet waits for job to complete.
    /// </summary>
    [Cmdlet("Wait", "Job", DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113422")]
    [OutputType(typeof(Job))]
    public class WaitJobCommand : JobCmdletBase, IDisposable
    {
        #region Parameters

        /// <summary>
        /// Specifies the Jobs objects which need to be
        /// removed
        /// </summary>
        [Parameter(Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = RemoveJobCommand.JobParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Job[] Job { get; set; }

        /// <summary>
        /// Complete the cmdlet when any of the job is completed, instead of waiting for all of them to be completed.
        /// </summary>
        [Parameter]
        public SwitchParameter Any { get; set; }

        /// <summary>
        /// If timeout is specified, the cmdlet will only wait for this number of seconds.
        /// Value of -1 means never timeout.
        /// </summary>
        [Parameter]
        [Alias("TimeoutSec")]
        [ValidateRangeAttribute(-1, Int32.MaxValue)]
        public int Timeout
        {
            get
            {
                return _timeoutInSeconds;
            }
            set
            {
                _timeoutInSeconds = value;
            }
        }
        int _timeoutInSeconds = -1; // -1: infinite, this default is to wait for as long as it takes.

        /// <summary>
        /// Forces the cmdlet to wait for Finished states (Completed, Failed, Stopped) instead of
        /// persistent states, which also include Suspended and Disconnected.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string[] Command { get; set; }
        #endregion Parameters

        #region Coordinating how different events (timeout, stopprocesing, job finished, job blocked) affect what happens in EndProcessing

        private readonly object _endProcessingActionLock = new object();
        private Action _endProcessingAction;
        private readonly ManualResetEventSlim _endProcessingActionIsReady = new ManualResetEventSlim(false);

        private void SetEndProcessingAction(Action endProcessingAction)
        {
            Dbg.Assert(endProcessingAction != null, "Caller should verify endProcessingAction != null");
            lock (this._endProcessingActionLock)
            {
                if (this._endProcessingAction == null)
                {
                    Dbg.Assert(!this._endProcessingActionIsReady.IsSet, "This line should execute only once");
                    this._endProcessingAction = endProcessingAction;
                    this._endProcessingActionIsReady.Set();
                }
            }
        }

        private void InvokeEndProcesingAction()
        {
            this._endProcessingActionIsReady.Wait();

            Action endProcessingAction;
            lock (this._endProcessingActionLock)
            {
                endProcessingAction = this._endProcessingAction;
            }

            // Inovke action outside lock.
            if (endProcessingAction != null)
            {
                endProcessingAction();
            }
        }

        private void CleanUpEndProcessing()
        {
            this._endProcessingActionIsReady.Dispose();
        }

        #endregion

        #region Support for triggering EndProcesing when jobs are finished or blocked
        
        private readonly HashSet<Job> _finishedJobs = new HashSet<Job>();
        private readonly HashSet<Job> _blockedJobs = new HashSet<Job>();
        private readonly List<Job> _jobsToWaitFor = new List<Job>();
        private readonly object _jobTrackingLock = new object();

        private void HandleJobStateChangedEvent(object source, JobStateEventArgs eventArgs)
        {
            Dbg.Assert(source is Job, "Caller should verify source is Job");
            Dbg.Assert(eventArgs != null, "Caller should verify eventArgs != null");

            var job = (Job) source;
            lock (_jobTrackingLock)
            {
                Dbg.Assert(this._blockedJobs.All(j => !this._finishedJobs.Contains(j)), "Job cannot be in *both* _blockedJobs and _finishedJobs");

                if (eventArgs.JobStateInfo.State == JobState.Blocked)
                {
                    this._blockedJobs.Add(job);
                } 
                else
                {
                    this._blockedJobs.Remove(job);
                } 
                
                // Treat jobs in Disconnected state as finished jobs since the user
                // will have to reconnect the job before more information can be
                // obtained.
                // Suspended jobs require a Resume-Job call. Both of these states are persistent
                // without user interaction.
                // Wait should wait until a job is in a persistent state, OR if the force parameter
                // is specified, until the job is in a finished state, which is a subset of
                // persistent states.
                if (!Force && job.IsPersistentState(eventArgs.JobStateInfo.State) || (Force && job.IsFinishedState(eventArgs.JobStateInfo.State)))
                {
                    if (!job.IsFinishedState(eventArgs.JobStateInfo.State))
                    {
                        _warnNotTerminal = true;
                    }
                    this._finishedJobs.Add(job);
                }
                else
                {
                    this._finishedJobs.Remove(job);
                }

                Dbg.Assert(this._blockedJobs.All(j => !this._finishedJobs.Contains(j)), "Job cannot be in *both* _blockedJobs and _finishedJobs");

                if (this.Any.IsPresent)
                {
                    if (this._finishedJobs.Count > 0)
                    {
                        this.SetEndProcessingAction(this.EndProcessingOutputSingleFinishedJob);
                    }
                    else if (this._blockedJobs.Count == this._jobsToWaitFor.Count)
                    {
                        this.SetEndProcessingAction(this.EndProcessingBlockedJobsError);
                    }
                }
                else
                {
                    if (this._finishedJobs.Count == this._jobsToWaitFor.Count)
                    {
                        this.SetEndProcessingAction(this.EndProcessingOutputAllFinishedJobs);
                    }
                    else if (this._blockedJobs.Count > 0)
                    {
                        this.SetEndProcessingAction(this.EndProcessingBlockedJobsError);
                    }
                }
            }
        }

        private void AddJobsThatNeedJobChangesTracking(IEnumerable<Job> jobsToAdd)
        {
            Dbg.Assert(jobsToAdd != null, "Caller should verify jobs != null");

            lock (this._jobTrackingLock)
            {
                this._jobsToWaitFor.AddRange(jobsToAdd);
            }
        }

        private void StartJobChangesTracking()
        {
            lock (this._jobTrackingLock)
            {
                if (this._jobsToWaitFor.Count == 0)
                {
                    this.SetEndProcessingAction(this.EndProcessingDoNothing);
                    return;
                }

                foreach (Job job in this._jobsToWaitFor)
                {
                    job.StateChanged += this.HandleJobStateChangedEvent;
                    this.HandleJobStateChangedEvent(job, new JobStateEventArgs(job.JobStateInfo));
                }
            }
        }

        private void CleanUpJobChangesTracking()
        {
            lock (this._jobTrackingLock)
            {
                foreach (Job job in this._jobsToWaitFor)
                {
                    job.StateChanged -= this.HandleJobStateChangedEvent;
                }
            }
        }

        private List<Job> GetFinishedJobs()
        {
            List<Job> jobsToOutput;
            lock (this._jobTrackingLock)
            {
                jobsToOutput = this._jobsToWaitFor.Where(j => ((!Force && j.IsPersistentState(j.JobStateInfo.State)) || (Force && j.IsFinishedState(j.JobStateInfo.State)))).ToList();
            }
            return jobsToOutput;
        }

        private Job GetOneBlockedJob()
        {
            lock (this._jobTrackingLock)
            {
                return this._jobsToWaitFor.FirstOrDefault(j => j.JobStateInfo.State == JobState.Blocked);
            }
        }

        #endregion

        #region Support for triggering EndProcessing when timing out

        private Timer _timer;
        private readonly object _timerLock = new object();

        private void StartTimeoutTracking(int timeoutInSeconds)
        {
            if (timeoutInSeconds == 0)
            {
                this.SetEndProcessingAction(this.EndProcessingDoNothing);
            }
            else if (timeoutInSeconds > 0)
            {
                lock (this._timerLock)
                {
                    this._timer = new Timer((_) => this.SetEndProcessingAction(this.EndProcessingDoNothing), null, timeoutInSeconds * 1000, System.Threading.Timeout.Infinite);
                }
            }
        }

        private void CleanUpTimoutTracking()
        {
            lock (this._timerLock)
            {
                if (this._timer != null)
                {
                    this._timer.Dispose();
                    this._timer = null;
                }
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Cancel the Wait-Job cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            this.SetEndProcessingAction(this.EndProcessingDoNothing);
        }

        /// <summary>
        /// In this method, we initialize the timer if timeout parameter is specified.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.StartTimeoutTracking(this._timeoutInSeconds);
        }

        /// <summary>
        /// This method just collects the Jobs which will be waited on in the EndProcessing method.
        /// </summary>
        protected override void ProcessRecord()
        {
            //List of jobs to wait
            List<Job> matches;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    matches = FindJobsMatchingByName(true, false, true, false);
                    break;

                case InstanceIdParameterSet:
                    matches = FindJobsMatchingByInstanceId(true, false, true, false);
                    break;

                case SessionIdParameterSet:
                    matches = FindJobsMatchingBySessionId(true, false, true, false);
                    break;

                case StateParameterSet:
                    matches = FindJobsMatchingByState(false);
                    break;

                case FilterParameterSet:
                    matches = FindJobsMatchingByFilter(false);
                    break;

                default:
                    matches = CopyJobsToList(this.Job, false, false);
                    break;
            }

            this.AddJobsThatNeedJobChangesTracking(matches);
        }

        /// <summary>
        /// Wait on the collected Jobs.
        /// </summary>
        protected override void EndProcessing()
        {
            this.StartJobChangesTracking();
            this.InvokeEndProcesingAction();
            if (_warnNotTerminal)
            {
                WriteWarning(RemotingErrorIdStrings.JobSuspendedDisconnectedWaitWithForce);
            }
        }

        private void EndProcessingOutputSingleFinishedJob()
        {
            Job finishedJob = this.GetFinishedJobs().FirstOrDefault();
            if (finishedJob != null)
            {
                this.WriteObject(finishedJob);
            }
        }

        private void EndProcessingOutputAllFinishedJobs()
        {
            IEnumerable<Job> finishedJobs = this.GetFinishedJobs();
            foreach (Job finishedJob in finishedJobs)
            {
                this.WriteObject(finishedJob);
            }
        }

        private void EndProcessingBlockedJobsError()
        {
            string message = RemotingErrorIdStrings.JobBlockedSoWaitJobCannotContinue;
            Exception exception = new ArgumentException(message);
            ErrorRecord errorRecord = new ErrorRecord(
                exception,
                "BlockedJobsDeadlockWithWaitJob",
                ErrorCategory.DeadlockDetected,
                this.GetOneBlockedJob());
            this.ThrowTerminatingError(errorRecord);
        }

        private void EndProcessingDoNothing()
        {
            // do nothing
        }

        #endregion Overrides

        #region IDisposable Members

        /// <summary>
        /// Dispose all managed resources. This will suppress finalizer on the object from getting called by
        /// calling System.GC.SuppressFinalize(this).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // To prevent derived types with finalizers from having to re-implement System.IDisposable to call it,
            // unsealed types without finalizers should still call SuppressFinalize.
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all the resources. 
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_disposableLock)
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;

                        this.CleanUpTimoutTracking();
                        this.CleanUpJobChangesTracking();
                        this.CleanUpEndProcessing(); // <- has to be last
                    }
                }
            }
        }

        private bool _isDisposed;
        private readonly object _disposableLock = new object();
        private bool _warnNotTerminal = false;

        #endregion IDisposable Members
    }
}
