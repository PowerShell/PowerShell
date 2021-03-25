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
        /// Default implementation for `ISubsystem.Kind`.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.CrossPlatformDsc;

        /// <summary>
        /// Default summary.
        /// </summary>
        void LoadDefaultCimKeywords(Collection<Exception> errors);

        /// <summary>
        /// Default summary.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Default summary.
        /// </summary>
        bool NewApiIsUsed { get; }

        /// <summary>
        /// Default summary.
        /// </summary>
        string GetDSCResourceUsageString(DynamicKeyword keyword);

        /// <summary>
        /// Experimental feature name for DSC v3.
        /// </summary>
        const string DscExperimentalFeatureName = "PS7DscSupport";
    }
}
