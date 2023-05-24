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
        [Obsolete("Legacy serialization support is deprecated since .NET 8, hence this method is now marked as obsolete", DiagnosticId = "SYSLIB0051")]
        public static FilterRule DeepCopy(this FilterRule rule)
        {
            throw new NotSupportedException();
        }
    }
}
