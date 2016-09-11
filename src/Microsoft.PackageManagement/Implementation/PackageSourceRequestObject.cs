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
    using PackageManagement.Implementation;
    using PackageManagement.Packaging;
    using Utility.Extensions;
    using System.Globalization;

    internal class PackageSourceRequestObject : EnumerableRequestObject<PackageSource> {
        private PackageSource _currentItem;

        public PackageSourceRequestObject(ProviderBase provider, IHostApi request, Action<RequestObject> action)
            : base(provider, request, action) {
            InvokeImpl();
        }

        private void CommitPackageSource() {
            if (_currentItem != null) {
                Results.Add(_currentItem);
            }
            _currentItem = null;
        }

        public override bool YieldPackageSource(string name, string location, bool isTrusted, bool isRegistered, bool isValidated) {
            Debug(String.Format(CultureInfo.CurrentCulture, "Yielding package source for {0} at location {1}", name, location));
            Activity();

            CommitPackageSource();

            _currentItem = new PackageSource {
                Name = name,
                Location = location,
                Provider = Provider as PackageProvider,
                IsTrusted = isTrusted,
                IsRegistered = isRegistered,
                IsValidated = isValidated,
            };
            return !IsCanceled;
        }

        public override bool YieldKeyValuePair(string key, string value) {
            Activity();

            if (_currentItem == null) {
                Console.WriteLine("TODO: SHOULD NOT GET HERE [(PackageSource)YieldKeyValuePair] ================================================");
                return !IsCanceled;
            }

            _currentItem.DetailsCollection.AddOrSet(key, value);
            return !IsCanceled;
        }

        protected override void Complete() {
            CommitPackageSource();
            base.Complete();
        }
    }
}