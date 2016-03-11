/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System.Collections.Generic;

namespace Microsoft.Management.Infrastructure.Generic
{
    /// <summary>
    /// Represents an immutable collection of <typeparamref name="T"/> objects.
    /// The objects can be enumerated (the order is undefined) or can be accessed by their name.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    public abstract class CimReadOnlyKeyedCollection<T> : IEnumerable<T>
    {
        internal CimReadOnlyKeyedCollection()
        {
        }

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets an item with a given <paramref name="itemName"/>
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public abstract T this[string itemName] { get; }

        /// <summary>
        /// Returns an enumerator that returns items from the collectio n
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator<T> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}