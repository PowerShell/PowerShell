/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Globalization;
using System.Diagnostics;

namespace System.Management.Automation.Help
{
    /// <summary>
    /// This class represents a help system URI
    /// </summary>
    internal class UpdatableHelpUri
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="moduleName">module name</param>
        /// <param name="moduleGuid">module guid</param>
        /// <param name="culture">UI culture</param>
        /// <param name="resolvedUri">resolved URI</param>
        internal UpdatableHelpUri(string moduleName, Guid moduleGuid, CultureInfo culture, string resolvedUri)
        {
            Debug.Assert(!String.IsNullOrEmpty(moduleName));
            Debug.Assert(moduleGuid != null);
            Debug.Assert(!String.IsNullOrEmpty(resolvedUri));

            ModuleName = moduleName;
            ModuleGuid = moduleGuid;
            Culture = culture;
            ResolvedUri = resolvedUri;
        }

        /// <summary>
        /// Module name
        /// </summary>
        internal string ModuleName { get; }

        /// <summary>
        /// Module GUID
        /// </summary>
        internal Guid ModuleGuid { get; }

        /// <summary>
        /// UI Culture
        /// </summary>
        internal CultureInfo Culture { get; }

        /// <summary>
        /// Resolved URI
        /// </summary>
        internal string ResolvedUri { get; }
    }
}
