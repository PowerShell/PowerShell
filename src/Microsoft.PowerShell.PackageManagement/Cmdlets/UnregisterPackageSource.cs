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
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;
    using Utility;

    [Cmdlet(VerbsLifecycle.Unregister, Constants.Nouns.PackageSourceNoun, SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517143")]
    public sealed class UnregisterPackageSource : CmdletWithSource {
        public UnregisterPackageSource()
            : base(new[] {OptionCategory.Provider, OptionCategory.Source}) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {Constants.ParameterSets.SourceByInputObjectSet, Constants.ParameterSets.SourceBySearchSet};
            }
        }


        protected override void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
            if (!IsInvocation) {
                var providerNames = PackageManagementService.AllProviderNames;
                var whatsOnCmdline = GetDynamicParameterValue<string[]>("ProviderName");
                if (whatsOnCmdline != null) {
                    providerNames = providerNames.Concat(whatsOnCmdline).Distinct();
                }

                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.SourceBySearchSet
                    },
                    new AliasAttribute("Provider"),
                    new ValidateSetAttribute(providerNames.ToArray())
                }));
            }
            else {
                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.SourceBySearchSet
                    },
                    new AliasAttribute("Provider")
                }));
            }
        }

        [Alias("Name")]
        [Parameter(Position = 0, ParameterSetName = Constants.ParameterSets.SourceBySearchSet)]
        public string Source {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.SourceBySearchSet)]
        public string Location {get; set;}

        public override IEnumerable<string> Sources {
            get {
                if (string.IsNullOrWhiteSpace(Source)) {
                    return new string[0];
                }
                return new[] {
                    Source
                };
            }
        }

        public override bool ProcessRecordAsync() {
            if (IsSourceByObject) {
                foreach (var source in InputObject) {
                    if (Stopping) {
                        return false;
                    }

                    var provider = SelectProviders(source.ProviderName).FirstOrDefault();
                    if (provider == null) {
                        if (string.IsNullOrWhiteSpace(source.ProviderName)) {
                            return Error(Constants.Errors.UnableToFindProviderForSource, source.Name);
                        }
                        return Error(Constants.Errors.UnknownProvider, source.ProviderName);
                    }
                    Unregister(source);
                }
                return true;
            }


            if (string.IsNullOrWhiteSpace(Source) && string.IsNullOrWhiteSpace(Location)) {
                Error(Constants.Errors.NameOrLocationRequired);
                return false;
            }

            // otherwise, we're just deleting a source by name
            var prov = SelectedProviders.ToArray();

            if (Stopping) {
                return false;
            }

            if (prov.Length == 0) {
                if (ProviderName.IsNullOrEmpty() || string.IsNullOrWhiteSpace(ProviderName[0])) {
                    return Error(Constants.Errors.UnableToFindProviderForSource, Source ?? Location);
                }
                return Error(Constants.Errors.UnknownProvider, ProviderName[0]);
            }

            if (prov.Length > 0) {
                var sources = prov.SelectMany(each => each.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing)).Where(source => source.IsRegistered && (source.Name.EqualsIgnoreCase(Source) || source.Location.EqualsIgnoreCase(Source) || source.Location.EqualsIgnoreCase(Location))).ToArray()).ToArray();

                if (sources.Length == 0) {
                    return Error(Constants.Errors.SourceNotFound, Source ?? Location);
                }

                if (sources.Length > 1) {
                    return Error(Constants.Errors.SourceFoundInMultipleProviders, Source ?? Location, prov.Select(each => each.ProviderName).JoinWithComma());
                }

                return Unregister(sources[0]);
            }

            return true;
        }

        public bool Unregister(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (ShouldProcess(FormatMessageString(Constants.Messages.TargetPackageSource, source.Name, source.Location, source.ProviderName), FormatMessageString(Constants.Messages.ActionUnregisterPackageSource)).Result) {
                source.Provider.RemovePackageSource(source.Name, this).Wait();
                return true;
            }
            return false;
        }
    }
}
