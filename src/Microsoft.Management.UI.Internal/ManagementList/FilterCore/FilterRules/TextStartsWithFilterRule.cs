// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The TextStartsWithFilterRule class evaluates a string item to
    /// check if it starts with the rule's value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextStartsWithFilterRule : TextFilterRule
    {
        private static readonly string TextStartsWithCharactersRegexPattern = "^{0}";
        private static readonly string TextStartsWithWordsRegexPattern = TextStartsWithCharactersRegexPattern + WordBoundaryRegexPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextStartsWithFilterRule"/> class.
        /// </summary>
        public TextStartsWithFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_TextStartsWith;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextStartsWithFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public TextStartsWithFilterRule(TextStartsWithFilterRule source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if data starts with Value.
        /// </summary>
        /// <param name="data">
        /// The value to compare with.
        /// </param>
        /// <returns>
        /// Returns true is data starts with Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            return this.ExactMatchEvaluate(data, TextStartsWithCharactersRegexPattern, TextStartsWithWordsRegexPattern);
        }
    }
}
