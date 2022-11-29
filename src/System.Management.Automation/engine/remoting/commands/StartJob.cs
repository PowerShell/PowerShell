// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet start invocation of jobs in background.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Job", DefaultParameterSetName = StartJobCommand.ComputerNameParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096796")]
    [OutputType(typeof(PSRemotingJob))]
    public class StartJobCommand : PSExecutionCmdlet, IDisposable
    {
        #region Private members

        private static readonly string s_startJobType = "BackgroundJob";

        #endregion

        #region Parameters

        private const string DefinitionNameParameterSet = "DefinitionName";

        /// <summary>
        /// JobDefinition Name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = StartJobCommand.DefinitionNameParameterSet)]
        [ValidateTrustedData]
        [ValidateNotNullOrEmpty]
        public string DefinitionName
        {
            get { return _definitionName; }

            set { _definitionName = value; }
        }

        private string _definitionName;

        /// <summary>
        /// JobDefinition file path.
        /// </summary>
        [Parameter(Position = 1,
                   ParameterSetName = StartJobCommand.DefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string DefinitionPath
        {
            get { return _definitionPath; }

            set { _definitionPath = value; }
        }

        private string _definitionPath;

        /// <summary>
        /// Job SourceAdapter type for this job definition.
        /// </summary>
        [Parameter(Position = 2,
            ParameterSetName = StartJobCommand.DefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Type
        {
            get { return _definitionType; }

            set { _definitionType = value; }
        }

        private string _definitionType;

        /// <summary>
        /// Friendly name for this job object.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        public virtual string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _name = value;
                }
            }
        }

        private string _name;

        /// <summary>
        /// Command to execute specified as a string. This can be a single
        /// cmdlet, an expression or anything that can be internally
        /// converted into a ScriptBlock.
        /// </summary>
        /// <remarks>This is used in the in process case with a
        /// "ValueFromPipelineProperty" enabled in order to maintain
        /// compatibility with v1.0</remarks>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [ValidateTrustedData]
        [Alias("Command")]
        public override ScriptBlock ScriptBlock
        {
            get
            {
                return base.ScriptBlock;
            }

            set
            {
                base.ScriptBlock = value;
            }
        }

        #region Suppress PSRemotingBaseCmdlet parameters

        // suppress all the parameters from PSRemotingBaseCmdlet
        // which should not be part of Start-PSJob

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override PSSession[] Session
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string[] ComputerName
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Not used for OutOfProc jobs.  Suppressing this parameter.
        /// </summary>
        public override SwitchParameter EnableNetworkAccess
        {
            get { return false; }
        }

        /// <summary>
        /// Suppress SSHTransport.
        /// </summary>
        public override SwitchParameter SSHTransport
        {
            get { return false; }
        }

        /// <summary>
        /// Suppress SSHConnection.
        /// </summary>
        public override Hashtable[] SSHConnection
        {
            get { return null; }
        }

        /// <summary>
        /// Suppress UserName.
        /// </summary>
        public override string UserName
        {
            get { return null; }
        }

        /// <summary>
        /// Suppress KeyFilePath.
        /// </summary>
        public override string KeyFilePath
        {
            get { return null; }
        }

        /// <summary>
        /// Suppress HostName.
        /// </summary>
        public override string[] HostName
        {
            get { return null; }
        }

        /// <summary>
        /// Suppress Subsystem.
        /// </summary>
        public override string Subsystem
        {
            get { return null; }
        }

        #endregion

        /// <summary>
        /// Credential to use for this job.
        /// </summary>
        [Parameter(ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
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
        /// Overriding to suppress this parameter.
        /// </summary>
        public override int Port
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override SwitchParameter UseSSL
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string ConfigurationName
        {
            get
            {
                return base.ConfigurationName;
            }

            set
            {
                base.ConfigurationName = value;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override Int32 ThrottleLimit
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string ApplicationName
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override Uri[] ConnectionUri
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Filepath to execute as a script.
        /// </summary>
        [Parameter(
            Position = 0,
            Mandatory = true,
            ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [ValidateTrustedData]
        public override string FilePath
        {
            get
            {
                return base.FilePath;
            }

            set
            {
                base.FilePath = value;
            }
        }

        /// <summary>
        /// Literal Filepath to execute as a script.
        /// </summary>
        [Parameter(
            Mandatory = true,
            ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        [ValidateTrustedData]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return base.FilePath;
            }

            set
            {
                base.FilePath = value;
                base.IsLiteralPath = true;
            }
        }

        /// <summary>
        /// Use basic authentication to authenticate the user.
        /// </summary>
        [Parameter(ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        public override AuthenticationMechanism Authentication
        {
            get
            {
                return base.Authentication;
            }

            set
            {
                base.Authentication = value;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string CertificateThumbprint
        {
            get
            {
                return base.CertificateThumbprint;
            }

            set
            {
                base.CertificateThumbprint = value;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override SwitchParameter AllowRedirection
        {
            get
            {
                return false;
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
        public override SwitchParameter RunAsAdministrator
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Extended Session Options for controlling the session creation. Use
        /// "New-WSManSessionOption" cmdlet to supply value for this parameter.
        /// </summary>
        /// <remarks>
        /// This is not declared as a Parameter for Start-PSJob as this is not
        /// used for background jobs.
        /// </remarks>
        public override PSSessionOption SessionOption
        {
            get
            {
                return base.SessionOption;
            }

            set
            {
                base.SessionOption = value;
            }
        }

        /// <summary>
        /// Script that is used to initialize the background job.
        /// </summary>
        [Parameter(Position = 1,
                   ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        [ValidateTrustedData]
        public virtual ScriptBlock InitializationScript
        {
            get { return _initScript; }

            set { _initScript = value; }
        }

        private ScriptBlock _initScript;

        /// <summary>
        /// Gets or sets an initial working directory for the powershell background job.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrWhiteSpace]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Launches the background job as a 32-bit process. This can be used on
        /// 64-bit systems to launch a 32-bit wow process for the background job.
        /// </summary>
        [Parameter(ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        public virtual SwitchParameter RunAs32 { get; set; }

        /// <summary>
        /// Powershell Version to execute the background job.
        /// </summary>
        [Parameter(ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual Version PSVersion
        {
            get
            {
                return _psVersion;
            }

            set
            {
                // PSVersion value can only be 5.1 for Start-Job.
                if (!(value.Major == 5 && value.Minor == 1))
                {
                    throw new ArgumentException(
                        StringUtil.Format(RemotingErrorIdStrings.PSVersionParameterOutOfRange, value, "PSVersion"));
                }

                _psVersion = value;
            }
        }

        private Version _psVersion;

        /// <summary>
        /// InputObject.
        /// </summary>
        [Parameter(ValueFromPipeline = true,
                   ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ValueFromPipeline = true,
                   ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipeline = true,
                   ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        [ValidateTrustedData]
        public override PSObject InputObject
        {
            get { return base.InputObject; }

            set { base.InputObject = value; }
        }

        /// <summary>
        /// ArgumentList.
        /// </summary>
        [Parameter(ParameterSetName = StartJobCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = StartJobCommand.LiteralFilePathComputerNameParameterSet)]
        [ValidateTrustedData]
        [Alias("Args")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public override object[] ArgumentList
        {
            get { return base.ArgumentList; }

            set { base.ArgumentList = value; }
        }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// 1. Set the throttling limit and reset operations complete
        /// 2. Create helper objects
        /// 3. For async case, write the async result object down the
        ///    pipeline.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (!File.Exists(PowerShellProcessInstance.PwshExePath))
            {
                // The pwsh executable file is not found under $PSHOME.
                // This means that PowerShell is currently being hosted in another application,
                // and 'Start-Job' is not supported by design in that scenario.
                string message = StringUtil.Format(
                    RemotingErrorIdStrings.IPCPwshExecutableNotFound,
                    PowerShellProcessInstance.PwshExePath);

                var errorRecord = new ErrorRecord(
                    new PSNotSupportedException(message),
                    "IPCPwshExecutableNotFound",
                    ErrorCategory.NotInstalled,
                    PowerShellProcessInstance.PwshExePath);

                ThrowTerminatingError(errorRecord);
            }

            if (RunAs32.IsPresent && Environment.Is64BitProcess)
            {
                // We cannot start a 32-bit 'pwsh' process from a 64-bit 'pwsh' installation.
                string message = RemotingErrorIdStrings.RunAs32NotSupported;
                var errorRecord = new ErrorRecord(
                    new PSNotSupportedException(message),
                    "RunAs32NotSupported",
                    ErrorCategory.InvalidOperation,
                    targetObject: null);

                ThrowTerminatingError(errorRecord);
            }

            if (WorkingDirectory != null && !InvokeProvider.Item.IsContainer(WorkingDirectory))
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.StartJobWorkingDirectoryNotFound, WorkingDirectory);
                var errorRecord = new ErrorRecord(
                    new DirectoryNotFoundException(message),
                    "DirectoryNotFoundException",
                    ErrorCategory.InvalidOperation,
                    targetObject: null);

                ThrowTerminatingError(errorRecord);
            }

            if (WorkingDirectory == null)
            {
                try
                {
                    WorkingDirectory = SessionState.Internal.CurrentLocation.Path;
                }
                catch (PSInvalidOperationException)
                {
                }
            }

            CommandDiscovery.AutoloadModulesWithJobSourceAdapters(this.Context, this.CommandOrigin);

            if (ParameterSetName == DefinitionNameParameterSet)
            {
                return;
            }

            // since jobs no more depend on WinRM
            // we will have to skip the check for the same
            SkipWinRMCheck = true;

            base.BeginProcessing();
        }

        /// <summary>
        /// Create a throttle operation using NewProcessConnectionInfo
        /// ie., Out-Of-Process runspace.
        /// </summary>
        protected override void CreateHelpersForSpecifiedComputerNames()
        {
            // If we're in ConstrainedLanguage mode and the system is in lockdown mode,
            // ensure that they haven't specified a ScriptBlock or InitScript - as
            // we can't protect that boundary
            if ((Context.LanguageMode == PSLanguageMode.ConstrainedLanguage) &&
                (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Enforce) &&
                ((ScriptBlock != null) || (InitializationScript != null)))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(RemotingErrorIdStrings.CannotStartJobInconsistentLanguageMode),
                            "CannotStartJobInconsistentLanguageMode",
                            ErrorCategory.PermissionDenied,
                            Context.LanguageMode));
            }

            NewProcessConnectionInfo connectionInfo = new NewProcessConnectionInfo(this.Credential);
            connectionInfo.InitializationScript = _initScript;
            connectionInfo.AuthenticationMechanism = this.Authentication;
            connectionInfo.PSVersion = this.PSVersion;
            connectionInfo.WorkingDirectory = this.WorkingDirectory;

            RemoteRunspace remoteRunspace = (RemoteRunspace)RunspaceFactory.CreateRunspace(connectionInfo, this.Host,
                        Utils.GetTypeTableFromExecutionContextTLS());

            remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;

            Pipeline pipeline = CreatePipeline(remoteRunspace);

            IThrottleOperation operation =
                new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline);

            Operations.Add(operation);
        }
        /// <summary>
        /// The expression will be executed in the remote computer if a
        /// remote runspace parameter or computer name is specified. If
        /// none other than command parameter is specified, then it
        /// just executes the command locally without creating a new
        /// remote runspace object.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName == DefinitionNameParameterSet)
            {
                // Get the Job2 object from the Job Manager for this definition name and start the job.
                string resolvedPath = null;
                if (!string.IsNullOrEmpty(_definitionPath))
                {
                    ProviderInfo provider = null;
                    System.Collections.ObjectModel.Collection<string> paths =
                        this.Context.SessionState.Path.GetResolvedProviderPathFromPSPath(_definitionPath, out provider);

                    // Only file system paths are allowed.
                    if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                    {
                        string message = StringUtil.Format(RemotingErrorIdStrings.StartJobDefinitionPathInvalidNotFSProvider,
                            _definitionName,
                            _definitionPath,
                            provider.FullName);
                        WriteError(new ErrorRecord(new RuntimeException(message), "StartJobFromDefinitionNamePathInvalidNotFileSystemProvider",
                            ErrorCategory.InvalidArgument, null));

                        return;
                    }

                    // Only a single file path is allowed.
                    if (paths.Count != 1)
                    {
                        string message = StringUtil.Format(RemotingErrorIdStrings.StartJobDefinitionPathInvalidNotSingle,
                            _definitionName,
                            _definitionPath);
                        WriteError(new ErrorRecord(new RuntimeException(message), "StartJobFromDefinitionNamePathInvalidNotSingle",
                            ErrorCategory.InvalidArgument, null));

                        return;
                    }

                    resolvedPath = paths[0];
                }

                List<Job2> jobs = JobManager.GetJobToStart(_definitionName, resolvedPath, _definitionType, this, false);

                if (jobs.Count == 0)
                {
                    string message = (_definitionType != null) ?
                        StringUtil.Format(RemotingErrorIdStrings.StartJobDefinitionNotFound2, _definitionType, _definitionName) :
                        StringUtil.Format(RemotingErrorIdStrings.StartJobDefinitionNotFound1, _definitionName);

                    WriteError(new ErrorRecord(new RuntimeException(message), "StartJobFromDefinitionNameNotFound",
                        ErrorCategory.ObjectNotFound, null));

                    return;
                }

                if (jobs.Count > 1)
                {
                    string message = StringUtil.Format(RemotingErrorIdStrings.StartJobManyDefNameMatches, _definitionName);
                    WriteError(new ErrorRecord(new RuntimeException(message), "StartJobFromDefinitionNameMoreThanOneMatch",
                        ErrorCategory.InvalidResult, null));

                    return;
                }

                // Start job.
                Job2 job = jobs[0];
                job.StartJob();

                // Write job object to host.
                WriteObject(job);

                return;
            }

            if (_firstProcessRecord)
            {
                _firstProcessRecord = false;

                PSRemotingJob job = new PSRemotingJob(ResolvedComputerNames, Operations,
                        ScriptBlock.ToString(), ThrottleLimit, _name);

                job.PSJobTypeName = s_startJobType;

                this.JobRepository.Add(job);
                WriteObject(job);
            }

            // inject input
            if (InputObject != AutomationNull.Value)
            {
                foreach (IThrottleOperation operation in Operations)
                {
                    ExecutionCmdletHelper helper = (ExecutionCmdletHelper)operation;
                    helper.Pipeline.Input.Write(InputObject);
                }
            }
        }

        private bool _firstProcessRecord = true;

        /// <summary>
        /// InvokeAsync would have been called in ProcessRecord. Wait here
        /// for all the results to become available.
        /// </summary>
        protected override void EndProcessing()
        {
            // close the input stream on all the pipelines
            CloseAllInputStreams();
        }

        #endregion Overrides

        #region IDisposable Overrides

        /// <summary>
        /// Dispose the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method which does the actual disposing.
        /// </summary>
        /// <param name="disposing">Whether called from dispose or finalize.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseAllInputStreams();
            }
        }

        #endregion IDisposable Overrides
    }
}
