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
    using Api;
    using Utility.Async;

    public class FuncRequestObject<T> : RequestObject, IAsyncValue<T> {
        private T _result;

        public FuncRequestObject(ProviderBase provider, IHostApi hostApi, Func<RequestObject, T> function)
            : base(provider, hostApi, null) {
            _action = r => {_result = function(r);};
            InvokeImpl();
        }

        public T Value {
            get {
                // wait for end.
                this.Wait();
                return _result;
            }
        }
    }
}