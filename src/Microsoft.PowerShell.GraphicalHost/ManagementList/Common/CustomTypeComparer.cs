//-----------------------------------------------------------------------
// <copyright file="CustomTypeComparer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// The CustomTypeComparer is responsible for holding custom comparers
    /// for different types, which are in turn used to perform comparison
    /// operations instead of the default IComparable comparison.
    /// with a custom comparer
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class CustomTypeComparer
    {
        private static Dictionary<Type, object> comparers = new Dictionary<Type, object>();

        /// <summary>
        /// The static constructor.
        /// </summary>
        static CustomTypeComparer()
        {
            comparers.Add(typeof(DateTime), new DateTimeApproximationComparer());
        }

        /// <summary>
        /// Compares two objects and returns a value indicating
        /// whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="value1">
        /// The first object to compare.
        /// </param>
        /// <param name="value2">
        /// The second object to compare.
        /// </param>
        /// <typeparam name="T">
        /// A type implementing IComparable.
        /// </typeparam>
        /// <returns>
        /// If value1 is less than value2, then a value less than zero is returned.
        /// If value1 equals value2, than zero is returned.
        /// If value1 is greater than value2, then a value greater than zero is returned.
        /// </returns>
        public static int Compare<T>(T value1, T value2) where T : IComparable
        {
            IComparer<T> comparer;
            if (false == TryGetCustomComparer<T>(out comparer))
            {
                return value1.CompareTo(value2);
            }

            return comparer.Compare(value1, value2);
        }

        private static bool TryGetCustomComparer<T>(out IComparer<T> comparer) where T : IComparable
        {
            comparer = null;

            object uncastComparer = null;
            if (false == comparers.TryGetValue(typeof(T), out uncastComparer))
            {
                return false;
            }

            Debug.Assert(uncastComparer is IComparer<T>);
            comparer = (IComparer<T>)uncastComparer;

            return true;
        }
    }
}
