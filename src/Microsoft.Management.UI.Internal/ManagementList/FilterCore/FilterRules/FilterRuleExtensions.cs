// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Management.UI.Internal
{
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
            ArgumentNullException.ThrowIfNull(rule);

            Debug.Assert(rule.GetType().IsSerializable, "rule is serializable");

            BinaryFormatter formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
            MemoryStream ms = new MemoryStream();

            FilterRule copy = null;
            try
            {
#pragma warning disable SYSLIB0011
                formatter.Serialize(ms, rule);
#pragma warning restore SYSLIB0011

                ms.Position = 0;
#pragma warning disable SYSLIB0011
                copy = (FilterRule)formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011
            }
            finally
            {
                ms.Close();
            }

            return copy;
        }
    }
}
