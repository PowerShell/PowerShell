// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet connects PS sessions (RemoteRunspaces) that are in the Disconnected
    /// state and returns those PS session objects in the Opened state.  One or more
    /// session objects can be specified for connecting, or a remote computer name can
    /// be specified and in this case all disconnected remote runspaces found on the
    /// remote computer will be connected and PSSession objects created on the local
    /// machine.
    ///
    /// The cmdlet can be used in the following ways:
    ///
    /// Connect a PS session object:
    /// > $session = New-PSSession serverName
    /// > Disconnect-PSSession $session
    /// > Connect-PSSession $session
    ///
    /// Connect a PS session by name:
    /// > Connect-PSSession $session.Name
    ///
    /// Connect a PS session by Id:
    /// > Connect-PSSession $session.Id
    ///
    /// Connect a collection of PS session:
    /// > Get-PSSession | Connect-PSSession
    ///
    /// Connect all disconnected PS sessions on a remote computer
    /// > Connect-PSSession serverName.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsCommunications.Connect, "PSSession", SupportsShouldProcess = true, DefaultParameterSetName = ConnectPSSessionCommand.NameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096694", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(PSSession))]
    public class ConnectPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        #region Parameters

        private const string ComputerNameGuidParameterSet = "ComputerNameGuid";
        private const string ConnectionUriParameterSet = "ConnectionUri";
        private const string ConnectionUriGuidParameterSet = "ConnectionUriGuid";

        /// <summary>
        /// The PSSession object or objects to be connected.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = ConnectPSSessionCommand.SessionParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSSession[] Session { get; set; }

        /// <summary>
        /// Computer names to connect to.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet,
                   Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        public override string[] ComputerName { get; set; }

        /// <summary>
        /// This parameters specifies the appname which identifies the connection
        /// end point on the remote machine. If this parameter is not specified
        /// then the value specified in DEFAULTREMOTEAPPNAME will be used. If that's
        /// not specified as well, then "WSMAN" will be used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        public string ApplicationName
        {
            get
            {
                return _appName;
            }

            set
            {
                _appName = ResolveAppName(value);
            }
        }

        private string _appName;

        /// <summary>
        /// If this parameter is not specified then the value specified in
        /// the environment variable DEFAULTREMOTESHELLNAME will be used. If
        /// this is not set as well, then Microsoft.PowerShell is used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        public string ConfigurationName
        {
            get
            {
                return _shell;
            }

            set
            {
                _shell = ResolveShell(value);
            }
        }

        private string _shell;

        /// <summary>
        /// A complete URI(s) specified for the remote computer and shell to
        /// connect to and create a runspace for.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Uri[] ConnectionUri { get; set; }

        /// <summary>
        /// The AllowRedirection parameter enables the implicit redirection functionality.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        public SwitchParameter AllowRedirection
        {
            get { return _allowRedirection; }

            set { _allowRedirection = value; }
        }

        private bool _allowRedirection = false;

        /// <summary>
        /// RemoteRunspaceId to retrieve corresponding PSSession
        /// object.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.InstanceIdParameterSet,
                   Mandatory = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public override Guid[] InstanceId
        {
            get { return base.InstanceId; }

            set { base.InstanceId = value; }
        }

        /// <summary>
        /// Name of the remote runspaceinfo object.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.NameParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] Name
        {
            get { return base.Name; }

            set { base.Name = value; }
        }

        /// <summary>
        /// Specifies the credentials of the user to impersonate in the
        /// remote machine. If this parameter is not specified then the
        /// credentials of the current user process will be assumed.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        [Credential]
        public PSCredential Credential
        {
            get
            {
                return _psCredential;
            }

            set
            {
                _psCredential = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private PSCredential _psCredential;

        /// <summary>
        /// Use basic authentication to authenticate the user.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        public AuthenticationMechanism Authentication
        {
            get
            {
                return _authentication;
            }

            set
            {
                _authentication = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private AuthenticationMechanism _authentication;

        /// <summary>
        /// Specifies the certificate thumbprint to be used to impersonate the user on the
        /// remote machine.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        public string CertificateThumbprint
        {
            get
            {
                return _thumbprint;
            }

            set
            {
                _thumbprint = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private string _thumbprint;

        /// <summary>
        /// Port specifies the alternate port to be used in case the
        /// default ports are not used for the transport mechanism
        /// (port 80 for http and port 443 for useSSL)
        /// </summary>
        /// <remarks>
        /// Currently this is being accepted as a parameter. But in future
        /// support will be added to make this a part of a policy setting.
        /// When a policy setting is in place this parameter can be used
        /// to override the policy setting
        /// </remarks>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public int Port { get; set; }

        /// <summary>
        /// This parameter suggests that the transport scheme to be used for
        /// remote connections is useSSL instead of the default http.Since
        /// there are only two possible transport schemes that are possible
        /// at this point, a SwitchParameter is being used to switch between
        /// the two.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL { get; set; }

        /// <summary>
        /// Extended session options.  Used in this cmdlet to set server disconnect options.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        public PSSessionOption SessionOption { get; set; }

        /// <summary>
        /// Allows the user of the cmdlet to specify a throttling value
        /// for throttling the number of remote operations that can
        /// be executed simultaneously.
        /// </summary>
        [Parameter(ParameterSetName = ConnectPSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.IdParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ComputerNameGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.ConnectionUriGuidParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.NameParameterSet)]
        [Parameter(ParameterSetName = ConnectPSSessionCommand.InstanceIdParameterSet)]
        public int ThrottleLimit { get; set; } = 0;

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string[] ContainerId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override Guid[] VMId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string[] VMName
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Set up the ThrottleManager for runspace connect processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            _throttleManager.ThrottleLimit = ThrottleLimit;
            _throttleManager.ThrottleComplete += HandleThrottleConnectComplete;
        }

        /// <summary>
        /// Perform runspace connect processing on all input.
        /// </summary>
        protected override void ProcessRecord()
        {
            Collection<PSSession> psSessions;

            try
            {
                if (ParameterSetName == ConnectPSSessionCommand.ComputerNameParameterSet ||
                    ParameterSetName == ConnectPSSessionCommand.ComputerNameGuidParameterSet ||
                    ParameterSetName == ConnectPSSessionCommand.ConnectionUriParameterSet ||
                    ParameterSetName == ConnectPSSessionCommand.ConnectionUriGuidParameterSet)
                {
                    // Query remote computers for disconnected sessions.
                    psSessions = QueryForDisconnectedSessions();
                }
                else
                {
                    // Collect provided disconnected sessions.
                    psSessions = CollectDisconnectedSessions();
                }
            }
            catch (PSRemotingDataStructureException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (PSRemotingTransportException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (RemoteException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (InvalidRunspaceStateException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }

            ConnectSessions(psSessions);
        }

        /// <summary>
        /// End processing clean up.
        /// </summary>
        protected override void EndProcessing()
        {
            _throttleManager.EndSubmitOperations();

            // Wait for all connect operations to complete.
            _operationsComplete.WaitOne();

            // If there are failed connect operations due to stale
            // session state then perform the query retry here.
            if (_failedSessions.Count > 0)
            {
                RetryFailedSessions();
            }

            // Read all objects in the stream pipeline.
            while (_stream.ObjectReader.Count > 0)
            {
                object streamObject = _stream.ObjectReader.Read();
                WriteStreamObject((Action<Cmdlet>)streamObject);
            }

            _stream.ObjectWriter.Close();

            // Add all successfully connected sessions to local repository.
            foreach (PSSession psSession in _allSessions)
            {
                if (psSession.Runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    // Make sure that this session is included in the PSSession repository.
                    // If it already exists then replace it because we want the latest/connected session in the repository.
                    this.RunspaceRepository.AddOrReplace(psSession);
                }
            }
        }

        /// <summary>
        /// User has signaled a stop for this cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            // Close the output stream for any further writes.
            _stream.ObjectWriter.Close();

            // Stop any remote server queries that may be running.
            _queryRunspaces.StopAllOperations();

            // Signal the ThrottleManager to stop any further
            // PSSession connect processing.
            _throttleManager.StopAllOperations();

            // Signal the Retry throttle manager in case it is running.
            _retryThrottleManager.StopAllOperations();
        }

        #endregion

        #region ConnectRunspaceOperation Class

        /// <summary>
        /// Throttle class to perform a remoterunspace connect operation.
        /// </summary>
        private sealed class ConnectRunspaceOperation : IThrottleOperation
        {
            private PSSession _session;
            private PSSession _oldSession;
            private readonly ObjectStream _writeStream;
            private readonly Collection<PSSession> _retryList;
            private readonly PSHost _host;
            private readonly QueryRunspaces _queryRunspaces;
            private static readonly object s_LockObject = new object();

            internal ConnectRunspaceOperation(
                PSSession session,
                ObjectStream stream,
                PSHost host,
                QueryRunspaces queryRunspaces,
                Collection<PSSession> retryList)
            {
                _session = session;
                _writeStream = stream;
                _host = host;
                _queryRunspaces = queryRunspaces;
                _retryList = retryList;
                _session.Runspace.StateChanged += StateCallBackHandler;
            }

            internal override void StartOperation()
            {
                bool startedSuccessfully = true;

                Exception ex = null;
                try
                {
                    if (_queryRunspaces != null)
                    {
                        PSSession newSession = QueryForSession(_session);
                        if (newSession != null)
                        {
                            _session.Runspace.StateChanged -= StateCallBackHandler;
                            _oldSession = _session;
                            _session = newSession;
                            _session.Runspace.StateChanged += StateCallBackHandler;
                            _session.Runspace.ConnectAsync();
                        }
                        else
                        {
                            startedSuccessfully = false;
                        }
                    }
                    else
                    {
                        _session.Runspace.ConnectAsync();
                    }
                }
                catch (PSInvalidOperationException e)
                {
                    ex = e;
                }
                catch (InvalidRunspacePoolStateException e)
                {
                    ex = e;
                }
                catch (RuntimeException e)
                {
                    ex = e;
                }

                if (ex != null)
                {
                    startedSuccessfully = false;
                    WriteConnectFailed(ex, _session);
                }

                if (!startedSuccessfully)
                {
                    // We are done at this point.  Notify throttle manager.
                    _session.Runspace.StateChanged -= StateCallBackHandler;
                    SendStartComplete();
                }
            }

            internal override void StopOperation()
            {
                _queryRunspaces?.StopAllOperations();

                _session.Runspace.StateChanged -= StateCallBackHandler;
                SendStopComplete();
            }

            internal override event EventHandler<OperationStateEventArgs> OperationComplete;

            internal PSSession QueryForSession(PSSession session)
            {
                Collection<WSManConnectionInfo> wsManConnectionInfos = new Collection<WSManConnectionInfo>();
                wsManConnectionInfos.Add(session.Runspace.ConnectionInfo as WSManConnectionInfo);

                Exception ex = null;
                Collection<PSSession> sessions = null;
                try
                {
                    sessions = _queryRunspaces.GetDisconnectedSessions(wsManConnectionInfos, _host, _writeStream, null,
                        0, SessionFilterState.Disconnected, new Guid[] { session.InstanceId }, null, null);
                }
                catch (RuntimeException e)
                {
                    ex = e;
                }

                if (ex != null)
                {
                    WriteConnectFailed(ex, session);
                    return null;
                }

                if (sessions.Count != 1)
                {
                    ex = new RuntimeException(StringUtil.Format(RemotingErrorIdStrings.CannotFindSessionForConnect,
                        session.Name, session.ComputerName));

                    WriteConnectFailed(ex, session);
                    return null;
                }

                return sessions[0];
            }

            private void StateCallBackHandler(object sender, RunspaceStateEventArgs eArgs)
            {
                if (eArgs.RunspaceStateInfo.State == RunspaceState.Connecting ||
                    eArgs.RunspaceStateInfo.State == RunspaceState.Disconnecting ||
                    eArgs.RunspaceStateInfo.State == RunspaceState.Disconnected)
                {
                    return;
                }

                Dbg.Assert(eArgs.RunspaceStateInfo.State != RunspaceState.BeforeOpen, "Can't reconnect a session that hasn't been previously Opened");
                Dbg.Assert(eArgs.RunspaceStateInfo.State != RunspaceState.Opening, "Can't reconnect a session that hasn't been previously Opened");

                if (eArgs.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    // Connect operation succeeded, write the PSSession object.
                    WriteConnectedPSSession();
                }
                else
                {
                    // Check to see if failure is due to stale PSSession error and
                    // add to retry list if this is the case.
                    bool writeError = true;
                    if (_queryRunspaces == null)
                    {
                        PSRemotingTransportException transportException = eArgs.RunspaceStateInfo.Reason as PSRemotingTransportException;
                        if (transportException != null &&
                            transportException.ErrorCode == WSManNativeApi.ERROR_WSMAN_INUSE_CANNOT_RECONNECT)
                        {
                            lock (s_LockObject)
                            {
                                _retryList.Add(_session);
                            }

                            writeError = false;
                        }
                    }

                    if (writeError)
                    {
                        // Connect operation failed, write error.
                        WriteConnectFailed(eArgs.RunspaceStateInfo.Reason, _session);
                    }
                }

                _session.Runspace.StateChanged -= StateCallBackHandler;
                SendStartComplete();
            }

            private void SendStartComplete()
            {
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StartComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            private void SendStopComplete()
            {
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StopComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            private void WriteConnectedPSSession()
            {
                // Use temporary variable because we need to preserve _session class variable
                // for later clean up.
                PSSession outSession = _session;
                if (_queryRunspaces != null)
                {
                    lock (s_LockObject)
                    {
                        // Pass back the original session if possible.
                        if (_oldSession != null &&
                            _oldSession.InsertRunspace(_session.Runspace as RemoteRunspace))
                        {
                            outSession = _oldSession;
                            _retryList.Add(_oldSession);
                        }
                        else
                        {
                            _retryList.Add(_session);
                        }
                    }
                }

                if (_writeStream.ObjectWriter.IsOpen)
                {
                    // This code is based on ThrottleManager infrastructure
                    // and this particular method may be called on a thread that
                    // is different from Pipeline Execution Thread. Hence using
                    // a delegate to perform the WriteObject.
                    Action<Cmdlet> outputWriter = (Cmdlet cmdlet) => cmdlet.WriteObject(outSession);
                    _writeStream.ObjectWriter.Write(outputWriter);
                }
            }

            private void WriteConnectFailed(
                Exception e,
                PSSession session)
            {
                if (_writeStream.ObjectWriter.IsOpen)
                {
                    string FQEID = "PSSessionConnectFailed";
                    Exception reason;
                    if (e != null && !string.IsNullOrEmpty(e.Message))
                    {
                        // Update fully qualified error Id if we have a transport error.
                        PSRemotingTransportException transportException = e as PSRemotingTransportException;
                        if (transportException != null)
                        {
                            FQEID = WSManTransportManagerUtils.GetFQEIDFromTransportError(transportException.ErrorCode, FQEID);
                        }

                        reason = new RuntimeException(
                            StringUtil.Format(RemotingErrorIdStrings.RunspaceConnectFailedWithMessage, session.Name, e.Message),
                            e);
                    }
                    else
                    {
                        reason = new RuntimeException(
                            StringUtil.Format(RemotingErrorIdStrings.RunspaceConnectFailed, session.Name,
                                session.Runspace.RunspaceStateInfo.State.ToString()), null);
                    }

                    ErrorRecord errorRecord = new ErrorRecord(reason, FQEID, ErrorCategory.InvalidOperation, null);
                    Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
                    _writeStream.ObjectWriter.Write(errorWriter);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Enum indicating an override on which parameter is used to filter
        /// local sessions.
        /// </summary>
        private enum OverrideParameter
        {
            /// <summary>
            /// No override.
            /// </summary>
            None = 0,

            /// <summary>
            /// Use the Name parameter as a filter.
            /// </summary>
            Name = 1,

            /// <summary>
            /// Use the InstanceId parameter as a filter.
            /// </summary>
            InstanceId = 2
        }

        /// <summary>
        /// Retrieves a collection of disconnected PSSession objects queried from
        /// remote computers.
        /// </summary>
        /// <returns>Collection of disconnected PSSession objects.</returns>
        private Collection<PSSession> QueryForDisconnectedSessions()
        {
            Collection<WSManConnectionInfo> connectionInfos = GetConnectionObjects();
            Collection<PSSession> psSessions = _queryRunspaces.GetDisconnectedSessions(connectionInfos, this.Host, _stream,
                                                    this.RunspaceRepository, ThrottleLimit,
                                                    SessionFilterState.Disconnected, this.InstanceId, this.Name, ConfigurationName);

            // Write any error output from stream object.
            Collection<object> streamObjects = _stream.ObjectReader.NonBlockingRead();
            foreach (object streamObject in streamObjects)
            {
                WriteStreamObject((Action<Cmdlet>)streamObject);
            }

            return psSessions;
        }

        /// <summary>
        /// Creates a collection of PSSession objects based on cmdlet parameters.
        /// </summary>
        /// <param name="overrideParam">OverrideParameter.</param>
        /// <returns>Collection of PSSession objects in disconnected state.</returns>
        private Collection<PSSession> CollectDisconnectedSessions(OverrideParameter overrideParam = OverrideParameter.None)
        {
            Collection<PSSession> psSessions = new Collection<PSSession>();

            // Get all remote runspaces to disconnect.
            if (ParameterSetName == DisconnectPSSessionCommand.SessionParameterSet)
            {
                if (Session != null)
                {
                    foreach (PSSession psSession in Session)
                    {
                        psSessions.Add(psSession);
                    }
                }
            }
            else
            {
                Dictionary<Guid, PSSession> entries = null;

                switch (overrideParam)
                {
                    case OverrideParameter.None:
                        entries = GetMatchingRunspaces(false, true);
                        break;

                    case OverrideParameter.Name:
                        entries = GetMatchingRunspacesByName(false, true);
                        break;

                    case OverrideParameter.InstanceId:
                        entries = GetMatchingRunspacesByRunspaceId(false, true);
                        break;
                }

                if (entries != null)
                {
                    foreach (PSSession psSession in entries.Values)
                    {
                        psSessions.Add(psSession);
                    }
                }
            }

            return psSessions;
        }

        /// <summary>
        /// Connect all disconnected sessions.
        /// </summary>
        private void ConnectSessions(Collection<PSSession> psSessions)
        {
            List<IThrottleOperation> connectOperations = new List<IThrottleOperation>();

            // Create a disconnect operation for each runspace to disconnect.
            foreach (PSSession psSession in psSessions)
            {
                if (ShouldProcess(psSession.Name, VerbsCommunications.Connect))
                {
                    if (psSession.ComputerType != TargetMachineType.RemoteMachine)
                    {
                        // PS session disconnection is not supported for VM/Container sessions.
                        string msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeConnectedForVMContainerSession,
                            psSession.Name, psSession.ComputerName, psSession.ComputerType);
                        Exception reason = new PSNotSupportedException(msg);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "CannotConnectVMContainerSession", ErrorCategory.InvalidOperation, psSession);
                        WriteError(errorRecord);
                    }
                    else if (psSession.Runspace.RunspaceStateInfo.State == RunspaceState.Disconnected &&
                        psSession.Runspace.RunspaceAvailability == RunspaceAvailability.None)
                    {
                        // Can only connect sessions that are in Disconnected state.
                        // Update session connection information based on cmdlet parameters.
                        UpdateConnectionInfo(psSession.Runspace.ConnectionInfo as WSManConnectionInfo);

                        ConnectRunspaceOperation connectOperation = new ConnectRunspaceOperation(
                            psSession,
                            _stream,
                            this.Host,
                            null,
                            _failedSessions);
                        connectOperations.Add(connectOperation);
                    }
                    else if (psSession.Runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                    {
                        // Write error record if runspace is not already in the Opened state.
                        string msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeConnected, psSession.Name);
                        Exception reason = new RuntimeException(msg);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "PSSessionConnectFailed", ErrorCategory.InvalidOperation, psSession);
                        WriteError(errorRecord);
                    }
                    else
                    {
                        // Session is already connected.  Write to output.
                        WriteObject(psSession);
                    }
                }

                _allSessions.Add(psSession);
            }

            if (connectOperations.Count > 0)
            {
                // Make sure operations are not set as complete while processing input.
                _operationsComplete.Reset();

                // Submit list of connect operations.
                _throttleManager.SubmitOperations(connectOperations);

                // Write any output now.
                Collection<object> streamObjects = _stream.ObjectReader.NonBlockingRead();
                foreach (object streamObject in streamObjects)
                {
                    WriteStreamObject((Action<Cmdlet>)streamObject);
                }
            }
        }

        /// <summary>
        /// Handles the connect throttling complete event from the ThrottleManager.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="eventArgs">EventArgs.</param>
        private void HandleThrottleConnectComplete(object sender, EventArgs eventArgs)
        {
            _operationsComplete.Set();
        }

        private Collection<WSManConnectionInfo> GetConnectionObjects()
        {
            Collection<WSManConnectionInfo> connectionInfos = new Collection<WSManConnectionInfo>();

            if (ParameterSetName == ConnectPSSessionCommand.ComputerNameParameterSet ||
                ParameterSetName == ConnectPSSessionCommand.ComputerNameGuidParameterSet)
            {
                string scheme = UseSSL ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;

                foreach (string computerName in ComputerName)
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.Scheme = scheme;
                    connectionInfo.ComputerName = ResolveComputerName(computerName);
                    connectionInfo.AppName = ApplicationName;
                    connectionInfo.ShellUri = ConfigurationName;
                    connectionInfo.Port = Port;
                    if (CertificateThumbprint != null)
                    {
                        connectionInfo.CertificateThumbprint = CertificateThumbprint;
                    }
                    else
                    {
                        connectionInfo.Credential = Credential;
                    }

                    connectionInfo.AuthenticationMechanism = Authentication;
                    UpdateConnectionInfo(connectionInfo);

                    connectionInfos.Add(connectionInfo);
                }
            }
            else if (ParameterSetName == ConnectPSSessionCommand.ConnectionUriParameterSet ||
                     ParameterSetName == ConnectPSSessionCommand.ConnectionUriGuidParameterSet)
            {
                foreach (var connectionUri in ConnectionUri)
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.ConnectionUri = connectionUri;
                    connectionInfo.ShellUri = ConfigurationName;
                    if (CertificateThumbprint != null)
                    {
                        connectionInfo.CertificateThumbprint = CertificateThumbprint;
                    }
                    else
                    {
                        connectionInfo.Credential = Credential;
                    }

                    connectionInfo.AuthenticationMechanism = Authentication;
                    UpdateConnectionInfo(connectionInfo);

                    connectionInfos.Add(connectionInfo);
                }
            }

            return connectionInfos;
        }

        /// <summary>
        /// Updates connection info with the data read from cmdlet's parameters.
        /// </summary>
        /// <param name="connectionInfo"></param>
        private void UpdateConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            if (ParameterSetName != ConnectPSSessionCommand.ConnectionUriParameterSet &&
                ParameterSetName != ConnectPSSessionCommand.ConnectionUriGuidParameterSet)
            {
                // uri redirection is supported only with URI parameter set
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }

            if (!_allowRedirection)
            {
                // uri redirection required explicit user consent
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }

            // Update the connectionInfo object with passed in session options.
            if (SessionOption != null)
            {
                connectionInfo.SetSessionOptions(SessionOption);
            }
        }

        private void RetryFailedSessions()
        {
            using (ManualResetEvent retrysComplete = new ManualResetEvent(false))
            {
                Collection<PSSession> connectedSessions = new Collection<PSSession>();
                List<IThrottleOperation> retryConnectionOperations = new List<IThrottleOperation>();
                _retryThrottleManager.ThrottleLimit = ThrottleLimit;
                _retryThrottleManager.ThrottleComplete += (sender, eventArgs) =>
                    {
                        try
                        {
                            retrysComplete.Set();
                        }
                        catch (ObjectDisposedException) { }
                    };

                foreach (var session in _failedSessions)
                {
                    retryConnectionOperations.Add(new ConnectRunspaceOperation(
                        session,
                        _stream,
                        this.Host,
                        new QueryRunspaces(),
                        connectedSessions));
                }

                _retryThrottleManager.SubmitOperations(retryConnectionOperations);
                _retryThrottleManager.EndSubmitOperations();

                retrysComplete.WaitOne();

                // Add or replace all successfully connected sessions to the local repository.
                foreach (var session in connectedSessions)
                {
                    this.RunspaceRepository.AddOrReplace(session);
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose method of IDisposable. Gets called in the following cases:
        ///     1. Pipeline explicitly calls dispose on cmdlets
        ///     2. Called by the garbage collector.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method which does the actual
        /// dispose operations and finalize suppressions.
        /// </summary>
        /// <param name="disposing">Whether method is called
        /// from Dispose or destructor</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _throttleManager.Dispose();

                _operationsComplete.WaitOne();
                _operationsComplete.Dispose();

                _throttleManager.ThrottleComplete -= HandleThrottleConnectComplete;
                _retryThrottleManager.Dispose();

                _stream.Dispose();
            }
        }

        #endregion

        #region Private Members

        // Collection of PSSessions to be connected.
        private readonly Collection<PSSession> _allSessions = new Collection<PSSession>();

        // Object used to perform network disconnect operations in a limited manner.
        private readonly ThrottleManager _throttleManager = new ThrottleManager();

        // Event indicating that all disconnect operations through the ThrottleManager
        // are complete.
        private readonly ManualResetEvent _operationsComplete = new ManualResetEvent(true);

        // Object used for querying remote runspaces.
        private readonly QueryRunspaces _queryRunspaces = new QueryRunspaces();

        // Object to collect output data from multiple threads.
        private readonly ObjectStream _stream = new ObjectStream();

        // Support for connection retry on failure.
        private readonly ThrottleManager _retryThrottleManager = new ThrottleManager();
        private readonly Collection<PSSession> _failedSessions = new Collection<PSSession>();

        #endregion
    }
}
