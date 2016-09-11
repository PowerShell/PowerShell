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
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Platform;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;
    using Microsoft.PackageManagement.Packaging;
    using Utility;

    [Cmdlet("Install", Constants.Nouns.PackageProviderNoun, SupportsShouldProcess = true, DefaultParameterSetName = Constants.ParameterSets.PackageBySearchSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=626941")]
    public sealed class InstallPackageProvider : CmdletWithSearchAndSource {

        private const string FilterOnTag = "FilterOnTag";
        private string _scope = "AllUsers";
        private List<string> _sourcesFromPipeline = null;

        public InstallPackageProvider()
            : base(new[] {
                OptionCategory.Package, OptionCategory.Install
            }) {
            ShouldSelectAllProviders = true;
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {Constants.ParameterSets.PackageBySearchSet, Constants.ParameterSets.PackageByInputObjectSet};
            }
        }

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string[] Name {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string RequiredVersion {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MinimumVersion {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MaximumVersion {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override PSCredential Credential {get; set;}

        [ValidateSetAttribute("CurrentUser", "AllUsers")]
        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        [Parameter(ParameterSetName = Constants.ParameterSets.PackageByInputObjectSet)]
        public string Scope {
            get {
                return _scope;
            }
            set {
                _scope = value;
            }
        }

        [Parameter(ValueFromPipelineByPropertyName = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string[] Source
        {
            get
            {
                if(InputObject != null) {
                    if (_sourcesFromPipeline == null) {
                        _sourcesFromPipeline = new List<string>();

                        foreach (var inputObj in InputObject) {
                            _sourcesFromPipeline.Add(inputObj.Source);
                        }                        
                    }
                    return _sourcesFromPipeline.ToArray();
                }
                return base.Source;
            }
            set
            {
                base.Source = value;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ParameterSetName = Constants.ParameterSets.PackageByInputObjectSet)]
        public SoftwareIdentity[] InputObject {get; set;}

        protected override void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
            // only generate these parameters if there is an actual call happening. it won't show in get-command -syntax
            if (IsInvocation) {
                //Use the InternalPackageManagementInstallOnly flag to indicate we are installing a provider right now.
                //This will separate a case from auto-bootstrapping.
                var packageManagementService = PackageManagementService as PackageManagementService;
                if (packageManagementService != null) {
                    packageManagementService.InternalPackageManagementInstallOnly = true;
                }
            }
        }


        protected override DynamicOption[] CachedDynamicOptions {
            get {
               //suppress the dynamic parameters from parent classes
                return new[] {
                    new DynamicOption()
                };
            }
        }

        protected override string BootstrapNuGet
        {
            get
            {
                // Generally bootstrapping NuGet is required for the install-packageprovider as PowerShellGet uses it.
                // However, when a user specifies Name as NuGet, e.g., 'install-packageprovider -name NuGet', we do not need to perform a hard 
                // bootstrap on NuGet because this case has been taken care of already. There is no difference from installing other packages.
                return InstallingNugetProvider ? "false" : "true";
            }
        }
        private bool InstallingNugetProvider
        {
            get
            {
                return (Name != null) && (Name.Length == 1 && Name.ContainsAnyOfIgnoreCase("NuGet"));
            }
        }

        public override bool ProcessRecordAsync() {
            ValidateVersion(RequiredVersion);
            ValidateVersion(MinimumVersion);
            ValidateVersion(MaximumVersion);

            if (!AdminPrivilege.IsElevated && Scope.EqualsIgnoreCase("AllUsers"))
            {
                var pkgMgmtService = PackageManagementService as PackageManagementService;
                if (pkgMgmtService != null)
                {
                    Error(Constants.Errors.InstallRequiresCurrentUserScopeParameterForNonAdminUser, pkgMgmtService.SystemAssemblyLocation, pkgMgmtService.UserAssemblyLocation);
                }
                return false;
            }

            if (!string.IsNullOrWhiteSpace(RequiredVersion)) {
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
                if (Name.Any(each => each.ContainsWildcards())) {
                    Error(Constants.Errors.WildCardCharsAreNotSupported, Name.JoinWithComma());
                    return false;
                }
                if ((Name.Length > 1) &&
                    ((!string.IsNullOrWhiteSpace(RequiredVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)) || (!string.IsNullOrWhiteSpace(MaximumVersion)))) {
                    Error(Constants.Errors.MultipleNamesWithVersionNotAllowed);
                    return false;
                }
            }

            if (IsPackageByObject) {
                return InstallPackages(InputObject);
            }

            // otherwise, just do the search right now.
            return base.ProcessRecordAsync();
        }

        

        /// <summary>
        /// Select existing providers on the system that will be used for searching any package providers in the repositories. 
        /// </summary>
        protected override IEnumerable<PackageProvider> SelectedProviders {

            get {
                var availableProviders = base.SelectedProviders.ToArray();
                if (availableProviders.Any(each => RequiredProviders.ContainsAnyOfIgnoreCase(each.ProviderName))) {
                    //For 'install-packageprovider NuGet', the PowerShellGet provider does not need to be involved.
                    //It will causes round trip as the PowerShellGet depends on NuGet. Here we choose the Bootstrap provider only.
                    return (InstallingNugetProvider)? availableProviders.Where(each => Bootstrap.EqualsIgnoreCase(each.ProviderName)):
                        availableProviders.Where(each => RequiredProviders.ContainsIgnoreCase(each.ProviderName));
                }

                Error(Constants.Errors.RegisterPackageSourceRequired, Source.JoinWithComma());
                return Enumerable.Empty<PackageProvider>();             
            }
        }

        protected override IHostApi GetProviderSpecificOption(PackageProvider pv) {
            var host = this.ProviderSpecific(pv);
            var host1 = host;
            //add filterontag for finding providers.  Provider keys are: PackageManagement and Providers
            host = host.Extend<IRequest>(
                new {
                    GetOptionValues = new Func<string, IEnumerable<string>>(key => {
                        if (key.EqualsIgnoreCase(FilterOnTag)) {
                            return ProviderFilters;
                        }

                        return host1.GetOptionValues(key);
                    }),
                });
            return host;
        }

        protected override void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package) {
            if (WhatIf) {
                // grab the dependencies and return them *first*

                foreach (var dep in package.Dependencies) {
                    // note: future work may be needed if the package sources currently selected by the user don't
                    // contain the dependencies.
                    var dependencies = PackageManagementService.FindPackageByCanonicalId(dep, this);
                    foreach (var depPackage in dependencies) {
                        ProcessPackage(depPackage.Provider, searchKey.Select(each => each + depPackage.Name).ToArray(), depPackage);
                    }
                }
            }
            base.ProcessPackage(provider, searchKey, package);
        }

        protected override bool EnsurePackageIsProvider(SoftwareIdentity package) {
            //Make sure the package is a provider package
            return ValidatePackageProvider(package);
        }

        public override bool EndProcessingAsync()
        {
            if (IsPackageByObject) {
                // we should have handled these already.
                return ImportProvider(InputObject);
            }

            var unmatched = _resultsPerName.Keys.Where(each => _resultsPerName[each] == null);

            if (unmatched.Any()) {
                Error(Constants.Errors.NoMatchFoundForProvider, unmatched.FirstOrDefault());
                return false;
            }

            if (!CheckUnmatchedPackages()) {
                // there are unmatched packages
                // not going to install.
                return false;
            }

            var list = ProcessMatchedDuplicates().ToArray();

            // Now install the provider
            if(!base.InstallPackages(list)) {
                return false;
            }

            return ImportProvider(list);
        }

        private bool ImportProvider(SoftwareIdentity[] list)
        {
            Verbose("Importing the package provider {0}", Name.JoinWithComma());

            //after the provider gets installed, we are trying to load it           
            var providers = list.SelectMany(each => PackageManagementService.ImportPackageProvider(this.SuppressErrorsAndWarnings(IsProcessing), each.Name, each.Version.ToVersion(), null, null, false, true)).ToArray();
            if (providers.Any()) {
                Verbose(Resources.Messages.ProviderImported, providers.Select(e => e.ProviderPath).JoinWithComma());
                return true;
            }
            else
            {
                Warning(Resources.Messages.ProviderNameDifferentFromPackageName, Name.JoinWithComma());
            }
            return false;
        }

        private List<SoftwareIdentity> ProcessMatchedDuplicates() {
            List<SoftwareIdentity> filteredSoftwareIdentity = new List<SoftwareIdentity>();
 
            var packagetable = _resultsPerName.Values;

            foreach (var pkgSet in packagetable) {

                if (pkgSet.Count == 0) {
                    continue;
                }
                if (pkgSet.Count == 1) {                    
                    filteredSoftwareIdentity.AddRange(pkgSet);
                    continue;
                }

                // the following are the case (pkgSet.Count > 1)

                // there are overmatched packages:
                // are they found across multiple providers?
                // are they found across multiple sources?
                // are they all from the same source?

                var providers = pkgSet.Select(each => each.ProviderName).Distinct().ToArray();
                var sources = pkgSet.Select(each => each.Source).Distinct().ToArray();

                //Handling a case where one provider with multiple sources found the same package
                if (providers.Length == 1) {
                    // it's matching this package multiple times in the same provider.
                    if (sources.Length == 1) {                      
                        string searchKey = null;
                        foreach (var pkg in pkgSet) {
                            Warning(Constants.Messages.MatchesMultiplePackages, pkg.SearchKey, pkg.ProviderName, pkg.Name, pkg.Version, pkg.Source);
                            searchKey = pkg.SearchKey;
                        }
                        //found the multiple matches in a single repository, we error out for asking exact -Name and -RequiredVersion.
                        Error(Constants.Errors.DisambiguateForInstall, searchKey, Resources.Messages.DisambiguateForInstall_SpecifyName);
                        
                    } else {
                        //found the multiple sources under one provider matches the same package
                        foreach (var subset in pkgSet) {
                            if (Source == null) {
                                //if a user does not provide -Source AND we are having matches from multiple sources, error out and asking for specifying -Source
                                Error(Constants.Errors.DisambiguateForInstall, subset.SearchKey, Resources.Messages.DisambiguateForInstall_SpecifySource);
                                break;
                            }
                            //if a user provides -Source, we try to find the match with -Source
                            if (Source.ContainsAnyOfIgnoreCase(subset.Source)) {
                                filteredSoftwareIdentity.Add(subset);
                                break;
                            }
                        }
                    }
                } else {
                    //Handling a case where the multiple providers match one package
                    //In this case, both bootstrap and PowerShellGet found the same package, let's take PowerShellGet as a precedence
                    foreach (var subset in pkgSet) {
                        if (subset.ProviderName.EqualsIgnoreCase(PowerShellGet)) {
                            //find the match
                            filteredSoftwareIdentity.Add(subset);
                            break;
                        }
                    }
                }             
            }
            
            return filteredSoftwareIdentity;
        }
    }
}
