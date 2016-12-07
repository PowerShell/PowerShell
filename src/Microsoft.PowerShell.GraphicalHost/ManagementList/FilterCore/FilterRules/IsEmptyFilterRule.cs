//-----------------------------------------------------------------------
// <copyright file="IsEmptyFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;

    /// <summary>
    /// The IsEmptyFilterRule evaluates an item to determine whether it
    /// is empty or not.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsEmptyFilterRule : FilterRule
    {
        /// <summary>
        /// Initializes a new instance of the IsEmptyFilterRule class.
        /// </summary>
        public IsEmptyFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_IsEmpty;
        }

        /// <summary>
        /// Gets a values indicating whether the supplied item is empty.
        /// </summary>
        /// <param name="item">The item to evaluate.</param>
        /// <returns>
        /// Returns true if the item is null or if the item is a string
        /// composed of whitespace. False otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            if (null == item)
            {
                return true;
            }

            Type type = item.GetType();

            if (typeof(string) == type)
            {
                return 0 == ((string)item).Trim().Length;
            }

            return false;
        }
    }
}
