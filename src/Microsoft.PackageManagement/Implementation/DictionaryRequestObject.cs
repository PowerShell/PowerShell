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

namespace Microsoft.PackageManagement.Internal.Implementation {
    using System;
    using System.Collections.Generic;
    using Api;
    using Utility.Async;
    using Utility.Extensions;

    public class DictionaryRequestObject : RequestObject, IAsyncValue<Dictionary<string, List<string>>> {
        private readonly Dictionary<string, List<string>> _results = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public DictionaryRequestObject(ProviderBase provider, IHostApi request, Action<RequestObject> action)
            : base(provider, request, action) {
            InvokeImpl();
        }

        public Dictionary<string, List<string>> Value {
            get {
                this.Wait();
                return _results;
            }
        }

        public override bool YieldKeyValuePair(string key, string value) {
            _results.GetOrAdd(key, () => new List<string>()).Add(value);
            return !IsCanceled;
        }
    }
}