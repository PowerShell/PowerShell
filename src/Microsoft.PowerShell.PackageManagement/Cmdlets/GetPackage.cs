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
    using System.Linq;
    using System.Management.Automation;
    using Utility;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;

    [Cmdlet(VerbsCommon.Get, Constants.Nouns.PackageNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517135")]
    public class GetPackage : CmdletWithSearch {
        private readonly Dictionary<string, bool> _namesProcessed = new Dictionary<string, bool>();
        private readonly string _newSoftwareIdentityTypeName = "Microsoft.PackageManagement.Packaging.SoftwareIdentity#GetPackage";
      
        public GetPackage()
            : base(new[] {
                OptionCategory.Provider, OptionCategory.Install
            }) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {""};
            }
        }

        protected IEnumerable<string> UnprocessedNames {
            get {
                return _namesProcessed.Keys.Where(each => !_namesProcessed[each]);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Considered. Still No.")]
        protected bool IsPackageInVersionRange(SoftwareIdentity pkg) {
            if (RequiredVersion != null && SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, RequiredVersion) != 0) {
                return false;
            }

            if (MinimumVersion != null && SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, MinimumVersion) < 0) {
                return false;
            }

            if (MaximumVersion != null && SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, MaximumVersion) > 0) {
                return false;
            }

            return true;
        }

        protected bool IsDuplicate(SoftwareIdentity package) {
            // todo: add duplicate checking (need canonical ids)
            return false;
        }

        public override bool ProcessRecordAsync() {
            
            ValidateVersion(RequiredVersion);
            ValidateVersion(MinimumVersion);
            ValidateVersion(MaximumVersion);

            // If AllVersions is specified, make sure other version parameters are not supplied
            if (AllVersions.IsPresent)
            {
                if ((!string.IsNullOrWhiteSpace(RequiredVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)) || (!string.IsNullOrWhiteSpace(MaximumVersion)))
                {
                    Error(Constants.Errors.AllVersionsCannotBeUsedWithOtherVersionParameters);
                }
            }

            // Cannot have Max/Min version parameters with RequiredVersion
            if (RequiredVersion != null)
            {
                if ((!string.IsNullOrWhiteSpace(MaximumVersion)) || (!string.IsNullOrWhiteSpace(MinimumVersion)))
                {
                    Error(Constants.Errors.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether);
                }
            }

            // keep track of what package names the user asked for.
            if (!Name.IsNullOrEmpty()) {
                foreach (var name in Name) {
                    _namesProcessed.GetOrAdd(name, () => false);
                }
            }

            var requests = (Name.IsNullOrEmpty() ?

                // if the user didn't specify any names
                SelectedProviders.Select(pv => new {
                    query = "?",
                    packages = pv.GetInstalledPackages("",RequiredVersion, MinimumVersion, MaximumVersion, this.ProviderSpecific(pv)).CancelWhen(CancellationEvent.Token)
                }) :

                // if the user specified a name,
                SelectedProviders.SelectMany(pv => {
                    // for a given provider, if we get an error, we want just that provider to stop.
                    var host = this.ProviderSpecific(pv);

                    return Name.Select(name => new {
                        query = name,
                        packages = pv.GetInstalledPackages(name, RequiredVersion, MinimumVersion, MaximumVersion, host).CancelWhen(CancellationEvent.Token)
                    });
                })).ToArray();

            var potentialPackagesToProcess = new System.Collections.ObjectModel.Collection<SoftwareIdentity>();
            while (WaitForActivity(requests.Select(each => each.packages))) {
                
                // keep processing while any of the the queries is still going.
                foreach (var result in requests.Where(each => each.packages.HasData)) {
                    // look only at requests that have data waiting.

                    foreach (var package in result.packages.GetConsumingEnumerable()) {
                        // process the results for that set.

                        if (IsPackageInVersionRange(package)) {
                            // it only counts if the package is in the range we're looking for.

                            // mark down that we found something for that query
                            _namesProcessed.AddOrSet(result.query, true);

                            // If AllVersions is specified, process the package immediately
                            if (AllVersions)
                            {
                                // Process the package immediately if -AllVersions are required
                                ProcessPackage(package);
                            }
                            else 
                            { 
                                // Save to perform post-processing to eliminate duplicate versions and group by Name
                                potentialPackagesToProcess.Add(package);
                            }
                        }
                    }
                }
                // just work with whatever is not yet consumed
                requests = requests.FilterWithFinalizer(each => each.packages.IsConsumed, each => each.packages.Dispose()).ToArray();
            } // end of WaitForActivity()
            
            // Perform post-processing only if -AllVersions is not specified            
            if (!AllVersions)
            {
                // post processing the potential packages as we have to display only
                // 1 package per name (note multiple versions of the same package may be installed)
                // In general, it is good practice to show only the latest one.

                // However there are cases when the same package can be found by different providers. in that case, we will show
                // the packages from different providers even through they have the same package name. This is important because uninstall-package 
                // inherits from get-package, so that when the first provider does not implement the uninstall-package(), such as Programs, others will
                // perform the uninstall.

                //grouping packages by package name first
                var enumerablePotentialPackages = from p in potentialPackagesToProcess
                    group p by p.Name
                    into grouping
                    select grouping.OrderByDescending(pp => pp, SoftwareIdentityVersionComparer.Instance);

                //each group of packages with the same name, return the first if the packages are from the same provider
                foreach (var potentialPackage in enumerablePotentialPackages.Select(pp => (from p in pp
                    group p by p.ProviderName
                    into grouping
                    select grouping.OrderByDescending(each => each, SoftwareIdentityVersionComparer.Instance).First())).SelectMany(pkgs => pkgs.ToArray())) {
                    ProcessPackage(potentialPackage);
                }
            }

            return true;
        }

        protected virtual void ProcessPackage(SoftwareIdentity package) {
            // Check for duplicates
            if (!IsDuplicate(package)) {

                // Display the SoftwareIdentity object in a format: Name, Version, Source and Provider
                var swidTagAsPsobj = PSObject.AsPSObject(package);
                var noteProperty = new PSNoteProperty("PropertyOfSoftwareIdentity", "PropertyOfSoftwareIdentity");
                swidTagAsPsobj.Properties.Add(noteProperty, true);
                swidTagAsPsobj.TypeNames.Insert(0, _newSoftwareIdentityTypeName);

                WriteObject(swidTagAsPsobj);
            }
        }

        public override bool EndProcessingAsync() {
            // give out errors for any package names that we don't find anything for.
            foreach (var name in UnprocessedNames) {
                Error(Constants.Errors.NoMatchFound, name);
            }
            return true;
        }

        //use wide Source column for get-package
        protected override bool UseDefaultSourceFormat {
            get {
                return false;
            }
        }
    }
}