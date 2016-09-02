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
    using System.Management.Automation;
    using System.Linq; 
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Packaging;

    [Cmdlet("Import", Constants.Nouns.PackageProviderNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=626942")]
    public sealed class ImportPackageProvider : CmdletBase {

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {""};
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        public string[] Name { get; set; }

        [Parameter]
        public string RequiredVersion {get; set;}

        [Parameter]
        public string MinimumVersion {get; set;}

        [Parameter]
        public string MaximumVersion {get; set;}

        public override bool BeginProcessingAsync() {
            ValidateVersion(RequiredVersion);
            ValidateVersion(MinimumVersion);
            ValidateVersion(MaximumVersion);

            //Error out for the case where requiredVersion with with maximumVersion or minimumVersion       
            if (!string.IsNullOrWhiteSpace(RequiredVersion)) {
                if (!string.IsNullOrWhiteSpace(MaximumVersion) || !string.IsNullOrWhiteSpace(MinimumVersion)) {
                    Error(Constants.Errors.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether);
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaximumVersion) && !string.IsNullOrWhiteSpace(MinimumVersion)
                && new Version(MaximumVersion) < new Version(MinimumVersion)) {
                Error(Constants.Errors.MinimumVersionMustBeLessThanMaximumVersion, MinimumVersion, MaximumVersion);
                return false;
            }

            return true;
        }

        public override bool ProcessRecordAsync() {
            //Error out for the case where multiple provider names with any version specified
            if (((!Name.IsNullOrEmpty() && Name.Length > 1) || Name[0].ContainsWildcards())
                && ((RequiredVersion != null) || (MinimumVersion != null) || (MaximumVersion != null)))
            {
                Error(Constants.Errors.MultipleNamesWithVersionNotAllowed);
                return false;
            }

            foreach (var path in Name) {
                var isRooted = false;

                var resolvedPath = path;

                if (!string.IsNullOrWhiteSpace(path)) {
                    if (IsRooted(path)) {
                        if ((RequiredVersion != null) || (MaximumVersion !=null) || (MinimumVersion !=null)) {
                            Error(Constants.Errors.FullProviderFilePathVersionNotAllowed);
                        }

                        try {
                           ProviderInfo provider = null;
                            Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(path, out provider);

                            // Ensure the path is a single path from the file system provider
                            if ((resolvedPaths.Count > 1) ||
                                (!String.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase))) {
                                Error(Constants.Errors.FilePathMustBeFileSystemPath, path);
                                return false;
                            }
                            resolvedPath = resolvedPaths[0];

                            isRooted = true;
                        } catch (Exception ex) {
                            Error(Constants.Errors.FileNotFound, ex.Message);
                            return true;
                        }
                    }

                    foreach (var p in PackageManagementService.ImportPackageProvider(this, resolvedPath, RequiredVersion.ToVersion(),
                        MinimumVersion.ToVersion(), MaximumVersion.ToVersion(), isRooted, Force.IsPresent)) {
                        WriteObject(p);
                    }
                }
            } //foreach name

            return true;
        }

        public override bool EndProcessingAsync() {
            return base.EndProcessingAsync();
        }
    }
}
