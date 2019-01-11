// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Collections.ObjectModel;

namespace System.Management.Automation.Runspaces.Internal
{
    /// <summary>
    /// PowerShell client side proxy base which handles invocation
    /// of powershell on a remote machine.
    /// </summary>
    internal class ClientRemotePowerShell : IDisposable
    {
        #region Tracer

        [TraceSourceAttribute("CRPS", "ClientRemotePowerShell")]
        private static PSTraceSource s_tracer = PSTraceSource.GetTracer("CRPS", "ClientRemotePowerShellBase");

        #endregion Tracer

        #region Constructors

        /// <summary>
        /// Constructor which creates a client remote powershell.
        /// </summary>
        /// <param name="shell">Powershell instance.</param>
        /// <param name="runspacePool">The runspace pool associated with
        /// this shell</param>
        internal ClientRemotePowerShell(PowerShell shell, RemoteRunspacePoolInternal runspacePool)
        {
            this.shell = shell;
            clientRunspacePoolId = runspacePool.InstanceId;
            this.runspacePool = runspacePool;

            // retrieve the computer name from the runspacepool
            // information so that it can be used in adding
            // warning to host messages
            computerName = runspacePool.ConnectionInfo.ComputerName;
        }

        #endregion Constructors

        #region Internal Methods/Properties

        /// <summary>
        /// Instance Id associated with this
        /// client remote powershell.
        /// </summary>
        internal Guid InstanceId
        {
            get
            {
                return PowerShell.InstanceId;
            }
        }

        /// <summary>
        /// PowerShell associated with this ClientRemotePowerShell.
        /// </summary>
        internal PowerShell PowerShell
        {
            get
            {
                return shell;
            }
        }

        /// <summary>
        /// Set the state information of the client powershell.
        /// </summary>
        /// <param name="stateInfo">State information to set.</param>
        internal void SetStateInfo(PSInvocationStateInfo stateInfo)
        {
            shell.SetStateChanged(stateInfo);
        }

        /// <summary>
        /// Whether input is available when this object is created.
        /// </summary>
        internal bool NoInput
        {
            get
            {
                return noInput;
            }
        }

        /// <summary>
        /// Input stream associated with this object.
        /// </summary>
        internal ObjectStreamBase InputStream
        {
            get
            {
                return inputstream;
            }

            set
            {
                inputstream = value;

                if (inputstream != null && (inputstream.IsOpen || inputstream.Count > 0))
                {
                    noInput = false;
                }
                else
                {
                    noInput = true;
                }
            }
        }

        /// <summary>
        /// Output stream associated with this object.
        /// </summary>
        internal ObjectStreamBase OutputStream
        {
            get
            {
                return outputstream;
            }

            set
            {
                outputstream = value;
            }
        }

        /// <summary>
        /// Data structure handler object.
        /// </summary>
        internal ClientPowerShellDataStructureHandler DataStructureHandler
        {
            get
            {
                return dataStructureHandler;
            }
        }

        /// <summary>
        /// Invocation settings associated with this
        /// ClientRemotePowerShell.
        /// </summary>
        internal PSInvocationSettings Settings
        {
            get
            {
                return settings;
            }
        }

        /// <summary>
        /// Close the output, error and other collections
        /// associated with the shell, so that the
        /// enumerator does not block.
        /// </summary>
        internal void UnblockCollections()
        {
            shell.ClearRemotePowerShell();

            outputstream.Close();
            errorstream.Close();
            if (inputstream != null)
            {
                inputstream.Close();
            }
        }

        /// <summary>
        /// Stop the remote powershell asynchronously.
        /// </summary>
        /// <remarks>This method will be called from
        /// within the lock on PowerShell. Hence no need
        /// to lock</remarks>
        internal void StopAsync()
        {
            // If we are in robust connection retry mode then auto-disconnect this command
            // rather than try to stop it.
            PSConnectionRetryStatus retryStatus = _connectionRetryStatus;
            if ((retryStatus == PSConnectionRetryStatus.NetworkFailureDetected ||
                 retryStatus == PSConnectionRetryStatus.ConnectionRetryAttempt) &&
                this.runspacePool.RunspacePoolStateInfo.State == RunspacePoolState.Opened)
            {
                // While in robust connection retry mode, this call forces robust connections
                // to abort retries and go directly to auto-disconnect.
                this.runspacePool.BeginDisconnect(null, null);
                return;
            }

            // powershell CoreStop would have handled cases
            // for NotStarted, Stopping and already Stopped
            // so at this point, there is no need to make any
            // check. The message simply needs to be sent
            // across to the server
            stopCalled = true;
            dataStructureHandler.SendStopPowerShellMessage();
        }

        /// <summary>
        /// </summary>
        internal void SendInput()
        {
            dataStructureHandler.SendInput(this.inputstream);
        }

        /// <summary>
        /// This event is raised, when a host call is for a remote pipeline
        /// which this remote powershell wraps.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteHostCall>> HostCallReceived;

        /// <summary>
        /// Initialize the client remote powershell instance.
        /// </summary>
        /// <param name="inputstream">Input for execution.</param>
        /// <param name="errorstream">error stream to which
        /// data needs to be written to</param>
        /// <param name="informationalBuffers">informational buffers
        /// which will hold debug, verbose and warning messages</param>
        /// <param name="settings">settings based on which this powershell
        /// needs to be executed</param>
        /// <param name="outputstream">output stream to which data
        /// needs to be written to</param>
        internal void Initialize(
            ObjectStreamBase inputstream, ObjectStreamBase outputstream,
                 ObjectStreamBase errorstream, PSInformationalBuffers informationalBuffers,
                        PSInvocationSettings settings)
        {
            initialized = true;
            this.informationalBuffers = informationalBuffers;
            InputStream = inputstream;
            this.errorstream = errorstream;
            this.outputstream = outputstream;
            this.settings = settings;

            if (settings == null || settings.Host == null)
            {
                hostToUse = runspacePool.Host;
            }
            else
            {
                hostToUse = settings.Host;
            }

            dataStructureHandler = runspacePool.DataStructureHandler.CreatePowerShellDataStructureHandler(this);

            // register for events from the data structure handler
            dataStructureHandler.InvocationStateInfoReceived +=
                new EventHandler<RemoteDataEventArgs<PSInvocationStateInfo>>(HandleInvocationStateInfoReceived);
            dataStructureHandler.OutputReceived += new EventHandler<RemoteDataEventArgs<object>>(HandleOutputReceived);
            dataStructureHandler.ErrorReceived += new EventHandler<RemoteDataEventArgs<ErrorRecord>>(HandleErrorReceived);
            dataStructureHandler.InformationalMessageReceived +=
                new EventHandler<RemoteDataEventArgs<InformationalMessage>>(HandleInformationalMessageReceived);
            dataStructureHandler.HostCallReceived +=
                new EventHandler<RemoteDataEventArgs<RemoteHostCall>>(HandleHostCallReceived);
            dataStructureHandler.ClosedNotificationFromRunspacePool +=
                new EventHandler<RemoteDataEventArgs<Exception>>(HandleCloseNotificationFromRunspacePool);
            dataStructureHandler.BrokenNotificationFromRunspacePool +=
                new EventHandler<RemoteDataEventArgs<Exception>>(HandleBrokenNotificationFromRunspacePool);
            dataStructureHandler.ConnectCompleted += new EventHandler<RemoteDataEventArgs<Exception>>(HandleConnectCompleted);
            dataStructureHandler.ReconnectCompleted += new EventHandler<RemoteDataEventArgs<Exception>>(HandleConnectCompleted);
            dataStructureHandler.RobustConnectionNotification +=
                new EventHandler<ConnectionStatusEventArgs>(HandleRobustConnectionNotification);
            dataStructureHandler.CloseCompleted +=
                new EventHandler<EventArgs>(HandleCloseCompleted);
        }

        /// <summary>
        /// Do any clean up operation per initialization here.
        /// </summary>
        internal void Clear()
        {
            initialized = false;
        }

        /// <summary>
        /// If this client remote powershell has been initialized.
        /// </summary>
        internal bool Initialized
        {
            get
            {
                return initialized;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        internal static void ExitHandler(object sender, RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            RemoteHostCall hostcall = eventArgs.Data;

            if (hostcall.IsSetShouldExitOrPopRunspace)
            {
                return;
            }

            // use the method from the RemotePowerShell to indeed execute this call
            ClientRemotePowerShell remotePowerShell = (ClientRemotePowerShell)sender;

            remotePowerShell.ExecuteHostCall(hostcall);
        }

        /// <summary>
        /// Attempts to reconnect or connect to a running command on a remote server,
        /// which will resume events and data collection from the server.
        /// If connectCmdInfo parameter is null then a reconnection is attempted and
        /// it is assumed that the current client state is unchanged since disconnection.
        /// If connectCmdInfo parameter is non-null then a connection is attempted to
        /// the specified remote running command.
        /// This is an asynchronous call and results will be reported in the ReconnectCompleted
        /// or the ConnectCompleted call back as appropriate.
        /// </summary>
        /// <param name="connectCmdInfo">ConnectCommandInfo specifying remote command.</param>
        internal void ConnectAsync(ConnectCommandInfo connectCmdInfo)
        {
            if (connectCmdInfo == null)
            {
                // Attempt to do a reconnect with the current PSRP client state.
                this.dataStructureHandler.ReconnectAsync();
            }
            else
            {
                // First add this command DS handler to the remote runspace pool list.
                Dbg.Assert(this.shell.RunspacePool != null, "Invalid runspace pool for this powershell object.");
                this.shell.RunspacePool.RemoteRunspacePoolInternal.AddRemotePowerShellDSHandler(
                                            this.InstanceId, this.dataStructureHandler);

                // Now do the asynchronous connect.
                this.dataStructureHandler.ConnectAsync();
            }
        }

        /// <summary>
        /// This event is fired when this PowerShell object receives a robust connection
        /// notification from the transport.
        /// </summary>
        internal event EventHandler<PSConnectionRetryStatusEventArgs> RCConnectionNotification;

        /// <summary>
        /// Current remote connection retry status.
        /// </summary>
        internal PSConnectionRetryStatus ConnectionRetryStatus
        {
            get { return _connectionRetryStatus; }
        }

        #endregion Internal Methods/Properties

        #region Private Methods

        /// <summary>
        /// An error record is received from the powershell at the
        /// server side. It is added to the error collection of the
        /// client powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleErrorReceived(object sender, RemoteDataEventArgs<ErrorRecord> eventArgs)
        {
            using (s_tracer.TraceEventHandlers())
            {
                shell.SetHadErrors(true);
                errorstream.Write(eventArgs.Data);
            }
        }

        /// <summary>
        /// An output object is received from the powershell at the
        /// server side. It is added to the output collection of the
        /// client powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleOutputReceived(object sender, RemoteDataEventArgs<object> eventArgs)
        {
            using (s_tracer.TraceEventHandlers())
            {
                object data = eventArgs.Data;

                try
                {
                    outputstream.Write(data);
                }
                catch (PSInvalidCastException e)
                {
                    shell.SetStateChanged(new PSInvocationStateInfo(PSInvocationState.Failed, e));
                }
            }
        }

        /// <summary>
        /// The invocation state of the server powershell has changed.
        /// The state of the client powershell is reflected accordingly.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInvocationStateInfoReceived(object sender,
            RemoteDataEventArgs<PSInvocationStateInfo> eventArgs)
        {
            using (s_tracer.TraceEventHandlers())
            {
                PSInvocationStateInfo stateInfo = eventArgs.Data;

                // we should not receive any transient state from
                // the server
                Dbg.Assert(!(stateInfo.State == PSInvocationState.Running ||
                           stateInfo.State == PSInvocationState.Stopping),
                           "Transient states should not be received from the server");

                if (stateInfo.State == PSInvocationState.Disconnected)
                {
                    SetStateInfo(stateInfo);
                }
                else if (stateInfo.State == PSInvocationState.Stopped ||
                         stateInfo.State == PSInvocationState.Failed ||
                         stateInfo.State == PSInvocationState.Completed)
                {
                    // Special case for failure error due to ErrorCode==-2144108453 (no ShellId found).
                    // In this case terminate session since there is no longer a shell to communicate
                    // with.
                    bool terminateSession = false;
                    if (stateInfo.State == PSInvocationState.Failed)
                    {
                        PSRemotingTransportException remotingTransportException = stateInfo.Reason as PSRemotingTransportException;
                        terminateSession = (remotingTransportException != null) &&
                                           (remotingTransportException.ErrorCode == System.Management.Automation.Remoting.Client.WSManNativeApi.ERROR_WSMAN_TARGETSESSION_DOESNOTEXIST);
                    }

                    // if state is completed or failed or stopped,
                    // then the collections need to be closed as
                    // well, else the enumerator will block
                    UnblockCollections();

                    if (stopCalled || terminateSession)
                    {
                        // Reset stop called flag.
                        stopCalled = false;

                        // if a Stop method has been called, then powershell
                        // would have already raised a Stopping event, after
                        // which only a Stopped should be raised
                        _stateInfoQueue.Enqueue(new PSInvocationStateInfo(PSInvocationState.Stopped,
                            stateInfo.Reason));

                        // If the stop call failed due to network problems then close the runspace
                        // since it is now unusable.
                        CheckAndCloseRunspaceAfterStop(stateInfo.Reason);
                    }
                    else
                    {
                        _stateInfoQueue.Enqueue(stateInfo);
                    }
                    // calling close async only after making sure all the internal members are prepared
                    // to handle close complete.
                    dataStructureHandler.CloseConnectionAsync(null);
                }
            }
        }

        /// <summary>
        /// Helper method to check any error condition after a stop call
        /// and close the remote runspace/pool if the stop call failed due
        /// to network outage problems.
        /// </summary>
        /// <param name="ex">Exception.</param>
        private void CheckAndCloseRunspaceAfterStop(Exception ex)
        {
            PSRemotingTransportException transportException = ex as PSRemotingTransportException;
            if (transportException != null &&
                (transportException.ErrorCode == System.Management.Automation.Remoting.Client.WSManNativeApi.ERROR_WSMAN_SENDDATA_CANNOT_CONNECT ||
                 transportException.ErrorCode == System.Management.Automation.Remoting.Client.WSManNativeApi.ERROR_WSMAN_SENDDATA_CANNOT_COMPLETE ||
                 transportException.ErrorCode == System.Management.Automation.Remoting.Client.WSManNativeApi.ERROR_WSMAN_TARGETSESSION_DOESNOTEXIST))
            {
                object rsObject = shell.GetRunspaceConnection();
                if (rsObject is Runspace)
                {
                    Runspace runspace = (Runspace)rsObject;
                    if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                    {
                        try
                        {
                            runspace.Close();
                        }
                        catch (PSRemotingTransportException)
                        { }
                    }
                }
                else if (rsObject is RunspacePool)
                {
                    RunspacePool runspacePool = (RunspacePool)rsObject;
                    if (runspacePool.RunspacePoolStateInfo.State == RunspacePoolState.Opened)
                    {
                        try
                        {
                            runspacePool.Close();
                        }
                        catch (PSRemotingTransportException)
                        { }
                    }
                }
            }
        }

        /// <summary>
        /// Handler for handling any informational message received
        /// from the server side.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInformationalMessageReceived(object sender,
            RemoteDataEventArgs<InformationalMessage> eventArgs)
        {
            using (s_tracer.TraceEventHandlers())
            {
                InformationalMessage infoMessage = eventArgs.Data;

                switch (infoMessage.DataType)
                {
                    case RemotingDataType.PowerShellDebug:
                        {
                            informationalBuffers.AddDebug((DebugRecord)infoMessage.Message);
                        }

                        break;

                    case RemotingDataType.PowerShellVerbose:
                        {
                            informationalBuffers.AddVerbose((VerboseRecord)infoMessage.Message);
                        }

                        break;

                    case RemotingDataType.PowerShellWarning:
                        {
                            informationalBuffers.AddWarning((WarningRecord)infoMessage.Message);
                        }

                        break;

                    case RemotingDataType.PowerShellProgress:
                        {
                            ProgressRecord progress = (ProgressRecord)LanguagePrimitives.ConvertTo(infoMessage.Message,
                                typeof(ProgressRecord), System.Globalization.CultureInfo.InvariantCulture);
                            informationalBuffers.AddProgress(progress);
                        }

                        break;

                    case RemotingDataType.PowerShellInformationStream:
                        {
                            informationalBuffers.AddInformation((InformationRecord)infoMessage.Message);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleHostCallReceived(object sender, RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            using (s_tracer.TraceEventHandlers())
            {
                Collection<RemoteHostCall> prerequisiteCalls =
                    eventArgs.Data.PerformSecurityChecksOnHostMessage(computerName);

                if (HostCallReceived != null)
                {
                    // raise events for all prerequisite calls
                    if (prerequisiteCalls.Count > 0)
                    {
                        foreach (RemoteHostCall hostcall in prerequisiteCalls)
                        {
                            RemoteDataEventArgs<RemoteHostCall> args =
                                new RemoteDataEventArgs<RemoteHostCall>(hostcall);

                            HostCallReceived.SafeInvoke(this, args);
                        }
                    }

                    HostCallReceived.SafeInvoke(this, eventArgs);
                }
                else
                {
                    // execute any prerequisite calls before
                    // executing this host call
                    if (prerequisiteCalls.Count > 0)
                    {
                        foreach (RemoteHostCall hostcall in prerequisiteCalls)
                        {
                            ExecuteHostCall(hostcall);
                        }
                    }

                    ExecuteHostCall(eventArgs.Data);
                }
            }
        }

        /// <summary>
        /// Handler for ConnectCompleted and ReconnectCompleted events from the
        /// PSRP layer.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleConnectCompleted(object sender, RemoteDataEventArgs<Exception> e)
        {
            // After initial connect/reconnect set state to "Running".  Later events
            // will update state to appropriate command execution state.
            SetStateInfo(new PSInvocationStateInfo(PSInvocationState.Running, null));
        }

        /// <summary>
        /// This is need for the state change events that resulted in closing the underlying
        /// datastructure handler. We cannot send the state back to the upper layers until
        /// close is completed from the datastructure/transport layer. We have to send
        /// the terminal state only when we know that underlying datastructure/transport
        /// is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleCloseCompleted(object sender, EventArgs args)
        {
            // if state is completed or failed or stopped,
            // then the collections need to be closed as
            // well, else the enumerator will block
            UnblockCollections();

            // close the transport manager when CreateCloseAckPacket is received
            // otherwise may have race conditions in Server.OutOfProcessMediator
            dataStructureHandler.RaiseRemoveAssociationEvent();

            if (_stateInfoQueue.Count == 0)
            {
                // If shell state is not finished on client side and queue is empty
                // then set state to stopped unless the current state is Disconnected
                // in which case transition state to failed.
                if (!IsFinished(shell.InvocationStateInfo.State))
                {
                    // If RemoteSessionStateEventArgs are provided then use them to set the
                    // session close reason when setting finished state.
                    RemoteSessionStateEventArgs sessionEventArgs = args as RemoteSessionStateEventArgs;
                    Exception closeReason = (sessionEventArgs != null) ? sessionEventArgs.SessionStateInfo.Reason : null;
                    PSInvocationState finishedState = (shell.InvocationStateInfo.State == PSInvocationState.Disconnected) ?
                        PSInvocationState.Failed : PSInvocationState.Stopped;

                    SetStateInfo(new PSInvocationStateInfo(finishedState, closeReason));
                }
            }
            else
            {
                // Apply queued state changes.
                while (_stateInfoQueue.Count > 0)
                {
                    PSInvocationStateInfo stateInfo = _stateInfoQueue.Dequeue();
                    SetStateInfo(stateInfo);
                }
            }
        }

        private bool IsFinished(PSInvocationState state)
        {
            return (state == PSInvocationState.Completed ||
                    state == PSInvocationState.Failed ||
                    state == PSInvocationState.Stopped);
        }

        /// <summary>
        /// Execute the specified host call.
        /// </summary>
        /// <param name="hostcall">Host call to execute.</param>
        private void ExecuteHostCall(RemoteHostCall hostcall)
        {
            if (hostcall.IsVoidMethod)
            {
                if (hostcall.IsSetShouldExitOrPopRunspace)
                {
                    this.shell.ClearRemotePowerShell();
                }

                hostcall.ExecuteVoidMethod(hostToUse);
            }
            else
            {
                RemoteHostResponse remoteHostResponse = hostcall.ExecuteNonVoidMethod(hostToUse);
                dataStructureHandler.SendHostResponseToServer(remoteHostResponse);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleCloseNotificationFromRunspacePool(object sender,
            RemoteDataEventArgs<Exception> eventArgs)
        {
            // RunspacePool is closed...so going to set the state of PowerShell
            // to stopped here.

            // if state is completed or failed or stopped,
            // then the collections need to be closed as
            // well, else the enumerator will block
            UnblockCollections();

            // Since this is a terminal state..close the transport manager.
            dataStructureHandler.RaiseRemoveAssociationEvent();

            // RunspacePool is closed...so going to set the state of PowerShell
            // to stopped here.
            SetStateInfo(new PSInvocationStateInfo(PSInvocationState.Stopped,
                eventArgs.Data));

            // Not calling dataStructureHandler.CloseConnection() as this must
            // have already been called by RunspacePool.Close()
        }

        /// <summary>
        /// Handles notification from RunspacePool indicating
        /// that the pool is broken. This sets the state of
        /// all the powershell objects associated with the
        /// runspace pool to Failed.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">arguments describing this event
        /// contains information on the reason associated with the
        /// runspace pool entering a Broken state</param>
        private void HandleBrokenNotificationFromRunspacePool(object sender,
            RemoteDataEventArgs<Exception> eventArgs)
        {
            // RunspacePool is closed...so going to set the state of PowerShell
            // to stopped here.

            // if state is completed or failed or stopped,
            // then the collections need to be closed as
            // well, else the enumerator will block
            UnblockCollections();

            // Since this is a terminal state..close the transport manager.
            dataStructureHandler.RaiseRemoveAssociationEvent();
            if (stopCalled)
            {
                // Reset stop called flag.
                stopCalled = false;

                // if a Stop method has been called, then powershell
                // would have already raised a Stopping event, after
                // which only a Stopped should be raised
                SetStateInfo(new PSInvocationStateInfo(PSInvocationState.Stopped,
                    eventArgs.Data));
            }
            else
            {
                SetStateInfo(new PSInvocationStateInfo(PSInvocationState.Failed,
                eventArgs.Data));
            }

            // Not calling dataStructureHandler.CloseConnection() as this must
            // have already been called by RunspacePool.Close()
        }

        /// <summary>
        /// Handles a robust connection layer notification from the transport
        /// manager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleRobustConnectionNotification(
            object sender,
            ConnectionStatusEventArgs e)
        {
            // Create event arguments and warnings/errors for this robust connection notification.
            PSConnectionRetryStatusEventArgs connectionRetryStatusArgs = null;
            WarningRecord warningRecord = null;
            ErrorRecord errorRecord = null;
            int maxRetryConnectionTimeMSecs = this.runspacePool.MaxRetryConnectionTime;
            int maxRetryConnectionTimeMinutes = maxRetryConnectionTimeMSecs / 60000;
            switch (e.Notification)
            {
                case ConnectionStatus.NetworkFailureDetected:
                    warningRecord = new WarningRecord(
                        PSConnectionRetryStatusEventArgs.FQIDNetworkFailureDetected,
                        StringUtil.Format(RemotingErrorIdStrings.RCNetworkFailureDetected,
                        this.computerName, maxRetryConnectionTimeMinutes));

                    connectionRetryStatusArgs =
                        new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.NetworkFailureDetected,
                            this.computerName, maxRetryConnectionTimeMSecs, warningRecord);
                    break;

                case ConnectionStatus.ConnectionRetryAttempt:
                    warningRecord = new WarningRecord(
                        PSConnectionRetryStatusEventArgs.FQIDConnectionRetryAttempt,
                        StringUtil.Format(RemotingErrorIdStrings.RCConnectionRetryAttempt, this.computerName));

                    connectionRetryStatusArgs =
                        new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.ConnectionRetryAttempt,
                            this.computerName, maxRetryConnectionTimeMSecs, warningRecord);
                    break;

                case ConnectionStatus.ConnectionRetrySucceeded:
                    warningRecord = new WarningRecord(
                        PSConnectionRetryStatusEventArgs.FQIDConnectionRetrySucceeded,
                        StringUtil.Format(RemotingErrorIdStrings.RCReconnectSucceeded, this.computerName));

                    connectionRetryStatusArgs =
                        new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.ConnectionRetrySucceeded,
                            this.computerName, maxRetryConnectionTimeMinutes, warningRecord);
                    break;

                case ConnectionStatus.AutoDisconnectStarting:
                    {
                        warningRecord = new WarningRecord(
                            PSConnectionRetryStatusEventArgs.FQIDAutoDisconnectStarting,
                            StringUtil.Format(RemotingErrorIdStrings.RCAutoDisconnectingWarning, this.computerName));

                        connectionRetryStatusArgs =
                            new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.AutoDisconnectStarting,
                                this.computerName, maxRetryConnectionTimeMinutes, warningRecord);
                    }

                    break;

                case ConnectionStatus.AutoDisconnectSucceeded:
                    warningRecord = new WarningRecord(
                        PSConnectionRetryStatusEventArgs.FQIDAutoDisconnectSucceeded,
                        StringUtil.Format(RemotingErrorIdStrings.RCAutoDisconnected, this.computerName));

                    connectionRetryStatusArgs =
                        new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.AutoDisconnectSucceeded,
                            this.computerName, maxRetryConnectionTimeMinutes, warningRecord);
                    break;

                case ConnectionStatus.InternalErrorAbort:
                    {
                        string msg = StringUtil.Format(RemotingErrorIdStrings.RCInternalError, this.computerName);
                        RuntimeException reason = new RuntimeException(msg);
                        errorRecord = new ErrorRecord(reason,
                            PSConnectionRetryStatusEventArgs.FQIDNetworkOrDisconnectFailed,
                            ErrorCategory.InvalidOperation, this);

                        connectionRetryStatusArgs =
                            new PSConnectionRetryStatusEventArgs(PSConnectionRetryStatus.InternalErrorAbort,
                                this.computerName, maxRetryConnectionTimeMinutes, errorRecord);
                    }

                    break;
            }

            if (connectionRetryStatusArgs == null)
            {
                return;
            }

            // Update connection status.
            _connectionRetryStatus = connectionRetryStatusArgs.Notification;

            if (warningRecord != null)
            {
                RemotingWarningRecord remotingWarningRecord = new RemotingWarningRecord(
                    warningRecord,
                    new OriginInfo(this.computerName, this.InstanceId));

                // Add warning record to information channel.
                HandleInformationalMessageReceived(this,
                    new RemoteDataEventArgs<InformationalMessage>(
                        new InformationalMessage(remotingWarningRecord, RemotingDataType.PowerShellWarning)));

                // Write warning to host.
                RemoteHostCall writeWarning = new RemoteHostCall(
                    -100,
                    RemoteHostMethodId.WriteWarningLine,
                    new object[] { warningRecord.Message });

                try
                {
                    HandleHostCallReceived(this,
                        new RemoteDataEventArgs<RemoteHostCall>(writeWarning));
                }
                catch (PSNotImplementedException)
                { }
            }

            if (errorRecord != null)
            {
                RemotingErrorRecord remotingErrorRecord = new RemotingErrorRecord(
                    errorRecord,
                    new OriginInfo(this.computerName, this.InstanceId));

                // Add error record to error channel, will also be written to host.
                HandleErrorReceived(this,
                    new RemoteDataEventArgs<ErrorRecord>(remotingErrorRecord));
            }

            // Raise event.
            RCConnectionNotification.SafeInvoke(this, connectionRetryStatusArgs);
        }

        #endregion Private Methods

        #region Protected Members

        protected ObjectStreamBase inputstream;
        protected ObjectStreamBase errorstream;
        protected PSInformationalBuffers informationalBuffers;
        protected PowerShell shell;
        protected Guid clientRunspacePoolId;
        protected bool noInput;
        protected PSInvocationSettings settings;
        protected ObjectStreamBase outputstream;
        protected string computerName;
        protected ClientPowerShellDataStructureHandler dataStructureHandler;
        protected bool stopCalled = false;
        protected PSHost hostToUse;
        protected RemoteRunspacePoolInternal runspacePool;
        protected const string WRITE_DEBUG_LINE = "WriteDebugLine";
        protected const string WRITE_VERBOSE_LINE = "WriteVerboseLine";
        protected const string WRITE_WARNING_LINE = "WriteWarningLine";
        protected const string WRITE_PROGRESS = "WriteProgress";
        protected bool initialized = false;
        /// <summary>
        /// This queue is for the state change events that resulted in closing the underlying
        /// datastructure handler. We cannot send the state back to the upper layers until
        /// close is completed from the datastructure/transport layer.
        /// </summary>
        private Queue<PSInvocationStateInfo> _stateInfoQueue = new Queue<PSInvocationStateInfo>();

        private PSConnectionRetryStatus _connectionRetryStatus = PSConnectionRetryStatus.None;

        #endregion Protected Members

        #region IDisposable

        /// <summary>
        /// Public interface for dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all managed resources.</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // inputstream.Dispose();
                // outputstream.Dispose();
                // errorstream.Dispose();
            }
        }
        #endregion IDisposable
    }

    #region PSConnectionRetryStatusEventArgs

    /// <summary>
    /// Robust Connection notifications.
    /// </summary>
    internal enum PSConnectionRetryStatus
    {
        None = 0,
        NetworkFailureDetected = 1,
        ConnectionRetryAttempt = 2,
        ConnectionRetrySucceeded = 3,
        AutoDisconnectStarting = 4,
        AutoDisconnectSucceeded = 5,
        InternalErrorAbort = 6
    };

    /// <summary>
    /// PSConnectionRetryStatusEventArgs.
    /// </summary>
    internal sealed class PSConnectionRetryStatusEventArgs : EventArgs
    {
        internal const string FQIDNetworkFailureDetected = "PowerShellNetworkFailureDetected";
        internal const string FQIDConnectionRetryAttempt = "PowerShellConnectionRetryAttempt";
        internal const string FQIDConnectionRetrySucceeded = "PowerShellConnectionRetrySucceeded";
        internal const string FQIDAutoDisconnectStarting = "PowerShellNetworkFailedStartDisconnect";
        internal const string FQIDAutoDisconnectSucceeded = "PowerShellAutoDisconnectSucceeded";
        internal const string FQIDNetworkOrDisconnectFailed = "PowerShellNetworkOrDisconnectFailed";

        internal PSConnectionRetryStatusEventArgs(
            PSConnectionRetryStatus notification,
            string computerName,
            int maxRetryConnectionTime,
            object infoRecord)
        {
            Notification = notification;
            ComputerName = computerName;
            MaxRetryConnectionTime = maxRetryConnectionTime;
            InformationRecord = infoRecord;
        }

        internal PSConnectionRetryStatus Notification { get; }

        internal string ComputerName { get; }

        internal int MaxRetryConnectionTime { get; }

        internal object InformationRecord { get; }
    }

    #endregion
}
