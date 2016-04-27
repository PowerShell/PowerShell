/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell
{
    [ExcludeFromCodeCoverage]
    internal sealed class QueueDebugView<T>
    {
        private readonly HistoryQueue<T> _queue;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get { return this._queue.ToArray(); }
        }

        public QueueDebugView(HistoryQueue<T> queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            this._queue = queue;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(QueueDebugView<>))]
    internal class HistoryQueue<T>
    {
        private readonly T[] _array;
        private int _head;
        private int _tail;

        public HistoryQueue(int capacity)
        {
            Debug.Assert(capacity > 0);
            _array = new T[capacity];
            _head = _tail = Count = 0;
        }

        public void Clear()
        {
            for (int i = 0; i < Count; i++)
            {
                this[i] = default(T);
            }
            _head = _tail = Count = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public int Count { get; private set; }

        public int IndexOf(T item)
        {
            // REVIEW: should we use case insensitive here?
            var eqComparer = EqualityComparer<T>.Default;
            for (int i = 0; i < Count; i++)
            {
                if (eqComparer.Equals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Enqueue(T item)
        {
            if (Count == _array.Length)
            {
                Dequeue();
            }
            _array[_tail] = item;
            _tail = (_tail + 1) % _array.Length;
            Count += 1;
        }

        public T Dequeue()
        {
            Debug.Assert(Count > 0);

            T obj = _array[_head];
            _array[_head] = default(T);
            _head = (_head + 1) % _array.Length;
            Count -= 1;
            return obj;
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            if (Count > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, result, 0, Count);
                }
                else
                {
                    Array.Copy(_array, _head, result, 0, _array.Length - _head);
                    Array.Copy(_array, 0, result, _array.Length - _head, _tail);
                }
            }
            return result;
        }

        [ExcludeFromCodeCoverage]
        public T this[int index]
        {
            get
            { 
                Debug.Assert(index >= 0 && index < Count);
                return _array[(_head + index) % _array.Length];
            }
            set
            {
                Debug.Assert(index >= 0 && index < Count);
                _array[(_head + index) % _array.Length] = value;
            }
        }
    }
}
