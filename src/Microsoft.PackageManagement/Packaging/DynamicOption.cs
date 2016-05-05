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

namespace Microsoft.PackageManagement.Internal.Packaging {
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public class DynamicOption {
        private IEnumerable<string> _values;
        public string ProviderName {get; set;}
        public OptionCategory Category {get; internal set;}
        public string Name {get; internal set;}
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Type is OK for a cmdlet parameter")]
        public OptionType Type { get; internal set;}
        public bool IsRequired {get; internal set;}

        public IEnumerable<string> PossibleValues {
            get {
                return _values ?? Enumerable.Empty<string>();
            }
            internal set {
                _values = value;
            }
        }
    }
}