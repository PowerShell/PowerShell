// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Utility.Collections {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    internal class OrderedDictionary<TKey, TValue> : OrderedDictionary, IDictionary<TKey, TValue> {
        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return new KvpEnumerator(base.GetEnumerator());
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            base.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return base.Contains(item.Key) && item.Value.Equals(base[item.Key]);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            for (var e = GetEnumerator(); e.MoveNext();) {
                array[arrayIndex++] = e.Current;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            if (Contains(item)) {
                base.Remove(item.Key);
                return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key) {
            return base.Contains(key);
        }

        public void Add(TKey key, TValue value) {
            base.Add(key, value);
        }

        public bool Remove(TKey key) {
            if (base.Contains(key)) {
                base.Remove(key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            if (base.Contains(key)) {
                value = (TValue)base[key];
                return true;
            }
            value = default(TValue);
            return false;
        }

        public TValue this[TKey key] {
            get {
                return (TValue)base[key];
            }
            set {
                base[key] = value;
            }
        }

        public new ICollection<TKey> Keys {
            get {
                return new KeyCollection(this);
            }
        }

        public new ICollection<TValue> Values {
            get {
                return new ValueCollection(this);
            }
        }

        internal class KeyCollection : ICollection<TKey> {
            private readonly OrderedDictionary _dictionary;

            public KeyCollection(OrderedDictionary dictionary) {
                _dictionary = dictionary;
            }

            public IEnumerator<TKey> GetEnumerator() {
                return _dictionary.Keys.Cast<TKey>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public void Add(TKey item) {
                throw new NotImplementedException();
            }

            public void Clear() {
                throw new NotImplementedException();
            }

            public bool Contains(TKey item) {
                return _dictionary.Contains(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex) {
                _dictionary.Keys.CopyTo(array, arrayIndex);
            }

            public bool Remove(TKey item) {
                throw new NotImplementedException();
            }

            public int Count {
                get {
                    return _dictionary.Keys.Count;
                }
            }

            public bool IsReadOnly {
                get {
                    return true;
                }
            }
        }

        internal class KvpEnumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            private readonly IDictionaryEnumerator _enumerator;

            internal KvpEnumerator(IDictionaryEnumerator e) {
                _enumerator = e;
            }

            public void Dispose() {
            }

            public bool MoveNext() {
                return _enumerator.MoveNext();
            }

            public void Reset() {
                _enumerator.Reset();
            }

            public KeyValuePair<TKey, TValue> Current {
                get {
                    return new KeyValuePair<TKey, TValue>((TKey)_enumerator.Key, (TValue)_enumerator.Value);
                }
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }
        }

        internal class ValueCollection : ICollection<TValue> {
            private readonly OrderedDictionary _dictionary;

            public ValueCollection(OrderedDictionary dictionary) {
                _dictionary = dictionary;
            }

            public IEnumerator<TValue> GetEnumerator() {
                return _dictionary.Values.Cast<TValue>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public void Add(TValue item) {
                throw new NotImplementedException();
            }

            public void Clear() {
                throw new NotImplementedException();
            }

            public bool Contains(TValue item) {
                return _dictionary.Values.Cast<TValue>().Contains(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex) {
                _dictionary.Values.CopyTo(array, arrayIndex);
            }

            public bool Remove(TValue item) {
                throw new NotImplementedException();
            }

            public int Count {
                get {
                    return _dictionary.Values.Count;
                }
            }

            public bool IsReadOnly {
                get {
                    return true;
                }
            }
        }
    }
}