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
    using System.Linq;

    internal static class Singleton<TResult> {
        private static readonly Dictionary<int, TResult> _singletons = new Dictionary<int, TResult>();

        private static readonly int[] _seeds = {
            53, 379, 1301, 2333, 2357, 2539, 3121, 1009, 419, 3559
        };

        /// <summary>
        ///     Create a pretty good hashcode using the hashcodes from a bunch of objects.
        /// </summary>
        public static int CreateHashCode(object[] keys) {
            if (keys.Length == 0) {
                return 0;
            }

            var hash = _seeds.Zip(keys, (prime, obj) => prime*(obj == null ? 0 : obj.GetHashCode())).Aggregate((accum, each) => accum + each);

            return (keys.Length <= _seeds.Length) ? hash : keys.Skip(10).Aggregate(hash, (accum, obj) => accum + (obj == null ? 0 : obj.GetHashCode()));
        }

        /// <summary>
        ///     Create a pretty good hashcode using the hashcodes from a bunch of objects.
        /// </summary>
        public static int CreateHashCode(object primaryKey, object[] keys) {
            if (primaryKey == null) {
                return CreateHashCode(keys);
            }

            if (keys.Length == 0) {
                return primaryKey.GetHashCode();
            }

            var hash = _seeds.Zip(keys, (prime, obj) => prime*(obj == null ? 0 : obj.GetHashCode())).Aggregate((accum, each) => accum + each) + primaryKey.GetHashCode();

            return (keys.Length <= _seeds.Length) ? hash : keys.Skip(10).Aggregate(hash, (accum, obj) => accum + (obj == null ? 0 : obj.GetHashCode()));
        }

        public static TResult GetOrAdd(Func<TResult> newInstance, object primaryKey, params object[] keys) {
            return _singletons.GetOrAdd(CreateHashCode(primaryKey, keys), newInstance);
        }

        public static TResult Get(object primaryKey, params object[] keys) {
            var hash = CreateHashCode(primaryKey, keys);
            return _singletons.ContainsKey(hash) ? _singletons[hash] : default(TResult);
        }

        public static TResult AddOrSet(TResult value, object primaryKey, params object[] keys) {
            return _singletons.AddOrSet(CreateHashCode(primaryKey, keys), value);
        }

        public static void Remove(object primaryKey, params object[] keys) {
            _singletons.Remove(CreateHashCode(primaryKey, keys));
        }
    }

    internal static class SingletonExtensions {
        public static TResult Get<TResult>(this object primaryKey, params object[] keys) {
            return Singleton<TResult>.Get(primaryKey, keys);
        }

        public static TResult GetOrAdd<TResult>(this object primaryKey, Func<TResult> newInstance, params object[] keys) {
            return Singleton<TResult>.GetOrAdd(newInstance, primaryKey, keys);
        }

        public static TResult AddOrSet<TResult>(this object primaryKey, TResult value, params object[] keys) {
            return Singleton<TResult>.AddOrSet(value, primaryKey, keys);
        }

        public static void Remove<TResult>(this object primaryKey, params object[] keys) {
            Singleton<TResult>.Remove(primaryKey, keys);
        }
    }
}