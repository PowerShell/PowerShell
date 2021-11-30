// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation.Subsystem.DSC
{
    /// <summary>
    /// Interface for implementing a cross platform desired state configuration component.
    /// </summary>
    public interface ICrossPlatformDsc : ISubsystem
    {
        /// <summary>
        /// Subsystem kind.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.CrossPlatformDsc;

        /// <summary>
        /// Default implementation. No function is required for this subsystem.
        /// </summary>
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// DSC initializer function.
        /// </summary>
        void LoadDefaultKeywords(Collection<Exception> errors);

        /// <summary>
        /// Clear internal class caches.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Returns resource usage string.
        /// </summary>
        string GetDSCResourceUsageString(DynamicKeyword keyword);

        /// <summary>
        /// Checks if a string is one of dynamic keywords that can be used in both configuration and meta configuration.
        /// </summary>
        bool IsSystemResourceName(string name);

        /// <summary>
        /// Checks if a string matches default module name used for meta configuration resources.
        /// </summary>
        bool IsDefaultModuleNameForMetaConfigResource(string name);
    }
}
