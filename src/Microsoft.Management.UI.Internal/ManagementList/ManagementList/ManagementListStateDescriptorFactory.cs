// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Defines a factory which returns ManagementListStateDescriptors.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ManagementListStateDescriptorFactory : IStateDescriptorFactory<ManagementList>
    {
        /// <summary>
        /// Factory method that creates a ManagementListStateDescriptor.
        /// </summary>
        /// <returns>A new ManagementListStateDescriptor.</returns>
        public StateDescriptor<ManagementList> Create()
        {
            return new ManagementListStateDescriptor();
        }
    }
}
