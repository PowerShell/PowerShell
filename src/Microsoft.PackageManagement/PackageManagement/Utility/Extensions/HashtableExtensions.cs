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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal static class HashtableExtensions {
        public static bool IsNullOrEmpty(this Hashtable hashtable) {
            return hashtable == null || hashtable.Count == 0;
        }

        public static IEnumerable<string> GetStringCollection(this Hashtable hashtable, string path) {
            if (hashtable.IsNullOrEmpty() || string.IsNullOrWhiteSpace(path)) {
                return Enumerable.Empty<string>();
            }
            return hashtable.GetStringCollection(path.Split('/').Select(each => each.Trim()).ToArray());
        }

        public static IEnumerable<string> GetStringCollection(this Hashtable hashtable, string[] paths) {
            if (hashtable.IsNullOrEmpty() || paths.IsNullOrEmpty() || !hashtable.ContainsKey(paths[0])) {
                return Enumerable.Empty<string>();
            }

            if (paths.Length == 1) {
                // looking for the actual result value
                var items = hashtable[paths[0]] as IEnumerable<object>;
                if (items != null) {
                    return items.Select(each => each.ToString());
                }

                return new[] {
                    (hashtable[paths[0]] ?? "").ToString()
                };
            }

            var item = hashtable[paths[0]] as Hashtable;
            return item == null ? Enumerable.Empty<string>() : item.GetStringCollection(paths.Skip(1).ToArray());
        }

        public static IEnumerable<KeyValuePair<string, string>> Flatten(this Hashtable hashTable) {
            foreach (var k in hashTable.Keys) {
                var value = hashTable[k];
                if (value == null) {
                    continue;
                }

                if (value is Hashtable) {
                    foreach (var kvp in (value as Hashtable).Flatten()) {
                        yield return new KeyValuePair<string, string>(k.ToString() + "/" + kvp.Key, kvp.Value);
                    }
                } else {
                    yield return new KeyValuePair<string, string>(k.ToString(), value.ToString());
                }
            }
        }
    }
}