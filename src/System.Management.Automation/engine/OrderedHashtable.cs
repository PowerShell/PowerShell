// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;

#nullable enable

namespace System.Management.Automation
{
    /// <summary>
    /// OrderedHashtable is a hashtable that preserves the order of the keys.
    /// </summary>
    public sealed class OrderedHashtable : Hashtable, IEnumerable
    {
        private readonly OrderedDictionary _orderedDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedHashtable"/> class.
        /// </summary>
        public OrderedHashtable()
        {
            _orderedDictionary = new OrderedDictionary();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedHashtable"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public OrderedHashtable(int capacity) : base(capacity)
        {
            _orderedDictionary = new OrderedDictionary(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedHashtable"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to use for initialization.</param>
        public OrderedHashtable(IDictionary dictionary)
        {
            _orderedDictionary = new OrderedDictionary(dictionary.Count);
            foreach (DictionaryEntry entry in dictionary)
            {
                _orderedDictionary.Add(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Get the number of items in the hashtable.
        /// </summary>
        public override int Count
        {
            get
            {
                return _orderedDictionary.Count;
            }
        }

        /// <summary>
        /// Get if the hashtable is a fixed size.
        /// </summary>
        public override bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Get if the hashtable is read-only.
        /// </summary>
        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Get if the hashtable is synchronized.
        /// </summary>
        public override bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the keys in the hashtable.
        /// </summary>
        public override ICollection Keys
        {
            get
            {
                return _orderedDictionary.Keys;
            }
        }

        /// <summary>
        /// Gets the values in the hashtable.
        /// </summary>
        public override ICollection Values
        {
            get
            {
                return _orderedDictionary.Values;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated with the key.</returns>
        public override object? this[object key]
        {
            get
            {
                return _orderedDictionary[key];
            }

            set
            {
                _orderedDictionary[key] = value;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the hashtable.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override void Add(object key, object? value)
        {
            _orderedDictionary.Add(key, value);
        }

        /// <summary>
        /// Removes all keys and values from the hashtable.
        /// </summary>
        public override void Clear()
        {
            _orderedDictionary.Clear();
        }

        /// <summary>
        /// Get a shallow clone of the hashtable.
        /// </summary>
        /// <returns>A shallow clone of the hashtable.</returns>
        public override object Clone()
        {
            return new OrderedHashtable(_orderedDictionary);
        }

        /// <summary>
        /// Determines whether the hashtable contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        /// <summary>
        /// Determines whether the hashtable contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified key; otherwise, false.</returns>
        public override bool ContainsKey(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        /// <summary>
        /// Determines whether the hashtable contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified value; otherwise, false.</returns>
        public override bool ContainsValue(object? value)
        {
            foreach (DictionaryEntry entry in _orderedDictionary)
            {
                if (Equals(entry.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the elements of the hashtable to an array of type object, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the hashtable. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public override void CopyTo(Array array, int arrayIndex)
        {
            _orderedDictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Get the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return _orderedDictionary.GetEnumerator();
        }

        /// <summary>
        /// Get the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the data needed to seralize the Hashtable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            _orderedDictionary.GetObjectData(info, context);
        }

        /// <summary>
        /// Removes the specified key from the hashtable.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public override void Remove(object key)
        {
            _orderedDictionary.Remove(key);
        }
    }
}
