// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet enters into an interactive session with the specified local process by
    /// creating a remote runspace to the process and pushing it on the current PSHost.
    /// If the selected process does not contain PowerShell then an error message will result.
    /// If the current user does not have sufficient privileges to attach to the selected process
    /// then an error message will result.
    /// </summary>
    [Cmdlet(VerbsCommon.Enter, "PSHostProcess", DefaultParameterSetName = EnterPSHostProcessCommand.ProcessIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096580")]
    public sealed class EnterPSHostProcessCommand : PSCmdlet
    {
        #region Members

        private IHostSupportsInteractiveSession _interactiveHost;
        private RemoteRunspace _connectingRemoteRunspace;

        #region Strings

        private const string ProcessParameterSet = "ProcessParameterSet";
        private const string ProcessNameParameterSet = "ProcessNameParameterSet";
        private const string ProcessIdParameterSet = "ProcessIdParameterSet";
        private const string PipeNameParameterSet = "PipeNameParameterSet";
        private const string PSHostProcessInfoParameterSet = "PSHostProcessInfoParameterSet";

        private const string NamedPipeRunspaceName = "PSAttachRunspace";

        #endregion

        #endregion

        #region Parameters

        /// <summary>
        /// Process to enter.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = EnterPSHostProcessCommand.ProcessParameterSet)]
        [ValidateNotNull()]
        public Process Process
        {
            get;
            set;
        }

        /// <summary>
        /// Id of process to enter.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = EnterPSHostProcessCommand.ProcessIdParameterSet)]
        [ValidateRange(0, int.MaxValue)]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// Name of process to enter.  An error will result if more than one such process exists.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = EnterPSHostProcessCommand.ProcessNameParameterSet)]
        [ValidateNotNullOrEmpty()]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Host Process Info object that describes a connectible process.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = EnterPSHostProcessCommand.PSHostProcessInfoParameterSet)]
        [ValidateNotNull()]
        public PSHostProcessInfo HostProcessInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom named pipe name to connect to. This is usually used in conjunction with `pwsh -CustomPipeName`.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = EnterPSHostProcessCommand.PipeNameParameterSet)]
        public string CustomPipeName
        {
            get;
            set;
        }

        /// <summary>
        /// Optional name of AppDomain in process to enter.  If not specified then the default AppDomain is used.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = EnterPSHostProcessCommand.ProcessParameterSet)]
        [Parameter(Position = 1, ParameterSetName = EnterPSHostProcessCommand.ProcessIdParameterSet)]
        [Parameter(Position = 1, ParameterSetName = EnterPSHostProcessCommand.ProcessNameParameterSet)]
        [Parameter(Position = 1, ParameterSetName = EnterPSHostProcessCommand.PSHostProcessInfoParameterSet)]
        [ValidateNotNullOrEmpty]
        public string AppDomainName
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// End Processing.
        /// </summary>
        protected override void EndProcessing()
        {
            // Check if system is in locked down mode, in which case this cmdlet is disabled.
            if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
            {
                WriteError(
                    new ErrorRecord(
                        new PSSecurityException(RemotingErrorIdStrings.EnterPSHostProcessCmdletDisabled),
                        "EnterPSHostProcessCmdletDisabled",
                        ErrorCategory.SecurityError,
                        null));

                return;
            }

            // Check for host that supports interactive remote sessions.
            _interactiveHost = this.Host as IHostSupportsInteractiveSession;
            if (_interactiveHost == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ArgumentException(RemotingErrorIdStrings.HostDoesNotSupportIASession),
                        "EnterPSHostProcessHostDoesNotSupportIASession",
                        ErrorCategory.InvalidArgument,
                        null));

                return;
            }

            // Check selected process for existence, and whether it hosts PowerShell.
            Runspace namedPipeRunspace = null;
            switch (ParameterSetName)
            {
                case ProcessIdParameterSet:
                    Process = GetProcessById(Id);
                    VerifyProcess(Process);
                    namedPipeRunspace = CreateNamedPipeRunspace(Process.Id, AppDomainName);
                    break;

                case ProcessNameParameterSet:
                    Process = GetProcessByName(Name);
                    VerifyProcess(Process);
                    namedPipeRunspace = CreateNamedPipeRunspace(Process.Id, AppDomainName);
                    break;

                case PSHostProcessInfoParameterSet:
                    Process = GetProcessByHostProcessInfo(HostProcessInfo);
                    VerifyProcess(Process);

                    // Create named pipe runspace for selected process and open.
                    namedPipeRunspace = CreateNamedPipeRunspace(Process.Id, AppDomainName);
                    break;

                case PipeNameParameterSet:
                    VerifyPipeName(CustomPipeName);
                    namedPipeRunspace = CreateNamedPipeRunspace(CustomPipeName);
                    break;
            }

            // Set runspace prompt.  The runspace is closed on pop so we don't
            // have to reverse this change.
            PrepareRunspace(namedPipeRunspace);

            try
            {
                // Push runspace onto host.
                _interactiveHost.PushRunspace(namedPipeRunspace);
            }
            catch (Exception e)
            {
                namedPipeRunspace.Close();

                ThrowTerminatingError(
                    new ErrorRecord(
                        e,
                        "EnterPSHostProcessCannotPushRunspace",
                        ErrorCategory.InvalidOperation,
                        this));
            }
        }

        /// <summary>
        /// Stop Processing.
        /// </summary>
        protected override void StopProcessing()
        {
            RemoteRunspace connectingRunspace = _connectingRemoteRunspace;
            connectingRunspace?.AbortOpen();
        }

        #endregion

        #region Private Methods

        private Runspace CreateNamedPipeRunspace(string customPipeName)
        {
            NamedPipeConnectionInfo connectionInfo = new NamedPipeConnectionInfo(customPipeName);
            return CreateNamedPipeRunspace(connectionInfo);
        }

        private Runspace CreateNamedPipeRunspace(int procId, string appDomainName)
        {
            NamedPipeConnectionInfo connectionInfo = new NamedPipeConnectionInfo(procId, appDomainName);
            return CreateNamedPipeRunspace(connectionInfo);
        }

        private Runspace CreateNamedPipeRunspace(NamedPipeConnectionInfo connectionInfo)
        {
            TypeTable typeTable = TypeTable.LoadDefaultTypeFiles();
            RemoteRunspace remoteRunspace = RunspaceFactory.CreateRunspace(connectionInfo, this.Host, typeTable) as RemoteRunspace;
            remoteRunspace.Name = NamedPipeRunspaceName;
            remoteRunspace.ShouldCloseOnPop = true;
            _connectingRemoteRunspace = remoteRunspace;

            try
            {
                remoteRunspace.Open();
                remoteRunspace.Debugger?.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
            }
            catch (RuntimeException e)
            {
                // Unwrap inner exception for original error message, if any.
                string errorMessage = (e.InnerException != null) ? (e.InnerException.Message ?? string.Empty) : string.Empty;

                if (connectionInfo.CustomPipeName != null)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new RuntimeException(
                                StringUtil.Format(
                                    RemotingErrorIdStrings.EnterPSHostProcessCannotConnectToPipe,
                                    connectionInfo.CustomPipeName,
                                    errorMessage),
                                e.InnerException),
                            "EnterPSHostProcessCannotConnectToPipe",
                            ErrorCategory.OperationTimeout,
                            this));
                }
                else
                {
                    string msgAppDomainName = connectionInfo.AppDomainName ?? NamedPipeUtils.DefaultAppDomainName;

                    ThrowTerminatingError(
                        new ErrorRecord(
                            new RuntimeException(
                                StringUtil.Format(
                                    RemotingErrorIdStrings.EnterPSHostProcessCannotConnectToProcess,
                                    msgAppDomainName,
                                    connectionInfo.ProcessId,
                                    errorMessage),
                                e.InnerException),
                            "EnterPSHostProcessCannotConnectToProcess",
                            ErrorCategory.OperationTimeout,
                            this));
                }
            }
            finally
            {
                _connectingRemoteRunspace = null;
            }

            return remoteRunspace;
        }

        private static void PrepareRunspace(Runspace runspace)
        {
            string promptFn = StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessPrompt,
                @"function global:prompt { """,
                @"$($PID)",
                @"PS $($executionContext.SessionState.Path.CurrentLocation)> "" }"
            );

            // Set prompt in pushed named pipe runspace.
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.Runspace = runspace;

                try
                {
                    // Set pushed runspace prompt.
                    ps.AddScript(promptFn).Invoke();
                }
                catch (Exception)
                {
                }
            }
        }

        private Process GetProcessById(int procId)
        {
            var process = PSHostProcessUtils.GetProcessById(procId);
            if (process is null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessNoProcessFoundWithId, procId)),
                        "EnterPSHostProcessNoProcessFoundWithId",
                        ErrorCategory.InvalidArgument,
                        this)
                    );
            }

            return process;
        }

        private Process GetProcessByHostProcessInfo(PSHostProcessInfo hostProcessInfo)
        {
            return GetProcessById(hostProcessInfo.ProcessId);
        }

        private Process GetProcessByName(string name)
        {
            Collection<Process> foundProcesses;

            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                ps.AddCommand("Get-Process").AddParameter("Name", name);
                foundProcesses = ps.Invoke<Process>();
            }

            if (foundProcesses.Count == 0)
            {
                ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessNoProcessFoundWithName, name)),
                            "EnterPSHostProcessNoProcessFoundWithName",
                            ErrorCategory.InvalidArgument,
                            this)
                        );
            }
            else if (foundProcesses.Count > 1)
            {
                ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessMultipleProcessesFoundWithName, name)),
                            "EnterPSHostProcessMultipleProcessesFoundWithName",
                            ErrorCategory.InvalidArgument,
                            this)
                        );
            }

            return foundProcesses[0];
        }

        private void VerifyProcess(Process process)
        {
            if (process.Id == Environment.ProcessId)
            {
                ThrowTerminatingError(
                        new ErrorRecord(
                            new PSInvalidOperationException(RemotingErrorIdStrings.EnterPSHostProcessCantEnterSameProcess),
                            "EnterPSHostProcessCantEnterSameProcess",
                            ErrorCategory.InvalidOperation,
                            this)
                        );
            }

            bool hostsSMA = false;
            IReadOnlyCollection<PSHostProcessInfo> availableProcInfo = GetPSHostProcessInfoCommand.GetAppDomainNamesFromProcessId(null);
            foreach (var procInfo in availableProcInfo)
            {
                if (process.Id == procInfo.ProcessId)
                {
                    hostsSMA = true;
                    break;
                }
            }

            if (!hostsSMA)
            {
                ThrowTerminatingError(
                        new ErrorRecord(
                            new PSInvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessNoPowerShell, Process.Id)),
                            "EnterPSHostProcessNoPowerShell",
                            ErrorCategory.InvalidOperation,
                            this)
                        );
            }
        }

        private void VerifyPipeName(string customPipeName)
        {
            // Named Pipes are represented differently on Windows vs macOS & Linux
            var sb = new StringBuilder(customPipeName.Length);
            if (Platform.IsWindows)
            {
                sb.Append(@"\\.\pipe\");
            }
            else
            {
                sb.Append(Path.GetTempPath()).Append("CoreFxPipe_");
            }

            sb.Append(customPipeName);

            string pipePath = sb.ToString();
            if (!File.Exists(pipePath))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.EnterPSHostProcessNoNamedPipeFound, customPipeName)),
                        "EnterPSHostProcessNoNamedPipeFound",
                        ErrorCategory.InvalidArgument,
                        this));
            }
        }

        #endregion
    }

    /// <summary>
    /// This cmdlet exits an interactive session with a local process.
    /// </summary>
    [Cmdlet(VerbsCommon.Exit, "PSHostProcess",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096583")]
    public sealed class ExitPSHostProcessCommand : PSCmdlet
    {
        #region Overrides

        /// <summary>
        /// Process Record.
        /// </summary>
        protected override void ProcessRecord()
        {
            var _interactiveHost = this.Host as IHostSupportsInteractiveSession;
            if (_interactiveHost == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ArgumentException(RemotingErrorIdStrings.HostDoesNotSupportIASession),
                        "ExitPSHostProcessHostDoesNotSupportIASession",
                        ErrorCategory.InvalidArgument,
                        null));

                return;
            }

            _interactiveHost.PopRunspace();
        }

        #endregion
    }

    /// <summary>
    /// This cmdlet returns a collection of PSHostProcessInfo objects containing
    /// process and AppDomain name information for processes that have PowerShell loaded.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSHostProcessInfo", DefaultParameterSetName = GetPSHostProcessInfoCommand.ProcessNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=517012")]
    [OutputType(typeof(PSHostProcessInfo))]
    public sealed class GetPSHostProcessInfoCommand : PSCmdlet
    {
        #region Strings

        private const string ProcessParameterSet = "ProcessParameterSet";
        private const string ProcessIdParameterSet = "ProcessIdParameterSet";
        private const string ProcessNameParameterSet = "ProcessNameParameterSet";

#if UNIX
        // CoreFx uses the system temp path to store the file used for named pipes and is not settable.
        // This member is only used by Get-PSHostProcessInfo to know where to look for the named pipe files.
        private static readonly string NamedPipePath = Path.GetTempPath();
#else
        private const string NamedPipePath = @"\\.\pipe\";
#endif

        #endregion

        #region Parameters

        /// <summary>
        /// Name of Process.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = GetPSHostProcessInfoCommand.ProcessNameParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty()]
        public string[] Name
        {
            get;
            set;
        }

        /// <summary>
        /// Process.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = GetPSHostProcessInfoCommand.ProcessParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty()]
        public Process[] Process
        {
            get;
            set;
        }

        /// <summary>
        /// Id of process.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = GetPSHostProcessInfoCommand.ProcessIdParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty()]
        public int[] Id
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// End bock processing.
        /// </summary>
        protected override void EndProcessing()
        {
            IReadOnlyCollection<PSHostProcessInfo> processAppDomainInfo;
            switch (ParameterSetName)
            {
                case ProcessNameParameterSet:
                    processAppDomainInfo = GetAppDomainNamesFromProcessId(GetProcIdsFromNames(Name));
                    break;

                case ProcessIdParameterSet:
                    processAppDomainInfo = GetAppDomainNamesFromProcessId(Id);
                    break;

                case ProcessParameterSet:
                    processAppDomainInfo = GetAppDomainNamesFromProcessId(GetProcIdsFromProcs(Process));
                    break;

                default:
                    Debug.Fail("Unknown parameter set.");
                    processAppDomainInfo = new ReadOnlyCollection<PSHostProcessInfo>(new Collection<PSHostProcessInfo>());
                    break;
            }

            WriteObject(processAppDomainInfo, true);
        }

        #endregion

        #region Private Methods

        private static int[] GetProcIdsFromProcs(Process[] processes)
        {
            List<int> returnIds = new List<int>();
            foreach (Process process in processes)
            {
                returnIds.Add(process.Id);
            }

            return returnIds.ToArray();
        }

        private static int[] GetProcIdsFromNames(string[] names)
        {
            if ((names == null) || (names.Length == 0))
            {
                return null;
            }

            List<int> returnIds = new List<int>();
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (string name in names)
            {
                WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
                foreach (var proc in processes)
                {
                    // Skip processes that have already terminated.
                    if (proc.HasExited)
                    {
                        continue;
                    }

                    try
                    {
                        if (namePattern.IsMatch(proc.ProcessName))
                        {
                            returnIds.Add(proc.Id);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore if process has exited in the mean time.
                    }
                }
            }

            return returnIds.ToArray();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Returns all named pipe AppDomain names for given process Ids or all PowerShell
        /// processes if procIds parameter is null.
        /// PowerShell pipe name example:
        ///     PSHost.130566795082911445.8224.DefaultAppDomain.powershell.
        /// </summary>
        /// <param name="procIds">Process Ids or null.</param>
        /// <returns>Collection of process AppDomain info.</returns>
        internal static IReadOnlyCollection<PSHostProcessInfo> GetAppDomainNamesFromProcessId(int[] procIds)
        {
            var procAppDomainInfo = new List<PSHostProcessInfo>();

            // Get all named pipe 'files' on local machine.
            List<string> namedPipes = new List<string>();
            var namedPipeDirectory = new DirectoryInfo(NamedPipePath);
            foreach (var pipeFileInfo in namedPipeDirectory.EnumerateFiles(NamedPipeUtils.NamedPipeNamePrefixSearch))
            {
                namedPipes.Add(Path.Combine(pipeFileInfo.DirectoryName, pipeFileInfo.Name));
            }

            // Collect all PowerShell named pipes for given process Ids.
            foreach (string namedPipe in namedPipes)
            {
                int startIndex = namedPipe.IndexOf(NamedPipeUtils.NamedPipeNamePrefix, StringComparison.OrdinalIgnoreCase);
                if (startIndex > -1)
                {
                    int pStartTimeIndex = namedPipe.IndexOf('.', startIndex);
                    if (pStartTimeIndex > -1)
                    {
                        int pIdIndex = namedPipe.IndexOf('.', pStartTimeIndex + 1);
                        if (pIdIndex > -1)
                        {
                            int pAppDomainIndex = namedPipe.IndexOf('.', pIdIndex + 1);
                            if (pAppDomainIndex > -1)
                            {
                                ReadOnlySpan<char> idString = namedPipe.AsSpan(pIdIndex + 1, (pAppDomainIndex - pIdIndex - 1));
                                int id = -1;
                                if (int.TryParse(idString, out id))
                                {
                                    // Filter on provided proc Ids.
                                    if (procIds != null)
                                    {
                                        bool found = false;
                                        foreach (int procId in procIds)
                                        {
                                            if (id == procId)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (!found)
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    // Process id is not valid so we'll skip
                                    continue;
                                }

                                int pNameIndex = namedPipe.IndexOf('.', pAppDomainIndex + 1);
                                if (pNameIndex > -1)
                                {
                                    string appDomainName = namedPipe.Substring(pAppDomainIndex + 1, (pNameIndex - pAppDomainIndex - 1));
                                    string pName = namedPipe.Substring(pNameIndex + 1);

                                    Process process = null;
                                    try
                                    {
                                        process = PSHostProcessUtils.GetProcessById(id);
                                    }
                                    catch (Exception)
                                    {
                                        // Do nothing if the process no longer exists
                                    }

                                    if (process == null)
                                    {
                                        try
                                        {
                                            // If the process is gone, try removing the PSHost named pipe
                                            var pipeFile = new FileInfo(namedPipe);
                                            pipeFile.Delete();
                                        }
                                        catch (Exception)
                                        {
                                            // best effort to cleanup
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (process.ProcessName.Equals(pName, StringComparison.Ordinal))
                                            {
                                                // only add if the process name matches
                                                procAppDomainInfo.Add(new PSHostProcessInfo(pName, id, appDomainName, namedPipe));
                                            }
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // Ignore if process has exited in the mean time.
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (procAppDomainInfo.Count > 1)
            {
                // Sort list by process name.
                var comparerInfo = CultureInfo.InvariantCulture.CompareInfo;
                procAppDomainInfo.Sort((firstItem, secondItem) => comparerInfo.Compare(firstItem.ProcessName, secondItem.ProcessName, CompareOptions.IgnoreCase));
            }

            return new ReadOnlyCollection<PSHostProcessInfo>(procAppDomainInfo);
        }

        #endregion
    }

    #region PSHostProcessInfo class

    /// <summary>
    /// PowerShell host process information class.
    /// </summary>
    public sealed class PSHostProcessInfo
    {
        #region Members

        private readonly string _pipeNameFilePath;

        #endregion

        #region Properties

        /// <summary>
        /// Name of process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Id of process.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Name of PowerShell AppDomain in process.
        /// </summary>
        public string AppDomainName { get; }

        /// <summary>
        /// Main window title of the process.
        /// </summary>
        public string MainWindowTitle { get; }

        #endregion

        #region Constructors

        private PSHostProcessInfo() { }

        /// <summary>
        /// Initializes a new instance of the PSHostProcessInfo type.
        /// </summary>
        /// <param name="processName">Name of process.</param>
        /// <param name="processId">Id of process.</param>
        /// <param name="appDomainName">Name of process AppDomain.</param>
        /// <param name="pipeNameFilePath">File path of pipe name.</param>
        internal PSHostProcessInfo(
            string processName,
            int processId,
            string appDomainName,
            string pipeNameFilePath)
        {
            if (string.IsNullOrEmpty(processName))
            {
                throw new PSArgumentNullException(nameof(processName));
            }

            if (string.IsNullOrEmpty(appDomainName))
            {
                throw new PSArgumentNullException(nameof(appDomainName));
            }

            MainWindowTitle = string.Empty;
            try
            {
                var process = PSHostProcessUtils.GetProcessById(processId);
                MainWindowTitle = process?.MainWindowTitle ?? string.Empty;
            }
            catch (ArgumentException)
            {
                // Window title is optional.
            }
            catch (InvalidOperationException)
            {
                // Window title is optional.
            }

            this.ProcessName = processName;
            this.ProcessId = processId;
            this.AppDomainName = appDomainName;
            _pipeNameFilePath = pipeNameFilePath;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the pipe name file path.
        /// </summary>
        /// <returns>Pipe name file path.</returns>
        public string GetPipeNameFilePath()
        {
            return _pipeNameFilePath;
        }

        #endregion
    }

    #endregion

    #region PSHostProcessUtils

    internal static class PSHostProcessUtils
    {
        /// <summary>
        /// Return a System.Diagnostics.Process object by process Id,
        /// or null if not found or process has exited.
        /// </summary>
        /// <param name="procId">Process of Id to find.</param>
        /// <returns>Process object or null.</returns>
        public static Process GetProcessById(int procId)
        {
            try
            {
                var process = Process.GetProcessById(procId);
                return process.HasExited ? null : process;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }
    }

    #endregion
}
