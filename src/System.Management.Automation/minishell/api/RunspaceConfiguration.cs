/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Enum for describing different kind information that can be configured in runspace configuration.
    /// </summary>
    internal enum RunspaceConfigurationCategory
    {
        /// <summary>
        /// Cmdlets
        /// </summary>
        Cmdlets,

        /// <summary>
        /// Providers
        /// </summary>
        Providers,

        /// <summary>
        /// Assemblies
        /// </summary>
        Assemblies,

        /// <summary>
        /// Scripts
        /// </summary>
        Scripts,

        /// <summary>
        /// Initialization scripts
        /// </summary>
        InitializationScripts,

        /// <summary>
        /// Types
        /// </summary>
        Types,

        /// <summary>
        /// Formats
        /// </summary>
        Formats,
    }

}
