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
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using System.Security;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Packaging;

    public abstract class CmdletWithSource : CmdletWithProvider {
        protected CmdletWithSource(OptionCategory[] categories)
            : base(categories) {
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(ParameterSetName = Constants.ParameterSets.SourceByInputObjectSet, Mandatory = true, ValueFromPipeline = true)]
        public PackageSource[] InputObject {get; set;}

        [Parameter]
        public PSCredential Credential {get; set;}

        public override string CredentialUsername {
            get {
                return Credential != null ? Credential.UserName : null;
            }
        }

        public override SecureString CredentialPassword {
            get {
                return Credential != null ? Credential.Password : null;
            }
        }

    }
}
