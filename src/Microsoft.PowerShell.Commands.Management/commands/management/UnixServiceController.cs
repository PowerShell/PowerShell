// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics; // Process
using System.Globalization;
using System.IO; // Path, File
using System.Management.Automation; // PSObject
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

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

        private static readonly ConcurrentDictionary<Guid, Process> s_activeProcesses = new();
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> s_cancellationSources = new();

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
        /// <param name="ownerToken">The invocation token used to correlate commands launched
        /// during resolution with the owning cmdlet run for cancellation.</param>
        public UnixServiceController(string serviceName, Guid? ownerToken = null)
        {
            // The '.service' suffix is a systemd naming detail; other managers
            // use the name exactly as provided.
            string trimmed = serviceName.Trim();
            Name = ResolveManager().Kind == ServiceManagerKind.Systemd
                ? UnixServiceParser.RemoveServiceSuffix(trimmed)
                : trimmed;
            OwnerToken = ownerToken;
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

        /// <summary>
        /// Gets or sets the invocation token that correlates this instance with the service
        /// cmdlet run that owns it, so a pipeline stop cancels the commands launched for it.
        /// Instances without a token use their own isolated execution key.
        /// </summary>
        internal Guid? OwnerToken { get; set; }

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
        public static UnixServiceController[] GetServices(Guid? ownerToken = null)
        {
            // Enumeration queries run on a transient instance carrying the caller's
            // invocation token, so cancellation reaches the manager commands.
            var query = new UnixServiceController { OwnerToken = ownerToken };
            (string control, _, ServiceManagerKind kind) = ResolveManager();
            List<UnixServiceController> services;

            switch (kind)
            {
                case ServiceManagerKind.Systemd:
                    {
                        // --plain keeps failed units from being prefixed with a marker,
                        // so their rows still start with the unit name.
                        string units = query.RunQueryCommand(control, new[] { "list-units", "--type=service", "--all", "--plain", "--no-legend", "--no-pager" });
                        string unitFiles = query.RunQueryCommand(control, new[] { "list-unit-files", "--type=service", "--no-legend", "--no-pager" });
                        services = UnixServiceParser.ParseSystemdListUnits(units);
                        UnixServiceParser.MergeSystemdUnitFiles(services, unitFiles);
                    }

                    break;

                case ServiceManagerKind.LaunchD:
                    {
                        services = UnixServiceParser.ParseLaunchCtlList(query.RunQueryCommand("launchctl", new[] { "list" }));
                    }

                    break;

                case ServiceManagerKind.SysVInit:
                    {
                        // Some init scripts write their status to standard error.
                        string output = query.RunQueryCommand("service", new[] { "--status-all" }, mergeErrorStream: true);
                        services = UnixServiceParser.ParseSysVStatusAll(output);
                    }

                    break;

                case ServiceManagerKind.OpenRC:
                    {
                        services = UnixServiceParser.ParseOpenRcStatusAll(query.RunQueryCommand("rc-status", new[] { "--all" }));
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

        /// <summary>
        /// Kills the in-flight service manager command, if any. Called from the service
        /// cmdlets' <see cref="Cmdlet.StopProcessing"/> when the pipeline is stopped, so a
        /// hung init script or manager command can't keep the pipeline waiting. The
        /// cancellation persists for the invocation: a manager command launched later for
        /// the same owner token is terminated immediately.
        /// </summary>
        /// <param name="ownerToken">The invocation token whose commands to cancel.</param>
        internal static void CancelActiveCommand(Guid ownerToken)
        {
            CancellationTokenSource cts = s_cancellationSources.GetOrAdd(ownerToken, static _ => new CancellationTokenSource());
            cts.Cancel();
            if (s_activeProcesses.TryRemove(ownerToken, out Process? process))
            {
                CancelActiveProcess(process);
            }
        }

        /// <summary>
        /// Releases the cancellation state of a finished cmdlet invocation. Called from the
        /// service cmdlets' <see cref="Cmdlet.EndProcessing"/>.
        /// </summary>
        /// <param name="ownerToken">The invocation token to clean up.</param>
        internal static void EndInvocation(Guid ownerToken)
        {
            if (s_cancellationSources.TryRemove(ownerToken, out CancellationTokenSource? cts))
            {
                cts.Dispose();
            }
        }

        private static void CancelActiveProcess(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process has not started or already exited.
            }
            catch (Win32Exception)
            {
                // The process could not be killed because it already exited.
            }
        }

        private static string? FindCommandOnPath(string command, string[] pathDirectories)
        {
            foreach (string directory in pathDirectories)
            {
                string candidate = Path.Combine(directory, command);
                if (File.Exists(candidate) && IsExecutable(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsExecutable(string path)
        {
            // access(X_OK) checks whether the current user can actually execute the file,
            // honoring ownership and group permissions; a mere execute bit on a file owned
            // by another user would let discovery select an unusable command.
            return UnixServiceNativeMethods.CanExecute(path);
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

        private (string Stdout, string Stderr, int ExitCode) Execute(ProcessStartInfo startInfo)
        {
            Guid executionKey = OwnerToken ?? Guid.NewGuid();
            CancellationTokenSource cts = s_cancellationSources.GetOrAdd(executionKey, static _ => new CancellationTokenSource());
            if (cts.IsCancellationRequested)
            {
                // The pipeline was stopped before this command could launch.
                throw new OperationCanceledException(cts.Token);
            }

            using Process process = new() { StartInfo = startInfo };
            s_activeProcesses[executionKey] = process;
            try
            {
                process.Start();

                // Drain both streams concurrently; reading them sequentially can deadlock when
                // the child process fills one pipe buffer while the other stream is still open.
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();

                // StopProcessing cancels the invocation token when the pipeline is stopped.
                // The registration is placed after Start, so a cancellation that arrived
                // before launch is applied here immediately, and a later one kills the
                // process, closes the pipes, completes both stream reads, and releases
                // this wait.
                using (cts.Token.Register(
                    static state => CancelActiveProcess((Process)state!),
                    process))
                {
                    process.WaitForExit();
                }

                return (stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult(), process.ExitCode);
            }
            finally
            {
                s_activeProcesses.TryRemove(executionKey, out _);
                if (OwnerToken is null && !cts.IsCancellationRequested)
                {
                    // Transient executions have no cmdlet run to clean up after them;
                    // owned sources persist until EndInvocation, so a cancellation that
                    // lands between manager commands still terminates the next one.
                    s_cancellationSources.TryRemove(executionKey, out _);
                }
            }
        }

        private string RunQueryCommand(string command, string[] arguments, bool mergeErrorStream = false)
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
            List<string> dependencyNames = UnixServiceParser.ParseSystemdDependencyUnits(output);
            if (dependencyNames.Count == 0)
            {
                return Array.Empty<UnixServiceController>();
            }

            // Resolve the dependencies against a single enumeration snapshot instead of
            // re-enumerating the whole catalog for every dependency.
            UnixServiceController[] services = GetServices();
            List<UnixServiceController> result = new();
            foreach (string dependencyName in dependencyNames)
            {
                if (string.Equals(dependencyName, Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                UnixServiceController? match = MatchService(services, dependencyName);
                if (match is null)
                {
                    // The dependency isn't reported by the enumeration (for example a
                    // template instance); resolve it by name instead.
                    result.Add(new UnixServiceController(dependencyName));
                    continue;
                }

                var dependent = new UnixServiceController();
                dependent.CopyFrom(match);
                dependent.AttachDisplaySettings();
                result.Add(dependent);
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

            UnixServiceController? match = MatchService(GetServices(OwnerToken), Name);
            if (match is not null)
            {
                CopyFrom(match);
            }

            AttachDisplaySettings();
        }

        private static UnixServiceController? MatchService(UnixServiceController[] services, string name)
        {
            foreach (UnixServiceController candidate in services)
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            // Fall back to the Windows-style case-insensitive lookup only when there is
            // no exact match.
            foreach (UnixServiceController candidate in services)
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void CopyFrom(UnixServiceController match)
        {
            // Adopt the canonical native name so that subsequent operations target the
            // service that was actually resolved, even when the lookup only matched
            // case-insensitively.
            Name = match.Name;
            DisplayName = match.DisplayName;
            Status = match.Status;
            StartType = match.StartType;
            Description = match.Description;
            SubState = match.SubState;
            UnitFileState = match.UnitFileState;
            _managerKind = match._managerKind;
            _exists = true;
        }

        internal UnixServiceController CreateInvocationLocal(Guid ownerToken)
        {
            // Input objects carry the producer run's owner token; create an invocation-local
            // controller so refresh and mutation run under the consuming run's token and are
            // cancellable by it.
            var local = new UnixServiceController { OwnerToken = ownerToken };
            local.Name = Name;
            local.DisplayName = DisplayName;
            local.StartType = StartType;
            local.Description = Description;
            local.SubState = SubState;
            local.UnitFileState = UnitFileState;
            local._managerKind = _managerKind;
            try
            {
                local.Status = Status;
                local._exists = _exists;
            }
            catch (InvalidOperationException)
            {
                // The producer resolved a service that no longer exists; keep the local view
                // consistent so the consumer surfaces the manager-side error instead.
                local.Status = ServiceControllerStatus.Stopped;
                local._exists = true;
            }

            local.AttachDisplaySettings();
            return local;
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
            serviceAsPSObj.Members.Add(new PSMemberSet("PSStandardMembers", defaultPropertySet));
            _displayMemberSet = serviceAsPSObj;
        }
    }
}
#endif
