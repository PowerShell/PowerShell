// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics; // Process
using System.Globalization;
using System.IO; // Path, File
using System.Management.Automation; // PSObject
using System.ServiceProcess;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Unix service managers supported by the service cmdlets.
    /// </summary>
    internal enum ServiceManagerKind
    {
        /// <summary>systemd, managed through 'systemctl'.</summary>
        Systemd,

        /// <summary>launchd, managed through 'launchctl'.</summary>
        LaunchD,

        /// <summary>sysvinit or a compatible init system, managed through the generic 'service' wrapper.</summary>
        SysVInit,

        /// <summary>OpenRC, managed through 'rc-service'.</summary>
        OpenRC,
    }

    /// <summary>
    /// Unix implementation of the service-controller surface used by the service
    /// cmdlets (Get/Start/Stop/Restart-Service). On Windows builds this type does
    /// not exist and Service.cs uses System.ServiceProcess.ServiceController; in
    /// Service.cs an alias maps the unqualified name to this type on Unix so the
    /// shared cmdlet implementation works unchanged.
    /// <para>
    /// Operations are performed through the init system's service manager,
    /// discovered in this order: systemctl (systemd), launchctl (macOS),
    /// service (sysvinit) and rc-service (OpenRC). Mutating operations are
    /// wrapped with <c>sudo -n</c> when not running as root.
    /// </para>
    /// </summary>
    public sealed class UnixServiceController : IDisposable
    {
        private const string ServiceSuffix = ".service";

        private static readonly (string ControlCommand, string StatusCommand, ServiceManagerKind Kind)[] ManagerCandidates =
        {
            ("systemctl", "systemctl", ServiceManagerKind.Systemd),
            ("launchctl", "launchctl", ServiceManagerKind.LaunchD),
            ("service", "service", ServiceManagerKind.SysVInit),
            ("rc-service", "rc-status", ServiceManagerKind.OpenRC),
        };

        private bool _resolved;
        private bool _exists = true;
        private ServiceManagerKind _managerKind = ServiceManagerKind.Systemd;
        private ServiceControllerStatus _status;
        private PSObject? _displayMemberSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnixServiceController"/> class
        /// for the specified service. Reading <see cref="Status"/> throws
        /// <see cref="InvalidOperationException"/> when the service does not exist.
        /// </summary>
        /// <param name="serviceName">The service name, with or without the '.service' suffix.</param>
        public UnixServiceController(string serviceName)
        {
            Name = UnixServiceParser.RemoveServiceSuffix(serviceName.Trim());
            Resolve();
            AttachDisplaySettings();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnixServiceController"/> class
        /// for population by the service parser.
        /// </summary>
        internal UnixServiceController()
        {
            // The parser populates all properties right after construction,
            // so lazy resolution is disabled for such instances.
            _resolved = true;
        }

        /// <summary>Gets the name of the service (without the '.service' suffix on systemd).</summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>Gets the name of the service. Same as <see cref="Name"/> on Unix.</summary>
        public string ServiceName => Name;

        /// <summary>Gets the display name of the service.</summary>
        public string DisplayName { get; internal set; } = string.Empty;

        /// <summary>Gets the machine the service resides on (always the local machine).</summary>
        public string MachineName => ".";

        /// <summary>
        /// Gets the status of the service, mapped onto the
        /// <see cref="ServiceControllerStatus"/> values also used on Windows.
        /// Throws <see cref="InvalidOperationException"/> when the service does not exist.
        /// </summary>
        public ServiceControllerStatus Status
        {
            get
            {
                Resolve();
                if (!_exists)
                {
                    throw new InvalidOperationException(
                        FormatMessage(ServiceResources.NoServiceFoundForGivenName, Name));
                }

                return _status;
            }

            internal set
            {
                _status = value;
            }
        }

        /// <summary>
        /// Gets the start type of the service, mapped onto the
        /// <see cref="ServiceStartMode"/> values also used on Windows.
        /// </summary>
        public ServiceStartMode StartType { get; internal set; }

        /// <summary>Gets the description of the service when the manager provides one.</summary>
        public string? Description { get; internal set; }

        /// <summary>Gets the sub state of a systemd service when available.</summary>
        public string? SubState { get; internal set; }

        /// <summary>Gets the unit file state of a systemd service when available.</summary>
        public string? UnitFileState { get; internal set; }

        /// <summary>Gets a value indicating whether the service can be stopped.</summary>
        public bool CanStop
        {
            get
            {
                Resolve();
                return _exists && (_status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending);
            }
        }

        /// <summary>Gets a value indicating whether the service can be paused and continued. Always false on Unix.</summary>
        public bool CanPauseAndContinue => false;

        /// <summary>Gets the services that depend on this service (systemd only).</summary>
        public UnixServiceController[] DependentServices
        {
            get
            {
                return GetRelatedServices(reverse: true);
            }
        }

        /// <summary>Gets the services this service depends on (systemd only).</summary>
        public UnixServiceController[] ServicesDependedOn
        {
            get
            {
                return GetRelatedServices(reverse: false);
            }
        }

        /// <summary>
        /// Enumerates all services known to the service manager.
        /// </summary>
        /// <returns>All services reported by the service manager, sorted by name.</returns>
        public static UnixServiceController[] GetServices()
        {
            (string control, _, ServiceManagerKind kind) = ResolveManager();
            List<UnixServiceController> services;

            switch (kind)
            {
                case ServiceManagerKind.Systemd:
                    {
                        string units = RunQueryCommand(control, new[] { "list-units", "--type=service", "--all", "--no-legend", "--no-pager" });
                        string unitFiles = RunQueryCommand(control, new[] { "list-unit-files", "--type=service", "--no-legend", "--no-pager" });
                        services = UnixServiceParser.ParseSystemdListUnits(units);
                        UnixServiceParser.MergeSystemdUnitFiles(services, unitFiles);
                    }

                    break;

                case ServiceManagerKind.LaunchD:
                    {
                        services = UnixServiceParser.ParseLaunchCtlList(RunQueryCommand("launchctl", new[] { "list" }));
                    }

                    break;

                case ServiceManagerKind.SysVInit:
                    {
                        // Some init scripts write their status to standard error.
                        string output = RunQueryCommand("service", new[] { "--status-all" }, mergeErrorStream: true);
                        services = UnixServiceParser.ParseSysVStatusAll(output);
                    }

                    break;

                case ServiceManagerKind.OpenRC:
                    {
                        services = UnixServiceParser.ParseOpenRcStatusAll(RunQueryCommand("rc-status", new[] { "--all" }));
                    }

                    break;

                default:
                    services = new List<UnixServiceController>();
                    break;
            }

            services.Sort(static (x, y) => string.CompareOrdinal(x.Name, y.Name));
            foreach (UnixServiceController service in services)
            {
                service._managerKind = kind;
                service.AttachDisplaySettings();
            }

            return services.ToArray();
        }

        /// <summary>Starts the service.</summary>
        public void Start()
        {
            RunManagerOperation("start");
            Status = ServiceControllerStatus.Running;
        }

        /// <summary>Stops the service.</summary>
        public void Stop()
        {
            RunManagerOperation("stop");
            Status = ServiceControllerStatus.Stopped;
        }

        /// <summary>Pausing a service is not supported on Unix.</summary>
        public void Pause()
        {
            throw new PlatformNotSupportedException(ServiceResources.CouldNotSuspendServiceNotSupported);
        }

        /// <summary>Resuming a paused service is not supported on Unix.</summary>
        public void Continue()
        {
            throw new PlatformNotSupportedException(ServiceResources.CouldNotSuspendServiceNotSupported);
        }

        /// <summary>
        /// Waits for the service to reach the specified status or throws
        /// System.ServiceProcess.TimeoutException when the timeout expires.
        /// </summary>
        /// <param name="desiredStatus">The status to wait for.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        public void WaitForStatus(ServiceControllerStatus desiredStatus, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                if (Status == desiredStatus)
                {
                    return;
                }

                if (DateTime.UtcNow > deadline)
                {
                    // System.ServiceProcess.ServiceController.WaitForStatus throws
                    // System.ServiceProcess.TimeoutException, which is what the
                    // shared cmdlet code catches.
                    throw new System.ServiceProcess.TimeoutException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "The service '{0}' did not reach the '{1}' status within the required timeout.",
                            Name,
                            desiredStatus));
                }

                Thread.Sleep(200);
                Refresh();
            }
        }

        /// <summary>Re-queries the service manager for the current service state.</summary>
        public void Refresh()
        {
            _resolved = false;
            Resolve();
        }

        /// <summary>Releases resources used by the service controller.</summary>
        public void Dispose()
        {
        }

        /// <summary>Returns the service name.</summary>
        /// <returns>The service name.</returns>
        public override string ToString() => Name;

        private static string[] GetPathDirectories()
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return pathVariable.Split(':', StringSplitOptions.RemoveEmptyEntries);
        }

        private static (string Control, string Status, ServiceManagerKind Kind) ResolveManager()
        {
            string[] pathDirectories = GetPathDirectories();
            foreach ((string control, string status, ServiceManagerKind kind) in ManagerCandidates)
            {
                if (FindCommandOnPath(control, pathDirectories) is not null)
                {
                    return (control, status, kind);
                }
            }

            throw NewServiceManagerNotFoundException();
        }

        private static string? FindCommandOnPath(string command, string[] pathDirectories)
        {
            foreach (string directory in pathDirectories)
            {
                string candidate = Path.Combine(directory, command);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static ProcessStartInfo CreateStartInfo(string fileName, string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        private static (string Stdout, string Stderr, int ExitCode) Execute(ProcessStartInfo startInfo)
        {
            using Process process = new() { StartInfo = startInfo };
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (stdout, stderr, process.ExitCode);
        }

        private static string RunQueryCommand(string command, string[] arguments, bool mergeErrorStream = false)
        {
            string? commandPath = FindCommandOnPath(command, GetPathDirectories());
            if (commandPath is null)
            {
                throw NewServiceManagerNotFoundException();
            }

            (string stdout, string stderr, int exitCode) = Execute(CreateStartInfo(commandPath, arguments));
            if (exitCode != 0)
            {
                throw new Win32Exception(exitCode, stderr.Length > 0 ? stderr.TrimEnd() : FormatMessage(ServiceResources.ExitCodeWithoutStderr, exitCode));
            }

            return mergeErrorStream ? stdout + stderr : stdout;
        }

        private static InvalidOperationException NewServiceManagerNotFoundException()
        {
            var exception = new InvalidOperationException(ServiceResources.ServiceManagerNotFound);
            exception.Data["NoServiceManagerFound"] = true;
            return exception;
        }

        private static string FormatMessage(string resourceId, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, resourceId, args);
        }

        private string UnitName() => Name + ServiceSuffix;

        private string LaunchdUnitPath()
        {
            string domain = UnixServiceNativeMethods.GetEffectiveUserId() == 0
                ? "system"
                : $"gui/{UnixServiceNativeMethods.GetUserId()}";
            return domain + "/" + Name;
        }

        private string[] LaunchArguments(string action)
        {
            return action switch
            {
                "start" => new[] { "kickstart", LaunchdUnitPath() },
                "stop" => new[] { "kill", "SIGTERM", LaunchdUnitPath() },
                _ => new[] { "kickstart", "-k", LaunchdUnitPath() },
            };
        }

        private void RunManagerOperation(string action)
        {
            (string control, _, ServiceManagerKind kind) = ResolveManager();
            string[] arguments = kind switch
            {
                ServiceManagerKind.Systemd => new[] { action, UnitName() },
                ServiceManagerKind.LaunchD => LaunchArguments(action),
                _ => new[] { Name, action },
            };

            RunControlCommand(control, arguments);
        }

        private void RunControlCommand(string control, string[] arguments)
        {
            string? commandPath = FindCommandOnPath(control, GetPathDirectories());
            if (commandPath is null)
            {
                throw NewServiceManagerNotFoundException();
            }

            bool isRoot = UnixServiceNativeMethods.GetEffectiveUserId() == 0;
            ProcessStartInfo startInfo = CreateStartInfo(commandPath, arguments);
            if (!isRoot)
            {
                // Wrap with 'sudo -n' so that prompts never hang; failures surface
                // as SudoRequired errors in the cmdlet layer.
                string? sudoPath = FindCommandOnPath("sudo", GetPathDirectories());
                if (sudoPath is null)
                {
                    throw NewSudoRequiredException();
                }

                string[] sudoArguments = new string[arguments.Length + 2];
                sudoArguments[0] = "-n";
                sudoArguments[1] = commandPath;
                arguments.CopyTo(sudoArguments, 2);
                startInfo = CreateStartInfo(sudoPath, sudoArguments);
            }

            (_, string stderr, int exitCode) = Execute(startInfo);
            if (exitCode != 0)
            {
                string detail = stderr.TrimEnd();
                if (!isRoot && detail.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    throw NewSudoRequiredException();
                }

                throw new Win32Exception(
                    exitCode,
                    detail.Length > 0 ? detail : FormatMessage(ServiceResources.ExitCodeWithoutStderr, exitCode));
            }
        }

        private InvalidOperationException NewSudoRequiredException()
        {
            var exception = new InvalidOperationException(
                FormatMessage(ServiceResources.SudoRequired, Name, DisplayName));
            exception.Data["SudoRequired"] = true;
            return exception;
        }

        private UnixServiceController[] GetRelatedServices(bool reverse)
        {
            Resolve();
            if (!_exists || _managerKind != ServiceManagerKind.Systemd)
            {
                return Array.Empty<UnixServiceController>();
            }

            string[] arguments = reverse
                ? new[] { "list-dependencies", "--reverse", "--plain", UnitName() }
                : new[] { "list-dependencies", "--plain", UnitName() };
            string output = RunQueryCommand("systemctl", arguments);
            List<UnixServiceController> result = new();
            foreach (string dependencyName in UnixServiceParser.ParseSystemdDependencyUnits(output))
            {
                if (!string.Equals(dependencyName, Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(new UnixServiceController(dependencyName));
                }
            }

            return result.ToArray();
        }

        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            _exists = false;

            foreach (UnixServiceController candidate in GetServices())
            {
                if (string.Equals(candidate.Name, Name, StringComparison.OrdinalIgnoreCase))
                {
                    DisplayName = candidate.DisplayName;
                    Status = candidate.Status;
                    StartType = candidate.StartType;
                    Description = candidate.Description;
                    SubState = candidate.SubState;
                    UnitFileState = candidate.UnitFileState;
                    _managerKind = candidate._managerKind;
                    _exists = true;
                    break;
                }
            }

            AttachDisplaySettings();
        }

        private void AttachDisplaySettings()
        {
            if (_displayMemberSet is not null)
            {
                return;
            }

            PSObject serviceAsPSObj = PSObject.AsPSObject(this);
            PSMemberInfo[] defaultPropertySet = new PSMemberInfo[]
            {
                new PSPropertySet("DefaultDisplayPropertySet", new string[] { "Status", "Name", "DisplayName", "StartType" }),
            };
            serviceAsPSObj.Members.Add(new PSMemberSet("PSStandardMember", defaultPropertySet));
            _displayMemberSet = serviceAsPSObj;
        }
    }
}
#endif
