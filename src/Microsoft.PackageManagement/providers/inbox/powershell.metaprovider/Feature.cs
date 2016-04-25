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

namespace Microsoft.PackageManagement.MetaProvider.PowerShell {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Internal;

    public class Feature : Yieldable {
        private KeyValuePair<string, string[]> _pair;

        public Feature(string name) : this(name, new string[0]) {
        }

        public Feature(string name, string value) : this(name, new[] {value}) {
        }

        public Feature(string name, string[] values) {
            _pair = new KeyValuePair<string, string[]>(name, values);
        }

        public override bool YieldResult(PsRequest r) {
            if (r == null) {
                throw new ArgumentNullException("r");
            }
            if (_pair.Value.Length == 0) {
                return r.YieldKeyValuePair(_pair.Key, null);
            }
            return _pair.Value.All(each => r.YieldKeyValuePair(_pair.Key, each));
        }
    }
}
