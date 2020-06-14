// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IFilterExpressionProvider interface defines the contract between
    /// providers of FilterExpressions and consumers thereof.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IFilterExpressionProvider
    {
        /// <summary>
        /// Gets a FilterExpression representing the current
        /// relational organization of FilterRules for this provider.
        /// </summary>
        FilterExpressionNode FilterExpression
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this provider currently has a non-empty filter expression.
        /// </summary>
        bool HasFilterExpression
        {
            get;
        }

        /// <summary>
        /// Raised when the FilterExpression of this provider
        /// has changed.
        /// </summary>
        event EventHandler FilterExpressionChanged;
    }
}
