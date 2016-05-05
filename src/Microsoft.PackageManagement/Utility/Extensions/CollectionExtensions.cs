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
    using System.Reflection;
    using System.Threading.Tasks;
    using Collections;

    internal static class CollectionExtensions {
        private static readonly MethodInfo _castMethod = typeof (Enumerable).GetMethod("Cast");
        private static readonly MethodInfo _toArrayMethod = typeof (Enumerable).GetMethod("ToArray");
        private static readonly IDictionary<Type, MethodInfo> _castMethods = new Dictionary<Type, MethodInfo>();
        private static readonly IDictionary<Type, MethodInfo> _toArrayMethods = new Dictionary<Type, MethodInfo>();

        /// <summary>
        ///     Determines whether the collection object is either null or an empty collection.
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="collection"> The collection. </param>
        /// <returns>
        ///     <c>true</c> if [is null or empty] [the specified collection]; otherwise, <c>false</c> .
        /// </returns>
        /// <remarks>
        /// </remarks>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) {
            return collection == null || !collection.Any();
        }

        /// <summary>
        ///     Concatenates a single item to an IEnumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IEnumerable<T> ConcatSingleItem<T>(this IEnumerable<T> enumerable, T item) {
            return enumerable.Concat(new[] {
                item
            });
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable) {
            return enumerable == null ? Enumerable.Empty<T>() : enumerable.Where(each => (object)each != null);
        }

        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator) {
            if (enumerator != null) {
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> set1, IEnumerator<T> set2) {
            var s1 = set1 ?? Enumerable.Empty<T>();
            var s2 = set2 == null ? Enumerable.Empty<T>() : set2.ToIEnumerable();
            return s1.Concat(s2);
        }

        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item) {
            return new T[] {
                item
            };
        }

        public static void AddRangeLocked<T>(this List<T> list, IEnumerable<T> items) {
            if (list == null) {
                throw new ArgumentNullException("list");
            }
            lock (list) {
                list.AddRange(items);
            }
        }

        public static void ParallelForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
            var items = enumerable.ReEnumerable();
            var first = items.FirstOrDefault();
            if (first != null) {
                object second = items.Skip(1).FirstOrDefault();
                if (second != null) {
                    Parallel.ForEach(items, new ParallelOptions {
                        MaxDegreeOfParallelism = -1,
                        TaskScheduler = new ThreadPerTaskScheduler()
                    }, action);
                } else {
                    action(first);
                }
            }
        }

        public static void SerialForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
            var items = enumerable.ReEnumerable();
            object first = items.FirstOrDefault();
            if (first != null) {
                object second = items.Skip(1).FirstOrDefault();
                if (second != null) {
                    foreach (var item in items) {
                        action(item);
                    }
                } else {
                    action(items.FirstOrDefault());
                }
            }
        }

        public static IEnumerable<Task> AsyncForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
            return enumerable.Select(i => Task.Factory.StartNew(() => action(i)));
        }

        public static void WaitAll(this IEnumerable<Task> tasks) {
            Task.WaitAll(tasks.ToArray());
        }

        public static object ToIEnumerableT(this IEnumerable<object> enumerable, Type elementType) {
            return _castMethods.GetOrAdd(elementType, () => _castMethod.MakeGenericMethod(elementType)).Invoke(null, new object[] {enumerable});
        }

        public static object ToArrayT(this IEnumerable<object> enumerable, Type elementType) {
            return _toArrayMethods.GetOrAdd(elementType, () => _toArrayMethod.MakeGenericMethod(elementType)).Invoke(null, new[] {enumerable.ToIEnumerableT(elementType)});
        }

        /// <summary>
        ///     returns the index position where the predicate matches the value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int IndexWhere<T>(this IEnumerable<T> source, Func<T, int, bool> predicate) {
            if (source != null && predicate != null) {
                var index = -1;
                if (source.Any(element => predicate(element, ++index))) {
                    return index;
                }
            }
            return -1;
        }

        /// <summary>
        ///     returns the index position where the predicate matches the value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int IndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
            if (source != null && predicate != null) {
                var index = -1;
                if (source.Any(element => {
                    ++index;
                    return predicate(element);
                })) {
                    return index;
                }
            }
            return -1;
        }
    }

    // <summary>Provides a task scheduler that dedicates a thread per task.</summary>
}