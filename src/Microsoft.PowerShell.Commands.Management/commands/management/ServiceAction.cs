// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

namespace Microsoft.PowerShell.Commands
{
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
}

#endif
