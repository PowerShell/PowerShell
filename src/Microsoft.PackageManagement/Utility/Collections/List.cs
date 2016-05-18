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
    using System.Collections.Generic;

    public class List<TKey, TValue> : List<KeyValuePair<TKey, TValue>> {
        public void Add(TKey key, TValue value) {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public int IndexOf(TKey key, TValue value) {
            for (var i = 0; i < Count; i++) {
                if ((ReferenceEquals(this[i].Key, key) || this[i].Key.Equals(key)) && (ReferenceEquals(this[i].Value, value) || this[i].Value.Equals(value))) {
                    return i;
                }
            }
            return -1;
        }

        public bool Contains(TKey key, TValue value) {
            return IndexOf(key, value) > -1;
        }

        public bool Remove(TKey key, TValue value) {
            var i = IndexOf(key, value);
            if (i > -1) {
                RemoveAt(i);
                return true;
            }
            return false;
        }
    }

    public class List<T1, T2, T3> : List<Tuple<T1, T2, T3>> {
        public void Add(T1 p1, T2 p2, T3 p3) {
            Add(new Tuple<T1, T2, T3>(p1, p2, p3));
        }

        public bool Contains(T1 p1, T2 p2, T3 p3) {
            return Contains(new Tuple<T1, T2, T3>(p1, p2, p3));
        }

        public bool Remove(T1 p1, T2 p2, T3 p3) {
            return Remove(new Tuple<T1, T2, T3>(p1, p2, p3));
        }
    }

    public class List<T1, T2, T3, T4> : List<Tuple<T1, T2, T3, T4>> {
        public void Add(T1 p1, T2 p2, T3 p3, T4 p4) {
            Add(new Tuple<T1, T2, T3, T4>(p1, p2, p3, p4));
        }

        public bool Contains(T1 p1, T2 p2, T3 p3, T4 p4) {
            return Contains(new Tuple<T1, T2, T3, T4>(p1, p2, p3, p4));
        }

        public bool Remove(T1 p1, T2 p2, T3 p3, T4 p4) {
            return Remove(new Tuple<T1, T2, T3, T4>(p1, p2, p3, p4));
        }
    }

    public class List<T1, T2, T3, T4, T5> : List<Tuple<T1, T2, T3, T4, T5>> {
        public void Add(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) {
            Add(new Tuple<T1, T2, T3, T4, T5>(p1, p2, p3, p4, p5));
        }

        public bool Contains(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) {
            return Contains(new Tuple<T1, T2, T3, T4, T5>(p1, p2, p3, p4, p5));
        }

        public bool Remove(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) {
            return Remove(new Tuple<T1, T2, T3, T4, T5>(p1, p2, p3, p4, p5));
        }
    }
}