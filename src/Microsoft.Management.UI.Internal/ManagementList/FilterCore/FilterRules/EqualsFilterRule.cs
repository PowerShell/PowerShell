// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The EqualsFilterRule class evaluates an IComparable item to
    /// check if it is equal to the rule's value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class EqualsFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the EqualsFilterRule class.
        /// </summary>
        public EqualsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Equals;
        }

        /// <summary>
        /// Determines if item is equal to Value.
        /// </summary>
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is equal to Value.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "isValid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), data);
            return result == 0;
        }
    }
}
