/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

namespace Microsoft.Management.Infrastructure.Generic
{
    /// <summary>
    /// Represents a mutable collection of <typeparamref name="T"/> objects.
    /// The objects can be enumerated (the order is undefined) or can be accessed by their name.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    public abstract class CimKeyedCollection<T> : CimReadOnlyKeyedCollection<T>
    {
        internal CimKeyedCollection()
        {
        }

        /// <summary>
        /// Adds <paramref name="newItem"/> to the collection.
        /// </summary>
        /// <param name="newItem">Item to add</param>
        public abstract void Add(T newItem);
    }
}