// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
#if !UNIX
using System.Security.Principal;
#endif
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Interface exposing driver single thread invoke enter/exit
    /// nested pipeline.
    /// </summary>
#nullable enable
    internal interface IRSPDriverInvoke
    {
        void EnterNestedPipeline();

        void ExitNestedPipeline();

        bool HandleStopSignal();
    }
#nullable restore

    /// <summary>
    /// This class wraps a RunspacePoolInternal object. It is used to function
    /// as a server side runspacepool.
    /// </summary>
    internal sealed class ServerRunspacePoolDriver : IRSPDriverInvoke
    {
        #region Private Members

        // local runspace pool at the server

        // Optional initial location of the PowerShell session
        private readonly string _initialLocation;

        // Script to run after a RunspacePool/Runspace is created in this session.
        private readonly ConfigurationDataFromXML _configData;

        // application private data to send back to the client in when we get into "opened" state
        private PSPrimitiveDictionary _applicationPrivateData;

        // the client runspacepool's guid that is
        // associated with this runspace pool driver

        // data structure handler object to handle all communications
        // with the client

        // powershell's associated with this runspace pool
        private readonly Dictionary<Guid, ServerPowerShellDriver> _associatedShells
            = new Dictionary<Guid, ServerPowerShellDriver>();

        // remote host associated with this runspacepool
        private readonly ServerDriverRemoteHost _remoteHost;

        private bool _isClosed;

        // server capability reported to the client during negotiation (not the actual capability)
        private readonly RemoteSessionCapability _serverCapability;
        private Runspace _rsToUseForSteppablePipeline;

        // steppable pipeline event subscribers exist per-session
        private readonly ServerSteppablePipelineSubscriber _eventSubscriber = new ServerSteppablePipelineSubscriber();
        private PSDataCollection<object> _inputCollection; // PowerShell driver input collection

        // Object to invoke nested PowerShell drivers on single pipeline worker thread.
        private readonly PowerShellDriverInvoker _driverNestedInvoker;

        // Remote wrapper for script debugger.
        private ServerRemoteDebugger _serverRemoteDebugger;

        // Version of PowerShell client.
        private readonly Version _clientPSVersion;

        // Optional endpoint configuration name.
        // Used in OutOfProc scenarios that do not support PSSession endpoint configuration.
        // Results in a configured remote runspace pushed onto driver host.
        private readonly string _configurationName;

        /// <summary>
        /// Event that get raised when the RunspacePool is closed.
        /// </summary>
        internal EventHandler<EventArgs> Closed;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the runspace pool driver.
        /// </summary>
        /// <param name="clientRunspacePoolId">Client runspace pool id to associate.</param>
        /// <param name="transportManager">Transport manager associated with this
        /// runspace pool driver.</param>
        /// <param name="maxRunspaces">Maximum runspaces to open.</param>
        /// <param name="minRunspaces">Minimum runspaces to open.</param>
        /// <param name="threadOptions">Threading options for the runspaces in the pool.</param>
        /// <param name="apartmentState">Apartment state for the runspaces in the pool.</param>
        /// <param name="hostInfo">Host information about client side host.</param>
        /// <param name="configData">Contains:
        /// 1. Script to run after a RunspacePool/Runspace is created in this session.
        /// For RunspacePool case, every newly created Runspace (in the pool) will run
        /// this script.
        /// 2. ThreadOptions for RunspacePool/Runspace
        /// 3. ThreadApartment for RunspacePool/Runspace
        /// </param>
        /// <param name="initialSessionState">Configuration of the runspace.</param>
        /// <param name="applicationPrivateData">Application private data.</param>
        /// <param name="isAdministrator">True if the driver is being created by an administrator.</param>
        /// <param name="serverCapability">Server capability reported to the client during negotiation (not the actual capability).</param>
        /// <param name="psClientVersion">Client PowerShell version.</param>
        /// <param name="configurationName">Optional endpoint configuration name to create a pushed configured runspace.</param>
        /// <param name="initialLocation">Optional initial location of the powershell.</param>
        internal ServerRunspacePoolDriver(
            Guid clientRunspacePoolId,
            int minRunspaces,
            int maxRunspaces,
            PSThreadOptions threadOptions,
            ApartmentState apartmentState,
            HostInfo hostInfo,
            InitialSessionState initialSessionState,
            PSPrimitiveDictionary applicationPrivateData,
            ConfigurationDataFromXML configData,
            AbstractServerSessionTransportManager transportManager,
            bool isAdministrator,
            RemoteSessionCapability serverCapability,
            Version psClientVersion,
            string configurationName,
            string initialLocation)
        {
            Dbg.Assert(configData != null, "ConfigurationData cannot be null");

            _serverCapability = serverCapability;
            _clientPSVersion = psClientVersion;

            _configurationName = configurationName;
            _initialLocation = initialLocation;

            // Create a new server host and associate for host call
            // integration
            _remoteHost = new ServerDriverRemoteHost(
                clientRunspacePoolId, Guid.Empty, hostInfo, transportManager, null);

            _configData = configData;
            _applicationPrivateData = applicationPrivateData;
            RunspacePool = RunspaceFactory.CreateRunspacePool(
                  minRunspaces, maxRunspaces, initialSessionState, _remoteHost);

            // Set ThreadOptions for this RunspacePool
            // The default server settings is to make new commands execute in the calling thread...this saves
            // thread switching time and thread pool pressure on the service.
            // Users can override the server settings only if they are administrators
            PSThreadOptions serverThreadOptions = configData.ShellThreadOptions ?? PSThreadOptions.UseCurrentThread;
            if (threadOptions == PSThreadOptions.Default || threadOptions == serverThreadOptions)
            {
                RunspacePool.ThreadOptions = serverThreadOptions;
            }
            else
            {
                if (!isAdministrator)
                {
                    throw new InvalidOperationException(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.MustBeAdminToOverrideThreadOptions));
                }

                RunspacePool.ThreadOptions = threadOptions;
            }

            // Set Thread ApartmentState for this RunspacePool
            ApartmentState serverApartmentState = configData.ShellThreadApartmentState ?? Runspace.DefaultApartmentState;

            if (apartmentState == ApartmentState.Unknown || apartmentState == serverApartmentState)
            {
                RunspacePool.ApartmentState = serverApartmentState;
            }
            else
            {
                RunspacePool.ApartmentState = apartmentState;
            }

            // If we have a runspace pool with a single runspace then we can run nested pipelines on
            // on it in a single pipeline invoke thread.
            if (maxRunspaces == 1 &&
                (RunspacePool.ThreadOptions == PSThreadOptions.Default ||
                 RunspacePool.ThreadOptions == PSThreadOptions.UseCurrentThread))
            {
                _driverNestedInvoker = new PowerShellDriverInvoker();
            }

            InstanceId = clientRunspacePoolId;
            DataStructureHandler = new ServerRunspacePoolDataStructureHandler(this, transportManager);

            // handle the StateChanged event of the runspace pool
            RunspacePool.StateChanged += HandleRunspacePoolStateChanged;

            // listen for events on the runspace pool
            RunspacePool.ForwardEvent += HandleRunspacePoolForwardEvent;

            RunspacePool.RunspaceCreated += HandleRunspaceCreated;

            // register for all the events from the data structure handler
            DataStructureHandler.CreateAndInvokePowerShell += HandleCreateAndInvokePowerShell;
            DataStructureHandler.GetCommandMetadata += HandleGetCommandMetadata;
            DataStructureHandler.HostResponseReceived += HandleHostResponseReceived;
            DataStructureHandler.SetMaxRunspacesReceived += HandleSetMaxRunspacesReceived;
            DataStructureHandler.SetMinRunspacesReceived += HandleSetMinRunspacesReceived;
            DataStructureHandler.GetAvailableRunspacesReceived += HandleGetAvailableRunspacesReceived;
            DataStructureHandler.ResetRunspaceState += HandleResetRunspaceState;
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Data structure handler for communicating with client.
        /// </summary>
        internal ServerRunspacePoolDataStructureHandler DataStructureHandler { get; }

        /// <summary>
        /// The server host associated with the runspace pool.
        /// </summary>
        internal ServerRemoteHost ServerRemoteHost
        {
            get { return _remoteHost; }
        }

        /// <summary>
        /// The client runspacepool id.
        /// </summary>
        internal Guid InstanceId { get; }

        /// <summary>
        /// The local runspace pool associated with
        /// this driver.
        /// </summary>
        internal RunspacePool RunspacePool { get; private set; }

        /// <summary>
        /// Start the RunspacePoolDriver. This will open the
        /// underlying RunspacePool.
        /// </summary>
        internal void Start()
        {
            // open the runspace pool
            RunspacePool.Open();
        }

        /// <summary>
        /// Send application private data to client
        /// will be called during runspace creation
        /// and each time a new client connects to the server session.
        /// </summary>
        internal void SendApplicationPrivateDataToClient()
        {
            // Include Debug mode information.
            _applicationPrivateData ??= new PSPrimitiveDictionary();

            if (_serverRemoteDebugger != null)
            {
                // Current debug mode.
                DebugModes debugMode = _serverRemoteDebugger.DebugMode;
                if (_applicationPrivateData.ContainsKey(RemoteDebugger.DebugModeSetting))
                {
                    _applicationPrivateData[RemoteDebugger.DebugModeSetting] = (int)debugMode;
                }
                else
                {
                    _applicationPrivateData.Add(RemoteDebugger.DebugModeSetting, (int)debugMode);
                }

                // Current debug state.
                bool inBreakpoint = _serverRemoteDebugger.InBreakpoint;
                if (_applicationPrivateData.ContainsKey(RemoteDebugger.DebugStopState))
                {
                    _applicationPrivateData[RemoteDebugger.DebugStopState] = inBreakpoint;
                }
                else
                {
                    _applicationPrivateData.Add(RemoteDebugger.DebugStopState, inBreakpoint);
                }

                // Current debug breakpoint count.
                int breakpointCount = _serverRemoteDebugger.GetBreakpointCount();
                if (_applicationPrivateData.ContainsKey(RemoteDebugger.DebugBreakpointCount))
                {
                    _applicationPrivateData[RemoteDebugger.DebugBreakpointCount] = breakpointCount;
                }
                else
                {
                    _applicationPrivateData.Add(RemoteDebugger.DebugBreakpointCount, breakpointCount);
                }

                // Current debugger BreakAll option setting.
                bool breakAll = _serverRemoteDebugger.IsDebuggerSteppingEnabled;
                if (_applicationPrivateData.ContainsKey(RemoteDebugger.BreakAllSetting))
                {
                    _applicationPrivateData[RemoteDebugger.BreakAllSetting] = breakAll;
                }
                else
                {
                    _applicationPrivateData.Add(RemoteDebugger.BreakAllSetting, breakAll);
                }

                // Current debugger PreserveUnhandledBreakpoints setting.
                UnhandledBreakpointProcessingMode bpMode = _serverRemoteDebugger.UnhandledBreakpointMode;
                if (_applicationPrivateData.ContainsKey(RemoteDebugger.UnhandledBreakpointModeSetting))
                {
                    _applicationPrivateData[RemoteDebugger.UnhandledBreakpointModeSetting] = (int)bpMode;
                }
                else
                {
                    _applicationPrivateData.Add(RemoteDebugger.UnhandledBreakpointModeSetting, (int)bpMode);
                }
            }

            DataStructureHandler.SendApplicationPrivateDataToClient(_applicationPrivateData, _serverCapability);
        }

        /// <summary>
        /// Dispose the runspace pool driver and release all its resources.
        /// </summary>
        internal void Close()
        {
            if (!_isClosed)
            {
                _isClosed = true;

                if ((_remoteHost != null) && (_remoteHost.IsRunspacePushed))
                {
                    Runspace runspaceToDispose = _remoteHost.PushedRunspace;
                    _remoteHost.PopRunspace();
                    runspaceToDispose?.Dispose();
                }

                DisposeRemoteDebugger();

                RunspacePool.Close();
                RunspacePool.StateChanged -= HandleRunspacePoolStateChanged;
                RunspacePool.ForwardEvent -= HandleRunspacePoolForwardEvent;
                RunspacePool.Dispose();
                RunspacePool = null;

                if (_rsToUseForSteppablePipeline != null)
                {
                    _rsToUseForSteppablePipeline.Close();
                    _rsToUseForSteppablePipeline.Dispose();
                    _rsToUseForSteppablePipeline = null;
                }

                Closed.SafeInvoke(this, EventArgs.Empty);
            }
        }

        #endregion Internal Methods

        #region IRSPDriverInvoke interface methods

        /// <summary>
        /// This method blocks the current thread execution and starts a
        /// new Invoker pump that will handle invoking client side nested commands.
        /// This method returns after ExitNestedPipeline is called.
        /// </summary>
        public void EnterNestedPipeline()
        {
            if (_driverNestedInvoker == null)
            {
                throw new PSNotSupportedException(RemotingErrorIdStrings.NestedPipelineNotSupported);
            }

            _driverNestedInvoker.PushInvoker();
        }

        /// <summary>
        /// Removes current nested command Invoker pump and allows parent command
        /// to continue running.
        /// </summary>
        public void ExitNestedPipeline()
        {
            if (_driverNestedInvoker == null)
            {
                throw new PSNotSupportedException(RemotingErrorIdStrings.NestedPipelineNotSupported);
            }

            _driverNestedInvoker.PopInvoker();
        }

        /// <summary>
        /// If script execution is currently in debugger stopped mode, this will
        /// release the debugger and terminate script execution, or if processing
        /// a debug command will stop the debug command.
        /// This is used to implement the remote stop signal and ensures a command
        /// will stop even when in debug stop mode.
        /// </summary>
        public bool HandleStopSignal()
        {
            if (_serverRemoteDebugger != null)
            {
                return _serverRemoteDebugger.HandleStopSignal();
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// RunspaceCreated eventhandler. This is used to set TypeTable for TransportManager.
        /// TransportManager needs TypeTable for Serializing/Deserializing objects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleRunspaceCreatedForTypeTable(object sender, RunspaceCreatedEventArgs args)
        {
            DataStructureHandler.TypeTable = args.Runspace.ExecutionContext.TypeTable;
            _rsToUseForSteppablePipeline = args.Runspace;

            SetupRemoteDebugger(_rsToUseForSteppablePipeline);

            if (!string.IsNullOrEmpty(_configurationName))
            {
                // Client is requesting a configured session.
                // Create a configured remote runspace and push onto host stack.
                if ((_remoteHost != null) && !(_remoteHost.IsRunspacePushed))
                {
                    // Let exceptions propagate.
                    RemoteRunspace remoteRunspace = HostUtilities.CreateConfiguredRunspace(_configurationName, _remoteHost);
                    _remoteHost.PropagatePop = true;
                    _remoteHost.PushRunspace(remoteRunspace);
                }
            }
        }

        private void SetupRemoteDebugger(Runspace runspace)
        {
            CmdletInfo cmdletInfo = runspace.ExecutionContext.SessionState.InvokeCommand.GetCmdlet(ServerRemoteDebugger.SetPSBreakCommandText);
            if (cmdletInfo == null)
            {
                if ((runspace.ExecutionContext.LanguageMode != PSLanguageMode.FullLanguage) &&
                    (!runspace.ExecutionContext.UseFullLanguageModeInDebugger))
                {
                    return;
                }
            }
            else
            {
                if (cmdletInfo.Visibility != SessionStateEntryVisibility.Public)
                {
                    return;
                }
            }

            // Remote debugger is created only when client version is PSVersion (4.0)
            // or greater, and remote session supports debugging.
            if ((_driverNestedInvoker != null) &&
                (_clientPSVersion != null && _clientPSVersion.Major >= 4) &&
                (runspace != null && runspace.Debugger != null))
            {
                _serverRemoteDebugger = new ServerRemoteDebugger(this, runspace, runspace.Debugger);
                _remoteHost.ServerDebugger = _serverRemoteDebugger;
            }
        }

        private void DisposeRemoteDebugger() => _serverRemoteDebugger?.Dispose();

        /// <summary>
        /// Invokes a script.
        /// </summary>
        /// <param name="cmdToRun"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private PSDataCollection<PSObject> InvokeScript(Command cmdToRun, RunspaceCreatedEventArgs args)
        {
            Debug.Assert(cmdToRun != null, "cmdToRun shouldn't be null");

            // Don't invoke initialization script as trusted (CommandOrigin == Internal) if the system is in lock down mode.
            cmdToRun.CommandOrigin = (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) ? CommandOrigin.Runspace : CommandOrigin.Internal;

            cmdToRun.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            PowerShell powershell = PowerShell.Create();
            powershell.AddCommand(cmdToRun).AddCommand("out-default");

            return InvokePowerShell(powershell, args);
        }

        /// <summary>
        /// Invokes a PowerShell instance.
        /// </summary>
        /// <param name="powershell"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private PSDataCollection<PSObject> InvokePowerShell(PowerShell powershell, RunspaceCreatedEventArgs args)
        {
            Debug.Assert(powershell != null, "powershell shouldn't be null");

            // run the startup script on the runspace's host
            HostInfo hostInfo = _remoteHost.HostInfo;
            ServerPowerShellDriver driver = new ServerPowerShellDriver(
                powershell,
                null,
                true,
                Guid.Empty,
                this.InstanceId,
                this,
                args.Runspace.ApartmentState,
                hostInfo,
                RemoteStreamOptions.AddInvocationInfo,
                false,
                args.Runspace);

            IAsyncResult asyncResult = driver.Start();

            // if there was an exception running the script..this may throw..this will
            // result in the runspace getting closed/broken.
            PSDataCollection<PSObject> results = powershell.EndInvoke(asyncResult);

            // find out if there are any error records reported. If there is one, report the error..
            // this will result in the runspace getting closed/broken.
            ArrayList errorList = (ArrayList)powershell.Runspace.GetExecutionContext.DollarErrorVariable;
            if (errorList.Count > 0)
            {
                string exceptionThrown;
                ErrorRecord lastErrorRecord = errorList[0] as ErrorRecord;
                if (lastErrorRecord != null)
                {
                    exceptionThrown = lastErrorRecord.ToString();
                }
                else
                {
                    Exception lastException = errorList[0] as Exception;
                    if (lastException != null)
                    {
                        exceptionThrown = lastException.Message ?? string.Empty;
                    }
                    else
                    {
                        exceptionThrown = string.Empty;
                    }
                }

                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.StartupScriptThrewTerminatingError, exceptionThrown);
            }

            return results;
        }

        /// <summary>
        /// Raised by RunspacePool whenever a new runspace is created. This is used
        /// by the driver to run startup script as well as set personal folder
        /// as the current working directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">
        /// Runspace that was created by the RunspacePool.
        /// </param>
        private void HandleRunspaceCreated(object sender, RunspaceCreatedEventArgs args)
        {
            this.ServerRemoteHost.Runspace = args.Runspace;

            // If the system lockdown policy says "Enforce", do so (unless it's in the
            // more restrictive NoLanguage mode)
            Utils.EnforceSystemLockDownLanguageMode(args.Runspace.ExecutionContext);

            // Set the current location to MyDocuments folder for this runspace.
            // This used to be set to the Personal folder but was changed to MyDocuments folder for
            // compatibility with PowerShell on Nano Server for PowerShell V5.
            // This is needed because in the remoting scenario, Environment.CurrentDirectory
            // always points to System Folder (%windir%\system32) irrespective of the
            // user as %HOMEDRIVE% and %HOMEPATH% are not available for the logon process.
            // Doing this here than AutomationEngine as I dont want to introduce a dependency
            // on Remoting in PowerShell engine
            try
            {
                string personalfolder = Platform.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                args.Runspace.ExecutionContext.EngineSessionState.SetLocation(personalfolder);
            }
            catch (Exception)
            {
                // SetLocation API can call 3rd party code and so there is no telling what exception may be thrown.
                // Setting location is not critical and is expected not to work with some account types, so we want
                // to ignore all but critical errors.
            }

            if (!string.IsNullOrWhiteSpace(_initialLocation))
            {
                var setLocationCommand = new Command("Set-Location");
                setLocationCommand.Parameters.Add(new CommandParameter("LiteralPath", _initialLocation));
                InvokeScript(setLocationCommand, args);
            }

            // Run startup scripts
            InvokeStartupScripts(args);

            // Now that the server side runspace is set up allow the secondary handler to run.
            HandleRunspaceCreatedForTypeTable(sender, args);
        }

        private void InvokeStartupScripts(RunspaceCreatedEventArgs args)
        {
            Command cmdToRun = null;
            if (!string.IsNullOrEmpty(_configData.StartupScript))
            {
                // build the startup script..merge output / error.
                cmdToRun = new Command(_configData.StartupScript, false, false);
            }
            else if (!string.IsNullOrEmpty(_configData.InitializationScriptForOutOfProcessRunspace))
            {
                cmdToRun = new Command(_configData.InitializationScriptForOutOfProcessRunspace, true, false);
            }

            if (cmdToRun != null)
            {
                InvokeScript(cmdToRun, args);

                // if startup script set $PSApplicationPrivateData, then use that value as ApplicationPrivateData
                // instead of using results from PSSessionConfiguration.GetApplicationPrivateData()
                if (RunspacePool.RunspacePoolStateInfo.State == RunspacePoolState.Opening)
                {
                    object privateDataVariable = args.Runspace.SessionStateProxy.PSVariable.GetValue("global:PSApplicationPrivateData");
                    if (privateDataVariable != null)
                    {
                        _applicationPrivateData = (PSPrimitiveDictionary)LanguagePrimitives.ConvertTo(
                            privateDataVariable,
                            typeof(PSPrimitiveDictionary),
                            true,
                            CultureInfo.InvariantCulture,
                            null);
                    }
                }
            }
        }

        /// <summary>
        /// Handler to the runspace pool state changed events.
        /// </summary>
        /// <param name="sender">Sender of this events.</param>
        /// <param name="eventArgs">arguments which describe the
        /// RunspacePool's StateChanged event</param>
        private void HandleRunspacePoolStateChanged(object sender,
                            RunspacePoolStateChangedEventArgs eventArgs)
        {
            RunspacePoolState state = eventArgs.RunspacePoolStateInfo.State;
            Exception reason = eventArgs.RunspacePoolStateInfo.Reason;

            switch (state)
            {
                case RunspacePoolState.Broken:
                case RunspacePoolState.Closing:
                case RunspacePoolState.Closed:
                    {
                        DataStructureHandler.SendStateInfoToClient(new RunspacePoolStateInfo(state, reason));
                    }

                    break;

                case RunspacePoolState.Opened:
                    {
                        SendApplicationPrivateDataToClient();
                        DataStructureHandler.SendStateInfoToClient(new RunspacePoolStateInfo(state, reason));
                    }

                    break;
            }
        }

        /// <summary>
        /// Handler to the runspace pool psevents.
        /// </summary>
        private void HandleRunspacePoolForwardEvent(object sender, PSEventArgs e)
        {
            if (e.ForwardEvent)
            {
                DataStructureHandler.SendPSEventArgsToClient(e);
            }
        }

        /// <summary>
        /// Handle the invocation of powershell.
        /// </summary>
        /// <param name="_">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleCreateAndInvokePowerShell(object _, RemoteDataEventArgs<RemoteDataObject<PSObject>> eventArgs)
        {
            RemoteDataObject<PSObject> data = eventArgs.Data;

            // it is sufficient to just construct the powershell
            // driver, the local powershell on server side is
            // invoked from within the driver
            HostInfo hostInfo = RemotingDecoder.GetHostInfo(data.Data);

            ApartmentState apartmentState = RemotingDecoder.GetApartmentState(data.Data);

            RemoteStreamOptions streamOptions = RemotingDecoder.GetRemoteStreamOptions(data.Data);
            PowerShell powershell = RemotingDecoder.GetPowerShell(data.Data);
            bool noInput = RemotingDecoder.GetNoInput(data.Data);
            bool addToHistory = RemotingDecoder.GetAddToHistory(data.Data);
            bool isNested = false;

            // The server would've dropped the protocol version of an older client was connecting
            if (_serverCapability.ProtocolVersion >= RemotingConstants.ProtocolVersionWin8RTM)
            {
                isNested = RemotingDecoder.GetIsNested(data.Data);
            }

            // Perform pre-processing of command for over the wire debugging commands.
            if (_serverRemoteDebugger != null)
            {
                DebuggerCommandArgument commandArgument;
                bool terminateImmediate = false;
                Collection<object> preProcessOutput = new Collection<object>();

                try
                {
                    var result = PreProcessDebuggerCommand(powershell.Commands, _serverRemoteDebugger, preProcessOutput, out commandArgument);

                    switch (result)
                    {
                        case PreProcessCommandResult.SetDebuggerAction:
                            // Run this directly on the debugger and terminate the remote command.
                            _serverRemoteDebugger.SetDebuggerAction(commandArgument.ResumeAction.Value);
                            terminateImmediate = true;
                            break;

                        case PreProcessCommandResult.SetDebugMode:
                            // Set debug mode directly and terminate remote command.
                            _serverRemoteDebugger.SetDebugMode(commandArgument.Mode.Value);
                            terminateImmediate = true;
                            break;

                        case PreProcessCommandResult.SetDebuggerStepMode:
                            // Enable debugger and set to step action, then terminate remote command.
                            _serverRemoteDebugger.SetDebuggerStepMode(commandArgument.DebuggerStepEnabled.Value);
                            terminateImmediate = true;
                            break;

                        case PreProcessCommandResult.SetPreserveUnhandledBreakpointMode:
                            _serverRemoteDebugger.UnhandledBreakpointMode = commandArgument.UnhandledBreakpointMode.Value;
                            terminateImmediate = true;
                            break;

                        case PreProcessCommandResult.ValidNotProcessed:
                            terminateImmediate = true;
                            break;

                        case PreProcessCommandResult.BreakpointManagement:
                            terminateImmediate = true;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    terminateImmediate = true;

                    preProcessOutput.Add(
                        PSObject.AsPSObject(ex));
                }

                // If we don't want to run or queue a command to run in the server session then
                // terminate the command here by making it a No Op.
                if (terminateImmediate)
                {
                    ServerPowerShellDriver noOpDriver = new ServerPowerShellDriver(
                        powershell,
                        null,
                        noInput,
                        data.PowerShellId,
                        data.RunspacePoolId,
                        this,
                        apartmentState,
                        hostInfo,
                        streamOptions,
                        addToHistory,
                        null);

                    noOpDriver.RunNoOpCommand(preProcessOutput);
                    return;
                }
            }

            if (_remoteHost.IsRunspacePushed)
            {
                // If we have a pushed runspace then execute there.
                // Ensure debugger is enabled to the original mode it was set to.
                _serverRemoteDebugger?.CheckDebuggerState();

                StartPowerShellCommandOnPushedRunspace(
                    powershell,
                    null,
                    data.PowerShellId,
                    data.RunspacePoolId,
                    hostInfo,
                    streamOptions,
                    noInput,
                    addToHistory);

                return;
            }
            else if (isNested)
            {
                if (RunspacePool.GetMaxRunspaces() == 1)
                {
                    if (_driverNestedInvoker != null && _driverNestedInvoker.IsActive)
                    {
                        if (!_driverNestedInvoker.IsAvailable)
                        {
                            // A nested command is already running.
                            throw new PSInvalidOperationException(
                                StringUtil.Format(RemotingErrorIdStrings.CannotInvokeNestedCommandNestedCommandRunning));
                        }

                        // Handle as nested pipeline invocation.
                        powershell.SetIsNested(true);

                        // Always invoke PowerShell commands on pipeline worker thread
                        // for single runspace case, to support nested invocation requests (debugging scenario).
                        ServerPowerShellDriver srdriver = new ServerPowerShellDriver(
                            powershell,
                            null,
                            noInput,
                            data.PowerShellId,
                            data.RunspacePoolId,
                            this,
                            apartmentState,
                            hostInfo,
                            streamOptions,
                            addToHistory,
                            _rsToUseForSteppablePipeline);

                        _inputCollection = srdriver.InputCollection;
                        _driverNestedInvoker.InvokeDriverAsync(srdriver);
                        return;
                    }
                    else if (_serverRemoteDebugger != null &&
                             _serverRemoteDebugger.InBreakpoint &&
                             _serverRemoteDebugger.IsPushed)
                    {
                        _serverRemoteDebugger.StartPowerShellCommand(
                            powershell,
                            data.PowerShellId,
                            data.RunspacePoolId,
                            this,
                            apartmentState,
                            _remoteHost,
                            hostInfo,
                            streamOptions,
                            addToHistory);

                        return;
                    }
                    else if (powershell.Commands.Commands.Count == 1 &&
                             !powershell.Commands.Commands[0].IsScript &&
                             (powershell.Commands.Commands[0].CommandText.Contains("Get-PSDebuggerStopArgs", StringComparison.OrdinalIgnoreCase) ||
                              powershell.Commands.Commands[0].CommandText.Contains("Set-PSDebuggerAction", StringComparison.OrdinalIgnoreCase)))
                    {
                        // We do not want to invoke debugger commands in the steppable pipeline.
                        // Consider adding IsSteppable message to PSRP to handle this.
                        // This will be caught on the client.
                        throw new PSInvalidOperationException();
                    }

                    ServerPowerShellDataStructureHandler psHandler = DataStructureHandler.GetPowerShellDataStructureHandler();
                    if (psHandler != null)
                    {
                        // Have steppable invocation request.
                        powershell.SetIsNested(false);
                        // Execute command concurrently
                        ServerSteppablePipelineDriver spDriver = new ServerSteppablePipelineDriver(
                            powershell,
                            noInput,
                            data.PowerShellId,
                            data.RunspacePoolId,
                            this,
                            apartmentState,
                            hostInfo,
                            streamOptions,
                            addToHistory,
                            _rsToUseForSteppablePipeline,
                            _eventSubscriber,
                            _inputCollection);

                        spDriver.Start();
                        return;
                    }
                }

                // Allow command to run as non-nested and non-stepping.
                powershell.SetIsNested(false);
            }

            // Invoke command normally.  Ensure debugger is enabled to the
            // original mode it was set to.
            _serverRemoteDebugger?.CheckDebuggerState();

            // Invoke PowerShell on driver runspace pool.
            ServerPowerShellDriver driver = new ServerPowerShellDriver(
                powershell,
                null,
                noInput,
                data.PowerShellId,
                data.RunspacePoolId,
                this,
                apartmentState,
                hostInfo,
                streamOptions,
                addToHistory,
                null);

            _inputCollection = driver.InputCollection;
            driver.Start();
        }

        private bool? _initialSessionStateIncludesGetCommandWithListImportedSwitch;
        private readonly object _initialSessionStateIncludesGetCommandWithListImportedSwitchLock = new object();

        private bool DoesInitialSessionStateIncludeGetCommandWithListImportedSwitch()
        {
            if (!_initialSessionStateIncludesGetCommandWithListImportedSwitch.HasValue)
            {
                lock (_initialSessionStateIncludesGetCommandWithListImportedSwitchLock)
                {
                    if (!_initialSessionStateIncludesGetCommandWithListImportedSwitch.HasValue)
                    {
                        bool newValue = false;

                        InitialSessionState iss = this.RunspacePool.InitialSessionState;
                        if (iss != null)
                        {
                            IEnumerable<SessionStateCommandEntry> publicGetCommandEntries = iss
                                .Commands["Get-Command"]
                                .Where(static entry => entry.Visibility == SessionStateEntryVisibility.Public);
                            SessionStateFunctionEntry getCommandProxy = publicGetCommandEntries.OfType<SessionStateFunctionEntry>().FirstOrDefault();
                            if (getCommandProxy != null)
                            {
                                if (getCommandProxy.ScriptBlock.ParameterMetadata.BindableParameters.ContainsKey("ListImported"))
                                {
                                    newValue = true;
                                }
                            }
                            else
                            {
                                SessionStateCmdletEntry getCommandCmdlet = publicGetCommandEntries.OfType<SessionStateCmdletEntry>().FirstOrDefault();
                                if ((getCommandCmdlet != null) && (getCommandCmdlet.ImplementingType.Equals(typeof(Microsoft.PowerShell.Commands.GetCommandCommand))))
                                {
                                    newValue = true;
                                }
                            }
                        }

                        _initialSessionStateIncludesGetCommandWithListImportedSwitch = newValue;
                    }
                }
            }

            return _initialSessionStateIncludesGetCommandWithListImportedSwitch.Value;
        }

        /// <summary>
        /// Handle the invocation of command discovery pipeline.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleGetCommandMetadata(object sender, RemoteDataEventArgs<RemoteDataObject<PSObject>> eventArgs)
        {
            RemoteDataObject<PSObject> data = eventArgs.Data;

            PowerShell countingPipeline = RemotingDecoder.GetCommandDiscoveryPipeline(data.Data);
            if (this.DoesInitialSessionStateIncludeGetCommandWithListImportedSwitch())
            {
                countingPipeline.AddParameter("ListImported", true);
            }

            countingPipeline
                .AddParameter("ErrorAction", "SilentlyContinue")
                .AddCommand("Measure-Object")
                .AddCommand("Select-Object")
                .AddParameter("Property", "Count");

            PowerShell mainPipeline = RemotingDecoder.GetCommandDiscoveryPipeline(data.Data);
            if (this.DoesInitialSessionStateIncludeGetCommandWithListImportedSwitch())
            {
                mainPipeline.AddParameter("ListImported", true);
            }

            mainPipeline
                .AddCommand("Select-Object")
                .AddParameter("Property", new string[] {
                    "Name", "Namespace", "HelpUri", "CommandType", "ResolvedCommandName", "OutputType", "Parameters" });

            HostInfo useRunspaceHost = new HostInfo(null);
            useRunspaceHost.UseRunspaceHost = true;

            if (_remoteHost.IsRunspacePushed)
            {
                // If we have a pushed runspace then execute there.
                StartPowerShellCommandOnPushedRunspace(
                    countingPipeline,
                    mainPipeline,
                    data.PowerShellId,
                    data.RunspacePoolId,
                    useRunspaceHost,
                    0,
                    true,
                    false);
            }
            else
            {
                // Run on usual driver.
                ServerPowerShellDriver driver = new ServerPowerShellDriver(
                    countingPipeline,
                    mainPipeline,
                    true /* no input */,
                    data.PowerShellId,
                    data.RunspacePoolId,
                    this,
                    ApartmentState.Unknown,
                    useRunspaceHost,
                    0 /* stream options */,
                    false /* addToHistory */,
                    null /* use default rsPool runspace */);

                driver.Start();
            }
        }

        /// <summary>
        /// Handles host responses.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleHostResponseReceived(object sender,
            RemoteDataEventArgs<RemoteHostResponse> eventArgs)
        {
            _remoteHost.ServerMethodExecutor.HandleRemoteHostResponseFromClient((eventArgs.Data));
        }

        /// <summary>
        /// Sets the maximum runspace of the runspace pool and sends a response back.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">contains information about the new maxRunspaces
        /// and the callId at the client</param>
        private void HandleSetMaxRunspacesReceived(object sender, RemoteDataEventArgs<PSObject> eventArgs)
        {
            PSObject data = eventArgs.Data;
            int maxRunspaces = (int)((PSNoteProperty)data.Properties[RemoteDataNameStrings.MaxRunspaces]).Value;
            long callId = (long)((PSNoteProperty)data.Properties[RemoteDataNameStrings.CallId]).Value;

            bool response = RunspacePool.SetMaxRunspaces(maxRunspaces);
            DataStructureHandler.SendResponseToClient(callId, response);
        }

        /// <summary>
        /// Sets the minimum runspace of the runspace pool and sends a response back.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">contains information about the new minRunspaces
        /// and the callId at the client</param>
        private void HandleSetMinRunspacesReceived(object sender, RemoteDataEventArgs<PSObject> eventArgs)
        {
            PSObject data = eventArgs.Data;
            int minRunspaces = (int)((PSNoteProperty)data.Properties[RemoteDataNameStrings.MinRunspaces]).Value;
            long callId = (long)((PSNoteProperty)data.Properties[RemoteDataNameStrings.CallId]).Value;

            bool response = RunspacePool.SetMinRunspaces(minRunspaces);
            DataStructureHandler.SendResponseToClient(callId, response);
        }

        /// <summary>
        /// Gets the available runspaces from the server and sends it across
        /// to the client.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Contains information on the callid.</param>
        private void HandleGetAvailableRunspacesReceived(object sender, RemoteDataEventArgs<PSObject> eventArgs)
        {
            PSObject data = eventArgs.Data;
            long callId = (long)((PSNoteProperty)data.Properties[RemoteDataNameStrings.CallId]).Value;

            int availableRunspaces = RunspacePool.GetAvailableRunspaces();

            DataStructureHandler.SendResponseToClient(callId, availableRunspaces);
        }

        /// <summary>
        /// Forces a state reset on a single runspace pool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleResetRunspaceState(object sender, RemoteDataEventArgs<PSObject> eventArgs)
        {
            long callId = (long)((PSNoteProperty)(eventArgs.Data).Properties[RemoteDataNameStrings.CallId]).Value;
            bool response = ResetRunspaceState();

            DataStructureHandler.SendResponseToClient(callId, response);
        }

        /// <summary>
        /// Resets the single runspace in the runspace pool.
        /// </summary>
        /// <returns></returns>
        private bool ResetRunspaceState()
        {
            LocalRunspace runspaceToReset = _rsToUseForSteppablePipeline as LocalRunspace;
            if ((runspaceToReset == null) || (RunspacePool.GetMaxRunspaces() > 1))
            {
                return false;
            }

            try
            {
                // Local runspace state reset.
                runspaceToReset.ResetRunspaceState();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the PowerShell command on the currently pushed Runspace.
        /// </summary>
        /// <param name="powershell">PowerShell command or script.</param>
        /// <param name="extraPowerShell">PowerShell command to run after first completes.</param>
        /// <param name="powershellId">PowerShell Id.</param>
        /// <param name="runspacePoolId">RunspacePool Id.</param>
        /// <param name="hostInfo">Host Info.</param>
        /// <param name="streamOptions">Remote stream options.</param>
        /// <param name="noInput">False when input is provided.</param>
        /// <param name="addToHistory">Add to history.</param>
        private void StartPowerShellCommandOnPushedRunspace(
            PowerShell powershell,
            PowerShell extraPowerShell,
            Guid powershellId,
            Guid runspacePoolId,
            HostInfo hostInfo,
            RemoteStreamOptions streamOptions,
            bool noInput,
            bool addToHistory)
        {
            Runspace runspace = _remoteHost.PushedRunspace;

            ServerPowerShellDriver driver = new ServerPowerShellDriver(
                powershell,
                extraPowerShell,
                noInput,
                powershellId,
                runspacePoolId,
                this,
                ApartmentState.MTA,
                hostInfo,
                streamOptions,
                addToHistory,
                runspace);

            try
            {
                driver.Start();
            }
            catch (Exception)
            {
                // Pop runspace on error.
                _remoteHost.PopRunspace();

                throw;
            }
        }

        #endregion Private Methods

        #region Remote Debugger Command Helpers

        /// <summary>
        /// Debugger command pre processing result type.
        /// </summary>
        private enum PreProcessCommandResult
        {
            /// <summary>
            /// No debugger pre-processing. "Get" commands use this so that the
            /// data they retrieve can be sent back to the caller.
            /// </summary>
            None = 0,

            /// <summary>
            /// This is a valid debugger command but was not processed because
            /// the debugger state was not correct.
            /// </summary>
            ValidNotProcessed,

            /// <summary>
            /// SetDebuggerAction.
            /// </summary>
            SetDebuggerAction,

            /// <summary>
            /// SetDebugMode.
            /// </summary>
            SetDebugMode,

            /// <summary>
            /// SetDebuggerStepMode.
            /// </summary>
            SetDebuggerStepMode,

            /// <summary>
            /// SetPreserveUnhandledBreakpointMode.
            /// </summary>
            SetPreserveUnhandledBreakpointMode,

            /// <summary>
            /// The PreProcessCommandResult used for managing breakpoints.
            /// </summary>
            BreakpointManagement,
        }

        private sealed class DebuggerCommandArgument
        {
            public DebugModes? Mode { get; set; }

            public DebuggerResumeAction? ResumeAction { get; set; }

            public bool? DebuggerStepEnabled { get; set; }

            public UnhandledBreakpointProcessingMode? UnhandledBreakpointMode { get; set; }
        }

        /// <summary>
        /// Pre-processor for debugger commands.
        /// Parses special debugger commands and converts to equivalent script for remote execution as needed.
        /// </summary>
        /// <param name="commands">The PSCommand.</param>
        /// <param name="serverRemoteDebugger">The debugger that can be used to invoke debug operations via API.</param>
        /// <param name="preProcessOutput">A Collection that can be used to send output to the client.</param>
        /// <param name="commandArgument">Command argument.</param>
        /// <returns>PreProcessCommandResult type if preprocessing occurred.</returns>
        private static PreProcessCommandResult PreProcessDebuggerCommand(
            PSCommand commands,
            ServerRemoteDebugger serverRemoteDebugger,
            Collection<object> preProcessOutput,
            out DebuggerCommandArgument commandArgument)
        {
            commandArgument = new DebuggerCommandArgument();
            PreProcessCommandResult result = PreProcessCommandResult.None;

            if (commands.Commands.Count == 0 || commands.Commands[0].IsScript)
            {
                return result;
            }

            var command = commands.Commands[0];
            string commandText = command.CommandText;
            if (commandText.Equals(RemoteDebuggingCommands.GetDebuggerStopArgs, StringComparison.OrdinalIgnoreCase))
            {
                // __Get-PSDebuggerStopArgs private virtual command.
                // No input parameters.
                // Returns DebuggerStopEventArgs object.

                // Evaluate this command only if the debugger is activated.
                if (!serverRemoteDebugger.IsActive)
                {
                    return PreProcessCommandResult.ValidNotProcessed;
                }

                ReplaceVirtualCommandWithScript(commands, "$host.Runspace.Debugger.GetDebuggerStopArgs()");
            }
            else if (commandText.Equals(RemoteDebuggingCommands.SetDebuggerAction, StringComparison.OrdinalIgnoreCase))
            {
                // __Set-PSDebuggerAction private virtual command.
                // DebuggerResumeAction enum input parameter.
                // Returns void.

                // Evaluate this command only if the debugger is activated.
                if (!serverRemoteDebugger.IsActive)
                {
                    return PreProcessCommandResult.ValidNotProcessed;
                }

                if ((command.Parameters == null) || (command.Parameters.Count == 0) ||
                    (!command.Parameters[0].Name.Equals("ResumeAction", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PSArgumentException("ResumeAction");
                }

                DebuggerResumeAction? resumeAction = null;
                PSObject resumeObject = command.Parameters[0].Value as PSObject;
                if (resumeObject != null)
                {
                    try
                    {
                        resumeAction = (DebuggerResumeAction)resumeObject.BaseObject;
                    }
                    catch (InvalidCastException)
                    {
                        // Do nothing.
                    }
                }

                commandArgument.ResumeAction = resumeAction ?? throw new PSArgumentException("ResumeAction");
                result = PreProcessCommandResult.SetDebuggerAction;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.SetDebugMode, StringComparison.OrdinalIgnoreCase))
            {
                // __Set-PSDebugMode private virtual command.
                // DebugModes enum input parameter.
                // Returns void.
                if ((command.Parameters == null) || (command.Parameters.Count == 0) ||
                    (!command.Parameters[0].Name.Equals("Mode", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PSArgumentException("Mode");
                }

                DebugModes? mode = null;
                PSObject modeObject = command.Parameters[0].Value as PSObject;
                if (modeObject != null)
                {
                    try
                    {
                        mode = (DebugModes)modeObject.BaseObject;
                    }
                    catch (InvalidCastException)
                    {
                        // Do nothing.
                    }
                }

                commandArgument.Mode = mode ?? throw new PSArgumentException("Mode");
                result = PreProcessCommandResult.SetDebugMode;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.SetDebuggerStepMode, StringComparison.OrdinalIgnoreCase))
            {
                // __Set-PSDebuggerStepMode private virtual command.
                // Boolean Enabled input parameter.
                // Returns void.
                if ((command.Parameters == null) || (command.Parameters.Count == 0) ||
                   (!command.Parameters[0].Name.Equals("Enabled", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PSArgumentException("Enabled");
                }

                bool enabled = (bool)command.Parameters[0].Value;
                commandArgument.DebuggerStepEnabled = enabled;
                result = PreProcessCommandResult.SetDebuggerStepMode;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.SetUnhandledBreakpointMode, StringComparison.OrdinalIgnoreCase))
            {
                // __Set-PSUnhandledBreakpointMode private virtual command.
                // UnhandledBreakpointMode input parameter.
                // Returns void.
                if ((command.Parameters == null) || (command.Parameters.Count == 0) ||
                   (!command.Parameters[0].Name.Equals("UnhandledBreakpointMode", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PSArgumentException("UnhandledBreakpointMode");
                }

                UnhandledBreakpointProcessingMode? mode = null;
                PSObject modeObject = command.Parameters[0].Value as PSObject;
                if (modeObject != null)
                {
                    try
                    {
                        mode = (UnhandledBreakpointProcessingMode)modeObject.BaseObject;
                    }
                    catch (InvalidCastException)
                    {
                        // Do nothing.
                    }
                }

                commandArgument.UnhandledBreakpointMode = mode ?? throw new PSArgumentException("Mode");
                result = PreProcessCommandResult.SetPreserveUnhandledBreakpointMode;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.GetBreakpoint, StringComparison.OrdinalIgnoreCase))
            {
                // __Get-PSBreakpoint private virtual command.
                // Input parameters:
                // [-Id <int>]
                // Returns Breakpoint object(s).
                TryGetParameter<int?>(command, "RunspaceId", out int? runspaceId);
                if (TryGetParameter<int>(command, "Id", out int breakpointId))
                {
                    preProcessOutput.Add(serverRemoteDebugger.GetBreakpoint(breakpointId, runspaceId));
                }
                else
                {
                    foreach (Breakpoint breakpoint in serverRemoteDebugger.GetBreakpoints(runspaceId))
                    {
                        preProcessOutput.Add(breakpoint);
                    }
                }

                result = PreProcessCommandResult.BreakpointManagement;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.SetBreakpoint, StringComparison.OrdinalIgnoreCase))
            {
                // __Set-PSBreakpoint private virtual command.
                // Input parameters:
                // -Breakpoint <Breakpoint> or -BreakpointList <IEnumerable<Breakpoint>>
                // [-RunspaceId <int?>]
                // Returns Breakpoint object(s).
                TryGetParameter<Breakpoint>(command, "Breakpoint", out Breakpoint breakpoint);
                TryGetParameter<ArrayList>(command, "BreakpointList", out ArrayList breakpoints);
                if (breakpoint == null && breakpoints == null)
                {
                    throw new PSArgumentException(DebuggerStrings.BreakpointOrBreakpointListNotSpecified);
                }

                TryGetParameter<int?>(command, "RunspaceId", out int? runspaceId);

                commands.Clear();

                // Any collection comes through remoting as an ArrayList of Objects so we convert each object
                // into a breakpoint and add it to the list.
                var bps = new List<Breakpoint>();
                if (breakpoints != null)
                {
                    foreach (object obj in breakpoints)
                    {
                        if (!LanguagePrimitives.TryConvertTo<Breakpoint>(obj, out Breakpoint bp))
                        {
                            throw new PSArgumentException(DebuggerStrings.BreakpointListContainedANonBreakpoint);
                        }

                        bps.Add(bp);
                    }
                }
                else
                {
                    bps.Add(breakpoint);
                }

                serverRemoteDebugger.SetBreakpoints(bps, runspaceId);

                foreach (var bp in bps)
                {
                    preProcessOutput.Add(bp);
                }

                result = PreProcessCommandResult.BreakpointManagement;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.RemoveBreakpoint, StringComparison.OrdinalIgnoreCase))
            {
                // __Remove-PSBreakpoint private virtual command.
                // Input parameters:
                // -Id <int>
                // [-RunspaceId <int?>]
                // Returns bool.
                int breakpointId = GetParameter<int>(command, "Id");
                TryGetParameter<int?>(command, "RunspaceId", out int? runspaceId);

                Breakpoint breakpoint = serverRemoteDebugger.GetBreakpoint(breakpointId, runspaceId);
                preProcessOutput.Add(
                    breakpoint != null && serverRemoteDebugger.RemoveBreakpoint(breakpoint, runspaceId));

                result = PreProcessCommandResult.BreakpointManagement;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.EnableBreakpoint, StringComparison.OrdinalIgnoreCase))
            {
                // __Enable-PSBreakpoint private virtual command.
                // Input parameters:
                // -Id <int>
                // [-RunspaceId <int?>]
                // Returns Breakpoint object.
                int breakpointId = GetParameter<int>(command, "Id");
                TryGetParameter<int?>(command, "RunspaceId", out int? runspaceId);

                Breakpoint bp = serverRemoteDebugger.GetBreakpoint(breakpointId, runspaceId);
                if (bp != null)
                {
                    preProcessOutput.Add(serverRemoteDebugger.EnableBreakpoint(bp, runspaceId));
                }

                result = PreProcessCommandResult.BreakpointManagement;
            }
            else if (commandText.Equals(RemoteDebuggingCommands.DisableBreakpoint, StringComparison.OrdinalIgnoreCase))
            {
                // __Disable-PSBreakpoint private virtual command.
                // Input parameters:
                // -Id <int>
                // [-RunspaceId <int?>]
                // Returns Breakpoint object.
                int breakpointId = GetParameter<int>(command, "Id");
                TryGetParameter<int?>(command, "RunspaceId", out int? runspaceId);

                Breakpoint bp = serverRemoteDebugger.GetBreakpoint(breakpointId, runspaceId);
                if (bp != null)
                {
                    preProcessOutput.Add(serverRemoteDebugger.DisableBreakpoint(bp, runspaceId));
                }

                result = PreProcessCommandResult.BreakpointManagement;
            }

            return result;
        }

        private static void ReplaceVirtualCommandWithScript(PSCommand commands, string script)
        {
            ScriptBlock scriptBlock = ScriptBlock.Create(script);
            scriptBlock.LanguageMode = PSLanguageMode.FullLanguage;
            commands.Clear();
            commands.AddCommand("Invoke-Command")
                    .AddParameter("ScriptBlock", scriptBlock)
                    .AddParameter("NoNewScope", true);
        }

        private static T GetParameter<T>(Command command, string parameterName)
        {
            if (command.Parameters?.Count == 0)
            {
                throw new PSArgumentException(parameterName);
            }

            foreach (CommandParameter param in command.Parameters)
            {
                if (string.Equals(param.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return LanguagePrimitives.ConvertTo<T>(param.Value);
                }
            }

            throw new PSArgumentException(parameterName);
        }

        private static bool TryGetParameter<T>(Command command, string parameterName, out T value)
        {
            try
            {
                value = GetParameter<T>(command, parameterName);
                return true;
            }
            catch (Exception ex) when (
                ex is PSArgumentException ||
                ex is InvalidCastException ||
                ex is PSInvalidCastException)
            {
                value = default(T);
                return false;
            }
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// Helper class to run ServerPowerShellDriver objects on a single thread.  This is
        /// needed to support nested pipeline execution and remote debugging.
        /// </summary>
        private sealed class PowerShellDriverInvoker
        {
            #region Private Members

            private readonly ConcurrentStack<InvokePump> _invokePumpStack;

            #endregion

            #region Constructor

            /// <summary>
            /// Constructor.
            /// </summary>
            public PowerShellDriverInvoker()
            {
                _invokePumpStack = new ConcurrentStack<InvokePump>();
            }

            #endregion

            #region Properties

            /// <summary>
            /// IsActive.
            /// </summary>
            public bool IsActive
            {
                get { return !_invokePumpStack.IsEmpty; }
            }

            /// <summary>
            /// True if thread is ready to invoke a PowerShell driver.
            /// </summary>
            public bool IsAvailable
            {
                get
                {
                    InvokePump pump;
                    if (!_invokePumpStack.TryPeek(out pump))
                    {
                        pump = null;
                    }

                    return (pump != null) && !(pump.IsBusy);
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Submit a driver object to be invoked.
            /// </summary>
            /// <param name="driver">ServerPowerShellDriver.</param>
            public void InvokeDriverAsync(ServerPowerShellDriver driver)
            {
                InvokePump currentPump;
                if (!_invokePumpStack.TryPeek(out currentPump))
                {
                    throw new PSInvalidOperationException(RemotingErrorIdStrings.PowerShellInvokerInvalidState);
                }

                currentPump.Dispatch(driver);
            }

            /// <summary>
            /// Blocking call that creates a new pump object and pumps
            /// driver invokes until stopped via a PopInvoker call.
            /// </summary>
            public void PushInvoker()
            {
                InvokePump newPump = new InvokePump();
                _invokePumpStack.Push(newPump);

                // Blocking call while new driver invocations are handled on
                // new pump.
                newPump.Start();
            }

            /// <summary>
            /// Stops the current driver invoker and restores the previous
            /// invoker object on the stack, if any, to handle driver invocations.
            /// </summary>
            public void PopInvoker()
            {
                InvokePump oldPump;
                if (_invokePumpStack.TryPop(out oldPump))
                {
                    oldPump.Stop();
                }
                else
                {
                    throw new PSInvalidOperationException(RemotingErrorIdStrings.CannotExitNestedPipeline);
                }
            }

            #endregion

            #region Private classes

            /// <summary>
            /// Class that queues and invokes ServerPowerShellDriver objects
            /// in sequence.
            /// </summary>
            private sealed class InvokePump
            {
                private readonly Queue<ServerPowerShellDriver> _driverInvokeQueue;
                private readonly ManualResetEvent _processDrivers;
                private readonly object _syncObject;
                private bool _stopPump;
                private bool _isDisposed;

                public InvokePump()
                {
                    _driverInvokeQueue = new Queue<ServerPowerShellDriver>();
                    _processDrivers = new ManualResetEvent(false);
                    _syncObject = new object();
                }

                public void Start()
                {
                    try
                    {
                        while (true)
                        {
                            _processDrivers.WaitOne();

                            // Synchronously invoke one ServerPowerShellDriver at a time.
                            ServerPowerShellDriver driver = null;

                            lock (_syncObject)
                            {
                                if (_stopPump)
                                {
                                    break;
                                }

                                if (_driverInvokeQueue.Count > 0)
                                {
                                    driver = _driverInvokeQueue.Dequeue();
                                }

                                if (_driverInvokeQueue.Count == 0)
                                {
                                    _processDrivers.Reset();
                                }
                            }

                            if (driver != null)
                            {
                                try
                                {
                                    IsBusy = true;
                                    driver.InvokeMain();
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    IsBusy = false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        _isDisposed = true;
                        _processDrivers.Dispose();
                    }
                }

                public void Dispatch(ServerPowerShellDriver driver)
                {
                    CheckDisposed();

                    lock (_syncObject)
                    {
                        _driverInvokeQueue.Enqueue(driver);
                        _processDrivers.Set();
                    }
                }

                public void Stop()
                {
                    CheckDisposed();

                    lock (_syncObject)
                    {
                        _stopPump = true;
                        _processDrivers.Set();
                    }
                }

                public bool IsBusy { get; private set; }

                private void CheckDisposed()
                {
                    if (_isDisposed)
                    {
                        throw new ObjectDisposedException("InvokePump");
                    }
                }
            }

            #endregion
        }

        #endregion
    }

    /// <summary>
    /// This class wraps the script debugger for a ServerRunspacePoolDriver runspace.
    /// </summary>
    internal sealed class ServerRemoteDebugger : Debugger, IDisposable
    {
        #region Private Members

        private readonly IRSPDriverInvoke _driverInvoker;
        private readonly Runspace _runspace;
        private readonly ObjectRef<Debugger> _wrappedDebugger;
        private bool _inDebugMode;
        private DebuggerStopEventArgs _debuggerStopEventArgs;

        private ManualResetEventSlim _nestedDebugStopCompleteEvent;
        private bool _nestedDebugging;
        private ManualResetEventSlim _processCommandCompleteEvent;
        private ThreadCommandProcessing _threadCommandProcessing;

        private bool _raiseStopEventLocally;

        internal const string SetPSBreakCommandText = "Set-PSBreakpoint";

        #endregion

        #region Constructor

        private ServerRemoteDebugger() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="driverInvoker"></param>
        /// <param name="runspace"></param>
        /// <param name="debugger"></param>
        internal ServerRemoteDebugger(
            IRSPDriverInvoke driverInvoker,
            Runspace runspace,
            Debugger debugger)
        {
            if (driverInvoker == null)
            {
                throw new PSArgumentNullException(nameof(driverInvoker));
            }

            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            _driverInvoker = driverInvoker;
            _runspace = runspace;

            _wrappedDebugger = new ObjectRef<Debugger>(debugger);

            SetDebuggerCallbacks();

            _runspace.Name = "RemoteHost";
            _runspace.InternalDebugger = this;
        }

        #endregion

        #region Debugger overrides

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get { return _inDebugMode; }
        }

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">List of breakpoints.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.Value.SetBreakpoints(breakpoints, runspaceId);

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The breakpoint with the specified id.</returns>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            _wrappedDebugger.Value.GetBreakpoint(id, runspaceId);

        /// <summary>
        /// Returns breakpoints on a runspace.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A list of breakpoints in a runspace.</returns>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            _wrappedDebugger.Value.GetBreakpoints(runspaceId);

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.Value.SetCommandBreakpoint(command, action, path, runspaceId);

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
            _wrappedDebugger.Value.SetLineBreakpoint(path, line, column, action, runspaceId);

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
            _wrappedDebugger.Value.SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId);

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.RemoveBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.EnableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.DisableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Exits debugger mode with the provided resume action.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            if (!_inDebugMode)
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(DebuggerStrings.CannotSetRemoteDebuggerAction));
            }

            ExitDebugMode(resumeAction);
        }

        /// <summary>
        /// Returns debugger stop event args if in debugger stop state.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.Value.GetDebuggerStopArgs();
        }

        /// <summary>
        /// ProcessCommand.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="output">Output.</param>
        /// <returns></returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            if (LocalDebugMode)
            {
                return _wrappedDebugger.Value.ProcessCommand(command, output);
            }

            if (!InBreakpoint || (_threadCommandProcessing != null))
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(DebuggerStrings.CannotProcessDebuggerCommandNotStopped));
            }

            _processCommandCompleteEvent ??= new ManualResetEventSlim(false);

            _threadCommandProcessing = new ThreadCommandProcessing(command, output, _wrappedDebugger.Value, _processCommandCompleteEvent);
            try
            {
                return _threadCommandProcessing.Invoke(_nestedDebugStopCompleteEvent);
            }
            finally
            {
                _threadCommandProcessing = null;
            }
        }

        /// <summary>
        /// StopProcessCommand.
        /// </summary>
        public override void StopProcessCommand()
        {
            if (LocalDebugMode)
            {
                _wrappedDebugger.Value.StopProcessCommand();
            }

            ThreadCommandProcessing threadCommandProcessing = _threadCommandProcessing;
            threadCommandProcessing?.Stop();
        }

        /// <summary>
        /// SetDebugMode.
        /// </summary>
        /// <param name="mode"></param>
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.Value.SetDebugMode(mode);

            base.SetDebugMode(mode);
        }

        /// <summary>
        /// True when debugger is active with breakpoints.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                return (InBreakpoint || _wrappedDebugger.Value.IsActive || _wrappedDebugger.Value.InBreakpoint);
            }
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            // Enable both the wrapper and wrapped debuggers for debugging before setting step mode.
            const DebugModes mode = DebugModes.LocalScript | DebugModes.RemoteScript;
            base.SetDebugMode(mode);
            _wrappedDebugger.Value.SetDebugMode(mode);

            _wrappedDebugger.Value.SetDebuggerStepMode(enabled);
        }

        /// <summary>
        /// InternalProcessCommand.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        internal override DebuggerCommand InternalProcessCommand(string command, IList<PSObject> output)
        {
            return _wrappedDebugger.Value.InternalProcessCommand(command, output);
        }

        /// <summary>
        /// Sets up debugger to debug provided job or its child jobs.
        /// </summary>
        /// <param name="job">
        /// Job object that is either a debuggable job or a container of
        /// debuggable child jobs.
        /// </param>
        /// <param name="breakAll">
        /// If true, the debugger automatically invokes a break all when it
        /// attaches to the job.
        /// </param>
        internal override void DebugJob(Job job, bool breakAll) =>
            _wrappedDebugger.Value.DebugJob(job, breakAll);

        /// <summary>
        /// Removes job from debugger job list and pops its
        /// debugger from the active debugger stack.
        /// </summary>
        /// <param name="job">Job.</param>
        internal override void StopDebugJob(Job job)
        {
            _wrappedDebugger.Value.StopDebugJob(job);
        }

        /// <summary>
        /// Sets up debugger to debug provided Runspace in a nested debug session.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to debug.
        /// </param>
        /// <param name="breakAll">
        /// When true, this command will invoke a BreakAll when the debugger is
        /// first attached.
        /// </param>
        internal override void DebugRunspace(Runspace runspace, bool breakAll)
        {
            _wrappedDebugger.Value.DebugRunspace(runspace, breakAll);
        }

        /// <summary>
        /// Removes the provided Runspace from the nested "active" debugger state.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        internal override void StopDebugRunspace(Runspace runspace)
        {
            _wrappedDebugger.Value.StopDebugRunspace(runspace);
        }

        /// <summary>
        /// IsPushed.
        /// </summary>
        internal override bool IsPushed
        {
            get
            {
                return _wrappedDebugger.Value.IsPushed;
            }
        }

        /// <summary>
        /// IsRemote.
        /// </summary>
        internal override bool IsRemote
        {
            get
            {
                return _wrappedDebugger.Value.IsRemote;
            }
        }

        /// <summary>
        /// IsDebuggerSteppingEnabled.
        /// </summary>
        internal override bool IsDebuggerSteppingEnabled
        {
            get
            {
                return _wrappedDebugger.Value.IsDebuggerSteppingEnabled;
            }
        }

        /// <summary>
        /// UnhandledBreakpointMode.
        /// </summary>
        internal override UnhandledBreakpointProcessingMode UnhandledBreakpointMode
        {
            get
            {
                return _wrappedDebugger.Value.UnhandledBreakpointMode;
            }

            set
            {
                _wrappedDebugger.Value.UnhandledBreakpointMode = value;
                if (value == UnhandledBreakpointProcessingMode.Ignore &&
                    _inDebugMode)
                {
                    // Release debugger stop hold.
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
            }
        }

        /// <summary>
        /// IsPendingDebugStopEvent.
        /// </summary>
        internal override bool IsPendingDebugStopEvent
        {
            get { return _wrappedDebugger.Value.IsPendingDebugStopEvent; }
        }

        /// <summary>
        /// ReleaseSavedDebugStop.
        /// </summary>
        internal override void ReleaseSavedDebugStop()
        {
            _wrappedDebugger.Value.ReleaseSavedDebugStop();
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            return _wrappedDebugger.Value.GetCallStack();
        }

        internal override void Break(object triggerObject = null)
        {
            _wrappedDebugger.Value.Break(triggerObject);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            RemoveDebuggerCallbacks();
            if (_inDebugMode)
            {
                ExitDebugMode(DebuggerResumeAction.Stop);
            }

            _nestedDebugStopCompleteEvent?.Dispose();
            _processCommandCompleteEvent?.Dispose();
        }

        #endregion

        #region Private Classes

        private sealed class ThreadCommandProcessing
        {
            // Members
            private readonly ManualResetEventSlim _commandCompleteEvent;
            private readonly Debugger _wrappedDebugger;
            private readonly PSCommand _command;
            private readonly PSDataCollection<PSObject> _output;
            private DebuggerCommandResults _results;
            private Exception _exception;
#if !UNIX
            private WindowsIdentity _identityToImpersonate;
#endif

            // Constructors
            private ThreadCommandProcessing() { }

            public ThreadCommandProcessing(
                PSCommand command,
                PSDataCollection<PSObject> output,
                Debugger debugger,
                ManualResetEventSlim processCommandCompleteEvent)
            {
                _command = command;
                _output = output;
                _wrappedDebugger = debugger;
                _commandCompleteEvent = processCommandCompleteEvent;
            }

            // Methods
            public DebuggerCommandResults Invoke(ManualResetEventSlim startInvokeEvent)
            {
#if !UNIX
                // Get impersonation information to flow if any.
                Utils.TryGetWindowsImpersonatedIdentity(out _identityToImpersonate);
#endif

                // Signal thread to process command.
                Dbg.Assert(!_commandCompleteEvent.IsSet, "Command complete event shoulds always be non-signaled here.");
                Dbg.Assert(!startInvokeEvent.IsSet, "The event should always be in non-signaled state here.");
                startInvokeEvent.Set();

                // Wait for completion.
                _commandCompleteEvent.Wait();
                _commandCompleteEvent.Reset();
#if !UNIX
                if (_identityToImpersonate != null)
                {
                    _identityToImpersonate.Dispose();
                    _identityToImpersonate = null;
                }
#endif

                // Propagate exception.
                if (_exception != null)
                {
                    throw _exception;
                }

                // Return command processing results.
                return _results;
            }

            public void Stop()
            {
                Debugger debugger = _wrappedDebugger;
                debugger?.StopProcessCommand();
            }

            internal void DoInvoke()
            {
                try
                {
#if !UNIX
                    if (_identityToImpersonate != null)
                    {
                        _results = WindowsIdentity.RunImpersonated(
                            _identityToImpersonate.AccessToken,
                            () => _wrappedDebugger.ProcessCommand(_command, _output));
                        return;
                    }
#endif
                    _results = _wrappedDebugger.ProcessCommand(_command, _output);
                }
                catch (Exception e)
                {
                    _exception = e;
                }
                finally
                {
                    _commandCompleteEvent.Set();
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Add Debugger suspend execution callback.
        /// </summary>
        private void SetDebuggerCallbacks()
        {
            if (_runspace != null &&
                _runspace.ExecutionContext != null &&
                _wrappedDebugger.Value != null)
            {
                SubscribeWrappedDebugger(_wrappedDebugger.Value);

                // Register debugger events for remote forwarding.
                var eventManager = _runspace.ExecutionContext.Events;

                if (!eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerStopEvent).GetEnumerator().MoveNext())
                {
                    eventManager.SubscribeEvent(
                        source: null,
                        eventName: null,
                        sourceIdentifier: RemoteDebugger.RemoteDebuggerStopEvent,
                        data: null,
                        action: null,
                        supportEvent: true,
                        forwardEvent: true);
                }

                if (!eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent).GetEnumerator().MoveNext())
                {
                    eventManager.SubscribeEvent(
                        source: null,
                        eventName: null,
                        sourceIdentifier: RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent,
                        data: null,
                        action: null,
                        supportEvent: true,
                        forwardEvent: true);
                }
            }
        }

        /// <summary>
        /// Remove the suspend execution callback.
        /// </summary>
        private void RemoveDebuggerCallbacks()
        {
            if (_runspace != null &&
                _runspace.ExecutionContext != null &&
                _wrappedDebugger.Value != null)
            {
                UnsubscribeWrappedDebugger(_wrappedDebugger.Value);

                // Unregister debugger events for remote forwarding.
                var eventManager = _runspace.ExecutionContext.Events;

                foreach (var subscriber in eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerStopEvent))
                {
                    eventManager.UnsubscribeEvent(subscriber);
                }

                foreach (var subscriber in eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent))
                {
                    eventManager.UnsubscribeEvent(subscriber);
                }
            }
        }

        /// <summary>
        /// Handler for debugger events.
        /// </summary>
        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            // Ignore if we are in restricted mode.
            if (!IsDebuggingSupported())
            {
                return;
            }

            if (LocalDebugMode)
            {
                // Forward event locally.
                RaiseDebuggerStopEvent(e);
                return;
            }

            if ((DebugMode & DebugModes.RemoteScript) != DebugModes.RemoteScript)
            {
                return;
            }

            _debuggerStopEventArgs = e;
            PSHost contextHost = null;

            try
            {
                // Save current context remote host.
                contextHost = _runspace.ExecutionContext.InternalHost.ExternalHost;

                // Forward event to remote client.
                Dbg.Assert(_runspace != null, "Runspace cannot be null.");
                _runspace.ExecutionContext.Events.GenerateEvent(
                    sourceIdentifier: RemoteDebugger.RemoteDebuggerStopEvent,
                    sender: null,
                    args: new object[] { e },
                    extraData: null);

                //
                // Start the debug mode.  This is a blocking call and will return only
                // after ExitDebugMode() is called.
                //
                EnterDebugMode(_wrappedDebugger.Value.IsPushed);

                // Restore original context remote host.
                _runspace.ExecutionContext.InternalHost.SetHostRef(contextHost);
            }
            catch (Exception)
            {
            }
            finally
            {
                _debuggerStopEventArgs = null;
            }
        }

        /// <summary>
        /// HandleBreakpointUpdated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            // Ignore if we are in restricted mode.
            if (!IsDebuggingSupported())
            {
                return;
            }

            if (LocalDebugMode)
            {
                // Forward event locally.
                RaiseBreakpointUpdatedEvent(e);
                return;
            }

            try
            {
                // Forward event to remote client.
                Dbg.Assert(_runspace != null, "Runspace cannot be null.");
                _runspace.ExecutionContext.Events.GenerateEvent(
                    sourceIdentifier: RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent,
                    sender: null,
                    args: new object[] { e },
                    extraData: null);
            }
            catch (Exception)
            {
            }
        }

        private void HandleNestedDebuggingCancelEvent(object sender, EventArgs e)
        {
            // Forward cancel event from wrapped debugger.
            RaiseNestedDebuggingCancelEvent();

            // Release debugger.
            if (_inDebugMode)
            {
                ExitDebugMode(DebuggerResumeAction.Continue);
            }
        }

        /// <summary>
        /// Sends a DebuggerStop event to the client and enters a nested pipeline.
        /// </summary>
        private void EnterDebugMode(bool isNestedStop)
        {
            _inDebugMode = true;

            try
            {
                _runspace.ExecutionContext.SetVariable(SpecialVariables.NestedPromptCounterVarPath, 1);

                if (isNestedStop)
                {
                    // Blocking call for nested debugger execution (Debug-Runspace) stop events.
                    // The root debugger never makes two EnterDebugMode calls without an ExitDebugMode.
                    _nestedDebugStopCompleteEvent ??= new ManualResetEventSlim(false);

                    _nestedDebugging = true;
                    OnEnterDebugMode(_nestedDebugStopCompleteEvent);
                }
                else
                {
                    // Blocking call.
                    // Process all client commands as nested until nested pipeline is exited at
                    // which point this call returns.
                    _driverInvoker.EnterNestedPipeline();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _inDebugMode = false;
                _nestedDebugging = false;
            }

            // Check to see if we should re-raise the stop event locally.
            if (_raiseStopEventLocally)
            {
                _raiseStopEventLocally = false;
                LocalDebugMode = true;
                HandleDebuggerStop(this, _debuggerStopEventArgs);
            }
        }

        /// <summary>
        /// Blocks DebuggerStop event thread until exit debug mode is
        /// received from the client.
        /// </summary>
        private void OnEnterDebugMode(ManualResetEventSlim debugModeCompletedEvent)
        {
            Dbg.Assert(!debugModeCompletedEvent.IsSet, "Event should always be non-signaled here.");

            while (true)
            {
                debugModeCompletedEvent.Wait();
                debugModeCompletedEvent.Reset();

                if (_threadCommandProcessing != null)
                {
                    // Process command.
                    _threadCommandProcessing.DoInvoke();
                    _threadCommandProcessing = null;
                }
                else
                {
                    // No command to process.  Exit debug mode.
                    break;
                }
            }
        }

        /// <summary>
        /// Exits the server side nested pipeline.
        /// </summary>
        private void ExitDebugMode(DebuggerResumeAction resumeAction)
        {
            _debuggerStopEventArgs.ResumeAction = resumeAction;

            try
            {
                if (_nestedDebugging)
                {
                    // Release nested debugger.
                    _nestedDebugStopCompleteEvent.Set();
                }
                else
                {
                    // Release EnterDebugMode blocking call.
                    _driverInvoker.ExitNestedPipeline();
                }

                _runspace.ExecutionContext.SetVariable(SpecialVariables.NestedPromptCounterVarPath, 0);
            }
            catch (Exception)
            {
            }
        }

        private void SubscribeWrappedDebugger(Debugger wrappedDebugger)
        {
            wrappedDebugger.DebuggerStop += HandleDebuggerStop;
            wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            wrappedDebugger.NestedDebuggingCancelledEvent += HandleNestedDebuggingCancelEvent;
        }

        private void UnsubscribeWrappedDebugger(Debugger wrappedDebugger)
        {
            wrappedDebugger.DebuggerStop -= HandleDebuggerStop;
            wrappedDebugger.BreakpointUpdated -= HandleBreakpointUpdated;
            wrappedDebugger.NestedDebuggingCancelledEvent -= HandleNestedDebuggingCancelEvent;
        }

        private bool IsDebuggingSupported()
        {
            // Restriction only occurs on a (non-pushed) local runspace.
            LocalRunspace localRunspace = _runspace as LocalRunspace;
            if (localRunspace != null)
            {
                CmdletInfo cmdletInfo = localRunspace.ExecutionContext.EngineSessionState.GetCmdlet(SetPSBreakCommandText);
                if ((cmdletInfo != null) && (cmdletInfo.Visibility != SessionStateEntryVisibility.Public))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// HandleStopSignal.
        /// </summary>
        /// <returns>True if stop signal is handled.</returns>
        internal bool HandleStopSignal()
        {
            // If in pushed mode then stop any running command.
            if (IsPushed && (_threadCommandProcessing != null))
            {
                StopProcessCommand();
                return true;
            }

            // Set debug mode to "None" so that current command can stop and not
            // potentially not respond in a debugger stop.  Use RestoreDebugger() to
            // restore debugger to original mode.
            _wrappedDebugger.Value.SetDebugMode(DebugModes.None);
            if (InBreakpoint)
            {
                try
                {
                    SetDebuggerAction(DebuggerResumeAction.Continue);
                }
                catch (PSInvalidOperationException) { }
            }

            return false;
        }

        // Sets the wrapped debugger to the same mode as the wrapper
        // server remote debugger, enabling it if remote debugging is enabled.
        internal void CheckDebuggerState()
        {
            if ((_wrappedDebugger.Value.DebugMode == DebugModes.None &&
                (DebugMode & DebugModes.RemoteScript) == DebugModes.RemoteScript))
            {
                _wrappedDebugger.Value.SetDebugMode(DebugMode);
            }
        }

        internal void StartPowerShellCommand(
            PowerShell powershell,
            Guid powershellId,
            Guid runspacePoolId,
            ServerRunspacePoolDriver runspacePoolDriver,
            ApartmentState apartmentState,
            ServerRemoteHost remoteHost,
            HostInfo hostInfo,
            RemoteStreamOptions streamOptions,
            bool addToHistory)
        {
            // For nested debugger command processing, invoke command on new local runspace since
            // the root script debugger runspace is unavailable (it is running a PS script).
            Runspace runspace = (remoteHost != null) ?
                RunspaceFactory.CreateRunspace(remoteHost) : RunspaceFactory.CreateRunspace();

            runspace.Open();

            try
            {
                powershell.InvocationStateChanged += HandlePowerShellInvocationStateChanged;
                powershell.SetIsNested(false);

                const string script = @"
                    param ($Debugger, $Commands, $output)
                    trap { throw $_ }

                    $Debugger.ProcessCommand($Commands, $output)
                    ";

                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                PSCommand Commands = new PSCommand(powershell.Commands);
                powershell.Commands.Clear();
                powershell.AddScript(script).AddParameter("Debugger", this).AddParameter("Commands", Commands).AddParameter("output", output);
                ServerPowerShellDriver driver = new ServerPowerShellDriver(
                    powershell,
                    null,
                    true,
                    powershellId,
                    runspacePoolId,
                    runspacePoolDriver,
                    apartmentState,
                    hostInfo,
                    streamOptions,
                    addToHistory,
                    runspace,
                    output);

                driver.Start();
            }
            catch (Exception)
            {
                runspace.Close();
                runspace.Dispose();
            }
        }

        private void HandlePowerShellInvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            if (e.InvocationStateInfo.State == PSInvocationState.Completed ||
                e.InvocationStateInfo.State == PSInvocationState.Stopped ||
                e.InvocationStateInfo.State == PSInvocationState.Failed)
            {
                PowerShell powershell = sender as PowerShell;
                powershell.InvocationStateChanged -= HandlePowerShellInvocationStateChanged;

                Runspace runspace = powershell.GetRunspaceConnection() as Runspace;
                runspace.Close();
                runspace.Dispose();
            }
        }

        internal int GetBreakpointCount()
        {
            ScriptDebugger scriptDebugger = _wrappedDebugger.Value as ScriptDebugger;
            if (scriptDebugger != null)
            {
                return scriptDebugger.GetBreakpoints().Count;
            }
            else
            {
                return 0;
            }
        }

        internal void PushDebugger(Debugger debugger)
        {
            if (debugger == null)
            {
                return;
            }

            if (debugger.Equals(this))
            {
                throw new PSInvalidOperationException(DebuggerStrings.RemoteServerDebuggerCannotPushSelf);
            }

            if (_wrappedDebugger.IsOverridden)
            {
                throw new PSInvalidOperationException(DebuggerStrings.RemoteServerDebuggerAlreadyPushed);
            }

            // Swap wrapped debugger.
            UnsubscribeWrappedDebugger(_wrappedDebugger.Value);
            _wrappedDebugger.Override(debugger);
            SubscribeWrappedDebugger(_wrappedDebugger.Value);
        }

        internal void PopDebugger()
        {
            if (!_wrappedDebugger.IsOverridden)
            {
                return;
            }

            // Swap wrapped debugger.
            UnsubscribeWrappedDebugger(_wrappedDebugger.Value);
            _wrappedDebugger.Revert();
            SubscribeWrappedDebugger(_wrappedDebugger.Value);
        }

        internal void ReleaseAndRaiseDebugStopLocal()
        {
            if (_inDebugMode)
            {
                // Release debugger stop and signal to re-raise locally.
                _raiseStopEventLocally = true;
                ExitDebugMode(DebuggerResumeAction.Continue);
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// When true, this debugger is being used for local debugging (not remote debugging)
        /// via the Debug-Runspace cmdlet.
        /// </summary>
        internal bool LocalDebugMode
        {
            get;
            set;
        }

        #endregion
    }
}
