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
    using System.Linq;

    internal class Types {
        private readonly Type _first;
        private readonly Type[] _second;

        public Types(Type first, params Type[] second) {
            _first = first;
            _second = second;
        }

        public override int GetHashCode() {
            return _second.Aggregate(_first.FullName.GetHashCode(), (current, each) => current ^ each.GetHashCode());
        }

        public override bool Equals(object obj) {
            if (obj == this) {
                return true;
            }
            var other = obj as Types;
            return other != null && (_first == other._first && _second.SequenceEqual(other._second));
        }
    }
}