// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Enums for ConvertFrom-Json -DateKind parameter.
    /// </summary>
    public enum JsonDateKind
    {
        /// <summary>
        /// DateTime values are returned as a DateTime with the Kind representing the time zone in the raw string.
        /// </summary>
        Default,

        /// <summary>
        /// DateTime values are returned as the Local kind representation of the value.
        /// </summary>
        Local,

        /// <summary>
        /// DateTime values are returned as the UTC kind representation of the value.
        /// </summary>
        Utc,

        /// <summary>
        /// DateTime values are returned as a DateTimeOffset value preserving the timezone information.
        /// </summary>
        Offset,

        /// <summary>
        /// DateTime values are returned as raw strings.
        /// </summary>
        String,
    }
}
