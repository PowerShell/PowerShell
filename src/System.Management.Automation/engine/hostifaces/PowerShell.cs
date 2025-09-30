// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Telemetry;

using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    #region Exceptions

    /// <summary>
    /// Defines exception which is thrown when state of the PowerShell is different
    /// from the expected state.
    /// </summary>
    public class InvalidPowerShellStateException : SystemException
    {
        /// <summary>
        /// Creates a new instance of InvalidPowershellStateException class.
        /// </summary>
        public InvalidPowerShellStateException()
        : base
        (StringUtil.Format(PowerShellStrings.InvalidPowerShellStateGeneral))
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidPowershellStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public InvalidPowerShellStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of InvalidPowershellStateException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidPowerShellStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPowerShellStateException and defines value of
        /// CurrentState.
        /// </summary>
        /// <param name="currentState">Current state of powershell.</param>
        internal InvalidPowerShellStateException(PSInvocationState currentState)
        : base
        (StringUtil.Format(PowerShellStrings.InvalidPowerShellStateGeneral))
        {
            _currState = currentState;
        }

        #region ISerializable Members

        // No need to implement GetObjectData
        // if all fields are static or [NonSerialized]

        /// <summary>
        /// Initializes a new instance of the InvalidPowerShellStateException
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
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected
        InvalidPowerShellStateException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion

        /// <summary>
        /// Gets CurrentState of the powershell.
        /// </summary>
        public PSInvocationState CurrentState
        {
            get
            {
                return _currState;
            }
        }

        /// <summary>
        /// State of powershell when exception was thrown.
        /// </summary>
        [NonSerialized]
        private readonly PSInvocationState _currState = 0;
    }

    #endregion

    #region PSInvocationState, PSInvocationStateInfo, PSInvocationStateChangedEventArgs

    /// <summary>
    /// Enumerated type defining the state of the PowerShell.
    /// </summary>
    public enum PSInvocationState
    {
        /// <summary>
        /// PowerShell has not been started.
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// PowerShell is executing.
        /// </summary>
        Running = 1,
        /// <summary>
        /// PowerShell is stoping execution.
        /// </summary>
        Stopping = 2,
        /// <summary>
        /// PowerShell is completed due to a stop request.
        /// </summary>
        Stopped = 3,
        /// <summary>
        /// PowerShell has completed executing a command.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// PowerShell completed abnormally due to an error.
        /// </summary>
        Failed = 5,
        /// <summary>
        /// PowerShell is in disconnected state.
        /// </summary>
        Disconnected = 6
    }

    /// <summary>
    /// Enumerated type defining runspace modes for nested pipeline.
    /// </summary>
    public enum RunspaceMode
    {
        /// <summary>
        /// Use current runspace from the current thread of execution.
        /// </summary>
        CurrentRunspace = 0,

        /// <summary>
        /// Create new runspace.
        /// </summary>
        NewRunspace = 1
    }

    /// <summary>
    /// Type which has information about InvocationState and Exception
    /// associated with InvocationState.
    /// </summary>
    public sealed class PSInvocationStateInfo
    {
        #region Constructors

        /// <summary>
        /// Constructor for state changes with an optional error.
        /// </summary>
        /// <param name="state">The new state.</param>
        /// <param name="reason">A non-null exception if the state change was
        /// caused by an error,otherwise; null.
        /// </param>
        internal PSInvocationStateInfo(PSInvocationState state, Exception reason)
        {
            _executionState = state;
            _exceptionReason = reason;
        }

        /// <summary>
        /// Construct from PipelineStateInfo.
        /// </summary>
        /// <param name="pipelineStateInfo"></param>
        internal PSInvocationStateInfo(PipelineStateInfo pipelineStateInfo)
        {
            _executionState = (PSInvocationState)((int)pipelineStateInfo.State);
            _exceptionReason = pipelineStateInfo.Reason;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The state of the PowerShell instance.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public PSInvocationState State
        {
            get
            {
                return _executionState;
            }
        }

        /// <summary>
        /// The reason for the state change, if caused by an error.
        /// </summary>
        /// <remarks>
        /// The value of this property is non-null if the state
        /// changed due to an error. Otherwise, the value of this
        /// property is null.
        /// </remarks>
        public Exception Reason
        {
            get
            {
                return _exceptionReason;
            }
        }

        #endregion

        /// <summary>
        /// Clone the current instance.
        /// </summary>
        /// <returns>
        /// A copy of the current instance.
        /// </returns>
        internal PSInvocationStateInfo Clone()
        {
            return new PSInvocationStateInfo(
                _executionState,
                _exceptionReason
                );
        }

        #region Private data

        /// <summary>
        /// The current execution state.
        /// </summary>
        private readonly PSInvocationState _executionState;

        /// <summary>
        /// Non-null exception if the execution state change was due to an error.
        /// </summary>
        private readonly Exception _exceptionReason;

        #endregion
    }

    /// <summary>
    /// Event arguments passed to PowerShell state change handlers
    /// <see cref="PowerShell.InvocationStateChanged"/> event.
    /// </summary>
    public sealed class PSInvocationStateChangedEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructs PSInvocationStateChangedEventArgs from PSInvocationStateInfo.
        /// </summary>
        /// <param name="psStateInfo">
        /// state to raise the event with.
        /// </param>
        internal PSInvocationStateChangedEventArgs(PSInvocationStateInfo psStateInfo)
        {
            Dbg.Assert(psStateInfo != null, "caller should validate the parameter");
            InvocationStateInfo = psStateInfo;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Information about current state of a PowerShell Instance.
        /// </summary>
        public PSInvocationStateInfo InvocationStateInfo { get; }

        #endregion
    }

    #endregion

    /// <summary>
    /// Settings to control command invocation.
    /// </summary>
    public sealed class PSInvocationSettings
    {
        #region Private Fields

        private PSHost _host;

        // the following are used to flow the identity to pipeline execution thread

        // Invokes a remote command and immediately disconnects, if transport layer
        // supports this operation.

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public PSInvocationSettings()
        {
            this.ApartmentState = ApartmentState.Unknown;
            _host = null;
            RemoteStreamOptions = 0;
            AddToHistory = false;
            ErrorActionPreference = null;
        }

        #endregion

        /// <summary>
        /// ApartmentState of the thread in which the command
        /// is executed.
        /// </summary>
        public ApartmentState ApartmentState { get; set; }

        /// <summary>
        /// Host to use with the Runspace when the command is
        /// executed.
        /// </summary>
        public PSHost Host
        {
            get
            {
                return _host;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Host");
                }

                _host = value;
            }
        }

        /// <summary>
        /// Options for the Error, Warning, Verbose and Debug streams during remote calls.
        /// </summary>
        public RemoteStreamOptions RemoteStreamOptions { get; set; }

        /// <summary>
        /// Boolean which tells if the command is added to the history of the
        /// Runspace the command is executing in. By default this is false.
        /// </summary>
        public bool AddToHistory { get; set; }

        /// <summary>
        /// Determines how errors should be handled during batch command execution.
        /// </summary>
        public ActionPreference? ErrorActionPreference { get; set; }

        /// <summary>
        /// Used by Powershell remoting infrastructure to flow identity from calling thread to
        /// Pipeline Execution Thread.
        /// </summary>
        /// <remarks>
        /// Scenario: In the IIS hosting model, the calling thread is impersonated with a different
        /// identity than the process identity. However Pipeline Execution Thread always inherits
        /// process's identity and this will create problems related to security. In the IIS hosting
        /// model, we should honor calling threads identity.
        /// </remarks>
        public bool FlowImpersonationPolicy { get; set; }

        internal System.Security.Principal.WindowsIdentity WindowsIdentityToImpersonate { get; set; }

        /// <summary>
        /// When true, allows an unhandled flow control exceptions to
        /// propagate to a caller invoking the PowerShell object.
        /// </summary>
        public bool ExposeFlowControlExceptions
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes a remote command and immediately disconnects, if the transport
        /// layer supports this operation.
        /// </summary>
        internal bool InvokeAndDisconnect { get; set; }
    }

    /// <summary>
    /// Batch execution context.
    /// </summary>
    internal class BatchInvocationContext
    {
        private readonly AutoResetEvent _completionEvent;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        internal BatchInvocationContext(PSCommand command, PSDataCollection<PSObject> output)
        {
            Command = command;
            Output = output;
            _completionEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Invocation output.
        /// </summary>
        internal PSDataCollection<PSObject> Output { get; }

        /// <summary>
        /// Command to invoke.
        /// </summary>
        internal PSCommand Command { get; }

        /// <summary>
        /// Waits for the completion event.
        /// </summary>
        internal void Wait()
        {
            _completionEvent.WaitOne();
        }

        /// <summary>
        /// Signals the completion event.
        /// </summary>
        internal void Signal()
        {
            _completionEvent.Set();
        }
    }

    /// <summary>
    /// These flags control whether InvocationInfo is added to items in the Error, Warning, Verbose and Debug
    /// streams during remote calls.
    /// </summary>
    [Flags]
    public enum RemoteStreamOptions
    {
        /// <summary>
        /// If this flag is set, ErrorRecord will include an instance of InvocationInfo on remote calls.
        /// </summary>
        AddInvocationInfoToErrorRecord = 0x01,

        /// <summary>
        /// If this flag is set, WarningRecord will include an instance of InvocationInfo on remote calls.
        /// </summary>
        AddInvocationInfoToWarningRecord = 0x02,

        /// <summary>
        /// If this flag is set, DebugRecord will include an instance of InvocationInfo on remote calls.
        /// </summary>
        AddInvocationInfoToDebugRecord = 0x04,

        /// <summary>
        /// If this flag is set, VerboseRecord will include an instance of InvocationInfo on remote calls.
        /// </summary>
        AddInvocationInfoToVerboseRecord = 0x08,

        /// <summary>
        /// If this flag is set, ErrorRecord, WarningRecord, DebugRecord, and VerboseRecord will include an instance of InvocationInfo on remote calls.
        /// </summary>
        AddInvocationInfo = AddInvocationInfoToErrorRecord
                          | AddInvocationInfoToWarningRecord
                          | AddInvocationInfoToDebugRecord
                          | AddInvocationInfoToVerboseRecord
    }

    #region PowerShell AsyncResult

    /// <summary>
    /// Internal Async result type used by BeginInvoke() and BeginStop() overloads.
    /// </summary>
    internal sealed class PowerShellAsyncResult : AsyncResult
    {
        #region Private Data / Properties

        // used to track if this AsyncResult is created by a BeginInvoke operation or
        // a BeginStop operation.

        /// <summary>
        /// True if AsyncResult monitors Async BeginInvoke().
        /// false otherwise.
        /// </summary>
        internal bool IsAssociatedWithAsyncInvoke { get; }

        /// <summary>
        /// The output buffer for the asynchronous invoke.
        /// </summary>
        internal PSDataCollection<PSObject> Output { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ownerId">
        /// Instance Id of the Powershell object creating this instance
        /// </param>
        /// <param name="callback">
        /// Callback to call when the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the "callback" with.
        /// </param>
        /// <param name="output">
        /// The output buffer to return from EndInvoke.
        /// </param>
        /// <param name="isCalledFromBeginInvoke">
        /// true if AsyncResult monitors BeginInvoke.
        /// false otherwise
        /// </param>
        internal PowerShellAsyncResult(Guid ownerId, AsyncCallback callback, object state, PSDataCollection<PSObject> output,
            bool isCalledFromBeginInvoke)
            : base(ownerId, callback, state)
        {
            IsAssociatedWithAsyncInvoke = isCalledFromBeginInvoke;
            Output = output;
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Represents a PowerShell command or script to execute against a
    /// Runspace(Pool) if provided, otherwise execute using a default
    /// Runspace. Provides access to different result buffers
    /// like output, error, debug, verbose, progress, warning, and information.
    ///
    /// Provides a simple interface to execute a powershell command:
    /// <code>
    ///    Powershell.Create().AddScript("get-process").Invoke();
    /// </code>
    /// The above statement creates a local runspace using default
    /// configuration, executes the command and then closes the runspace.
    ///
    /// Using RunspacePool property, the caller can provide the runspace
    /// where the command / script is executed.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "PowerShell is a valid type in SMAR namespace.")]
    public sealed class PowerShell : IDisposable
    {
        #region Private Fields

        private PSCommand _psCommand;
        // worker object which does the invoke
        private Worker _worker;
        private PowerShellAsyncResult _invokeAsyncResult;
        private PowerShellAsyncResult _stopAsyncResult;
        private PowerShellAsyncResult _batchAsyncResult;
        private PSInvocationSettings _batchInvocationSettings;
        private PSCommand _backupPSCommand;
        private object _rsConnection;

        private PSDataCollection<ErrorRecord> _errorBuffer;

        private bool _isDisposed;
        private readonly object _syncObject = new object();

        // client remote powershell if the powershell
        // is executed with a remote runspace pool

        private ConnectCommandInfo _connectCmdInfo;
        private bool _commandInvokedSynchronously = false;
        private bool _isBatching = false;
        private bool _stopBatchExecution = false;

        // Delegates for asynchronous invocation/termination of PowerShell commands
        private readonly Func<IAsyncResult, PSDataCollection<PSObject>> _endInvokeMethod;
        private readonly Action<IAsyncResult> _endStopMethod;

        #endregion

        #region Internal Constructors

        /// <summary>
        /// Constructs PowerShell.
        /// </summary>
        /// <param name="command">
        /// A PSCommand.
        /// </param>
        /// <param name="extraCommands">
        /// A list of extra commands to run
        /// </param>
        /// <param name="rsConnection">
        /// A Runspace or RunspacePool to refer while invoking the command.
        /// This can be null in which case a new runspace is created
        /// whenever Invoke* method is called.
        /// </param>
        private PowerShell(PSCommand command, Collection<PSCommand> extraCommands, object rsConnection)
        {
            Dbg.Assert(command != null, "command must not be null");
            ExtraCommands = extraCommands ?? new Collection<PSCommand>();
            RunningExtraCommands = false;
            _psCommand = command;
            _psCommand.Owner = this;
            RemoteRunspace remoteRunspace = rsConnection as RemoteRunspace;
            _rsConnection = remoteRunspace != null ? remoteRunspace.RunspacePool : rsConnection;
            InstanceId = Guid.NewGuid();
            InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.NotStarted, null);
            OutputBuffer = null;
            OutputBufferOwner = true;
            _errorBuffer = new PSDataCollection<ErrorRecord>();
            ErrorBufferOwner = true;
            InformationalBuffers = new PSInformationalBuffers(InstanceId);
            Streams = new PSDataStreams(this);
            _endInvokeMethod = EndInvoke;
            _endStopMethod = EndStop;
            ApplicationInsightsTelemetry.SendTelemetryMetric(TelemetryType.PowerShellCreate, "create");
        }

        /// <summary>
        /// Constructs a PowerShell instance in the disconnected start state with
        /// the provided remote command connect information and runspace(pool) objects.
        /// </summary>
        /// <param name="connectCmdInfo">Remote command connect information.</param>
        /// <param name="rsConnection">Remote Runspace or RunspacePool object.</param>
        internal PowerShell(ConnectCommandInfo connectCmdInfo, object rsConnection)
            : this(new PSCommand(), null, rsConnection)
        {
            ExtraCommands = new Collection<PSCommand>();
            RunningExtraCommands = false;
            AddCommand(connectCmdInfo.Command);
            _connectCmdInfo = connectCmdInfo;

            // The command ID is passed to the PSRP layer through the PowerShell instanceID.
            InstanceId = _connectCmdInfo.CommandId;

            InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Disconnected, null);

            if (rsConnection is RemoteRunspace)
            {
                _runspace = rsConnection as Runspace;
                _runspacePool = ((RemoteRunspace)rsConnection).RunspacePool;
            }
            else if (rsConnection is RunspacePool)
            {
                _runspacePool = (RunspacePool)rsConnection;
            }

            Dbg.Assert(_runspacePool != null, "Invalid rsConnection parameter>");
            RemotePowerShell = new ClientRemotePowerShell(this, _runspacePool.RemoteRunspacePoolInternal);
        }

        /// <summary>
        /// </summary>
        /// <param name="inputstream"></param>
        /// <param name="outputstream"></param>
        /// <param name="errorstream"></param>
        /// <param name="runspacePool"></param>
        internal PowerShell(ObjectStreamBase inputstream,
            ObjectStreamBase outputstream, ObjectStreamBase errorstream, RunspacePool runspacePool)
        {
            ExtraCommands = new Collection<PSCommand>();
            RunningExtraCommands = false;
            _rsConnection = runspacePool;
            InstanceId = Guid.NewGuid();
            InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.NotStarted, null);
            InformationalBuffers = new PSInformationalBuffers(InstanceId);
            Streams = new PSDataStreams(this);

            PSDataCollectionStream<PSObject> outputdatastream = (PSDataCollectionStream<PSObject>)outputstream;
            OutputBuffer = outputdatastream.ObjectStore;

            PSDataCollectionStream<ErrorRecord> errordatastream = (PSDataCollectionStream<ErrorRecord>)errorstream;
            _errorBuffer = errordatastream.ObjectStore;

            if (runspacePool != null && runspacePool.RemoteRunspacePoolInternal != null)
            {
                RemotePowerShell = new ClientRemotePowerShell(this, runspacePool.RemoteRunspacePoolInternal);
            }

            _endInvokeMethod = EndInvoke;
            _endStopMethod = EndStop;
        }

        /// <summary>
        /// Creates a PowerShell object in the disconnected start state and with a ConnectCommandInfo object
        /// parameter that specifies what remote command to associate with this PowerShell when it is connected.
        /// </summary>
        /// <param name="connectCmdInfo"></param>
        /// <param name="inputstream"></param>
        /// <param name="outputstream"></param>
        /// <param name="errorstream"></param>
        /// <param name="runspacePool"></param>
        internal PowerShell(ConnectCommandInfo connectCmdInfo, ObjectStreamBase inputstream, ObjectStreamBase outputstream,
            ObjectStreamBase errorstream, RunspacePool runspacePool)
            : this(inputstream, outputstream, errorstream, runspacePool)
        {
            ExtraCommands = new Collection<PSCommand>();
            RunningExtraCommands = false;
            _psCommand = new PSCommand();
            _psCommand.Owner = this;
            _runspacePool = runspacePool;

            AddCommand(connectCmdInfo.Command);
            _connectCmdInfo = connectCmdInfo;

            // The command ID is passed to the PSRP layer through the PowerShell instanceID.
            InstanceId = _connectCmdInfo.CommandId;

            InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Disconnected, null);

            RemotePowerShell = new ClientRemotePowerShell(this, runspacePool.RemoteRunspacePoolInternal);
        }

        /// <summary>
        /// Sets the command collection in this powershell.
        /// </summary>
        /// <remarks>This method will be called by RemotePipeline
        /// before it begins execution. This method is used to set
        /// the command collection of the remote pipeline as the
        /// command collection of the underlying powershell</remarks>
        internal void InitForRemotePipeline(CommandCollection command, ObjectStreamBase inputstream,
            ObjectStreamBase outputstream, ObjectStreamBase errorstream, PSInvocationSettings settings, bool redirectShellErrorOutputPipe)
        {
            Dbg.Assert(command != null, "A command collection need to be specified");

            _psCommand = new PSCommand(command[0]);
            _psCommand.Owner = this;

            for (int i = 1; i < command.Count; i++)
            {
                AddCommand(command[i]);
            }

            RedirectShellErrorOutputPipe = redirectShellErrorOutputPipe;

            // create the client remote powershell for remoting
            // communications
            RemotePowerShell ??= new ClientRemotePowerShell(this, ((RunspacePool)_rsConnection).RemoteRunspacePoolInternal);

            // If we get here, we don't call 'Invoke' or any of it's friends on 'this', instead we serialize 'this' in PowerShell.ToPSObjectForRemoting.
            // Without the following two steps, we'll be missing the 'ExtraCommands' on the serialized instance of 'this'.
            // This is the last possible chance to call set up for batching as we will indirectly call ToPSObjectForRemoting
            // in the call to ClientRemotePowerShell.Initialize (which happens just below.)
            DetermineIsBatching();

            if (_isBatching)
            {
                SetupAsyncBatchExecution();
            }

            RemotePowerShell.Initialize(inputstream, outputstream,
                errorstream, InformationalBuffers, settings);
        }

        /// <summary>
        /// Initialize PowerShell object for connection to remote command.
        /// </summary>
        /// <param name="inputstream">Input stream.</param>
        /// <param name="outputstream">Output stream.</param>
        /// <param name="errorstream">Error stream.</param>
        /// <param name="settings">Settings information.</param>
        /// <param name="redirectShellErrorOutputPipe">Redirect error output.</param>
        internal void InitForRemotePipelineConnect(ObjectStreamBase inputstream, ObjectStreamBase outputstream,
            ObjectStreamBase errorstream, PSInvocationSettings settings, bool redirectShellErrorOutputPipe)
        {
            // The remotePowerShell and DSHandler cannot be initialized with a disconnected runspace.
            // Make sure the associated runspace is valid and connected.
            CheckRunspacePoolAndConnect();

            if (InvocationStateInfo.State != PSInvocationState.Disconnected)
            {
                throw new InvalidPowerShellStateException(InvocationStateInfo.State);
            }

            RedirectShellErrorOutputPipe = redirectShellErrorOutputPipe;

            RemotePowerShell ??= new ClientRemotePowerShell(this, ((RunspacePool)_rsConnection).RemoteRunspacePoolInternal);

            if (!RemotePowerShell.Initialized)
            {
                RemotePowerShell.Initialize(inputstream, outputstream, errorstream, InformationalBuffers, settings);
            }
        }

        #endregion

        #region Construction Factory

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance.
        /// </summary>
        /// <returns>
        /// An instance of PowerShell.
        /// </returns>
        public static PowerShell Create()
        {
            return new PowerShell(new PSCommand(), null, null);
        }

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance.
        /// </summary>
        /// <param name="runspace">Runspace mode.</param>
        /// <returns>An instance of PowerShell.</returns>
        public static PowerShell Create(RunspaceMode runspace)
        {
            PowerShell result = null;

            switch (runspace)
            {
                case RunspaceMode.CurrentRunspace:
                    if (Runspace.DefaultRunspace == null)
                    {
                        throw new InvalidOperationException(PowerShellStrings.NoDefaultRunspaceForPSCreate);
                    }

                    result = new PowerShell(new PSCommand(), null, Runspace.DefaultRunspace);
                    result.IsChild = true;
                    result.IsNested = true;
                    result.IsRunspaceOwner = false;
                    result._runspace = Runspace.DefaultRunspace;
                    break;
                case RunspaceMode.NewRunspace:
                    result = new PowerShell(new PSCommand(), null, null);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance.
        /// </summary>
        /// <param name="initialSessionState">InitialSessionState with which to create the runspace.</param>
        /// <returns>An instance of PowerShell.</returns>
        public static PowerShell Create(InitialSessionState initialSessionState)
        {
            PowerShell result = Create();

            result.Runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            result.Runspace.Open();

            return result;
        }

        /// <summary>
        /// Constructs an empty PowerShell instance and associates it with the provided
        /// Runspace; a script or command must be added before invoking this instance.
        /// </summary>
        /// <param name="runspace">Runspace in which to invoke commands.</param>
        /// <returns>An instance of PowerShell.</returns>
        /// <remarks>
        /// The required Runspace argument is accepted no matter what state it is in.
        /// Leaving Runspace state management to the caller allows them to open their
        /// runspace in whatever manner is most appropriate for their application
        /// (in another thread while this instance of the PowerShell class is being
        /// instantiated, for example).
        /// </remarks>
        public static PowerShell Create(Runspace runspace)
        {
            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            PowerShell result = Create();
            result.Runspace = runspace;

            return result;
        }

        /// <summary>
        /// Creates a nested powershell within the current instance.
        /// Nested PowerShell is used to do simple operations like checking state
        /// of a variable while another command is using the runspace.
        ///
        /// Nested PowerShell should be invoked from the same thread as the parent
        /// PowerShell invocation thread. So effectively the parent Powershell
        /// invocation thread is blocked until nested invoke() operation is
        /// complete.
        ///
        /// Implement PSHost.EnterNestedPrompt to perform invoke() operation on the
        /// nested powershell.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 1. State of powershell instance is not valid to create a nested powershell instance.
        /// Nested PowerShell should be created only for a running powershell instance.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ps", Justification = "ps represents PowerShell and is used at many places.")]
        public PowerShell CreateNestedPowerShell()
        {
            if ((_worker != null) && (_worker.CurrentlyRunningPipeline != null))
            {
                PowerShell result = new PowerShell(new PSCommand(),
                    null, _worker.CurrentlyRunningPipeline.Runspace);
                result.IsNested = true;
                return result;
            }

            throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.InvalidStateCreateNested);
        }

        /// <summary>
        /// Method needed when deserializing PowerShell object coming from a RemoteDataObject.
        /// </summary>
        /// <param name="isNested">Indicates if PowerShell object is nested.</param>
        /// <param name="psCommand">Commands that the PowerShell pipeline is built of.</param>
        /// <param name="extraCommands">Extra commands to run.</param>
        private static PowerShell Create(bool isNested, PSCommand psCommand, Collection<PSCommand> extraCommands)
        {
            PowerShell powerShell = new PowerShell(psCommand, extraCommands, null);
            powerShell.IsNested = isNested;
            return powerShell;
        }

        #endregion

        #region Command / Parameter Construction

        /// <summary>
        /// Add a cmdlet to construct a command pipeline.
        /// For example, to construct a command string "Get-Process | Sort-Object",
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddCommand("Get-Process")
        ///             .AddCommand("Sort-Object");
        ///     </code>
        /// </summary>
        /// <param name="cmdlet">
        /// A string representing cmdlet.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="cmdlet"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddCommand(string cmdlet)
        {
            lock (_syncObject)
            {
                AssertChangesAreAccepted();

                _psCommand.AddCommand(cmdlet);

                return this;
            }
        }

        /// <summary>
        /// Add a cmdlet to construct a command pipeline.
        /// For example, to construct a command string "Get-Process | Sort-Object",
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddCommand("Get-Process", true)
        ///             .AddCommand("Sort-Object", true);
        ///     </code>
        /// </summary>
        /// <param name="cmdlet">
        /// A string representing cmdlet.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="cmdlet"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddCommand(string cmdlet, bool useLocalScope)
        {
            lock (_syncObject)
            {
                AssertChangesAreAccepted();

                _psCommand.AddCommand(cmdlet, useLocalScope);

                return this;
            }
        }

        /// <summary>
        /// Add a piece of script to construct a command pipeline.
        /// For example, to construct a command string "Get-Process | ForEach-Object { $_.Name }"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddScript("Get-Process | ForEach-Object { $_.Name }");
        ///     </code>
        /// </summary>
        /// <param name="script">
        /// A string representing a script.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddScript(string script)
        {
            lock (_syncObject)
            {
                AssertChangesAreAccepted();

                _psCommand.AddScript(script);

                return this;
            }
        }

        /// <summary>
        /// Add a piece of script to construct a command pipeline.
        /// For example, to construct a command string "Get-Process | ForEach-Object { $_.Name }"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddScript("Get-Process | ForEach-Object { $_.Name }", true);
        ///     </code>
        /// </summary>
        /// <param name="script">
        /// A string representing a script.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddScript(string script, bool useLocalScope)
        {
            lock (_syncObject)
            {
                AssertChangesAreAccepted();

                _psCommand.AddScript(script, useLocalScope);

                return this;
            }
        }

        /// <summary>
        /// Add a <see cref="Command"/> element to the current command
        /// pipeline.
        /// </summary>
        /// <param name="command">
        /// Command to add.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        internal PowerShell AddCommand(Command command)
        {
            lock (_syncObject)
            {
                AssertChangesAreAccepted();

                _psCommand.AddCommand(command);

                return this;
            }
        }

        /// <summary>
        /// CommandInfo object for the command to add.
        /// </summary>
        /// <param name="commandInfo">The CommandInfo object for the command to add.</param>
        /// <returns>
        /// A PSCommand instance with the command added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddCommand(CommandInfo commandInfo)
        {
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandInfo));
            }

            Command cmd = new Command(commandInfo);
            _psCommand.AddCommand(cmd);
            return this;
        }

        /// <summary>
        /// Add a parameter to the last added command.
        /// For example, to construct a command string "Get-Process | Select-Object -Property Name"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddCommand("Get-Process")
        ///             .AddCommand("Select-Object").AddParameter("Property", "Name");
        ///     </code>
        /// </summary>
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="parameterName"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddParameter(string parameterName, object value)
        {
            lock (_syncObject)
            {
                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                _psCommand.AddParameter(parameterName, value);
                return this;
            }
        }

        /// <summary>
        /// Adds a switch parameter to the last added command.
        /// For example, to construct a command string "get-process | sort-object -descending"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("sort-object").AddParameter("descending");
        ///     </code>
        /// </summary>
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <returns>
        /// A PowerShell instance with <paramref name="parameterName"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PowerShell AddParameter(string parameterName)
        {
            lock (_syncObject)
            {
                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                _psCommand.AddParameter(parameterName);
                return this;
            }
        }

        /// <summary>
        /// Adds a <see cref="CommandParameter"/> instance to the last added command.
        /// </summary>
        internal PowerShell AddParameter(CommandParameter parameter)
        {
            lock (_syncObject)
            {
                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                _psCommand.AddParameter(parameter);
                return this;
            }
        }

        /// <summary>
        /// Adds a set of parameters to the last added command.
        /// </summary>
        /// <param name="parameters">
        /// List of parameters.
        /// </param>
        /// <returns>
        /// A PowerShell instance with the items in <paramref name="parameters"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="PSArgumentNullException">
        /// The function was given a null argument.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        public PowerShell AddParameters(IList parameters)
        {
            lock (_syncObject)
            {
                if (parameters == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(parameters));
                }

                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();

                foreach (object p in parameters)
                {
                    _psCommand.AddParameter(null, p);
                }

                return this;
            }
        }

        /// <summary>
        /// Adds a set of parameters to the last added command.
        /// </summary>
        /// <param name="parameters">
        /// Dictionary of parameters. Each key-value pair corresponds to a parameter name and its value. Keys must strings.
        /// </param>
        /// <returns>
        /// A PowerShell instance with the items in <paramref name="parameters"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="PSArgumentNullException">
        /// The function was given a null argument.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// One of the dictionary keys is not a string.
        /// </exception>
        public PowerShell AddParameters(IDictionary parameters)
        {
            lock (_syncObject)
            {
                if (parameters == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(parameters));
                }

                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();

                foreach (DictionaryEntry entry in parameters)
                {
                    if (!(entry.Key is string parameterName))
                    {
                        throw PSTraceSource.NewArgumentException(nameof(parameters), PowerShellStrings.KeyMustBeString);
                    }

                    _psCommand.AddParameter(parameterName, entry.Value);
                }

                return this;
            }
        }

        /// <summary>
        /// Adds an argument to the last added command.
        /// For example, to construct a command string "Get-Process | Select-Object Name"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create()
        ///             .AddCommand("Get-Process")
        ///             .AddCommand("Select-Object").AddArgument("Name");
        ///     </code>
        /// This will add the value "name" to the positional parameter list of "select-object"
        /// cmdlet. When the command is invoked, this value will get bound to positional parameter 0
        /// of the "select-object" cmdlet which is "Property".
        /// </summary>
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance parameter value <paramref name="value"/> added
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        public PowerShell AddArgument(object value)
        {
            lock (_syncObject)
            {
                if (_psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                _psCommand.AddArgument(value);
                return this;
            }
        }

        /// <summary>
        /// Adds an additional statement for execution
        ///
        /// For example,
        ///     <code>
        ///         Runspace rs = RunspaceFactory.CreateRunspace();
        ///         PowerShell ps = PowerShell.Create();
        ///
        ///         ps.Runspace = rs;
        ///         ps.AddCommand("Get-Process").AddArgument("idle");
        ///         ps.AddStatement().AddCommand("Get-Service").AddArgument("audiosrv");
        ///         ps.Invoke();
        ///     </code>
        /// </summary>
        /// <returns>
        /// A PowerShell instance with the items in <paramref name="parameters"/> added
        /// to the parameter list of the last command.
        /// </returns>
        public PowerShell AddStatement()
        {
            lock (_syncObject)
            {
                // for PowerShell.Create().AddStatement().AddCommand("Get-Process");
                // we reduce it to PowerShell.Create().AddCommand("Get-Process");
                if (_psCommand.Commands.Count == 0)
                {
                    return this;
                }

                AssertChangesAreAccepted();

                _psCommand.Commands[_psCommand.Commands.Count - 1].IsEndOfStatement = true;

                return this;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets current powershell command line.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public PSCommand Commands
        {
            get
            {
                return _psCommand;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Command");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    _psCommand = value.Clone();
                    _psCommand.Owner = this;
                }
            }
        }

        /// <summary>
        /// Streams generated by PowerShell invocations.
        /// </summary>
        public PSDataStreams Streams { get; }

        /// <summary>
        /// Gets or sets the error buffer. Powershell invocation writes
        /// the error data into this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<ErrorRecord> ErrorBuffer
        {
            get
            {
                return _errorBuffer;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Error");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    _errorBuffer = value;
                    ErrorBufferOwner = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress buffer. Powershell invocation writes
        /// the progress data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<ProgressRecord> ProgressBuffer
        {
            get
            {
                return InformationalBuffers.Progress;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Progress");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    InformationalBuffers.Progress = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the verbose buffer. Powershell invocation writes
        /// the verbose data into this buffer.  Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<VerboseRecord> VerboseBuffer
        {
            get
            {
                return InformationalBuffers.Verbose;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Verbose");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    InformationalBuffers.Verbose = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the debug buffer. Powershell invocation writes
        /// the debug data into this buffer.  Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<DebugRecord> DebugBuffer
        {
            get
            {
                return InformationalBuffers.Debug;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Debug");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    InformationalBuffers.Debug = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the warning buffer. Powershell invocation writes
        /// the warning data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<WarningRecord> WarningBuffer
        {
            get
            {
                return InformationalBuffers.Warning;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Warning");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    InformationalBuffers.Warning = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the information buffer. Powershell invocation writes
        /// the information data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        internal PSDataCollection<InformationRecord> InformationBuffer
        {
            get
            {
                return InformationalBuffers.Information;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Information");
                }

                lock (_syncObject)
                {
                    AssertChangesAreAccepted();
                    InformationalBuffers.Information = value;
                }
            }
        }

        /// <summary>
        /// Gets the informational buffers.
        /// </summary>
        internal PSInformationalBuffers InformationalBuffers { get; }

        /// <summary>
        /// If this flag is true, the commands in this Pipeline will redirect
        /// the global error output pipe to the command's error output pipe.
        ///
        /// (see the comment in Pipeline.RedirectShellErrorOutputPipe for an
        /// explanation of why this flag is needed)
        /// </summary>
        internal bool RedirectShellErrorOutputPipe { get; set; } = true;

        /// <summary>
        /// Get unique id for this instance of runspace pool. It is primarily used
        /// for logging purposes.
        /// </summary>
        public Guid InstanceId { get; private set; }

        /// <summary>
        /// Gets the execution state of the current PowerShell instance.
        /// </summary>
        public PSInvocationStateInfo InvocationStateInfo { get; private set; }

        /// <summary>
        /// Gets the property which indicates if this PowerShell instance
        /// is nested.
        /// </summary>
        public bool IsNested { get; private set; }

        /// <summary>
        /// Gets the property which indicates if this PowerShell instance
        /// is a child instance.
        ///
        /// IsChild flag makes it possible for the pipeline to differentiate between
        /// a true v1 nested pipeline and the cmdlets calling cmdlets case. See bug
        /// 211462.
        /// </summary>
        internal bool IsChild { get; private set; } = false;

        /// <summary>
        /// If an error occurred while executing the pipeline, this will be set to true.
        /// </summary>
        public bool HadErrors { get; private set; }

        internal void SetHadErrors(bool status)
        {
            HadErrors = status;
        }

        /// <summary>
        /// Access to the EndInvoke AsyncResult object.  Used by remote
        /// debugging to invoke debugger commands on command thread.
        /// </summary>
        internal AsyncResult EndInvokeAsyncResult
        {
            get;
            private set;
        }

        /// <summary>
        /// Event raised when PowerShell Execution State Changes.
        /// </summary>
        public event EventHandler<PSInvocationStateChangedEventArgs> InvocationStateChanged;

        /// <summary>
        /// This event gets fired when a Runspace from the RunspacePool is assigned to this PowerShell
        /// instance to invoke the commands.
        /// </summary>
        internal event EventHandler<PSEventArgs<Runspace>> RunspaceAssigned;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets an associated Runspace for this PowerShell instance.
        /// This can be null in which case a new runspace is created
        /// whenever Invoke* method is called.
        /// </summary>
        /// <remarks>
        /// This property and RunspacePool are mutually exclusive; setting one of them resets the other to null
        /// </remarks>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace", Justification = "Runspace is a well-known term in PowerShell.")]
        public Runspace Runspace
        {
            get
            {
                if (_runspace == null && _runspacePool == null) // create a runspace only if neither a runspace nor a runspace pool have been set
                {
                    lock (_syncObject)
                    {
                        if (_runspace == null && _runspacePool == null)
                        {
                            AssertChangesAreAccepted();
                            SetRunspace(RunspaceFactory.CreateRunspace(), true);
                            this.Runspace.Open();
                        }
                    }
                }

                return _runspace;
            }

            set
            {
                lock (_syncObject)
                {
                    AssertChangesAreAccepted();

                    if (_runspace != null && IsRunspaceOwner)
                    {
                        _runspace.Dispose();
                        _runspace = null;
                        IsRunspaceOwner = false;
                    }

                    SetRunspace(value, false);
                }
            }
        }

        /// <summary>
        /// Internal method to set the Runspace property.
        /// </summary>
        private void SetRunspace(Runspace runspace, bool owner)
        {
            RemoteRunspace remoteRunspace = runspace as RemoteRunspace;

            if (remoteRunspace == null)
            {
                _rsConnection = runspace;
            }
            else
            {
                _rsConnection = remoteRunspace.RunspacePool;

                if (RemotePowerShell != null)
                {
                    RemotePowerShell.Clear();
                    RemotePowerShell.Dispose();
                }

                RemotePowerShell = new ClientRemotePowerShell(this, remoteRunspace.RunspacePool.RemoteRunspacePoolInternal);
            }

            _runspace = runspace;
            IsRunspaceOwner = owner;
            _runspacePool = null;
        }

        private Runspace _runspace = null;

        /// <summary>
        /// Sets an associated RunspacePool for this PowerShell instance.
        /// A Runspace from this pool is used whenever Invoke* method
        /// is called.
        ///
        /// This can be null in which case a new runspace is created
        /// whenever Invoke* method is called.
        /// </summary>
        /// <remarks>
        /// This property and Runspace are mutually exclusive; setting one of them resets the other to null
        /// </remarks>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace", Justification = "Runspace is a well-known term in PowerShell.")]
        public RunspacePool RunspacePool
        {
            get
            {
                return _runspacePool;
            }

            set
            {
                if (value != null)
                {
                    lock (_syncObject)
                    {
                        AssertChangesAreAccepted();

                        if (_runspace != null && IsRunspaceOwner)
                        {
                            _runspace.Dispose();
                            _runspace = null;
                            IsRunspaceOwner = false;
                        }

                        _rsConnection = value;
                        _runspacePool = value;

                        if (_runspacePool.IsRemote)
                        {
                            if (RemotePowerShell != null)
                            {
                                RemotePowerShell.Clear();
                                RemotePowerShell.Dispose();
                            }

                            RemotePowerShell = new
                                ClientRemotePowerShell(this, _runspacePool.RemoteRunspacePoolInternal);
                        }

                        _runspace = null;
                    }
                }
            }
        }

        private RunspacePool _runspacePool = null;

        /// <summary>
        /// Gets the associated Runspace or RunspacePool for this PowerShell
        /// instance. If this is null, PowerShell instance is not associated
        /// with any runspace.
        /// </summary>
        internal object GetRunspaceConnection()
        {
            return _rsConnection;
        }

        #endregion

        #region Connect Support

        /// <summary>
        /// Synchronously connects to a running command on a remote server.
        /// </summary>
        /// <returns>Command output as a PSDataCollection.</returns>
        public Collection<PSObject> Connect()
        {
            // Regardless of how the command was originally invoked, set this member
            // variable to indicate the command is now running synchronously.
            _commandInvokedSynchronously = true;

            IAsyncResult asyncResult = ConnectAsync();
            PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

            // Wait for command to complete.  If an exception was thrown during command
            // execution (such as disconnect occurring during this synchronous execution)
            // then the exception will be thrown here.
            EndInvokeAsyncResult = psAsyncResult;
            psAsyncResult.EndInvoke();
            EndInvokeAsyncResult = null;

            Collection<PSObject> results = null;
            if (psAsyncResult.Output != null)
            {
                results = psAsyncResult.Output.ReadAll();
            }
            else
            {
                // Return empty collection.
                results = new Collection<PSObject>();
            }

            return results;
        }

        /// <summary>
        /// Asynchronously connects to a running command on a remote server.
        /// The returned IAsyncResult object can be used with EndInvoke() method
        /// to wait on command and/or get command returned data.
        /// </summary>
        /// <returns>IAsyncResult.</returns>
        public IAsyncResult ConnectAsync()
        {
            return ConnectAsync(null, null, null);
        }

        /// <summary>
        /// Asynchronously connects to a running command on a remote server.
        /// The returned IAsyncResult object can be used with EndInvoke() method
        /// to wait on command and/or get command returned data.
        /// </summary>
        /// <param name="output">The output buffer to return from EndInvoke.</param>
        /// <param name="invocationCallback">An AsyncCallback to be called once the previous invocation has completed.</param>
        /// <param name="state">A user supplied state to call the <paramref name="invocationCallback"/> with.</param>
        /// <returns>IAsyncResult.</returns>
        public IAsyncResult ConnectAsync(
            PSDataCollection<PSObject> output,
            AsyncCallback invocationCallback,
            object state)
        {
            if (InvocationStateInfo.State != PSInvocationState.Disconnected)
            {
                throw new InvalidPowerShellStateException(InvocationStateInfo.State);
            }

            // Ensure this is a command invoked on a remote runspace(pool) and connect the
            // runspace if it is currently disconnected.
            CheckRunspacePoolAndConnect();

            if (_connectCmdInfo != null)
            {
                //
                // This is a reconstruct/connect scenario and we create new state.
                //

                PSDataCollection<PSObject> streamToUse = OutputBuffer;

                // The remotePowerShell may have been initialized by InitForRemotePipelineConnect()
                if (!RemotePowerShell.Initialized)
                {
                    // Empty input stream.
                    ObjectStreamBase inputStream = new ObjectStream();
                    inputStream.Close();

                    // Output stream.
                    if (output != null)
                    {
                        // Use the supplied output buffer.
                        OutputBuffer = output;
                        OutputBufferOwner = false;
                    }
                    else if (OutputBuffer == null)
                    {
                        OutputBuffer = new PSDataCollection<PSObject>();
                        OutputBufferOwner = true;
                    }

                    streamToUse = OutputBuffer;

                    ObjectStreamBase outputStream = new PSDataCollectionStream<PSObject>(InstanceId, streamToUse);

                    RemotePowerShell.Initialize(inputStream, outputStream,
                                                     new PSDataCollectionStream<ErrorRecord>(InstanceId, _errorBuffer),
                                                     InformationalBuffers, null);
                }

                Dbg.Assert((_invokeAsyncResult == null), "Async result should be null in the reconstruct scenario.");
                _invokeAsyncResult = new PowerShellAsyncResult(InstanceId, invocationCallback, state, streamToUse, true);
            }
            else
            {
                // If this is not a reconstruct scenario then this must be a PowerShell object that was
                // previously disconnected, and all state should be valid.
                Dbg.Assert((_invokeAsyncResult != null && RemotePowerShell.Initialized),
                            "AsyncResult and RemotePowerShell objects must be valid here.");

                if (output != null ||
                    invocationCallback != null ||
                    _invokeAsyncResult.IsCompleted)
                {
                    // A new async object is needed.
                    PSDataCollection<PSObject> streamToUse;
                    if (output != null)
                    {
                        streamToUse = output;
                        OutputBuffer = output;
                        OutputBufferOwner = false;
                    }
                    else if (_invokeAsyncResult.Output == null ||
                             !_invokeAsyncResult.Output.IsOpen)
                    {
                        OutputBuffer = new PSDataCollection<PSObject>();
                        OutputBufferOwner = true;
                        streamToUse = OutputBuffer;
                    }
                    else
                    {
                        streamToUse = _invokeAsyncResult.Output;
                        OutputBuffer = streamToUse;
                        OutputBufferOwner = false;
                    }

                    _invokeAsyncResult = new PowerShellAsyncResult(
                        InstanceId,
                        invocationCallback ?? _invokeAsyncResult.Callback,
                        (invocationCallback != null) ? state : _invokeAsyncResult.AsyncState,
                        streamToUse,
                        true);
                }
            }

            try
            {
                // Perform the connect operation to the remote server through the PSRP layer.
                // If this.connectCmdInfo is null then a connection will be attempted using current state.
                // If this.connectCmdInfo is non-null then a connection will be attempted with this info.
                RemotePowerShell.ConnectAsync(_connectCmdInfo);
            }
            catch (Exception exception)
            {
                // allow GC collection
                _invokeAsyncResult = null;
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));

                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;
                if (poolException != null && _runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }

                throw;
            }

            return _invokeAsyncResult;
        }

        /// <summary>
        /// Checks that the current runspace associated with this PowerShell is a remote runspace,
        /// and if it is in Disconnected state then to connect it.
        /// </summary>
        private void CheckRunspacePoolAndConnect()
        {
            RemoteRunspacePoolInternal remoteRunspacePoolInternal = null;
            if (_rsConnection is RemoteRunspace)
            {
                remoteRunspacePoolInternal = (_rsConnection as RemoteRunspace).RunspacePool.RemoteRunspacePoolInternal;
            }
            else if (_rsConnection is RunspacePool)
            {
                remoteRunspacePoolInternal = (_rsConnection as RunspacePool).RemoteRunspacePoolInternal;
            }

            if (remoteRunspacePoolInternal == null)
            {
                throw new InvalidOperationException(PowerShellStrings.CannotConnect);
            }

            // Connect runspace if needed.
            if (remoteRunspacePoolInternal.RunspacePoolStateInfo.State == RunspacePoolState.Disconnected)
            {
                remoteRunspacePoolInternal.Connect();
            }

            // Make sure runspace is in valid state for connection.
            if (remoteRunspacePoolInternal.RunspacePoolStateInfo.State != RunspacePoolState.Opened)
            {
                throw new InvalidRunspacePoolStateException(RunspacePoolStrings.InvalidRunspacePoolState,
                                    remoteRunspacePoolInternal.RunspacePoolStateInfo.State, RunspacePoolState.Opened);
            }
        }

        #endregion

        #region Script Debugger Support

        /// <summary>
        /// This method allows the script debugger first crack at evaluating the
        /// command in case it is a debugger command, otherwise the command is
        /// evaluated by PowerShell.
        /// If the debugger evaluated a command then DebuggerCommand.ResumeAction
        /// value will be set appropriately.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="output">Output collection.</param>
        /// <param name="settings">PS invocation settings.</param>
        /// <param name="invokeMustRun">True if PowerShell Invoke must run regardless
        /// of whether debugger handles the command.
        /// </param>
        /// <returns>DebuggerCommandResults.</returns>
        internal void InvokeWithDebugger(
            IEnumerable<object> input,
            IList<PSObject> output,
            PSInvocationSettings settings,
            bool invokeMustRun)
        {
            Debugger debugger = _runspace.Debugger;
            bool addToHistory = true;

            if (debugger != null &&
                Commands.Commands.Count > 0)
            {
                Command cmd = this.Commands.Commands[0];

                DebuggerCommand dbgCommandResult = debugger.InternalProcessCommand(
                    cmd.CommandText, output);

                if (dbgCommandResult.ResumeAction != null ||
                    dbgCommandResult.ExecutedByDebugger)
                {
                    output.Add(new PSObject(dbgCommandResult));
                    Commands.Commands.Clear();
                    addToHistory = false;
                }
                else if (!dbgCommandResult.Command.Equals(cmd.CommandText, StringComparison.OrdinalIgnoreCase))
                {
                    // Script debugger will replace commands, e.g., "k" -> "Get-PSCallStack".
                    Commands.Commands[0] = new Command(dbgCommandResult.Command, false, true, true);

                    // Report that these replaced commands are executed by the debugger.
                    DebuggerCommand dbgCommand = new DebuggerCommand(dbgCommandResult.Command, null, false, true);
                    output.Add(new PSObject(dbgCommand));
                    addToHistory = false;
                }
            }

            if (addToHistory && (Commands.Commands.Count > 0))
            {
                addToHistory = DebuggerUtils.ShouldAddCommandToHistory(
                    Commands.Commands[0].CommandText);
            }

            // Remote PowerShell Invoke must always run Invoke so that the
            // command can complete.
            if (Commands.Commands.Count == 0 &&
                invokeMustRun)
            {
                Commands.Commands.AddScript(string.Empty);
            }

            if (Commands.Commands.Count > 0)
            {
                if (addToHistory)
                {
                    settings ??= new PSInvocationSettings();

                    settings.AddToHistory = true;
                }

                Invoke<PSObject>(input, output, settings);
            }
        }

        #endregion

        #region Invoke Overloads

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output PSObject collection.
        /// </summary>
        /// <returns>
        /// collection of PSObjects.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<PSObject> Invoke()
        {
            return Invoke(null, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output PSObject collection.
        /// </summary>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <returns>
        /// Collection of PSObjects representing output.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<PSObject> Invoke(IEnumerable input)
        {
            return Invoke(input, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output PSObject collection.
        /// </summary>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <param name="settings">
        /// Invocation Settings
        /// </param>
        /// <returns>
        /// Collection of PSObjects representing output.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<PSObject> Invoke(IEnumerable input, PSInvocationSettings settings)
        {
            Collection<PSObject> result = new Collection<PSObject>();
            PSDataCollection<PSObject> listToWriteTo = new PSDataCollection<PSObject>(result);
            CoreInvoke<PSObject>(input, listToWriteTo, settings);
            return result;
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output.
        /// </summary>
        /// <typeparam name="T">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<T> Invoke<T>()
        {
            // We should bind all the results to this instance except
            // for output.
            Collection<T> result = new Collection<T>();
            Invoke<T>(null, result, null);
            return result;
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output.
        /// </summary>
        /// <typeparam name="T">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<T> Invoke<T>(IEnumerable input)
        {
            Collection<T> result = new Collection<T>();
            Invoke<T>(input, result, null);
            return result;
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and return
        /// the output.
        /// </summary>
        /// <typeparam name="T">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <param name="settings">
        /// Invocation Settings
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<T> Invoke<T>(IEnumerable input, PSInvocationSettings settings)
        {
            Collection<T> result = new Collection<T>();
            Invoke<T>(input, result, settings);
            return result;
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and collect
        /// output data into the buffer <paramref name="output"/>
        /// </summary>
        /// <typeparam name="T">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <param name="output">
        /// A collection supplied by the user where output is collected.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> cannot be null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is
        /// already started.Stop the command and try the operation again.
        /// (or)
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public void Invoke<T>(IEnumerable input, IList<T> output)
        {
            Invoke<T>(input, output, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and collect
        /// output data into the buffer <paramref name="output"/>
        /// </summary>
        /// <typeparam name="T">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <param name="output">
        /// A collection supplied by the user where output is collected.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> cannot be null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public void Invoke<T>(IEnumerable input, IList<T> output, PSInvocationSettings settings)
        {
            if (output == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(output));
            }
            // use the above collection as the data store.
            PSDataCollection<T> listToWriteTo = new PSDataCollection<T>(output);
            CoreInvoke<T>(input, listToWriteTo, settings);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> synchronously and stream
        /// output data into the buffer <paramref name="output"/>
        /// </summary>
        /// <typeparam name="TInput">
        /// Type of input object(s) expected from the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command
        /// </param>
        /// <param name="output">
        /// Output of the command.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> cannot be null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public void Invoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            if (output == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(output));
            }

            CoreInvoke<TInput, TOutput>(input, output, settings);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> asynchronously.
        /// Use EndInvoke() to obtain the output of the command.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginInvoke()
        {
            return BeginInvoke<object>(null, null, null, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> asynchronously.
        /// Use EndInvoke() to obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// When invoked using BeginInvoke, invocation doesn't
        /// finish until Input is closed. Caller of BeginInvoke must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        ///
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling BeginInvoke().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </remarks>
        /// <typeparam name="T">
        /// Type of the input buffer
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginInvoke<T>(PSDataCollection<T> input)
        {
            return BeginInvoke<T>(input, null, null, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> asynchronously.
        /// Use EndInvoke() to obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// When invoked using BeginInvoke, invocation doesn't
        /// finish until Input is closed. Caller of BeginInvoke must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        ///
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling BeginInvoke().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </remarks>
        /// <typeparam name="T">
        /// Type of the input buffer
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings.
        /// </param>
        /// <param name="callback">
        /// An AsyncCallback to call once the BeginInvoke completes.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginInvoke<T>(PSDataCollection<T> input, PSInvocationSettings settings, AsyncCallback callback, object state)
        {
            DetermineIsBatching();

            if (OutputBuffer != null)
            {
                if (_isBatching || ExtraCommands.Count != 0)
                {
                    return BeginBatchInvoke<T, PSObject>(input, OutputBuffer, settings, callback, state);
                }

                return CoreInvokeAsync<T, PSObject>(input, OutputBuffer, settings, callback, state, null);
            }
            else
            {
                OutputBuffer = new PSDataCollection<PSObject>();
                OutputBufferOwner = true;

                if (_isBatching || ExtraCommands.Count != 0)
                {
                    return BeginBatchInvoke<T, PSObject>(input, OutputBuffer, settings, callback, state);
                }

                return CoreInvokeAsync<T, PSObject>(input, OutputBuffer, settings, callback, state, OutputBuffer);
            }
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> asynchronously.
        /// When this method is used EndInvoke() returns a null buffer.
        /// </summary>
        /// <remarks>
        /// When invoked using BeginInvoke, invocation doesn't
        /// finish until Input is closed. Caller of BeginInvoke must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        ///
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling BeginInvoke().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </remarks>
        /// <typeparam name="TInput">
        /// Type of input object(s) for the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="output">
        /// A buffer supplied by the user where output is collected.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output)
        {
            return BeginInvoke<TInput, TOutput>(input, output, null, null, null);
        }

        /// <summary>
        /// Invoke the <see cref="Command"/> asynchronously and collect
        /// output data into the buffer <paramref name="output"/>.
        /// When this method is used EndInvoke() returns a null buffer.
        /// </summary>
        /// <remarks>
        /// When invoked using BeginInvoke, invocation doesn't
        /// finish until Input is closed. Caller of BeginInvoke must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        ///
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling BeginInvoke().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </remarks>
        /// <typeparam name="TInput">
        /// Type of input object(s) for the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="output">
        /// A buffer supplied by the user where output is collected.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings.
        /// </param>
        /// <param name="callback">
        /// An AsyncCallback to call once the BeginInvoke completes.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state)
        {
            if (output == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(output));
            }

            DetermineIsBatching();

            if (_isBatching || ExtraCommands.Count != 0)
            {
                return BeginBatchInvoke<TInput, TOutput>(input, output, settings, callback, state);
            }

            return CoreInvokeAsync<TInput, TOutput>(input, output, settings, callback, state, null);
        }

        /// <summary>
        /// Invoke a PowerShell command asynchronously.
        /// Use await to wait for the command to complete and obtain the output of the command.
        /// </summary>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// </exception>
        public Task<PSDataCollection<PSObject>> InvokeAsync()
            => Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke(), _endInvokeMethod);

        /// <summary>
        /// Invoke a PowerShell command asynchronously.
        /// Use await to wait for the command to complete and obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When invoked using InvokeAsync, invocation doesn't
        /// finish until Input is closed. Caller of InvokeAsync must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        /// </para><para>
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling InvokeAsync().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        /// Type of the input buffer.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// </exception>
        public Task<PSDataCollection<PSObject>> InvokeAsync<T>(PSDataCollection<T> input)
            => Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<T>(input), _endInvokeMethod);

        /// <summary>
        /// Invoke a PowerShell command asynchronously.
        /// Use await to wait for the command to complete and obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When invoked using InvokeAsync, invocation doesn't
        /// finish until Input is closed. Caller of InvokeAsync must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        /// </para><para>
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling InvokeAsync().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        /// Type of the input buffer.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings.
        /// </param>
        /// <param name="callback">
        /// An AsyncCallback to call once the command is invoked.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// </exception>
        public Task<PSDataCollection<PSObject>> InvokeAsync<T>(PSDataCollection<T> input, PSInvocationSettings settings, AsyncCallback callback, object state)
            => Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<T>(input, settings, callback, state), _endInvokeMethod);

        /// <summary>
        /// Invoke a PowerShell command asynchronously.
        /// Use await to wait for the command to complete and obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When invoked using InvokeAsync, invocation doesn't
        /// finish until Input is closed. Caller of InvokeAsync must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        /// </para><para>
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling InvokeAsync().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </para>
        /// </remarks>
        /// <typeparam name="TInput">
        /// Type of input object(s) for the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="output">
        /// A buffer supplied by the user where output is collected.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke,
        /// or null if the caller provided their own buffer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// To collect partial output in this scenario,
        /// supply a <see cref="System.Management.Automation.PSDataCollection{T}" /> for the <paramref name="output"/> parameter,
        /// and either add a handler for the <see cref="System.Management.Automation.PSDataCollection{T}.DataAdding"/> event
        /// or catch the exception and enumerate the object supplied for <paramref name="output"/>.
        /// </exception>
        public Task<PSDataCollection<PSObject>> InvokeAsync<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output)
            => Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<TInput, TOutput>(input, output), _endInvokeMethod);

        /// <summary>
        /// Invoke a PowerShell command asynchronously and collect
        /// output data into the buffer <paramref name="output"/>.
        /// Use await to wait for the command to complete and obtain the output of the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When invoked using InvokeAsync, invocation doesn't
        /// finish until Input is closed. Caller of InvokeAsync must
        /// close the input buffer after all input has been written to
        /// input buffer. Input buffer is closed by calling
        /// Close() method.
        /// </para><para>
        /// If you want this command to execute as a standalone cmdlet
        /// (that is, using command-line parameters only),
        /// be sure to call Close() before calling InvokeAsync().  Otherwise,
        /// the command will be executed as though it had external input.
        /// If you observe that the command isn't doing anything,
        /// this may be the reason.
        /// </para>
        /// </remarks>
        /// <typeparam name="TInput">
        /// Type of input object(s) for the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="output">
        /// A buffer supplied by the user where output is collected.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings.
        /// </param>
        /// <param name="callback">
        /// An AsyncCallback to call once the command is invoked.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke,
        /// or null if the caller provided their own buffer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// To collect partial output in this scenario,
        /// supply a <see cref="System.Management.Automation.PSDataCollection{T}" /> for the <paramref name="output"/> parameter,
        /// and either add a handler for the <see cref="System.Management.Automation.PSDataCollection{T}.DataAdding"/> event
        /// or catch the exception and use object supplied for <paramref name="output"/>.
        /// </exception>
        public Task<PSDataCollection<PSObject>> InvokeAsync<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state)
            => Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<TInput, TOutput>(input, output, settings, callback, state), _endInvokeMethod);

        /// <summary>
        /// Begins a batch execution.
        /// </summary>
        /// <typeparam name="TInput">
        /// Type of input object(s) for the command invocation.
        /// </typeparam>
        /// <typeparam name="TOutput">
        /// Type of output object(s) expected from the command invocation.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command. See remarks for more details.
        /// </param>
        /// <param name="output">
        /// A buffer supplied by the user where output is collected.
        /// </param>
        /// <param name="settings">
        /// Invocation Settings.
        /// </param>
        /// <param name="callback">
        /// An AsyncCallback to call once the BeginInvoke completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        private IAsyncResult BeginBatchInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state)
        {
            if (!((object)output is PSDataCollection<PSObject> asyncOutput))
            {
                throw PSTraceSource.NewInvalidOperationException();
            }

            if (_isBatching)
            {
                SetupAsyncBatchExecution();
            }

            RunspacePool pool = _rsConnection as RunspacePool;
            if ((pool != null) && (pool.IsRemote))
            {
                // Server supports batch invocation, in this case, we just send everything to the server and return immediately
                if (ServerSupportsBatchInvocation())
                {
                    try
                    {
                        return CoreInvokeAsync<TInput, TOutput>(input, output, settings, callback, state, asyncOutput);
                    }
                    finally
                    {
                        if (_isBatching)
                        {
                            EndAsyncBatchExecution();
                        }
                    }
                }
            }

            // Non-remoting case or server does not support batching
            // In this case we execute the cmdlets one by one

            RunningExtraCommands = true;
            _batchInvocationSettings = settings;

            _batchAsyncResult = new PowerShellAsyncResult(InstanceId, callback, state, asyncOutput, true);

            CoreInvokeAsync<TInput, TOutput>(input, output, settings, new AsyncCallback(BatchInvocationCallback), state, asyncOutput);

            return _batchAsyncResult;
        }

        /// <summary>
        /// Batch invocation callback.
        /// </summary>
        /// <param name="state"></param>
        private void BatchInvocationWorkItem(object state)
        {
            Debug.Assert(ExtraCommands.Count != 0, "This callback is for batch invocation only");

            BatchInvocationContext context = state as BatchInvocationContext;

            Debug.Assert(context != null, "Context should never be null");

            PSCommand backupCommand = _psCommand;
            try
            {
                _psCommand = context.Command;

                // Last element
                if (_psCommand == ExtraCommands[ExtraCommands.Count - 1])
                {
                    RunningExtraCommands = false;
                }

                try
                {
                    IAsyncResult cmdResult = CoreInvokeAsync<object, PSObject>(null, context.Output, _batchInvocationSettings,
                        null, _batchAsyncResult.AsyncState, context.Output);

                    EndInvoke(cmdResult);
                }
                catch (ActionPreferenceStopException e)
                {
                    // We need to honor the current error action preference here
                    _stopBatchExecution = true;
                    _batchAsyncResult.SetAsCompleted(e);
                    return;
                }
                catch (Exception e)
                {
                    SetHadErrors(true);

                    // Stop if necessarily
                    if ((_batchInvocationSettings != null) && _batchInvocationSettings.ErrorActionPreference == ActionPreference.Stop)
                    {
                        _stopBatchExecution = true;
                        AppendExceptionToErrorStream(e);
                        _batchAsyncResult.SetAsCompleted(null);
                        return;
                    }

                    // If we get here, then ErrorActionPreference is either Continue,
                    // SilentlyContinue, or Inquire (Continue), so we just continue....
                    if (_batchInvocationSettings == null)
                    {
                        ActionPreference preference = (ActionPreference)Runspace.SessionStateProxy.GetVariable("ErrorActionPreference");

                        switch (preference)
                        {
                            case ActionPreference.SilentlyContinue:
                            case ActionPreference.Continue:
                                AppendExceptionToErrorStream(e);
                                break;
                            case ActionPreference.Stop:
                                _batchAsyncResult.SetAsCompleted(e);
                                return;
                            case ActionPreference.Inquire:
                            case ActionPreference.Ignore:
                                break;
                        }
                    }
                    else if (_batchInvocationSettings.ErrorActionPreference != ActionPreference.Ignore)
                    {
                        AppendExceptionToErrorStream(e);
                    }

                    // Let it continue
                }

                if (_psCommand == ExtraCommands[ExtraCommands.Count - 1])
                {
                    _batchAsyncResult.SetAsCompleted(null);
                }
            }
            finally
            {
                _psCommand = backupCommand;
                context.Signal();
            }
        }

        /// <summary>
        /// Batch invocation callback.
        /// </summary>
        /// <param name="result"></param>
        private void BatchInvocationCallback(IAsyncResult result)
        {
            Debug.Assert(ExtraCommands.Count != 0, "This callback is for batch invocation only");

            PSDataCollection<PSObject> objs = null;

            try
            {
                objs = EndInvoke(result) ?? _batchAsyncResult.Output;

                DoRemainingBatchCommands(objs);
            }
            catch (PipelineStoppedException e)
            {
                // PowerShell throws the pipeline stopped exception.
                _batchAsyncResult.SetAsCompleted(e);
                return;
            }
            catch (ActionPreferenceStopException e)
            {
                // We need to honor the current error action preference here
                _batchAsyncResult.SetAsCompleted(e);
                return;
            }
            catch (Exception e)
            {
                RunningExtraCommands = false;

                SetHadErrors(true);

                ActionPreference preference;
                if (_batchInvocationSettings != null)
                {
                    preference = _batchInvocationSettings.ErrorActionPreference ?? ActionPreference.Continue;
                }
                else
                {
                    preference = (Runspace != null) ?
                        (ActionPreference)Runspace.SessionStateProxy.GetVariable("ErrorActionPreference")
                        : ActionPreference.Continue;
                }

                switch (preference)
                {
                    case ActionPreference.SilentlyContinue:
                    case ActionPreference.Continue:
                        AppendExceptionToErrorStream(e);
                        break;
                    case ActionPreference.Stop:
                        _batchAsyncResult.SetAsCompleted(e);
                        return;
                    case ActionPreference.Inquire:
                    case ActionPreference.Ignore:
                        break;
                }

                objs ??= _batchAsyncResult.Output;

                DoRemainingBatchCommands(objs);
            }
            finally
            {
                if (_isBatching)
                {
                    EndAsyncBatchExecution();
                }
            }
        }

        /// <summary>
        /// Executes remaining batch commands.
        /// </summary>
        private void DoRemainingBatchCommands(PSDataCollection<PSObject> objs)
        {
            if (ExtraCommands.Count > 1)
            {
                for (int i = 1; i < ExtraCommands.Count; i++)
                {
                    if (_stopBatchExecution)
                    {
                        break;
                    }

                    BatchInvocationContext context = new BatchInvocationContext(ExtraCommands[i], objs);

                    // Queue a batch work item here.
                    // Calling CoreInvokeAsync / CoreInvoke here directly doesn't work and causes the thread to not respond.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BatchInvocationWorkItem), context);
                    context.Wait();
                }
            }
        }

        /// <summary>
        /// </summary>
        private void DetermineIsBatching()
        {
            foreach (Command command in _psCommand.Commands)
            {
                if (command.IsEndOfStatement)
                {
                    _isBatching = true;
                    return;
                }
            }

            _isBatching = false;
        }

        /// <summary>
        /// Prepare for async batch execution.
        /// </summary>
        private void SetupAsyncBatchExecution()
        {
            Debug.Assert(_isBatching);

            _backupPSCommand = _psCommand.Clone();
            ExtraCommands.Clear();

            PSCommand currentPipe = new PSCommand();
            currentPipe.Owner = this;

            foreach (Command command in _psCommand.Commands)
            {
                if (command.IsEndOfStatement)
                {
                    currentPipe.Commands.Add(command);
                    ExtraCommands.Add(currentPipe);
                    currentPipe = new PSCommand();
                    currentPipe.Owner = this;
                }
                else
                {
                    currentPipe.Commands.Add(command);
                }
            }

            if (currentPipe.Commands.Count != 0)
            {
                ExtraCommands.Add(currentPipe);
            }

            _psCommand = ExtraCommands[0];
        }

        /// <summary>
        /// Ends an async batch execution.
        /// </summary>
        private void EndAsyncBatchExecution()
        {
            Debug.Assert(_isBatching);

            _psCommand = _backupPSCommand;
        }

        /// <summary>
        /// Appends an exception to the error stream.
        /// </summary>
        /// <param name="e"></param>
        private void AppendExceptionToErrorStream(Exception e)
        {
            IContainsErrorRecord er = e as IContainsErrorRecord;

            if (er != null && er.ErrorRecord != null)
            {
                this.Streams.Error.Add(er.ErrorRecord);
            }
            else
            {
                this.Streams.Error.Add(new ErrorRecord(e,
                    "InvalidOperation", ErrorCategory.InvalidOperation, null));
            }
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginInvoke to complete.
        /// </summary>
        /// <param name="asyncResult">
        /// Instance of IAsyncResult returned by BeginInvoke.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke, or null if the caller provided their own buffer.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginInvoke
        /// on this PowerShell instance.
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The running PowerShell pipeline was stopped.
        /// This occurs when <see cref="PowerShell.Stop"/> or <see cref="PowerShell.StopAsync(AsyncCallback, object)"/> is called.
        /// To collect partial output in this scenario,
        /// supply a <see cref="System.Management.Automation.PSDataCollection{T}" /> to <see cref="PowerShell.BeginInvoke"/> for the <paramref name="output"/> parameter
        /// and either add a handler for the <see cref="System.Management.Automation.PSDataCollection{T}.DataAdding"/> event
        /// or catch the exception and enumerate the object supplied.
        /// </exception>
        public PSDataCollection<PSObject> EndInvoke(IAsyncResult asyncResult)
        {
            try
            {
                _commandInvokedSynchronously = true;

                if (asyncResult == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
                }

                PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

                if ((psAsyncResult == null) ||
                    (psAsyncResult.OwnerId != InstanceId) ||
                    (!psAsyncResult.IsAssociatedWithAsyncInvoke))
                {
                    throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                        PowerShellStrings.AsyncResultNotOwned, "IAsyncResult", "BeginInvoke");
                }

                EndInvokeAsyncResult = psAsyncResult;
                psAsyncResult.EndInvoke();
                EndInvokeAsyncResult = null;

                // PowerShell no longer owns the output buffer when it is passed back to the caller.
                ResetOutputBufferAsNeeded();
                return psAsyncResult.Output;
            }
            catch (InvalidRunspacePoolStateException exception)
            {
                SetHadErrors(true);
                if (_runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw exception.ToInvalidRunspaceStateException();
                }

                throw;
            }
        }

        #endregion

        #region Stop Overloads

        /// <summary>
        /// Stop the currently running command synchronously.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <remarks>
        /// When used with <see cref="PowerShell.Invoke()"/>, that call will return a partial result.
        /// When used with <see cref="PowerShell.InvokeAsync"/>, that call will throw a <see cref="System.Management.Automation.PipelineStoppedException"/>. 
        /// </remarks>
        public void Stop()
        {
            try
            {
                IAsyncResult asyncResult = CoreStop(true, null, null);
                // This is a sync call..Wait for the stop operation to complete.
                asyncResult.AsyncWaitHandle.WaitOne();

                // PowerShell no longer owns the output buffer when the pipeline is stopped by caller.
                ResetOutputBufferAsNeeded();
            }
            catch (ObjectDisposedException)
            {
                // If it's already disposed, then the client doesn't need to know.
            }
        }

        /// <summary>
        /// Stop the currently running command asynchronously. If the command is not started,
        /// the state of PowerShell instance is changed to Stopped and corresponding events
        /// will be raised.
        ///
        /// The returned IAsyncResult object can be used to wait for the stop operation
        /// to complete.
        /// </summary>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginStop completes.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        public IAsyncResult BeginStop(AsyncCallback callback, object state)
        {
            return CoreStop(false, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous BeginStop to complete.
        /// </summary>
        /// <param name="asyncResult">
        /// Instance of IAsyncResult returned by BeginStop.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginStop
        /// on this PowerShell instance.
        /// </exception>
        /// <remarks>
        /// When used with <see cref="PowerShell.Invoke()"/>, that call will return a partial result.
        /// When used with <see cref="PowerShell.InvokeAsync"/>, that call will throw a <see cref="System.Management.Automation.PipelineStoppedException"/>. 
        /// </remarks>
        public void EndStop(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

            if ((psAsyncResult == null) ||
                (psAsyncResult.OwnerId != InstanceId) ||
                (psAsyncResult.IsAssociatedWithAsyncInvoke))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                    PowerShellStrings.AsyncResultNotOwned, "IAsyncResult", "BeginStop");
            }

            psAsyncResult.EndInvoke();

            // PowerShell no longer owns the output buffer when the pipeline is stopped by caller.
            ResetOutputBufferAsNeeded();
        }

        /// <summary>
        /// Stop a PowerShell command asynchronously.
        /// Use await to wait for the command to stop.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the command is not started, the state of the PowerShell instance
        /// is changed to Stopped and corresponding events will be raised.
        /// </para>
        /// </remarks>
        /// <param name="callback">
        /// An AsyncCallback to call once the command is invoked.
        /// Note: when using this API in script, don't pass in a delegate that is cast from a script block.
        /// The callback could be invoked from a thread without a default Runspace and a delegate cast from
        /// a script block would fail in that case.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// The output buffer created to hold the results of the asynchronous invoke,
        /// or null if the caller provided their own buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        /// <remarks>
        /// When used with <see cref="PowerShell.Invoke()"/>, that call will return a partial result.
        /// When used with <see cref="PowerShell.InvokeAsync"/>, that call will throw a <see cref="System.Management.Automation.PipelineStoppedException"/>. 
        /// </remarks>
        public Task StopAsync(AsyncCallback callback, object state)
            => Task.Factory.FromAsync(BeginStop(callback, state), _endStopMethod);

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler for state changed events for the currently running pipeline.
        /// </summary>
        /// <param name="source">
        /// Source of the event.
        /// </param>
        /// <param name="stateEventArgs">
        /// Pipeline State.
        /// </param>
        private void PipelineStateChanged(object source, PipelineStateEventArgs stateEventArgs)
        {
            // we need to process the pipeline event.
            PSInvocationStateInfo targetStateInfo = new PSInvocationStateInfo(stateEventArgs.PipelineStateInfo);
            SetStateChanged(targetStateInfo);
        }

        #endregion

        #region IDisposable Overrides

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

        #endregion

        #region Internal / Private Methods / Properties

        /// <summary>
        /// Indicates if this PowerShell object is the owner of the
        /// runspace or RunspacePool assigned to this object.
        /// </summary>
        public bool IsRunspaceOwner { get; internal set; } = false;

        internal bool ErrorBufferOwner { get; set; } = true;

        internal bool OutputBufferOwner { get; set; } = true;

        /// <summary>
        /// OutputBuffer.
        /// </summary>
        internal PSDataCollection<PSObject> OutputBuffer { get; private set; }

        /// <summary>
        /// Reset the output buffer to null if it's owned by the current powershell instance.
        /// </summary>
        private void ResetOutputBufferAsNeeded()
        {
            if (OutputBufferOwner)
            {
                OutputBufferOwner = false;
                OutputBuffer = null;
            }
        }

        /// <summary>
        /// Get a steppable pipeline object.
        /// </summary>
        /// <returns>A steppable pipeline object.</returns>
        /// <exception cref="InvalidOperationException">An attempt was made to use the scriptblock outside of the engine.</exception>
        public SteppablePipeline GetSteppablePipeline()
        {
            ExecutionContext context = GetContextFromTLS();
            SteppablePipeline spl = GetSteppablePipeline(context, CommandOrigin.Internal);
            return spl;
        }

        /// <summary>
        /// Returns the current execution context from TLS, or raises an exception if it is null.
        /// </summary>
        /// <exception cref="InvalidOperationException">An attempt was made to use the scriptblock outside of the engine.</exception>
        internal ExecutionContext GetContextFromTLS()
        {
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();

            // If ExecutionContext from TLS is null then we are not in powershell engine thread.
            if (context == null)
            {
                string scriptText = this.Commands.Commands.Count > 0 ? this.Commands.Commands[0].CommandText : null;
                PSInvalidOperationException e = null;

                if (scriptText != null)
                {
                    scriptText = ErrorCategoryInfo.Ellipsize(System.Globalization.CultureInfo.CurrentUICulture, scriptText);
                    e = PSTraceSource.NewInvalidOperationException(
                        PowerShellStrings.CommandInvokedFromWrongThreadWithCommand,
                        scriptText);
                }
                else
                {
                    e = PSTraceSource.NewInvalidOperationException(
                        PowerShellStrings.CommandInvokedFromWrongThreadWithoutCommand);
                }

                e.SetErrorId("CommandInvokedFromWrongThread");
                throw e;
            }

            return context;
        }

        /// <summary>
        /// Gets the steppable pipeline from the powershell object.
        /// </summary>
        /// <param name="context">Engine execution context.</param>
        /// <param name="commandOrigin">Command origin.</param>
        /// <returns>Steppable pipeline object.</returns>
        private SteppablePipeline GetSteppablePipeline(ExecutionContext context, CommandOrigin commandOrigin)
        {
            // Check for an empty pipeline
            if (Commands.Commands.Count == 0)
            {
                return null;
            }

            PipelineProcessor pipelineProcessor = new PipelineProcessor();
            bool failed = false;

            try
            {
                foreach (Command cmd in Commands.Commands)
                {
                    CommandProcessorBase commandProcessorBase =
                            cmd.CreateCommandProcessor
                            (
                                Runspace.DefaultRunspace.ExecutionContext,
                                false,
                                IsNested ? CommandOrigin.Internal : CommandOrigin.Runspace
                            );

                    commandProcessorBase.RedirectShellErrorOutputPipe = RedirectShellErrorOutputPipe;

                    pipelineProcessor.Add(commandProcessorBase);
                }
            }
            catch (RuntimeException)
            {
                failed = true;
                throw;
            }
            catch (Exception e)
            {
                failed = true;
                throw new RuntimeException(PipelineStrings.CannotCreatePipeline, e);
            }
            finally
            {
                if (failed)
                {
                    pipelineProcessor.Dispose();
                }
            }

            return new SteppablePipeline(context, pipelineProcessor);
        }

        internal bool IsGetCommandMetadataSpecialPipeline { get; set; }

        /// <summary>
        /// Checks if the command is running.
        /// </summary>
        /// <returns></returns>
        private bool IsCommandRunning()
        {
            if (InvocationStateInfo.State == PSInvocationState.Running)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the current state is Disconnected.
        /// </summary>
        /// <returns></returns>
        private bool IsDisconnected()
        {
            return (InvocationStateInfo.State == PSInvocationState.Disconnected);
        }

        /// <summary>
        /// Checks if the command is already running.
        /// If the command is already running, throws an
        /// exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        private void AssertExecutionNotStarted()
        {
            AssertNotDisposed();
            if (IsCommandRunning())
            {
                string message = StringUtil.Format(PowerShellStrings.ExecutionAlreadyStarted);
                throw new InvalidOperationException(message);
            }

            if (IsDisconnected())
            {
                string message = StringUtil.Format(PowerShellStrings.ExecutionDisconnected);
                throw new InvalidOperationException(message);
            }

            if (InvocationStateInfo.State == PSInvocationState.Stopping)
            {
                string message = StringUtil.Format(PowerShellStrings.ExecutionStopping);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Checks if the current powershell instance can accept changes like
        /// changing one of the properties like Output, Command etc.
        /// If changes are not allowed, throws an exception.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        internal void AssertChangesAreAccepted()
        {
            lock (_syncObject)
            {
                AssertNotDisposed();
                if (IsCommandRunning() || IsDisconnected())
                {
                    throw new InvalidPowerShellStateException(InvocationStateInfo.State);
                }
            }
        }

        /// <summary>
        /// Checks if the current powershell instance is disposed.
        /// If disposed, throws ObjectDisposedException.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw PSTraceSource.NewObjectDisposedException("PowerShell");
            }
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
                lock (_syncObject)
                {
                    // if already disposed return
                    if (_isDisposed)
                    {
                        return;
                    }
                }

                // Stop the currently running command outside of the lock
                if (InvocationStateInfo.State == PSInvocationState.Running ||
                    InvocationStateInfo.State == PSInvocationState.Stopping)
                {
                    Stop();
                }

                lock (_syncObject)
                {
                    _isDisposed = true;
                }

                if (OutputBuffer != null && OutputBufferOwner)
                {
                    OutputBuffer.Dispose();
                }

                if (_errorBuffer != null && ErrorBufferOwner)
                {
                    _errorBuffer.Dispose();
                }

                if (IsRunspaceOwner)
                {
                    _runspace.Dispose();
                }

                RemotePowerShell?.Dispose();

                _invokeAsyncResult = null;
                _stopAsyncResult = null;
            }
        }

        /// <summary>
        /// Clear the internal elements.
        /// </summary>
        private void InternalClearSuppressExceptions()
        {
            lock (_syncObject)
            {
                _worker?.InternalClearSuppressExceptions();
            }
        }

        /// <summary>
        /// Raise the execution state change event handlers.
        /// </summary>
        /// <param name="stateInfo">
        /// State Information
        /// </param>
        private void RaiseStateChangeEvent(PSInvocationStateInfo stateInfo)
        {
            // First update the runspace availability.
            // The Pipeline class takes care of updating local runspaces.
            // Don't update for RemoteRunspace and nested PowerShell since this is used
            // only internally by the remote debugger.
            RemoteRunspace remoteRunspace = _runspace as RemoteRunspace;
            if (remoteRunspace != null && !this.IsNested)
            {
                _runspace.UpdateRunspaceAvailability(InvocationStateInfo.State, true, InstanceId);
            }

            if (stateInfo.State == PSInvocationState.Running)
            {
                AddToRemoteRunspaceRunningList();
            }
            else if (stateInfo.State == PSInvocationState.Completed || stateInfo.State == PSInvocationState.Stopped ||
                     stateInfo.State == PSInvocationState.Failed)
            {
                RemoveFromRemoteRunspaceRunningList();
            }

            InvocationStateChanged.SafeInvoke(this, new PSInvocationStateChangedEventArgs(stateInfo));
        }

        /// <summary>
        /// Sets the state of this powershell instance.
        /// </summary>
        /// <param name="stateInfo">The state info to set.</param>
        internal void SetStateChanged(PSInvocationStateInfo stateInfo)
        {
            PSInvocationStateInfo copyStateInfo = stateInfo;
            PSInvocationState previousState;

            // copy pipeline HasdErrors property to PowerShell instance...
            if (_worker != null && _worker.CurrentlyRunningPipeline != null)
            {
                SetHadErrors(_worker.CurrentlyRunningPipeline.HadErrors);
            }

            // win281312: Usig temporary variables to avoid thread
            // synchronization issues between Dispose and transition
            // to Terminal States (Completed/Failed/Stopped)
            PowerShellAsyncResult tempInvokeAsyncResult;
            PowerShellAsyncResult tempStopAsyncResult;

            lock (_syncObject)
            {
                previousState = InvocationStateInfo.State;

                // Check the current state and see if we need to process this pipeline event.
                switch (InvocationStateInfo.State)
                {
                    case PSInvocationState.Completed:
                    case PSInvocationState.Failed:
                    case PSInvocationState.Stopped:
                        // if the current state is already completed..then no need to process state
                        // change requests. This will happen if another thread calls BeginStop
                        return;
                    case PSInvocationState.Running:
                        if (stateInfo.State == PSInvocationState.Running)
                        {
                            return;
                        }

                        break;
                    case PSInvocationState.Stopping:
                        // We are in stopping state and we should not honor Running state
                        // here.
                        if (stateInfo.State == PSInvocationState.Running ||
                            stateInfo.State == PSInvocationState.Stopping)
                        {
                            return;
                        }
                        else if (stateInfo.State == PSInvocationState.Completed ||
                                 stateInfo.State == PSInvocationState.Failed)
                        {
                            copyStateInfo = new PSInvocationStateInfo(PSInvocationState.Stopped, stateInfo.Reason);
                        }

                        break;
                    default:
                        break;
                }

                tempInvokeAsyncResult = _invokeAsyncResult;
                tempStopAsyncResult = _stopAsyncResult;
                InvocationStateInfo = copyStateInfo;
            }

            bool isExceptionOccured = false;
            switch (InvocationStateInfo.State)
            {
                case PSInvocationState.Running:
                    CloseInputBufferOnReconnection(previousState);
                    RaiseStateChangeEvent(InvocationStateInfo.Clone());
                    break;
                case PSInvocationState.Stopping:
                    RaiseStateChangeEvent(InvocationStateInfo.Clone());
                    break;
                case PSInvocationState.Completed:
                case PSInvocationState.Failed:
                case PSInvocationState.Stopped:
                    // Clear Internal data
                    InternalClearSuppressExceptions();

                    // Ensure remote receive queue is not blocked.
                    if (RemotePowerShell != null)
                    {
                        ResumeIncomingData();
                    }

                    try
                    {
                        if (RunningExtraCommands)
                        {
                            tempInvokeAsyncResult?.SetAsCompleted(InvocationStateInfo.Reason);

                            RaiseStateChangeEvent(InvocationStateInfo.Clone());
                        }
                        else
                        {
                            RaiseStateChangeEvent(InvocationStateInfo.Clone());

                            tempInvokeAsyncResult?.SetAsCompleted(InvocationStateInfo.Reason);
                        }

                        tempStopAsyncResult?.SetAsCompleted(null);
                    }
                    catch (Exception)
                    {
                        // need to release asyncresults if there is an
                        // exception from the eventhandlers.
                        isExceptionOccured = true;
                        SetHadErrors(true);
                        throw;
                    }
                    finally
                    {
                        // takes care exception occurred with invokeAsyncResult
                        if (isExceptionOccured && (tempStopAsyncResult != null))
                        {
                            tempStopAsyncResult.Release();
                        }
                    }

                    break;
                case PSInvocationState.Disconnected:
                    try
                    {
                        // Ensure remote receive queue is not blocked.
                        if (RemotePowerShell != null)
                        {
                            ResumeIncomingData();
                        }

                        // If this command was disconnected and was also invoked synchronously then
                        // we throw an exception on the calling thread.
                        if (_commandInvokedSynchronously && (tempInvokeAsyncResult != null))
                        {
                            tempInvokeAsyncResult.SetAsCompleted(new RuntimeException(PowerShellStrings.DiscOnSyncCommand));
                        }

                        // This object can be disconnected even if "BeginStop" was called if it is a remote object
                        // and robust connections is retrying a failed network connection.
                        // In this case release the stop wait handle to prevent not responding.
                        tempStopAsyncResult?.SetAsCompleted(null);

                        // Only raise the Disconnected state changed event if the PowerShell state
                        // actually transitions to Disconnected from some other state.  This condition
                        // can happen when the corresponding runspace disconnects/connects multiple
                        // times with the command remaining in Disconnected state.
                        if (previousState != PSInvocationState.Disconnected)
                        {
                            RaiseStateChangeEvent(InvocationStateInfo.Clone());
                        }
                    }
                    catch (Exception)
                    {
                        // need to release asyncresults if there is an
                        // exception from the eventhandlers.
                        isExceptionOccured = true;
                        SetHadErrors(true);
                        throw;
                    }
                    finally
                    {
                        // takes care exception occurred with invokeAsyncResult
                        if (isExceptionOccured && (tempStopAsyncResult != null))
                        {
                            tempStopAsyncResult.Release();
                        }
                    }

                    // Make sure the connect command information is null when going to Disconnected state.
                    // This parameter is used to determine reconnect/reconstruct scenarios.  Setting to null
                    // means we have a reconnect scenario.
                    _connectCmdInfo = null;
                    break;

                default:
                    return;
            }
        }

        /// <summary>
        /// Helper function to close the input buffer after command is reconnected.
        /// </summary>
        /// <param name="previousState">Previous state.</param>
        private void CloseInputBufferOnReconnection(PSInvocationState previousState)
        {
            // If the previous state was disconnected and we are now running (reconnected),
            // and we reconnected synchronously with pending input, then we need to close
            // the input buffer to allow the remote command to complete.  Otherwise the
            // synchronous Connect() method will wait indefinitely for the command to complete.
            if (previousState == PSInvocationState.Disconnected &&
                _commandInvokedSynchronously &&
                RemotePowerShell.InputStream != null &&
                RemotePowerShell.InputStream.IsOpen &&
                RemotePowerShell.InputStream.Count > 0)
            {
                RemotePowerShell.InputStream.Close();
            }
        }

        /// <summary>
        /// Clear the internal reference to remote powershell.
        /// </summary>
        internal void ClearRemotePowerShell()
        {
            lock (_syncObject)
            {
                RemotePowerShell?.Clear();
            }
        }

        /// <summary>
        /// Sets if the pipeline is nested, typically used by the remoting infrastructure.
        /// </summary>
        /// <param name="isNested"></param>
        internal void SetIsNested(bool isNested)
        {
            AssertChangesAreAccepted();

            IsNested = isNested;
        }

        /// <summary>
        /// Performs the actual synchronous command invocation. The caller
        /// should check if it safe to call this method.
        /// </summary>
        /// <typeparam name="TOutput">
        /// Type of objects to return.
        /// </typeparam>
        /// <param name="input">
        /// Input to the command.
        /// </param>
        /// <param name="output">
        /// output from the command
        /// </param>
        /// <param name="settings">
        /// Invocation settings.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// No commands are specified.
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// PowerShell.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        private void CoreInvoke<TOutput>(IEnumerable input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            PSDataCollection<object> inputBuffer = null;
            if (input != null)
            {
                inputBuffer = new PSDataCollection<object>();
                foreach (object o in input)
                {
                    inputBuffer.Add(o);
                }

                inputBuffer.Complete();
            }

            CoreInvoke(inputBuffer, output, settings);
        }

        /// <summary>
        /// Core invocation helper method.
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">Input objects.</param>
        /// <param name="output">Output object.</param>
        /// <param name="settings">Invocation settings.</param>
        private void CoreInvokeHelper<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            RunspacePool pool = _rsConnection as RunspacePool;

            // Prepare the environment...non-remoting case.
            Prepare<TInput, TOutput>(input, output, settings, true);

            try
            {
                // Invoke in the same thread as the calling thread.
                Runspace rsToUse = null;
                if (!IsNested)
                {
                    if (pool != null)
                    {
#if !UNIX
                        VerifyThreadSettings(settings, pool.ApartmentState, pool.ThreadOptions, false);
#endif

                        // getting the runspace asynchronously so that Stop can be supported from a different
                        // thread.
                        _worker.GetRunspaceAsyncResult = pool.BeginGetRunspace(null, null);
                        _worker.GetRunspaceAsyncResult.AsyncWaitHandle.WaitOne();
                        rsToUse = pool.EndGetRunspace(_worker.GetRunspaceAsyncResult);
                    }
                    else
                    {
                        rsToUse = _rsConnection as Runspace;
                        if (rsToUse != null)
                        {
#if !UNIX
                            VerifyThreadSettings(settings, rsToUse.ApartmentState, rsToUse.ThreadOptions, false);
#endif

                            if (rsToUse.RunspaceStateInfo.State != RunspaceState.Opened)
                            {
                                string message = StringUtil.Format(PowerShellStrings.InvalidRunspaceState, RunspaceState.Opened, rsToUse.RunspaceStateInfo.State);

                                InvalidRunspaceStateException e = new InvalidRunspaceStateException(message,
                                        rsToUse.RunspaceStateInfo.State,
                                        RunspaceState.Opened
                                    );

                                throw e;
                            }
                        }
                    }

                    // perform the work in the current thread
                    _worker.CreateRunspaceIfNeededAndDoWork(rsToUse, true);
                }
                else
                {
                    rsToUse = _rsConnection as Runspace;
                    Dbg.Assert(rsToUse != null,
                        "Nested PowerShell can only work on a Runspace");

                    // Perform work on the current thread. Nested Pipeline
                    // should be invoked from the same thread that the parent
                    // pipeline is executing in.
                    _worker.ConstructPipelineAndDoWork(rsToUse, true);
                }
            }
            catch (Exception exception)
            {
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));

                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;

                if (poolException != null && _runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }

                throw;
            }
        }

        /// <summary>
        /// Core invocation helper method for remoting.
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">Input objects.</param>
        /// <param name="output">Output object.</param>
        /// <param name="settings">Invocation settings.</param>
        private void CoreInvokeRemoteHelper<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            RunspacePool pool = _rsConnection as RunspacePool;

            // For remote calls..use the infrastructure built in CoreInvokeAsync..
            IAsyncResult asyncResult = CoreInvokeAsync<TInput, TOutput>(input, output, settings,
                null, null, null);
            _commandInvokedSynchronously = true;
            PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

            // Wait for command to complete.  If an exception was thrown during command
            // execution (such as disconnect occurring during this synchronous execution)
            // then the exception will be thrown here.
            EndInvokeAsyncResult = psAsyncResult;
            psAsyncResult.EndInvoke();
            EndInvokeAsyncResult = null;

            if ((InvocationStateInfo.State == PSInvocationState.Failed) &&
                        (InvocationStateInfo.Reason != null))
            {
                throw InvocationStateInfo.Reason;
            }

            return;
        }

        /// <summary>
        /// Core invocation method.
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">Input objects.</param>
        /// <param name="output">Output object.</param>
        /// <param name="settings">Invocation settings.</param>
        private void CoreInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            bool isRemote = false;

            DetermineIsBatching();

            if (_isBatching)
            {
                SetupAsyncBatchExecution();
            }

            SetHadErrors(false);
            RunspacePool pool = _rsConnection as RunspacePool;
            if ((pool != null) && (pool.IsRemote))
            {
                if (ServerSupportsBatchInvocation())
                {
                    try
                    {
                        CoreInvokeRemoteHelper(input, output, settings);
                    }
                    finally
                    {
                        if (_isBatching)
                        {
                            EndAsyncBatchExecution();
                        }
                    }

                    return;
                }

                isRemote = true;
            }

            if (_isBatching)
            {
                try
                {
                    foreach (PSCommand command in ExtraCommands)
                    {
                        // Last element
                        if (_psCommand != ExtraCommands[ExtraCommands.Count - 1])
                        {
                            RunningExtraCommands = true;
                        }
                        else
                        {
                            RunningExtraCommands = false;
                        }

                        try
                        {
                            _psCommand = command;

                            if (isRemote)
                            {
                                CoreInvokeRemoteHelper(input, output, settings);
                            }
                            else
                            {
                                CoreInvokeHelper(input, output, settings);
                            }
                        }
                        catch (ActionPreferenceStopException)
                        {
                            // We need to honor the current error action preference here
                            throw;
                        }
                        catch (Exception e)
                        {
                            SetHadErrors(true);

                            // Stop if necessarily
                            if ((settings != null) && settings.ErrorActionPreference == ActionPreference.Stop)
                            {
                                throw;
                            }

                            // Ignore the exception if necessary.
                            if ((settings != null) && settings.ErrorActionPreference == ActionPreference.Ignore)
                            {
                                continue;
                            }

                            // If we get here, then ErrorActionPreference is either Continue,
                            // SilentlyContinue, or Inquire (Continue), so we just continue....

                            IContainsErrorRecord er = e as IContainsErrorRecord;

                            if (er != null && er.ErrorRecord != null)
                            {
                                this.Streams.Error.Add(er.ErrorRecord);
                            }
                            else
                            {
                                this.Streams.Error.Add(new ErrorRecord(e,
                                    "InvalidOperation", ErrorCategory.InvalidOperation, null));
                            }

                            continue;
                        }
                    }
                }
                finally
                {
                    RunningExtraCommands = false;
                    EndAsyncBatchExecution();
                }
            }
            else
            {
                RunningExtraCommands = false;

                if (isRemote)
                {
                    CoreInvokeRemoteHelper(input, output, settings);
                }
                else
                {
                    CoreInvokeHelper(input, output, settings);
                }
            }
        }

        /// <summary>
        /// Performs the actual asynchronous command invocation.
        /// </summary>
        /// <typeparam name="TInput">Type of the input buffer</typeparam>
        /// <typeparam name="TOutput">Type of the output buffer</typeparam>
        /// <param name="input">
        /// input can be null
        /// </param>
        /// <param name="output"></param>
        /// <param name="settings"></param>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginInvoke completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <param name="asyncResultOutput">
        /// The output buffer to attach to the IAsyncResult returned by this method
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// (or)
        /// No command is added.
        /// (or)
        /// BeginInvoke is called on nested powershell. Nested
        /// Powershell cannot be executed Asynchronously.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private IAsyncResult CoreInvokeAsync<TInput, TOutput>(PSDataCollection<TInput> input,
            PSDataCollection<TOutput> output, PSInvocationSettings settings,
            AsyncCallback callback, object state, PSDataCollection<PSObject> asyncResultOutput)
        {
            RunspacePool pool = _rsConnection as RunspacePool;

            // We dont need to create worker if pool is remote
            Prepare<TInput, TOutput>(input, output, settings, (pool == null || !pool.IsRemote));

            _invokeAsyncResult = new PowerShellAsyncResult(InstanceId, callback, state, asyncResultOutput, true);

            try
            {
                // IsNested is true for the icm | % { icm } scenario
                if (!IsNested || (pool != null && pool.IsRemote))
                {
                    if (pool != null)
                    {
#if !UNIX
                        VerifyThreadSettings(settings, pool.ApartmentState, pool.ThreadOptions, pool.IsRemote);
#endif

                        pool.AssertPoolIsOpen();

                        // for executing in a remote runspace pool case
                        if (pool.IsRemote)
                        {
                            _worker = null;

                            lock (_syncObject)
                            {
                                // for remoting case, when the state is set to
                                // Running, the message should have been sent
                                // to the server. In order to ensure the same
                                // all of the following are placed inside the
                                // lock
                                //    1. set the state to Running
                                //    2. create remotePowerShell
                                //    3. Send message to server

                                // set the execution state to running.. so changes
                                // to the current instance of powershell
                                // are blocked.
                                AssertExecutionNotStarted();

                                InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Running, null);

                                ObjectStreamBase inputStream = null;

                                if (input != null)
                                {
                                    inputStream = new PSDataCollectionStream<TInput>(InstanceId, input);
                                }

                                if (!RemotePowerShell.Initialized)
                                {
                                    if (inputStream == null)
                                    {
                                        inputStream = new ObjectStream();
                                        inputStream.Close();
                                    }

                                    RemotePowerShell.Initialize(
                                        inputStream, new PSDataCollectionStream<TOutput>(InstanceId, output),
                                                new PSDataCollectionStream<ErrorRecord>(InstanceId, _errorBuffer),
                                                    InformationalBuffers, settings);
                                }
                                else
                                {
                                    if (inputStream != null)
                                    {
                                        RemotePowerShell.InputStream = inputStream;
                                    }

                                    if (output != null)
                                    {
                                        RemotePowerShell.OutputStream =
                                            new PSDataCollectionStream<TOutput>(InstanceId, output);
                                    }
                                }

                                pool.RemoteRunspacePoolInternal.CreatePowerShellOnServerAndInvoke(RemotePowerShell);
                            }

                            RaiseStateChangeEvent(InvocationStateInfo.Clone());
                        }
                        else
                        {
                            _worker.GetRunspaceAsyncResult = pool.BeginGetRunspace(
                                    new AsyncCallback(_worker.RunspaceAvailableCallback), null);
                        }
                    }
                    else
                    {
                        LocalRunspace rs = _rsConnection as LocalRunspace;
                        if (rs != null)
                        {
#if !UNIX
                            VerifyThreadSettings(settings, rs.ApartmentState, rs.ThreadOptions, false);
#endif

                            if (rs.RunspaceStateInfo.State != RunspaceState.Opened)
                            {
                                string message = StringUtil.Format(PowerShellStrings.InvalidRunspaceState, RunspaceState.Opened, rs.RunspaceStateInfo.State);

                                InvalidRunspaceStateException e = new InvalidRunspaceStateException(message,
                                        rs.RunspaceStateInfo.State,
                                        RunspaceState.Opened
                                    );

                                throw e;
                            }

                            _worker.CreateRunspaceIfNeededAndDoWork(rs, false);
                        }
                        else
                        {
                            // create a new runspace and perform invoke..
                            ThreadPool.QueueUserWorkItem(
                                new WaitCallback(_worker.CreateRunspaceIfNeededAndDoWork),
                                _rsConnection);
                        }
                    }
                }
                else
                {
                    // Nested PowerShell
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.NestedPowerShellInvokeAsync);
                }
            }
            catch (Exception exception)
            {
                // allow GC collection
                _invokeAsyncResult = null;
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));
                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;

                if (poolException != null && _runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }

                throw;
            }

            return _invokeAsyncResult;
        }

        // Apartment thread state does not apply to non-Windows platforms.
#if !UNIX
        /// <summary>
        /// Verifies the settings for ThreadOptions and ApartmentState.
        /// </summary>
        private static void VerifyThreadSettings(PSInvocationSettings settings, ApartmentState runspaceApartmentState, PSThreadOptions runspaceThreadOptions, bool isRemote)
        {
            ApartmentState apartmentState;

            if (settings != null && settings.ApartmentState != ApartmentState.Unknown)
            {
                apartmentState = settings.ApartmentState;
            }
            else
            {
                apartmentState = runspaceApartmentState;
            }

            if (runspaceThreadOptions == PSThreadOptions.ReuseThread)
            {
                if (apartmentState != runspaceApartmentState)
                {
                    throw new InvalidOperationException(PowerShellStrings.ApartmentStateMismatch);
                }
            }
            else if (runspaceThreadOptions == PSThreadOptions.UseCurrentThread)
            {
                if (!isRemote) // on remote calls this check needs to be done by the server
                {
                    if (apartmentState != ApartmentState.Unknown && apartmentState != Thread.CurrentThread.GetApartmentState())
                    {
                        throw new InvalidOperationException(PowerShellStrings.ApartmentStateMismatchCurrentThread);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// </summary>
        /// <typeparam name="TInput">Type for the input collection</typeparam>
        /// <typeparam name="TOutput">Type for the output collection</typeparam>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="settings"></param>
        /// <param name="shouldCreateWorker"></param>
        /// <exception cref="InvalidOperationException">
        /// Cannot perform the operation because the command is already started.
        /// Stop the command and try the operation again.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        private void Prepare<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, bool shouldCreateWorker)
        {
            Dbg.Assert(output != null, "Output cannot be null");

            lock (_syncObject)
            {
                if ((_psCommand == null) || (_psCommand.Commands == null) || (_psCommand.Commands.Count == 0))
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.NoCommandToInvoke);
                }
                // If execution has already started this will throw
                AssertExecutionNotStarted();

                if (shouldCreateWorker)
                {
                    // set the execution state to running.. so changes
                    // to the current instance of powershell
                    // are blocked.
                    InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Running, null);

                    // update settings for impersonation policy
                    if ((settings != null) && (settings.FlowImpersonationPolicy))
                    {
                        // get the identity of the thread.
                        // false behavior: If the thread is impersonating the WindowsIdentity for the
                        // thread is returned. If the thread is not impersonating, the WindowsIdentity of
                        // the process is returned.
                        settings.WindowsIdentityToImpersonate =
                            System.Security.Principal.WindowsIdentity.GetCurrent(false);
                    }

                    // Create the streams and handoff these to the pipeline
                    // this way pipeline will not waste resources creating
                    // the same.
                    ObjectStreamBase inputStream;
                    if (input != null)
                    {
                        inputStream = new PSDataCollectionStream<TInput>(InstanceId, input);
                    }
                    else
                    {
                        inputStream = new ObjectStream();
                        inputStream.Close();
                    }

                    ObjectStreamBase outputStream = new PSDataCollectionStream<TOutput>(InstanceId, output);
                    _worker = new Worker(inputStream, outputStream, settings, this);
                }
            }

            // Only one thread will be running after this point
            // so no need to lock.

            if (shouldCreateWorker)
            {
                // Raise the state change events outside of the lock
                // send a cloned copy..this way the handler will
                // not see changes happening to the instance's execution state.
                RaiseStateChangeEvent(InvocationStateInfo.Clone());
            }
        }

        /// <summary>
        /// Called by both Sync Stop and Async Stop.
        /// If isSyncCall is false, then an IAsyncResult object is returned which
        /// can be passed back to the user.
        /// </summary>
        /// <param name="isSyncCall">
        /// true if pipeline to be stopped synchronously,
        /// false otherewise.
        /// </param>
        /// <param name="callback">
        /// Valid for asynchronous stop
        /// </param>
        /// <param name="state">
        /// Valid for asynchronous stop
        /// </param>
        private IAsyncResult CoreStop(bool isSyncCall, AsyncCallback callback, object state)
        {
            bool isRunning = false;
            bool isDisconnected = false;
            Queue<PSInvocationStateInfo> events = new Queue<PSInvocationStateInfo>();

            // Acquire lock as we are going to change state here..
            lock (_syncObject)
            {
                // BUGBUG: remote powershell appears to handle state change's differently
                // Need to speak with remoting dev and resolve this.
                switch (InvocationStateInfo.State)
                {
                    case PSInvocationState.NotStarted:
                        // Stopped is called before operation started..we need to change
                        // state to stopping and then to stopped... so that future stops
                        // dont affect the state.
                        InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Stopping,
                            null);

                        events.Enqueue(new PSInvocationStateInfo(PSInvocationState.Stopped,
                            null));
                        break;

                    case PSInvocationState.Completed:
                    case PSInvocationState.Failed:
                    case PSInvocationState.Stopped:
                        _stopAsyncResult = new PowerShellAsyncResult(InstanceId, callback, state, null, false);
                        _stopAsyncResult.SetAsCompleted(null);
                        return _stopAsyncResult;

                    case PSInvocationState.Stopping:
                        // Create new stop sync object if none exists.  Otherwise return existing.
                        if (_stopAsyncResult == null)
                        {
                            _stopAsyncResult = new PowerShellAsyncResult(InstanceId, callback, state, null, false);
                            _stopAsyncResult.SetAsCompleted(null);
                        }

                        return _stopAsyncResult;

                    case PSInvocationState.Running:
                        InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Stopping,
                            null);
                        isRunning = true;
                        break;

                    case PSInvocationState.Disconnected:
                        // Stopping a disconnected command results in a failed state.
                        InvocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Failed, null);
                        isDisconnected = true;
                        break;
                }

                _stopAsyncResult = new PowerShellAsyncResult(InstanceId, callback, state, null, false);
            }

            // If in the Disconnected state then stopping simply cuts loose the PowerShell object
            // so that a new one can be connected.  The state is set to Failed since the command
            // cannot complete with this object.
            if (isDisconnected)
            {
                // Since object is stopped, allow result wait to end.
                _invokeAsyncResult?.SetAsCompleted(null);

                _stopAsyncResult.SetAsCompleted(null);

                // Raise event for failed state change.
                RaiseStateChangeEvent(InvocationStateInfo.Clone());

                return _stopAsyncResult;
            }

            // Ensure the runspace is not blocking in a debug stop.
            ReleaseDebugger();

            RaiseStateChangeEvent(InvocationStateInfo.Clone());

            bool shouldRunStopHelper = false;
            RunspacePool pool = _rsConnection as RunspacePool;

            if (pool != null && pool.IsRemote)
            {
                if ((RemotePowerShell != null) && RemotePowerShell.Initialized)
                {
                    RemotePowerShell.StopAsync();

                    if (isSyncCall)
                    {
                        _stopAsyncResult.AsyncWaitHandle.WaitOne();
                    }
                }
                else
                {
                    shouldRunStopHelper = true;
                }
            }
            else if (isRunning)
            {
                _worker.Stop(isSyncCall);
            }
            else
            {
                shouldRunStopHelper = true;
            }

            if (shouldRunStopHelper)
            {
                if (isSyncCall)
                {
                    StopHelper(events);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StopThreadProc), events);
                }
            }

            return _stopAsyncResult;
        }

        private void ReleaseDebugger()
        {
            LocalRunspace localRunspace = _runspace as LocalRunspace;
            localRunspace?.ReleaseDebugger();
        }

        /// <summary>
        /// If there is no worker assigned yet, we need to emulate stop here.
        /// In Asynchronous stop case, we need to send event change notifications
        /// from a different thread.
        /// </summary>
        /// <param name="state"></param>
        private void StopHelper(object state)
        {
            Queue<PSInvocationStateInfo> events = state as Queue<PSInvocationStateInfo>;
            Dbg.Assert(events != null,
                "StopImplementation expects a Queue<PSInvocationStateInfo> as parameter");

            // Raise the events outside of the lock..this way 3rd party callback
            // cannot hold our lock.
            while (events.Count > 0)
            {
                PSInvocationStateInfo targetStateInfo = events.Dequeue();
                SetStateChanged(targetStateInfo);
            }

            // Clear internal resources
            InternalClearSuppressExceptions();
        }

        private void StopThreadProc(object state)
        {
            // variable to keep track of exceptions.
            Exception exception = null;

            try
            {
                StopHelper(state);
            }
            catch (Exception e)
            {
                // report non-severe exceptions to the user via the
                // asyncresult object
                exception = e;
                throw;
            }
        }

        /// <summary>
        /// The client remote powershell associated with this
        /// powershell object.
        /// </summary>
        internal ClientRemotePowerShell RemotePowerShell { get; private set; }

        /// <summary>
        /// The history string to be used for displaying
        /// the history.
        /// </summary>
        public string HistoryString { get; set; }

        /// <summary>
        /// Extra commands to run in a single invocation.
        /// </summary>
        internal Collection<PSCommand> ExtraCommands { get; }

        /// <summary>
        /// Currently running extra commands.
        /// </summary>
        internal bool RunningExtraCommands { get; private set; }

        private bool ServerSupportsBatchInvocation()
        {
            if (_runspace != null)
            {
                return _runspace.RunspaceStateInfo.State != RunspaceState.BeforeOpen &&
                       _runspace.GetRemoteProtocolVersion() >= RemotingConstants.ProtocolVersion_2_2;
            }

            RemoteRunspacePoolInternal remoteRunspacePoolInternal = null;
            if (_rsConnection is RemoteRunspace)
            {
                remoteRunspacePoolInternal = (_rsConnection as RemoteRunspace).RunspacePool.RemoteRunspacePoolInternal;
            }
            else if (_rsConnection is RunspacePool)
            {
                remoteRunspacePoolInternal = (_rsConnection as RunspacePool).RemoteRunspacePoolInternal;
            }

            return remoteRunspacePoolInternal != null &&
                   remoteRunspacePoolInternal.PSRemotingProtocolVersion >= RemotingConstants.ProtocolVersion_2_2;
        }

        /// <summary>
        /// Helper method to add running remote PowerShell to the remote runspace list.
        /// </summary>
        private void AddToRemoteRunspaceRunningList()
        {
            if (_runspace != null)
            {
                _runspace.PushRunningPowerShell(this);
            }
            else
            {
                RemoteRunspacePoolInternal remoteRunspacePoolInternal = GetRemoteRunspacePoolInternal();
                remoteRunspacePoolInternal?.PushRunningPowerShell(this);
            }
        }

        /// <summary>
        /// Helper method to remove running remote PowerShell from the remote runspacelist.
        /// </summary>
        private void RemoveFromRemoteRunspaceRunningList()
        {
            if (_runspace != null)
            {
                _runspace.PopRunningPowerShell();
            }
            else
            {
                RemoteRunspacePoolInternal remoteRunspacePoolInternal = GetRemoteRunspacePoolInternal();
                remoteRunspacePoolInternal?.PopRunningPowerShell();
            }
        }

        private RemoteRunspacePoolInternal GetRemoteRunspacePoolInternal()
        {
            RunspacePool runspacePool = _rsConnection as RunspacePool;
            return runspacePool?.RemoteRunspacePoolInternal;
        }

        #endregion

        #region Worker

        /// <summary>
        /// AsyncResult object used to monitor pipeline creation and invocation.
        /// This is needed as a Runspace may not be available in the RunspacePool.
        /// </summary>
        private sealed class Worker
        {
            private readonly ObjectStreamBase _inputStream;
            private readonly ObjectStreamBase _outputStream;
            private readonly ObjectStreamBase _errorStream;
            private readonly PSInvocationSettings _settings;
            private bool _isNotActive;
            private readonly PowerShell _shell;
            private readonly object _syncObject = new object();

            /// <summary>
            /// </summary>
            /// <param name="inputStream"></param>
            /// <param name="outputStream"></param>
            /// <param name="settings"></param>
            /// <param name="shell"></param>
            internal Worker(ObjectStreamBase inputStream,
                ObjectStreamBase outputStream,
                PSInvocationSettings settings,
                PowerShell shell)
            {
                _inputStream = inputStream;
                _outputStream = outputStream;
                _errorStream = new PSDataCollectionStream<ErrorRecord>(shell.InstanceId, shell._errorBuffer);
                _settings = settings;
                _shell = shell;
            }

            /// <summary>
            /// Sets the async result object that monitors a
            /// BeginGetRunspace async operation on the
            /// RunspacePool.
            /// </summary>
            internal IAsyncResult GetRunspaceAsyncResult { get; set; }

            /// <summary>
            /// Gets the currently running pipeline.
            /// </summary>
            internal Pipeline CurrentlyRunningPipeline { get; private set; }

            /// <summary>
            /// This method gets invoked from a ThreadPool thread.
            /// </summary>
            /// <param name="state"></param>
            internal void CreateRunspaceIfNeededAndDoWork(object state)
            {
                Runspace rsToUse = state as Runspace;
                CreateRunspaceIfNeededAndDoWork(rsToUse, false);
            }

            /// <summary>
            /// This method gets invoked when PowerShell is not associated
            /// with a RunspacePool.
            /// </summary>
            /// <param name="rsToUse">
            /// User supplied Runspace if any.
            /// </param>
            /// <param name="isSync">
            /// true if Invoke() should be used to invoke pipeline
            /// false if InvokeAsync() should be used.
            /// </param>
            /// <remarks>
            /// All exceptions are caught and reported via a
            /// PipelineStateChanged event.
            /// </remarks>
            internal void CreateRunspaceIfNeededAndDoWork(Runspace rsToUse, bool isSync)
            {
#pragma warning disable 56500
                try
                {
                    // Set the host for this local runspace if user specified one.
                    LocalRunspace rs = rsToUse as LocalRunspace;
                    if (rs == null)
                    {
                        lock (_shell._syncObject)
                        {
                            if (_shell._runspace != null)
                            {
                                rsToUse = _shell._runspace;
                            }
                            else
                            {
                                Runspace runspace = null;

                                if ((_settings != null) && (_settings.Host != null))
                                {
                                    runspace = RunspaceFactory.CreateRunspace(_settings.Host);
                                }
                                else
                                {
                                    runspace = RunspaceFactory.CreateRunspace();
                                }

                                _shell.SetRunspace(runspace, true);

                                rsToUse = (LocalRunspace)runspace;
                                rsToUse.Open();
                            }
                        }
                    }

                    ConstructPipelineAndDoWork(rsToUse, isSync);
                }
                catch (Exception e)
                {
                    // PipelineStateChangedEvent is not raised
                    // if there is an exception calling BeginInvoke
                    // So raise the event here and notify the caller.
                    lock (_syncObject)
                    {
                        if (_isNotActive)
                            return;
                        _isNotActive = true;
                    }

                    _shell.PipelineStateChanged(this,
                           new PipelineStateEventArgs(
                               new PipelineStateInfo(PipelineState.Failed,
                                   e)));

                    if (isSync)
                    {
                        throw;
                    }
                }
#pragma warning restore 56500
            }

            /// <summary>
            /// This method gets called from a ThreadPool thread.
            /// This method gets called from a RunspacePool thread when a
            /// Runspace is available.
            /// </summary>
            /// <param name="asyncResult">
            /// AsyncResult object which monitors the asyncOperation.
            /// </param>
            /// <remarks>
            /// All exceptions are caught and reported via a
            /// PipelineStateChanged event.
            /// </remarks>
            internal void RunspaceAvailableCallback(IAsyncResult asyncResult)
            {
#pragma warning disable 56500
                try
                {
                    RunspacePool pool = _shell._rsConnection as RunspacePool;
                    Dbg.Assert(pool != null, "RunspaceConnection must be a runspace pool");

                    // get the runspace..this will throw if there is an exception
                    // occurred while getting the runspace.
                    Runspace pooledRunspace = pool.EndGetRunspace(asyncResult);

                    bool isPipelineCreated = ConstructPipelineAndDoWork(pooledRunspace, false);
                    if (!isPipelineCreated)
                    {
                        pool.ReleaseRunspace(pooledRunspace);
                    }
                }
                catch (Exception e)
                {
                    // PipelineStateChangedEvent is not raised
                    // if there is an exception calling BeginInvoke
                    // So raise the event here and notify the caller.
                    lock (_syncObject)
                    {
                        if (_isNotActive)
                            return;
                        _isNotActive = true;
                    }

                    _shell.PipelineStateChanged(this,
                            new PipelineStateEventArgs(
                                new PipelineStateInfo(PipelineState.Failed,
                                    e)));
                }
#pragma warning restore 56500
            }

            /// <summary>
            /// Constructs a pipeline from the supplied runspace and invokes
            /// pipeline either synchronously or asynchronously identified by
            /// <paramref name="performSyncInvoke"/>.
            /// </summary>
            /// <param name="rs">
            /// Runspace to create pipeline. Cannot be null.
            /// </param>
            /// <param name="performSyncInvoke">
            /// if true, Invoke() is called
            /// BeginInvoke() otherwise.
            /// </param>
            /// <exception cref="InvalidOperationException">
            /// 1.BeginInvoke is called on nested powershell. Nested
            /// Powershell cannot be executed Asynchronously.
            /// </exception>
            /// <returns>
            /// true if the pipeline is created/invoked successfully.
            /// false otherwise.
            /// </returns>
            internal bool ConstructPipelineAndDoWork(Runspace rs, bool performSyncInvoke)
            {
                Dbg.Assert(rs != null, "Runspace cannot be null in ConstructPipelineAndDoWork");
                _shell.RunspaceAssigned.SafeInvoke(this, new PSEventArgs<Runspace>(rs));

                // lock is needed until a pipeline is created to
                // make stop() cleanly release resources.
                LocalRunspace lrs = rs as LocalRunspace;

                lock (_syncObject)
                {
                    if (_isNotActive)
                    {
                        return false;
                    }

                    if (lrs != null)
                    {
                        LocalPipeline localPipeline = new LocalPipeline(
                            lrs,
                            _shell.Commands.Commands,
                            (_settings != null && _settings.AddToHistory),
                            _shell.IsNested,
                            _inputStream,
                            _outputStream,
                            _errorStream,
                            _shell.InformationalBuffers);

                        localPipeline.IsChild = _shell.IsChild;

                        if (!string.IsNullOrEmpty(_shell.HistoryString))
                        {
                            localPipeline.SetHistoryString(_shell.HistoryString);
                        }

                        localPipeline.RedirectShellErrorOutputPipe = _shell.RedirectShellErrorOutputPipe;

                        CurrentlyRunningPipeline = localPipeline;
                        // register for pipeline state changed events within a lock...so that if
                        // stop is called before invoke, we can listen to state transition and
                        // take appropriate action.
                        CurrentlyRunningPipeline.StateChanged += _shell.PipelineStateChanged;
                    }
                    else
                    {
                        throw PSTraceSource.NewNotImplementedException();
                    }
                }

                // Set pipeline specific settings
                CurrentlyRunningPipeline.InvocationSettings = _settings;

                Dbg.Assert(lrs != null, "LocalRunspace cannot be null here");

                if (performSyncInvoke)
                {
                    CurrentlyRunningPipeline.Invoke();
                }
                else
                {
                    CurrentlyRunningPipeline.InvokeAsync();
                }

                return true;
            }

            /// <summary>
            /// Stops the async operation.
            /// </summary>
            /// <param name="isSyncCall"></param>
            internal void Stop(bool isSyncCall)
            {
                lock (_syncObject)
                {
                    if (_isNotActive)
                    {
                        return;
                    }

                    _isNotActive = true;
                    if (CurrentlyRunningPipeline != null)
                    {
                        if (isSyncCall)
                        {
                            CurrentlyRunningPipeline.Stop();
                        }
                        else
                        {
                            CurrentlyRunningPipeline.StopAsync();
                        }

                        return;
                    }

                    if (GetRunspaceAsyncResult != null)
                    {
                        RunspacePool pool = _shell._rsConnection as RunspacePool;
                        Dbg.Assert(pool != null, "RunspaceConnection must be a runspace pool");
                        pool.CancelGetRunspace(GetRunspaceAsyncResult);
                    }
                }

                // Pipeline is not yet associated with PowerShell..so emulate stop
                // locally
                Queue<PSInvocationStateInfo> events = new Queue<PSInvocationStateInfo>();
                events.Enqueue(new PSInvocationStateInfo(PSInvocationState.Stopped, null));

                if (isSyncCall)
                {
                    _shell.StopHelper(events);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(_shell.StopThreadProc), events);
                }
            }

            /// <summary>
            /// Internal clear is called when the invoke operation
            /// is completed or failed or stopped.
            /// </summary>
            internal void InternalClearSuppressExceptions()
            {
                try
                {
                    if ((_settings != null) && (_settings.WindowsIdentityToImpersonate != null))
                    {
                        _settings.WindowsIdentityToImpersonate.Dispose();
                        _settings.WindowsIdentityToImpersonate = null;
                    }

                    _inputStream.Close();
                    _outputStream.Close();
                    _errorStream.Close();

                    if (CurrentlyRunningPipeline == null)
                    {
                        return;
                    }

                    // Detach state changed handler so that runspace.close
                    // and pipeline.dispose will not change powershell instances state
                    CurrentlyRunningPipeline.StateChanged -= _shell.PipelineStateChanged;

                    if ((GetRunspaceAsyncResult == null) && (_shell._rsConnection == null))
                    {
                        // user did not supply a runspace..Invoke* method created
                        // a new runspace..so close it.
                        CurrentlyRunningPipeline.Runspace.Close();
                    }
                    else
                    {
                        RunspacePool pool = _shell._rsConnection as RunspacePool;
                        pool?.ReleaseRunspace(CurrentlyRunningPipeline.Runspace);
                    }

                    CurrentlyRunningPipeline.Dispose();
                }
                catch (ArgumentException)
                {
                }
                catch (InvalidOperationException)
                {
                }
                catch (InvalidRunspaceStateException)
                {
                }
                catch (InvalidRunspacePoolStateException)
                {
                }

                CurrentlyRunningPipeline = null;
            }

#if !CORECLR // PSMI Not Supported On CSS
            internal void GetSettings(out bool addToHistory, out bool noInput, out uint apartmentState)
            {
                addToHistory = _settings.AddToHistory;
                noInput = false;
                apartmentState = (uint)_settings.ApartmentState;
            }
#endif
        }

        #endregion

        #region Serialization / deserialization for remoting

        /// <summary>
        /// Creates a PowerShell object from a PSObject property bag.
        /// PSObject has to be in the format returned by ToPSObjectForRemoting method.
        /// </summary>
        /// <param name="powerShellAsPSObject">PSObject to rehydrate.</param>
        /// <returns>
        /// PowerShell rehydrated from a PSObject property bag
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the PSObject is null.
        /// </exception>
        /// <exception cref="System.Management.Automation.Remoting.PSRemotingDataStructureException">
        /// Thrown when the PSObject is not in the expected format
        /// </exception>
        internal static PowerShell FromPSObjectForRemoting(PSObject powerShellAsPSObject)
        {
            if (powerShellAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(powerShellAsPSObject));
            }

            Collection<PSCommand> extraCommands = null;
            ReadOnlyPSMemberInfoCollection<PSPropertyInfo> properties = powerShellAsPSObject.Properties.Match(RemoteDataNameStrings.ExtraCommands);

            if (properties.Count > 0)
            {
                extraCommands = new Collection<PSCommand>();

                foreach (PSObject extraCommandsAsPSObject in RemotingDecoder.EnumerateListProperty<PSObject>(powerShellAsPSObject, RemoteDataNameStrings.ExtraCommands))
                {
                    PSCommand cmd = null;
                    foreach (PSObject extraCommand in RemotingDecoder.EnumerateListProperty<PSObject>(extraCommandsAsPSObject, RemoteDataNameStrings.Commands))
                    {
                        System.Management.Automation.Runspaces.Command command =
                            System.Management.Automation.Runspaces.Command.FromPSObjectForRemoting(extraCommand);

                        if (cmd == null)
                        {
                            cmd = new PSCommand(command);
                        }
                        else
                        {
                            cmd.AddCommand(command);
                        }
                    }

                    extraCommands.Add(cmd);
                }
            }

            PSCommand psCommand = null;
            foreach (PSObject commandAsPSObject in RemotingDecoder.EnumerateListProperty<PSObject>(powerShellAsPSObject, RemoteDataNameStrings.Commands))
            {
                System.Management.Automation.Runspaces.Command command =
                    System.Management.Automation.Runspaces.Command.FromPSObjectForRemoting(commandAsPSObject);

                if (psCommand == null)
                {
                    psCommand = new PSCommand(command);
                }
                else
                {
                    psCommand.AddCommand(command);
                }
            }

            bool isNested = RemotingDecoder.GetPropertyValue<bool>(powerShellAsPSObject, RemoteDataNameStrings.IsNested);
            PowerShell shell = PowerShell.Create(isNested, psCommand, extraCommands);
            shell.HistoryString = RemotingDecoder.GetPropertyValue<string>(powerShellAsPSObject, RemoteDataNameStrings.HistoryString);
            shell.RedirectShellErrorOutputPipe = RemotingDecoder.GetPropertyValue<bool>(powerShellAsPSObject, RemoteDataNameStrings.RedirectShellErrorOutputPipe);
            return shell;
        }

        /// <summary>
        /// Returns this object as a PSObject property bag
        /// that can be used in a remoting protocol data object.
        /// </summary>
        /// <returns>This object as a PSObject property bag.</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject powerShellAsPSObject = RemotingEncoder.CreateEmptyPSObject();
            Version psRPVersion = RemotingEncoder.GetPSRemotingProtocolVersion(_rsConnection as RunspacePool);

            // Check if the server supports batch invocation
            if (ServerSupportsBatchInvocation())
            {
                if (ExtraCommands.Count > 0)
                {
                    List<PSObject> extraCommandsAsListOfPSObjects = new List<PSObject>(ExtraCommands.Count);
                    foreach (PSCommand extraCommand in ExtraCommands)
                    {
                        PSObject obj = RemotingEncoder.CreateEmptyPSObject();

                        obj.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.Commands, CommandsAsListOfPSObjects(extraCommand.Commands, psRPVersion)));

                        extraCommandsAsListOfPSObjects.Add(obj);
                    }

                    powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ExtraCommands, extraCommandsAsListOfPSObjects));
                }
            }

            List<PSObject> commandsAsListOfPSObjects = CommandsAsListOfPSObjects(Commands.Commands, psRPVersion);

            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.Commands, commandsAsListOfPSObjects));
            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.IsNested, this.IsNested));
            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.HistoryString, HistoryString));
            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.RedirectShellErrorOutputPipe, this.RedirectShellErrorOutputPipe));
            return powerShellAsPSObject;
        }

        private static List<PSObject> CommandsAsListOfPSObjects(CommandCollection commands, Version psRPVersion)
        {
            List<PSObject> commandsAsListOfPSObjects = new List<PSObject>(commands.Count);
            foreach (Command command in commands)
            {
                commandsAsListOfPSObjects.Add(command.ToPSObjectForRemoting(psRPVersion));
            }

            return commandsAsListOfPSObjects;
        }

        #endregion

        #region Remote data drain/block methods

        /// <summary>
        /// Suspends data arriving from remote session.
        /// </summary>
        internal void SuspendIncomingData()
        {
            if (RemotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            RemotePowerShell.DataStructureHandler?.TransportManager.SuspendQueue(true);
        }

        /// <summary>
        /// Resumes data arriving from remote session.
        /// </summary>
        internal void ResumeIncomingData()
        {
            if (RemotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            RemotePowerShell.DataStructureHandler?.TransportManager.ResumeQueue();
        }

        /// <summary>
        /// Blocking call that waits until the *current remote* data
        /// queue at the transport manager is empty.  This affects only
        /// the current queue until it is empty.
        /// </summary>
        internal void WaitForServicingComplete()
        {
            if (RemotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            if (RemotePowerShell.DataStructureHandler != null)
            {
                int count = 0;
                while (++count < 2 &&
                       RemotePowerShell.DataStructureHandler.TransportManager.IsServicing)
                {
                    // Try waiting for 50 ms, then continue.
                    Threading.Thread.Sleep(50);
                }
            }
        }

        #endregion

#if !CORECLR // PSMI Not Supported On CSS
        #region Win Blue Extensions

        internal CimInstance AsPSPowerShellPipeline()
        {
            CimInstance c = InternalMISerializer.CreateCimInstance("PS_PowerShellPipeline");
            CimProperty instanceIdProperty = InternalMISerializer.CreateCimProperty("InstanceId",
                                                                                    this.InstanceId.ToString(),
                                                                                    Microsoft.Management.Infrastructure.CimType.String);
            c.CimInstanceProperties.Add(instanceIdProperty);
            CimProperty isNestedProperty = InternalMISerializer.CreateCimProperty("IsNested",
                                                                                  this.IsNested,
                                                                                  Microsoft.Management.Infrastructure.CimType.Boolean);
            c.CimInstanceProperties.Add(isNestedProperty);

            bool addToHistoryValue = false, noInputValue = false;
            uint apartmentStateValue = 0;
            if (_worker != null)
            {
                _worker.GetSettings(out addToHistoryValue, out noInputValue, out apartmentStateValue);
            }

            CimProperty addToHistoryProperty = InternalMISerializer.CreateCimProperty("AddToHistory",
                                                                                      addToHistoryValue,
                                                                                      Microsoft.Management.Infrastructure.CimType.Boolean);
            c.CimInstanceProperties.Add(addToHistoryProperty);
            CimProperty noInputProperty = InternalMISerializer.CreateCimProperty("NoInput",
                                                                                 noInputValue,
                                                                                 Microsoft.Management.Infrastructure.CimType.Boolean);
            c.CimInstanceProperties.Add(noInputProperty);
            CimProperty apartmentStateProperty = InternalMISerializer.CreateCimProperty("ApartmentState",
                                                                                        apartmentStateValue,
                                                                                        Microsoft.Management.Infrastructure.CimType.UInt32);
            c.CimInstanceProperties.Add(apartmentStateProperty);

            if (this.Commands.Commands.Count > 0)
            {
                List<CimInstance> commandInstances = new List<CimInstance>();
                foreach (var command in this.Commands.Commands)
                {
                    commandInstances.Add(command.ToCimInstance());
                }

                CimProperty commandsProperty = InternalMISerializer.CreateCimProperty("Commands",
                                                                                      commandInstances.ToArray(),
                                                                                      Microsoft.Management.Infrastructure.CimType.ReferenceArray);
                c.CimInstanceProperties.Add(commandsProperty);
            }

            return c;
        }
        #endregion Win Blue Extensions
#endif
    }

    /// <summary>
    /// Streams generated by PowerShell invocations.
    /// </summary>
    public sealed class PSDataStreams
    {
        /// <summary>
        /// PSDataStreams is the public interface to access the *Buffer properties in the PowerShell class.
        /// </summary>
        internal PSDataStreams(PowerShell powershell)
        {
            _powershell = powershell;
        }

        /// <summary>
        /// Gets or sets the error buffer. Powershell invocation writes
        /// the error data into this buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<ErrorRecord> Error
        {
            get
            {
                return _powershell.ErrorBuffer;
            }

            set
            {
                _powershell.ErrorBuffer = value;
            }
        }

        /// <summary>
        /// Gets or sets the progress buffer. Powershell invocation writes
        /// the progress data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<ProgressRecord> Progress
        {
            get
            {
                return _powershell.ProgressBuffer;
            }

            set
            {
                _powershell.ProgressBuffer = value;
            }
        }

        /// <summary>
        /// Gets or sets the verbose buffer. Powershell invocation writes
        /// the verbose data into this buffer.  Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<VerboseRecord> Verbose
        {
            get
            {
                return _powershell.VerboseBuffer;
            }

            set
            {
                _powershell.VerboseBuffer = value;
            }
        }

        /// <summary>
        /// Gets or sets the debug buffer. Powershell invocation writes
        /// the debug data into this buffer.  Can be null.
        /// </summary>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<DebugRecord> Debug
        {
            get
            {
                return _powershell.DebugBuffer;
            }

            set
            {
                _powershell.DebugBuffer = value;
            }
        }

        /// <summary>
        /// Gets or sets the warning buffer. Powershell invocation writes
        /// the warning data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<WarningRecord> Warning
        {
            get
            {
                return _powershell.WarningBuffer;
            }

            set
            {
                _powershell.WarningBuffer = value;
            }
        }

        /// <summary>
        /// Gets or sets the information buffer. Powershell invocation writes
        /// the warning data into this buffer. Can be null.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Cannot set to a null value.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its
        /// current state.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Object is disposed.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "We want to allow callers to change the backing store.")]
        public PSDataCollection<InformationRecord> Information
        {
            get
            {
                return _powershell.InformationBuffer;
            }

            set
            {
                _powershell.InformationBuffer = value;
            }
        }

        /// <summary>
        /// Removes all items from all the data streams.
        /// </summary>
        public void ClearStreams()
        {
            this.Error.Clear();
            this.Progress.Clear();
            this.Verbose.Clear();
            this.Information.Clear();
            this.Debug.Clear();
            this.Warning.Clear();
        }

        private readonly PowerShell _powershell;
    }

    /// <summary>
    /// Helper class for making sure Ctrl-C stops an active powershell invocation.
    /// </summary>
    /// <example>
    ///     powerShell = PowerShell.Create();
    ///     powerShell.AddCommand("Start-Sleep");
    ///     powerShell.AddParameter("Seconds", 10);
    ///     powerShell.Runspace = remoteRunspace;
    ///     Collection&lt;PSObject&gt; result;
    ///     using (new PowerShellStopper(context, powerShell))
    ///     {
    ///         result = powerShell.Invoke();
    ///     }
    /// </example>
    internal class PowerShellStopper : IDisposable
    {
        private readonly PipelineBase _pipeline;
        private readonly PowerShell _powerShell;
        private EventHandler<PipelineStateEventArgs> _eventHandler;

        internal PowerShellStopper(ExecutionContext context, PowerShell powerShell)
        {
            ArgumentNullException.ThrowIfNull(context);

            ArgumentNullException.ThrowIfNull(powerShell);

            _powerShell = powerShell;

            if ((context.CurrentCommandProcessor != null) &&
                (context.CurrentCommandProcessor.CommandRuntime != null) &&
                (context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor != null) &&
                (context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor.LocalPipeline != null))
            {
                _eventHandler = new EventHandler<PipelineStateEventArgs>(LocalPipeline_StateChanged);
                _pipeline = context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor.LocalPipeline;
                _pipeline.StateChanged += _eventHandler;
            }
        }

        private void LocalPipeline_StateChanged(object sender, PipelineStateEventArgs e)
        {
            if ((e.PipelineStateInfo.State == PipelineState.Stopping) &&
                (_powerShell.InvocationStateInfo.State == PSInvocationState.Running))
            {
                _powerShell.Stop();
            }
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_eventHandler != null)
                {
                    _pipeline.StateChanged -= _eventHandler;
                    _eventHandler = null;
                }

                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
    }
}
