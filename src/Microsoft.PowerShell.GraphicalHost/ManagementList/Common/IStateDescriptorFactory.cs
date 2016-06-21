//-----------------------------------------------------------------------
// <copyright file="IStateDescriptorFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;

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
