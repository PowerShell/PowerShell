// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Internal;
using System.Threading;

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell.Cim;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Base class for all child jobs that wrap CIM operations.
    /// </summary>
    internal abstract class CimChildJobBase<T> :
        StartableJob,
        IObserver<T>
    {
        private static long s_globalJobNumberCounter;
        private readonly long _myJobNumber = Interlocked.Increment(ref s_globalJobNumberCounter);

        private const string CIMJobType = "CimJob";

        internal CimJobContext JobContext
        {
            get
            {
                return _jobContext;
            }
        }

        private readonly CimJobContext _jobContext;

        internal CimChildJobBase(CimJobContext jobContext)
            : base(Job.GetCommandTextFromInvocationInfo(jobContext.CmdletInvocationInfo), " " /* temporary name - reset below */)
        {
            _jobContext = jobContext;
            this.PSJobTypeName = CIMJobType;

            this.Name = this.GetType().Name + _myJobNumber.ToString(CultureInfo.InvariantCulture);
            UsesResultsCollection = true;

            lock (s_globalRandom)
            {
                _random = new Random(s_globalRandom.Next());
            }

            _jobSpecificCustomOptions = new Lazy<CimCustomOptionsDictionary>(this.CalculateJobSpecificCustomOptions);
        }

        private readonly CimSensitiveValueConverter _cimSensitiveValueConverter = new();

        internal CimSensitiveValueConverter CimSensitiveValueConverter { get { return _cimSensitiveValueConverter; } }

        internal abstract IObservable<T> GetCimOperation();

        public abstract void OnNext(T item);

        // copied from sdpublic\sdk\inc\wsmerror.h
        private enum WsManErrorCode : uint
        {
            ERROR_WSMAN_QUOTA_MAX_SHELLS = 0x803381A5,
            ERROR_WSMAN_QUOTA_MAX_OPERATIONS = 0x803381A6,
            ERROR_WSMAN_QUOTA_USER = 0x803381A7,
            ERROR_WSMAN_QUOTA_SYSTEM = 0x803381A8,
            ERROR_WSMAN_QUOTA_MAX_SHELLUSERS = 0x803381AB,
            ERROR_WSMAN_QUOTA_MAX_SHELLS_PPQ = 0x803381E4,
            ERROR_WSMAN_QUOTA_MAX_USERS_PPQ = 0x803381E5,
            ERROR_WSMAN_QUOTA_MAX_PLUGINSHELLS_PPQ = 0x803381E6,
            ERROR_WSMAN_QUOTA_MAX_PLUGINOPERATIONS_PPQ = 0x803381E7,
            ERROR_WSMAN_QUOTA_MAX_OPERATIONS_USER_PPQ = 0x803381E8,
            ERROR_WSMAN_QUOTA_MAX_COMMANDS_PER_SHELL_PPQ = 0x803381E9,
            ERROR_WSMAN_QUOTA_MIN_REQUIREMENT_NOT_AVAILABLE_PPQ = 0x803381EA,
        }

        private static bool IsWsManQuotaReached(Exception exception)
        {
            if (!(exception is CimException cimException))
            {
                return false;
            }

            if (cimException.NativeErrorCode != NativeErrorCode.ServerLimitsExceeded)
            {
                return false;
            }

            CimInstance cimError = cimException.ErrorData;
            if (cimError == null)
            {
                return false;
            }

            CimProperty errorCodeProperty = cimError.CimInstanceProperties["error_Code"];
            if (errorCodeProperty == null)
            {
                return false;
            }

            if (errorCodeProperty.CimType != CimType.UInt32)
            {
                return false;
            }

            WsManErrorCode wsManErrorCode = (WsManErrorCode)(uint)(errorCodeProperty.Value);
            switch (wsManErrorCode) // error codes that should result in sleep-and-retry are based on an email from Ryan
            {
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_SHELLS:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_OPERATIONS:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_USER:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_SYSTEM:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_SHELLUSERS:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_SHELLS_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_USERS_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_PLUGINSHELLS_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_PLUGINOPERATIONS_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_OPERATIONS_USER_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MAX_COMMANDS_PER_SHELL_PPQ:
                case WsManErrorCode.ERROR_WSMAN_QUOTA_MIN_REQUIREMENT_NOT_AVAILABLE_PPQ:
                    return true;

                default:
                    return false;
            }
        }

        public virtual void OnError(Exception exception)
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        if (IsWsManQuotaReached(exception))
                        {
                            this.SleepAndRetry();
                            return;
                        }

                        var cje = CimJobException.CreateFromAnyException(this.GetDescription(), this.JobContext, exception);
                        this.ReportJobFailure(cje);
                    });
        }

        public virtual void OnCompleted()
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        this.SetCompletedJobState(JobState.Completed, null);
                    });
        }

        private static readonly Random s_globalRandom = new();
        private readonly Random _random;
        private int _sleepAndRetryDelayRangeMs = 1000;
        private int _sleepAndRetryExtraDelayMs = 0;

        private const int MaxRetryDelayMs = 15 * 1000;
        private const int MinRetryDelayMs = 100;

        private Timer _sleepAndRetryTimer;

        private void SleepAndRetry_OnWakeup(object state)
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        lock (_jobStateLock)
                        {
                            if (_sleepAndRetryTimer != null)
                            {
                                _sleepAndRetryTimer.Dispose();
                                _sleepAndRetryTimer = null;
                            }

                            if (_jobWasStopped)
                            {
                                this.SetCompletedJobState(JobState.Stopped, null);
                                return;
                            }
                        }

                        this.StartJob();
                    });
        }

        private void SleepAndRetry()
        {
            int tmpRandomDelay = _random.Next(0, _sleepAndRetryDelayRangeMs);
            int delay = MinRetryDelayMs + _sleepAndRetryExtraDelayMs + tmpRandomDelay;
            _sleepAndRetryExtraDelayMs = _sleepAndRetryDelayRangeMs - tmpRandomDelay;
            if (_sleepAndRetryDelayRangeMs < MaxRetryDelayMs)
            {
                _sleepAndRetryDelayRangeMs *= 2;
            }

            string verboseMessage = string.Format(
                CultureInfo.InvariantCulture,
                CmdletizationResources.CimJob_SleepAndRetryVerboseMessage,
                this.JobContext.CmdletInvocationInfo.InvocationName,
                this.JobContext.Session.ComputerName ?? "localhost",
                delay / 1000.0);
            this.WriteVerbose(verboseMessage);

            lock (_jobStateLock)
            {
                if (_jobWasStopped)
                {
                    this.SetCompletedJobState(JobState.Stopped, null);
                }
                else
                {
                    Dbg.Assert(_sleepAndRetryTimer == null, "There should be only 1 active _sleepAndRetryTimer");
                    _sleepAndRetryTimer = new Timer(
                        state: null,
                        dueTime: delay,
                        period: Timeout.Infinite,
                        callback: SleepAndRetry_OnWakeup);
                }
            }
        }

        /// <summary>
        /// Indicates a location where this job is running.
        /// </summary>
        public override string Location
        {
            get
            {
                // this.JobContext is set in the constructor of CimChildJobBase,
                // but the constructor of Job wants to access Location property
                // before CimChildJobBase is fully initialized
                if (this.JobContext == null)
                {
                    return null;
                }

                string location = this.JobContext.Session.ComputerName ?? Environment.MachineName;
                return location;
            }
        }

        /// <summary>
        /// Status message associated with the Job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return this.JobStateInfo.State.ToString();
            }
        }

        /// <summary>
        /// Indicates if job has more data available.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (Results.IsOpen || Results.Count > 0);
            }
        }

        internal void WriteVerboseStartOfCimOperation()
        {
            if (this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.ClientSideWriteVerbose)
            {
                string verboseMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    CmdletizationResources.CimJob_VerboseExecutionMessage,
                    this.GetDescription());
                this.WriteVerbose(verboseMessage);
            }
        }

        internal override void StartJob()
        {
            lock (_jobStateLock)
            {
                if (_jobWasStopped)
                {
                    this.SetCompletedJobState(JobState.Stopped, null);
                    return;
                }

                Dbg.Assert(!_alreadyReachedCompletedState, "Job shouldn't reach completed state, before ThrottlingJob has a chance to register for job-completed/failed events");

                TerminatingErrorTracker tracker = TerminatingErrorTracker.GetTracker(this.JobContext.CmdletInvocationInfo);
                if (tracker.IsSessionTerminated(this.JobContext.Session))
                {
                    this.SetCompletedJobState(JobState.Failed, new OperationCanceledException());
                    return;
                }

                if (!_jobWasStarted)
                {
                    _jobWasStarted = true;
                    this.SetJobState(JobState.Running);
                }
            }

            // This invocation can block (i.e. by calling Job.ShouldProcess) and wait for pipeline thread to unblock it
            // Therefore we have to do the invocation outside of the pipeline thread.
            ThreadPool.QueueUserWorkItem(delegate
            {
                this.ExceptionSafeWrapper(delegate
                {
                    IObservable<T> observable = this.GetCimOperation();
                    observable?.Subscribe(this);
                });
            });
        }

        internal string GetDescription()
        {
            try
            {
                return this.Description;
            }
            catch (Exception)
            {
                return this.FailSafeDescription;
            }
        }

        internal abstract string Description { get; }

        internal abstract string FailSafeDescription { get; }

        internal void ExceptionSafeWrapper(Action action)
        {
            try
            {
                try
                {
                    Dbg.Assert(action != null, "Caller should verify action != null");
                    action();
                }
                catch (CimJobException e)
                {
                    this.ReportJobFailure(e);
                }
                catch (PSInvalidCastException e)
                {
                    this.ReportJobFailure(e);
                }
                catch (CimException e)
                {
                    var cje = CimJobException.CreateFromCimException(this.GetDescription(), this.JobContext, e);
                    this.ReportJobFailure(cje);
                }
                catch (PSInvalidOperationException)
                {
                    lock (_jobStateLock)
                    {
                        bool everythingIsOk = false;
                        if (_jobWasStopped)
                        {
                            everythingIsOk = true;
                        }

                        if (_alreadyReachedCompletedState && _jobHadErrors)
                        {
                            everythingIsOk = true;
                        }

                        if (!everythingIsOk)
                        {
                            Dbg.Assert(false, "PSInvalidOperationException should only happen in certain job states");
                            throw;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var cje = CimJobException.CreateFromAnyException(this.GetDescription(), this.JobContext, e);
                this.ReportJobFailure(cje);
            }
        }

        #region Operation options

        internal virtual string GetProviderVersionExpectedByJob()
        {
            return this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassVersion;
        }

        internal CimOperationOptions CreateOperationOptions()
        {
            var operationOptions = new CimOperationOptions(mustUnderstand: false)
            {
                CancellationToken = _cancellationTokenSource.Token,
                WriteProgress = this.WriteProgressCallback,
                WriteMessage = this.WriteMessageCallback,
                WriteError = this.WriteErrorCallback,
                PromptUser = this.PromptUserCallback,
            };

            operationOptions.SetOption("__MI_OPERATIONOPTIONS_IMPROVEDPERF_STREAMING", 1);

            operationOptions.Flags |= this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.SchemaConformanceLevel;

            if (this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.ResourceUri != null)
            {
                operationOptions.ResourceUri = this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.ResourceUri;
            }

            if ((
                  (_jobContext.WarningActionPreference == ActionPreference.SilentlyContinue) ||
                  (_jobContext.WarningActionPreference == ActionPreference.Ignore)
                ) && (!_jobContext.IsRunningInBackground))
            {
                operationOptions.DisableChannel((uint)MessageChannel.Warning);
            }
            else
            {
                operationOptions.EnableChannel((uint)MessageChannel.Warning);
            }

            if ((
                  (_jobContext.VerboseActionPreference == ActionPreference.SilentlyContinue) ||
                  (_jobContext.VerboseActionPreference == ActionPreference.Ignore)
                ) && (!_jobContext.IsRunningInBackground))
            {
                operationOptions.DisableChannel((uint)MessageChannel.Verbose);
            }
            else
            {
                operationOptions.EnableChannel((uint)MessageChannel.Verbose);
            }

            if ((
                  (_jobContext.DebugActionPreference == ActionPreference.SilentlyContinue) ||
                  (_jobContext.DebugActionPreference == ActionPreference.Ignore)
                ) && (!_jobContext.IsRunningInBackground))
            {
                operationOptions.DisableChannel((uint)MessageChannel.Debug);
            }
            else
            {
                operationOptions.EnableChannel((uint)MessageChannel.Debug);
            }

            switch (this.JobContext.ShouldProcessOptimization)
            {
                case MshCommandRuntime.ShouldProcessPossibleOptimization.AutoNo_CanCallShouldProcessAsynchronously:
                    operationOptions.SetPromptUserRegularMode(CimCallbackMode.Report, automaticConfirmation: false);
                    break;

                case MshCommandRuntime.ShouldProcessPossibleOptimization.AutoYes_CanCallShouldProcessAsynchronously:
                    operationOptions.SetPromptUserRegularMode(CimCallbackMode.Report, automaticConfirmation: true);
                    break;

                case MshCommandRuntime.ShouldProcessPossibleOptimization.AutoYes_CanSkipShouldProcessCall:
                    operationOptions.SetPromptUserRegularMode(CimCallbackMode.Ignore, automaticConfirmation: true);
                    break;

                case MshCommandRuntime.ShouldProcessPossibleOptimization.NoOptimizationPossible:
                default:
                    operationOptions.PromptUserMode = CimCallbackMode.Inquire;
                    break;
            }

            switch (this.JobContext.ErrorActionPreference)
            {
                case ActionPreference.Continue:
                case ActionPreference.SilentlyContinue:
                case ActionPreference.Ignore:
                    operationOptions.WriteErrorMode = CimCallbackMode.Report;
                    break;

                case ActionPreference.Stop:
                case ActionPreference.Inquire:
                default:
                    operationOptions.WriteErrorMode = CimCallbackMode.Inquire;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(this.GetProviderVersionExpectedByJob()))
            {
                CimOperationOptionsHelper.SetCustomOption(
                    operationOptions,
                    "MI_OPERATIONOPTIONS_PROVIDERVERSION",
                    this.GetProviderVersionExpectedByJob(),
                    CimSensitiveValueConverter);
            }

            if (this.JobContext.CmdletizationModuleVersion != null)
            {
                CimOperationOptionsHelper.SetCustomOption(
                    operationOptions,
                    "MI_OPERATIONOPTIONS_POWERSHELL_MODULEVERSION",
                    this.JobContext.CmdletizationModuleVersion,
                    CimSensitiveValueConverter);
            }

            CimOperationOptionsHelper.SetCustomOption(
                operationOptions,
                "MI_OPERATIONOPTIONS_POWERSHELL_CMDLETNAME",
                this.JobContext.CmdletInvocationInfo.MyCommand.Name,
                CimSensitiveValueConverter);
            if (!string.IsNullOrWhiteSpace(this.JobContext.Session.ComputerName))
            {
                CimOperationOptionsHelper.SetCustomOption(
                    operationOptions,
                    "MI_OPERATIONOPTIONS_POWERSHELL_COMPUTERNAME",
                    this.JobContext.Session.ComputerName,
                    CimSensitiveValueConverter);
            }

            CimCustomOptionsDictionary jobSpecificCustomOptions = this.GetJobSpecificCustomOptions();
            jobSpecificCustomOptions?.Apply(operationOptions, CimSensitiveValueConverter);

            return operationOptions;
        }

        private readonly Lazy<CimCustomOptionsDictionary> _jobSpecificCustomOptions;

        internal abstract CimCustomOptionsDictionary CalculateJobSpecificCustomOptions();

        private CimCustomOptionsDictionary GetJobSpecificCustomOptions()
        {
            return _jobSpecificCustomOptions.Value;
        }

        #endregion

        #region Controlling job state

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Stops this job.
        /// </summary>
        public override void StopJob()
        {
            lock (_jobStateLock)
            {
                if (_jobWasStopped || _alreadyReachedCompletedState)
                {
                    return;
                }

                _jobWasStopped = true;

                if (!_jobWasStarted)
                {
                    this.SetCompletedJobState(JobState.Stopped, null);
                }
                else if (_sleepAndRetryTimer != null)
                {
                    _sleepAndRetryTimer.Dispose();
                    _sleepAndRetryTimer = null;
                    this.SetCompletedJobState(JobState.Stopped, null);
                }
                else
                {
                    this.SetJobState(JobState.Stopping);
                }
            }

            _cancellationTokenSource.Cancel();
        }

        private readonly object _jobStateLock = new();
        private bool _jobHadErrors;
        private bool _jobWasStarted;
        private bool _jobWasStopped;
        private bool _alreadyReachedCompletedState;

        internal bool JobHadErrors
        {
            get
            {
                lock (_jobStateLock)
                {
                    return _jobHadErrors;
                }
            }
        }

        internal void ReportJobFailure(IContainsErrorRecord exception)
        {
            TerminatingErrorTracker terminatingErrorTracker = TerminatingErrorTracker.GetTracker(this.JobContext.CmdletInvocationInfo);

            bool sessionWasAlreadyTerminated = false;
            bool isThisTerminatingError = false;
            Exception brokenSessionException = null;
            lock (_jobStateLock)
            {
                if (!_jobWasStopped)
                {
                    brokenSessionException = terminatingErrorTracker.GetExceptionIfBrokenSession(
                        this.JobContext.Session,
                        this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.SkipTestConnection,
                        out sessionWasAlreadyTerminated);
                }
            }

            if (brokenSessionException != null)
            {
                string brokenSessionMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_BrokenSession,
                    brokenSessionException.Message);
                exception = CimJobException.CreateWithFullControl(
                    this.JobContext,
                    brokenSessionMessage,
                    "CimJob_BrokenCimSession",
                    ErrorCategory.ResourceUnavailable,
                    brokenSessionException);
                isThisTerminatingError = true;
            }
            else
            {
                CimJobException cje = exception as CimJobException;
                if ((cje != null) && (cje.IsTerminatingError))
                {
                    terminatingErrorTracker.MarkSessionAsTerminated(this.JobContext.Session, out sessionWasAlreadyTerminated);
                    isThisTerminatingError = true;
                }
            }

            bool writeError = !sessionWasAlreadyTerminated;
            if (writeError)
            {
                lock (_jobStateLock)
                {
                    if (_jobWasStopped)
                    {
                        writeError = false;
                    }
                }
            }

            ErrorRecord errorRecord = exception.ErrorRecord;
            errorRecord.SetInvocationInfo(this.JobContext.CmdletInvocationInfo);
            errorRecord.PreserveInvocationInfoOnce = true;

            if (writeError)
            {
                lock (_jobStateLock)
                {
                    if (!_alreadyReachedCompletedState)
                    {
                        if (isThisTerminatingError)
                        {
                            this.Error.Add(errorRecord);
                            CmdletMethodInvoker<bool> methodInvoker = terminatingErrorTracker.GetErrorReportingDelegate(errorRecord);
                            this.Results.Add(new PSStreamObject(PSStreamObjectType.ShouldMethod, methodInvoker));
                        }
                        else
                        {
                            this.WriteError(errorRecord);
                        }
                    }
                }
            }

            this.SetCompletedJobState(JobState.Failed, errorRecord.Exception);
        }

        internal override void WriteWarning(string message)
        {
            message = this.JobContext.PrependComputerNameToMessage(message);
            base.WriteWarning(message);
        }

        internal override void WriteVerbose(string message)
        {
            message = this.JobContext.PrependComputerNameToMessage(message);
            base.WriteVerbose(message);
        }

        internal override void WriteDebug(string message)
        {
            message = this.JobContext.PrependComputerNameToMessage(message);
            base.WriteDebug(message);
        }

        internal void SetCompletedJobState(JobState state, Exception reason)
        {
            lock (_jobStateLock)
            {
                if (_alreadyReachedCompletedState)
                {
                    return;
                }

                _alreadyReachedCompletedState = true;

                if ((state == JobState.Failed) || (reason != null))
                {
                    _jobHadErrors = true;
                }

                if (_jobWasStopped)
                {
                    state = JobState.Stopped;
                }
                else if (_jobHadErrors)
                {
                    state = JobState.Failed;
                }
            }

            this.FinishProgressReporting();
            this.SetJobState(state, reason);
            this.CloseAllStreams();
            _cancellationTokenSource.Cancel();
        }

        #endregion

        #region Support for progress reporting

        private readonly ConcurrentDictionary<int, ProgressRecord> _activityIdToLastProgressRecord = new();

        internal override void WriteProgress(ProgressRecord progressRecord)
        {
            progressRecord.Activity = this.JobContext.PrependComputerNameToMessage(progressRecord.Activity);

            _activityIdToLastProgressRecord.AddOrUpdate(
                progressRecord.ActivityId,
                progressRecord,
                (activityId, oldProgressRecord) => progressRecord);

            base.WriteProgress(progressRecord);
        }

        internal void FinishProgressReporting()
        {
            foreach (ProgressRecord lastProgressRecord in _activityIdToLastProgressRecord.Values)
            {
                if (lastProgressRecord.RecordType != ProgressRecordType.Completed)
                {
                    var newProgressRecord = new ProgressRecord(lastProgressRecord.ActivityId, lastProgressRecord.Activity, lastProgressRecord.StatusDescription);
                    newProgressRecord.RecordType = ProgressRecordType.Completed;
                    newProgressRecord.PercentComplete = 100;
                    newProgressRecord.SecondsRemaining = 0;
                    this.WriteProgress(newProgressRecord);
                }
            }
        }

        #endregion

        #region Handling extended semantics callbacks

        private void WriteProgressCallback(string activity, string currentOperation, string statusDescription, uint percentageCompleted, uint secondsRemaining)
        {
            if (string.IsNullOrEmpty(activity))
            {
                activity = this.GetDescription();
            }

            if (string.IsNullOrEmpty(statusDescription))
            {
                statusDescription = this.StatusMessage;
            }

            int signedSecondsRemaining;
            if (secondsRemaining == uint.MaxValue)
            {
                signedSecondsRemaining = -1;
            }
            else if (secondsRemaining <= int.MaxValue)
            {
                signedSecondsRemaining = (int)secondsRemaining;
            }
            else
            {
                signedSecondsRemaining = int.MaxValue;
            }

            int signedPercentageComplete;
            if (percentageCompleted == uint.MaxValue)
            {
                signedPercentageComplete = -1;
            }
            else if (percentageCompleted <= 100)
            {
                signedPercentageComplete = (int)percentageCompleted;
            }
            else
            {
                signedPercentageComplete = 100;
            }

            var progressRecord = new ProgressRecord(unchecked((int)(_myJobNumber % int.MaxValue)), activity, statusDescription)
            {
                CurrentOperation = currentOperation,
                PercentComplete = signedPercentageComplete,
                SecondsRemaining = signedSecondsRemaining,
                RecordType = ProgressRecordType.Processing,
            };

            this.ExceptionSafeWrapper(
                    delegate
                    {
                        this.WriteProgress(progressRecord);
                    });
        }

        private enum MessageChannel
        {
            Warning = 0,
            Verbose = 1,
            Debug = 2,
        }

        private void WriteMessageCallback(uint channel, string message)
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        switch ((MessageChannel)channel)
                        {
                            case MessageChannel.Warning:
                                this.WriteWarning(message);
                                break;

                            case MessageChannel.Verbose:
                                this.WriteVerbose(message);
                                break;

                            case MessageChannel.Debug:
                                this.WriteDebug(message);
                                break;

                            default:
                                Dbg.Assert(false, "We shouldn't get messages in channels that we didn't register for");
                                break;
                        }
                    });
        }

        private CimResponseType BlockingWriteError(ErrorRecord errorRecord)
        {
            Exception exceptionThrownOnCmdletThread = null;
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        this.WriteError(errorRecord, out exceptionThrownOnCmdletThread);
                    });

            return (exceptionThrownOnCmdletThread != null)
                       ? CimResponseType.NoToAll
                       : CimResponseType.Yes;
        }

        private CimResponseType WriteErrorCallback(CimInstance cimError)
        {
            lock (_jobStateLock)
            {
                _jobHadErrors = true;
            }

            var cimException = new CimException(cimError);
            var jobException = CimJobException.CreateFromCimException(this.GetDescription(), this.JobContext, cimException);
            var errorRecord = jobException.ErrorRecord;

            switch (this.JobContext.ErrorActionPreference)
            {
                case ActionPreference.Stop:
                case ActionPreference.Inquire:
                    return this.BlockingWriteError(errorRecord);

                default:
                    this.WriteError(errorRecord);
                    return CimResponseType.Yes;
            }
        }

        private bool _userWasPromptedForContinuationOfProcessing;
        private bool _userRespondedYesToAtLeastOneShouldProcess;

        internal bool DidUserSuppressTheOperation
        {
            get
            {
                bool didUserSuppressTheOperation = _userWasPromptedForContinuationOfProcessing && (!_userRespondedYesToAtLeastOneShouldProcess);
                return didUserSuppressTheOperation;
            }
        }

        internal CimResponseType ShouldProcess(string target, string action)
        {
            string verboseDescription = StringUtil.Format(CommandBaseStrings.ShouldProcessMessage,
                action,
                target,
                null);

            return ShouldProcess(verboseDescription, null, null);
        }

        internal CimResponseType ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            if (this.JobContext.IsRunningInBackground)
            {
                return CimResponseType.YesToAll;
            }

            if (this.JobContext.ShouldProcessOptimization == MshCommandRuntime.ShouldProcessPossibleOptimization.AutoNo_CanCallShouldProcessAsynchronously)
            {
                this.NonblockingShouldProcess(verboseDescription, verboseWarning, caption);
                return CimResponseType.No;
            }

            if (this.JobContext.ShouldProcessOptimization == MshCommandRuntime.ShouldProcessPossibleOptimization.AutoYes_CanCallShouldProcessAsynchronously)
            {
                this.NonblockingShouldProcess(verboseDescription, verboseWarning, caption);
                return CimResponseType.Yes;
            }

            Dbg.Assert(
                (this.JobContext.ShouldProcessOptimization != MshCommandRuntime.ShouldProcessPossibleOptimization.AutoYes_CanSkipShouldProcessCall) ||
                (this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.ClientSideShouldProcess),
                "MI layer should not call us when AutoYes_CanSkipShouldProcessCall optimization is in effect");
            Exception exceptionThrownOnCmdletThread;
            ShouldProcessReason shouldProcessReason;
            bool shouldProcessResponse = this.ShouldProcess(verboseDescription, verboseWarning, caption, out shouldProcessReason, out exceptionThrownOnCmdletThread);
            if (exceptionThrownOnCmdletThread != null)
            {
                return CimResponseType.NoToAll;
            }
            else if (shouldProcessResponse)
            {
                return CimResponseType.Yes;
            }
            else
            {
                return CimResponseType.No;
            }
        }

        private CimResponseType PromptUserCallback(string message, CimPromptType promptType)
        {
            message = this.JobContext.PrependComputerNameToMessage(message);

            Exception exceptionThrownOnCmdletThread = null;
            CimResponseType result = CimResponseType.No;

            _userWasPromptedForContinuationOfProcessing = true;
            switch (promptType)
            {
                case CimPromptType.Critical:
                    this.ExceptionSafeWrapper(
                            delegate
                            {
                                if (this.ShouldContinue(message, null, out exceptionThrownOnCmdletThread))
                                {
                                    result = CimResponseType.Yes;
                                }
                                else
                                {
                                    result = CimResponseType.No;
                                }
                            });
                    break;

                case CimPromptType.Normal:
                    this.ExceptionSafeWrapper(
                            delegate
                            {
                                result = this.ShouldProcess(message, null, null);
                            });
                    break;

                default:
                    Dbg.Assert(false, "Unrecognized CimPromptType");
                    break;
            }

            if (exceptionThrownOnCmdletThread != null)
            {
                result = CimResponseType.NoToAll;
            }

            if ((result == CimResponseType.Yes) || (result == CimResponseType.YesToAll))
            {
                _userRespondedYesToAtLeastOneShouldProcess = true;
            }

            return result;
        }

        #endregion

        internal static bool IsShowComputerNameMarkerPresent(CimInstance cimInstance)
        {
            PSObject pso = PSObject.AsPSObject(cimInstance);
            if (!(pso.InstanceMembers[RemotingConstants.ShowComputerNameNoteProperty] is PSPropertyInfo psShowComputerNameProperty))
            {
                return false;
            }

            return true.Equals(psShowComputerNameProperty.Value);
        }

        internal static void AddShowComputerNameMarker(PSObject pso)
        {
            PSPropertyInfo psShowComputerNameProperty = pso.InstanceMembers[RemotingConstants.ShowComputerNameNoteProperty] as PSPropertyInfo;
            if (psShowComputerNameProperty != null)
            {
                psShowComputerNameProperty.Value = true;
            }
            else
            {
                psShowComputerNameProperty = new PSNoteProperty(RemotingConstants.ShowComputerNameNoteProperty, true);
                pso.InstanceMembers.Add(psShowComputerNameProperty);
            }
        }

        internal override void WriteObject(object outputObject)
        {
            CimInstance cimInstance = null;
            PSObject pso = null;
            if (outputObject is PSObject)
            {
                pso = PSObject.AsPSObject(outputObject);
                cimInstance = pso.BaseObject as CimInstance;
            }
            else
            {
                cimInstance = outputObject as CimInstance;
            }

            if (cimInstance != null)
            {
                CimCmdletAdapter.AssociateSessionOfOriginWithInstance(cimInstance, this.JobContext.Session);
                CimCustomOptionsDictionary.AssociateCimInstanceWithCustomOptions(cimInstance, this.GetJobSpecificCustomOptions());
            }

            if (this.JobContext.ShowComputerName)
            {
                pso ??= PSObject.AsPSObject(outputObject);

                AddShowComputerNameMarker(pso);
                if (cimInstance == null)
                {
                    pso.Properties.Add(new PSNoteProperty(RemotingConstants.ComputerNameNoteProperty, this.JobContext.Session.ComputerName));
                }
            }

            base.WriteObject(outputObject);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool isCompleted;
                lock (_jobStateLock)
                {
                    isCompleted = _alreadyReachedCompletedState;
                }

                if (!isCompleted)
                {
                    this.StopJob();
                    this.Finished.WaitOne();
                }

                _cimSensitiveValueConverter.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
