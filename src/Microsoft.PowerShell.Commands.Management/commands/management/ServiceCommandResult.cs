// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX


namespace Microsoft.PowerShell.Commands
{
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

        /// <summary>
        /// Gets or sets a value indicating whether the invocation required elevation,
        /// the process is not running as root, and sudo was not found on PATH.
        /// </summary>
        internal bool SudoUnavailable { get; set; }
    }
}

#endif
