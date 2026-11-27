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

        private const string SudoDiagnosticPrefix = "sudo:";

        private static readonly (string ControlCommand, string StatusCommand, ServiceManagerKind Kind)[] ManagerCandidates =
        {
            ("systemctl", "systemctl", ServiceManagerKind.Systemd),
            ("launchctl", "launchctl", ServiceManagerKind.LaunchD),
            ("service", "service", ServiceManagerKind.SysVInit),
            ("rc-service", "rc-status", ServiceManagerKind.OpenRC),
        };

        private static readonly ConcurrentDictionary<Guid, Process> s_activeProcesses = new();
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> s_cancellationSources = new();
        private static readonly ConcurrentDictionary<Guid, byte> s_liveInvocations = new();

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
            : this(serviceName, ownerToken: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnixServiceController"/> class
        /// for the specified service on behalf of a running cmdlet invocation.
        /// </summary>
        /// <param name="serviceName">The service name, with or without the '.service' suffix.</param>
        /// <param name="ownerToken">The invocation token used to correlate commands launched
        /// during resolution with the owning cmdlet run for cancellation.</param>
        internal UnixServiceController(string serviceName, Guid? ownerToken)
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
        /// Tokens of invocations that are no longer running are ignored, so work performed
        /// through a controller that outlived its cmdlet is treated as transient.
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
        /// Gets the services this service depends on. Windows exposes this as an alias of
        /// <see cref="ServicesDependedOn"/> through type data, which doesn't apply to this type.
        /// </summary>
        public UnixServiceController[] RequiredServices => ServicesDependedOn;

        /// <summary>
        /// Enumerates all services known to the service manager.
        /// </summary>
        /// <returns>All services reported by the service manager, sorted by name.</returns>
        public static UnixServiceController[] GetServices()
        {
            return GetServices(ownerToken: null);
        }

        /// <summary>
        /// Enumerates all services known to the service manager on behalf of a running cmdlet
        /// invocation.
        /// </summary>
        /// <param name="ownerToken">The invocation token used to correlate the enumeration
        /// commands with the owning cmdlet run for cancellation.</param>
        /// <returns>All services reported by the service manager, sorted by name.</returns>
        internal static UnixServiceController[] GetServices(Guid? ownerToken)
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

                        // 'rc-status' only reports services in a runlevel or started dynamically;
                        // add the installed services that belong to no runlevel as stopped.
                        UnixServiceParser.MergeOpenRcInstalledServices(services, query.RunQueryCommand("rc-service", new[] { "--list" }));
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
            // Stopping a service that isn't running is a no-op, like the Windows cmdlets, which
            // ignore ERROR_SERVICE_NOT_ACTIVE. This also avoids launchd's 'kill' failing with
            // 'No such process' for a loaded job that has no running process.
            if (Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending or ServiceControllerStatus.StopPending)
            {
                RunManagerOperation("stop");
            }

            Status = ServiceControllerStatus.Stopped;
        }

        /// <summary>
        /// Restarts the service through the service manager's native restart operation, which
        /// preserves the manager's dependency semantics (for example systemd's <c>PartOf=</c>).
        /// </summary>
        public void Restart()
        {
            RunManagerOperation("restart");
            Status = ServiceControllerStatus.Running;
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

        /// <summary>
        /// Registers a starting cmdlet invocation. Controllers created for it correlate their
        /// service manager commands with the invocation token until <see cref="EndInvocation"/>.
        /// </summary>
        /// <param name="ownerToken">The invocation token to register.</param>
        internal static void BeginInvocation(Guid ownerToken)
        {
            s_liveInvocations[ownerToken] = 0;
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
                TryKillProcess(process);
            }
        }

        /// <summary>
        /// Releases the state of a finished cmdlet invocation. Called through cmdlet disposal,
        /// which the engine guarantees even after terminating errors and cancellations. Work
        /// performed afterwards through controllers that outlived the invocation is transient.
        /// </summary>
        /// <param name="ownerToken">The invocation token to clean up.</param>
        internal static void EndInvocation(Guid ownerToken)
        {
            s_liveInvocations.TryRemove(ownerToken, out _);
            if (s_cancellationSources.TryRemove(ownerToken, out CancellationTokenSource? cts))
            {
                cts.Dispose();
            }
        }

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

        private static void TryKillProcess(Process process)
        {
            try
            {
                // Kill the whole tree so the sudo wrapper and init scripts are terminated too.
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process has not started or already exited.
            }
            catch (Win32Exception)
            {
                // The process could not be killed, for example because it already exited.
            }
            catch (AggregateException)
            {
                // The process tree was only partially terminated, for example because an
                // elevated descendant survived. The waits are cancelled independently of it.
            }
        }

        private static void ObserveRead(Task<string>? task)
        {
            if (task is null || task.IsCompleted)
            {
                return;
            }

            // Never block on a read that may still be waiting for a pipe the process tree
            // left open; just make sure a late failure doesn't surface as unobserved.
            _ = task.ContinueWith(
                static completed => _ = completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
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

        private static bool HasSudoDiagnostic(string stderr)
        {
            // sudo reports its own failures on stderr with a 'sudo:' prefix, for example when
            // a password is required or the user isn't allowed to run the command. The prefix
            // is the program name, so it's stable across locales; everything else is the
            // wrapped command's own output.
            foreach (string rawLine in stderr.Split('\n'))
            {
                if (rawLine.TrimStart().StartsWith(SudoDiagnosticPrefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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

        private (string Stdout, string Stderr, int ExitCode) Execute(ProcessStartInfo startInfo)
        {
            // Only a live cmdlet invocation owns persistent cancellation state. Controllers
            // that outlived their invocation (for example saved Get-Service output) run as
            // transient work with an isolated key that is released right after the command.
            bool isLiveInvocation = OwnerToken.HasValue && s_liveInvocations.ContainsKey(OwnerToken.Value);
            Guid executionKey = isLiveInvocation ? OwnerToken!.Value : Guid.NewGuid();
            CancellationTokenSource cts = s_cancellationSources.GetOrAdd(executionKey, static _ => new CancellationTokenSource());
            if (cts.IsCancellationRequested)
            {
                // The pipeline was stopped before this command could launch.
                throw new OperationCanceledException(cts.Token);
            }

            Task<string>? stdoutTask = null;
            Task<string>? stderrTask = null;
            using Process process = new() { StartInfo = startInfo };
            s_activeProcesses[executionKey] = process;
            try
            {
                process.Start();

                // Best-effort termination on cancellation. Elevated descendants may survive
                // the kill, so the waits below are cancelled independently of it.
                using CancellationTokenRegistration registration = cts.Token.Register(
                    static state => TryKillProcess((Process)state!),
                    process);

                // Drain both streams concurrently with cancellation: reading them
                // sequentially can deadlock on a full pipe, and a background child that
                // inherits the pipes must not keep the pipeline blocked after cancellation.
                stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
                stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
                process.WaitForExitAsync(cts.Token).GetAwaiter().GetResult();

                return (stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult(), process.ExitCode);
            }
            catch (OperationCanceledException)
            {
                ObserveRead(stdoutTask);
                ObserveRead(stderrTask);
                throw;
            }
            finally
            {
                s_activeProcesses.TryRemove(executionKey, out _);
                if (!isLiveInvocation && s_cancellationSources.TryRemove(executionKey, out CancellationTokenSource? transientSource))
                {
                    transientSource.Dispose();
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
            bool usedSudo = false;
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
                usedSudo = true;
            }

            (_, string stderr, int exitCode) = Execute(startInfo);
            if (exitCode != 0)
            {
                // Only sudo's own diagnostics mean that elevation was unavailable. The
                // wrapped manager's stderr is reported as the manager failure it is, even
                // when it happens to mention a password (for example 'password-store.service').
                if (usedSudo && HasSudoDiagnostic(stderr))
                {
                    throw NewSudoRequiredException();
                }

                string detail = stderr.TrimEnd();
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

            // Query the direct dependency properties instead of 'list-dependencies', which
            // recursively expands target units and would pull in transitive dependencies.
            string[] arguments = reverse
                ? new[] { "show", UnitName(), "--property=WantedBy,RequiredBy", "--value" }
                : new[] { "show", UnitName(), "--property=Wants,Requires", "--value" };
            string output = RunQueryCommand("systemctl", arguments);
            List<string> dependencyNames = UnixServiceParser.ParseSystemdDependencyUnits(output);
            if (dependencyNames.Count == 0)
            {
                return Array.Empty<UnixServiceController>();
            }

            // Resolve the dependencies against a single enumeration snapshot instead of
            // re-enumerating the whole catalog for every dependency. The invocation token is
            // carried along so the enumeration stays cancellable.
            UnixServiceController[] services = GetServices(OwnerToken);
            List<UnixServiceController> result = new();
            foreach (string dependencyName in dependencyNames)
            {
                // Systemd unit names are case-sensitive, so only the service itself is
                // excluded; a distinct service that differs in case is a real dependency.
                if (string.Equals(dependencyName, Name, StringComparison.Ordinal))
                {
                    continue;
                }

                UnixServiceController? match = MatchService(services, dependencyName);
                if (match is null)
                {
                    // The dependency isn't reported by the enumeration, for example a
                    // 'not-found' unit. Skip it instead of emitting a controller whose
                    // status can't be read.
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

            UnixServiceController[] services = GetServices(OwnerToken);
            UnixServiceController? match = MatchService(services, Name);
            if (match is null)
            {
                // An alias unit (for example sshd.service for ssh.service) isn't listed
                // under its own name, so ask the manager which unit the name refers to.
                string? canonicalName = ResolveCanonicalUnitName();
                if (canonicalName is not null)
                {
                    match = MatchService(services, canonicalName);
                }
            }

            if (match is not null)
            {
                CopyFrom(match);
            }

            AttachDisplaySettings();
        }

        private string? ResolveCanonicalUnitName()
        {
            if (ResolveManager().Kind != ServiceManagerKind.Systemd)
            {
                return null;
            }

            string output;
            try
            {
                output = RunQueryCommand("systemctl", new[] { "show", UnitName(), "--property=LoadState,Id", "--value" });
            }
            catch (Win32Exception)
            {
                // The manager doesn't know a unit by that name at all.
                return null;
            }

            // One value per property, in the requested order.
            string[] values = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length < 2 || !string.Equals(values[0].Trim(), "loaded", StringComparison.Ordinal))
            {
                return null;
            }

            string unit = values[1].Trim();
            return unit.EndsWith(ServiceSuffix, StringComparison.Ordinal)
                ? UnixServiceParser.RemoveServiceSuffix(unit)
                : null;
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
