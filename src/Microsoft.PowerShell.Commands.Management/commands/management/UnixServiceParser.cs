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
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.Ordinal);
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

                // An alias unit file is another name for a different unit, not a service of
                // its own; listing it as a separate stopped service would be wrong.
                if (string.Equals(unitFile.State, "alias", StringComparison.OrdinalIgnoreCase))
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
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.Ordinal);

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
        /// Adds the services installed but not reported by 'rc-status --all' as stopped.
        /// </summary>
        /// <remarks>
        /// 'rc-status' only lists services in a runlevel or started dynamically, so an
        /// installed service that belongs to no runlevel only appears in
        /// 'rc-service --list'.
        /// </remarks>
        /// <param name="services">The services parsed from the status output.</param>
        /// <param name="output">The standard output of 'rc-service --list'.</param>
        internal static void MergeOpenRcInstalledServices(List<UnixServiceController> services, string output)
        {
            var knownNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (UnixServiceController service in services)
            {
                knownNames.Add(service.Name);
            }

            foreach (string rawLine in output.Split('\n'))
            {
                // One service name per line; the names of a service provided by several
                // init scripts are separated by whitespace.
                foreach (string name in rawLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!knownNames.Add(name))
                    {
                        continue;
                    }

                    services.Add(new UnixServiceController
                    {
                        Name = name,
                        DisplayName = name,
                        Status = ServiceControllerStatus.Stopped,
                        StartType = ServiceStartMode.Manual,
                    });
                }
            }
        }

        /// <summary>
        /// Parses the output of 'systemctl show --property=... --value' for dependency
        /// properties such as Wants, Requires, WantedBy and RequiredBy.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The names of the referenced services, without the '.service' suffix.</returns>
        internal static List<string> ParseSystemdDependencyUnits(string output)
        {
            var names = new List<string>();

            // Each property value is a whitespace-separated list of unit names.
            foreach (string rawLine in output.Split('\n'))
            {
                foreach (string unit in rawLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (unit.EndsWith(ServiceSuffix, StringComparison.Ordinal) &&
                        !unit.EndsWith("@.service", StringComparison.Ordinal))
                    {
                        string name = RemoveServiceSuffix(unit);
                        if (!names.Contains(name))
                        {
                            names.Add(name);
                        }
                    }
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
                "active" or "reloading" => ServiceControllerStatus.Running,
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
                "masked" or "masked-runtime" => ServiceStartMode.Disabled,
                _ => ServiceStartMode.Manual, // disabled (can still be started manually), static, linked, indirect, alias, ...
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
        /// Creates a service from the output of 'systemctl show &lt;unit&gt; --property=...'
        /// for a single unit.
        /// </summary>
        /// <remarks>
        /// The properties are requested as 'key=value' pairs and looked up by key, because
        /// systemctl doesn't preserve the order of the requested properties.
        /// </remarks>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The service, or null when the unit isn't a service unit or isn't known.</returns>
        internal static UnixServiceController? CreateFromSystemdUnitProperties(string output)
        {
            Dictionary<string, string> properties = ParseSystemdUnitProperties(output);
            if (!properties.TryGetValue("LoadState", out string? loadState)
                || string.Equals(loadState, "not-found", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!properties.TryGetValue("Id", out string? id)
                || !id.EndsWith(ServiceSuffix, StringComparison.Ordinal))
            {
                return null;
            }

            properties.TryGetValue("ActiveState", out string? activeState);
            properties.TryGetValue("SubState", out string? subState);
            properties.TryGetValue("Description", out string? description);
            properties.TryGetValue("UnitFileState", out string? unitFileState);
            if (string.IsNullOrEmpty(description))
            {
                description = null;
            }

            string name = RemoveServiceSuffix(id);
            return new UnixServiceController
            {
                Name = name,
                DisplayName = description ?? name,
                Description = description,
                Status = MapSystemdStatus(activeState ?? string.Empty),
                SubState = subState,
                UnitFileState = string.IsNullOrEmpty(unitFileState) ? null : unitFileState,
                StartType = MapSystemdUnitFileState(unitFileState ?? string.Empty),
                ManagerKind = ServiceManagerKind.Systemd,
            };
        }

        /// <summary>
        /// Parses the output of 'systemctl show &lt;unit...&gt; --property=Id,Description' for
        /// one or more units into a map from service name to description.
        /// </summary>
        /// <param name="output">The standard output of the command.</param>
        /// <returns>The description of each unit that has one.</returns>
        internal static Dictionary<string, string> ParseSystemdUnitDescriptions(string output)
        {
            var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);

            // The units are separated by blank lines.
            string name = string.Empty;
            string description = string.Empty;
            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    AddDescription(descriptions, name, description);
                    name = string.Empty;
                    description = string.Empty;
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator < 0)
                {
                    continue;
                }

                string key = line[..separator];
                string value = line[(separator + 1)..];
                if (key == "Id" && value.EndsWith(ServiceSuffix, StringComparison.Ordinal))
                {
                    name = RemoveServiceSuffix(value);
                }
                else if (key == "Description")
                {
                    description = value;
                }
            }

            AddDescription(descriptions, name, description);
            return descriptions;
        }

        private static void AddDescription(Dictionary<string, string> descriptions, string name, string description)
        {
            if (name.Length > 0 && description.Length > 0)
            {
                descriptions[name] = description;
            }
        }

        private static Dictionary<string, string> ParseSystemdUnitProperties(string output)
        {
            var properties = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rawLine in output.Split('\n'))
            {
                int separator = rawLine.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                properties[rawLine[..separator]] = rawLine[(separator + 1)..].TrimEnd('\r');
            }

            return properties;
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

            // Format: '[ + ] name [ (description) ]'. '+' marks a running service, '-' a
            // stopped one, '?' that the script has no 'status' action at all.
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
                // '?' means the status is unknown, not that the service is stopped.
                StatusKnown = state != "?",
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
            // For supervised services OpenRC appends uptime details to the state, for
            // example 'started 00:12:34 (0)'; only the first word is the state.
            string[] stateTokens = line[(openIndex + 1)..closeIndex].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (stateTokens.Length == 0)
            {
                return (null, ServiceControllerStatus.Stopped);
            }

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

            return (name, MapOpenRcStatus(stateTokens[0]));
        }
    }
}

#endif
