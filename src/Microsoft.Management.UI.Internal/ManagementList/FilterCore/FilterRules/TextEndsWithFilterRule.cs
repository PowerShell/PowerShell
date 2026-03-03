// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The TextEndsWithFilterRule class evaluates a string item to
    /// check if it ends with the rule's value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class TextEndsWithFilterRule : TextFilterRule
    {
        private static readonly string TextEndsWithCharactersRegexPattern = "{0}$";
        private static readonly string TextEndsWithWordsRegexPattern = WordBoundaryRegexPattern + TextEndsWithCharactersRegexPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextEndsWithFilterRule"/> class.
        /// </summary>
        public TextEndsWithFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_TextEndsWith;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextEndsWithFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public TextEndsWithFilterRule(TextEndsWithFilterRule source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if data ends with Value.
        /// </summary>
        /// <param name="data">
        /// The value to compare with.
        /// </param>
        /// <returns>
        /// Returns true is data ends with Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            return this.ExactMatchEvaluate(data, TextEndsWithCharactersRegexPattern, TextEndsWithWordsRegexPattern);
        }
    }
}
