//-----------------------------------------------------------------------
// <copyright file="TextContainsFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// The TextContainsFilterRule class evaluates a string item to
    /// check if it is contains the rule's value within it.
    /// </summary>
    [Serializable]
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
            Debug.Assert(this.IsValid);

            // True "text contains": \\
            return this.ExactMatchEvaluate(data, TextContainsCharactersRegexPattern, TextContainsWordsRegexPattern);
        }
    }
}
