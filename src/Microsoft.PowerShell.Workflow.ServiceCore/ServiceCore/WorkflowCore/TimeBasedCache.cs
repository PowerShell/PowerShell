/*
 * Copyright (c) 2011 Microsoft Corporation. All rights reserved
 */
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Timers;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// Bounded cache which will clear items when not used for a while
    /// </summary>
    /// <typeparam name="T">type of item to use</typeparam>
    internal class TimeBasedCache<T> : IEnumerable, IDisposable
    {
        private readonly int _timeoutInSeconds;
        private readonly object _timerServicingSyncObject = new object();
        private readonly Timer _validationTimer;
        private int _timerFired;
        private const int TimerFired = 1;
        private const int TimerReset = 0;
        private readonly ConcurrentDictionary<Guid, Item<T>> _cache = new ConcurrentDictionary<Guid, Item<T>>();

        /// <summary>
        ///
        /// </summary>
        internal ConcurrentDictionary<Guid, Item<T>> Cache
        {
            get { return _cache; }
        }

        /// <summary>
        /// The consumer of this class should hold a lock
        /// on this object when servicing requests and adding
        /// to this cache
        /// </summary>
        internal object TimerServicingSyncObject
        {
            get { return _timerServicingSyncObject; }
        }

        internal TimeBasedCache(int timeoutInSeconds)
        {
            _timeoutInSeconds = timeoutInSeconds;
            _validationTimer = new Timer
                                   {
                                       AutoReset = true,
                                       Interval = _timeoutInSeconds*1000,
                                       Enabled = false
                                   };
            _validationTimer.Elapsed += ValidationTimerElapsed;
            _validationTimer.Start();
        }

        private void ValidationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // ensure that the servicing thread is done before proceeding
            if (_timerFired == TimerFired) return;

            lock(TimerServicingSyncObject)
            {
                if (_timerFired == TimerFired) return;
                _timerFired = TimerFired;
                Collection<Item<T>> toRemove = new Collection<Item<T>>();

                foreach(Item<T> item in _cache.Values)
                {
                    if (item.Idle)
                        toRemove.Add(item);
                    else if (!item.Busy)
                        item.Idle = true;
                }

                foreach(Item<T> item in toRemove)
                {
                    Item<T> removedItem;
                    _cache.TryRemove(item.InstanceId, out removedItem);
                    IDisposable disposable = removedItem.Value as IDisposable;
                    if (disposable == null) continue;

                    //dispose and force early GC
                    disposable.Dispose();
                    disposable = null;
                    removedItem = null;
                }

                _timerFired = TimerReset;
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void Add(Item<T> item)
        {
            item.Busy = true;
            item.Idle = false;
            _cache.TryAdd(item.InstanceId, item);
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _validationTimer.Dispose();
            }
        }

        internal class CacheEnumerator : IEnumerator
        {
            private readonly ConcurrentDictionary<Guid, Item<T>> _cache;
            private readonly IEnumerator _dictionaryEnumerator;
            private Item<T> _currentItem;

            internal CacheEnumerator(ConcurrentDictionary<Guid, Item<T>> cache)
            {
                _cache = cache;
                _dictionaryEnumerator = _cache.Keys.GetEnumerator();
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                if (_dictionaryEnumerator.MoveNext())
                {
                    Guid key = (Guid)_dictionaryEnumerator.Current;
                    _currentItem = default(Item<T>);
                    _cache.TryGetValue(key, out _currentItem);

                    if (_currentItem != null)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                _dictionaryEnumerator.Reset();
            }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <returns>
            /// The current element in the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
            public object Current
            {
                get { return _currentItem; }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public IEnumerator GetEnumerator()
        {
            return new CacheEnumerator(_cache);
        }
    }

    /// <summary>
    /// one item that will be used in the cache
    /// </summary>
    /// <typeparam name="T">type of item to use</typeparam>
    internal class Item<T>
    {
        private readonly Guid _instanceId;
        private readonly T _value;
        internal bool Busy { get; set; }
        internal bool Idle { get; set; }
        internal T Value
        {
            get { return _value; }
        }
        internal Guid InstanceId
        {
            get { return _instanceId; }
        }
        internal Item(T value, Guid instanceId)
        {
            _value = value;
            _instanceId = instanceId;
        }
    }
}
