// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Generic;
using System.ServiceProcess;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Parses the output of the supported service managers into
    /// <see cref="UnixServiceController"/> objects.
    /// </summary>
    internal static class UnixServiceParser
    {
        private static readonly string ServiceSuffix = ".service";

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
                UnixServiceController? service = TryParseSystemdUnitLine(rawLine);
                if (service is not null)
                {
                    services.Add(service);
                }
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
                (string? Name, string? State) unitFile = TryParseSystemdUnitFileLine(rawLine);
                if (unitFile.Name is null || unitFile.State is null)
                {
                    continue;
                }

                if (servicesByName.TryGetValue(unitFile.Name, out UnixServiceController? service))
                {
                    service.UnitFileState = unitFile.State;
                    service.StartType = MapSystemdUnitFileState(unitFile.State);
                }
                else
                {
                    // The service exists but was never loaded (for example it is disabled).
                    var newService = new UnixServiceController
                    {
                        Name = unitFile.Name,
                        DisplayName = unitFile.Name,
                        Status = ServiceControllerStatus.Stopped,
                        StartType = MapSystemdUnitFileState(unitFile.State),
                        UnitFileState = unitFile.State,
                    };
                    servicesByName[unitFile.Name] = newService;
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
                UnixServiceController? service = TryParseLaunchCtlLine(rawLine);
                if (service is not null)
                {
                    services.Add(service);
                }
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
                UnixServiceController? service = TryParseSysVStatusLine(rawLine);
                if (service is not null)
                {
                    services.Add(service);
                }
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
                (string? Name, ServiceControllerStatus Status) service = TryParseOpenRcStatusLine(rawLine);
                if (service.Name is null)
                {
                    continue;
                }

                if (servicesByName.TryGetValue(service.Name, out UnixServiceController? existing))
                {
                    // Services may be listed under multiple runlevels; prefer a running entry.
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        existing.Status = service.Status;
                    }
                }
                else
                {
                    servicesByName[service.Name] = new UnixServiceController
                    {
                        Name = service.Name,
                        DisplayName = service.Name,
                        Status = service.Status,
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

        /// <summary>
        /// Parses a single line of 'systemctl list-units' output. Returns null for
        /// lines that do not describe a real service unit.
        /// </summary>
        /// <param name="rawLine">The line to parse.</param>
        /// <returns>The parsed service, or null.</returns>
        private static UnixServiceController? TryParseSystemdUnitLine(string rawLine)
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                return null;
            }

            // Columns: UNIT LOAD ACTIVE SUB DESCRIPTION
            string[] tokens = line.Split((char[]?)null, 5, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4 ||
                !tokens[0].EndsWith(ServiceSuffix, StringComparison.Ordinal) ||
                tokens[0].EndsWith("@.service", StringComparison.Ordinal))
            {
                // Only real '.service' units are of interest here and
                // template units ('foo@.service') are not services themselves.
                return null;
            }

            string loadState = tokens[1];
            if (loadState == "not-found")
            {
                return null;
            }

            string activeState = tokens[2];
            string subState = tokens[3];
            string description = tokens.Length > 4 ? tokens[4].Trim() : string.Empty;
            string name = RemoveServiceSuffix(tokens[0]);

            return new UnixServiceController
            {
                Name = name,
                DisplayName = description.Length > 0 ? description : name,
                Description = description.Length > 0 ? description : null,
                Status = MapSystemdStatus(activeState),
                SubState = subState,
                StartType = ServiceStartMode.Manual,
            };
        }

        /// <summary>
        /// Parses a single line of 'systemctl list-unit-files' output. Returns null
        /// parts for lines that do not describe a real service unit file.
        /// </summary>
        /// <param name="rawLine">The line to parse.</param>
        /// <returns>The service name and unit file state, or null parts.</returns>
        private static (string? Name, string? State) TryParseSystemdUnitFileLine(string rawLine)
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                return (null, null);
            }

            // Columns: UNIT FILE STATE [VENDOR PRESET]
            string[] tokens = line.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2 ||
                !tokens[0].EndsWith(ServiceSuffix, StringComparison.Ordinal) ||
                tokens[0].EndsWith("@.service", StringComparison.Ordinal))
            {
                return (null, null);
            }

            return (RemoveServiceSuffix(tokens[0]), tokens[1]);
        }

        /// <summary>
        /// Parses a single line of 'launchctl list' output. Returns null for header
        /// lines and lines that do not describe a service label.
        /// </summary>
        /// <param name="rawLine">The line to parse.</param>
        /// <returns>The parsed service, or null.</returns>
        private static UnixServiceController? TryParseLaunchCtlLine(string rawLine)
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                return null;
            }

            // Columns: PID Status Label. The PID is '-' when the label is not running.
            string[] tokens = line.Split('\t');
            if (tokens.Length < 3)
            {
                tokens = line.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
            }

            if (tokens.Length < 3 || tokens[0] == "PID")
            {
                return null;
            }

            bool isRunning = tokens[0] != "-";
            return new UnixServiceController
            {
                Name = tokens[2],
                DisplayName = tokens[2],
                Status = isRunning ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped,
                StartType = ServiceStartMode.Manual,
            };
        }

        /// <summary>
        /// Parses a single line of 'service --status-all' output. Returns null for
        /// lines that do not describe a service.
        /// </summary>
        /// <param name="rawLine">The line to parse.</param>
        /// <returns>The parsed service, or null.</returns>
        private static UnixServiceController? TryParseSysVStatusLine(string rawLine)
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || !line.StartsWith('['))
            {
                return null;
            }

            int closeIndex = line.IndexOf(']');
            if (closeIndex < 0)
            {
                return null;
            }

            // Format: '[ + ] name [ (description) ]'. '+' marks a running service.
            string state = line[1..closeIndex].Trim();
            string rest = line[(closeIndex + 1)..].Trim();
            if (rest.Length == 0)
            {
                return null;
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

            return new UnixServiceController
            {
                Name = name,
                DisplayName = description ?? name,
                Description = description,
                Status = state == "+" ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped,
                StartType = ServiceStartMode.Manual,
            };
        }

        /// <summary>
        /// Parses a single line of 'rc-status --all' output. Returns a null name for
        /// lines that do not describe a service (for example runlevel headers).
        /// </summary>
        /// <param name="rawLine">The line to parse.</param>
        /// <returns>The service name and status, or a null name.</returns>
        private static (string? Name, ServiceControllerStatus Status) TryParseOpenRcStatusLine(string rawLine)
        {
            string line = rawLine.Trim();
            int openIndex = line.IndexOf('[');
            if (openIndex < 0)
            {
                return (null, ServiceControllerStatus.Stopped);
            }

            int closeIndex = line.IndexOf(']', openIndex);
            if (closeIndex < 0)
            {
                return (null, ServiceControllerStatus.Stopped);
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
                return (null, ServiceControllerStatus.Stopped);
            }

            return (name, MapOpenRcStatus(state));
        }
    }
}

#endif
