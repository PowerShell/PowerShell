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
    using Microsoft.PackageManagement.Internal.Utility.Extensions;

    public class DynamicOption : Yieldable {
        public DynamicOption(OptionCategory category, string name, OptionType expectedType, bool isRequired, IEnumerable<object> permittedValues) {
            Name = name;
            ExpectedType = expectedType;
            IsRequired = isRequired;
            PermittedValues = permittedValues;
        }

        public DynamicOption(string name, OptionType expectedType, bool isRequired, IEnumerable<object> permittedValues) {
            Name = name;
            ExpectedType = expectedType;
            IsRequired = isRequired;
            PermittedValues = permittedValues;
        }


        public DynamicOption(OptionCategory category,string name,  OptionType expectedType, bool isRequired) : this(category, name , expectedType, isRequired, null) {
        }

        public DynamicOption(string name, OptionType expectedType, bool isRequired)
            : this(name, expectedType, isRequired, null) {
        }


        public DynamicOption() {
        }

        public string Name {get; set;}
        public OptionType ExpectedType {get; set;}
        public bool IsRequired {get; set;}
        public IEnumerable<object> PermittedValues {get; set;}

        public override bool YieldResult(PsRequest r) {
            if (r == null) {
                throw new ArgumentNullException("r");
            }
            return r.YieldDynamicOption(Name, ExpectedType.ToString(), IsRequired) && PermittedValues.WhereNotNull().Select(each => each.ToString()).ToArray().All(v => r.YieldKeyValuePair(Name, v));
        }
    }
}
