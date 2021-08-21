// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A completer for HTTP version names.
    /// </summary>
    internal sealed class HttpVersionCompletionsAttribute : ArgumentCompletionsAttribute
    {
        /// <inheritdoc/>
        public HttpVersionCompletionsAttribute() : base(HttpVersionUtils.AllowedVersions)
        {
        } 
    }
}
