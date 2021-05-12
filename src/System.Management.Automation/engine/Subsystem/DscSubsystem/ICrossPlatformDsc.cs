// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Interface for implementing a cross platform desired state configuration component.
    /// </summary>
    public interface ICrossPlatformDsc : ISubsystem
    {
        /// <summary>
        /// Possible API versions.
        /// </summary>
        public enum ApiVersion
        {
            /// <summary>
            /// V2 API based on MOF/script resources and used by PSDesiredStateConfiguration v2.x module.
            /// </summary>
            V2,

            /// <summary>
            /// V3 API based on JSON/class resources and used by PSDesiredStateConfiguration v3.x module.
            /// </summary>
            V3
        }

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
        /// Flag that shows what version of API was initialized.
        /// </summary>
        ApiVersion InitializedApiVersion { get; }

        /// <summary>
        /// Returns resource usage string.
        /// </summary>
        string GetDSCResourceUsageString(DynamicKeyword keyword);
    }
}
