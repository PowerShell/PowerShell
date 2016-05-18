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

    internal class EqualityComparer<T> : IEqualityComparer<T> {
        private readonly Func<T, T, bool> _compareFn;
        private readonly Func<T, int> _hashFn;

        public EqualityComparer(Func<T, T, bool> compareFn, Func<T, int> hashFn) {
            _compareFn = compareFn;
            _hashFn = hashFn;
        }

        public EqualityComparer(Func<T, T, bool> compareFn) {
            _compareFn = compareFn;
            _hashFn = (obj) =>obj.GetHashCode();
        }

        public bool Equals(T x, T y) {
            return _compareFn(x, y);
        }

        public int GetHashCode(T obj) {
            return _hashFn(obj);
        }
    }
}