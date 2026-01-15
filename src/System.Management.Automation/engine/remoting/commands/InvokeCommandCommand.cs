// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet executes a specified script block on one or more
    /// remote machines. The expression or command, as they will be
    /// interchangeably called, need to be contained in a script
    /// block. This is to ensure two things:
    ///       1. The expression that the user has entered is
    ///          syntactically correct (its compiled)
    ///       2. The scriptblock can be converted to a powershell
    ///          object before transmitting it to the remote end
    ///          so that it can be run on constrained runspaces in
    ///          the no language mode
    ///
    /// In general, the command script block is executed as if
    /// the user had typed it at the command line. The output of the
    /// command is the output of the cmdlet. However, since
    /// invoke-command is a cmdlet, it will unravel its output:
    ///     - if the command outputs an empty array, invoke-command
    ///     will output $null
    ///     - if the command outputs a single-element array, invoke-command
    ///     will output that single element.
    ///
    ///     Additionally, the command will be run on a remote system.
    ///
    /// This cmdlet can be called in the following different ways:
    ///
    /// Execute a command in a remote machine by specifying the command
    /// and machine name
    ///     invoke-command -Command {get-process} -computername "server1"
    ///
    /// Execute a command in a set of remote machines by specifying the
    /// command and the list of machines
    ///     $servers = 1..10 | ForEach-Object {"Server${_}"}
    ///     invoke-command -command {get-process} -computername $servers
    ///
    /// Create a new runspace and use it to execute a command on a remote machine
    ///     $runspace = New-PSSession -computername "Server1"
    ///     $credential = get-credential "user01"
    ///     invoke-command -command {get-process} -Session $runspace -credential $credential
    ///
    /// Execute a command in a set of remote machines by specifying the
    /// complete uri for the machines
    ///     $uri = "http://hostedservices.microsoft.com/someservice"
    ///     invoke-command -command { get-mail } - uri $uri
    ///
    /// Create a collection of runspaces and use it to execute a command on a set
    /// of remote machines
    ///
    ///     $serveruris = 1..8 | ForEach-Object {"http://Server${_}/"}
    ///     $runspaces = New-PSSession -URI $serveruris
    ///     invoke-command -command {get-process} -Session $runspaces
    ///
    /// The cmdlet can also be invoked in the asynchronous mode.
    ///
    ///     invoke-command -command {get-process} -computername $servers -asjob
    ///
    /// When the -AsJob switch is used, the cmdlet will emit an PSJob Object.
    /// The user can then use the other job cmdlets to work with this object
    ///
    /// Note there are two types of errors:
    ///     1. Remote invocation errors
    ///     2. Local errors.
    ///
    /// Both types of errors will be available when the user invokes
    /// a receive operation.
    ///
    /// The PSJob object has its own throttling mechanism.
    /// The result object will be stored in a global cache. If a user wants to
    /// retrieve data from the result object the user should be able to do so
    /// using the Receive-PSJob cmdlet
    ///
    /// The following needs to be noted about exception/error reporting in this
    /// cmdlet:
    ///     The exception objects that are thrown by underlying layers will be
    ///     written as errors, to avoid stopping the entire cmdlet in case of
    ///     multi-computername or multi-Session usage (for consistency, this
    ///     is true even when done using one computername or runspace)
    ///
    /// Only one expression may be executed at a time in any single runspace.
    /// Attempts to invoke an expression on a runspace that is already executing
    /// an expression shall return an error with ErrorCategory ResourceNotAvailable
    /// and notify the user that the runspace is currently busy.
    ///
    /// Some additional notes:
    /// - invoke-command issues a single scriptblock to the computer or
    /// runspace. If a runspace is specified and a command is already running
    /// in that runspace, then the second command will fail
    /// - The files necessary to execute the command (cmdlets, scripts, data
    /// files, etc) must be present on the remote system; the cmdlet is not
    /// responsible for copying them over
    /// - The entire input stream is collected and sent to the remote system
    /// before execution of the command begins (no input streaming)
    /// - Input shall be available as $input.  Remote Runspaces must reference
    /// $input explicitly (input will not automatically be available)
    /// - Output from the command streams back to the client as it is
    /// available
    /// - Ctrl-C and pause/resume are supported; the client will send a
    /// message to the remote powershell instance.
    /// - By default if no -credential is specified, the host will impersonate
    /// the current user on the client when executing the command
    /// - The standard output of invoke-command is the output of the
    /// last element of the remote pipeline, with some extra properties added
    /// - If -Shell is not specified, then the value of the environment
    /// variable DEFAULTREMOTESHELLNAME is used. If this is not set, then
    /// "Microsoft.PowerShell" is used.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Command", DefaultParameterSetName = InvokeCommandCommand.InProcParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096789", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class InvokeCommandCommand : PSExecutionCmdlet, IDisposable
    {
        #region Parameters

        /// <summary>
        /// The PSSession object describing the remote runspace
        /// using which the specified cmdlet operation will be performed.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [ValidateNotNullOrEmpty]
        public override PSSession[] Session
        {
            get
            {
                return base.Session;
            }

            set
            {
                base.Session = value;
            }
        }

        /// <summary>
        /// This parameter represents the address(es) of the remote
        /// computer(s). The following formats are supported:
        ///      (a) Computer name
        ///      (b) IPv4 address : 132.3.4.5
        ///      (c) IPv6 address: 3ffe:8311:ffff:f70f:0:5efe:172.30.162.18.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Alias("Cn")]
        [ValidateNotNullOrEmpty]
        public override string[] ComputerName
        {
            get
            {
                return base.ComputerName;
            }

            set
            {
                base.ComputerName = value;
            }
        }

        /// <summary>
        /// Specifies the credentials of the user to impersonate in the
        /// remote machine. If this parameter is not specified then the
        /// credentials of the current user process will be assumed.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [Credential]
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
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public override int Port
        {
            get
            {
                return base.Port;
            }

            set
            {
                base.Port = value;
            }
        }

        /// <summary>
        /// This parameter suggests that the transport scheme to be used for
        /// remote connections is useSSL instead of the default http.Since
        /// there are only two possible transport schemes that are possible
        /// at this point, a SwitchParameter is being used to switch between
        /// the two.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public override SwitchParameter UseSSL
        {
            get
            {
                return base.UseSSL;
            }

            set
            {
                base.UseSSL = value;
            }
        }

        /// <summary>
        /// For WSMan session:
        /// If this parameter is not specified then the value specified in
        /// the environment variable DEFAULTREMOTESHELLNAME will be used. If
        /// this is not set as well, then Microsoft.PowerShell is used.
        ///
        /// For VM/Container sessions:
        /// If this parameter is not specified then no configuration is used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
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
        /// This parameters specifies the appname which identifies the connection
        /// end point on the remote machine. If this parameter is not specified
        /// then the value specified in DEFAULTREMOTEAPPNAME will be used. If that's
        /// not specified as well, then "WSMAN" will be used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        public override string ApplicationName
        {
            get
            {
                return base.ApplicationName;
            }

            set
            {
                base.ApplicationName = value;
            }
        }

        /// <summary>
        /// Allows the user of the cmdlet to specify a throttling value
        /// for throttling the number of remote operations that can
        /// be executed simultaneously.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        public override int ThrottleLimit
        {
            get
            {
                return base.ThrottleLimit;
            }

            set
            {
                base.ThrottleLimit = value;
            }
        }

        /// <summary>
        /// A complete URI(s) specified for the remote computer and shell to
        /// connect to and create runspace for.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(Position = 0,
                   ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        public override Uri[] ConnectionUri
        {
            get
            {
                return base.ConnectionUri;
            }

            set
            {
                base.ConnectionUri = value;
            }
        }

        /// <summary>
        /// Specifies if the cmdlet needs to be run asynchronously.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostHashParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostHashParameterSet)]
        public SwitchParameter AsJob
        {
            get
            {
                return _asjob;
            }

            set
            {
                _asjob = value;
            }
        }

        private bool _asjob = false;

        /// <summary>
        /// Specifies that after the command is invoked on a remote computer the
        /// remote session should be disconnected.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Alias("Disconnected")]
        public SwitchParameter InDisconnectedSession
        {
            get { return InvokeAndDisconnect; }

            set { InvokeAndDisconnect = value; }
        }

        /// <summary>
        /// Specifies the name of the returned session when the InDisconnectedSession switch
        /// is used.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SessionName
        {
            get { return DisconnectedSessionName; }

            set { DisconnectedSessionName = value; }
        }

        /// <summary>
        /// Hide/Show computername of the remote objects.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostHashParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostHashParameterSet)]
        [Alias("HCN")]
        public SwitchParameter HideComputerName
        {
            get { return _hideComputerName; }

            set { _hideComputerName = value; }
        }

        private bool _hideComputerName;

        /// <summary>
        /// Friendly name for the job object if AsJob is used.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostHashParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        public string JobName
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
                    _asjob = true;
                }
            }
        }

        private string _name = string.Empty;

        /// <summary>
        /// The script block that the user has specified in the
        /// cmdlet. This will be converted to a powershell before
        /// its actually sent to the remote end.
        /// </summary>
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.InProcParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = InvokeCommandCommand.SSHHostHashParameterSet)]
        [ValidateNotNull]
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

        /// <summary>
        /// When executing a scriptblock in the current session, tell the cmdlet not to create a new scope.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.InProcParameterSet)]
        public SwitchParameter NoNewScope { get; set; }

        /// <summary>
        /// The script block that the user has specified in the
        /// cmdlet. This will be converted to a powershell before
        /// its actually sent to the remote end.
        /// </summary>
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathComputerNameParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathSessionParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathUriParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathVMIdParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathVMNameParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathContainerIdParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = FilePathSSHHostParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = FilePathSSHHostHashParameterSet)]
        [ValidateNotNull]
        [Alias("PSPath")]
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
        /// The AllowRedirection parameter enables the implicit redirection functionality.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        public override SwitchParameter AllowRedirection
        {
            get
            {
                return base.AllowRedirection;
            }

            set
            {
                base.AllowRedirection = value;
            }
        }

        /// <summary>
        /// Extended Session Options for controlling the session creation. Use
        /// "New-WSManSessionOption" cmdlet to supply value for this parameter.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
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
        /// Authentication mechanism to authenticate the user.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
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
        /// When set and in loopback scenario (localhost) this enables creation of WSMan
        /// host process with the user interactive token, allowing PowerShell script network access,
        /// i.e., allows going off box.  When this property is true and a PSSession is disconnected,
        /// reconnection is allowed only if reconnecting from a PowerShell session on the same box.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        public override SwitchParameter EnableNetworkAccess
        {
            get { return base.EnableNetworkAccess; }

            set { base.EnableNetworkAccess = value; }
        }

        /// <summary>
        /// When set, PowerShell process inside container will be launched with
        /// high privileged account.
        /// Otherwise (default case), PowerShell process inside container will be launched
        /// with low privileged account.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        public override SwitchParameter RunAsAdministrator
        {
            get { return base.RunAsAdministrator; }

            set { base.RunAsAdministrator = value; }
        }

        #region SSH Parameters

        /// <summary>
        /// Host name for an SSH remote connection.
        /// </summary>
        [Parameter(Mandatory = true,
            ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(Mandatory = true,
            ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] HostName
        {
            get { return base.HostName; }

            set { base.HostName = value; }
        }

        /// <summary>
        /// User Name.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string UserName
        {
            get { return base.UserName; }

            set { base.UserName = value; }
        }

        /// <summary>
        /// Key Path.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("IdentityFilePath")]
        public override string KeyFilePath
        {
            get { return base.KeyFilePath; }

            set { base.KeyFilePath = value; }
        }

        /// <summary>
        /// Gets and sets a value for the SSH subsystem to use for the remote connection.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        public override string Subsystem
        {
            get { return base.Subsystem; }

            set { base.Subsystem = value; }
        }

        /// <summary>
        /// Gets and sets a value in milliseconds that limits the time allowed for an SSH connection to be established.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        public override int ConnectingTimeout
        {
            get { return base.ConnectingTimeout; }

            set { base.ConnectingTimeout = value; }
        }

        /// <summary>
        /// This parameter specifies that SSH is used to establish the remote
        /// connection and act as the remoting transport.  By default WinRM is used
        /// as the remoting transport.  Using the SSH transport requires that SSH is
        /// installed and PowerShell remoting is enabled on both client and remote machines.
        /// </summary>
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [ValidateSet("true")]
        public override SwitchParameter SSHTransport
        {
            get { return base.SSHTransport; }

            set { base.SSHTransport = value; }
        }

        /// <summary>
        /// Hashtable array containing SSH connection parameters for each remote target
        ///   ComputerName  (Alias: HostName)           (required)
        ///   UserName                                  (optional)
        ///   KeyFilePath   (Alias: IdentityFilePath)   (optional)
        /// </summary>
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostHashParameterSet, Mandatory = true)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostHashParameterSet, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public override Hashtable[] SSHConnection
        {
            get;
            set;
        }

        /// <summary>
        /// Hashtable containing options to be passed to OpenSSH.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [ValidateNotNullOrEmpty]
        public override Hashtable Options
        {
            get
            {
                return base.Options;
            }

            set
            {
                base.Options = value;
            }
        }

        #endregion

        #region Remote Debug Parameters

        /// <summary>
        /// When selected this parameter causes a debugger Step-Into action for each running remote session.
        /// </summary>
        [Parameter(ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSessionParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostHashParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostParameterSet)]
        [Parameter(ParameterSetName = InvokeCommandCommand.FilePathSSHHostHashParameterSet)]
        public virtual SwitchParameter RemoteDebug
        {
            get;
            set;
        }

        #endregion

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Creates the helper classes for the specified
        /// parameter set.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.InvokeAndDisconnect && _asjob)
            {
                // The -AsJob and -InDisconnectedSession parameter switches are mutually exclusive.
                throw new InvalidOperationException(RemotingErrorIdStrings.AsJobAndDisconnectedError);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(SessionName)) && !this.InvokeAndDisconnect)
            {
                throw new InvalidOperationException(RemotingErrorIdStrings.SessionNameWithoutInvokeDisconnected);
            }

            // Adjust RemoteDebug value based on current state
            var hostDebugger = GetHostDebugger();
            if (hostDebugger == null)
            {
                // Do not allow RemoteDebug if there is no host debugger available.  Otherwise script will not respond indefinitely.
                RemoteDebug = false;
            }
            else if (hostDebugger.IsDebuggerSteppingEnabled)
            {
                // If host debugger is in step-in mode then always make RemoteDebug true
                RemoteDebug = true;
            }

            // Checking session's availability and reporting errors in early stage, unless '-AsJob' is specified.
            // When '-AsJob' is specified, Invoke-Command should return a job object without throwing error, even
            // if the session is not in available state -- this is the PSv3 behavior and we should not break it.
            if (!_asjob && (ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet) ||
                ParameterSetName.Equals(InvokeCommandCommand.FilePathSessionParameterSet)))
            {
                long localPipelineId =
                    ((LocalRunspace)this.Context.CurrentRunspace).GetCurrentlyRunningPipeline().InstanceId;

                // Check for sessions in invalid state for running commands.
                List<PSSession> availableSessions = new List<PSSession>();
                foreach (var session in Session)
                {
                    if (session.Runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                    {
                        // Session not in Opened state.
                        string msg = StringUtil.Format(RemotingErrorIdStrings.ICMInvalidSessionState,
                            session.Name, session.InstanceId, session.ComputerName, session.Runspace.RunspaceStateInfo.State);

                        WriteError(new ErrorRecord(
                            new InvalidRunspaceStateException(msg),
                            "InvokeCommandCommandInvalidSessionState",
                            ErrorCategory.InvalidOperation,
                            session));
                    }
                    else if (session.Runspace.RunspaceAvailability != RunspaceAvailability.Available)
                    {
                        // Check to see if this is a steppable pipeline case.
                        RemoteRunspace remoteRunspace = session.Runspace as RemoteRunspace;
                        if ((remoteRunspace != null) &&
                            (remoteRunspace.RunspaceAvailability == RunspaceAvailability.Busy) &&
                            (remoteRunspace.IsAnotherInvokeCommandExecuting(this, localPipelineId)))
                        {
                            // Valid steppable pipeline session.
                            availableSessions.Add(session);
                        }
                        else
                        {
                            // Session not Available.
                            string msg = StringUtil.Format(RemotingErrorIdStrings.ICMInvalidSessionAvailability,
                                session.Name, session.InstanceId, session.ComputerName, session.Runspace.RunspaceAvailability);

                            WriteError(new ErrorRecord(
                                new InvalidRunspaceStateException(msg),
                                "InvokeCommandCommandInvalidSessionAvailability",
                                ErrorCategory.InvalidOperation,
                                session));
                        }
                    }
                    else
                    {
                        availableSessions.Add(session);
                    }
                }

                if (availableSessions.Count == 0)
                {
                    throw new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.ICMNoValidRunspaces));
                }

                if (availableSessions.Count < Session.Length)
                {
                    Session = availableSessions.ToArray();
                }
            }

            if (ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet))
            {
                if (FilePath != null)
                {
                    ScriptBlock = GetScriptBlockFromFile(FilePath, false);
                }

                if (this.MyInvocation.ExpectingInput)
                {
                    if (!ScriptBlock.IsUsingDollarInput())
                    {
                        try
                        {
                            _steppablePipeline = ScriptBlock.GetSteppablePipeline(CommandOrigin.Internal, ArgumentList);
                            _steppablePipeline.Begin(this);
                        }
                        catch (InvalidOperationException)
                        {
                            // ignore exception and don't do any streaming if can't convert to steppable pipeline
                        }
                    }
                }

                return;
            }

            if (string.IsNullOrEmpty(ConfigurationName))
            {
                if ((ParameterSetName == InvokeCommandCommand.ComputerNameParameterSet) ||
                    (ParameterSetName == InvokeCommandCommand.UriParameterSet) ||
                    (ParameterSetName == InvokeCommandCommand.FilePathComputerNameParameterSet) ||
                    (ParameterSetName == InvokeCommandCommand.FilePathUriParameterSet))
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

            base.BeginProcessing();

            // create collection of input writers here
            foreach (IThrottleOperation operation in Operations)
            {
                _inputWriters.Add(((ExecutionCmdletHelper)operation).Pipeline.Input);
            }

            // we need to verify, if this Invoke-Command is the first
            // instance within the current local pipeline. If not, then
            // we need to collect all the data and run the invoke-command
            // when the remote runspace is free

            // We also need to worry about it only in the case of
            // runspace parameter set - for all else we will never hit
            // this scenario
            if (ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet))
            {
                long localPipelineId =
                    ((LocalRunspace)this.Context.CurrentRunspace).GetCurrentlyRunningPipeline().InstanceId;
                foreach (PSSession runspaceInfo in Session)
                {
                    RemoteRunspace remoteRunspace = (RemoteRunspace)runspaceInfo.Runspace;
                    if (remoteRunspace.IsAnotherInvokeCommandExecuting(this, localPipelineId))
                    {
                        // Use remote steppable pipeline only for non-input piping case.
                        // Win8 Bug:898011 - We are restricting remote steppable pipeline because
                        // of this bug in Win8 where not responding can occur during data piping.
                        // We are reverting to Win7 behavior for {icm | icm} and {proxycommand | proxycommand}
                        // cases. For ICM | % ICM case, we are using remote steppable pipeline.
                        if ((MyInvocation != null) && (MyInvocation.PipelinePosition == 1) && !MyInvocation.ExpectingInput)
                        {
                            PSPrimitiveDictionary table = (object)runspaceInfo.ApplicationPrivateData[PSVersionInfo.PSVersionTableName] as PSPrimitiveDictionary;
                            if (table != null)
                            {
                                Version version = (object)table[PSVersionInfo.PSRemotingProtocolVersionName] as Version;

                                if (version != null)
                                {
                                    // In order to support foreach remoting properly ( icm | % { icm } ), the server must
                                    // be using protocol version 2.2. Otherwise, we skip this and assume the old behavior.
                                    if (version >= RemotingConstants.ProtocolVersion_2_2)
                                    {
                                        // Suppress collection behavior
                                        _needToCollect = false;
                                        _needToStartSteppablePipelineOnServer = true;
                                        break;
                                    }
                                }
                            }
                        }

                        // Either version table is null or the server is not version 2.2 and beyond, we need to collect
                        _needToCollect = true;
                        _needToStartSteppablePipelineOnServer = false;
                        break;
                    }
                }
            }

            if (_needToStartSteppablePipelineOnServer)
            {
                // create collection of input writers here
                foreach (IThrottleOperation operation in Operations)
                {
                    if (operation is not ExecutionCmdletHelperRunspace ecHelper)
                    {
                        // either all the operations will be of type ExecutionCmdletHelperRunspace
                        // or not...there is no mix.
                        break;
                    }

                    ecHelper.ShouldUseSteppablePipelineOnServer = true;
                }
            }
            else
            {
                // RemoteRunspace must be holding this InvokeCommand..So release
                // this at dispose time
                _clearInvokeCommandOnRunspace = true;
            }

            // check if we need to propagate terminating errors
            DetermineThrowStatementBehavior();
        }

        /// <summary>
        /// The expression will be executed in the remote computer if a
        /// remote runspace parameter or computer name or uri is specified.
        /// </summary>
        /// <remarks>
        /// 1. Identify if the command belongs to the same pipeline
        /// 2. If so, use the same GUID to create Pipeline/PowerShell
        /// </remarks>
        protected override void ProcessRecord()
        {
            // we should create the pipeline on first instance
            // and if there are no invoke-commands running
            // ahead in the pipeline
            if (!_pipelineinvoked && !_needToCollect)
            {
                _pipelineinvoked = true;

                if (InputObject == AutomationNull.Value)
                {
                    CloseAllInputStreams();
                    _inputStreamClosed = true;
                }

                if (!ParameterSetName.Equals(InProcParameterSet))
                {
                    // at this point there is nothing to do for
                    // inproc case. The script block is executed
                    // in EndProcessing
                    if (!_asjob)
                    {
                        CreateAndRunSyncJob();
                    }
                    else
                    {
                        switch (ParameterSetName)
                        {
                            case InvokeCommandCommand.ComputerNameParameterSet:
                            case InvokeCommandCommand.FilePathComputerNameParameterSet:
                            case InvokeCommandCommand.VMIdParameterSet:
                            case InvokeCommandCommand.VMNameParameterSet:
                            case InvokeCommandCommand.ContainerIdParameterSet:
                            case InvokeCommandCommand.FilePathVMIdParameterSet:
                            case InvokeCommandCommand.FilePathVMNameParameterSet:
                            case InvokeCommandCommand.FilePathContainerIdParameterSet:
                            case InvokeCommandCommand.SSHHostParameterSet:
                            case InvokeCommandCommand.FilePathSSHHostParameterSet:
                            case InvokeCommandCommand.SSHHostHashParameterSet:
                            case InvokeCommandCommand.FilePathSSHHostHashParameterSet:
                                {
                                    if (ResolvedComputerNames.Length != 0 && Operations.Count > 0)
                                    {
                                        PSRemotingJob job = new PSRemotingJob(ResolvedComputerNames, Operations,
                                                ScriptBlock.ToString(), ThrottleLimit, _name);
                                        job.PSJobTypeName = RemoteJobType;
                                        job.HideComputerName = _hideComputerName;
                                        this.JobRepository.Add(job);
                                        WriteObject(job);
                                    }
                                }

                                break;

                            case InvokeCommandCommand.SessionParameterSet:
                            case InvokeCommandCommand.FilePathSessionParameterSet:
                                {
                                    PSRemotingJob job = new PSRemotingJob(Session, Operations,
                                            ScriptBlock.ToString(), ThrottleLimit, _name);
                                    job.PSJobTypeName = RemoteJobType;
                                    job.HideComputerName = _hideComputerName;
                                    this.JobRepository.Add(job);
                                    WriteObject(job);
                                }

                                break;

                            case InvokeCommandCommand.UriParameterSet:
                            case InvokeCommandCommand.FilePathUriParameterSet:
                                {
                                    if (Operations.Count > 0)
                                    {
                                        string[] locations = new string[ConnectionUri.Length];
                                        for (int i = 0; i < locations.Length; i++)
                                        {
                                            locations[i] = ConnectionUri[i].ToString();
                                        }

                                        PSRemotingJob job = new PSRemotingJob(locations, Operations,
                                            ScriptBlock.ToString(), ThrottleLimit, _name);
                                        job.PSJobTypeName = RemoteJobType;
                                        job.HideComputerName = _hideComputerName;
                                        this.JobRepository.Add(job);
                                        WriteObject(job);
                                    }
                                }

                                break;
                        }
                    }
                }
            }

            if (InputObject != AutomationNull.Value && !_inputStreamClosed)
            {
                if ((ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet) && (_steppablePipeline == null)) ||
                    _needToCollect)
                {
                    _input.Add(InputObject);
                }
                else if (ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet) && (_steppablePipeline != null))
                {
                    _steppablePipeline.Process(InputObject);
                }
                else
                {
                    WriteInput(InputObject);

                    // if not a job write out the results available thus far
                    if (!_asjob)
                    {
                        WriteJobResults(true);
                    }
                }
            }
        }

        /// <summary>
        /// InvokeAsync would have been called in ProcessRecord. Wait here
        /// for all the results to become available.
        /// </summary>
        protected override void EndProcessing()
        {
            // close the input stream on all the pipelines
            if (!_needToCollect)
            {
                CloseAllInputStreams();
            }

            if (!_asjob)
            {
                if (ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet))
                {
                    if (_steppablePipeline != null)
                    {
                        _steppablePipeline.End();
                    }
                    else
                    {
                        ScriptBlock.InvokeUsingCmdlet(
                            contextCmdlet: this,
                            useLocalScope: !NoNewScope,
                            errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                            dollarUnder: AutomationNull.Value,
                            input: _input,
                            scriptThis: AutomationNull.Value,
                            args: ArgumentList);
                    }
                }
                else
                {
                    // runspace and computername parameter sets
                    if (_job != null)
                    {
                        // The job/command is disconnected immediately after it is invoked.  The command
                        // will continue to run on the server but we don't wait and return immediately.
                        if (InvokeAndDisconnect)
                        {
                            // Wait for the Job disconnect to complete.
                            WaitForDisconnectAndDisposeJob();
                            return;
                        }

                        // Wait for job results and for job to complete.
                        // The Job may auto-disconnect in which case it may be
                        // converted to "asJob" so that it isn't disposed and can
                        // be connected to later.
                        WriteJobResults(false);

                        // Dispose job object if it is not returned to the user.
                        // The _asjob field can change dynamically and needs to be checked before the job
                        // object is disposed. For example, if remote sessions are disconnected abruptly
                        // via WinRM, a disconnected job object is created to facilitate a reconnect.
                        // If the job object is disposed here, then a session reconnect cannot happen.
                        if (!_asjob)
                        {
                            _job.Dispose();
                        }

                        // We no longer need to call ClearInvokeCommandOnRunspaces() here because
                        // this command might finish before the foreach block finishes. previously,
                        // icm | icm was implemented so that the first icm always finishes before
                        // the second icm runs, this is not the case with the new implementation
                    }
                    else
                    {
                        if (_needToCollect && ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet))
                        {
                            // if job was null, then its because the invoke-command
                            // was collecting or ProcessRecord() was not called.
                            // If we are collecting, then
                            // we would have collected until this point
                            // so now start the execution with the collected
                            // input

                            Dbg.Assert(_needToCollect, "InvokeCommand should have collected input before this");
                            Dbg.Assert(ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet), "Collecting and invoking should happen only in case of Runspace parameter set");

                            CreateAndRunSyncJob();

                            // loop through and write all input
                            foreach (object inputValue in _input)
                            {
                                WriteInput(inputValue);
                            }

                            CloseAllInputStreams();

                            // The job/command is disconnected immediately after it is invoked.  The command
                            // will continue to run on the server but we don't wait and return immediately.
                            if (InvokeAndDisconnect)
                            {
                                // Wait for the Job disconnect to complete.
                                WaitForDisconnectAndDisposeJob();
                                return;
                            }

                            // This calls waits for the job to return and then writes the results.
                            // The Job may auto-disconnect in which case it may be
                            // converted to "asJob" so that it isn't disposed and can
                            // be connected to later.
                            WriteJobResults(false);

                            // Dispose job object if it is not returned to the user.
                            // The _asjob field can change dynamically and needs to be checked before the job
                            // object is disposed. For example, if remote sessions are disconnected abruptly
                            // via WinRM, a disconnected job object is created to facilitate a reconnect.
                            // If the job object is disposed here, then a session reconnect cannot happen.
                            if (!_asjob)
                            {
                                _job.Dispose();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method is called when the user sends a stop signal to the
        /// cmdlet. The cmdlet will not exit until it has completed
        /// executing the command on all the runspaces. However, when a stop
        /// signal is sent, execution needs to be stopped on the pipelines
        /// corresponding to these runspaces.
        /// </summary>
        /// <remarks>This is called from a separate thread so need to worry
        /// about concurrency issues
        /// </remarks>
        protected override void StopProcessing()
        {
            // Ensure that any runspace debug processing is ended
            var hostDebugger = GetHostDebugger();
            if (hostDebugger != null)
            {
                try
                {
                    hostDebugger.CancelDebuggerProcessing();
                }
                catch (PSNotImplementedException) { }
            }

            if (!ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet))
            {
                if (!_asjob)
                {
                    // stop all operations in the job
                    // we need to check is job is not null, since
                    // StopProcessing() may be called even before the
                    // job is created
                    bool stopjob = false;
                    lock (_jobSyncObject)
                    {
                        if (_job != null)
                        {
                            stopjob = true;
                        }
                        else
                        {
                            // StopProcessing() has already been called
                            // the job should not be created anymore
                            _nojob = true;
                        }
                    }

                    if (stopjob)
                    {
                        _job.StopJob();
                    }

                    // clear the need to collect flag
                    _needToCollect = false;
                }
            }
        }

        #endregion Overrides

        #region Private Methods

        private Debugger GetHostDebugger()
        {
            Debugger hostDebugger = null;
            try
            {
                System.Management.Automation.Internal.Host.InternalHost chost =
                    this.Host as System.Management.Automation.Internal.Host.InternalHost;
                hostDebugger = chost.Runspace.Debugger;
            }
            catch (PSNotImplementedException) { }

            return hostDebugger;
        }

        /// <summary>
        /// Handle event from the throttle manager indicating that all
        /// operations are complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleThrottleComplete(object sender, EventArgs eventArgs)
        {
            _operationsComplete.Set();
            _throttleManager.ThrottleComplete -= HandleThrottleComplete;
        }

        /// <summary>
        /// Clears the internal invoke command instance on all
        /// remote runspaces.
        /// </summary>
        private void ClearInvokeCommandOnRunspaces()
        {
            if (ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet))
            {
                foreach (PSSession runspaceInfo in Session)
                {
                    RemoteRunspace remoteRunspace = (RemoteRunspace)runspaceInfo.Runspace;
                    remoteRunspace.ClearInvokeCommand();
                }
            }
        }

        /// <summary>
        /// Sets the throttle limit, creates the invoke expression
        /// sync job and executes the same.
        /// </summary>
        private void CreateAndRunSyncJob()
        {
            lock (_jobSyncObject)
            {
                if (!_nojob)
                {
                    _throttleManager.ThrottleLimit = ThrottleLimit;
                    _throttleManager.ThrottleComplete += HandleThrottleComplete;

                    _operationsComplete.Reset();
                    Dbg.Assert(_disconnectComplete == null, "disconnectComplete event should only be used once.");
                    _disconnectComplete = new ManualResetEvent(false);
                    _job = new PSInvokeExpressionSyncJob(Operations, _throttleManager);
                    _job.HideComputerName = _hideComputerName;
                    _job.StateChanged += HandleJobStateChanged;

                    // Add robust connection retry notification handler.
                    AddConnectionRetryHandler(_job);

                    // Enable all Invoke-Command synchronous jobs for remote debugging (in case Wait-Debugger or
                    // or line breakpoints are set in script).
                    foreach (var operation in Operations)
                    {
                        operation.RunspaceDebuggingEnabled = true;
                        operation.RunspaceDebugStepInEnabled = RemoteDebug;
                        operation.RunspaceDebugStop += HandleRunspaceDebugStop;
                    }

                    _job.StartOperations(Operations);
                }
            }
        }

        private void HandleRunspaceDebugStop(object sender, StartRunspaceDebugProcessingEventArgs args)
        {
            var operation = sender as IThrottleOperation;
            operation.RunspaceDebugStop -= HandleRunspaceDebugStop;

            var hostDebugger = GetHostDebugger();
            hostDebugger?.QueueRunspaceForDebug(args.Runspace);
        }

        private void HandleJobStateChanged(object sender, JobStateEventArgs e)
        {
            JobState state = e.JobStateInfo.State;
            if (state == JobState.Disconnected ||
                state == JobState.Completed ||
                state == JobState.Stopped ||
                state == JobState.Failed)
            {
                _job.StateChanged -= HandleJobStateChanged;
                RemoveConnectionRetryHandler(sender as PSInvokeExpressionSyncJob);

                // Signal that this job has been disconnected, or has ended.
                lock (_jobSyncObject)
                {
                    _disconnectComplete?.Set();
                }
            }
        }

        private void AddConnectionRetryHandler(PSInvokeExpressionSyncJob job)
        {
            if (job == null)
            {
                return;
            }

            Collection<System.Management.Automation.PowerShell> powershells = job.GetPowerShells();
            foreach (var ps in powershells)
            {
                if (ps.RemotePowerShell != null)
                {
                    ps.RemotePowerShell.RCConnectionNotification += RCConnectionNotificationHandler;
                }
            }
        }

        private void RemoveConnectionRetryHandler(PSInvokeExpressionSyncJob job)
        {
            // Ensure progress bar is removed.
            StopProgressBar(0);

            if (job == null)
            {
                return;
            }

            Collection<System.Management.Automation.PowerShell> powershells = job.GetPowerShells();
            foreach (var ps in powershells)
            {
                if (ps.RemotePowerShell != null)
                {
                    ps.RemotePowerShell.RCConnectionNotification -= RCConnectionNotificationHandler;
                }
            }
        }

        private void RCConnectionNotificationHandler(object sender, PSConnectionRetryStatusEventArgs e)
        {
            // Update the progress bar.
            switch (e.Notification)
            {
                case PSConnectionRetryStatus.NetworkFailureDetected:
                    StartProgressBar(sender.GetHashCode(), e.ComputerName, (e.MaxRetryConnectionTime / 1000));
                    break;

                case PSConnectionRetryStatus.ConnectionRetrySucceeded:
                case PSConnectionRetryStatus.AutoDisconnectStarting:
                case PSConnectionRetryStatus.InternalErrorAbort:
                    StopProgressBar(sender.GetHashCode());
                    break;
            }
        }

        /// <summary>
        /// Waits for the disconnectComplete event and then disposes the job
        /// object.
        /// </summary>
        private void WaitForDisconnectAndDisposeJob()
        {
            if (_disconnectComplete != null)
            {
                _disconnectComplete.WaitOne();

                // Create disconnected PSSession objects for each powershell and write to output.
                List<PSSession> discSessions = GetDisconnectedSessions(_job);
                foreach (PSSession session in discSessions)
                {
                    this.RunspaceRepository.AddOrReplace(session);
                    WriteObject(session);
                }

                // Check to see if the disconnect was successful.  If not write any errors there may be.
                if (_job.Error.Count > 0)
                {
                    WriteStreamObjectsFromCollection(_job.ReadAll());
                }

                _job.Dispose();
            }
        }

        /// <summary>
        /// Creates a disconnected session for each disconnected PowerShell object in
        /// PSInvokeExpressionSyncJob.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private List<PSSession> GetDisconnectedSessions(PSInvokeExpressionSyncJob job)
        {
            List<PSSession> discSessions = new List<PSSession>();

            Collection<System.Management.Automation.PowerShell> powershells = job.GetPowerShells();
            foreach (System.Management.Automation.PowerShell ps in powershells)
            {
                // Get the command information from the PowerShell object.
                string commandText = (ps.Commands != null && ps.Commands.Commands.Count > 0) ?
                    ps.Commands.Commands[0].CommandText : string.Empty;
                ConnectCommandInfo cmdInfo = new ConnectCommandInfo(ps.InstanceId, commandText);

                // Get the old RunspacePool object that the command was initially run on.
                RunspacePool oldRunspacePool = null;
                if (ps.RunspacePool != null)
                {
                    oldRunspacePool = ps.RunspacePool;
                }
                else
                {
                    object rsConnection = ps.GetRunspaceConnection();
                    RunspacePool rsPool = rsConnection as RunspacePool;
                    if (rsPool != null)
                    {
                        oldRunspacePool = rsPool;
                    }
                    else
                    {
                        RemoteRunspace remoteRs = rsConnection as RemoteRunspace;
                        if (remoteRs != null)
                        {
                            oldRunspacePool = remoteRs.RunspacePool;
                        }
                    }
                }

                // Create a new disconnected PSSession object and return to the user.
                // The user can use this object to connect to the command on the server
                // and retrieve data.
                if (oldRunspacePool != null)
                {
                    if (oldRunspacePool.RunspacePoolStateInfo.State != RunspacePoolState.Disconnected)
                    {
                        // InvokeAndDisconnect starts the command and immediately disconnects the command,
                        // but we need to disconnect the associated runspace/pool here.
                        if (InvokeAndDisconnect && oldRunspacePool.RunspacePoolStateInfo.State == RunspacePoolState.Opened)
                        {
                            oldRunspacePool.Disconnect();
                        }
                        else
                        {
                            // Skip runspace pools that have not been disconnected.
                            continue;
                        }
                    }

                    // Auto-generate a session name if one was not provided.
                    string sessionName = oldRunspacePool.RemoteRunspacePoolInternal.Name;
                    if (string.IsNullOrEmpty(sessionName))
                    {
                        int id;
                        sessionName = PSSession.GenerateRunspaceName(out id);
                    }

                    RunspacePool runspacePool = new RunspacePool(
                                                        true,
                                                        oldRunspacePool.RemoteRunspacePoolInternal.InstanceId,
                                                        sessionName,
                                                        new ConnectCommandInfo[1] { cmdInfo },
                                                        oldRunspacePool.RemoteRunspacePoolInternal.ConnectionInfo,
                                                        this.Host,
                                                        this.Context.TypeTable);
                    runspacePool.RemoteRunspacePoolInternal.IsRemoteDebugStop = oldRunspacePool.RemoteRunspacePoolInternal.IsRemoteDebugStop;

                    RemoteRunspace remoteRunspace = new RemoteRunspace(runspacePool);
                    discSessions.Add(new PSSession(remoteRunspace));
                }
            }

            return discSessions;
        }

        /// <summary>
        /// Writes an input value to the pipeline.
        /// </summary>
        /// <param name="inputValue">Input value to write.</param>
        private void WriteInput(object inputValue)
        {
            // when there are no input writers, there is no
            // point either accumulating or trying to write data
            // so throw an exception in that case
            if (_inputWriters.Count == 0)
            {
                if (!_asjob)
                {
                    WriteJobResults(false);
                }

                this.EndProcessing();
                throw new StopUpstreamCommandsException(this);
            }

            List<PipelineWriter> removeCollection = new List<PipelineWriter>();

            foreach (PipelineWriter writer in _inputWriters)
            {
                try
                {
                    writer.Write(inputValue);
                }
                catch (PipelineClosedException)
                {
                    removeCollection.Add(writer);
                    continue;
                }
            }

            foreach (PipelineWriter writer in removeCollection)
            {
                _inputWriters.Remove(writer);
            }
        }

        /// <summary>
        /// Writes the results in the job object.
        /// </summary>
        /// <param name="nonblocking">Write in a non-blocking manner.</param>
        private void WriteJobResults(bool nonblocking)
        {
            if (_job == null)
            {
                return;
            }

            try
            {
                PipelineStoppedException caughtPipelineStoppedException = null;
                _job.PropagateThrows = _propagateErrors;

                do
                {
                    if (!nonblocking)
                    {
                        // we need to wait until results arrive
                        // before we attempt to read. This will
                        // ensure that the thread blocks. Else
                        // the thread will spin leading to a CPU
                        // usage spike
                        if (_disconnectComplete != null)
                        {
                            // An auto-disconnect can occur and we need to detect
                            // this condition along with a job results signal.
                            WaitHandle.WaitAny(new WaitHandle[] {
                                                    _disconnectComplete,
                                                    _job.Results.WaitHandle });
                        }
                        else
                        {
                            _job.Results.WaitHandle.WaitOne();
                        }
                    }

                    try
                    {
                        WriteStreamObjectsFromCollection(_job.ReadAll());
                    }
                    catch (System.Management.Automation.PipelineStoppedException pse)
                    {
                        caughtPipelineStoppedException = pse;
                    }

                    if (nonblocking)
                    {
                        break;
                    }
                } while (!_job.IsTerminalState());

                try
                {
                    WriteStreamObjectsFromCollection(_job.ReadAll());
                }
                catch (System.Management.Automation.PipelineStoppedException pse)
                {
                    caughtPipelineStoppedException = pse;
                }

                if (caughtPipelineStoppedException != null)
                {
                    HandlePipelinesStopped();
                    throw caughtPipelineStoppedException;
                }

                if (_job.JobStateInfo.State == JobState.Disconnected)
                {
                    if (ParameterSetName == InvokeCommandCommand.SessionParameterSet ||
                        ParameterSetName == InvokeCommandCommand.FilePathSessionParameterSet)
                    {
                        // Create a PSRemoting job we can add to the job repository and that
                        // a user can reconnect to (via Receive-PSSession).
                        PSRemotingJob rtnJob = _job.CreateDisconnectedRemotingJob();
                        if (rtnJob != null)
                        {
                            rtnJob.PSJobTypeName = RemoteJobType;

                            // Don't let the job object be disposed or stopped since
                            // we want to be able to reconnect to the disconnected
                            // pipelines.
                            _asjob = true;

                            // Write warnings to user about each disconnect.
                            foreach (var cjob in rtnJob.ChildJobs)
                            {
                                PSRemotingChildJob childJob = cjob as PSRemotingChildJob;
                                if (childJob != null)
                                {
                                    // Get session for this job.
                                    PSSession session = GetPSSession(childJob.Runspace.InstanceId);
                                    if (session != null)
                                    {
                                        // Write network failed, auto-disconnect error
                                        WriteNetworkFailedError(session);

                                        // Session disconnected message.
                                        WriteWarning(
                                            StringUtil.Format(RemotingErrorIdStrings.RCDisconnectSession,
                                                session.Name, session.InstanceId, session.ComputerName));
                                    }
                                }
                            }

                            if (rtnJob.ChildJobs.Count > 0)
                            {
                                JobRepository.Add(rtnJob);

                                // Inform the user that a new Job object was created and added to the repository
                                // to support later reconnection.
                                WriteWarning(
                                    StringUtil.Format(RemotingErrorIdStrings.RCDisconnectedJob, rtnJob.Name));
                            }
                        }
                    }
                    else if (ParameterSetName == InvokeCommandCommand.ComputerNameParameterSet ||
                             ParameterSetName == InvokeCommandCommand.FilePathComputerNameParameterSet)
                    {
                        // Create disconnected sessions for each PowerShell in job that was disconnected,
                        // and add them to the local repository.
                        List<PSSession> discSessions = GetDisconnectedSessions(_job);
                        foreach (PSSession session in discSessions)
                        {
                            // Add to session repository.
                            this.RunspaceRepository.AddOrReplace(session);

                            // Write network failed, auto-disconnect error
                            WriteNetworkFailedError(session);

                            // Session disconnected message.
                            WriteWarning(
                                StringUtil.Format(RemotingErrorIdStrings.RCDisconnectSession,
                                    session.Name, session.InstanceId, session.ComputerName));

                            // Session created message.
                            WriteWarning(
                                StringUtil.Format(RemotingErrorIdStrings.RCDisconnectSessionCreated,
                                    session.Name, session.InstanceId));
                        }
                    }
                }
            }
            finally
            {
                if (_job.JobStateInfo.State == JobState.Disconnected)
                {
                    // Allow Invoke-Command to end even though not all remote pipelines
                    // finished.
                    HandleThrottleComplete(null, null);
                }
            }
        }

        private void WriteNetworkFailedError(PSSession session)
        {
            RuntimeException reason = new RuntimeException(
                StringUtil.Format(RemotingErrorIdStrings.RCAutoDisconnectingError, session.ComputerName));

            WriteError(new ErrorRecord(reason,
                PSConnectionRetryStatusEventArgs.FQIDAutoDisconnectStarting,
                ErrorCategory.OperationTimeout, session));
        }

        private PSSession GetPSSession(Guid runspaceId)
        {
            foreach (PSSession session in Session)
            {
                if (session.Runspace.InstanceId == runspaceId)
                {
                    return session;
                }
            }

            return null;
        }

        private void HandlePipelinesStopped()
        {
            // Emit warning for cases where commands were stopped during connection retry attempts.
            bool retryCanceled = false;
            Collection<System.Management.Automation.PowerShell> powershells = _job.GetPowerShells();
            foreach (System.Management.Automation.PowerShell ps in powershells)
            {
                if (ps.RemotePowerShell != null &&
                    ps.RemotePowerShell.ConnectionRetryStatus != PSConnectionRetryStatus.None &&
                    ps.RemotePowerShell.ConnectionRetryStatus != PSConnectionRetryStatus.ConnectionRetrySucceeded &&
                    ps.RemotePowerShell.ConnectionRetryStatus != PSConnectionRetryStatus.AutoDisconnectSucceeded)
                {
                    retryCanceled = true;
                    break;
                }
            }

            if (retryCanceled &&
                this.Host != null)
            {
                // Write warning directly to host since pipeline has been stopped.
                this.Host.UI.WriteWarningLine(RemotingErrorIdStrings.StopCommandOnRetry);
            }
        }

        private void StartProgressBar(
            long sourceId,
            string computerName,
            int totalSeconds)
        {
            s_RCProgress.StartProgress(
                sourceId,
                computerName,
                totalSeconds,
                this.Host);
        }

        private static void StopProgressBar(
            long sourceId)
        {
            s_RCProgress.StopProgress(sourceId);
        }

        /// <summary>
        /// Writes the stream objects in the specified collection.
        /// </summary>
        /// <param name="results">Collection to read from.</param>
        private void WriteStreamObjectsFromCollection(IEnumerable<PSStreamObject> results)
        {
            foreach (var result in results)
            {
                if (result != null)
                {
                    PreProcessStreamObject(result);
                    result.WriteStreamObject(this);
                }
            }
        }

        /// <summary>
        /// Determine if we have to throw for a
        /// "throw" statement from scripts
        ///  This means that the local pipeline will be terminated as well.
        /// </summary>
        /// <remarks>
        /// This is valid when only one pipeline is
        /// existing. Which means, there can be only one of the following:
        ///     1. A single computer name
        ///     2. A single session
        ///     3. A single uri
        ///
        /// It can be used in conjunction with a filepath or a script block parameter
        ///
        /// It doesn't take effect with the -AsJob parameter
        /// </remarks>
        private void DetermineThrowStatementBehavior()
        {
            if (ParameterSetName.Equals(InvokeCommandCommand.InProcParameterSet))
            {
                // in proc parameter set - just return
                return;
            }

            if (!_asjob)
            {
                if (ParameterSetName.Equals(InvokeCommandCommand.ComputerNameParameterSet) ||
                    ParameterSetName.Equals(InvokeCommandCommand.FilePathComputerNameParameterSet))
                {
                    if (ComputerName.Length == 1)
                    {
                        _propagateErrors = true;
                    }
                }
                else if (ParameterSetName.Equals(InvokeCommandCommand.SessionParameterSet) ||
                         ParameterSetName.Equals(InvokeCommandCommand.FilePathSessionParameterSet))
                {
                    if (Session.Length == 1)
                    {
                        _propagateErrors = true;
                    }
                }
                else if (ParameterSetName.Equals(InvokeCommandCommand.UriParameterSet) ||
                         ParameterSetName.Equals(InvokeCommandCommand.FilePathUriParameterSet))
                {
                    if (ConnectionUri.Length == 1)
                    {
                        _propagateErrors = true;
                    }
                }
            }
        }

        /// <summary>
        /// Process the stream object before writing it in the specified collection.
        /// </summary>
        /// <param name="streamObject">Stream object to process.</param>
        private static void PreProcessStreamObject(PSStreamObject streamObject)
        {
            ErrorRecord errorRecord = streamObject.Value as ErrorRecord;

            //
            // In case of PSDirectException, we should output the precise error message
            // in inner exception instead of the generic one in outer exception.
            //
            if ((errorRecord != null) &&
                (errorRecord.Exception != null) &&
                (errorRecord.Exception.InnerException != null))
            {
                PSDirectException ex = errorRecord.Exception.InnerException as PSDirectException;
                if (ex != null)
                {
                    streamObject.Value = new ErrorRecord(errorRecord.Exception.InnerException,
                                                         errorRecord.FullyQualifiedErrorId,
                                                         errorRecord.CategoryInfo.Category,
                                                         errorRecord.TargetObject);
                }
            }
        }

        #endregion Private Methods

        #region Private Members

        private ThrottleManager _throttleManager = new ThrottleManager();
        // throttle manager for handling all throttling operations
        private readonly ManualResetEvent _operationsComplete = new ManualResetEvent(true);
        private ManualResetEvent _disconnectComplete;
        // the initial state is true because when no
        // operations actually take place as in case of a
        // parameter binding exception, then Dispose is
        // called. Since Dispose waits on this handler
        // it is set to true initially and is Reset() in
        // BeginProcessing()
        private PSInvokeExpressionSyncJob _job;

        // used for streaming behavior for local invocations
        private SteppablePipeline _steppablePipeline;

        private bool _pipelineinvoked = false;    // if pipeline has been invoked
        private bool _inputStreamClosed = false;

        private const string InProcParameterSet = "InProcess";

        private readonly PSDataCollection<object> _input = new PSDataCollection<object>();
        private bool _needToCollect = false;
        private bool _needToStartSteppablePipelineOnServer = false;
        private bool _clearInvokeCommandOnRunspace = false;
        private readonly List<PipelineWriter> _inputWriters = new List<PipelineWriter>();
        private readonly object _jobSyncObject = new object();
        private bool _nojob = false;
        private readonly Guid _instanceId = Guid.NewGuid();
        private bool _propagateErrors = false;

        private static readonly RobustConnectionProgress s_RCProgress = new RobustConnectionProgress();

        internal static readonly string RemoteJobType = "RemoteJob";

        #endregion Private Members

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
                // this call fixes bug Windows 7 #278836
                // by making sure the server is stopped even if it is waiting
                // for further input from this Invoke-Command cmdlet

                this.StopProcessing();
                // wait for all operations to complete
                _operationsComplete.WaitOne();
                _operationsComplete.Dispose();

                if (!_asjob)
                {
                    // job will be null in the "InProcess" case
                    _job?.Dispose();

                    _throttleManager.ThrottleComplete -= HandleThrottleComplete;
                    _throttleManager.Dispose();
                    _throttleManager = null;
                }

                // clear the invoke command references we have stored
                if (_clearInvokeCommandOnRunspace)
                {
                    ClearInvokeCommandOnRunspaces();
                }

                _input.Dispose();

                lock (_jobSyncObject)
                {
                    if (_disconnectComplete != null)
                    {
                        _disconnectComplete.Dispose();
                        _disconnectComplete = null;
                    }
                }
            }
        }

        #endregion IDisposable Overrides
    }
}

namespace System.Management.Automation.Internal
{
    #region RobustConnectionProgress class

    /// <summary>
    /// Encapsulates the Robust Connection retry progress bar.
    /// </summary>
    internal class RobustConnectionProgress
    {
        private System.Management.Automation.Host.PSHost _psHost;
        private readonly string _activity;
        private string _status;
        private int _secondsTotal;
        private int _secondsRemaining;
        private ProgressRecord _progressRecord;
        private long _sourceId;
        private bool _progressIsRunning;
        private readonly object _syncObject;
        private Timer _updateTimer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RobustConnectionProgress()
        {
            _syncObject = new object();
            _activity = RemotingErrorIdStrings.RCProgressActivity;
        }

        /// <summary>
        /// Starts progress bar.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="computerName"></param>
        /// <param name="secondsTotal"></param>
        /// <param name="psHost"></param>
        public void StartProgress(
            long sourceId,
            string computerName,
            int secondsTotal,
            System.Management.Automation.Host.PSHost psHost)
        {
            if (psHost == null)
            {
                return;
            }

            if (secondsTotal < 1)
            {
                return;
            }

            ArgumentException.ThrowIfNullOrEmpty(computerName);

            lock (_syncObject)
            {
                if (_progressIsRunning)
                {
                    return;
                }

                _progressIsRunning = true;
                _sourceId = sourceId;
                _secondsTotal = secondsTotal;
                _secondsRemaining = secondsTotal;
                _psHost = psHost;
                _status = StringUtil.Format(RemotingErrorIdStrings.RCProgressStatus, computerName);
                _progressRecord = new ProgressRecord(0, _activity, _status);

                // Create timer to fire every second to update progress bar.
                _updateTimer = new Timer(new TimerCallback(UpdateCallback), null, TimeSpan.Zero, new TimeSpan(0, 0, 1));
            }
        }

        /// <summary>
        /// Stops progress bar.
        /// </summary>
        public void StopProgress(
            long sourceId)
        {
            lock (_syncObject)
            {
                if ((sourceId == _sourceId || sourceId == 0) &&
                    _progressIsRunning)
                {
                    RemoveProgressBar();
                }
            }
        }

        private void UpdateCallback(object state)
        {
            lock (_syncObject)
            {
                if (!_progressIsRunning)
                {
                    return;
                }

                if (_secondsRemaining > 0)
                {
                    // Update progress bar.
                    _progressRecord.PercentComplete =
                        ((_secondsTotal - _secondsRemaining) * 100) / _secondsTotal;
                    _progressRecord.SecondsRemaining = _secondsRemaining--;
                    _progressRecord.RecordType = ProgressRecordType.Processing;
                    _psHost.UI.WriteProgress(0, _progressRecord);
                }
                else
                {
                    // Remove progress bar.
                    RemoveProgressBar();
                }
            }
        }

        private void RemoveProgressBar()
        {
            _progressIsRunning = false;

            // Remove progress bar.
            _progressRecord.RecordType = ProgressRecordType.Completed;
            _psHost.UI.WriteProgress(0, _progressRecord);

            // Remove timer.
            _updateTimer.Dispose();
            _updateTimer = null;
        }
    }

    #endregion
}
