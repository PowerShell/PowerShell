//---------------------------------------------------------------------
// <copyright file="HandleManager.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Cab
{
    using System.Collections.Generic;

    /// <summary>
    /// Generic class for managing allocations of integer handles
    /// for objects of a certain type.
    /// </summary>
    /// <typeparam name="T">The type of objects the handles refer to.</typeparam>
    internal sealed class HandleManager<T> where T : class
    {
        /// <summary>
        /// Auto-resizing list of objects for which handles have been allocated.
        /// Each handle is just an index into this list. When a handle is freed,
        /// the list item at that index is set to null.
        /// </summary>
        private List<T> handles;

        /// <summary>
        /// Creates a new HandleManager instance.
        /// </summary>
        public HandleManager()
        {
            this.handles = new List<T>();
        }

        /// <summary>
        /// Gets the object of a handle, or null if the handle is invalid.
        /// </summary>
        /// <param name="handle">The integer handle previously allocated
        /// for the desired object.</param>
        /// <returns>The object for which the handle was allocated.</returns>
        public T this[int handle]
        {
            get
            {
                if (handle > 0 && handle <= this.handles.Count)
                {
                    return this.handles[handle - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Allocates a new handle for an object.
        /// </summary>
        /// <param name="obj">Object that the handle will refer to.</param>
        /// <returns>New handle that can be later used to retrieve the object.</returns>
        public int AllocHandle(T obj)
        {
            this.handles.Add(obj);
            int handle = this.handles.Count;
            return handle;
        }

        /// <summary>
        /// Frees a handle that was previously allocated. Afterward the handle
        /// will be invalid and the object it referred to can no longer retrieved.
        /// </summary>
        /// <param name="handle">Handle to be freed.</param>
        public void FreeHandle(int handle)
        {
            if (handle > 0 && handle <= this.handles.Count)
            {
                this.handles[handle - 1] = null;
            }
        }
    }
}
