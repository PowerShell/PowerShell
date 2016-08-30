/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Management.Automation.Remoting;
using System.Text;
using System.Diagnostics;


namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PowerShellProcessInstance : IDisposable
    {
        #region Private Members

        private readonly ProcessStartInfo _startInfo;
        private static readonly string s_PSExePath;
        private RunspacePool _runspacePool;
        private readonly object _syncObject = new object();
        private bool _started;
        private bool _isDisposed;
        private bool _processExited;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        static PowerShellProcessInstance()
        {
            s_PSExePath = Path.Combine(Utils.GetApplicationBase(Utils.DefaultPowerShellShellID),
                            "powershell.exe");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerShellVersion"></param>
        /// <param name="credential"></param>
        /// <param name="initializationScript"></param>
        /// <param name="useWow64"></param>
        public PowerShellProcessInstance(Version powerShellVersion, PSCredential credential, ScriptBlock initializationScript, bool useWow64)
        {
            string psWow64Path = s_PSExePath;

            if (useWow64)
            {
                string procArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

                if ((!string.IsNullOrEmpty(procArch)) && (procArch.Equals("amd64", StringComparison.OrdinalIgnoreCase) ||
                    procArch.Equals("ia64", StringComparison.OrdinalIgnoreCase)))
                {
                    psWow64Path = s_PSExePath.ToLowerInvariant().Replace("\\system32\\", "\\syswow64\\");

                    if (!File.Exists(psWow64Path))
                    {
                        string message =
                            PSRemotingErrorInvariants.FormatResourceString(
                                RemotingErrorIdStrings.IPCWowComponentNotPresent,
                                psWow64Path);
                        throw new PSInvalidOperationException(message);
                    }
                }
            }

            string processArguments = string.Empty;
            // Adding Version parameter to powershell.exe
            // Version parameter needs to go before all other parameters because the native layer looks for Version or 
            // PSConsoleFile parameters before parsing other parameters.
            // The other parameters get parsed in the managed layer.
            Version tempVersion = powerShellVersion ?? PSVersionInfo.PSVersion;
            processArguments = string.Format(CultureInfo.InvariantCulture,
                       "-Version {0}", new Version(tempVersion.Major, tempVersion.Minor));

            processArguments = string.Format(CultureInfo.InvariantCulture,
                "{0} -s -NoLogo -NoProfile", processArguments);

            if (initializationScript != null)
            {
                string scripBlockAsString = initializationScript.ToString();
                if (!string.IsNullOrEmpty(scripBlockAsString))
                {
                    string encodedCommand =
                        Convert.ToBase64String(Encoding.Unicode.GetBytes(scripBlockAsString));
                    processArguments = string.Format(CultureInfo.InvariantCulture,
                        "{0} -EncodedCommand {1}", processArguments, encodedCommand);
                }
            }

            // 'WindowStyle' is used only if 'UseShellExecute' is 'true'. Since 'UseShellExecute' is set
            // to 'false' in our use, we can ignore the 'WindowStyle' setting in the initialization below.
            _startInfo = new ProcessStartInfo
            {
                FileName = useWow64 ? psWow64Path : s_PSExePath,
                Arguments = processArguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                LoadUserProfile = true,
            };

            if (credential != null)
            {
                Net.NetworkCredential netCredential = credential.GetNetworkCredential();

                _startInfo.UserName = netCredential.UserName;
                _startInfo.Domain = string.IsNullOrEmpty(netCredential.Domain) ? "." : netCredential.Domain;
#if CORECLR
                _startInfo.PasswordInClearText = ClrFacade.ConvertSecureStringToString(credential.Password);
#else
                _startInfo.Password = credential.Password;
#endif
            }

            Process = new Process { StartInfo = _startInfo, EnableRaisingEvents = true };
        }

        /// <summary>
        /// 
        /// </summary>
        public PowerShellProcessInstance() : this(null, null, null, false)
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
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            lock (_syncObject)
            {
                if (_isDisposed) return;
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
        /// 
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
