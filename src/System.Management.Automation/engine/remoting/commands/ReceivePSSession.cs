// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet connects a running command associated with a PS session and then
    /// directs the command output either:
    /// a) To Host.  This is the synchronous mode of the cmdlet which won't return
    ///    until the running command completes and all output data is received on
    ///    the client.
    /// b) To a job object.  This is the asynchronous mode of the cmdlet which will
    ///    return immediately providing the job object that is collecting the
    ///    running command output data.
    ///
    /// The running command becomes disconnected when the associated runspace is
    /// disconnected (via the Disconnect-PSSession cmdlet).
    ///
    /// The associated runspace object must be in the Opened state (connected) before
    /// the running command can be connected.  If the associated runspace object is
    /// in the disconnected state, it will first be connected before the running
    /// command is connected.
    ///
    /// The user can specify how command output data is returned by using the public
    /// OutTarget enumeration (Host, Job).
    /// The default actions of this cmdlet is to always direct output to host unless
    /// a job object already exists on the client that is associated with the running
    /// command.  In this case the existing job object is connected to the running
    /// command and returned.
    ///
    /// The cmdlet can be used in the following ways:
    ///
    /// Receive PS session data by session object
    /// > $session = New-PSSession serverName
    /// > $job1 = Invoke-Command $session { [script] } -asjob
    /// > Disconnect-PSSession $session
    /// > Connect-PSSession $session
    /// > Receive-PSSession $session    // command output continues collecting at job object.
    ///
    /// Receive PS session data by session Id
    /// > Receive-PSSession $session.Id
    ///
    /// Receive PS session data by session instance Id
    /// > Receive-PSSession $session.InstanceId
    ///
    /// Receive PS session data by session Name.  Direct output to job
    /// > Receive-PSSession $session.Name
    ///
    /// Receive a running command from a computer.
    /// > $job = Receive-PSSession -ComputerName ServerOne -Name SessionName -OutTarget Job.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsCommunications.Receive, "PSSession", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        DefaultParameterSetName = ReceivePSSessionCommand.SessionParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096800",
        RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class ReceivePSSessionCommand : PSRemotingCmdlet
    {
        #region Parameters

        private const string IdParameterSet = "Id";
        private const string InstanceIdParameterSet = "InstanceId";
        private const string NameParameterSet = "SessionName";
        private const string ComputerSessionNameParameterSet = "ComputerSessionName";   // Computer name and session Name.
        private const string ConnectionUriSessionNameParameterSet = "ConnectionUriSessionName";
        private const string ConnectionUriInstanceIdParameterSet = "ConnectionUriInstanceId";

        /// <summary>
        /// The PSSession object to receive data from.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = ReceivePSSessionCommand.SessionParameterSet)]
        [ValidateNotNullOrEmpty]
        public PSSession Session { get; set; }

        /// <summary>
        /// Session Id of PSSession object to receive data from.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = ReceivePSSessionCommand.IdParameterSet)]
        public int Id { get; set; }

        /// <summary>
        /// Computer name to receive session data from.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        public string ComputerName { get; set; }

        /// <summary>
        /// This parameters specifies the appname which identifies the connection
        /// end point on the remote machine. If this parameter is not specified
        /// then the value specified in DEFAULTREMOTEAPPNAME will be used. If that's
        /// not specified as well, then "WSMAN" will be used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
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
                   ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        public Uri ConnectionUri { get; set; }

        /// <summary>
        /// The AllowRedirection parameter enables the implicit redirection functionality.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public SwitchParameter AllowRedirection
        {
            get { return _allowRedirection; }

            set { _allowRedirection = value; }
        }

        private bool _allowRedirection = false;

        /// <summary>
        /// Instance Id of PSSession object to receive data from.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = ReceivePSSessionCommand.InstanceIdParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public Guid InstanceId { get; set; }

        /// <summary>
        /// Name of PSSession object to receive data from.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = ReceivePSSessionCommand.NameParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Determines how running command output is returned on client.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.IdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.NameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public OutTarget OutTarget { get; set; } = OutTarget.Default;

        /// <summary>
        /// Provides job name when job is created for returned data.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.IdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.NameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Specifies the credentials of the user to impersonate in the
        /// remote machine. If this parameter is not specified then the
        /// credentials of the current user process will be assumed.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public int Port { get; set; }

        /// <summary>
        /// This parameter suggests that the transport scheme to be used for
        /// remote connections is useSSL instead of the default http.Since
        /// there are only two possible transport schemes that are possible
        /// at this point, a SwitchParameter is being used to switch between
        /// the two.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL { get; set; }

        /// <summary>
        /// Session options.
        /// </summary>
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ComputerSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)]
        [Parameter(ParameterSetName = ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public PSSessionOption SessionOption { get; set; }

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName == ReceivePSSessionCommand.ComputerSessionNameParameterSet ||
                ParameterSetName == ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)
            {
                QueryForAndConnectCommands(Name, Guid.Empty);
            }
            else if (ParameterSetName == ReceivePSSessionCommand.ComputerInstanceIdParameterSet ||
                     ParameterSetName == ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet)
            {
                QueryForAndConnectCommands(string.Empty, InstanceId);
            }
            else
            {
                GetAndConnectSessionCommand();
            }
        }

        /// <summary>
        /// User has signaled a stop for this cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            RemotePipeline tmpPipeline;
            Job tmpJob;

            lock (_syncObject)
            {
                _stopProcessing = true;
                tmpPipeline = _remotePipeline;
                tmpJob = _job;
            }

            tmpPipeline?.StopAsync();
            tmpJob?.StopJob();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Queries the remote computer for the specified session, creates a disconnected
        /// session object, connects the runspace/command and collects command data.
        /// Command output is either returned (OutTarget.Host) or collected
        /// in a job object that is returned (OutTarget.Job).
        /// </summary>
        /// <param name="name">Name of session to find.</param>
        /// <param name="instanceId">Instance Id of session to find.</param>
        private void QueryForAndConnectCommands(string name, Guid instanceId)
        {
            WSManConnectionInfo connectionInfo = GetConnectionObject();

            // Retrieve all disconnected runspaces on the remote computer.
            Runspace[] runspaces;
            try
            {
                runspaces = Runspace.GetRunspaces(connectionInfo, this.Host, QueryRunspaces.BuiltInTypesTable);
            }
            catch (System.Management.Automation.RuntimeException e)
            {
                int errorCode;
                string msg = StringUtil.Format(RemotingErrorIdStrings.QueryForRunspacesFailed, connectionInfo.ComputerName,
                    QueryRunspaces.ExtractMessage(e.InnerException, out errorCode));
                string FQEID = WSManTransportManagerUtils.GetFQEIDFromTransportError(errorCode, "ReceivePSSessionQueryForSessionFailed");
                Exception reason = new RuntimeException(msg, e.InnerException);
                ErrorRecord errorRecord = new ErrorRecord(reason, FQEID, ErrorCategory.InvalidOperation, connectionInfo);
                WriteError(errorRecord);
                return;
            }

            // Convert configuration name into shell Uri for comparison.
            string shellUri = null;
            if (!string.IsNullOrEmpty(ConfigurationName))
            {
                shellUri = ConfigurationName.Contains(WSManNativeApi.ResourceURIPrefix, StringComparison.OrdinalIgnoreCase)
                    ? ConfigurationName
                    : WSManNativeApi.ResourceURIPrefix + ConfigurationName;
            }

            // Connect selected runspace/command and direct command output to host
            // or job objects.
            foreach (Runspace runspace in runspaces)
            {
                if (_stopProcessing)
                {
                    break;
                }

                // Filter returned runspaces by ConfigurationName if provided.
                if (shellUri != null)
                {
                    // Compare with returned shell Uri in connection info.
                    WSManConnectionInfo wsmanConnectionInfo = runspace.ConnectionInfo as WSManConnectionInfo;
                    if (wsmanConnectionInfo != null &&
                        !shellUri.Equals(wsmanConnectionInfo.ShellUri, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // Find specified session.
                bool haveMatch = false;
                if (!string.IsNullOrEmpty(name) &&
                    string.Equals(name, ((RemoteRunspace)runspace).RunspacePool.RemoteRunspacePoolInternal.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Selected by friendly name.
                    haveMatch = true;
                }
                else if (instanceId.Equals(runspace.InstanceId))
                {
                    // Selected by instance Id (note that session/runspace/runspacepool instanceIds are identical.)
                    haveMatch = true;
                }

                if (haveMatch &&
                    ShouldProcess(((RemoteRunspace)runspace).PSSessionName, VerbsCommunications.Receive))
                {
                    // Check the local repository for an existing viable session.
                    PSSession locSession = this.RunspaceRepository.GetItem(runspace.InstanceId);

                    // Connect the session here.  If it fails (connectedSession == null) revert to the
                    // reconstruct method.
                    Exception ex;
                    PSSession connectedSession = ConnectSession(locSession, out ex);

                    if (connectedSession != null)
                    {
                        // Make sure that this connected session is included in the PSSession repository.
                        // If it already exists then replace it because we want the latest/connected session in the repository.
                        this.RunspaceRepository.AddOrReplace(connectedSession);

                        // Since we have a local runspace we will do a *reconnect* operation and will
                        // need the corresponding job object.
                        PSRemotingJob job = FindJobForSession(connectedSession);
                        if (this.OutTarget == OutTarget.Host)
                        {
                            ConnectSessionToHost(connectedSession, job);
                        }
                        else
                        {
                            // Connection to Job is default option.
                            ConnectSessionToJob(connectedSession, job);
                        }
                    }
                    else
                    {
                        // Otherwise create a new session from the queried runspace object.
                        // This will be a *reconstruct* operation.
                        // Create and connect session.
                        PSSession newSession = new PSSession(runspace as RemoteRunspace);
                        connectedSession = ConnectSession(newSession, out ex);
                        if (connectedSession != null)
                        {
                            // Try to reuse the existing local repository PSSession object.
                            if (locSession != null)
                            {
                                connectedSession = locSession.InsertRunspace(connectedSession.Runspace as RemoteRunspace) ? locSession : connectedSession;
                            }

                            // Make sure that this connected session is included in the PSSession repository.
                            // If it already exists then replace it because we want the latest/connected session in the repository.
                            this.RunspaceRepository.AddOrReplace(connectedSession);

                            if (this.OutTarget == OutTarget.Job)
                            {
                                ConnectSessionToJob(connectedSession);
                            }
                            else
                            {
                                // Connection to Host is default option.
                                ConnectSessionToHost(connectedSession);
                            }
                        }
                        else
                        {
                            string message = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeConnected, newSession.Name);
                            WriteError(new ErrorRecord(new ArgumentException(message, ex), "ReceivePSSessionCannotConnectSession",
                                       ErrorCategory.InvalidOperation, newSession));
                        }
                    }

                    break;
                }
            }
        }

        private WSManConnectionInfo GetConnectionObject()
        {
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo();

            if (ParameterSetName == ReceivePSSessionCommand.ComputerSessionNameParameterSet ||
                ParameterSetName == ReceivePSSessionCommand.ComputerInstanceIdParameterSet)
            {
                // Create the WSManConnectionInfo object for the specified computer name.
                string scheme = UseSSL.IsPresent ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;

                connectionInfo.Scheme = scheme;
                connectionInfo.ComputerName = RemotingUtils.ResolveComputerName(ComputerName);
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
            }
            else
            {
                connectionInfo.ConnectionUri = ConnectionUri;
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
            }

            return connectionInfo;
        }

        /// <summary>
        /// Updates connection info with the data read from cmdlet's parameters.
        /// </summary>
        /// <param name="connectionInfo"></param>
        private void UpdateConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            if (ParameterSetName != ReceivePSSessionCommand.ConnectionUriInstanceIdParameterSet &&
                ParameterSetName != ReceivePSSessionCommand.ConnectionUriSessionNameParameterSet)
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

        /// <summary>
        /// Gets the PSSession object to connect based on Id, Name, etc.
        /// Connects the running command associated with the PSSession runspace object.
        /// Command output is either returned (OutTarget.Host) or collected
        /// in a job object that is returned (OutTarget.Job).
        /// </summary>
        private void GetAndConnectSessionCommand()
        {
            PSSession session = null;

            if (ParameterSetName == ReceivePSSessionCommand.SessionParameterSet)
            {
                session = Session;
            }
            else if (ParameterSetName == ReceivePSSessionCommand.IdParameterSet)
            {
                session = GetSessionById(Id);
                if (session == null)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedSessionId,
                                              RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedSessionId,
                                              Id);

                    return;
                }
            }
            else if (ParameterSetName == ReceivePSSessionCommand.NameParameterSet)
            {
                session = GetSessionByName(Name);
                if (session == null)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedName,
                                              RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedName,
                                              Name);

                    return;
                }
            }
            else if (ParameterSetName == ReceivePSSessionCommand.InstanceIdParameterSet)
            {
                session = GetSessionByInstanceId(InstanceId);
                if (session == null)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedRunspaceId,
                                              RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedRunspaceId,
                                              InstanceId);

                    return;
                }
            }
            else
            {
                Dbg.Assert(false, "Invalid Parameter Set");
            }

            // PS session disconnection is not supported for VM/Container sessions.
            if (session.ComputerType != TargetMachineType.RemoteMachine)
            {
                string msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeReceivedForVMContainerSession,
                    session.Name, session.ComputerName, session.ComputerType);
                Exception reason = new PSNotSupportedException(msg);
                ErrorRecord errorRecord = new ErrorRecord(reason, "CannotReceiveVMContainerSession", ErrorCategory.InvalidOperation, session);
                WriteError(errorRecord);
                return;
            }

            if (ShouldProcess(session.Name, VerbsCommunications.Receive))
            {
                Exception ex;
                if (ConnectSession(session, out ex) == null)
                {
                    // Unable to connect runspace.  If this was a *reconnect* runspace then try
                    // obtaining a connectable runspace directly from the server and do a
                    // *reconstruct* connect.
                    PSSession oldSession = session;
                    session = TryGetSessionFromServer(oldSession);
                    if (session == null)
                    {
                        // No luck.  Return error.
                        string message = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeConnected, oldSession.Name);
                        WriteError(new ErrorRecord(new ArgumentException(message, ex), "ReceivePSSessionCannotConnectSession",
                                   ErrorCategory.InvalidOperation, oldSession));

                        return;
                    }
                }

                // Look to see if there exists a job associated with this runspace.
                // If so then we use this job object, unless the user explicitly specifies
                // output to host.
                PSRemotingJob job = FindJobForSession(session);
                if (job != null)
                {
                    // Default is to route data to job.
                    if (OutTarget == OutTarget.Host)
                    {
                        // This performs a *reconstruct* connection scenario where a new
                        // pipeline object is created and connected.
                        ConnectSessionToHost(session, job);
                    }
                    else
                    {
                        // This preforms a *reconnect* scenario where the existing job
                        // and runspace objects are reconnected.
                        ConnectSessionToJob(session, job);
                    }
                }
                else
                {
                    // Default is to route data to host.
                    if (OutTarget == OutTarget.Job)
                    {
                        // This performs a *reconstruct* connection scenario where new
                        // pipeline/job objects are created and connected.
                        ConnectSessionToJob(session);
                    }
                    else
                    {
                        // This performs a *reconstruct* connection scenario where a new
                        // pipeline object is created and connected.
                        ConnectSessionToHost(session);
                    }
                }

                // Make sure that if this session is successfully connected that it is included
                // in the PSSession repository.  If it already exists then replace it because we
                // want the latest/connected session in the repository.
                if (session.Runspace.RunspaceStateInfo.State != RunspaceState.Disconnected)
                {
                    this.RunspaceRepository.AddOrReplace(session);
                }
            }
        }

        private bool CheckForDebugMode(PSSession session, bool monitorAvailabilityChange)
        {
            RemoteRunspace remoteRunspace = session.Runspace as RemoteRunspace;
            if (remoteRunspace.RunspaceAvailability == RunspaceAvailability.RemoteDebug)
            {
                DisconnectAndStopRunningCmds(remoteRunspace);
                WriteDebugStopWarning();
                return true;
            }

            if (monitorAvailabilityChange)
            {
                // Monitor runspace availability transition to RemoteDebug
                remoteRunspace.AvailabilityChanged += HandleRunspaceAvailabilityChanged;
            }

            return false;
        }

        private void HandleRunspaceAvailabilityChanged(object sender, RunspaceAvailabilityEventArgs e)
        {
            if ((e.RunspaceAvailability == RunspaceAvailability.RemoteDebug))
            {
                RemoteRunspace remoteRunspace = sender as RemoteRunspace;
                remoteRunspace.AvailabilityChanged -= HandleRunspaceAvailabilityChanged;

                DisconnectAndStopRunningCmds(remoteRunspace);
            }
        }

        private void DisconnectAndStopRunningCmds(RemoteRunspace remoteRunspace)
        {
            // Disconnect runspace to stop command from running and to allow reconnect
            // via the Enter-PSSession cmdlet.
            if (remoteRunspace.RunspaceStateInfo.State == RunspaceState.Opened)
            {
                Job job;
                ManualResetEvent stopPipelineReceive;
                lock (_syncObject)
                {
                    job = _job;
                    stopPipelineReceive = _stopPipelineReceive;
                }

                remoteRunspace.Disconnect();

                try
                {
                    stopPipelineReceive?.Set();
                }
                catch (ObjectDisposedException) { }

                job?.StopJob();
            }
        }

        private void WriteDebugStopWarning()
        {
            WriteWarning(
                GetMessage(RemotingErrorIdStrings.ReceivePSSessionInDebugMode));
            WriteObject(string.Empty);
        }

        /// <summary>
        /// Connects session, retrieves command output data and writes to host.
        /// </summary>
        /// <param name="session">PSSession object.</param>
        /// <param name="job">Job object associated with session.</param>
        private void ConnectSessionToHost(PSSession session, PSRemotingJob job = null)
        {
            RemoteRunspace remoteRunspace = session.Runspace as RemoteRunspace;
            Dbg.Assert(remoteRunspace != null, "PS sessions can only contain RemoteRunspace type.");

            if (job != null)
            {
                // If we have a job object associated with the session then this means
                // the user explicitly chose to connect and return data synchronously.

                // Reconnect the job object and stream data to host.
                lock (_syncObject) { _job = job; _stopPipelineReceive = new ManualResetEvent(false); }

                using (_stopPipelineReceive)
                using (job)
                {
                    Job childJob = job.ChildJobs[0];
                    job.ConnectJobs();
                    if (CheckForDebugMode(session, true))
                    {
                        return;
                    }

                    do
                    {
                        // Retrieve and display results from child job as they become
                        // available.
                        int index = WaitHandle.WaitAny(new WaitHandle[] {
                            _stopPipelineReceive,
                            childJob.Results.WaitHandle });

                        foreach (var result in childJob.ReadAll())
                        {
                            result?.WriteStreamObject(this);
                        }

                        if (index == 0)
                        {
                            WriteDebugStopWarning();
                            return;
                        }
                    }
                    while (!job.IsFinishedState(job.JobStateInfo.State));
                }

                lock (_syncObject) { _job = null; _stopPipelineReceive = null; }

                return;
            }

            // Otherwise this must be a new disconnected session object that has a running command
            // associated with it.
            if (remoteRunspace.RemoteCommand == null)
            {
                // There is no associated running command for this runspace, so we cannot proceed.
                // Check to see if session is in debug mode.
                CheckForDebugMode(session, false);
                return;
            }

            // Create a RemotePipeline object for this command and attempt to connect.
            lock (_syncObject)
            {
                _remotePipeline = (RemotePipeline)session.Runspace.CreateDisconnectedPipeline();
                _stopPipelineReceive = new ManualResetEvent(false);
            }

            using (_stopPipelineReceive)
            {
                using (_remotePipeline)
                {
                    // Connect to remote running command.
                    ManualResetEvent pipelineConnectedEvent = new ManualResetEvent(false);
                    using (pipelineConnectedEvent)
                    {
                        _remotePipeline.StateChanged += (sender, args) =>
                            {
                                if (pipelineConnectedEvent != null &&
                                    (args.PipelineStateInfo.State == PipelineState.Running ||
                                     args.PipelineStateInfo.State == PipelineState.Stopped ||
                                     args.PipelineStateInfo.State == PipelineState.Failed))
                                {
                                    pipelineConnectedEvent.Set();
                                }
                            };
                        _remotePipeline.ConnectAsync();
                        pipelineConnectedEvent.WaitOne();
                    }

                    pipelineConnectedEvent = null;

                    if (CheckForDebugMode(session, true))
                    {
                        return;
                    }

                    // Wait for remote command to complete, while writing any available data.
                    while (!_remotePipeline.Output.EndOfPipeline)
                    {
                        if (_stopProcessing)
                        {
                            break;
                        }

                        int index = WaitHandle.WaitAny(new WaitHandle[] {
                            _stopPipelineReceive,
                            _remotePipeline.Output.WaitHandle });

                        if (index == 0)
                        {
                            WriteDebugStopWarning();
                            return;
                        }

                        while (_remotePipeline.Output.Count > 0)
                        {
                            if (_stopProcessing)
                            {
                                break;
                            }

                            PSObject psObject = _remotePipeline.Output.Read();
                            WriteRemoteObject(psObject, session);
                        }
                    }

                    // Write pipeline object errors.
                    if (_remotePipeline.Error.Count > 0)
                    {
                        while (!_remotePipeline.Error.EndOfPipeline)
                        {
                            object errorObj = _remotePipeline.Error.Read();
                            if (errorObj is Collection<ErrorRecord>)
                            {
                                Collection<ErrorRecord> errorCollection = (Collection<ErrorRecord>)errorObj;
                                foreach (ErrorRecord errorRecord in errorCollection)
                                {
                                    WriteError(errorRecord);
                                }
                            }
                            else if (errorObj is ErrorRecord)
                            {
                                WriteError((ErrorRecord)errorObj);
                            }
                            else
                            {
                                Dbg.Assert(false, "Objects in pipeline Error collection must be ErrorRecord type.");
                            }
                        }
                    }

                    // Wait for pipeline to finish.
                    int wIndex = WaitHandle.WaitAny(new WaitHandle[] {
                            _stopPipelineReceive,
                            _remotePipeline.PipelineFinishedEvent });

                    if (wIndex == 0)
                    {
                        WriteDebugStopWarning();
                        return;
                    }

                    // Set the runspace RemoteCommand to null.  It is not needed anymore and it
                    // allows the runspace to become available after pipeline completes.
                    remoteRunspace.RunspacePool.RemoteRunspacePoolInternal.ConnectCommands = null;

                    // Check for any terminating errors to report.
                    if (_remotePipeline.PipelineStateInfo.State == PipelineState.Failed)
                    {
                        Exception reason = _remotePipeline.PipelineStateInfo.Reason;
                        string msg;
                        if (reason != null && !string.IsNullOrEmpty(reason.Message))
                        {
                            msg = StringUtil.Format(RemotingErrorIdStrings.PipelineFailedWithReason, reason.Message);
                        }
                        else
                        {
                            msg = RemotingErrorIdStrings.PipelineFailedWithoutReason;
                        }

                        ErrorRecord errorRecord = new ErrorRecord(new RuntimeException(msg, reason),
                                                            "ReceivePSSessionPipelineFailed",
                                                            ErrorCategory.OperationStopped,
                                                            _remotePipeline
                                                            );

                        WriteError(errorRecord);
                    }
                }
            }

            lock (_syncObject) { _remotePipeline = null; _stopPipelineReceive = null; }
        }

        /// <summary>
        /// Helper method to append computer name and session GUID
        /// note properties to the PSObject before it is written.
        /// </summary>
        /// <param name="psObject">PSObject.</param>
        /// <param name="session">PSSession.</param>
        private void WriteRemoteObject(
            PSObject psObject,
            PSSession session)
        {
            if (psObject == null)
            {
                return;
            }

            // Add note properties for this session if they don't already exist.
            if (psObject.Properties[RemotingConstants.ComputerNameNoteProperty] == null)
            {
                psObject.Properties.Add(new PSNoteProperty(RemotingConstants.ComputerNameNoteProperty, session.ComputerName));
            }

            if (psObject.Properties[RemotingConstants.RunspaceIdNoteProperty] == null)
            {
                psObject.Properties.Add(new PSNoteProperty(RemotingConstants.RunspaceIdNoteProperty, session.InstanceId));
            }

            if (psObject.Properties[RemotingConstants.ShowComputerNameNoteProperty] == null)
            {
                psObject.Properties.Add(new PSNoteProperty(RemotingConstants.ShowComputerNameNoteProperty, true));
            }

            WriteObject(psObject);
        }

        /// <summary>
        /// Connects session, collects command output data in a job object.
        /// If a PSRemotingJob object is passed in then that job will be
        /// (re)connected.  Otherwise a new job object will be created that
        /// will be connected to the session's running command.
        /// </summary>
        /// <param name="session">PSSession object.</param>
        /// <param name="job">Job object to connect to.</param>
        private void ConnectSessionToJob(PSSession session, PSRemotingJob job = null)
        {
            // Otherwise create a new job object in the disconnected state for this
            // session and then connect it.
            bool newJobCreated = false;
            if (job == null)
            {
                // The PSRemoting job object uses helper objects to track remote command execution.
                List<IThrottleOperation> helpers = new List<IThrottleOperation>();

                // Create the remote pipeline object that will represent the running command
                // on the server machine.  This object will be in the disconnected state.
                Pipeline remotePipeline = session.Runspace.CreateDisconnectedPipeline();

                // Create a disconnected runspace helper for this remote command.
                helpers.Add(new DisconnectedJobOperation(remotePipeline));

                // Create the job object in a disconnected state.  Note that the job name
                // will be autogenerated.
                job = new PSRemotingJob(helpers, 0, JobName, false);
                job.PSJobTypeName = InvokeCommandCommand.RemoteJobType;
                job.HideComputerName = false;
                newJobCreated = true;
            }

            if (job.JobStateInfo.State == JobState.Disconnected)
            {
                // Connect the job to the remote command running on the server.
                job.ConnectJob(session.Runspace.InstanceId);

                // Add the created job to the store if it was connected successfully.
                if (newJobCreated)
                {
                    JobRepository.Add(job);
                }
            }

            if (CheckForDebugMode(session, true))
            {
                return;
            }

            // Write the job object to output.
            WriteObject(job);
        }

        /// <summary>
        /// Helper method to connect the runspace.  If the session/runspace can't
        /// be connected or fails to be connected then a null PSSessionobject is
        /// returned.
        /// </summary>
        /// <param name="session">Session to connect.</param>
        /// <param name="ex">Optional exception object.</param>
        /// <returns>Connected session or null.</returns>
        private static PSSession ConnectSession(PSSession session, out Exception ex)
        {
            ex = null;

            if (session == null ||
                (session.Runspace.RunspaceStateInfo.State != RunspaceState.Opened &&
                 session.Runspace.RunspaceStateInfo.State != RunspaceState.Disconnected))
            {
                return null;
            }
            else if (session.Runspace.RunspaceStateInfo.State == RunspaceState.Opened)
            {
                return session;
            }

            try
            {
                session.Runspace.Connect();
            }
            catch (PSInvalidOperationException e)
            {
                ex = e;
            }
            catch (InvalidRunspaceStateException e)
            {
                ex = e;
            }
            catch (RuntimeException e)
            {
                ex = e;
            }

            return (ex == null) ? session : null;
        }

        /// <summary>
        /// Helper method to attempt to retrieve a disconnected runspace object
        /// from the server, based on the provided session object.
        /// </summary>
        /// <param name="session">PSSession session object.</param>
        /// <returns>PSSession disconnected runspace object.</returns>
        private PSSession TryGetSessionFromServer(PSSession session)
        {
            if (session.Runspace is not RemoteRunspace remoteRunspace)
            {
                return null;
            }

            remoteRunspace = null;
            Runspace[] runspaces = Runspace.GetRunspaces(session.Runspace.ConnectionInfo, this.Host, QueryRunspaces.BuiltInTypesTable);
            foreach (Runspace runspace in runspaces)
            {
                if (runspace.InstanceId == session.Runspace.InstanceId)
                {
                    remoteRunspace = runspace as RemoteRunspace;
                    break;
                }
            }

            if (remoteRunspace != null)
            {
                // Try inserting connected runspace into existing PSSession.
                session = session.InsertRunspace(remoteRunspace) ? session : new PSSession(remoteRunspace);
                return session;
            }

            return null;
        }

        /// <summary>
        /// Helper method to search the local PS client job repository
        /// for a job associated with the provided session.
        /// </summary>
        /// <param name="session">PSSession object.</param>
        /// <returns>Associated job object from the job repository.</returns>
        private PSRemotingJob FindJobForSession(PSSession session)
        {
            PSRemotingJob job = null;
            RemoteRunspace remoteSessionRunspace = session.Runspace as RemoteRunspace;

            if (remoteSessionRunspace == null ||
                remoteSessionRunspace.RemoteCommand != null)
            {
                // The provided session is created for *reconstruction* and we
                // cannot connect a previous job even if one exists.  A new job
                // will have to be created.
                return null;
            }

            foreach (Job repJob in this.JobRepository.Jobs)
            {
                if (repJob is PSRemotingJob)
                {
                    foreach (PSRemotingChildJob childJob in repJob.ChildJobs)
                    {
                        if (childJob.Runspace.InstanceId.Equals(session.InstanceId) &&
                            (childJob.JobStateInfo.State == JobState.Disconnected))
                        {
                            job = (PSRemotingJob)repJob;
                            break;
                        }
                    }

                    if (job != null)
                    {
                        break;
                    }
                }
            }

            return job;
        }

        /// <summary>
        /// Searches runspace repository for session by Id.
        /// </summary>
        /// <param name="id">Id to match.</param>
        /// <returns>PSSession object.</returns>
        private PSSession GetSessionById(int id)
        {
            foreach (PSSession session in this.RunspaceRepository.Runspaces)
            {
                if (session.Id == id)
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches runspace repository for session by Name.
        /// </summary>
        /// <param name="name">Name to match.</param>
        /// <returns>PSSession object.</returns>
        private PSSession GetSessionByName(string name)
        {
            WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
            foreach (PSSession session in this.RunspaceRepository.Runspaces)
            {
                if (namePattern.IsMatch(session.Name))
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches runspace repository for session by InstanceId.
        /// </summary>
        /// <param name="instanceId">InstanceId to match.</param>
        /// <returns>PSSession object.</returns>
        private PSSession GetSessionByInstanceId(Guid instanceId)
        {
            foreach (PSSession session in this.RunspaceRepository.Runspaces)
            {
                if (instanceId.Equals(session.InstanceId))
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>
        /// Write invalid argument error.
        /// </summary>
        private void WriteInvalidArgumentError(PSRemotingErrorId errorId, string resourceString, object errorArgument)
        {
            string message = GetMessage(resourceString, errorArgument);

            WriteError(new ErrorRecord(new ArgumentException(message), errorId.ToString(),
                       ErrorCategory.InvalidArgument, errorArgument));
        }

        #endregion

        #region Private Members

        private bool _stopProcessing;
        private RemotePipeline _remotePipeline;
        private Job _job;
        private ManualResetEvent _stopPipelineReceive;
        private readonly object _syncObject = new object();

        #endregion
    }

    #region OutTarget Enum

    /// <summary>
    /// Output modes available to the Receive-PSSession cmdlet.
    /// </summary>
    public enum OutTarget
    {
        /// <summary>
        /// Default mode.  If.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Synchronous mode.  Receive-PSSession output data goes to host (returned by cmdlet object).
        /// </summary>
        Host = 1,

        /// <summary>
        /// Asynchronous mode.  Receive-PSSession output data goes to returned job object.
        /// </summary>
        Job = 2
    }

    #endregion
}
