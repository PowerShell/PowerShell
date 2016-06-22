//-----------------------------------------------------------------------
// <copyright file="FilterRuleExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// The FilterRuleExtensions class provides extension methods
    /// for FilterRule classes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class FilterRuleExtensions
    {
        /// <summary>
        /// Creates a deep copy of a FilterRule.
        /// </summary>
        /// <param name="rule">
        /// The FilterRule to clone.
        /// </param>
        /// <returns>
        /// Returns a deep copy of the passed in rule.
        /// </returns>
        public static FilterRule DeepCopy(this FilterRule rule)
        {
            if (null == rule)
            {
                throw new ArgumentNullException("rule");
            }

            Debug.Assert(rule.GetType().IsSerializable);

            BinaryFormatter formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
            MemoryStream ms = new MemoryStream();

            FilterRule copy = null;
            try
            {
                formatter.Serialize(ms, rule);

                ms.Position = 0;
                copy = (FilterRule)formatter.Deserialize(ms);
            }
            finally
            {
                ms.Close();
            }

            return copy;
        }
    }
}
