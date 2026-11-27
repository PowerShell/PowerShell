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
using System.Text;
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
    /// wrapped with <c>sudo -n</c> when they need elevation and the process isn't
    /// running as root.
    /// </para>
    /// </summary>
    public sealed class UnixServiceController : IDisposable
    {
        private const string ServiceSuffix = ".service";

        private const string SudoDiagnosticPrefix = "sudo:";

        /// <summary>The launchd domain of system-wide daemons, which needs elevation.</summary>
        private const string SystemLaunchDomain = "system";

        /// <summary>
        /// How long output is still collected after the manager exited. A daemon started by
        /// an init script can inherit the pipes and keep them open long after the script is done.
        /// </summary>
        private static readonly TimeSpan StreamDrainTimeout = TimeSpan.FromSeconds(1);

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
        private ServiceControllerStatus _status;
        private PSObject? _displayMemberSet;
        private UnixServiceController[]? _dependentServices;
        private UnixServiceController[]? _servicesDependedOn;

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

        /// <summary>Gets or sets the service manager that reported this service.</summary>
        internal ServiceManagerKind ManagerKind { get; set; } = ServiceManagerKind.Systemd;

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
        /// Gets a value indicating whether the manager positively reported the status. A
        /// sysvinit script without a 'status' action only reports that the status is unknown.
        /// </summary>
        internal bool StatusKnown { get; set; } = true;

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

        /// <summary>Gets the services that directly depend on this service (systemd only).</summary>
        public UnixServiceController[] DependentServices
        {
            get
            {
                // Each read would otherwise start a manager command and an enumeration.
                _dependentServices ??= GetRelatedServices(reverse: true);
                return _dependentServices;
            }
        }

        /// <summary>Gets the services this service directly depends on (systemd only).</summary>
        public UnixServiceController[] ServicesDependedOn
        {
            get
            {
                _servicesDependedOn ??= GetRelatedServices(reverse: false);
                return _servicesDependedOn;
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
            return EnumerateServices(ownerToken, out _);
        }

        /// <summary>
        /// Removes the systemd unit suffix from a name pattern, so that '<c>ssh*.service</c>'
        /// matches the same services as '<c>ssh*</c>' and '<c>*.service</c>' as '<c>*</c>'.
        /// Service names don't carry the suffix, and neither do patterns for other managers.
        /// </summary>
        /// <param name="pattern">The name pattern to normalize.</param>
        /// <returns>The pattern to match against service names.</returns>
        internal static string RemoveUnitSuffixFromPattern(string pattern)
        {
            if (pattern.EndsWith(ServiceSuffix, StringComparison.OrdinalIgnoreCase)
                && ResolveManager().Kind == ServiceManagerKind.Systemd)
            {
                return pattern[..^ServiceSuffix.Length];
            }

            return pattern;
        }

        /// <summary>
        /// Loads the descriptions of installed services that were never loaded and therefore
        /// aren't listed with a description. A display-name lookup can't find them otherwise.
        /// </summary>
        /// <param name="services">The enumerated services.</param>
        /// <param name="ownerToken">The invocation token used to correlate the query with the
        /// owning cmdlet run for cancellation.</param>
        internal static void LoadMissingDescriptions(UnixServiceController[] services, Guid? ownerToken)
        {
            List<UnixServiceController> pending = new();
            foreach (UnixServiceController service in services)
            {
                if (service.ManagerKind == ServiceManagerKind.Systemd && service.Description is null)
                {
                    pending.Add(service);
                }
            }

            if (pending.Count == 0)
            {
                return;
            }

            // One query for a batch of units keeps this at a single manager command for the
            // usual case.
            const int batchSize = 50;
            var query = new UnixServiceController { OwnerToken = ownerToken };
            for (int start = 0; start < pending.Count; start += batchSize)
            {
                int count = Math.Min(batchSize, pending.Count - start);
                string[] arguments = new string[count + 3];
                arguments[0] = "show";
                for (int index = 0; index < count; index++)
                {
                    arguments[index + 1] = pending[start + index].Name + ServiceSuffix;
                }

                arguments[arguments.Length - 2] = "--property=Id,Description";
                arguments[arguments.Length - 1] = "--no-pager";

                string output;
                try
                {
                    output = query.RunQueryCommand("systemctl", arguments);
                }
                catch (Win32Exception)
                {
                    // Descriptions are best effort; matching by name still works.
                    return;
                }

                Dictionary<string, string> descriptions = UnixServiceParser.ParseSystemdUnitDescriptions(output);
                for (int index = 0; index < count; index++)
                {
                    UnixServiceController service = pending[start + index];
                    if (descriptions.TryGetValue(service.Name, out string? description) && description.Length > 0)
                    {
                        service.Description = description;
                        service.DisplayName = description;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all services known to the service manager.
        /// </summary>
        /// <remarks>
        /// The managers found on PATH are tried in order of preference. A manager that is
        /// installed but doesn't answer is skipped in favor of the next one: 'systemctl' for
        /// example is present on systems that don't run systemd, such as some WSL
        /// distributions and containers, where the generic 'service' wrapper still works.
        /// When no manager answers, the failure of the preferred one is reported.
        /// </remarks>
        /// <param name="ownerToken">The invocation token used to correlate the enumeration
        /// commands with the owning cmdlet run for cancellation.</param>
        /// <param name="managerKind">The manager that answered.</param>
        /// <returns>All services reported by the service manager, sorted by name.</returns>
        internal static UnixServiceController[] EnumerateServices(Guid? ownerToken, out ServiceManagerKind managerKind)
        {
            // Enumeration queries run on a transient instance carrying the caller's
            // invocation token, so cancellation reaches the manager commands.
            var query = new UnixServiceController { OwnerToken = ownerToken };
            Win32Exception? preferredFailure = null;
            foreach ((string control, string status, ServiceManagerKind kind) in FindAvailableManagers())
            {
                List<UnixServiceController> services;
                try
                {
                    services = query.Enumerate(control, status, kind);
                }
                catch (Win32Exception e)
                {
                    preferredFailure ??= e;
                    continue;
                }

                services.Sort(static (x, y) => string.CompareOrdinal(x.Name, y.Name));
                foreach (UnixServiceController service in services)
                {
                    service.ManagerKind = kind;
                    service.AttachDisplaySettings();
                }

                managerKind = kind;
                return services.ToArray();
            }

            if (preferredFailure is not null)
            {
                // Every installed manager failed; report the preferred one's failure.
                throw preferredFailure;
            }

            throw NewServiceManagerNotFoundException();
        }

        /// <summary>Starts the service.</summary>
        public void Start()
        {
            RunManagerOperation("start");
            Status = ServiceControllerStatus.Running;
            StatusKnown = true;
        }

        /// <summary>Stops the service.</summary>
        public void Stop()
        {
            // Stopping a service that is known to be inactive is a no-op, like the Windows
            // cmdlets, which ignore ERROR_SERVICE_NOT_ACTIVE. This also avoids launchd's
            // 'kill' failing with 'No such process' for a loaded job with no running process.
            // A service whose status the manager can't report is still stopped.
            if (!StatusKnown || Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending or ServiceControllerStatus.StopPending)
            {
                RunManagerOperation("stop");
            }

            Status = ServiceControllerStatus.Stopped;
            StatusKnown = true;
        }

        /// <summary>
        /// Restarts the service through the service manager's native restart operation, which
        /// preserves the manager's dependency semantics (for example systemd's <c>PartOf=</c>).
        /// </summary>
        public void Restart()
        {
            RunManagerOperation("restart");
            Status = ServiceControllerStatus.Running;
            StatusKnown = true;
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
            _dependentServices = null;
            _servicesDependedOn = null;
            if (ManagerKind == ServiceManagerKind.Systemd)
            {
                // A single-unit query is much cheaper than enumerating the whole catalog.
                UnixServiceController? current = QueryUnit();
                if (current is not null)
                {
                    CopyFrom(current);
                    return;
                }
            }

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

        /// <summary>
        /// Creates a controller for this service that belongs to the given cmdlet invocation,
        /// with its state brought up to date from an enumeration snapshot taken by that
        /// invocation.
        /// </summary>
        /// <param name="ownerToken">The token of the consuming cmdlet invocation.</param>
        /// <param name="snapshot">A current enumeration of all services.</param>
        /// <returns>A controller owned by the invocation.</returns>
        internal UnixServiceController CreateInvocationLocal(Guid ownerToken, UnixServiceController[] snapshot)
        {
            // Input objects carry the producer run's owner token; create an invocation-local
            // controller so refresh and mutation run under the consuming run's token and are
            // cancellable by it. Its state comes from the snapshot so that a pipeline over
            // many services doesn't re-enumerate the whole catalog for each of them.
            var local = new UnixServiceController { OwnerToken = ownerToken };
            local.CopyStateFrom(this);
            UnixServiceController? match = MatchService(snapshot, Name);
            if (match is not null)
            {
                local.CopyFrom(match);
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

        private static IEnumerable<(string Control, string Status, ServiceManagerKind Kind)> FindAvailableManagers()
        {
            string[] pathDirectories = GetPathDirectories();
            foreach ((string control, string status, ServiceManagerKind kind) in ManagerCandidates)
            {
                if (FindCommandOnPath(control, pathDirectories) is not null
                    && FindCommandOnPath(status, pathDirectories) is not null)
                {
                    yield return (control, status, kind);
                }
            }
        }

        private static (string Control, string Status, ServiceManagerKind Kind) ResolveManager()
        {
            // The manager is only looked up, not contacted: this is used for naming decisions
            // and ahead of the enumeration that picks the manager that actually answers.
            foreach ((string control, string status, ServiceManagerKind kind) in FindAvailableManagers())
            {
                return (control, status, kind);
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

        private static void ObserveRead(Task? task)
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

        private static async Task ReadIntoAsync(StreamReader reader, StringBuilder destination, CancellationToken cancellationToken)
        {
            // The output is collected incrementally, so whatever arrived is kept even when
            // the rest of the stream is abandoned.
            char[] buffer = new char[4096];
            while (true)
            {
                int read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                destination.Append(buffer, 0, read);
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

            StringBuilder stdout = new();
            StringBuilder stderr = new();
            Task? stdoutTask = null;
            Task? stderrTask = null;
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

                // Drain both streams concurrently: reading them sequentially can deadlock
                // on a full pipe.
                stdoutTask = ReadIntoAsync(process.StandardOutput, stdout, cts.Token);
                stderrTask = ReadIntoAsync(process.StandardError, stderr, cts.Token);
                process.WaitForExitAsync(cts.Token).GetAwaiter().GetResult();

                // The manager exited. A daemon it started can inherit the pipes and keep
                // them open, so the remaining drain is bounded and whatever was received
                // so far is used.
                try
                {
                    if (!Task.WhenAll(stdoutTask, stderrTask).Wait(StreamDrainTimeout))
                    {
                        ObserveRead(stdoutTask);
                        ObserveRead(stderrTask);
                    }
                }
                catch (AggregateException)
                {
                    // A stream failed after the manager exited; its output so far is kept.
                }

                return (stdout.ToString(), stderr.ToString(), process.ExitCode);
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

        private List<UnixServiceController> Enumerate(string control, string status, ServiceManagerKind kind)
        {
            switch (kind)
            {
                case ServiceManagerKind.Systemd:
                    {
                        // --plain keeps failed units from being prefixed with a marker,
                        // so their rows still start with the unit name.
                        string units = RunQueryCommand(control, new[] { "list-units", "--type=service", "--all", "--plain", "--no-legend", "--no-pager" });
                        string unitFiles = RunQueryCommand(control, new[] { "list-unit-files", "--type=service", "--no-legend", "--no-pager" });
                        List<UnixServiceController> services = UnixServiceParser.ParseSystemdListUnits(units);
                        UnixServiceParser.MergeSystemdUnitFiles(services, unitFiles);
                        return services;
                    }

                case ServiceManagerKind.LaunchD:
                    {
                        // 'launchctl list' reports the jobs of the caller's own domain.
                        return UnixServiceParser.ParseLaunchCtlList(RunQueryCommand(status, new[] { "list" }));
                    }

                case ServiceManagerKind.SysVInit:
                    {
                        // Some init scripts write their status to standard error.
                        string output = RunQueryCommand(status, new[] { "--status-all" }, mergeErrorStream: true);
                        return UnixServiceParser.ParseSysVStatusAll(output);
                    }

                case ServiceManagerKind.OpenRC:
                    {
                        List<UnixServiceController> services = UnixServiceParser.ParseOpenRcStatusAll(RunQueryCommand(status, new[] { "--all" }));

                        // 'rc-status' only reports services in a runlevel or started dynamically;
                        // add the installed services that belong to no runlevel as stopped.
                        UnixServiceParser.MergeOpenRcInstalledServices(services, RunQueryCommand(control, new[] { "--list" }));
                        return services;
                    }

                default:
                    return new List<UnixServiceController>();
            }
        }

        private string UnitName() => Name + ServiceSuffix;

        private UnixServiceController? QueryUnit()
        {
            // The properties are requested in 'key=value' form and read by key: systemctl
            // doesn't preserve the order of the requested properties.
            string output;
            try
            {
                output = RunQueryCommand("systemctl", new[] { "show", UnitName(), "--property=LoadState,Id,ActiveState,SubState,Description,UnitFileState", "--no-pager" });
            }
            catch (Win32Exception)
            {
                // The manager doesn't know a unit by that name, or didn't answer.
                return null;
            }

            return UnixServiceParser.CreateFromSystemdUnitProperties(output);
        }

        private static string QueryLaunchDomain()
        {
            // 'launchctl managername' reports the bootstrap domain of the caller, which the
            // jobs of 'launchctl list' belong to. It can't be derived from the UID alone:
            // an SSH session runs in the per-user domain 'user/<uid>', a GUI session in
            // 'gui/<uid>'.
            string uid = UnixServiceNativeMethods.GetUserId().ToString(CultureInfo.InvariantCulture);
            string fallback = UnixServiceNativeMethods.GetEffectiveUserId() == 0
                ? SystemLaunchDomain
                : $"gui/{uid}";
            string managerName;
            try
            {
                managerName = new UnixServiceController().RunQueryCommand("launchctl", new[] { "managername" }).Trim();
            }
            catch (Win32Exception)
            {
                return fallback;
            }

            return managerName.ToUpperInvariant() switch
            {
                "SYSTEM" => SystemLaunchDomain,
                "GUI" => $"gui/{uid}",
                "USER" => $"user/{uid}",
                _ => fallback,
            };
        }

        private static string[] LaunchArguments(string action, string target)
        {
            return action switch
            {
                "start" => new[] { "kickstart", target },
                "stop" => new[] { "kill", "SIGTERM", target },
                _ => new[] { "kickstart", "-k", target },
            };
        }

        private void RunManagerOperation(string action)
        {
            bool isRoot = UnixServiceNativeMethods.GetEffectiveUserId() == 0;
            string control;
            string[] arguments;
            bool needsElevation = !isRoot;
            switch (ManagerKind)
            {
                case ServiceManagerKind.Systemd:
                    control = "systemctl";
                    arguments = new[] { action, UnitName() };
                    break;

                case ServiceManagerKind.LaunchD:
                    {
                        control = "launchctl";
                        string domain = QueryLaunchDomain();

                        // Jobs in the caller's own domain are managed without elevation;
                        // only the system domain needs it.
                        needsElevation = !isRoot && domain == SystemLaunchDomain;
                        arguments = LaunchArguments(action, domain + "/" + Name);
                        break;
                    }

                case ServiceManagerKind.OpenRC:
                    control = "rc-service";
                    arguments = new[] { Name, action };
                    break;

                default:
                    control = "service";
                    arguments = new[] { Name, action };
                    break;
            }

            RunControlCommand(control, arguments, needsElevation);
        }

        private void RunControlCommand(string control, string[] arguments, bool needsElevation)
        {
            string? commandPath = FindCommandOnPath(control, GetPathDirectories());
            if (commandPath is null)
            {
                throw NewServiceManagerNotFoundException();
            }

            ProcessStartInfo startInfo = CreateStartInfo(commandPath, arguments);
            bool usedSudo = false;
            if (needsElevation)
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
            if (!_exists || ManagerKind != ServiceManagerKind.Systemd)
            {
                return Array.Empty<UnixServiceController>();
            }

            // Query the direct dependency properties instead of 'list-dependencies', which
            // recursively expands target units and would pull in transitive dependencies.
            string[] arguments = reverse
                ? new[] { "show", UnitName(), "--property=WantedBy,RequiredBy", "--value", "--no-pager" }
                : new[] { "show", UnitName(), "--property=Wants,Requires", "--value", "--no-pager" };
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
                // Systemd unit names are case-sensitive: exclude only the service itself, and
                // match the native names exactly so that a missing 'foo.service' isn't
                // resolved to a distinct 'FOO.service'.
                if (string.Equals(dependencyName, Name, StringComparison.Ordinal))
                {
                    continue;
                }

                UnixServiceController? match = null;
                foreach (UnixServiceController candidate in services)
                {
                    if (string.Equals(candidate.Name, dependencyName, StringComparison.Ordinal))
                    {
                        match = candidate;
                        break;
                    }
                }

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

            UnixServiceController[] services = EnumerateServices(OwnerToken, out ServiceManagerKind managerKind);
            ManagerKind = managerKind;
            UnixServiceController? match = MatchService(services, Name);
            if (match is null && managerKind == ServiceManagerKind.Systemd)
            {
                // Not listed under this name: an alias unit, or a not-yet-loaded template
                // instance. Ask the manager about the unit itself.
                UnixServiceController? queried = QueryUnit();
                if (queried is not null)
                {
                    // Prefer the catalog entry so that the service looks the same as in
                    // Get-Service; a unit that isn't listed is taken from the query.
                    match = MatchService(services, queried.Name) ?? queried;
                }
            }

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

        private void CopyStateFrom(UnixServiceController source)
        {
            Name = source.Name;
            DisplayName = source.DisplayName;
            _status = source._status;
            StatusKnown = source.StatusKnown;
            StartType = source.StartType;
            Description = source.Description;
            SubState = source.SubState;
            UnitFileState = source.UnitFileState;
            ManagerKind = source.ManagerKind;
            _exists = source._exists;
        }

        private void CopyFrom(UnixServiceController match)
        {
            // Adopt the canonical native name so that subsequent operations target the
            // service that was actually resolved, even when the lookup only matched
            // case-insensitively.
            CopyStateFrom(match);
            _resolved = true;
            _dependentServices = null;
            _servicesDependedOn = null;
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
