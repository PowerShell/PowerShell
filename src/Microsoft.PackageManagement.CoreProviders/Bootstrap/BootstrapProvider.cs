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

namespace Microsoft.PackageManagement.Providers.Internal.Bootstrap {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Api;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Packaging;
    using PackageManagement.Packaging;
    using PackageManagement.Internal.Utility.Extensions;
    using PackageManagement.Internal.Utility.Plugin;
    using PackageManagement.Internal.Utility.Versions;
    using Directory = System.IO.Directory;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using File = System.IO.File;

    public class BootstrapProvider {
        private static readonly Dictionary<string, string[]> _features = new Dictionary<string, string[]> {
            // {Constants.Features.SupportedSchemes, new[] {"http", "https", "file"}},
            // {Constants.Features.SupportedExtensions, new[] {"exe", "msi"}},
            {Constants.Features.MagicSignatures, Constants.Empty},
            {Constants.Features.AutomationOnly, Constants.Empty}
        };
        
        private const WildcardOptions WildcardOptions = System.Management.Automation.WildcardOptions.CultureInvariant | System.Management.Automation.WildcardOptions.IgnoreCase;

        private static IEqualityComparer<Package> PackageEqualityComparer = new PackageManagement.Internal.Utility.Extensions.EqualityComparer<Package>(
            (x, y) => x.Name.EqualsIgnoreCase(y.Name) && x.Version.EqualsIgnoreCase(y.Version), (x) => (x.Name + x.Version).GetHashCode());

        private PackageManagementService PackageManagementService {
            get {
                return PackageManager.Instance as PackageManagementService;
            }
        }

        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <required />
        /// <returns>the name of the package provider</returns>
        public string PackageProviderName {
            get {
                return "Bootstrap";
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void InitializeProvider(BootstrapRequest request) {
            // we should go find out what's available once here just to make sure that
            // we have a list
            try {
                request.Debug("Initialize Bootstrapper");
                Task.Factory.StartNew(() => {
                    // we can do this asynchronously, it'll cut down on any startup delay when the network is slow or unavailable.
                    try {
                        PackageManagementService.BootstrappableProviderNames = request.Providers.Select(provider => provider.Name).ToArray();

                    } catch (Exception e) {
                        // if we have a serious problem, it just means we can't bootstrap those providers anyway.
                        // in the event of a catastrophic failure, request isn't going to be valid anymore (and hence the user won't see it)
                        // but we can send the error to the system debug output.
                        e.Dump();
                    }
                });
            } catch (Exception e) {
                e.Dump();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void GetFeatures(BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling 'Bootstrap::GetFeatures'");
            foreach (var feature in _features) {
                request.Yield(feature);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void GetDynamicOptions(string category, BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling 'Bootstrap::GetDynamicOptions ({0})'", category);

            switch ((category ?? string.Empty).ToLowerInvariant()) {
                case "package":
                    break;

                case "source":
                    break;

                case "install":
                    request.YieldDynamicOption("DestinationPath", "Folder", false);
                    request.YieldDynamicOption("Scope", "String", false, new[] { "CurrentUser", "AllUsers" });
                    request.YieldDynamicOption("DisplayLongSourceName", "Switch", false);
                    break;
            }
        }

        public void ResolvePackageSources(BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling ResolvePackageSources");

            try {
                if (request.LocalSource.Any()) {
                    foreach (var source in request.LocalSource) {
                        request.YieldPackageSource(source, source, false, true, true);
                    }
                    return;
                }

                foreach (var source in request._urls) {

                    request.YieldPackageSource(source.AbsoluteUri, source.AbsoluteUri, false, true, true);
                }

            } catch (Exception e) {
                e.Dump();
            }
        }

        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Verbose(Resources.Messages.FindingPackage, string.Format(CultureInfo.CurrentCulture, "{0}::FindPackage' '{1}','{2}','{3}','{4}'", PackageProviderName, name, requiredVersion, minimumVersion, maximumVersion));

            if (name != null && name.EqualsIgnoreCase("PackageManagement")) {
                // they are looking for PackageManagement itself.
                // future todo: let PackageManagement update itself.
                return;
            }

            if (request.LocalSource.Any()) {
                // find a provider from given path
                request.FindProviderFromFile(name, requiredVersion, minimumVersion, maximumVersion);
                return;
            }

            // are they are looking for a specific provider?
            if (string.IsNullOrWhiteSpace(name) || WildcardPattern.ContainsWildcardCharacters(name)) {
                

                // no, return all providers that match the range.                
                var wildcardPattern = new WildcardPattern(name, WildcardOptions);

                if (request.GetOptionValue("AllVersions").IsTrue()) {
                    // Feed.Query() can return an empty provider, so here we need to execlude it by checking p.Name !=null or empty.
                    foreach (var p in request.Providers.Distinct(PackageEqualityComparer).Where(p => !string.IsNullOrWhiteSpace(p.Name) && (string.IsNullOrWhiteSpace(name) || wildcardPattern.IsMatch(p.Name)))) {
                        FindPackage(p.Name, null, "0.0", null, 0, request);
                    }
                    return;
                }

                if (request.Providers.Distinct(PackageEqualityComparer).Where(p => string.IsNullOrWhiteSpace(name) || wildcardPattern.IsMatch(p.Name)).Any(p => !request.YieldFromSwidtag(p, requiredVersion, minimumVersion, maximumVersion, name))) {
                    // if there is a problem, exit.
                    return;
                }
            } else {
                // return just the one they asked for.

                // asked for a specific version?
                if (!string.IsNullOrWhiteSpace(requiredVersion)) {
                    request.YieldFromSwidtag(request.GetProvider(name, requiredVersion), name);
                    return;
                }

                if (request.GetOptionValue("AllVersions").IsTrue()) {
                    if (request.GetProviderAll(name, minimumVersion, maximumVersion).Distinct(PackageEqualityComparer).Any(provider => !request.YieldFromSwidtag(provider, name))) {
                        // if there is a problem, exit.
                        return;
                    }
                    return;
                }

                // asked for a version range?
                if (!string.IsNullOrWhiteSpace(minimumVersion) || !string.IsNullOrEmpty(maximumVersion)) {
                    if (request.GetProvider(name, minimumVersion, maximumVersion).Distinct(PackageEqualityComparer).Any(provider => !request.YieldFromSwidtag(provider, name))) {
                        // if there is a problem, exit.
                        return;
                    }
                    return;
                }

                // just return by name
                request.YieldFromSwidtag(request.GetProvider(name), name);
            }

            // return any matches in the name
        }        

        /// <summary>
        ///     Returns the packages that are installed
        /// </summary>
        /// <param name="name">the package name to match. Empty or null means match everything</param>
        /// <param name="requiredVersion">
        ///     the specific version asked for. If this parameter is specified (ie, not null or empty
        ///     string) then the minimum and maximum values are ignored
        /// </param>
        /// <param name="minimumVersion">
        ///     the minimum version of packages to return . If the <code>requiredVersion</code> parameter
        ///     is specified (ie, not null or empty string) this should be ignored
        /// </param>
        /// <param name="maximumVersion">
        ///     the maximum version of packages to return . If the <code>requiredVersion</code> parameter
        ///     is specified (ie, not null or empty string) this should be ignored
        /// </param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling '{0}::GetInstalledPackages' '{1}','{2}','{3}','{4}'", PackageProviderName, name, requiredVersion, minimumVersion, maximumVersion);

            //search under the providerAssembies folder for the installed providers
            var providers = PackageManagementService.AllProvidersFromProviderAssembliesLocation(request).Select(providerFileAssembly => {
                          
                //get the provider's name\version
                var versionFolder = Path.GetDirectoryName(providerFileAssembly);

                if (string.IsNullOrWhiteSpace(versionFolder)) {
                    return null;
                }
 
                Version ver;
                if (!Version.TryParse(Path.GetFileName(versionFolder), out ver)) {
                    //this will cover whether the providerFileAssembly is at top level as well as a bad version folder
                    //skip if the provider is at the top level as they are imported already via LoadProviders() during the initialization. 
                    //the provider will be handled PackageManagementService.DynamicProviders below.
                    return null;
                }
                                              
                var providerNameFolder = Path.GetDirectoryName(versionFolder);
                if (!string.IsNullOrWhiteSpace(providerNameFolder)) {
                    var providerName = Path.GetFileName(providerNameFolder);
                    if (!string.IsNullOrWhiteSpace(providerName)) {
                        return new {
                            Name = providerName,
                            Version = (FourPartVersion)ver,
                            ProviderPath = providerFileAssembly
                        };
                    }
                }
                
                return null;
            }).WhereNotNull();

            // return all the dynamic package providers as packages
            providers = providers.Concat(PackageManagementService.DynamicProviders.Select(each => new {
                Name = each.ProviderName,
                each.Version,
                each.ProviderPath
            })).Distinct();

            var pp = request.LocalSource.Any() ? providers.Select(each => request.GetProviderFromFile(each.ProviderPath, false, true)).WhereNotNull() :
                                                                    providers.Select(each => request.GetProvider(each.Name, each.Version)).WhereNotNull();

            foreach (var p in pp) {
                request.YieldFromSwidtag(p, requiredVersion, minimumVersion, maximumVersion, name);
            }
        }
      
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void DownloadPackage(string fastPath, string location, BootstrapRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            request.Debug("Calling 'Bootstrap::DownloadPackage'");
        }

        private bool InstallProviderFromInstaller(Package provider, Link link, string fastPath, BootstrapRequest request) {
            switch (link.MediaType) {
                case Iso19770_2.MediaType.MsiPackage:
                case Iso19770_2.MediaType.MsuPackage:
                    return InstallPackageFile(provider, fastPath, request);

                case Iso19770_2.MediaType.PackageReference:
                    // let the core figure out how to install this package 
                    var packages = PackageManagementService.FindPackageByCanonicalId(link.HRef.AbsoluteUri, request).ToArray();
                    switch (packages.Length) {
                        case 0:
                            request.Warning("Unable to resolve package reference '{0}'", link.HRef);
                            return false;

                        case 1:
                            return InstallPackageReference(provider, fastPath, request, packages);

                        default:
                            request.Warning("Package Reference '{0}' resolves to {1} packages.", link.HRef, packages.Length);
                            return false;
                    }

                default:
                    request.Warning("Provider '{0}' with link '{1}' has unknown media type '{2}'.", provider.Name, link.HRef, link.MediaType);
                    return false;
            }
        }

        private bool InstallPackageFile(Package provider, string fastPath, BootstrapRequest request) {
            // we can download and verify this package and get the core to install it.
            var file = request.DownloadAndValidateFile(provider._swidtag);
            if (file != null) {
                // we have a valid file.
                // run the installer
                if (request.ProviderServices.Install(file, "", request)) {
                    // it installed ok!
                    request.YieldFromSwidtag(provider, fastPath);
                    PackageManagementService.LoadProviders(request.As<IRequest>());
                    return true;
                }
                request.Warning(Constants.Messages.FailedProviderBootstrap, fastPath);
            }
            return false;
        }

        private bool InstallPackageReference(Package provider, string fastPath, BootstrapRequest request, SoftwareIdentity[] packages) {
            IHostApi installRequest = request;
            if (packages[0].Provider.Name.EqualsIgnoreCase("PowerShellGet") && !request.ProviderServices.IsElevated) {
                // if we're not elevated, we want powershellget to install to the user scope
            
                installRequest = new object[] {
                    new {
                        GetOptionKeys = new Func<IEnumerable<string>>(() => request.OptionKeys.ConcatSingleItem("Scope")),
                        GetOptionValues = new Func<string, IEnumerable<string>>((key) => {
                            if (key != null && key.EqualsIgnoreCase("Scope")) {
                                return "CurrentUser".SingleItemAsEnumerable();
                            }
                            return request.GetOptionValues(key);
                        })
                    }
                    , installRequest
                }.As<IHostApi>();
            }

            var installing = packages[0].Provider.InstallPackage(packages[0], installRequest);

            SoftwareIdentity lastPackage = null;

            foreach (var i in installing) {
                lastPackage = i;
                // should we echo each package back as it comes back? 
                request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);

                if (request.IsCanceled) {
                    installing.Cancel();
                }
            }

            if (!request.IsCanceled && lastPackage != null) {
                if (provider.Name.EqualsIgnoreCase("PowerShellGet")) {
                    // special case. PSModules we can just ask the PowerShell provider to pick it up 
                    // rather than try to scan for it.
                    PackageManagementService.TryLoadProviderViaMetaProvider("PowerShell", lastPackage.FullPath, request);
                    request.YieldFromSwidtag(provider, fastPath);
                    return true;
                }

                // looks like it installed ok.
                request.YieldFromSwidtag(provider, fastPath);

                // rescan providers
                PackageManagementService.LoadProviders(request.As<IRequest>());
                return true;
            }
            return false;
        }

        private bool InstallAssemblyProvider(Package provider, Link link, string fastPath, BootstrapRequest request, bool deleteFile=true) {
            request.Verbose(Resources.Messages.InstallingPackage, fastPath);
            
            if (!Directory.Exists(request.DestinationPath(request))) {
                request.Error(ErrorCategory.InvalidOperation, fastPath, Constants.Messages.DestinationPathNotSet);
                return false;
            }

            string targetFilename = fastPath;
            string file = fastPath;

            //source can be from install-packageprovider or can be from the pipeline
            if (!request.LocalSource.Any() && !fastPath.IsFile() && link != null) {

                targetFilename = link.Attributes[Iso19770_2.Discovery.TargetFilename];

                // download the file
                file = request.DownloadAndValidateFile(provider._swidtag);

            }

            if (string.IsNullOrWhiteSpace(targetFilename)) {
                request.Error(ErrorCategory.InvalidOperation, fastPath, Constants.Messages.InvalidFilename);
                return false;
            }

            targetFilename = Path.GetFileName(targetFilename);
            if (string.IsNullOrWhiteSpace(provider.Version)) {
                request.Error(ErrorCategory.InvalidOperation, fastPath, Resources.Messages.MissingVersion);
                return false;
            }

            //the provider is installing to like this folder: \WindowsPowerShell\Modules\PackageManagement\ProviderAssemblies\nuget\2.8.5.127
            //... providername\version\.dll
            var versionFolder = Path.Combine(request.DestinationPath(request), provider.Name, provider.Version);

            // if version folder exists, remove it
            if (Directory.Exists(versionFolder))
            {
                RemoveDirectory(versionFolder);
            }

            // create the directory if we successfully deleted it
            if (!Directory.Exists(versionFolder))
            {
                Directory.CreateDirectory(versionFolder);
            }

            var targetFile = Path.Combine(versionFolder, targetFilename);

            if (file != null) {
                try
                {
                    // is that file still there?
                    if (File.Exists(targetFile)) {
                        request.Error(ErrorCategory.InvalidOperation, fastPath, Constants.Messages.UnableToRemoveFile, targetFile);
                        return false;
                    }

                    request.Debug("Copying file '{0}' to '{1}'", file, targetFile);
                    try {
                        if (File.Exists(file))
                        {
                            // if this is a file
                            File.Copy(file, targetFile);
                        }
                        else if (Directory.Exists(file))
                        {
                            // if this is a directory, copy items over
                            CopyDirectory(file, versionFolder);
                        }
                    }
                    catch (Exception ex) {
                        request.Debug(ex.StackTrace);
                        return false;
                    }

                    if (File.Exists(targetFile)) {
                        request.Verbose(Resources.Messages.InstalledPackage, provider.Name, targetFile);
                        request.YieldFromSwidtag(provider, fastPath);

                        //Load the provider. This is needed when a provider has dependencies. For example, if Nuget provider has a dependent foobar 
                        // provider and when 'Install-PackageProvider -name NuGet', we want both NuGet and Foobar provider gets loaded.
                        PackageManagementService.LoadProviderAssembly(request, targetFile, false);
                        return true;
                    }

                }
                finally
                {
                    if (deleteFile) {
                        file.TryHardToDelete();
                    }
                }
            }            

            return false;
        }

        private void RemoveDirectory(string directoryFolder)
        {
            // remove all files
            foreach (var fileToBeRemoved in Directory.EnumerateFiles(directoryFolder))
            {
                fileToBeRemoved.TryHardToDelete();
            }

            // remove all subdirectories
            foreach (var folderToBeRemoved in Directory.EnumerateDirectories(directoryFolder))
            {
                RemoveDirectory(folderToBeRemoved);
            }

            try
            {
                // now try to remove the directory
                Directory.Delete(directoryFolder);
            }
            catch { }
        }

        private void CopyDirectory(string sourceFolder, string destinationFolder)
        {
            // check that source and destination folders exist
            if (!sourceFolder.DirectoryExists() || !destinationFolder.DirectoryExists())
            {
                return;
            }

            // copy the files over
            foreach (var file in Directory.EnumerateFiles(sourceFolder))
            {
                File.Copy(file, Path.Combine(destinationFolder, Path.GetFileName(file)), true);
            }

            // copy the directories over
            foreach (var directory in Directory.EnumerateDirectories(sourceFolder))
            {
                var destinationDirName = Path.Combine(destinationFolder, Path.GetFileName(directory));

                if (!Directory.Exists(destinationDirName))
                {
                    Directory.CreateDirectory(destinationDirName);
                }

                CopyDirectory(directory, destinationDirName);
            }
        }

        private void InstallPackageFromFile(string fastPath, BootstrapRequest request)
        {
            var filePath = new Uri(fastPath).LocalPath;

            var pkg = request.GetProviderFromFile(filePath, true, false);

            if (pkg != null) {
                InstallAssemblyProvider(pkg, null, filePath, request, false);
            }
        }

        public void InstallPackage(string fastPath, BootstrapRequest request)
        {
            InstallPackage(fastPath, request, false);
        }

        internal void InstallPackage(string fastPath, BootstrapRequest request, bool errorContinue) {
            if (fastPath == null) {
                throw new ArgumentNullException("fastPath");
            }
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            // ensure that mandatory parameters are present.
            request.Debug("Calling 'Bootstrap::InstallPackage'");
            var triedAndFailed = false;

            //source can be from install - packageprovider or can be from the pipeline
            if ((request.LocalSource.Any() || fastPath.IsFile()))
            {
                InstallPackageFromFile(fastPath, request);
                return;
            }

            // verify the package integrity (ie, check if it's digitally signed before installing)

            var provider = request.GetProvider(new Uri(fastPath));
            if (provider == null || !provider.IsValid) {
                var result = errorContinue ? request.Warning(Constants.Messages.UnableToResolvePackage, fastPath) : request.Error(ErrorCategory.InvalidData, fastPath, Constants.Messages.UnableToResolvePackage, fastPath);
                return;
            }

            // first install the dependencies if any
            var dependencyLinks = provider._swidtag.Links.Where(link => link.Relationship == Iso19770_2.Relationship.Requires).GroupBy(link => link.Artifact);
            foreach (var depLinks in dependencyLinks) {
                foreach (var item in depLinks) {
                    Package packages = null;
                    if (string.IsNullOrWhiteSpace(item.Attributes[Iso19770_2.Discovery.Name])) {
                        //select the packages that marked as "latest"
                        packages = (new Feed(request, new[] {item.HRef})).Query().FirstOrDefault();
                    } else {
                        if (string.IsNullOrWhiteSpace(item.Attributes[Iso19770_2.Discovery.Version])) {
                            //select the packages that marked as "latest" and matches the name specified
                            packages = (new Feed(request, new[] { item.HRef })).Query().FirstOrDefault(p => p.Name.EqualsIgnoreCase(item.Attributes[Iso19770_2.Discovery.Name]));
                        } else {
                            //select the packages that matches version and name
                            packages = (new Feed(request, new[] { item.HRef })).Query(item.Attributes[Iso19770_2.Discovery.Name], item.Attributes[Iso19770_2.Discovery.Version]).FirstOrDefault();
                        }
                    }

                    if (packages == null) {
                        // no package found
                        request.Warning(Resources.Messages.NoDependencyPackageFound, item.HRef);
                        continue;
                    }
                    // try to install dependent providers. If fails, continue
                    InstallPackage(packages.Location.AbsoluteUri, request, errorContinue: true);
                }
            }

            // group the links along 'artifact' lines
            var artifacts = provider._swidtag.Links.Where(link => link.Relationship == Iso19770_2.Relationship.InstallationMedia).GroupBy(link => link.Artifact);


            // try one artifact set at a time.
            foreach (var artifact in artifacts) {
                // first time we succeed, we're good to go.
                foreach (var link in artifact) {
                    switch (link.Attributes[Iso19770_2.Discovery.Type]) {
                        case "assembly":
                            if (InstallAssemblyProvider(provider, link, fastPath, request)) {
                                return;
                            }
                            triedAndFailed = true;
                            continue;

                        default:
                            if (InstallProviderFromInstaller(provider, link, fastPath, request)) {
                                return;
                            }
                            triedAndFailed = true;
                            continue;
                    }
                }
            }

            if (triedAndFailed) {
                // we tried installing something and it didn't go well.
                var result = errorContinue ? request.Warning(Constants.Messages.FailedProviderBootstrap, fastPath) : request.Error(ErrorCategory.InvalidOperation, fastPath, Constants.Messages.FailedProviderBootstrap, fastPath);
            } else {
                // we didn't even find a link to bootstrap.
                var result = errorContinue ? request.Warning(Resources.Messages.MissingInstallationmedia, fastPath) : request.Error(ErrorCategory.InvalidOperation, fastPath, Resources.Messages.MissingInstallationmedia, fastPath);
            }
        }
    }
}