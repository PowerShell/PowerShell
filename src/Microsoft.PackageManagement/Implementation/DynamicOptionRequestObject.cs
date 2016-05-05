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
    using Packaging;

    internal class DynamicOptionRequestObject : EnumerableRequestObject<DynamicOption> {
        private DynamicOption _currentItem;
        private List<string> _list = new List<string>();
        private readonly OptionCategory _category;

        public DynamicOptionRequestObject(ProviderBase provider, IHostApi request, Action<RequestObject> action, OptionCategory category)
            : base(provider, request, action) {
            _category = category;
            InvokeImpl();
        }

        public override bool YieldDynamicOption(string name, string expectedType, bool isRequired) {
            Activity();

            if (_currentItem != null) {
                _currentItem.PossibleValues = _list.ToArray();
                _list = new List<string>();
                Results.Add(_currentItem);
            }

            OptionType typ;

            if (Enum.TryParse(expectedType, true, out typ)) {
                _currentItem = new DynamicOption {
                    Category = _category,
                    Name = name,
                    Type = typ,
                    IsRequired = isRequired,
                    ProviderName = Provider.ProviderName,
                };
            }
            return !IsCanceled;
        }

        public override bool YieldKeyValuePair(string key, string value) {
            Activity();

            if (_currentItem != null && _currentItem.Name == key) {
                _list.Add(value);
            }
            return !IsCanceled;
        }

        protected override void Complete() {
            if (_currentItem != null) {
                _currentItem.PossibleValues = _list.ToArray();
                Results.Add(_currentItem);
            }

            base.Complete();
        }
    }
}