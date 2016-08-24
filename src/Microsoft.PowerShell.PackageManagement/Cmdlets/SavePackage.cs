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
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;
    using Utility;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
    using Microsoft.PackageManagement.Internal.Utility.Versions;

    [Cmdlet(VerbsData.Save, Constants.Nouns.PackageNoun, SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517140")]
    public sealed class SavePackage : CmdletWithSearchAndSource {
        public SavePackage()
            : base(new[] {
                OptionCategory.Provider, OptionCategory.Source, OptionCategory.Package
            }) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {Constants.ParameterSets.PackageByInputObjectSet, ""};
            }
        }

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string[] Name { get; set; }

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string RequiredVersion { get; set; }

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MinimumVersion { get; set; }

        [Parameter(ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        public override string MaximumVersion { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true, ParameterSetName = Constants.ParameterSets.PackageBySearchSet)]
        // Use the base Source property so relative path will be resolved
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

        protected override void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
            if (!IsInvocation) {
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
                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string[]), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true,
                        ParameterSetName = Constants.ParameterSets.PackageBySearchSet
                    },
                    new AliasAttribute("Provider")
                }));
            }
        }

        [Parameter]
        public string Path {get; set;}

        [Parameter]
        public string LiteralPath {get; set;}

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = Constants.ParameterSets.PackageByInputObjectSet)]
        public SoftwareIdentity InputObject {get; set;}

        private string SaveFileName(string packageName) {
            string resolvedPath = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(Path))
                {
                    resolvedPath = ResolveExistingFolderPath(Path, !Force);
                }

                if (!string.IsNullOrWhiteSpace(LiteralPath))
                {
                    // Validate that the path exists
                    try
                    {
                        SessionState.InvokeProvider.Item.Get(new string[] { LiteralPath }, false, true);
                    }
                    catch (ItemNotFoundException)
                    {
                        if(!Force)
                        {
                            throw;
                        }
                    }

                    resolvedPath = LiteralPath;
                }

                if (string.IsNullOrWhiteSpace(resolvedPath)) {
                    Error(Constants.Errors.DestinationPathInvalid, resolvedPath, packageName);
                    return null;
                }

                // If the destination directory doesn't exist, create it
                if (!Directory.Exists(resolvedPath)) {
                    Directory.CreateDirectory(resolvedPath);
                }

                // don't append path and package name here
                return resolvedPath;
            }
            catch (Exception e)
            {
                Error(Constants.Errors.SavePackageError, e.Message);
                return null;
            }
        }

        public override bool ProcessRecordAsync() {
            if (string.IsNullOrWhiteSpace(Path) && string.IsNullOrWhiteSpace(LiteralPath)) {
                Error(Constants.Errors.DestinationOrLiteralPathNotSpecified);
                return false;
            }

            if (IsPackageByObject)
            {
                return DownloadPackage(InputObject);
            }

            if (Name.Any(each => each.ContainsWildcards()))
            {
                Error(Constants.Errors.WildCardCharsAreNotSupported, Name.JoinWithComma());
                return false;
            }
  
            return base.ProcessRecordAsync();
        }

        public override bool EndProcessingAsync()
        {
            if (IsCanceled)
            {
                return false;
            }

            if (IsPackageByObject)
            {
                // we should have handled these already in ProcessRecordAsync()
                return true;
            }

            if (!CheckUnmatchedPackages())
            {
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
            return DownloadPackage(swids.ToArray());
        }

        private bool DownloadPackage(params SoftwareIdentity[] packagesToSave)
        {
            foreach (var package in packagesToSave)
            {
                if (IsCanceled)
                {
                    return false;
                }

                var provider = package.Provider;


                if (!provider.IsMethodImplemented("DownloadPackage"))
                {
                    Error(Constants.Errors.MethodNotImplemented, provider.ProviderName, "Save-Package");
                    return false;
                }

                // if we do save-package jquery -path C:\test then savepath would be C:\test
                var savePath = SaveFileName(package.PackageFilename);

                bool mainPackageDownloaded = false;

                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    // let the provider handles everything
                    // message would be something like What if: Performing the operation "Save Package" on target "'jQuery' to location 'C:\test\test'".
                    if (ShouldProcess(FormatMessageString(Resources.Messages.SavePackageWhatIfDescription, package.Name, savePath), FormatMessageString(Resources.Messages.SavePackage)).Result)
                    {
                        var host = this.ProviderSpecific(provider);
                        foreach (var downloadedPkg in provider.DownloadPackage(package, savePath, host).CancelWhen(CancellationEvent.Token))
                            //foreach (var downloadedPkg in provider.DownloadPackage(package, savePath, ErrorAndWarningContinue ? host.SuppressErrorsAndWarnings(IsProcessing) : host).CancelWhen(CancellationEvent.Token))
                        {
                            if (IsCanceled)
                            {
                                Error(Constants.Errors.ProviderFailToDownloadFile, downloadedPkg.PackageFilename, provider.ProviderName);
                                return false;
                            }

                            // check whether main package is downloaded;
                            if (downloadedPkg.Name.EqualsIgnoreCase(package.Name) && (FourPartVersion)downloadedPkg.Version >= (FourPartVersion)package.Version)
                            {
                                mainPackageDownloaded = true;
                            }

                            WriteObject(AddPropertyToSoftwareIdentity(downloadedPkg));
                            LogEvent(EventTask.Download, EventId.Save, Resources.Messages.PackageSaved, downloadedPkg.Name, downloadedPkg.Version, downloadedPkg.ProviderName, downloadedPkg.Source ?? string.Empty, downloadedPkg.Status ?? string.Empty, downloadedPkg.InstallationPath ?? string.Empty);
                            TraceMessage(Constants.SavePackageTrace, downloadedPkg);
                        }

                    }
                    else
                    {
                        // What if scenario, don't error out
                        return true;
                    }
                }

                if (!mainPackageDownloaded)
                {
                    Error(Constants.Errors.ProviderFailToDownloadFile, package.PackageFilename, provider.ProviderName);
                    return false;
                }
            }

            return true;
        }
    }
}
