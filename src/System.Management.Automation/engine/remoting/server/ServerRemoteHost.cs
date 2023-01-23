// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The ServerRemoteHost class.
    /// </summary>
    internal class ServerRemoteHost : PSHost, IHostSupportsInteractiveSession
    {
        #region Private Members

        /// <summary>
        /// Remote host user interface.
        /// </summary>
        private readonly ServerRemoteHostUserInterface _remoteHostUserInterface;

        /// <summary>
        /// Server method executor.
        /// </summary>
        private readonly ServerMethodExecutor _serverMethodExecutor;

        /// <summary>
        /// Client runspace pool id.
        /// </summary>
        private readonly Guid _clientRunspacePoolId;

        /// <summary>
        /// Client power shell id.
        /// </summary>
        private readonly Guid _clientPowerShellId;

        /// <summary>
        /// Transport manager.
        /// </summary>
        protected AbstractServerTransportManager _transportManager;

        /// <summary>
        /// ServerDriverRemoteHost.
        /// </summary>
        private readonly ServerDriverRemoteHost _serverDriverRemoteHost;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for ServerRemoteHost.
        /// </summary>
        internal ServerRemoteHost(
            Guid clientRunspacePoolId,
            Guid clientPowerShellId,
            HostInfo hostInfo,
            AbstractServerTransportManager transportManager,
            Runspace runspace,
            ServerDriverRemoteHost serverDriverRemoteHost)
        {
            _clientRunspacePoolId = clientRunspacePoolId;
            _clientPowerShellId = clientPowerShellId;
            Dbg.Assert(hostInfo != null, "Expected hostInfo != null");
            Dbg.Assert(transportManager != null, "Expected transportManager != null");

            // Set host-info and the transport-manager.
            HostInfo = hostInfo;
            _transportManager = transportManager;
            _serverDriverRemoteHost = serverDriverRemoteHost;

            // Create the executor for the host methods.
            _serverMethodExecutor = new ServerMethodExecutor(
                clientRunspacePoolId, clientPowerShellId, _transportManager);

            // Use HostInfo to create host-UI as null or non-null based on the client's host-UI.
            _remoteHostUserInterface = hostInfo.IsHostUINull ? null : new ServerRemoteHostUserInterface(this);

            Runspace = runspace;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Server method executor.
        /// </summary>
        internal ServerMethodExecutor ServerMethodExecutor
        {
            get { return _serverMethodExecutor; }
        }

        /// <summary>
        /// The user interface.
        /// </summary>
        public override PSHostUserInterface UI
        {
            get { return _remoteHostUserInterface; }
        }

        /// <summary>
        /// Name.
        /// </summary>
        public override string Name
        {
            get { return "ServerRemoteHost"; }
        }

        /// <summary>
        /// Version.
        /// </summary>
        public override Version Version
        {
            get { return RemotingConstants.HostVersion; }
        }

        /// <summary>
        /// Instance id.
        /// </summary>
        public override Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// Is runspace pushed.
        /// </summary>
        public virtual bool IsRunspacePushed
        {
            get
            {
                if (_serverDriverRemoteHost != null)
                {
                    return _serverDriverRemoteHost.IsRunspacePushed;
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetIsRunspacePushed);
                }
            }
        }

        /// <summary>
        /// Runspace.
        /// </summary>
        public Runspace Runspace { get; internal set; }

        /// <summary>
        /// Host info.
        /// </summary>
        internal HostInfo HostInfo { get; }

        #endregion

        #region Method Overrides

        /// <summary>
        /// Set should exit.
        /// </summary>
        public override void SetShouldExit(int exitCode)
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetShouldExit, new object[] { exitCode });
        }

        /// <summary>
        /// Enter nested prompt.
        /// </summary>
        public override void EnterNestedPrompt()
        {
            throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.EnterNestedPrompt);
        }

        /// <summary>
        /// Exit nested prompt.
        /// </summary>
        public override void ExitNestedPrompt()
        {
            throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.ExitNestedPrompt);
        }

        /// <summary>
        /// Notify begin application.
        /// </summary>
        public override void NotifyBeginApplication()
        {
            // This is called when a native application is executed on the server. It gives the
            // host an opportunity to save state that might be altered by the native application.
            // This call should not be sent to the client because the native application running
            // on the server cannot affect the state of the machine on the client.
        }

        /// <summary>
        /// Notify end application.
        /// </summary>
        public override void NotifyEndApplication()
        {
            // See note in NotifyBeginApplication.
        }

        /// <summary>
        /// Current culture.
        /// </summary>
        public override CultureInfo CurrentCulture
        {
            get
            {
                // Return the thread's current culture and rely on WinRM to set this
                // correctly based on the client's culture.
                return CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Current ui culture.
        /// </summary>
        public override CultureInfo CurrentUICulture
        {
            get
            {
                // Return the thread's current UI culture and rely on WinRM to set
                // this correctly based on the client's UI culture.
                return CultureInfo.CurrentUICulture;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Push runspace.
        /// </summary>
        public virtual void PushRunspace(Runspace runspace)
        {
            if (_serverDriverRemoteHost != null)
            {
                _serverDriverRemoteHost.PushRunspace(runspace);
            }
            else
            {
                throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.PushRunspace);
            }
        }

        /// <summary>
        /// Pop runspace.
        /// </summary>
        public virtual void PopRunspace()
        {
            if ((_serverDriverRemoteHost != null) && (_serverDriverRemoteHost.IsRunspacePushed))
            {
                if (_serverDriverRemoteHost.PropagatePop)
                {
                    // Forward the PopRunspace command to client and keep *this* pushed runspace as
                    // the configured JEA restricted session.
                    _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.PopRunspace);
                }
                else
                {
                    _serverDriverRemoteHost.PopRunspace();
                }
            }
            else
            {
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.PopRunspace);
            }
        }

        #endregion
    }

    /// <summary>
    /// The remote host class for the ServerRunspacePoolDriver.
    /// </summary>
    internal class ServerDriverRemoteHost : ServerRemoteHost
    {
        #region Private Members

        private RemoteRunspace _pushedRunspace;
        private ServerRemoteDebugger _debugger;
        private bool _hostSupportsPSEdit;

        #endregion

        #region Constructor

        internal ServerDriverRemoteHost(
            Guid clientRunspacePoolId,
            Guid clientPowerShellId,
            HostInfo hostInfo,
            AbstractServerSessionTransportManager transportManager,
            ServerRemoteDebugger debugger)
            : base(clientRunspacePoolId, clientPowerShellId, hostInfo, transportManager, null, null)
        {
            _debugger = debugger;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// True if runspace is pushed.
        /// </summary>
        public override bool IsRunspacePushed
        {
            get
            {
                return (_pushedRunspace != null);
            }
        }

        /// <summary>
        /// Push runspace to use for remote command execution.
        /// </summary>
        /// <param name="runspace">RemoteRunspace.</param>
        public override void PushRunspace(Runspace runspace)
        {
            if (_debugger == null)
            {
                throw new PSInvalidOperationException(RemotingErrorIdStrings.ServerDriverRemoteHostNoDebuggerToPush);
            }

            if (_pushedRunspace != null)
            {
                throw new PSInvalidOperationException(RemotingErrorIdStrings.ServerDriverRemoteHostAlreadyPushed);
            }

            if (!(runspace is RemoteRunspace remoteRunspace))
            {
                throw new PSInvalidOperationException(RemotingErrorIdStrings.ServerDriverRemoteHostNotRemoteRunspace);
            }

            // PSEdit support.  Existence of RemoteSessionOpenFileEvent event indicates host supports PSEdit
            _hostSupportsPSEdit = false;
            PSEventManager localEventManager = Runspace?.Events;
            _hostSupportsPSEdit = localEventManager != null && localEventManager.GetEventSubscribers(HostUtilities.RemoteSessionOpenFileEvent).GetEnumerator().MoveNext();
            if (_hostSupportsPSEdit)
            {
                AddPSEditForRunspace(remoteRunspace);
            }

            _debugger.PushDebugger(runspace.Debugger);
            _pushedRunspace = remoteRunspace;
        }

        /// <summary>
        /// Pop runspace.
        /// </summary>
        public override void PopRunspace()
        {
            if (_pushedRunspace != null)
            {
                _debugger?.PopDebugger();

                if (_hostSupportsPSEdit)
                {
                    RemovePSEditFromRunspace(_pushedRunspace);
                }

                if (_pushedRunspace.ShouldCloseOnPop)
                {
                    _pushedRunspace.Close();
                }

                _pushedRunspace = null;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Server Debugger.
        /// </summary>
        internal Debugger ServerDebugger
        {
            get { return _debugger; }

            set { _debugger = value as ServerRemoteDebugger; }
        }

        /// <summary>
        /// Pushed runspace or null.
        /// </summary>
        internal Runspace PushedRunspace
        {
            get { return _pushedRunspace; }
        }

        /// <summary>
        /// When true will propagate pop call to client after popping runspace from this
        /// host.  Used for OutOfProc remote sessions in a restricted (pushed) remote runspace,
        /// where a pop (exit session) should occur.
        /// </summary>
        internal bool PropagatePop
        {
            get;
            set;
        }

        #endregion

        #region PSEdit Support for ISE Host

        private void AddPSEditForRunspace(RemoteRunspace remoteRunspace)
        {
            if (remoteRunspace.Events == null) { return; }

            // Add event handler.
            remoteRunspace.Events.ReceivedEvents.PSEventReceived += HandleRemoteSessionForwardedEvent;

            // Add script function.
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.Runspace = remoteRunspace;
                powershell.AddScript(HostUtilities.CreatePSEditFunction).AddParameter("PSEditFunction", HostUtilities.PSEditFunction);
                try
                {
                    powershell.Invoke();
                }
                catch (RemoteException) { }
            }
        }

        private void RemovePSEditFromRunspace(RemoteRunspace remoteRunspace)
        {
            if (remoteRunspace.Events == null) { return; }

            // It is possible for the popped runspace to be in a bad state after an error.
            if ((remoteRunspace.RunspaceStateInfo.State != RunspaceState.Opened) || (remoteRunspace.RunspaceAvailability != RunspaceAvailability.Available))
            {
                return;
            }

            // Remove event handler.
            remoteRunspace.Events.ReceivedEvents.PSEventReceived -= HandleRemoteSessionForwardedEvent;

            // Remove script function.
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.Runspace = remoteRunspace;
                powershell.AddScript(HostUtilities.RemovePSEditFunction);
                try
                {
                    powershell.Invoke();
                }
                catch (RemoteException) { }
            }
        }

        private void HandleRemoteSessionForwardedEvent(object sender, PSEventArgs args)
        {
            if ((Runspace == null) || (Runspace.Events == null)) { return; }

            // Forward events from nested pushed session to parent session.
            try
            {
                Runspace.Events.GenerateEvent(
                    sourceIdentifier: args.SourceIdentifier,
                    sender: null,
                    args: args.SourceArgs,
                    extraData: null);
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }
}
