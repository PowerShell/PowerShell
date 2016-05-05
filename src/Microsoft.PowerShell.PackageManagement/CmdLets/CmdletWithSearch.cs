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

namespace Microsoft.PowerShell.PackageManagement.Cmdlets {
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Internal.Packaging;

    public abstract class CmdletWithSearch : CmdletWithProvider {
        protected CmdletWithSearch(OptionCategory[] categories)
            : base(categories) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {""};
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Position = 0)]
        public virtual string[] Name {get; set;}
     
        [Parameter]
        public virtual string RequiredVersion {get; set;}

        [Parameter]
        public virtual string MinimumVersion {get; set;}

        [Parameter]
        public virtual string MaximumVersion {get; set;}

        [Parameter]
        public virtual SwitchParameter AllVersions { get; set; }
    }
}
