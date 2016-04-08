/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/

using System.Security.Principal;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Remoting;
using System.Management.Automation.Internal;
using System.Threading;


namespace System.Management.Automation
{
    /// <summary>
    /// This class wraps a PowerShell object. It is used to function
    /// as a server side powershell
    /// </summary>
    internal class ServerPowerShellDriver
    {
        #region Private Members

        private bool extraPowerShellAlreadyScheduled;     
        private PowerShell extraPowerShell;     // extra PowerShell at the server to be run after localPowerShell
        private PowerShell localPowerShell;     // local PowerShell at the server
        private PSDataCollection<PSObject> localPowerShellOutput; // output buffer for the local PowerShell
        private Guid clientPowerShellId;        // the client PowerShell's guid 
                                                // that is associated with this
                                                // powershell driver
        private Guid clientRunspacePoolId;      // the id of the client runspace pool
                                                // associated with this powershell
        private ServerPowerShellDataStructureHandler dsHandler;
                                                // data structure handler object to handle all
                                                // communications with the client
        private PSDataCollection<object> input; // input for local powershell invocation
        private bool[] datasent = new bool[2];  // if the remaining data has been sent
                                                // to the client before sending state
                                                // information
        private object syncObject = new object(); // sync object for synchronizing sending
                                                  // data to client
        private bool noInput;                   // there is no input when this driver 
                                                // was created
        private bool addToHistory;
        private ServerRemoteHost remoteHost;   // the server remote host instance
                                                // associated with this powershell
#if !CORECLR // No ApartmentState In CoreCLR
        private ApartmentState apartmentState;  // apartment state for this powershell
#endif
        private RemoteStreamOptions remoteStreamOptions;  // serialization options for the streams in this powershell

        private IRSPDriverInvoke psDriverInvoker;  // Handles nested invocation of PS drivers.

        #endregion Private Members

        #region Constructors

#if !CORECLR
        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="extraPowerShell">extra pipeline to be run after <paramref name="powershell"/> completes</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">apartment state for this powershell</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
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
#else
        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="extraPowerShell">extra pipeline to be run after <paramref name="powershell"/> completes</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>        
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
        /// <param name="addToHistory">
        /// true if the command is to be added to history list of the runspace. false, otherwise.
        /// </param>
        /// <param name="rsToUse">
        /// If not null, this Runspace will be used to invoke Powershell.
        /// If null, the RunspacePool pointed by <paramref name="runspacePoolDriver"/> will be used.
        /// </param>
        internal ServerPowerShellDriver(PowerShell powershell, PowerShell extraPowerShell, bool noInput, Guid clientPowerShellId,
           Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
           HostInfo hostInfo, RemoteStreamOptions streamOptions,
           bool addToHistory, Runspace rsToUse)
            : this(powershell, extraPowerShell, noInput, clientPowerShellId, clientRunspacePoolId, runspacePoolDriver,
                   hostInfo, streamOptions, addToHistory, rsToUse, null)
        {
        }
#endif

#if CORECLR
        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="extraPowerShell">extra pipeline to be run after <paramref name="powershell"/> completes</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>        
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
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
            HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse, PSDataCollection<PSObject> output)
#else
        /// <summary>
        /// Default constructor for creating ServerPowerShellDrivers
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="extraPowerShell">extra pipeline to be run after <paramref name="powershell"/> completes</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">apartment state for this powershell</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
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
#endif
        {
            this.clientPowerShellId = clientPowerShellId;
            this.clientRunspacePoolId = clientRunspacePoolId;
            this.remoteStreamOptions = streamOptions;
#if !CORECLR // No ApartmentState In CoreCLR
            this.apartmentState = apartmentState;
#endif
            this.localPowerShell = powershell;
            this.extraPowerShell = extraPowerShell;
            this.localPowerShellOutput = new PSDataCollection<PSObject>();
            this.noInput = noInput;
            this.addToHistory = addToHistory;
            this.psDriverInvoker = runspacePoolDriver;

            this.dsHandler = runspacePoolDriver.DataStructureHandler.CreatePowerShellDataStructureHandler(clientPowerShellId, clientRunspacePoolId, remoteStreamOptions, localPowerShell);
            this.remoteHost = dsHandler.GetHostAssociatedWithPowerShell(hostInfo, runspacePoolDriver.ServerRemoteHost);

            if (!noInput)
            {
                input = new PSDataCollection<object>();
                input.ReleaseOnEnumeration = true;
                input.IdleEvent += new EventHandler<EventArgs>(HandleIdleEvent);
            }

            RegisterPipelineOutputEventHandlers(localPowerShellOutput);

            if (localPowerShell != null)
            {
                RegisterPowerShellEventHandlers(localPowerShell);
                datasent[0] = false;
            }

            if (extraPowerShell != null)
            {
                RegisterPowerShellEventHandlers(extraPowerShell);
                datasent[1] = false;
            }

            RegisterDataStructureHandlerEventHandlers(dsHandler);

            // set the runspace pool and invoke this powershell
            if (null != rsToUse)
            {
                localPowerShell.Runspace = rsToUse;
                if (extraPowerShell != null)
                {
                    extraPowerShell.Runspace = rsToUse;
                }
            }
            else
            {
                localPowerShell.RunspacePool = runspacePoolDriver.RunspacePool;
                if (extraPowerShell != null)
                {
                    extraPowerShell.RunspacePool = runspacePoolDriver.RunspacePool;
                }
            }

            if (output != null)
            {
                output.DataAdded += (sender, args) =>
                    {
                        if (localPowerShellOutput.IsOpen)
                        {
                            var items = output.ReadAll();
                            foreach (var item in items)
                            {
                                localPowerShellOutput.Add(item);
                            }
                        }
                    };
            }
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Input collection sync object
        /// </summary>
        internal PSDataCollection<object> InputCollection
        {
            get { return input; }
        }

        /// <summary>
        /// Local PowerShell instance
        /// </summary>
        internal PowerShell LocalPowerShell
        {
            get { return localPowerShell; }
        }

        /// <summary>
        /// Instance id by which this powershell driver is 
        /// identified. This is the same as the id of the
        /// powershell on the client side
        /// </summary>
        internal Guid InstanceId
        {
            get
            {
                return clientPowerShellId;
            }
        }

        /// <summary>
        /// Serialization options for the streams in this powershell
        /// </summary>
        internal RemoteStreamOptions RemoteStreamOptions
        {
            get
            {
                return this.remoteStreamOptions;
            }
        }

        /// <summary>
        /// Id of the runspace pool driver which created
        /// this object. This is the same as the id of 
        /// the runspace pool at the client side which
        /// is associated with the powershell on the 
        /// client side
        /// </summary>
        internal Guid RunspacePoolId
        {
            get
            {
                return clientRunspacePoolId;
            }

        }

        /// <summary>
        /// ServerPowerShellDataStructureHandler associated with this
        /// powershell driver
        /// </summary>
        internal ServerPowerShellDataStructureHandler DataStructureHandler
        {
            get
            {
                return dsHandler;
            }
        }

        private PSInvocationSettings PrepInvoke(bool startMainPowerShell)
        {
            if (startMainPowerShell)
            {
                // prepare transport manager for sending and receiving data.
                dsHandler.Prepare();
            }

            PSInvocationSettings settings = new PSInvocationSettings();
#if !CORECLR // No ApartmentState In CoreCLR            
            settings.ApartmentState = apartmentState;
#endif
            settings.Host = remoteHost;

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

            settings.AddToHistory = addToHistory;
            return settings;
        }

        private IAsyncResult Start(bool startMainPowerShell)
        {
            PSInvocationSettings settings = PrepInvoke(startMainPowerShell);

            if (startMainPowerShell)
            {
                return localPowerShell.BeginInvoke<object, PSObject>(input, localPowerShellOutput, settings, null, null);
            }
            else
            {
                return extraPowerShell.BeginInvoke<object, PSObject>(input, localPowerShellOutput, settings, null, null);
            }
        }

        /// <summary>
        /// invokes the powershell asynchronously
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
        internal void RunNoOpCommand()
        {
            if (this.localPowerShell != null)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    (state) => 
                        {
                            this.localPowerShell.SetStateChanged(
                                new PSInvocationStateInfo(
                                    PSInvocationState.Running, null));

                            this.localPowerShell.SetStateChanged(
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
                localPowerShell.InvokeWithDebugger(input, localPowerShellOutput, settings, true);
            }
            catch (Exception e)
            {
                CommandProcessor.CheckForSevereException(e);
                ex = e;
            }

            if (ex != null)
            {
                // Since this is being invoked asynchronously on a single pipeline thread
                // any invoke failures (such as possible debugger failures) need to be
                // passed back to client or the original client invoke request will hang.
                string failedCommand = localPowerShell.Commands.Commands[0].CommandText;
                localPowerShell.Commands.Clear();
                string msg = StringUtil.Format(
                    RemotingErrorIdStrings.ServerSideNestedCommandInvokeFailed,
                    failedCommand ?? string.Empty,
                    ex.Message ?? string.Empty);

                localPowerShell.AddCommand("Write-Error").AddArgument(msg);
                localPowerShell.Invoke();
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
        /// and send it to the client
        /// </summary>
        /// <param name="sender">sender of this event</param>
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
                        if (localPowerShell.RunningExtraCommands)
                        {
                            // If completed successfully then allow extra commands to run.
                            if (state == PSInvocationState.Completed) { return; }
                            
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
                            (extraPowerShell != null) && 
                            !extraPowerShellAlreadyScheduled)
                        {
                            extraPowerShellAlreadyScheduled = true;
                            Start(false);
                        }
                        else
                        {
                            dsHandler.RaiseRemoveAssociationEvent();

                            // send the state change notification to the client
                            dsHandler.SendStateChangedInformationToClient(
                                eventArgs.InvocationStateInfo);

                            UnregisterPowerShellEventHandlers(localPowerShell);
                            if (extraPowerShell != null)
                            {
                                UnregisterPowerShellEventHandlers(extraPowerShell);
                            }
                            UnregisterDataStructureHandlerEventHandlers(dsHandler);
                            UnregisterPipelineOutputEventHandlers(localPowerShellOutput);

                            // BUGBUG: currently the local powershell cannot
                            // be disposed as raising the events is
                            // not done towards the end. Need to fix
                            // powershell in order to get this enabled
                            //localPowerShell.Dispose();
                        }
                    }
                    break;

                case PSInvocationState.Stopping:
                    {
                        // abort all pending host calls
                        remoteHost.ServerMethodExecutor.AbortAllCalls();
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles DataAdded event from the Output of the powershell
        /// </summary>
        /// <param name="sender">sender of this information</param>
        /// <param name="e">arguments describing this event</param>
        private void HandleOutputDataAdded(object sender, DataAddedEventArgs e)
        {
            int index = e.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if (!datasent[indexIntoDataSent])
                {
                    PSObject data = localPowerShellOutput[index];
                    // once send the output is removed so that the same
                    // is not sent again by SendRemainingData() method
                    localPowerShellOutput.RemoveAt(index);

                    // send the output data to the client
                    dsHandler.SendOutputDataToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Handles DataAdded event from Error of the PowerShell
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">arguments describing this event</param>
        private void HandleErrorDataAdded(object sender, DataAddedEventArgs e)
        {
            int index = e.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    ErrorRecord errorRecord = localPowerShell.Streams.Error[index];
                    // once send the error record is removed so that the same
                    // is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Error.RemoveAt(index);

                    // send the error record to the client
                    dsHandler.SendErrorRecordToClient(errorRecord);
                }
            } // lock ...
        }

        /// <summary>
        /// Handles DataAdded event from Progress of PowerShell
        /// </summary>
        /// <param name="sender">sender of this information, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleProgressAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    ProgressRecord data = localPowerShell.Streams.Progress[index];
                    // once the debug message is sent, it is removed so that 
                    // the same is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Progress.RemoveAt(index);

                    // send the output data to the client
                    dsHandler.SendProgressRecordToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Handles DataAdded event from Warning of PowerShell
        /// </summary>
        /// <param name="sender">sender of this information, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleWarningAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    WarningRecord data = localPowerShell.Streams.Warning[index];
                    // once the debug message is sent, it is removed so that 
                    // the same is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Warning.RemoveAt(index);
                    
                    // send the output data to the client
                    dsHandler.SendWarningRecordToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Handles DataAdded from Verbose of PowerShell
        /// </summary>
        /// <param name="sender">sender of this information, unused</param>
        /// <param name="eventArgs">sender of this information</param>
        private void HandleVerboseAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    VerboseRecord data = localPowerShell.Streams.Verbose[index];
                    // once the debug message is sent, it is removed so that 
                    // the same is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Verbose.RemoveAt(index);

                    // send the output data to the client
                    dsHandler.SendVerboseRecordToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Handles DataAdded from Debug of PowerShell
        /// </summary>
        /// <param name="sender">sender of this information, unused</param>
        /// <param name="eventArgs">sender of this information</param>
        private void HandleDebugAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    DebugRecord data = localPowerShell.Streams.Debug[index];
                    // once the debug message is sent, it is removed so that 
                    // the same is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Debug.RemoveAt(index);

                    // send the output data to the client
                    dsHandler.SendDebugRecordToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Handles DataAdded from Information of PowerShell
        /// </summary>
        /// <param name="sender">sender of this information, unused</param>
        /// <param name="eventArgs">sender of this information</param>
        private void HandleInformationAdded(object sender, DataAddedEventArgs eventArgs)
        {
            int index = eventArgs.Index;

            lock (syncObject)
            {
                int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
                if ((indexIntoDataSent == 0) && (!datasent[indexIntoDataSent]))
                {
                    InformationRecord data = localPowerShell.Streams.Information[index];
                    // once the Information message is sent, it is removed so that 
                    // the same is not sent again by SendRemainingData() method
                    localPowerShell.Streams.Information.RemoveAt(index);

                    // send the output data to the client
                    dsHandler.SendInformationRecordToClient(data);
                }
            } // lock ..
        }

        /// <summary>
        /// Send the remaining output and error information to
        /// client
        /// </summary>
        /// <remarks>This method should be called before
        /// sending the state information. The client will 
        /// remove the association between a powershell and
        /// runspace pool if it recieves any of the terminal
        /// states. Hence all the remaining data should be
        /// sent before this happens. Else the data will be
        /// discarded</remarks>
        private void SendRemainingData()
        {
            int indexIntoDataSent = (!extraPowerShellAlreadyScheduled) ? 0 : 1;
            lock (syncObject)
            {
                datasent[indexIntoDataSent] = true;
            }

            try
            {
                // BUGBUG: change this code to use enumerator
                // blocked on bug #108824, to be fixed by Kriscv
                for (int i = 0; i < localPowerShellOutput.Count; i++)
                {
                    PSObject data = localPowerShellOutput[i];
                    dsHandler.SendOutputDataToClient(data);
                }
                localPowerShellOutput.Clear();

                //foreach (ErrorRecord errorRecord in localPowerShell.Error)
                for (int i = 0; i < localPowerShell.Streams.Error.Count; i++)
                {
                    ErrorRecord errorRecord = localPowerShell.Streams.Error[i];
                    dsHandler.SendErrorRecordToClient(errorRecord);
                }
                localPowerShell.Streams.Error.Clear();
            }
            finally
            {
                lock (syncObject)
                {
                    // reset to original state so other pipelines can stream.
                    datasent[indexIntoDataSent] = true;
                }
            }
        }

        /// <summary>
        /// Stop the local powershell
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">unused</param>
        private void HandleStopReceived(object sender, EventArgs eventArgs)
        {
            do // false loop
            {
                if (localPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped ||
                    localPowerShell.InvocationStateInfo.State == PSInvocationState.Completed ||
                    localPowerShell.InvocationStateInfo.State == PSInvocationState.Failed ||
                    localPowerShell.InvocationStateInfo.State == PSInvocationState.Stopping)
                {
                    break;
                }
                else
                {
                    // Ensure that the local PowerShell command is not stopped in debug mode.
                    bool handledByDebugger = false;
                    if (!localPowerShell.IsNested &&
                        this.psDriverInvoker != null)
                    {
                        handledByDebugger = this.psDriverInvoker.HandleStopSignal();
                    }
                    
                    if (!handledByDebugger)
                    {
                        localPowerShell.Stop();
                    }
                }
            } while (false);

            if (extraPowerShell != null)
            {
                do // false loop
                {
                    if (extraPowerShell.InvocationStateInfo.State == PSInvocationState.Stopped ||
                        extraPowerShell.InvocationStateInfo.State == PSInvocationState.Completed ||
                        extraPowerShell.InvocationStateInfo.State == PSInvocationState.Failed ||
                        extraPowerShell.InvocationStateInfo.State == PSInvocationState.Stopping)
                    {
                        break;
                    }
                    else
                    {
                        extraPowerShell.Stop();
                    }
                } while (false);
            }
        }

        /// <summary>
        /// Add input to the local powershell's input collection
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleInputReceived(object sender, RemoteDataEventArgs<object> eventArgs)
        {
            // This can be called in pushed runspace scenarios for error reporting (pipeline stopped).  
            // Ignore for noInput.
            if (!noInput && (input != null))
            {
                input.Add(eventArgs.Data);
            }
        }

        /// <summary>
        /// Close the input collection of the local powershell
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleInputEndReceived(object sender, EventArgs eventArgs)
        {
            // This can be called in pushed runspace scenarios for error reporting (pipeline stopped).  
            // Ignore for noInput.
            if (!noInput && (input != null))
            {
                input.Complete();
            }
        }

        private void HandleSessionConnected(object sender, EventArgs eventArgs)
        {
            //Close input if its active. no need to synchronize as input stream would have already been processed
            // when connect call came into PS plugin
            if (input != null)
            {
                //TODO: Post an ETW event
                input.Complete();
            }
        }

        /// <summary>
        /// Handle a host message response received
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleHostResponseReceived(object sender, RemoteDataEventArgs<RemoteHostResponse> eventArgs)
        {
            remoteHost.ServerMethodExecutor.HandleRemoteHostResponseFromClient(eventArgs.Data);
        }

        /// <summary>
        /// Handles the PSDataCollection idle event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleIdleEvent(object sender, EventArgs args)
        {
            Runspace rs = dsHandler.RunspaceUsedToInvokePowerShell;
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
