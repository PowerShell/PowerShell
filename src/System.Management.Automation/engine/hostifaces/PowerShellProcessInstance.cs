// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation.Remoting;
using System.Text;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// This class represents a PowerShell process that is used for an out-of-process remote Runspace.
    /// </summary>
    public sealed class PowerShellProcessInstance : IDisposable
    {
        #region Fields

        private readonly ProcessStartInfo _startInfo;
        private RunspacePool _runspacePool;
        private readonly object _syncObject = new object();
        private bool _started;
        private bool _isDisposed;
        private bool _processExited;

        internal static readonly string PwshExePath;
        internal static readonly string WinPwshExePath;

        #endregion Fields

        #region Constructors

        static PowerShellProcessInstance()
        {
#if UNIX
            const string exeName = "pwsh";
#else
            const string exeName = "pwsh.exe";
#endif
            PwshExePath = Path.Combine(Utils.DefaultPowerShellAppBase, exeName);
            if (!File.Exists(PwshExePath))
            {
                // Fallback to the currently running process path if the computed path doesn't exist.
                // This handles scenarios like Alpine Linux containers where PowerShell is installed as a dotnet tool
                // and the computed path points to a non-existent or incompatible binary.
                // Only use the host process if it is pwsh/pwsh.exe to avoid accidentally using a different executable.
                var processPath = System.Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    var processFileName = Path.GetFileName(processPath);
#if UNIX
                    if (processFileName == exeName)
#else
                    if (processFileName.Equals(exeName, StringComparison.OrdinalIgnoreCase))
#endif
                    {
                        PwshExePath = processPath;
                    }
                }
            }

#if !UNIX
            var winPowerShellDir = Utils.GetApplicationBaseFromRegistry(Utils.DefaultPowerShellShellID);
            WinPwshExePath = string.IsNullOrEmpty(winPowerShellDir) ? null : Path.Combine(winPowerShellDir, "powershell.exe");
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellProcessInstance"/> class. Initializes the underlying dotnet process class.
        /// </summary>
        /// <param name="powerShellVersion">Specifies the version of powershell.</param>
        /// <param name="credential">Specifies a user account credentials.</param>
        /// <param name="initializationScript">Specifies a script that will be executed when the powershell process is initialized.</param>
        /// <param name="useWow64">Specifies if the powershell process will be 32-bit.</param>
        /// <param name="workingDirectory">Specifies the initial working directory for the new powershell process.</param>
        public PowerShellProcessInstance(Version powerShellVersion, PSCredential credential, ScriptBlock initializationScript, bool useWow64, string workingDirectory)
        {
            string exePath = PwshExePath;
            bool startingWindowsPowerShell51 = false;
#if !UNIX
            // if requested PS version was "5.1" then we start Windows PS instead of PS Core
            startingWindowsPowerShell51 = (powerShellVersion != null) && (powerShellVersion.Major == 5) && (powerShellVersion.Minor == 1);
            if (startingWindowsPowerShell51)
            {
                if (WinPwshExePath == null)
                {
                    throw new PSInvalidOperationException(RemotingErrorIdStrings.WindowsPowerShellNotPresent);
                }

                exePath = WinPwshExePath;

                if (useWow64)
                {
                    string procArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

                    if ((!string.IsNullOrEmpty(procArch)) && (procArch.Equals("amd64", StringComparison.OrdinalIgnoreCase) ||
                        procArch.Equals("ia64", StringComparison.OrdinalIgnoreCase)))
                    {
                        exePath = WinPwshExePath.ToLowerInvariant().Replace("\\system32\\", "\\syswow64\\");

                        if (!File.Exists(exePath))
                        {
                            string message = PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.WowComponentNotPresent, exePath);
                            throw new PSInvalidOperationException(message);
                        }
                    }
                }
            }
#endif
            // 'WindowStyle' is used only if 'UseShellExecute' is 'true'. Since 'UseShellExecute' is set
            // to 'false' in our use, we can ignore the 'WindowStyle' setting in the initialization below.
            _startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
#if !UNIX
                LoadUserProfile = true,
#endif
            };
#if !UNIX
            if (startingWindowsPowerShell51)
            {
                _startInfo.ArgumentList.Add("-Version");
                _startInfo.ArgumentList.Add("5.1");

                // if starting Windows PowerShell, need to remove PowerShell specific segments of PSModulePath
                _startInfo.Environment["PSModulePath"] = ModuleIntrinsics.GetWindowsPowerShellModulePath();
            }
#endif
            _startInfo.ArgumentList.Add("-s");
            _startInfo.ArgumentList.Add("-NoLogo");
            _startInfo.ArgumentList.Add("-NoProfile");

            if (!string.IsNullOrWhiteSpace(workingDirectory) && !startingWindowsPowerShell51)
            {
                _startInfo.ArgumentList.Add("-wd");
                _startInfo.ArgumentList.Add(workingDirectory);
            }

            if (initializationScript != null)
            {
                var scriptBlockString = initializationScript.ToString();
                if (!string.IsNullOrEmpty(scriptBlockString))
                {
                    var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(scriptBlockString));
                    _startInfo.ArgumentList.Add("-EncodedCommand");
                    _startInfo.ArgumentList.Add(encodedCommand);
                }
            }

            if (credential != null)
            {
                Net.NetworkCredential netCredential = credential.GetNetworkCredential();

                _startInfo.UserName = netCredential.UserName;
                _startInfo.Domain = string.IsNullOrEmpty(netCredential.Domain) ? "." : netCredential.Domain;
                _startInfo.Password = credential.Password;
            }

            Process = new Process { StartInfo = _startInfo, EnableRaisingEvents = true };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellProcessInstance"/> class. Initializes the underlying dotnet process class.
        /// </summary>
        /// <param name="powerShellVersion">Specifies the version of powershell.</param>
        /// <param name="credential">Specifies a user account credentials.</param>
        /// <param name="initializationScript">Specifies a script that will be executed when the powershell process is initialized.</param>
        /// <param name="useWow64">Specifies if the powershell process will be 32-bit.</param>
        public PowerShellProcessInstance(Version powerShellVersion, PSCredential credential, ScriptBlock initializationScript, bool useWow64) : this(powerShellVersion, credential, initializationScript, useWow64, workingDirectory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellProcessInstance"/> class. Default initializes the underlying dotnet process class.
        /// </summary>
        public PowerShellProcessInstance() : this(powerShellVersion: null, credential: null, initializationScript: null, useWow64: false, workingDirectory: null)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the associated process has been terminated.
        /// true if the operating system process referenced by the Process component has terminated; otherwise, false.
        /// </summary>
        public bool HasExited
        {
            get
            {
                // When process is exited, there is some delay in receiving ProcessExited event and HasExited property on process object.
                // Using HasExited property on started process object to determine if powershell process has exited.
                //
                return _processExited || (_started && Process != null && Process.HasExited);
            }
        }

        #endregion Constructors

        #region Dispose

        /// <summary>
        /// Implementing the <see cref="IDisposable"/> interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_syncObject)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
            }

            if (disposing)
            {
                try
                {
                    if (Process != null && !Process.HasExited)
                        Process.Kill();
                }
                catch (InvalidOperationException)
                {
                }
                catch (Win32Exception)
                {
                }
                catch (NotSupportedException)
                {
                }
            }
        }

        #endregion Dispose

        #region Public Properties

        /// <summary>
        /// Gets the process object of the remote target.
        /// </summary>
        public Process Process { get; }

        #endregion Public Properties

        #region Internal Members

        internal RunspacePool RunspacePool
        {
            get
            {
                lock (_syncObject)
                {
                    return _runspacePool;
                }
            }

            set
            {
                lock (_syncObject)
                {
                    _runspacePool = value;
                }
            }
        }

        internal OutOfProcessTextWriter StdInWriter { get; set; }

        internal void Start()
        {
            // To fix the deadlock, we should not call Process.HasExited by holding the sync lock as Process.HasExited can raise ProcessExited event
            //
            if (HasExited)
            {
                throw new InvalidOperationException();
            }

            lock (_syncObject)
            {
                if (_started)
                {
                    return;
                }

                _started = true;
                Process.Exited += ProcessExited;
            }

            Process.Start();
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            lock (_syncObject)
            {
                _processExited = true;
            }
        }

        #endregion Internal Members
    }
}
