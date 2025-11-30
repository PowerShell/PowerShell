// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Help
{
    /// <summary>
    /// This class represents a help system URI.
    /// </summary>
    internal class UpdatableHelpUri
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="moduleGuid">Module guid.</param>
        /// <param name="culture">UI culture.</param>
        /// <param name="resolvedUri">Resolved URI.</param>
        internal UpdatableHelpUri(string moduleName, Guid moduleGuid, CultureInfo culture, string resolvedUri)
        {
            Debug.Assert(!string.IsNullOrEmpty(moduleName));
            // Condition is required, `GetHelpInfoUri()` may call this function with empty GUID.
            if (!moduleName.Equals(InitialSessionState.CoreSnapin, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Assert(moduleGuid != Guid.Empty);
            }
            Debug.Assert(!string.IsNullOrEmpty(resolvedUri));

            ModuleName = moduleName;
            ModuleGuid = moduleGuid;
            Culture = culture;
            ResolvedUri = resolvedUri;
        }

        /// <summary>
        /// Module name.
        /// </summary>
        internal string ModuleName { get; }

        /// <summary>
        /// Module GUID.
        /// </summary>
        internal Guid ModuleGuid { get; }

        /// <summary>
        /// UI Culture.
        /// </summary>
        internal CultureInfo Culture { get; }

        /// <summary>
        /// Resolved URI.
        /// </summary>
        internal string ResolvedUri { get; }
    }
}
