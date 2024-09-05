// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel; // Win32Exception
using System.Diagnostics; // Process class
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Commands.Internal;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Commands
{
    #region ProcessBaseCommand
    /// <summary>
    /// This class implements the base for process commands.
    /// </summary>
    public abstract class ProcessBaseCommand : Cmdlet
    {
        #region Parameters
        /// <summary>
        /// The various process selection modes.
        /// </summary>
        internal enum MatchMode
        {
            /// <summary>
            /// Select all processes.
            /// </summary>
            All,
            /// <summary>
            /// Select processes matching the supplied names.
            /// </summary>
            ByName,
            /// <summary>
            /// Select the processes matching the id.
            /// </summary>
            ById,
            /// <summary>
            /// Select the processes specified as input.
            /// </summary>
            ByInput
        }
        /// <summary>
        /// The current process selection mode.
        /// </summary>
        internal MatchMode myMode = MatchMode.All;

        /// <remarks>
        /// The Name parameter is declared in subclasses,
        /// since it is optional for GetProcess and mandatory for StopProcess.
        /// </remarks>
        internal string[] processNames = null;

        // The Id parameter is declared in subclasses,
        // since it is positional for StopProcess but not for GetProcess.
        internal int[] processIds = null;

        /// <summary>
        /// If the input is a stream of [collections of]
        /// Process objects, we bypass the Name and
        /// Id parameters and read the Process objects
        /// directly.  This allows us to deal with processes which
        /// have wildcard characters in their name.
        /// </summary>
        /// <value>Process objects</value>
        [Parameter(
            ParameterSetName = "InputObject",
            Mandatory = true,
            ValueFromPipeline = true)]
        public virtual Process[] InputObject
        {
            get
            {
                return _input;
            }

            set
            {
                myMode = MatchMode.ByInput;
                _input = value;
            }
        }

        private Process[] _input = null;
        #endregion Parameters

        #region Internal

        // We use a Dictionary to optimize the check whether the object
        // is already in the list.
        private List<Process> _matchingProcesses = new();
        private readonly Dictionary<int, Process> _keys = new();

        /// <summary>
        /// Retrieve the list of all processes matching the Name, Id
        /// and InputObject parameters, sorted by Id.
        /// </summary>
        /// <returns></returns>
        internal List<Process> MatchingProcesses()
        {
            _matchingProcesses.Clear();
            switch (myMode)
            {
                case MatchMode.ById:
                    RetrieveMatchingProcessesById();
                    break;

                case MatchMode.ByInput:
                    RetrieveProcessesByInput();
                    break;
                default:
                    // Default is "Name":
                    RetrieveMatchingProcessesByProcessName();
                    break;
            }
            // 2004/12/16 Note that the processes will be sorted
            //  before being stopped.  PM confirms that this is fine.
            _matchingProcesses.Sort(ProcessComparison);
            return _matchingProcesses;
        }

        /// <summary>
        /// Sort function to sort by Name first, then Id.
        /// </summary>
        /// <param name="x">First Process object.</param>
        /// <param name="y">Second Process object.</param>
        /// <returns>
        /// As string.Compare: returns less than zero if x less than y,
        /// greater than 0 if x greater than y, 0 if x == y.
        /// </returns>
        private static int ProcessComparison(Process x, Process y)
        {
            int diff = string.Compare(
                SafeGetProcessName(x),
                SafeGetProcessName(y),
                StringComparison.OrdinalIgnoreCase);
            if (diff != 0)
                return diff;
            return SafeGetProcessId(x) - SafeGetProcessId(y);
        }

        /// <summary>
        /// Retrieves the list of all processes matching the Name
        /// parameter.
        /// Generates a non-terminating error for each specified
        /// process name which is not found even though it contains
        /// no wildcards.
        /// </summary>
        /// <returns></returns>
        private void RetrieveMatchingProcessesByProcessName()
        {
            if (processNames == null)
            {
                _matchingProcesses = new List<Process>(AllProcesses);
                return;
            }

            foreach (string pattern in processNames)
            {
                WildcardPattern wildcard =
                    WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                bool found = false;
                foreach (Process process in AllProcesses)
                {
                    if (!wildcard.IsMatch(SafeGetProcessName(process)))
                        continue;
                    found = true;
                    AddIdempotent(process);
                }

                if (!found &&
                    !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    string errorText = ProcessResources.NoProcessFoundForGivenName;
                    string errorName = nameof(ProcessResources.NoProcessFoundForGivenName);

                    if (int.TryParse(pattern, out int x) && x >= 0)
                    {
                        errorText = ProcessResources.RecommendIdTagForGivenName;
                        errorName = nameof(ProcessResources.RecommendIdTagForGivenName);
                    }

                    WriteNonTerminatingError(
                        processName: pattern,
                        processId: 0,
                        targetObject: pattern,
                        innerException: null,
                        resourceId: errorText,
                        errorId: errorName,
                        category: ErrorCategory.ObjectNotFound);
                }
            }
        }

        /// <summary>
        /// Retrieves the list of all processes matching the Id
        /// parameter.
        /// Generates a non-terminating error for each specified
        /// process ID which is not found.
        /// </summary>
        /// <returns></returns>
        private void RetrieveMatchingProcessesById()
        {
            if (processIds == null)
            {
                Diagnostics.Assert(false, "null processIds");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (int processId in processIds)
            {
                Process process;
                try
                {
                    process = Process.GetProcessById(processId);
                    AddIdempotent(process);
                }
                catch (ArgumentException)
                {
                    WriteNonTerminatingError(
                        string.Empty,
                        processId,
                        processId,
                        null,
                        ProcessResources.NoProcessFoundForGivenId,
                        "NoProcessFoundForGivenId",
                        ErrorCategory.ObjectNotFound);
                    continue;
                }
            }
        }

        /// <summary>
        /// Retrieves the list of all processes matching the InputObject
        /// parameter.
        /// </summary>
        /// <returns></returns>
        private void RetrieveProcessesByInput()
        {
            if (InputObject == null)
            {
                Diagnostics.Assert(false, "null InputObject");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (Process process in InputObject)
            {
                SafeRefresh(process);
                AddIdempotent(process);
            }
        }

        /// <summary>
        /// Gets an array of all processes.
        /// </summary>
        /// <value>An array of <see cref="Process"/> components that represents all the process resources.</value>
        /// <exception cref="System.Security.SecurityException">
        /// MSDN does not document the list of exceptions,
        /// but it is reasonable to expect that SecurityException is
        /// among them.  Errors here will terminate the cmdlet.
        /// </exception>
        internal Process[] AllProcesses => _allProcesses ??= Process.GetProcesses();

        private Process[] _allProcesses;

        /// <summary>
        /// Add <paramref name="process"/> to <see cref="_matchingProcesses"/>,
        /// but only if it is not already on  <see cref="_matchingProcesses"/>.
        /// We use a Dictionary to optimize the check whether the object
        /// is already in the list.
        /// </summary>
        /// <param name="process">Process to add to list.</param>
        private void AddIdempotent(
            Process process)
        {
            int hashCode = SafeGetProcessName(process).GetHashCode()
                ^ SafeGetProcessId(process); // XOR
            if (!_keys.ContainsKey(hashCode))
            {
                _keys.Add(hashCode, process);
                _matchingProcesses.Add(process);
            }
        }

        /// <summary>
        /// Writes a non-terminating error.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="innerException"></param>
        /// <param name="resourceId"></param>
        /// <param name="errorId"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            Process process,
            Exception innerException,
            string resourceId, string errorId,
            ErrorCategory category)
        {
            WriteNonTerminatingError(
                SafeGetProcessName(process),
                SafeGetProcessId(process),
                process,
                innerException,
                resourceId,
                errorId,
                category);
        }

        /// <summary>
        /// Writes a non-terminating error.
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="processId"></param>
        /// <param name="targetObject"></param>
        /// <param name="innerException"></param>
        /// <param name="resourceId"></param>
        /// <param name="errorId"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            string processName,
            int processId,
            object targetObject,
            Exception innerException,
            string resourceId,
            string errorId,
            ErrorCategory category)
        {
            string message = StringUtil.Format(resourceId,
                processName,
                processId,
                (innerException == null) ? string.Empty : innerException.Message);
            ProcessCommandException exception =
                new(message, innerException);
            exception.ProcessName = processName;

            WriteError(new ErrorRecord(
                exception, errorId, category, targetObject));
        }

        // The Name property is not always available, even for
        // live processes (such as the Idle process).
        internal static string SafeGetProcessName(Process process)
        {
            try
            {
                return process.ProcessName;
            }
            catch (Win32Exception)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        // 2004/12/17-JonN I saw this fail once too, so we'll play it safe
        internal static int SafeGetProcessId(Process process)
        {
            try
            {
                return process.Id;
            }
            catch (Win32Exception)
            {
                return int.MinValue;
            }
            catch (InvalidOperationException)
            {
                return int.MinValue;
            }
        }

        internal static void SafeRefresh(Process process)
        {
            try
            {
                process.Refresh();
            }
            catch (Win32Exception)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// TryHasExited is a helper function used to detect if the process has aready exited or not.
        /// </summary>
        /// <param name="process">
        /// Process whose exit status has to be checked.
        /// </param>
        /// <returns>Tre if the process has exited or else returns false.</returns>
        internal static bool TryHasExited(Process process)
        {
            bool hasExited = true;

            try
            {
                hasExited = process.HasExited;
            }
            catch (Win32Exception)
            {
                hasExited = false;
            }
            catch (InvalidOperationException)
            {
                hasExited = false;
            }

            return hasExited;
        }

        #endregion Internal
    }
    #endregion ProcessBaseCommand

    #region GetProcessCommand
    /// <summary>
    /// This class implements the get-process command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Process", DefaultParameterSetName = NameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096814", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ProcessModule), typeof(FileVersionInfo), typeof(Process))]
    public sealed class GetProcessCommand : ProcessBaseCommand
    {
        #region ParameterSetStrings

        private const string NameParameterSet = "Name";
        private const string IdParameterSet = "Id";
        private const string InputObjectParameterSet = "InputObject";
        private const string NameWithUserNameParameterSet = "NameWithUserName";
        private const string IdWithUserNameParameterSet = "IdWithUserName";
        private const string InputObjectWithUserNameParameterSet = "InputObjectWithUserName";

        #endregion ParameterSetStrings

        #region Parameters

        /// <summary>
        /// Has the list of process names on which to this command will work.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = NameParameterSet, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = NameWithUserNameParameterSet, ValueFromPipelineByPropertyName = true)]
        [Alias("ProcessName")]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get
            {
                return processNames;
            }

            set
            {
                myMode = MatchMode.ByName;
                processNames = value;
            }
        }

        /// <summary>
        /// Gets/sets an array of process IDs.
        /// </summary>
        [Parameter(ParameterSetName = IdParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = IdWithUserNameParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PID")]
        public int[] Id
        {
            get
            {
                return processIds;
            }

            set
            {
                myMode = MatchMode.ById;
                processIds = value;
            }
        }

        /// <summary>
        /// Input is a stream of [collections of] Process objects.
        /// </summary>
        [Parameter(ParameterSetName = InputObjectParameterSet, Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = InputObjectWithUserNameParameterSet, Mandatory = true, ValueFromPipeline = true)]
        public override Process[] InputObject
        {
            get
            {
                return base.InputObject;
            }

            set
            {
                base.InputObject = value;
            }
        }

        /// <summary>
        /// Include the UserName.
        /// </summary>
        [Parameter(ParameterSetName = NameWithUserNameParameterSet, Mandatory = true)]
        [Parameter(ParameterSetName = IdWithUserNameParameterSet, Mandatory = true)]
        [Parameter(ParameterSetName = InputObjectWithUserNameParameterSet, Mandatory = true)]
        public SwitchParameter IncludeUserName { get; set; }

        /// <summary>
        /// To display the modules of a process.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [Parameter(ParameterSetName = IdParameterSet)]
        [Parameter(ParameterSetName = InputObjectParameterSet)]
        [ValidateNotNull]
        public SwitchParameter Module { get; set; }

        /// <summary>
        /// To display the fileversioninfo of the main module of a process.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [Parameter(ParameterSetName = IdParameterSet)]
        [Parameter(ParameterSetName = InputObjectParameterSet)]
        [Alias("FV", "FVI")]
        [ValidateNotNull]
        public SwitchParameter FileVersionInfo { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Write the process objects.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (Process process in MatchingProcesses())
            {
                if (Module.IsPresent && FileVersionInfo.IsPresent)
                {
                    ProcessModule tempmodule = null;
                    try
                    {
                        ProcessModuleCollection modules = process.Modules;
                        foreach (ProcessModule pmodule in modules)
                        {
                            // Assigning to tempmodule to rethrow for exceptions on 64 bit machines
                            tempmodule = pmodule;
                            WriteObject(pmodule.FileVersionInfo, true);
                        }
                    }
                    catch (InvalidOperationException exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModuleFileVer, "CouldNotEnumerateModuleFileVer", ErrorCategory.PermissionDenied);
                    }
                    catch (ArgumentException exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModuleFileVer, "CouldNotEnumerateModuleFileVer", ErrorCategory.PermissionDenied);
                    }
                    catch (Win32Exception exception)
                    {
                        try
                        {
                            if (exception.HResult == 299)
                            {
                                WriteObject(tempmodule.FileVersionInfo, true);
                            }
                            else
                            {
                                WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModuleFileVer, "CouldNotEnumerateModuleFileVer", ErrorCategory.PermissionDenied);
                            }
                        }
                        catch (Win32Exception ex)
                        {
                            WriteNonTerminatingError(process, ex, ProcessResources.CouldNotEnumerateModuleFileVer, "CouldNotEnumerateModuleFileVer", ErrorCategory.PermissionDenied);
                        }
                    }
                    catch (Exception exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModuleFileVer, "CouldNotEnumerateModuleFileVer", ErrorCategory.PermissionDenied);
                    }
                }
                else if (Module.IsPresent)
                {
                    try
                    {
                        WriteObject(process.Modules, true);
                    }
                    catch (Win32Exception exception)
                    {
                        try
                        {
                            if (exception.HResult == 299)
                            {
                                WriteObject(process.Modules, true);
                            }
                            else
                            {
                                WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModules, "CouldNotEnumerateModules", ErrorCategory.PermissionDenied);
                            }
                        }
                        catch (Win32Exception ex)
                        {
                            WriteNonTerminatingError(process, ex, ProcessResources.CouldNotEnumerateModules, "CouldNotEnumerateModules", ErrorCategory.PermissionDenied);
                        }
                    }
                    catch (PipelineStoppedException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateModules, "CouldNotEnumerateModules", ErrorCategory.PermissionDenied);
                    }
                }
                else if (FileVersionInfo.IsPresent)
                {
                    try
                    {
                        ProcessModule mainModule = process.MainModule;
                        if (mainModule != null)
                        {
                            WriteObject(mainModule.FileVersionInfo, true);
                        }
                    }
                    catch (InvalidOperationException exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateFileVer, "CouldNotEnumerateFileVer", ErrorCategory.PermissionDenied);
                    }
                    catch (ArgumentException exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateFileVer, "CouldNotEnumerateFileVer", ErrorCategory.PermissionDenied);
                    }
                    catch (Win32Exception exception)
                    {
                        try
                        {
                            if (exception.HResult == 299)
                            {
                                WriteObject(process.MainModule?.FileVersionInfo, true);
                            }
                            else
                            {
                                WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateFileVer, "CouldNotEnumerateFileVer", ErrorCategory.PermissionDenied);
                            }
                        }
                        catch (Win32Exception ex)
                        {
                            WriteNonTerminatingError(process, ex, ProcessResources.CouldNotEnumerateFileVer, "CouldNotEnumerateFileVer", ErrorCategory.PermissionDenied);
                        }
                    }
                    catch (Exception exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.CouldNotEnumerateFileVer, "CouldNotEnumerateFileVer", ErrorCategory.PermissionDenied);
                    }
                }
                else
                {
                    WriteObject(IncludeUserName.IsPresent ? AddUserNameToProcess(process) : process);
                }
            }
        }

        #endregion Overrides

        #region Privates

        /// <summary>
        /// New PSTypeName added to the process object.
        /// </summary>
        private const string TypeNameForProcessWithUserName = "System.Diagnostics.Process#IncludeUserName";

        /// <summary>
        /// Add the 'UserName' NoteProperty to the Process object.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static PSObject AddUserNameToProcess(Process process)
        {
            // Return null if we failed to get the owner information
            string userName = RetrieveProcessUserName(process);

            PSObject processAsPsobj = PSObject.AsPSObject(process);
            PSNoteProperty noteProperty = new("UserName", userName);

            processAsPsobj.Properties.Add(noteProperty, true);
            processAsPsobj.TypeNames.Insert(0, TypeNameForProcessWithUserName);

            return processAsPsobj;
        }

        /// <summary>
        /// Retrieve the UserName through PInvoke.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static string RetrieveProcessUserName(Process process)
        {
            string userName = null;
#if UNIX
            userName = Platform.NonWindowsGetUserFromPid(process.Id);
#else
            IntPtr tokenUserInfo = IntPtr.Zero;
            IntPtr processTokenHandler = IntPtr.Zero;

            const uint TOKEN_QUERY = 0x0008;

            try
            {
                int error;
                if (!Win32Native.OpenProcessToken(process.Handle, TOKEN_QUERY, out processTokenHandler))
                {
                    return null;
                }

                // Set the default length to be 256, so it will be sufficient for most cases.
                int tokenInfoLength = 256;
                tokenUserInfo = Marshal.AllocHGlobal(tokenInfoLength);
                if (!Win32Native.GetTokenInformation(processTokenHandler, Win32Native.TOKEN_INFORMATION_CLASS.TokenUser, tokenUserInfo, tokenInfoLength, out tokenInfoLength))
                {
                    error = Marshal.GetLastWin32Error();
                    if (error == Win32Native.ERROR_INSUFFICIENT_BUFFER)
                    {
                        Marshal.FreeHGlobal(tokenUserInfo);
                        tokenUserInfo = Marshal.AllocHGlobal(tokenInfoLength);

                        if (!Win32Native.GetTokenInformation(processTokenHandler, Win32Native.TOKEN_INFORMATION_CLASS.TokenUser, tokenUserInfo, tokenInfoLength, out tokenInfoLength))
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                var tokenUser = Marshal.PtrToStructure<Win32Native.TOKEN_USER>(tokenUserInfo);
                SecurityIdentifier sid = new SecurityIdentifier(tokenUser.User.Sid);
                userName = sid.Translate(typeof(System.Security.Principal.NTAccount)).Value;
            }
            catch (NotSupportedException)
            {
                // The Process not started yet, or it's a process from a remote machine.
            }
            catch (IdentityNotMappedException)
            {
                // SID cannot be mapped to a user
            }
            catch (InvalidOperationException)
            {
                // The Process has exited, Process.Handle will raise this exception.
            }
            catch (Win32Exception)
            {
                // We might get an AccessDenied error.
            }
            catch (Exception)
            {
                // I don't expect to get other exceptions.
            }
            finally
            {
                if (tokenUserInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenUserInfo);
                }

                if (processTokenHandler != IntPtr.Zero)
                {
                    Win32Native.CloseHandle(processTokenHandler);
                }
            }
#endif
            return userName;
        }

        #endregion Privates
    }
    #endregion GetProcessCommand

    #region WaitProcessCommand
    /// <summary>
    /// This class implements the Wait-process command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "Process", DefaultParameterSetName = "Name", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097146")]
    [OutputType(typeof(Process))]
    public sealed class WaitProcessCommand : ProcessBaseCommand
    {
        #region Parameters

        /// <summary>
        /// Specifies the process IDs of the processes to be waited on.
        /// </summary>
        [Parameter(
            ParameterSetName = "Id",
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PID", "ProcessId")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Id
        {
            get
            {
                return processIds;
            }

            set
            {
                myMode = MatchMode.ById;
                processIds = value;
            }
        }

        /// <summary>
        /// Name of the processes to wait on for termination.
        /// </summary>
        [Parameter(
            ParameterSetName = "Name",
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [Alias("ProcessName")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get
            {
                return processNames;
            }

            set
            {
                myMode = MatchMode.ByName;
                processNames = value;
            }
        }

        /// <summary>
        /// If specified, wait for this number of seconds.
        /// </summary>
        [Parameter(Position = 1)]
        [Alias("TimeoutSec")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, 32767)]
        public int Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                _timeout = value;
                _timeOutSpecified = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to return after any one process exits.
        /// </summary>
        [Parameter]
        public SwitchParameter Any { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to return the Process objects after waiting.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        private int _timeout = 0;
        private bool _timeOutSpecified;

        #endregion Parameters

        private bool _disposed = false;

        #region IDisposable
        /// <summary>
        /// Dispose method of IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_waitHandle != null)
                {
                    _waitHandle.Dispose();
                    _waitHandle = null;
                }

                _disposed = true;
            }
        }

        #endregion

        #region private methods
        // Handle Exited event and display process information.
        private void myProcess_Exited(object sender, System.EventArgs e)
        {
            if (Any || (Interlocked.Decrement(ref _numberOfProcessesToWaitFor) == 0))
            {
                _waitHandle?.Set();
            }
        }

        #endregion

        #region Overrides

        private readonly List<Process> _processList = new();

        // Wait handle which is used by thread to sleep.
        private ManualResetEvent _waitHandle;
        private int _numberOfProcessesToWaitFor;

        /// <summary>
        /// Gets the list of process.
        /// </summary>
        protected override void ProcessRecord()
        {
            // adding the processes into the list
            foreach (Process process in MatchingProcesses())
            {
                // Idle process has processid zero,so handle that because we cannot wait on it.
                if (process.Id == 0)
                {
                    WriteNonTerminatingError(process, null, ProcessResources.WaitOnIdleProcess, "WaitOnIdleProcess", ErrorCategory.ObjectNotFound);
                    continue;
                }

                // It cannot wait on itself
                if (process.Id.Equals(Environment.ProcessId))
                {
                    WriteNonTerminatingError(process, null, ProcessResources.WaitOnItself, "WaitOnItself", ErrorCategory.ObjectNotFound);
                    continue;
                }

                _processList.Add(process);
            }
        }

        /// <summary>
        /// Wait for the process to terminate.
        /// </summary>
        protected override void EndProcessing()
        {
            _waitHandle = new ManualResetEvent(false);
            foreach (Process process in _processList)
            {
                try
                {
                    // Check for processes that exit too soon for us to add an event.
                    if (Any && process.HasExited)
                    {
                        _waitHandle.Set();
                    }
                    else if (!process.HasExited)
                    {
                        process.EnableRaisingEvents = true;
                        process.Exited += myProcess_Exited;
                        if (!process.HasExited)
                        {
                            System.Threading.Interlocked.Increment(ref _numberOfProcessesToWaitFor);
                        }
                    }
                }
                catch (Win32Exception exception)
                {
                    WriteNonTerminatingError(process, exception, ProcessResources.ProcessIsNotTerminated, "ProcessNotTerminated", ErrorCategory.CloseError);
                }
            }

            bool hasTimedOut = false;
            if (_numberOfProcessesToWaitFor > 0)
            {
                if (_timeOutSpecified)
                {
                    hasTimedOut = !_waitHandle.WaitOne(_timeout * 1000);
                }
                else
                {
                    _waitHandle.WaitOne();
                }
            }

            if (hasTimedOut || (!Any && _numberOfProcessesToWaitFor > 0))
            {
                foreach (Process process in _processList)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            string message = StringUtil.Format(ProcessResources.ProcessNotTerminated, new object[] { process.ProcessName, process.Id });
                            ErrorRecord errorRecord = new(new TimeoutException(message), "ProcessNotTerminated", ErrorCategory.CloseError, process);
                            WriteError(errorRecord);
                        }
                    }
                    catch (Win32Exception exception)
                    {
                        WriteNonTerminatingError(process, exception, ProcessResources.ProcessIsNotTerminated, "ProcessNotTerminated", ErrorCategory.CloseError);
                    }
                }
            }

            if (PassThru)
            {
                WriteObject(_processList, enumerateCollection: true);
            }
        }

        /// <summary>
        /// StopProcessing.
        /// </summary>
        protected override void StopProcessing() => _waitHandle?.Set();

        #endregion Overrides

    }
    #endregion WaitProcessCommand

    #region StopProcessCommand
    /// <summary>
    /// This class implements the stop-process command.
    /// </summary>
    /// <remarks>
    /// Processes will be sorted before being stopped.  PM confirms
    /// that this should be fine.
    /// </remarks>
    [Cmdlet(VerbsLifecycle.Stop, "Process",
        DefaultParameterSetName = "Id",
        SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097058")]
    [OutputType(typeof(Process))]
    public sealed class StopProcessCommand : ProcessBaseCommand
    {
        #region Parameters
        /// <summary>
        /// Has the list of process names on which to this command will work.
        /// </summary>
        [Parameter(
            ParameterSetName = "Name",
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [Alias("ProcessName")]
        public string[] Name
        {
            get
            {
                return processNames;
            }

            set
            {
                processNames = value;
                myMode = MatchMode.ByName;
            }
        }

        /// <summary>
        /// Gets/sets an array of process IDs.
        /// </summary>
        [Parameter(
           Position = 0,
           ParameterSetName = "Id",
           Mandatory = true,
           ValueFromPipelineByPropertyName = true)]
        public int[] Id
        {
            get
            {
                return processIds;
            }

            set
            {
                myMode = MatchMode.ById;
                processIds = value;
            }
        }

        /// <summary>
        /// Gets/sets an array of objects.
        /// </summary>
        [Parameter(
            Position = 0,
            ParameterSetName = "InputObject",
            Mandatory = true,
            ValueFromPipeline = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new Process[] InputObject
        {
            get
            {
                return base.InputObject;
            }

            set
            {
                base.InputObject = value;
            }
        }

        private bool _passThru;
        /// <summary>
        /// The updated process object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        /// <summary>
        /// Specifies whether to force a process to kill
        /// even if it has dependent services.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public SwitchParameter Force { get; set; }

        #endregion Parameters

        #region Overrides
        /// <summary>
        /// Kill the processes.
        /// It is a non-terminating error if the Process.Kill() operation fails.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (myMode == MatchMode.All || (myMode == MatchMode.ByName && processNames == null))
            {
                Diagnostics.Assert(false, "trying to kill all processes");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (Process process in MatchingProcesses())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                // 2005-06-21 Moved this ahead of the hasExited check
                string targetString = StringUtil.Format(
                            ProcessResources.ProcessNameForConfirmation,
                            SafeGetProcessName(process),
                            SafeGetProcessId(process));

                if (!ShouldProcess(targetString))
                {
                    continue;
                }

                try
                {
                    // Many properties including Name are not available if the process has exited.
                    // If this is the case, we skip the process. If the process is from a remote
                    // machine, then we generate a non-terminating error because .NET doesn't support
                    // terminate a remote process.
                    if (process.HasExited)
                    {
                        if (PassThru)
                            WriteObject(process);
                        continue;
                    }
                }
                catch (NotSupportedException ex)
                {
                    WriteNonTerminatingError(
                        process, ex, ProcessResources.CouldNotStopProcess,
                        "CouldNotStopProcess", ErrorCategory.InvalidOperation);
                    continue;
                }
                catch (Win32Exception ex)
                {
                    WriteNonTerminatingError(
                        process, ex, ProcessResources.CouldNotStopProcess,
                        "CouldNotStopProcess", ErrorCategory.CloseError);
                    continue;
                }

                try
                {
                    if (Environment.ProcessId == SafeGetProcessId(process))
                    {
                        _shouldKillCurrentProcess = true;
                        continue;
                    }

                    if (Platform.IsWindows && !Force)
                    {
                        if (!IsProcessOwnedByCurrentUser(process))
                        {
                            string message = StringUtil.Format(
                                        ProcessResources.ConfirmStopProcess,
                                        SafeGetProcessName(process),
                                        SafeGetProcessId(process));

                            // caption: null = default caption
                            if (!ShouldContinue(message, null, ref _yesToAll, ref _noToAll))
                                continue;
                        }
                    }

                    // If the process is svchost stop all the dependent services before killing process
                    if (string.Equals(SafeGetProcessName(process), "SVCHOST", StringComparison.OrdinalIgnoreCase))
                    {
                        StopDependentService(process);
                    }

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Win32Exception exception)
                {
                    if (!TryHasExited(process))
                    {
                        WriteNonTerminatingError(
                            process, exception, ProcessResources.CouldNotStopProcess,
                            "CouldNotStopProcess", ErrorCategory.CloseError);
                        continue;
                    }
                }
                catch (InvalidOperationException exception)
                {
                    if (!TryHasExited(process))
                    {
                        WriteNonTerminatingError(
                            process, exception, ProcessResources.CouldNotStopProcess,
                            "CouldNotStopProcess", ErrorCategory.CloseError);
                        continue;
                    }
                }

                if (PassThru)
                    WriteObject(process);
            }
        }

        /// <summary>
        /// Kill the current process here.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_shouldKillCurrentProcess)
            {
                StopProcess(Process.GetCurrentProcess());
            }
        }

        #endregion Overrides

        #region Private
        /// <summary>
        /// Should the current powershell process to be killed.
        /// </summary>
        private bool _shouldKillCurrentProcess;

        /// <summary>
        /// Boolean variables to display the warning using ShouldContinue.
        /// </summary>
        private bool _yesToAll, _noToAll;

        /// <summary>
        /// Current windows user name.
        /// </summary>
        private string _currentUserName;

        /// <summary>
        /// Gets the owner of the process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns>Returns the owner.</returns>
        private bool IsProcessOwnedByCurrentUser(Process process)
        {
            const uint TOKEN_QUERY = 0x0008;
            IntPtr ph = IntPtr.Zero;
            try
            {
                if (Win32Native.OpenProcessToken(process.Handle, TOKEN_QUERY, out ph))
                {
                    if (_currentUserName == null)
                    {
                        using (var currentUser = WindowsIdentity.GetCurrent())
                        {
                            _currentUserName = currentUser.Name;
                        }
                    }

                    using (var processUser = new WindowsIdentity(ph))
                    {
                        return string.Equals(processUser.Name, _currentUserName, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (IdentityNotMappedException)
            {
                // Catching IdentityMappedException
                // Need not throw error.
            }
            catch (ArgumentException)
            {
                // Catching ArgumentException. In Win2k3 Token is zero
                // Need not throw error.
            }
            finally
            {
                if (ph != IntPtr.Zero)
                {
                    Win32Native.CloseHandle(ph);
                }
            }

            return false;
        }

        /// <summary>
        /// Stop the service that depends on the process and its child services.
        /// </summary>
        /// <param name="process"></param>
        private void StopDependentService(Process process)
        {
            string queryString = "Select * From Win32_Service Where ProcessId=" + SafeGetProcessId(process) + " And State !='Stopped'";

            try
            {
                using (CimSession cimSession = CimSession.Create(null))
                {
                    IEnumerable<CimInstance> serviceList =
                        cimSession.QueryInstances("root/cimv2", "WQL", queryString);
                    foreach (CimInstance oService in serviceList)
                    {
                        string serviceName = oService.CimInstanceProperties["Name"].Value.ToString();
                        using (var service = new System.ServiceProcess.ServiceController(serviceName))
                        {
                            try
                            {
                                service.Stop();
                                // Wait 2 sec for the status to become 'Stopped'
                                service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 2));
                            }
                            catch (Win32Exception) { }
                            catch (InvalidOperationException) { }
                            catch (System.ServiceProcess.TimeoutException) { }
                        }
                    }
                }
            }
            catch (CimException ex)
            {
                var errorRecord = new ErrorRecord(ex, "GetCimException", ErrorCategory.InvalidOperation, null);
                WriteError(errorRecord);
            }
        }

        /// <summary>
        /// Stops the given process throws non terminating error if can't.
        /// </summary>
        /// <param name="process">Process to be stopped.</param>
        /// <returns>True if process stopped successfully else false.</returns>
        private void StopProcess(Process process)
        {
            Exception exception = null;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Win32Exception e)
            {
                exception = e;
            }
            catch (InvalidOperationException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                if (!TryHasExited(process))
                {
                    // This process could not be stopped,
                    // so write a non-terminating error.
                    WriteNonTerminatingError(
                        process, exception, ProcessResources.CouldNotStopProcess,
                        "CouldNotStopProcess", ErrorCategory.CloseError);
                }
            }
        }

        #endregion Private
    }
    #endregion StopProcessCommand

    #region DebugProcessCommand
    /// <summary>
    /// This class implements the Debug-process command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Debug, "Process", DefaultParameterSetName = "Name", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096809")]
    public sealed class DebugProcessCommand : ProcessBaseCommand
    {
        #region Parameters

        /// <summary>
        /// Specifies the process IDs of the processes to be waited on.
        /// </summary>
        [Parameter(
            ParameterSetName = "Id",
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PID", "ProcessId")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Id
        {
            get
            {
                return processIds;
            }

            set
            {
                myMode = MatchMode.ById;
                processIds = value;
            }
        }

        /// <summary>
        /// Name of the processes to wait on for termination.
        /// </summary>
        [Parameter(
            ParameterSetName = "Name",
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [Alias("ProcessName")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get
            {
                return processNames;
            }

            set
            {
                myMode = MatchMode.ByName;
                processNames = value;
            }
        }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Gets the list of process and attach the debugger to the processes.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (Process process in MatchingProcesses())
            {
                string targetMessage = StringUtil.Format(
                            ProcessResources.ProcessNameForConfirmation,
                            SafeGetProcessName(process),
                            SafeGetProcessId(process));

                if (!ShouldProcess(targetMessage))
                {
                    continue;
                }

                // Sometimes Idle process has processid zero,so handle that because we cannot attach debugger to it.
                if (process.Id == 0)
                {
                    WriteNonTerminatingError(
                        process, null, ProcessResources.NoDebuggerFound,
                        "NoDebuggerFound", ErrorCategory.ObjectNotFound);
                    continue;
                }

                try
                {
                    // If the process has exited, we skip it. If the process is from a remote
                    // machine, then we generate a non-terminating error.
                    if (process.HasExited)
                    {
                        continue;
                    }
                }
                catch (NotSupportedException ex)
                {
                    WriteNonTerminatingError(
                        process, ex, ProcessResources.CouldNotDebugProcess,
                        "CouldNotDebugProcess", ErrorCategory.InvalidOperation);
                    continue;
                }
                catch (Win32Exception ex)
                {
                    // This process could not be stopped, so write a non-terminating error.
                    WriteNonTerminatingError(
                        process, ex, ProcessResources.CouldNotDebugProcess,
                        "CouldNotDebugProcess", ErrorCategory.CloseError);
                    continue;
                }

                AttachDebuggerToProcess(process);
            }
        }

        #endregion Overrides

        /// <summary>
        /// Attach debugger to the process.
        /// </summary>
        private void AttachDebuggerToProcess(Process process)
        {
            string searchQuery = "Select * From Win32_Process Where ProcessId=" + SafeGetProcessId(process);
            using (CimSession cimSession = CimSession.Create(null))
            {
                IEnumerable<CimInstance> processCollection =
                    cimSession.QueryInstances("root/cimv2", "WQL", searchQuery);
                foreach (CimInstance processInstance in processCollection)
                {
                    try
                    {
                        // Call the AttachDebugger method
                        CimMethodResult result = cimSession.InvokeMethod(processInstance, "AttachDebugger", null);
                        int returnCode = Convert.ToInt32(result.ReturnValue.Value, System.Globalization.CultureInfo.CurrentCulture);
                        if (returnCode != 0)
                        {
                            var ex = new InvalidOperationException(MapReturnCodeToErrorMessage(returnCode));
                            WriteNonTerminatingError(
                                process, ex, ProcessResources.CouldNotDebugProcess,
                                "CouldNotDebugProcess", ErrorCategory.InvalidOperation);
                        }
                    }
                    catch (CimException e)
                    {
                        string message = e.Message?.Trim();

                        var errorRecord = new ErrorRecord(
                                new InvalidOperationException(StringUtil.Format(ProcessResources.DebuggerError, message)),
                                "GetCimException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
            }
        }

        /// <summary>
        /// Map the return code from 'AttachDebugger' to error message.
        /// </summary>
        private static string MapReturnCodeToErrorMessage(int returnCode)
        {
            string errorMessage = returnCode switch
            {
                2 => ProcessResources.AttachDebuggerReturnCode2,
                3 => ProcessResources.AttachDebuggerReturnCode3,
                8 => ProcessResources.AttachDebuggerReturnCode8,
                9 => ProcessResources.AttachDebuggerReturnCode9,
                21 => ProcessResources.AttachDebuggerReturnCode21,
                _ => string.Empty
            };

            Diagnostics.Assert(!string.IsNullOrEmpty(errorMessage), "Error message should not be null or empty.");

            return errorMessage;
        }
    }
    #endregion DebugProcessCommand

    #region StartProcessCommand

    /// <summary>
    /// This class implements the Start-process command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Process", DefaultParameterSetName = "Default", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097141")]
    [OutputType(typeof(Process))]
    public sealed class StartProcessCommand : PSCmdlet, IDisposable
    {
        private ManualResetEvent _waithandle = null;
        private bool _isDefaultSetParameterSpecified = false;

        #region Parameters

        /// <summary>
        /// Path/FileName of the process to start.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "Path")]
        public string FilePath { get; set; }

        /// <summary>
        /// Arguments for the process.
        /// </summary>
        [Parameter(Position = 1)]
        [Alias("Args")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ArgumentList { get; set; }

        /// <summary>
        /// Credentials for the process.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("RunAs")]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential
        {
            get
            {
                return _credential;
            }

            set
            {
                _credential = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private PSCredential _credential;

        /// <summary>
        /// Working directory of the process.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Load user profile from registry.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("Lup")]
        public SwitchParameter LoadUserProfile
        {
            get
            {
                return _loaduserprofile;
            }

            set
            {
                _loaduserprofile = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private SwitchParameter _loaduserprofile = SwitchParameter.Present;

        /// <summary>
        /// Starts process in a new window.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("nnw")]
        public SwitchParameter NoNewWindow
        {
            get
            {
                return _nonewwindow;
            }

            set
            {
                _nonewwindow = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private SwitchParameter _nonewwindow;

        /// <summary>
        /// PassThru parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Redirect error.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("RSE")]
        [ValidateNotNullOrEmpty]
        public string RedirectStandardError
        {
            get
            {
                return _redirectstandarderror;
            }

            set
            {
                _redirectstandarderror = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private string _redirectstandarderror;

        /// <summary>
        /// Redirect input.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("RSI")]
        [ValidateNotNullOrEmpty]
        public string RedirectStandardInput
        {
            get
            {
                return _redirectstandardinput;
            }

            set
            {
                _redirectstandardinput = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private string _redirectstandardinput;

        /// <summary>
        /// Redirect output.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        [Alias("RSO")]
        [ValidateNotNullOrEmpty]
        public string RedirectStandardOutput
        {
            get
            {
                return _redirectstandardoutput;
            }

            set
            {
                _redirectstandardoutput = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private string _redirectstandardoutput;

        /// <summary>
        /// Verb.
        /// </summary>
        /// <remarks>
        /// The 'Verb' parameter is only supported on Windows Desktop.
        /// </remarks>
        [Parameter(ParameterSetName = "UseShellExecute")]
        [ValidateNotNullOrEmpty]
        [ArgumentCompleter(typeof(VerbArgumentCompleter))]
        public string Verb { get; set; }

        /// <summary>
        /// Window style of the process window.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProcessWindowStyle WindowStyle
        {
            get
            {
                return _windowstyle;
            }

            set
            {
                _windowstyle = value;
                _windowstyleSpecified = true;
            }
        }

        private ProcessWindowStyle _windowstyle = ProcessWindowStyle.Normal;
        private bool _windowstyleSpecified = false;

        /// <summary>
        /// Wait for the process to terminate.
        /// </summary>
        [Parameter]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// Default Environment.
        /// </summary>
        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter UseNewEnvironment
        {
            get
            {
                return _UseNewEnvironment;
            }

            set
            {
                _UseNewEnvironment = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private SwitchParameter _UseNewEnvironment;

        /// <summary>
        /// Gets or sets the environment variables for the process.
        /// </summary>
        [Parameter]
        public Hashtable Environment
        {
            get
            {
                return _environment;
            }

            set
            {
                _environment = value;
                _isDefaultSetParameterSpecified = true;
            }
        }

        private Hashtable _environment;

        #endregion

        #region overrides

        /// <summary>
        /// BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            string message = string.Empty;

            // -Verb and -WindowStyle are not supported on non-Windows platforms as well as Windows headless SKUs
            if (Platform.IsWindowsDesktop)
            {
                // Parameters '-NoNewWindow' and '-WindowStyle' are both valid on full windows SKUs.
                if (_nonewwindow && _windowstyleSpecified)
                {
                    message = StringUtil.Format(ProcessResources.ContradictParametersSpecified, "-NoNewWindow", "-WindowStyle");
                    ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }
            }
            else
            {
                if (this.ParameterSetName.Equals("UseShellExecute"))
                {
                    message = StringUtil.Format(ProcessResources.ParameterNotSupportedOnPSEdition, "-Verb", "Start-Process");
                }
                else if (_windowstyleSpecified)
                {
                    message = StringUtil.Format(ProcessResources.ParameterNotSupportedOnPSEdition, "-WindowStyle", "Start-Process");
                }

                if (!string.IsNullOrEmpty(message))
                {
                    ErrorRecord er = new(new NotSupportedException(message), "NotSupportedException", ErrorCategory.NotImplemented, null);
                    ThrowTerminatingError(er);
                }
            }

            ProcessStartInfo startInfo = new();
            // Use ShellExecute by default if we are running on full windows SKUs
            startInfo.UseShellExecute = Platform.IsWindowsDesktop;

            // Path = Mandatory parameter -> Will not be empty.
            try
            {
                CommandInfo cmdinfo = CommandDiscovery.LookupCommandInfo(
                    FilePath, CommandTypes.Application | CommandTypes.ExternalScript,
                    SearchResolutionOptions.None, CommandOrigin.Internal, this.Context);

                startInfo.FileName = cmdinfo.Definition;
            }
            catch (CommandNotFoundException)
            {
                startInfo.FileName = FilePath;
#if UNIX
                // Arguments are passed incorrectly to the executable used for ShellExecute and not to filename https://github.com/dotnet/corefx/issues/30718
                // so don't use ShellExecute if arguments are specified

                // Linux relies on `xdg-open` and macOS relies on `open` which behave differently than Windows ShellExecute when running console commands
                // as a new console will be opened.  So to avoid that, we only use ShellExecute on non-Windows if the filename is not an actual command (like a URI)
                startInfo.UseShellExecute = (ArgumentList == null);
#endif
            }

            if (ArgumentList != null)
            {
                startInfo.Arguments = string.Join(' ', ArgumentList);
            }

            if (WorkingDirectory != null)
            {
                // WorkingDirectory -> Not Exist -> Throw Error
                WorkingDirectory = ResolveFilePath(WorkingDirectory);
                if (!Directory.Exists(WorkingDirectory))
                {
                    message = StringUtil.Format(ProcessResources.InvalidInput, "WorkingDirectory");
                    ErrorRecord er = new(new DirectoryNotFoundException(message), "DirectoryNotFoundException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                startInfo.WorkingDirectory = WorkingDirectory;
            }
            else
            {
                // Working Directory not specified -> Assign Current Path, but only if it still exists
                var currentDirectory = PathUtils.ResolveFilePath(this.SessionState.Path.CurrentFileSystemLocation.Path, this, isLiteralPath: true);
                if (Directory.Exists(currentDirectory))
                {
                    startInfo.WorkingDirectory = currentDirectory;
                }
            }

            if (this.ParameterSetName.Equals("Default"))
            {
                if (_isDefaultSetParameterSpecified)
                {
                    startInfo.UseShellExecute = false;
                }

                if (_UseNewEnvironment)
                {
                    startInfo.EnvironmentVariables.Clear();
                    LoadEnvironmentVariable(startInfo, System.Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));
                    LoadEnvironmentVariable(startInfo, System.Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));
                }

                if (_environment != null)
                {
                    LoadEnvironmentVariable(startInfo, _environment);
                }

                startInfo.WindowStyle = _windowstyle;

                if (_nonewwindow)
                {
                    startInfo.CreateNoWindow = _nonewwindow;
                }
#if !UNIX
                startInfo.LoadUserProfile = _loaduserprofile;
#endif
                if (_credential != null)
                {
                    NetworkCredential nwcredential = _credential.GetNetworkCredential();
                    startInfo.UserName = nwcredential.UserName;
                    if (string.IsNullOrEmpty(nwcredential.Domain))
                    {
                        startInfo.Domain = ".";
                    }
                    else
                    {
                        startInfo.Domain = nwcredential.Domain;
                    }

                    startInfo.Password = _credential.Password;
                }

                // RedirectionInput File Check -> Not Exist -> Throw Error
                if (_redirectstandardinput != null)
                {
                    _redirectstandardinput = ResolveFilePath(_redirectstandardinput);
                    if (!File.Exists(_redirectstandardinput))
                    {
                        message = StringUtil.Format(ProcessResources.InvalidInput, "RedirectStandardInput '" + this.RedirectStandardInput + "'");
                        ErrorRecord er = new(new FileNotFoundException(message), "FileNotFoundException", ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                        return;
                    }
                }

                // RedirectionInput == RedirectionOutput -> Throw Error
                if (_redirectstandardinput != null && _redirectstandardoutput != null)
                {
                    _redirectstandardinput = ResolveFilePath(_redirectstandardinput);
                    _redirectstandardoutput = ResolveFilePath(_redirectstandardoutput);
                    if (_redirectstandardinput.Equals(_redirectstandardoutput, StringComparison.OrdinalIgnoreCase))
                    {
                        message = StringUtil.Format(ProcessResources.DuplicateEntry, "RedirectStandardInput", "RedirectStandardOutput");
                        ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                        return;
                    }
                }

                // RedirectionInput == RedirectionError -> Throw Error
                if (_redirectstandardinput != null && _redirectstandarderror != null)
                {
                    _redirectstandardinput = ResolveFilePath(_redirectstandardinput);
                    _redirectstandarderror = ResolveFilePath(_redirectstandarderror);
                    if (_redirectstandardinput.Equals(_redirectstandarderror, StringComparison.OrdinalIgnoreCase))
                    {
                        message = StringUtil.Format(ProcessResources.DuplicateEntry, "RedirectStandardInput", "RedirectStandardError");
                        ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                        return;
                    }
                }

                // RedirectionOutput == RedirectionError -> Throw Error
                if (_redirectstandardoutput != null && _redirectstandarderror != null)
                {
                    _redirectstandarderror = ResolveFilePath(_redirectstandarderror);
                    _redirectstandardoutput = ResolveFilePath(_redirectstandardoutput);
                    if (_redirectstandardoutput.Equals(_redirectstandarderror, StringComparison.OrdinalIgnoreCase))
                    {
                        message = StringUtil.Format(ProcessResources.DuplicateEntry, "RedirectStandardOutput", "RedirectStandardError");
                        ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                        return;
                    }
                }
            }
            else if (ParameterSetName.Equals("UseShellExecute"))
            {
                if (Verb != null)
                {
                    startInfo.Verb = Verb;
                }

                startInfo.WindowStyle = _windowstyle;
            }

            string targetMessage = StringUtil.Format(ProcessResources.StartProcessTarget, startInfo.FileName, startInfo.Arguments.Trim());
            if (!ShouldProcess(targetMessage))
            {
                return;
            }

            Process process = null;

#if !UNIX
            ProcessCollection jobObject = null;
            bool? jobAssigned = null;
#endif
            if (startInfo.UseShellExecute)
            {
                process = StartWithShellExecute(startInfo);
            }
            else
            {
#if UNIX
                process = new Process() { StartInfo = startInfo };
                SetupInputOutputRedirection(process);
                process.Start();
                if (process.StartInfo.RedirectStandardOutput)
                {
                    process.BeginOutputReadLine();
                }

                if (process.StartInfo.RedirectStandardError)
                {
                    process.BeginErrorReadLine();
                }

                if (process.StartInfo.RedirectStandardInput)
                {
                    WriteToStandardInput(process);
                }
#else
                using ProcessInformation processInfo = StartWithCreateProcess(startInfo);
                process = Process.GetProcessById(processInfo.ProcessId);

                // Starting a process as another user might make it impossible
                // to get the process handle from the S.D.Process object. Use
                // the ALL_ACCESS token from CreateProcess here to setup the
                // job object assignment early if -Wait was specified.
                // https://github.com/PowerShell/PowerShell/issues/17033
                if (Wait)
                {
                    jobObject = new();
                    jobAssigned = jobObject.AssignProcessToJobObject(processInfo.Process);
                }

                // Since the process wasn't spawned by .NET, we need to trigger .NET to get a lock on the handle of the process.
                // Otherwise, accessing properties like `ExitCode` will throw the following exception:
                //   "Process was not started by this object, so requested information cannot be determined."
                // Fetching the process handle will trigger the `Process` object to update its internal state by calling `SetProcessHandle`,
                // the result is discarded as it's not used later in this code.
                try
                {
                    _ = process.Handle;
                }
                catch (Win32Exception e)
                {
                    // If the caller was not an admin and the process was started with another user's credentials .NET
                    // won't be able to retrieve the process handle. As this is not a critical failure we treat this as
                    // a warning.
                    if (PassThru)
                    {
                        string msg = StringUtil.Format(ProcessResources.FailedToCreateProcessObject, e.Message);
                        WriteDebug(msg);
                    }
                }

                // Resume the process now that is has been set up.
                processInfo.Resume();
#endif
            }

            if (PassThru.IsPresent)
            {
                if (process != null)
                {
                    WriteObject(process);
                }
                else
                {
                    message = StringUtil.Format(ProcessResources.CannotStarttheProcess);
                    ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    ThrowTerminatingError(er);
                }
            }

            if (Wait.IsPresent)
            {
                if (process != null)
                {
                    if (!process.HasExited)
                    {
#if UNIX
                        process.WaitForExit();
#else
                        _waithandle = new ManualResetEvent(false);

                        // Create and start the job object. This may have
                        // already been done in StartWithCreateProcess.
                        jobObject ??= new();
                        if (jobAssigned == true || (jobAssigned is null && jobObject.AssignProcessToJobObject(process.SafeHandle)))
                        {
                            // Wait for the job object to finish
                            jobObject.WaitOne(_waithandle);
                        }
                        else
                        {
                            // WinBlue: 27537 Start-Process -Wait doesn't work in a remote session on Windows 7 or lower.
                            // A Remote session is in it's own job and nested job support was only added in Windows 8/Server 2012.
                            process.WaitForExit();
                        }
#endif
                    }
                }
                else
                {
                    message = StringUtil.Format(ProcessResources.CannotStarttheProcess);
                    ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    ThrowTerminatingError(er);
                }
            }
        }
        /// <summary>
        /// Implements ^c, after creating a process.
        /// </summary>
        protected override void StopProcessing() => _waithandle?.Set();

        #endregion

        #region IDisposable Overrides

        /// <summary>
        /// Dispose WaitHandle used to honor -Wait parameter.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_waithandle != null)
            {
                _waithandle.Dispose();
                _waithandle = null;
            }
        }

        #endregion

        #region Private Methods

        private string ResolveFilePath(string path)
        {
            string filepath = PathUtils.ResolveFilePath(path, this);
            return filepath;
        }

        private static void LoadEnvironmentVariable(ProcessStartInfo startinfo, IDictionary EnvironmentVariables)
        {
            var processEnvironment = startinfo.EnvironmentVariables;
            foreach (DictionaryEntry entry in EnvironmentVariables)
            {
                if (processEnvironment.ContainsKey(entry.Key.ToString()))
                {
                    processEnvironment.Remove(entry.Key.ToString());
                }

                if (entry.Value != null)
                {
                    if (entry.Key.ToString().Equals("PATH"))
                    {
#if UNIX
                        processEnvironment.Add(entry.Key.ToString(), entry.Value.ToString());
#else
                        processEnvironment.Add(entry.Key.ToString(), entry.Value.ToString() + Path.PathSeparator + System.Environment.GetEnvironmentVariable(entry.Key.ToString(), EnvironmentVariableTarget.Machine) + Path.PathSeparator + System.Environment.GetEnvironmentVariable(entry.Key.ToString(), EnvironmentVariableTarget.User));
#endif
                    }
                    else
                    {
                        processEnvironment.Add(entry.Key.ToString(), entry.Value.ToString());
                    }
                }
            }
        }

#if UNIX
        private StreamWriter _outputWriter;
        private StreamWriter _errorWriter;

        private void StdOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                _outputWriter.WriteLine(outLine.Data);
                _outputWriter.Flush();
            }
        }

        private void StdErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                _errorWriter.WriteLine(outLine.Data);
                _errorWriter.Flush();
            }
        }

        private void ExitHandler(object sendingProcess, System.EventArgs e)
        {
            // To avoid a race condition with Std*Handler, let's wait a bit before closing the streams
            // System.Timer is not supported in CoreCLR, so let's spawn a new thread to do the wait

            Thread delayedStreamClosing = new Thread(StreamClosing);
            delayedStreamClosing.Start();
        }

        private void StreamClosing()
        {
            Thread.Sleep(1000);

            _outputWriter?.Dispose();
            _errorWriter?.Dispose();
        }

        private void SetupInputOutputRedirection(Process p)
        {
            if (_redirectstandardinput != null)
            {
                p.StartInfo.RedirectStandardInput = true;
                _redirectstandardinput = ResolveFilePath(_redirectstandardinput);
            }
            else
            {
                p.StartInfo.RedirectStandardInput = false;
            }

            if (_redirectstandardoutput != null)
            {
                p.StartInfo.RedirectStandardOutput = true;
                _redirectstandardoutput = ResolveFilePath(_redirectstandardoutput);
                p.OutputDataReceived += new DataReceivedEventHandler(StdOutputHandler);

                // Can't do StreamWriter(string) in coreCLR
                _outputWriter = new StreamWriter(new FileStream(_redirectstandardoutput, FileMode.Create));
            }
            else
            {
                p.StartInfo.RedirectStandardOutput = false;
                _outputWriter = null;
            }

            if (_redirectstandarderror != null)
            {
                p.StartInfo.RedirectStandardError = true;
                _redirectstandarderror = ResolveFilePath(_redirectstandarderror);
                p.ErrorDataReceived += new DataReceivedEventHandler(StdErrorHandler);

                // Can't do StreamWriter(string) in coreCLR
                _errorWriter = new StreamWriter(new FileStream(_redirectstandarderror, FileMode.Create));
            }
            else
            {
                p.StartInfo.RedirectStandardError = false;
                _errorWriter = null;
            }

            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(ExitHandler);
        }

        private void WriteToStandardInput(Process p)
        {
            StreamWriter writer = p.StandardInput;
            using (StreamReader reader = new StreamReader(new FileStream(_redirectstandardinput, FileMode.Open)))
            {
                string line = reader.ReadToEnd();
                writer.WriteLine(line);
            }

            writer.Dispose();
        }
#else

        private SafeFileHandle GetSafeFileHandleForRedirection(string RedirectionPath, FileMode mode)
        {
            SafeFileHandle sf = null;
            try
            {
                sf = File.OpenHandle(RedirectionPath, mode, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Inheritable, FileOptions.WriteThrough);
            }
            catch (Win32Exception win32ex)
            {
                sf?.Dispose();
                string message = StringUtil.Format(ProcessResources.InvalidStartProcess, win32ex.Message);
                ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }

            return sf;
        }

        private static StringBuilder BuildCommandLine(string executableFileName, string arguments)
        {
            StringBuilder builder = new();
            string str = executableFileName.Trim();
            bool flag = str.StartsWith('"') && str.EndsWith('"');
            if (!flag)
            {
                builder.Append('"');
            }

            builder.Append(str);
            if (!flag)
            {
                builder.Append('"');
            }

            if (!string.IsNullOrEmpty(arguments))
            {
                builder.Append(' ');
                builder.Append(arguments);
            }

            return builder;
        }

        private static byte[] ConvertEnvVarsToByteArray(StringDictionary sd)
        {
            string[] array = new string[sd.Count];
            byte[] bytes = null;
            sd.Keys.CopyTo(array, 0);
            string[] strArray2 = new string[sd.Count];
            sd.Values.CopyTo(strArray2, 0);
            Array.Sort(array, strArray2, StringComparer.OrdinalIgnoreCase);
            StringBuilder builder = new();
            for (int i = 0; i < sd.Count; i++)
            {
                builder.Append(array[i]);
                builder.Append('=');
                builder.Append(strArray2[i]);
                builder.Append('\0');
            }

            builder.Append('\0');

            // Use Unicode encoding
            bytes = Encoding.Unicode.GetBytes(builder.ToString());

            return bytes;
        }

        private void SetStartupInfo(ProcessStartInfo startinfo, ref ProcessNativeMethods.STARTUPINFO lpStartupInfo, ref int creationFlags)
        {
            bool hasRedirection = false;
            // RedirectionStandardInput
            if (_redirectstandardinput != null)
            {
                hasRedirection = true;
                startinfo.RedirectStandardInput = true;
                _redirectstandardinput = ResolveFilePath(_redirectstandardinput);
                lpStartupInfo.hStdInput = GetSafeFileHandleForRedirection(_redirectstandardinput, FileMode.Open);
            }

            // RedirectionStandardOutput
            if (_redirectstandardoutput != null)
            {
                hasRedirection = true;
                startinfo.RedirectStandardOutput = true;
                _redirectstandardoutput = ResolveFilePath(_redirectstandardoutput);
                lpStartupInfo.hStdOutput = GetSafeFileHandleForRedirection(_redirectstandardoutput, FileMode.Create);
            }

            // RedirectionStandardError
            if (_redirectstandarderror != null)
            {
                hasRedirection = true;
                startinfo.RedirectStandardError = true;
                _redirectstandarderror = ResolveFilePath(_redirectstandarderror);
                lpStartupInfo.hStdError = GetSafeFileHandleForRedirection(_redirectstandarderror, FileMode.Create);
            }

            if (hasRedirection)
            {
                // Set STARTF_USESTDHANDLES only if there is redirection.
                lpStartupInfo.dwFlags = 0x100;
            }

            if (startinfo.CreateNoWindow)
            {
                // No new window: Inherit the parent process's console window
                creationFlags = 0x00000000;
            }
            else
            {
                // CREATE_NEW_CONSOLE
                creationFlags |= 0x00000010;

                // STARTF_USESHOWWINDOW
                lpStartupInfo.dwFlags |= 0x00000001;

                // On headless SKUs like NanoServer and IoT, window style can only be the default value 'Normal'.
                switch (startinfo.WindowStyle)
                {
                    case ProcessWindowStyle.Normal:
                        // SW_SHOWNORMAL
                        lpStartupInfo.wShowWindow = 1;
                        break;
                    case ProcessWindowStyle.Minimized:
                        // SW_SHOWMINIMIZED
                        lpStartupInfo.wShowWindow = 2;
                        break;
                    case ProcessWindowStyle.Maximized:
                        // SW_SHOWMAXIMIZED
                        lpStartupInfo.wShowWindow = 3;
                        break;
                    case ProcessWindowStyle.Hidden:
                        // SW_HIDE
                        lpStartupInfo.wShowWindow = 0;
                        break;
                }
            }

            // Create the new process suspended so we have a chance to get a corresponding Process object in case it terminates quickly.
            creationFlags |= 0x00000004;
        }

        /// <summary>
        /// This method will be used on all windows platforms, both full desktop and headless SKUs.
        /// </summary>
        private ProcessInformation StartWithCreateProcess(ProcessStartInfo startinfo)
        {
            ProcessNativeMethods.STARTUPINFO lpStartupInfo = new();
            ProcessNativeMethods.PROCESS_INFORMATION lpProcessInformation = new();
            int error = 0;
            GCHandle pinnedEnvironmentBlock = new();
            IntPtr AddressOfEnvironmentBlock = IntPtr.Zero;
            string message = string.Empty;

            // building the cmdline with the file name given and it's arguments
            StringBuilder cmdLine = BuildCommandLine(startinfo.FileName, startinfo.Arguments);

            try
            {
                int creationFlags = 0;

                SetStartupInfo(startinfo, ref lpStartupInfo, ref creationFlags);

                // We follow the logic:
                //   - Ignore `UseNewEnvironment` when we run a process as another user.
                //          Setting initial environment variables makes sense only for current user.
                //   - Set environment variables if they present in ProcessStartupInfo.
                if (!UseNewEnvironment)
                {
                    var environmentVars = startinfo.EnvironmentVariables;
                    if (environmentVars != null)
                    {
                        // All Windows Operating Systems that we support are Windows NT systems, so we use Unicode for environment.
                        creationFlags |= 0x400;

                        pinnedEnvironmentBlock = GCHandle.Alloc(ConvertEnvVarsToByteArray(environmentVars), GCHandleType.Pinned);
                        AddressOfEnvironmentBlock = pinnedEnvironmentBlock.AddrOfPinnedObject();
                    }
                }

                bool flag;

                if (_credential != null)
                {
                    // Run process as another user.
                    ProcessNativeMethods.LogonFlags logonFlags = 0;
                    if (startinfo.LoadUserProfile)
                    {
                        logonFlags = ProcessNativeMethods.LogonFlags.LOGON_WITH_PROFILE;
                    }

                    IntPtr password = IntPtr.Zero;
                    try
                    {
                        password = (startinfo.Password == null) ? Marshal.StringToCoTaskMemUni(string.Empty) : Marshal.SecureStringToCoTaskMemUnicode(startinfo.Password);
                        flag = ProcessNativeMethods.CreateProcessWithLogonW(startinfo.UserName, startinfo.Domain, password, logonFlags, null, cmdLine, creationFlags, AddressOfEnvironmentBlock, startinfo.WorkingDirectory, lpStartupInfo, ref lpProcessInformation);
                        if (!flag)
                        {
                            error = Marshal.GetLastWin32Error();
                            ErrorRecord er = null;

                            if (error == 0xc1)
                            {
                                message = StringUtil.Format(ProcessResources.InvalidApplication, FilePath);
                            }
                            else if (error == 0x424)
                            {
                                // The API 'CreateProcessWithLogonW' depends on the 'Secondary Logon' service, but the component 'Microsoft-Windows-SecondaryLogonService'
                                // is not installed in OneCoreUAP. We will get error code 0x424 when the service is not available.
                                message = StringUtil.Format(ProcessResources.ParameterNotSupported, "-Credential", "Start-Process");
                                er = new ErrorRecord(new NotSupportedException(message), "NotSupportedException", ErrorCategory.NotInstalled, null);
                            }
                            else
                            {
                                Win32Exception win32ex = new(error);
                                message = StringUtil.Format(ProcessResources.InvalidStartProcess, win32ex.Message);
                            }

                            er ??= new ErrorRecord(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                            ThrowTerminatingError(er);
                        }

                        goto Label_03AE;
                    }
                    finally
                    {
                        if (password != IntPtr.Zero)
                        {
                            Marshal.ZeroFreeCoTaskMemUnicode(password);
                        }
                    }
                }

                // Run process as current user.
                if (UseNewEnvironment)
                {
                    // All Windows Operating Systems that we support are Windows NT systems, so we use Unicode for environment.
                    creationFlags |= 0x400;

                    IntPtr token = WindowsIdentity.GetCurrent().Token;
                    if (!ProcessNativeMethods.CreateEnvironmentBlock(out AddressOfEnvironmentBlock, token, false))
                    {
                        Win32Exception win32ex = new(error);
                        message = StringUtil.Format(ProcessResources.InvalidStartProcess, win32ex.Message);
                        var errorRecord = new ErrorRecord(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                        ThrowTerminatingError(errorRecord);
                    }
                }

                ProcessNativeMethods.SECURITY_ATTRIBUTES lpProcessAttributes = new();
                ProcessNativeMethods.SECURITY_ATTRIBUTES lpThreadAttributes = new();
                flag = ProcessNativeMethods.CreateProcess(null, cmdLine, lpProcessAttributes, lpThreadAttributes, true, creationFlags, AddressOfEnvironmentBlock, startinfo.WorkingDirectory, lpStartupInfo, ref lpProcessInformation);
                if (!flag)
                {
                    error = Marshal.GetLastWin32Error();

                    Win32Exception win32ex = new(error);
                    message = StringUtil.Format(ProcessResources.InvalidStartProcess, win32ex.Message);
                    ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    ThrowTerminatingError(er);
                }

            Label_03AE:

                return new ProcessInformation(lpProcessInformation);
            }
            finally
            {
                if (pinnedEnvironmentBlock.IsAllocated)
                {
                    pinnedEnvironmentBlock.Free();
                }
                else
                {
                    ProcessNativeMethods.DestroyEnvironmentBlock(AddressOfEnvironmentBlock);
                }

                lpStartupInfo.Dispose();
            }
        }
#endif

        /// <summary>
        /// This method will be used only on Windows full desktop.
        /// </summary>
        private Process StartWithShellExecute(ProcessStartInfo startInfo)
        {
            Process result = null;
            try
            {
                result = Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                string message = StringUtil.Format(ProcessResources.InvalidStartProcess, ex.Message);
                ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }

            return result;
        }
        #endregion
    }

    /// <summary>
    /// Provides argument completion for Verb parameter.
    /// </summary>
    public class VerbArgumentCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Returns completion results for verb parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // -Verb is not supported on non-Windows platforms as well as Windows headless SKUs
            if (!Platform.IsWindowsDesktop)
            {
                yield break;
            }

            // Completion: Start-Process -FilePath <path> -Verb <wordToComplete>
            if (commandName.Equals("Start-Process", StringComparison.OrdinalIgnoreCase)
                && fakeBoundParameters.Contains("FilePath"))
            {
                string filePath = fakeBoundParameters["FilePath"].ToString();

                // Complete file verbs if extension exists
                if (Path.HasExtension(filePath))
                {
                    foreach (string verb in CompleteFileVerbs(filePath, wordToComplete))
                    {
                        yield return new CompletionResult(verb);
                    }

                    yield break;
                }

                // Otherwise check if command is an Application to resolve executable full path with extension
                // e.g if powershell was given, resolve to powershell.exe to get verbs
                using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);

                var commandInfo = new CmdletInfo("Get-Command", typeof(GetCommandCommand));

                ps.AddCommand(commandInfo);
                ps.AddParameter("Name", filePath);
                ps.AddParameter("CommandType", CommandTypes.Application);

                Collection<CommandInfo> commands = ps.Invoke<CommandInfo>();

                // Start-Process & Get-Command select first found application based on PATHEXT environment variable
                if (commands.Count >= 1)
                {
                    foreach (string verb in CompleteFileVerbs(commands[0].Source, wordToComplete))
                    {
                        yield return new CompletionResult(verb);
                    }
                }
            }
        }

        /// <summary>
        /// Completes file verbs.
        /// </summary>
        /// <param name="filePath">The file path to get verbs.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <returns>List of file verbs to complete.</returns>
        private static IEnumerable<string> CompleteFileVerbs(string filePath, string wordToComplete)
        {
            var verbPattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            string[] verbs = new ProcessStartInfo(filePath).Verbs;

            foreach (string verb in verbs)
            {
                if (verbPattern.IsMatch(verb))
                {
                    yield return verb;
                }
            }
        }
    }

#if !UNIX
    /// <summary>
    /// ProcessCollection is a helper class used by Start-Process -Wait cmdlet to monitor the
    /// child processes created by the main process hosted by the Start-process cmdlet.
    /// </summary>
    internal class ProcessCollection
    {
        /// <summary>
        /// JobObjectHandle is a reference to the job object used to track
        /// the child processes created by the main process hosted by the Start-Process cmdlet.
        /// </summary>
        private readonly Microsoft.PowerShell.Commands.SafeJobHandle _jobObjectHandle;

        /// <summary>
        /// ProcessCollection constructor.
        /// </summary>
        internal ProcessCollection()
        {
            IntPtr jobObjectHandleIntPtr = NativeMethods.CreateJobObject(IntPtr.Zero, null);
            _jobObjectHandle = new SafeJobHandle(jobObjectHandleIntPtr);
        }

        /// <summary>
        /// Start API assigns the process to the JobObject and starts monitoring
        /// the child processes hosted by the process created by Start-Process cmdlet.
        /// </summary>
        internal bool AssignProcessToJobObject(SafeProcessHandle process)
        {
            // Add the process to the job object
            bool result = Interop.Windows.AssignProcessToJobObject(
                _jobObjectHandle.DangerousGetHandle(),
                process.DangerousGetHandle());
            return result;
        }

        /// <summary>
        /// Checks to see if the JobObject is empty (has no assigned processes).
        /// If job is empty the auto reset event supplied as input would be set.
        /// </summary>
        internal void CheckJobStatus(object stateInfo)
        {
            ManualResetEvent emptyJobAutoEvent = (ManualResetEvent)stateInfo;
            int dwSize = 0;
            const int JOB_OBJECT_BASIC_PROCESS_ID_LIST = 3;
            JOBOBJECT_BASIC_PROCESS_ID_LIST JobList = new();

            dwSize = Marshal.SizeOf(JobList);
            if (NativeMethods.QueryInformationJobObject(_jobObjectHandle,
                JOB_OBJECT_BASIC_PROCESS_ID_LIST,
                ref JobList, dwSize, IntPtr.Zero))
            {
                if (JobList.NumberOfAssignedProcess == 0)
                {
                    emptyJobAutoEvent.Set();
                }
            }
        }

        /// <summary>
        /// WaitOne blocks the current thread until the current instance receives a signal, using
        /// a System.TimeSpan to measure the time interval and specifying whether to
        /// exit the synchronization domain before the wait.
        /// </summary>
        /// <param name="waitHandleToUse">
        /// WaitHandle to use for waiting on the job object.
        /// </param>
        internal void WaitOne(ManualResetEvent waitHandleToUse)
        {
            TimerCallback jobObjectStatusCb = this.CheckJobStatus;
            using (Timer stateTimer = new(jobObjectStatusCb, waitHandleToUse, 0, 1000))
            {
                waitHandleToUse.WaitOne();
            }
        }
    }

    /// <summary>
    /// ProcessInformation is a helper class that wraps the native PROCESS_INFORMATION structure
    /// returned by CreateProcess or CreateProcessWithLogon. It ensures the process and thread
    /// HANDLEs are disposed once it's not needed.
    /// </summary>
    internal sealed class ProcessInformation : IDisposable
    {
        public SafeProcessHandle Process { get; }

        public SafeProcessHandle Thread { get; }

        public Int32 ProcessId { get; }

        public Int32 ThreadId { get; }

        internal ProcessInformation(ProcessNativeMethods.PROCESS_INFORMATION info)
        {
            Process = new(info.hProcess, true);
            Thread = new(info.hThread, true);
            ProcessId = info.dwProcessId;
            ThreadId = info.dwThreadId;
        }

        public void Resume()
        {
            ProcessNativeMethods.ResumeThread(Thread.DangerousGetHandle());
        }

        public void Dispose()
        {
            Process.Dispose();
            Thread.Dispose();
            GC.SuppressFinalize(this);
        }

        ~ProcessInformation() => Dispose();
    }

    /// <summary>
    /// JOBOBJECT_BASIC_PROCESS_ID_LIST Contains the process identifier list for a job object.
    /// If the job is nested, the process identifier list consists of all
    /// processes associated with the job and its child jobs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_PROCESS_ID_LIST
    {
        /// <summary>
        /// The number of process identifiers to be stored in ProcessIdList.
        /// </summary>
        public uint NumberOfAssignedProcess;

        /// <summary>
        /// The number of process identifiers returned in the ProcessIdList buffer.
        /// If this number is less than NumberOfAssignedProcesses, increase
        /// the size of the buffer to accommodate the complete list.
        /// </summary>
        public uint NumberOfProcessIdsInList;

        /// <summary>
        /// A variable-length array of process identifiers returned by this call.
        /// Array elements 0 through NumberOfProcessIdsInList minus 1
        /// contain valid process identifiers.
        /// </summary>
        public IntPtr ProcessIdList;
    }

    internal static class ProcessNativeMethods
    {
        [DllImport(PinvokeDllNames.CreateProcessWithLogonWDllName, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcessWithLogonW(string userName,
            string domain,
            IntPtr password,
            LogonFlags logonFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string appName,
            StringBuilder cmdLine,
            int creationFlags,
            IntPtr environmentBlock,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport(PinvokeDllNames.CreateProcessDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcess([MarshalAs(UnmanagedType.LPWStr)] string lpApplicationName,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport(PinvokeDllNames.ResumeThreadDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint ResumeThread(IntPtr threadHandle);

        [DllImport("userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [Flags]
        internal enum LogonFlags
        {
            LOGON_NETCREDENTIALS_ONLY = 2,
            LOGON_WITH_PROFILE = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;

            public SECURITY_ATTRIBUTES()
            {
                this.nLength = 12;
                this.bInheritHandle = true;
                this.lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, true);
            }
        }

        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            internal SafeLocalMemHandle()
                : base(true)
            {
            }

            internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(existingHandle);
            }

            [DllImport(PinvokeDllNames.LocalFreeDllName)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public SafeFileHandle hStdInput;
            public SafeFileHandle hStdOutput;
            public SafeFileHandle hStdError;

            public STARTUPINFO()
            {
                this.lpReserved = IntPtr.Zero;
                this.lpDesktop = IntPtr.Zero;
                this.lpTitle = IntPtr.Zero;
                this.lpReserved2 = IntPtr.Zero;
                this.hStdInput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdError = new SafeFileHandle(IntPtr.Zero, false);
                this.cb = Marshal.SizeOf(this);
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if ((this.hStdInput != null) && !this.hStdInput.IsInvalid)
                    {
                        this.hStdInput.Dispose();
                        this.hStdInput = null;
                    }

                    if ((this.hStdOutput != null) && !this.hStdOutput.IsInvalid)
                    {
                        this.hStdOutput.Dispose();
                        this.hStdOutput = null;
                    }

                    if ((this.hStdError != null) && !this.hStdError.IsInvalid)
                    {
                        this.hStdError.Dispose();
                        this.hStdError = null;
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }
    }

    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeJobHandle(IntPtr jobHandle)
            : base(true)
        {
            base.SetHandle(jobHandle);
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Windows.CloseHandle(base.handle);
        }
    }
#endif
    #endregion

    #region ProcessCommandException
    /// <summary>
    /// Non-terminating errors occurring in the process noun commands.
    /// </summary>
    public class ProcessCommandException : SystemException
    {
        #region ctors
        /// <summary>
        /// Unimplemented standard constructor.
        /// </summary>
        /// <returns>Doesn't return.</returns>
        public ProcessCommandException() : base()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public ProcessCommandException(string message) : base(message)
        {
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ProcessCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctors

        #region Serialization
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ProcessCommandException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Serialization

        #region Properties
        /// <summary>
        /// Name of the process which could not be found or operated upon.
        /// </summary>
        /// <value></value>
        public string ProcessName
        {
            get { return _processName; }

            set { _processName = value; }
        }

        private string _processName = string.Empty;
        #endregion Properties
    }

    #endregion ProcessCommandException
}
