// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Win32;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region Restart-Computer

    /// <summary>
    /// This exception is thrown when the timeout expires before a computer finishes restarting.
    /// </summary>
    public sealed class RestartComputerTimeoutException : RuntimeException
    {
        /// <summary>
        /// Name of the computer that is restarting.
        /// </summary>
        public string ComputerName { get; }

        /// <summary>
        /// The timeout value specified by the user. It indicates the seconds to wait before timeout.
        /// </summary>
        public int Timeout { get; }

        /// <summary>
        /// Construct a RestartComputerTimeoutException.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="timeout"></param>
        /// <param name="message"></param>
        /// <param name="errorId"></param>
        internal RestartComputerTimeoutException(string computerName, int timeout, string message, string errorId)
            : base(message)
        {
            SetErrorId(errorId);
            SetErrorCategory(ErrorCategory.OperationTimeout);
            ComputerName = computerName;
            Timeout = timeout;
        }

        /// <summary>
        /// Construct a RestartComputerTimeoutException.
        /// </summary>
        public RestartComputerTimeoutException() : base() { }

        /// <summary>
        /// Constructs a RestartComputerTimeoutException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public RestartComputerTimeoutException(string message) : base(message) { }

        /// <summary>
        /// Constructs a RestartComputerTimeoutException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        /// <param name="innerException">
        /// An exception that led to this exception.
        /// </param>
        public RestartComputerTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Defines the services that Restart-Computer can wait on.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum WaitForServiceTypes
    {
        /// <summary>
        /// Wait for the WMI service to be ready.
        /// </summary>
        Wmi = 0x0,

        /// <summary>
        /// Wait for the WinRM service to be ready.
        /// </summary>
        WinRM = 0x1,

        /// <summary>
        /// Wait for the PowerShell to be ready.
        /// </summary>
        PowerShell = 0x2,
    }

    /// <summary>
    /// Restarts the computer.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Computer", SupportsShouldProcess = true, DefaultParameterSetName = DefaultParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097060", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RestartComputerCommand : PSCmdlet, IDisposable
    {
        #region "Parameters and PrivateData"

        private const string DefaultParameterSet = "DefaultSet";
        private const int forcedReboot = 6; // see https://msdn.microsoft.com/library/aa394058(v=vs.85).aspx

        /// <summary>
        /// The authentication options for CIM_WSMan connection.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with explicit or implicit credential
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; }

        /// <summary>
        /// Specifies the computer (s)Name on which this command is executed.
        /// When this parameter is omitted, this cmdlet restarts the local computer.
        /// Type the NETBIOS name, IP address, or fully-qualified domain name of one
        /// or more computers in a comma-separated list. To specify the local computer, type the computername or "localhost".
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("CN", "__SERVER", "Server", "IPAddress")]
        public string[] ComputerName { get; set; } = new string[] { "." };

        private List<string> _validatedComputerNames = new();
        private readonly List<string> _waitOnComputers = new();
        private readonly HashSet<string> _uniqueComputerNames = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. Type a
        /// user-name, such as "User01" or "Domain01\User01", or enter a PSCredential
        /// object, such as one from the Get-Credential cmdlet.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Using Force in conjunction with Reboot on a
        /// remote computer immediately reboots the remote computer.
        /// </summary>
        [Parameter]
        [Alias("f")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Specify the Wait parameter. Prompt will be blocked is the Timeout is not 0.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// Specify the Timeout parameter.
        /// Negative value indicates wait infinitely.
        /// Positive value indicates the seconds to wait before timeout.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [Alias("TimeoutSec")]
        [ValidateRange(-1, int.MaxValue)]
        public int Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                _timeout = value;
                _timeoutSpecified = true;
            }
        }

        private int _timeout = -1;
        private bool _timeoutSpecified = false;

        /// <summary>
        /// Specify the For parameter.
        /// Wait for the specific service before unblocking the prompt.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        public WaitForServiceTypes For
        {
            get
            {
                return _waitFor;
            }

            set
            {
                _waitFor = value;
                _waitForSpecified = true;
            }
        }

        private WaitForServiceTypes _waitFor = WaitForServiceTypes.PowerShell;
        private bool _waitForSpecified = false;

        /// <summary>
        /// Specify the Delay parameter.
        /// The specific time interval (in second) to wait between network pings or service queries.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [ValidateRange(1, short.MaxValue)]
        public short Delay
        {
            get
            {
                return (short)_delay;
            }

            set
            {
                _delay = value;
                _delaySpecified = true;
            }
        }

        private int _delay = 5;
        private bool _delaySpecified = false;

        /// <summary>
        /// Script to test if the PowerShell is ready.
        /// </summary>
        private const string TestPowershellScript = @"
$array = @($input)
$result = @{}
foreach ($computerName in $array[1])
{
    $ret = $null
    $arguments = @{
        ComputerName = $computerName
        ScriptBlock = { $true }

        SessionOption = New-PSSessionOption -NoMachineProfile
        ErrorAction = 'SilentlyContinue'
    }

    if ( $null -ne $array[0] )
    {
        $arguments['Credential'] = $array[0]
    }

    $result[$computerName] = (Invoke-Command @arguments) -as [bool]
}
$result
";

        /// <summary>
        /// The indicator to use when show progress.
        /// </summary>
        private readonly string[] _indicator = { "|", "/", "-", "\\" };

        /// <summary>
        /// The activity id.
        /// </summary>
        private int _activityId;

        /// <summary>
        /// After call 'Shutdown' on the target computer, wait a few
        /// seconds for the restart to begin.
        /// </summary>
        private const int SecondsToWaitForRestartToBegin = 25;

        /// <summary>
        /// Actual time out in seconds.
        /// </summary>
        private int _timeoutInMilliseconds;

        /// <summary>
        /// Indicate to exit.
        /// </summary>
        private bool _exit, _timeUp;
        private readonly CancellationTokenSource _cancel = new();

        /// <summary>
        /// A waithandler to wait on. Current thread will wait on it during the delay interval.
        /// </summary>
        private readonly ManualResetEventSlim _waitHandler = new(false);
        private readonly Dictionary<string, ComputerInfo> _computerInfos = new(StringComparer.OrdinalIgnoreCase);

        // CLR 4.0 Port note - use https://msdn.microsoft.com/library/system.net.networkinformation.ipglobalproperties.hostname(v=vs.110).aspx
        private readonly string _shortLocalMachineName = Dns.GetHostName();

        // And for this, use PsUtils.GetHostname()
        private readonly string _fullLocalMachineName = Dns.GetHostEntryAsync(string.Empty).Result.HostName;

        private int _percent;
        private string _status;
        private string _activity;
        private Timer _timer;
        private System.Management.Automation.PowerShell _powershell;

        private const string StageVerification = "VerifyStage";
        private const string WmiConnectionTest = "WMI";
        private const string WinrmConnectionTest = "WinRM";
        private const string PowerShellConnectionTest = "PowerShell";

        #endregion "parameters and PrivateData"

        #region "IDisposable Members"

        /// <summary>
        /// Dispose Method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _waitHandler.Dispose();
                _cancel.Dispose();
                _powershell?.Dispose();
            }
        }

        #endregion "IDisposable Members"

        #region "Private Methods"

        /// <summary>
        /// Validate parameters for 'DefaultSet'
        /// 1. When the Wait is specified, the computername cannot contain the local machine
        /// 2. If the local machine is present, make sure it is at the end of the list (so the remote ones get restarted before the local machine reboot).
        /// </summary>
        private void ValidateComputerNames()
        {
            bool containLocalhost = false;
            _validatedComputerNames.Clear();

            foreach (string name in ComputerName)
            {
                ErrorRecord error = null;
                string targetComputerName = ComputerWMIHelper.ValidateComputerName(name, _shortLocalMachineName, _fullLocalMachineName, ref error);
                if (targetComputerName == null)
                {
                    if (error != null)
                    {
                        WriteError(error);
                    }

                    continue;
                }

                if (targetComputerName.Equals(ComputerWMIHelper.localhostStr, StringComparison.OrdinalIgnoreCase))
                {
                    containLocalhost = true;
                }
                else if (!_uniqueComputerNames.Contains(targetComputerName))
                {
                    _validatedComputerNames.Add(targetComputerName);
                    _uniqueComputerNames.Add(targetComputerName);
                }
            }

            // Force wait with a test hook even if we're on the local computer
            if (!InternalTestHooks.TestWaitStopComputer && Wait && containLocalhost)
            {
                // The local machine will be ignored, and an error will be emitted.
                InvalidOperationException ex = new(ComputerResources.CannotWaitLocalComputer);
                WriteError(new ErrorRecord(ex, "CannotWaitLocalComputer", ErrorCategory.InvalidOperation, null));
                containLocalhost = false;
            }

            // Add the localhost to the end of the list, so we will restart remote machines
            // before we restart the local one.
            if (containLocalhost)
            {
                _validatedComputerNames.Add(ComputerWMIHelper.localhostStr);
            }
        }

        /// <summary>
        /// Write out progress.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="status"></param>
        /// <param name="percent"></param>
        /// <param name="progressRecordType"></param>
        private void WriteProgress(string activity, string status, int percent, ProgressRecordType progressRecordType)
        {
            ProgressRecord progress = new(_activityId, activity, status);
            progress.PercentComplete = percent;
            progress.RecordType = progressRecordType;
            WriteProgress(progress);
        }

        /// <summary>
        /// Calculate the progress percentage.
        /// </summary>
        /// <param name="currentStage"></param>
        /// <returns></returns>
        private int CalculateProgressPercentage(string currentStage)
        {
            switch (currentStage)
            {
                case StageVerification:
                    return _waitFor.Equals(WaitForServiceTypes.Wmi) || _waitFor.Equals(WaitForServiceTypes.WinRM)
                               ? 33
                               : 20;
                case WmiConnectionTest:
                    return _waitFor.Equals(WaitForServiceTypes.Wmi) ? 66 : 40;
                case WinrmConnectionTest:
                    return _waitFor.Equals(WaitForServiceTypes.WinRM) ? 66 : 60;
                case PowerShellConnectionTest:
                    return 80;
                default:
                    break;
            }

            Dbg.Diagnostics.Assert(false, "CalculateProgressPercentage should never hit the default case");
            return 0;
        }

        /// <summary>
        /// Event handler for the timer.
        /// </summary>
        /// <param name="s"></param>
        private void OnTimedEvent(object s)
        {
            _exit = _timeUp = true;
            _cancel.Cancel();
            _waitHandler.Set();

            if (_powershell != null)
            {
                _powershell.Stop();
                _powershell.Dispose();
            }
        }

        private sealed class ComputerInfo
        {
            internal string LastBootUpTime;
            internal bool RebootComplete;
        }

        private List<string> TestRestartStageUsingWsman(IEnumerable<string> computerNames, List<string> nextTestList, CancellationToken token)
        {
            var restartStageTestList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, Credential, WsmanAuthentication, isLocalHost: false, this, token))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 "Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem,
                                                                 operationOptions);
                        foreach (CimInstance os in mCollection)
                        {
                            itemRetrieved = true;
                            string newLastBootUpTime = os.CimInstanceProperties["LastBootUpTime"].Value.ToString();
                            string oldLastBootUpTime = _computerInfos[computer].LastBootUpTime;

                            if (!string.Equals(newLastBootUpTime, oldLastBootUpTime, StringComparison.OrdinalIgnoreCase))
                            {
                                _computerInfos[computer].RebootComplete = true;
                                nextTestList.Add(computer);
                            }
                            else
                            {
                                restartStageTestList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            restartStageTestList.Add(computer);
                        }
                    }
                }
                catch (CimException)
                {
                    restartStageTestList.Add(computer);
                }
                catch (Exception)
                {
                    restartStageTestList.Add(computer);
                }
            }

            return restartStageTestList;
        }

        private List<string> SetUpComputerInfoUsingWsman(IEnumerable<string> computerNames, CancellationToken token)
        {
            var validComputerNameList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, Credential, WsmanAuthentication, isLocalHost: false, this, token))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 "Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem,
                                                                 operationOptions);
                        foreach (CimInstance os in mCollection)
                        {
                            itemRetrieved = true;
                            if (!_computerInfos.ContainsKey(computer))
                            {
                                var info = new ComputerInfo
                                {
                                    LastBootUpTime = os.CimInstanceProperties["LastBootUpTime"].Value.ToString(),
                                    RebootComplete = false
                                };
                                _computerInfos.Add(computer, info);
                                validComputerNameList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ComputerResources.CannotGetOperatingSystemObject);
                            var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                            ErrorCategory.OperationStopped, computer);
                            this.WriteError(error);
                        }
                    }
                }
                catch (CimException ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
                catch (Exception ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
            }

            return validComputerNameList;
        }

        private void WriteOutTimeoutError(IEnumerable<string> computerNames)
        {
            const string errorId = "RestartComputerTimeout";
            foreach (string computer in computerNames)
            {
                string errorMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computer, ComputerResources.TimeoutError);
                var exception = new RestartComputerTimeoutException(computer, Timeout, errorMsg, errorId);
                var error = new ErrorRecord(exception, errorId, ErrorCategory.OperationTimeout, computer);
                if (!InternalTestHooks.TestWaitStopComputer)
                {
                    WriteError(error);
                }
            }
        }

        #endregion "Private Methods"

        #region "Internal Methods"

        internal static List<string> TestWmiConnectionUsingWsman(List<string> computerNames, List<string> nextTestList, PSCredential credential, string wsmanAuthentication, PSCmdlet cmdlet, CancellationToken token)
        {
            // Check if the WMI service "Winmgmt" is started
            const string wmiServiceQuery = "Select * from " + ComputerWMIHelper.WMI_Class_Service + " Where name = 'Winmgmt'";
            var wmiTestList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, credential, wsmanAuthentication, isLocalHost: false, cmdlet, token))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 wmiServiceQuery,
                                                                 operationOptions);
                        foreach (CimInstance service in mCollection)
                        {
                            itemRetrieved = true;
                            if (LanguagePrimitives.IsTrue(service.CimInstanceProperties["Started"].Value))
                            {
                                nextTestList.Add(computer);
                            }
                            else
                            {
                                wmiTestList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            wmiTestList.Add(computer);
                        }
                    }
                }
                catch (CimException)
                {
                    wmiTestList.Add(computer);
                }
                catch (Exception)
                {
                    wmiTestList.Add(computer);
                }
            }

            return wmiTestList;
        }

        /// <summary>
        /// Test the PowerShell state for the restarting computer.
        /// </summary>
        /// <param name="computerNames"></param>
        /// <param name="nextTestList"></param>
        /// <param name="powershell"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        internal static List<string> TestPowerShell(List<string> computerNames, List<string> nextTestList, System.Management.Automation.PowerShell powershell, PSCredential credential)
        {
            List<string> psList = new();

            try
            {
                Collection<PSObject> psObjectCollection = powershell.Invoke(new object[] { credential, computerNames.ToArray() });
                if (psObjectCollection == null)
                {
                    Dbg.Diagnostics.Assert(false, "This should never happen. Invoke should never return null.");
                }

                // If ^C or timeout happens when we are in powershell.Invoke(), the psObjectCollection might be empty
                if (psObjectCollection.Count == 0)
                {
                    return computerNames;
                }

                object result = PSObject.Base(psObjectCollection[0]);
                Hashtable data = result as Hashtable;

                Dbg.Diagnostics.Assert(data != null, "data should never be null");
                Dbg.Diagnostics.Assert(data.Count == computerNames.Count, "data should contain results for all computers in computerNames");

                foreach (string computer in computerNames)
                {
                    if (LanguagePrimitives.IsTrue(data[computer]))
                    {
                        nextTestList.Add(computer);
                    }
                    else
                    {
                        psList.Add(computer);
                    }
                }
            }
            catch (PipelineStoppedException)
            {
                // powershell.Stop() is invoked because timeout expires, or Ctrl+C is pressed
            }
            catch (ObjectDisposedException)
            {
                // powershell.dispose() is invoked because timeout expires, or Ctrl+C is pressed
            }

            return psList;
        }

        #endregion "Internal Methods"

        #region "Overrides"

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Timeout, For, Delay, Progress cannot be present if Wait is not present
            if ((_timeoutSpecified || _waitForSpecified || _delaySpecified) && !Wait)
            {
                InvalidOperationException ex = new(ComputerResources.RestartComputerInvalidParameter);
                ThrowTerminatingError(new ErrorRecord(ex, "RestartComputerInvalidParameter", ErrorCategory.InvalidOperation, null));
            }

            if (Wait)
            {
                _activityId = Random.Shared.Next();
                if (_timeout == -1 || _timeout >= int.MaxValue / 1000)
                {
                    _timeoutInMilliseconds = int.MaxValue;
                }
                else
                {
                    _timeoutInMilliseconds = _timeout * 1000;
                }

                // We don't support combined service types for now
                switch (_waitFor)
                {
                    case WaitForServiceTypes.Wmi:
                    case WaitForServiceTypes.WinRM:
                        break;
                    case WaitForServiceTypes.PowerShell:
                        _powershell = System.Management.Automation.PowerShell.Create();
                        _powershell.AddScript(TestPowershellScript);
                        break;
                    default:
                        InvalidOperationException ex = new(ComputerResources.NoSupportForCombinedServiceType);
                        ErrorRecord error = new(ex, "NoSupportForCombinedServiceType", ErrorCategory.InvalidOperation, (int)_waitFor);
                        ThrowTerminatingError(error);
                        break;
                }
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ProcessRecord()
        {
            // Validate parameters
            ValidateComputerNames();

            object[] flags = new object[] { 2, 0 };
            if (Force)
            {
                flags[0] = forcedReboot;
            }

            if (ParameterSetName.Equals(DefaultParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                if (Wait && _timeout != 0)
                {
                    _validatedComputerNames =
                        SetUpComputerInfoUsingWsman(_validatedComputerNames, _cancel.Token);
                }

                foreach (string computer in _validatedComputerNames)
                {
                    bool isLocal = false;
                    string compname;

                    if (computer.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        compname = _shortLocalMachineName;
                        isLocal = true;
                    }
                    else
                    {
                        compname = computer;
                    }

                    // Generate target and action strings
                    string action =
                        StringUtil.Format(
                            ComputerResources.RestartComputerAction,
                            isLocal ? ComputerResources.LocalShutdownPrivilege : ComputerResources.RemoteShutdownPrivilege);
                    string target =
                        isLocal ? StringUtil.Format(ComputerResources.DoubleComputerName, "localhost", compname) : compname;

                    if (!ShouldProcess(target, action))
                    {
                        continue;
                    }

                    bool isSuccess =
                        ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(this, isLocal, compname, flags, Credential, WsmanAuthentication, ComputerResources.RestartcomputerFailed, "RestartcomputerFailed", _cancel.Token);

                    if (isSuccess && Wait && _timeout != 0)
                    {
                        _waitOnComputers.Add(computer);
                    }
                }

                if (_waitOnComputers.Count > 0)
                {
                    var restartStageTestList = new List<string>(_waitOnComputers);
                    var wmiTestList = new List<string>();
                    var winrmTestList = new List<string>();
                    var psTestList = new List<string>();
                    var allDoneList = new List<string>();

                    bool isForWmi = _waitFor.Equals(WaitForServiceTypes.Wmi);
                    bool isForWinRm = _waitFor.Equals(WaitForServiceTypes.WinRM);
                    bool isForPowershell = _waitFor.Equals(WaitForServiceTypes.PowerShell);

                    int indicatorIndex = 0;
                    int machineCompleteRestart = 0;
                    int actualDelay = SecondsToWaitForRestartToBegin;
                    bool first = true;
                    bool waitComplete = false;

                    _percent = 0;
                    _status = ComputerResources.WaitForRestartToBegin;
                    _activity = _waitOnComputers.Count == 1 ?
                        StringUtil.Format(ComputerResources.RestartSingleComputerActivity, _waitOnComputers[0]) :
                        ComputerResources.RestartMultipleComputersActivity;

                    _timer = new Timer(OnTimedEvent, null, _timeoutInMilliseconds, System.Threading.Timeout.Infinite);

                    while (true)
                    {
                        // (delay * 1000)/250ms
                        int loopCount = actualDelay * 4;
                        while (loopCount > 0)
                        {
                            WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);

                            loopCount--;
                            _waitHandler.Wait(250);
                            if (_exit)
                            {
                                break;
                            }
                        }

                        if (first)
                        {
                            actualDelay = _delay;
                            first = false;

                            if (_waitOnComputers.Count > 1)
                            {
                                _status = StringUtil.Format(ComputerResources.WaitForMultipleComputers, machineCompleteRestart, _waitOnComputers.Count);
                                WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                            }
                        }

                        do
                        {
                            // Test restart stage.
                            // We check if the target machine has already rebooted by querying the LastBootUpTime from the Win32_OperatingSystem object.
                            // So after this step, we are sure that both the Network and the WMI or WinRM service have already come up.
                            if (_exit)
                            {
                                break;
                            }

                            if (restartStageTestList.Count > 0)
                            {
                                if (_waitOnComputers.Count == 1)
                                {
                                    _status = ComputerResources.VerifyRebootStage;
                                    _percent = CalculateProgressPercentage(StageVerification);
                                    WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                }

                                List<string> nextTestList = (isForWmi || isForPowershell) ? wmiTestList : winrmTestList;
                                restartStageTestList = TestRestartStageUsingWsman(restartStageTestList, nextTestList, _cancel.Token);
                            }

                            // Test WMI service
                            if (_exit)
                            {
                                break;
                            }

                            if (wmiTestList.Count > 0)
                            {
                                // This statement block executes for both CLRs.
                                // In the "full" CLR, it serves as the else case.
                                {
                                    if (_waitOnComputers.Count == 1)
                                    {
                                        _status = ComputerResources.WaitForWMI;
                                        _percent = CalculateProgressPercentage(WmiConnectionTest);
                                        WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                    }

                                    wmiTestList = TestWmiConnectionUsingWsman(wmiTestList, winrmTestList, Credential, WsmanAuthentication, this, _cancel.Token);
                                }
                            }

                            if (isForWmi)
                            {
                                break;
                            }

                            // Test WinRM service
                            if (_exit)
                            {
                                break;
                            }

                            if (winrmTestList.Count > 0)
                            {
                                // This statement block executes for both CLRs.
                                // In the "full" CLR, it serves as the else case.
                                {
                                    // CIM-WSMan in use. In this case, restart stage checking is done by using WMIv2,
                                    // so the WinRM service on the target machine is already up at this point.
                                    psTestList.AddRange(winrmTestList);
                                    winrmTestList.Clear();

                                    if (_waitOnComputers.Count == 1)
                                    {
                                        // This is to simulate the test for WinRM service
                                        _status = ComputerResources.WaitForWinRM;
                                        _percent = CalculateProgressPercentage(WinrmConnectionTest);

                                        loopCount = actualDelay * 4; // (delay * 1000)/250ms
                                        while (loopCount > 0)
                                        {
                                            WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);

                                            loopCount--;
                                            _waitHandler.Wait(250);
                                            if (_exit)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (isForWinRm)
                            {
                                break;
                            }

                            // Test PowerShell
                            if (_exit)
                            {
                                break;
                            }

                            if (psTestList.Count > 0)
                            {
                                if (_waitOnComputers.Count == 1)
                                {
                                    _status = ComputerResources.WaitForPowerShell;
                                    _percent = CalculateProgressPercentage(PowerShellConnectionTest);
                                    WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                }

                                psTestList = TestPowerShell(psTestList, allDoneList, _powershell, this.Credential);
                            }
                        } while (false);

                        // if time is up or Ctrl+c is typed, break out
                        if (_exit)
                        {
                            break;
                        }

                        // Check if the restart completes
                        switch (_waitFor)
                        {
                            case WaitForServiceTypes.Wmi:
                                waitComplete = (winrmTestList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = winrmTestList.Count;
                                break;
                            case WaitForServiceTypes.WinRM:
                                waitComplete = (psTestList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = psTestList.Count;
                                break;
                            case WaitForServiceTypes.PowerShell:
                                waitComplete = (allDoneList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = allDoneList.Count;
                                break;
                        }

                        // Wait is done or time is up
                        if (waitComplete || _exit)
                        {
                            if (waitComplete)
                            {
                                _status = ComputerResources.RestartComplete;
                                WriteProgress(_indicator[indicatorIndex % 4] + _activity, _status, 100, ProgressRecordType.Completed);
                                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                            }

                            break;
                        }

                        if (_waitOnComputers.Count > 1)
                        {
                            _status = StringUtil.Format(ComputerResources.WaitForMultipleComputers, machineCompleteRestart, _waitOnComputers.Count);
                            _percent = machineCompleteRestart * 100 / _waitOnComputers.Count;
                        }
                    }

                    if (_timeUp)
                    {
                        // The timeout expires. Write out timeout error messages for the computers that haven't finished restarting
                        do
                        {
                            if (restartStageTestList.Count > 0)
                            {
                                WriteOutTimeoutError(restartStageTestList);
                            }

                            if (wmiTestList.Count > 0)
                            {
                                WriteOutTimeoutError(wmiTestList);
                            }

                            // Wait for WMI. All computers that finished restarting are put in "winrmTestList"
                            if (isForWmi)
                            {
                                break;
                            }

                            // Wait for WinRM. All computers that finished restarting are put in "psTestList"
                            if (winrmTestList.Count > 0)
                            {
                                WriteOutTimeoutError(winrmTestList);
                            }

                            if (isForWinRm)
                            {
                                break;
                            }

                            if (psTestList.Count > 0)
                            {
                                WriteOutTimeoutError(psTestList);
                            }

                            // Wait for PowerShell. All computers that finished restarting are put in "allDoneList"
                        } while (false);
                    }
                }
            }
        }

        /// <summary>
        /// To implement ^C.
        /// </summary>
        protected override void StopProcessing()
        {
            _exit = true;
            _cancel.Cancel();
            _waitHandler.Set();

            _timer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            if (_powershell != null)
            {
                _powershell.Stop();
                _powershell.Dispose();
            }
        }

        #endregion "Overrides"
    }

    #endregion Restart-Computer

    #region Stop-Computer

    /// <summary>
    /// Cmdlet to stop computer.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097151", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class StopComputerCommand : PSCmdlet, IDisposable
    {
        #region Private Members

        private readonly CancellationTokenSource _cancel = new();

        private const int forcedShutdown = 5; // See https://msdn.microsoft.com/library/aa394058(v=vs.85).aspx

        #endregion

        #region "Parameters"

        /// <summary>
        /// The authentication options for CIM_WSMan connection.
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with explicit or implicit credential
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; } = "Default";

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Value of the address requested. The form of the value can be either the
        /// computer name ("wxyz1234"), IPv4 address ("192.168.177.124"), or IPv6
        /// address ("2010:836B:4179::836B:4179").
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("CN", "__SERVER", "Server", "IPAddress")]
        public string[] ComputerName { get; set; } = new string[] { "." };

        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. Type a
        /// user-name, such as "User01" or "Domain01\User01", or enter a PSCredential
        /// object, such as one from the Get-Credential cmdlet.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Force the operation to take place if possible.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; } = false;

        #endregion "parameters"

        #region "IDisposable Members"

        /// <summary>
        /// Dispose Method.
        /// </summary>
        public void Dispose()
        {
            _cancel.Dispose();
        }

        #endregion "IDisposable Members"

        #region "Overrides"

        /// <summary>
        /// ProcessRecord.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ProcessRecord()
        {
            object[] flags = new object[] { 1, 0 };
            if (Force)
                flags[0] = forcedShutdown;

            ProcessWSManProtocol(flags);
        }

        /// <summary>
        /// To implement ^C.
        /// </summary>
        protected override void StopProcessing()
        {
            try
            {
                _cancel.Cancel();
            }
            catch (ObjectDisposedException) { }
            catch (AggregateException) { }
        }

        #endregion "Overrides"

        #region Private Methods

        private void ProcessWSManProtocol(object[] flags)
        {
            foreach (string computer in ComputerName)
            {
                string compname = string.Empty;
                string strLocal = string.Empty;
                bool isLocalHost = false;

                if (_cancel.Token.IsCancellationRequested)
                {
                    break;
                }

                if ((computer.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (computer.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    compname = Dns.GetHostName();
                    strLocal = "localhost";
                    isLocalHost = true;
                }
                else
                {
                    compname = computer;
                }

                if (!ShouldProcess(StringUtil.Format(ComputerResources.DoubleComputerName, strLocal, compname)))
                {
                    continue;
                }
                else
                {
                    ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(
                        this,
                        isLocalHost,
                        compname,
                        flags,
                        Credential,
                        WsmanAuthentication,
                        ComputerResources.StopcomputerFailed,
                        "StopComputerException",
                        _cancel.Token);
                }
            }
        }

        #endregion
    }

    #endregion

    #region Rename-Computer

    /// <summary>
    /// Renames a domain computer and its corresponding domain account or a
    /// workgroup computer. Use this command to rename domain workstations and local
    /// machines only. It cannot be used to rename Domain Controllers.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097054", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(RenameComputerChangeInfo))]
    public class RenameComputerCommand : PSCmdlet
    {
        #region Private Members

        private bool _containsLocalHost = false;
        private string _newNameForLocalHost = null;

        private readonly string _shortLocalMachineName = Dns.GetHostName();
        private readonly string _fullLocalMachineName = Dns.GetHostEntryAsync(string.Empty).Result.HostName;

        #endregion

        #region Parameters

        /// <summary>
        /// Target computers to rename.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string ComputerName { get; set; } = "localhost";

        /// <summary>
        /// Emit the output.
        /// </summary>
        // [Alias("Restart")]
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// The domain credential of the domain the target computer joined.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential DomainCredential { get; set; }

        /// <summary>
        /// The administrator credential of the target computer.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential LocalCredential { get; set; }

        /// <summary>
        /// New names for the target computers.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; }

        /// <summary>
        /// Suppress the ShouldContinue.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        private bool _force;

        /// <summary>
        /// To restart the target computer after rename it.
        /// </summary>
        [Parameter]
        public SwitchParameter Restart
        {
            get { return _restart; }

            set { _restart = value; }
        }

        private bool _restart;

        /// <summary>
        /// The authentication options for CIM_WSMan connection.
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with implicit or explicit credential
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; } = "Default";

        #endregion

        #region "Private Methods"

        /// <summary>
        /// Check to see if the target computer is the local machine.
        /// </summary>
        private string ValidateComputerName()
        {
            // Validate target name.
            ErrorRecord targetError = null;
            string targetComputer = ComputerWMIHelper.ValidateComputerName(ComputerName, _shortLocalMachineName, _fullLocalMachineName, ref targetError);
            if (targetComputer == null)
            {
                if (targetError != null)
                {
                    WriteError(targetError);
                }

                return null;
            }

            // Validate *new* name. Validate the format of the new name. Check if the old name is the same as the
            // new name later.
            if (!ComputerWMIHelper.IsComputerNameValid(NewName))
            {
                bool isLocalhost = targetComputer.Equals(ComputerWMIHelper.localhostStr, StringComparison.OrdinalIgnoreCase);
                string errMsg = StringUtil.Format(ComputerResources.InvalidNewName, isLocalhost ? _shortLocalMachineName : targetComputer, NewName);
                ErrorRecord error = new(
                        new InvalidOperationException(errMsg), "InvalidNewName",
                        ErrorCategory.InvalidArgument, NewName);
                WriteError(error);
                return null;
            }

            return targetComputer;
        }

        private void DoRenameComputerAction(string computer, string newName, bool isLocalhost)
        {
            string computerName = isLocalhost ? _shortLocalMachineName : computer;

            if (!ShouldProcess(computerName))
            {
                return;
            }

            // Check the length of the new name
            if (newName != null && newName.Length > ComputerWMIHelper.NetBIOSNameMaxLength)
            {
                string truncatedName = newName.Substring(0, ComputerWMIHelper.NetBIOSNameMaxLength);
                string query = StringUtil.Format(ComputerResources.TruncateNetBIOSName, truncatedName);
                string caption = ComputerResources.TruncateNetBIOSNameCaption;
                if (!Force && !ShouldContinue(query, caption))
                {
                    return;
                }
            }

            DoRenameComputerWsman(computer, computerName, newName, isLocalhost);
        }

        private void DoRenameComputerWsman(string computer, string computerName, string newName, bool isLocalhost)
        {
            bool successful = false;
            int retVal;
            PSCredential credToUse = isLocalhost ? null : (LocalCredential ?? DomainCredential);

            try
            {
                using (CancellationTokenSource cancelTokenSource = new())
                using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, credToUse, WsmanAuthentication, isLocalhost, this, cancelTokenSource.Token))
                {
                    var operationOptions = new CimOperationOptions
                    {
                        Timeout = TimeSpan.FromMilliseconds(10000),
                        CancellationToken = cancelTokenSource.Token,
                        // This prefix works against all versions of the WinRM server stack, both win8 and win7
                        ResourceUriPrefix = new Uri(ComputerWMIHelper.CimUriPrefix)
                    };

                    IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                             ComputerWMIHelper.CimOperatingSystemNamespace,
                                                             ComputerWMIHelper.CimQueryDialect,
                                                             "Select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem,
                                                             operationOptions);

                    foreach (CimInstance cimInstance in mCollection)
                    {
                        var oldName = cimInstance.CimInstanceProperties["DNSHostName"].Value.ToString();
                        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                        {
                            string errMsg = StringUtil.Format(ComputerResources.NewNameIsOldName, computerName, newName);
                            ErrorRecord error = new(
                                    new InvalidOperationException(errMsg), "NewNameIsOldName",
                                    ErrorCategory.InvalidArgument, newName);
                            WriteError(error);

                            continue;
                        }

                        // If the target computer is in a domain, always use the DomainCred. If the DomainCred is not given,
                        // use null for UserName and Password, so the context of the caller will be used.
                        // If the target computer is not in a domain, just use null for the UserName and Password
                        string dUserName = null;
                        string dPassword = null;
                        if (((bool)cimInstance.CimInstanceProperties["PartOfDomain"].Value) && (DomainCredential != null))
                        {
                            dUserName = DomainCredential.UserName;
                            dPassword = Utils.GetStringFromSecureString(DomainCredential.Password);
                        }

                        var methodParameters = new CimMethodParametersCollection();
                        methodParameters.Add(CimMethodParameter.Create(
                            "Name",
                            newName,
                            Microsoft.Management.Infrastructure.CimType.String,
                            CimFlags.None));

                        methodParameters.Add(CimMethodParameter.Create(
                            "UserName",
                            dUserName,
                            Microsoft.Management.Infrastructure.CimType.String,
                            (dUserName == null) ? CimFlags.NullValue : CimFlags.None));

                        methodParameters.Add(
                            CimMethodParameter.Create(
                            "Password",
                            dPassword,
                            Microsoft.Management.Infrastructure.CimType.String,
                            (dPassword == null) ? CimFlags.NullValue : CimFlags.None));

                        if (!InternalTestHooks.TestRenameComputer)
                        {
                            CimMethodResult result = cimSession.InvokeMethod(
                                ComputerWMIHelper.CimOperatingSystemNamespace,
                                cimInstance,
                                "Rename",
                                methodParameters,
                                operationOptions);

                            retVal = Convert.ToInt32(result.ReturnValue.Value, CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            retVal = InternalTestHooks.TestRenameComputerResults;
                        }

                        if (retVal != 0)
                        {
                            var ex = new Win32Exception(retVal);
                            string errMsg = StringUtil.Format(ComputerResources.FailToRename, computerName, newName, ex.Message);
                            ErrorRecord error = new(new InvalidOperationException(errMsg), "FailToRenameComputer", ErrorCategory.OperationStopped, computerName);
                            WriteError(error);
                        }
                        else
                        {
                            successful = true;
                        }

                        if (PassThru)
                        {
                            WriteObject(ComputerWMIHelper.GetRenameComputerStatusObject(retVal, newName, computerName));
                        }

                        if (successful)
                        {
                            if (_restart)
                            {
                                // If successful and the Restart parameter is specified, restart the computer
                                object[] flags = new object[] { 6, 0 };
                                ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(
                                    this,
                                    isLocalhost,
                                    computerName,
                                    flags,
                                    credToUse,
                                    WsmanAuthentication,
                                    ComputerResources.RestartcomputerFailed,
                                    "RestartcomputerFailed",
                                    cancelTokenSource.Token);
                            }
                            else
                            {
                                WriteWarning(StringUtil.Format(ComputerResources.RestartNeeded, null, computerName));
                            }
                        }
                    }
                }
            }
            catch (CimException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (Exception ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
        }

        #endregion "Private Methods"

        #region "Override Methods"

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            string targetComputer = ValidateComputerName();
            if (targetComputer == null)
            {
                return;
            }

            bool isLocalhost = targetComputer.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            if (isLocalhost)
            {
                if (!_containsLocalHost)
                    _containsLocalHost = true;
                _newNameForLocalHost = NewName;

                return;
            }

            DoRenameComputerAction(targetComputer, NewName, false);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_containsLocalHost)
            {
                return;
            }

            DoRenameComputerAction("localhost", _newNameForLocalHost, true);
        }

        #endregion "Override Methods"
    }

    #endregion Rename-Computer

    #region "Public API"
    /// <summary>
    /// The object returned by SAM Computer cmdlets representing the status of the target machine.
    /// </summary>
    public sealed class ComputerChangeInfo
    {
        private const string MatchFormat = "{0}:{1}";

        /// <summary>
        /// The HasSucceeded which shows the operation was success or not.
        /// </summary>
        public bool HasSucceeded { get; set; }

        /// <summary>
        /// The ComputerName on which the operation is done.
        /// </summary>
        public string ComputerName { get; set; }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FormatLine(this.HasSucceeded.ToString(), this.ComputerName);
        }

        /// <summary>
        /// Formats a line for use in ToString.
        /// </summary>
        /// <param name="HasSucceeded"></param>
        /// <param name="computername"></param>
        /// <returns></returns>
        private static string FormatLine(string HasSucceeded, string computername)
        {
            return StringUtil.Format(MatchFormat, HasSucceeded, computername);
        }
    }

    /// <summary>
    /// The object returned by Rename-Computer cmdlet representing the status of the target machine.
    /// </summary>
    public sealed class RenameComputerChangeInfo
    {
        private const string MatchFormat = "{0}:{1}:{2}";

        /// <summary>
        /// The status which shows the operation was success or failure.
        /// </summary>
        public bool HasSucceeded { get; set; }

        /// <summary>
        /// The NewComputerName which represents the target machine.
        /// </summary>
        public string NewComputerName { get; set; }

        /// <summary>
        /// The OldComputerName which represented the target machine.
        /// </summary>
        public string OldComputerName { get; set; }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FormatLine(this.HasSucceeded.ToString(), this.NewComputerName, this.OldComputerName);
        }

        /// <summary>
        /// Formats a line for use in ToString.
        /// </summary>
        /// <param name="HasSucceeded"></param>
        /// <param name="newcomputername"></param>
        /// <param name="oldcomputername"></param>
        /// <returns></returns>
        private static string FormatLine(string HasSucceeded, string newcomputername, string oldcomputername)
        {
            return StringUtil.Format(MatchFormat, HasSucceeded, newcomputername, oldcomputername);
        }
    }
    #endregion "Public API"

    #region Helper
    /// <summary>
    /// Helper Class used by Stop-Computer,Restart-Computer and Test-Connection
    /// Also Contain constants used by System Restore related Cmdlets.
    /// </summary>
    internal static class ComputerWMIHelper
    {
        /// <summary>
        /// The maximum length of a valid NetBIOS name.
        /// </summary>
        internal const int NetBIOSNameMaxLength = 15;

        /// <summary>
        /// System Restore Class used by Cmdlets.
        /// </summary>
        internal const string WMI_Class_SystemRestore = "SystemRestore";

        /// <summary>
        /// OperatingSystem WMI class used by Cmdlets.
        /// </summary>
        internal const string WMI_Class_OperatingSystem = "Win32_OperatingSystem";

        /// <summary>
        /// Service WMI class used by Cmdlets.
        /// </summary>
        internal const string WMI_Class_Service = "Win32_Service";

        /// <summary>
        /// Win32_ComputerSystem WMI class used by Cmdlets.
        /// </summary>
        internal const string WMI_Class_ComputerSystem = "Win32_ComputerSystem";

        /// <summary>
        /// Ping Class used by Cmdlet.
        /// </summary>
        internal const string WMI_Class_PingStatus = "Win32_PingStatus";

        /// <summary>
        /// CIMV2 path.
        /// </summary>
        internal const string WMI_Path_CIM = "\\root\\cimv2";

        /// <summary>
        /// Default path.
        /// </summary>
        internal const string WMI_Path_Default = "\\root\\default";

        /// <summary>
        /// The error says The interface is unknown.
        /// </summary>
        internal const int ErrorCode_Interface = 1717;

        /// <summary>
        /// This error says An instance of the service is already running.
        /// </summary>
        internal const int ErrorCode_Service = 1056;

        /// <summary>
        /// The name of the privilege to shutdown a local system.
        /// </summary>
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        /// <summary>
        /// The name of the privilege to shutdown a remote system.
        /// </summary>
        internal const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";

        /// <summary>
        /// CimUriPrefix.
        /// </summary>
        internal const string CimUriPrefix = "http://schemas.microsoft.com/wbem/wsman/1/wmi/root/cimv2";

        /// <summary>
        /// CimOperatingSystemNamespace.
        /// </summary>
        internal const string CimOperatingSystemNamespace = "root/cimv2";

        /// <summary>
        /// CimOperatingSystemShutdownMethod.
        /// </summary>
        internal const string CimOperatingSystemShutdownMethod = "Win32shutdown";

        /// <summary>
        /// CimQueryDialect.
        /// </summary>
        internal const string CimQueryDialect = "WQL";

        /// <summary>
        /// Local host name.
        /// </summary>
        internal const string localhostStr = "localhost";

        /// <summary>
        /// Get the local admin user name from a local NetworkCredential.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="psLocalCredential"></param>
        /// <returns></returns>
        internal static string GetLocalAdminUserName(string computerName, PSCredential psLocalCredential)
        {
            string localUserName = null;

            // The format of local admin username should be "ComputerName\AdminName"
            if (psLocalCredential.UserName.Contains('\\'))
            {
                localUserName = psLocalCredential.UserName;
            }
            else
            {
                int dotIndex = computerName.IndexOf('.');
                if (dotIndex == -1)
                {
                    localUserName = computerName + "\\" + psLocalCredential.UserName;
                }
                else
                {
                    localUserName = string.Concat(computerName.AsSpan(0, dotIndex), "\\", psLocalCredential.UserName);
                }
            }

            return localUserName;
        }

        /// <summary>
        /// Generate a random password.
        /// </summary>
        /// <param name="passwordLength"></param>
        /// <returns></returns>
        internal static string GetRandomPassword(int passwordLength)
        {
            const int charMin = 32, charMax = 122;
            const int allowedCharsCount = charMax - charMin + 1;
            byte[] randomBytes = new byte[passwordLength];
            char[] chars = new char[passwordLength];

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = (char)(randomBytes[i] % allowedCharsCount + charMin);
            }

            return new string(chars);
        }

        /// <summary>
        /// Gets the Scope.
        /// </summary>
        /// <param name="computer"></param>
        /// <param name="namespaceParameter"></param>
        /// <returns></returns>
        internal static string GetScopeString(string computer, string namespaceParameter)
        {
            StringBuilder returnValue = new("\\\\");
            if (computer.Equals("::1", StringComparison.OrdinalIgnoreCase) || computer.Equals("[::1]", StringComparison.OrdinalIgnoreCase))
            {
                returnValue.Append("localhost");
            }
            else
            {
                returnValue.Append(computer);
            }

            returnValue.Append(namespaceParameter);
            return returnValue.ToString();
        }

        /// <summary>
        /// Returns true if it is a valid drive on the system.
        /// </summary>
        /// <param name="drive"></param>
        /// <returns></returns>
        internal static bool IsValidDrive(string drive)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo logicalDrive in drives)
            {
                if (logicalDrive.DriveType.Equals(DriveType.Fixed))
                {
                    if (drive.Equals(logicalDrive.Name, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether string[] contains System Drive.
        /// </summary>
        /// <param name="drives"></param>
        /// <param name="sysdrive"></param>
        /// <returns></returns>
        internal static bool ContainsSystemDrive(string[] drives, string sysdrive)
        {
            string driveApp;
            foreach (string drive in drives)
            {
                if (!drive.EndsWith('\\'))
                {
                    driveApp = string.Concat(drive, "\\");
                }
                else
                    driveApp = drive;
                if (driveApp.Equals(sysdrive, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the given computernames in a string.
        /// </summary>
        /// <param name="computerNames"></param>
        internal static string GetMachineNames(string[] computerNames)
        {
            string separator = ",";
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\International");
            if (regKey != null)
            {
                object sListValue = regKey.GetValue("sList");
                if (sListValue != null)
                {
                    separator = sListValue.ToString();
                }
            }

            string compname = string.Empty;
            StringBuilder strComputers = new();
            int i = 0;
            foreach (string computer in computerNames)
            {
                if (i > 0)
                {
                    strComputers.Append(separator);
                }
                else
                {
                    i++;
                }

                if ((computer.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (computer.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    compname = Dns.GetHostName();
                }
                else
                {
                    compname = computer;
                }

                strComputers.Append(compname);
            }

            return strComputers.ToString();
        }

        internal static ComputerChangeInfo GetComputerStatusObject(int errorcode, string computername)
        {
            ComputerChangeInfo computerchangeinfo = new();
            computerchangeinfo.ComputerName = computername;
            if (errorcode != 0)
            {
                computerchangeinfo.HasSucceeded = false;
            }
            else
            {
                computerchangeinfo.HasSucceeded = true;
            }

            return computerchangeinfo;
        }

        internal static RenameComputerChangeInfo GetRenameComputerStatusObject(int errorcode, string newcomputername, string oldcomputername)
        {
            RenameComputerChangeInfo renamecomputerchangeinfo = new();
            renamecomputerchangeinfo.OldComputerName = oldcomputername;
            renamecomputerchangeinfo.NewComputerName = newcomputername;
            if (errorcode != 0)
            {
                renamecomputerchangeinfo.HasSucceeded = false;
            }
            else
            {
                renamecomputerchangeinfo.HasSucceeded = true;
            }

            return renamecomputerchangeinfo;
        }

        internal static void WriteNonTerminatingError(int errorcode, PSCmdlet cmdlet, string computername)
        {
            Win32Exception ex = new(errorcode);
            string additionalmessage = string.Empty;
            if (ex.NativeErrorCode.Equals(0x00000035))
            {
                additionalmessage = StringUtil.Format(ComputerResources.NetworkPathNotFound, computername);
            }

            string message = StringUtil.Format(ComputerResources.OperationFailed, ex.Message, computername, additionalmessage);
            ErrorRecord er = new(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, computername);
            cmdlet.WriteError(er);
        }

        /// <summary>
        /// Check whether the new computer name is valid.
        /// </summary>
        /// <param name="computerName"></param>
        /// <returns></returns>
        internal static bool IsComputerNameValid(string computerName)
        {
            bool allDigits = true;

            if (computerName.Length >= 64)
                return false;

            foreach (char t in computerName)
            {
                if (t >= 'A' && t <= 'Z' ||
                    t >= 'a' && t <= 'z')
                {
                    allDigits = false;
                    continue;
                }
                else if (t >= '0' && t <= '9')
                {
                    continue;
                }
                else if (t == '-')
                {
                    allDigits = false;
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return !allDigits;
        }

        /// <summary>
        /// Invokes the Win32Shutdown command on provided target computer using WSMan
        /// over a CIMSession.  The flags parameter determines the type of shutdown operation
        /// such as shutdown, reboot, force etc.
        /// </summary>
        /// <param name="cmdlet">Cmdlet host for reporting errors.</param>
        /// <param name="isLocalhost">True if local host computer.</param>
        /// <param name="computerName">Target computer.</param>
        /// <param name="flags">Win32Shutdown flags.</param>
        /// <param name="credential">Optional credential.</param>
        /// <param name="authentication">Optional authentication.</param>
        /// <param name="formatErrorMessage">Error message format string that takes two parameters.</param>
        /// <param name="ErrorFQEID">Fully qualified error Id.</param>
        /// <param name="cancelToken">Cancel token.</param>
        /// <returns>True on success.</returns>
        internal static bool InvokeWin32ShutdownUsingWsman(
            PSCmdlet cmdlet,
            bool isLocalhost,
            string computerName,
            object[] flags,
            PSCredential credential,
            string authentication,
            string formatErrorMessage,
            string ErrorFQEID,
            CancellationToken cancelToken)
        {
            Dbg.Diagnostics.Assert(flags.Length == 2, "Caller need to verify the flags passed in");

            bool isSuccess = false;
            string targetMachine = isLocalhost ? "localhost" : computerName;
            string authInUse = isLocalhost ? null : authentication;
            PSCredential credInUse = isLocalhost ? null : credential;
            var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(10000),
                CancellationToken = cancelToken,
                // This prefix works against all versions of the WinRM server stack, both win8 and win7
                ResourceUriPrefix = new Uri(ComputerWMIHelper.CimUriPrefix)
            };

            try
            {
                if (!(isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_SHUTDOWN_NAME, ref currentPrivilegeState)) &&
                    !(!isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState)))
                {
                    string message =
                        StringUtil.Format(ComputerResources.PrivilegeNotEnabled, computerName,
                            isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME);
                    ErrorRecord errorRecord = new(new InvalidOperationException(message), "PrivilegeNotEnabled", ErrorCategory.InvalidOperation, null);
                    cmdlet.WriteError(errorRecord);
                    return false;
                }

                using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(targetMachine, credInUse, authInUse, isLocalhost, cmdlet, cancelToken))
                {
                    var methodParameters = new CimMethodParametersCollection();
                    int retVal;
                    methodParameters.Add(CimMethodParameter.Create(
                        "Flags",
                        flags[0],
                        Microsoft.Management.Infrastructure.CimType.SInt32,
                        CimFlags.None));

                    methodParameters.Add(CimMethodParameter.Create(
                        "Reserved",
                        flags[1],
                        Microsoft.Management.Infrastructure.CimType.SInt32,
                        CimFlags.None));

                    if (!InternalTestHooks.TestStopComputer)
                    {
                        CimMethodResult result = null;

                        if (isLocalhost)
                        {
                            // Win32_ComputerSystem is a singleton hence FirstOrDefault() return the only instance returned by EnumerateInstances.
                            var computerSystem = cimSession.EnumerateInstances(ComputerWMIHelper.CimOperatingSystemNamespace, ComputerWMIHelper.WMI_Class_OperatingSystem).FirstOrDefault();

                            result = cimSession.InvokeMethod(
                                ComputerWMIHelper.CimOperatingSystemNamespace,
                                computerSystem,
                                ComputerWMIHelper.CimOperatingSystemShutdownMethod,
                                methodParameters,
                                operationOptions);
                        }
                        else
                        {
                            result = cimSession.InvokeMethod(
                                ComputerWMIHelper.CimOperatingSystemNamespace,
                                ComputerWMIHelper.WMI_Class_OperatingSystem,
                                ComputerWMIHelper.CimOperatingSystemShutdownMethod,
                                methodParameters,
                                operationOptions);
                        }

                        retVal = Convert.ToInt32(result.ReturnValue.Value, CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        retVal = InternalTestHooks.TestStopComputerResults;
                    }

                    if (retVal != 0)
                    {
                        var ex = new Win32Exception(retVal);
                        string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                        ErrorRecord error = new(
                            new InvalidOperationException(errMsg), ErrorFQEID, ErrorCategory.OperationStopped, computerName);
                        cmdlet.WriteError(error);
                    }
                    else
                    {
                        isSuccess = true;
                    }
                }
            }
            catch (CimException ex)
            {
                string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                ErrorRecord error = new(new InvalidOperationException(errMsg), ErrorFQEID,
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            catch (Exception ex)
            {
                string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                ErrorRecord error = new(new InvalidOperationException(errMsg), ErrorFQEID,
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            finally
            {
                // Restore the previous privilege state if something unexpected happened
                PlatformInvokes.RestoreTokenPrivilege(
                    isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState);
            }

            return isSuccess;
        }

        /// <summary>
        /// Returns valid computer name or null on failure.
        /// </summary>
        /// <param name="nameToCheck">Computer name to validate.</param>
        /// <param name="shortLocalMachineName"></param>
        /// <param name="fullLocalMachineName"></param>
        /// <param name="error"></param>
        /// <returns>Valid computer name.</returns>
        internal static string ValidateComputerName(
            string nameToCheck,
            string shortLocalMachineName,
            string fullLocalMachineName,
            ref ErrorRecord error)
        {
            string validatedComputerName = null;

            if (nameToCheck.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(localhostStr, StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(shortLocalMachineName, StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(fullLocalMachineName, StringComparison.OrdinalIgnoreCase))
            {
                validatedComputerName = localhostStr;
            }
            else
            {
                bool isIPAddress = false;
                try
                {
                    isIPAddress = IPAddress.TryParse(nameToCheck, out _);
                }
                catch (Exception)
                {
                }

                try
                {
                    string fqcn = Dns.GetHostEntryAsync(nameToCheck).Result.HostName;
                    if (fqcn.Equals(shortLocalMachineName, StringComparison.OrdinalIgnoreCase) ||
                        fqcn.Equals(fullLocalMachineName, StringComparison.OrdinalIgnoreCase))
                    {
                        // The IPv4 or IPv6 of the local machine is specified
                        validatedComputerName = localhostStr;
                    }
                    else
                    {
                        validatedComputerName = nameToCheck;
                    }
                }
                catch (Exception e)
                {
                    // If GetHostEntry() throw exception, then the target should not be the local machine
                    if (!isIPAddress)
                    {
                        // Return error if the computer name is not an IP address. Dns.GetHostEntry() may not work on IP addresses.
                        string errMsg = StringUtil.Format(ComputerResources.CannotResolveComputerName, nameToCheck, e.Message);
                        error = new ErrorRecord(
                            new InvalidOperationException(errMsg), "AddressResolutionException",
                            ErrorCategory.InvalidArgument, nameToCheck);

                        return null;
                    }

                    validatedComputerName = nameToCheck;
                }
            }

            return validatedComputerName;
        }
    }
    #endregion Helper
}

#endif
