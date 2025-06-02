// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The TextDoesNotEqualFilterRule class evaluates a string item to
    /// check if it is not equal to the rule's value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextDoesNotEqualFilterRule : TextEqualsFilterRule
    {
        /// <summary>
        /// Initializes a new instance of the TextDoesNotEqualFilterRule class.
        /// </summary>
        public TextDoesNotEqualFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotEqual;
            this.DefaultNullValueEvaluation = true;
        }

        /// <summary>
        /// Determines if data is not equal to Value.
        /// </summary>
        /// <param name="data">
        /// The value to compare against.
        /// </param>
        /// <returns>
        /// Returns true is data does not equal Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
