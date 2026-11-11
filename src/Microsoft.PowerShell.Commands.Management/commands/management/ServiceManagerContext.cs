// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

namespace Microsoft.PowerShell.Commands
{
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
}

#endif
