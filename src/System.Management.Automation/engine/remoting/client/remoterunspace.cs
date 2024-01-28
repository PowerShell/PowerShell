// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Management.Automation.Tracing;
#if !UNIX
using System.Security.Principal;
#endif
using System.Threading;
using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    /// <summary>
    /// Remote runspace which will be created on the client side. This
    /// runspace is wrapped on a RunspacePool(1).
    /// </summary>
    internal sealed class RemoteRunspace : Runspace, IDisposable
    {
        #region Private Members

        private readonly List<RemotePipeline> _runningPipelines = new List<RemotePipeline>();
        private readonly object _syncRoot = new object();
        private RunspaceStateInfo _runspaceStateInfo = new RunspaceStateInfo(RunspaceState.BeforeOpen);
        private readonly bool _bSessionStateProxyCallInProgress = false;
        private readonly RunspaceConnectionInfo _connectionInfo;
        private RemoteDebugger _remoteDebugger;
        private PSPrimitiveDictionary _applicationPrivateData;

        private bool _disposed = false;

        // the following two variables have been added for supporting
        // the Invoke-Command | Invoke-Command scenario
        private InvokeCommandCommand _currentInvokeCommand = null;
        private long _currentLocalPipelineId = 0;

        /// <summary>
        /// This is queue of all the state change event which have occurred for
        /// this runspace. RaiseRunspaceStateEvents raises event for each
        /// item in this queue. We don't raise events from with SetRunspaceState
        /// because SetRunspaceState is often called from with in the a lock.
        /// Raising event with in a lock introduces chances of deadlock in GUI
        /// applications.
        /// </summary>
        private Queue<RunspaceEventQueueItem> _runspaceEventQueue = new Queue<RunspaceEventQueueItem>();

        private sealed class RunspaceEventQueueItem
        {
            public RunspaceEventQueueItem(RunspaceStateInfo runspaceStateInfo, RunspaceAvailability currentAvailability, RunspaceAvailability newAvailability)
            {
                this.RunspaceStateInfo = runspaceStateInfo;
                this.CurrentRunspaceAvailability = currentAvailability;
                this.NewRunspaceAvailability = newAvailability;
            }

            public RunspaceStateInfo RunspaceStateInfo;
            public RunspaceAvailability CurrentRunspaceAvailability;
            public RunspaceAvailability NewRunspaceAvailability;
        }

        /// <summary>
        /// In RemoteRunspace, it is required to invoke pipeline
        /// as part of open call (i.e. while state is Opening).
        /// If this property is true, runspace state check is
        /// not performed in AddToRunningPipelineList call.
        /// </summary>
        private bool _bypassRunspaceStateCheck;

        /// <summary>
        /// In RemoteRunspace, it is required to invoke pipeline
        /// as part of open call (i.e. while state is Opening).
        /// If this property is true, runspace state check is
        /// not performed in AddToRunningPipelineList call.
        /// </summary>
        private bool ByPassRunspaceStateCheck
        {
            get
            {
                return _bypassRunspaceStateCheck;
            }

            set
            {
                _bypassRunspaceStateCheck = value;
            }
        }

        /// <summary>
        /// Temporary place to remember whether to close this runspace on pop or not.
        /// Used by Start-PSSession.
        /// </summary>
        internal bool ShouldCloseOnPop { get; set; } = false;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Construct a remote runspace based on the connection information
        /// and the specified host.
        /// </summary>
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
        /// <param name="connectionInfo">connection information which identifies
        /// the remote computer</param>
        /// <param name="host">Host on the client.</param>
        /// <param name="applicationArguments">
        /// <param name="name">Friendly name for remote runspace session.</param>
        /// <param name="id">Id for remote runspace.</param>
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        internal RemoteRunspace(TypeTable typeTable, RunspaceConnectionInfo connectionInfo, PSHost host, PSPrimitiveDictionary applicationArguments, string name = null, int id = -1)
        {
            PSEtwLog.SetActivityIdForCurrentThread(this.InstanceId);
            PSEtwLog.LogOperationalVerbose(PSEventId.RunspaceConstructor, PSOpcode.Constructor,
                        PSTask.CreateRunspace, PSKeyword.UseAlwaysOperational,
                        InstanceId.ToString());

            _connectionInfo = connectionInfo.Clone();
            OriginalConnectionInfo = connectionInfo.Clone();

            RunspacePool = new RunspacePool(1, 1, typeTable, host, applicationArguments, connectionInfo, name);

            this.PSSessionId = id;

            SetEventHandlers();
        }

        /// <summary>
        /// Constructs a RemoteRunspace object based on the passed in RunspacePool object,
        /// with a starting state of Disconnected.
        /// </summary>
        /// <param name="runspacePool"></param>
        internal RemoteRunspace(RunspacePool runspacePool)
        {
            // The RemoteRunspace object can only be constructed this way with a RunspacePool that
            // is in the disconnected state.
            if (runspacePool.RunspacePoolStateInfo.State != RunspacePoolState.Disconnected
                || runspacePool.ConnectionInfo is not WSManConnectionInfo)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.InvalidRunspacePool);
            }

            RunspacePool = runspacePool;

            // The remote runspace pool object can only have the value one set for min/max pools.
            // This sets the runspace pool object min/max pool values to one.  The PSRP/WSMan stack
            // will fail during connection if the min/max pool values do not match.
            RunspacePool.RemoteRunspacePoolInternal.SetMinRunspaces(1);
            RunspacePool.RemoteRunspacePoolInternal.SetMaxRunspaces(1);

            _connectionInfo = runspacePool.ConnectionInfo.Clone();

            // Update runspace DisconnectedOn and ExpiresOn property from WSManConnectionInfo
            UpdateDisconnectExpiresOn();

            // Initial state must be Disconnected.
            SetRunspaceState(RunspaceState.Disconnected, null);

            // Normal Availability for a disconnected runspace is "None", which means it can be connected.
            // However, we can also have disconnected runspace objects that are *not* available for
            // connection and in this case the Availability is set to "Busy".
            _runspaceAvailability = RunspacePool.RemoteRunspacePoolInternal.AvailableForConnection ?
                Runspaces.RunspaceAvailability.None : Runspaces.RunspaceAvailability.Busy;

            SetEventHandlers();

            PSEtwLog.SetActivityIdForCurrentThread(this.InstanceId);
            PSEtwLog.LogOperationalVerbose(PSEventId.RunspaceConstructor, PSOpcode.Constructor,
                        PSTask.CreateRunspace, PSKeyword.UseAlwaysOperational,
                        this.InstanceId.ToString());
        }

        /// <summary>
        /// Helper function to set event handlers.
        /// </summary>
        private void SetEventHandlers()
        {
            // RemoteRunspace must have the same instanceID as its contained RunspacePool instance because
            // the PSRP/WinRS layer tracks remote runspace Ids.
            this.InstanceId = RunspacePool.InstanceId;

            _eventManager = new PSRemoteEventManager(_connectionInfo.ComputerName, this.InstanceId);

            RunspacePool.StateChanged += HandleRunspacePoolStateChanged;
            RunspacePool.RemoteRunspacePoolInternal.HostCallReceived += HandleHostCallReceived;
            RunspacePool.RemoteRunspacePoolInternal.URIRedirectionReported += HandleURIDirectionReported;
            RunspacePool.ForwardEvent += HandleRunspacePoolForwardEvent;
            RunspacePool.RemoteRunspacePoolInternal.SessionCreateCompleted += HandleSessionCreateCompleted;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Initialsessionstate information for this runspace.
        /// </summary>
        public override InitialSessionState InitialSessionState
        {
            get
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        /// <summary>
        /// Manager for JobSourceAdapters registered in this runspace.
        /// </summary>
        public override JobManager JobManager
        {
            get
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        /// <summary>
        /// Return version of this runspace.
        /// </summary>
        public override Version Version { get; } = PSVersionInfo.PSVersion;

        /// <summary>
        /// PS Version running on server.
        /// </summary>
        internal Version ServerVersion { get; private set; }

        /// <summary>
        /// Retrieve information about current state of the runspace.
        /// </summary>
        public override RunspaceStateInfo RunspaceStateInfo
        {
            get
            {
                lock (_syncRoot)
                {
                    // Do not return internal state.
                    return _runspaceStateInfo.Clone();
                }
            }
        }

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
        public override PSThreadOptions ThreadOptions
        {
            get
            {
                return _createThreadOptions;
            }

            set
            {
                lock (_syncRoot)
                {
                    if (value != _createThreadOptions)
                    {
                        if (this.RunspaceStateInfo.State != RunspaceState.BeforeOpen)
                        {
                            throw new InvalidRunspaceStateException(StringUtil.Format(RunspaceStrings.ChangePropertyAfterOpen));
                        }

                        _createThreadOptions = value;
                    }
                }
            }
        }

        private PSThreadOptions _createThreadOptions = PSThreadOptions.Default;

        /// <summary>
        /// Gets the current availability of the Runspace.
        /// </summary>
        public override RunspaceAvailability RunspaceAvailability
        {
            get { return _runspaceAvailability; }

            protected set { _runspaceAvailability = value; }
        }

        private RunspaceAvailability _runspaceAvailability = RunspaceAvailability.None;

        /// <summary>
        /// Event raised when RunspaceState changes.
        /// </summary>
        public override event EventHandler<RunspaceStateEventArgs> StateChanged;

        /// <summary>
        /// Event raised when the availability of the Runspace changes.
        /// </summary>
        public override event EventHandler<RunspaceAvailabilityEventArgs> AvailabilityChanged;

        /// <summary>
        /// Returns true if there are any subscribers to the AvailabilityChanged event.
        /// </summary>
        internal override bool HasAvailabilityChangedSubscribers
        {
            get { return this.AvailabilityChanged != null; }
        }

        /// <summary>
        /// Raises the AvailabilityChanged event.
        /// </summary>
        protected override void OnAvailabilityChanged(RunspaceAvailabilityEventArgs e)
        {
            EventHandler<RunspaceAvailabilityEventArgs> eh = this.AvailabilityChanged;

            if (eh != null)
            {
                try
                {
                    eh(this, e);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Connection information to this runspace.
        /// </summary>
        public override RunspaceConnectionInfo ConnectionInfo
        {
            get
            {
                return _connectionInfo;
            }
        }

        /// <summary>
        /// ConnectionInfo originally supplied by the user.
        /// </summary>
        public override RunspaceConnectionInfo OriginalConnectionInfo { get; }

        /// <summary>
        /// Gets the event manager.
        /// </summary>
        public override PSEventManager Events
        {
            get
            {
                return _eventManager;
            }
        }

        private PSRemoteEventManager _eventManager;

#pragma warning disable 56503

        /// <summary>
        /// Gets the execution context for this runspace.
        /// </summary>
        internal override ExecutionContext GetExecutionContext
        {
            get
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        /// <summary>
        /// Returns true if the internal host is in a nested prompt.
        /// </summary>
        internal override bool InNestedPrompt
        {
            get
            {
                return false; // nested prompts are not supported on remote runspaces
            }
        }

#pragma warning restore 56503

        /// <summary>
        /// Gets the client remote session associated with this
        /// runspace.
        /// </summary>
        /// <remarks>This member is actually not required
        /// for the product code. However, there are
        /// existing transport manager tests which depend on
        /// the same. Once transport manager is modified,
        /// this needs to be removed</remarks>
        internal ClientRemoteSession ClientRemoteSession
        {
            get
            {
                try
                {
                    return RunspacePool.RemoteRunspacePoolInternal.DataStructureHandler.RemoteSession;
                }
                catch (InvalidRunspacePoolStateException e)
                {
                    throw e.ToInvalidRunspaceStateException();
                }
            }
        }

        /// <summary>
        /// Gets command information on a currently running remote command.
        /// If no command is running then null is returned.
        /// </summary>
        internal ConnectCommandInfo RemoteCommand
        {
            get
            {
                if (RunspacePool.RemoteRunspacePoolInternal.ConnectCommands == null)
                {
                    return null;
                }

                Dbg.Assert(RunspacePool.RemoteRunspacePoolInternal.ConnectCommands.Length < 2, "RemoteRunspace should have no more than one remote running command.");
                if (RunspacePool.RemoteRunspacePoolInternal.ConnectCommands.Length > 0)
                {
                    return RunspacePool.RemoteRunspacePoolInternal.ConnectCommands[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets friendly name for the remote PSSession.
        /// </summary>
        internal string PSSessionName
        {
            get { return RunspacePool.RemoteRunspacePoolInternal.Name; }

            set { RunspacePool.RemoteRunspacePoolInternal.Name = value; }
        }

        /// <summary>
        /// Gets the Id value for the remote PSSession.
        /// </summary>
        internal int PSSessionId { get; set; } = -1;

        /// <summary>
        /// Returns true if Runspace supports disconnect.
        /// </summary>
        internal bool CanDisconnect
        {
            get { return RunspacePool.RemoteRunspacePoolInternal.CanDisconnect; }
        }

        /// <summary>
        /// Returns true if Runspace can be connected.
        /// </summary>
        internal bool CanConnect
        {
            get { return RunspacePool.RemoteRunspacePoolInternal.AvailableForConnection; }
        }

        /// <summary>
        /// This is used to indicate a special loopback remote session used for JEA restrictions.
        /// </summary>
        internal bool IsConfiguredLoopBack
        {
            get;
            set;
        }

        /// <summary>
        /// Debugger.
        /// </summary>
        public override Debugger Debugger
        {
            get
            {
                return _remoteDebugger;
            }
        }

        #endregion Properties

        #region Open

        /// <summary>
        /// Open the runspace Asynchronously.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        public override void OpenAsync()
        {
            AssertIfStateIsBeforeOpen();

            try
            {
                RunspacePool.BeginOpen(null, null);
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        /// <summary>
        /// Open the runspace synchronously.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        public override void Open()
        {
            AssertIfStateIsBeforeOpen();

            try
            {
                RunspacePool.ThreadOptions = this.ThreadOptions;
                RunspacePool.ApartmentState = this.ApartmentState;
                RunspacePool.Open();
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        #endregion Open

        #region Close

        /// <summary>
        /// Close the runspace Asynchronously.
        /// </summary>
        public override void CloseAsync()
        {
            try
            {
                RunspacePool.BeginClose(null, null);
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        /// <summary>
        /// Close the runspace synchronously.
        /// </summary>
        /// <remarks>
        /// Attempts to execute pipelines after a call to close will fail.
        /// </remarks>
        public override void Close()
        {
            try
            {
                IAsyncResult result = RunspacePool.BeginClose(null, null);

                WaitForFinishofPipelines();

                // It is possible for the result ASyncResult object to be null if the runspace
                // pool is already being closed from a server initiated close event.
                if (result != null)
                {
                    RunspacePool.EndClose(result);
                }
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        /// <summary>
        /// Dispose this runspace.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                lock (_syncRoot)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                }

                if (disposing)
                {
                    try
                    {
                        Close();
                    }
                    catch (PSRemotingTransportException)
                    {
                        //
                        // If the WinRM listener has been removed before the runspace is closed, then calling
                        // Close() will cause a PSRemotingTransportException.  We don't want this exception
                        // surfaced.  Most developers don't expect an exception from calling Dispose.
                        // See [Windows 8 Bugs] 968184.
                        //
                    }

                    // Release RunspacePool event forwarding handlers.
                    _remoteDebugger?.Dispose();

                    try
                    {
                        RunspacePool.StateChanged -= HandleRunspacePoolStateChanged;
                        RunspacePool.RemoteRunspacePoolInternal.HostCallReceived -= HandleHostCallReceived;
                        RunspacePool.RemoteRunspacePoolInternal.URIRedirectionReported -= HandleURIDirectionReported;
                        RunspacePool.ForwardEvent -= HandleRunspacePoolForwardEvent;
                        RunspacePool.RemoteRunspacePoolInternal.SessionCreateCompleted -= HandleSessionCreateCompleted;

                        _eventManager = null;

                        RunspacePool.Dispose();
                        // _runspacePool = null;
                    }
                    catch (InvalidRunspacePoolStateException e)
                    {
                        throw e.ToInvalidRunspaceStateException();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion Close

        #region Reset Runspace State

        /// <summary>
        /// Resets the runspace state to allow for fast reuse. Not all of the runspace
        /// elements are reset. The goal is to minimize the chance of the user taking
        /// accidental dependencies on prior runspace state.
        /// </summary>
        /// <exception cref="PSInvalidOperationException">
        /// Thrown when runspace is not in proper state or availability or if the
        /// reset operation fails in the remote session.
        /// </exception>
        public override void ResetRunspaceState()
        {
            PSInvalidOperationException invalidOperation = null;

            if (this.RunspaceStateInfo.State != Runspaces.RunspaceState.Opened)
            {
                invalidOperation = PSTraceSource.NewInvalidOperationException(
                        RunspaceStrings.RunspaceNotInOpenedState, this.RunspaceStateInfo.State);
            }
            else if (this.RunspaceAvailability != Runspaces.RunspaceAvailability.Available)
            {
                invalidOperation = PSTraceSource.NewInvalidOperationException(
                        RunspaceStrings.ConcurrentInvokeNotAllowed);
            }
            else
            {
                bool success = RunspacePool.RemoteRunspacePoolInternal.ResetRunspaceState();
                if (!success)
                {
                    invalidOperation = PSTraceSource.NewInvalidOperationException();
                }
            }

            if (invalidOperation != null)
            {
                invalidOperation.Source = "ResetRunspaceState";
                throw invalidOperation;
            }
        }

        #endregion

        #region Disconnect-Connect

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
        internal static Runspace[] GetRemoteRunspaces(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            List<Runspace> runspaces = new List<Runspace>();
            RunspacePool[] runspacePools = RemoteRunspacePoolInternal.GetRemoteRunspacePools(connectionInfo, host, typeTable);

            // We don't yet know how many runspaces there are in these runspace pool objects.  This information isn't updated
            // until a Connect() is performed.  But we can use the ConnectCommands list to prune runspace pool objects that
            // clearly have more than one command/runspace.
            foreach (RunspacePool runspacePool in runspacePools)
            {
                if (runspacePool.RemoteRunspacePoolInternal.ConnectCommands.Length < 2)
                {
                    runspaces.Add(new RemoteRunspace(runspacePool));
                }
            }

            return runspaces.ToArray();
        }

        /// <summary>
        /// Creates a single disconnected remote Runspace object based on connection information and
        /// session / command identifiers.
        /// </summary>
        /// <param name="connectionInfo">Connection object for target machine.</param>
        /// <param name="sessionId">Session Id to connect to.</param>
        /// <param name="commandId">Optional command Id to connect to.</param>
        /// <param name="host">Optional PSHost.</param>
        /// <param name="typeTable">Optional TypeTable.</param>
        /// <returns>Disconnect remote Runspace object.</returns>
        internal static Runspace GetRemoteRunspace(RunspaceConnectionInfo connectionInfo, Guid sessionId, Guid? commandId, PSHost host, TypeTable typeTable)
        {
            RunspacePool runspacePool = RemoteRunspacePoolInternal.GetRemoteRunspacePool(
                connectionInfo,
                sessionId,
                commandId,
                host,
                typeTable);

            return new RemoteRunspace(runspacePool);
        }

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
        public override void Disconnect()
        {
            if (!CanDisconnect)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.DisconnectNotSupportedOnServer);
            }

            UpdatePoolDisconnectOptions();

            try
            {
                RunspacePool.Disconnect();
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

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
        public override void DisconnectAsync()
        {
            if (!CanDisconnect)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.DisconnectNotSupportedOnServer);
            }

            UpdatePoolDisconnectOptions();

            try
            {
                RunspacePool.BeginDisconnect(null, null);
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

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
        public override void Connect()
        {
            if (!CanConnect)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.CannotConnect);
            }

            UpdatePoolDisconnectOptions();

            try
            {
                RunspacePool.Connect();
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

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
        public override void ConnectAsync()
        {
            if (!CanConnect)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.CannotConnect);
            }

            UpdatePoolDisconnectOptions();

            try
            {
                RunspacePool.BeginConnect(null, null);
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        /// <summary>
        /// Creates a PipeLine object in the disconnected state for the currently disconnected
        /// remote running command associated with this runspace.
        /// </summary>
        /// <returns>Pipeline object in disconnected state.</returns>
        public override Pipeline CreateDisconnectedPipeline()
        {
            if (RemoteCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoDisconnectedCommand);
            }

            return new RemotePipeline(this);
        }

        /// <summary>
        /// Creates a PowerShell object in the disconnected state for the currently disconnected
        /// remote running command associated with this runspace.
        /// </summary>
        /// <returns>PowerShell object in disconnected state.</returns>
        public override PowerShell CreateDisconnectedPowerShell()
        {
            if (RemoteCommand == null)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoDisconnectedCommand);
            }

            return new PowerShell(RemoteCommand, this);
        }

        /// <summary>
        /// Returns Runspace capabilities.
        /// </summary>
        /// <returns>RunspaceCapability.</returns>
        public override RunspaceCapability GetCapabilities()
        {
            RunspaceCapability returnCaps = RunspaceCapability.Default;

            if (CanDisconnect)
            {
                returnCaps |= RunspaceCapability.SupportsDisconnect;
            }

            if (_connectionInfo is WSManConnectionInfo)
            {
                return returnCaps;
            }

            if (_connectionInfo is NamedPipeConnectionInfo)
            {
                returnCaps |= RunspaceCapability.NamedPipeTransport;
            }
            else if (_connectionInfo is VMConnectionInfo)
            {
                returnCaps |= RunspaceCapability.VMSocketTransport;
            }
            else if (_connectionInfo is SSHConnectionInfo)
            {
                returnCaps |= RunspaceCapability.SSHTransport;
            }
            else if (_connectionInfo is ContainerConnectionInfo containerConnectionInfo)
            {
                if ((containerConnectionInfo != null) &&
                    (containerConnectionInfo.ContainerProc.RuntimeId == Guid.Empty))
                {
                    returnCaps |= RunspaceCapability.NamedPipeTransport;
                }
            }
            else
            {
                // Unknown connection info type means a custom connection/transport, which at
                // minimum supports remote runspace capability starting from PowerShell v7.x.
                returnCaps |= RunspaceCapability.CustomTransport;
            }

            return returnCaps;
        }

        /// <summary>
        /// Update the pool disconnect options so that any changes will be
        /// passed to the server during the disconnect/connect operations.
        /// </summary>
        private void UpdatePoolDisconnectOptions()
        {
            WSManConnectionInfo runspaceWSManConnectionInfo = RunspacePool.ConnectionInfo as WSManConnectionInfo;
            WSManConnectionInfo wsManConnectionInfo = ConnectionInfo as WSManConnectionInfo;

            Dbg.Assert(runspaceWSManConnectionInfo != null, "Disconnect-Connect feature is currently only supported for WSMan transport");
            Dbg.Assert(wsManConnectionInfo != null, "Disconnect-Connect feature is currently only supported for WSMan transport");

            runspaceWSManConnectionInfo.IdleTimeout = wsManConnectionInfo.IdleTimeout;
            runspaceWSManConnectionInfo.OutputBufferingMode = wsManConnectionInfo.OutputBufferingMode;
        }

        #endregion

        #region Remote Debugging

        /// <summary>
        /// Remote DebuggerStop event.
        /// </summary>
        internal event EventHandler<PSEventArgs> RemoteDebuggerStop;

        /// <summary>
        /// Remote BreakpointUpdated event.
        /// </summary>
        internal event EventHandler<PSEventArgs> RemoteDebuggerBreakpointUpdated;

        #endregion

        #region CreatePipeline

        /// <summary>
        /// Create an empty pipeline.
        /// </summary>
        /// <returns>An empty pipeline.</returns>
        public override Pipeline CreatePipeline()
        {
            return CoreCreatePipeline(null, false, false);
        }

        /// <summary>
        /// Create a pipeline from a command string.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <returns>
        /// A pipeline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public override Pipeline CreatePipeline(string command)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            return CoreCreatePipeline(command, false, false);
        }

        /// <summary>
        /// Create a pipeline from a command string.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <param name="addToHistory">If true command is added to history.</param>
        /// <returns>
        /// A pipeline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public override Pipeline CreatePipeline(string command, bool addToHistory)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            return CoreCreatePipeline(command, addToHistory, false);
        }

        /// <summary>
        /// Creates a nested pipeline.
        /// </summary>
        /// <remarks>
        /// Nested pipelines are needed for nested prompt scenario. Nested
        /// prompt requires that we execute new pipelines( child pipelines)
        /// while current pipeline (lets call it parent pipeline) is blocked.
        /// </remarks>
        /// <exception cref="PSNotSupportedException">Not supported in remoting
        /// scenarios</exception>
        public override Pipeline CreateNestedPipeline()
        {
            return CoreCreatePipeline(null, false, true);
        }

        /// <summary>
        /// Creates a nested pipeline.
        /// </summary>
        /// <param name="command">A valid command string.</param>
        /// <param name="addToHistory">If true command is added to history.</param>
        /// <returns>
        /// A pipeline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="PSNotSupportedException">Not supported in remoting
        /// scenarios</exception>
        public override Pipeline CreateNestedPipeline(string command, bool addToHistory)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            return CoreCreatePipeline(command, addToHistory, true);
        }

        #endregion CreatePipeline

        #region Running Pipeline Management

        /// <summary>
        /// Add the pipeline to list of pipelines in execution.
        /// </summary>
        /// <param name="pipeline">Pipeline to add to the
        /// list of pipelines in execution</param>
        /// <exception cref="InvalidRunspaceStateException">
        /// Thrown if the runspace is not in the Opened state.
        /// <see cref="RunspaceState"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if
        /// <paramref name="pipeline"/> is null.
        /// </exception>
        internal void AddToRunningPipelineList(RemotePipeline pipeline)
        {
            Dbg.Assert(pipeline != null, "caller should validate the parameter");

            lock (_syncRoot)
            {
                if (!_bypassRunspaceStateCheck &&
                    _runspaceStateInfo.State != RunspaceState.Opened &&
                    _runspaceStateInfo.State != RunspaceState.Disconnected) // Disconnected runspaces can have running pipelines.
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipeline,
                                _runspaceStateInfo.State.ToString()
                            ),
                            _runspaceStateInfo.State,
                            RunspaceState.Opened
                        );
                    if (this.ConnectionInfo != null)
                    {
                        e.Source = this.ConnectionInfo.ComputerName;
                    }

                    throw e;
                }

                // Add the pipeline to list of Executing pipeline.
                // Note:_runningPipelines is always accessed with the lock so
                // there is no need to create a synchronized version of list
                _runningPipelines.Add(pipeline);
            }
        }

        /// <summary>
        /// Remove the pipeline from list of pipelines in execution.
        /// </summary>
        /// <param name="pipeline">Pipeline to remove from the
        /// list of pipelines in execution</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="pipeline"/> is null.
        /// </exception>
        internal void RemoveFromRunningPipelineList(RemotePipeline pipeline)
        {
            Dbg.Assert(pipeline != null, "caller should validate the parameter");

            lock (_syncRoot)
            {
                Dbg.Assert(_runspaceStateInfo.State != RunspaceState.BeforeOpen,
                             "Runspace should not be before open when pipeline is running");

                // Remove the pipeline to list of Executing pipeline.
                // Note:_runningPipelines is always accessed with the lock so
                // there is no need to create a synchronized version of list
                _runningPipelines.Remove(pipeline);
                pipeline.PipelineFinishedEvent.Set();
            }
        }

        /// <summary>
        /// Check to see, if there is any other pipeline running in this
        /// runspace. If not, then add this to the list of pipelines.
        /// </summary>
        /// <param name="pipeline">Pipeline to check and add.</param>
        /// <param name="syncCall">whether this is being called from
        /// a synchronous method call</param>
        internal void DoConcurrentCheckAndAddToRunningPipelines(RemotePipeline pipeline, bool syncCall)
        {
            // Concurrency check should be done under runspace lock
            lock (_syncRoot)
            {
                if (_bSessionStateProxyCallInProgress)
                {
                    throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoPipelineWhenSessionStateProxyInProgress);
                }

                // Delegate to pipeline to do check if it is fine to invoke if another
                // pipeline is running.
                pipeline.DoConcurrentCheck(syncCall);
                // Finally add to the list of running pipelines.
                AddToRunningPipelineList(pipeline);
            }
        }

        #endregion Running Pipeline Management

        #region SessionState Proxy

        /// <summary>
        /// Returns SessionState proxy object.
        /// </summary>
        /// <returns></returns>
        internal override SessionStateProxy GetSessionStateProxy()
        {
            return _sessionStateProxy ??= new RemoteSessionStateProxy(this);
        }

        private RemoteSessionStateProxy _sessionStateProxy = null;

        #endregion SessionState Proxy

        #region Private Methods

        private void HandleRunspacePoolStateChanged(object sender, RunspacePoolStateChangedEventArgs e)
        {
            RunspaceState newState = (RunspaceState)e.RunspacePoolStateInfo.State;

            RunspaceState prevState = SetRunspaceState(newState, e.RunspacePoolStateInfo.Reason);

            switch (newState)
            {
                case RunspaceState.Opened:
                    switch (prevState)
                    {
                        case RunspaceState.Opening:
                            // For newly opened remote runspaces, set the debug mode based on the
                            // associated host.  This involves running a remote command and is Ok
                            // since this event is called on a worker thread and not a WinRM callback.
                            SetDebugModeOnOpen();
                            break;

                        case RunspaceState.Connecting:
                            UpdateDisconnectExpiresOn();

                            // Application private data containing server debug state is updated on
                            // a *reconstruct* connect operation when _applicationPrivateData is null.
                            // Pass new information to the debugger.
                            if (_applicationPrivateData == null)
                            {
                                _applicationPrivateData = GetApplicationPrivateData();
                                SetDebugInfo(_applicationPrivateData);
                            }

                            break;
                    }

                    break;

                case RunspaceState.Disconnected:
                    UpdateDisconnectExpiresOn();
                    break;
            }

            RaiseRunspaceStateEvents();
        }

        /// <summary>
        /// Set debug mode on remote session based on the interactive host
        /// setting, if available.
        /// </summary>
        private void SetDebugModeOnOpen()
        {
            // Update client remote debugger based on server capabilities.
            _applicationPrivateData = GetApplicationPrivateData();
            bool serverSupportsDebugging = SetDebugInfo(_applicationPrivateData);
            if (!serverSupportsDebugging)
            { return; }

            // Set server side initial debug mode based on interactive host.
            DebugModes hostDebugMode = DebugModes.Default;
            try
            {
                IHostSupportsInteractiveSession interactiveHost =
                    RunspacePool.RemoteRunspacePoolInternal.Host as IHostSupportsInteractiveSession;
                if (interactiveHost != null &&
                    interactiveHost.Runspace != null &&
                    interactiveHost.Runspace.Debugger != null)
                {
                    hostDebugMode = interactiveHost.Runspace.Debugger.DebugMode;
                }
            }
            catch (PSNotImplementedException) { }

            if ((hostDebugMode & DebugModes.RemoteScript) == DebugModes.RemoteScript)
            {
                try
                {
                    _remoteDebugger.SetDebugMode(hostDebugMode);
                }
                catch (Exception)
                {
                }
            }
        }

        private bool SetDebugInfo(PSPrimitiveDictionary psApplicationPrivateData)
        {
            DebugModes? debugMode = null;
            bool inDebugger = false;
            int breakpointCount = 0;
            bool breakAll = false;
            UnhandledBreakpointProcessingMode unhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;

            if (psApplicationPrivateData != null)
            {
                if (psApplicationPrivateData.ContainsKey(RemoteDebugger.DebugModeSetting))
                {
                    debugMode = (DebugModes)(int)psApplicationPrivateData[RemoteDebugger.DebugModeSetting];
                }

                if (psApplicationPrivateData.ContainsKey(RemoteDebugger.DebugStopState))
                {
                    inDebugger = (bool)psApplicationPrivateData[RemoteDebugger.DebugStopState];
                }

                if (psApplicationPrivateData.ContainsKey(RemoteDebugger.DebugBreakpointCount))
                {
                    breakpointCount = (int)psApplicationPrivateData[RemoteDebugger.DebugBreakpointCount];
                }

                if (psApplicationPrivateData.ContainsKey(RemoteDebugger.BreakAllSetting))
                {
                    breakAll = (bool)psApplicationPrivateData[RemoteDebugger.BreakAllSetting];
                }

                if (psApplicationPrivateData.ContainsKey(RemoteDebugger.UnhandledBreakpointModeSetting))
                {
                    unhandledBreakpointMode = (UnhandledBreakpointProcessingMode)(int)psApplicationPrivateData[RemoteDebugger.UnhandledBreakpointModeSetting];
                }

                if (psApplicationPrivateData.ContainsKey(PSVersionInfo.PSVersionTableName))
                {
                    var psVersionTable = psApplicationPrivateData[PSVersionInfo.PSVersionTableName] as PSPrimitiveDictionary;
                    if (psVersionTable.ContainsKey(PSVersionInfo.PSVersionName))
                    {
                        ServerVersion = PSObject.Base(psVersionTable[PSVersionInfo.PSVersionName]) as Version;
                    }
                }
            }

            if (debugMode != null)
            {
                // Server supports remote debugging.  Create Debugger object for
                // this remote runspace.
                Dbg.Assert(_remoteDebugger == null, "Remote runspace should not have a debugger yet.");
                _remoteDebugger = new RemoteDebugger(this);

                // Set initial debugger state.
                _remoteDebugger.SetClientDebugInfo(debugMode, inDebugger, breakpointCount, breakAll, unhandledBreakpointMode, ServerVersion);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Asserts if the current state of the runspace is BeforeOpen.
        /// </summary>
        private void AssertIfStateIsBeforeOpen()
        {
            lock (_syncRoot)
            {
                // Call fails if RunspaceState is not BeforeOpen.
                if (_runspaceStateInfo.State != RunspaceState.BeforeOpen)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.CannotOpenAgain,
                                new object[] { _runspaceStateInfo.State.ToString() }
                            ),
                            _runspaceStateInfo.State,
                            RunspaceState.BeforeOpen
                        );
                    throw e;
                }
            }
        }

        /// <summary>
        /// Set the new runspace state.
        /// </summary>
        /// <param name="state">The new state.</param>
        /// <param name="reason">An exception indicating the state change is the
        /// result of an error, otherwise; null.
        /// </param>
        /// <returns>Previous runspace state.</returns>
        /// <remarks>
        /// Sets the internal runspace state information member variable. It also
        /// adds RunspaceStateInfo to a queue.
        /// RaiseRunspaceStateEvents raises event for each item in this queue.
        /// </remarks>
        private RunspaceState SetRunspaceState(RunspaceState state, Exception reason)
        {
            RunspaceState prevState;

            lock (_syncRoot)
            {
                prevState = _runspaceStateInfo.State;
                if (state != prevState)
                {
                    _runspaceStateInfo = new RunspaceStateInfo(state, reason);

                    // Add _runspaceStateInfo to _runspaceEventQueue.
                    // RaiseRunspaceStateEvents will raise event for each item
                    // in this queue.
                    // Note:We are doing clone here instead of passing the member
                    // _runspaceStateInfo because we donot want outside
                    // to change our runspace state.
                    RunspaceAvailability previousAvailability = _runspaceAvailability;

                    this.UpdateRunspaceAvailability(_runspaceStateInfo.State, false);

                    _runspaceEventQueue.Enqueue(
                        new RunspaceEventQueueItem(
                            _runspaceStateInfo.Clone(),
                            previousAvailability,
                            _runspaceAvailability));

                    PSEtwLog.LogOperationalVerbose(PSEventId.RunspaceStateChange, PSOpcode.Open,
                                PSTask.CreateRunspace, PSKeyword.UseAlwaysOperational,
                                state.ToString());
                }
            }

            return prevState;
        }

        /// <summary>
        /// Raises events for changes in runspace state.
        /// </summary>
        private void RaiseRunspaceStateEvents()
        {
            Queue<RunspaceEventQueueItem> tempEventQueue = null;
            EventHandler<RunspaceStateEventArgs> stateChanged = null;
            bool hasAvailabilityChangedSubscribers = false;

            lock (_syncRoot)
            {
                stateChanged = this.StateChanged;
                hasAvailabilityChangedSubscribers = this.HasAvailabilityChangedSubscribers;

                if (stateChanged != null || hasAvailabilityChangedSubscribers)
                {
                    tempEventQueue = _runspaceEventQueue;
                    _runspaceEventQueue = new Queue<RunspaceEventQueueItem>();
                }
                else
                {
                    // Clear the events if there are no EventHandlers. This
                    // ensures that events do not get called for state
                    // changes prior to their registration.
                    _runspaceEventQueue.Clear();
                }
            }

            if (tempEventQueue != null)
            {
                while (tempEventQueue.Count > 0)
                {
                    RunspaceEventQueueItem queueItem = tempEventQueue.Dequeue();

                    if (hasAvailabilityChangedSubscribers && queueItem.NewRunspaceAvailability != queueItem.CurrentRunspaceAvailability)
                    {
                        this.OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(queueItem.NewRunspaceAvailability));
                    }

                    // Exception raised by events are not error condition for runspace
                    // object.
                    if (stateChanged != null)
                    {
                        try
                        {
                            stateChanged(this, new RunspaceStateEventArgs(queueItem.RunspaceStateInfo));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a pipeline.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="addToHistory"></param>
        /// <param name="isNested"></param>
        /// <returns></returns>
        private Pipeline CoreCreatePipeline(string command, bool addToHistory, bool isNested)
        {
            return new RemotePipeline(this, command, addToHistory, isNested);
        }

        /// <summary>
        /// Waits till all the pipelines running in the runspace have
        /// finished execution.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Finishof")]
        private bool WaitForFinishofPipelines()
        {
            // Take a snapshot of list of active pipelines.
            // Note:Before we enter to this CloseHelper routine
            // CoreClose has already set the state of Runspace
            // to closing. So no new pipelines can be executed on this
            // runspace and so no new pipelines will be added to
            // _runningPipelines. However we still need to lock because
            // running pipelines can be removed from this.
            RemotePipeline[] runningPipelines;

            lock (_syncRoot)
            {
                runningPipelines = _runningPipelines.ToArray();
            }

            if (runningPipelines.Length > 0)
            {
                WaitHandle[] waitHandles = new WaitHandle[runningPipelines.Length];

                for (int i = 0; i < runningPipelines.Length; i++)
                {
                    waitHandles[i] = runningPipelines[i].PipelineFinishedEvent;
                }

                return WaitHandle.WaitAll(waitHandles);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the currently executing pipeline.
        /// </summary>
        /// <remarks>Internal because it is needed by invoke-history</remarks>
        internal override Pipeline GetCurrentlyRunningPipeline()
        {
            lock (_syncRoot)
            {
                if (_runningPipelines.Count != 0)
                {
                    return (Pipeline)_runningPipelines[_runningPipelines.Count - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Handles any host calls received from the server.
        /// </summary>
        /// <param name="sender">Sender of this information, unused.</param>
        /// <param name="eventArgs">arguments describing this event, contains
        /// a RemoteHostCall object</param>
        private void HandleHostCallReceived(object sender, RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            ClientMethodExecutor.Dispatch(
                RunspacePool.RemoteRunspacePoolInternal.DataStructureHandler.TransportManager,
                RunspacePool.RemoteRunspacePoolInternal.Host,
                null,       /* error stream */
                null,       /* method executor stream */
                false,      /* is method stream enabled */
                RunspacePool.RemoteRunspacePoolInternal,
                Guid.Empty, /* powershell id */
                eventArgs.Data);
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
                // change the runspace's uri to the new URI.
                wsmanConnectionInfo.ConnectionUri = eventArgs.Data;
                URIRedirectionReported.SafeInvoke(this, eventArgs);
            }
        }

        /// <summary>
        /// Forward the events from the runspace pool to the current instance.
        /// </summary>
        private void HandleRunspacePoolForwardEvent(object sender, PSEventArgs e)
        {
            if (e.SourceIdentifier.Equals(RemoteDebugger.RemoteDebuggerStopEvent))
            {
                // Special processing for forwarded remote DebuggerStop event.
                RemoteDebuggerStop.SafeInvoke(this, e);
            }
            else if (e.SourceIdentifier.Equals(RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent))
            {
                // Special processing for forwarded remote DebuggerBreakpointUpdated event.
                RemoteDebuggerBreakpointUpdated.SafeInvoke(this, e);
            }
            else
            {
                _eventManager.AddForwardedEvent(e);
            }
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
        }

        /// <summary>
        /// Updates runspace DisconnectedOn/ExpiresOn based on RS Pool connectionInfo.
        /// </summary>
        private void UpdateDisconnectExpiresOn()
        {
            WSManConnectionInfo wsmanConnectionInfo = RunspacePool.RemoteRunspacePoolInternal.ConnectionInfo as WSManConnectionInfo;
            if (wsmanConnectionInfo != null)
            {
                this.DisconnectedOn = wsmanConnectionInfo.DisconnectedOn;
                this.ExpiresOn = wsmanConnectionInfo.ExpiresOn;
            }
        }

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// Determines if another Invoke-Command is executing
        /// in this runspace in the currently running local pipeline
        /// ahead on the specified invoke-command.
        /// </summary>
        /// <param name="invokeCommand">current invoke-command
        /// instance</param>
        /// <param name="localPipelineId">Local pipeline id.</param>
        /// <returns>True, if another invoke-command is running
        /// before, false otherwise.</returns>
        internal bool IsAnotherInvokeCommandExecuting(InvokeCommandCommand invokeCommand,
            long localPipelineId)
        {
            // the invoke-command's pipeline should be the currently
            // running pipeline. This will ensure that, we do not
            // return true, when one invoke-command is running as a
            // job and another invoke-command is entered at the
            // console prompt
            if (_currentLocalPipelineId != localPipelineId && _currentLocalPipelineId != 0)
            {
                return false;
            }
            else
            {
                // the local pipeline ids are the same
                // this invoke command is running may be
                // running in the same pipeline as another
                // invoke command
                if (_currentInvokeCommand == null)
                {
                    // this is the first invoke-command, just
                    // set the reference
                    SetCurrentInvokeCommand(invokeCommand, localPipelineId);
                    return false;
                }
                else if (_currentInvokeCommand.Equals(invokeCommand))
                {
                    // the currently active invoke command is the one
                    // specified
                    return false;
                }
                else
                {
                    // the local pipeline id is the same and there
                    // is another invoke command that is active already
                    return true;
                }
            }
        }

        /// <summary>
        /// Keeps track of the current invoke command executing
        /// within the current local pipeline.
        /// </summary>
        /// <param name="invokeCommand">reference to invoke command
        /// which is currently being processed</param>
        /// <param name="localPipelineId">The local pipeline id.</param>
        internal void SetCurrentInvokeCommand(InvokeCommandCommand invokeCommand,
            long localPipelineId)
        {
            Dbg.Assert(invokeCommand != null, "InvokeCommand instance cannot be null, use ClearInvokeCommand() method to reset current command");
            Dbg.Assert(localPipelineId != 0, "Local pipeline id needs to be supplied - cannot be 0");

            _currentInvokeCommand = invokeCommand;
            _currentLocalPipelineId = localPipelineId;
        }

        /// <summary>
        /// Clears the current invoke-command reference stored within
        /// this remote runspace.
        /// </summary>
        internal void ClearInvokeCommand()
        {
            _currentLocalPipelineId = 0;
            _currentInvokeCommand = null;
        }

        /// <summary>
        /// Aborts any current Opening process.  If runspace is not opening then this has no effect.
        /// This is currently *only* for named pipe connections where a connection
        /// to a process is limited to a single client.
        /// </summary>
        internal void AbortOpen()
        {
            System.Management.Automation.Remoting.Client.NamedPipeClientSessionTransportManager transportManager =
                RunspacePool.RemoteRunspacePoolInternal.DataStructureHandler.TransportManager as System.Management.Automation.Remoting.Client.NamedPipeClientSessionTransportManager;

            transportManager?.AbortConnect();
        }

        #endregion Internal Methods

        #region Misc Properties / Events

        /// <summary>
        /// The runspace pool that this remote runspace wraps.
        /// </summary>
        internal RunspacePool RunspacePool { get; }

        /// <summary>
        /// EventHandler used to report connection URI redirections to the application.
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs<Uri>> URIRedirectionReported;

        #endregion Misc Properties

        #region Application private data

        /// <summary>
        /// Private data to be used by applications built on top of PowerShell.
        ///
        /// Remote runspace gets its application private data from the server (when creating the remote runspace pool)
        /// - calling this method on a remote runspace will block until the data is received from the server.
        ///
        /// Runspaces that are part of a <see cref="RunspacePool"/> inherit application private data from the pool.
        /// </summary>
        public override PSPrimitiveDictionary GetApplicationPrivateData()
        {
            try
            {
                return RunspacePool.GetApplicationPrivateData();
            }
            catch (InvalidRunspacePoolStateException e)
            {
                throw e.ToInvalidRunspaceStateException();
            }
        }

        internal override void SetApplicationPrivateData(PSPrimitiveDictionary applicationPrivateData)
        {
            Dbg.Assert(false, "RemoteRunspace.SetApplicationPrivateData shouldn't be called - this runspace does not belong to a runspace pool [although it does use a remote runspace pool internally]");
        }

        #endregion
    }

    #region Remote Debugger

    /// <summary>
    /// RemoteDebugger.
    /// </summary>
    internal sealed class RemoteDebugger : Debugger, IDisposable
    {
        #region Members

        private readonly RemoteRunspace _runspace;
        private PowerShell _psDebuggerCommand;
        private bool _remoteDebugSupported;
        private bool _isActive;
        private int _breakpointCount;
        private RemoteDebuggingCapability _remoteDebuggingCapability;
        private bool? _remoteBreakpointManagementIsSupported;
        private volatile bool _handleDebuggerStop;
        private bool _isDebuggerSteppingEnabled;
        private UnhandledBreakpointProcessingMode _unhandledBreakpointMode;
        private bool _detachCommand;

#if !UNIX
        // Windows impersonation flow
        private WindowsIdentity _identityToPersonate;
        private bool _identityPersonationChecked;
#endif

        /// <summary>
        /// RemoteDebuggerStopEvent.
        /// </summary>
        public const string RemoteDebuggerStopEvent = "PSInternalRemoteDebuggerStopEvent";

        /// <summary>
        /// RemoteDebuggerBreakpointUpdatedEvent.
        /// </summary>
        public const string RemoteDebuggerBreakpointUpdatedEvent = "PSInternalRemoteDebuggerBreakpointUpdatedEvent";

        // Remote debugger settings
        public const string DebugModeSetting = "DebugMode";
        public const string DebugStopState = "DebugStop";
        public const string DebugBreakpointCount = "DebugBreakpointCount";
        public const string BreakAllSetting = "BreakAll";
        public const string UnhandledBreakpointModeSetting = "UnhandledBreakpointMode";

        #endregion

        #region Constructor

        private RemoteDebugger() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runspace">Associated remote runspace.</param>
        public RemoteDebugger(RemoteRunspace runspace)
        {
            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            _runspace = runspace;

            _unhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;

            // Hook up remote debugger forwarded event handlers.
            _runspace.RemoteDebuggerStop += HandleForwardedDebuggerStopEvent;
            _runspace.RemoteDebuggerBreakpointUpdated += HandleForwardedDebuggerBreakpointUpdatedEvent;
        }

        #endregion

        #region Class overrides

        /// <summary>
        /// Process debugger command.
        /// </summary>
        /// <param name="command">Debugger PSCommand.</param>
        /// <param name="output">Output.</param>
        /// <returns>DebuggerCommandResults.</returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            CheckForValidateState();
            _detachCommand = false;

            if (command == null)
            {
                throw new PSArgumentNullException(nameof(command));
            }

            if (output == null)
            {
                throw new PSArgumentNullException(nameof(output));
            }

            if (!DebuggerStopped)
            {
                throw new PSInvalidOperationException(
                    DebuggerStrings.CannotProcessDebuggerCommandNotStopped,
                    null,
                    Debugger.CannotProcessCommandNotStopped,
                    ErrorCategory.InvalidOperation,
                    null);
            }

            DebuggerCommandResults results = null;

            // Execute command on server.
            bool executionError = false;
            using (_psDebuggerCommand = GetNestedPowerShell())
            {
                foreach (var cmd in command.Commands)
                {
                    cmd.MergeMyResults(PipelineResultTypes.All, PipelineResultTypes.Output);
                    _psDebuggerCommand.AddCommand(cmd);
                }

                PSDataCollection<PSObject> internalOutput = new PSDataCollection<PSObject>();
                internalOutput.DataAdded += (sender, args) =>
                    {
                        foreach (var item in internalOutput.ReadAll())
                        {
                            if (item == null) { return; }

                            DebuggerCommand dbgCmd = item.BaseObject as DebuggerCommand;
                            if (dbgCmd != null)
                            {
                                bool executedByDebugger = (dbgCmd.ResumeAction != null || dbgCmd.ExecutedByDebugger);
                                results = new DebuggerCommandResults(dbgCmd.ResumeAction, executedByDebugger);
                            }
                            else if (item.BaseObject is DebuggerCommandResults)
                            {
                                results = item.BaseObject as DebuggerCommandResults;
                            }
                            else
                            {
                                output.Add(item);
                            }
                        }
                    };

                try
                {
                    _psDebuggerCommand.Invoke(null, internalOutput, null);
                }
                catch (Exception e)
                {
                    executionError = true;
                    RemoteException re = e as RemoteException;
                    if ((re != null) && (re.ErrorRecord != null))
                    {
                        // Allow the IncompleteParseException to throw so that the console
                        // can handle here strings and continued parsing.
                        if (re.ErrorRecord.CategoryInfo.Reason == nameof(IncompleteParseException))
                        {
                            throw new IncompleteParseException(
                                re.ErrorRecord.Exception?.Message,
                                re.ErrorRecord.FullyQualifiedErrorId);
                        }

                        // Allow the RemoteException and InvalidRunspacePoolStateException to propagate so that the host can
                        // clean up the debug session.
                        if ((re.ErrorRecord.CategoryInfo.Reason == nameof(InvalidRunspacePoolStateException)) ||
                            (re.ErrorRecord.CategoryInfo.Reason == nameof(RemoteException)))
                        {
                            throw new PSRemotingTransportException(
                                (re.ErrorRecord.Exception != null) ? re.ErrorRecord.Exception.Message : string.Empty);
                        }
                    }

                    // Allow all PSRemotingTransportException and RemoteException errors to propagate as this
                    // indicates a broken debug session.
                    if ((e is PSRemotingTransportException) || (e is RemoteException))
                    {
                        throw;
                    }

                    output.Add(
                        new PSObject(
                            new ErrorRecord(
                            e,
                            "DebuggerError",
                            ErrorCategory.InvalidOperation,
                            null)));
                }
            }

            executionError = executionError || _psDebuggerCommand.HadErrors;
            _psDebuggerCommand = null;

            // Special processing when the detach command is run.
            _detachCommand = (!executionError) && (command.Commands.Count > 0) && (command.Commands[0].CommandText.Equals("Detach", StringComparison.OrdinalIgnoreCase));

            return results ?? new DebuggerCommandResults(null, false);
        }

        /// <summary>
        /// StopProcessCommand.
        /// </summary>
        public override void StopProcessCommand()
        {
            CheckForValidateState();

            PowerShell ps = _psDebuggerCommand;
            if ((ps != null) &&
                (ps.InvocationStateInfo.State == PSInvocationState.Running))
            {
                ps.BeginStop(null, null);
            }
        }

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">Breakpoints to set.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.SetBreakpoint);

            var functionParameters = new Dictionary<string, object>
            {
                { "BreakpointList", breakpoints },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            InvokeRemoteBreakpointFunction<CommandBreakpoint>(RemoteDebuggingCommands.SetBreakpoint, functionParameters);
        }

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The breakpoint with the specified id.</returns>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.GetBreakpoint);

            var functionParameters = new Dictionary<string, object>
            {
                { "Id", id },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<Breakpoint>(RemoteDebuggingCommands.GetBreakpoint, functionParameters);
        }

        /// <summary>
        /// Returns breakpoints primarily for the Get-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A list of breakpoints in a runspace.</returns>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.GetBreakpoint);

            CheckForValidateState();

            var breakpoints = new List<Breakpoint>();

            using (PowerShell ps = GetNestedPowerShell())
            {
                ps.AddCommand(RemoteDebuggingCommands.GetBreakpoint);

                if (runspaceId.HasValue)
                {
                    ps.AddParameter("RunspaceId", runspaceId.Value);
                }

                Collection<PSObject> output = ps.Invoke<PSObject>();
                foreach (var item in output)
                {
                    if (item?.BaseObject is Breakpoint bp)
                    {
                        breakpoints.Add(bp);
                    }
                    else if (TryGetRemoteDebuggerException(item, out Exception ex))
                    {
                        throw ex;
                    }
                }
            }

            return breakpoints;
        }

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.SetBreakpoint);

            Breakpoint breakpoint = new CommandBreakpoint(path, null, command, action);
            var functionParameters = new Dictionary<string, object>
            {
                { "Breakpoint", breakpoint },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<CommandBreakpoint>(RemoteDebuggingCommands.SetBreakpoint, functionParameters);
        }

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.SetBreakpoint);

            Breakpoint breakpoint = new LineBreakpoint(path, line, column, action);

            var functionParameters = new Dictionary<string, object>
            {
                { "Breakpoint", breakpoint },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<LineBreakpoint>(RemoteDebuggingCommands.SetBreakpoint, functionParameters);
        }

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.SetBreakpoint);

            Breakpoint breakpoint = new VariableBreakpoint(path, variableName, accessMode, action);

            var functionParameters = new Dictionary<string, object>
            {
                { "Breakpoint", breakpoint },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<VariableBreakpoint>(RemoteDebuggingCommands.SetBreakpoint, functionParameters);
        }

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.RemoveBreakpoint);

            if (breakpoint == null)
            {
                return false;
            }

            var functionParameters = new Dictionary<string, object>
            {
                { "Id", breakpoint.Id },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<bool>(RemoteDebuggingCommands.RemoveBreakpoint, functionParameters);
        }

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.EnableBreakpoint);

            if (breakpoint == null)
            {
                return null;
            }

            var functionParameters = new Dictionary<string, object>
            {
                { "Id", breakpoint.Id },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<Breakpoint>(RemoteDebuggingCommands.EnableBreakpoint, functionParameters);
        }

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            // This is supported only for PowerShell versions >= 7.0
            CheckRemoteBreakpointManagementSupport(RemoteDebuggingCommands.DisableBreakpoint);

            if (breakpoint == null)
            {
                return null;
            }

            var functionParameters = new Dictionary<string, object>
            {
                { "Id", breakpoint.Id },
            };

            if (runspaceId.HasValue)
            {
                functionParameters.Add("RunspaceId", runspaceId.Value);
            }

            return InvokeRemoteBreakpointFunction<Breakpoint>(RemoteDebuggingCommands.DisableBreakpoint, functionParameters);
        }

        /// <summary>
        /// SetDebuggerAction.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            CheckForValidateState();

            SetRemoteDebug(false, RunspaceAvailability.Busy);

            using (PowerShell ps = GetNestedPowerShell())
            {
                ps.AddCommand(RemoteDebuggingCommands.SetDebuggerAction).AddParameter("ResumeAction", resumeAction);
                ps.Invoke();

                // If an error exception is returned then throw it here.
                if (ps.ErrorBuffer.Count > 0)
                {
                    Exception e = ps.ErrorBuffer[0].Exception;
                    if (e != null) { throw e; }
                }
            }
        }

        /// <summary>
        /// GetDebuggerStopped.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            CheckForValidateState();

            DebuggerStopEventArgs rtnArgs = null;

            try
            {
                using (PowerShell ps = GetNestedPowerShell())
                {
                    ps.AddCommand(RemoteDebuggingCommands.GetDebuggerStopArgs);
                    Collection<PSObject> output = ps.Invoke<PSObject>();
                    foreach (var item in output)
                    {
                        if (item == null) { continue; }

                        rtnArgs = item.BaseObject as DebuggerStopEventArgs;
                        if (rtnArgs != null) { break; }
                    }
                }
            }
            catch (Exception)
            {
            }

            return rtnArgs;
        }

        /// <summary>
        /// SetDebugMode.
        /// </summary>
        /// <param name="mode"></param>
        public override void SetDebugMode(DebugModes mode)
        {
            CheckForValidateState();

            // Only set debug mode on server if no commands are currently
            // running on remote runspace.
            if ((_runspace.GetCurrentlyRunningPipeline() != null) ||
                (_runspace.RemoteCommand != null))
            {
                return;
            }

            using (PowerShell ps = GetNestedPowerShell())
            {
                ps.SetIsNested(false);
                ps.AddCommand(RemoteDebuggingCommands.SetDebugMode).AddParameter("Mode", mode);
                ps.Invoke();
            }

            base.SetDebugMode(mode);

            SetIsActive(_breakpointCount);
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            CheckForValidateState();

            // This is supported only for PowerShell versions >= 5.0
            if (!_remoteDebuggingCapability.IsCommandSupported(RemoteDebuggingCommands.SetDebuggerStepMode))
            {
                return;
            }

            try
            {
                // Ensure debugger is in correct mode.
                base.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);

                // Send Enable-DebuggerStepping virtual command.
                using (PowerShell ps = GetNestedPowerShell())
                {
                    ps.AddCommand(RemoteDebuggingCommands.SetDebuggerStepMode).AddParameter("Enabled", enabled);
                    ps.Invoke();
                    _isDebuggerSteppingEnabled = enabled;
                }
            }
            catch (Exception)
            {
                // Don't propagate exceptions.
            }
        }

        /// <summary>
        /// True when debugger is active with breakpoints.
        /// </summary>
        public override bool IsActive
        {
            get { return _isActive; }
        }

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get
            {
                return _handleDebuggerStop || (_runspace.RunspaceAvailability == RunspaceAvailability.RemoteDebug);
            }
        }

        /// <summary>
        /// InternalProcessCommand.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        internal override DebuggerCommand InternalProcessCommand(string command, IList<PSObject> output)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// IsRemote.
        /// </summary>
        internal override bool IsRemote
        {
            get { return true; }
        }

        /// <summary>
        /// Sets how the debugger deals with breakpoint events that are not handled.
        ///  Ignore - This is the default behavior and ignores any breakpoint event
        ///           if there is no handler.  Releases any preserved event.
        ///  Wait   - This mode preserves a breakpoint event until a handler is
        ///           subscribed.
        /// </summary>
        internal override UnhandledBreakpointProcessingMode UnhandledBreakpointMode
        {
            get
            {
                return _unhandledBreakpointMode;
            }

            set
            {
                CheckForValidateState();

                // This is supported only for PowerShell versions >= 5.0
                if (!_remoteDebuggingCapability.IsCommandSupported(RemoteDebuggingCommands.SetUnhandledBreakpointMode))
                {
                    return;
                }

                SetRemoteDebug(false, (RunspaceAvailability?)null);

                // Send Set-PSUnhandledBreakpointMode virtual command.
                using (PowerShell ps = GetNestedPowerShell())
                {
                    ps.AddCommand(RemoteDebuggingCommands.SetUnhandledBreakpointMode).AddParameter("UnhandledBreakpointMode", value);
                    ps.Invoke();
                }

                _unhandledBreakpointMode = value;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            _runspace.RemoteDebuggerStop -= HandleForwardedDebuggerStopEvent;
            _runspace.RemoteDebuggerBreakpointUpdated -= HandleForwardedDebuggerBreakpointUpdatedEvent;

#if !UNIX
            if (_identityToPersonate != null)
            {
                _identityToPersonate.Dispose();
                _identityToPersonate = null;
            }
#endif
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Internal method that checks the debug state of
        /// the remote session and raises the DebuggerStop event
        /// if debugger is in stopped state.
        /// This is used internally to help clients get back to
        /// debug state when reconnecting to remote session in debug state.
        /// </summary>
        internal void CheckStateAndRaiseStopEvent()
        {
            DebuggerStopEventArgs stopArgs = GetDebuggerStopArgs();
            if (stopArgs != null)
            {
                ProcessDebuggerStopEvent(stopArgs);
            }
        }

        /// <summary>
        /// IsRemoteDebug.
        /// </summary>
        internal bool IsRemoteDebug
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets client debug info state based on server info.
        /// </summary>
        /// <param name="debugMode">Debug mode.</param>
        /// <param name="inBreakpoint">Currently in breakpoint.</param>
        /// <param name="breakpointCount">Breakpoint count.</param>
        /// <param name="breakAll">Break All setting.</param>
        /// <param name="unhandledBreakpointMode">UnhandledBreakpointMode.</param>
        /// <param name="serverPSVersion">Server PowerShell version.</param>
        internal void SetClientDebugInfo(
            DebugModes? debugMode,
            bool inBreakpoint,
            int breakpointCount,
            bool breakAll,
            UnhandledBreakpointProcessingMode unhandledBreakpointMode,
            Version serverPSVersion)
        {
            if (debugMode != null)
            {
                _remoteDebugSupported = true;
                DebugMode = debugMode.Value;
            }
            else
            {
                _remoteDebugSupported = false;
            }

            if (inBreakpoint)
            {
                SetRemoteDebug(true, RunspaceAvailability.RemoteDebug);
            }

            _remoteDebuggingCapability = RemoteDebuggingCapability.CreateDebuggingCapability(serverPSVersion);

            _breakpointCount = breakpointCount;
            _isDebuggerSteppingEnabled = breakAll;
            _unhandledBreakpointMode = unhandledBreakpointMode;
            SetIsActive(breakpointCount);
        }

        /// <summary>
        /// If a command is stopped while in debug stopped state and it
        /// is the only command running then server is no longer debug stopped.
        /// </summary>
        internal void OnCommandStopped()
        {
            if (IsRemoteDebug)
            {
                IsRemoteDebug = false;
            }
        }

        /// <summary>
        /// Gets breakpoint information from the target machine and passes that information
        /// on through the BreakpointUpdated event.
        /// </summary>
        internal void SendBreakpointUpdatedEvents()
        {
            if (!IsDebuggerBreakpointUpdatedEventSubscribed() ||
                (_breakpointCount == 0))
            {
                return;
            }

            PSDataCollection<PSObject> breakpoints = new PSDataCollection<PSObject>();

            // Get breakpoint information by running "Get-PSBreakpoint" PowerShell command.
            using (PowerShell ps = GetNestedPowerShell())
            {
                if (!this.InBreakpoint)
                {
                    // Can't use nested PowerShell if we are not stopped in a breakpoint.
                    ps.SetIsNested(false);
                }

                ps.AddCommand("Get-PSBreakpoint");
                ps.Invoke(null, breakpoints);
            }

            // Raise BreakpointUpdated event to client for each breakpoint.
            foreach (PSObject obj in breakpoints)
            {
                Breakpoint breakpoint = obj.BaseObject as Breakpoint;
                if (breakpoint != null)
                {
                    RaiseBreakpointUpdatedEvent(
                        new BreakpointUpdatedEventArgs(breakpoint, BreakpointUpdateType.Set, _breakpointCount));
                }
            }
        }

        /// <summary>
        /// IsDebuggerSteppingEnabled.
        /// </summary>
        internal override bool IsDebuggerSteppingEnabled
        {
            get { return _isDebuggerSteppingEnabled; }
        }

        #endregion

        #region Private Methods

        private static bool TryGetRemoteDebuggerException(
            PSObject item,
            out Exception exception)
        {
            exception = null;
            if (item == null)
            {
                return false;
            }

            bool haveExceptionType = false;
            foreach (var typeName in item.TypeNames)
            {
                if (typeName.Equals("Deserialized.System.Exception"))
                {
                    haveExceptionType = true;
                    break;
                }
            }

            if (haveExceptionType)
            {
                var errorMessage = item.Properties["Message"]?.Value ?? string.Empty;
                exception = new RemoteException(
                    StringUtil.Format(
                        RemotingErrorIdStrings.RemoteDebuggerError, item.TypeNames[0], errorMessage));
                return true;
            }

            return false;
        }

        //
        // Event handlers
        //

        private void HandleForwardedDebuggerStopEvent(object sender, PSEventArgs e)
        {
            Dbg.Assert(e.SourceArgs.Length == 1, "Forwarded debugger stop event args must always contain one SourceArgs item.");
            DebuggerStopEventArgs args;
            if (e.SourceArgs[0] is PSObject)
            {
                args = ((PSObject)e.SourceArgs[0]).BaseObject as DebuggerStopEventArgs;
            }
            else
            {
                args = e.SourceArgs[0] as DebuggerStopEventArgs;
            }

            ProcessDebuggerStopEvent(args);
        }

        private void ProcessDebuggerStopEvent(DebuggerStopEventArgs args)
        {
            // It is possible to get a stop event raise request while
            // debugger is already in stop mode (after remote runspace debugger
            // reconnect).  In this case ignore the request.
            if (_handleDebuggerStop) { return; }

            // Attempt to process debugger stop event on original thread if it
            // is available (i.e., if it is blocked by EndInvoke).
            PowerShell powershell = _runspace.RunspacePool.RemoteRunspacePoolInternal.GetCurrentRunningPowerShell();
            AsyncResult invokeAsyncResult = powershell?.EndInvokeAsyncResult;

            bool invokedOnBlockedThread = false;
            if ((invokeAsyncResult != null) && (!invokeAsyncResult.IsCompleted))
            {
                invokedOnBlockedThread = invokeAsyncResult.InvokeCallbackOnThread(
                    ProcessDebuggerStopEventProc,
                    args);
            }

            if (!invokedOnBlockedThread)
            {
                // Otherwise run on worker thread.
#if !UNIX
                Utils.QueueWorkItemWithImpersonation(
                    _identityToPersonate,
                    ProcessDebuggerStopEventProc,
                    args);
#else
                Threading.ThreadPool.QueueUserWorkItem(
                    ProcessDebuggerStopEventProc,
                    args);

#endif
            }
        }

        private void ProcessDebuggerStopEventProc(object state)
        {
            RunspaceAvailability prevAvailability = _runspace.RunspaceAvailability;
            bool restoreAvailability = true;

            try
            {
                _handleDebuggerStop = true;

                // Update runspace availability
                SetRemoteDebug(true, RunspaceAvailability.RemoteDebug);

                // Raise event and wait for response.
                DebuggerStopEventArgs args = state as DebuggerStopEventArgs;
                if (args != null)
                {
                    if (IsDebuggerStopEventSubscribed())
                    {
                        try
                        {
                            // Blocking call.
                            base.RaiseDebuggerStopEvent(args);
                        }
                        finally
                        {
                            _handleDebuggerStop = false;
                            if (!_detachCommand && !args.SuspendRemote)
                            {
                                SetDebuggerAction(args.ResumeAction);
                            }
                        }
                    }
                    else
                    {
                        // If no debugger is subscribed to the DebuggerStop event then we
                        // allow the server side script execution to remain blocked in debug
                        // stop mode.  The runspace Availability reflects this and the client
                        // must take action (attach debugger or release remote debugger stop
                        // via SetDebuggerAction()).
                        restoreAvailability = false;
                        _handleDebuggerStop = false;
                    }
                }
                else
                {
                    // Null arguments may indicate that remote runspace was created without
                    // default type table and so arguments cannot be serialized.  In this case
                    // we don't want to block the remote side script execution.
                    _handleDebuggerStop = false;
                    SetDebuggerAction(DebuggerResumeAction.Continue);
                }
            }
            catch (Exception)
            {
                _handleDebuggerStop = false;
            }
            finally
            {
                // Restore runspace availability.
                if (restoreAvailability && (_runspace.RunspaceAvailability == RunspaceAvailability.RemoteDebug))
                {
                    SetRemoteDebug(false, prevAvailability);
                }

                if (_detachCommand)
                {
                    _detachCommand = false;
                }
            }
        }

        private void HandleForwardedDebuggerBreakpointUpdatedEvent(object sender, PSEventArgs e)
        {
            Dbg.Assert(e.SourceArgs.Length == 1, "Forwarded debugger breakpoint event args must always contain one SourceArgs item.");
            BreakpointUpdatedEventArgs bpArgs = e.SourceArgs[0] as BreakpointUpdatedEventArgs;

            if (bpArgs != null)
            {
                UpdateBreakpointCount(bpArgs.BreakpointCount);
                base.RaiseBreakpointUpdatedEvent(bpArgs);
            }
        }

        private PowerShell GetNestedPowerShell()
        {
            PowerShell ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.SetIsNested(true);

            return ps;
        }

        private void CheckForValidateState()
        {
            if (!_remoteDebugSupported)
            {
                throw new PSInvalidOperationException(
                    // The remote session to which you are connected does not support remote debugging.
                    // You must connect to a remote computer that is running PowerShell 4.0 or greater.
                    RemotingErrorIdStrings.RemoteDebuggingEndpointVersionError,
                    null,
                    "RemoteDebugger:RemoteDebuggingNotSupported",
                    ErrorCategory.NotImplemented,
                    null);
            }

            if (_runspace.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                throw new InvalidRunspaceStateException();
            }

#if !UNIX
            if (!_identityPersonationChecked)
            {
                _identityPersonationChecked = true;

                // Save identity to impersonate.
                Utils.TryGetWindowsImpersonatedIdentity(out _identityToPersonate);
            }
#endif
        }

        private void SetRemoteDebug(bool remoteDebug, RunspaceAvailability? availability)
        {
            if (_runspace.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                return;
            }

            if (IsRemoteDebug != remoteDebug)
            {
                IsRemoteDebug = remoteDebug;
                _runspace.RunspacePool.RemoteRunspacePoolInternal.IsRemoteDebugStop = remoteDebug;
            }

            if (availability != null)
            {
                RunspaceAvailability newAvailability = availability.Value;

                if ((_runspace.RunspaceAvailability != newAvailability) &&
                    (remoteDebug || (newAvailability != RunspaceAvailability.RemoteDebug)))
                {
                    try
                    {
                        _runspace.UpdateRunspaceAvailability(newAvailability, true);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void UpdateBreakpointCount(int bpCount)
        {
            _breakpointCount = bpCount;
            SetIsActive(bpCount);
        }

        private void SetIsActive(int breakpointCount)
        {
            if ((DebugMode & DebugModes.RemoteScript) == 0)
            {
                // Debugger is always inactive if RemoteScript is not selected.
                if (_isActive) { _isActive = false; }

                return;
            }

            if (breakpointCount > 0)
            {
                if (!_isActive) { _isActive = true; }
            }
            else
            {
                if (_isActive) { _isActive = false; }
            }
        }

        private T InvokeRemoteBreakpointFunction<T>(string functionName, Dictionary<string, object> parameters)
        {
            CheckForValidateState();

            using (PowerShell ps = GetNestedPowerShell())
            {
                ps.AddCommand(functionName);
                foreach (var parameterName in parameters.Keys)
                {
                    ps.AddParameter(parameterName, parameters[parameterName]);
                }

                Collection<PSObject> output = ps.Invoke<PSObject>();

                // If an error exception is returned then throw it here.
                if (ps.ErrorBuffer.Count > 0)
                {
                    Exception e = ps.ErrorBuffer[0].Exception;
                    if (e != null)
                    {
                        throw e;
                    }
                }

                // This helper is only used to return a single output item of type T.
                foreach (var item in output)
                {
                    if (item?.BaseObject is T)
                    {
                        return (T)item.BaseObject;
                    }

                    if (TryGetRemoteDebuggerException(item, out Exception ex))
                    {
                        throw ex;
                    }
                }

                return default(T);
            }
        }

        private void CheckRemoteBreakpointManagementSupport(string breakpointCommandNameToCheck)
        {
            _remoteBreakpointManagementIsSupported ??= _remoteDebuggingCapability.IsCommandSupported(breakpointCommandNameToCheck);

            if (!_remoteBreakpointManagementIsSupported.Value)
            {
                throw new PSNotSupportedException(
                    StringUtil.Format(
                        DebuggerStrings.CommandNotSupportedForRemoteUseInServerDebugger,
                        RemoteDebuggingCommands.CleanCommandName(breakpointCommandNameToCheck)));
            }
        }

        #endregion
    }

    #endregion

    #region RemoteSessionStateProxy

    internal sealed class RemoteSessionStateProxy : SessionStateProxy
    {
        private readonly RemoteRunspace _runspace;

        internal RemoteSessionStateProxy(RemoteRunspace runspace)
        {
            Dbg.Assert(runspace != null, "Caller should validate the parameter");
            _runspace = runspace;
        }

        private Exception _isInNoLanguageModeException = null;
        private Exception _getVariableCommandNotFoundException = null;
        private Exception _setVariableCommandNotFoundException = null;

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
        public override void SetVariable(string name, object value)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            // Verify the runspace has the Set-Variable command. For performance, throw if we got an error
            // before.
            if (_setVariableCommandNotFoundException != null)
                throw _setVariableCommandNotFoundException;

            // Since these are implemented as pipelines, we don't need to do our own
            // locking of sessionStateCallInProgress like we do with local runspaces.
            Pipeline remotePipeline = _runspace.CreatePipeline();
            Command command = new Command("Microsoft.PowerShell.Utility\\Set-Variable");
            command.Parameters.Add("Name", name);
            command.Parameters.Add("Value", value);
            remotePipeline.Commands.Add(command);

            try
            {
                remotePipeline.Invoke();
            }
            catch (RemoteException e)
            {
                if (string.Equals("CommandNotFoundException", e.ErrorRecord.FullyQualifiedErrorId, StringComparison.OrdinalIgnoreCase))
                {
                    _setVariableCommandNotFoundException = new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, e);
                    throw _setVariableCommandNotFoundException;
                }
                else
                    throw;
            }

            if (remotePipeline.Error.Count > 0)
            {
                // Don't cache these errors, as they are related to the actual variable being set.
                ErrorRecord error = (ErrorRecord)remotePipeline.Error.Read();
                throw new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, error.Exception);
            }
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
        public override object GetVariable(string name)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            // Verify the runspace has the Get-Variable command. For performance, throw if we got an error
            // before.
            if (_getVariableCommandNotFoundException != null)
                throw _getVariableCommandNotFoundException;

            // Since these are implemented as pipelines, we don't need to do our own
            // locking of sessionStateCallInProgress like we do with local runspaces.
            Pipeline remotePipeline = _runspace.CreatePipeline();
            Command command = new Command("Microsoft.PowerShell.Utility\\Get-Variable");
            command.Parameters.Add("Name", name);
            remotePipeline.Commands.Add(command);
            System.Collections.ObjectModel.Collection<PSObject> result = null;

            try
            {
                result = remotePipeline.Invoke();
            }
            catch (RemoteException e)
            {
                if (string.Equals("CommandNotFoundException", e.ErrorRecord.FullyQualifiedErrorId, StringComparison.OrdinalIgnoreCase))
                {
                    _getVariableCommandNotFoundException = new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, e);
                    throw _getVariableCommandNotFoundException;
                }
                else
                    throw;
            }

            if (remotePipeline.Error.Count > 0)
            {
                // Don't cache these errors, as they are related to the actual variable being set.
                ErrorRecord error = (ErrorRecord)remotePipeline.Error.Read();
                if (string.Equals("CommandNotFoundException", error.FullyQualifiedErrorId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, error.Exception);
                }
                else
                {
                    throw new PSInvalidOperationException(error.Exception.Message, error.Exception);
                }
            }

            if (result.Count != 1)
                return null;
            else
                return result[0].Properties["Value"].Value;
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
        public override List<string> Applications
        {
            get
            {
                // Verify the runspace has is not in NoLanguage mode. For performance, throw if we got an error
                // before.
                if (_isInNoLanguageModeException != null)
                    throw _isInNoLanguageModeException;

                // Since these are implemented as pipelines, we don't need to do our own
                // locking of sessionStateCallInProgress like we do with local runspaces.
                Pipeline remotePipeline = _runspace.CreatePipeline();
                remotePipeline.Commands.AddScript("$executionContext.SessionState.Applications");

                List<string> result = new List<string>();
                try
                {
                    foreach (PSObject application in remotePipeline.Invoke())
                    {
                        result.Add(application.BaseObject as string);
                    }
                }
                catch (RemoteException e)
                {
                    if (e.ErrorRecord.CategoryInfo.Category == ErrorCategory.ParserError)
                    {
                        _isInNoLanguageModeException = new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, e);
                        throw _isInNoLanguageModeException;
                    }
                    else
                        throw;
                }

                return result;
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
        public override List<string> Scripts
        {
            get
            {
                // Verify the runspace has is not in NoLanguage mode. For performance, throw if we got an error
                // before.
                if (_isInNoLanguageModeException != null)
                    throw _isInNoLanguageModeException;

                // Since these are implemented as pipelines, we don't need to do our own
                // locking of sessionStateCallInProgress like we do with local runspaces.
                Pipeline remotePipeline = _runspace.CreatePipeline();
                remotePipeline.Commands.AddScript("$executionContext.SessionState.Scripts");

                List<string> result = new List<string>();
                try
                {
                    foreach (PSObject application in remotePipeline.Invoke())
                    {
                        result.Add(application.BaseObject as string);
                    }
                }
                catch (RemoteException e)
                {
                    if (e.ErrorRecord.CategoryInfo.Category == ErrorCategory.ParserError)
                    {
                        _isInNoLanguageModeException = new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, e);
                        throw _isInNoLanguageModeException;
                    }
                    else
                        throw;
                }

                return result;
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
        public override DriveManagementIntrinsics Drive
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override PSLanguageMode LanguageMode
        {
            get
            {
                // Verify the runspace has is not in NoLanguage mode. For performance, return our
                // cached value if we got an error before.
                if (_isInNoLanguageModeException != null)
                    return PSLanguageMode.NoLanguage;

                // Since these are implemented as pipelines, we don't need to do our own
                // locking of sessionStateCallInProgress like we do with local runspaces.
                Pipeline remotePipeline = _runspace.CreatePipeline();
                remotePipeline.Commands.AddScript("$executionContext.SessionState.LanguageMode");

                System.Collections.ObjectModel.Collection<PSObject> result = null;

                try
                {
                    result = remotePipeline.Invoke();
                }
                catch (RemoteException e)
                {
                    if (e.ErrorRecord.CategoryInfo.Category == ErrorCategory.ParserError)
                    {
                        _isInNoLanguageModeException = new PSNotSupportedException(RunspaceStrings.NotSupportedOnRestrictedRunspace, e);
                        return PSLanguageMode.NoLanguage;
                    }
                    else
                        throw;
                }

                return (PSLanguageMode)LanguagePrimitives.ConvertTo(result[0], typeof(PSLanguageMode), CultureInfo.InvariantCulture);
            }

            set
            {
                throw new PSNotSupportedException();
            }
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
        public override PSModuleInfo Module
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override PathIntrinsics Path
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override CmdletProviderManagementIntrinsics Provider
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override PSVariableIntrinsics PSVariable
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override CommandInvocationIntrinsics InvokeCommand
        {
            get
            {
                throw new PSNotSupportedException();
            }
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
        public override ProviderIntrinsics InvokeProvider
        {
            get
            {
                throw new PSNotSupportedException();
            }
        }
    }

    #endregion
}
