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
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;
    using Microsoft.PackageManagement.Packaging;
    using Utility;
    using System.Security;

    [Cmdlet(VerbsCommon.Set, Constants.Nouns.PackageSourceNoun, SupportsShouldProcess = true, DefaultParameterSetName = Constants.ParameterSets.SourceBySearchSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517141")]
    public sealed class SetPackageSource : CmdletWithProvider {
        [Parameter(ValueFromPipeline = true, ParameterSetName = Constants.ParameterSets.SourceByInputObjectSet, Mandatory = true)]
        public PackageSource InputObject;

        public SetPackageSource() : base(new[] {OptionCategory.Provider, OptionCategory.Source}) {
        }

        [Parameter]
        [ValidateNotNull()]
        public Uri Proxy { get; set; }

        [Parameter]
        [ValidateNotNull()]
        public PSCredential ProxyCredential { get; set; }

        /// <summary>
        /// Returns web proxy that provider can use
        /// Construct the webproxy using InternalWebProxy
        /// </summary>
        public override System.Net.IWebProxy WebProxy
        {
            get
            {
                if (Proxy != null)
                {
                    return new PackageManagement.Utility.InternalWebProxy(Proxy, ProxyCredential == null ? null : ProxyCredential.GetNetworkCredential());
                }

                return null;
            }
        }

        [Parameter]
        public PSCredential Credential { get; set; }
        
        public override string CredentialUsername
        {
            get
            {
                return Credential != null ? Credential.UserName : null;
            }
        }

        public override SecureString CredentialPassword
        {
            get
            {
                return Credential != null ? Credential.Password : null;
            }
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
                        ParameterSetName = Constants.ParameterSets.SourceBySearchSet,
                    },
                    new AliasAttribute("Provider"),
                    new ValidateSetAttribute(providerNames.ToArray())
                }));
            }
            else {
                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.SourceBySearchSet,
                    },
                    new AliasAttribute("Provider")
                }));
            }
        }

        [Alias("SourceName")]
        [Parameter(Position = 0, ParameterSetName = Constants.ParameterSets.SourceBySearchSet)]
        public string Name {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.SourceBySearchSet)]
        public string Location {get; set;}

        [Parameter]
        public string NewLocation {get; set;}

        [Parameter]
        public string NewName {get; set;}

        [Parameter]
        public SwitchParameter Trusted {get; set;}

        public override IEnumerable<string> Sources {
            get {
                if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Location)) {
                    return Microsoft.PackageManagement.Internal.Constants.Empty;
                }

                return new[] {
                    Name ?? Location
                };
            }
        }

        /// <summary>
        ///     This can be used when we want to override some of the functions that are passed
        ///     in as the implementation of the IHostApi (ie, 'request object').
        ///     Because the DynamicInterface DuckTyper will use all the objects passed in in order
        ///     to implement a given API, if we put in delegates to handle some of the functions
        ///     they will get called instead of the implementation in the current class. ('this')
        /// </summary>
        private IHostApi UpdatePackageSourceRequest {
            get {
                return new object[] {
                    new {
                        // override the GetOptionKeys and the GetOptionValues on the fly.
                        GetOptionKeys = new Func<IEnumerable<string>>(() => OptionKeys.ConcatSingleItem("IsUpdatePackageSource")),

                        GetOptionValues = new Func<string, IEnumerable<string>>((key) => {
                            if (key != null && key.EqualsIgnoreCase("IsUpdatePackageSource")) {
                                return "true".SingleItemAsEnumerable();
                            }
                            return GetOptionValues(key);
                        })
                    },
                    this,
                }.As<IHostApi>();
            }
        }

        private void UpdatePackageSource(PackageSource source) {
            if (WhatIf) {

                var p = new PSObject(source);

                if (!string.IsNullOrWhiteSpace(NewName)) {
                    p.Properties.Remove("Name");
                    p.Properties.Add( new PSNoteProperty("Name",NewName));
                }

                if (!string.IsNullOrWhiteSpace(NewLocation)) {
                    p.Properties.Remove("Location");
                    p.Properties.Add(new PSNoteProperty("Location", NewLocation));
                }

                if (Trusted.IsPresent) {
                    p.Properties.Remove("Trusted");
                    p.Properties.Add(new PSNoteProperty("Trusted", Trusted.ToBool()));
                }

                WriteObject(p);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewName)) {
                // this is a replacement of an existing package source, we're *not* changing the name. (easy)

                foreach (var src in source.Provider.AddPackageSource(string.IsNullOrWhiteSpace(NewName) ? source.Name : NewName, string.IsNullOrWhiteSpace(NewLocation) ? source.Location : NewLocation, Trusted, UpdatePackageSourceRequest)) {
                    WriteObject(src);
                }

            } else {
                // we're renaming a source.
                // a bit more messy at this point
                // create a new package source first

                bool removed = false;

                foreach (var src in source.Provider.AddPackageSource(NewName, string.IsNullOrWhiteSpace(NewLocation) ? source.Location : NewLocation, Trusted.IsPresent ? Trusted.ToBool() : source.IsTrusted, this)) {
                    WriteObject(src);
                    if (!removed)
                    {
                        // if we are able to successfully add a source, then we remove the original source that was supposed to be replace.
                        // This will only happen once (as there is only one original source)
                        source.Provider.RemovePackageSource(source.Name, this);
                        removed = true;
                    }
                }
            }
        }

        public override bool ProcessRecordAsync() {
            if (IsSourceByObject) {
                // we've already got the package source
                UpdatePackageSource(InputObject);
                return true;
            }

            if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Location)) {
                Error(Constants.Errors.NameOrLocationRequired);
                return false;
            }

            // otherwise, we're just changing a source by name
            var prov = SelectedProviders.ToArray();

            if (Stopping) {
                return false;
            }

            if (prov.Length == 0) {
                if (ProviderName.IsNullOrEmpty() || string.IsNullOrWhiteSpace(ProviderName[0])) {
                    return Error(Constants.Errors.UnableToFindProviderForSource, Name ?? Location);
                }
                return Error(Constants.Errors.UnknownProvider, ProviderName[0]);
            }

            if (prov.Length > 0) {
                var sources = prov.SelectMany(each => each.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing)).Where(source => source.IsRegistered &&
                                                                                                       (Name == null || source.Name.EqualsIgnoreCase(Name)) || (Location == null || source.Location.EqualsIgnoreCase(Location))).ToArray()).ToArray();

                if (sources.Length == 0) {
                    return Error(Constants.Errors.SourceNotFound, Name);
                }

                if (sources.Length > 1) {
                    return Error(Constants.Errors.SourceFoundInMultipleProviders, Name, prov.Select(each => each.ProviderName).JoinWithComma());
                }

                UpdatePackageSource(sources[0]);
            }
            return true;
        }
    }
}
