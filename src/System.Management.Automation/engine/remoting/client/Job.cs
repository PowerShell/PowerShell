// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    /// <summary>
    /// Enumeration for job status values. Indicates the status
    /// of the result object.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// Execution of command in job not started.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Execution of command in progress.
        /// </summary>
        Running = 1,

        /// <summary>
        /// Execution of command completed in all
        /// computernames/runspaces.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// An error was encountered when trying to executed
        /// command in one or more computernames/runspaces.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Command execution is cancelled (stopped) in one or more
        /// computernames/runspaces.
        /// </summary>
        Stopped = 4,

        /// <summary>
        /// Command execution is blocked (on user input host calls etc)
        /// </summary>
        Blocked = 5,

        /// <summary>
        /// The job has been suspended.
        /// </summary>
        Suspended = 6,

        /// <summary>
        /// The job is a remote job and has been disconnected from the server.
        /// </summary>
        Disconnected = 7,

        /// <summary>
        /// Suspend is in progress.
        /// </summary>
        Suspending = 8,

        /// <summary>
        /// Stop is in progress.
        /// </summary>
        Stopping = 9,

        /// <summary>
        /// Script execution is halted in a debugger stop.
        /// </summary>
        AtBreakpoint = 10
    }

    /// <summary>
    /// Defines exception which is thrown when state of the PSJob is different
    /// from the expected state.
    /// </summary>
    [Serializable]
    public class InvalidJobStateException : SystemException
    {
        /// <summary>
        /// Creates a new instance of InvalidPSJobStateException class.
        /// </summary>
        public InvalidJobStateException()
            : base
        (
            PSRemotingErrorInvariants.FormatResourceString
            (
                RemotingErrorIdStrings.InvalidJobStateGeneral
            )
        )
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidPSJobStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public InvalidJobStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidPSJobStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidJobStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidJobStateException class.
        /// </summary>
        /// <param name="currentState">
        /// The Job State at the time of the error.
        /// </param>
        /// <param name="actionMessage">
        /// An additional message that gives more information about the error. Used
        /// for context after a generalized error message.
        /// </param>
        public InvalidJobStateException(JobState currentState, string actionMessage)
            : base
        (
            PSRemotingErrorInvariants.FormatResourceString
            (
                RemotingErrorIdStrings.InvalidJobStateSpecific, currentState, actionMessage
            )
        )
        {
            _currState = currentState;
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPSJobStateException and defines value of
        /// CurrentState.
        /// </summary>
        /// <param name="currentState">Current state of powershell.</param>
        internal InvalidJobStateException(JobState currentState)
            : base
        (
            PSRemotingErrorInvariants.FormatResourceString
            (
                RemotingErrorIdStrings.InvalidJobStateGeneral
            )
        )
        {
            _currState = currentState;
        }

        #region ISerializable Members

        // No need to implement GetObjectData
        // if all fields are static or [NonSerialized]

        /// <summary>
        /// Initializes a new instance of the InvalidPSJobStateException
        /// class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object
        /// data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information
        /// about the source or destination.
        /// </param>
        protected
        InvalidJobStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        /// <summary>
        /// Gets CurrentState of the Job.
        /// </summary>
        public JobState CurrentState
        {
            get
            {
                return _currState;
            }
        }

        /// <summary>
        /// State of job when exception was thrown.
        /// </summary>
        [NonSerialized]
        private readonly JobState _currState = 0;
    }

    /// <summary>
    /// Type which has information about JobState and Exception
    /// ,if any, associated with JobState.
    /// </summary>
    public sealed class JobStateInfo
    {
        #region constructors

        /// <summary>
        /// Constructor for state changes not resulting from an error.
        /// </summary>
        /// <param name="state">Execution state.</param>
        public JobStateInfo(JobState state)
            : this(state, null)
        {
        }

        /// <summary>
        /// Constructor for state changes with an optional error.
        /// </summary>
        /// <param name="state">The new state.</param>
        /// <param name="reason">A non-null exception if the state change was
        /// caused by an error,otherwise; null.
        /// </param>
        public JobStateInfo(JobState state, Exception reason)
        {
            State = state;
            Reason = reason;
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="jobStateInfo">Source information.</param>
        /// <throws>
        /// ArgumentNullException when <paramref name="jobStateInfo"/> is null.
        /// </throws>
        internal JobStateInfo(JobStateInfo jobStateInfo)
        {
            State = jobStateInfo.State;
            Reason = jobStateInfo.Reason;
        }

        #endregion constructors

        #region public_properties

        /// <summary>
        /// The state of the job.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the job .
        /// </remarks>
        public JobState State { get; }

        /// <summary>
        /// The reason for the state change, if caused by an error.
        /// </summary>
        /// <remarks>
        /// The value of this property is non-null if the state
        /// changed due to an error. Otherwise, the value of this
        /// property is null.
        /// </remarks>
        public Exception Reason { get; }

        #endregion public_properties

        /// <summary>
        /// Override for ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return State.ToString();
        }

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns>Cloned object.</returns>
        internal JobStateInfo Clone()
        {
            return new JobStateInfo(this);
        }

        #region private_fields

        #endregion private_fields
    }

    /// <summary>
    /// Event arguments passed to JobStateEvent handlers
    /// <see cref="Job.StateChanged"/> event.
    /// </summary>
    public sealed class JobStateEventArgs : EventArgs
    {
        #region constructors

        /// <summary>
        /// Constructor of JobStateEventArgs.
        /// </summary>
        /// <param name="jobStateInfo">The current state of the job.</param>
        public JobStateEventArgs(JobStateInfo jobStateInfo)
            : this(jobStateInfo, null)
        {
        }

        /// <summary>
        /// Constructor of JobStateEventArgs.
        /// </summary>
        /// <param name="jobStateInfo">The current state of the job.</param>
        /// <param name="previousJobStateInfo">The previous state of the job.</param>
        public JobStateEventArgs(JobStateInfo jobStateInfo, JobStateInfo previousJobStateInfo)
        {
            if (jobStateInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(jobStateInfo));
            }

            JobStateInfo = jobStateInfo;
            PreviousJobStateInfo = previousJobStateInfo;
        }

        #endregion constructors

        #region public_properties

        /// <summary>
        /// Info about the current state of the job.
        /// </summary>
        public JobStateInfo JobStateInfo { get; }

        /// <summary>
        /// Info about the previous state of the job.
        /// </summary>
        public JobStateInfo PreviousJobStateInfo { get; }

        #endregion public_properties
    }

    /// <summary>
    /// Object that must be created by PowerShell to allow reuse of an ID for a job.
    /// Also allows setting of the Instance Id so that jobs may be recreated.
    /// </summary>
    public sealed class JobIdentifier
    {
        internal JobIdentifier(int id, Guid instanceId)
        {
            if (id <= 0)
                PSTraceSource.NewArgumentException(nameof(id), RemotingErrorIdStrings.JobSessionIdLessThanOne, id);
            Id = id;
            InstanceId = instanceId;
        }

        internal int Id { get; }

        internal Guid InstanceId { get; private set; }
    }

    /// <summary>
    /// Interface to expose a job debugger.
    /// </summary>
#nullable enable
    public interface IJobDebugger
    {
        /// <summary>
        /// Job Debugger.
        /// </summary>
        Debugger? Debugger
        {
            get;
        }

        /// <summary>
        /// True if job is running asynchronously.
        /// </summary>
        bool IsAsync
        {
            get;
            set;
        }
    }
#nullable restore

    /// <summary>
    /// Represents a command running in background. A job object can internally
    /// contain many child job objects.
    /// </summary>
    public abstract class Job : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected Job()
        {
            Id = System.Threading.Interlocked.Increment(ref s_jobIdSeed);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        protected Job(string command)
            : this()
        {
            Command = command;
            _name = AutoGenerateJobName();
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        protected Job(string command, string name)
            : this(command)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _name = name;
            }
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        /// <param name="childJobs">Child jobs of this job object.</param>
        protected Job(string command, string name, IList<Job> childJobs)
            : this(command, name)
        {
            _childJobs = childJobs;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        /// <param name="token">Id and InstanceId pair to be used for this job object.</param>
        /// <remarks>The JobIdentifier is a token that must be issued by PowerShell to allow
        /// reuse of the Id. This is the only way to set either Id or instance Id.</remarks>
        protected Job(string command, string name, JobIdentifier token)
        {
            if (token == null)
                throw PSTraceSource.NewArgumentNullException(nameof(token), RemotingErrorIdStrings.JobIdentifierNull);
            if (token.Id > s_jobIdSeed)
            {
                throw PSTraceSource.NewArgumentException(nameof(token), RemotingErrorIdStrings.JobIdNotYetAssigned, token.Id);
            }

            Command = command;

            Id = token.Id;
            InstanceId = token.InstanceId;

            if (!string.IsNullOrEmpty(name))
            {
                _name = name;
            }
            else
            {
                _name = AutoGenerateJobName();
            }
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="command">Command invoked by this job object.</param>
        /// <param name="name">Friendly name for the job object.</param>
        /// <param name="instanceId">InstanceId to be used for this job object.</param>
        /// <remarks>The InstanceId may need to be set to maintain job identity across
        /// instances of the process.</remarks>
        protected Job(string command, string name, Guid instanceId)
            : this(command, name)
        {
            InstanceId = instanceId;
        }

        internal static string GetCommandTextFromInvocationInfo(InvocationInfo invocationInfo)
        {
            if (invocationInfo == null)
            {
                return null;
            }

            IScriptExtent scriptExtent = invocationInfo.ScriptPosition;
            if ((scriptExtent != null) && (scriptExtent.StartScriptPosition != null) && !string.IsNullOrWhiteSpace(scriptExtent.StartScriptPosition.Line))
            {
                Dbg.Assert(scriptExtent.StartScriptPosition.ColumnNumber > 0, "Column numbers start at 1");
                Dbg.Assert(scriptExtent.StartScriptPosition.ColumnNumber <= scriptExtent.StartScriptPosition.Line.Length, "Column numbers are not greater than the length of a line");
                return scriptExtent.StartScriptPosition.Line.AsSpan(scriptExtent.StartScriptPosition.ColumnNumber - 1).Trim().ToString();
            }

            return invocationInfo.InvocationName;
        }

        #endregion Constructor

        #region Private Members

        private ManualResetEvent _finished = new ManualResetEvent(false);

        private string _name;
        private IList<Job> _childJobs;
        internal readonly object syncObject = new object();   // object used for synchronization
        // ISSUE: Should Result be public property
        private PSDataCollection<PSStreamObject> _results = new PSDataCollection<PSStreamObject>();
        private bool _resultsOwner = true;
        private PSDataCollection<ErrorRecord> _error = new PSDataCollection<ErrorRecord>();
        private bool _errorOwner = true;
        private PSDataCollection<ProgressRecord> _progress = new PSDataCollection<ProgressRecord>();
        private bool _progressOwner = true;
        private PSDataCollection<VerboseRecord> _verbose = new PSDataCollection<VerboseRecord>();
        private bool _verboseOwner = true;
        private PSDataCollection<WarningRecord> _warning = new PSDataCollection<WarningRecord>();
        private bool _warningOwner = true;
        private PSDataCollection<DebugRecord> _debug = new PSDataCollection<DebugRecord>();
        private bool _debugOwner = true;
        private PSDataCollection<InformationRecord> _information = new PSDataCollection<InformationRecord>();
        private bool _informationOwner = true;
        private PSDataCollection<PSObject> _output = new PSDataCollection<PSObject>();
        private bool _outputOwner = true;

        /// <summary>
        /// Static variable which is incremented to generate id.
        /// </summary>
        private static int s_jobIdSeed = 0;

        private string _jobTypeName = string.Empty;

        #endregion Private Members

        #region Job Properties

        /// <summary>
        /// Command Invoked by this Job.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Status of the command execution.
        /// </summary>
        public JobStateInfo JobStateInfo { get; private set; } = new JobStateInfo(JobState.NotStarted);

        /// <summary>
        /// Wait Handle which is signaled when job is finished.
        /// This is set when state of the job is set to Completed,
        /// Stopped or Failed.
        /// </summary>
        public WaitHandle Finished
        {
            get
            {
                lock (this.syncObject)
                {
                    if (_finished != null)
                    {
                        return _finished;
                    }
                    else
                    {
                        // Damage control mode:
                        // Somebody is trying to get Finished handle for an already disposed Job instance.
                        // Return an already triggered handle (disposed job is finished by definition).
                        // The newly created handle will not be disposed in a deterministic manner
                        // and in some circumstances can be mistaken for a handle leak.
                        return new ManualResetEvent(true);
                    }
                }
            }
        }

        /// <summary>
        /// Unique identifier for this job.
        /// </summary>
        public Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// Short identifier for this result which will be
        /// recycled and used within a process.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Name for identifying this job object.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                AssertNotDisposed();
                _name = value;
            }
        }

        /// <summary>
        /// List of child jobs contained within this job.
        /// </summary>
        public IList<Job> ChildJobs
        {
            get
            {
                if (_childJobs == null)
                {
                    lock (syncObject)
                    {
                        _childJobs ??= new List<Job>();
                    }
                }

                return _childJobs;
            }
        }

        /// <summary>
        /// Success status of the command execution.
        /// </summary>
        public abstract string StatusMessage { get; }

        /// <summary>
        /// Indicates that more data is available in this
        /// result object for reading.
        /// </summary>
        public abstract bool HasMoreData { get; }

        /// <summary>
        /// Time job was started.
        /// </summary>
        public DateTime? PSBeginTime { get; protected set; } = null;

        /// <summary>
        /// Time job stopped.
        /// </summary>
        public DateTime? PSEndTime { get; protected set; } = null;

        /// <summary>
        /// Job type name.
        /// </summary>
        public string PSJobTypeName
        {
            get
            {
                return _jobTypeName;
            }

            protected internal set
            {
                _jobTypeName = value ?? this.GetType().ToString();
            }
        }

        #region results

        /// <summary>
        /// Result objects from this job. If this object is not a
        /// leaf node (with no children), then this will
        /// aggregate the results from all child jobs.
        /// </summary>
        internal PSDataCollection<PSStreamObject> Results
        {
            get
            {
                return _results;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Results");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _resultsOwner = false;
                    _results = value;
                }
            }
        }

        /// <summary>
        /// Indicates if a particular Job type uses the
        /// internal results collection.
        /// </summary>
        internal bool UsesResultsCollection { get; set; }

        /// <summary>
        /// Suppresses forwarding of job output into a cmdlet (like Receive-Job).
        /// This flag modifies functionality of <see cref="WriteObject"/> method, so that it doesnt add output-processing to <see cref="Results"/> collection.
        /// </summary>
        internal bool SuppressOutputForwarding { get; set; }

        internal virtual void WriteObject(object outputObject)
        {
            PSObject pso = (outputObject == null) ? null : PSObject.AsPSObject(outputObject);
            this.Output.Add(pso);

            if (!SuppressOutputForwarding)
            {
                this.Results.Add(new PSStreamObject(PSStreamObjectType.Output, pso));
            }
        }

        /// <summary>
        /// Allows propagating of terminating exceptions from remote "throw" statement
        /// (normally / by default all remote errors are transformed into non-terminating errors.
        /// </summary>
        internal bool PropagateThrows { get; set; }

        private void WriteError(Cmdlet cmdlet, ErrorRecord errorRecord)
        {
            if (this.PropagateThrows)
            {
                Exception e = GetExceptionFromErrorRecord(errorRecord);
                if (e != null)
                    throw e;
            }

            errorRecord.PreserveInvocationInfoOnce = true;
            cmdlet.WriteError(errorRecord);
        }

        private static Exception GetExceptionFromErrorRecord(ErrorRecord errorRecord)
        {
            if (!(errorRecord.Exception is RuntimeException runtimeException))
                return null;

            if (!(runtimeException is RemoteException remoteException))
                return null;

            PSPropertyInfo wasThrownFromThrow =
                remoteException.SerializedRemoteException.Properties["WasThrownFromThrowStatement"];
            if (wasThrownFromThrow == null || !((bool)wasThrownFromThrow.Value))
                return null;

            runtimeException.WasThrownFromThrowStatement = true;
            return runtimeException;
        }

        internal virtual void WriteError(ErrorRecord errorRecord)
        {
            Error.Add(errorRecord);
            if (PropagateThrows)
            {
                Exception exception = GetExceptionFromErrorRecord(errorRecord);
                if (exception != null)
                {
                    Results.Add(new PSStreamObject(PSStreamObjectType.Exception, exception));
                    return;
                }
            }

            Results.Add(new PSStreamObject(PSStreamObjectType.Error, errorRecord));
        }

        internal void WriteError(ErrorRecord errorRecord, out Exception exceptionThrownOnCmdletThread)
        {
            this.Error.Add(errorRecord);
            this.InvokeCmdletMethodAndWaitForResults<object>(
                (Cmdlet cmdlet) =>
                {
                    this.WriteError(cmdlet, errorRecord);
                    return null;
                },
                out exceptionThrownOnCmdletThread);
        }

        internal virtual void WriteWarning(string message)
        {
            this.Warning.Add(new WarningRecord(message));
            this.Results.Add(new PSStreamObject(PSStreamObjectType.Warning, message));
        }

        internal virtual void WriteVerbose(string message)
        {
            this.Verbose.Add(new VerboseRecord(message));
            this.Results.Add(new PSStreamObject(PSStreamObjectType.Verbose, message));
        }

        internal virtual void WriteDebug(string message)
        {
            this.Debug.Add(new DebugRecord(message));
            this.Results.Add(new PSStreamObject(PSStreamObjectType.Debug, message));
        }

        internal virtual void WriteProgress(ProgressRecord progressRecord)
        {
            if ((progressRecord.ParentActivityId == (-1)) && (_parentActivityId != null))
            {
                progressRecord = new ProgressRecord(progressRecord) { ParentActivityId = _parentActivityId.Value };
            }

            Progress.Add(progressRecord);
            Results.Add(new PSStreamObject(PSStreamObjectType.Progress, progressRecord));
        }

        internal virtual void WriteInformation(InformationRecord informationRecord)
        {
            Information.Add(informationRecord);
            Results.Add(new PSStreamObject(PSStreamObjectType.Information, informationRecord));
        }

        private Lazy<int> _parentActivityId;

        internal void SetParentActivityIdGetter(Func<int> parentActivityIdGetter)
        {
            Dbg.Assert(parentActivityIdGetter != null, "Caller should verify parentActivityIdGetter != null");
            _parentActivityId = new Lazy<int>(parentActivityIdGetter);
        }

        internal bool ShouldContinue(string query, string caption)
        {
            Exception exceptionThrownOnCmdletThread;
            return this.ShouldContinue(query, caption, out exceptionThrownOnCmdletThread);
        }

        internal bool ShouldContinue(string query, string caption, out Exception exceptionThrownOnCmdletThread)
        {
            bool methodResult = InvokeCmdletMethodAndWaitForResults(
                cmdlet => cmdlet.ShouldContinue(query, caption),
                out exceptionThrownOnCmdletThread);
            return methodResult;
        }

        internal virtual void NonblockingShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            InvokeCmdletMethodAndIgnoreResults(
                (Cmdlet cmdlet) =>
                {
                    ShouldProcessReason throwAwayProcessReason;
                    cmdlet.ShouldProcess(
                        verboseDescription,
                        verboseWarning,
                        caption,
                        out throwAwayProcessReason);
                });
        }

        internal virtual bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason,
            out Exception exceptionThrownOnCmdletThread)
        {
            ShouldProcessReason closureSafeShouldProcessReason = ShouldProcessReason.None;

            bool methodResult = InvokeCmdletMethodAndWaitForResults(
                cmdlet => cmdlet.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption,
                    out closureSafeShouldProcessReason),
                out exceptionThrownOnCmdletThread);

            shouldProcessReason = closureSafeShouldProcessReason;
            return methodResult;
        }

        private void InvokeCmdletMethodAndIgnoreResults(Action<Cmdlet> invokeCmdletMethod)
        {
            object resultsLock = new object();
            CmdletMethodInvoker<object> methodInvoker = new CmdletMethodInvoker<object>
            {
                Action = (Cmdlet cmdlet) => { invokeCmdletMethod(cmdlet); return null; },
                Finished = null,
                SyncObject = resultsLock
            };
            Results.Add(new PSStreamObject(PSStreamObjectType.BlockingError, methodInvoker));
        }

        private T InvokeCmdletMethodAndWaitForResults<T>(Func<Cmdlet, T> invokeCmdletMethodAndReturnResult, out Exception exceptionThrownOnCmdletThread)
        {
            Dbg.Assert(invokeCmdletMethodAndReturnResult != null, "Caller should verify invokeCmdletMethodAndReturnResult != null");

            T methodResult = default(T);
            Exception closureSafeExceptionThrownOnCmdletThread = null;
            object resultsLock = new object();
            using (var gotResultEvent = new ManualResetEventSlim(false))
            {
                EventHandler<JobStateEventArgs> stateChangedEventHandler =
                    (object sender, JobStateEventArgs eventArgs) =>
                    {
                        if (IsFinishedState(eventArgs.JobStateInfo.State) || eventArgs.JobStateInfo.State == JobState.Stopping)
                        {
                            lock (resultsLock)
                            {
                                closureSafeExceptionThrownOnCmdletThread = new OperationCanceledException();
                            }

                            gotResultEvent.Set();
                        }
                    };
                this.StateChanged += stateChangedEventHandler;
                Interlocked.MemoryBarrier();
                try
                {
                    stateChangedEventHandler(null, new JobStateEventArgs(this.JobStateInfo));

                    if (!gotResultEvent.IsSet)
                    {
                        this.SetJobState(JobState.Blocked);

                        // addition to results column happens here
                        CmdletMethodInvoker<T> methodInvoker = new CmdletMethodInvoker<T>
                        {
                            Action = invokeCmdletMethodAndReturnResult,
                            Finished = gotResultEvent,
                            SyncObject = resultsLock
                        };
                        PSStreamObjectType objectType = PSStreamObjectType.ShouldMethod;

                        if (typeof(T) == typeof(object))
                            objectType = PSStreamObjectType.BlockingError;

                        Results.Add(new PSStreamObject(objectType, methodInvoker));

                        gotResultEvent.Wait();
                        this.SetJobState(JobState.Running);

                        lock (resultsLock)
                        {
                            if (closureSafeExceptionThrownOnCmdletThread == null) // stateChangedEventHandler didn't set the results?  = ok to clobber results?
                            {
                                closureSafeExceptionThrownOnCmdletThread = methodInvoker.ExceptionThrownOnCmdletThread;
                                methodResult = methodInvoker.MethodResult;
                            }
                        }
                    }
                }
                finally
                {
                    this.StateChanged -= stateChangedEventHandler;
                }
            }

            lock (resultsLock)
            {
                exceptionThrownOnCmdletThread = closureSafeExceptionThrownOnCmdletThread;
                return methodResult;
            }
        }

        internal virtual void ForwardAvailableResultsToCmdlet(Cmdlet cmdlet)
        {
            foreach (PSStreamObject obj in Results.ReadAll())
            {
                obj.WriteStreamObject(cmdlet);
            }
        }

        internal virtual void ForwardAllResultsToCmdlet(Cmdlet cmdlet)
        {
            foreach (PSStreamObject obj in this.Results)
            {
                obj.WriteStreamObject(cmdlet);
            }
        }

        /// <summary>
        /// This method is introduce for delaying the loading of streams
        /// for a particular job.
        /// </summary>
        protected virtual void DoLoadJobStreams()
        {
        }

        /// <summary>
        /// Unloads job streams information. Enables jobs to
        /// clear stream information from memory.
        /// </summary>
        protected virtual void DoUnloadJobStreams()
        {
        }

        /// <summary>
        /// Load the required job streams.
        /// </summary>
        public void LoadJobStreams()
        {
            if (_jobStreamsLoaded) return;

            lock (syncObject)
            {
                if (_jobStreamsLoaded) return;

                _jobStreamsLoaded = true;
            }

            try
            {
                DoLoadJobStreams();
            }
            catch (Exception e)
            {
                // third party call-out for platform API
                // Therefore it is fine to eat the exception
                // here
                using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
                {
                    tracer.TraceException(e);
                }
            }
        }

        private bool _jobStreamsLoaded;

        /// <summary>
        /// Unload the required job streams.
        /// </summary>
        public void UnloadJobStreams()
        {
            if (!_jobStreamsLoaded) return;

            lock (syncObject)
            {
                if (!_jobStreamsLoaded) return;
                _jobStreamsLoaded = false;
            }

            try
            {
                DoUnloadJobStreams();
            }
            catch (Exception e)
            {
                // third party call-out for platform API
                // Therefore it is fine to eat the exception
                // here
                using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
                {
                    tracer.TraceException(e);
                }
            }
        }

        /// <summary>
        /// Gets or sets the output buffer. Output of job is written
        /// into this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<PSObject> Output
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _output;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Output");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _outputOwner = false;
                    _output = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the error buffer. Errors of job are written
        /// into this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ErrorRecord> Error
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _error;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Error");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _errorOwner = false;
                    _error = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress buffer. Progress of job is written
        /// into this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ProgressRecord> Progress
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _progress;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Progress");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _progressOwner = false;
                    _progress = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the verbose buffer. Verbose output of job is written to
        /// this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<VerboseRecord> Verbose
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _verbose;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Verbose");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _verboseOwner = false;
                    _verbose = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the debug buffer. Debug output of Job is written
        /// to this buffer.
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<DebugRecord> Debug
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _debug;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Debug");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _debugOwner = false;
                    _debug = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the warning buffer. Warnings of job are written to
        /// this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<WarningRecord> Warning
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _warning;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Warning");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _warningOwner = false;
                    _warning = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the information buffer. Information records of job are written to
        /// this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<InformationRecord> Information
        {
            get
            {
                LoadJobStreams(); // for delayed loading
                return _information;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Information");
                }

                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    _informationOwner = false;
                    _information = value;
                    _jobStreamsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Indicates a location where this job is running.
        /// </summary>
        public abstract string Location { get; }

        #endregion results

        #region Connect/Disconnect

        /// <summary>
        /// Returns boolean indicating whether the underlying
        /// transport for the job (or child jobs) supports
        /// connect/disconnect semantics.
        /// </summary>
        internal virtual bool CanDisconnect
        {
            get { return false; }
        }

        /// <summary>
        /// Returns runspaces associated with the Job, including
        /// child jobs.
        /// </summary>
        /// <returns>IEnumerable of RemoteRunspaces.</returns>
        internal virtual IEnumerable<RemoteRunspace> GetRunspaces()
        {
            return null;
        }

        #endregion

        #endregion Job Properties

        #region Job State and State Change Event

        /// <summary>
        /// Event raised when state of the job changes.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public event EventHandler<JobStateEventArgs> StateChanged;

        /// <summary>
        /// Sets Job State.
        /// </summary>
        /// <param name="state">
        /// New State of Job
        /// </param>
        protected void SetJobState(JobState state)
        {
            AssertNotDisposed();
            SetJobState(state, null);
        }

        /// <summary>
        /// Sets Job State.
        /// </summary>
        /// <param name="state">
        /// New State of Job
        /// </param>
        /// <param name="reason">
        /// Reason associated with the state.
        /// </param>
        internal void SetJobState(JobState state, Exception reason)
        {
            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                AssertNotDisposed();

                bool alldone = false;
                JobStateInfo previousState = JobStateInfo;

                lock (syncObject)
                {
                    JobStateInfo = new JobStateInfo(state, reason);

                    if (state == JobState.Running)
                    {
                        // BeginTime is set only once.
                        if (PSBeginTime == null)
                        {
                            PSBeginTime = DateTime.Now;
                        }
                    }
                    else if (IsFinishedState(state))
                    {
                        alldone = true;

                        // EndTime is set only once.
                        if (PSEndTime == null)
                        {
                            PSEndTime = DateTime.Now;
                        }
                    }
                }

                if (alldone)
                {
                    CloseAllStreams();

                    if (_processingOutput)
                    {
                        try
                        {
                            // Still marked as processing output.  Send final state changed.
                            HandleOutputProcessingStateChanged(this, new OutputProcessingStateEventArgs(false));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

#pragma warning disable 56500
                // Exception raised in the eventhandler are not error in job.
                // silently ignore them.
                try
                {
                    tracer.WriteMessage("Job", "SetJobState", Guid.Empty, this, "Invoking StateChanged event", null);
                    StateChanged.SafeInvoke(this, new JobStateEventArgs(JobStateInfo.Clone(), previousState));
                }
                catch (Exception exception) // ignore non-severe exceptions
                {
                    tracer.WriteMessage("Job", "SetJobState", Guid.Empty, this,
                                        "Some Job StateChange event handler threw an unhandled exception.", null);
                    tracer.TraceException(exception);
                }

                // finished needs to be set after StateChanged event
                // has been raised
                if (alldone)
                {
                    lock (syncObject)
                    {
                        _finished?.Set();
                    }
                }
#pragma warning restore 56500
            }
        }

        #endregion Job State and State Change Event

        #region Job Public Methods

        /// <summary>
        /// Stop this job object. If job contains child job, this should
        /// stop child job objects also.
        /// </summary>
        public abstract void StopJob();

        #endregion Job Public Methods

        #region Private/Internal Methods

        /// <summary>
        /// Returns the items in results collection
        /// after clearing up all the internal
        /// structures.
        /// </summary>
        /// <returns>Collection of stream objects.</returns>
        internal Collection<PSStreamObject> ReadAll()
        {
            Output.Clear();
            Error.Clear();
            Debug.Clear();
            Warning.Clear();
            Verbose.Clear();
            Progress.Clear();

            return Results.ReadAll();
        }

        /// <summary>
        /// Helper function to check if job is finished.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal bool IsFinishedState(JobState state)
        {
            lock (syncObject)
            {
                return (state == JobState.Completed || state == JobState.Failed || state == JobState.Stopped);
            }
        }

        internal bool IsPersistentState(JobState state)
        {
            lock (syncObject)
            {
                return (IsFinishedState(state) || state == JobState.Disconnected || state == JobState.Suspended);
            }
        }

        /// <summary>
        /// Checks if the current instance can accept changes like
        /// changing one of the properties like Output, Error etc.
        /// If changes are not allowed, throws an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        private void AssertChangesAreAccepted()
        {
            AssertNotDisposed();
            lock (syncObject)
            {
                if (JobStateInfo.State == JobState.Running)
                {
                    throw new InvalidJobStateException(JobState.Running);
                }
            }
        }

        /// <summary>
        /// Automatically generate a job name if the user
        /// does not supply one.
        /// </summary>
        /// <returns>Auto generated job name.</returns>
        /// <remarks>Since the user can script/program against the
        /// job name, the auto generated name will not be
        /// localizable</remarks>
        protected string AutoGenerateJobName()
        {
            return "Job" + Id.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Checks if the current powershell instance is disposed.
        /// If disposed, throws ObjectDisposedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        internal void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw PSTraceSource.NewObjectDisposedException("PSJob");
            }
        }

        /// <summary>
        /// A helper method to close all the streams.
        /// </summary>
        internal void CloseAllStreams()
        {
            // The Complete() method includes raising public notification events that third parties can
            // handle and potentially throw exceptions on the notification thread.  We don't want to
            // propagate those exceptions because it prevents this thread from completing its processing.
            if (_resultsOwner) { try { _results.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_outputOwner) { try { _output.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_errorOwner) { try { _error.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_progressOwner) { try { _progress.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_verboseOwner) { try { _verbose.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_warningOwner) { try { _warning.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_debugOwner) { try { _debug.Complete(); } catch (Exception e) { TraceException(e); } }

            if (_informationOwner) { try { _information.Complete(); } catch (Exception e) { TraceException(e); } }
        }

        private static void TraceException(Exception e)
        {
            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.TraceException(e);
            }
        }

        /// <summary>
        /// Gets the job for the specified location.
        /// </summary>
        /// <param name="location">Location to filter on.</param>
        /// <returns>Collection of jobs.</returns>
        internal List<Job> GetJobsForLocation(string location)
        {
            List<Job> returnJobList = new List<Job>();

            foreach (Job job in ChildJobs)
            {
                if (string.Equals(job.Location, location, StringComparison.OrdinalIgnoreCase))
                {
                    returnJobList.Add(job);
                }
            }

            return returnJobList;
        }

        #endregion Private/Internal Methods

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
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    CloseAllStreams();

                    // release the WaitHandle
                    lock (syncObject)
                    {
                        if (_finished != null)
                        {
                            _finished.Dispose();
                            _finished = null;
                        }
                    }

                    // Only dispose the collections if we've created them...
                    if (_resultsOwner) _results.Dispose();
                    if (_outputOwner) _output.Dispose();
                    if (_errorOwner) _error.Dispose();
                    if (_debugOwner) _debug.Dispose();
                    if (_informationOwner) _information.Dispose();
                    if (_verboseOwner) _verbose.Dispose();
                    if (_warningOwner) _warning.Dispose();
                    if (_progressOwner) _progress.Dispose();

                    _isDisposed = true;
                }
            }
        }

        private bool _isDisposed;

        #endregion IDisposable Members

        #region MonitorOutputProcessing

        internal event EventHandler<OutputProcessingStateEventArgs> OutputProcessingStateChanged;

        private bool _processingOutput;

        /// <summary>
        /// MonitorOutputProcessing.
        /// </summary>
        internal bool MonitorOutputProcessing
        {
            get;
            set;
        }

        internal void SetMonitorOutputProcessing(IOutputProcessingState outputProcessingState)
        {
            if (outputProcessingState != null)
            {
                outputProcessingState.OutputProcessingStateChanged += HandleOutputProcessingStateChanged;
            }
        }

        internal void RemoveMonitorOutputProcessing(IOutputProcessingState outputProcessingState)
        {
            if (outputProcessingState != null)
            {
                outputProcessingState.OutputProcessingStateChanged -= HandleOutputProcessingStateChanged;
            }
        }

        private void HandleOutputProcessingStateChanged(object sender, OutputProcessingStateEventArgs e)
        {
            _processingOutput = e.ProcessingOutput;
            OutputProcessingStateChanged.SafeInvoke<OutputProcessingStateEventArgs>(this, e);
        }

        #endregion
    }

    /// <summary>
    /// Top level job object for remoting. This contains multiple child job
    /// objects. Each child job object invokes command on one remote machine.
    /// </summary>
    /// <remarks>
    /// Not removing the prefix "PS" as this signifies powershell specific remoting job
    /// </remarks>
    internal class PSRemotingJob : Job
    {
        #region Internal Constructors

        /// <summary>
        /// Internal constructor for initializing PSRemotingJob using
        /// computer names.
        /// </summary>
        /// <param name="computerNames">names of computers for
        /// which the job object is being created</param>
        /// <param name="computerNameHelpers">list of helper objects
        /// corresponding to the computer names
        /// </param>
        /// <param name="remoteCommand">remote command corresponding to this
        /// job object</param>
        /// <param name="name"> a friendly name for the job object
        /// </param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PSRemotingJob(string[] computerNames,
                        List<IThrottleOperation> computerNameHelpers, string remoteCommand, string name)
            : this(computerNames, computerNameHelpers, remoteCommand, 0, name)
        { }

        /// <summary>
        /// Internal constructor for initializing job using
        /// PSSession objects.
        /// </summary>
        /// <param name="remoteRunspaceInfos">array of runspace info
        /// objects on which the remote command is executed</param>
        /// <param name="runspaceHelpers">List of helper objects for the
        /// runspaces</param>
        /// <param name="remoteCommand"> remote command corresponding to this
        /// job object</param>
        /// <param name="name">a friendly name for the job object
        /// </param>
        internal PSRemotingJob(PSSession[] remoteRunspaceInfos,
                        List<IThrottleOperation> runspaceHelpers, string remoteCommand, string name)
            : this(remoteRunspaceInfos, runspaceHelpers, remoteCommand, 0, name)
        { }

        /// <summary>
        /// Internal constructor for initializing PSRemotingJob using
        /// computer names.
        /// </summary>
        /// <param name="computerNames">names of computers for
        /// which the result object is being created</param>
        /// <param name="computerNameHelpers">list of helper objects
        /// corresponding to the computer names
        /// </param>
        /// <param name="remoteCommand">remote command corresponding to this
        /// result object</param>
        /// <param name="throttleLimit">Throttle limit to use.</param>
        /// <param name="name">A friendly name for the job object.</param>
        internal PSRemotingJob(string[] computerNames,
                        List<IThrottleOperation> computerNameHelpers, string remoteCommand,
                            int throttleLimit, string name)
            : base(remoteCommand, name)
        {
            // Create child jobs for each object in the list
            foreach (ExecutionCmdletHelperComputerName helper in computerNameHelpers)
            {
                // Create Child Job and Register for its StateChanged Event
                PSRemotingChildJob childJob = new PSRemotingChildJob(remoteCommand,
                                            helper, _throttleManager);
                childJob.StateChanged += HandleChildJobStateChanged;
                childJob.JobUnblocked += HandleJobUnblocked;

                // Add this job to list of childjobs
                ChildJobs.Add(childJob);
            }

            CommonInit(throttleLimit, computerNameHelpers);
        }

        /// <summary>
        /// Internal constructor for initializing job using
        /// PSSession objects.
        /// </summary>
        /// <param name="remoteRunspaceInfos">array of runspace info
        /// objects on which the remote command is executed</param>
        /// <param name="runspaceHelpers">List of helper objects for the
        /// runspaces</param>
        /// <param name="remoteCommand"> remote command corresponding to this
        /// result object</param>
        /// <param name="throttleLimit">Throttle limit to use.</param>
        /// <param name="name"></param>
        internal PSRemotingJob(PSSession[] remoteRunspaceInfos,
                        List<IThrottleOperation> runspaceHelpers, string remoteCommand,
                        int throttleLimit, string name)
            : base(remoteCommand, name)
        {
            // Create child jobs for each object in the list
            for (int i = 0; i < remoteRunspaceInfos.Length; i++)
            {
                ExecutionCmdletHelperRunspace helper = (ExecutionCmdletHelperRunspace)runspaceHelpers[i];

                // Create Child Job object and Register for its state changed event
                PSRemotingChildJob job = new PSRemotingChildJob(remoteCommand,
                                helper, _throttleManager);
                job.StateChanged += HandleChildJobStateChanged;
                job.JobUnblocked += HandleJobUnblocked;

                // Add the child job to list of child jobs
                ChildJobs.Add(job);
            }

            CommonInit(throttleLimit, runspaceHelpers);
        }

        /// <summary>
        /// Creates a job object and child jobs for each disconnected pipeline/runspace
        /// provided in the list of ExecutionCmdletHelperRunspace items.  The runspace
        /// object must have a remote running command that can be connected to.
        /// Use Connect() method to transition to the connected state.
        /// </summary>
        /// <param name="helpers">List of DisconnectedJobOperation objects with disconnected pipelines.</param>
        /// <param name="throttleLimit">Throttle limit value.</param>
        /// <param name="name">Job name.</param>
        /// <param name="aggregateResults">Aggregate results.</param>
        internal PSRemotingJob(List<IThrottleOperation> helpers,
                               int throttleLimit, string name, bool aggregateResults)
            : base(string.Empty, name)
        {
            // All pipeline objects must be in "disconnected" state and associated to running
            // remote commands.  Once the jobs are connected they can be stopped using the
            // ExecutionCmdletHelperRunspace object and ThrottleManager.
            foreach (ExecutionCmdletHelper helper in helpers)
            {
                PSRemotingChildJob job = new PSRemotingChildJob(helper, _throttleManager, aggregateResults);
                job.StateChanged += HandleChildJobStateChanged;
                job.JobUnblocked += HandleJobUnblocked;

                ChildJobs.Add(job);
            }

            // Since no results are produced by any streams. We should
            // close all the streams.
            base.CloseAllStreams();

            // Set status to "disconnected".
            SetJobState(JobState.Disconnected);

            // Submit the disconnected operation helpers to the throttle manager
            _throttleManager.ThrottleLimit = throttleLimit;
            _throttleManager.SubmitOperations(helpers);
            _throttleManager.EndSubmitOperations();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected PSRemotingJob() { }

        /// <summary>
        /// Initialization common to both constructors.
        /// </summary>
        private void CommonInit(int throttleLimit, List<IThrottleOperation> helpers)
        {
            // Since no results are produced by any streams. We should
            // close all the streams
            base.CloseAllStreams();

            // set status to "in progress"
            SetJobState(JobState.Running);

            // submit operations to the throttle manager
            _throttleManager.ThrottleLimit = throttleLimit;
            _throttleManager.SubmitOperations(helpers);
            _throttleManager.EndSubmitOperations();
        }
        #endregion Internal Constructors

        #region internal methods

        /// <summary>
        /// Get entity result for the specified computer.
        /// </summary>
        /// <param name="computerName">computername for which entity
        /// result is required</param>
        /// <returns>Entity result.</returns>
        internal List<Job> GetJobsForComputer(string computerName)
        {
            List<Job> returnJobList = new List<Job>();

            foreach (Job j in ChildJobs)
            {
                if (!(j is PSRemotingChildJob child)) continue;
                if (string.Equals(child.Runspace.ConnectionInfo.ComputerName, computerName,
                                StringComparison.OrdinalIgnoreCase))
                {
                    returnJobList.Add(child);
                }
            }

            return returnJobList;
        }

        /// <summary>
        /// Get entity result for the specified runspace.
        /// </summary>
        /// <param name="runspace">runspace for which entity
        /// result is required</param>
        /// <returns>Entity result.</returns>
        internal List<Job> GetJobsForRunspace(PSSession runspace)
        {
            List<Job> returnJobList = new List<Job>();

            foreach (Job j in ChildJobs)
            {
                if (!(j is PSRemotingChildJob child)) continue;
                if (child.Runspace.InstanceId.Equals(runspace.InstanceId))
                {
                    returnJobList.Add(child);
                }
            }

            return returnJobList;
        }

        /// <summary>
        /// Get entity result for the specified helper object.
        /// </summary>
        /// <param name="operation">helper for which entity
        /// result is required</param>
        /// <returns>Entity result.</returns>
        internal List<Job> GetJobsForOperation(IThrottleOperation operation)
        {
            List<Job> returnJobList = new List<Job>();
            ExecutionCmdletHelper helper = operation as ExecutionCmdletHelper;

            foreach (Job j in ChildJobs)
            {
                if (!(j is PSRemotingChildJob child)) continue;
                if (child.Helper.Equals(helper))
                {
                    returnJobList.Add(child);
                }
            }

            return returnJobList;
        }

        #endregion internal methods

        #region Connection Support

        /// <summary>
        /// Connect all child jobs if they are in a disconnected state.
        /// </summary>
        internal void ConnectJobs()
        {
            // Create connect operation objects for each child job object to connect.
            List<IThrottleOperation> connectJobOperations = new List<IThrottleOperation>();
            foreach (PSRemotingChildJob childJob in ChildJobs)
            {
                if (childJob.JobStateInfo.State == JobState.Disconnected)
                {
                    connectJobOperations.Add(new ConnectJobOperation(childJob));
                }
            }

            if (connectJobOperations.Count == 0)
            {
                return;
            }

            // Submit the connect job operation.
            // Return only after the connect operation completes.
            SubmitAndWaitForConnect(connectJobOperations);
        }

        /// <summary>
        /// Connect a single child job associated with the provided runspace.
        /// </summary>
        /// <param name="runspaceInstanceId">Runspace instance id for child job.</param>
        internal void ConnectJob(Guid runspaceInstanceId)
        {
            List<IThrottleOperation> connectJobOperations = new List<IThrottleOperation>();
            PSRemotingChildJob childJob = FindDisconnectedChildJob(runspaceInstanceId);
            if (childJob != null)
            {
                connectJobOperations.Add(new ConnectJobOperation(childJob));
            }

            if (connectJobOperations.Count == 0)
            {
                return;
            }

            // Submit the connect job operation.
            // Return only after the connect operation completes.
            SubmitAndWaitForConnect(connectJobOperations);
        }

        private static void SubmitAndWaitForConnect(List<IThrottleOperation> connectJobOperations)
        {
            using (ThrottleManager connectThrottleManager = new ThrottleManager())
            {
                using (ManualResetEvent connectResult = new ManualResetEvent(false))
                {
                    EventHandler<EventArgs> throttleCompleteEventHandler =
                        (object sender, EventArgs eventArgs) => connectResult.Set();

                    connectThrottleManager.ThrottleComplete += throttleCompleteEventHandler;
                    try
                    {
                        connectThrottleManager.ThrottleLimit = 0;
                        connectThrottleManager.SubmitOperations(connectJobOperations);
                        connectThrottleManager.EndSubmitOperations();

                        connectResult.WaitOne();
                    }
                    finally
                    {
                        connectThrottleManager.ThrottleComplete -= throttleCompleteEventHandler;
                    }
                }
            }
        }

        /// <summary>
        /// Simple throttle operation class for connecting jobs.
        /// </summary>
        private sealed class ConnectJobOperation : IThrottleOperation
        {
            private readonly PSRemotingChildJob _psRemoteChildJob;

            internal ConnectJobOperation(PSRemotingChildJob job)
            {
                _psRemoteChildJob = job;
                _psRemoteChildJob.StateChanged += ChildJobStateChangedHandler;
            }

            internal override void StartOperation()
            {
                bool startedSuccessfully = true;

                try
                {
                    _psRemoteChildJob.ConnectAsync();
                }
                catch (InvalidJobStateException e)
                {
                    startedSuccessfully = false;

                    string msg = StringUtil.Format(RemotingErrorIdStrings.JobConnectFailed, _psRemoteChildJob.Name);
                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "PSJobConnectFailed", ErrorCategory.InvalidOperation, _psRemoteChildJob);
                    _psRemoteChildJob.WriteError(errorRecord);
                }

                if (!startedSuccessfully)
                {
                    RemoveEventCallback();
                    SendStartComplete();
                }
            }

            internal override void StopOperation()
            {
                RemoveEventCallback();

                // Cannot stop a connect attempt.
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StopComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            internal override event EventHandler<OperationStateEventArgs> OperationComplete;

            private void ChildJobStateChangedHandler(object sender, JobStateEventArgs eArgs)
            {
                if (eArgs.JobStateInfo.State == JobState.Disconnected)
                {
                    return;
                }

                RemoveEventCallback();
                SendStartComplete();
            }

            private void SendStartComplete()
            {
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StartComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            private void RemoveEventCallback()
            {
                _psRemoteChildJob.StateChanged -= ChildJobStateChangedHandler;
            }
        }

        /// <summary>
        /// Finds the disconnected child job associated with this runspace and returns
        /// the PowerShell object that is remotely executing the command.
        /// </summary>
        /// <param name="runspaceInstanceId">Runspace instance Id.</param>
        /// <returns>Associated PowerShell object.</returns>
        internal PowerShell GetAssociatedPowerShellObject(Guid runspaceInstanceId)
        {
            PowerShell ps = null;
            PSRemotingChildJob childJob = FindDisconnectedChildJob(runspaceInstanceId);
            if (childJob != null)
            {
                ps = childJob.GetPowerShell();
            }

            return ps;
        }

        /// <summary>
        /// Helper method to find a disconnected child job associated with
        /// a given runspace.
        /// </summary>
        /// <param name="runspaceInstanceId">Runspace Id.</param>
        /// <returns>PSRemotingChildJob object.</returns>
        private PSRemotingChildJob FindDisconnectedChildJob(Guid runspaceInstanceId)
        {
            PSRemotingChildJob rtnJob = null;

            foreach (PSRemotingChildJob childJob in this.ChildJobs)
            {
                if ((childJob.Runspace.InstanceId.Equals(runspaceInstanceId)) &&
                    (childJob.JobStateInfo.State == JobState.Disconnected))
                {
                    rtnJob = childJob;
                    break;
                }
            }

            return rtnJob;
        }

        /// <summary>
        /// Internal method to stop a job without first connecting it if it is in
        /// a disconnected state.  This supports Receive-PSSession where it abandons
        /// a currently running/disconnected job when user selects -OutTarget Host.
        /// </summary>
        internal void InternalStopJob()
        {
            if (_isDisposed || _stopIsCalled || IsFinishedState(JobStateInfo.State))
            {
                return;
            }

            lock (_syncObject)
            {
                if (_isDisposed || _stopIsCalled || IsFinishedState(JobStateInfo.State))
                {
                    return;
                }

                _stopIsCalled = true;
            }

            _throttleManager.StopAllOperations();

            Finished.WaitOne();
        }

        #endregion

        private bool _moreData = true;
        /// <summary>
        /// Indicates if more data is available.
        /// </summary>
        /// <remarks>
        /// This has more data if any of the child jobs have more data.
        /// </remarks>
        public override bool HasMoreData
        {
            get
            {
                // moreData is initially set to true, and it
                // will remain so until the async result
                // object has completed execution.

                if (_moreData && IsFinishedState(JobStateInfo.State))
                {
                    bool atleastOneChildHasMoreData = false;

                    for (int i = 0; i < ChildJobs.Count; i++)
                    {
                        if (ChildJobs[i].HasMoreData)
                        {
                            atleastOneChildHasMoreData = true;
                            break;
                        }
                    }

                    _moreData = atleastOneChildHasMoreData;
                }

                return _moreData;
            }
        }

        private bool _stopIsCalled = false;
        /// <summary>
        /// Stop Job.
        /// </summary>
        public override void StopJob()
        {
            // If the job is in a disconnected state then try to connect it
            // so that it can be stopped on the server.
            if (JobStateInfo.State == JobState.Disconnected)
            {
                bool ConnectSuccessful;

                try
                {
                    ConnectJobs();
                    ConnectSuccessful = true;
                }
                catch (InvalidRunspaceStateException)
                {
                    ConnectSuccessful = false;
                }
                catch (PSRemotingTransportException)
                {
                    ConnectSuccessful = false;
                }
                catch (PSInvalidOperationException)
                {
                    ConnectSuccessful = false;
                }

                if (!ConnectSuccessful && this.Error.IsOpen)
                {
                    string msg = StringUtil.Format(RemotingErrorIdStrings.StopJobNotConnected, this.Name);
                    Exception reason = new RuntimeException(msg);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "StopJobCannotConnectToServer",
                        ErrorCategory.InvalidOperation, this);
                    WriteError(errorRecord);

                    return;
                }
            }

            InternalStopJob();
        }

        private string _statusMessage;
        /// <summary>
        /// Message indicating status of the job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
        }

        /// <summary>
        /// Used by Invoke-Command cmdlet to show/hide computername property value.
        /// Format and Output has capability to understand RemoteObjects and this property lets
        /// Format and Output decide whether to show/hide computername.
        /// Default is true.
        /// </summary>
        internal bool HideComputerName
        {
            get
            {
                return _hideComputerName;
            }

            set
            {
                _hideComputerName = value;
                foreach (Job job in this.ChildJobs)
                {
                    PSRemotingChildJob rJob = job as PSRemotingChildJob;
                    if (rJob != null)
                    {
                        rJob.HideComputerName = value;
                    }
                }
            }
        }

        private bool _hideComputerName = true;

        // ISSUE: Implement StatusMessage
        /// <summary>
        /// Checks the status of remote command execution.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void SetStatusMessage()
        {
            _statusMessage = "test";
            //        bool localErrors = false; // if local errors are present
            //        bool setFinished = false;    // if finished needs to be set

            //        statusMessage = "OK";

            //        lock (syncObject)
            //        {
            //            if (finishedCount == ChildJobs.Count)
            //            {
            //                // ISSUE: Change this code to look in to child jobs for exception
            //                if (errors.Count > 0)
            //                {
            //                    statusMessage = "LocalErrors";
            //                    localErrors = true;
            //                }

            //                // check for status of remote command
            //                for (int i = 0; i < ChildJobs.Count; i++)
            //                {
            //                    PSRemotingChildJob childJob = ChildJobs[i] as PSRemotingChildJob;
            //                    if (childJob == null) continue;
            //                    if (childJob.ContainsErrors)
            //                    {
            //                        if (localErrors)
            //                        {
            //                            statusMessage = "LocalAndRemoteErrors";
            //                        }
            //                        else
            //                        {
            //                            statusMessage = "RemoteErrors";
            //                        }
            //                        break;
            //                    }
            //                }

            //                setFinished = true;
            //            }
            //        }
        }

        #region finish logic

        // This variable is set to true if at least one child job failed.
        private bool _atleastOneChildJobFailed = false;

        // count of number of child jobs which have finished
        private int _finishedChildJobsCount = 0;

        // count of number of child jobs which are blocked
        private int _blockedChildJobsCount = 0;

        // count of child jobs that are in disconnected state.
        private int _disconnectedChildJobsCount = 0;

        // count of child jobs that are in Debug halted state.
        private int _debugChildJobsCount = 0;

        /// <summary>
        /// Handles the StateChanged event from each of the child job objects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleChildJobStateChanged(object sender, JobStateEventArgs e)
        {
            // Update object state to reflect disconnect state related changes in child jobs.
            CheckDisconnectedAndUpdateState(e.JobStateInfo.State, e.PreviousJobStateInfo.State);

            if (e.JobStateInfo.State == JobState.Blocked)
            {
                // increment count of blocked child jobs
                lock (_syncObject)
                {
                    _blockedChildJobsCount++;
                }

                // if any of the child job is blocked, we set state to blocked
                SetJobState(JobState.Blocked, null);
                return;
            }

            // Handle transition of child job to Debug halt state.
            if (e.JobStateInfo.State == JobState.AtBreakpoint)
            {
                lock (_syncObject) { _debugChildJobsCount++; }

                // If any child jobs are Debug halted, we set state to Debug.
                SetJobState(JobState.AtBreakpoint);
                return;
            }

            // Handle transition of child job back to running state.
            if ((e.JobStateInfo.State == JobState.Running) &&
                (e.PreviousJobStateInfo.State == JobState.AtBreakpoint))
            {
                int totalDebugCount;
                lock (_syncObject) { totalDebugCount = --_debugChildJobsCount; }

                if (totalDebugCount == 0)
                {
                    SetJobState(JobState.Running);
                    return;
                }
            }

            // Ignore state changes which are not resulting in state change to finished.
            if (!IsFinishedState(e.JobStateInfo.State))
            {
                return;
            }

            if (e.JobStateInfo.State == JobState.Failed)
            {
                // If any of the child job failed, we set status to failed
                _atleastOneChildJobFailed = true;
            }

            bool allChildJobsFinished = false;
            lock (_syncObject)
            {
                _finishedChildJobsCount++;

                // We are done
                if (_finishedChildJobsCount + _disconnectedChildJobsCount
                    == ChildJobs.Count)
                {
                    allChildJobsFinished = true;
                }
            }

            if (allChildJobsFinished)
            {
                // if any child job failed, set status to failed
                // If stop was called set, status to stopped
                // else completed
                if (_disconnectedChildJobsCount > 0)
                {
                    SetJobState(JobState.Disconnected);
                }
                else if (_atleastOneChildJobFailed)
                {
                    SetJobState(JobState.Failed);
                }
                else if (_stopIsCalled)
                {
                    SetJobState(JobState.Stopped);
                }
                else
                {
                    SetJobState(JobState.Completed);
                }
            }
        }

        /// <summary>
        /// Updates the parent job state based on state of all child jobs.
        /// </summary>
        /// <param name="newState">New child job state.</param>
        /// <param name="prevState">Previous child job state.</param>
        private void CheckDisconnectedAndUpdateState(JobState newState, JobState prevState)
        {
            if (IsFinishedState(JobStateInfo.State))
            {
                return;
            }

            // Do all logic inside a lock to ensure it is atomic against
            // multiple job thread state changes.
            lock (_syncObject)
            {
                if (newState == JobState.Disconnected)
                {
                    ++(_disconnectedChildJobsCount);

                    // If previous state was Blocked then we need to decrement the count
                    // since it is now Disconnected.
                    if (prevState == JobState.Blocked)
                    {
                        --_blockedChildJobsCount;
                    }

                    // If all unfinished and unblocked child jobs are disconnected then this
                    // parent job becomes disconnected.
                    if ((_disconnectedChildJobsCount +
                         _finishedChildJobsCount +
                         _blockedChildJobsCount) == ChildJobs.Count)
                    {
                        SetJobState(JobState.Disconnected, null);
                    }
                }
                else
                {
                    if (prevState == JobState.Disconnected)
                    {
                        --(_disconnectedChildJobsCount);
                    }

                    if ((newState == JobState.Running) && (JobStateInfo.State == JobState.Disconnected))
                    {
                        // Note that SetJobState() takes a lock so it is unnecessary to do
                        // this under a lock here.
                        SetJobState(JobState.Running, null);
                    }
                }
            }
        }

        #endregion finish logic

        /// <summary>
        /// Release all the resources.
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (_syncObject)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                }

                try
                {
                    if (!IsFinishedState(JobStateInfo.State))
                    {
                        StopJob();
                    }

                    foreach (Job job in ChildJobs)
                    {
                        job.Dispose();
                    }

                    _throttleManager.Dispose();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        private bool _isDisposed = false;

        private string ConstructLocation()
        {
            StringBuilder location = new StringBuilder();

            if (ChildJobs.Count > 0)
            {
                foreach (PSRemotingChildJob job in ChildJobs)
                {
                    location.Append(job.Location);
                    location.Append(',');
                }

                location.Remove(location.Length - 1, 1);
            }

            return location.ToString();
        }

        /// <summary>
        /// Computers on which this job is running.
        /// </summary>
        public override string Location
        {
            get
            {
                return ConstructLocation();
            }
        }

        /// <summary>
        /// Returns boolean indicating whether the underlying
        /// transport for the job (or child jobs) supports
        /// connect/disconnect semantics.
        /// </summary>
        internal override bool CanDisconnect
        {
            get
            {
                // If one child job can disconnect then all of them can since
                // all child jobs use the same remote runspace transport.
                return (ChildJobs.Count > 0) && ChildJobs[0].CanDisconnect;
            }
        }

        /// <summary>
        /// Returns runspaces associated with the Job, including
        /// child jobs.
        /// </summary>
        /// <returns>IEnumerable of RemoteRunspaces.</returns>
        internal override IEnumerable<RemoteRunspace> GetRunspaces()
        {
            List<RemoteRunspace> runspaces = new List<RemoteRunspace>();
            foreach (PSRemotingChildJob job in ChildJobs)
            {
                runspaces.Add(job.Runspace as RemoteRunspace);
            }

            return runspaces;
        }

        /// <summary>
        /// Handles JobUnblocked event from a child job and decrements
        /// count of blocked child jobs. When count reaches 0, sets the
        /// state of the parent job to running.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">event arguments, should be empty in this
        /// case</param>
        private void HandleJobUnblocked(object sender, EventArgs eventArgs)
        {
            bool unblockjob = false;

            lock (_syncObject)
            {
                _blockedChildJobsCount--;

                if (_blockedChildJobsCount == 0)
                {
                    unblockjob = true;
                }
            }

            if (unblockjob)
            {
                SetJobState(JobState.Running, null);
            }
        }

        #region Private Members

        private readonly ThrottleManager _throttleManager = new ThrottleManager();

        private readonly object _syncObject = new object();           // sync object

        #endregion Private Members
    }

    #region DisconnectedJobOperation class

    /// <summary>
    /// Simple throttle operation class for PSRemoting jobs created in the
    /// disconnected state.
    /// </summary>
    internal class DisconnectedJobOperation : ExecutionCmdletHelper
    {
        internal DisconnectedJobOperation(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.pipeline.StateChanged += HandlePipelineStateChanged;
        }

        internal override void StartOperation()
        {
            // This is a no-op since disconnected jobs (pipelines) have
            // already been started.
        }

        internal override void StopOperation()
        {
            if (pipeline.PipelineStateInfo.State == PipelineState.Running ||
                pipeline.PipelineStateInfo.State == PipelineState.Disconnected ||
                pipeline.PipelineStateInfo.State == PipelineState.NotStarted)
            {
                // If the pipeline state has reached Complete/Failed/Stopped
                // by the time control reaches here, then this operation
                // becomes a no-op. However, an OperationComplete would have
                // already been raised from the handler.
                pipeline.StopAsync();
            }
            else
            {
                // Will have to raise OperationComplete from here,
                // else ThrottleManager will have
                SendStopComplete();
            }
        }

        internal override event EventHandler<OperationStateEventArgs> OperationComplete;

        private void HandlePipelineStateChanged(object sender, PipelineStateEventArgs stateEventArgs)
        {
            PipelineStateInfo stateInfo = stateEventArgs.PipelineStateInfo;

            switch (stateInfo.State)
            {
                case PipelineState.Running:
                case PipelineState.NotStarted:
                case PipelineState.Stopping:
                case PipelineState.Disconnected:
                    return;
            }

            SendStopComplete(stateEventArgs);
        }

        private void SendStopComplete(EventArgs eventArgs = null)
        {
            OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
            operationStateEventArgs.BaseEvent = eventArgs;
            operationStateEventArgs.OperationState = OperationState.StopComplete;
            OperationComplete.SafeInvoke(this, operationStateEventArgs);
        }
    }

    #endregion

    /// <summary>
    /// Class for RemotingChildJob object. This job object invokes command
    /// on one remote machine.
    /// </summary>
    /// <remarks>
    /// TODO: I am not sure whether to change this internal to just RemotingChildJob.
    /// </remarks>
    /// <remarks>
    /// Not removing the prefix "PS" as this signifies powershell specific remoting job
    /// </remarks>
    internal class PSRemotingChildJob : Job, IJobDebugger
    {
        #region Internal Constructor

        /// <summary>
        /// Creates an instance of PSRemotingChildJob.
        /// </summary>
        /// <param name="remoteCommand">Command invoked by this job object.</param>
        /// <param name="helper"></param>
        /// <param name="throttleManager"></param>
        internal PSRemotingChildJob(string remoteCommand, ExecutionCmdletHelper helper, ThrottleManager throttleManager)
            : base(remoteCommand)
        {
            UsesResultsCollection = true;
            Dbg.Assert(helper.Pipeline is RemotePipeline, "Pipeline passed should be a remote pipeline");

            Helper = helper;
            Runspace = helper.Pipeline.Runspace;
            _remotePipeline = helper.Pipeline as RemotePipeline;
            _throttleManager = throttleManager;

            RemoteRunspace remoteRS = Runspace as RemoteRunspace;
            if ((remoteRS != null) && (remoteRS.RunspaceStateInfo.State == RunspaceState.BeforeOpen))
            {
                remoteRS.URIRedirectionReported += HandleURIDirectionReported;
            }

            AggregateResultsFromHelper(helper);

            Runspace.AvailabilityChanged += HandleRunspaceAvailabilityChanged;

            RegisterThrottleComplete(throttleManager);
        }

        /// <summary>
        /// Constructs a disconnected child job that is able to connect to a remote
        /// runspace/command on a server.  The ExecutionCmdletHelperRunspace must
        /// contain a remote pipeline object in a disconnected state.  In addition
        /// the pipeline runspace must be associated with a valid running remote
        /// command that can be connected to.
        /// </summary>
        /// <param name="helper">ExecutionCmdletHelper object containing runspace and pipeline objects.</param>
        /// <param name="throttleManager">ThrottleManger object.</param>
        /// <param name="aggregateResults">Aggregate results.</param>
        internal PSRemotingChildJob(ExecutionCmdletHelper helper, ThrottleManager throttleManager, bool aggregateResults = false)
        {
            UsesResultsCollection = true;
            Dbg.Assert((helper.Pipeline is RemotePipeline), "Helper pipeline object should be a remote pipeline");
            Dbg.Assert((helper.Pipeline.PipelineStateInfo.State == PipelineState.Disconnected), "Remote pipeline object must be in Disconnected state.");

            Helper = helper;
            _remotePipeline = helper.Pipeline as RemotePipeline;
            Runspace = helper.Pipeline.Runspace;
            _throttleManager = throttleManager;

            if (aggregateResults)
            {
                AggregateResultsFromHelper(helper);
            }
            else
            {
                _remotePipeline.StateChanged += HandlePipelineStateChanged;
                _remotePipeline.Output.DataReady += HandleOutputReady;
                _remotePipeline.Error.DataReady += HandleErrorReady;
            }

            Runspace.AvailabilityChanged += HandleRunspaceAvailabilityChanged;

            IThrottleOperation operation = helper as IThrottleOperation;
            operation.OperationComplete += HandleOperationComplete;

            SetJobState(JobState.Disconnected, null);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected PSRemotingChildJob()
        {
        }

        #endregion Internal Constructor

        #region Internal Methods

        /// <summary>
        /// Connects the remote pipeline that this job represents.
        /// </summary>
        internal void ConnectAsync()
        {
            if (JobStateInfo.State != JobState.Disconnected)
            {
                throw new InvalidJobStateException(JobStateInfo.State);
            }

            _remotePipeline.ConnectAsync();
        }

        #endregion

        #region stop

        // bool isStopCalled = false;
        /// <summary>
        /// Stops the job.
        /// </summary>
        public override void StopJob()
        {
            if (_isDisposed || _stopIsCalled || IsFinishedState(JobStateInfo.State))
            {
                return;
            }

            lock (SyncObject)
            {
                if (_isDisposed || _stopIsCalled || IsFinishedState(JobStateInfo.State))
                {
                    return;
                }

                _stopIsCalled = true;
            }

            _throttleManager.StopOperation(Helper);

            // if IgnoreStop is set, then StopOperation will
            // return immediately, but StopJob should only
            // return when job is complete. Waiting on the
            // wait handle will ensure that its blocked
            // until the job reaches a terminal state
            Finished.WaitOne();
        }

        #endregion stop

        #region Properties

        /// <summary>
        /// Status Message associated with the Job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                // ISSUE implement this.
                return string.Empty;
            }
        }

        /// <summary>
        /// Indicates if there is more data available in
        /// this Job.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (Results.IsOpen || Results.Count > 0);
            }
        }

        /// <summary>
        /// Returns the computer on which this command is
        /// running.
        /// </summary>
        public override string Location
        {
            get
            {
                return (Runspace != null) ? Runspace.ConnectionInfo.ComputerName : string.Empty;
            }
        }

        /// <summary>
        /// </summary>
        public Runspace Runspace { get; }

        /// <summary>
        /// Helper associated with this entity.
        /// </summary>
        internal ExecutionCmdletHelper Helper { get; } = null;

        /// <summary>
        /// Used by Invoke-Command cmdlet to show/hide computername property value.
        /// Format and Output has capability to understand RemoteObjects and this property lets
        /// Format and Output decide whether to show/hide computername.
        /// Default is true.
        /// </summary>
        internal bool HideComputerName
        {
            get
            {
                return _hideComputerName;
            }

            set
            {
                _hideComputerName = value;
                foreach (Job job in this.ChildJobs)
                {
                    PSRemotingChildJob rJob = job as PSRemotingChildJob;
                    if (rJob != null)
                    {
                        rJob.HideComputerName = value;
                    }
                }
            }
        }

        private bool _hideComputerName = true;

        /// <summary>
        /// Property that indicates this disconnected child job was
        /// previously in the Blocked state.
        /// </summary>
        internal bool DisconnectedAndBlocked { get; private set; } = false;

        /// <summary>
        /// Returns boolean indicating whether the underlying
        /// transport for the job (or child jobs) supports
        /// connect/disconnect semantics.
        /// </summary>
        internal override bool CanDisconnect
        {
            get
            {
                RemoteRunspace remoteRS = Runspace as RemoteRunspace;
                return remoteRS != null && remoteRS.CanDisconnect;
            }
        }

        #endregion Properties

        #region IJobDebugger

        /// <summary>
        /// Job Debugger.
        /// </summary>
        public Debugger Debugger
        {
            get
            {
                if (_jobDebugger == null)
                {
                    lock (this.SyncObject)
                    {
                        if ((_jobDebugger == null) &&
                            (Runspace.Debugger != null))
                        {
                            _jobDebugger = new RemotingJobDebugger(Runspace.Debugger, Runspace, this.Name);
                        }
                    }
                }

                return _jobDebugger;
            }
        }

        /// <summary>
        /// True if job is synchronous and can be debugged.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }

            set { _isAsync = true; }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handler which will handle output ready events of the
        /// pipeline. The output objects are queued on to the
        /// internal stream.
        /// </summary>
        /// <param name="sender">the pipeline reader which raised
        /// this event</param>
        /// <param name="eventArgs">Information describing the ready event.</param>
        private void HandleOutputReady(object sender, EventArgs eventArgs)
        {
            PSDataCollectionPipelineReader<PSObject, PSObject> reader =
                    sender as PSDataCollectionPipelineReader<PSObject, PSObject>;

            Collection<PSObject> output = reader.NonBlockingRead();

            foreach (PSObject dataObject in output)
            {
                // attach origin information only if it doesn't exist
                // in case of a second-hop scenario, the origin information
                // will already be added at the second hop machine
                if (dataObject != null)
                {
                    // if the server has already added some properties, which we do not
                    // want to trust, we simply replace them with the server's
                    // identity we know of

                    if (dataObject.Properties[RemotingConstants.ComputerNameNoteProperty] != null)
                    {
                        dataObject.Properties.Remove(RemotingConstants.ComputerNameNoteProperty);
                    }

                    if (dataObject.Properties[RemotingConstants.RunspaceIdNoteProperty] != null)
                    {
                        dataObject.Properties.Remove(RemotingConstants.RunspaceIdNoteProperty);
                    }

                    dataObject.Properties.Add(new PSNoteProperty(RemotingConstants.ComputerNameNoteProperty, reader.ComputerName));
                    dataObject.Properties.Add(new PSNoteProperty(RemotingConstants.RunspaceIdNoteProperty, reader.RunspaceId));
                    // PSShowComputerName is present for all the objects (from remoting)..this is to allow PSComputerName to be selected.
                    // Ex: Invoke-Command localhost,blah { gps } | select PSComputerName should work.
                    if (dataObject.Properties[RemotingConstants.ShowComputerNameNoteProperty] == null)
                    {
                        PSNoteProperty showComputerNameNP = new PSNoteProperty(RemotingConstants.ShowComputerNameNoteProperty, !_hideComputerName);
                        dataObject.Properties.Add(showComputerNameNP);
                    }
                }

                this.WriteObject(dataObject);
            }
        }

        /// <summary>
        /// Handler which will handle error ready events of the
        /// pipeline. The error records are queued on to the
        /// internal stream.
        /// </summary>
        /// <param name="sender">the pipeline reader which raised
        /// this event</param>
        /// <param name="eventArgs">Information describing the ready event.</param>
        private void HandleErrorReady(object sender, EventArgs eventArgs)
        {
            PSDataCollectionPipelineReader<ErrorRecord, object> reader =
                sender as PSDataCollectionPipelineReader<ErrorRecord, object>;

            Collection<object> error = reader.NonBlockingRead();

            foreach (object errorData in error)
            {
                ErrorRecord er = errorData as ErrorRecord;
                if (er != null)
                {
                    OriginInfo originInfo = new OriginInfo(reader.ComputerName, reader.RunspaceId);

                    RemotingErrorRecord errorRecord =
                        new RemotingErrorRecord(er, originInfo);
                    errorRecord.PreserveInvocationInfoOnce = true;

                    // ISSUE: Add an Assert for ErrorRecord.
                    // Add to the PSRemotingChild jobs streams
                    this.WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// When the client remote session reports a URI redirection, this method will report the
        /// message to the user as a Warning using Host method calls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void HandleURIDirectionReported(object sender, RemoteDataEventArgs<Uri> eventArgs)
        {
            string message = StringUtil.Format(RemotingErrorIdStrings.URIRedirectWarningToHost, eventArgs.Data.OriginalString);
            this.WriteWarning(message);
        }

        /// <summary>
        /// Handle method executor stream events.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The event args.</param>
        private void HandleHostCalls(object sender, EventArgs eventArgs)
        {
            ObjectStream hostCallsStream = sender as ObjectStream;

            if (hostCallsStream != null)
            {
                Collection<object> hostCallMethodExecutors =
                    hostCallsStream.NonBlockingRead(hostCallsStream.Count);

                lock (SyncObject)
                {
                    foreach (ClientMethodExecutor hostCallMethodExecutor in hostCallMethodExecutors)
                    {
                        Results.Add(new PSStreamObject(PSStreamObjectType.MethodExecutor, hostCallMethodExecutor));

                        // if the call id of the underlying remote host call is not ServerDispatchTable.VoidCallId
                        // then the call is waiting on user input. Change state to Blocked
                        if (hostCallMethodExecutor.RemoteHostCall.CallId != ServerDispatchTable.VoidCallId)
                        {
                            SetJobState(JobState.Blocked, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle changes in pipeline states.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandlePipelineStateChanged(object sender, PipelineStateEventArgs e)
        {
            if ((Runspace != null) && (e.PipelineStateInfo.State != PipelineState.Running))
            {
                // since we got state changed event..we dont need to listen on
                // URI redirections anymore
                ((RemoteRunspace)Runspace).URIRedirectionReported -= HandleURIDirectionReported;
            }

            PipelineState state = e.PipelineStateInfo.State;
            switch (state)
            {
                case PipelineState.Running:
                    if (DisconnectedAndBlocked)
                    {
                        DisconnectedAndBlocked = false;
                        SetJobState(JobState.Blocked);
                    }
                    else
                    {
                        SetJobState(JobState.Running);
                    }

                    break;

                case PipelineState.Disconnected:
                    DisconnectedAndBlocked = (JobStateInfo.State == JobState.Blocked);
                    SetJobState(JobState.Disconnected);
                    break;
            }

            // Question: Why is the DoFinish() call on terminal pipeline state deleted
            // Answer: Because in the runspace case, when pipeline reaches a terminal state
            // OperationComplete will be raised and DoFinish() is called on OperationComplete
            // In the computer name case, once pipeline reaches a terminal state, runspace is
            // closed which will result in an OperationComplete event
        }

        /// <summary>
        /// Handle a throttle complete event.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">Not used in this method.</param>
        private void HandleThrottleComplete(object sender, EventArgs eventArgs)
        {
            // Question: Why do we register for HandleThrottleComplete when we have already
            // registered for PipelineStateChangedEvent?
            // Answer: Because ThrottleManager at a given time can have some pipelines which are
            // still not started. If TM.Stop() is called, then it simply discards those pipelines and
            // PipelineStateChangedEvent is not called for them. For such jobs, we depend on
            // HandleThrottleComplete to mark the finish of job.

            // Question: So it is possible in some cases DoFinish can be called twice.
            // Answer: Yes: One from PipelineStateChangedEvent and Another here. But
            // DoFinish has logic to check if it has been already called and second call
            // becomes noOp.
            DoFinish();
        }

        /// <summary>
        /// Handle the operation complete event.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="stateEventArgs">Operation complete event args.</param>
        protected virtual void HandleOperationComplete(object sender, OperationStateEventArgs stateEventArgs)
        {
            // Question:Why are we registering for OperationComplete if we already
            // registering for StateChangedEvent and ThrottleComplete event
            // Answer:Because in case of computer, if Runspace.Open it self fails,
            // no pipeline is created and no pipeline state changed event is raised.
            // We can wait for throttle complete, but it is raised only when all the
            // operations are completed and this means that status of job is not updated
            // until Operation Complete.
            ExecutionCmdletHelper helper = sender as ExecutionCmdletHelper;
            Dbg.Assert(helper != null, "Sender of OperationComplete has to be ExecutionCmdletHelper");

            DeterminedAndSetJobState(helper);
        }

        private bool _doFinishCalled = false;

        /// <summary>
        /// This method marks the completion state for Job. Also if job failed, it processes the
        /// reason of failure.
        /// </summary>
        protected virtual void DoFinish()
        {
            if (_doFinishCalled)
                return;

            lock (SyncObject)
            {
                if (_doFinishCalled)
                    return;

                _doFinishCalled = true;
            }

            DeterminedAndSetJobState(Helper);

            DoCleanupOnFinished();
        }

        /// <summary>
        /// This is the pretty formated error record associated with the reason of failure.
        /// </summary>
        private ErrorRecord _failureErrorRecord;

        /// <summary>
        /// This is the pretty formated error record associated with the reason of failure.
        /// This is set if Job state is Failed and Reason has a exception.
        /// </summary>
        internal ErrorRecord FailureErrorRecord
        {
            get
            {
                return _failureErrorRecord;
            }
        }

        /// <summary>
        /// Process the exceptions to decide reason for job failure.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="failureException"></param>
        /// <param name="failureErrorRecord"></param>
        protected void ProcessJobFailure(ExecutionCmdletHelper helper, out Exception failureException,
                            out ErrorRecord failureErrorRecord)
        {
            //      There are three errors possible
            //      1. The remote runspace is in (or went into) a
            //         broken state. This information is available
            //         in the runspace state information
            //      2. The remote pipeline failed because of an
            //         exception. This information is available
            //         in the pipeline state information
            //      3. Runspace.OpenAsync or Pipeline.InvokeAsync threw exception
            //         They are in Helper.InternalException

            Dbg.Assert(helper != null, "helper is null");

            RemotePipeline pipeline = helper.Pipeline as RemotePipeline;
            Dbg.Assert(pipeline != null, "pipeline is null");

            RemoteRunspace runspace = pipeline.GetRunspace() as RemoteRunspace;
            Dbg.Assert(runspace != null, "runspace is null");

            failureException = null;
            failureErrorRecord = null;

            if (helper.InternalException != null)
            {
                string errorId = "RemotePipelineExecutionFailed";
                failureException = helper.InternalException;
                if ((failureException is InvalidRunspaceStateException) || (failureException is InvalidRunspacePoolStateException))
                {
                    errorId = "InvalidSessionState";
                    if (!string.IsNullOrEmpty(failureException.Source))
                    {
                        errorId = string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{errorId},{failureException.Source}");
                    }
                }

                failureErrorRecord = new ErrorRecord(helper.InternalException,
                       errorId, ErrorCategory.OperationStopped,
                            helper);
            }
            // there is a failure reason available in the runspace
            else if ((runspace.RunspaceStateInfo.State == RunspaceState.Broken) ||
                     (runspace.RunspaceStateInfo.Reason != null))
            {
                failureException = runspace.RunspaceStateInfo.Reason;
                object targetObject = runspace.ConnectionInfo.ComputerName;

                string errorDetails = null;

                // set the transport message in the error detail so that
                // the user can directly get to see the message without
                // having to mine through the error record details
                PSRemotingTransportException transException =
                            failureException as PSRemotingTransportException;

                string fullyQualifiedErrorId =
                    System.Management.Automation.Remoting.Client.WSManTransportManagerUtils.GetFQEIDFromTransportError(
                        (transException != null) ? transException.ErrorCode : 0,
                        "PSSessionStateBroken");

                if (transException != null)
                {
                    errorDetails = "[" + runspace.ConnectionInfo.ComputerName + "] ";

                    if (transException.ErrorCode ==
                        Remoting.Client.WSManNativeApi.ERROR_WSMAN_REDIRECT_REQUESTED)
                    {
                        // Handling a special case for redirection..we should talk about
                        // AllowRedirection parameter and WSManMaxRedirectionCount preference
                        // variables
                        string message = PSRemotingErrorInvariants.FormatResourceString(
                            RemotingErrorIdStrings.URIRedirectionReported,
                            transException.Message,
                            "MaximumConnectionRedirectionCount",
                            Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.DEFAULT_SESSION_OPTION,
                            "AllowRedirection");

                        errorDetails += message;
                    }
                    else if (!string.IsNullOrEmpty(transException.Message))
                    {
                        errorDetails += transException.Message;
                    }
                    else if (!string.IsNullOrEmpty(transException.TransportMessage))
                    {
                        errorDetails += transException.TransportMessage;
                    }
                }

                failureException ??= new RuntimeException(
                    PSRemotingErrorInvariants.FormatResourceString(
                        RemotingErrorIdStrings.RemoteRunspaceOpenUnknownState,
                        runspace.RunspaceStateInfo.State));

                failureErrorRecord = new ErrorRecord(failureException, targetObject,
                                fullyQualifiedErrorId, ErrorCategory.OpenError,
                                null, null, null, null, null, errorDetails, null);
            }
            else if (pipeline.PipelineStateInfo.State == PipelineState.Failed
                || (pipeline.PipelineStateInfo.State == PipelineState.Stopped
                    && pipeline.PipelineStateInfo.Reason != null
                    && pipeline.PipelineStateInfo.Reason is not PipelineStoppedException))
            {
                // Pipeline stopped state is also an error condition if the associated exception is not 'PipelineStoppedException'.
                object targetObject = runspace.ConnectionInfo.ComputerName;
                failureException = pipeline.PipelineStateInfo.Reason;
                if (failureException != null)
                {
                    RemoteException rException = failureException as RemoteException;

                    ErrorRecord errorRecord = null;
                    if (rException != null)
                    {
                        errorRecord = rException.ErrorRecord;

                        // A RemoteException will hide a PipelineStoppedException, which should be ignored.
                        if (errorRecord != null &&
                            errorRecord.FullyQualifiedErrorId.Equals("PipelineStopped", StringComparison.OrdinalIgnoreCase))
                        {
                            // PipelineStoppedException should not be reported as error.
                            failureException = null;
                            return;
                        }
                    }
                    else
                    {
                        // at this point, there may be no failure reason available in
                        // the runspace because the remoting protocol
                        // layer may not have yet assigned it to the runspace
                        // in such a case, the remoting protocol layer would have
                        // assigned an exception in the client end to the pipeline
                        // create an error record from it and write it out
                        errorRecord = new ErrorRecord(pipeline.PipelineStateInfo.Reason,
                                                        "JobFailure", ErrorCategory.OperationStopped,
                                                            targetObject);
                    }

                    string computerName = ((RemoteRunspace)pipeline.GetRunspace()).ConnectionInfo.ComputerName;
                    Guid runspaceId = pipeline.GetRunspace().InstanceId;

                    OriginInfo originInfo = new OriginInfo(computerName, runspaceId);

                    failureErrorRecord = new RemotingErrorRecord(errorRecord, originInfo);
                }
            }
        }

        /// <summary>
        /// Release all the resources.
        /// </summary>
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (SyncObject)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                }

                try
                {
                    DoCleanupOnFinished();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        private bool _isDisposed = false;

        private bool _cleanupDone = false;

        /// <summary>
        /// Cleanup after state changes to finished.
        /// </summary>
        protected virtual void DoCleanupOnFinished()
        {
            bool doCleanup = false;
            if (!_cleanupDone)
            {
                lock (SyncObject)
                {
                    if (!_cleanupDone)
                    {
                        _cleanupDone = true;
                        doCleanup = true;
                    }
                }
            }

            if (!doCleanup) return;

            StopAggregateResultsFromHelper(Helper);
            Runspace.AvailabilityChanged -= HandleRunspaceAvailabilityChanged;
            IThrottleOperation operation = Helper as IThrottleOperation;
            operation.OperationComplete -= HandleOperationComplete;
            UnregisterThrottleComplete(_throttleManager);
            _throttleManager = null;
        }

        /// <summary>
        /// Aggregates results from the pipeline associated
        /// with the specified helper.
        /// </summary>
        /// <param name="helper">helper whose pipeline results
        /// need to be aggregated</param>
        protected void AggregateResultsFromHelper(ExecutionCmdletHelper helper)
        {
            // Get the pipeline associated with this helper and register for appropriate events
            Pipeline pipeline = helper.Pipeline;
            pipeline.Output.DataReady += HandleOutputReady;
            pipeline.Error.DataReady += HandleErrorReady;
            pipeline.StateChanged += HandlePipelineStateChanged;

            // Register handler for method executor object stream.
            Dbg.Assert(pipeline is RemotePipeline, "pipeline is RemotePipeline");
            RemotePipeline remotePipeline = pipeline as RemotePipeline;
            remotePipeline.MethodExecutorStream.DataReady += HandleHostCalls;
            remotePipeline.PowerShell.Streams.Progress.DataAdded += HandleProgressAdded;
            remotePipeline.PowerShell.Streams.Warning.DataAdded += HandleWarningAdded;
            remotePipeline.PowerShell.Streams.Verbose.DataAdded += HandleVerboseAdded;
            remotePipeline.PowerShell.Streams.Debug.DataAdded += HandleDebugAdded;
            remotePipeline.PowerShell.Streams.Information.DataAdded += HandleInformationAdded;

            // Enable method executor stream so that host methods are queued up
            // on it instead of being executed asynchronously when they arrive.
            remotePipeline.IsMethodExecutorStreamEnabled = true;

            IThrottleOperation operation = helper as IThrottleOperation;
            operation.OperationComplete += HandleOperationComplete;
        }

        /// <summary>
        /// If the pipeline is not null, returns the pipeline's PowerShell
        /// If it is null, then returns the PowerShell with the specified
        /// instance Id.
        /// </summary>
        /// <param name="pipeline">Remote pipeline.</param>
        /// <param name="instanceId">Instance as described in event args.</param>
        /// <returns>PowerShell instance.</returns>
        private PowerShell GetPipelinePowerShell(RemotePipeline pipeline, Guid instanceId)
        {
            if (pipeline != null)
            {
                return pipeline.PowerShell;
            }

            return GetPowerShell(instanceId);
        }

        /// <summary>
        /// When a debug message is raised in the underlying PowerShell
        /// add it to the jobs debug stream.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleDebugAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;
            PowerShell powershell = GetPipelinePowerShell(_remotePipeline, eventArgs.PowerShellInstanceId);

            if (powershell != null)
            {
                this.Debug.Add(powershell.Streams.Debug[index]);
            }
        }

        /// <summary>
        /// When a verbose message is raised in the underlying PowerShell
        /// add it to the jobs verbose stream.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleVerboseAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;
            PowerShell powershell = GetPipelinePowerShell(_remotePipeline, eventArgs.PowerShellInstanceId);

            if (powershell != null)
            {
                this.Verbose.Add(powershell.Streams.Verbose[index]);
            }
        }

        /// <summary>
        /// When a warning message is raised in the underlying PowerShell
        /// add it to the jobs warning stream.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleWarningAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;
            PowerShell powershell = GetPipelinePowerShell(_remotePipeline, eventArgs.PowerShellInstanceId);

            if (powershell != null)
            {
                WarningRecord warningRecord = powershell.Streams.Warning[index];
                this.Warning.Add(warningRecord);
                this.Results.Add(new PSStreamObject(PSStreamObjectType.WarningRecord, warningRecord));
            }
        }

        /// <summary>
        /// When a progress message is raised in the underlying PowerShell
        /// add it to the jobs progress tream.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleProgressAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;
            PowerShell powershell = GetPipelinePowerShell(_remotePipeline, eventArgs.PowerShellInstanceId);

            if (powershell != null)
            {
                this.Progress.Add(powershell.Streams.Progress[index]);
            }
        }

        /// <summary>
        /// When a Information message is raised in the underlying PowerShell
        /// add it to the jobs Information stream.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInformationAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;
            PowerShell powershell = GetPipelinePowerShell(_remotePipeline, eventArgs.PowerShellInstanceId);

            if (powershell != null)
            {
                InformationRecord informationRecord = powershell.Streams.Information[index];
                this.Information.Add(informationRecord);

                // Host output is handled by the hosting APIs directly, so we need to add a tag that it was
                // forwarded so that it is not written twice.
                //  For all other Information records, forward them.
                if (informationRecord.Tags.Contains("PSHOST"))
                {
                    informationRecord.Tags.Add("FORWARDED");
                }

                this.Results.Add(new PSStreamObject(PSStreamObjectType.Information, informationRecord));
            }
        }

        /// <summary>
        /// Stops collecting results from the pipeline associated with
        /// the specified helper.
        /// </summary>
        /// <param name="helper">helper class whose pipeline results
        /// aggregation has to be stopped</param>
        protected void StopAggregateResultsFromHelper(ExecutionCmdletHelper helper)
        {
            // Get the pipeline associated with this helper and register for appropriate events
            RemoveAggreateCallbacksFromHelper(helper);

            Pipeline pipeline = helper.Pipeline;
            pipeline.Dispose();
            pipeline = null;
        }

        /// <summary>
        /// Removes aggregate callbacks from pipeline so that a new job object can
        /// be created and can add its own callbacks.
        /// This is to support Invoke-Command auto-disconnect where a new PSRemoting
        /// job must be created to pass back to user for connection.
        /// </summary>
        /// <param name="helper">Helper class.</param>
        protected void RemoveAggreateCallbacksFromHelper(ExecutionCmdletHelper helper)
        {
            // Remove old data output callbacks from pipeline so new callbacks can be added.
            Pipeline pipeline = helper.Pipeline;
            pipeline.Output.DataReady -= HandleOutputReady;
            pipeline.Error.DataReady -= HandleErrorReady;
            pipeline.StateChanged -= HandlePipelineStateChanged;

            // Remove old data aggregation and host calls.
            Dbg.Assert(pipeline is RemotePipeline, "pipeline is RemotePipeline");
            RemotePipeline remotePipeline = pipeline as RemotePipeline;
            remotePipeline.MethodExecutorStream.DataReady -= HandleHostCalls;
            if (remotePipeline.PowerShell != null)
            {
                remotePipeline.PowerShell.Streams.Progress.DataAdded -= HandleProgressAdded;
                remotePipeline.PowerShell.Streams.Warning.DataAdded -= HandleWarningAdded;
                remotePipeline.PowerShell.Streams.Verbose.DataAdded -= HandleVerboseAdded;
                remotePipeline.PowerShell.Streams.Debug.DataAdded -= HandleDebugAdded;
                remotePipeline.PowerShell.Streams.Information.DataAdded -= HandleInformationAdded;
                remotePipeline.IsMethodExecutorStreamEnabled = false;
            }
        }

        /// <summary>
        /// Register for throttle complete from the specified
        /// throttlemanager.
        /// </summary>
        /// <param name="throttleManager"></param>
        protected void RegisterThrottleComplete(ThrottleManager throttleManager)
        {
            throttleManager.ThrottleComplete += HandleThrottleComplete;
        }

        /// <summary>
        /// Unregister for throttle complete from the specified
        /// throttle manager.
        /// </summary>
        /// <param name="throttleManager"></param>
        protected void UnregisterThrottleComplete(ThrottleManager throttleManager)
        {
            throttleManager.ThrottleComplete -= HandleThrottleComplete;
        }

        /// <summary>
        /// Determine the current state of the job based on the underlying
        /// pipeline state and set the state accordingly.
        /// </summary>
        /// <param name="helper"></param>
        protected void DeterminedAndSetJobState(ExecutionCmdletHelper helper)
        {
            Exception failureException;
            // Process the reason in case of failure.
            ProcessJobFailure(helper, out failureException, out _failureErrorRecord);

            if (failureException != null)
            {
                SetJobState(JobState.Failed, failureException);
            }
            else
            {
                // Get the state of the pipeline
                PipelineState state = helper.Pipeline.PipelineStateInfo.State;
                if (state == PipelineState.NotStarted)
                {
                    // This is a case in which pipeline was not started and TM.Stop was
                    // called. See comment in HandleThrottleComplete
                    SetJobState(JobState.Stopped);
                }
                else if (state == PipelineState.Completed)
                {
                    SetJobState(JobState.Completed);
                }
                else
                {
                    SetJobState(JobState.Stopped);
                }
            }
        }

        /// <summary>
        /// Set the state of the current job from blocked to
        /// running and raise an event indicating to this
        /// parent job that this job is unblocked.
        /// </summary>
        internal void UnblockJob()
        {
            Dbg.Assert(JobStateInfo.State == JobState.Blocked,
                "Current state of job must be blocked before it can be unblocked");

            SetJobState(JobState.Running, null);

            Dbg.Assert(JobUnblocked != null, "Parent job must register for JobUnblocked event from all child jobs");
            JobUnblocked.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns the PowerShell for the specified instance id.
        /// </summary>
        /// <param name="instanceId">Instance id of powershell.</param>
        /// <returns>Powershell instance.</returns>
        internal virtual PowerShell GetPowerShell(Guid instanceId)
        {
            // this should be called only in the derived implementation
            throw PSTraceSource.NewInvalidOperationException();
        }

        /// <summary>
        /// Returns the PowerShell object associated with this remote child job.
        /// </summary>
        /// <returns>PowerShell object.</returns>
        internal PowerShell GetPowerShell()
        {
            PowerShell ps = null;
            if (_remotePipeline != null)
            {
                ps = _remotePipeline.PowerShell;
            }

            return ps;
        }

        /// <summary>
        /// Monitor runspace availability and if it goes to RemoteDebug then set
        /// job state to Debug.  Set back to Running when availability goes back to
        /// Busy (indicating the script/command is running again).
        /// </summary>
        /// <param name="sender">Runspace.</param>
        /// <param name="e">RunspaceAvailabilityEventArgs.</param>
        private void HandleRunspaceAvailabilityChanged(object sender, RunspaceAvailabilityEventArgs e)
        {
            RunspaceAvailability prevAvailability = _prevRunspaceAvailability;
            _prevRunspaceAvailability = e.RunspaceAvailability;

            if (e.RunspaceAvailability == RunspaceAvailability.RemoteDebug)
            {
                SetJobState(JobState.AtBreakpoint);
            }
            else if ((prevAvailability == RunspaceAvailability.RemoteDebug) &&
                     (e.RunspaceAvailability == RunspaceAvailability.Busy))
            {
                SetJobState(JobState.Running);
            }
        }

        /// <summary>
        /// Event raised by this job to indicate to its parent that
        /// its now unblocked by the user.
        /// </summary>
        internal event EventHandler JobUnblocked;

        #endregion Private Methods

        #region Private Members

        // helper associated with this job object
        private readonly RemotePipeline _remotePipeline = null;

        // object used for synchronization
        protected object SyncObject = new object();

        private ThrottleManager _throttleManager;
        private bool _stopIsCalled = false;

        private volatile Debugger _jobDebugger;

        private bool _isAsync = true;

        private RunspaceAvailability _prevRunspaceAvailability = RunspaceAvailability.None;

        #endregion Private Members
    }

    /// <summary>
    /// This is a debugger wrapper class used to allow debugging of
    /// remoting jobs that implement the IJobDebugger interface.
    /// </summary>
    internal sealed class RemotingJobDebugger : Debugger
    {
        #region Members

        private readonly Debugger _wrappedDebugger;
        private readonly Runspace _runspace;
        private readonly string _jobName;

        #endregion

        #region Constructor

        private RemotingJobDebugger() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="debugger">Debugger to wrap.</param>
        /// <param name="runspace">Remote runspace.</param>
        /// <param name="jobName">Name of associated job.</param>
        public RemotingJobDebugger(
            Debugger debugger,
            Runspace runspace,
            string jobName)
        {
            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            _wrappedDebugger = debugger;
            _runspace = runspace;
            _jobName = jobName ?? string.Empty;

            // Create handlers for wrapped debugger events.
            _wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            _wrappedDebugger.DebuggerStop += HandleDebuggerStop;
        }

        #endregion

        #region Debugger overrides

        /// <summary>
        /// Evaluates provided command either as a debugger specific command
        /// or a PowerShell command.
        /// </summary>
        /// <param name="command">PowerShell command.</param>
        /// <param name="output">Output.</param>
        /// <returns>DebuggerCommandResults.</returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            // Special handling for the prompt command.
            if (command.Commands[0].CommandText.Trim().Equals("prompt", StringComparison.OrdinalIgnoreCase))
            {
                return HandlePromptCommand(output);
            }

            return _wrappedDebugger.ProcessCommand(command, output);
        }

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">Breakpoints to set.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.SetBreakpoints(breakpoints, runspaceId);

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A breakpoint with the specified id.</returns>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            _wrappedDebugger.GetBreakpoint(id, runspaceId);

        /// <summary>
        /// Returns breakpoints on a runspace.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A list of breakpoints in a runspace.</returns>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            _wrappedDebugger.GetBreakpoints(runspaceId);

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetCommandBreakpoint(command, action, path, runspaceId);

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId) =>
            _wrappedDebugger.SetLineBreakpoint(path, line, column, action, runspaceId);

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId);

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.RemoveBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.EnableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.DisableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Sets the debugger resume action.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            _wrappedDebugger.SetDebuggerAction(resumeAction);
        }

        /// <summary>
        /// Stops a running command.
        /// </summary>
        public override void StopProcessCommand()
        {
            _wrappedDebugger.StopProcessCommand();
        }

        /// <summary>
        /// Returns current debugger stop event arguments if debugger is in
        /// debug stop state.  Otherwise returns null.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.GetDebuggerStopArgs();
        }

        /// <summary>
        /// Sets the parent debugger, breakpoints, and other debugging context information.
        /// </summary>
        /// <param name="parent">Parent debugger.</param>
        /// <param name="breakPoints">List of breakpoints.</param>
        /// <param name="startAction">Debugger mode.</param>
        /// <param name="host">PowerShell host.</param>
        /// <param name="path">Current path.</param>
        public override void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? startAction,
            PSHost host,
            PathInfo path)
        {
            // For now always enable step mode debugging.
            SetDebuggerStepMode(true);
        }

        /// <summary>
        /// Sets the debugger mode.
        /// </summary>
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.SetDebugMode(mode);

            base.SetDebugMode(mode);
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            return _wrappedDebugger.GetCallStack();
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            _wrappedDebugger.SetDebuggerStepMode(enabled);
        }

        /// <summary>
        /// CheckStateAndRaiseStopEvent.
        /// </summary>
        internal void CheckStateAndRaiseStopEvent()
        {
            RemoteDebugger remoteDebugger = _wrappedDebugger as RemoteDebugger;
            remoteDebugger?.CheckStateAndRaiseStopEvent();
        }

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get { return _wrappedDebugger.InBreakpoint; }
        }

        #endregion

        #region Private methods

        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            Pipeline remoteRunningCmd = null;
            try
            {
                // For remote debugging drain/block output channel.
                remoteRunningCmd = DrainAndBlockRemoteOutput();

                this.RaiseDebuggerStopEvent(e);
            }
            finally
            {
                RestoreRemoteOutput(remoteRunningCmd);
            }
        }

        private Pipeline DrainAndBlockRemoteOutput()
        {
            // We only do this for remote runspaces.
            if (_runspace is not RemoteRunspace) { return null; }

            Pipeline runningCmd = _runspace.GetCurrentlyRunningPipeline();
            if (runningCmd != null)
            {
                runningCmd.DrainIncomingData();
                runningCmd.SuspendIncomingData();

                return runningCmd;
            }

            return null;
        }

        private static void RestoreRemoteOutput(Pipeline runningCmd) => runningCmd?.ResumeIncomingData();

        private void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            this.RaiseBreakpointUpdatedEvent(e);
        }

        private DebuggerCommandResults HandlePromptCommand(PSDataCollection<PSObject> output)
        {
            // Nested debugged runspace prompt should look like:
            // [DBG]: [JobName]: PS C:\>>
            string promptScript = "'[DBG]: '" + " + " + "'[" + CodeGeneration.EscapeSingleQuotedStringContent(_jobName) + "]: '" + " + " + @"""PS $($executionContext.SessionState.Path.CurrentLocation)>> """;
            PSCommand promptCommand = new PSCommand();
            promptCommand.AddScript(promptScript);
            _wrappedDebugger.ProcessCommand(promptCommand, output);

            return new DebuggerCommandResults(null, true);
        }

        #endregion
    }

    /// <summary>
    /// This job is used for running as a job the results from multiple
    /// pipelines. This is used in synchronous Invoke-Expression execution.
    /// </summary>
    /// <remarks>
    /// TODO: I am not sure whether to change this internal to just InvokeExpressionSyncJob.
    /// </remarks>
    /// <remarks>
    /// Not removing the prefix "PS" as this signifies powershell specific remoting job
    /// </remarks>
    internal class PSInvokeExpressionSyncJob : PSRemotingChildJob
    {
        #region Private Members

        private readonly List<ExecutionCmdletHelper> _helpers = new List<ExecutionCmdletHelper>();
        private readonly ThrottleManager _throttleManager;
        private readonly Dictionary<Guid, PowerShell> _powershells = new Dictionary<Guid, PowerShell>();

        private int _pipelineFinishedCount;
        private int _pipelineDisconnectedCount;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Construct an invoke-expression sync job.
        /// </summary>
        /// <param name="operations">List of operations to use.</param>
        /// <param name="throttleManager">throttle manager to use for
        /// this job</param>
        internal PSInvokeExpressionSyncJob(List<IThrottleOperation> operations, ThrottleManager throttleManager)
        {
            UsesResultsCollection = true;
            Results.AddRef();

            _throttleManager = throttleManager;
            RegisterThrottleComplete(_throttleManager);

            foreach (IThrottleOperation operation in operations)
            {
                ExecutionCmdletHelper helper = operation as ExecutionCmdletHelper;

                RemoteRunspace remoteRS = helper.Pipeline.Runspace as RemoteRunspace;
                if (remoteRS != null)
                {
                    remoteRS.StateChanged += HandleRunspaceStateChanged;

                    if (remoteRS.RunspaceStateInfo.State == RunspaceState.BeforeOpen)
                    {
                        remoteRS.URIRedirectionReported += HandleURIDirectionReported;
                    }
                }

                _helpers.Add(helper);
                AggregateResultsFromHelper(helper);

                Dbg.Assert(helper.Pipeline is RemotePipeline, "Only remote pipeline can be used in InvokeExpressionSyncJob");
                RemotePipeline pipeline = helper.Pipeline as RemotePipeline;
                _powershells.Add(pipeline.PowerShell.InstanceId, pipeline.PowerShell);
            }
        }

        #endregion Constructors

        #region Protected Methods

        private bool _cleanupDone = false;

        /// <summary>
        /// Clean up once job is finished.
        /// </summary>
        protected override void DoCleanupOnFinished()
        {
            bool doCleanup = false;
            if (!_cleanupDone)
            {
                lock (SyncObject)
                {
                    if (!_cleanupDone)
                    {
                        _cleanupDone = true;
                        doCleanup = true;
                    }
                }
            }

            if (!doCleanup) return;

            foreach (ExecutionCmdletHelper helper in _helpers)
            {
                // cleanup remote runspace related handlers
                RemoteRunspace remoteRS = helper.PipelineRunspace as RemoteRunspace;
                if (remoteRS != null)
                {
                    remoteRS.StateChanged -= HandleRunspaceStateChanged;
                    remoteRS.URIRedirectionReported -= HandleURIDirectionReported;
                }

                StopAggregateResultsFromHelper(helper);
            }

            UnregisterThrottleComplete(_throttleManager);
            // throttleManager = null;

            Results.DecrementRef();
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">True if called by Dispose().</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// Handles operation complete from the operations. Adds an error record
        /// to results whenever an error is encountered.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="stateEventArgs">Arguments describing this event, unused.</param>
        protected override void HandleOperationComplete(object sender, OperationStateEventArgs stateEventArgs)
        {
            ExecutionCmdletHelper helper = sender as ExecutionCmdletHelper;
            Dbg.Assert(helper != null, "Sender of OperationComplete has to be ExecutionCmdletHelper");

            Exception failureException;
            // Process the reason in case of failure.
            ErrorRecord failureErrorRecord;

            ProcessJobFailure(helper, out failureException, out failureErrorRecord);

            if (failureErrorRecord != null)
            {
                this.WriteError(failureErrorRecord);
            }
        }

        /// <summary>
        /// Handle changes in pipeline states.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void HandlePipelineStateChanged(object sender, PipelineStateEventArgs e)
        {
            PipelineState state = e.PipelineStateInfo.State;
            switch (state)
            {
                case PipelineState.Running:
                    SetJobState(JobState.Running);
                    break;

                case PipelineState.Completed:
                case PipelineState.Failed:
                case PipelineState.Stopped:
                case PipelineState.Disconnected:
                    CheckForAndSetDisconnectedState(state);
                    break;
            }
        }

        /// <summary>
        /// Checks for a condition where all pipelines are either finished
        /// or disconnected and at least one pipeline is disconnected.
        /// In this case the Job state is set to Disconnected.
        /// </summary>
        private void CheckForAndSetDisconnectedState(PipelineState pipelineState)
        {
            bool setJobStateToDisconnected;
            lock (SyncObject)
            {
                if (IsTerminalState())
                {
                    return;
                }

                switch (pipelineState)
                {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        _pipelineFinishedCount += 1;
                        break;

                    case PipelineState.Disconnected:
                        _pipelineDisconnectedCount += 1;
                        break;
                }

                setJobStateToDisconnected = ((_pipelineFinishedCount + _pipelineDisconnectedCount) == _helpers.Count &&
                                              _pipelineDisconnectedCount > 0);
            }

            if (setJobStateToDisconnected)
            {
                // Job cannot finish with pipelines in disconnected state.
                // Set Job state to Disconnected.
                SetJobState(JobState.Disconnected);
            }
        }

        /// <summary>
        /// Used to stop all operations.
        /// </summary>
        public override void StopJob()
        {
            _throttleManager.StopAllOperations();
        }

        private bool _doFinishCalled = false;

        /// <summary>
        /// This method marks the completion state for Job. Also if job failed, it processes the
        /// reason of failure.
        /// </summary>
        protected override void DoFinish()
        {
            if (_doFinishCalled)
                return;

            lock (SyncObject)
            {
                if (_doFinishCalled)
                    return;

                _doFinishCalled = true;
            }

            foreach (ExecutionCmdletHelper helper in _helpers)
            {
                DeterminedAndSetJobState(helper);
            }

            if (_helpers.Count == 0 && this.JobStateInfo.State == JobState.NotStarted)
            {
                SetJobState(JobState.Completed);
            }

            DoCleanupOnFinished();
        }

        /// <summary>
        /// Returns the PowerShell instance for the specified id.
        /// </summary>
        /// <param name="instanceId">Instance id of PowerShell.</param>
        /// <returns>PowerShell instance.</returns>
        internal override PowerShell GetPowerShell(Guid instanceId)
        {
            PowerShell powershell = null;

            _powershells.TryGetValue(instanceId, out powershell);

            return powershell;
        }

        #endregion Protected Methods

        #region Event Handlers

        /// <summary>
        /// Used to unregister URI Redirection handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleRunspaceStateChanged(object sender, RunspaceStateEventArgs e)
        {
            RemoteRunspace remoteRS = sender as RemoteRunspace;
            // remote runspace must be connected (or connection failed)
            // we dont need URI redirection any more..so clear it
            if (remoteRS != null)
            {
                if (e.RunspaceStateInfo.State != RunspaceState.Opening)
                {
                    remoteRS.URIRedirectionReported -= HandleURIDirectionReported;

                    if (e.RunspaceStateInfo.State != RunspaceState.Opened)
                    {
                        remoteRS.StateChanged -= HandleRunspaceStateChanged;
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Submits the operations created in the constructor for invocation.
        /// </summary>
        internal void StartOperations(List<IThrottleOperation> operations)
        {
            // submit operations to the throttle manager
            _throttleManager.SubmitOperations(operations);
            _throttleManager.EndSubmitOperations();
        }

        /// <summary>
        /// Determines if the job is in a terminal state.
        /// </summary>
        /// <returns>True, if job in terminal state
        /// false otherwise.</returns>
        internal bool IsTerminalState()
        {
            return (IsFinishedState(this.JobStateInfo.State) ||
                    this.JobStateInfo.State == JobState.Disconnected);
        }

        /// <summary>
        /// Returns a collection of all powershells for this job.
        /// </summary>
        /// <returns>Collection of PowerShell objects.</returns>
        internal Collection<PowerShell> GetPowerShells()
        {
            Collection<PowerShell> powershellsToReturn = new Collection<PowerShell>();
            foreach (PowerShell ps in _powershells.Values)
            {
                powershellsToReturn.Add(ps);
            }

            return powershellsToReturn;
        }

        /// <summary>
        /// Returns a disconnected remoting job object that contains all
        /// remote pipelines/runspaces that are in the Disconnected state.
        /// </summary>
        /// <returns></returns>
        internal PSRemotingJob CreateDisconnectedRemotingJob()
        {
            List<IThrottleOperation> disconnectedJobHelpers = new List<IThrottleOperation>();
            foreach (var helper in _helpers)
            {
                if (helper.Pipeline.PipelineStateInfo.State == PipelineState.Disconnected)
                {
                    // Remove data callbacks from the old helper.
                    RemoveAggreateCallbacksFromHelper(helper);

                    // Create new helper used to create the new Disconnected PSRemoting job.
                    disconnectedJobHelpers.Add(new DisconnectedJobOperation(helper.Pipeline));
                }
            }

            if (disconnectedJobHelpers.Count == 0)
            {
                return null;
            }

            return new PSRemotingJob(disconnectedJobHelpers, 0, Name, true);
        }

        #endregion Internal Methods
    }

    #region OutputProcessingState class

    internal class OutputProcessingStateEventArgs : EventArgs
    {
        internal bool ProcessingOutput { get; }

        internal OutputProcessingStateEventArgs(bool processingOutput)
        {
            ProcessingOutput = processingOutput;
        }
    }

#nullable enable
    internal interface IOutputProcessingState
    {
        event EventHandler<OutputProcessingStateEventArgs>? OutputProcessingStateChanged;
    }

    #endregion
}
