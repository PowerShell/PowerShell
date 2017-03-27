//-----------------------------------------------------------------------
// <copyright file="TextEqualsFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// The TextEqualsFilterRule class evaluates a string item to
    /// check if it is equal to the rule's value.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextEqualsFilterRule : TextFilterRule
    {
        private static readonly string TextEqualsCharactersRegexPattern = "^{0}$";

        /// <summary>
        /// Initializes a new instance of the TextEqualsFilterRule class.
        /// </summary>
        public TextEqualsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Equals;
        }

        /// <summary>
        /// Determines if data is equal to Value.
        /// </summary>
        /// <param name="data">
        /// The value to compare against.
        /// </param>
        /// <returns>
        /// Returns true is data equals Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid);

            return this.ExactMatchEvaluate(data, TextEqualsCharactersRegexPattern, TextEqualsCharactersRegexPattern);
        }
    }
}
