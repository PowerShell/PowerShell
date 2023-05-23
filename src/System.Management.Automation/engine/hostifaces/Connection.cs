// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    #region Exceptions

    /// <summary>
    /// Exception thrown when state of the runspace is different from
    /// expected state of runspace.
    /// </summary>
    public class InvalidRunspaceStateException : SystemException
    {
        /// <summary>
        /// Initializes a new instance of InvalidRunspaceStateException.
        /// </summary>
        public InvalidRunspaceStateException()
        : base
        (
            StringUtil.Format(RunspaceStrings.InvalidRunspaceStateGeneral)
        )
        {
        }

        /// <summary>
        /// Initializes a new instance of InvalidRunspaceStateException with a specified error message.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InvalidRunspaceStateException(string message)
        : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidRunspaceStateException class
        /// with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidRunspaceStateException(string message, Exception innerException)
        : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidRunspaceStateException
        /// with a specified error message and current and expected state.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="currentState">Current state of runspace.</param>
        /// <param name="expectedState">Expected states of runspace.</param>
        internal InvalidRunspaceStateException
        (
            string message,
            RunspaceState currentState,
            RunspaceState expectedState
        )
        : base(message)
        {
            _expectedState = expectedState;
            _currentState = currentState;
        }

        /// <summary>
        /// Access CurrentState of the runspace.
        /// </summary>
        /// <remarks>This is the state of the runspace when exception was thrown.
        /// </remarks>
        public RunspaceState CurrentState
        {
            get
            {
                return _currentState;
            }

            internal set
            {
                _currentState = value;
            }
        }

        /// <summary>
        /// Expected state of runspace by the operation which has thrown this exception.
        /// </summary>
        public RunspaceState ExpectedState
        {
            get
            {
                return _expectedState;
            }

            internal set
            {
                _expectedState = value;
            }
        }

        /// <summary>
        /// State of the runspace when exception was thrown.
        /// </summary>
        [NonSerialized]
        private RunspaceState _currentState = 0;

        /// <summary>
        /// States of the runspace expected in method which throws this exception.
        /// </summary>
        [NonSerialized]
        private RunspaceState _expectedState = 0;
    }

    #endregion Exceptions

    #region Runspace state

    /// <summary>
    /// Defines various states of runspace.
    /// </summary>
    public enum RunspaceState
    {
        /// <summary>
        /// Beginning state upon creation.
        /// </summary>
        BeforeOpen = 0,
        /// <summary>
        /// A runspace is being established.
        /// </summary>
        Opening = 1,
        /// <summary>
        /// The runspace is established and valid.
        /// </summary>
        Opened = 2,
        /// <summary>
        /// The runspace is closed or has not been established.
        /// </summary>
        Closed = 3,
        /// <summary>
        /// The runspace is being closed.
        /// </summary>
        Closing = 4,
        /// <summary>
        /// The runspace has been disconnected abnormally.
        /// </summary>
        Broken = 5,
        /// <summary>
        /// The runspace is being disconnected.
        /// </summary>
        Disconnecting = 6,
        /// <summary>
        /// The runspace is disconnected.
        /// </summary>
        Disconnected = 7,
        /// <summary>
        /// The runspace is Connecting.
        /// </summary>
        Connecting = 8
    }

    /// <summary>
    /// These options control whether a new thread is created when a command is executed within a runspace.
    /// </summary>
    public enum PSThreadOptions
    {
        /// <summary>
        /// Use the default options: UseNewThread for local Runspace, ReuseThread for local RunspacePool, server settings for remote Runspace and RunspacePool.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Creates a new thread for each invocation.
        /// </summary>
        UseNewThread = 1,

        /// <summary>
        /// Creates a new thread for the first invocation and then re-uses
        /// that thread in subsequent invocations.
        /// </summary>
        ReuseThread = 2,

        /// <summary>
        /// Doesn't create a new thread; the execution occurs on the
        /// thread that calls Invoke.
        /// </summary>
        /// <remarks>
        /// This option is not valid for asynchronous calls
        /// </remarks>
        UseCurrentThread = 3
    }

    /// <summary>
    /// Defines type which has information about RunspaceState and
    /// Exception associated with RunspaceState.
    /// </summary>
    public sealed class RunspaceStateInfo
    {
        #region constructors

        /// <summary>
        /// Constructor for state changes not resulting from an error.
        /// </summary>
        /// <param name="state">The state of the runspace.</param>
        internal RunspaceStateInfo(RunspaceState state)
            : this(state, null)
        {
        }

        /// <summary>
        /// Constructor for state changes with an optional error.
        /// </summary>
        /// <param name="state">The state of runspace.</param>
        /// <param name="reason">A non-null exception if the state change was
        /// caused by an error, otherwise; null.
        /// </param>
        internal RunspaceStateInfo(RunspaceState state, Exception reason)
            : base()
        {
            State = state;
            Reason = reason;
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="runspaceStateInfo">The source
        /// RunspaceStateInfo
        /// </param>
        internal RunspaceStateInfo(RunspaceStateInfo runspaceStateInfo)
        {
            State = runspaceStateInfo.State;
            Reason = runspaceStateInfo.Reason;
        }
        #endregion constructors

        #region public_properties

        /// <summary>
        /// The state of the runspace.
        /// </summary>
        public RunspaceState State { get; }

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
        /// Clones current object.
        /// </summary>
        /// <returns>Cloned object.</returns>
        internal RunspaceStateInfo Clone()
        {
            return new RunspaceStateInfo(this);
        }

        #region private_fields

        #endregion private_fields
    }

    /// <summary>
    /// Defines Event arguments passed to RunspaceStateEvent handler
    /// <see cref="Runspace.StateChanged"/> event.
    /// </summary>
    public sealed class RunspaceStateEventArgs : EventArgs
    {
        #region constructors

        /// <summary>
        /// Constructs RunspaceStateEventArgs using RunspaceStateInfo.
        /// </summary>
        /// <param name="runspaceStateInfo">The information about
        /// current state of the runspace.</param>
        /// <exception cref="ArgumentNullException">RunspaceStateInfo is null
        /// </exception>
        internal RunspaceStateEventArgs(RunspaceStateInfo runspaceStateInfo)
        {
            if (runspaceStateInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runspaceStateInfo));
            }

            RunspaceStateInfo = runspaceStateInfo;
        }

        #endregion constructors

        #region public_properties

        /// <summary>
        /// Information about state of the runspace.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the runspace after the
        /// change.
        /// </remarks>
        public RunspaceStateInfo RunspaceStateInfo { get; }

        #endregion public_properties
    }

    /// <summary>
    /// Enum to indicate whether a Runspace is busy or available.
    /// </summary>
    public enum RunspaceAvailability
    {
        /// <summary>
        /// The Runspace is not been in the Opened state.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Runspace is available to execute commands.
        /// </summary>
        Available,

        /// <summary>
        /// The Runspace is available to execute nested commands.
        /// </summary>
        AvailableForNestedCommand,

        /// <summary>
        /// The Runspace is busy executing a command.
        /// </summary>
        Busy,

        /// <summary>
        /// Applies only to remote runspace case.  The remote runspace
        /// is currently in a Debugger Stop mode and requires a debugger
        /// SetDebuggerAction() call to continue.
        /// </summary>
        RemoteDebug
    }

    /// <summary>
    /// Defines the event arguments passed to the AvailabilityChanged <see cref="Runspace.AvailabilityChanged"/> event.
    /// </summary>
    public sealed class RunspaceAvailabilityEventArgs : EventArgs
    {
        internal RunspaceAvailabilityEventArgs(RunspaceAvailability runspaceAvailability)
        {
            RunspaceAvailability = runspaceAvailability;
        }

        /// <summary>
        /// Whether the Runspace is available to execute commands.
        /// </summary>
        public RunspaceAvailability RunspaceAvailability { get; }
    }

    #endregion Runspace state

    #region Runspace capabilities

    /// <summary>
    /// Defines runspace capabilities.
    /// </summary>
    public enum RunspaceCapability
    {
        /// <summary>
        /// Legacy capabilities for WinRM only, from Win7 timeframe.
        /// </summary>
        Default = 0x0,

        /// <summary>
        /// Runspace and remoting layer supports disconnect/connect feature.
        /// </summary>
        SupportsDisconnect = 0x1,

        /// <summary>
        /// Runspace is based on a named pipe transport.
        /// </summary>
        NamedPipeTransport = 0x2,

        /// <summary>
        /// Runspace is based on a VM socket transport.
        /// </summary>
        VMSocketTransport = 0x4,

        /// <summary>
        /// Runspace is based on SSH transport.
        /// </summary>
        SSHTransport = 0x8,

        /// <summary>
        /// Runspace is based on open custom connection/transport support.
        /// </summary>
        CustomTransport = 0x100
    }

    #endregion

    /// <summary>
    /// Public interface to PowerShell Runtime. Provides APIs for creating pipelines,
    /// access session state etc.
    /// </summary>
    public abstract class Runspace : IDisposable
    {
        #region Private Data

        private static int s_globalId;
        private readonly Stack<PowerShell> _runningPowerShells;
        private PowerShell _baseRunningPowerShell;
        private readonly object _syncObject;

        #endregion

        #region constructor

        /// <summary>
        /// Explicit default constructor.
        /// </summary>
        internal Runspace()
        {
            // Create the default Runspace Id and friendly name.
            Id = System.Threading.Interlocked.Increment(ref s_globalId);
            Name = "Runspace" + Id.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            _runningPowerShells = new Stack<PowerShell>();
            _syncObject = new object();

            // Keep track of this runspace until it is disposed.
            lock (s_syncObject)
            {
                s_runspaceDictionary.Add(Id, new WeakReference<Runspace>(this));
            }
        }

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static Runspace()
        {
            s_syncObject = new object();
            s_runspaceDictionary = new SortedDictionary<int, WeakReference<Runspace>>();
            s_globalId = 0;
        }

        #endregion constructor

        #region properties

        /// <summary>
        /// Used to store Runspace reference on per thread basis. Used by
        /// various PowerShell engine features to get access to TypeTable
        /// etc.
        /// </summary>
        [ThreadStatic]
        private static Runspace t_threadSpecificDefaultRunspace = null;
        /// <summary>
        /// Gets and sets the default Runspace used to evaluate scripts.
        /// </summary>
        /// <remarks>The Runspace used to set this property should not be shared between different threads.</remarks>
        public static Runspace DefaultRunspace
        {
            get
            {
                return t_threadSpecificDefaultRunspace;
            }

            set
            {
                if (value == null || !value.RunspaceIsRemote)
                {
                    t_threadSpecificDefaultRunspace = value;
                }
                else
                {
                    throw new InvalidOperationException(RunspaceStrings.RunspaceNotLocal);
                }
            }
        }

        /// <summary>
        /// A PrimaryRunspace is a runspace that persists for the entire lifetime of the PowerShell session. It is only
        /// closed or disposed when the session is ending.  So when the PrimaryRunspace is closing it will trigger on-exit
        /// cleanup that includes closing any other local runspaces left open, and will allow the process to exit.
        /// </summary>
        internal static Runspace PrimaryRunspace
        {
            get
            {
                return s_primaryRunspace;
            }

            set
            {
                var result = Interlocked.CompareExchange<Runspace>(ref s_primaryRunspace, value, null);
                if (result != null)
                {
                    throw new PSInvalidOperationException(RunspaceStrings.PrimaryRunspaceAlreadySet);
                }
            }
        }

        private static Runspace s_primaryRunspace;

        /// <summary>
        /// Returns true if Runspace.DefaultRunspace can be used to
        /// create an instance of the PowerShell class with
        /// 'UseCurrentRunspace = true'.
        /// </summary>
        public static bool CanUseDefaultRunspace
        {
            // can use default runspace in a thread safe manner only if
            // 1. we have a default runspace
            // 2. we recognize the type of current runspace and current pipeline
            // 3. the pipeline executes on the same thread as this method

            // we don't return "true" for
            // 1. we have a default runspace
            // 2. no currently executing pipeline
            // to avoid a race condition where a pipeline is started
            // after this property getter did all the checks

            get
            {
                RunspaceBase runspace = Runspace.DefaultRunspace as RunspaceBase;
                if (runspace != null)
                {
                    Pipeline currentPipeline = runspace.GetCurrentlyRunningPipeline();
                    LocalPipeline localPipeline = currentPipeline as LocalPipeline;
                    if ((localPipeline != null) && (localPipeline.NestedPipelineExecutionThread != null))
                    {
                        return
                            (localPipeline.NestedPipelineExecutionThread.ManagedThreadId
                            == Environment.CurrentManagedThreadId);
                    }
                }

                return false;
            }
        }

        internal const ApartmentState DefaultApartmentState = ApartmentState.Unknown;

        /// <summary>
        /// ApartmentState of the thread used to execute commands within this Runspace.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the Runspace is opened
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// An attempt to change this property was made after opening the Runspace
        /// </exception>
        public ApartmentState ApartmentState
        {
            get
            {
                return this.apartmentState;
            }

            set
            {
                if (this.RunspaceStateInfo.State != RunspaceState.BeforeOpen)
                {
                    throw new InvalidRunspaceStateException(StringUtil.Format(RunspaceStrings.ChangePropertyAfterOpen));
                }

                this.apartmentState = value;
            }
        }

        private ApartmentState apartmentState = Runspace.DefaultApartmentState;

        /// <summary>
        /// This property determines whether a new thread is create for each invocation.
        /// </summary>
        /// <remarks>
        /// Any updates to the value of this property must be done before the Runspace is opened
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// An attempt to change this property was made after opening the Runspace
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The thread options cannot be changed to the requested value
        /// </exception>
        public abstract PSThreadOptions ThreadOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Return version of this runspace.
        /// </summary>
        public abstract Version Version
        {
            get;
        }

        /// <summary>
        /// Return whether the Runspace is Remote
        /// We can determine this by whether the runspace is an implementation of LocalRunspace
        /// or infer it from whether the ConnectionInfo property is null
        /// If it happens to be an instance of a LocalRunspace, but has a non-null ConnectionInfo
        /// we declare it to be remote.
        /// </summary>
        public bool RunspaceIsRemote
        {
            get
            {
                return this is not LocalRunspace && ConnectionInfo != null;
            }
        }

        /// <summary>
        /// Retrieve information about current state of the runspace.
        /// </summary>
        public abstract RunspaceStateInfo RunspaceStateInfo
        {
            get;
        }

        /// <summary>
        /// Gets the current availability of the Runspace.
        /// </summary>
        public abstract RunspaceAvailability RunspaceAvailability
        {
            get;
            protected set;
        }

        /// <summary>
        /// InitialSessionState information for this runspace.
        /// </summary>
        public abstract InitialSessionState InitialSessionState
        {
            get;
        }

        /// <summary>
        /// Get unique id for this instance of runspace. It is primarily used
        /// for logging purposes.
        /// </summary>
        public Guid InstanceId
        {
            get;

            // This id is also used to identify proxy and remote runspace objects.
            // We need to set this when reconstructing a remote runspace to connect
            // to an existing remote runspace.
            internal set;
        } = Guid.NewGuid();

        /// <summary>
        /// Gets execution context.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">Runspace is not opened.
        /// </exception>
        internal ExecutionContext ExecutionContext
        {
            get
            {
                return GetExecutionContext;
            }
        }

        /// <summary>
        /// Skip user profile on engine initialization.
        /// </summary>
        internal bool SkipUserProfile { get; set; } = false;

        /// <summary>
        /// Connection information for remote Runspaces, null for local Runspaces.
        /// </summary>
        public abstract RunspaceConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// ConnectionInfo originally supplied by the user.
        /// </summary>
        public abstract RunspaceConnectionInfo OriginalConnectionInfo { get; }

        /// <summary>
        /// Manager for JobSourceAdapters registered in this runspace.
        /// </summary>
        public abstract JobManager JobManager { get; }

        /// <summary>
        /// DisconnectedOn property applies to remote runspaces that have
        /// been disconnected.
        /// </summary>
        public DateTime? DisconnectedOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// ExpiresOn property applies to remote runspaces that have been
        /// disconnected.
        /// </summary>
        public DateTime? ExpiresOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets and sets a friendly name for the Runspace.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the Runspace Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Returns protocol version that the remote server uses for PS remoting.
        /// </summary>
        internal Version GetRemoteProtocolVersion()
        {
            Version remoteProtocolVersionDeclaredByServer;
            bool isServerDeclarationValid = PSPrimitiveDictionary.TryPathGet(
                this.GetApplicationPrivateData(),
                out remoteProtocolVersionDeclaredByServer,
                PSVersionInfo.PSVersionTableName,
                PSVersionInfo.PSRemotingProtocolVersionName);

            if (isServerDeclarationValid)
            {
                return remoteProtocolVersionDeclaredByServer;
            }
            else
            {
                return RemotingConstants.ProtocolVersion;
            }
        }

        /// <summary>
        /// Engine activity id (for ETW tracing)
        /// </summary>
        internal Guid EngineActivityId { get; set; } = Guid.Empty;

        /// <summary>
        /// Returns a read only runspace dictionary.
        /// </summary>
        internal static ReadOnlyDictionary<int, WeakReference<Runspace>> RunspaceDictionary
        {
            get
            {
                lock (s_syncObject)
                {
                    return new ReadOnlyDictionary<int, WeakReference<Runspace>>(new Dictionary<int, WeakReference<Runspace>>(s_runspaceDictionary));
                }
            }
        }

        private static readonly SortedDictionary<int, WeakReference<Runspace>> s_runspaceDictionary;
        private static readonly object s_syncObject;

        /// <summary>
        /// Returns a read only list of runspaces.
        /// </summary>
        internal static IReadOnlyList<Runspace> RunspaceList
        {
            get
            {
                List<Runspace> runspaceList = new List<Runspace>();

                lock (s_syncObject)
                {
                    foreach (var item in s_runspaceDictionary.Values)
                    {
                        Runspace runspace;
                        if (item.TryGetTarget(out runspace))
                        {
                            runspaceList.Add(runspace);
                        }
                    }
                }

                return new ReadOnlyCollection<Runspace>(runspaceList);
            }
        }

        #endregion properties

        #region events

        /// <summary>
        /// Event raised when RunspaceState changes.
        /// </summary>
        public abstract event EventHandler<RunspaceStateEventArgs> StateChanged;

        /// <summary>
        /// Event raised when the availability of the Runspace changes.
        /// </summary>
        public abstract event EventHandler<RunspaceAvailabilityEventArgs> AvailabilityChanged;

        /// <summary>
        /// Returns true if there are any subscribers to the AvailabilityChanged event.
        /// </summary>
        internal abstract bool HasAvailabilityChangedSubscribers
        {
            get;
        }

        /// <summary>
        /// Raises the AvailabilityChanged event.
        /// </summary>
        protected abstract void OnAvailabilityChanged(RunspaceAvailabilityEventArgs e);

        /// <summary>
        /// Used to raise the AvailabilityChanged event when the state of the currently executing pipeline changes.
        /// </summary>
        /// <remarks>
        /// The possible pipeline states are
        ///     NotStarted
        ///     Running
        ///     Disconnected
        ///     Stopping
        ///     Stopped
        ///     Completed
        ///     Failed
        /// </remarks>
        internal void UpdateRunspaceAvailability(PipelineState pipelineState, bool raiseEvent, Guid? cmdInstanceId = null)
        {
            RunspaceAvailability oldAvailability = this.RunspaceAvailability;

            switch (oldAvailability)
            {
                // Because of disconnect/connect support runspace availability can now transition
                // in and out of "None" state.
                case RunspaceAvailability.None:
                    switch (pipelineState)
                    {
                        case PipelineState.Running:
                            this.RunspaceAvailability = RunspaceAvailability.Busy;
                            break;

                            // Otherwise no change.
                    }

                    break;

                case RunspaceAvailability.Available:
                    switch (pipelineState)
                    {
                        case PipelineState.Running:
                            this.RunspaceAvailability = RunspaceAvailability.Busy;
                            break;

                        case PipelineState.Disconnected:
                            this.RunspaceAvailability = Runspaces.RunspaceAvailability.None;
                            break;
                    }

                    break;

                case RunspaceAvailability.AvailableForNestedCommand:
                    switch (pipelineState)
                    {
                        case PipelineState.Running:
                            this.RunspaceAvailability = RunspaceAvailability.Busy;
                            break;

                        case PipelineState.Completed: // a nested pipeline caused the host to exit nested prompt
                            this.RunspaceAvailability = (this.InNestedPrompt || (_runningPowerShells.Count > 1)) ?
                                RunspaceAvailability.AvailableForNestedCommand : RunspaceAvailability.Available;
                            break;

                        default:
                            break; // no change in the availability
                    }

                    break;

                case RunspaceAvailability.Busy:
                case RunspaceAvailability.RemoteDebug:
                    switch (pipelineState)
                    {
                        case PipelineState.Disconnected:
                            if (oldAvailability == Runspaces.RunspaceAvailability.RemoteDebug)
                            {
                                this.RunspaceAvailability = RunspaceAvailability.RemoteDebug;
                            }
                            else
                            {
                                this.RunspaceAvailability = RunspaceAvailability.None;
                            }

                            break;

                        case PipelineState.Stopping:
                            break; // no change in the availability

                        case PipelineState.Completed:
                        case PipelineState.Stopped:
                        case PipelineState.Failed:
                            if (this.InNestedPrompt
                                || (this is not RemoteRunspace && this.Debugger.InBreakpoint))
                            {
                                this.RunspaceAvailability = RunspaceAvailability.AvailableForNestedCommand;
                            }
                            else
                            {
                                RemoteRunspace remoteRunspace = this as RemoteRunspace;
                                RemoteDebugger remoteDebugger = (remoteRunspace != null) ? remoteRunspace.Debugger as RemoteDebugger : null;
                                Internal.ConnectCommandInfo remoteCommand = remoteRunspace?.RemoteCommand;
                                if (((pipelineState == PipelineState.Completed) || (pipelineState == PipelineState.Failed) ||
                                    ((pipelineState == PipelineState.Stopped) && (this.RunspaceStateInfo.State == RunspaceState.Opened)))
                                    && (remoteCommand != null) && (cmdInstanceId != null) && (remoteCommand.CommandId == cmdInstanceId))
                                {
                                    // Completed, Failed, and Stopped with Runspace.Opened states are command finish states and we know
                                    // that the command is finished on the server.
                                    // Setting ConnectCommands to null indicates that the runspace is free to run other
                                    // commands.
                                    remoteRunspace.RunspacePool.RemoteRunspacePoolInternal.ConnectCommands = null;
                                    remoteCommand = null;

                                    if ((remoteDebugger != null) && (pipelineState == PipelineState.Stopped))
                                    {
                                        // Notify remote debugger of a stop in case the stop occurred while command was in debug stop.
                                        remoteDebugger.OnCommandStopped();
                                    }
                                }

                                Pipeline currentPipeline = this.GetCurrentlyRunningPipeline();
                                RemotePipeline remotePipeline = currentPipeline as RemotePipeline;
                                Guid? pipeLineCmdInstance = (remotePipeline != null && remotePipeline.PowerShell != null) ? remotePipeline.PowerShell.InstanceId : (Guid?)null;
                                if (currentPipeline == null)
                                {
                                    // A runspace is available:
                                    //  - if there is no currently running pipeline
                                    //    and for remote runspaces:
                                    //    - if there is no remote command associated with it.
                                    //    - if the remote runspace pool is marked as available for connection.
                                    if (remoteCommand == null)
                                    {
                                        if (remoteRunspace != null)
                                        {
                                            if ((remoteDebugger != null) && (pipelineState == PipelineState.Stopped))
                                            {
                                                // Notify remote debugger of a stop in case the stop occurred while command was in debug stop.
                                                remoteDebugger.OnCommandStopped();
                                            }

                                            this.RunspaceAvailability =
                                                remoteRunspace.RunspacePool.RemoteRunspacePoolInternal.AvailableForConnection ?
                                                RunspaceAvailability.Available : Runspaces.RunspaceAvailability.Busy;
                                        }
                                        else
                                        {
                                            this.RunspaceAvailability = RunspaceAvailability.Available;
                                        }
                                    }
                                }
                                else if ((cmdInstanceId != null) && (pipeLineCmdInstance != null) && (cmdInstanceId == pipeLineCmdInstance))
                                {
                                    if ((remoteDebugger != null) && (pipelineState == PipelineState.Stopped))
                                    {
                                        // Notify remote debugger of a stop in case the stop occurred while command was in debug stop.
                                        remoteDebugger.OnCommandStopped();
                                    }

                                    this.RunspaceAvailability = RunspaceAvailability.Available;
                                }
                                else // a nested pipeline completed, but the parent pipeline is still running
                                {
                                    if (oldAvailability == Runspaces.RunspaceAvailability.RemoteDebug)
                                    {
                                        this.RunspaceAvailability = Runspaces.RunspaceAvailability.RemoteDebug;
                                    }
                                    else if ((currentPipeline.PipelineStateInfo.State == PipelineState.Running) || (_runningPowerShells.Count > 1))
                                    {
                                        // Either the current pipeline is running or there are other nested commands to run in the Runspace.
                                        this.RunspaceAvailability = RunspaceAvailability.Busy;
                                    }
                                    else
                                    {
                                        this.RunspaceAvailability = RunspaceAvailability.Available;
                                    }
                                }
                            }

                            break;

                        case PipelineState.Running: // this can happen if a nested pipeline is created without entering a nested prompt
                            break; // no change in the availability

                        default:
                            break; // no change in the availability
                    }

                    break;

                default:
                    Diagnostics.Assert(false, "Invalid RunspaceAvailability");
                    break;
            }

            if (raiseEvent && this.RunspaceAvailability != oldAvailability)
            {
                OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(this.RunspaceAvailability));
            }
        }

        /// <summary>
        /// Used to update the runspace availability when the state of the currently executing PowerShell instance changes.
        /// </summary>
        /// <remarks>
        /// The possible invocation states are
        ///     NotStarted
        ///     Running
        ///     Stopping
        ///     Stopped
        ///     Completed
        ///     Failed
        /// </remarks>
        internal void UpdateRunspaceAvailability(PSInvocationState invocationState, bool raiseEvent, Guid cmdInstanceId)
        {
            switch (invocationState)
            {
                case PSInvocationState.NotStarted:
                    UpdateRunspaceAvailability(PipelineState.NotStarted, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Running:
                    UpdateRunspaceAvailability(PipelineState.Running, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Completed:
                    UpdateRunspaceAvailability(PipelineState.Completed, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Failed:
                    UpdateRunspaceAvailability(PipelineState.Failed, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Stopping:
                    UpdateRunspaceAvailability(PipelineState.Stopping, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Stopped:
                    UpdateRunspaceAvailability(PipelineState.Stopped, raiseEvent, cmdInstanceId);
                    break;

                case PSInvocationState.Disconnected:
                    UpdateRunspaceAvailability(PipelineState.Disconnected, raiseEvent, cmdInstanceId);
                    break;

                default:
                    Diagnostics.Assert(false, "Invalid PSInvocationState");
                    break;
            }
        }

        /// <summary>
        /// Used to update the runspace availability event when the state of the runspace changes.
        /// </summary>
        /// <remarks>
        /// The possible runspace states are:
        ///     BeforeOpen
        ///     Opening
        ///     Opened
        ///     Closed
        ///     Closing
        ///     Broken
        /// </remarks>
        protected void UpdateRunspaceAvailability(RunspaceState runspaceState, bool raiseEvent)
        {
            RunspaceAvailability oldAvailability = this.RunspaceAvailability;
            RemoteRunspace remoteRunspace = this as RemoteRunspace;
            Internal.ConnectCommandInfo remoteCommand = null;
            bool remoteDebug = false;

            if (remoteRunspace != null)
            {
                remoteCommand = remoteRunspace.RemoteCommand;
                RemoteDebugger remoteDebugger = remoteRunspace.Debugger as RemoteDebugger;
                remoteDebug = (remoteDebugger != null) && remoteDebugger.IsRemoteDebug;
            }

            switch (oldAvailability)
            {
                case RunspaceAvailability.None:
                    switch (runspaceState)
                    {
                        case RunspaceState.Opened:
                            if (remoteDebug)
                            {
                                this.RunspaceAvailability = Runspaces.RunspaceAvailability.RemoteDebug;
                            }
                            else
                            {
                                this.RunspaceAvailability = (remoteCommand == null && GetCurrentlyRunningPipeline() == null) ?
                                    RunspaceAvailability.Available : RunspaceAvailability.Busy;
                            }

                            break;

                        default:
                            break; // no change in the availability
                    }

                    break;

                case RunspaceAvailability.Available:
                case RunspaceAvailability.AvailableForNestedCommand:
                case RunspaceAvailability.RemoteDebug:
                case RunspaceAvailability.Busy:
                    switch (runspaceState)
                    {
                        case RunspaceState.Closing:
                        case RunspaceState.Closed:
                        case RunspaceState.Broken:
                        case RunspaceState.Disconnected:
                            this.RunspaceAvailability = RunspaceAvailability.None;
                            break;

                        default:
                            break; // no change in the availability
                    }

                    break;

                default:
                    Diagnostics.Assert(false, "Invalid RunspaceAvailability");
                    break;
            }

            if (raiseEvent && this.RunspaceAvailability != oldAvailability)
            {
                OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(this.RunspaceAvailability));
            }
        }

        /// <summary>
        /// Used to update the runspace availability from Enter/ExitNestedPrompt and the debugger.
        /// </summary>
        internal void UpdateRunspaceAvailability(RunspaceAvailability availability, bool raiseEvent)
        {
            RunspaceAvailability oldAvailability = this.RunspaceAvailability;

            this.RunspaceAvailability = availability;

            if (raiseEvent && this.RunspaceAvailability != oldAvailability)
            {
                OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(this.RunspaceAvailability));
            }
        }

        /// <summary>
        /// Raises the AvailabilityChanged event.
        /// </summary>
        internal void RaiseAvailabilityChangedEvent(RunspaceAvailability availability)
        {
            OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(availability));
        }

        #endregion events

        #region Public static methods

        /// <summary>
        /// Queries the server for disconnected runspaces and creates an array of runspace
        /// objects associated with each disconnected runspace on the server.  Each
        /// runspace object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <returns>Array of Runspace objects each in the Disconnected state.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo)
        {
            return GetRunspaces(connectionInfo, null, null);
        }

        /// <summary>
        /// Queries the server for disconnected runspaces and creates an array of runspace
        /// objects associated with each disconnected runspace on the server.  Each
        /// runspace object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <returns>Array of Runspace objects each in the Disconnected state.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo, PSHost host)
        {
            return GetRunspaces(connectionInfo, host, null);
        }

        /// <summary>
        /// Queries the server for disconnected runspaces and creates an array of runspace
        /// objects associated with each disconnected runspace on the server.  Each
        /// runspace object in the returned array is in the Disconnected state and can be
        /// connected to the server by calling the Connect() method on the runspace.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <param name="typeTable">TypeTable object.</param>
        /// <returns>Array of Runspace objects each in the Disconnected state.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            return RemoteRunspace.GetRemoteRunspaces(connectionInfo, host, typeTable);
        }

        /// <summary>
        /// Returns a single disconnected Runspace object targeted to the remote computer and remote
        /// session as specified by the connection, session Id, and command Id parameters.
        /// </summary>
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="sessionId">Id of a disconnected remote session on the target server.</param>
        /// <param name="commandId">Optional Id of a disconnected command running in the disconnected remote session on the target server.</param>
        /// <param name="host">Optional client host object.</param>
        /// <param name="typeTable">Optional TypeTable object.</param>
        /// <returns>Disconnected runspace corresponding to the provided session Id.</returns>
        public static Runspace GetRunspace(RunspaceConnectionInfo connectionInfo, Guid sessionId, Guid? commandId, PSHost host, TypeTable typeTable)
        {
            return RemoteRunspace.GetRemoteRunspace(connectionInfo, sessionId, commandId, host, typeTable);
        }

        #endregion

        #region public Disconnect-Connect methods

        /// <summary>
        /// Disconnects the runspace synchronously.
        /// </summary>
        /// <remarks>
        /// Disconnects the remote runspace and any running command from the server
        /// machine.  Any data generated by the running command on the server is
        /// cached on the server machine.  This runspace object goes to the disconnected
        /// state.  This object can be reconnected to the server by calling the
        /// Connect() method.
        /// If the remote runspace on the server remains disconnected for the IdleTimeout
        /// value (as defined in the WSManConnectionInfo object) then it is closed and
        /// torn down on the server.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Opened.
        /// </exception>
        public abstract void Disconnect();

        /// <summary>
        /// Disconnects the runspace asynchronously.
        /// </summary>
        /// <remarks>
        /// Disconnects the remote runspace and any running command from the server
        /// machine.  Any data generated by the running command on the server is
        /// cached on the server machine.  This runspace object goes to the disconnected
        /// state.  This object can be reconnected to the server by calling the
        /// Connect() method.
        /// If the remote runspace on the server remains disconnected for the IdleTimeout
        /// value (as defined in the WSManConnectionInfo object) then it is closed and
        /// torn down on the server.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Opened.
        /// </exception>
        public abstract void DisconnectAsync();

        /// <summary>
        /// Connects the runspace to its remote counterpart synchronously.
        /// </summary>
        /// <remarks>
        /// Connects the runspace object to its corresponding runspace on the target
        /// server machine.  The target server machine is identified by the connection
        /// object passed in during construction.  The remote runspace is identified
        /// by the internal runspace Guid value.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Disconnected.
        /// </exception>
        public abstract void Connect();

        /// <summary>
        /// Connects a runspace to its remote counterpart asynchronously.
        /// </summary>
        /// <remarks>
        /// Connects the runspace object to its corresponding runspace on the target
        /// server machine.  The target server machine is identified by the connection
        /// object passed in during construction.  The remote runspace is identified
        /// by the internal runspace Guid value.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Disconnected.
        /// </exception>
        public abstract void ConnectAsync();

        /// <summary>
        /// Creates a PipeLine object in the disconnected state for the currently disconnected
        /// remote running command associated with this runspace.
        /// </summary>
        /// <returns>Pipeline object in disconnected state.</returns>
        public abstract Pipeline CreateDisconnectedPipeline();

        /// <summary>
        /// Creates a PowerShell object in the disconnected state for the currently disconnected
        /// remote running command associated with this runspace.
        /// </summary>
        /// <returns>PowerShell object in disconnected state.</returns>
        public abstract PowerShell CreateDisconnectedPowerShell();

        /// <summary>
        /// Returns Runspace capabilities.
        /// </summary>
        /// <returns>RunspaceCapability.</returns>
        public abstract RunspaceCapability GetCapabilities();

        #endregion

        #region methods

        /// <summary>
        /// Opens the runspace synchronously. Runspace must be opened before it can be used.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        public abstract void Open();

        /// <summary>
        /// Open the runspace Asynchronously.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        public abstract void OpenAsync();

        /// <summary>
        /// Close the runspace synchronously.
        /// </summary>
        /// <remarks>
        /// Attempts to execute pipelines after a call to close will fail.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is BeforeOpen or Opening
        /// </exception>
        public abstract void Close();

        /// <summary>
        /// Close the runspace Asynchronously.
        /// </summary>
        /// <remarks>
        /// Attempts to execute pipelines after a call to
        /// close will fail.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is BeforeOpen or Opening
        /// </exception>
        public abstract void CloseAsync();

        /// <summary>
        /// Create an empty pipeline.
        /// </summary>
        /// <returns>An empty pipeline.</returns>
        public abstract Pipeline CreatePipeline();

        /// <summary>
        /// Creates a pipeline for specified command string.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <returns>
        /// A pipeline pre-filled with a <see cref="Command"/> object for specified command parameter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public abstract Pipeline CreatePipeline(string command);

        /// <summary>
        /// Create a pipeline from a command string.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <param name="addToHistory">If true command is added to history.</param>
        /// <returns>
        /// A pipeline pre-filled with a <see cref="Command"/> object for specified command parameter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public abstract Pipeline CreatePipeline(string command, bool addToHistory);

        /// <summary>
        /// Creates a nested pipeline.
        /// </summary>
        /// <remarks>
        /// Nested pipelines are needed for nested prompt scenario. Nested
        /// prompt requires that we execute new pipelines( child pipelines)
        /// while current pipeline (lets call it parent pipeline) is blocked.
        /// </remarks>
        public abstract Pipeline CreateNestedPipeline();

        /// <summary>
        /// Creates a nested pipeline.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <param name="addToHistory">If true command is added to history.</param>
        /// <returns>
        /// A pipeline pre-filled with Command specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public abstract Pipeline CreateNestedPipeline(string command, bool addToHistory);

        /// <summary>
        /// Returns the currently executing pipeline,  or null if no pipeline is executing.
        /// </summary>
        internal abstract Pipeline GetCurrentlyRunningPipeline();

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        ///
        /// Local runspace is created with application private data set to an empty <see cref="PSPrimitiveDictionary"/>.
        ///
        /// Remote runspace gets its application private data from the server (set when creating a remote runspace pool)
        /// Calling this method on a remote runspace will block until the data is received from the server.
        /// The server will send application private data before reaching <see cref="RunspacePoolState.Opened"/> state.
        ///
        /// Runspaces that are part of a <see cref="RunspacePool"/> inherit application private data from the pool.
        /// </summary>
        public abstract PSPrimitiveDictionary GetApplicationPrivateData();

        /// <summary>
        /// A method that runspace pools can use to propagate application private data into runspaces.
        /// </summary>
        /// <param name="applicationPrivateData"></param>
        internal abstract void SetApplicationPrivateData(PSPrimitiveDictionary applicationPrivateData);

        /// <summary>
        /// Push a running PowerShell onto the stack.
        /// </summary>
        /// <param name="ps">PowerShell.</param>
        internal void PushRunningPowerShell(PowerShell ps)
        {
            Dbg.Assert(ps != null, "Caller should not pass in null reference.");

            lock (_syncObject)
            {
                _runningPowerShells.Push(ps);

                if (_runningPowerShells.Count == 1)
                {
                    _baseRunningPowerShell = ps;
                }
            }
        }

        /// <summary>
        /// Pop the currently running PowerShell from stack.
        /// </summary>
        /// <returns>PowerShell.</returns>
        internal PowerShell PopRunningPowerShell()
        {
            lock (_syncObject)
            {
                int count = _runningPowerShells.Count;

                if (count > 0)
                {
                    if (count == 1)
                    {
                        _baseRunningPowerShell = null;
                    }

                    return _runningPowerShells.Pop();
                }
            }

            return null;
        }

        internal PowerShell GetCurrentBasePowerShell()
        {
            return _baseRunningPowerShell;
        }

        #endregion methods

        #region SessionStateProxy

        /// <summary>
        /// Gets session state proxy.
        /// </summary>
        public SessionStateProxy SessionStateProxy
        {
            get
            {
                return GetSessionStateProxy();
            }
        }

        internal abstract SessionStateProxy GetSessionStateProxy();

        #endregion SessionStateProxy

        #region IDisposable Members

        /// <summary>
        /// Disposes this runspace instance. Dispose will close the runspace if not closed already.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose which can be overridden by derived classes.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            lock (s_syncObject)
            {
                s_runspaceDictionary.Remove(Id);
            }
        }

        #endregion IDisposable Members

        /// <summary>
        /// Gets the execution context.
        /// </summary>
        internal abstract ExecutionContext GetExecutionContext
        {
            get;
        }

        /// <summary>
        /// Returns true if the internal host is in a nested prompt.
        /// </summary>
        internal abstract bool InNestedPrompt
        {
            get;
        }

        /// <summary>
        /// Gets the debugger.
        /// </summary>
        public virtual Debugger Debugger
        {
            get
            {
                var context = GetExecutionContext;
                return context?.Debugger;
            }
        }

        /// <summary>
        /// InternalDebugger.
        /// </summary>
        internal Debugger InternalDebugger
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the event manager.
        /// </summary>
        public abstract PSEventManager Events
        {
            get;
        }

#if !CORECLR // Transaction Not Supported On CSS
        /// <summary>
        /// Sets the base transaction for the runspace; any transactions created on this runspace will be nested to this instance.
        /// </summary>
        /// <param name="transaction">The base transaction</param>
        /// <remarks>This overload uses RollbackSeverity.Error; i.e. the transaction will be rolled back automatically on a non-terminating error or worse</remarks>
        public void SetBaseTransaction(System.Transactions.CommittableTransaction transaction)
        {
            this.ExecutionContext.TransactionManager.SetBaseTransaction(transaction, RollbackSeverity.Error);
        }

        /// <summary>
        /// Sets the base transaction for the runspace; any transactions created on this runspace will be nested to this instance.
        /// </summary>
        /// <param name="transaction">The base transaction</param>
        /// <param name="severity">The severity of error that causes PowerShell to automatically rollback the transaction</param>
        public void SetBaseTransaction(System.Transactions.CommittableTransaction transaction, RollbackSeverity severity)
        {
            this.ExecutionContext.TransactionManager.SetBaseTransaction(transaction, severity);
        }

        /// <summary>
        /// Clears the transaction set by SetBaseTransaction()
        /// </summary>
        public void ClearBaseTransaction()
        {
            this.ExecutionContext.TransactionManager.ClearBaseTransaction();
        }
#endif

        /// <summary>
        /// Resets the variable table for the runspace to the default state.
        /// </summary>
        public virtual void ResetRunspaceState()
        {
            throw new NotImplementedException("ResetRunspaceState");
        }

        // Used for pipeline id generation.
        private long _pipelineIdSeed;

        // Generate pipeline id unique to this runspace
        internal long GeneratePipelineId()
        {
            return System.Threading.Interlocked.Increment(ref _pipelineIdSeed);
        }
    }

    /// <summary>
    /// This class provides subset of functionality provided by
    /// session state.
    /// </summary>
    public class SessionStateProxy
    {
        internal SessionStateProxy()
        {
        }

        private readonly RunspaceBase _runspace;

        internal SessionStateProxy(RunspaceBase runspace)
        {
            Dbg.Assert(runspace != null, "Caller should validate the parameter");
            _runspace = runspace;
        }

        /// <summary>
        /// Set a variable in session state.
        /// </summary>
        /// <param name="name">
        /// The name of the item to set.
        /// </param>
        /// <param name="value">
        /// The new value of the item being set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// name is null
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual void SetVariable(string name, object value)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            _runspace.SetVariable(name, value);
        }

        /// <summary>
        /// Get a variable out of session state.
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// name is null
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual object GetVariable(string name)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (name.Equals(string.Empty))
            {
                return null;
            }

            return _runspace.GetVariable(name);
        }

        /// <summary>
        /// Get the list of applications out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual List<string> Applications
        {
            get
            {
                return _runspace.Applications;
            }
        }

        /// <summary>
        /// Get the list of scripts out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual List<string> Scripts
        {
            get
            {
                return _runspace.Scripts;
            }
        }

        /// <summary>
        /// Get the APIs to access drives out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual DriveManagementIntrinsics Drive
        {
            get { return _runspace.Drive; }
        }

        /// <summary>
        /// Get/Set the language mode out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual PSLanguageMode LanguageMode
        {
            get { return _runspace.LanguageMode; }

            set { _runspace.LanguageMode = value; }
        }

        /// <summary>
        /// Get the module info out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "Shipped this way in V2 before becoming virtual.")]
        public virtual PSModuleInfo Module
        {
            get { return _runspace.Module; }
        }

        /// <summary>
        /// Get the APIs to access paths and locations out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual PathIntrinsics Path
        {
            get { return _runspace.PathIntrinsics; }
        }

        /// <summary>
        /// Get the APIs to access a provider out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual CmdletProviderManagementIntrinsics Provider
        {
            get { return _runspace.Provider; }
        }

        /// <summary>
        /// Get the APIs to access variables out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual PSVariableIntrinsics PSVariable
        {
            get { return _runspace.PSVariable; }
        }

        /// <summary>
        /// Get the APIs to build script blocks and execute script out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual CommandInvocationIntrinsics InvokeCommand
        {
            get { return _runspace.InvokeCommand; }
        }

        /// <summary>
        /// Gets the instance of the provider interface APIs out of session state.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// Runspace is not open.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Another SessionStateProxy call or another pipeline is in progress.
        /// </exception>
        public virtual ProviderIntrinsics InvokeProvider
        {
            get { return _runspace.InvokeProvider; }
        }
    }
}
