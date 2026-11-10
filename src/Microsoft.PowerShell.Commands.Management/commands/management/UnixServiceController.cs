// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System.ServiceProcess;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// Represents a service managed by the operating system service manager on Unix platforms,
    /// such as systemd (Linux), launchd (macOS), sysvinit or OpenRC.
    /// This is the object type returned by the service cmdlets when they run on Unix.
    /// </summary>
    public sealed class UnixServiceController
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// For systemd, this is the unit name without the '.service' suffix.
        /// </summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the service.
        /// For systemd, this is the unit description when available, otherwise the service name.
        /// </summary>
        public string DisplayName { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status of the service, mapped onto the <see cref="ServiceControllerStatus"/>
        /// values also used by <see cref="System.ServiceProcess.ServiceController"/> on Windows.
        /// </summary>
        public ServiceControllerStatus Status { get; internal set; }

        /// <summary>
        /// Gets or sets the start type of the service, mapped onto the <see cref="ServiceStartMode"/>
        /// values also used on Windows. When the service manager does not provide this
        /// information, <see cref="ServiceStartMode.Manual"/> is reported.
        /// </summary>
        public ServiceStartMode StartType { get; internal set; }

        /// <summary>
        /// Gets or sets the description of the service when the service manager provides one.
        /// </summary>
        public string? Description { get; internal set; }

        /// <summary>
        /// Gets or sets the sub state of a systemd service (for example 'running', 'dead' or 'failed')
        /// when the service manager provides one.
        /// </summary>
        public string? SubState { get; internal set; }

        /// <summary>
        /// Gets or sets the unit file state of a systemd service (for example 'enabled' or 'disabled')
        /// when the service manager provides one.
        /// </summary>
        public string? UnitFileState { get; internal set; }

        /// <summary>
        /// Returns the name of the service.
        /// </summary>
        /// <returns>The name of the service.</returns>
        public override string ToString() => Name;
    }
}

#endif
