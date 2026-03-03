// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The TextDoesNotContainFilterRule class evaluates a string item to
    /// check if it is does not contain the rule's value within it.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class TextDoesNotContainFilterRule : TextContainsFilterRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextDoesNotContainFilterRule"/> class.
        /// </summary>
        public TextDoesNotContainFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotContain;
            this.DefaultNullValueEvaluation = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextDoesNotContainFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public TextDoesNotContainFilterRule(TextDoesNotContainFilterRule source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if Value is not contained within data.
        /// </summary>
        /// <param name="data">
        /// The data to compare with.
        /// </param>
        /// <returns>
        /// Returns true if data does not contain Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
