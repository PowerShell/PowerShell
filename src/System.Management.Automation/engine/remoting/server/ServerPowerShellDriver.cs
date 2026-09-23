// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Security.Principal;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This class wraps a PowerShell object. It is used to function
    /// as a server side powershell.
    /// </summary>
    internal class ServerPowerShellDriver
    {
        #region Private Members

        private bool _extraPowerShellAlreadyScheduled;

        // extra PowerShell at the server to be run after localPowerShell
        private readonly PowerShell _extraPowerShell;

        // output buffer for the local PowerShell that is associated with this powershell driver
        // associated with this powershell data structure handler object to handle all communications with the client
        private readonly PSDataCollection<PSObject> _localPowerShellOutput;

        // if the remaining data has been sent to the client before sending state information
        private readonly bool[] _datasent = new bool[2];

        // sync object for synchronizing sending data to client
        private readonly object _syncObject = new object();

        // there is no input when this driver was created
        private readonly bool _noInput;
        private readonly bool _addToHistory;

        // the server remote host instance
        // associated with this powershell
        private readonly ServerRemoteHost _remoteHost;

        // apartment state for this powershell
        private readonly ApartmentState apartmentState;

        // Handles nested invocation of PS drivers.
        private readonly IRSPDriverInvoke _psDriverInvoker;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers.
        /// </summary>
        /// <param name="powershell">Decoded powershell object.</param>
        /// <param name="extraPowerShell">Extra pipeline to be run after <paramref name="powershell"/> completes.</param>
        /// <param name="noInput">Whether there is input for this powershell.</param>
        /// <param name="clientPowerShellId">The client powershell id.</param>
        /// <param name="clientRunspacePoolId">The client runspacepool id.</param>
        /// <param name="runspacePoolDriver">runspace pool driver
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">Apartment state for this powershell.</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">Serialization options for the streams in this powershell.</param>
        /// <param name="addToHistory">
        /// true if the command is to be added to history list of the runspace. false, otherwise.
        /// </param>
        /// <param name="rsToUse">
        /// If not null, this Runspace will be used to invoke Powershell.
        /// If null, the RunspacePool pointed by <paramref name="runspacePoolDriver"/> will be used.
        /// </param>
        internal ServerPowerShellDriver(PowerShell powershell, PowerShell extraPowerShell, bool noInput, Guid clientPowerShellId,
            Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
            ApartmentState apartmentState, HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse)
            : this(powershell, extraPowerShell, noInput, clientPowerShellId, clientRunspacePoolId, runspacePoolDriver,
                   apartmentState, hostInfo, streamOptions, addToHistory, rsToUse, null)
        {
        }

        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers.
        /// </summary>
        /// <param name="powershell">Decoded powershell object.</param>
        /// <param name="extraPowerShell">Extra pipeline to be run after <paramref name="powershell"/> completes.</param>
        /// <param name="noInput">Whether there is input for this powershell.</param>
        /// <param name="clientPowerShellId">The client powershell id.</param>
        /// <param name="clientRunspacePoolId">The client runspacepool id.</param>
        /// <param name="runspacePoolDriver">runspace pool driver
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">Apartment state for this powershell.</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">Serialization options for the streams in this powershell.</param>
        /// <param name="addToHistory">
        /// true if the command is to be added to history list of the runspace. false, otherwise.
        /// </param>
        /// <param name="rsToUse">
        /// If not null, this Runspace will be used to invoke Powershell.
        /// If null, the RunspacePool pointed by <paramref name="runspacePoolDriver"/> will be used.
        /// </param>
        /// <param name="output">
        /// If not null, this is used as another source of output sent to the client.
        /// </param>
        internal ServerPowerShellDriver(PowerShell powershell, PowerShell extraPowerShell, bool noInput, Guid clientPowerShellId,
            Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
            ApartmentState apartmentState, HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse, PSDataCollection<PSObject> output)
        {
            InstanceId = clientPowerShellId;
            RunspacePoolId = clientRunspacePoolId;
            RemoteStreamOptions = streamOptions;
            this.apartmentState = apartmentState;
            LocalPowerShell = powershell;
            _extraPowerShell = extraPowerShell;
            _localPowerShellOutput = new PSDataCollection<PSObject>();
            _noInput = noInput;
            _addToHistory = addToHistory;
            _psDriverInvoker = runspacePoolDriver;

            DataStructureHandler = runspacePoolDriver.DataStructureHandler.CreatePowerShellDataStructureHandler(clientPowerShellId, clientRunspacePoolId, RemoteStreamOptions, LocalPowerShell);
            _remoteHost = DataStructureHandler.GetHostAssociatedWithPowerShell(hostInfo, runspacePoolDriver.ServerRemoteHost);

            if (!noInput)
            {
                InputCollection = new PSDataCollection<object>();
                InputCollection.ReleaseOnEnumeration = true;
                InputCollection.IdleEvent += HandleIdleEvent;
            }

            RegisterPipelineOutputEventHandlers(_localPowerShellOutput);

            if (LocalPowerShell != null)
            {
                RegisterPowerShellEventHandlers(LocalPowerShell);
                _datasent[0] = false;
            }

            if (extraPowerShell != null)
            {
                RegisterPowerShellEventHandlers(extraPowerShell);
                _datasent[1] = false;
            }

            RegisterDataStructureHandlerEventHandlers(DataStructureHandler);

            // set the runspace pool and invoke this powershell
            if (rsToUse != null)
            {
                LocalPowerShell.Runspace = rsToUse;
                if (extraPowerShell != null)
                {
                    extraPowerShell.Runspace = rsToUse;
                }
            }
            else
            {
                LocalPowerShell.RunspacePool = runspacePoolDriver.RunspacePool;
                if (extraPowerShell != null)
                {
                    extraPowerShell.RunspacePool = runspacePoolDriver.RunspacePool;
                }
            }

            if (output != null)
            {
                output.DataAdded += (sender, args) =>
                    {
                        if (_localPowerShellOutput.IsOpen)
                        {
                            var items = output.ReadAll();
                            foreach (var item in items)
                            {
                                _localPowerShellOutput.Add(item);
                            }
                        }
                    };
            }
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Input collection sync object.
        /// </summary>
        internal PSDataCollection<object> InputCollection { get; }

        /// <summary>
        /// Local PowerShell instance.
        /// </summary>
        internal PowerShell LocalPowerShell { get; }

        /// <summary>
        /// Instance id by which this powershell driver is
        /// identified. This is the same as the id of the
        /// powershell on the client side.
        /// </summary>
        internal Guid InstanceId { get; }

        /// <summary>
        /// Serialization options for the streams in this powershell.
        /// </summary>
        internal RemoteStreamOptions RemoteStreamOptions { get; }

        /// <summary>
        /// Id of the runspace pool driver which created
        /// this object. This is the same as the id of
        /// the runspace pool at the client side which
        /// is associated with the powershell on the
        /// client side.
        /// </summary>
        internal Guid RunspacePoolId { get; }

        /// <summary>
        /// ServerPowerShellDataStructureHandler associated with this
        /// powershell driver.
        /// </summary>
        internal ServerPowerShellDataStructureHandler DataStructureHandler { get; }

        private PSInvocationSettings PrepInvoke(bool startMainPowerShell)
        {
            if (startMainPowerShell)
            {
                // prepare transport manager for sending and receiving data.
                DataStructureHandler.Prepare();
            }

            PSInvocationSettings settings = new PSInvocationSettings();
            settings.ApartmentState = apartmentState;
            settings.Host = _remoteHost;

            // Flow the impersonation policy to pipeline execution thread
            // only if the current thread is impersonated (Delegation is
            // also a kind of impersonation).
            if (Platform.IsWindows)
            {
                WindowsIdentity currentThreadIdentity = WindowsIdentity.GetCurrent();
                switch (currentThreadIdentity.ImpersonationLevel)
                {
                    case TokenImpersonationLevel.Impersonation:
                    case TokenImpersonationLevel.Delegation:
                        settings.FlowImpersonationPolicy = true;
                        break;
                    default:
                        settings.FlowImpersonationPolicy = false;
                        break;
                }
            }
            else
            {
                settings.FlowImpersonationPolicy = false;
            }

            settings.AddToHistory = _addToHistory;
            return settings;
        }

        private IAsyncResult Start(bool startMainPowerShell)
        {
            PSInvocationSettings settings = PrepInvoke(startMainPowerShell);

            if (startMainPowerShell)
            {
                return LocalPowerShell.BeginInvoke<object, PSObject>(InputCollection, _localPowerShellOutput, settings, null, null);
            }
            else
            {
                return _extraPowerShell.BeginInvoke<object, PSObject>(InputCollection, _localPowerShellOutput, settings, null, null);
            }
        }

        /// <summary>
        /// Invokes the powershell asynchronously.
        /// </summary>
        internal IAsyncResult Start()
        {
            return Start(true);
        }

        /// <summary>
        /// Runs no command but allows the PowerShell object on the client
        /// to complete.  This is used for running "virtual" remote debug
        /// commands that sets debugger state but doesn't run any command
        /// on the server runspace.
        /// </summary>
        /// <param name="output">The output from preprocessing that we want to send to the client.</param>
        internal void RunNoOpCommand(IReadOnlyCollection<object> output)
        {
            if (LocalPowerShell != null)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    (state) =>
                        {
                            LocalPowerShell.SetStateChanged(
                                new PSInvocationStateInfo(
                                    PSInvocationState.Running, null));

                            foreach (var item in output)
                            {
                                if (item != null)
                                {
                                    _localPowerShellOutput.Add(PSObject.AsPSObject(item));
                                }
                            }

                            LocalPowerShell.SetStateChanged(
                                new PSInvocationStateInfo(
                                    PSInvocationState.Completed, null));
                        });
            }
        }

        /// <summary>
        /// Invokes the Main PowerShell object synchronously.
        /// </summary>
        internal void InvokeMain()
        {
            PSInvocationSettings settings = PrepInvoke(true);

            Exception ex = null;
            try
            {
                LocalPowerShell.InvokeWithDebugger(InputCollection, _localPowerShellOutput, settings, true);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                // Since this is being invoked asynchronously on a single pipeline thread
                // any invoke failures (such as possible debugger failures) need to be
                // passed back to client or the original client invoke request will not respond.
                string failedCommand = LocalPowerShell.Commands.Commands[0].CommandText;
                LocalPowerShell.Commands.Clear();
                string msg = StringUtil.Format(
                    RemotingErrorIdStrings.ServerSideNestedCommandInvokeFailed,
                    failedCommand ?? string.Empty,
                    ex.Message ?? string.Empty);

                LocalPowerShell.AddCommand("Write-Error").AddArgument(msg);
                LocalPowerShell.Invoke();
            }
        }

        #endregion Internal Methods

        #region Private Methods

        private void RegisterPowerShellEventHandlers(PowerShell powerShell)
        {
            powerShell.InvocationStateChanged += HandlePowerShellInvocationStateChanged;

            powerShell.Streams.Error.DataAdded += HandleErrorDataAdded;
            powerShell.Streams.Debug.DataAdded += HandleDebugAdded;
            powerShell.Streams.Verbose.DataAdded += HandleVerboseAdded;
            powerShell.Streams.Warning.DataAdded += HandleWarningAdded;
            powerShell.Streams.Progress.DataAdded += HandleProgressAdded;
            powerShell.Streams.Information.DataAdded += HandleInformationAdded;
        }

        private void UnregisterPowerShellEventHandlers(PowerShell powerShell)
        {
            powerShell.InvocationStateChanged -= HandlePowerShellInvocationStateChanged;

            powerShell.Streams.Error.DataAdded -= HandleErrorDataAdded;
            powerShell.Streams.Debug.DataAdded -= HandleDebugAdded;
            powerShell.Streams.Verbose.DataAdded -= HandleVerboseAdded;
            powerShell.Streams.Warning.DataAdded -= HandleWarningAdded;
            powerShell.Streams.Progress.DataAdded -= HandleProgressAdded;
            powerShell.Streams.Information.DataAdded -= HandleInformationAdded;
        }

        private void RegisterDataStructureHandlerEventHandlers(ServerPowerShellDataStructureHandler dsHandler)
        {
            dsHandler.InputEndReceived += HandleInputEndReceived;
            dsHandler.InputReceived += HandleInputReceived;
            dsHandler.StopPowerShellReceived += HandleStopReceived;
            dsHandler.HostResponseReceived += HandleHostResponseReceived;
            dsHandler.OnSessionConnected += HandleSessionConnected;
        }

        private void UnregisterDataStructureHandlerEventHandlers(ServerPowerShellDataStructureHandler dsHandler)
        {
            dsHandler.InputEndReceived -= HandleInputEndReceived;
            dsHandler.InputReceived -= HandleInputReceived;
            dsHandler.StopPowerShellReceived -= HandleStopReceived;
            dsHandler.HostResponseReceived -= HandleHostResponseReceived;
            dsHandler.OnSessionConnected -= HandleSessionConnected;
        }

        private void RegisterPipelineOutputEventHandlers(PSDataCollection<PSObject> pipelineOutput)
        {
            pipelineOutput.DataAdded += HandleOutputDataAdded;
        }

        private void UnregisterPipelineOutputEventHandlers(PSDataCollection<PSObject> pipelineOutput)
        {
            pipelineOutput.DataAdded -= HandleOutputDataAdded;
        }

        /// <summary>
        /// Handle state changed information from PowerShell
        /// and send it to the client.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">arguments describing state changed
        /// information for this powershell</param>
        private void HandlePowerShellInvocationStateChanged(object sender,
            PSInvocationStateChangedEventArgs eventArgs)
        {
            PSInvocationState state = eventArgs.InvocationStateInfo.State;
            switch (state)
            {
                case PSInvocationState.Completed:
                case PSInvocationState.Failed:
                case PSInvocationState.Stopped:
                    {
                        if (LocalPowerShell.RunningExtraCommands)
                        {
                            // If completed successfully then allow extra commands to run.
                            if (state == PSInvocationState.Completed)
                            {
                                return;
                            }

                            // For failed or stopped state, extra commands cannot run and
                            // we allow this command invocation to finish.
                        }

                        // send the remaining data before sending in
                        // state information. This is required because
                        // the client side runspace pool will remove
                        // the association with the client side powershell
                        // once the powershell reaches a terminal state.
                        // If the association is removed, then any data
                        // sent to the powershell will be discarded by
                        // the runspace pool data structure handler on the client side
                        SendRemainingData();

                        if (state == PSInvocationState.Completed &&
                            (_extraPowerShell != null) &&
                            !_extraPowerShellAlreadyScheduled)
                        {
                            _extraPowerShellAlreadyScheduled = true;
                            Start(false);
                        }
                        else
                        {
                            DataStructureHandler.RaiseRemoveAssociationEvent();

                            // send the state change notification to the client
                            DataStructureHandler.SendStateChangedInformationToClient(
                                eventArgs.InvocationStateInfo);

                            UnregisterPowerShellEventHandlers(LocalPowerShell);
                            if (_extraPowerShell != null)
                            {
                                UnregisterPowerShellEventHandlers(_extraPowerShell);
                            }

                            UnregisterDataStructureHandlerEventHandlers(DataStructureHandler);
                            UnregisterPipelineOutputEventHandlers(_localPowerShellOutput);

                            // BUGBUG: currently the local powershell cannot
                            // be disposed as raising the events is
                            // not done towards the end. Need to fix
                            // powershell in order to get this enabled
                            // localPowerShell.Dispose();
                        }
                    }

                    break;

                case PSInvocationState.Stopping:
                    {
                        // abort all pending host calls
                        _remoteHost.ServerMethodExecutor.AbortAllCalls();
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles DataAdded event from the Output of the powershell.
        /// </summary>
        /// <param name="sender">Sender of this information.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void HandleOutputDataAdded(object sender, DataAddedEventArgs e)
        {
            int index = e.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if (!_datasent[indexIntoDataSent])
                {
                    PSObject data = _localPowerShellOutput[index];
                    // once send the output is removed so that the same
                    // is not sent again by SendRemainingData() method
                    _localPowerShellOutput.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendOutputDataToClient(data);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded event from Error of the PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void HandleErrorDataAdded(object sender, DataAddedEventArgs e)
        {
            int index = e.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    ErrorRecord errorRecord = LocalPowerShell.Streams.Error[index];
                    // once send the error record is removed so that the same
                    // is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Error.RemoveAt(index);

                    // send the error record to the client
                    DataStructureHandler.SendErrorRecordToClient(errorRecord);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded event from Progress of PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleProgressAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    ProgressRecord data = LocalPowerShell.Streams.Progress[index];
                    // once the debug message is sent, it is removed so that
                    // the same is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Progress.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendProgressRecordToClient(data);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded event from Warning of PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleWarningAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    WarningRecord data = LocalPowerShell.Streams.Warning[index];
                    // once the debug message is sent, it is removed so that
                    // the same is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Warning.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendWarningRecordToClient(data);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded from Verbose of PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">Sender of this information.</param>
        private void HandleVerboseAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    VerboseRecord data = LocalPowerShell.Streams.Verbose[index];
                    // once the debug message is sent, it is removed so that
                    // the same is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Verbose.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendVerboseRecordToClient(data);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded from Debug of PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">Sender of this information.</param>
        private void HandleDebugAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    DebugRecord data = LocalPowerShell.Streams.Debug[index];
                    // once the debug message is sent, it is removed so that
                    // the same is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Debug.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendDebugRecordToClient(data);
                }
            }
        }

        /// <summary>
        /// Handles DataAdded from Information of PowerShell.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">Sender of this information.</param>
        private void HandleInformationAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (_syncObject)
            {
                int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!_datasent[indexIntoDataSent]))
                {
                    InformationRecord data = LocalPowerShell.Streams.Information[index];
                    // once the Information message is sent, it is removed so that
                    // the same is not sent again by SendRemainingData() method
                    LocalPowerShell.Streams.Information.RemoveAt(index);

                    // send the output data to the client
                    DataStructureHandler.SendInformationRecordToClient(data);
                }
            }
        }

        /// <summary>
        /// Send the remaining output and error information to
        /// client.
        /// </summary>
        /// <remarks>This method should be called before
        /// sending the state information. The client will
        /// remove the association between a powershell and
        /// runspace pool if it receives any of the terminal
        /// states. Hence all the remaining data should be
        /// sent before this happens. Else the data will be
        /// discarded</remarks>
        private void SendRemainingData()
        {
            int indexIntoDataSent = (!_extraPowerShellAlreadyScheduled) ? 0 : 1;
            lock (_syncObject)
            {
                _datasent[indexIntoDataSent] = true;
            }

            try
            {
                // BUGBUG: change this code to use enumerator
                // blocked on bug #108824, to be fixed by Kriscv
                for (int i = 0; i < _localPowerShellOutput.Count; i++)
                {
                    PSObject data = _localPowerShellOutput[i];
                    DataStructureHandler.SendOutputDataToClient(data);
                }

                _localPowerShellOutput.Clear();

                // foreach (ErrorRecord errorRecord in localPowerShell.Error)
                for (int i = 0; i < LocalPowerShell.Streams.Error.Count; i++)
                {
                    ErrorRecord errorRecord = LocalPowerShell.Streams.Error[i];
                    DataStructureHandler.SendErrorRecordToClient(errorRecord);
                }

                LocalPowerShell.Streams.Error.Clear();
            }
            finally
            {
                lock (_syncObject)
                {
                    // reset to original state so other pipelines can stream.
                    _datasent[indexIntoDataSent] = true;
                }
            }
        }

        /// <summary>
        /// Stop the local powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Unused.</param>
        private void HandleStopReceived(object sender, EventArgs eventArgs)
        {
            do // false loop
            {
                if (LocalPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped ||
                    LocalPowerShell.InvocationStateInfo.State == PSInvocationState.Completed ||
                    LocalPowerShell.InvocationStateInfo.State == PSInvocationState.Failed ||
                    LocalPowerShell.InvocationStateInfo.State == PSInvocationState.Stopping)
                {
                    break;
                }
                else
                {
                    // Ensure that the local PowerShell command is not stopped in debug mode.
                    bool handledByDebugger = false;
                    if (!LocalPowerShell.IsNested &&
                        _psDriverInvoker != null)
                    {
                        handledByDebugger = _psDriverInvoker.HandleStopSignal();
                    }

                    if (!handledByDebugger)
                    {
                        LocalPowerShell.Stop();
                    }
                }
            } while (false);

            if (_extraPowerShell != null)
            {
                do // false loop
                {
                    if (_extraPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped ||
                        _extraPowerShell.InvocationStateInfo.State == PSInvocationState.Completed ||
                        _extraPowerShell.InvocationStateInfo.State == PSInvocationState.Failed ||
                        _extraPowerShell.InvocationStateInfo.State == PSInvocationState.Stopping)
                    {
                        break;
                    }
                    else
                    {
                        _extraPowerShell.Stop();
                    }
                } while (false);
            }
        }

        /// <summary>
        /// Add input to the local powershell's input collection.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInputReceived(object sender, RemoteDataEventArgs<object> eventArgs)
        {
            // This can be called in pushed runspace scenarios for error reporting (pipeline stopped).
            // Ignore for noInput.
            if (!_noInput && (InputCollection != null))
            {
                InputCollection.Add(eventArgs.Data);
            }
        }

        /// <summary>
        /// Close the input collection of the local powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInputEndReceived(object sender, EventArgs eventArgs)
        {
            // This can be called in pushed runspace scenarios for error reporting (pipeline stopped).
            // Ignore for noInput.
            if (!_noInput && (InputCollection != null))
            {
                InputCollection.Complete();
            }
        }

        private void HandleSessionConnected(object sender, EventArgs eventArgs)
        {
            // Close input if its active. no need to synchronize as input stream would have already been processed
            // when connect call came into PS plugin

            // TODO: Post an ETW event
            InputCollection?.Complete();
        }

        /// <summary>
        /// Handle a host message response received.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleHostResponseReceived(object sender, RemoteDataEventArgs<RemoteHostResponse> eventArgs)
        {
            _remoteHost.ServerMethodExecutor.HandleRemoteHostResponseFromClient(eventArgs.Data);
        }

        /// <summary>
        /// Handles the PSDataCollection idle event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleIdleEvent(object sender, EventArgs args)
        {
            Runspace rs = DataStructureHandler.RunspaceUsedToInvokePowerShell;
            if (rs != null)
            {
                PSLocalEventManager events = (object)rs.Events as PSLocalEventManager;

                if (events != null)
                {
                    foreach (PSEventSubscriber subscriber in events.Subscribers)
                    {
                        // Use the synchronous version
                        events.DrainPendingActions(subscriber);
                    }
                }
            }
        }

        #endregion Private Methods
    }
}
