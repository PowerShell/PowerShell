// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IsGreaterThanFilterRule class evaluates an IComparable item to
    /// check if it is greater than its value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsGreaterThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsGreaterThanFilterRule{T}"/> class.
        /// </summary>
        public IsGreaterThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_GreaterThanOrEqual;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsGreaterThanFilterRule{T}"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public IsGreaterThanFilterRule(IsGreaterThanFilterRule<T> source)
            : base(source)
        {
        }

        /// <summary>
        /// Determines if item is greater than Value.
        /// </summary>
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is greater than Value.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "is valid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), data);
            return result <= 0;
        }
    }
}
