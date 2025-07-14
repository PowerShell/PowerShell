// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Defines a generalized method for creating a deep copy of an instance.
    /// </summary>
    internal interface IDeepCloneable
    {
        /// <summary>
        /// Creates a deep copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a deep copy of the current instance.</returns>
        object DeepClone();
    }
}
