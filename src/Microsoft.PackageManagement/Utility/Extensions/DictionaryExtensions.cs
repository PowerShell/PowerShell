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

namespace Microsoft.PackageManagement.Internal.Utility.Extensions {
    using System;
    using System.Collections.Generic;

    internal static class DictionaryExtensions {
        public static Tuple<T1, T2> Add<TKey, T1, T2>(this IDictionary<TKey, Tuple<T1, T2>> dictionary, TKey key, T1 v1, T2 v2) {
            var item = new Tuple<T1, T2>(v1, v2);
            dictionary.Add(key, item);
            return item;
        }

        public static void RemoveSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            lock (dictionary) {
                if (dictionary.ContainsKey(key)) {
                    dictionary.Remove(key);
                }
            }
        }

        public static TValue AddOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
            lock (dictionary) {
                if (dictionary.ContainsKey(key)) {
                    dictionary[key] = value;
                } else {
                    dictionary.Add(key, value);
                }
            }
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFunction) {
            lock (dictionary) {
                return dictionary.ContainsKey(key) ? dictionary[key] : dictionary.AddOrSet(key, valueFunction());
            }
        }

        public static TValue GetOrSetIfDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFunction) {
            lock (dictionary) {
                return !dictionary.ContainsKey(key) || ((object)default(TValue) == (object)dictionary[key]) ? dictionary.AddOrSet(key, valueFunction()) : dictionary[key];
            }
        }

        public static TValue TryPullValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            if (dictionary.ContainsKey(key)) {
                var v = dictionary[key];
                dictionary.Remove(key);
                return v;
            }
            return default(TValue);
        }

        public static Dictionary<TKey, TElement> ToDictionaryNicely<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null) {
                throw new ArgumentNullException("keySelector");
            }
            if (elementSelector == null) {
                throw new ArgumentNullException("elementSelector");
            }

            var d = new Dictionary<TKey, TElement>(comparer);
            foreach (var element in source) {
                var key = keySelector(element);
                if (key != null) {
                    d.AddOrSet(key, elementSelector(element));
                }
            }

            return d;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);
        }
    }
}