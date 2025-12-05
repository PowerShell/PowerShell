// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The TextContainsFilterRule class evaluates a string item to
    /// check if it is contains the rule's value within it.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextContainsFilterRule : TextFilterRule
    {
        private static readonly string TextContainsCharactersRegexPattern = "{0}";
        private static readonly string TextContainsWordsRegexPattern = WordBoundaryRegexPattern + TextContainsCharactersRegexPattern + WordBoundaryRegexPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextContainsFilterRule"/> class.
        /// </summary>
        public TextContainsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Contains;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextContainsFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public TextContainsFilterRule(TextContainsFilterRule source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if Value is contained within data.
        /// </summary>
        /// <param name="data">
        /// The data to compare with.
        /// </param>
        /// <returns>
        /// Returns true if data contains Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            // True "text contains": \\
            return this.ExactMatchEvaluate(data, TextContainsCharactersRegexPattern, TextContainsWordsRegexPattern);
        }
    }
}
