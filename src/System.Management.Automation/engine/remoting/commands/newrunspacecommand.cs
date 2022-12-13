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
using System.Management.Automation.Runspaces.Internal;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet establishes a new Runspace either on the local machine or
    /// on the specified remote machine(s). The runspace established can be used
    /// to invoke expressions remotely.
    ///
    /// The cmdlet can be used in the following ways:
    ///
    /// Open a local runspace
    /// $rs = New-PSSession
    ///
    /// Open a runspace to a remote system.
    /// $rs = New-PSSession -Machine PowerShellWorld
    ///
    /// Create a runspace specifying that it is globally scoped.
    /// $global:rs = New-PSSession -Machine PowerShellWorld
    ///
    /// Create a collection of runspaces
    /// $runspaces = New-PSSession -Machine PowerShellWorld,PowerShellPublish,PowerShellRepo
    ///
    /// Create a set of Runspaces using the Secure Socket Layer by specifying the URI form.
    /// This assumes that an shell by the name of E12 exists on the remote server.
    ///     $serverURIs = 1..8 | ForEach-Object { "SSL://server${_}:443/E12" }
    ///     $rs = New-PSSession -URI $serverURIs
    ///
    /// Create a runspace by connecting to port 8081 on servers s1, s2 and s3
    /// $rs = New-PSSession -computername s1,s2,s3 -port 8081
    ///
    /// Create a runspace by connecting to port 443 using ssl on servers s1, s2 and s3
    /// $rs = New-PSSession -computername s1,s2,s3 -port 443 -useSSL
    ///
    /// Create a runspace by connecting to port 8081 on server s1 and run shell named E12.
    /// This assumes that a shell by the name E12 exists on the remote server
    /// $rs = New-PSSession -computername s1 -port 8061 -ShellName E12.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSSession", DefaultParameterSetName = "ComputerName",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096484", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(PSSession))]
    public class NewPSSessionCommand : PSRemotingBaseCmdlet, IDisposable
    {
        #region Parameters

        /// <summary>
        /// This parameter represents the address(es) of the remote
        /// computer(s). The following formats are supported:
        ///      (a) Computer name
        ///      (b) IPv4 address : 132.3.4.5
        ///      (c) IPv6 address: 3ffe:8311:ffff:f70f:0:5efe:172.30.162.18.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.ComputerNameParameterSet)]
        [Alias("Cn")]
        [ValidateNotNullOrEmpty]
        public override string[] ComputerName { get; set; }

        /// <summary>
        /// Specifies the credentials of the user to impersonate in the
        /// remote machine. If this parameter is not specified then the
        /// credentials of the current user process will be assumed.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMIdParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMNameParameterSet)]
        [Credential()]
        public override PSCredential Credential
        {
            get
            {
                return base.Credential;
            }

            set
            {
                base.Credential = value;
            }
        }

        /// <summary>
        /// The PSSession object describing the remote runspace
        /// using which the specified cmdlet operation will be performed.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = NewPSSessionCommand.SessionParameterSet)]
        [ValidateNotNullOrEmpty]
        public override PSSession[] Session
        {
            get
            {
                return _remoteRunspaceInfos;
            }

            set
            {
                _remoteRunspaceInfos = value;
            }
        }

        private PSSession[] _remoteRunspaceInfos;

        /// <summary>
        /// Friendly names for the new PSSessions.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name { get; set; }

        /// <summary>
        /// When set and in loopback scenario (localhost) this enables creation of WSMan
        /// host process with the user interactive token, allowing PowerShell script network access,
        /// i.e., allows going off box.  When this property is true and a PSSession is disconnected,
        /// reconnection is allowed only if reconnecting from a PowerShell session on the same box.
        /// </summary>
        [Parameter(ParameterSetName = NewPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = NewPSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = NewPSSessionCommand.UriParameterSet)]
        public SwitchParameter EnableNetworkAccess { get; set; }

        /// <summary>
        /// For WSMan sessions:
        /// If this parameter is not specified then the value specified in
        /// the environment variable DEFAULTREMOTESHELLNAME will be used. If
        /// this is not set as well, then Microsoft.PowerShell is used.
        ///
        /// For VM/Container sessions:
        /// If this parameter is not specified then no configuration is used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.UriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = NewPSSessionCommand.VMNameParameterSet)]
        public string ConfigurationName { get; set; }

        /// <summary>
        /// Gets or sets parameter value that creates connection to a Windows PowerShell process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = NewPSSessionCommand.UseWindowsPowerShellParameterSet)]
        public SwitchParameter UseWindowsPowerShell { get; set; }

        #endregion Parameters

        #region Cmdlet Overrides

        /// <summary>
        /// The throttle limit will be set here as it needs to be done
        /// only once per cmdlet and not for every call.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _operationsComplete.Reset();
            _throttleManager.ThrottleLimit = ThrottleLimit;
            _throttleManager.ThrottleComplete += HandleThrottleComplete;

            if (string.IsNullOrEmpty(ConfigurationName))
            {
                if ((ParameterSetName == NewPSSessionCommand.ComputerNameParameterSet) ||
                    (ParameterSetName == NewPSSessionCommand.UriParameterSet))
                {
                    // set to default value for WSMan session
                    ConfigurationName = ResolveShell(null);
                }
                else
                {
                    // convert null to string.Empty for VM/Container session
                    ConfigurationName = string.Empty;
                }
            }
        }

        /// <summary>
        /// The runspace objects will be created using OpenAsync.
        /// At the end, the method will check if any runspace
        /// opened has already become available. If so, then it
        /// will be written to the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<RemoteRunspace> remoteRunspaces = null;
            List<IThrottleOperation> operations = new List<IThrottleOperation>();

            switch (ParameterSetName)
            {
                case NewPSSessionCommand.SessionParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesWhenRunspaceParameterSpecified();
                    }

                    break;

                case "Uri":
                    {
                        remoteRunspaces = CreateRunspacesWhenUriParameterSpecified();
                    }

                    break;

                case NewPSSessionCommand.ComputerNameParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesWhenComputerNameParameterSpecified();
                    }

                    break;

                case NewPSSessionCommand.VMIdParameterSet:
                case NewPSSessionCommand.VMNameParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesWhenVMParameterSpecified();
                    }

                    break;

                case NewPSSessionCommand.ContainerIdParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesWhenContainerParameterSpecified();
                    }

                    break;

                case NewPSSessionCommand.SSHHostParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesForSSHHostParameterSet();
                    }

                    break;

                case NewPSSessionCommand.SSHHostHashParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesForSSHHostHashParameterSet();
                    }

                    break;

                case NewPSSessionCommand.UseWindowsPowerShellParameterSet:
                    {
                        remoteRunspaces = CreateRunspacesForUseWindowsPowerShellParameterSet();
                    }

                    break;

                default:
                    {
                        Dbg.Assert(false, "Missing parameter set in switch statement");
                        remoteRunspaces = new List<RemoteRunspace>(); // added to avoid prefast warning
                    }

                    break;
            }

            foreach (RemoteRunspace remoteRunspace in remoteRunspaces)
            {
                remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;

                OpenRunspaceOperation operation = new OpenRunspaceOperation(remoteRunspace);
                // HandleRunspaceStateChanged callback is added before ThrottleManager complete
                // callback handlers so HandleRunspaceStateChanged will always be called first.
                operation.OperationComplete += HandleRunspaceStateChanged;
                remoteRunspace.URIRedirectionReported += HandleURIDirectionReported;
                operations.Add(operation);
            }

            // submit list of operations to throttle manager to start opening
            // runspaces
            _throttleManager.SubmitOperations(operations);

            // Add to list for clean up.
            _allOperations.Add(operations);

            // If there are any runspaces opened asynchronously
            // that are ready now, check their status and do
            // necessary action. If there are any error records
            // or verbose messages write them as well
            Collection<object> streamObjects =
                _stream.ObjectReader.NonBlockingRead();

            foreach (object streamObject in streamObjects)
            {
                WriteStreamObject((Action<Cmdlet>)streamObject);
            }
        }

        /// <summary>
        /// OpenAsync would have been called from ProcessRecord. This method
        /// will wait until all runspaces are opened and then write them to
        /// the pipeline as and when they become available.
        /// </summary>
        protected override void EndProcessing()
        {
            // signal to throttle manager end of submit operations
            _throttleManager.EndSubmitOperations();

            while (true)
            {
                // Keep reading objects until end of pipeline is encountered
                _stream.ObjectReader.WaitHandle.WaitOne();

                if (!_stream.ObjectReader.EndOfPipeline)
                {
                    object streamObject = _stream.ObjectReader.Read();
                    WriteStreamObject((Action<Cmdlet>)streamObject);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// This method is called when the user sends a stop signal to the
        /// cmdlet. The cmdlet will not exit until it has completed
        /// creating all the runspaces (basically the runspaces its
        /// waiting on OpenAsync is made available). However, when a stop
        /// signal is sent, CloseAsyn needs to be called to close all the
        /// pending runspaces.
        /// </summary>
        /// <remarks>This is called from a separate thread so need to worry
        /// about concurrency issues
        /// </remarks>
        protected override void StopProcessing()
        {
            // close the outputStream so that further writes to the outputStream
            // are not possible
            _stream.ObjectWriter.Close();

            // for all the runspaces that have been submitted for opening
            // call StopOperation on each object and quit
            _throttleManager.StopAllOperations();
        }

        #endregion Cmdlet Overrides

        #region IDisposable Overrides

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

        #endregion IDisposable Overrides

        #region Private Methods

        /// <summary>
        /// Adds forwarded events to the local queue.
        /// </summary>
        private void OnRunspacePSEventReceived(object sender, PSEventArgs e) => this.Events?.AddForwardedEvent(e);

        /// <summary>
        /// When the client remote session reports a URI redirection, this method will report the
        /// message to the user as a Warning using Host method calls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleURIDirectionReported(object sender, RemoteDataEventArgs<Uri> eventArgs)
        {
            string message = StringUtil.Format(RemotingErrorIdStrings.URIRedirectWarningToHost, eventArgs.Data.OriginalString);
            Action<Cmdlet> warningWriter = (Cmdlet cmdlet) => cmdlet.WriteWarning(message);
            _stream.Write(warningWriter);
        }

        /// <summary>
        /// Handles state changes for Runspace.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="stateEventArgs">Event information object which describes
        /// the event which triggered this method</param>
        private void HandleRunspaceStateChanged(object sender, OperationStateEventArgs stateEventArgs)
        {
            if (sender == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sender));
            }

            if (stateEventArgs == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(stateEventArgs));
            }

            RunspaceStateEventArgs runspaceStateEventArgs =
                        stateEventArgs.BaseEvent as RunspaceStateEventArgs;
            RunspaceStateInfo stateInfo = runspaceStateEventArgs.RunspaceStateInfo;
            RunspaceState state = stateInfo.State;
            OpenRunspaceOperation operation = sender as OpenRunspaceOperation;
            RemoteRunspace remoteRunspace = operation.OperatedRunspace;

            // since we got state changed event..we dont need to listen on
            // URI redirections anymore
            if (remoteRunspace != null)
            {
                remoteRunspace.URIRedirectionReported -= HandleURIDirectionReported;
            }

            PipelineWriter writer = _stream.ObjectWriter;
            Exception reason = runspaceStateEventArgs.RunspaceStateInfo.Reason;

            switch (state)
            {
                case RunspaceState.Opened:
                    {
                        // Indicates that runspace is successfully opened
                        // Write it to PipelineWriter to be handled in
                        // HandleRemoteRunspace
                        PSSession remoteRunspaceInfo = new PSSession(remoteRunspace);

                        this.RunspaceRepository.Add(remoteRunspaceInfo);

                        Action<Cmdlet> outputWriter = (Cmdlet cmdlet) => cmdlet.WriteObject(remoteRunspaceInfo);
                        if (writer.IsOpen)
                        {
                            writer.Write(outputWriter);
                        }
                    }

                    break;

                case RunspaceState.Broken:
                    {
                        // Open resulted in a broken state. Extract reason
                        // and write an error record

                        // set the transport message in the error detail so that
                        // the user can directly get to see the message without
                        // having to mine through the error record details
                        PSRemotingTransportException transException =
                            reason as PSRemotingTransportException;
                        string errorDetails = null;
                        int transErrorCode = 0;
                        if (transException != null)
                        {
                            OpenRunspaceOperation senderAsOp = sender as OpenRunspaceOperation;
                            transErrorCode = transException.ErrorCode;
                            if (senderAsOp != null)
                            {
                                string host = senderAsOp.OperatedRunspace.ConnectionInfo.ComputerName;

                                if (transException.ErrorCode ==
                                    System.Management.Automation.Remoting.Client.WSManNativeApi.ERROR_WSMAN_REDIRECT_REQUESTED)
                                {
                                    // Handling a special case for redirection..we should talk about
                                    // AllowRedirection parameter and WSManMaxRedirectionCount preference
                                    // variables
                                    string message = PSRemotingErrorInvariants.FormatResourceString(
                                        RemotingErrorIdStrings.URIRedirectionReported,
                                        transException.Message,
                                        "MaximumConnectionRedirectionCount",
                                        Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.DEFAULT_SESSION_OPTION,
                                        "AllowRedirection");

                                    errorDetails = "[" + host + "] " + message;
                                }
                                else
                                {
                                    errorDetails = "[" + host + "] ";
                                    if (!string.IsNullOrEmpty(transException.Message))
                                    {
                                        errorDetails += transException.Message;
                                    }
                                    else if (!string.IsNullOrEmpty(transException.TransportMessage))
                                    {
                                        errorDetails += transException.TransportMessage;
                                    }
                                }
                            }
                        }

                        // add host identification information in data structure handler message
                        PSRemotingDataStructureException protoException = reason as PSRemotingDataStructureException;

                        if (protoException != null)
                        {
                            OpenRunspaceOperation senderAsOp = sender as OpenRunspaceOperation;

                            if (senderAsOp != null)
                            {
                                string host = senderAsOp.OperatedRunspace.ConnectionInfo.ComputerName;

                                errorDetails = "[" + host + "] " + protoException.Message;
                            }
                        }

                        reason ??= new RuntimeException(this.GetMessage(RemotingErrorIdStrings.RemoteRunspaceOpenUnknownState, state));

                        string fullyQualifiedErrorId = WSManTransportManagerUtils.GetFQEIDFromTransportError(
                            transErrorCode,
                            _defaultFQEID);

                        if (transErrorCode == WSManNativeApi.ERROR_WSMAN_NO_LOGON_SESSION_EXIST)
                        {
                            errorDetails += System.Environment.NewLine + string.Format(System.Globalization.CultureInfo.CurrentCulture, RemotingErrorIdStrings.RemotingErrorNoLogonSessionExist);
                        }

                        ErrorRecord errorRecord = new ErrorRecord(reason,
                             remoteRunspace, fullyQualifiedErrorId,
                                   ErrorCategory.OpenError, null, null,
                                        null, null, null, errorDetails, null);

                        Action<Cmdlet> errorWriter = (Cmdlet cmdlet) =>
                        {
                            //
                            // In case of PSDirectException, we should output the precise error message
                            // in inner exception instead of the generic one in outer exception.
                            //
                            if ((errorRecord.Exception != null) &&
                                (errorRecord.Exception.InnerException != null))
                            {
                                PSDirectException ex = errorRecord.Exception.InnerException as PSDirectException;
                                if (ex != null)
                                {
                                    errorRecord = new ErrorRecord(errorRecord.Exception.InnerException,
                                                                  errorRecord.FullyQualifiedErrorId,
                                                                  errorRecord.CategoryInfo.Category,
                                                                  errorRecord.TargetObject);
                                }
                            }

                            cmdlet.WriteError(errorRecord);
                        };
                        if (writer.IsOpen)
                        {
                            writer.Write(errorWriter);
                        }

                        _toDispose.Add(remoteRunspace);
                    }

                    break;

                case RunspaceState.Closed:
                    {
                        // The runspace was closed possibly because the user
                        // hit ctrl-C when runspaces were being opened or Dispose has been
                        // called when there are open runspaces
                        Uri connectionUri = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<Uri>(remoteRunspace.ConnectionInfo,
                            "ConnectionUri", null);
                        string message =
                            GetMessage(RemotingErrorIdStrings.RemoteRunspaceClosed,
                                        (connectionUri != null) ?
                                        connectionUri.AbsoluteUri : string.Empty);

                        Action<Cmdlet> verboseWriter = (Cmdlet cmdlet) => cmdlet.WriteVerbose(message);
                        if (writer.IsOpen)
                        {
                            writer.Write(verboseWriter);
                        }

                        // runspace may not have been opened in certain cases
                        // like when the max memory is set to 25MB, in such
                        // cases write an error record
                        if (reason != null)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(reason,
                                 "PSSessionStateClosed",
                                       ErrorCategory.OpenError, remoteRunspace);

                            Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
                            if (writer.IsOpen)
                            {
                                writer.Write(errorWriter);
                            }
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Creates the remote runspace objects when PSSession
        /// parameter is specified
        /// It now supports PSSession based on VM/container connection info as well.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private List<RemoteRunspace> CreateRunspacesWhenRunspaceParameterSpecified()
        {
            List<RemoteRunspace> remoteRunspaces = new List<RemoteRunspace>();

            // validate the runspaces specified before processing them.
            // The function will result in terminating errors, if any
            // validation failure is encountered
            ValidateRemoteRunspacesSpecified();

            int rsIndex = 0;
            foreach (PSSession remoteRunspaceInfo in _remoteRunspaceInfos)
            {
                if (remoteRunspaceInfo == null || remoteRunspaceInfo.Runspace == null)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ArgumentNullException("PSSession"), "PSSessionArgumentNull",
                            ErrorCategory.InvalidArgument, null));
                }
                else
                {
                    // clone the object based on what's specified in the input parameter
                    try
                    {
                        RemoteRunspace remoteRunspace = (RemoteRunspace)remoteRunspaceInfo.Runspace;
                        RunspaceConnectionInfo newConnectionInfo = null;

                        if (remoteRunspace.ConnectionInfo is VMConnectionInfo)
                        {
                            newConnectionInfo = remoteRunspace.ConnectionInfo.Clone();
                        }
                        else if (remoteRunspace.ConnectionInfo is ContainerConnectionInfo)
                        {
                            ContainerConnectionInfo newContainerConnectionInfo = remoteRunspace.ConnectionInfo.Clone() as ContainerConnectionInfo;
                            newContainerConnectionInfo.CreateContainerProcess();
                            newConnectionInfo = newContainerConnectionInfo;
                        }
                        else
                        {
                            // WSMan case
                            WSManConnectionInfo originalWSManConnectionInfo = remoteRunspace.ConnectionInfo as WSManConnectionInfo;
                            WSManConnectionInfo newWSManConnectionInfo = null;

                            if (originalWSManConnectionInfo != null)
                            {
                                newWSManConnectionInfo = originalWSManConnectionInfo.Copy();
                                newWSManConnectionInfo.EnableNetworkAccess = newWSManConnectionInfo.EnableNetworkAccess || EnableNetworkAccess;
                                newConnectionInfo = newWSManConnectionInfo;
                            }
                            else
                            {
                                Uri connectionUri = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<Uri>(remoteRunspace.ConnectionInfo,
                                            "ConnectionUri", null);
                                string shellUri = WSManConnectionInfo.ExtractPropertyAsWsManConnectionInfo<string>(remoteRunspace.ConnectionInfo,
                                    "ShellUri", string.Empty);
                                newWSManConnectionInfo = new WSManConnectionInfo(connectionUri,
                                                                shellUri,
                                                                remoteRunspace.ConnectionInfo.Credential);
                                UpdateConnectionInfo(newWSManConnectionInfo);
                                newWSManConnectionInfo.EnableNetworkAccess = EnableNetworkAccess;
                                newConnectionInfo = newWSManConnectionInfo;
                            }
                        }

                        RemoteRunspacePoolInternal rrsPool = remoteRunspace.RunspacePool.RemoteRunspacePoolInternal;
                        TypeTable typeTable = null;
                        if ((rrsPool != null) &&
                            (rrsPool.DataStructureHandler != null) &&
                            (rrsPool.DataStructureHandler.TransportManager != null))
                        {
                            typeTable = rrsPool.DataStructureHandler.TransportManager.Fragmentor.TypeTable;
                        }

                        // Create new remote runspace with name and Id.
                        int rsId;
                        string rsName = GetRunspaceName(rsIndex, out rsId);
                        RemoteRunspace newRemoteRunspace = new RemoteRunspace(
                            typeTable, newConnectionInfo, this.Host, this.SessionOption.ApplicationArguments,
                            rsName, rsId);

                        remoteRunspaces.Add(newRemoteRunspace);
                    }
                    catch (UriFormatException e)
                    {
                        PipelineWriter writer = _stream.ObjectWriter;

                        ErrorRecord errorRecord = new ErrorRecord(e, "CreateRemoteRunspaceFailed",
                                ErrorCategory.InvalidArgument, remoteRunspaceInfo);

                        Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
                        writer.Write(errorWriter);
                    }
                }

                ++rsIndex;
            }

            return remoteRunspaces;
        }

        /// <summary>
        /// Creates the remote runspace objects when the URI parameter
        /// is specified.
        /// </summary>
        private List<RemoteRunspace> CreateRunspacesWhenUriParameterSpecified()
        {
            List<RemoteRunspace> remoteRunspaces = new List<RemoteRunspace>();

            // parse the Uri to obtain information about the runspace
            // required
            for (int i = 0; i < ConnectionUri.Length; i++)
            {
                try
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.ConnectionUri = ConnectionUri[i];
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

                    connectionInfo.EnableNetworkAccess = EnableNetworkAccess;

                    // Create new remote runspace with name and Id.
                    int rsId;
                    string rsName = GetRunspaceName(i, out rsId);
                    RemoteRunspace remoteRunspace = new RemoteRunspace(
                        Utils.GetTypeTableFromExecutionContextTLS(), connectionInfo, this.Host,
                        this.SessionOption.ApplicationArguments, rsName, rsId);

                    Dbg.Assert(remoteRunspace != null,
                            "RemoteRunspace object created using URI is null");

                    remoteRunspaces.Add(remoteRunspace);
                }
                catch (UriFormatException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                }
                catch (InvalidOperationException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                }
                catch (ArgumentException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                }
                catch (NotSupportedException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                }
            }

            return remoteRunspaces;
        }

        /// <summary>
        /// Creates the remote runspace objects when the ComputerName parameter
        /// is specified.
        /// </summary>
        private List<RemoteRunspace> CreateRunspacesWhenComputerNameParameterSpecified()
        {
            List<RemoteRunspace> remoteRunspaces =
                new List<RemoteRunspace>();

            // Resolve all the machine names
            string[] resolvedComputerNames;

            ResolveComputerNames(ComputerName, out resolvedComputerNames);

            ValidateComputerName(resolvedComputerNames);

            // Do for each machine
            for (int i = 0; i < resolvedComputerNames.Length; i++)
            {
                try
                {
                    WSManConnectionInfo connectionInfo = null;
                    connectionInfo = new WSManConnectionInfo();
                    string scheme = UseSSL.IsPresent ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;
                    connectionInfo.ComputerName = resolvedComputerNames[i];
                    connectionInfo.Port = Port;
                    connectionInfo.AppName = ApplicationName;
                    connectionInfo.ShellUri = ConfigurationName;
                    connectionInfo.Scheme = scheme;
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

                    connectionInfo.EnableNetworkAccess = EnableNetworkAccess;

                    // Create new remote runspace with name and Id.
                    int rsId;
                    string rsName = GetRunspaceName(i, out rsId);
                    RemoteRunspace runspace = new RemoteRunspace(
                        Utils.GetTypeTableFromExecutionContextTLS(), connectionInfo, this.Host,
                        this.SessionOption.ApplicationArguments, rsName, rsId);

                    remoteRunspaces.Add(runspace);
                }
                catch (UriFormatException e)
                {
                    PipelineWriter writer = _stream.ObjectWriter;

                    ErrorRecord errorRecord = new ErrorRecord(e, "CreateRemoteRunspaceFailed",
                            ErrorCategory.InvalidArgument, resolvedComputerNames[i]);

                    Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
                    writer.Write(errorWriter);
                }
            }

            return remoteRunspaces;
        }

        /// <summary>
        /// Creates the remote runspace objects when the VMId or VMName parameter
        /// is specified.
        /// </summary>
        private List<RemoteRunspace> CreateRunspacesWhenVMParameterSpecified()
        {
            int inputArraySize;
            bool isVMIdSet = false;
            int index;
            string command;
            Collection<PSObject> results;
            List<RemoteRunspace> remoteRunspaces = new List<RemoteRunspace>();

            if (ParameterSetName == PSExecutionCmdlet.VMIdParameterSet)
            {
                isVMIdSet = true;
                inputArraySize = this.VMId.Length;
                this.VMName = new string[inputArraySize];
                command = "Get-VM -Id $args[0]";
            }
            else
            {
                Dbg.Assert((ParameterSetName == PSExecutionCmdlet.VMNameParameterSet),
                           "Expected ParameterSetName == VMId or VMName");

                inputArraySize = this.VMName.Length;
                this.VMId = new Guid[inputArraySize];
                command = "Get-VM -Name $args";
            }

            for (index = 0; index < inputArraySize; index++)
            {
                try
                {
                    results = this.InvokeCommand.InvokeScript(
                        command, false, PipelineResultTypes.None, null,
                        isVMIdSet ? this.VMId[index].ToString() : this.VMName[index]);
                }
                catch (CommandNotFoundException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new ArgumentException(RemotingErrorIdStrings.HyperVModuleNotAvailable),
                            nameof(PSRemotingErrorId.HyperVModuleNotAvailable),
                            ErrorCategory.NotInstalled,
                            null));

                    return null;
                }

                // handle invalid input
                if (results.Count != 1)
                {
                    if (isVMIdSet)
                    {
                        this.VMName[index] = string.Empty;

                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMIdNotSingle,
                                                                 this.VMId[index].ToString(null))),
                                nameof(PSRemotingErrorId.InvalidVMIdNotSingle),
                                ErrorCategory.InvalidArgument,
                                null));

                        continue;
                    }
                    else
                    {
                        this.VMId[index] = Guid.Empty;

                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMNameNotSingle,
                                                                 this.VMName[index])),
                                nameof(PSRemotingErrorId.InvalidVMNameNotSingle),
                                ErrorCategory.InvalidArgument,
                                null));

                        continue;
                    }
                }
                else
                {
                    this.VMId[index] = (Guid)results[0].Properties["VMId"].Value;
                    this.VMName[index] = (string)results[0].Properties["VMName"].Value;

                    //
                    // VM should be in running state.
                    //
                    if ((VMState)results[0].Properties["State"].Value != VMState.Running)
                    {
                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMState,
                                                                 this.VMName[index])),
                                nameof(PSRemotingErrorId.InvalidVMState),
                                ErrorCategory.InvalidArgument,
                                null));

                        continue;
                    }
                }

                // create helper objects for VM GUIDs or names
                RemoteRunspace runspace = null;
                VMConnectionInfo connectionInfo;
                int rsId;
                string rsName = GetRunspaceName(index, out rsId);

                try
                {
                    connectionInfo = new VMConnectionInfo(this.Credential, this.VMId[index], this.VMName[index], this.ConfigurationName);

                    runspace = new RemoteRunspace(Utils.GetTypeTableFromExecutionContextTLS(),
                        connectionInfo, this.Host, null, rsName, rsId);

                    remoteRunspaces.Add(runspace);
                }
                catch (InvalidOperationException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForVMFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                }
                catch (ArgumentException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForVMFailed",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                }
            }

            ResolvedComputerNames = this.VMName;

            return remoteRunspaces;
        }

        /// <summary>
        /// Creates the remote runspace objects when the ContainerId parameter is specified.
        /// </summary>
        private List<RemoteRunspace> CreateRunspacesWhenContainerParameterSpecified()
        {
            int index = 0;
            List<string> resolvedNameList = new List<string>();
            List<RemoteRunspace> remoteRunspaces = new List<RemoteRunspace>();

            Dbg.Assert((ParameterSetName == PSExecutionCmdlet.ContainerIdParameterSet),
                       "Expected ParameterSetName == ContainerId");

            foreach (var input in ContainerId)
            {
                //
                // Create helper objects for container ID or name.
                //
                RemoteRunspace runspace = null;
                ContainerConnectionInfo connectionInfo = null;
                int rsId;
                string rsName = GetRunspaceName(index, out rsId);
                index++;

                try
                {
                    //
                    // Hyper-V container uses Hype-V socket as transport.
                    // Windows Server container uses named pipe as transport.
                    //
                    connectionInfo = ContainerConnectionInfo.CreateContainerConnectionInfo(input, RunAsAdministrator.IsPresent, this.ConfigurationName);

                    resolvedNameList.Add(connectionInfo.ComputerName);

                    connectionInfo.CreateContainerProcess();

                    runspace = new RemoteRunspace(Utils.GetTypeTableFromExecutionContextTLS(),
                        connectionInfo, this.Host, null, rsName, rsId);

                    remoteRunspaces.Add(runspace);
                }
                catch (InvalidOperationException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                    continue;
                }
                catch (ArgumentException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                    continue;
                }
                catch (Exception e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                    continue;
                }
            }

            ResolvedComputerNames = resolvedNameList.ToArray();

            return remoteRunspaces;
        }

        /// <summary>
        /// CreateRunspacesForSSHHostParameterSet.
        /// </summary>
        /// <returns></returns>
        private List<RemoteRunspace> CreateRunspacesForSSHHostParameterSet()
        {
            // Resolve all the machine names
            string[] resolvedComputerNames;

            ResolveComputerNames(HostName, out resolvedComputerNames);

            var remoteRunspaces = new List<RemoteRunspace>();
            int index = 0;
            foreach (var computerName in resolvedComputerNames)
            {
                ParseSshHostName(computerName, out string host, out string userName, out int port);

                var sshConnectionInfo = new SSHConnectionInfo(
                    userName,
                    host,
                    this.KeyFilePath,
                    port,
                    Subsystem,
                    ConnectingTimeout,
                    Options);
                var typeTable = TypeTable.LoadDefaultTypeFiles();
                string rsName = GetRunspaceName(index, out int rsIdUnused);
                index++;
                remoteRunspaces.Add(RunspaceFactory.CreateRunspace(connectionInfo: sshConnectionInfo,
                                                                   host: this.Host,
                                                                   typeTable: typeTable,
                                                                   applicationArguments: null,
                                                                   name: rsName) as RemoteRunspace);
            }

            return remoteRunspaces;
        }

        private List<RemoteRunspace> CreateRunspacesForSSHHostHashParameterSet()
        {
            var sshConnections = ParseSSHConnectionHashTable();
            var remoteRunspaces = new List<RemoteRunspace>();
            int index = 0;
            foreach (var sshConnection in sshConnections)
            {
                var sshConnectionInfo = new SSHConnectionInfo(
                    sshConnection.UserName,
                    sshConnection.ComputerName,
                    sshConnection.KeyFilePath,
                    sshConnection.Port,
                    sshConnection.Subsystem,
                    sshConnection.ConnectingTimeout,
                    sshConnection.Options);
                var typeTable = TypeTable.LoadDefaultTypeFiles();
                string rsName = GetRunspaceName(index, out int rsIdUnused);
                index++;
                remoteRunspaces.Add(RunspaceFactory.CreateRunspace(connectionInfo: sshConnectionInfo,
                                                                   host: this.Host,
                                                                   typeTable: typeTable,
                                                                   applicationArguments: null,
                                                                   name: rsName) as RemoteRunspace);
            }

            return remoteRunspaces;
        }

        /// <summary>
        /// Helper method to create remote runspace based on UseWindowsPowerShell parameter set.
        /// </summary>
        /// <returns>Remote runspace that was created.</returns>
        private List<RemoteRunspace> CreateRunspacesForUseWindowsPowerShellParameterSet()
        {
            var remoteRunspaces = new List<RemoteRunspace>();

            NewProcessConnectionInfo connectionInfo = new NewProcessConnectionInfo(this.Credential);
            connectionInfo.AuthenticationMechanism = this.Authentication;
#if !UNIX
            connectionInfo.PSVersion = new Version(5, 1);
#endif
            var typeTable = TypeTable.LoadDefaultTypeFiles();
            string runspaceName = GetRunspaceName(0, out int runspaceIdUnused);
            remoteRunspaces.Add(RunspaceFactory.CreateRunspace(connectionInfo: connectionInfo,
                                                               host: this.Host,
                                                               typeTable: typeTable,
                                                               applicationArguments: null,
                                                               name: runspaceName) as RemoteRunspace);
            return remoteRunspaces;
        }

        /// <summary>
        /// Helper method to either get a user supplied runspace/session name
        /// or to generate one along with a unique Id.
        /// </summary>
        /// <param name="rsIndex">Runspace name array index.</param>
        /// <param name="rsId">Runspace Id.</param>
        /// <returns>Runspace name.</returns>
        private string GetRunspaceName(int rsIndex, out int rsId)
        {
            // Get a unique session/runspace Id and default Name.
            string rsName = PSSession.GenerateRunspaceName(out rsId);

            // If there is a friendly name for the runspace, we need to pass it to the
            // runspace pool object, which in turn passes it on to the server during
            // construction.  This way the friendly name can be returned when querying
            // the sever for disconnected sessions/runspaces.
            if (Name != null && rsIndex < Name.Length)
            {
                rsName = Name[rsIndex];
            }

            return rsName;
        }

        /// <summary>
        /// Internal dispose method which does the actual
        /// dispose operations and finalize suppressions.
        /// </summary>
        /// <param name="disposing">Whether method is called
        /// from Dispose or destructor</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _throttleManager.Dispose();

                // wait for all runspace operations to be complete
                _operationsComplete.WaitOne();
                _operationsComplete.Dispose();

                _throttleManager.ThrottleComplete -= HandleThrottleComplete;
                _throttleManager = null;

                foreach (RemoteRunspace remoteRunspace in _toDispose)
                {
                    remoteRunspace.Dispose();
                }

                // Dispose all open operation objects, to remove runspace event callback.
                foreach (List<IThrottleOperation> operationList in _allOperations)
                {
                    foreach (OpenRunspaceOperation operation in operationList)
                    {
                        operation.Dispose();
                    }
                }

                _stream.Dispose();
            }
        }

        /// <summary>
        /// Handles the throttling complete event of the throttle manager.
        /// </summary>
        /// <param name="sender">Sender of this event.</param>
        /// <param name="eventArgs"></param>
        private void HandleThrottleComplete(object sender, EventArgs eventArgs)
        {
            // all operations are complete close the stream
            _stream.ObjectWriter.Close();

            _operationsComplete.Set();
        }

        /// <summary>
        /// Writes an error record specifying that creation of remote runspace
        /// failed.
        /// </summary>
        /// <param name="e">exception which is causing this error record
        /// to be written</param>
        /// <param name="uri">Uri which caused this exception.</param>
        private void WriteErrorCreateRemoteRunspaceFailed(Exception e, Uri uri)
        {
            Dbg.Assert(e is UriFormatException || e is InvalidOperationException ||
                       e is ArgumentException || e is NotSupportedException,
                       "Exception has to be of type UriFormatException or InvalidOperationException or ArgumentException or NotSupportedException");

            PipelineWriter writer = _stream.ObjectWriter;

            ErrorRecord errorRecord = new ErrorRecord(e, "CreateRemoteRunspaceFailed",
                ErrorCategory.InvalidArgument, uri);

            Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
            writer.Write(errorWriter);
        }

        #endregion Private Methods

        #region Private Members

        private ThrottleManager _throttleManager = new ThrottleManager();
        private readonly ObjectStream _stream = new ObjectStream();
        // event that signals that all operations are
        // complete (including closing if any)
        private readonly ManualResetEvent _operationsComplete = new ManualResetEvent(true);
        // the initial state is true because when no
        // operations actually take place as in case of a
        // parameter binding exception, then Dispose is
        // called. Since Dispose waits on this handler
        // it is set to true initially and is Reset() in
        // BeginProcessing()

        // list of runspaces to dispose
        private readonly List<RemoteRunspace> _toDispose = new List<RemoteRunspace>();

        // List of runspace connect operations.  Need to keep for cleanup.
        private readonly Collection<List<IThrottleOperation>> _allOperations = new Collection<List<IThrottleOperation>>();

        // Default FQEID.
        private readonly string _defaultFQEID = "PSSessionOpenFailed";

        #endregion Private Members
    }

    #region Helper Classes

    /// <summary>
    /// Class that implements the IThrottleOperation in turn wrapping the
    /// opening of a runspace asynchronously within it.
    /// </summary>
    internal class OpenRunspaceOperation : IThrottleOperation, IDisposable
    {
        // Member variables to ensure that the ThrottleManager gets StartComplete
        // or StopComplete called only once per Start or Stop operation.
        private bool _startComplete;
        private bool _stopComplete;

        private readonly object _syncObject = new object();

        internal RemoteRunspace OperatedRunspace { get; }

        internal OpenRunspaceOperation(RemoteRunspace runspace)
        {
            _startComplete = true;
            _stopComplete = true;
            OperatedRunspace = runspace;
            OperatedRunspace.StateChanged += HandleRunspaceStateChanged;
        }

        /// <summary>
        /// Opens the runspace asynchronously.
        /// </summary>
        internal override void StartOperation()
        {
            lock (_syncObject)
            {
                _startComplete = false;
            }

            OperatedRunspace.OpenAsync();
        }

        /// <summary>
        /// Closes the runspace already opened asynchronously.
        /// </summary>
        internal override void StopOperation()
        {
            OperationStateEventArgs operationStateEventArgs = null;

            lock (_syncObject)
            {
                // Ignore stop operation if start operation has completed.
                if (_startComplete)
                {
                    _stopComplete = true;
                    _startComplete = true;
                    operationStateEventArgs = new OperationStateEventArgs();
                    operationStateEventArgs.BaseEvent = new RunspaceStateEventArgs(OperatedRunspace.RunspaceStateInfo);
                    operationStateEventArgs.OperationState = OperationState.StopComplete;
                }
                else
                {
                    _stopComplete = false;
                }
            }

            if (operationStateEventArgs != null)
            {
                FireEvent(operationStateEventArgs);
            }
            else
            {
                OperatedRunspace.CloseAsync();
            }
        }

        // OperationComplete event handler uses an internal collection of event handler
        // callbacks for two reasons:
        //  a) To ensure callbacks are made in list order (first added, first called).
        //  b) To ensure all callbacks are fired by manually invoking callbacks and handling
        //     any exceptions thrown on this thread. (ThrottleManager will not respond if it doesn't
        //     get a start/stop complete callback).
        private readonly List<EventHandler<OperationStateEventArgs>> _internalCallbacks = new List<EventHandler<OperationStateEventArgs>>();

        internal override event EventHandler<OperationStateEventArgs> OperationComplete
        {
            add
            {
                lock (_internalCallbacks)
                {
                    _internalCallbacks.Add(value);
                }
            }

            remove
            {
                lock (_internalCallbacks)
                {
                    _internalCallbacks.Remove(value);
                }
            }
        }

        /// <summary>
        /// Handler for handling runspace state changed events. This method will be
        /// registered in the StartOperation and StopOperation methods. This handler
        /// will in turn invoke the OperationComplete event for all events that are
        /// necessary - Opened, Closed, Disconnected, Broken. It will ignore all other state
        /// changes.
        /// </summary>
        /// <remarks>
        /// There are two problems that need to be handled.
        /// 1) We need to make sure that the ThrottleManager StartComplete and StopComplete
        ///    operation events are called or the ThrottleManager will never end (will stop reponding).
        /// 2) The HandleRunspaceStateChanged event handler remains in the Runspace
        ///    StateChanged event call chain until this object is disposed.  We have to
        ///    disallow the HandleRunspaceStateChanged event from running and throwing
        ///    an exception since this prevents other event handlers in the chain from
        ///    being called.
        /// </remarks>
        /// <param name="source">Source of this event.</param>
        /// <param name="stateEventArgs">object describing state information of the
        /// runspace</param>
        private void HandleRunspaceStateChanged(object source, RunspaceStateEventArgs stateEventArgs)
        {
            // Disregard intermediate states.
            switch (stateEventArgs.RunspaceStateInfo.State)
            {
                case RunspaceState.Opening:
                case RunspaceState.BeforeOpen:
                case RunspaceState.Closing:
                    return;
            }

            OperationStateEventArgs operationStateEventArgs = null;
            lock (_syncObject)
            {
                // We must call OperationComplete ony *once* for each Start/Stop operation.
                if (!_stopComplete)
                {
                    // Note that the StopComplete callback removes *both* the Start and Stop
                    // operations from their respective queues.  So update the member vars
                    // accordingly.
                    _stopComplete = true;
                    _startComplete = true;
                    operationStateEventArgs = new OperationStateEventArgs();
                    operationStateEventArgs.BaseEvent = stateEventArgs;
                    operationStateEventArgs.OperationState = OperationState.StopComplete;
                }
                else if (!_startComplete)
                {
                    _startComplete = true;
                    operationStateEventArgs = new OperationStateEventArgs();
                    operationStateEventArgs.BaseEvent = stateEventArgs;
                    operationStateEventArgs.OperationState = OperationState.StartComplete;
                }
            }

            if (operationStateEventArgs != null)
            {
                // Fire callbacks in list order.
                FireEvent(operationStateEventArgs);
            }
        }

        private void FireEvent(OperationStateEventArgs operationStateEventArgs)
        {
            EventHandler<OperationStateEventArgs>[] copyCallbacks;
            lock (_internalCallbacks)
            {
                copyCallbacks = new EventHandler<OperationStateEventArgs>[_internalCallbacks.Count];
                _internalCallbacks.CopyTo(copyCallbacks);
            }

            foreach (var callbackDelegate in copyCallbacks)
            {
                // Ensure all callbacks get called to prevent ThrottleManager from not responding.
                try
                {
                    callbackDelegate.SafeInvoke(this, operationStateEventArgs);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        public void Dispose()
        {
            // Must remove the event callback from the new runspace or it will block other event
            // handling by throwing an exception on the event thread.
            OperatedRunspace.StateChanged -= HandleRunspaceStateChanged;

            GC.SuppressFinalize(this);
        }
    }

    #endregion Helper Classes
}
