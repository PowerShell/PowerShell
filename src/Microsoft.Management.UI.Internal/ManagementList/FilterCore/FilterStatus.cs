// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterStatus enum is used to classify the current status a <see cref="FilterEvaluator" /> is in.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum FilterStatus
    {
        /// <summary>
        /// A FilterStatus of NotApplied indicates that the filter is currently
        /// not applied.
        /// </summary>
        NotApplied = 0,

        /// <summary>
        /// A FilterStatus of InProgress indicates that the filter is being
        /// applied but is not done.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// A FilterStatus of Applied indicates that the filter has been
        /// applied.
        /// </summary>
        Applied = 2
    }
}
