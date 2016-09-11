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
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;

    [Cmdlet(VerbsCommon.Get, Constants.Nouns.PackageProviderNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517136")]
    public sealed class GetPackageProvider : CmdletBase {
        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {"",};
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Position = 0)]
        public string[] Name {get; set;}

        [Parameter]
        public SwitchParameter ListAvailable {get; set;}

        public override bool ProcessRecordAsync() {

            if (Name.IsNullOrEmpty()) {
                var providers = ListAvailable.IsPresent ? PackageManagementService.GetAvailableProviders(this, Name).ReEnumerable() : SelectProviders(Name).ReEnumerable();

                var providerOrdered = providers.OrderBy(each => each.ProviderName);
                // Get all available providers
                foreach (var p in providerOrdered) {
                    WriteObject(p);
                }
            } else {
                ProcessProvidersFilteredByName();
            }

            return true;
        }

        private void ProcessProvidersFilteredByName() {
            //Do not log error when a provider is not found in the list returned by SelectProviders(). This allows the searching continues 
            //in the list of providers that are not loaded.
            ShouldLogError = false;
            var notfound = new List<string>();
            foreach (var name in Name) {
                var providers = ListAvailable.IsPresent ? PackageManagementService.GetAvailableProviders(this, new[] {name}).ReEnumerable() : SelectProviders(name).ReEnumerable();
                if (providers.Any()) {
                    var providerOrdered = providers.OrderByDescending(each => each.ProviderName);

                    foreach (var provider in providerOrdered) {
                        WriteObject(provider);
                    }
                } else {
                    notfound.Add(name);
                }
            }

            //Error out if the specific provider is not found
            if (notfound.Any()) {
                if (notfound.Count == 1) {
                    Error(ListAvailable.IsPresent ? Constants.Errors.UnknownProvider : Constants.Errors.UnknownProviderFromActivatedList, notfound.FirstOrDefault());
                } else {
                    Error(ListAvailable.IsPresent ? Constants.Errors.UnknownProviders : Constants.Errors.UnknownProviderFromActivatedList, notfound.JoinWithComma());
                }
            }
            notfound.Clear();
        }
    }
}

