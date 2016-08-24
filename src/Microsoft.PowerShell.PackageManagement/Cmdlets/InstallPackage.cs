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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;

    [Cmdlet(VerbsLifecycle.Install, Constants.Nouns.PackageNoun, SupportsShouldProcess = true, DefaultParameterSetName = Constants.ParameterSets.PackageBySearchSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517138")]
    public sealed class InstallPackage : CmdletWithSearchAndSource {

        public InstallPackage()
            : base(new[] {
                OptionCategory.Provider, OptionCategory.Source, OptionCategory.Package, OptionCategory.Install
            }) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {Constants.ParameterSets.PackageBySearchSet, Constants.ParameterSets.PackageByInputObjectSet};
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ParameterSetName = Constants.ParameterSets.PackageByInputObjectSet),]
        public SoftwareIdentity[] InputObject {get; set;}

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string[] Name {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string RequiredVersion {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MinimumVersion {get; set;}

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MaximumVersion {get; set;}

        [Parameter(ValueFromPipelineByPropertyName = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        // Use the base Source property so relative path will be resolved
        public override string[] Source {
            get
            {
                return base.Source;
            }
            set
            {
                base.Source = value;
            }
        }


        protected override void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
#if DEEP_DEBUG
            Console.WriteLine("»» Entering GCSP ");
#endif
            if (!IsInvocation) {
#if DEEP_DEBUG
                Console.WriteLine("»»» Does not appear to be Invocation (locked:{0})", IsReentrantLocked);
#endif
                var providerNames = PackageManagementService.AllProviderNames;
                var whatsOnCmdline = GetDynamicParameterValue<string[]>("ProviderName");
                if (whatsOnCmdline != null) {
                    providerNames = providerNames.Concat(whatsOnCmdline).Distinct();
                }

                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string[]), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.PackageBySearchSet
                    },
                    new AliasAttribute("Provider"),
                    new ValidateSetAttribute(providerNames.ToArray())
                }));
            }
            else {
#if DEEP_DEBUG
                Console.WriteLine("»»» Does appear to be Invocation (locked:{0})", IsReentrantLocked);
#endif
                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string[]), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.PackageBySearchSet
                    },
                    new AliasAttribute("Provider")
                }));
            }
        }



        public override bool BeginProcessingAsync() {
            return true;
        }

        public override bool ProcessRecordAsync() {
            if (IsPackageByObject) {
                return InstallPackages(InputObject);
            }
            if (MyInvocation.BoundParameters.Count == 0 || (MyInvocation.BoundParameters.Count == 1 &&  MyInvocation.BoundParameters.ContainsKey("ProviderName")) ) {
                // didn't pass in anything, (except maybe Providername)
                // that's no ok -- we need some criteria
                Error(Constants.Errors.MustSpecifyCriteria);
                return false;
            }

            if (Name.Any(each => each.ContainsWildcards())) {
                Error(Constants.Errors.WildCardCharsAreNotSupported, Name.JoinWithComma());
                return false;
            }
            // otherwise, just do the search right now.
            return base.ProcessRecordAsync();
        }

        public override bool EndProcessingAsync() {
            if (IsPackageByObject) {
                // we should have handled these already.
                // buh-bye
                return true;
            }

            if (!CheckUnmatchedPackages()) {
                // there are unmatched packages
                // not going to install.
                return false;
            }

            var swids = CheckMatchedDuplicates().ReEnumerable();
            if (swids == null || !swids.Any())
            {
                // there are duplicate packages
                // not going to install.
                return false;
            }

            // good list. Let's roll...
            return base.InstallPackages(swids.ToArray());
        }

        protected override void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package) {
            if (WhatIf) {
                // grab the dependencies and return them *first*
                bool hasDependencyLoop = false;
                var dependencies = GetDependenciesToInstall(package, ref hasDependencyLoop);

                if (!hasDependencyLoop)
                {
                    foreach (var dependency in dependencies)
                    {
                        base.ProcessPackage(provider, searchKey.Select(each => each+dependency.Name).ToArray(), dependency);
                    }
                }
            }

            base.ProcessPackage(provider, searchKey, package);
        }

        private IEnumerable<SoftwareIdentity> GetDependenciesToInstall(SoftwareIdentity package, ref bool hasDependencyLoop)
        {
            // No dependency
            if (package.Dependencies == null || package.Dependencies.Count() == 0)
            {
                return Enumerable.Empty<SoftwareIdentity>();
            }

            // Returns list of dependency to be installed in the correct order that we should install them
            List<SoftwareIdentity> dependencyToBeInstalled = new List<SoftwareIdentity>();

            HashSet<SoftwareIdentity> permanentlyMarked = new HashSet<SoftwareIdentity>(new SoftwareIdentityNameVersionComparer());
            HashSet<SoftwareIdentity> temporarilyMarked = new HashSet<SoftwareIdentity>(new SoftwareIdentityNameVersionComparer());

            // checks that there are no dependency loop 
            hasDependencyLoop = !DepthFirstVisit(package, temporarilyMarked, permanentlyMarked, dependencyToBeInstalled);

            if (!hasDependencyLoop)
            {
                // remove the last item of the list because that is the package itself
                dependencyToBeInstalled.RemoveAt(dependencyToBeInstalled.Count - 1);
                return dependencyToBeInstalled;
            }

            // there are dependency loop. 
            return Enumerable.Empty<SoftwareIdentity>();            
        }

        /// <summary>
        /// Do a dfs visit. returns false if a cycle is encountered. Add the packageItem to the list at the end of each visit
        /// </summary>
        /// <param name="packageItem"></param>
        /// <param name="dependencyToBeInstalled"></param>
        /// <param name="permanentlyMarked"></param>
        /// <param name="temporarilyMarked"></param>
        /// <returns></returns>
        internal bool DepthFirstVisit(SoftwareIdentity packageItem, HashSet<SoftwareIdentity> temporarilyMarked,
            HashSet<SoftwareIdentity> permanentlyMarked, List<SoftwareIdentity> dependencyToBeInstalled)
        {
            // dependency loop detected because the element is temporarily marked
            if (temporarilyMarked.Contains(packageItem))
            {
                return false;
            }

            // this is permanently marked. So we don't have to visit it.
            // This is to resolve a case where we have: A->B->C and A->C. Then we need this when we visit C again from either B or A.
            if (permanentlyMarked.Contains(packageItem))
            {
                return true;
            }

            // Mark this node temporarily so we can detect cycle.
            temporarilyMarked.Add(packageItem);

            // Visit the dependency
            foreach (var dependency in packageItem.Dependencies)
            {
                var dependencies = PackageManagementService.FindPackageByCanonicalId(dependency, this);
                var depPkg = dependencies.OrderByDescending(pp => pp, SoftwareIdentityVersionComparer.Instance).FirstOrDefault();

                if (!DepthFirstVisit(depPkg, temporarilyMarked, permanentlyMarked, dependencyToBeInstalled))
                {
                    // if dfs returns false then we have encountered a loop
                    return false;
                }
                // otherwise visit the next dependency
            }

            // Add the package to the list so we can install later
            dependencyToBeInstalled.Add(packageItem);

            // Done with this node so mark it permanently
            permanentlyMarked.Add(packageItem);

            // Unmark it temporarily
            temporarilyMarked.Remove(packageItem);

            return true;
        }

    }
}
