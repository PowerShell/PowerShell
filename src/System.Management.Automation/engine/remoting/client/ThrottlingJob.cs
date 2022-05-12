// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Remoting.Internal;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal abstract class StartableJob : Job
    {
        internal StartableJob(string commandName, string jobName)
            : base(commandName, jobName)
        {
        }

        internal abstract void StartJob();
    }

    /// <summary>
    /// A job that can throttle execution of child jobs.
    /// </summary>
    internal sealed class ThrottlingJob : Job
    {
        #region IDisposable Members

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.StopJob();

                    List<Job> childJobsToDispose;
                    lock (_lockObject)
                    {
                        Dbg.Assert(this.IsFinishedState(this.JobStateInfo.State), "ThrottlingJob should be completed before removing and disposing child jobs");
                        childJobsToDispose = new List<Job>(this.ChildJobs);
                        this.ChildJobs.Clear();
                    }

                    foreach (Job childJob in childJobsToDispose)
                    {
                        childJob.Dispose();
                    }

                    if (_jobResultsThrottlingSemaphore != null)
                    {
                        _jobResultsThrottlingSemaphore.Dispose();
                    }

                    _cancellationTokenSource.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Support for progress reporting

        private readonly DateTime _progressStartTime = DateTime.UtcNow;
        private readonly int _progressActivityId;
        private readonly object _progressLock = new object();
        private DateTime _progressReportLastTime = DateTime.MinValue;

        internal int GetProgressActivityId()
        {
            lock (_progressLock)
            {
                if (_progressReportLastTime.Equals(DateTime.MinValue))
                {
                    try
                    {
                        this.ReportProgress(minimizeFrequentUpdates: false);
                        Dbg.Assert(_progressReportLastTime > DateTime.MinValue, "Progress was reported (lastTimeProgressWasReported)");
                    }
                    catch (PSInvalidOperationException)
                    {
                        // return "no parent activity id" if this ThrottlingJob has already finished
                        return -1;
                    }
                }

                return _progressActivityId;
            }
        }

        private void ReportProgress(bool minimizeFrequentUpdates)
        {
            lock (_progressLock)
            {
                DateTime now = DateTime.UtcNow;

                if (minimizeFrequentUpdates)
                {
                    if ((now - _progressStartTime) < TimeSpan.FromSeconds(1))
                    {
                        return;
                    }

                    if ((!_progressReportLastTime.Equals(DateTime.MinValue)) &&
                        (now - _progressReportLastTime < TimeSpan.FromMilliseconds(200)))
                    {
                        return;
                    }
                }

                _progressReportLastTime = now;

                double workCompleted;
                double totalWork;
                int percentComplete;
                lock (_lockObject)
                {
                    totalWork = _countOfAllChildJobs;
                    workCompleted = this.CountOfFinishedChildJobs;
                }

                if (totalWork >= 1.0)
                {
                    percentComplete = (int)(100.0 * workCompleted / totalWork);
                }
                else
                {
                    percentComplete = -1;
                }

                percentComplete = Math.Max(-1, Math.Min(100, percentComplete));

                var progressRecord = new ProgressRecord(
                    activityId: _progressActivityId,
                    activity: this.Command,
                    statusDescription: this.StatusMessage);

                if (this.IsThrottlingJobCompleted)
                {
                    if (_progressReportLastTime.Equals(DateTime.MinValue))
                    {
                        return;
                    }

                    progressRecord.RecordType = ProgressRecordType.Completed;
                    progressRecord.PercentComplete = 100;
                    progressRecord.SecondsRemaining = 0;
                }
                else
                {
                    progressRecord.RecordType = ProgressRecordType.Processing;
                    progressRecord.PercentComplete = percentComplete;
                    int? secondsRemaining = null;
                    if (percentComplete >= 0)
                    {
                        secondsRemaining = ProgressRecord.GetSecondsRemaining(_progressStartTime, (double)percentComplete / 100.0);
                    }

                    if (secondsRemaining.HasValue)
                    {
                        progressRecord.SecondsRemaining = secondsRemaining.Value;
                    }
                }

                this.WriteProgress(progressRecord);
            }
        }

        #endregion

        /// <summary>
        /// Flags of child jobs of a <see cref="ThrottlingJob"/>
        /// </summary>
        [Flags]
        internal enum ChildJobFlags
        {
            /// <summary>
            /// Child job doesn't have any special properties.
            /// </summary>
            None = 0,

            /// <summary>
            /// Child job can call <see cref="ThrottlingJob.AddChildJobWithoutBlocking"/> method
            /// or <see cref="ThrottlingJob.AddChildJobAndPotentiallyBlock(StartableJob, ChildJobFlags)"/>
            /// or <see cref="ThrottlingJob.AddChildJobAndPotentiallyBlock(Cmdlet, StartableJob, ChildJobFlags)"/>
            /// method
            /// of the <see cref="ThrottlingJob"/> instance it belongs to.
            /// </summary>
            CreatesChildJobs = 0x1,
        }

        private bool _ownerWontSubmitNewChildJobs = false;
        private readonly HashSet<Guid> _setOfChildJobsThatCanAddMoreChildJobs = new HashSet<Guid>();

        private bool IsEndOfChildJobs
        {
            get
            {
                lock (_lockObject)
                {
                    return _isStopping || (_ownerWontSubmitNewChildJobs && _setOfChildJobsThatCanAddMoreChildJobs.Count == 0);
                }
            }
        }

        private bool IsThrottlingJobCompleted
        {
            get
            {
                lock (_lockObject)
                {
                    return this.IsEndOfChildJobs && (_countOfAllChildJobs <= this.CountOfFinishedChildJobs);
                }
            }
        }

        private readonly bool _cmdletMode;

        private int _countOfAllChildJobs;

        private int _countOfBlockedChildJobs;

        private int _countOfFailedChildJobs;
        private int _countOfStoppedChildJobs;
        private int _countOfSuccessfullyCompletedChildJobs;

        private int CountOfFinishedChildJobs
        {
            get
            {
                lock (_lockObject)
                {
                    return _countOfFailedChildJobs + _countOfStoppedChildJobs + _countOfSuccessfullyCompletedChildJobs;
                }
            }
        }

        private int CountOfRunningOrReadyToRunChildJobs
        {
            get
            {
                lock (_lockObject)
                {
                    return _countOfAllChildJobs - this.CountOfFinishedChildJobs;
                }
            }
        }

        private readonly object _lockObject = new object();

        /// <summary>
        /// Creates a new <see cref="ThrottlingJob"/> object.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="jobName">Friendly name for the job object.</param>
        /// <param name="jobTypeName">Name describing job type.</param>
        /// <param name="maximumConcurrentChildJobs">
        /// The maximum number of child jobs that can be running at any given point in time.
        /// Passing 0 requests to turn off throttling (i.e. allow unlimited number of child jobs to run)
        /// </param>
        /// <param name="cmdletMode">
        /// <see langword="true"/> if this <see cref="ThrottlingJob"/> is used from a cmdlet invoked without -AsJob switch.
        /// <see langword="false"/> if this <see cref="ThrottlingJob"/> is used from a cmdlet invoked with -AsJob switch.
        ///
        /// If <paramref name="cmdletMode"/> is <see langword="true"/>, then
        /// memory can be managed more aggressively (for example ChildJobs can be discarded as soon as they complete)
        /// because the <see cref="ThrottlingJob"/> is not exposed to the end user.
        /// </param>
        internal ThrottlingJob(string command, string jobName, string jobTypeName, int maximumConcurrentChildJobs, bool cmdletMode)
            : base(command, jobName)
        {
            this.Results.BlockingEnumerator = true;
            _cmdletMode = cmdletMode;
            this.PSJobTypeName = jobTypeName;
            if (_cmdletMode)
            {
                _jobResultsThrottlingSemaphore = new SemaphoreSlim(ForwardingHelper.AggregationQueueMaxCapacity);
            }

            _progressActivityId = new Random(this.GetHashCode()).Next();

            this.SetupThrottlingQueue(maximumConcurrentChildJobs);
        }

        internal void AddChildJobAndPotentiallyBlock(
            StartableJob childJob,
            ChildJobFlags flags)
        {
            using (var jobGotEnqueued = new ManualResetEventSlim(initialState: false))
            {
                if (childJob == null) throw new ArgumentNullException(nameof(childJob));

                this.AddChildJobWithoutBlocking(childJob, flags, jobGotEnqueued.Set);
                jobGotEnqueued.Wait();
            }
        }

        internal void AddChildJobAndPotentiallyBlock(
            Cmdlet cmdlet,
            StartableJob childJob,
            ChildJobFlags flags)
        {
            using (var forwardingCancellation = new CancellationTokenSource())
            {
                if (childJob == null) throw new ArgumentNullException(nameof(childJob));

                this.AddChildJobWithoutBlocking(childJob, flags, forwardingCancellation.Cancel);
                this.ForwardAllResultsToCmdlet(cmdlet, forwardingCancellation.Token);
            }
        }

        private bool _alreadyDisabledFlowControlForPendingJobsQueue = false;

        internal void DisableFlowControlForPendingJobsQueue()
        {
            if (!_cmdletMode || _alreadyDisabledFlowControlForPendingJobsQueue)
            {
                return;
            }

            _alreadyDisabledFlowControlForPendingJobsQueue = true;

            lock (_lockObject)
            {
                _maxReadyToRunJobs = int.MaxValue;

                while (_actionsForUnblockingChildAdditions.Count > 0)
                {
                    Action a = _actionsForUnblockingChildAdditions.Dequeue();
                    a?.Invoke();
                }
            }
        }

        private bool _alreadyDisabledFlowControlForPendingCmdletActionsQueue = false;

        internal void DisableFlowControlForPendingCmdletActionsQueue()
        {
            if (!_cmdletMode || _alreadyDisabledFlowControlForPendingCmdletActionsQueue)
            {
                return;
            }

            _alreadyDisabledFlowControlForPendingCmdletActionsQueue = true;

            long slotsToRelease = (long)(int.MaxValue / 2) - (long)(_jobResultsThrottlingSemaphore.CurrentCount);
            if ((slotsToRelease > 0) && (slotsToRelease < int.MaxValue))
            {
                _jobResultsThrottlingSemaphore.Release((int)slotsToRelease);
            }
        }

        /// <summary>
        /// Adds and starts a child job.
        /// </summary>
        /// <param name="childJob">Child job to add.</param>
        /// <param name="flags">Flags of the child job.</param>
        /// <param name="jobEnqueuedAction">Action to run after enqueuing the job.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the child job is not in the <see cref="JobState.NotStarted"/> state.
        /// (because this can lead to race conditions - the child job can finish before the parent job has a chance to register for child job events)
        /// </exception>
        internal void AddChildJobWithoutBlocking(StartableJob childJob, ChildJobFlags flags, Action jobEnqueuedAction = null)
        {
            if (childJob == null) throw new ArgumentNullException(nameof(childJob));
            if (childJob.JobStateInfo.State != JobState.NotStarted) throw new ArgumentException(RemotingErrorIdStrings.ThrottlingJobChildAlreadyRunning, nameof(childJob));
            this.AssertNotDisposed();

            JobStateInfo newJobStateInfo = null;
            lock (_lockObject)
            {
                if (this.IsEndOfChildJobs) throw new InvalidOperationException(RemotingErrorIdStrings.ThrottlingJobChildAddedAfterEndOfChildJobs);
                if (_isStopping) { return; }

                if (_countOfAllChildJobs == 0)
                {
                    newJobStateInfo = new JobStateInfo(JobState.Running);
                }

                if ((ChildJobFlags.CreatesChildJobs & flags) == ChildJobFlags.CreatesChildJobs)
                {
                    _setOfChildJobsThatCanAddMoreChildJobs.Add(childJob.InstanceId);
                }

                this.ChildJobs.Add(childJob);
                _childJobLocations.Add(childJob.Location);
                _countOfAllChildJobs++;

                this.WriteWarningAboutHighUsageOfFlowControlBuffers(this.CountOfRunningOrReadyToRunChildJobs);
                if (this.CountOfRunningOrReadyToRunChildJobs > _maxReadyToRunJobs)
                {
                    _actionsForUnblockingChildAdditions.Enqueue(jobEnqueuedAction);
                }
                else
                {
                    jobEnqueuedAction?.Invoke();
                }
            }

            if (newJobStateInfo != null)
            {
                this.SetJobState(newJobStateInfo.State, newJobStateInfo.Reason);
            }

            this.ChildJobAdded.SafeInvoke(this, new ThrottlingJobChildAddedEventArgs(childJob));

            childJob.SetParentActivityIdGetter(this.GetProgressActivityId);
            childJob.StateChanged += this.childJob_StateChanged;
            if (_cmdletMode)
            {
                childJob.Results.DataAdded += childJob_ResultsAdded;
            }

            this.EnqueueReadyToRunChildJob(childJob);

            this.ReportProgress(minimizeFrequentUpdates: true);
        }

        private void childJob_ResultsAdded(object sender, DataAddedEventArgs e)
        {
            Dbg.Assert(_jobResultsThrottlingSemaphore != null, "JobResultsThrottlingSemaphore should be non-null if childJob_ResultsAdded handled is registered");
            try
            {
                long jobResultsUpdatedCount = Interlocked.Increment(ref _jobResultsCurrentCount);
                this.WriteWarningAboutHighUsageOfFlowControlBuffers(jobResultsUpdatedCount);

                _jobResultsThrottlingSemaphore.Wait(_cancellationTokenSource.Token);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }

        private readonly object _alreadyWroteFlowControlBuffersHighMemoryUsageWarningLock = new object();
        private bool _alreadyWroteFlowControlBuffersHighMemoryUsageWarning;

        private const long FlowControlBuffersHighMemoryUsageThreshold = 30000;

        private void WriteWarningAboutHighUsageOfFlowControlBuffers(long currentCount)
        {
            if (!_cmdletMode)
            {
                return;
            }

            if (currentCount < FlowControlBuffersHighMemoryUsageThreshold)
            {
                return;
            }

            lock (_alreadyWroteFlowControlBuffersHighMemoryUsageWarningLock)
            {
                if (_alreadyWroteFlowControlBuffersHighMemoryUsageWarning)
                {
                    return;
                }

                _alreadyWroteFlowControlBuffersHighMemoryUsageWarning = true;
            }

            string warningMessage = string.Format(
                CultureInfo.InvariantCulture,
                RemotingErrorIdStrings.ThrottlingJobFlowControlMemoryWarning,
                this.Command);
            this.WriteWarning(warningMessage);
        }

        internal event EventHandler<ThrottlingJobChildAddedEventArgs> ChildJobAdded;

        private int _maximumConcurrentChildJobs;
        private int _extraCapacityForRunningQueryJobs;
        private int _extraCapacityForRunningAllJobs;
        private bool _inBoostModeToPreventQueryJobDeadlock;
        private Queue<StartableJob> _readyToRunQueryJobs;
        private Queue<StartableJob> _readyToRunRegularJobs;
        private Queue<Action> _actionsForUnblockingChildAdditions;
        private int _maxReadyToRunJobs;
        private readonly SemaphoreSlim _jobResultsThrottlingSemaphore;
        private long _jobResultsCurrentCount;

        private static readonly int s_maximumReadyToRunJobs = 10000;

        private void SetupThrottlingQueue(int maximumConcurrentChildJobs)
        {
            _maximumConcurrentChildJobs = maximumConcurrentChildJobs > 0 ? maximumConcurrentChildJobs : int.MaxValue;
            if (_cmdletMode)
            {
                _maxReadyToRunJobs = s_maximumReadyToRunJobs;
            }
            else
            {
                _maxReadyToRunJobs = int.MaxValue;
            }

            _extraCapacityForRunningAllJobs = _maximumConcurrentChildJobs;
            _extraCapacityForRunningQueryJobs = Math.Max(1, _extraCapacityForRunningAllJobs / 2);

            _inBoostModeToPreventQueryJobDeadlock = false;
            _readyToRunQueryJobs = new Queue<StartableJob>();
            _readyToRunRegularJobs = new Queue<StartableJob>();
            _actionsForUnblockingChildAdditions = new Queue<Action>();
        }

        private void StartChildJobIfPossible()
        {
            StartableJob readyToRunChildJob = null;
            lock (_lockObject)
            {
                do
                {
                    if ((_readyToRunQueryJobs.Count > 0) &&
                        (_extraCapacityForRunningQueryJobs > 0) &&
                        (_extraCapacityForRunningAllJobs > 0))
                    {
                        _extraCapacityForRunningQueryJobs--;
                        _extraCapacityForRunningAllJobs--;
                        readyToRunChildJob = _readyToRunQueryJobs.Dequeue();
                        break;
                    }

                    if ((_readyToRunRegularJobs.Count > 0) &&
                        (_extraCapacityForRunningAllJobs > 0))
                    {
                        _extraCapacityForRunningAllJobs--;
                        readyToRunChildJob = _readyToRunRegularJobs.Dequeue();
                        break;
                    }
                } while (false);
            }

            if (readyToRunChildJob != null)
            {
                readyToRunChildJob.StartJob();
            }
        }

        private void EnqueueReadyToRunChildJob(StartableJob childJob)
        {
            lock (_lockObject)
            {
                bool isQueryJob = _setOfChildJobsThatCanAddMoreChildJobs.Contains(childJob.InstanceId);
                if (isQueryJob &&
                    !_inBoostModeToPreventQueryJobDeadlock &&
                    (_maximumConcurrentChildJobs == 1))
                {
                    _inBoostModeToPreventQueryJobDeadlock = true;
                    _extraCapacityForRunningAllJobs++;
                }

                if (isQueryJob)
                {
                    _readyToRunQueryJobs.Enqueue(childJob);
                }
                else
                {
                    _readyToRunRegularJobs.Enqueue(childJob);
                }
            }

            StartChildJobIfPossible();
        }

        private void MakeRoomForRunningOtherJobs(Job completedChildJob)
        {
            lock (_lockObject)
            {
                _extraCapacityForRunningAllJobs++;

                bool isQueryJob = _setOfChildJobsThatCanAddMoreChildJobs.Contains(completedChildJob.InstanceId);
                if (isQueryJob)
                {
                    _setOfChildJobsThatCanAddMoreChildJobs.Remove(completedChildJob.InstanceId);

                    _extraCapacityForRunningQueryJobs++;
                    if (_inBoostModeToPreventQueryJobDeadlock && (_setOfChildJobsThatCanAddMoreChildJobs.Count == 0))
                    {
                        _inBoostModeToPreventQueryJobDeadlock = false;
                        _extraCapacityForRunningAllJobs--;
                    }
                }
            }

            StartChildJobIfPossible();
        }

        private void FigureOutIfThrottlingJobIsCompleted()
        {
            JobStateInfo finalJobStateInfo = null;
            lock (_lockObject)
            {
                if (this.IsThrottlingJobCompleted && !IsFinishedState(this.JobStateInfo.State))
                {
                    if (_isStopping)
                    {
                        finalJobStateInfo = new JobStateInfo(JobState.Stopped, null);
                    }
                    else if (_countOfFailedChildJobs > 0)
                    {
                        finalJobStateInfo = new JobStateInfo(JobState.Failed, null);
                    }
                    else if (_countOfStoppedChildJobs > 0)
                    {
                        finalJobStateInfo = new JobStateInfo(JobState.Stopped, null);
                    }
                    else
                    {
                        finalJobStateInfo = new JobStateInfo(JobState.Completed);
                    }
                }
            }

            if (finalJobStateInfo != null)
            {
                this.SetJobState(finalJobStateInfo.State, finalJobStateInfo.Reason);
                this.CloseAllStreams();
            }
        }

        /// <summary>
        /// Notifies this <see cref="ThrottlingJob"/> object that no more child jobs will be added.
        /// </summary>
        internal void EndOfChildJobs()
        {
            this.AssertNotDisposed();
            lock (_lockObject)
            {
                _ownerWontSubmitNewChildJobs = true;
            }

            this.FigureOutIfThrottlingJobIsCompleted();
        }

        /// <summary>
        /// Stop this job object and all the <see cref="System.Management.Automation.Job.ChildJobs"/>.
        /// </summary>
        public override void StopJob()
        {
            List<Job> childJobsToStop = null;
            lock (_lockObject)
            {
                if (!(_isStopping || this.IsThrottlingJobCompleted))
                {
                    _isStopping = true;
                    childJobsToStop = this.GetChildJobsSnapshot();
                }
            }

            if (childJobsToStop != null)
            {
                this.SetJobState(JobState.Stopping);

                _cancellationTokenSource.Cancel();
                foreach (Job childJob in childJobsToStop)
                {
                    if (!childJob.IsFinishedState(childJob.JobStateInfo.State))
                    {
                        childJob.StopJob();
                    }
                }

                this.FigureOutIfThrottlingJobIsCompleted();
            }

            this.Finished.WaitOne();
        }

        private bool _isStopping;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private void childJob_StateChanged(object sender, JobStateEventArgs e)
        {
            Dbg.Assert(sender != null, "Only our internal implementation of Job should raise this event and it should make sure that sender != null");
            Dbg.Assert(sender is Job, "Only our internal implementation of Job should raise this event and it should make sure that sender is Job");
            var childJob = (Job)sender;

            if ((e.PreviousJobStateInfo.State == JobState.Blocked) && (e.JobStateInfo.State != JobState.Blocked))
            {
                bool parentJobGotUnblocked = false;
                lock (_lockObject)
                {
                    _countOfBlockedChildJobs--;
                    if (_countOfBlockedChildJobs == 0)
                    {
                        parentJobGotUnblocked = true;
                    }
                }

                if (parentJobGotUnblocked)
                {
                    this.SetJobState(JobState.Running);
                }
            }

            switch (e.JobStateInfo.State)
            {
                // intermediate states
                case JobState.Blocked:
                    lock (_lockObject)
                    {
                        _countOfBlockedChildJobs++;
                    }

                    this.SetJobState(JobState.Blocked);
                    break;

                // 3 finished states
                case JobState.Failed:
                case JobState.Stopped:
                case JobState.Completed:
                    childJob.StateChanged -= childJob_StateChanged;
                    this.MakeRoomForRunningOtherJobs(childJob);
                    lock (_lockObject)
                    {
                        if (e.JobStateInfo.State == JobState.Failed)
                        {
                            _countOfFailedChildJobs++;
                        }
                        else if (e.JobStateInfo.State == JobState.Stopped)
                        {
                            _countOfStoppedChildJobs++;
                        }
                        else if (e.JobStateInfo.State == JobState.Completed)
                        {
                            _countOfSuccessfullyCompletedChildJobs++;
                        }

                        if (_actionsForUnblockingChildAdditions.Count > 0)
                        {
                            Action a = _actionsForUnblockingChildAdditions.Dequeue();
                            a?.Invoke();
                        }

                        if (_cmdletMode)
                        {
                            foreach (PSStreamObject streamObject in childJob.Results.ReadAll())
                            {
                                this.Results.Add(streamObject);
                            }

                            this.ChildJobs.Remove(childJob);
                            _setOfChildJobsThatCanAddMoreChildJobs.Remove(childJob.InstanceId);
                            childJob.Dispose();
                        }
                    }

                    this.ReportProgress(minimizeFrequentUpdates: !this.IsThrottlingJobCompleted);
                    break;

                default:
                    // do nothing
                    break;
            }

            this.FigureOutIfThrottlingJobIsCompleted();
        }

        private List<Job> GetChildJobsSnapshot()
        {
            lock (_lockObject)
            {
                return new List<Job>(this.ChildJobs);
            }
        }

        /// <summary>
        /// Indicates if job has more data available.
        /// <see langword="true"/> if any of the child jobs have more data OR if <see cref="EndOfChildJobs"/> have not been called yet;
        /// <see langword="false"/> otherwise.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return this.GetChildJobsSnapshot().Any(static childJob => childJob.HasMoreData) || (this.Results.Count != 0);
            }
        }

        /// <summary>
        /// Comma-separated list of locations of <see cref="System.Management.Automation.Job.ChildJobs"/>.
        /// </summary>
        public override string Location
        {
            get
            {
                lock (_lockObject)
                {
                    return string.Join(", ", _childJobLocations);
                }
            }
        }

        private readonly HashSet<string> _childJobLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Status message associated with the Job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                int completedChildJobs;
                int totalChildJobs;
                lock (_lockObject)
                {
                    completedChildJobs = this.CountOfFinishedChildJobs;
                    totalChildJobs = _countOfAllChildJobs;
                }

                string totalChildJobsString = totalChildJobs.ToString(CultureInfo.CurrentCulture);
                if (!this.IsEndOfChildJobs)
                {
                    totalChildJobsString += "+";
                }

                return string.Format(
                    CultureInfo.CurrentUICulture,
                    RemotingErrorIdStrings.ThrottlingJobStatusMessage,
                    completedChildJobs,
                    totalChildJobsString);
            }
        }

        #region Forwarding results to a cmdlet

        internal override void ForwardAvailableResultsToCmdlet(Cmdlet cmdlet)
        {
            this.AssertNotDisposed();

            base.ForwardAvailableResultsToCmdlet(cmdlet);
            foreach (Job childJob in this.GetChildJobsSnapshot())
            {
                childJob.ForwardAvailableResultsToCmdlet(cmdlet);
            }
        }

        private sealed class ForwardingHelper : IDisposable
        {
            // This is higher than 1000 used in
            //      RxExtensionMethods+ToEnumerableObserver<T>.BlockingCollectionCapacity
            // and in
            //      RemoteDiscoveryHelper.BlockingCollectionCapacity
            // It needs to be higher, because the high value is used as an attempt to workaround the fact that
            // WSMan will timeout if an OnNext call blocks for more than X minutes.

            // This is a static field (instead of a constant) to make it possible to set through tests (and/or by customers if needed for a workaround)
            internal static readonly int AggregationQueueMaxCapacity = 10000;

            private readonly ThrottlingJob _throttlingJob;

            private readonly object _myLock;
            private readonly BlockingCollection<PSStreamObject> _aggregatedResults;
            private readonly HashSet<Job> _monitoredJobs;

            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            private bool _disposed;

            private ForwardingHelper(ThrottlingJob throttlingJob)
            {
                _throttlingJob = throttlingJob;

                _myLock = new object();
                _monitoredJobs = new HashSet<Job>();

                _aggregatedResults = new BlockingCollection<PSStreamObject>();
            }

            private void StartMonitoringJob(Job job)
            {
                lock (_myLock)
                {
                    if (_disposed || _stoppedMonitoringAllJobs)
                    {
                        return;
                    }

                    if (_monitoredJobs.Contains(job))
                    {
                        return;
                    }

                    _monitoredJobs.Add(job);

                    job.Results.DataAdded += this.MonitoredJobResults_DataAdded;
                    job.StateChanged += MonitoredJob_StateChanged;
                }

                this.AggregateJobResults(job.Results);
                this.CheckIfMonitoredJobIsComplete(job);
            }

            private void StopMonitoringJob(Job job)
            {
                lock (_myLock)
                {
                    if (_monitoredJobs.Contains(job))
                    {
                        job.Results.DataAdded -= this.MonitoredJobResults_DataAdded;
                        job.StateChanged -= this.MonitoredJob_StateChanged;
                        _monitoredJobs.Remove(job);
                    }
                }
            }

            private void AggregateJobResults(PSDataCollection<PSStreamObject> resultsCollection)
            {
                lock (_myLock)
                {
                    // try not to remove results from a job, unless it seems safe ...
                    if (_disposed || _stoppedMonitoringAllJobs || _aggregatedResults.IsAddingCompleted || _cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                }

                // ... and after removing the results via ReadAll, we have to make sure that we don't drop them ...
                foreach (var result in resultsCollection.ReadAll())
                {
                    bool successfullyAggregatedResult = false;
                    try
                    {
                        lock (_myLock)
                        {
                            // try not to remove results from a job, unless it seems safe ...
                            if (!(_disposed || _stoppedMonitoringAllJobs || _aggregatedResults.IsAddingCompleted || _cancellationTokenSource.IsCancellationRequested))
                            {
                                _aggregatedResults.Add(result, _cancellationTokenSource.Token);
                                successfullyAggregatedResult = true;
                            }
                        }
                    }
                    catch (Exception) // BlockingCollection.Add can throw undocumented exceptions - we cannot just catch InvalidOperationException
                    {
                    }

                    // ... so if _aggregatedResults is not accepting new results, we will store them in the throttling job
                    if (!successfullyAggregatedResult)
                    {
                        this.StopMonitoringJob(_throttlingJob);
                        try
                        {
                            _throttlingJob.Results.Add(result);
                        }
                        catch (InvalidOperationException)
                        {
                            Dbg.Assert(false, "ThrottlingJob.Results was already closed when trying to preserve results aggregated by ForwardingHelper");
                        }
                    }
                }
            }

            private void CancelForwarding()
            {
                _cancellationTokenSource.Cancel();
                lock (_myLock)
                {
                    Dbg.Assert(!_disposed, "CancelForwarding should be unregistered before ForwardingHelper gets disposed");
                    _aggregatedResults.CompleteAdding();
                }
            }

            private void CheckIfMonitoredJobIsComplete(Job job)
            {
                CheckIfMonitoredJobIsComplete(job, job.JobStateInfo.State);
            }

            private void CheckIfMonitoredJobIsComplete(Job job, JobState jobState)
            {
                if (job.IsFinishedState(jobState))
                {
                    lock (_myLock)
                    {
                        this.StopMonitoringJob(job);
                    }
                }
            }

            private void CheckIfThrottlingJobIsComplete()
            {
                if (_throttlingJob.IsThrottlingJobCompleted)
                {
                    List<PSDataCollection<PSStreamObject>> resultsToAggregate = new List<PSDataCollection<PSStreamObject>>();
                    lock (_myLock)
                    {
                        foreach (Job registeredJob in _monitoredJobs)
                        {
                            resultsToAggregate.Add(registeredJob.Results);
                        }

                        foreach (Job throttledJob in _throttlingJob.GetChildJobsSnapshot())
                        {
                            resultsToAggregate.Add(throttledJob.Results);
                        }

                        resultsToAggregate.Add(_throttlingJob.Results);
                    }

                    foreach (PSDataCollection<PSStreamObject> resultToAggregate in resultsToAggregate)
                    {
                        this.AggregateJobResults(resultToAggregate);
                    }

                    lock (_myLock)
                    {
                        if (!_disposed && !_aggregatedResults.IsAddingCompleted)
                        {
                            _aggregatedResults.CompleteAdding();
                        }
                    }
                }
            }

            private void MonitoredJobResults_DataAdded(object sender, DataAddedEventArgs e)
            {
                var resultsCollection = (PSDataCollection<PSStreamObject>)sender;
                this.AggregateJobResults(resultsCollection);
            }

            private void MonitoredJob_StateChanged(object sender, JobStateEventArgs e)
            {
                var job = (Job)sender;
                this.CheckIfMonitoredJobIsComplete(job, e.JobStateInfo.State);
            }

            private void ThrottlingJob_ChildJobAdded(object sender, ThrottlingJobChildAddedEventArgs e)
            {
                this.StartMonitoringJob(e.AddedChildJob);
            }

            private void ThrottlingJob_StateChanged(object sender, JobStateEventArgs e)
            {
                this.CheckIfThrottlingJobIsComplete();
            }

            private void AttemptToPreserveAggregatedResults()
            {
#if DEBUG
                lock (_myLock)
                {
                    Dbg.Assert(!_disposed, "AttemptToPreserveAggregatedResults should be called before disposing ForwardingHelper");
                    Dbg.Assert(_stoppedMonitoringAllJobs, "Caller should guarantee no-more-results before calling AttemptToPreserveAggregatedResults (1)");
                    Dbg.Assert(_aggregatedResults.IsAddingCompleted, "Caller should guarantee no-more-results before calling AttemptToPreserveAggregatedResults (2)");
                }
#endif

                bool isThrottlingJobFinished = false;
                foreach (var aggregatedButNotYetProcessedResult in _aggregatedResults)
                {
                    if (!isThrottlingJobFinished)
                    {
                        try
                        {
                            _throttlingJob.Results.Add(aggregatedButNotYetProcessedResult);
                        }
                        catch (PSInvalidOperationException)
                        {
                            isThrottlingJobFinished = _throttlingJob.IsFinishedState(_throttlingJob.JobStateInfo.State);
                            Dbg.Assert(isThrottlingJobFinished, "Buffers should not be closed before throttling job is stopped");
                        }
                    }
                }
            }

#if DEBUG
            // CDXML_CLIXML_TEST testability hook

            private static readonly bool s_isCliXmlTestabilityHookActive = GetIsCliXmlTestabilityHookActive();

            private static bool GetIsCliXmlTestabilityHookActive()
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CDXML_CLIXML_TEST"));
            }

            internal static void ProcessCliXmlTestabilityHook(PSStreamObject streamObject)
            {
                if (!s_isCliXmlTestabilityHookActive)
                {
                    return;
                }

                if (streamObject.ObjectType != PSStreamObjectType.Output)
                {
                    return;
                }

                if (streamObject.Value == null)
                {
                    return;
                }

                if (!(PSObject.AsPSObject(streamObject.Value).BaseObject.GetType().Name.Equals("CimInstance")))
                {
                    return;
                }

                string serializedForm = PSSerializer.Serialize(streamObject.Value, depth: 1);
                object deserializedObject = PSSerializer.Deserialize(serializedForm);
                streamObject.Value = PSObject.AsPSObject(deserializedObject).BaseObject;
            }
#endif

            private void ForwardResults(Cmdlet cmdlet)
            {
                try
                {
                    foreach (var result in _aggregatedResults.GetConsumingEnumerable(_throttlingJob._cancellationTokenSource.Token))
                    {
                        if (result != null)
                        {
#if DEBUG
                            // CDXML_CLIXML_TEST testability hook
                            ProcessCliXmlTestabilityHook(result);
#endif
                            try
                            {
                                result.WriteStreamObject(cmdlet);
                            }
                            finally
                            {
                                if (_throttlingJob._cmdletMode)
                                {
                                    Dbg.Assert(_throttlingJob._jobResultsThrottlingSemaphore != null, "JobResultsThrottlingSemaphore should be present in cmdlet mode");
                                    Interlocked.Decrement(ref _throttlingJob._jobResultsCurrentCount);
                                    _throttlingJob._jobResultsThrottlingSemaphore.Release();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    this.StopMonitoringAllJobs();
                    this.AttemptToPreserveAggregatedResults();

                    throw;
                }
            }

            private bool _stoppedMonitoringAllJobs;

            private void StopMonitoringAllJobs()
            {
                _cancellationTokenSource.Cancel();
                lock (_myLock)
                {
                    _stoppedMonitoringAllJobs = true;

                    List<Job> snapshotOfCurrentlyMonitoredJobs = _monitoredJobs.ToList();
                    foreach (Job monitoredJob in snapshotOfCurrentlyMonitoredJobs)
                    {
                        this.StopMonitoringJob(monitoredJob);
                    }

                    Dbg.Assert(_monitoredJobs.Count == 0, "No monitored jobs should be left after ForwardingHelper is disposed");

                    if (!_disposed && !_aggregatedResults.IsAddingCompleted)
                    {
                        _aggregatedResults.CompleteAdding();
                    }
                }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                _cancellationTokenSource.Cancel();
                lock (_myLock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    this.StopMonitoringAllJobs();
                    _aggregatedResults.Dispose();
                    _cancellationTokenSource.Dispose();
                    _disposed = true;
                }
            }

            public static void ForwardAllResultsToCmdlet(ThrottlingJob throttlingJob, Cmdlet cmdlet, CancellationToken? cancellationToken)
            {
                using (var helper = new ForwardingHelper(throttlingJob))
                {
                    try
                    {
                        throttlingJob.ChildJobAdded += helper.ThrottlingJob_ChildJobAdded;

                        try
                        {
                            throttlingJob.StateChanged += helper.ThrottlingJob_StateChanged;

                            IDisposable cancellationTokenRegistration = null;
                            if (cancellationToken.HasValue)
                            {
                                cancellationTokenRegistration = cancellationToken.Value.Register(helper.CancelForwarding);
                            }

                            try
                            {
                                Interlocked.MemoryBarrier();
                                ThreadPool.QueueUserWorkItem(
                                        delegate
                                        {
                                            helper.StartMonitoringJob(throttlingJob);
                                            foreach (Job childJob in throttlingJob.GetChildJobsSnapshot())
                                            {
                                                helper.StartMonitoringJob(childJob);
                                            }

                                            helper.CheckIfThrottlingJobIsComplete();
                                        });

                                helper.ForwardResults(cmdlet);
                            }
                            finally
                            {
                                if (cancellationTokenRegistration != null)
                                {
                                    cancellationTokenRegistration.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            throttlingJob.StateChanged -= helper.ThrottlingJob_StateChanged;
                        }
                    }
                    finally
                    {
                        throttlingJob.ChildJobAdded -= helper.ThrottlingJob_ChildJobAdded;
                    }
                }
            }
        }

        internal override void ForwardAllResultsToCmdlet(Cmdlet cmdlet)
        {
            this.ForwardAllResultsToCmdlet(cmdlet, cancellationToken: null);
        }

        private void ForwardAllResultsToCmdlet(Cmdlet cmdlet, CancellationToken? cancellationToken)
        {
            this.AssertNotDisposed();
            ForwardingHelper.ForwardAllResultsToCmdlet(this, cmdlet, cancellationToken);
        }

        #endregion Forwarding results to a cmdlet
    }

    internal class ThrottlingJobChildAddedEventArgs : EventArgs
    {
        internal Job AddedChildJob { get; }

        internal ThrottlingJobChildAddedEventArgs(Job addedChildJob)
        {
            Dbg.Assert(addedChildJob != null, "Caller should verify addedChildJob != null");
            AddedChildJob = addedChildJob;
        }
    }
}
