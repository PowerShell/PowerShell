// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Represents the result of search text parsing.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SearchTextParseResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SearchTextParseResult"/> with the specified <see cref="FilterRule"/>.
        /// </summary>
        /// <param name="rule">The rule that resulted from parsing the search text.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public SearchTextParseResult(FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.FilterRule = rule;
        }

        /// <summary>
        /// Gets the rule that resulted from parsing the search text.
        /// </summary>
        public FilterRule FilterRule
        {
            get;
            private set;
        }
    }
}
