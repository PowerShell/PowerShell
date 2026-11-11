// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// This class implements the base for service commands on Unix platforms.
    /// </summary>
    public abstract class ServiceBaseCommand : Cmdlet, IDisposable
    {

        /// <summary>
        /// The service manager commands of the supported init systems, in the order in
        /// which they are preferred.
        /// </summary>
        private static readonly (ServiceManagerKind Kind, string CommandName)[] ManagerCandidates =
        {
            (ServiceManagerKind.Systemd, "systemctl"),
            (ServiceManagerKind.LaunchD, "launchctl"),
            (ServiceManagerKind.SysVInit, "service"),
            (ServiceManagerKind.OpenRC, "rc-service"),
        };

        private ServiceManagerContext? serviceManager;
        private Process? currentProcess;

        /// <summary>
        /// Releases all resources used by the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the resolved service manager, searching the PATH for the service manager
        /// commands of the supported init systems. When no supported service manager is
        /// found, a terminating error is thrown.
        /// </summary>
        /// <returns>The resolved service manager context.</returns>
        internal ServiceManagerContext GetServiceManager()
        {
            if (serviceManager is null)
            {
                ServiceManagerContext? resolved = ResolveServiceManager();
                if (resolved is null)
                {
                    ErrorRecord error = new(
                        new ServiceCommandException(ServiceResources.ServiceManagerNotFound),
                        "NoServiceManagerFound",
                        ErrorCategory.NotImplemented,
                        targetObject: null);
                    ThrowTerminatingError(error);
                }

                serviceManager = resolved!;
            }

            return serviceManager;
        }

        /// <summary>
        /// Gets the command name of the resolved service manager, for use in messages.
        /// </summary>
        /// <returns>The command name of the resolved service manager.</returns>
        internal string GetManagerCommandName()
        {
            return GetServiceManager().Kind switch
            {
                ServiceManagerKind.Systemd => "systemctl",
                ServiceManagerKind.LaunchD => "launchctl",
                ServiceManagerKind.SysVInit => "service",
                ServiceManagerKind.OpenRC => "rc-service",
                _ => "unknown",
            };
        }

        /// <summary>
        /// Enumerates all services known to the service manager.
        /// </summary>
        /// <returns>All services reported by the service manager.</returns>
        internal List<UnixServiceController> EnumerateServices()
        {
            ServiceManagerContext manager = GetServiceManager();
            List<UnixServiceController> services = manager.Kind switch
            {
                ServiceManagerKind.Systemd => EnumerateSystemdServices(),
                ServiceManagerKind.LaunchD => EnumerateLaunchdServices(),
                ServiceManagerKind.SysVInit => EnumerateSysVServices(),
                ServiceManagerKind.OpenRC => EnumerateOpenRcServices(),
                _ => new List<UnixServiceController>(),
            };

            return services;
        }

        /// <summary>
        /// Builds the service manager arguments for the given operation.
        /// </summary>
        /// <param name="action">The operation to perform.</param>
        /// <param name="service">The service to operate on.</param>
        /// <param name="noWait">Do not wait for the operation to complete when the service manager supports it.</param>
        /// <returns>The argument list.</returns>
        internal List<string> GetOperationArguments(ServiceAction action, UnixServiceController service, bool noWait)
        {
            ServiceManagerContext manager = GetServiceManager();
            return manager.Kind switch
            {
                ServiceManagerKind.Systemd => GetSystemdOperationArguments(action, service, noWait),
                ServiceManagerKind.LaunchD => GetLaunchdOperationArguments(action, service, manager),
                _ => GetScriptOperationArguments(action, service),
            };
        }

        /// <summary>
        /// Refreshes the details of a single systemd service that was found only in the
        /// unit file list and therefore has no description or status information, using
        /// a single 'systemctl show' invocation. Failures are ignored so that the
        /// fallback values are used.
        /// </summary>
        /// <param name="service">The service to refresh.</param>
        internal void RefreshServiceDetails(UnixServiceController service)
        {
            if (GetServiceManager().Kind != ServiceManagerKind.Systemd || service.Description is not null)
            {
                return;
            }

            string[] args =
            {
                "show",
                service.Name + ".service",
                "--property=Description,ActiveState,SubState",
                "--no-pager",
            };
            ServiceCommandResult result = RunServiceManagerCommand(args, elevate: false);
            if (result.ExitCode != 0)
            {
                return;
            }

            foreach (string rawLine in result.StandardOutput.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                int separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                {
                    continue;
                }

                string key = line[..separatorIndex];
                string value = line[(separatorIndex + 1)..];
                switch (key)
                {
                    case "Description":
                        if (value.Length > 0)
                        {
                            service.Description = value;
                            service.DisplayName = value;
                        }

                        break;

                    case "ActiveState":
                        service.Status = UnixServiceParser.MapSystemdStatus(value);
                        break;

                    case "SubState":
                        service.SubState = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Builds the service manager arguments to retrieve the dependent or required
        /// services of a service. Returns null when the resolved service manager does
        /// not support this.
        /// </summary>
        /// <param name="service">The service to get the dependencies of.</param>
        /// <param name="dependents">True for services that depend on the service, false for services the service depends on.</param>
        /// <returns>The argument list, or null when not supported.</returns>
        internal List<string>? GetDependencyArguments(UnixServiceController service, bool dependents)
        {
            if (GetServiceManager().Kind != ServiceManagerKind.Systemd)
            {
                return null;
            }

            string unit = service.Name + ".service";
            return dependents
                ? new List<string> { "list-dependencies", "--reverse", "--plain", unit }
                : new List<string> { "list-dependencies", "--plain", unit };
        }

        /// <summary>
        /// Runs the service manager executable with the given arguments. Mutating
        /// operations are wrapped with 'sudo -n' when the current process is not
        /// running as root, since sudo cannot be used to elevate a cmdlet.
        /// </summary>
        /// <param name="arguments">The service manager arguments.</param>
        /// <param name="elevate">True when the operation requires elevated permissions.</param>
        /// <param name="managerExecutable">Optional full path of an alternate service manager executable to invoke (for example 'rc-status' for OpenRC enumeration); when null, the resolved service manager is used.</param>
        /// <returns>The invocation result.</returns>
        internal ServiceCommandResult RunServiceManagerCommand(IList<string> arguments, bool elevate, string? managerExecutable = null)
        {
            ServiceManagerContext manager = GetServiceManager();
            string fileName = managerExecutable ?? manager.ManagerPath;
            var args = new List<string>(arguments);
            var result = new ServiceCommandResult();

            bool usedSudo = false;
            if (elevate && !manager.IsElevated)
            {
                if (manager.SudoPath is not null)
                {
                    // Insert the non-interactive flag first so that 'sudo -n' does not
                    // hang when a password would be required.
                    args.Insert(0, fileName);
                    args.Insert(0, "-n");
                    fileName = manager.SudoPath;
                    usedSudo = true;
                }
                else
                {
                    result.SudoUnavailable = true;
                }
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            foreach (string argument in args)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            currentProcess = process;            try
            {
                process.Start();

                // Read both streams asynchronously to avoid a deadlock when a
                // stream buffer fills up while the process is running.
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                result.ExitCode = process.ExitCode;
                result.StandardOutput = stdoutTask.GetAwaiter().GetResult();
                result.StandardError = stderrTask.GetAwaiter().GetResult();

                if (usedSudo && result.ExitCode != 0 &&
                    result.StandardError.Contains("password is required", StringComparison.OrdinalIgnoreCase))
                {
                    result.SudoPasswordRequired = true;
                }
            }
            finally
            {
                currentProcess = null;
                process.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Confirm that the operation should proceed.
        /// </summary>
        /// <param name="service">Service object to be acted on.</param>
        /// <returns>True if the operation should continue, false otherwise.</returns>
        internal bool ShouldProcessServiceOperation(UnixServiceController service)
        {
            return ShouldProcessServiceOperation(service.DisplayName, service.Name);
        }

        /// <summary>
        /// Confirm that the operation should proceed.
        /// </summary>
        /// <param name="displayName">Display name of the service to be acted on.</param>
        /// <param name="serviceName">Name of the service to be acted on.</param>
        /// <returns>True if the operation should continue, false otherwise.</returns>
        internal bool ShouldProcessServiceOperation(string displayName, string serviceName)
        {
            string name = StringUtil.Format(ServiceResources.ServiceNameForConfirmation, displayName, serviceName);
            return ShouldProcess(name);
        }

        /// <summary>
        /// Writes a non-terminating error following the message format used by the
        /// service cmdlets: {0} is the service name, {1} the display name and {2} the
        /// error detail.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="displayName">Display name of the service.</param>
        /// <param name="targetObject">The target of the error.</param>
        /// <param name="innerException">The inner exception, when present.</param>
        /// <param name="errorId">The error id.</param>
        /// <param name="errorMessage">The message resource string.</param>
        /// <param name="category">The error category.</param>
        internal void WriteNonTerminatingError(
            string serviceName,
            string displayName,
            object? targetObject,
            Exception? innerException,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            string message = StringUtil.Format(
                errorMessage,
                serviceName,
                displayName,
                (innerException == null) ? category.ToString() : innerException.Message);

            var exception = new ServiceCommandException(message, innerException)
            {
                ServiceName = serviceName,
            };

            WriteError(new ErrorRecord(exception, errorId, category, targetObject));
        }

        /// <summary>
        /// Writes a non-terminating error for a failed service manager invocation,
        /// including the standard error output of the command.
        /// </summary>
        /// <param name="service">The service the operation failed for.</param>
        /// <param name="result">The failed invocation result.</param>
        /// <param name="errorId">The error id.</param>
        /// <param name="errorMessage">The message resource string.</param>
        /// <param name="category">The error category.</param>
        internal void WriteOperationFailedError(
            UnixServiceController service,
            ServiceCommandResult result,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            string detail = string.IsNullOrWhiteSpace(result.StandardError)
                ? StringUtil.Format(ServiceResources.ExitCodeWithoutStderr, result.ExitCode)
                : result.StandardError.Trim();

            string message = StringUtil.Format(errorMessage, service.Name, service.DisplayName, detail);
            var exception = new ServiceCommandException(message)
            {
                ServiceName = service.Name,
            };

            WriteError(new ErrorRecord(exception, errorId, category, service));
        }

        /// <summary>
        /// Writes a non-terminating error explaining how to run the operation with
        /// sufficient permissions when sudo reported that a password would be required.
        /// </summary>
        /// <param name="service">The service the operation failed for.</param>
        internal void WriteSudoRequiredError(UnixServiceController service)
        {
            string message = StringUtil.Format(ServiceResources.SudoRequired, service.Name, service.DisplayName);
            var exception = new ServiceCommandException(message)
            {
                ServiceName = service.Name,
            };

            WriteError(new ErrorRecord(exception, "SudoRequired", ErrorCategory.PermissionDenied, service));
        }

        /// <summary>
        /// Terminate the service manager process when the pipeline is stopped,
        /// for example by CTRL+C.
        /// </summary>
        protected override void StopProcessing()
        {
            try
            {
                if (currentProcess is not null && !currentProcess.HasExited)
                {
                    currentProcess.Kill(entireProcessTree: true);
                }
            }
            catch (Exception e) when (e is InvalidOperationException || e is NotSupportedException)
            {
                // The process has already exited or cannot be killed; there is
                // nothing left to do when the pipeline is being stopped.
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the cmdlet and optionally releases
        /// the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentProcess?.Dispose();
                currentProcess = null;
            }
        }

        /// <summary>
        /// Builds the systemctl arguments for the given operation.
        /// </summary>
        /// <param name="action">The operation to perform.</param>
        /// <param name="service">The service to operate on.</param>
        /// <param name="noWait">Prepend '--no-block' so that systemctl returns before the operation completes.</param>
        /// <returns>The argument list.</returns>
        private static List<string> GetSystemdOperationArguments(ServiceAction action, UnixServiceController service, bool noWait)
        {
            var args = new List<string>();
            if (noWait)
            {
                args.Add("--no-block");
            }

            args.Add(action switch
            {
                ServiceAction.Start => "start",
                ServiceAction.Stop => "stop",
                ServiceAction.Restart => "restart",
                _ => throw PSTraceSource.NewArgumentException(nameof(action)),
            });
            args.Add(service.Name + ".service");
            return args;
        }

        /// <summary>
        /// Builds the launchctl arguments for the given operation, targeting the
        /// launchd domain of the current user ('system' for root, or the 'gui/{uid}' domain otherwise).
        /// </summary>
        /// <param name="action">The operation to perform.</param>
        /// <param name="service">The service to operate on.</param>
        /// <param name="manager">The resolved launchd manager.</param>
        /// <returns>The argument list.</returns>
        private static List<string> GetLaunchdOperationArguments(ServiceAction action, UnixServiceController service, ServiceManagerContext manager)
        {
            string domain = manager.IsElevated
                ? "system"
                : $"gui/{UnixServiceNativeMethods.GetUserId()}";
            string unitPath = domain + "/" + service.Name;
            return action switch
            {
                ServiceAction.Start => new List<string> { "kickstart", unitPath },
                ServiceAction.Stop => new List<string> { "kill", "SIGTERM", unitPath },
                ServiceAction.Restart => new List<string> { "kickstart", "-k", unitPath },
                _ => throw PSTraceSource.NewArgumentException(nameof(action)),
            };
        }

        /// <summary>
        /// Builds the arguments for sysvinit and OpenRC, which both take the service
        /// name before the action verb, e.g. 'service sshd restart' and
        /// 'rc-service sshd restart'.
        /// </summary>
        /// <param name="action">The operation to perform.</param>
        /// <param name="service">The service to operate on.</param>
        /// <returns>The argument list.</returns>
        private static List<string> GetScriptOperationArguments(ServiceAction action, UnixServiceController service)
        {
            string actionName = action switch
            {
                ServiceAction.Start => "start",
                ServiceAction.Stop => "stop",
                ServiceAction.Restart => "restart",
                _ => throw PSTraceSource.NewArgumentException(nameof(action)),
            };
            return new List<string> { service.Name, actionName };
        }

        /// <summary>
        /// Enumerates services through 'systemctl list-units' and 'systemctl list-unit-files'.
        /// </summary>
        /// <returns>All services reported by systemctl.</returns>
        private List<UnixServiceController> EnumerateSystemdServices()
        {
            ServiceCommandResult units = RunServiceManagerCommand(
                new[] { "list-units", "--type=service", "--all", "--no-legend", "--no-pager" },
                elevate: false);
            ThrowIfEnumerationFailed(units);

            ServiceCommandResult unitFiles = RunServiceManagerCommand(
                new[] { "list-unit-files", "--type=service", "--no-legend", "--no-pager" },
                elevate: false);
            ThrowIfEnumerationFailed(unitFiles);

            List<UnixServiceController> services = UnixServiceParser.ParseSystemdListUnits(units.StandardOutput);
            UnixServiceParser.MergeSystemdUnitFiles(services, unitFiles.StandardOutput);
            return services;
        }

        /// <summary>
        /// Enumerates services through 'launchctl list'.
        /// </summary>
        /// <returns>All services reported by launchctl.</returns>
        private List<UnixServiceController> EnumerateLaunchdServices()
        {
            ServiceCommandResult result = RunServiceManagerCommand(new[] { "list" }, elevate: false);
            ThrowIfEnumerationFailed(result);
            return UnixServiceParser.ParseLaunchCtlList(result.StandardOutput);
        }

        /// <summary>
        /// Enumerates services through 'service --status-all'.
        /// </summary>
        /// <returns>All services reported by the service wrapper.</returns>
        private List<UnixServiceController> EnumerateSysVServices()
        {
            ServiceCommandResult result = RunServiceManagerCommand(new[] { "--status-all" }, elevate: false);
            ThrowIfEnumerationFailed(result);

            // Some init scripts write their status to standard error.
            return UnixServiceParser.ParseSysVStatusAll(result.StandardOutput + result.StandardError);
        }

        /// <summary>
        /// Enumerates services through 'rc-status --all'. The status listing lives in
        /// 'rc-status'; 'rc-service' only manages a single service at a time.
        /// </summary>
        /// <returns>All services reported by rc-status.</returns>
        private List<UnixServiceController> EnumerateOpenRcServices()
        {
            // When 'rc-status' cannot be found, the enumeration is left to fail through
            // the service manager itself so that the resulting error names the actual
            // command that ran.
            string? rcStatusPath = ResolveApplicationPath("rc-status");
            ServiceCommandResult result = RunServiceManagerCommand(new[] { "--all" }, elevate: false, managerExecutable: rcStatusPath);
            ThrowIfEnumerationFailed(result);
            return UnixServiceParser.ParseOpenRcStatusAll(result.StandardOutput);
        }

        private ServiceManagerContext? ResolveServiceManager()
        {
            foreach ((ServiceManagerKind kind, string commandName) in ManagerCandidates)
            {
                CommandInfo? commandInfo;
                try
                {
                    commandInfo = CommandDiscovery.LookupCommandInfo(
                        commandName,
                        CommandTypes.Application,
                        SearchResolutionOptions.None,
                        CommandOrigin.Internal,
                        Context);
                }
                catch (CommandNotFoundException)
                {
                    continue;
                }

                if (commandInfo is null)
                {
                    continue;
                }

                return new ServiceManagerContext
                {
                    Kind = kind,
                    ManagerPath = commandInfo.Definition,
                    SudoPath = ResolveApplicationPath("sudo"),
                };
            }

            return null;
        }

        private string? ResolveApplicationPath(string commandName)
        {
            try
            {
                CommandInfo? commandInfo = CommandDiscovery.LookupCommandInfo(
                    commandName,
                    CommandTypes.Application,
                    SearchResolutionOptions.None,
                    CommandOrigin.Internal,
                    Context);
                return commandInfo?.Definition;
            }
            catch (CommandNotFoundException)
            {
                return null;
            }
        }

        private void ThrowIfEnumerationFailed(ServiceCommandResult result)
        {
            if (result.ExitCode == 0)
            {
                return;
            }

            string detail = string.IsNullOrWhiteSpace(result.StandardError)
                ? StringUtil.Format(ServiceResources.ExitCodeWithoutStderr, result.ExitCode)
                : result.StandardError.Trim();

            string message = StringUtil.Format(
                ServiceResources.ServiceEnumerationFailed,
                GetServiceManager().ManagerPath,
                detail);

            ErrorRecord error = new(
                new ServiceCommandException(message),
                "ServiceEnumerationFailed",
                ErrorCategory.OperationStopped,
                targetObject: null);
            ThrowTerminatingError(error);
        }

    }
}

#endif
