// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Defines an interface for a factory that creates
    /// StateDescriptors.
    /// </summary>
    /// <typeparam name="T">The type T used by the StateDescriptor.</typeparam>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IStateDescriptorFactory<T>
    {
        /// <summary>
        /// Creates a new StateDescriptor based upon custom
        /// logic.
        /// </summary>
        /// <returns>A new StateDescriptor.</returns>
        StateDescriptor<T> Create();
    }
}
