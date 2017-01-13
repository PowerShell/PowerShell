//-----------------------------------------------------------------------
// <copyright file="IsLessThanFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// The IsLessThanFilterRule class evaluates an IComparable item to
    /// check if it is less than the rule's value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsLessThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the IsLessThanFilterRule class.
        /// </summary>
        public IsLessThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_LessThanOrEqual;
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
            Debug.Assert(this.IsValid);

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), item);
            return (result >= 0);
        }
    }
}
