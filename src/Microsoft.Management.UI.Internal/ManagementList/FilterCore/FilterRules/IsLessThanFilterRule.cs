// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IsLessThanFilterRule class evaluates an IComparable item to
    /// check if it is less than the rule's value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class IsLessThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsLessThanFilterRule{T}"/> class.
        /// </summary>
        public IsLessThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_LessThanOrEqual;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsLessThanFilterRule{T}"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public IsLessThanFilterRule(IsLessThanFilterRule<T> source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if item is less than Value.
        /// </summary>
        /// <param name="item">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is less than Value.
        /// </returns>
        protected override bool Evaluate(T item)
        {
            Debug.Assert(this.IsValid, "is valid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), item);
            return result >= 0;
        }
    }
}
