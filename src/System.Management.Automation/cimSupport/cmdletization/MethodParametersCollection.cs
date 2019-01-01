// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Cmdletization
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Collection of method parameters and their arguments
    /// used to invoke a method in an object model wrapped by <see cref="CmdletAdapter&lt;TObjectInstance&gt;"/>
    /// </summary>
    internal sealed class MethodParametersCollection : KeyedCollection<string, MethodParameter>
    {
        /// <summary>
        /// Creates an empty collection of method parameters.
        /// </summary>
        public MethodParametersCollection()
            : base(StringComparer.Ordinal, 5)
        {
        }

        /// <summary>
        /// Gets key for a method parameter.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override string GetKeyForItem(MethodParameter item)
        {
            return item.Name;
        }
    }
}
