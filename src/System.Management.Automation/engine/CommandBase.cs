// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Defines members used by Cmdlets.
    /// All Cmdlets must derive from
    /// <see cref="System.Management.Automation.Cmdlet"/>.
    /// </summary>
    /// <remarks>
    /// Only use <see cref="System.Management.Automation.Internal.InternalCommand"/>
    /// as a subclass of
    /// <see cref="System.Management.Automation.Cmdlet"/>.
    /// Do not attempt to create instances of
    /// <see cref="System.Management.Automation.Internal.InternalCommand"/>
    /// independently, or to derive other classes than
    /// <see cref="System.Management.Automation.Cmdlet"/> from
    /// <see cref="System.Management.Automation.Internal.InternalCommand"/>.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Cmdlet"/>
    /// <!--
    /// These are the Cmdlet members which are also used by other
    /// non-public command types.
    ///
    /// Ideally this would be an internal class, but C# does not support
    /// public classes deriving from internal classes.
    /// -->
    [DebuggerDisplay("Command = {_commandInfo}")]
    public abstract class InternalCommand
    {
        #region private_members

        internal ICommandRuntime commandRuntime;

        #endregion private_members

        #region ctor

        /// <summary>
        /// Initializes the new instance of Cmdlet class.
        /// </summary>
        /// <remarks>
        /// The only constructor is internal, so outside users cannot create
        /// an instance of this class.
        /// </remarks>
        internal InternalCommand()
        {
            this.CommandInfo = null;
        }

        #endregion ctor

        #region internal_members

        /// <summary>
        /// Allows you to access the calling token for this command invocation...
        /// </summary>
        /// <value></value>
        internal IScriptExtent InvocationExtent { get; set; }

        private InvocationInfo _myInvocation = null;
        /// <summary>
        /// Return the invocation data object for this command.
        /// </summary>
        /// <value>The invocation object for this command.</value>
        internal InvocationInfo MyInvocation
        {
            get { return _myInvocation ?? (_myInvocation = new InvocationInfo(this)); }
        }

        /// <summary>
        /// Represents the current pipeline object under consideration.
        /// </summary>
        internal PSObject currentObjectInPipeline = AutomationNull.Value;

        /// <summary>
        /// Gets or sets the current pipeline object under consideration.
        /// </summary>
        internal PSObject CurrentPipelineObject
        {
            get { return currentObjectInPipeline; }

            set
            {
                currentObjectInPipeline = value;
            }
        }

        /// <summary>
        /// Internal helper. Interface that should be used for interaction with host.
        /// </summary>
        internal PSHost PSHostInternal
        {
            get { return _CBhost; }
        }

        private PSHost _CBhost;

        /// <summary>
        /// Internal helper to get to SessionState.
        /// </summary>
        internal SessionState InternalState
        {
            get { return _state; }
        }

        private SessionState _state;

        /// <summary>
        /// Internal helper. Indicates whether stop has been requested on this command.
        /// </summary>
        internal bool IsStopping
        {
            get
            {
                MshCommandRuntime mcr = this.commandRuntime as MshCommandRuntime;
                return (mcr != null && mcr.IsStopping);
            }
        }

        /// <summary>
        /// The information about the command.
        /// </summary>
        private CommandInfo _commandInfo;
        /// <summary>
        /// Gets or sets the command information for the command.
        /// </summary>
        internal CommandInfo CommandInfo
        {
            get { return _commandInfo; }

            set { _commandInfo = value; }
        }

        #endregion internal_members

        #region public_properties

        /// <summary>
        /// Gets or sets the execution context.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// may not be set to null
        /// </exception>
        internal ExecutionContext Context
        {
            get { return _context; }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Context");
                }

                _context = value;
                Diagnostics.Assert(_context.EngineHostInterface is InternalHost, "context.EngineHostInterface is not an InternalHost");
                _CBhost = (InternalHost)_context.EngineHostInterface;

                // Construct the session state API set from the new context

                _state = new SessionState(_context.EngineSessionState);
            }
        }

        private ExecutionContext _context;

        /// <summary>
        /// This property tells you if you were being invoked inside the runspace or
        /// if it was an external request.
        /// </summary>
        public CommandOrigin CommandOrigin
        {
            get { return CommandOriginInternal; }
        }

        internal CommandOrigin CommandOriginInternal = CommandOrigin.Internal;

        #endregion public_properties

        #region Override

        /// <summary>
        /// When overridden in the derived class, performs initialization
        /// of command execution.
        /// Default implementation in the base class just returns.
        /// </summary>
        internal virtual void DoBeginProcessing()
        {
        }

        /// <summary>
        /// When overridden in the derived class, performs execution
        /// of the command.
        /// </summary>
        internal virtual void DoProcessRecord()
        {
        }

        /// <summary>
        /// When overridden in the derived class, performs clean-up
        /// after the command execution.
        /// Default implementation in the base class just returns.
        /// </summary>
        internal virtual void DoEndProcessing()
        {
        }

        /// <summary>
        /// When overridden in the derived class, interrupts currently
        /// running code within the command. It should interrupt BeginProcessing,
        /// ProcessRecord, and EndProcessing.
        /// Default implementation in the base class just returns.
        /// </summary>
        internal virtual void DoStopProcessing()
        {
        }

        #endregion Override

        /// <summary>
        /// Throws if the pipeline is stopping.
        /// </summary>
        /// <exception cref="System.Management.Automation.PipelineStoppedException"></exception>
        internal void ThrowIfStopping()
        {
            if (IsStopping)
                throw new PipelineStoppedException();
        }

        #region Dispose

        /// <summary>
        /// IDisposable implementation
        /// When the command is complete, release the associated members.
        /// </summary>
        /// <remarks>
        /// Using InternalDispose instead of Dispose pattern because this
        /// interface was shipped in PowerShell V1 and 3rd cmdlets indirectly
        /// derive from this interface. If we depend on Dispose() and 3rd
        /// party cmdlets do not call base.Dispose (which is the case), we
        /// will still end up having this leak.
        /// </remarks>
        internal void InternalDispose(bool isDisposing)
        {
            _myInvocation = null;
            _state = null;
            _commandInfo = null;
            _context = null;
        }

        #endregion
    }
}

namespace System.Management.Automation
{
    #region ErrorView
    /// <summary>
    /// Defines the potential ErrorView options.
    /// </summary>
    public enum ErrorView
    {
        /// <summary>Existing all red multi-line output.</summary>
        NormalView = 0,

        /// <summary>Only show category information.</summary>
        CategoryView = 1,

        /// <summary>Concise shows more information on the context of the error or just the message if not a script or parser error.</summary>
        ConciseView = 2,
    }
    #endregion ErrorView

    #region ActionPreference
    /// <summary>
    /// Defines the Action Preference options.  These options determine
    /// what will happen when a particular type of event occurs.
    /// For example, setting shell variable ErrorActionPreference to "Stop"
    /// will cause the command to stop when an otherwise non-terminating
    /// error occurs.
    /// </summary>
    public enum ActionPreference
    {
        /// <summary>Ignore this event and continue</summary>
        SilentlyContinue = 0,

        /// <summary>Stop the command</summary>
        Stop = 1,

        /// <summary>Handle this event as normal and continue</summary>
        Continue = 2,

        /// <summary>Ask whether to stop or continue</summary>
        Inquire = 3,

        /// <summary>Ignore the event completely (not even logging it to the target stream)</summary>
        Ignore = 4,

        /// <summary>Reserved for future use.</summary>
        Suspend = 5,

        /// <summary>Enter the debugger.</summary>
        Break = 6,
    } // enum ActionPreference
    #endregion ActionPreference

    #region ConfirmImpact
    /// <summary>
    /// Defines the ConfirmImpact levels.  These levels describe
    /// the "destructiveness" of an action, and thus the degree of
    /// important that the user confirm the action.
    /// For example, setting the read-only flag on a file might be Low,
    /// and reformatting a disk might be High.
    /// These levels are also used in $ConfirmPreference to describe
    /// which operations should be confirmed.  Operations with ConfirmImpact
    /// equal to or greater than $ConfirmPreference are confirmed.
    /// Operations with ConfirmImpact.None are never confirmed, and
    /// no operations are confirmed when $ConfirmPreference is ConfirmImpact.None
    /// (except when explicitly requested with -Confirm).
    /// </summary>
    public enum ConfirmImpact
    {
        /// <summary>There is never any need to confirm this action.</summary>
        None,
        /// <summary>
        /// This action only needs to be confirmed when the
        /// user has requested that low-impact changes must be confirmed.
        /// </summary>
        Low,
        /// <summary>
        /// This action should be confirmed in most scenarios where
        /// confirmation is requested.
        /// </summary>
        Medium,
        /// <summary>
        /// This action is potentially highly "destructive" and should be
        /// confirmed by default unless otherwise specified.
        /// </summary>
        High,
    }
    #endregion ConfirmImpact

    /// <summary>
    /// Defines members and overrides used by Cmdlets.
    /// All Cmdlets must derive from <see cref="System.Management.Automation.Cmdlet"/>.
    /// </summary>
    /// <remarks>
    /// There are two ways to create a Cmdlet: by deriving from the Cmdlet base class, and by
    /// deriving from the PSCmdlet base class.  The Cmdlet base class is the primary means by
    /// which users create their own Cmdlets.  Extending this class provides support for the most
    /// common functionality, including object output and record processing.
    /// If your Cmdlet requires access to the MSH Runtime (for example, variables in the session state,
    /// access to the host, or information about the current Cmdlet Providers,) then you should instead
    /// derive from the PSCmdlet base class.
    /// The public members defined by the PSCmdlet class are not designed to be overridden; instead, they
    /// provided access to different aspects of the MSH runtime.
    /// In both cases, users should first develop and implement an object model to accomplish their
    /// task, extending the Cmdlet or PSCmdlet classes only as a thin management layer.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Internal.InternalCommand"/>
    public abstract partial class PSCmdlet : Cmdlet
    {
        #region private_members

        private ProviderIntrinsics _invokeProvider = null;

        #endregion private_members

        #region public_properties

        /// <summary>
        /// Gets the host interaction APIs.
        /// </summary>
        public PSHost Host
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return PSHostInternal;
                }
            }
        }

        /// <summary>
        /// Gets the instance of session state for the current runspace.
        /// </summary>
        public SessionState SessionState
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return this.InternalState;
                }
            }
        }

        /// <summary>
        /// Gets the event manager for the current runspace.
        /// </summary>
        public PSEventManager Events
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return this.Context.Events;
                }
            }
        }

        /// <summary>
        /// Repository for jobs.
        /// </summary>
        public JobRepository JobRepository
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return ((LocalRunspace)this.Context.CurrentRunspace).JobRepository;
                }
            }
        }

        /// <summary>
        /// Manager for JobSourceAdapters registered.
        /// </summary>
        public JobManager JobManager
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return ((LocalRunspace)this.Context.CurrentRunspace).JobManager;
                }
            }
        }

        /// <summary>
        /// Repository for runspaces.
        /// </summary>
        internal RunspaceRepository RunspaceRepository
        {
            get
            {
                return ((LocalRunspace)this.Context.CurrentRunspace).RunspaceRepository;
            }
        }

        /// <summary>
        /// Gets the instance of the provider interface APIs for the current runspace.
        /// </summary>
        public ProviderIntrinsics InvokeProvider
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _invokeProvider ?? (_invokeProvider = new ProviderIntrinsics(this));
                }
            }
        }

        #region Provider wrappers

        /// <Content contentref="System.Management.Automation.PathIntrinsics.CurrentProviderLocation" />
        public PathInfo CurrentProviderLocation(string providerId)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (providerId == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(providerId));
                }

                PathInfo result = SessionState.Path.CurrentProviderLocation(providerId);

                Diagnostics.Assert(result != null, "DataStoreAdapterCollection.GetNamespaceCurrentLocation() should " + "throw an exception, not return null");
                return result;
            }
        }
        /// <Content contentref="System.Management.Automation.PathIntrinsics.GetUnresolvedProviderPathFromPSPath" />
        public string GetUnresolvedProviderPathFromPSPath(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
            }
        }

        /// <Content contentref="System.Management.Automation.PathIntrinsics.GetResolvedProviderPathFromPSPath" />
        public Collection<string> GetResolvedProviderPathFromPSPath(string path, out ProviderInfo provider)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider);
            }
        }
        #endregion Provider wrappers

        #endregion internal_members

        #region ctor

        /// <summary>
        /// Initializes the new instance of PSCmdlet class.
        /// </summary>
        /// <remarks>
        /// Only subclasses of <see cref="System.Management.Automation.Cmdlet"/>
        /// can be created.
        /// </remarks>
        protected PSCmdlet()
        {
        }

        #endregion ctor

        #region public_methods

        #region PSVariable APIs

        /// <Content contentref="System.Management.Automation.VariableIntrinsics.GetValue" />
        public object GetVariableValue(string name)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return this.SessionState.PSVariable.GetValue(name);
            }
        }

        /// <Content contentref="System.Management.Automation.VariableIntrinsics.GetValue" />
        public object GetVariableValue(string name, object defaultValue)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return this.SessionState.PSVariable.GetValue(name, defaultValue);
            }
        }

        #endregion PSVariable APIs

        #region Parameter methods

        #endregion Parameter methods

        #endregion public_methods
    }
}

