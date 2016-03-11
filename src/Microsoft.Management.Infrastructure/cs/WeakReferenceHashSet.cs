/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Management.Infrastructure.Internal
{
    /// <summary>
    /// HashSet that 1) uses reference equality, 2) doesn't keep elements alive, 3) is thread-safe
    /// </summary>
    internal class WeakReferenceHashSet<T>
        where T : class
    {
        private class WeakReferenceEqualityComparer : IEqualityComparer<WeakReference>
        {
            public bool Equals(WeakReference x, WeakReference y)
            {
                object tx = x.Target;
                if (tx == null)
                {
                    return false; // collected object is not equal to anything (object.ReferenceEquals(null, null) == true)
                }

                object ty = y.Target;
                if (ty == null)
                {
                    return false; // collected object is not equal to anything (object.ReferenceEquals(null, null) == true)
                }

                return object.ReferenceEquals(tx, ty);
            }

            public int GetHashCode(WeakReference obj)
            {
                object t = obj.Target;
                if (t == null)
                {
                    // collected object doesn't have a hash code
                    // return an arbitrary hashcode here and fall back on Equal method for comparison
                    return RuntimeHelpers.GetHashCode(obj); // RuntimeHelpers.GetHashCode(null) returns 0 - this would cause many hashtable colisions for WeakReferences to dead objects
                }
                else
                {
                    return RuntimeHelpers.GetHashCode(t);
                }
            }

            static public WeakReferenceEqualityComparer Singleton
            {
                get { return LazySingleton.Value; }
            }
            static private readonly Lazy<WeakReferenceEqualityComparer> LazySingleton = new Lazy<WeakReferenceEqualityComparer>(() => new WeakReferenceEqualityComparer());
        }

        private ConcurrentDictionary<WeakReference, object> _underlyingCollection;

        public WeakReferenceHashSet()
        {
            this._underlyingCollection = new ConcurrentDictionary<WeakReference, object>(WeakReferenceEqualityComparer.Singleton);
        }

        public void Add(T o)
        {
            var weakReference = new WeakReference(o);
            bool success = this._underlyingCollection.TryAdd(weakReference, null);
            Debug.Assert(success, "No duplicate adds");
            this.CleanUp();
        }

        public void Remove(T o)
        {
            var weakReference = new WeakReference(o);
            object oldValue;
            this._underlyingCollection.TryRemove(weakReference, out oldValue);
        }

        public IEnumerable<T> GetSnapshotOfLiveObjects()
        {
            return this
                ._underlyingCollection
                .Keys
                .Select(w => w.Target)
                .OfType<T>()
                .Where(t => t != null)
                .ToList();
        }

#if DEBUG
        private const int InitialCleanupTriggerSize = 2; // 2 will stress this code more
#else
        private const int InitialCleanupTriggerSize = 1000;
#endif
        private int _cleanupTriggerSize = InitialCleanupTriggerSize;

        private void CleanUp()
        {
            if (this._underlyingCollection.Count > this._cleanupTriggerSize)
            {
                var aliveObjects = new ConcurrentDictionary<WeakReference, object>(
                    this.GetSnapshotOfLiveObjects().Select(t => new KeyValuePair<WeakReference, object>(new WeakReference(t), null)),
                    WeakReferenceEqualityComparer.Singleton);
                this._underlyingCollection = aliveObjects;
                this._cleanupTriggerSize = InitialCleanupTriggerSize + this._underlyingCollection.Count * 2;
            }
        }

    }
}