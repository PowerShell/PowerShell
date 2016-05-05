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
    using System.Collections;
    using System.Collections.Generic;

    internal abstract class MutableEnumerable<T> : IEnumerable<T> {
        private List<T> _list;

        protected List<T> List {
            get {
                if (_list == null) {
                    lock (this) {
                        if (_list == null) {
                            _list = new List<T>();
                        }
                    }
                }
                return _list;
            }
        }

        public IEnumerable<IEnumerator<T>> GetEnumerators(int copies) {
            for (var i = 0; i < copies; i++) {
                yield return GetEnumerator();
            }
        }

        protected abstract bool ItemExists(int index);

        internal class Enumerator<TT> : IEnumerator<TT> {
            private MutableEnumerable<TT> _collection;
            private int _index = -1;

            internal Enumerator(MutableEnumerable<TT> collection) {
                _collection = collection;
            }

            #region IEnumerator<Tt> Members

            public TT Current {
                get {
                    return _collection.List[_index];
                }
            }

            public void Dispose() {
                _collection = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                _index++;
                return _collection.ItemExists(_index);
            }

            public void Reset() {
                _index = -1;
            }

            public IEnumerator<TT> Clone() {
                return new Enumerator<TT>(_collection) {
                    _index = _index
                };
            }

            #endregion
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion
    }
}