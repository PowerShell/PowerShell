// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Handles all data structure handler communication with the client
    /// runspace pool.
    /// </summary>
    internal sealed class ServerRunspacePoolDataStructureHandler
    {
        #region Constructors

        /// <summary>
        /// Constructor which takes a server runspace pool driver and
        /// creates an associated ServerRunspacePoolDataStructureHandler.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="transportManager"></param>
        internal ServerRunspacePoolDataStructureHandler(ServerRunspacePoolDriver driver,
            AbstractServerSessionTransportManager transportManager)
        {
            _clientRunspacePoolId = driver.InstanceId;
            _transportManager = transportManager;
        }

        #endregion Constructors

        #region Data Structure Handler Methods

        /// <summary>
        /// Send a message with application private data to the client.
        /// </summary>
        /// <param name="applicationPrivateData">ApplicationPrivateData to send.</param>
        /// <param name="serverCapability">Server capability negotiated during initial exchange of remoting messages / session capabilities of client and server.</param>
        internal void SendApplicationPrivateDataToClient(PSPrimitiveDictionary applicationPrivateData, RemoteSessionCapability serverCapability)
        {
            // make server's PSVersionTable available to the client using ApplicationPrivateData
            PSPrimitiveDictionary applicationPrivateDataWithVersionTable =
                PSPrimitiveDictionary.CloneAndAddPSVersionTable(applicationPrivateData);

            // override the hardcoded version numbers with the stuff that was reported to the client during negotiation
            PSPrimitiveDictionary versionTable = (PSPrimitiveDictionary)applicationPrivateDataWithVersionTable[PSVersionInfo.PSVersionTableName];
            versionTable[PSVersionInfo.PSRemotingProtocolVersionName] = serverCapability.ProtocolVersion;
            versionTable[PSVersionInfo.SerializationVersionName] = serverCapability.SerializationVersion;

            // Pass back the true PowerShell version to the client via application private data.
            versionTable[PSVersionInfo.PSVersionName] = PSVersionInfo.PSVersion;

            RemoteDataObject data = RemotingEncoder.GenerateApplicationPrivateData(
                _clientRunspacePoolId, applicationPrivateDataWithVersionTable);

            SendDataAsync(data);
        }

        /// <summary>
        /// Send a message with the RunspacePoolStateInfo to the client.
        /// </summary>
        /// <param name="stateInfo">State info to send.</param>
        internal void SendStateInfoToClient(RunspacePoolStateInfo stateInfo)
        {
            RemoteDataObject data = RemotingEncoder.GenerateRunspacePoolStateInfo(
                    _clientRunspacePoolId, stateInfo);

            SendDataAsync(data);
        }

        /// <summary>
        /// Send a message with the PSEventArgs to the client.
        /// </summary>
        /// <param name="e">Event to send.</param>
        internal void SendPSEventArgsToClient(PSEventArgs e)
        {
            RemoteDataObject data = RemotingEncoder.GeneratePSEventArgs(_clientRunspacePoolId, e);

            SendDataAsync(data);
        }

        /// <summary>
        /// Called when session is connected from a new client
        /// call into the sessionconnect handlers for each associated powershell dshandler.
        /// </summary>
        internal void ProcessConnect()
        {
            List<ServerPowerShellDataStructureHandler> dsHandlers;
            lock (_associationSyncObject)
            {
                dsHandlers = new List<ServerPowerShellDataStructureHandler>(_associatedShells.Values);
            }

            foreach (var dsHandler in dsHandlers)
            {
                dsHandler.ProcessConnect();
            }
        }

        /// <summary>
        /// Process the data received from the runspace pool on
        /// the server.
        /// </summary>
        /// <param name="receivedData">Data received.</param>
        internal void ProcessReceivedData(RemoteDataObject<PSObject> receivedData)
        {
            if (receivedData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(receivedData));
            }

            Dbg.Assert(receivedData.TargetInterface == RemotingTargetInterface.RunspacePool,
                "RemotingTargetInterface must be Runspace");

            switch (receivedData.DataType)
            {
                case RemotingDataType.CreatePowerShell:
                    {
                        Dbg.Assert(CreateAndInvokePowerShell != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        CreateAndInvokePowerShell.SafeInvoke(this, new RemoteDataEventArgs<RemoteDataObject<PSObject>>(receivedData));
                    }

                    break;

                case RemotingDataType.GetCommandMetadata:
                    {
                        Dbg.Assert(GetCommandMetadata != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        GetCommandMetadata.SafeInvoke(this, new RemoteDataEventArgs<RemoteDataObject<PSObject>>(receivedData));
                    }

                    break;

                case RemotingDataType.RemoteRunspaceHostResponseData:
                    {
                        Dbg.Assert(HostResponseReceived != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        RemoteHostResponse remoteHostResponse = RemoteHostResponse.Decode(receivedData.Data);

                        // part of host message robustness algo. Now the host response is back, report to transport that
                        // execution status is back to running
                        _transportManager.ReportExecutionStatusAsRunning();

                        HostResponseReceived.SafeInvoke(this, new RemoteDataEventArgs<RemoteHostResponse>(remoteHostResponse));
                    }

                    break;

                case RemotingDataType.SetMaxRunspaces:
                    {
                        Dbg.Assert(SetMaxRunspacesReceived != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        SetMaxRunspacesReceived.SafeInvoke(this, new RemoteDataEventArgs<PSObject>(receivedData.Data));
                    }

                    break;

                case RemotingDataType.SetMinRunspaces:
                    {
                        Dbg.Assert(SetMinRunspacesReceived != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        SetMinRunspacesReceived.SafeInvoke(this, new RemoteDataEventArgs<PSObject>(receivedData.Data));
                    }

                    break;

                case RemotingDataType.AvailableRunspaces:
                    {
                        Dbg.Assert(GetAvailableRunspacesReceived != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events");

                        GetAvailableRunspacesReceived.SafeInvoke(this, new RemoteDataEventArgs<PSObject>(receivedData.Data));
                    }

                    break;

                case RemotingDataType.ResetRunspaceState:
                    {
                        Dbg.Assert(ResetRunspaceState != null,
                            "The ServerRunspacePoolDriver should subscribe to all data structure handler events.");

                        ResetRunspaceState.SafeInvoke(this, new RemoteDataEventArgs<PSObject>(receivedData.Data));
                    }

                    break;
            }
        }

        /// <summary>
        /// Creates a powershell data structure handler from this runspace pool.
        /// </summary>
        /// <param name="instanceId">Powershell instance id.</param>
        /// <param name="runspacePoolId">Runspace pool id.</param>
        /// <param name="remoteStreamOptions">Remote stream options.</param>
        /// <param name="localPowerShell">Local PowerShell object.</param>
        /// <returns>ServerPowerShellDataStructureHandler.</returns>
        internal ServerPowerShellDataStructureHandler CreatePowerShellDataStructureHandler(
            Guid instanceId, Guid runspacePoolId, RemoteStreamOptions remoteStreamOptions, PowerShell localPowerShell)
        {
            // start with pool's transport manager.
            AbstractServerTransportManager cmdTransportManager = _transportManager;

            if (instanceId != Guid.Empty)
            {
                cmdTransportManager = _transportManager.GetCommandTransportManager(instanceId);
                Dbg.Assert(cmdTransportManager.TypeTable != null, "This should be already set in managed C++ code");
            }

            ServerPowerShellDataStructureHandler dsHandler =
                new ServerPowerShellDataStructureHandler(instanceId, runspacePoolId, remoteStreamOptions, cmdTransportManager, localPowerShell);

            lock (_associationSyncObject)
            {
                _associatedShells.Add(dsHandler.PowerShellId, dsHandler);
            }

            dsHandler.RemoveAssociation += HandleRemoveAssociation;

            return dsHandler;
        }

        /// <summary>
        /// Returns the currently active PowerShell datastructure handler.
        /// </summary>
        /// <returns>
        /// ServerPowerShellDataStructureHandler if one is present, null otherwise.
        /// </returns>
        internal ServerPowerShellDataStructureHandler GetPowerShellDataStructureHandler()
        {
            lock (_associationSyncObject)
            {
                if (_associatedShells.Count > 0)
                {
                    foreach (object o in _associatedShells.Values)
                    {
                        ServerPowerShellDataStructureHandler result = o as ServerPowerShellDataStructureHandler;
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Dispatch the message to the associated powershell data structure handler.
        /// </summary>
        /// <param name="rcvdData">Message to dispatch.</param>
        internal void DispatchMessageToPowerShell(RemoteDataObject<PSObject> rcvdData)
        {
            ServerPowerShellDataStructureHandler dsHandler =
                GetAssociatedPowerShellDataStructureHandler(rcvdData.PowerShellId);

            // if data structure handler is not found, then association has already been
            // removed, discard message
            dsHandler?.ProcessReceivedData(rcvdData);
        }

        /// <summary>
        /// Send the specified response to the client. The client call will
        /// be blocked on the same.
        /// </summary>
        /// <param name="callId">Call id on the client.</param>
        /// <param name="response">Response to send.</param>
        internal void SendResponseToClient(long callId, object response)
        {
            RemoteDataObject message =
                RemotingEncoder.GenerateRunspacePoolOperationResponse(_clientRunspacePoolId, response, callId);

            SendDataAsync(message);
        }

        /// <summary>
        /// TypeTable used for Serialization/Deserialization.
        /// </summary>
        internal TypeTable TypeTable
        {
            get { return _transportManager.TypeTable; }

            set { _transportManager.TypeTable = value; }
        }

        #endregion Data Structure Handler Methods

        #region Data Structure Handler events

        /// <summary>
        /// This event is raised whenever there is a request from the
        /// client to create a powershell on the server and invoke it.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteDataObject<PSObject>>> CreateAndInvokePowerShell;

        /// <summary>
        /// This event is raised whenever there is a request from the
        /// client to run command discovery pipeline.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteDataObject<PSObject>>> GetCommandMetadata;

        /// <summary>
        /// This event is raised when a host call response is received.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteHostResponse>> HostResponseReceived;

        /// <summary>
        /// This event is raised when there is a request to modify the
        /// maximum runspaces in the runspace pool.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<PSObject>> SetMaxRunspacesReceived;

        /// <summary>
        /// This event is raised when there is a request to modify the
        /// minimum runspaces in the runspace pool.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<PSObject>> SetMinRunspacesReceived;

        /// <summary>
        /// This event is raised when there is a request to get the
        /// available runspaces in the runspace pool.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<PSObject>> GetAvailableRunspacesReceived;

        /// <summary>
        /// This event is raised when the client requests the runspace state
        /// to be reset.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<PSObject>> ResetRunspaceState;

        #endregion Data Structure Handler events

        #region Private Methods

        /// <summary>
        /// Send the data specified as a RemoteDataObject asynchronously
        /// to the runspace pool on the remote session.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <remarks>This overload takes a RemoteDataObject and should
        /// be the one that's used to send data from within this
        /// data structure handler class</remarks>
        private void SendDataAsync(RemoteDataObject data)
        {
            Dbg.Assert(data != null, "Cannot send null object.");
            _transportManager.SendDataToClient(data, true);
        }

        /// <summary>
        /// Get the associated powershell data structure handler for the specified
        /// powershell id.
        /// </summary>
        /// <param name="clientPowerShellId">powershell id for the
        /// powershell data structure handler</param>
        /// <returns>ServerPowerShellDataStructureHandler.</returns>
        internal ServerPowerShellDataStructureHandler GetAssociatedPowerShellDataStructureHandler
            (Guid clientPowerShellId)
        {
            ServerPowerShellDataStructureHandler dsHandler = null;

            lock (_associationSyncObject)
            {
                bool success = _associatedShells.TryGetValue(clientPowerShellId, out dsHandler);

                if (!success)
                {
                    dsHandler = null;
                }
            }

            return dsHandler;
        }

        /// <summary>
        /// Remove the association of the powershell from the runspace pool.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Unused.</param>
        private void HandleRemoveAssociation(object sender, EventArgs e)
        {
            Dbg.Assert(sender is ServerPowerShellDataStructureHandler, @"sender of the event
                must be ServerPowerShellDataStructureHandler");

            ServerPowerShellDataStructureHandler dsHandler = sender as ServerPowerShellDataStructureHandler;

            lock (_associationSyncObject)
            {
                _associatedShells.Remove(dsHandler.PowerShellId);
            }

            // let session transport manager remove its association of command transport manager.
            _transportManager.RemoveCommandTransportManager(dsHandler.PowerShellId);
        }

        #endregion Private Methods

        #region Private Members

        private readonly Guid _clientRunspacePoolId;
        // transport manager using which this
        // runspace pool driver handles all client
        // communication
        private readonly AbstractServerSessionTransportManager _transportManager;

        private readonly Dictionary<Guid, ServerPowerShellDataStructureHandler> _associatedShells
            = new Dictionary<Guid, ServerPowerShellDataStructureHandler>();

        // powershell data structure handlers associated with this
        // runspace pool data structure handler
        private readonly object _associationSyncObject = new object();
        // object to synchronize operations to above

        #endregion Private Members
    }

    /// <summary>
    /// Handles all PowerShell data structure handler communication
    /// with the client side PowerShell.
    /// </summary>
    internal sealed class ServerPowerShellDataStructureHandler
    {
        #region Private Members
        // transport manager using which this
        // powershell driver handles all client
        // communication
        private readonly AbstractServerTransportManager _transportManager;
        private readonly Guid _clientRunspacePoolId;
        private readonly Guid _clientPowerShellId;
        private readonly RemoteStreamOptions _streamSerializationOptions;
        private Runspace _rsUsedToInvokePowerShell;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Default constructor for creating ServerPowerShellDataStructureHandler
        /// instance.
        /// </summary>
        /// <param name="instanceId">Powershell instance id.</param>
        /// <param name="runspacePoolId">Runspace pool id.</param>
        /// <param name="remoteStreamOptions">Remote stream options.</param>
        /// <param name="transportManager">Transport manager.</param>
        /// <param name="localPowerShell">Local powershell object.</param>
        internal ServerPowerShellDataStructureHandler(Guid instanceId, Guid runspacePoolId, RemoteStreamOptions remoteStreamOptions,
            AbstractServerTransportManager transportManager, PowerShell localPowerShell)
        {
            _clientPowerShellId = instanceId;
            _clientRunspacePoolId = runspacePoolId;
            _transportManager = transportManager;
            _streamSerializationOptions = remoteStreamOptions;
            transportManager.Closing += HandleTransportClosing;

            if (localPowerShell != null)
            {
                localPowerShell.RunspaceAssigned += LocalPowerShell_RunspaceAssigned;
            }
        }

        private void LocalPowerShell_RunspaceAssigned(object sender, PSEventArgs<Runspace> e)
        {
            _rsUsedToInvokePowerShell = e.Args;
        }

        #endregion Constructors

        #region Data Structure Handler Methods

        /// <summary>
        /// Prepare transport manager to send data to client.
        /// </summary>
        internal void Prepare()
        {
            // When Guid.Empty is used, PowerShell must be using pool's transport manager
            // to send data to client. so we dont need to prepare command transport manager
            if (_clientPowerShellId != Guid.Empty)
            {
                _transportManager.Prepare();
            }
        }

        /// <summary>
        /// Send the state information to the client.
        /// </summary>
        /// <param name="stateInfo">state information to be
        /// sent to the client</param>
        internal void SendStateChangedInformationToClient(PSInvocationStateInfo
            stateInfo)
        {
            Dbg.Assert((stateInfo.State == PSInvocationState.Completed) ||
                       (stateInfo.State == PSInvocationState.Failed) ||
                       (stateInfo.State == PSInvocationState.Stopped),
                       "SendStateChangedInformationToClient should be called to notify a termination state");
            SendDataAsync(RemotingEncoder.GeneratePowerShellStateInfo(
                stateInfo, _clientPowerShellId, _clientRunspacePoolId));

            // Close the transport manager only if the PowerShell Guid != Guid.Empty.
            // When Guid.Empty is used, PowerShell must be using pool's transport manager
            // to send data to client.
            if (_clientPowerShellId != Guid.Empty)
            {
                // no need to listen for closing events as we are initiating the close
                _transportManager.Closing -= HandleTransportClosing;
                // if terminal state is reached close the transport manager instead of letting
                // the client initiate the close.
                _transportManager.Close(null);
            }
        }

        /// <summary>
        /// Send the output data to the client.
        /// </summary>
        /// <param name="data">Data to send.</param>
        internal void SendOutputDataToClient(PSObject data)
        {
            SendDataAsync(RemotingEncoder.GeneratePowerShellOutput(data,
                _clientPowerShellId, _clientRunspacePoolId));
        }

        /// <summary>
        /// Send the error record to client.
        /// </summary>
        /// <param name="errorRecord">Error record to send.</param>
        internal void SendErrorRecordToClient(ErrorRecord errorRecord)
        {
            errorRecord.SerializeExtendedInfo = (_streamSerializationOptions & RemoteStreamOptions.AddInvocationInfoToErrorRecord) != 0;

            SendDataAsync(RemotingEncoder.GeneratePowerShellError(
                errorRecord, _clientRunspacePoolId, _clientPowerShellId));
        }

        /// <summary>
        /// Send the specified warning record to client.
        /// </summary>
        /// <param name="record">Warning record.</param>
        internal void SendWarningRecordToClient(WarningRecord record)
        {
            record.SerializeExtendedInfo = (_streamSerializationOptions & RemoteStreamOptions.AddInvocationInfoToWarningRecord) != 0;

            SendDataAsync(RemotingEncoder.GeneratePowerShellInformational(
                record, _clientRunspacePoolId, _clientPowerShellId, RemotingDataType.PowerShellWarning));
        }

        /// <summary>
        /// Send the specified debug record to client.
        /// </summary>
        /// <param name="record">Debug record.</param>
        internal void SendDebugRecordToClient(DebugRecord record)
        {
            record.SerializeExtendedInfo = (_streamSerializationOptions & RemoteStreamOptions.AddInvocationInfoToDebugRecord) != 0;

            SendDataAsync(RemotingEncoder.GeneratePowerShellInformational(
                record, _clientRunspacePoolId, _clientPowerShellId, RemotingDataType.PowerShellDebug));
        }

        /// <summary>
        /// Send the specified verbose record to client.
        /// </summary>
        /// <param name="record">Warning record.</param>
        internal void SendVerboseRecordToClient(VerboseRecord record)
        {
            record.SerializeExtendedInfo = (_streamSerializationOptions & RemoteStreamOptions.AddInvocationInfoToVerboseRecord) != 0;

            SendDataAsync(RemotingEncoder.GeneratePowerShellInformational(
                record, _clientRunspacePoolId, _clientPowerShellId, RemotingDataType.PowerShellVerbose));
        }

        /// <summary>
        /// Send the specified progress record to client.
        /// </summary>
        /// <param name="record">Progress record.</param>
        internal void SendProgressRecordToClient(ProgressRecord record)
        {
            SendDataAsync(RemotingEncoder.GeneratePowerShellInformational(
                record, _clientRunspacePoolId, _clientPowerShellId));
        }

        /// <summary>
        /// Send the specified information record to client.
        /// </summary>
        /// <param name="record">Information record.</param>
        internal void SendInformationRecordToClient(InformationRecord record)
        {
            SendDataAsync(RemotingEncoder.GeneratePowerShellInformational(
                record, _clientRunspacePoolId, _clientPowerShellId));
        }

        /// <summary>
        /// Called when session is connected from a new client
        /// calls into observers of this event.
        /// observers include corresponding driver that shutdown
        /// input stream is present.
        /// </summary>
        internal void ProcessConnect()
        {
            OnSessionConnected.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Process the data received from the powershell on
        /// the client.
        /// </summary>
        /// <param name="receivedData">Data received.</param>
        internal void ProcessReceivedData(RemoteDataObject<PSObject> receivedData)
        {
            if (receivedData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(receivedData));
            }

            Dbg.Assert(receivedData.TargetInterface == RemotingTargetInterface.PowerShell,
                "RemotingTargetInterface must be PowerShell");

            switch (receivedData.DataType)
            {
                case RemotingDataType.StopPowerShell:
                    {
                        Dbg.Assert(StopPowerShellReceived != null,
                            "ServerPowerShellDriver should subscribe to all data structure handler events");
                        StopPowerShellReceived.SafeInvoke(this, EventArgs.Empty);
                    }

                    break;

                case RemotingDataType.PowerShellInput:
                    {
                        Dbg.Assert(InputReceived != null,
                            "ServerPowerShellDriver should subscribe to all data structure handler events");
                        InputReceived.SafeInvoke(this, new RemoteDataEventArgs<object>(receivedData.Data));
                    }

                    break;

                case RemotingDataType.PowerShellInputEnd:
                    {
                        Dbg.Assert(InputEndReceived != null,
                            "ServerPowerShellDriver should subscribe to all data structure handler events");
                        InputEndReceived.SafeInvoke(this, EventArgs.Empty);
                    }

                    break;

                case RemotingDataType.RemotePowerShellHostResponseData:
                    {
                        Dbg.Assert(HostResponseReceived != null,
                            "ServerPowerShellDriver should subscribe to all data structure handler events");

                        RemoteHostResponse remoteHostResponse = RemoteHostResponse.Decode(receivedData.Data);

                        // part of host message robustness algo. Now the host response is back, report to transport that
                        // execution status is back to running
                        _transportManager.ReportExecutionStatusAsRunning();

                        HostResponseReceived.SafeInvoke(this, new RemoteDataEventArgs<RemoteHostResponse>(remoteHostResponse));
                    }

                    break;
            }
        }

        /// <summary>
        /// Raise a remove association event. This is raised
        /// when the powershell has gone into a terminal state
        /// and the runspace pool need not maintain any further
        /// associations.
        /// </summary>
        internal void RaiseRemoveAssociationEvent()
        {
            Dbg.Assert(RemoveAssociation != null, @"The ServerRunspacePoolDataStructureHandler should subscribe
                to the RemoveAssociation event of ServerPowerShellDataStructureHandler");
            RemoveAssociation.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Creates a ServerRemoteHost which is associated with this powershell.
        /// </summary>
        /// <param name="powerShellHostInfo">Host information about the host associated
        /// PowerShell object on the client.</param>
        /// <param name="runspaceServerRemoteHost">Host associated with the RunspacePool
        /// on the server.</param>
        /// <returns>A new ServerRemoteHost for the PowerShell.</returns>
        internal ServerRemoteHost GetHostAssociatedWithPowerShell(
            HostInfo powerShellHostInfo,
            ServerRemoteHost runspaceServerRemoteHost)
        {
            HostInfo hostInfo;

            // If host was null use the runspace's host for this powershell; otherwise,
            // use the HostInfo to create a proxy host of the powershell's host.
            if (powerShellHostInfo.UseRunspaceHost)
            {
                hostInfo = runspaceServerRemoteHost.HostInfo;
            }
            else
            {
                hostInfo = powerShellHostInfo;
            }

            // If the host was not null on the client, then the PowerShell object should
            // get a brand spanking new host.
            return new ServerRemoteHost(_clientRunspacePoolId, _clientPowerShellId, hostInfo,
                _transportManager, runspaceServerRemoteHost.Runspace, runspaceServerRemoteHost as ServerDriverRemoteHost);
        }

        #endregion Data Structure Handler Methods

        #region Data Structure Handler events

        /// <summary>
        /// This event is raised when the state of associated
        /// powershell is terminal and the runspace pool has
        /// to detach the association.
        /// </summary>
        internal event EventHandler RemoveAssociation;

        /// <summary>
        /// This event is raised when the a message to stop the
        /// powershell is received from the client.
        /// </summary>
        internal event EventHandler StopPowerShellReceived;

        /// <summary>
        /// This event is raised when an input object is received
        /// from the client.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<object>> InputReceived;

        /// <summary>
        /// This event is raised when end of input is received from
        /// the client.
        /// </summary>
        internal event EventHandler InputEndReceived;

        /// <summary>
        /// Raised when server session is connected from a new client.
        /// </summary>
        internal event EventHandler OnSessionConnected;

        /// <summary>
        /// This event is raised when a host response is received.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteHostResponse>> HostResponseReceived;

        #endregion Data Structure Handler events

        #region Internal Methods

        /// <summary>
        /// Client powershell id.
        /// </summary>
        internal Guid PowerShellId
        {
            get
            {
                return _clientPowerShellId;
            }
        }

        /// <summary>
        /// Runspace used to invoke PowerShell, this is used by the steppable
        /// pipeline driver.
        /// </summary>
        internal Runspace RunspaceUsedToInvokePowerShell
        {
            get { return _rsUsedToInvokePowerShell; }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Send the data specified as a RemoteDataObject asynchronously
        /// to the runspace pool on the remote session.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <remarks>This overload takes a RemoteDataObject and should
        /// be the one that's used to send data from within this
        /// data structure handler class</remarks>
        private void SendDataAsync(RemoteDataObject data)
        {
            Dbg.Assert(data != null, "Cannot send null object.");
            // this is from a command execution..let transport manager collect
            // as much data as possible and send bigger buffer to client.
            _transportManager.SendDataToClient(data, false);
        }

        /// <summary>
        /// Handle transport manager's closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleTransportClosing(object sender, EventArgs args)
        {
            StopPowerShellReceived.SafeInvoke(this, args);
        }

        #endregion Private Methods
    }
}
