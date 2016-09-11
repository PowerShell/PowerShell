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
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;
    using Utility;

    [Cmdlet(VerbsCommon.Find, Constants.Nouns.PackageNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517132"), OutputType(new Type[1] { typeof(SoftwareIdentity) })]
    public sealed class FindPackage : CmdletWithSearchAndSource {
        public FindPackage()
            : base(new[] {
                OptionCategory.Provider, OptionCategory.Source, OptionCategory.Package
            }) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {"",};
            }
        }

        [Parameter]
        public SwitchParameter IncludeDependencies {get; set;}

        [Parameter]
        public override SwitchParameter AllVersions {get; set;}

        public override bool BeginProcessingAsync()
        {
            if (!string.IsNullOrEmpty(RequiredVersion))
            {
                if ((!string.IsNullOrEmpty(MinimumVersion)) || (!string.IsNullOrEmpty(MaximumVersion)))
                {
                    Error(Constants.Errors.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether);
                }
            }

            return true;
        }

        protected override void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package)
        {
            ProcessPackage(provider, searchKey, package, IncludeDependencies ? new HashSet<string>() : null);
        }

        private void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package, HashSet<string> processedDependencies) {

            try {

                base.ProcessPackage(provider, searchKey, package);
            
                // return the object to the caller now.
                WriteObject(AddPropertyToSoftwareIdentity(package));

                if (IncludeDependencies) {
                    var missingDependencies = new HashSet<string>();
                    foreach (var dep in package.Dependencies) {
                        // note: future work may be needed if the package sources currently selected by the user don't
                        // contain the dependencies.

                        // this dep is not processed yet
                        if (!processedDependencies.Contains(dep))
                        {
                            var dependencies = PackageManagementService.FindPackageByCanonicalId(dep, this);
                            var depPkg = dependencies.OrderByDescending(pp => pp, SoftwareIdentityVersionComparer.Instance).FirstOrDefault();

                            processedDependencies.Add(dep);

                            if (depPkg == null)
                            {
                                missingDependencies.Add(dep);
                                Warning(Constants.Messages.UnableToFindDependencyPackage, dep);
                            }
                            else
                            {
                                ProcessPackage(depPkg.Provider, searchKey.Select(each => each + depPkg.Name).ToArray(), depPkg, processedDependencies);
                            }
                        }
                    }
                    if (missingDependencies.Any()) {
                        Error(Constants.Errors.UnableToFindDependencyPackage, missingDependencies.JoinWithComma());
                    }
                }
            } catch (Exception ex) {

                Debug("Calling ProcessPackage {0}", ex.ToString());
            }
        }


        public override bool EndProcessingAsync() {
            return CheckUnmatchedPackages();
        }
    }
}
