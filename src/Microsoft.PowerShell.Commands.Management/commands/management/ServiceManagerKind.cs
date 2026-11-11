// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

namespace Microsoft.PowerShell.Commands
{
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
}

#endif
