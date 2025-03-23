// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The DoesNotEqualFilterRule class evaluates an IComparable item to
    /// check if it is not equal to the rule's value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class DoesNotEqualFilterRule<T> : EqualsFilterRule<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the DoesNotEqualFilterRule class.
        /// </summary>
        public DoesNotEqualFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotEqual;
            this.DefaultNullValueEvaluation = true;
        }

        /// <summary>
        /// Determines if item is not equal to Value.
        /// </summary>
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is not equal to Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            return !base.Evaluate(data);
        }
    }
}
