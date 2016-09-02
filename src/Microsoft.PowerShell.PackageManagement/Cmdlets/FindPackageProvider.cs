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

namespace Microsoft.PowerShell.PackageManagement.Cmdlets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;
    using Microsoft.PackageManagement.Packaging;
    using Utility;

    [Cmdlet("Find", Constants.Nouns.PackageProviderNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=626939"), OutputType(new Type[1] {typeof(SoftwareIdentity)})]
    public sealed class FindPackageProvider : CmdletWithSearchAndSource {
        private const string FilterOnTag = "FilterOnTag";

        public FindPackageProvider() : base(new[] {OptionCategory.Package}) {
            //this will include bootstrap provider
            ShouldSelectAllProviders = true;
        }
       
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0)]
        public override string[] Name { get; set; }

        [Parameter]
        public override SwitchParameter AllVersions { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public override string[] Source
        {
            get
            {
                return base.Source;
            }
            set
            {
                base.Source = value;
            }
        }

        [Parameter]
        public SwitchParameter IncludeDependencies { get; set; }
    
        protected override void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
            //this will suppress the dynamic parameters from the parent classes
            //Noop       
        }


        protected override DynamicOption[] CachedDynamicOptions
        {
            //this will suppress the dynamic parameters from parent classes
            get
            {
                return new[] {new DynamicOption()};
            }
        }

        public override bool ProcessRecordAsync() {
            ValidateVersion(RequiredVersion);
            ValidateVersion(MinimumVersion);
            ValidateVersion(MaximumVersion);

            if (RequiredVersion != null) {
                if ((!string.IsNullOrWhiteSpace(MaximumVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion))) {
                    Error(Constants.Errors.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether);
                    return false;
                }
            }
            //Error out for the case where multiple provider names with any version specified
            if (Name.IsNullOrEmpty()) {
                if ((!string.IsNullOrWhiteSpace(RequiredVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)) || (!string.IsNullOrWhiteSpace(MaximumVersion))) {
                    Error(Constants.Errors.MultipleNamesWithVersionNotAllowed);
                    return false;
                }
            } else {
                if (((Name.Length > 1) || Name.Any(each => each.ContainsWildcards())) &&
                    ((!string.IsNullOrWhiteSpace(RequiredVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)) || (!string.IsNullOrWhiteSpace(MaximumVersion)))) {
                    Error(Constants.Errors.MultipleNamesWithVersionNotAllowed);
                    return false;
                }
            }
            if (AllVersions.IsPresent) {
                if ((!string.IsNullOrWhiteSpace(RequiredVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)) || (!string.IsNullOrWhiteSpace(MaximumVersion))) {
                    Error(Constants.Errors.AllVersionsCannotBeUsedWithOtherVersionParameters);
                    return false;
                }
            }

            base.ProcessRecordAsync();

            return true;
        }

        /// <summary>
        /// Select existing providers on the system that will be used for searching any package providers in the repositories. 
        /// </summary>
        protected override IEnumerable<PackageProvider> SelectedProviders
        {
            get
            {
                var availableProviders = base.SelectedProviders.ToArray();
                if (availableProviders.Any(each => RequiredProviders.ContainsAnyOfIgnoreCase(each.ProviderName)))
                {
                    return availableProviders.Where(each => RequiredProviders.ContainsIgnoreCase(each.ProviderName));
                }
                //if the existing available providers don't have what we required, error out
                Error(Constants.Errors.RegisterPackageSourceRequired, Source.JoinWithComma());
                return Enumerable.Empty<PackageProvider>();
            }
        }

        protected override IHostApi GetProviderSpecificOption(PackageProvider pv)
        {
            var host = this.ProviderSpecific(pv);
            var host1 = host;
            //add filterontag for finding providers.  Provider keys are: PackageManagement and Provider
            host = host.Extend<IRequest>(
                new
                {
                    GetOptionValues = new Func<string, IEnumerable<string>>(key =>
                    {
                        if (key.EqualsIgnoreCase(FilterOnTag)) {
                            return ProviderFilters;
                        }

                        return host1.GetOptionValues(key);
                    }),
                });
            return host;
        }

        protected override bool EnsurePackageIsProvider(SoftwareIdentity package) {
            //Make sure the package is a provider package
            return ValidatePackageProvider(package);
        }

        protected override void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package) {

            Debug("Calling ProcessPackage SearchKey = '{0}' and provider name ='{1}'", searchKey, package.ProviderName);
            try {
                base.ProcessPackage(provider, searchKey, package);

                // output to console
                WriteObject(AddPropertyToSoftwareIdentity(package));

                if (IncludeDependencies) {
                    var missingDependencies = new HashSet<string>();
                    foreach (var dep in package.Dependencies) {
                        var dependendcies = PackageManagementService.FindPackageByCanonicalId(dep, this);
                        var depPkg = dependendcies.OrderByDescending(pp => pp, SoftwareIdentityVersionComparer.Instance).FirstOrDefault();

                        if (depPkg == null) {
                            missingDependencies.Add(dep);
                            Warning(Constants.Messages.UnableToFindDependencyPackage, dep);
                        } else {
                            ProcessPackage(depPkg.Provider, searchKey.Select(each => each + depPkg.Name).ToArray(), depPkg);
                        }
                    }
                    if (missingDependencies.Any()) {
                        Error(Constants.Errors.UnableToFindDependencyPackage, missingDependencies.JoinWithComma());
                    }
                }
            } catch (Exception ex) {
                Debug("Calling ProcessPackage {0}", ex.Message);
            }
        }

        public override bool EndProcessingAsync() {
                return CheckUnmatchedPackages();
        }
    }
}
