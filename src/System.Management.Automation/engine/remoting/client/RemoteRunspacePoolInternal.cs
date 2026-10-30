// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Tracing;
using System.Threading;
using Microsoft.PowerShell.Telemetry;

using Dbg = System.Management.Automation.Diagnostics;
#if LEGACYTELEMETRY
using Microsoft.PowerShell.Telemetry.Internal;
#endif

namespace System.Management.Automation.Runspaces.Internal
{
    /// <summary>
    /// Class which supports pooling remote powerShell runspaces
    /// on the client.
    /// </summary>
    internal sealed class RemoteRunspacePoolInternal : RunspacePoolInternal
    {
        #region Constructor

        /// <summary>
        /// Constructor which creates a RunspacePool using the
        /// supplied <paramref name="connectionInfo"/>, <paramref name="minRunspaces"/>
        /// and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <param name="host">Host associated with this runspacepool.</param>
        /// <param name="applicationArguments">
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        /// <param name="connectionInfo">The RunspaceConnectionInfo object
        /// which identifies this runspace pools connection to the server
        /// </param>
        /// <param name="name">Session name.</param>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// ConnectionInfo specified is null
        /// </exception>
        internal RemoteRunspacePoolInternal(int minRunspaces,
            int maxRunspaces, TypeTable typeTable, PSHost host, PSPrimitiveDictionary applicationArguments, RunspaceConnectionInfo connectionInfo, string name = null)
            : base(minRunspaces, maxRunspaces)
        {
            if (connectionInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException("WSManConnectionInfo");
            }

            PSEtwLog.LogOperationalVerbose(PSEventId.RunspacePoolConstructor,
                    PSOpcode.Constructor, PSTask.CreateRunspace,
                    PSKeyword.UseAlwaysOperational,
                    instanceId.ToString(),
                    minPoolSz.ToString(CultureInfo.InvariantCulture),
                    maxPoolSz.ToString(CultureInfo.InvariantCulture));

            _connectionInfo = connectionInfo.Clone();

            this.host = host;
            ApplicationArguments = applicationArguments;
            AvailableForConnection = false;
            DispatchTable = new DispatchTable<object>();
            _runningPowerShells = new System.Collections.Concurrent.ConcurrentStack<PowerShell>();

            if (!string.IsNullOrEmpty(name))
            {
                this.Name = name;
            }

            CreateDSHandler(typeTable);
        }

        /// <summary>
        /// Create a runspacepool object in the disconnected state.
        /// </summary>
        /// <param name="instanceId">Identifies remote session to connect.</param>
        /// <param name="name">Friendly name for runspace pool.</param>
        /// <param name="isDisconnected">Indicates whether the runspacepool is disconnected.</param>
        /// <param name="connectCommands">Array of commands associated with this runspace pool.</param>
        /// <param name="connectionInfo">Connection information for remote server.</param>
        /// <param name="host">PSHost object.</param>
        /// <param name="typeTable">TypeTable for object serialization/deserialization.</param>
        internal RemoteRunspacePoolInternal(Guid instanceId, string name, bool isDisconnected,
            ConnectCommandInfo[] connectCommands, RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
            : base(1, 1)
        {
            if (instanceId == Guid.Empty)
            {
                throw PSTraceSource.NewArgumentException(nameof(instanceId));
            }

            if (connectCommands == null)
            {
                throw PSTraceSource.NewArgumentNullException("ConnectCommandInfo[]");
            }

            if (connectionInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException("WSManConnectionInfo");
            }

            if (connectionInfo is WSManConnectionInfo)
            {
                _connectionInfo = connectionInfo.Clone();
            }
            else
            {
                Dbg.Assert(false, "ConnectionInfo must be WSManConnectionInfo");
            }

            // Create the runspace pool object to have the same instanceId as the remote session.
            this.instanceId = instanceId;

            // This indicates that this is a disconnected remote runspace pool and min/max values
            // are currently unknown. These values will be updated once the object is connected.
            this.minPoolSz = -1;
            this.maxPoolSz = -1;

            PSEtwLog.LogOperationalVerbose(PSEventId.RunspacePoolConstructor,
                    PSOpcode.Constructor, PSTask.CreateRunspace,
                    PSKeyword.UseAlwaysOperational,
                    instanceId.ToString(),
                    minPoolSz.ToString(CultureInfo.InvariantCulture),
                    maxPoolSz.ToString(CultureInfo.InvariantCulture));

            ConnectCommands = connectCommands;
            this.Name = name;
            this.host = host;
            DispatchTable = new DispatchTable<object>();
            _runningPowerShells = new System.Collections.Concurrent.ConcurrentStack<PowerShell>();

            // Create this object in the disconnected state.
            SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Disconnected, null));

            CreateDSHandler(typeTable);

            AvailableForConnection = isDisconnected;
        }

        /// <summary>
        /// Helper method to create the dispatchTable and dataStructureHandler objects.
        /// </summary>
        private void CreateDSHandler(TypeTable typeTable)
        {
            DataStructureHandler = new ClientRunspacePoolDataStructureHandler(this, typeTable);

            // register for events from the data structure handler
            DataStructureHandler.RemoteHostCallReceived += HandleRemoteHostCalls;
            DataStructureHandler.StateInfoReceived += HandleStateInfoReceived;
            DataStructureHandler.RSPoolInitInfoReceived += HandleInitInfoReceived;
            DataStructureHandler.ApplicationPrivateDataReceived += HandleApplicationPrivateDataReceived;
            DataStructureHandler.SessionClosing += HandleSessionClosing;
            DataStructureHandler.SessionClosed += HandleSessionClosed;
            DataStructureHandler.SetMaxMinRunspacesResponseReceived += HandleResponseReceived;
            DataStructureHandler.URIRedirectionReported += HandleURIDirectionReported;
            DataStructureHandler.PSEventArgsReceived += HandlePSEventArgsReceived;
            DataStructureHandler.SessionDisconnected += HandleSessionDisconnected;
            DataStructureHandler.SessionReconnected += HandleSessionReconnected;
            DataStructureHandler.SessionRCDisconnecting += HandleSessionRCDisconnecting;
            DataStructureHandler.SessionCreateCompleted += HandleSessionCreateCompleted;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The connection associated with this runspace pool.
        /// </summary>
        public override RunspaceConnectionInfo ConnectionInfo
        {
            get
            {
                return _connectionInfo;
            }
        }

        /// <summary>
        /// The ClientRunspacePoolDataStructureHandler associated with this
        /// runspace pool.
        /// </summary>
        internal ClientRunspacePoolDataStructureHandler DataStructureHandler { get; private set; }

        /// <summary>
        /// List of CommandConnectInfo objects for each remote running command
        /// associated with this remote runspace pool.
        /// </summary>
        internal ConnectCommandInfo[] ConnectCommands { get; set; } = null;

        /// <summary>
        /// Gets and sets the name string for this runspace pool object.
        /// </summary>
        internal string Name
        {
            get { return _friendlyName; }

            set { _friendlyName = value ?? string.Empty; }
        }

        /// <summary>
        /// Indicates whether this runspace pools viable/available for connection.
        /// </summary>
        internal bool AvailableForConnection { get; private set; }

        /// <summary>
        /// Returns robust connection maximum retry time in milliseconds.
        /// </summary>
        internal int MaxRetryConnectionTime
        {
            get
            {
                return (DataStructureHandler != null) ? DataStructureHandler.MaxRetryConnectionTime : 0;
            }
        }

        /// <summary>
        /// Returns runspace pool availability.
        /// </summary>
        public override RunspacePoolAvailability RunspacePoolAvailability
        {
            get
            {
                RunspacePoolAvailability availability;
                if (stateInfo.State == RunspacePoolState.Disconnected)
                {
                    // Set availability for disconnected runspace pool in the
                    // same way it is set for runspaces.
                    availability = (AvailableForConnection) ?
                            RunspacePoolAvailability.None :     // Disconnected runspacepool available for connection.
                            RunspacePoolAvailability.Busy;      // Disconnected runspacepool unavailable for connection.
                }
                else
                {
                    availability = base.RunspacePoolAvailability;
                }

                return availability;
            }
        }

        /// <summary>
        /// Property to indicate that the debugger associated to this
        /// remote runspace is in debug stop mode.
        /// </summary>
        internal bool IsRemoteDebugStop
        {
            get;
            set;
        }

        #endregion

        #region internal Methods

        /// <summary>
        /// Resets the runspace state on a runspace pool with a single
        /// runspace.
        /// This is currently supported *only* for remote runspaces.
        /// </summary>
        /// <returns>True if successful.</returns>
        internal override bool ResetRunspaceState()
        {
            // Version check.  Reset Runspace is supported only on PSRP protocol
            // version 2.3 or greater.
            Version remoteProtocolVersionDeclaredByServer = PSRemotingProtocolVersion;
            if ((remoteProtocolVersionDeclaredByServer == null) ||
                (remoteProtocolVersionDeclaredByServer < RemotingConstants.ProtocolVersion_2_3))
            {
                throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.ResetRunspaceStateNotSupportedOnServer);
            }

            long callId = 0;

            lock (syncObject)
            {
                callId = DispatchTable.CreateNewCallId();
                DataStructureHandler.SendResetRunspaceStateToServer(callId);
            }

            // This call blocks until the response is received.
            object response = DispatchTable.GetResponse(callId, false);
            return (bool)response;
        }

        /// <summary>
        /// Sets the maximum number of Runspaces that can be active concurrently
        /// in the pool. All requests above that number remain queued until
        /// runspaces become available.
        /// </summary>
        /// <param name="maxRunspaces">
        /// The maximum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of runspaces to a number smaller than
        /// the minimum runspaces.
        /// </remarks>
        internal override bool SetMaxRunspaces(int maxRunspaces)
        {
            bool isSizeIncreased = false;
            long callId = 0;

            lock (syncObject)
            {
                if (maxRunspaces < minPoolSz || maxRunspaces == maxPoolSz || stateInfo.State == RunspacePoolState.Closed
                    || stateInfo.State == RunspacePoolState.Closing || stateInfo.State == RunspacePoolState.Broken)
                {
                    return false;
                }

                // if the runspace pool is not opened as yet, or is in Disconnected state.
                // just change the value locally. No need to
                // send a message
                if (stateInfo.State == RunspacePoolState.BeforeOpen ||
                    stateInfo.State == RunspacePoolState.Disconnected)
                {
                    maxPoolSz = maxRunspaces;
                    return true;
                }

                // sending the message should be done within the lock
                // to ensure that multiple calls to SetMaxRunspaces
                // will be executed on the server in the order in which
                // they were called in the client
                callId = DispatchTable.CreateNewCallId();

                DataStructureHandler.SendSetMaxRunspacesToServer(maxRunspaces, callId);
            }

            // this call blocks until the response is received
            object response = DispatchTable.GetResponse(callId, false);

            isSizeIncreased = (bool)response;

            if (isSizeIncreased)
            {
                lock (syncObject)
                {
                    maxPoolSz = maxRunspaces;
                }
            }

            return isSizeIncreased;
        }

        /// <summary>
        /// Sets the minimum number of Runspaces that the pool maintains
        /// in anticipation of new requests.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of idle runspaces to a number smaller than
        /// 1 or greater than maximum number of active runspaces.
        /// </remarks>
        internal override bool SetMinRunspaces(int minRunspaces)
        {
            bool isSizeDecreased = false;
            long callId = 0;

            lock (syncObject)
            {
                if ((minRunspaces < 1) || (minRunspaces > maxPoolSz) || (minRunspaces == minPoolSz)
                    || stateInfo.State == RunspacePoolState.Closed || stateInfo.State == RunspacePoolState.Closing ||
                    stateInfo.State == RunspacePoolState.Broken)
                {
                    return false;
                }

                // if the runspace pool is not opened as yet, or is in Disconnected state.
                // just change the value locally. No need to
                // send a message
                if (stateInfo.State == RunspacePoolState.BeforeOpen ||
                    stateInfo.State == RunspacePoolState.Disconnected)
                {
                    minPoolSz = minRunspaces;
                    return true;
                }

                // sending the message should be done within the lock
                // to ensure that multiple calls to SetMinRunspaces
                // will be executed on the server in the order in which
                // they were called in the client
                callId = DispatchTable.CreateNewCallId();

                DataStructureHandler.SendSetMinRunspacesToServer(minRunspaces, callId);
            }

            // this call blocks until the response is received
            object response = DispatchTable.GetResponse(callId, false);

            isSizeDecreased = (bool)response;

            if (isSizeDecreased)
            {
                lock (syncObject)
                {
                    minPoolSz = minRunspaces;
                }
            }

            return isSizeDecreased;
        }

        /// <summary>
        /// Retrieves the number of runspaces available at the time of calling
        /// this method from the remote server.
        /// </summary>
        /// <returns>The number of runspaces available in the pool.</returns>
        internal override int GetAvailableRunspaces()
        {
            int availableRunspaces = 0;
            long callId = 0;

            lock (syncObject)
            {
                // if the runspace pool is opened we need to
                // get the value from the server, else
                // return maxrunspaces
                if (stateInfo.State == RunspacePoolState.Opened)
                {
                    // sending the message should be done within the lock
                    // to ensure that multiple calls to GetAvailableRunspaces
                    // will be executed on the server in the order in which
                    // they were called in the client
                    callId = DispatchTable.CreateNewCallId();
                }
                else if (stateInfo.State != RunspacePoolState.BeforeOpen && stateInfo.State != RunspacePoolState.Opening)
                {
                    throw new InvalidOperationException(HostInterfaceExceptionsStrings.RunspacePoolNotOpened);
                }
                else
                {
                    return maxPoolSz;
                }

                DataStructureHandler.SendGetAvailableRunspacesToServer(callId);
            }

            // this call blocks until the response is received
            object response = DispatchTable.GetResponse(callId, 0);
            availableRunspaces = (int)response;

            return availableRunspaces;
        }

        /// <summary>
        /// The server sent application private data.  Store the data so that user
        /// can get it later.
        /// </summary>
        /// <param name="eventArgs">Argument describing this event.</param>
        /// <param name="sender">Sender of this event.</param>
        internal void HandleApplicationPrivateDataReceived(object sender,
            RemoteDataEventArgs<PSPrimitiveDictionary> eventArgs)
        {
            this.SetApplicationPrivateData(eventArgs.Data);
        }

        internal void HandleInitInfoReceived(object sender,
                        RemoteDataEventArgs<RunspacePoolInitInfo> eventArgs)
        {
            RunspacePoolStateInfo info = new RunspacePoolStateInfo(RunspacePoolState.Opened, null);

            bool raiseEvents = false;

            lock (syncObject)
            {
                minPoolSz = eventArgs.Data.MinRunspaces;
                maxPoolSz = eventArgs.Data.MaxRunspaces;
                if (stateInfo.State == RunspacePoolState.Connecting)
                {
                    ResetDisconnectedOnExpiresOn();

                    raiseEvents = true;
                    SetRunspacePoolState(info);
                }
            }

            if (raiseEvents)
            {
                // Private application data is sent after (post) connect.  We need
                // to wait for application data before raising the state change
                // Connecting -> Opened event.
                ThreadPool.QueueUserWorkItem(WaitAndRaiseConnectEventsProc, info);
            }
        }

        /// <summary>
        /// The state of the server RunspacePool has changed. Handle
        /// the same and reflect local states accordingly.
        /// </summary>
        /// <param name="eventArgs">Argument describing this event.</param>
        /// <param name="sender">Sender of this event.</param>
        internal void HandleStateInfoReceived(object sender,
            RemoteDataEventArgs<RunspacePoolStateInfo> eventArgs)
        {
            RunspacePoolStateInfo newStateInfo = eventArgs.Data;
            bool raiseEvents = false;

            Dbg.Assert(newStateInfo != null, "state information should not be null");

            if (newStateInfo.State == RunspacePoolState.Opened)
            {
                lock (syncObject)
                {
                    if (stateInfo.State == RunspacePoolState.Opening)
                    {
                        SetRunspacePoolState(newStateInfo);
                        raiseEvents = true;
                    }
                }

                if (raiseEvents)
                {
                    // this needs to be done outside the lock to avoid a
                    // deadlock scenario
                    RaiseStateChangeEvent(stateInfo);
                    SetOpenAsCompleted();
                }
            }
            else if (newStateInfo.State == RunspacePoolState.Closed || newStateInfo.State == RunspacePoolState.Broken)
            {
                bool doClose = false;

                lock (syncObject)
                {
                    if (stateInfo.State == RunspacePoolState.Closed || stateInfo.State == RunspacePoolState.Broken)
                    {
                        // there is nothing to do here
                        return;
                    }

                    if (stateInfo.State == RunspacePoolState.Opening
                     || stateInfo.State == RunspacePoolState.Opened
                     || stateInfo.State == RunspacePoolState.Closing)
                    {
                        doClose = true;
                        SetRunspacePoolState(newStateInfo);
                    }
                }

                if (doClose)
                {
                    // if closeAsyncResult is null, BeginClose is not called. That means
                    // we are getting close event from server, in this case release the
                    // local resources
                    if (_closeAsyncResult == null)
                    {
                        // Close the local resources.
                        DataStructureHandler.CloseRunspacePoolAsync();
                    }

                    // Delay notifying upper layers of finished state change event
                    // until after transport close ack is received (HandleSessionClosed handler).
                }
            }
        }

        /// <summary>
        /// A host call has been proxied from the server which needs to
        /// be executed.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        internal void HandleRemoteHostCalls(object sender,
            RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            if (HostCallReceived != null)
            {
                HostCallReceived.SafeInvoke(sender, eventArgs);
            }
            else
            {
                RemoteHostCall hostCall = eventArgs.Data;

                if (hostCall.IsVoidMethod)
                {
                    hostCall.ExecuteVoidMethod(host);
                }
                else
                {
                    RemoteHostResponse remoteHostResponse = hostCall.ExecuteNonVoidMethod(host);
                    DataStructureHandler.SendHostResponseToServer(remoteHostResponse);
                }
            }
        }

        internal PSHost Host
        {
            get
            {
                return host;
            }
        }

        /// <summary>
        /// Application arguments to use when opening a remote session.
        /// </summary>
        internal PSPrimitiveDictionary ApplicationArguments { get; }

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        ///
        /// Remote runspace pool gets its application private data from the server (when creating the remote runspace pool)
        /// - calling this method on a remote runspace will block until the data is received from the server.
        /// - unless the runspace is disconnected and data hasn't been received in which case it returns null immediately.
        /// </summary>
        internal override PSPrimitiveDictionary GetApplicationPrivateData()
        {
            if (this.RunspacePoolStateInfo.State == RunspacePoolState.Disconnected &&
                !_applicationPrivateDataReceived.WaitOne(0))
            {
                // Runspace pool was disconnected before application data was returned.  Application
                // data cannot be returned with the runspace pool disconnected so return null.
                return null;
            }

            return _applicationPrivateData;
        }

        internal void SetApplicationPrivateData(PSPrimitiveDictionary applicationPrivateData)
        {
            lock (this.syncObject)
            {
                if (_applicationPrivateDataReceived.WaitOne(0))
                {
                    return; // ignore server's attempt to set application private data if it has already been set
                }

                _applicationPrivateData = applicationPrivateData;
                _applicationPrivateDataReceived.Set();

                foreach (Runspace runspace in this.runspaceList)
                {
                    runspace.SetApplicationPrivateData(applicationPrivateData);
                }
            }
        }

        internal override void PropagateApplicationPrivateData(Runspace runspace)
        {
            if (_applicationPrivateDataReceived.WaitOne(0))
            {
                runspace.SetApplicationPrivateData(this.GetApplicationPrivateData());
            }
        }

        private PSPrimitiveDictionary _applicationPrivateData;
        private readonly ManualResetEvent _applicationPrivateDataReceived = new ManualResetEvent(false);

        /// <summary>
        /// This event is raised, when a host call is for a remote runspace
        /// which this runspace pool wraps.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<RemoteHostCall>> HostCallReceived;

        /// <summary>
        /// EventHandler used to report connection URI redirections to the application.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<Uri>> URIRedirectionReported;

        /// <summary>
        /// Notifies the successful creation of the runspace session.
        /// </summary>
        internal event EventHandler<CreateCompleteEventArgs> SessionCreateCompleted;

        /// <summary>
        /// </summary>
        /// <param name="shell"></param>
        internal void CreatePowerShellOnServerAndInvoke(ClientRemotePowerShell shell)
        {
            DataStructureHandler.CreatePowerShellOnServerAndInvoke(shell);

            // send any input that may be available
            if (!shell.NoInput)
            {
                shell.SendInput();
            }
        }

        /// <summary>
        /// Add a ClientPowerShellDataStructureHandler to ClientRunspaceDataStructureHandler list.
        /// </summary>
        /// <param name="psShellInstanceId">PowerShell Instance Id.</param>
        /// <param name="psDSHandler">ClientPowerShellDataStructureHandler for PowerShell.</param>
        internal void AddRemotePowerShellDSHandler(Guid psShellInstanceId, ClientPowerShellDataStructureHandler psDSHandler)
        {
            DataStructureHandler.AddRemotePowerShellDSHandler(psShellInstanceId, psDSHandler);
        }

        /// <summary>
        /// Returns true if Runspace supports disconnect.
        /// </summary>
        internal bool CanDisconnect
        {
            get
            {
                Version remoteProtocolVersionDeclaredByServer = PSRemotingProtocolVersion;
                if (remoteProtocolVersionDeclaredByServer != null && DataStructureHandler != null)
                {
                    // Disconnect/Connect support is currently only provided by the WSMan transport
                    // that is running PSRP protocol version 2.2 and greater.
                    return (remoteProtocolVersionDeclaredByServer >= RemotingConstants.ProtocolVersion_2_2 &&
                            DataStructureHandler.EndpointSupportsDisconnect);
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the WinRM protocol version object for this runspace
        /// pool connection.
        /// </summary>
        internal Version PSRemotingProtocolVersion
        {
            get
            {
                Version winRMProtocolVersion = null;

                PSPrimitiveDictionary psPrimitiveDictionary = GetApplicationPrivateData();
                if (psPrimitiveDictionary != null)
                {
                    PSPrimitiveDictionary.TryPathGet(
                        psPrimitiveDictionary,
                        out winRMProtocolVersion,
                        PSVersionInfo.PSVersionTableName,
                        PSVersionInfo.PSRemotingProtocolVersionName);
                }

                return winRMProtocolVersion;
            }
        }

        /// <summary>
        /// Push a running PowerShell onto the stack.
        /// </summary>
        /// <param name="ps">PowerShell.</param>
        internal void PushRunningPowerShell(PowerShell ps)
        {
            Dbg.Assert(ps != null, "Caller should not pass in null reference.");
            _runningPowerShells.Push(ps);
        }

        /// <summary>
        /// Pop the currently running PowerShell from stack.
        /// </summary>
        /// <returns>PowerShell.</returns>
        internal PowerShell PopRunningPowerShell()
        {
            PowerShell powershell;
            if (_runningPowerShells.TryPop(out powershell))
            {
                return powershell;
            }

            return null;
        }

        /// <summary>
        /// Return the current running PowerShell.
        /// </summary>
        /// <returns>PowerShell.</returns>
        internal PowerShell GetCurrentRunningPowerShell()
        {
            PowerShell powershell;
            if (_runningPowerShells.TryPeek(out powershell))
            {
                return powershell;
            }

            return null;
        }

        #endregion Internal Methods

        #region Protected Methods

        /// <summary>
        /// Opens the runspacepool synchronously / asynchronously.
        /// Runspace pool must be opened before it can be used.
        /// </summary>
        /// <param name="isAsync">
        /// true to open asynchronously
        /// </param>
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginOpen completes.
        /// </param>
        /// <param name="asyncState">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// asyncResult object to monitor status of the async
        /// open operation. This is returned only if <paramref name="isAsync"/>
        /// is true.
        /// </returns>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot open RunspacePool because RunspacePool is not in
        /// the BeforeOpen state.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// There is not enough memory available to start this asynchronously.
        /// </exception>
        protected override IAsyncResult CoreOpen(bool isAsync, AsyncCallback callback,
            object asyncState)
        {
            PSEtwLog.SetActivityIdForCurrentThread(this.InstanceId);
            PSEtwLog.LogOperationalVerbose(PSEventId.RunspacePoolOpen, PSOpcode.Open,
                            PSTask.CreateRunspace, PSKeyword.UseAlwaysOperational);

            // Telemetry here - remote session
            ApplicationInsightsTelemetry.SendTelemetryMetric(TelemetryType.RemoteSessionOpen, isAsync.ToString());
#if LEGACYTELEMETRY
            TelemetryAPI.ReportRemoteSessionCreated(_connectionInfo);
#endif

            lock (syncObject)
            {
                AssertIfStateIsBeforeOpen();

                stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Opening, null);
            }

            // BUGBUG: the following comment needs to be validated
            // only one thread will reach here, so no need
            // to lock
            RaiseStateChangeEvent(stateInfo);

            RunspacePoolAsyncResult asyncResult = new RunspacePoolAsyncResult(
                    instanceId, callback, asyncState, true);

            _openAsyncResult = asyncResult;

            // send a message using the data structure handler to open the RunspacePool
            // on the remote server
            DataStructureHandler.CreateRunspacePoolAndOpenAsync();

            return asyncResult;
        }

        #endregion Protected Methods

        #region Public Methods

        /// <summary>
        /// Synchronous open.
        /// </summary>
        public override void Open()
        {
            IAsyncResult asyncResult = BeginOpen(null, null);

            EndOpen(asyncResult);
        }

        /// <summary>
        /// Closes the RunspacePool and cleans all the internal
        /// resources. This will close all the runspaces in the
        /// runspacepool and release all the async operations
        /// waiting for a runspace. If the pool is already closed
        /// or broken or closing this will just return.
        /// </summary>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot close the RunspacePool because RunspacePool is
        /// in Closing state.
        /// </exception>
        public override void Close()
        {
            // close and wait
            IAsyncResult asyncResult = BeginClose(null, null);
            EndClose(asyncResult);
        }

        /// <summary>
        /// Closes the RunspacePool asynchronously. To get the exceptions
        /// that might have occurred, call EndOpen.
        /// </summary>
        /// <param name="callback">
        /// An AsyncCallback to call once the BeginClose completes
        /// </param>
        /// <param name="asyncState">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with
        /// </param>
        /// <returns>
        /// An AsyncResult object to monitor the state of the async
        /// operation
        /// </returns>
        public override IAsyncResult BeginClose(AsyncCallback callback, object asyncState)
        {
            bool raiseEvents = false;
            bool skipClosing = false;
            RunspacePoolStateInfo copyState = new RunspacePoolStateInfo(RunspacePoolState.BeforeOpen, null);
            RunspacePoolAsyncResult asyncResult = null;

            lock (syncObject)
            {
                if ((stateInfo.State == RunspacePoolState.Closed) ||
                    (stateInfo.State == RunspacePoolState.Broken))
                {
                    skipClosing = true;
                    asyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                }
                else if (stateInfo.State == RunspacePoolState.BeforeOpen)
                {
                    copyState = stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Closed, null);
                    raiseEvents = true;
                    skipClosing = true;
                    _closeAsyncResult = null;
                    asyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                }
                else if (stateInfo.State == RunspacePoolState.Opened ||
                         stateInfo.State == RunspacePoolState.Opening)
                {
                    copyState = stateInfo = new RunspacePoolStateInfo(RunspacePoolState.Closing, null);
                    _closeAsyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                    asyncResult = _closeAsyncResult;
                    raiseEvents = true;
                }
                else if (stateInfo.State == RunspacePoolState.Disconnected ||
                         stateInfo.State == RunspacePoolState.Disconnecting ||
                         stateInfo.State == RunspacePoolState.Connecting)
                {
                    // Continue with closing so the PSRP layer is aware that the client side session is
                    // being closed.  This will result in a broken session on the client.
                    _closeAsyncResult = new RunspacePoolAsyncResult(instanceId, callback, asyncState, false);
                    asyncResult = _closeAsyncResult;
                }
                else if (stateInfo.State == RunspacePoolState.Closing)
                {
                    return _closeAsyncResult;
                }
            }

            // raise the events outside the lock
            if (raiseEvents)
            {
                RaiseStateChangeEvent(copyState);
            }

            if (!skipClosing)
            {
                // SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Closing, null), true);

                // send a message using the data structure handler to close the RunspacePool
                // on the remote server
                DataStructureHandler.CloseRunspacePoolAsync();
            }
            else
            {
                // signal the wait handle
                asyncResult.SetAsCompleted(null);
            }

            return asyncResult;
        }

        /// <summary>
        /// Synchronous disconnect.
        /// </summary>
        public override void Disconnect()
        {
            IAsyncResult asyncResult = BeginDisconnect(null, null);

            EndDisconnect(asyncResult);
        }

        /// <summary>
        /// Asynchronous disconnect.
        /// </summary>
        /// <param name="callback">AsyncCallback object.</param>
        /// <param name="state">State object.</param>
        /// <returns>IAsyncResult.</returns>
        public override IAsyncResult BeginDisconnect(AsyncCallback callback, object state)
        {
            if (!CanDisconnect)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.DisconnectNotSupportedOnServer);
            }

            RunspacePoolState currentState;
            bool raiseEvents = false;
            lock (syncObject)
            {
                currentState = stateInfo.State;
                if (currentState == RunspacePoolState.Opened)
                {
                    RunspacePoolStateInfo newStateInfo = new RunspacePoolStateInfo(RunspacePoolState.Disconnecting, null);

                    SetRunspacePoolState(newStateInfo);
                    raiseEvents = true;
                }
            }

            // Raise events outside of lock.
            if (raiseEvents)
            {
                RaiseStateChangeEvent(this.stateInfo);
            }

            if (currentState == RunspacePoolState.Opened)
            {
                RunspacePoolAsyncResult asyncResult = new RunspacePoolAsyncResult(
                    instanceId, callback, state, false);

                _disconnectAsyncResult = asyncResult;
                DataStructureHandler.DisconnectPoolAsync();

                // Return local reference to async object since the class member can
                // be asynchronously nulled if the session closes suddenly.
                return asyncResult;
            }
            else
            {
                string message = StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolState, RunspacePoolState.Opened, stateInfo.State);
                InvalidRunspacePoolStateException invalidStateException = new InvalidRunspacePoolStateException(message,
                        stateInfo.State, RunspacePoolState.Opened);

                throw invalidStateException;
            }
        }

        /// <summary>
        /// Waits for BeginDisconnect operation to complete.
        /// </summary>
        /// <param name="asyncResult">IAsyncResult object.</param>
        public override void EndDisconnect(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            RunspacePoolAsyncResult rsAsyncResult = asyncResult as RunspacePoolAsyncResult;

            if ((rsAsyncResult == null) ||
                (rsAsyncResult.OwnerId != instanceId) ||
                (rsAsyncResult.IsAssociatedWithAsyncOpen))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginOpen");
            }

            rsAsyncResult.EndInvoke();
        }

        /// <summary>
        /// Synchronous connect.
        /// </summary>
        public override void Connect()
        {
            IAsyncResult asyncResult = BeginConnect(null, null);

            EndConnect(asyncResult);
        }

        /// <summary>
        /// Asynchronous connect.
        /// </summary>
        /// <param name="callback">ASyncCallback object.</param>
        /// <param name="state">State Object.</param>
        /// <returns>IAsyncResult.</returns>
        public override IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            if (!AvailableForConnection)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspacePoolStrings.CannotConnect);
            }

            RunspacePoolState currentState;
            bool raiseEvents = false;
            lock (syncObject)
            {
                currentState = stateInfo.State;
                if (currentState == RunspacePoolState.Disconnected)
                {
                    RunspacePoolStateInfo newStateInfo = new RunspacePoolStateInfo(RunspacePoolState.Connecting, null);

                    SetRunspacePoolState(newStateInfo);
                    raiseEvents = true;
                }
            }

            // Raise events outside of lock.
            if (raiseEvents)
            {
                RaiseStateChangeEvent(this.stateInfo);
            }

            raiseEvents = false;

            if (currentState == RunspacePoolState.Disconnected)
            {
                // Assign to local variable to ensure we always pass a non-null value.
                // The async class members can be nulled out if the session closes suddenly.
                RunspacePoolAsyncResult ret = new RunspacePoolAsyncResult(
                    instanceId, callback, state, false);

                if (_canReconnect)
                {
                    // This indicates a reconnect scenario where this object instance was previously
                    // disconnected.
                    _reconnectAsyncResult = ret;
                    DataStructureHandler.ReconnectPoolAsync();
                }
                else
                {
                    // This indicates a reconstruction scenario where this object was created
                    // in the disconnect state and is being connected for the first time.
                    _openAsyncResult = ret;
                    DataStructureHandler.ConnectPoolAsync();
                }

                if (raiseEvents)
                {
                    RaiseStateChangeEvent(this.stateInfo);
                }

                return ret;
            }
            else
            {
                string message = StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolState, RunspacePoolState.Disconnected, stateInfo.State);
                InvalidRunspacePoolStateException invalidStateException = new InvalidRunspacePoolStateException(message,
                        stateInfo.State, RunspacePoolState.Disconnected);

                throw invalidStateException;
            }
        }

        /// <summary>
        /// Waits for BeginConnect to complete.
        /// </summary>
        /// <param name="asyncResult">IAsyncResult object.</param>
        public override void EndConnect(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(asyncResult));
            }

            RunspacePoolAsyncResult rsAsyncResult = asyncResult as RunspacePoolAsyncResult;

            if ((rsAsyncResult == null) ||
                (rsAsyncResult.OwnerId != instanceId) ||
                (rsAsyncResult.IsAssociatedWithAsyncOpen))
            {
                throw PSTraceSource.NewArgumentException(nameof(asyncResult),
                                                         RunspacePoolStrings.AsyncResultNotOwned,
                                                         "IAsyncResult",
                                                         "BeginOpen");
            }

            rsAsyncResult.EndInvoke();
        }

        /// <summary>
        /// Creates an array of PowerShell objects that are in the Disconnected state for
        /// all currently disconnected running commands associated with this runspace pool.
        /// </summary>
        /// <returns>Array of PowerShell objects.</returns>
        public override Collection<PowerShell> CreateDisconnectedPowerShells(RunspacePool runspacePool)
        {
            Collection<PowerShell> psCollection = new Collection<PowerShell>();

            if (ConnectCommands == null)
            {
                // Throw error indicating that this runspacepool is not configured for
                // reconstructing commands.
                string msg = StringUtil.Format(RunspacePoolStrings.CannotReconstructCommands, this.Name);
                throw new InvalidRunspacePoolStateException(msg);
            }

            // Get list of all disconnected commands associated with this runspace pool.
            foreach (ConnectCommandInfo connectCmdInfo in ConnectCommands)
            {
                psCollection.Add(new PowerShell(connectCmdInfo, runspacePool));
            }

            return psCollection;
        }

        /// <summary>
        /// Returns RunspacePool capabilities.
        /// </summary>
        /// <returns>RunspacePoolCapability.</returns>
        public override RunspacePoolCapability GetCapabilities()
        {
            RunspacePoolCapability returnCaps = RunspacePoolCapability.Default;

            if (CanDisconnect)
            {
                returnCaps |= RunspacePoolCapability.SupportsDisconnect;
            }

            return returnCaps;
        }

        #endregion Public Methods

        #region Static methods

        internal static RunspacePool[] GetRemoteRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            if (connectionInfo is not WSManConnectionInfo wsmanConnectionInfoParam)
            {
                // Disconnect-Connect currently only supported by WSMan.
                throw new NotSupportedException();
            }

            List<RunspacePool> discRunspacePools = new List<RunspacePool>();

            // Enumerate all runspacepools
            Collection<PSObject> runspaceItems = RemoteRunspacePoolEnumeration.GetRemotePools(wsmanConnectionInfoParam);
            foreach (PSObject rsObject in runspaceItems)
            {
                // Create a new WSMan connection info object for each returned runspace pool.
                WSManConnectionInfo wsmanConnectionInfo = wsmanConnectionInfoParam.Copy();

                PSPropertyInfo pspShellId = rsObject.Properties["ShellId"];
                PSPropertyInfo pspState = rsObject.Properties["State"];
                PSPropertyInfo pspName = rsObject.Properties["Name"];
                PSPropertyInfo pspResourceUri = rsObject.Properties["ResourceUri"];

                if (pspShellId == null || pspState == null || pspName == null || pspResourceUri == null)
                {
                    continue;
                }

                string strName = pspName.Value.ToString();
                string strShellUri = pspResourceUri.Value.ToString();
                bool isDisconnected = pspState.Value.ToString().Equals("Disconnected", StringComparison.OrdinalIgnoreCase);
                Guid shellId = Guid.Parse(pspShellId.Value.ToString());

                // Filter returned items for PowerShell sessions.
                if (!strShellUri.StartsWith(WSManNativeApi.ResourceURIPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Update wsmanconnection information with server settings.
                UpdateWSManConnectionInfo(wsmanConnectionInfo, rsObject);
                // Ensure that EnableNetworkAccess property is always enabled for reconstructed runspaces.
                wsmanConnectionInfo.EnableNetworkAccess = true;

                // Compute runspace DisconnectedOn and ExpiresOn fields.
                if (isDisconnected)
                {
                    DateTime? disconnectedOn;
                    DateTime? expiresOn;
                    ComputeDisconnectedOnExpiresOn(rsObject, out disconnectedOn, out expiresOn);
                    wsmanConnectionInfo.DisconnectedOn = disconnectedOn;
                    wsmanConnectionInfo.ExpiresOn = expiresOn;
                }

                List<ConnectCommandInfo> connectCmdInfos = new List<ConnectCommandInfo>();

                // Enumerate all commands on runspace pool.
                Collection<PSObject> commandItems;
                try
                {
                    commandItems = RemoteRunspacePoolEnumeration.GetRemoteCommands(shellId, wsmanConnectionInfo);
                }
                catch (CmdletInvocationException e)
                {
                    if (e.InnerException != null && e.InnerException is InvalidOperationException)
                    {
                        // If we cannot successfully retrieve command information then this runspace
                        // object we are building is invalid and must be skipped.
                        continue;
                    }

                    throw;
                }

                foreach (PSObject cmdObject in commandItems)
                {
                    PSPropertyInfo pspCommandId = cmdObject.Properties["CommandId"];
                    PSPropertyInfo pspCommandLine = cmdObject.Properties["CommandLine"];

                    if (pspCommandId == null)
                    {
                        Dbg.Assert(false, "Should not get an empty command Id from a remote runspace pool.");
                        continue;
                    }

                    string cmdLine = (pspCommandLine != null) ? pspCommandLine.Value.ToString() : string.Empty;
                    Guid cmdId = Guid.Parse(pspCommandId.Value.ToString());

                    connectCmdInfos.Add(new ConnectCommandInfo(cmdId, cmdLine));
                }

                // At this point we don't know if the runspace pool we want to connect to has just one runspace
                // (a RemoteRunspace/PSSession) or multiple runspaces in its pool.  We do have an array of
                // running command information which will indicate a runspace pool if the count is gt one.
                RunspacePool runspacePool = new RunspacePool(isDisconnected, shellId, strName,
                    connectCmdInfos.ToArray(), wsmanConnectionInfo, host, typeTable);
                discRunspacePools.Add(runspacePool);
            }

            return discRunspacePools.ToArray();
        }

        internal static RunspacePool GetRemoteRunspacePool(RunspaceConnectionInfo connectionInfo, Guid sessionId, Guid? commandId, PSHost host, TypeTable typeTable)
        {
            List<ConnectCommandInfo> connectCmdInfos = new List<ConnectCommandInfo>();
            if (commandId != null)
            {
                connectCmdInfos.Add(new ConnectCommandInfo(commandId.Value, string.Empty));
            }

            return new RunspacePool(true, sessionId, string.Empty, connectCmdInfos.ToArray(), connectionInfo, host, typeTable);
        }

        private static void UpdateWSManConnectionInfo(
            WSManConnectionInfo wsmanConnectionInfo,
            PSObject rsInfoObject)
        {
            PSPropertyInfo pspIdleTimeOut = rsInfoObject.Properties["IdleTimeOut"];
            PSPropertyInfo pspBufferMode = rsInfoObject.Properties["BufferMode"];
            PSPropertyInfo pspResourceUri = rsInfoObject.Properties["ResourceUri"];
            PSPropertyInfo pspLocale = rsInfoObject.Properties["Locale"];
            PSPropertyInfo pspDataLocale = rsInfoObject.Properties["DataLocale"];
            PSPropertyInfo pspCompressionMode = rsInfoObject.Properties["CompressionMode"];
            PSPropertyInfo pspEncoding = rsInfoObject.Properties["Encoding"];
            PSPropertyInfo pspProfile = rsInfoObject.Properties["ProfileLoaded"];
            PSPropertyInfo pspMaxIdleTimeout = rsInfoObject.Properties["MaxIdleTimeout"];

            if (pspIdleTimeOut != null)
            {
                int idleTimeout;
                if (GetTimeIntValue(pspIdleTimeOut.Value as string, out idleTimeout))
                {
                    wsmanConnectionInfo.IdleTimeout = idleTimeout;
                }
            }

            if (pspBufferMode != null)
            {
                string bufferingMode = pspBufferMode.Value as string;
                if (bufferingMode != null)
                {
                    OutputBufferingMode outputBufferingMode;
                    if (Enum.TryParse<OutputBufferingMode>(bufferingMode, out outputBufferingMode))
                    {
                        // Update connection info.
                        wsmanConnectionInfo.OutputBufferingMode = outputBufferingMode;
                    }
                }
            }

            if (pspResourceUri != null)
            {
                string strShellUri = pspResourceUri.Value as string;
                if (strShellUri != null)
                {
                    wsmanConnectionInfo.ShellUri = strShellUri;
                }
            }

            if (pspLocale != null)
            {
                string localString = pspLocale.Value as string;
                if (localString != null)
                {
                    try
                    {
                        wsmanConnectionInfo.UICulture = new CultureInfo(localString);
                    }
                    catch (ArgumentException)
                    { }
                }
            }

            if (pspDataLocale != null)
            {
                string dataLocalString = pspDataLocale.Value as string;
                if (dataLocalString != null)
                {
                    try
                    {
                        wsmanConnectionInfo.Culture = new CultureInfo(dataLocalString);
                    }
                    catch (ArgumentException)
                    { }
                }
            }

            if (pspCompressionMode != null)
            {
                string compressionModeString = pspCompressionMode.Value as string;
                if (compressionModeString != null)
                {
                    wsmanConnectionInfo.UseCompression = !compressionModeString.Equals("NoCompression", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (pspEncoding != null)
            {
                string encodingString = pspEncoding.Value as string;
                if (encodingString != null)
                {
                    wsmanConnectionInfo.UseUTF16 = encodingString.Equals("UTF16", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (pspProfile != null)
            {
                string machineProfileLoadedString = pspProfile.Value as string;
                if (machineProfileLoadedString != null)
                {
                    wsmanConnectionInfo.NoMachineProfile = !machineProfileLoadedString.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (pspMaxIdleTimeout != null)
            {
                int maxIdleTimeout;
                if (GetTimeIntValue(pspMaxIdleTimeout.Value as string, out maxIdleTimeout))
                {
                    wsmanConnectionInfo.MaxIdleTimeout = maxIdleTimeout;
                }
            }
        }

        private static void ComputeDisconnectedOnExpiresOn(
            PSObject rsInfoObject,
            out DateTime? disconnectedOn,
            out DateTime? expiresOn)
        {
            PSPropertyInfo pspIdleTimeOut = rsInfoObject.Properties["IdleTimeOut"];
            PSPropertyInfo pspShellInactivity = rsInfoObject.Properties["ShellInactivity"];

            if (pspIdleTimeOut != null && pspShellInactivity != null)
            {
                string shellInactivityString = pspShellInactivity.Value as string;
                int idleTimeout;
                if ((shellInactivityString != null) &&
                    GetTimeIntValue(pspIdleTimeOut.Value as string, out idleTimeout))
                {
                    try
                    {
                        TimeSpan shellInactivityTime = Xml.XmlConvert.ToTimeSpan(shellInactivityString);
                        TimeSpan idleTimeoutTime = TimeSpan.FromSeconds(idleTimeout / 1000);

                        if (idleTimeoutTime > shellInactivityTime)
                        {
                            DateTime now = DateTime.Now;
                            disconnectedOn = now.Subtract(shellInactivityTime);
                            expiresOn = disconnectedOn.Value.Add(idleTimeoutTime);

                            return;
                        }
                    }
                    catch (FormatException)
                    { }
                    catch (ArgumentOutOfRangeException)
                    { }
                    catch (OverflowException)
                    { }
                }
            }

            disconnectedOn = null;
            expiresOn = null;
        }

        private static bool GetTimeIntValue(string timeString, out int value)
        {
            if (timeString != null)
            {
                string timeoutString = timeString.Replace("PT", string.Empty).Replace("S", string.Empty);
                try
                {
                    // Convert time from seconds to milliseconds.
                    int idleTimeout = (int)(Convert.ToDouble(timeoutString, CultureInfo.InvariantCulture) * 1000);
                    if (idleTimeout > 0)
                    {
                        value = idleTimeout;
                        return true;
                    }
                }
                catch (FormatException)
                { }
                catch (OverflowException)
                { }
            }

            value = 0;
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Set the new runspace pool state based on the state of the
        /// server RunspacePool.
        /// </summary>
        /// <param name="newStateInfo">state information object
        /// describing the state change at the server RunspacePool</param>
        private void SetRunspacePoolState(RunspacePoolStateInfo newStateInfo)
        {
            SetRunspacePoolState(newStateInfo, false);
        }

        /// <summary>
        /// Set the new runspace pool state based on the state of the
        /// server RunspacePool and raise events if required.
        /// </summary>
        /// <param name="newStateInfo">state information object
        /// describing the state change at the server RunspacePool</param>
        /// <param name="raiseEvents">Raise state changed events if true.</param>
        private void SetRunspacePoolState(RunspacePoolStateInfo newStateInfo, bool raiseEvents)
        {
            stateInfo = newStateInfo;

            // Update the availableForConnection variable based on state change.
            AvailableForConnection = (stateInfo.State == RunspacePoolState.Disconnected ||
                                           stateInfo.State == RunspacePoolState.Opened);

            if (raiseEvents)
            {
                RaiseStateChangeEvent(newStateInfo);
            }
        }

        private void HandleSessionDisconnected(object sender, RemoteDataEventArgs<Exception> eventArgs)
        {
            bool stateChange = false;
            lock (this.syncObject)
            {
                if (stateInfo.State == RunspacePoolState.Disconnecting)
                {
                    UpdateDisconnectedExpiresOn();

                    SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Disconnected, eventArgs.Data));
                    stateChange = true;
                }

                // Set boolean indicating this object has previous connection state and so
                // can be reconnected as opposed to the alternative where the connection
                // state has to be reconstructed then connected.
                _canReconnect = true;
            }

            // Do state change work outside of lock.
            if (stateChange)
            {
                RaiseStateChangeEvent(this.stateInfo);
                SetDisconnectAsCompleted();
            }
        }

        private void SetDisconnectAsCompleted()
        {
            if (_disconnectAsyncResult != null && !_disconnectAsyncResult.IsCompleted)
            {
                _disconnectAsyncResult.SetAsCompleted(stateInfo.Reason);
                _disconnectAsyncResult = null;
            }
        }

        private void HandleSessionReconnected(object sender, RemoteDataEventArgs<Exception> eventArgs)
        {
            bool stateChange = false;
            lock (this.syncObject)
            {
                if (stateInfo.State == RunspacePoolState.Connecting)
                {
                    ResetDisconnectedOnExpiresOn();

                    SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Opened, null));
                    stateChange = true;
                }
            }

            // Do state change work outside of lock.
            if (stateChange)
            {
                RaiseStateChangeEvent(this.stateInfo);
                SetReconnectAsCompleted();
            }
        }

        private void SetReconnectAsCompleted()
        {
            if (_reconnectAsyncResult != null && !_reconnectAsyncResult.IsCompleted)
            {
                _reconnectAsyncResult.SetAsCompleted(stateInfo.Reason);
                _reconnectAsyncResult = null;
            }
        }

        /// <summary>
        /// The session is closing set the state and reason accordingly.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleSessionClosing(object sender, RemoteDataEventArgs<Exception> eventArgs)
        {
            // just capture the reason for closing here..handle the session closed event
            // to change state appropriately.
            _closingReason = eventArgs.Data;
        }

        /// <summary>
        /// The session closed, set the state and reason accordingly.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleSessionClosed(object sender, RemoteDataEventArgs<Exception> eventArgs)
        {
            if (eventArgs.Data != null)
            {
                _closingReason = eventArgs.Data;
            }

            // Set state under lock.
            RunspacePoolState prevState;
            RunspacePoolStateInfo finishedStateInfo;
            lock (syncObject)
            {
                prevState = stateInfo.State;

                switch (prevState)
                {
                    case RunspacePoolState.Opening:
                    case RunspacePoolState.Opened:
                    case RunspacePoolState.Disconnecting:
                    case RunspacePoolState.Disconnected:
                    case RunspacePoolState.Connecting:
                        // Since RunspacePool is not in closing state, this close is
                        // happening because of data structure handler error. Set the state to broken.
                        SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Broken, _closingReason));
                        break;

                    case RunspacePoolState.Closing:
                        SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Closed, _closingReason));
                        break;
                }

                finishedStateInfo = new RunspacePoolStateInfo(stateInfo.State, stateInfo.Reason);
            }

            // Raise notification event outside of lock.
            try
            {
                RaiseStateChangeEvent(finishedStateInfo);
            }
            catch (Exception)
            {
                // Don't throw exception on notification thread.
            }

            // Check if we have either an existing disconnect or connect async object
            // and if so make sure they are set to completed since this is a
            // final state for the runspace pool.
            SetDisconnectAsCompleted();
            SetReconnectAsCompleted();

            // Ensure an existing Close async object is completed.
            SetCloseAsCompleted();
        }

        /// <summary>
        /// Set the async result for open as completed.
        /// </summary>
        private void SetOpenAsCompleted()
        {
            RunspacePoolAsyncResult tempOpenAsyncResult = _openAsyncResult;
            _openAsyncResult = null;
            if (tempOpenAsyncResult != null && !tempOpenAsyncResult.IsCompleted)
            {
                tempOpenAsyncResult.SetAsCompleted(stateInfo.Reason);
            }
        }

        /// <summary>
        /// Set the async result for close as completed.
        /// </summary>
        private void SetCloseAsCompleted()
        {
            // abort all pending calls.
            DispatchTable.AbortAllCalls();

            if (_closeAsyncResult != null)
            {
                _closeAsyncResult.SetAsCompleted(stateInfo.Reason);
                _closeAsyncResult = null;
            }

            // Ensure that openAsyncResult is completed and that
            // any error is thrown on the calling thread.
            // The session can be closed at any time, including
            // during Open processing.
            SetOpenAsCompleted();

            // Ensure private application data wait is released.
            try
            {
                _applicationPrivateDataReceived.Set();
            }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// When a response to a SetMaxRunspaces or SetMinRunspaces is received,
        /// from the server, this method sets the response and thereby unblocks
        /// corresponding call.
        /// </summary>
        /// <param name="sender">Sender of this message, unused.</param>
        /// <param name="eventArgs">Contains response and call id.</param>
        private void HandleResponseReceived(object sender, RemoteDataEventArgs<PSObject> eventArgs)
        {
            PSObject data = eventArgs.Data;
            object response = RemotingDecoder.GetPropertyValue<object>(data, RemoteDataNameStrings.RunspacePoolOperationResponse);
            long callId = RemotingDecoder.GetPropertyValue<long>(data, RemoteDataNameStrings.CallId);
            DispatchTable.SetResponse(callId, response);
        }

        /// <summary>
        /// When the client remote session reports a URI redirection, this method will report the
        /// message to the user as a Warning using Host method calls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleURIDirectionReported(object sender, RemoteDataEventArgs<Uri> eventArgs)
        {
            WSManConnectionInfo wsmanConnectionInfo = _connectionInfo as WSManConnectionInfo;
            if (wsmanConnectionInfo != null)
            {
                wsmanConnectionInfo.ConnectionUri = eventArgs.Data;
                URIRedirectionReported.SafeInvoke(this, eventArgs);
            }
        }

        /// <summary>
        /// When the server sends a PSEventArgs this method will add it to the local event queue.
        /// </summary>
        private void HandlePSEventArgsReceived(object sender, RemoteDataEventArgs<PSEventArgs> e)
        {
            OnForwardEvent(e.Data);
        }

        /// <summary>
        /// A session disconnect has been initiated by the WinRM robust connection layer.  Set
        /// internal state to Disconnecting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleSessionRCDisconnecting(object sender, RemoteDataEventArgs<Exception> e)
        {
            Dbg.Assert(this.stateInfo.State == RunspacePoolState.Opened,
                "RC disconnect should only occur for runspace pools in the Opened state.");

            lock (this.syncObject)
            {
                SetRunspacePoolState(new RunspacePoolStateInfo(RunspacePoolState.Disconnecting, e.Data));
            }

            RaiseStateChangeEvent(this.stateInfo);
        }

        /// <summary>
        /// The session has been successfully created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleSessionCreateCompleted(object sender, CreateCompleteEventArgs eventArgs)
        {
            // Update connectionInfo with updated information from the transport.
            if (eventArgs != null)
            {
                _connectionInfo.IdleTimeout = eventArgs.ConnectionInfo.IdleTimeout;
                _connectionInfo.MaxIdleTimeout = eventArgs.ConnectionInfo.MaxIdleTimeout;
                WSManConnectionInfo wsmanConnectionInfo = _connectionInfo as WSManConnectionInfo;
                if (wsmanConnectionInfo != null)
                {
                    wsmanConnectionInfo.OutputBufferingMode =
                        ((WSManConnectionInfo)eventArgs.ConnectionInfo).OutputBufferingMode;
                }
            }

            // Forward event.
            SessionCreateCompleted.SafeInvoke<CreateCompleteEventArgs>(this, eventArgs);
        }

        private void ResetDisconnectedOnExpiresOn()
        {
            // Reset DisconnectedOn/ExpiresOn
            WSManConnectionInfo wsManConnectionInfo = _connectionInfo as WSManConnectionInfo;
            wsManConnectionInfo?.NullDisconnectedExpiresOn();
        }

        private void UpdateDisconnectedExpiresOn()
        {
            // Set DisconnectedOn/ExpiresOn for disconnected session.
            WSManConnectionInfo wsManConnectionInfo = _connectionInfo as WSManConnectionInfo;
            wsManConnectionInfo?.SetDisconnectedExpiresOnToNow();
        }

        /// <summary>
        /// Waits for application private data from server before raising
        /// event:  Connecting->Opened state changed event.
        /// </summary>
        /// <param name="state"></param>
        private void WaitAndRaiseConnectEventsProc(object state)
        {
            RunspacePoolStateInfo info = state as RunspacePoolStateInfo;
            Dbg.Assert(info != null, "State -> Event arguments cannot be null.");

            // Wait for private application data to arrive from server.
            try
            {
                _applicationPrivateDataReceived.WaitOne();
            }
            catch (ObjectDisposedException) { }

            // Raise state changed event.
            try
            {
                RaiseStateChangeEvent(info);
            }
            catch (Exception)
            {
            }

            // Set Opened async object.
            SetOpenAsCompleted();
        }

        #endregion Private Methods

        #region Private Members

        private readonly RunspaceConnectionInfo _connectionInfo;     // connection info with which this
        // runspace is created
        // data structure handler handling
        private RunspacePoolAsyncResult _openAsyncResult; // async result object generated on
        // CoreOpen
        private RunspacePoolAsyncResult _closeAsyncResult; // async result object generated by
        // BeginClose
        private Exception _closingReason;                       // reason for a Closing state transition
        private RunspacePoolAsyncResult _disconnectAsyncResult; // async result object generated on CoreDisconnect
        private RunspacePoolAsyncResult _reconnectAsyncResult;  // async result object generated on CoreReconnect
        private bool _isDisposed;

        private DispatchTable<object> DispatchTable { get; }

        private bool _canReconnect;
        private string _friendlyName = string.Empty;

        private readonly System.Collections.Concurrent.ConcurrentStack<PowerShell> _runningPowerShells;

        #endregion Private Members

        #region IDisposable

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            // dispose the base class before disposing dataStructure handler.
            base.Dispose(disposing);
            if (!_isDisposed)
            {
                _isDisposed = true;
                DataStructureHandler.Dispose();
                _applicationPrivateDataReceived.Dispose();
            }
        }

        #endregion IDisposable
    }

    #region ConnectCommandInfo class

    /// <summary>
    /// Class defining a remote command to connect to.
    /// </summary>
    internal class ConnectCommandInfo
    {
        /// <summary>
        /// Remote command instance Id.
        /// </summary>
        public Guid CommandId { get; } = Guid.Empty;

        /// <summary>
        /// Remote command string.
        /// </summary>
        public string Command { get; } = string.Empty;

        /// <summary>
        /// Constructs a remote command object.
        /// </summary>
        /// <param name="cmdId">Command instance Id.</param>
        /// <param name="cmdStr">Command string.</param>
        public ConnectCommandInfo(Guid cmdId, string cmdStr)
        {
            CommandId = cmdId;
            Command = cmdStr;
        }
    }

    #endregion

    #region RemoteRunspacePoolEnumeration class

    /// <summary>
    /// Enumerates remote runspacepools (Shells) and running commands
    /// using Get-WSManInstance cmdlet.
    /// </summary>
    internal static class RemoteRunspacePoolEnumeration
    {
        /// <summary>
        /// Gets an array of XmlElement objects representing all
        /// disconnected runspace pools on the indicated server.
        /// </summary>
        /// <param name="wsmanConnectionInfo">Specifies the remote server to connect to.</param>]
        /// <returns>Collection of XmlElement objects.</returns>
        internal static Collection<PSObject> GetRemotePools(WSManConnectionInfo wsmanConnectionInfo)
        {
            Collection<PSObject> result;
            using (PowerShell powerShell = PowerShell.Create())
            {
                // Enumerate remote runspaces using the Get-WSManInstance cmdlet.
                powerShell.AddCommand("Get-WSManInstance");

                // Add parameters to enumerate Shells (runspace pools).
                powerShell.AddParameter("ResourceURI", "Shell");
                powerShell.AddParameter("Enumerate", true);

                // Add parameters for server connection.
                powerShell.AddParameter("ComputerName", wsmanConnectionInfo.ComputerName);
                powerShell.AddParameter("Authentication", ConvertPSAuthToWSManAuth(wsmanConnectionInfo.AuthenticationMechanism));
                if (wsmanConnectionInfo.Credential != null)
                {
                    powerShell.AddParameter("Credential", wsmanConnectionInfo.Credential);
                }

                if (wsmanConnectionInfo.CertificateThumbprint != null)
                {
                    powerShell.AddParameter("CertificateThumbprint", wsmanConnectionInfo.CertificateThumbprint);
                }

                if (wsmanConnectionInfo.PortSetting != -1)
                {
                    powerShell.AddParameter("Port", wsmanConnectionInfo.Port);
                }

                if (CheckForSSL(wsmanConnectionInfo))
                {
                    powerShell.AddParameter("UseSSL", true);
                }

                if (!string.IsNullOrEmpty(wsmanConnectionInfo.AppName))
                {
                    // Remove prepended path character.
                    string appName = wsmanConnectionInfo.AppName.TrimStart('/');
                    powerShell.AddParameter("ApplicationName", appName);
                }

                powerShell.AddParameter("SessionOption", GetSessionOptions(wsmanConnectionInfo));

                result = powerShell.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Gets an array of XmlElement objects representing each running command
        /// on the specified runspace pool with the shellid Guid.
        /// </summary>
        /// <param name="shellId">Guid of shellId (runspacepool Id).</param>
        /// <param name="wsmanConnectionInfo">Specifies the remote server to connect to.</param>]
        /// <returns>Collection of XmlElement objects.</returns>
        internal static Collection<PSObject> GetRemoteCommands(Guid shellId, WSManConnectionInfo wsmanConnectionInfo)
        {
            Collection<PSObject> result;
            using (PowerShell powerShell = PowerShell.Create())
            {
                // Enumerate remote runspace commands using the Get-WSManInstance cmdlet.
                powerShell.AddCommand("Get-WSManInstance");

                // Add parameters to enumerate commands.
                string filterStr = string.Create(CultureInfo.InvariantCulture, $"ShellId='{shellId.ToString().ToUpperInvariant()}'");
                powerShell.AddParameter("ResourceURI", @"Shell/Command");
                powerShell.AddParameter("Enumerate", true);
                powerShell.AddParameter("Dialect", "Selector");
                powerShell.AddParameter("Filter", filterStr);

                // Add parameters for server connection.
                powerShell.AddParameter("ComputerName", wsmanConnectionInfo.ComputerName);
                powerShell.AddParameter("Authentication", ConvertPSAuthToWSManAuth(wsmanConnectionInfo.AuthenticationMechanism));
                if (wsmanConnectionInfo.Credential != null)
                {
                    powerShell.AddParameter("Credential", wsmanConnectionInfo.Credential);
                }

                if (wsmanConnectionInfo.CertificateThumbprint != null)
                {
                    powerShell.AddParameter("CertificateThumbprint", wsmanConnectionInfo.CertificateThumbprint);
                }

                if (wsmanConnectionInfo.PortSetting != -1)
                {
                    powerShell.AddParameter("Port", wsmanConnectionInfo.Port);
                }

                if (CheckForSSL(wsmanConnectionInfo))
                {
                    powerShell.AddParameter("UseSSL", true);
                }

                if (!string.IsNullOrEmpty(wsmanConnectionInfo.AppName))
                {
                    // Remove prepended path character.
                    string appName = wsmanConnectionInfo.AppName.TrimStart('/');
                    powerShell.AddParameter("ApplicationName", appName);
                }

                powerShell.AddParameter("SessionOption", GetSessionOptions(wsmanConnectionInfo));

                result = powerShell.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Use the WSMan New-WSManSessionOption cmdlet to create a session options
        /// object used for Get-WSManInstance queries.
        /// </summary>
        /// <param name="wsmanConnectionInfo">WSManConnectionInfo.</param>
        /// <returns>WSMan session options object.</returns>
        private static object GetSessionOptions(WSManConnectionInfo wsmanConnectionInfo)
        {
            Collection<PSObject> result;
            using (PowerShell powerShell = PowerShell.Create())
            {
                powerShell.AddCommand("New-WSManSessionOption");

                if (wsmanConnectionInfo.ProxyAccessType != ProxyAccessType.None)
                {
                    powerShell.AddParameter("ProxyAccessType", "Proxy" + wsmanConnectionInfo.ProxyAccessType.ToString());
                    powerShell.AddParameter("ProxyAuthentication", wsmanConnectionInfo.ProxyAuthentication.ToString());

                    if (wsmanConnectionInfo.ProxyCredential != null)
                    {
                        powerShell.AddParameter("ProxyCredential", wsmanConnectionInfo.ProxyCredential);
                    }
                }

                // New-WSManSessionOption uses the SPNPort number here to enable SPN
                // server authentication.  It looks like any value > 0 will enable
                // this.  Since the Port property always returns a valid port value (>0)
                // just pass the WSManConnectionInfo port parameter.
                if (wsmanConnectionInfo.IncludePortInSPN)
                {
                    powerShell.AddParameter("SPNPort", wsmanConnectionInfo.Port);
                }

                powerShell.AddParameter("SkipCACheck", wsmanConnectionInfo.SkipCACheck);
                powerShell.AddParameter("SkipCNCheck", wsmanConnectionInfo.SkipCNCheck);
                powerShell.AddParameter("SkipRevocationCheck", wsmanConnectionInfo.SkipRevocationCheck);

                powerShell.AddParameter("OperationTimeout", wsmanConnectionInfo.OperationTimeout);
                powerShell.AddParameter("NoEncryption", wsmanConnectionInfo.NoEncryption);
                powerShell.AddParameter("UseUTF16", wsmanConnectionInfo.UseUTF16);

                result = powerShell.Invoke();
            }

            return result[0].BaseObject;
        }

        private static bool CheckForSSL(WSManConnectionInfo wsmanConnectionInfo)
        {
            return (!string.IsNullOrEmpty(wsmanConnectionInfo.Scheme) &&
                    wsmanConnectionInfo.Scheme.Contains(WSManConnectionInfo.HttpsScheme, StringComparison.OrdinalIgnoreCase));
        }

        private static int ConvertPSAuthToWSManAuth(AuthenticationMechanism psAuth)
        {
            int wsmanAuth;

            switch (psAuth)
            {
                case AuthenticationMechanism.Default:
                    wsmanAuth = 0x1;
                    break;

                case AuthenticationMechanism.Basic:
                    wsmanAuth = 0x8;
                    break;

                case AuthenticationMechanism.Digest:
                    wsmanAuth = 0x2;
                    break;

                case AuthenticationMechanism.Credssp:
                    wsmanAuth = 0x80;
                    break;

                case AuthenticationMechanism.Kerberos:
                    wsmanAuth = 0x10;
                    break;

                case AuthenticationMechanism.Negotiate:
                    wsmanAuth = 0x4;
                    break;

                default:
                    wsmanAuth = 0x1;
                    break;
            }

            return wsmanAuth;
        }
    }

    #endregion
}
