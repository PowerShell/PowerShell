// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IsNotEmptyFilterRule evaluates an item to determine whether it
    /// is empty or not.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class IsNotEmptyFilterRule : IsEmptyFilterRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsNotEmptyFilterRule"/> class.
        /// </summary>
        public IsNotEmptyFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_IsNotEmpty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsNotEmptyFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public IsNotEmptyFilterRule(IsNotEmptyFilterRule source)
            : base(source)
        {
        }

        /// <summary>
        /// Gets a values indicating whether the supplied item is not empty.
        /// </summary>
        /// <param name="item">The item to evaluate.</param>
        /// <returns>
        /// Returns false if the item is null or if the item is a string
        /// composed of whitespace. True otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            return !base.Evaluate(item);
        }
    }
}
