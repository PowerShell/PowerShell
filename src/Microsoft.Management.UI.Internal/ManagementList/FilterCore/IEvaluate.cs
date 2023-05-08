// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IEvaluate interface provides the most basic
    /// support for the evaluation of an item against
    /// criteria defined in a derived class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IEvaluate
    {
        /// <summary>
        /// Gets a values indicating whether the supplied item has meet the
        /// criteria rule specified by the rule.
        /// </summary>
        /// <param name="item">
        /// The item to evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item meets the criteria. False otherwise.
        /// </returns>
        bool Evaluate(object item);
    }
}
