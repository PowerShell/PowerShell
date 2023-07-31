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
        /// Initializes a new instance of the TextContainsFilterRule class.
        /// </summary>
        public TextContainsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Contains;
        }

        /// <summary>
        /// Creates a clone of the TextContainsFilterRule instance.
        /// </summary>
        /// <returns>
        /// Returns a clone of the TextContainsFilterRule instance.
        /// </returns>
        public override FilterRule Clone()
        {
            TextContainsFilterRule rule = new TextContainsFilterRule();
            rule.Value = this.Value;
            rule.DefaultNullValueEvaluation = this.DefaultNullValueEvaluation;
            return rule;
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
