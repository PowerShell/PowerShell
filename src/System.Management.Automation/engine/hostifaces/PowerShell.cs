/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Diagnostics.CodeAnalysis; // for fxcop.
using Dbg = System.Management.Automation.Diagnostics;
using System.Diagnostics;
using Microsoft.Management.Infrastructure;

#if CORECLR
// Use stub for SerializableAttribute, NoSerializedAttribute, SystemException, ThreadAbortException and ISerializable related types.
using Microsoft.PowerShell.CoreClr.Stubs;
#endif

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    #region Exceptions

    /// <summary>
    /// Defines exception which is thrown when state of the PowerShell is different 
    /// from the expected state.
    /// </summary>
    [Serializable]
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
            :base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPowerShellStateException and defines value of 
        /// CurrentState.
        /// </summary>
        /// <param name="currentState">Current state of powershell</param>
        internal InvalidPowerShellStateException(PSInvocationState currentState)
        : base
        (StringUtil.Format(PowerShellStrings.InvalidPowerShellStateGeneral))
        {
            currState = currentState;
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
        protected
        InvalidPowerShellStateException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
        {
        }

        #endregion

        /// <summary>
        /// Gets CurrentState of the powershell
        /// </summary>
        public PSInvocationState CurrentState
        {
            get
            {
                return currState;
            }
        }

        /// <summary>
        /// State of powershell when exception was thrown.
        /// </summary>
        [NonSerialized]
        private PSInvocationState currState = 0;
    }

    #endregion

    #region PSInvocationState, PSInvocationStateInfo, PSInvocationStateChangedEventArgs

    /// <summary>
    /// Enumerated type defining the state of the PowerShell
    /// </summary>
    public enum PSInvocationState
    {
        /// <summary>
        /// PowerShell has not been started
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// PowerShell is executing
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
    /// Enumerated type defining runspace modes for nested pipeline
    /// </summary>
    public enum RunspaceMode
    {
        /// <summary>
        /// Use current runspace from the current thread of execution
        /// </summary>
        CurrentRunspace = 0,

        /// <summary>
        /// Create new runspace
        /// </summary>
        NewRunspace = 1
    }

    /// <summary>
    /// Type which has information about InvocationState and Exception 
    /// associated with InvocationState
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
        internal PSInvocationStateInfo (PSInvocationState state, Exception reason)
        {
            executionState = state;
            exceptionReason = reason;
        }

        /// <summary>
        /// Construct from PipelineStateInfo
        /// </summary>
        /// <param name="pipelineStateInfo"></param>
        internal PSInvocationStateInfo(PipelineStateInfo pipelineStateInfo)
        {
            executionState = (PSInvocationState)((int)pipelineStateInfo.State);
            exceptionReason = pipelineStateInfo.Reason;
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
                return executionState;
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
                return exceptionReason;
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
                this.executionState,
                this.exceptionReason
                );
        }

        #region Private data

        /// <summary>
        /// The current execution state
        /// </summary>
        private PSInvocationState executionState;

        /// <summary>
        /// Non-null exception if the execution state change was due to an error.
        /// </summary>
        private Exception exceptionReason;

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
        /// Constructs PSInvocationStateChangedEventArgs from PSInvocationStateInfo
        /// </summary>
        /// <param name="psStateInfo">
        /// state to raise the event with.
        /// </param>
        internal PSInvocationStateChangedEventArgs(PSInvocationStateInfo psStateInfo)
        {
            Dbg.Assert(psStateInfo != null, "caller should validate the parameter");
            executionStateInfo = psStateInfo;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Information about current state of a PowerShell Instance.
        /// </summary>
        public PSInvocationStateInfo InvocationStateInfo
        {
            get
            {
                return executionStateInfo;
            }
        }

        #endregion

        /// <summary>
        /// Information about current state of a PowerShell Instance.
        /// </summary>
        private PSInvocationStateInfo executionStateInfo;
    }

    #endregion

    /// <summary>
    /// Settings to control command invocation.
    /// </summary>
    public sealed class PSInvocationSettings
    {
        #region Private Fields

        private PSHost host;
        private RemoteStreamOptions remoteStreamOptions;
        private ActionPreference? errorActionPreference;
        private bool addToHistory;

#if !CORECLR // No ApartmentState In CoreCLR
        private ApartmentState apartmentState;
#endif
        // the following are used to flow the identity to pipeline execution thread
        private bool flowImpersonationPolicy;
        private System.Security.Principal.WindowsIdentity windowsIdentityToImpersonate;

        // Invokes a remote command and immediately disconnects, if transport layer
        // supports this operation.
        private bool invokeAndDisconnect;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public PSInvocationSettings()
        {
#if !CORECLR // No ApartmentState In CoreCLR
            this.apartmentState = ApartmentState.Unknown;
#endif
            this.host = null;
            this.remoteStreamOptions = 0;
            this.addToHistory = false;
            this.errorActionPreference = null;
        }

        #endregion

#if !CORECLR // No ApartmentState In CoreCLR
        /// <summary>
        /// ApartmentState of the thread in which the command
        /// is executed.
        /// </summary>
        public ApartmentState ApartmentState 
        {
            get
            {
                return apartmentState;
            }

            set
            {
                apartmentState = value;
            }
        }
#endif

        /// <summary>
        /// Host to use with the Runspace when the command is
        /// executed.
        /// </summary>
        public PSHost Host 
        {
            get
            {
                return host;
            }
            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Host");
                }
                host = value;
            }
        }

        /// <summary>
        /// Options for the Error, Warning, Verbose and Debug streams during remote calls
        /// </summary>
        public RemoteStreamOptions RemoteStreamOptions
        {
            get
            {
                return this.remoteStreamOptions;
            }
            set
            {
                this.remoteStreamOptions = value;
            }
        }

        /// <summary>
        /// Boolean which tells if the command is added to the history of the 
        /// Runspace the command is executing in. By default this is false.
        /// </summary>
        public bool AddToHistory
        {
            get { return addToHistory; }
            set { addToHistory = value; }
        }

        /// <summary>
        /// Determines how errors should be handled during batch command execution
        /// </summary>
        public ActionPreference? ErrorActionPreference
        {
            get { return errorActionPreference; }
            set { errorActionPreference = value; }
        }
        
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
        public bool FlowImpersonationPolicy
        {
            get
            {
                return flowImpersonationPolicy;
            }
            set
            {
                flowImpersonationPolicy = value;
            }
        }

        internal System.Security.Principal.WindowsIdentity WindowsIdentityToImpersonate
        {
            get { return windowsIdentityToImpersonate; }
            set { windowsIdentityToImpersonate = value; }
        }

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
        internal bool InvokeAndDisconnect
        {
            get { return this.invokeAndDisconnect; }
            set { this.invokeAndDisconnect = value; }
        }
    }

    /// <summary>
    /// Batch execution context
    /// </summary>
    internal class BatchInvocationContext
    {
        private PSCommand command;
        private PSDataCollection<PSObject> output;
        private AutoResetEvent completionEvent;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        internal BatchInvocationContext(PSCommand command, PSDataCollection<PSObject> output)
        {
            this.command = command;
            this.output = output;
            completionEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Invocation output
        /// </summary>
        internal PSDataCollection<PSObject> Output
        {
            get { return output; }
        }

        /// <summary>
        /// Command to invoke
        /// </summary>
        internal PSCommand Command
        {
            get { return command; }
        }

        /// <summary>
        /// Waits for the completion event
        /// </summary>
        internal void Wait()
        {
            completionEvent.WaitOne();
        }

        /// <summary>
        /// Signals the completion event
        /// </summary>
        internal void Signal()
        {
            completionEvent.Set();
        }
    }

    /// <summary>
    /// These flags control whether InvocationInfo is added to items in the Error, Warning, Verbose and Debug 
    /// streams during remote calls
    /// </summary>
    [Flags] public enum RemoteStreamOptions
    {
        /// <summary>
        /// If this flag is set, ErrorRecord will include an instance of InvocationInfo on remote calls
        /// </summary>
        AddInvocationInfoToErrorRecord = 0x01,

        /// <summary>
        /// If this flag is set, WarningRecord will include an instance of InvocationInfo on remote calls
        /// </summary>
        AddInvocationInfoToWarningRecord = 0x02,

        /// <summary>
        /// If this flag is set, DebugRecord will include an instance of InvocationInfo on remote calls
        /// </summary>
        AddInvocationInfoToDebugRecord = 0x04,

        /// <summary>
        /// If this flag is set, VerboseRecord will include an instance of InvocationInfo on remote calls
        /// </summary>
        AddInvocationInfoToVerboseRecord = 0x08,

        /// <summary>
        /// If this flag is set, ErrorRecord, WarningRecord, DebugRecord, and VerboseRecord will include an instance of InvocationInfo on remote calls
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
        private bool isAssociatedWithAsyncInvoke;

        /// <summary>
        /// true if AsyncResult monitors Async BeginInvoke().
        /// false otherwise
        /// </summary>
        internal bool IsAssociatedWithAsyncInvoke
        {
            get { return isAssociatedWithAsyncInvoke; }
        }

        /// <summary>
        /// The output buffer for the asynchronous invoke
        /// </summary>
        internal PSDataCollection<PSObject> Output
        {
            get { return output; }
        }

        private PSDataCollection<PSObject> output;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownerId">
        /// Instace Id of the Powershell object creating this instance
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
            this.isAssociatedWithAsyncInvoke = isCalledFromBeginInvoke;
            this.output = output;
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
    ///    Powershell.Create("get-process").Invoke();
    /// </code>
    /// The above statetement creates a local runspace using default
    /// configuration, executes the command and then closes the runspace.
    /// 
    /// Using RunspacePool property, the caller can provide the runspace
    /// where the command / script is executed. 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "PowerShell is a valid type in SMAR namespace.")]
    public sealed class PowerShell : IDisposable
    {
        #region Private Fields

        private bool isGetCommandMetadataSpecialPipeline;
        private PSCommand psCommand;
        private Collection<PSCommand> extraCommands;
        private bool runningExtraCommands;
        // worker object which does the invoke
        private Worker worker;
        private PSInvocationStateInfo invocationStateInfo;
        private PowerShellAsyncResult invokeAsyncResult;
        private PowerShellAsyncResult stopAsyncResult;
        private PowerShellAsyncResult batchAsyncResult;
        private PSInvocationSettings batchInvocationSettings;
        private PSCommand backupPSCommand;
        private bool isNested;
        private bool isChild = false;
        private object rsConnection;

        private PSDataCollection<PSObject> outputBuffer;
        private bool outputBufferOwner = true;
        private PSDataCollection<ErrorRecord> errorBuffer;
        private bool errorBufferOwner = true;
        private PSInformationalBuffers informationalBuffers;
        private PSDataStreams dataStreams;

        private bool isDisposed;
        private Guid instanceId;
        private object syncObject = new object();

        private ClientRemotePowerShell remotePowerShell;
                    // client remote powershell if the powershell
                    // is executed with a remote runspace pool
        private String historyString;

        private ConnectCommandInfo connectCmdInfo;
        private bool commandInvokedSynchronously = false;
        private bool isBatching = false;
        private bool stopBatchExecution = false;

        #endregion

        #region Internal Constructors

        /// <summary>
        /// Constructs PowerShell
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
            this.extraCommands = (extraCommands == null) ? new Collection<PSCommand>() : extraCommands;
            this.runningExtraCommands = false;
            psCommand = command;
            psCommand.Owner = this;
            RemoteRunspace remoteRunspace = rsConnection as RemoteRunspace;
            this.rsConnection = remoteRunspace != null ? remoteRunspace.RunspacePool : rsConnection;
            instanceId = Guid.NewGuid();
            invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.NotStarted, null);
            outputBuffer = null;
            outputBufferOwner = true;
            errorBuffer = new PSDataCollection<ErrorRecord>();
            errorBufferOwner = true;
            informationalBuffers = new PSInformationalBuffers(instanceId);
            dataStreams = new PSDataStreams(this);
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
            this.extraCommands = new Collection<PSCommand>();
            this.runningExtraCommands = false;
            AddCommand(connectCmdInfo.Command);
            this.connectCmdInfo = connectCmdInfo;

            // The command ID is passed to the PSRP layer through the PowerShell instanceID.
            this.instanceId = this.connectCmdInfo.CommandId;

            this.invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Disconnected, null);

            if (rsConnection is RemoteRunspace)
            {
                this.runspace = rsConnection as Runspace;
                this.runspacePool = ((RemoteRunspace)rsConnection).RunspacePool;
            }
            else if (rsConnection is RunspacePool)
            {
                this.runspacePool = (RunspacePool)rsConnection;
            }
            Dbg.Assert(this.runspacePool != null, "Invalid rsConnection parameter>");
            this.remotePowerShell = new ClientRemotePowerShell(this, this.runspacePool.RemoteRunspacePoolInternal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputstream"></param>
        /// <param name="outputstream"></param>
        /// <param name="errorstream"></param>
        /// <param name="runspacePool"></param>
        internal PowerShell(ObjectStreamBase inputstream,
            ObjectStreamBase outputstream, ObjectStreamBase errorstream, RunspacePool runspacePool)
        {
            this.extraCommands = new Collection<PSCommand>();
            this.runningExtraCommands = false;
            rsConnection = runspacePool;
            instanceId = Guid.NewGuid();
            invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.NotStarted, null);
            informationalBuffers = new PSInformationalBuffers(instanceId);
            dataStreams = new PSDataStreams(this);
           
            PSDataCollectionStream<PSObject> outputdatastream = (PSDataCollectionStream<PSObject>)outputstream;
            outputBuffer = outputdatastream.ObjectStore;

            PSDataCollectionStream<ErrorRecord> errordatastream = (PSDataCollectionStream<ErrorRecord>)errorstream;
            errorBuffer = errordatastream.ObjectStore;

            if (runspacePool != null && runspacePool.RemoteRunspacePoolInternal != null)
            {
                remotePowerShell = new ClientRemotePowerShell(this, runspacePool.RemoteRunspacePoolInternal);
            }
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
            this.extraCommands = new Collection<PSCommand>();
            this.runningExtraCommands = false;
            this.psCommand = new PSCommand();
            psCommand.Owner = this;
            this.runspacePool = runspacePool;

            AddCommand(connectCmdInfo.Command);
            this.connectCmdInfo = connectCmdInfo;

            // The command ID is passed to the PSRP layer through the PowerShell instanceID.
            this.instanceId = this.connectCmdInfo.CommandId;

            this.invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Disconnected, null);

            this.remotePowerShell = new ClientRemotePowerShell(this, runspacePool.RemoteRunspacePoolInternal);
        }

        /// <summary>
        /// Sets the command collection in this powershell
        /// </summary>
        /// <remarks>This method will be called by RemotePipeline
        /// before it begins execution. This method is used to set
        /// the command collection of the remote pipeline as the 
        /// command collection of the underlying powershell</remarks>
        internal void InitForRemotePipeline(CommandCollection command, ObjectStreamBase inputstream,
            ObjectStreamBase outputstream, ObjectStreamBase errorstream, PSInvocationSettings settings, bool redirectShellErrorOutputPipe)
        {
            Dbg.Assert(command != null, "A command collection need to be specified");

            psCommand = new PSCommand(command[0]);
            psCommand.Owner = this;

            for (int i = 1; i < command.Count; i++)
            {
                AddCommand(command[i]);
            }

            this.redirectShellErrorOutputPipe = redirectShellErrorOutputPipe;

            // create the client remote powershell for remoting
            // communications
            if (remotePowerShell == null)
            {
                remotePowerShell = new ClientRemotePowerShell(this, ((RunspacePool)rsConnection).RemoteRunspacePoolInternal);
            }

            // If we get here, we don't call 'Invoke' or any of it's friends on 'this', instead we serialize 'this' in PowerShell.ToPSObjectForRemoting.
            // Without the following two steps, we'll be missing the 'ExtraCommands' on the serialized instance of 'this'.
            // This is the last possible chance to call set up for batching as we will indirectly call ToPSObjectForRemoting
            // in the call to ClientRemotePowerShell.Initialize (which happens just below.)
            DetermineIsBatching();

            if (isBatching)
            {
                SetupAsyncBatchExecution();
            }

            remotePowerShell.Initialize(inputstream, outputstream,
                errorstream, this.informationalBuffers, settings);
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

            if (this.invocationStateInfo.State != PSInvocationState.Disconnected)
            {
                throw new InvalidPowerShellStateException(this.invocationStateInfo.State);
            }

            this.redirectShellErrorOutputPipe = redirectShellErrorOutputPipe;

            if (this.remotePowerShell == null)
            {
                this.remotePowerShell = new ClientRemotePowerShell(this, ((RunspacePool)rsConnection).RemoteRunspacePoolInternal);
            }

            if (!this.remotePowerShell.Initialized)
            {
                this.remotePowerShell.Initialize(inputstream, outputstream, errorstream, this.informationalBuffers, settings);
            }
        }

        #endregion

        #region Construction Factory

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance
        /// </summary>
        /// <returns>
        /// An instance of PowerShell.
        /// </returns>
        public static PowerShell Create()
        {
            return new PowerShell(new PSCommand(), null, null);
        }

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance
        /// </summary>
        /// <param name="runspace">runspace mode</param>
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
                    result.isChild = true;
                    result.isNested = true;
                    result.IsRunspaceOwner = false;
                    result.runspace = Runspace.DefaultRunspace;
                    break;
                case RunspaceMode.NewRunspace:
                    result = new PowerShell(new PSCommand(), null, null);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Constructs an empty PowerShell instance; a script or command must be added before invoking this instance
        /// </summary>
        /// <param name="initialSessionState">InitialSessionState with which to create the runspace</param>
        /// <returns>An instance of PowerShell.</returns>
        public static PowerShell Create(InitialSessionState initialSessionState)
        {
            PowerShell result = Create();
            
            result.Runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            result.Runspace.Open();
            
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
            if ((null != this.worker) && (null != this.worker.CurrentlyRunningPipeline))
            {
                PowerShell result = new PowerShell(new PSCommand(), 
                    null, this.worker.CurrentlyRunningPipeline.Runspace);
                result.isNested = true;
                return result;
            }

            throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.InvalidStateCreateNested);
        }

        /// <summary>
        /// Method needed when deserializing PowerShell object coming from a RemoteDataObject
        /// </summary>
        /// <param name="isNested">Indicates if PowerShell object is nested</param>
        /// <param name="psCommand">Commands that the PowerShell pipeline is built of</param>
        /// <param name="extraCommands">Extra commands to run</param>
        private static PowerShell Create(bool isNested, PSCommand psCommand, Collection<PSCommand> extraCommands)
        {
            PowerShell powerShell = new PowerShell(psCommand, extraCommands, null);
            powerShell.isNested = isNested;
            return powerShell;
        }

        #endregion

        #region Command / Parameter Construction

        /// <summary>
        /// Add a cmdlet to construct a command pipeline.
        /// For example, to construct a command string "get-process | sort-object",
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").AddCommand("sort-object");
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
            lock (syncObject)
            {
                AssertChangesAreAccepted();

                psCommand.AddCommand(cmdlet);

                return this;
            }
        }

        /// <summary>
        /// Add a cmdlet to construct a command pipeline.
        /// For example, to construct a command string "get-process | sort-object",
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").AddCommand("sort-object");
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
            lock (syncObject)
            {
                AssertChangesAreAccepted();

                psCommand.AddCommand(cmdlet, useLocalScope);

                return this;
            }
        }

        /// <summary>
        /// Add a piece of script to construct a command pipeline.
        /// For example, to construct a command string "get-process | foreach { $_.Name }"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").
        ///                                     AddCommand("foreach { $_.Name }", true);
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
            lock (syncObject)
            {
                AssertChangesAreAccepted();

                psCommand.AddScript(script);

                return this;
            }
        }

        /// <summary>
        /// Add a piece of script to construct a command pipeline.
        /// For example, to construct a command string "get-process | foreach { $_.Name }"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").
        ///                                     AddCommand("foreach { $_.Name }", true);
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
            lock (syncObject)
            {
                AssertChangesAreAccepted();

                psCommand.AddScript(script, useLocalScope);

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
            lock (syncObject)
            {
                AssertChangesAreAccepted();

                psCommand.AddCommand(command);

                return this;
            }
        }

        /// <summary>
        /// CommandInfo object for the command to add.
        /// </summary>
        /// <param name="commandInfo">The CommandInfo object for the command to add</param>
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
                throw PSTraceSource.NewArgumentNullException("commandInfo");
            }
            Command cmd = new Command(commandInfo);
            psCommand.AddCommand(cmd);
            return this;
        }


        /// <summary>
        /// Add a parameter to the last added command.
        /// For example, to construct a command string "get-process | select-object -property name"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").
        ///                                     AddCommand("select-object").AddParameter("property","name");
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
            lock (syncObject)
            {
                if (psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                psCommand.AddParameter(parameterName, value);
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
            lock (syncObject)
            {
                if (psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                psCommand.AddParameter(parameterName);
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
            lock (syncObject)
            {
                if (parameters == null)
                {
                    throw PSTraceSource.NewArgumentNullException("parameters");
                }

                if (psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();

                foreach (object p in parameters)
                {
                    psCommand.AddParameter(null, p);
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
            lock (syncObject)
            {
                if (parameters == null)
                {
                    throw PSTraceSource.NewArgumentNullException("parameters");
                }

                if (psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();

                foreach (DictionaryEntry entry in parameters)
                {
                    string parameterName = entry.Key as string;

                    if (parameterName == null)
                    {
                        throw PSTraceSource.NewArgumentException("parameters", PowerShellStrings.KeyMustBeString);
                    }

                    psCommand.AddParameter(parameterName, entry.Value);
                }

                return this;
            }
        }

        /// <summary>
        /// Adds an argument to the last added command.
        /// For example, to construct a command string "get-process | select-object name"
        ///     <code>
        ///         PowerShell shell = PowerShell.Create("get-process").
        ///                                     AddCommand("select-object").AddParameter("name");
        ///     </code>
        /// 
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
            lock (syncObject)
            {
                if (psCommand.Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.ParameterRequiresCommand);
                }

                AssertChangesAreAccepted();
                psCommand.AddArgument(value);
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
            lock (syncObject)
            {
                // for PowerShell.Create().AddStatement().AddCommand("Get-Process");
                // we reduce it to PowerShell.Create().AddCommand("Get-Process");
                if (psCommand.Commands.Count == 0)
                {
                    return this;
                }

                AssertChangesAreAccepted();

                psCommand.Commands[psCommand.Commands.Count - 1].IsEndOfStatement = true;

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
                return psCommand;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Command");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    psCommand = value.Clone();
                    psCommand.Owner = this;
                }
            }
        }

        /// <summary>
        /// Streams generated by PowerShell invocations
        /// </summary>
        public PSDataStreams Streams
        {
            get
            {
                return this.dataStreams;
            }
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
        internal PSDataCollection<ErrorRecord> ErrorBuffer
        {
            get
            {
                return errorBuffer;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Error");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    errorBuffer = value;
                    errorBufferOwner = false;
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
                return informationalBuffers.Progress;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Progress");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    informationalBuffers.Progress = value;
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
                return informationalBuffers.Verbose;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Verbose");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    informationalBuffers.Verbose = value;
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
                return informationalBuffers.Debug;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Debug");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    informationalBuffers.Debug = value;
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
                return informationalBuffers.Warning;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Warning");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    informationalBuffers.Warning = value;
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
                return informationalBuffers.Information;
            }

            set
            {
                if (null == value)
                {
                    throw PSTraceSource.NewArgumentNullException("Information");
                }
                lock (syncObject)
                {
                    AssertChangesAreAccepted();
                    informationalBuffers.Information = value;
                }
            }
        }

        /// <summary>
        /// Gets the informational buffers
        /// </summary>
        internal PSInformationalBuffers InformationalBuffers
        {
            get { return informationalBuffers; }
        }

        /// <summary>
        /// If this flag is true, the commands in this Pipeline will redirect 
        /// the global error output pipe to the command's error output pipe.
        /// 
        /// (see the comment in Pipeline.RedirectShellErrorOutputPipe for an 
        /// explanation of why this flag is needed)
        /// </summary>
        internal bool RedirectShellErrorOutputPipe
        {
            get { return this.redirectShellErrorOutputPipe; }
            set { this.redirectShellErrorOutputPipe = value; }
        }
        private bool redirectShellErrorOutputPipe = true;

        /// <summary>
        /// Get unqiue id for this instance of runspace pool. It is primarily used 
        /// for logging purposes.
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return instanceId;
            }
        }

        /// <summary>
        /// Gets the execution state of the current PowerShell instance.
        /// </summary>
        public PSInvocationStateInfo InvocationStateInfo
        {
            get
            {
                return invocationStateInfo;
            }
        }

        /// <summary>
        /// Gets the property which indicates if this PowerShell instance
        /// is nested.
        /// </summary>
        public bool IsNested
        {
            get
            {
                return isNested;
            }
        }

        /// <summary>
        /// Gets the property which indicates if this PowerShell instance
        /// is a child instance.
        /// 
        /// IsChild flag makes it possible for the pipeline to differentiate between
        /// a true v1 nested pipeline and the cmdlets calling cmdlets case. See bug
        /// 211462.
        /// 
        /// </summary>
        internal bool IsChild
        {
            get
            {
                return isChild;
            }
        }

        /// <summary>
        /// If an error occurred while executing the pipeline, this will be set to true.
        /// </summary>
        public bool HadErrors
        {
            get { return _hadErrors; }
        }
        internal void SetHadErrors(bool status)
        {
            _hadErrors = status;
        }
        bool _hadErrors;

        /// <summary>
        /// Access to the EndInvoke AysncResult object.  Used by remote
        /// debugging to invoke debugger commands on command thread.
        /// </summary>
        internal AsyncResult EndInvokeAsyncResult
        {
            get;
            private set;
        }

        /// <summary>
        /// Event rasied when PowerShell Execution State Changes.
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
                if (this.runspace == null && this.runspacePool == null) // create a runspace only if neither a runspace nor a runspace pool have been set
                {
                    lock (this.syncObject)
                    {
                        if (this.runspace == null && this.runspacePool == null)
                        {
                            AssertChangesAreAccepted();
                            SetRunspace(RunspaceFactory.CreateRunspace(), true);
                            this.Runspace.Open();
                        }
                    }
                }

                return runspace;
            }

            set
            {
                lock (this.syncObject)
                {
                    AssertChangesAreAccepted();

                    if (this.runspace != null && this.runspaceOwner)
                    {
                        this.runspace.Dispose();
                        this.runspace = null;
                        this.runspaceOwner = false;
                    }

                    SetRunspace(value, false);
                }
            }
        }        

        /// <summary>
        /// Internal method to set the Runspace property
        /// </summary>
        private void SetRunspace(Runspace runspace, bool owner)
        {
            RemoteRunspace remoteRunspace = runspace as RemoteRunspace;

            if (remoteRunspace == null)
            {
                this.rsConnection = runspace;
            }
            else
            {
                this.rsConnection = remoteRunspace.RunspacePool;

                if (remotePowerShell != null)
                {
                    remotePowerShell.Clear();
                    remotePowerShell.Dispose();
                }
                remotePowerShell = new ClientRemotePowerShell(this, remoteRunspace.RunspacePool.RemoteRunspacePoolInternal);
            }

            this.runspace = runspace;
            this.runspaceOwner = owner;
            this.runspacePool = null;
        }

        private Runspace runspace = null;
        private bool runspaceOwner = false; // If true, runspace was created by this instance; otherwise it was set by the user and we should not dispose it

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
                return this.runspacePool;
            }

            set
            {
                if (value != null)
                {
                    lock (this.syncObject)
                    {
                        AssertChangesAreAccepted();

                        if (this.runspace != null && this.runspaceOwner)
                        {
                            this.runspace.Dispose();
                            this.runspace = null;
                            this.runspaceOwner = false;
                        }

                        this.rsConnection = value;
                        this.runspacePool = value;

                        if (this.runspacePool.IsRemote)
                        {
                            if (this.remotePowerShell != null)
                            {
                                this.remotePowerShell.Clear();
                                this.remotePowerShell.Dispose();
                            }
                            this.remotePowerShell = new
                                ClientRemotePowerShell(this, this.runspacePool.RemoteRunspacePoolInternal);
                        }
                        this.runspace = null;
                    }
                }
            }
        }

        private RunspacePool runspacePool = null;

        /// <summary>
        /// Gets the associated Runspace or RunspacePool for this PowerShell
        /// instance. If this is null, PowerShell instance is not associated
        /// with any runspace.
        /// </summary>
        internal object GetRunspaceConnection()
        {
            return rsConnection;
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
            this.commandInvokedSynchronously = true;

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
        /// <returns>IAsyncResult</returns>
        public IAsyncResult ConnectAsync(
            PSDataCollection<PSObject> output,
            AsyncCallback invocationCallback,
            object state)
        {
            if (this.invocationStateInfo.State != PSInvocationState.Disconnected)
            {
                throw new InvalidPowerShellStateException(this.invocationStateInfo.State);
            }

            // Ensure this is a command invoked on a remote runspace(pool) and connect the
            // runspace if it is currently disconnected.
            CheckRunspacePoolAndConnect();

            if (this.connectCmdInfo != null)
            {
                //
                // This is a reconstruct/connect scenario and we create new state.
                //

                PSDataCollection<PSObject> streamToUse = this.outputBuffer;

                // The remotePowerShell may have been initialized by InitForRemotePipelineConnect()
                if (!this.remotePowerShell.Initialized)
                {
                    // Empty input stream.
                    ObjectStreamBase inputStream = new ObjectStream();
                    inputStream.Close();

                    // Output stream.
                    if (output != null)
                    {
                        // Use the supplied output buffer.
                        this.outputBuffer = output;
                        this.outputBufferOwner = false;
                    }
                    else if (this.outputBuffer == null)
                    {
                        this.outputBuffer = new PSDataCollection<PSObject>();
                        this.outputBufferOwner = true;
                    }
                    streamToUse = this.outputBuffer;

                    ObjectStreamBase outputStream = new PSDataCollectionStream<PSObject>(this.instanceId, streamToUse);

                    this.remotePowerShell.Initialize(inputStream, outputStream,
                                                     new PSDataCollectionStream<ErrorRecord>(this.instanceId, this.errorBuffer),
                                                     this.informationalBuffers, null);
                }

                Dbg.Assert((this.invokeAsyncResult == null), "Async result should be null in the reconstruct scenario.");
                this.invokeAsyncResult = new PowerShellAsyncResult(this.instanceId, invocationCallback, state, streamToUse, true);
            }
            else
            {
                // If this is not a reconstruct scenario then this must be a PowerShell object that was 
                // previously disconnected, and all state should be valid.
                Dbg.Assert((this.invokeAsyncResult != null && this.remotePowerShell.Initialized),
                            "AsyncResult and RemotePowerShell objects must be valid here.");

                if (output != null ||
                    invocationCallback != null ||
                    invokeAsyncResult.IsCompleted)
                {
                    // A new async object is needed.
                    PSDataCollection<PSObject> streamToUse;
                    if (output != null)
                    {
                        streamToUse = output;
                        this.outputBuffer = output;
                        this.outputBufferOwner = false;
                    }
                    else if (invokeAsyncResult.Output == null ||
                             !invokeAsyncResult.Output.IsOpen)
                    {
                        this.outputBuffer = new PSDataCollection<PSObject>();
                        this.outputBufferOwner = true;
                        streamToUse = this.outputBuffer;
                    }
                    else
                    {
                        streamToUse = invokeAsyncResult.Output;
                        this.outputBuffer = streamToUse;
                        this.outputBufferOwner = false;
                    }

                    this.invokeAsyncResult = new PowerShellAsyncResult(
                        this.instanceId,
                        invocationCallback ?? this.invokeAsyncResult.Callback,
                        (invocationCallback != null) ? state : this.invokeAsyncResult.AsyncState,
                        streamToUse,
                        true);
                }
            }

            try
            {
                // Perform the connect operation to the remote server through the PSRP layer.
                // If this.connectCmdInfo is null then a connection will be attempted using current state.
                // If this.connectCmdInfo is non-null then a connection will be attempted with this info.
                this.remotePowerShell.ConnectAsync(this.connectCmdInfo);
            }
            catch (Exception exception)
            {
                // allow GC collection
                invokeAsyncResult = null;
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));

                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;
                if (poolException != null && this.runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }
                throw;
            }

            return this.invokeAsyncResult;
        }

        /// <summary>
        /// Checks that the current runspace associated with this PowerShell is a remote runspace,
        /// and if it is in Disconnected state then to connect it.
        /// </summary>
        private void CheckRunspacePoolAndConnect()
        {
            RemoteRunspacePoolInternal remoteRunspacePoolInternal = null;
            if (this.rsConnection is RemoteRunspace)
            {
                remoteRunspacePoolInternal = (this.rsConnection as RemoteRunspace).RunspacePool.RemoteRunspacePoolInternal;
            }
            else if (this.rsConnection is RunspacePool)
            {
                remoteRunspacePoolInternal = (this.rsConnection as RunspacePool).RemoteRunspacePoolInternal;
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
        /// <param name="input">Input</param>
        /// <param name="output">Output collection</param>
        /// <param name="settings">PS invocation settings</param>
        /// <param name="invokeMustRun">True if PowerShell Invoke must run regardless
        /// of whether debugger handles the command.
        /// </param>
        /// <returns>DebuggerCommandResults</returns>
        internal void InvokeWithDebugger(
            IEnumerable<object> input, 
            IList<PSObject> output, 
            PSInvocationSettings settings,
            bool invokeMustRun)
        {
            Debugger debugger = this.runspace.Debugger;
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
                Commands.Commands.AddScript("");
            }

            if (Commands.Commands.Count > 0)
            {
                if (addToHistory)
                {
                    if (settings == null)
                    {
                        settings = new PSInvocationSettings();
                    }
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
            if (null == output)
            {
                throw PSTraceSource.NewArgumentNullException("output");
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
        public void Invoke<TInput, TOutput>(PSDataCollection<TInput>input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            if (null == output)
            {
                throw PSTraceSource.NewArgumentNullException("output");
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

            if (this.outputBuffer != null)
            {
                if (isBatching || extraCommands.Count != 0)
                {
                    return BeginBatchInvoke<T, PSObject>(input, this.outputBuffer, settings, callback, state);
                }

                return CoreInvokeAsync<T, PSObject>(input, this.outputBuffer, settings, callback, state, null);
            }
            else
            {
                this.outputBuffer = new PSDataCollection<PSObject>();
                outputBufferOwner = true;

                if (isBatching || extraCommands.Count != 0)
                {
                    return BeginBatchInvoke<T, PSObject>(input, this.outputBuffer, settings, callback, state);
                }

                return CoreInvokeAsync<T, PSObject>(input, this.outputBuffer, settings, callback, state, this.outputBuffer);
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
        public IAsyncResult BeginInvoke<TInput,TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output)
        {
            return BeginInvoke<TInput,TOutput>(input, output, null, null, null);
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
        public IAsyncResult BeginInvoke<TInput,TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state)
        {
            if (null == output)
            {
                throw PSTraceSource.NewArgumentNullException("output");
            }

            DetermineIsBatching();

            if (isBatching || extraCommands.Count != 0)
            {
                return BeginBatchInvoke<TInput, TOutput>(input, output, settings, callback, state);
            }

            return CoreInvokeAsync<TInput,TOutput>(input, output, settings, callback, state, null);
        }

        /// <summary>
        /// Begins a batch execution
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
            PSDataCollection<PSObject> asyncOutput = (object)output as PSDataCollection<PSObject>;

            if (asyncOutput == null)
            {
                throw PSTraceSource.NewInvalidOperationException();
            }

            if (isBatching)
            {
                SetupAsyncBatchExecution();
            }

            RunspacePool pool = rsConnection as RunspacePool;
            if ((null != pool) && (pool.IsRemote))
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
                        if (isBatching)
                        {
                            EndAsyncBatchExecution();
                        }
                    }
                }
            }

            // Non-remoting case or server does not support batching
            // In this case we execute the cmdlets one by one

            runningExtraCommands = true;
            batchInvocationSettings = settings;

            batchAsyncResult = new PowerShellAsyncResult(instanceId, callback, state, asyncOutput, true);

            CoreInvokeAsync<TInput, TOutput>(input, output, settings, new AsyncCallback(BatchInvocationCallback), state, asyncOutput);

            return batchAsyncResult;
        }


        /// <summary>
        /// Batch invocation callback
        /// </summary>
        /// <param name="state"></param>
        private void BatchInvocationWorkItem(object state)
        {
            Debug.Assert(extraCommands.Count != 0, "This callback is for batch invocation only");

            BatchInvocationContext context = state as BatchInvocationContext;

            Debug.Assert(context != null, "Context should never be null");

            PSCommand backupCommand = psCommand;
            try
            {
                psCommand = context.Command;

                // Last element
                if (psCommand == extraCommands[extraCommands.Count - 1])
                {
                    runningExtraCommands = false;
                }

                try
                {
                    IAsyncResult cmdResult = CoreInvokeAsync<object, PSObject>(null, context.Output, batchInvocationSettings,
                        null, batchAsyncResult.AsyncState, context.Output);

                    EndInvoke(cmdResult);
                }
                catch (ActionPreferenceStopException e)
                {
                    // We need to honor the current error action preference here
                    stopBatchExecution = true;
                    batchAsyncResult.SetAsCompleted(e);
                    return;
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);

                    SetHadErrors(true);

                    // Stop if necessarily
                    if ((null != batchInvocationSettings) && batchInvocationSettings.ErrorActionPreference == ActionPreference.Stop)
                    {
                        stopBatchExecution = true;
                        AppendExceptionToErrorStream(e);
                        batchAsyncResult.SetAsCompleted(null);
                        return;
                    }

                    // If we get here, then ErrorActionPreference is either Continue,
                    // SilentlyContinue, or Inquire (Continue), so we just continue....
                    if (batchInvocationSettings == null)
                    {
                        ActionPreference preference = (ActionPreference)Runspace.SessionStateProxy.GetVariable("ErrorActionPreference");

                        switch (preference)
                        {
                            case ActionPreference.SilentlyContinue:
                            case ActionPreference.Continue:
                                AppendExceptionToErrorStream(e);
                                break;
                            case ActionPreference.Stop:
                                batchAsyncResult.SetAsCompleted(e);
                                return;
                            case ActionPreference.Inquire:
                            case ActionPreference.Ignore:
                                break;
                        }
                    }
                    else if (batchInvocationSettings.ErrorActionPreference != ActionPreference.Ignore)
                    {
                        AppendExceptionToErrorStream(e);
                    }

                    // Let it continue
                }

                if (psCommand == extraCommands[extraCommands.Count - 1])
                {
                    batchAsyncResult.SetAsCompleted(null);
                }
            }
            finally
            {
                psCommand = backupCommand;
                context.Signal();
            }
        }

        /// <summary>
        /// Batch invocation callback
        /// </summary>
        /// <param name="result"></param>
        private void BatchInvocationCallback(IAsyncResult result)
        {
            Debug.Assert(extraCommands.Count != 0, "This callback is for batch invocation only");

            PSDataCollection<PSObject> objs = null;

            try
            {

                objs = EndInvoke(result);

                if (objs == null)
                {
                    objs = batchAsyncResult.Output;
                }

                DoRemainingBatchCommands(objs);
            }
            catch (PipelineStoppedException e)
            {
                // PowerShell throws the pipeline stopped exception.
                batchAsyncResult.SetAsCompleted(e);
                return;
            }
            catch (ActionPreferenceStopException e)
            {
                // We need to honor the current error action preference here
                batchAsyncResult.SetAsCompleted(e);
                return;
            }
            catch (Exception e)
            {
                runningExtraCommands = false;
                CommandProcessorBase.CheckForSevereException(e);

                SetHadErrors(true);

                ActionPreference preference;
                if (batchInvocationSettings != null)
                {
                    preference = (batchInvocationSettings.ErrorActionPreference.HasValue) ?
                        batchInvocationSettings.ErrorActionPreference.Value
                        : ActionPreference.Continue;
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
                        batchAsyncResult.SetAsCompleted(e);
                        return;
                    case ActionPreference.Inquire:
                    case ActionPreference.Ignore:
                        break;
                }

                if (objs == null)
                {
                    objs = batchAsyncResult.Output;
                }

                DoRemainingBatchCommands(objs);
            }
            finally
            {
                if (isBatching)
                {
                    EndAsyncBatchExecution();
                }
            }
        }

        /// <summary>
        /// Executes remaining batch commands
        /// </summary>
        private void DoRemainingBatchCommands(PSDataCollection<PSObject> objs)
        {
            if (extraCommands.Count > 1)
            {
                for (int i = 1; i < extraCommands.Count; i++)
                {
                    if (stopBatchExecution)
                    {
                        break;
                    }

                    BatchInvocationContext context = new BatchInvocationContext(extraCommands[i], objs);

                    // Queue a batch work item here.
                    // Calling CoreInvokeAsync / CoreInvoke here directly doesn't work and causes the thread to hang.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BatchInvocationWorkItem), context);
                    context.Wait();
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void DetermineIsBatching()
        {
            foreach (Command command in psCommand.Commands)
            {
                if (command.IsEndOfStatement)
                {
                    isBatching = true;
                    return;
                }
            }

            isBatching = false;
        }

        /// <summary>
        /// Prepare for async batch execution
        /// </summary>
        private void SetupAsyncBatchExecution()
        {
            Debug.Assert(isBatching);

            backupPSCommand = psCommand.Clone();
            extraCommands.Clear();

            PSCommand currentPipe = new PSCommand();
            currentPipe.Owner = this;

            foreach (Command command in psCommand.Commands)
            {
                if (command.IsEndOfStatement)
                {
                    currentPipe.Commands.Add(command);
                    extraCommands.Add(currentPipe);
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
                extraCommands.Add(currentPipe);
            }

            psCommand = extraCommands[0];
        }

        /// <summary>
        /// Ends an async batch execution
        /// </summary>
        private void EndAsyncBatchExecution()
        {
            Debug.Assert(isBatching);

            psCommand = backupPSCommand;
        }

        /// <summary>
        /// Appends an exception to the error stream
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
        public PSDataCollection<PSObject> EndInvoke(IAsyncResult asyncResult)
        {
            try
            {
                this.commandInvokedSynchronously = true;

                if (null == asyncResult)
                {
                    throw PSTraceSource.NewArgumentNullException("asyncResult");
                }

                PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

                if ((null == psAsyncResult) ||
                    (psAsyncResult.OwnerId != instanceId) ||
                    (psAsyncResult.IsAssociatedWithAsyncInvoke != true))
                {
                    throw PSTraceSource.NewArgumentException("asyncResult",
                        PowerShellStrings.AsyncResultNotOwned, "IAsyncResult", "BeginInvoke");
                }

                EndInvokeAsyncResult = psAsyncResult;
                psAsyncResult.EndInvoke();
                EndInvokeAsyncResult = null;

                if (this.outputBufferOwner)
                {
                    // PowerShell no longer owns the output buffer when it is passed back to the caller.
                    this.outputBufferOwner = false;
                    this.outputBuffer = null;
                }

                return psAsyncResult.Output;
            }
            catch (InvalidRunspacePoolStateException exception)
            {
                SetHadErrors(true);
                if (this.runspace != null) // the pool exception was actually thrown by a runspace
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
        public void Stop()
        {
            try
            {
                IAsyncResult asyncResult = CoreStop(true, null, null);
                // This is a sync call..Wait for the stop operation to complete.
                asyncResult.AsyncWaitHandle.WaitOne();
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
        public void EndStop(IAsyncResult asyncResult)
        {
            if (null == asyncResult)
            {
                throw PSTraceSource.NewArgumentNullException("asyncResult");
            }

            PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

            if ((null == psAsyncResult) || 
                (psAsyncResult.OwnerId != instanceId) || 
                (psAsyncResult.IsAssociatedWithAsyncInvoke != false))
            {
                throw PSTraceSource.NewArgumentException("asyncResult",
                    PowerShellStrings.AsyncResultNotOwned, "IAsyncResult", "BeginStop");
            }

            psAsyncResult.EndInvoke();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler for state changed changed events for the currently running pipeline.
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
        /// runspace or RunspacePool assigned to this object
        /// </summary>
        public bool IsRunspaceOwner
        {
            get { return runspaceOwner; }
            internal set { runspaceOwner = value; }
        }

        internal bool ErrorBufferOwner
        {
            get { return errorBufferOwner; }
            set { errorBufferOwner = value; }
        }

        internal bool OutputBufferOwner
        {
            get { return outputBufferOwner; }
            set { outputBufferOwner = value; }
        }

        /// <summary>
        /// OutputBuffer
        /// </summary>
        internal PSDataCollection<PSObject> OutputBuffer
        {
            get { return this.outputBuffer; }
        }

        /// <summary>
        /// This has been added as a work around for Windows8 bug 803461.
        /// It should be used only for the PSJobProxy API.
        /// 
        /// Resets the instance ID of the command to a new guid.
        /// If this is not done, then there is a race condition on the server
        /// in the following circumstances:
        /// 
        ///   ps.BeginInvoke(...);
        ///   ps.Stop()
        ///   ps.Commands.Clear();
        ///   ps.AddCommand("Foo");
        ///   ps.Invoke();
        ///   
        /// In these conditions, stop returns before the server is done cleaning up.
        /// The subsequent invoke will cause an error because the guid already
        /// identifies a command in progress.
        /// </summary>
        internal void GenerateNewInstanceId()
        {
            instanceId = Guid.NewGuid();
        }

        /// <summary>
        /// Get a steppable pipeline object.
        /// </summary>
        /// <returns>A steppable pipeline object</returns>
        /// <exception cref="InvalidOperationException">An attempt was made to use the scriptblock outside of the engine.</exception>
        internal SteppablePipeline GetSteppablePipeline()
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
        /// Gets the steppable pipeline from the powershell object
        /// </summary>
        /// <param name="context">engine execution context</param>
        /// <param name="commandOrigin">command origin</param>
        /// <returns>steppable pipeline object</returns>
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
                                ((LocalRunspace)Runspace.DefaultRunspace).CommandFactory,
                                false,
                                isNested == true ? CommandOrigin.Internal : CommandOrigin.Runspace
                            );

                    commandProcessorBase.RedirectShellErrorOutputPipe = this.redirectShellErrorOutputPipe;

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
                CommandProcessorBase.CheckForSevereException(e);
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

        internal bool IsGetCommandMetadataSpecialPipeline
        {
            get { return this.isGetCommandMetadataSpecialPipeline; }
            set { this.isGetCommandMetadataSpecialPipeline = value; }            
        }

        /// <summary>
        /// Checks if the command is running
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

            if (invocationStateInfo.State == PSInvocationState.Stopping)
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
            lock (syncObject)
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
            if (isDisposed)
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
                lock (syncObject)
                {
                    // if already disposed return
                    if (isDisposed)
                    {
                        return;
                    }
                }

                // Stop the currently running command outside of the lock
                if (invocationStateInfo.State == PSInvocationState.Running ||
                    invocationStateInfo.State == PSInvocationState.Stopping)
                {
                    Stop();
                }

                lock (syncObject)
                {
                    isDisposed = true;
                }

                if (outputBuffer != null && outputBufferOwner)
                {
                    outputBuffer.Dispose();
                }

                if (errorBuffer != null && errorBufferOwner)
                {
                    errorBuffer.Dispose();
                }

                if (runspaceOwner)
                {
                    runspace.Dispose();
                }

                if (remotePowerShell != null)
                {
                    remotePowerShell.Dispose();
                }

                invokeAsyncResult = null;
                stopAsyncResult = null;
            }
        }

        /// <summary>
        /// Clear the internal elements
        /// </summary>
        private void InternalClearSuppressExceptions()
        {
            lock (syncObject)
            {
                if (null != worker)
                {
                    worker.InternalClearSuppressExceptions();                    
                }
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
            RemoteRunspace remoteRunspace = this.runspace as RemoteRunspace;
            if (remoteRunspace != null && !this.IsNested)
            {
                this.runspace.UpdateRunspaceAvailability(invocationStateInfo.State, true, instanceId);
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
        /// <param name="stateInfo">the state info to set</param>
        internal void SetStateChanged(PSInvocationStateInfo stateInfo)
        {
            PSInvocationStateInfo copyStateInfo = stateInfo;
            PSInvocationState previousState;

            // copy pipeline HasdErrors property to PowerShell instance...
            if (worker != null && worker.CurrentlyRunningPipeline != null)
            {
                SetHadErrors(worker.CurrentlyRunningPipeline.HadErrors);
            }

            // win281312: Usig temporary variables to avoid thread
            // synchronization issues between Dispose and transition
            // to Terminal States (Completed/Failed/Stopped)
            PowerShellAsyncResult tempInvokeAsyncResult;
            PowerShellAsyncResult tempStopAsyncResult;

            lock (syncObject)
            {
                previousState = invocationStateInfo.State;

                // Check the current state and see if we need to process this pipeline event.
                switch (invocationStateInfo.State)
                {
                    case PSInvocationState.Completed:
                    case PSInvocationState.Failed:
                    case PSInvocationState.Stopped:
                        // if the current state is already completed..then no need to process state
                        // change requests. This will happen if another thread  calls BeginStop
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

                tempInvokeAsyncResult = invokeAsyncResult;
                tempStopAsyncResult = stopAsyncResult;
                invocationStateInfo = copyStateInfo;
            }

            bool isExceptionOccured = false;
            switch (invocationStateInfo.State)
            {
                case PSInvocationState.Running:
                    CloseInputBufferOnReconnection(previousState);
                    RaiseStateChangeEvent(invocationStateInfo.Clone());
                    break;
                case PSInvocationState.Stopping:
                    RaiseStateChangeEvent(invocationStateInfo.Clone());
                    break;
                case PSInvocationState.Completed:
                case PSInvocationState.Failed:
                case PSInvocationState.Stopped:
                    // Clear Internal data
                    InternalClearSuppressExceptions();

                    // Ensure remote receive queue is not blocked.
                    if (remotePowerShell != null)
                    {
                        ResumeIncomingData();
                    }

                    try
                    {
                        if (runningExtraCommands)
                        {
                            if (null != tempInvokeAsyncResult)
                            {
                                tempInvokeAsyncResult.SetAsCompleted(invocationStateInfo.Reason);
                            }

                            RaiseStateChangeEvent(invocationStateInfo.Clone());
                        }
                        else
                        {
                            RaiseStateChangeEvent(invocationStateInfo.Clone());

                            if (null != tempInvokeAsyncResult)
                            {
                                tempInvokeAsyncResult.SetAsCompleted(invocationStateInfo.Reason);
                            }
                        }

                        if (null != tempStopAsyncResult)
                        {
                            tempStopAsyncResult.SetAsCompleted(null);
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
                        // takes care exception occured with invokeAsyncResult
                        if (isExceptionOccured && (null != tempStopAsyncResult))
                        {
                            tempStopAsyncResult.Release();
                        }
                    }
                    break;
                case PSInvocationState.Disconnected:
                    try
                    {
                        // Ensure remote receive queue is not blocked.
                        if (remotePowerShell != null)
                        {
                            ResumeIncomingData();
                        }

                        // If this command was disconnected and was also invoked synchronously then
                        // we throw an exception on the calling thread.
                        if (this.commandInvokedSynchronously && (tempInvokeAsyncResult != null))
                        {
                            tempInvokeAsyncResult.SetAsCompleted(new RuntimeException(PowerShellStrings.DiscOnSyncCommand));
                        }

                        // This object can be disconnected even if "BeginStop" was called if it is a remote object
                        // and robust connections is retrying a failed network connection.
                        // In this case release the stop wait handle to prevent hangs.
                        if (tempStopAsyncResult != null)
                        {
                            tempStopAsyncResult.SetAsCompleted(null);
                        }

                        // Only raise the Disconnected state changed event if the PowerShell state
                        // actually transitions to Disconnected from some other state.  This condition 
                        // can happen when the corresponding runspace disconnects/connects multiple 
                        // times with the command remaining in Disconnected state.
                        if (previousState != PSInvocationState.Disconnected)
                        {
                            RaiseStateChangeEvent(invocationStateInfo.Clone());
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
                        // takes care exception occured with invokeAsyncResult
                        if (isExceptionOccured && (null != tempStopAsyncResult))
                        {
                            tempStopAsyncResult.Release();
                        }
                    }

                    // Make sure the connect command information is null when going to Disconnected state.
                    // This parameter is used to determine reconnect/reconstruct scenarios.  Setting to null
                    // means we have a reconnect scenario.
                    this.connectCmdInfo = null;
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
                this.commandInvokedSynchronously &&
                this.remotePowerShell.InputStream != null &&
                this.remotePowerShell.InputStream.IsOpen &&
                this.remotePowerShell.InputStream.Count > 0)
            {
                this.remotePowerShell.InputStream.Close();
            }
        }

        /// <summary>
        /// clear the internal reference to remote powershell
        /// </summary>
        internal void ClearRemotePowerShell()
        {
            lock (syncObject)
            {
                if (null != remotePowerShell)
                {
                    remotePowerShell.Clear();
                }
            }
        }

        /// <summary>
        /// Sets if the pipeline is nested, typically used by the remoting infrastructure
        /// </summary>
        /// <param name="isNested"></param>
        internal void SetIsNested(bool isNested)
        {
            AssertChangesAreAccepted();

            this.isNested = isNested;
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
        /// reflextion access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the command was executing was aborted.
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
            if (null != input)
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
        /// Core invocation helper method
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">input objects</param>
        /// <param name="output">output object</param>
        /// <param name="settings">invocation settings</param>
        private void CoreInvokeHelper<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            RunspacePool pool = rsConnection as RunspacePool;

            // Prepare the environment...non-remoting case.
            Prepare<TInput, TOutput>(input, output, settings, true);

            try
            {
                // Invoke in the same thread as the calling thread.
                Runspace rsToUse = null;
                if (!isNested)
                {
                    if (null != pool)
                    {
#if !CORECLR            // No ApartmentState In CoreCLR
                        VerifyThreadSettings(settings, pool.ApartmentState, pool.ThreadOptions, false);
#endif
                        // getting the runspace asynchronously so that Stop can be supported from a different
                        // thread.
                        worker.GetRunspaceAsyncResult = pool.BeginGetRunspace(null, null);
                        worker.GetRunspaceAsyncResult.AsyncWaitHandle.WaitOne();
                        rsToUse = pool.EndGetRunspace(worker.GetRunspaceAsyncResult);
                    }
                    else
                    {
                        rsToUse = rsConnection as Runspace;
                        if (null != rsToUse)
                        {
#if !CORECLR                // No ApartmentState In CoreCLR
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
                    worker.CreateRunspaceIfNeededAndDoWork(rsToUse, true);
                }
                else
                {
                    rsToUse = rsConnection as Runspace;
                    Dbg.Assert(null != rsToUse,
                        "Nested PowerShell can only work on a Runspace");


                    // Peform work on the current thread. Nested Pipeline
                    // should be invoked from the same thread that the parent
                    // pipeline is executing in.
                    worker.ConstructPipelineAndDoWork(rsToUse, true);
                }
            }
            catch (Exception exception)
            {
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));

                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;

                if (poolException != null && this.runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }
                throw;
            }
        }

        /// <summary>
        /// Core invocation helper method for remoting
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">input objects</param>
        /// <param name="output">output object</param>
        /// <param name="settings">invocation settings</param>
        private void CoreInvokeRemoteHelper<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            RunspacePool pool = rsConnection as RunspacePool;

            // For remote calls..use the infrastructure built in CoreInvokeAsync..
            IAsyncResult asyncResult = CoreInvokeAsync<TInput, TOutput>(input, output, settings,
                null, null, null);
            this.commandInvokedSynchronously = true;
            PowerShellAsyncResult psAsyncResult = asyncResult as PowerShellAsyncResult;

            // Wait for command to complete.  If an exception was thrown during command
            // execution (such as disconnect occurring during this synchronous execution)
            // then the exception will be thrown here.
            EndInvokeAsyncResult = psAsyncResult;
            psAsyncResult.EndInvoke();
            EndInvokeAsyncResult = null;

            if ((PSInvocationState.Failed == invocationStateInfo.State) &&
                        (null != invocationStateInfo.Reason))
            {
                throw invocationStateInfo.Reason;
            }

            return;
        }

        /// <summary>
        /// Core invocation method
        /// </summary>
        /// <typeparam name="TInput">input type</typeparam>
        /// <typeparam name="TOutput">output type</typeparam>
        /// <param name="input">input objects</param>
        /// <param name="output">output object</param>
        /// <param name="settings">invocation settings</param>
        private void CoreInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings)
        {
            bool isRemote = false;

            DetermineIsBatching();

            if (isBatching)
            {
                SetupAsyncBatchExecution();
            }

            SetHadErrors(false);
            RunspacePool pool = rsConnection as RunspacePool;
            if ((null != pool) && (pool.IsRemote))
            {
                if (ServerSupportsBatchInvocation())
                {
                    try
                    {
                        CoreInvokeRemoteHelper(input, output, settings);
                    }
                    finally
                    {
                        if (isBatching)
                        {
                            EndAsyncBatchExecution();
                        }
                    }

                    return;
                }

                isRemote = true;
            }

            if (isBatching)
            {
                try
                {
                    foreach (PSCommand command in extraCommands)
                    {
                        // Last element
                        if (psCommand != extraCommands[extraCommands.Count - 1])
                        {
                            runningExtraCommands = true;
                        }
                        else
                        {
                            runningExtraCommands = false;
                        }

                        try
                        {
                            psCommand = command;

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
                            CommandProcessorBase.CheckForSevereException(e);

                            SetHadErrors(true);

                            // Stop if necessarily
                            if ((null != settings) && settings.ErrorActionPreference == ActionPreference.Stop)
                            {
                                throw;
                            }

                            // Ignore the exception if neccessary.
                            if ((null != settings) && settings.ErrorActionPreference == ActionPreference.Ignore)
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
                    runningExtraCommands = false;

                    if (isBatching)
                    {
                        EndAsyncBatchExecution();
                    }
                }
            }
            else
            {
                runningExtraCommands = false;

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
        private IAsyncResult CoreInvokeAsync<TInput,TOutput>(PSDataCollection<TInput> input,
            PSDataCollection<TOutput> output, PSInvocationSettings settings,
            AsyncCallback callback, object state, PSDataCollection<PSObject> asyncResultOutput)
        {
            RunspacePool pool = rsConnection as RunspacePool;

            // We dont need to create worker if pool is remote
            Prepare<TInput,TOutput>(input, output, settings, (pool == null || !pool.IsRemote));

            invokeAsyncResult = new PowerShellAsyncResult(instanceId, callback, state, asyncResultOutput, true);

            try
            {
                // IsNested is true for the icm | % { icm } scenario
                if (!isNested || (pool != null && pool.IsRemote))
                {
                    if (null != pool)
                    {
#if !CORECLR            // No ApartmentState In CoreCLR
                        VerifyThreadSettings(settings, pool.ApartmentState, pool.ThreadOptions, pool.IsRemote);
#endif
                        pool.AssertPoolIsOpen();

                        // for executing in a remote runspace pool case
                        if (pool.IsRemote)
                        {
                            worker = null;

                            lock (syncObject)
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

                                invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Running, null);

                                ObjectStreamBase inputStream = null;

                                if (input != null)
                                {
                                    inputStream = new PSDataCollectionStream<TInput>(instanceId, input);
                                }

                                if (!remotePowerShell.Initialized)
                                {
                                    if (inputStream == null)
                                    {
                                        inputStream = new ObjectStream();
                                        inputStream.Close();
                                    }
                                    remotePowerShell.Initialize(
                                        inputStream, new PSDataCollectionStream<TOutput>(instanceId, output),
                                                new PSDataCollectionStream<ErrorRecord>(instanceId, errorBuffer),
                                                    informationalBuffers, settings);
                                }
                                else
                                {
                                    if (inputStream != null)
                                    {
                                        remotePowerShell.InputStream = inputStream;
                                    }
                                    if (output != null)
                                    {
                                        remotePowerShell.OutputStream =
                                            new PSDataCollectionStream<TOutput>(instanceId, output);
                                    }
                                }

                                pool.RemoteRunspacePoolInternal.CreatePowerShellOnServerAndInvoke(remotePowerShell);

                            } // lock

                            RaiseStateChangeEvent(invocationStateInfo.Clone());
                        }
                        else
                        {
                            worker.GetRunspaceAsyncResult = pool.BeginGetRunspace(
                                    new AsyncCallback(worker.RunspaceAvailableCallback), null);
                        }
                    }
                    else
                    {
                        LocalRunspace rs = rsConnection as LocalRunspace;
                        if (null != rs)
                        {
#if !CORECLR                // No ApartmentState In CoreCLR
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
                            worker.CreateRunspaceIfNeededAndDoWork(rs, false);
                        }
                        else
                        {
                            // create a new runspace and perform invoke..
                            ThreadPool.QueueUserWorkItem(
                                new WaitCallback(worker.CreateRunspaceIfNeededAndDoWork),
                                rsConnection);
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
                invokeAsyncResult = null;
                SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, exception));
                // re-throw the exception
                InvalidRunspacePoolStateException poolException = exception as InvalidRunspacePoolStateException;

                if (poolException != null && this.runspace != null) // the pool exception was actually thrown by a runspace
                {
                    throw poolException.ToInvalidRunspaceStateException();
                }
                throw;
            }

            return invokeAsyncResult;
        }

#if !CORECLR // No ApartmentState In CoreCLR
        /// <summary>
        /// Verifies the settings for ThreadOptions and ApartmentState
        /// </summary>
        private void VerifyThreadSettings(PSInvocationSettings settings, ApartmentState runspaceApartmentState, PSThreadOptions runspaceThreadOptions, bool isRemote)
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
        /// 
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
        private void Prepare<TInput,TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, bool shouldCreateWorker)
        {
            Dbg.Assert(null != output, "Output cannot be null");
         
            lock (syncObject)
            {
                if ((null == psCommand) || (null == psCommand.Commands) || (0 == psCommand.Commands.Count))
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
                    invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Running, null);

                    // update settings for impersonation policy
                    if ((null != settings) && (settings.FlowImpersonationPolicy))
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
                    if (null != input)
                    {
                        inputStream = new PSDataCollectionStream<TInput>(instanceId, input);
                    }
                    else
                    {
                        inputStream = new ObjectStream();
                        inputStream.Close();
                    }

                    ObjectStreamBase outputStream = new PSDataCollectionStream<TOutput>(instanceId, output);
                    worker = new Worker(inputStream, outputStream, settings, this);
                }
            }

            // Only one thread will be running after this point
            // so no need to lock.

            if (shouldCreateWorker)
            {
                // Raise the state change events outside of the lock
                // send a cloned copy..this way the handler will
                // not see changes happening to the instance's execution state.
                RaiseStateChangeEvent(invocationStateInfo.Clone());                
            }
        }

        /// <summary>
        /// Called by both Sync Stop and Async Stop.
        /// If isSyncCall is false, then an IAsyncResult object is returned which
        /// can be passed back to the user
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
            lock (syncObject)
            {
                //BUGBUG: remote powershell appears to handle state change's differently
                //Need to speak with remoting dev and resolve this.
                switch (invocationStateInfo.State)
                {
                    case PSInvocationState.NotStarted:
                        // Stopped is called before operation started..we need to change
                        // state to stopping and then to stopped... so that future stops
                        // dont affect the state.
                        invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Stopping,
                            null);

                        events.Enqueue(new PSInvocationStateInfo(PSInvocationState.Stopped,
                            null));
                        break;

                    case PSInvocationState.Completed:
                    case PSInvocationState.Failed:
                    case PSInvocationState.Stopped:
                        stopAsyncResult = new PowerShellAsyncResult(instanceId, callback, state, null, false);
                        stopAsyncResult.SetAsCompleted(null);
                        return stopAsyncResult;

                    case PSInvocationState.Stopping:
                        // Create new stop sync object if none exists.  Otherwise return existing.
                        if (stopAsyncResult == null)
                        {
                            stopAsyncResult = new PowerShellAsyncResult(instanceId, callback, state, null, false);
                            stopAsyncResult.SetAsCompleted(null);
                        }
                        return stopAsyncResult;

                    case PSInvocationState.Running:
                        invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Stopping,
                            null);
                        isRunning = true;
                        break;

                    case PSInvocationState.Disconnected:
                        // Stopping a disconnected command results in a failed state.
                        invocationStateInfo = new PSInvocationStateInfo(PSInvocationState.Failed, null);
                        isDisconnected = true;
                        break;
                }

                stopAsyncResult = new PowerShellAsyncResult(instanceId, callback, state, null, false);
            }

            // If in the Disconnected state then stopping simply cuts loose the PowerShell object
            // so that a new one can be connected.  The state is set to Failed since the command 
            // cannot complete with this object.
            if (isDisconnected)
            {
                if (invokeAsyncResult != null)
                {
                    // Since object is stopped, allow result wait to end.
                    invokeAsyncResult.SetAsCompleted(null);
                }
                stopAsyncResult.SetAsCompleted(null);

                // Raise event for failed state change.
                RaiseStateChangeEvent(invocationStateInfo.Clone());

                return stopAsyncResult;
            }

            // Ensure the runspace is not blocking in a debug stop.
            ReleaseDebugger();

            RaiseStateChangeEvent(invocationStateInfo.Clone());

            bool shouldRunStopHelper = false;
            RunspacePool pool = rsConnection as RunspacePool;

            if (pool != null && pool.IsRemote)
            {
                if ((remotePowerShell != null) && remotePowerShell.Initialized)
                {                
                    remotePowerShell.StopAsync();

                    if (isSyncCall)
                    {
                        stopAsyncResult.AsyncWaitHandle.WaitOne();
                    }
                }
                else
                {
                    shouldRunStopHelper = true;
                }
            }
            else if (isRunning)
            {
                 worker.Stop(isSyncCall);
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

            return stopAsyncResult;
        }

        private void ReleaseDebugger()
        {
            LocalRunspace localRunspace = this.runspace as LocalRunspace;
            if (localRunspace != null)
            {
                localRunspace.ReleaseDebugger();
            }
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
                CommandProcessorBase.CheckForSevereException(e);
                // report non-severe exceptions to the user via the
                // asyncresult object
                exception = e;
                throw;
            }
        }

        /// <summary>
        /// The client remote powershell associated with this
        /// powershell object
        /// </summary>
        internal ClientRemotePowerShell RemotePowerShell
        {
            get
            {
                return remotePowerShell;
            }
        }

        /// <summary>
        /// The history string to be used for displaying
        /// the history.
        /// </summary>
        public String HistoryString
        {
            get
            {
                return historyString;
            }
            set
            {
                historyString = value;
            }
        }

        /// <summary>
        /// Extra commands to run in a single invocation
        /// </summary>
        internal Collection<PSCommand> ExtraCommands
        {
            get { return extraCommands; }
        }

        /// <summary>
        /// Currently running extra commands
        /// </summary>
        internal bool RunningExtraCommands
        {
            get { return runningExtraCommands; }
        }

        private bool ServerSupportsBatchInvocation()
        {
            if (runspace != null)
            {
                return runspace.RunspaceStateInfo.State != RunspaceState.BeforeOpen &&
                       runspace.GetRemoteProtocolVersion() >= RemotingConstants.ProtocolVersionWin8RTM;
            }

            RemoteRunspacePoolInternal remoteRunspacePoolInternal = null;
            if (this.rsConnection is RemoteRunspace)
            {
                remoteRunspacePoolInternal = (this.rsConnection as RemoteRunspace).RunspacePool.RemoteRunspacePoolInternal;
            }
            else if (this.rsConnection is RunspacePool)
            {
                remoteRunspacePoolInternal = (this.rsConnection as RunspacePool).RemoteRunspacePoolInternal;
            }

            return remoteRunspacePoolInternal != null &&
                   remoteRunspacePoolInternal.PSRemotingProtocolVersion >= RemotingConstants.ProtocolVersionWin8RTM;
        }

        /// <summary>
        /// Helper method to add running remote PowerShell to the remote runspace list.
        /// </summary>
        private void AddToRemoteRunspaceRunningList()
        {
            if (this.runspace != null)
            {
                this.runspace.PushRunningPowerShell(this);
            }
            else
            {
                RemoteRunspacePoolInternal remoteRunspacePoolInternal = GetRemoteRunspacePoolInternal();
                if (remoteRunspacePoolInternal != null)
                {
                    remoteRunspacePoolInternal.PushRunningPowerShell(this);
                }
            }
        }

        /// <summary>
        /// Helper method to remove running remote PowerShell from the remote runspacelist.
        /// </summary>
        private void RemoveFromRemoteRunspaceRunningList()
        {
            if (this.runspace != null)
            {
                this.runspace.PopRunningPowerShell();
            }
            else
            {
                RemoteRunspacePoolInternal remoteRunspacePoolInternal = GetRemoteRunspacePoolInternal();
                if (remoteRunspacePoolInternal != null)
                {
                    remoteRunspacePoolInternal.PopRunningPowerShell();
                }
            }
        }

        private RemoteRunspacePoolInternal GetRemoteRunspacePoolInternal()
        {
            RunspacePool runspacePool = this.rsConnection as RunspacePool;
            return (runspacePool != null) ? (runspacePool.RemoteRunspacePoolInternal) : null;
        }

        #endregion

        #region Worker

        /// <summary>
        /// AsyncResult object used to monitor pipeline creation and invocation.
        /// This is needed as a Runspace may not be available in the RunspacePool.
        /// </summary>
        private sealed class Worker
        {
            private ObjectStreamBase inputStream;
            private ObjectStreamBase outputStream;
            private ObjectStreamBase errorStream;
            private PSInvocationSettings settings;
            private IAsyncResult getRunspaceAsyncResult;
            private Pipeline currentlyRunningPipeline;
            private bool isNotActive;
            private PowerShell shell;
            private object syncObject = new object();

            /// <summary>
            /// 
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
                this.inputStream = inputStream;
                this.outputStream = outputStream;
                this.errorStream = new PSDataCollectionStream<ErrorRecord>(shell.instanceId, shell.errorBuffer);
                this.settings = settings;
                this.shell = shell;
            }

            /// <summary>
            /// Sets the async result object that monitors a
            /// BeginGetRunspace async operation on the 
            /// RunspacePool.
            /// </summary>
            internal IAsyncResult GetRunspaceAsyncResult
            {
                get { return getRunspaceAsyncResult; }
                set { getRunspaceAsyncResult = value; }
            }

            /// <summary>
            /// Gets the currently running pipeline.
            /// </summary>
            internal Pipeline CurrentlyRunningPipeline
            {
                get { return currentlyRunningPipeline; }
            }

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
                    if (null == rs)
                    {
                        lock (this.shell.syncObject)
                        {
                            if (this.shell.runspace != null)
                            {
                                rsToUse = this.shell.runspace;
                            }
                            else
                            {
                                Runspace runspace = null;

                                if ((null != settings) && (null != settings.Host))
                                {
                                    runspace = RunspaceFactory.CreateRunspace(settings.Host);
                                }
                                else
                                {
                                    runspace = RunspaceFactory.CreateRunspace();
                                }

                                this.shell.SetRunspace(runspace, true);

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
                    lock (syncObject)
                    {
                        if (isNotActive)
                            return;
                        isNotActive = true;
                    }

                    shell.PipelineStateChanged(this,
                           new PipelineStateEventArgs(
                               new PipelineStateInfo(PipelineState.Failed,
                                   e)));


                    if (!isSync)
                    {
                        // eat the exception but check if it is severe.
                        CommandProcessorBase.CheckForSevereException(e);
                    }
                    else
                    {
                        throw;
                    }
                }
#pragma warning restore 56500
            }

            /// <summary>
            /// This method gets called from a ThreadPool thread.
            /// This method gets called from a RunspacePool thread when a
            /// Runsapce is available.
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
                    RunspacePool pool = shell.rsConnection as RunspacePool;
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
                    CommandProcessorBase.CheckForSevereException(e);
                    // PipelineStateChangedEvent is not raised
                    // if there is an exception calling BeginInvoke
                    // So raise the event here and notify the caller.
                    lock (syncObject)
                    {
                        if (isNotActive)
                            return;
                        isNotActive = true;
                    }
                    shell.PipelineStateChanged(this,
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
                shell.RunspaceAssigned.SafeInvoke(this, new PSEventArgs<Runspace>(rs));

                // lock is needed until a pipeline is created to 
                // make stop() cleanly release resources.
                LocalRunspace lrs = rs as LocalRunspace;
                
                lock (syncObject)
                {
                    if (isNotActive)
                    {
                        return false;
                    }

                    if (null != lrs)
                    {
                        LocalPipeline localPipeline = new LocalPipeline(
                            lrs,
                            shell.Commands.Commands,
                            ((null != settings) && (settings.AddToHistory)) ? true : false,
                            shell.IsNested,
                            inputStream,
                            outputStream,
                            errorStream,
                            shell.informationalBuffers);

                        localPipeline.IsChild = shell.IsChild;

                        if (!String.IsNullOrEmpty(shell.HistoryString))
                        {
                            localPipeline.SetHistoryString(shell.HistoryString);
                        }

                        localPipeline.RedirectShellErrorOutputPipe = this.shell.RedirectShellErrorOutputPipe;

                        currentlyRunningPipeline = localPipeline;
                        // register for pipeline state changed events within a lock...so that if
                        // stop is called before invoke, we can listen to state transition and 
                        // take appropriate action.
                        currentlyRunningPipeline.StateChanged += shell.PipelineStateChanged;
                    }
                    else
                    {
                        throw PSTraceSource.NewNotImplementedException();
                    }
                }

                // Set pipeline specific settings
                currentlyRunningPipeline.InvocationSettings = settings;        
        
                Dbg.Assert(lrs != null, "LocalRunspace cannot be null here");

                if (performSyncInvoke)
                {
                    currentlyRunningPipeline.Invoke();
                }
                else
                {
                    currentlyRunningPipeline.InvokeAsync();
                }

                return true;
            }

            /// <summary>
            /// Stops the async operation.
            /// </summary>
            /// <param name="isSyncCall"></param>
            internal void Stop(bool isSyncCall)
            {
                lock (syncObject)
                {
                    if (isNotActive)
                    {
                        return;
                    }

                    isNotActive = true;
                    if (null != currentlyRunningPipeline)
                    {
                        if (isSyncCall)
                        {
                            currentlyRunningPipeline.Stop();
                        }
                        else
                        {
                            currentlyRunningPipeline.StopAsync();
                        }
                        return;
                    }
                    
                    if (null != getRunspaceAsyncResult)
                    {
                        RunspacePool pool = shell.rsConnection as RunspacePool;
                        Dbg.Assert(pool != null, "RunspaceConnection must be a runspace pool");
                        pool.CancelGetRunspace(getRunspaceAsyncResult);
                    }
                }

                // Pipeline is not yet associated with PowerShell..so emulate stop
                // locally
                Queue<PSInvocationStateInfo> events = new Queue<PSInvocationStateInfo>();
                events.Enqueue(new PSInvocationStateInfo(PSInvocationState.Stopped, null));

                if (isSyncCall)
                {
                    shell.StopHelper(events);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(shell.StopThreadProc), events);
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
                    if ((null != settings) && (null != settings.WindowsIdentityToImpersonate))
                    {
                        settings.WindowsIdentityToImpersonate.Dispose();
                        settings.WindowsIdentityToImpersonate = null;
                    }

                    inputStream.Close();
                    outputStream.Close();
                    errorStream.Close();

                    if (null == currentlyRunningPipeline)
                    {
                        return;
                    }

                    // Detach state changed handler so that runspace.close
                    // and pipeline.dispose will not change powershell instances state
                    currentlyRunningPipeline.StateChanged -= shell.PipelineStateChanged;

                    if ((null == getRunspaceAsyncResult) && (null == shell.rsConnection))
                    {
                        // user did not supply a runspace..Invoke* method created
                        // a new runspace..so close it.
                        currentlyRunningPipeline.Runspace.Close();
                    }
                    else
                    {
                        RunspacePool pool = shell.rsConnection as RunspacePool;
                        if (null != pool)
                        {
                            pool.ReleaseRunspace(currentlyRunningPipeline.Runspace);
                        }
                    }

                    currentlyRunningPipeline.Dispose();
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

                currentlyRunningPipeline = null;
            }

#if !CORECLR // PSMI Not Supported On CSS
            internal void GetSettings(out bool addToHistory, out bool noInput, out uint apartmentState)
            {
                addToHistory = this.settings.AddToHistory;
                noInput = false;
                apartmentState = (uint)this.settings.ApartmentState;
            }
#endif
        }

        #endregion

        #region Serialization / deserialization for remoting

        /// <summary>
        /// Creates a PowerShell object from a PSObject property bag. 
        /// PSObject has to be in the format returned by ToPSObjectForRemoting method.
        /// </summary>
        /// <param name="powerShellAsPSObject">PSObject to rehydrate</param>
        /// <returns>
        /// PowerShell rehydrated from a PSObject property bag
        /// </returns>       
        /// <exception cref="ArgumentNullException">
        /// Thrown if the PSObject is null.
        /// </exception>
        /// <exception cref="System.Management.Automation.Remoting.PSRemotingDataStructureException">
        /// Thrown when the PSObject is not in the expected format
        /// </exception>
        static internal PowerShell FromPSObjectForRemoting(PSObject powerShellAsPSObject)
        {
            if (powerShellAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException("powerShellAsPSObject");
            }

            Collection<PSCommand> extraCommands = null;
            ReadOnlyPSMemberInfoCollection<PSPropertyInfo> properties = powerShellAsPSObject.Properties.Match(RemoteDataNameStrings.ExtraCommands);
            
            if(properties.Count > 0)
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
        /// <returns>This object as a PSObject property bag</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject powerShellAsPSObject = RemotingEncoder.CreateEmptyPSObject();
            Version psRPVersion = RemotingEncoder.GetPSRemotingProtocolVersion(rsConnection as RunspacePool);

            // Check if the server supports batch invocation
            if (ServerSupportsBatchInvocation())
            {
                if (extraCommands.Count > 0)
                {
                    List<PSObject> extraCommandsAsListOfPSObjects = new List<PSObject>(this.extraCommands.Count);
                    foreach (PSCommand extraCommand in extraCommands)
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
            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.HistoryString, this.historyString));
            powerShellAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.RedirectShellErrorOutputPipe, this.RedirectShellErrorOutputPipe));
            return powerShellAsPSObject;
        }

        private List<PSObject> CommandsAsListOfPSObjects(CommandCollection commands, Version psRPVersion)
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
            if (remotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            if (remotePowerShell.DataStructureHandler != null)
            {
                remotePowerShell.DataStructureHandler.TransportManager.SuspendQueue(true);
            }
        }

        /// <summary>
        /// Resumes data arriving from remote session.
        /// </summary>
        internal void ResumeIncomingData()
        {
            if (remotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            if (remotePowerShell.DataStructureHandler != null)
            {
                remotePowerShell.DataStructureHandler.TransportManager.ResumeQueue();
            }
        }

        /// <summary>
        /// Blocking call that waits until the *current remote* data
        /// queue at the transport manager is empty.  This affects only
        /// the current queue until it is empty.
        /// </summary>
        internal void WaitForServicingComplete()
        {
            if (remotePowerShell == null)
            {
                throw new PSNotSupportedException();
            }

            if (remotePowerShell.DataStructureHandler != null)
            {
                int count = 0;
                while (++count < 2 &&
                       remotePowerShell.DataStructureHandler.TransportManager.IsServicing)
                {
                    // Try waiting for 50 ms, then continue.
                    Threading.Thread.Sleep(50);
                }
            }
        }

        #endregion

        #region V3 Extensions

        /// <summary>
        /// Returns a job object which can be used to
        /// control the invocation of the command with
        /// AsJob Parameter
        /// </summary>
        /// <returns>Job object</returns>
        public PSJobProxy AsJobProxy()
        {
            // if there are no commands added
            // throw an invalid operation exception
            if (this.Commands.Commands.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.GetJobForCommandRequiresACommand);
            }

            // if there is more than one command in the
            // command collection throw an error
            if (this.Commands.Commands.Count > 1)
            {
                throw PSTraceSource.NewInvalidOperationException(PowerShellStrings.GetJobForCommandNotSupported);
            }

            // check if the AsJob parameter has already 
            // been added. If not, add the same
            bool found = false;
            foreach(CommandParameter parameter in this.Commands.Commands[0].Parameters)
            {
                if (string.Compare(parameter.Name, "AsJob", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    found = true;
                }
            }
            
            if (!found)
            {
                AddParameter("AsJob");
            }

            // initialize the job invoker and return the same
            PSJobProxy job = new PSJobProxy(this.Commands.Commands[0].CommandText);
            job.InitializeJobProxy(this.Commands, this.Runspace, this.RunspacePool);

            return job;
        }

        #endregion V3 Extensions

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

            bool addToHistoryValue=false, noInputValue=false;
            uint apartmentStateValue = 0;
            if (this.worker != null)
            {
                this.worker.GetSettings(out addToHistoryValue, out noInputValue, out apartmentStateValue);
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
    /// Streams generated by PowerShell invocations
    /// </summary>
    public sealed class PSDataStreams
    {
        /// <summary>
        /// PSDataStreams is the public interface to access the *Buffer properties in the PowerShell class
        /// </summary>
        internal PSDataStreams(PowerShell powershell)
        {
            this.powershell = powershell;
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
                return this.powershell.ErrorBuffer;
            }

            set
            {
                this.powershell.ErrorBuffer = value;
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
                return this.powershell.ProgressBuffer;
            }

            set
            {
                this.powershell.ProgressBuffer = value;
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
                return this.powershell.VerboseBuffer;
            }

            set
            {
                this.powershell.VerboseBuffer = value;
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
                return this.powershell.DebugBuffer;
            }

            set
            {
                this.powershell.DebugBuffer = value;
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
                return this.powershell.WarningBuffer;
            }

            set
            {
                this.powershell.WarningBuffer = value;
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
                return this.powershell.InformationBuffer;
            }

            set
            {
                this.powershell.InformationBuffer = value;
            }
        }

        /// <summary>
        /// Removes all items from all the data streams
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
        
        private PowerShell powershell;
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
        private PipelineBase pipeline;
        private PowerShell powerShell;
        private EventHandler<PipelineStateEventArgs> eventHandler;

        internal PowerShellStopper(ExecutionContext context, PowerShell powerShell)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (powerShell == null)
            {
                throw new ArgumentNullException("powerShell");
            }

            this.powerShell = powerShell;
            
            if ((context.CurrentCommandProcessor != null) &&
                (context.CurrentCommandProcessor.CommandRuntime != null) &&
                (context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor != null) &&
                (context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor.LocalPipeline != null))
            {
                this.eventHandler = new EventHandler<PipelineStateEventArgs>(LocalPipeline_StateChanged);
                this.pipeline = context.CurrentCommandProcessor.CommandRuntime.PipelineProcessor.LocalPipeline;
                this.pipeline.StateChanged += eventHandler;
            }
        }

        private void LocalPipeline_StateChanged(object sender, PipelineStateEventArgs e)
        {
            if ((e.PipelineStateInfo.State == PipelineState.Stopping) &&
                (this.powerShell.InvocationStateInfo.State == PSInvocationState.Running))
            {
                this.powerShell.Stop();
            }
        }

        private bool isDisposed;
    
        public void  Dispose()
        {
            if (!isDisposed)
            {
                if (this.eventHandler != null)
                {
                    pipeline.StateChanged -= this.eventHandler;
                    this.eventHandler = null;
                }

                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }
    }

}
