// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet is used to retrieve runspaces from the global cache
    /// and write it to the pipeline. The runspaces are wrapped and
    /// returned as PSSession objects.
    ///
    /// The cmdlet can be used in the following ways:
    ///
    /// List all the available runspaces
    ///     get-pssession
    ///
    /// Get the PSSession from session name
    ///     get-pssession -Name sessionName
    ///
    /// Get the PSSession for the specified ID
    ///     get-pssession -Id sessionId
    ///
    /// Get the PSSession for the specified instance Guid
    ///     get-pssession -InstanceId sessionGuid
    ///
    /// Get PSSessions from remote computer.  Optionally filter on state, session instanceid or session name.
    ///     get-psession -ComputerName computerName -StateFilter Disconnected
    ///
    /// Get PSSessions from virtual machine. Optionally filter on state, session instanceid or session name.
    ///     get-psession -VMName vmName -Name sessionName
    ///
    /// Get PSSessions from container. Optionally filter on state, session instanceid or session name.
    ///     get-psession -ContainerId containerId -InstanceId instanceId.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSSession", DefaultParameterSetName = PSRunspaceCmdlet.NameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096697", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(PSSession))]
    public class GetPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        #region Parameters

        private const string ConnectionUriParameterSet = "ConnectionUri";
        private const string ConnectionUriInstanceIdParameterSet = "ConnectionUriInstanceId";

        /// <summary>
        /// Computer names to connect to.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
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
                   ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
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
        /// A complete URI(s) specified for the remote computer and shell to
        /// connect to and create a runspace for.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Uri[] ConnectionUri { get; set; }

        /// <summary>
        /// For WSMan sessions:
        /// If this parameter is not specified then the value specified in
        /// the environment variable DEFAULTREMOTESHELLNAME will be used. If
        /// this is not set as well, then Microsoft.PowerShell is used.
        ///
        /// For VM/Container sessions:
        /// If this parameter is not specified then all sessions that match other filters are returned.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet)]
        public string ConfigurationName { get; set; }

        /// <summary>
        /// The AllowRedirection parameter enables the implicit redirection functionality.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public SwitchParameter AllowRedirection
        {
            get { return _allowRedirection; }

            set { _allowRedirection = value; }
        }

        private bool _allowRedirection = false;

        /// <summary>
        /// Session names to filter on.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] Name
        {
            get { return base.Name; }

            set { base.Name = value; }
        }

        /// <summary>
        /// Instance Ids to filter on.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet,
                   Mandatory = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public override Guid[] InstanceId
        {
            get { return base.InstanceId; }

            set { base.InstanceId = value; }
        }

        /// <summary>
        /// Specifies the credentials of the user to impersonate in the
        /// remote machine. If this parameter is not specified then the
        /// credentials of the current user process will be assumed.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
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
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public int Port { get; set; }

        /// <summary>
        /// This parameter suggests that the transport scheme to be used for
        /// remote connections is useSSL instead of the default http.Since
        /// there are only two possible transport schemes that are possible
        /// at this point, a SwitchParameter is being used to switch between
        /// the two.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL { get; set; }

        /// <summary>
        /// Allows the user of the cmdlet to specify a throttling value
        /// for throttling the number of remote operations that can
        /// be executed simultaneously.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public int ThrottleLimit { get; set; } = 0;

        /// <summary>
        /// Filters returned remote runspaces based on runspace state.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet)]
        public SessionFilterState State { get; set; }

        /// <summary>
        /// Session options.
        /// </summary>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public PSSessionOption SessionOption { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// Resolves shellname.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            ConfigurationName ??= string.Empty;
        }

        /// <summary>
        /// Get the list of runspaces from the global cache and write them
        /// down. If no computername or instance id is specified then
        /// list all runspaces.
        /// </summary>
        protected override void ProcessRecord()
        {
            if ((ParameterSetName == GetPSSessionCommand.NameParameterSet) && ((Name == null) || (Name.Length == 0)))
            {
                // that means Get-PSSession (with no parameters)..so retrieve all the runspaces.
                GetAllRunspaces(true, true);
            }
            else if (ParameterSetName == GetPSSessionCommand.ComputerNameParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ComputerInstanceIdParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
            {
                // Perform the remote query for each provided computer name.
                QueryForRemoteSessions();
            }
            else
            {
                GetMatchingRunspaces(true, true, this.State, this.ConfigurationName);
            }
        }

        /// <summary>
        /// End processing clean up.
        /// </summary>
        protected override void EndProcessing()
        {
            _stream.ObjectWriter.Close();
        }

        /// <summary>
        /// User has signaled a stop for this cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            _queryRunspaces.StopAllOperations();
        }

        #endregion Overrides

        #region Private Methods

        /// <summary>
        /// Creates a connectionInfo object for each computer name and performs a remote
        /// session query for each computer filtered by the filterState parameter.
        /// </summary>
        private void QueryForRemoteSessions()
        {
            // Get collection of connection objects for each computer name or
            // connection uri.
            Collection<WSManConnectionInfo> connectionInfos = GetConnectionObjects();

            // Query for sessions.
            Collection<PSSession> results = _queryRunspaces.GetDisconnectedSessions(connectionInfos, this.Host, _stream,
                                                                                        this.RunspaceRepository, ThrottleLimit,
                                                                                        State, InstanceId, Name, ConfigurationName);

            // Write any error output from stream object.
            Collection<object> streamObjects = _stream.ObjectReader.NonBlockingRead();
            foreach (object streamObject in streamObjects)
            {
                if (this.IsStopping)
                {
                    break;
                }

                WriteStreamObject((Action<Cmdlet>)streamObject);
            }

            // Write each session object.
            foreach (PSSession session in results)
            {
                if (this.IsStopping)
                {
                    break;
                }

                WriteObject(session);
            }
        }

        private Collection<WSManConnectionInfo> GetConnectionObjects()
        {
            Collection<WSManConnectionInfo> connectionInfos = new Collection<WSManConnectionInfo>();

            if (ParameterSetName == GetPSSessionCommand.ComputerNameParameterSet ||
                ParameterSetName == GetPSSessionCommand.ComputerInstanceIdParameterSet)
            {
                string scheme = UseSSL.IsPresent ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;

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
            else if (ParameterSetName == GetPSSessionCommand.ConnectionUriParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
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
            if (ParameterSetName != GetPSSessionCommand.ConnectionUriParameterSet &&
                ParameterSetName != GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
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

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose method of IDisposable.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        // Object used for querying remote runspaces.
        private readonly QueryRunspaces _queryRunspaces = new QueryRunspaces();

        // Object to collect output data from multiple threads.
        private readonly ObjectStream _stream = new ObjectStream();

        #endregion
    }
}
