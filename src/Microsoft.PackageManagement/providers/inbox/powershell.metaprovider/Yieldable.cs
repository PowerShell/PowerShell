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

namespace Microsoft.PackageManagement.MetaProvider.PowerShell.Internal {
    using System.Collections;
    using System.Linq;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;

    public abstract class Yieldable {
        protected Hashtable _details;

        public Hashtable Details {
            get {
                return _details ?? (_details = new Hashtable());
            }
            internal set {
                _details = value;
            }
        }

        public abstract bool YieldResult(PsRequest r);

        protected virtual bool YieldDetails(PsRequest r) {
            if (_details != null && _details.Count > 0) {
                // we need to send this back as a set of key/path & value  pairs.
                return _details.Flatten().All(kvp => r.YieldKeyValuePair(kvp.Key, kvp.Value));
            }
            return true;
        }
    }
}
