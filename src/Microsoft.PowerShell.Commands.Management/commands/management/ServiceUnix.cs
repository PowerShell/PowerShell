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
    #region UnixServiceController

    /// <summary>
    /// Represents a service managed by the operating system service manager on Unix platforms,
    /// such as systemd (Linux), launchd (macOS), sysvinit or OpenRC.
    /// This is the object type returned by the service cmdlets when they run on Unix.
    /// </summary>
    public sealed class UnixServiceController
    {
        /// <summary>
        /// Gets the name of the service.
        /// For systemd, this is the unit name without the '.service' suffix.
        /// </summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the display name of the service.
        /// For systemd, this is the unit description when available, otherwise the service name.
        /// </summary>
        public string DisplayName { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the status of the service, mapped onto the <see cref="ServiceControllerStatus"/>
        /// values also used by <see cref="System.ServiceProcess.ServiceController"/> on Windows.
        /// </summary>
        public ServiceControllerStatus Status { get; internal set; }

        /// <summary>
        /// Gets the start type of the service, mapped onto the <see cref="ServiceStartMode"/>
        /// values also used on Windows. When the service manager does not provide this
        /// information, <see cref="ServiceStartMode.Manual"/> is reported.
        /// </summary>
        public ServiceStartMode StartType { get; internal set; }

        /// <summary>
        /// Gets the description of the service when the service manager provides one.
        /// </summary>
        public string? Description { get; internal set; }

        /// <summary>
        /// Gets the sub state of a systemd service (for example 'running', 'dead' or 'failed')
        /// when the service manager provides one.
        /// </summary>
        public string? SubState { get; internal set; }

        /// <summary>
        /// Gets the unit file state of a systemd service (for example 'enabled' or 'disabled')
        /// when the service manager provides one.
        /// </summary>
        public string? UnitFileState { get; internal set; }

        /// <summary>
        /// Returns the name of the service.
        /// </summary>
        public override string ToString() => Name;
    }

    #endregion UnixServiceController

    #region ServiceCommandException

    /// <summary>
    /// Non-terminating errors occurring in the service noun commands on Unix platforms.
    /// </summary>
    public class ServiceCommandException : SystemException
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        public ServiceCommandException() : base()
        {
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServiceCommandException(string message) : base(message)
        {
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceCommandException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets or sets the name of the service which could not be found or operated upon.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
    }

    #endregion ServiceCommandException

    #region ServiceManagerSupport

    /// <summary>
    /// The Unix service managers supported by the service cmdlets.
    /// </summary>
    internal enum ServiceManagerKind
    {
        /// <summary>
        /// No supported service manager was found.
        /// </summary>
        None = 0,

        /// <summary>
        /// systemd, managed through 'systemctl'.
        /// </summary>
        Systemd,

        /// <summary>
        /// launchd, managed through 'launchctl'.
        /// </summary>
        LaunchD,

        /// <summary>
        /// sysvinit or a compatible init system, managed through the generic 'service' wrapper.
        /// </summary>
        SysVInit,

        /// <summary>
        /// OpenRC, managed through 'rc-service'.
        /// </summary>
        OpenRC,
    }

    /// <summary>
    /// Native methods used by the Unix service cmdlets.
    /// </summary>
    internal static class UnixServiceNativeMethods
    {
        [DllImport("libc",
            EntryPoint = "geteuid",
            CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        private static extern uint geteuid();

        [DllImport("libc",
            EntryPoint = "getuid",
            CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        private static extern uint getuid();

        /// <summary>
        /// Gets the effective user id of the current process.
        /// </summary>
        internal static uint GetEffectiveUserId() => geteuid();

        /// <summary>
        /// Gets the real user id of the current process.
        /// </summary>
        internal static uint GetUserId() => getuid();
    }

    /// <summary>
    /// Holds the resolved service manager used by the service cmdlets on Unix.
    /// </summary>
    internal sealed class ServiceManagerContext
    {
        /// <summary>
        /// Gets or sets the kind of the service manager.
        /// </summary>
        internal ServiceManagerKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the full path of the service manager executable.
        /// </summary>
        internal string ManagerPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path of sudo when it is available, otherwise null.
        /// </summary>
        internal string? SudoPath { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current process is running as root.
        /// </summary>
        internal bool IsElevated => UnixServiceNativeMethods.GetEffectiveUserId() == 0;
    }

    /// <summary>
    /// The outcome of a service manager invocation.
    /// </summary>
    internal sealed class ServiceCommandResult
    {
        /// <summary>
        /// Gets or sets the exit code of the service manager invocation.
        /// </summary>
        internal int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output of the service manager invocation.
        /// </summary>
        internal string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the standard error of the service manager invocation.
        /// </summary>
        internal string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the invocation was wrapped with
        /// 'sudo -n' and sudo reported that a password would be required.
        /// </summary>
        internal bool SudoPasswordRequired { get; set; }
    }

    /// <summary>
    /// Operations supported by <see cref="ServiceBaseCommand.GetOperationArguments"/>.
    /// </summary>
    internal enum ServiceAction
    {
        /// <summary>
        /// Start the service.
        /// </summary>
        Start,

        /// <summary>
        /// Stop the service.
        /// </summary>
        Stop,

        /// <summary>
        /// Restart the service.
        /// </summary>
        Restart,
    }

    /// <summary>
    /// Parses the output of the supported service managers into
    /// <see cref="UnixServiceController"/> objects.
    /// </summary>
    internal static class UnixServiceParser
    {
        private const string ServiceSuffix = ".service";

        /// <summary>
        /// Parses the output of 'systemctl list-units --type=service --all'.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The parsed services.</returns>
        internal static List<UnixServiceController> ParseSystemdListUnits(string output)
        {
            var services = new List<UnixServiceController>();

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                // Columns: UNIT LOAD ACTIVE SUB DESCRIPTION
                string[] tokens = line.Split((char[]?)null, 5, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4)
                {
                    continue;
                }

                string unit = tokens[0];
                if (!unit.EndsWith(ServiceSuffix, StringComparison.Ordinal) ||
                    unit.EndsWith("@.service", StringComparison.Ordinal))
                {
                    // Only real '.service' units are of interest here and
                    // template units ('foo@.service') are not services themselves.
                    continue;
                }

                string loadState = tokens[1];
                if (loadState == "not-found")
                {
                    continue;
                }

                string activeState = tokens[2];
                string subState = tokens[3];
                string description = tokens.Length > 4 ? tokens[4].Trim() : string.Empty;
                string name = RemoveServiceSuffix(unit);

                services.Add(new UnixServiceController
                {
                    Name = name,
                    DisplayName = description.Length > 0 ? description : name,
                    Description = description.Length > 0 ? description : null,
                    Status = MapSystemdStatus(activeState),
                    SubState = subState,
                    StartType = ServiceStartMode.Manual,
                });
            }

            return services;
        }

        /// <summary>
        /// Merges the output of 'systemctl list-unit-files --type=service' into the given
        /// services, adding start type information and services that were never loaded.
        /// </summary>
        /// <param name="services">The services parsed so far.</param>
        /// <param name="output">The standard output of the command.</param>
        internal static void MergeSystemdUnitFiles(List<UnixServiceController> services, string output)
        {
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.OrdinalIgnoreCase);
            foreach (UnixServiceController service in services)
            {
                servicesByName[service.Name] = service;
            }

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                // Columns: UNIT FILE STATE [VENDOR PRESET]
                string[] tokens = line.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                {
                    continue;
                }

                string unit = tokens[0];
                if (!unit.EndsWith(ServiceSuffix, StringComparison.Ordinal) ||
                    unit.EndsWith("@.service", StringComparison.Ordinal))
                {
                    continue;
                }

                string state = tokens[1];
                string name = RemoveServiceSuffix(unit);
                if (servicesByName.TryGetValue(name, out UnixServiceController? service))
                {
                    service.UnitFileState = state;
                    service.StartType = MapSystemdUnitFileState(state);
                }
                else
                {
                    // The service exists but was never loaded (for example it is disabled).
                    var newService = new UnixServiceController
                    {
                        Name = name,
                        DisplayName = name,
                        Status = ServiceControllerStatus.Stopped,
                        StartType = MapSystemdUnitFileState(state),
                        UnitFileState = state,
                    };
                    servicesByName[name] = newService;
                    services.Add(newService);
                }
            }
        }

        /// <summary>
        /// Parses the output of 'launchctl list'.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The parsed services.</returns>
        internal static List<UnixServiceController> ParseLaunchCtlList(string output)
        {
            var services = new List<UnixServiceController>();

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                // Columns: PID Status Label. The PID is '-' when the label is not running.
                string[] tokens = line.Split('\t');
                if (tokens.Length < 3)
                {
                    tokens = line.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
                }

                if (tokens.Length < 3 || tokens[0] == "PID")
                {
                    continue;
                }

                bool isRunning = tokens[0] != "-";
                services.Add(new UnixServiceController
                {
                    Name = tokens[2],
                    DisplayName = tokens[2],
                    Status = isRunning ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped,
                    StartType = ServiceStartMode.Manual,
                });
            }

            return services;
        }

        /// <summary>
        /// Parses the output of 'service --status-all'.
        /// </summary>
        /// <param name="output">The combined standard output and standard error of the command.</param>
        /// <returns>The parsed services.</returns>
        internal static List<UnixServiceController> ParseSysVStatusAll(string output)
        {
            var services = new List<UnixServiceController>();

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || !line.StartsWith('['))
                {
                    continue;
                }

                int closeIndex = line.IndexOf(']');
                if (closeIndex < 0)
                {
                    continue;
                }

                // Format: '[ + ] name [ (description) ]'. '+' marks a running service.
                string state = line[1..closeIndex].Trim();
                string rest = line[(closeIndex + 1)..].Trim();
                if (rest.Length == 0)
                {
                    continue;
                }

                int spaceIndex = rest.IndexOf(' ');
                string name;
                string? description = null;
                if (spaceIndex < 0)
                {
                    name = rest;
                }
                else
                {
                    name = rest[..spaceIndex];
                    description = rest[(spaceIndex + 1)..].Trim();
                    if (description.Length == 0)
                    {
                        description = null;
                    }
                }

                services.Add(new UnixServiceController
                {
                    Name = name,
                    DisplayName = description ?? name,
                    Description = description,
                    Status = state == "+" ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped,
                    StartType = ServiceStartMode.Manual,
                });
            }

            return services;
        }

        /// <summary>
        /// Parses the output of 'rc-status --all'.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The parsed services.</returns>
        internal static List<UnixServiceController> ParseOpenRcStatusAll(string output)
        {
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.Trim();
                int openIndex = line.IndexOf('[');
                if (openIndex < 0)
                {
                    continue;
                }

                int closeIndex = line.IndexOf(']', openIndex);
                if (closeIndex < 0)
                {
                    continue;
                }

                // Format: ' * name [ state ]'. Runlevel headers do not contain brackets.
                string state = line[(openIndex + 1)..closeIndex].Trim();
                string before = line[..openIndex].Trim();
                string? name = null;
                foreach (string token in before.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token != "*")
                    {
                        name = token;
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                ServiceControllerStatus status = MapOpenRcStatus(state);
                if (servicesByName.TryGetValue(name, out UnixServiceController? service))
                {
                    // Services may be listed under multiple runlevels; prefer a running entry.
                    if (status == ServiceControllerStatus.Running)
                    {
                        service.Status = status;
                    }
                }
                else
                {
                    servicesByName[name] = new UnixServiceController
                    {
                        Name = name,
                        DisplayName = name,
                        Status = status,
                        StartType = ServiceStartMode.Manual,
                    };
                }
            }

            return new List<UnixServiceController>(servicesByName.Values);
        }

        /// <summary>
        /// Parses the output of 'systemctl list-dependencies --plain', returning service names.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The names of the listed services, without the '.service' suffix.</returns>
        internal static List<string> ParseSystemdDependencyUnits(string output)
        {
            var names = new List<string>();

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r').Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                string unit = tokens[0];
                if (unit.EndsWith(ServiceSuffix, StringComparison.Ordinal) &&
                    !unit.EndsWith("@.service", StringComparison.Ordinal))
                {
                    names.Add(RemoveServiceSuffix(unit));
                }
            }

            return names;
        }

        /// <summary>
        /// Maps a systemd active state onto a <see cref="ServiceControllerStatus"/>.
        /// </summary>
        /// <param name="activeState">The ActiveState reported by systemctl.</param>
        /// <returns>The mapped status.</returns>
        internal static ServiceControllerStatus MapSystemdStatus(string activeState)
        {
            return activeState switch
            {
                "active" => ServiceControllerStatus.Running,
                "activating" => ServiceControllerStatus.StartPending,
                "deactivating" => ServiceControllerStatus.StopPending,
                _ => ServiceControllerStatus.Stopped, // inactive, failed, unknown
            };
        }

        /// <summary>
        /// Maps a systemd unit file state onto a <see cref="ServiceStartMode"/>.
        /// </summary>
        /// <param name="unitFileState">The UnitFileState reported by systemctl.</param>
        /// <returns>The mapped start type.</returns>
        internal static ServiceStartMode MapSystemdUnitFileState(string unitFileState)
        {
            return unitFileState switch
            {
                "enabled" or "enabled-runtime" or "generated" or "generated-runtime" or "transient" => ServiceStartMode.Automatic,
                "disabled" or "masked" => ServiceStartMode.Disabled,
                _ => ServiceStartMode.Manual, // static, linked, indirect, alias, ...
            };
        }

        /// <summary>
        /// Maps an OpenRC state onto a <see cref="ServiceControllerStatus"/>.
        /// </summary>
        /// <param name="state">The state reported by rc-status.</param>
        /// <returns>The mapped status.</returns>
        internal static ServiceControllerStatus MapOpenRcStatus(string state)
        {
            return state switch
            {
                "started" => ServiceControllerStatus.Running,
                "starting" => ServiceControllerStatus.StartPending,
                "stopping" => ServiceControllerStatus.StopPending,
                _ => ServiceControllerStatus.Stopped, // stopped, crashed, inactive, scheduled, gone
            };
        }

        /// <summary>
        /// Removes the '.service' suffix from a unit name.
        /// </summary>
        /// <param name="name">The unit name.</param>
        /// <returns>The name without the '.service' suffix.</returns>
        internal static string RemoveServiceSuffix(string name)
        {
            return name.EndsWith(ServiceSuffix, StringComparison.OrdinalIgnoreCase)
                ? name[..^ServiceSuffix.Length]
                : name;
        }
    }

    #endregion ServiceManagerSupport

    #region ServiceBaseCommand

    /// <summary>
    /// This class implements the base for service commands on Unix platforms.
    /// </summary>
    public abstract class ServiceBaseCommand : Cmdlet, IDisposable
    {
        #region Private Members

        private ServiceManagerContext? _manager;
        private Process? _currentProcess;

        private static readonly (ServiceManagerKind Kind, string CommandName)[] s_managerCandidates =
        {
            (ServiceManagerKind.Systemd, "systemctl"),
            (ServiceManagerKind.LaunchD, "launchctl"),
            (ServiceManagerKind.SysVInit, "service"),
            (ServiceManagerKind.OpenRC, "rc-service"),
        };

        #endregion Private Members

        #region Internal Members

        /// <summary>
        /// Gets the resolved service manager, searching the PATH for the service manager
        /// commands of the supported init systems. When no supported service manager is
        /// found, a terminating error is thrown.
        /// </summary>
        internal ServiceManagerContext GetServiceManager()
        {
            if (_manager is null)
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

                _manager = resolved!;
            }

            return _manager;
        }

        /// <summary>
        /// Gets the command name of the resolved service manager, for use in messages.
        /// </summary>
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
            List<UnixServiceController> services;

            switch (manager.Kind)
            {
                case ServiceManagerKind.Systemd:
                    {
                        ServiceCommandResult units = RunServiceManagerCommand(
                            new[] { "list-units", "--type=service", "--all", "--no-legend", "--no-pager" },
                            elevate: false);
                        ThrowIfEnumerationFailed(units);

                        ServiceCommandResult unitFiles = RunServiceManagerCommand(
                            new[] { "list-unit-files", "--type=service", "--no-legend", "--no-pager" },
                            elevate: false);
                        ThrowIfEnumerationFailed(unitFiles);

                        services = UnixServiceParser.ParseSystemdListUnits(units.StandardOutput);
                        UnixServiceParser.MergeSystemdUnitFiles(services, unitFiles.StandardOutput);
                    }

                    break;

                case ServiceManagerKind.LaunchD:
                    {
                        ServiceCommandResult result = RunServiceManagerCommand(new[] { "list" }, elevate: false);
                        ThrowIfEnumerationFailed(result);
                        services = UnixServiceParser.ParseLaunchCtlList(result.StandardOutput);
                    }

                    break;

                case ServiceManagerKind.SysVInit:
                    {
                        ServiceCommandResult result = RunServiceManagerCommand(new[] { "--status-all" }, elevate: false);
                        ThrowIfEnumerationFailed(result);
                        // Some init scripts write their status to standard error.
                        services = UnixServiceParser.ParseSysVStatusAll(result.StandardOutput + result.StandardError);
                    }

                    break;

                case ServiceManagerKind.OpenRC:
                    {
                        ServiceCommandResult result = RunServiceManagerCommand(new[] { "--all" }, elevate: false);
                        ThrowIfEnumerationFailed(result);
                        services = UnixServiceParser.ParseOpenRcStatusAll(result.StandardOutput);
                    }

                    break;

                default:
                    services = new List<UnixServiceController>();
                    break;
            }

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
            var args = new List<string>();

            switch (manager.Kind)
            {
                case ServiceManagerKind.Systemd:
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
                    break;

                case ServiceManagerKind.LaunchD:
                    {
                        string domain = manager.IsElevated
                            ? "system"
                            : $"gui/{UnixServiceNativeMethods.GetUserId()}";
                        string unitPath = domain + "/" + service.Name;
                        switch (action)
                        {
                            case ServiceAction.Start:
                                args.Add("kickstart");
                                args.Add(unitPath);
                                break;

                            case ServiceAction.Stop:
                                args.Add("kill");
                                args.Add("SIGTERM");
                                args.Add(unitPath);
                                break;

                            case ServiceAction.Restart:
                                args.Add("kickstart");
                                args.Add("-k");
                                args.Add(unitPath);
                                break;

                            default:
                                throw PSTraceSource.NewArgumentException(nameof(action));
                        }
                    }

                    break;

                default:
                    // sysvinit and OpenRC use the same action verbs.
                    args.Add(action switch
                    {
                        ServiceAction.Start => "start",
                        ServiceAction.Stop => "stop",
                        ServiceAction.Restart => "restart",
                        _ => throw PSTraceSource.NewArgumentException(nameof(action)),
                    });
                    args.Add(service.Name);
                    break;
            }

            return args;
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
        internal List<string>? GetDependencyArguments(UnixServiceController service, bool dependents)        {
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
        /// <returns>The invocation result.</returns>
        internal ServiceCommandResult RunServiceManagerCommand(IList<string> arguments, bool elevate)
        {
            ServiceManagerContext manager = GetServiceManager();
            string fileName = manager.ManagerPath;
            var args = new List<string>(arguments);

            bool usedSudo = false;
            if (elevate && !manager.IsElevated && manager.SudoPath is not null)
            {
                // Insert the non-interactive flag first so that 'sudo -n' does not
                // hang when a password would be required.
                args.Insert(0, fileName);
                args.Insert(0, "-n");
                fileName = manager.SudoPath;
                usedSudo = true;
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

            var result = new ServiceCommandResult();
            _currentProcess = process;
            try
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
                _currentProcess = null;
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

        #endregion Internal Members

        #region Overrides

        /// <summary>
        /// Terminate the service manager process when the pipeline is stopped,
        /// for example by CTRL+C.
        /// </summary>
        protected override void StopProcessing()
        {
            try
            {
                if (_currentProcess is not null && !_currentProcess.HasExited)
                {
                    _currentProcess.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }

        #endregion Overrides

        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
                _currentProcess?.Dispose();
                _currentProcess = null;
            }
        }

        #endregion IDisposable Members

        #region Private Members

        private ServiceManagerContext? ResolveServiceManager()
        {
            foreach ((ServiceManagerKind kind, string commandName) in s_managerCandidates)
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

        #endregion Private Members
    }

    #endregion ServiceBaseCommand

    #region MultipleServiceCommandBase

    /// <summary>
    /// This class implements the base for service commands on Unix platforms which can
    /// operate on multiple services.
    /// </summary>
    public abstract class MultipleServiceCommandBase : ServiceBaseCommand
    {
        #region Private Members

        /// <summary>
        /// The various service selection modes.
        /// </summary>
        internal enum SelectionMode
        {
            /// <summary>
            /// Select all services.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Select services matching the supplied display names.
            /// </summary>
            DisplayName = 1,

            /// <summary>
            /// Select services based on pipeline input.
            /// </summary>
            InputObject = 2,

            /// <summary>
            /// Select services by service name.
            /// </summary>
            ServiceName = 3,
        }

        /// <summary>
        /// Holds the selection mode setting.
        /// </summary>
        internal SelectionMode selectionMode;

        /// <summary>
        /// Holds the service names. The Name parameter is declared in subclasses,
        /// since it is optional for Get-Service and mandatory otherwise.
        /// </summary>
        internal string[]? serviceNames;

        private UnixServiceController[]? _inputObject;
        private List<UnixServiceController>? _allServices;

        #endregion Private Members

        #region Parameters

        /// <summary>
        /// Gets or sets an array of display names for services.
        /// </summary>
        [Parameter(ParameterSetName = "DisplayName", Mandatory = true)]
        public string[]? DisplayName
        {
            get
            {
                return displayNames;
            }

            set
            {
                displayNames = value;
                selectionMode = SelectionMode.DisplayName;
            }
        }

        internal string[]? displayNames;

        /// <summary>
        /// Lets you include particular services. Services not matching one of these
        /// (if specified) are excluded. These are interpreted as either service names
        /// or display names according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[]? Include
        {
            get
            {
                return include;
            }

            set
            {
                include = value;
            }
        }

        internal string[]? include;

        /// <summary>
        /// Lets you exclude particular services. Services matching one of these
        /// (if specified) are excluded. These are interpreted as either service names
        /// or display names according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[]? Exclude
        {
            get
            {
                return exclude;
            }

            set
            {
                exclude = value;
            }
        }

        internal string[]? exclude;

        /// <summary>
        /// The input is a stream of UnixServiceController objects. This bypasses the
        /// Name and DisplayName parameters and reads the services directly. This allows
        /// us to deal with services which have wildcard characters in their name.
        /// </summary>
        [Parameter(ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public UnixServiceController[]? InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
                selectionMode = SelectionMode.InputObject;
            }
        }

        #endregion Parameters

        #region Internal

        /// <summary>
        /// Gets an array of all services.
        /// </summary>
        internal List<UnixServiceController> AllServices => _allServices ??= EnumerateServices();

        /// <summary>
        /// Gets a single service by name without wildcard characters.
        /// </summary>
        /// <param name="nameOfService">The name of the service, with or without the '.service' suffix.</param>
        /// <returns>The service, or null when it was not found.</returns>
        internal UnixServiceController? GetOneService(string nameOfService)
        {
            Dbg.Assert(!WildcardPattern.ContainsWildcardCharacters(nameOfService), "Caller should verify that nameOfService doesn't contain wildcard characters");

            string name = UnixServiceParser.RemoveServiceSuffix(nameOfService.Trim());
            foreach (UnixServiceController service in AllServices)
            {
                if (string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Services that were never loaded are only known by name; fetch
                    // their details from the service manager when it supports it.
                    RefreshServiceDetails(service);
                    return service;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve the list of all services matching the ServiceName, DisplayName,
        /// Include and Exclude parameters, sorted by service name.
        /// </summary>
        /// <returns>The matching services.</returns>
        internal List<UnixServiceController> MatchingServices()
        {
            List<UnixServiceController> matchingServices = selectionMode switch
            {
                SelectionMode.DisplayName => MatchingServicesByDisplayName(),
                SelectionMode.InputObject => MatchingServicesByInput(),
                _ => MatchingServicesByServiceName(),
            };

            matchingServices.Sort(static (x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the ServiceName, Include and
        /// Exclude parameters. Generates a non-terminating error for each specified
        /// service name which is not found even though it contains no wildcards.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByServiceName()
        {
            List<UnixServiceController> matchingServices = new();

            if (serviceNames is null)
            {
                foreach (UnixServiceController service in AllServices)
                {
                    IncludeExcludeAdd(matchingServices, service, checkDuplicates: false);
                }

                return matchingServices;
            }

            foreach (string pattern in serviceNames)
            {
                bool found = false;

                if (WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                    foreach (UnixServiceController service in AllServices)
                    {
                        if (!MatchesServiceName(wildcard, service))
                        {
                            continue;
                        }

                        found = true;
                        IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                    }
                }
                else
                {
                    UnixServiceController? service = GetOneService(pattern);
                    if (service is not null)
                    {
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        pattern,
                        string.Empty,
                        pattern,
                        innerException: null,
                        "NoServiceFoundForGivenName",
                        ServiceResources.NoServiceFoundForGivenName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the DisplayName, Include and
        /// Exclude parameters. Generates a non-terminating error for each specified
        /// display name which is not found even though it contains no wildcards.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByDisplayName()
        {
            List<UnixServiceController> matchingServices = new();
            if (DisplayName is null)
            {
                Dbg.Assert(false, "null DisplayName");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (string pattern in DisplayName)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                bool found = false;
                foreach (UnixServiceController service in AllServices)
                {
                    if (!wildcard.IsMatch(service.DisplayName))
                    {
                        continue;
                    }

                    found = true;
                    IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        string.Empty,
                        pattern,
                        pattern,
                        innerException: null,
                        "NoServiceFoundForGivenDisplayName",
                        ServiceResources.NoServiceFoundForGivenDisplayName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the InputObject, Include and
        /// Exclude parameters.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByInput()
        {
            List<UnixServiceController> matchingServices = new();
            if (InputObject is null)
            {
                Dbg.Assert(false, "null InputObject");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (UnixServiceController service in InputObject)
            {
                IncludeExcludeAdd(matchingServices, service, checkDuplicates: false);
            }

            return matchingServices;
        }

        /// <summary>
        /// Add <paramref name="service"/> to <paramref name="list"/>, but only if it
        /// passes the Include and Exclude filters (if present) and (if
        /// <paramref name="checkDuplicates"/>) if it is not already on
        /// <paramref name="list"/>.
        /// </summary>
        /// <param name="list">List of services.</param>
        /// <param name="service">Service to add to list.</param>
        /// <param name="checkDuplicates">Check list for duplicates.</param>
        private void IncludeExcludeAdd(
            List<UnixServiceController> list,
            UnixServiceController service,
            bool checkDuplicates)
        {
            if (include is not null && !Matches(service, include))
            {
                return;
            }

            if (exclude is not null && Matches(service, exclude))
            {
                return;
            }

            if (checkDuplicates)
            {
                foreach (UnixServiceController sc in list)
                {
                    if (string.Equals(sc.Name, service.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            list.Add(service);
        }

        /// <summary>
        /// Check whether <paramref name="service"/> matches the list of patterns in
        /// <paramref name="matchList"/>.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="matchList">The list of wildcard patterns.</param>
        /// <returns>True when the service matches one of the patterns.</returns>
        private bool Matches(UnixServiceController service, string[] matchList)
        {
            if (matchList is null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(matchList));
            }

            string serviceID = (selectionMode == SelectionMode.DisplayName)
                                ? service.DisplayName
                                : service.Name;
            foreach (string pattern in matchList)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                if (wildcard.IsMatch(serviceID))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether the service name matches the wildcard pattern, accepting both
        /// the bare name and the '.service'-suffixed unit name.
        /// </summary>
        /// <param name="wildcard">The wildcard pattern.</param>
        /// <param name="service">The service.</param>
        /// <returns>True when the pattern matches the service name.</returns>
        private static bool MatchesServiceName(WildcardPattern wildcard, UnixServiceController service)
        {
            return wildcard.IsMatch(service.Name) || wildcard.IsMatch(service.Name + ".service");
        }

        #endregion Internal
    }

    #endregion MultipleServiceCommandBase

    #region ServiceOperationBaseCommand

    /// <summary>
    /// This class implements the base for service commands on Unix platforms which
    /// actually act on the service(s).
    /// </summary>
    public abstract class ServiceOperationBaseCommand : MultipleServiceCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [Alias("ServiceName")]
        public string[]? Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        /// <summary>
        /// Service objects.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public new UnixServiceController[]? InputObject
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
        /// Specifies whether to write the objects successfully operated upon
        /// to the success stream.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion Parameters

        #region Internal

        /// <summary>
        /// This will start the service.
        /// </summary>
        /// <param name="service">Service to start.</param>
        /// <returns>True if-and-only-if the service was started.</returns>
        internal bool DoStartService(UnixServiceController service)
        {
            List<string> args = GetOperationArguments(ServiceAction.Start, service, noWait: false);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotStartService",
                ServiceResources.CouldNotStartService,
                ErrorCategory.OpenError);
        }

        /// <summary>
        /// This will stop the service.
        /// </summary>
        /// <param name="service">Service to stop.</param>
        /// <param name="noWait">Do not wait for the service to stop, when the service manager supports it.</param>
        /// <returns>True if-and-only-if the service was stopped.</returns>
        internal bool DoStopService(UnixServiceController service, bool noWait)
        {
            List<string> args = GetOperationArguments(ServiceAction.Stop, service, noWait);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotStopService",
                ServiceResources.CouldNotStopService,
                ErrorCategory.CloseError);
        }

        /// <summary>
        /// This will restart the service.
        /// </summary>
        /// <param name="service">Service to restart.</param>
        /// <returns>True if-and-only-if the service was restarted.</returns>
        internal bool DoRestartService(UnixServiceController service)
        {
            List<string> args = GetOperationArguments(ServiceAction.Restart, service, noWait: false);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotRestartService",
                ServiceResources.CouldNotRestartService,
                ErrorCategory.CloseError);
        }

        /// <summary>
        /// Applies the expected status of the service after a successful operation, so
        /// that -PassThru returns an up-to-date object without re-querying the service
        /// manager.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="status">The expected status.</param>
        /// <returns>The service with the expected status.</returns>
        internal static UnixServiceController ApplyExpectedStatus(UnixServiceController service, ServiceControllerStatus status)
        {
            service.Status = status;
            return service;
        }

        /// <summary>
        /// Runs the service manager for a mutating operation, writing a non-terminating
        /// error when the command fails. Non-zero exit codes surface the standard error
        /// output of the command as an ErrorRecord.
        /// </summary>
        /// <param name="service">The service to operate on.</param>
        /// <param name="args">The service manager arguments.</param>
        /// <param name="errorId">The error id for the failure.</param>
        /// <param name="errorMessage">The message resource string for the failure.</param>
        /// <param name="category">The error category for the failure.</param>
        /// <returns>True if-and-only-if the operation succeeded.</returns>
        private bool InvokeServiceOperation(
            UnixServiceController service,
            List<string> args,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            ServiceCommandResult result = RunServiceManagerCommand(args, elevate: true);

            if (result.SudoPasswordRequired)
            {
                WriteSudoRequiredError(service);
                return false;
            }

            if (result.ExitCode != 0)
            {
                WriteOperationFailedError(service, result, errorId, errorMessage, category);
                return false;
            }

            return true;
        }

        #endregion Internal
    }

    #endregion ServiceOperationBaseCommand

    #region GetServiceCommand

    /// <summary>
    /// This class implements the get-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Service", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096496", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(UnixServiceController))]
    public sealed class GetServiceCommand : MultipleServiceCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets/sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ServiceName")]
        public string[]? Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        /// <summary>
        /// This returns the dependent services of the specified service.
        /// </summary>
        [Parameter]
        [Alias("DS")]
        public SwitchParameter DependentServices { get; set; }

        /// <summary>
        /// This returns the services the specified service depends on.
        /// </summary>
        [Parameter]
        [Alias("SDO", "ServicesDependedOn")]
        public SwitchParameter RequiredServices { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Write the service objects.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController service in MatchingServices())
            {
                if (!DependentServices.IsPresent && !RequiredServices.IsPresent)
                {
                    WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(service));
                    continue;
                }

                if (DependentServices.IsPresent)
                {
                    foreach (UnixServiceController dependent in GetDependencies(service, dependents: true))
                    {
                        WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(dependent));
                    }
                }

                if (RequiredServices.IsPresent)
                {
                    foreach (UnixServiceController required in GetDependencies(service, dependents: false))
                    {
                        WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(required));
                    }
                }
            }
        }

        #endregion Overrides

        #region Private Members

        /// <summary>
        /// Gets the dependent or required services of a service. Only systemd provides
        /// dependency information; other service managers write a non-terminating error.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="dependents">True for services that depend on the service, false for services the service depends on.</param>
        /// <returns>The dependencies of the service.</returns>
        private List<UnixServiceController> GetDependencies(UnixServiceController service, bool dependents)
        {
            var results = new List<UnixServiceController>();
            List<string>? args = GetDependencyArguments(service, dependents);
            if (args is null)
            {
                string message = StringUtil.Format(
                    ServiceResources.ServiceManagerDoesNotSupportFeature,
                    GetManagerCommandName());
                var exception = new ServiceCommandException(message)
                {
                    ServiceName = service.Name,
                };

                WriteError(new ErrorRecord(exception, "ServiceManagerDoesNotSupportFeature", ErrorCategory.NotImplemented, service));
                return results;
            }

            ServiceCommandResult result = RunServiceManagerCommand(args, elevate: false);
            if (result.ExitCode != 0)
            {
                WriteOperationFailedError(
                    service,
                    result,
                    "CouldNotGetServiceDependencies",
                    ServiceResources.CouldNotGetServiceDependencies,
                    ErrorCategory.InvalidOperation);
                return results;
            }

            List<string> dependencyNames = UnixServiceParser.ParseSystemdDependencyUnits(result.StandardOutput);
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.OrdinalIgnoreCase);
            foreach (UnixServiceController candidate in AllServices)
            {
                servicesByName[candidate.Name] = candidate;
            }

            foreach (string dependencyName in dependencyNames)
            {
                if (string.Equals(dependencyName, service.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (servicesByName.TryGetValue(dependencyName, out UnixServiceController? dependency))
                {
                    results.Add(dependency);
                }
                else
                {
                    results.Add(new UnixServiceController
                    {
                        Name = dependencyName,
                        DisplayName = dependencyName,
                        Status = ServiceControllerStatus.Stopped,
                        StartType = ServiceStartMode.Manual,
                    });
                }
            }

            return results;
        }

        #endregion Private Members
    }

    #endregion GetServiceCommand

    #region StopServiceCommand

    /// <summary>
    /// This class implements the stop-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097052")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class StopServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether to force the service to stop even if
        /// it has dependent services. The parameter is accepted for script compatibility
        /// with Windows; on Unix, the service manager itself handles service ordering.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to return immediately instead of
        /// waiting for the service to reach the stopped state, when the service manager
        /// supports it.
        /// </summary>
        [Parameter]
        public SwitchParameter NoWait { get; set; }

        /// <summary>
        /// Stop the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoStopService(serviceController, noWait: NoWait.IsPresent) && PassThru)
                {
                    WriteObject(
                        UnixServiceCmdletHelper.AddDefaultDisplayProperties(
                            ApplyExpectedStatus(serviceController, ServiceControllerStatus.Stopped)));
                }
            }
        }
    }

    #endregion StopServiceCommand

    #region StartServiceCommand

    /// <summary>
    /// This class implements the start-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097051")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class StartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Start the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoStartService(serviceController) && PassThru)
                {
                    WriteObject(
                        UnixServiceCmdletHelper.AddDefaultDisplayProperties(
                            ApplyExpectedStatus(serviceController, ServiceControllerStatus.Running)));
                }
            }
        }
    }

    #endregion StartServiceCommand

    #region RestartServiceCommand

    /// <summary>
    /// This class implements the restart-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097059")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class RestartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether to force the service to stop even if
        /// it has dependent services. The parameter is accepted for script compatibility
        /// with Windows; on Unix, the service manager itself handles service ordering.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Stop and restart the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoRestartService(serviceController) && PassThru)
                {
                    WriteObject(
                        UnixServiceCmdletHelper.AddDefaultDisplayProperties(
                            ApplyExpectedStatus(serviceController, ServiceControllerStatus.Running)));
                }
            }
        }
    }

    #endregion RestartServiceCommand

    #region UnixServiceCmdletHelper

    /// <summary>
    /// Shared helpers for the service cmdlets on Unix platforms.
    /// </summary>
    internal static class UnixServiceCmdletHelper
    {
        /// <summary>
        /// Adds the default display property set to the service so that it is formatted
        /// similarly to the Windows output of Get-Service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns>The service as a PSObject with the default display property set.</returns>
        internal static PSObject AddDefaultDisplayProperties(UnixServiceController service)
        {
            PSObject serviceAsPSObj = PSObject.AsPSObject(service);
            PSMemberInfo[] defaultPropertySet = new PSMemberInfo[]
            {
                new PSPropertySet("DefaultDisplayPropertySet", new string[] { "Status", "Name", "DisplayName", "StartType" }),
            };
            serviceAsPSObj.Members.Add(new PSMemberSet("PSStandardMember", defaultPropertySet));
            return serviceAsPSObj;
        }
    }

    #endregion UnixServiceCmdletHelper
}

#endif
