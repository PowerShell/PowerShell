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
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Utility;
    using System.Security;

    [Cmdlet(VerbsLifecycle.Register, Constants.Nouns.PackageSourceNoun, SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517139")]
    public sealed class RegisterPackageSource : CmdletWithProvider {
        public RegisterPackageSource()
            : base(new[] {OptionCategory.Provider, OptionCategory.Source}) {
        }

        [Parameter]
        [ValidateNotNull()]
        public Uri Proxy { get; set; }

        [Parameter]
        [ValidateNotNull()]
        public PSCredential ProxyCredential { get; set; }

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

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {""};
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


        [Parameter(Position = 0)]
        public string Name {get; set;}

        [Parameter(Position = 1)]
        public string Location {get; set;}

        [Parameter]
        public PSCredential Credential {get; set;}

        [Parameter]
        public SwitchParameter Trusted {get; set;}

        public override bool ProcessRecordAsync() {
            if (Stopping) {
                return false;
            }

            var packageProvider = SelectProviders(ProviderName).ReEnumerable();

            if (ProviderName.IsNullOrEmpty())
            {
                Error(Constants.Errors.ProviderNameNotSpecified, packageProvider.Select(p => p.ProviderName).JoinWithComma());
                return false;
            }

                switch (packageProvider.Count()) {
                    case 0:
                        Error(Constants.Errors.UnknownProvider, ProviderName);
                        return false;

                    case 1:
                        break;

                    default:
                        Error(Constants.Errors.MatchesMultipleProviders, packageProvider.Select(p => p.ProviderName).JoinWithComma());
                        return false;
                }


            var provider = packageProvider.First();

            using (var sources = provider.ResolvePackageSources(this).CancelWhen(CancellationEvent.Token)) {
                // first, check if there is a source by this name already.
                var existingSources = sources.Where(each => each.IsRegistered && each.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)).ToArray();

                if (existingSources.Any()) {
                    // if there is, and the user has said -Force, then let's remove it.
                    foreach (var existingSource in existingSources) {
                        if (Force) {

                            if (ShouldProcess(FormatMessageString(Constants.Messages.TargetPackageSource, existingSource.Name, existingSource.Location, existingSource.ProviderName), Constants.Messages.ActionReplacePackageSource).Result) {
                                var removedSources = provider.RemovePackageSource(existingSource.Name, this).CancelWhen(CancellationEvent.Token);
                                foreach (var removedSource in removedSources) {
                                    Verbose(Constants.Messages.OverwritingPackageSource, removedSource.Name);
                                }
                            }
                        } else {
                            Error(Constants.Errors.PackageSourceExists, existingSource.Name);
                            return false;
                        }
                    }
                }
            }

            string providerNameForProcessMessage = ProviderName.JoinWithComma();
            if (ShouldProcess(FormatMessageString(Constants.Messages.TargetPackageSource, Name, Location, providerNameForProcessMessage), FormatMessageString(Constants.Messages.ActionRegisterPackageSource)).Result)
            {
                //Need to resolve the path created via psdrive. 
                //e.g., New-PSDrive -Name x -PSProvider FileSystem -Root \\foobar\myfolder. Here we are resolving x:\
                try
                {
                    if (FilesystemExtensions.LooksLikeAFilename(Location))
                    {
                        ProviderInfo providerInfo = null;
                        var resolvedPaths = GetResolvedProviderPathFromPSPath(Location, out providerInfo);

                        // Ensure the path is a single path from the file system provider
                        if ((providerInfo != null) && (resolvedPaths.Count == 1) && String.Equals(providerInfo.Name, "FileSystem", StringComparison.OrdinalIgnoreCase))
                        {
                            Location = resolvedPaths[0];
                        }
                    }
                } catch (Exception) {
                    //allow to continue handling the cases other than file system                  
                }

                using (var added = provider.AddPackageSource(Name, Location, Trusted, this).CancelWhen(CancellationEvent.Token)) {
                    foreach (var addedSource in added) {
                        WriteObject(addedSource);
                    }
                }
                return true;
            }

            return false;
        }
    }
}
