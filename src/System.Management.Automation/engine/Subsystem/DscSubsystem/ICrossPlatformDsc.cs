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
        /// Subsystem kind.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.CrossPlatformDsc;

        /// <summary>
        /// Dsc initializer function.
        /// </summary>
        void LoadDefaultCimKeywords(Collection<Exception> errors);

        /// <summary>
        /// Clear internal class caches.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Flag that shows whether v2 or v3 API was initialized.
        /// </summary>
        bool NewApiIsUsed { get; }

        /// <summary>
        /// Returns resource usage string.
        /// </summary>
        string GetDSCResourceUsageString(DynamicKeyword keyword);
    }
}
