#if !UNIX

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

namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Provider.Utility;
    using PackageManagement.Packaging;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using System.Security.Cryptography;

    public class PackageSourceListProvider {

        private static readonly IEqualityComparer<PackageJson> PackageEqualityComparer = new Microsoft.PackageManagement.Provider.Utility.EqualityComparer<PackageJson>(
            (x, y) => x.Name.EqualsIgnoreCase(y.Name) && x.Version.EqualsIgnoreCase(y.Version), (x) => (x.Name + x.Version).GetHashCode());

        private readonly Dictionary<string, SoftwareIdentity> _fastPackReftable = new Dictionary<string, SoftwareIdentity>(StringComparer.OrdinalIgnoreCase);
        private const string PackageManagement = "PackageManagement";
        private const string RequiredPackageManagementVersion = "1.0.0.1";
        private static bool _doesPackageManagementVersionMatch = false;
        private static Version _currentPackageManagementVersion;
        private static string _pslDirLocation = Path.Combine(Environment.GetEnvironmentVariable("appdata"), Constants.ProviderName);

        /// <summary>
        /// The features that this package supports.
        /// </summary>
        internal static Dictionary<string, string[]> Features = new Dictionary<string, string[]> {

            //Required by PowerShellGet provider
            {Constants.Features.SupportsPowerShellModules, Constants.FeaturePresent},

            // specify the extensions that your provider uses for its package files (if you have any)
            {Constants.Features.SupportedExtensions, new[] {"nupkg", "msi", "WSA", "zip", "exe"}},

            // you can list the URL schemes that you support searching for packages with
            {Constants.Features.SupportedSchemes, new[] {"http", "https", "file", "ftp"}},

            // Add the minimum module version of PackageManagement
            {Constants.Features.PackageManagementMinimumVersion, new[] {RequiredPackageManagementVersion}},

            // you can list the magic signatures (bytes at the beginning of a file) that we can use
            // to peek and see if a given file is yours.
            {Constants.Features.MagicSignatures, new[] {Constants.Signatures.Zip}},

        };

       
        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <required />
        /// <returns>the name of the package provider</returns>
        public string PackageProviderName
        {
            get
            {
                return Constants.ProviderName;
            }
        }


        /// <summary>
        /// Returns the version of the Provider.
        /// </summary>
        /// <returns>The version of this provider </returns>
        public string ProviderVersion
        {
            get
            {
                return Constants.ProviderVersion;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void InitializeProvider(PackageSourceListRequest request)
        {
            request.Debug("Initialize PackageSourceListProvider");
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void GetFeatures(PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling ''{0}'::{1}'", Constants.ProviderName, "GetFeatures");

            request.Debug("Calling 'PackageSourceListProvider::GetFeatures'");
            foreach (var feature in Features) {
                request.Yield(feature);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void GetDynamicOptions(string category, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling ''{0}'::{1} ({2})'", Constants.ProviderName, "GetDynamicOptions", category);

            switch ((category ?? string.Empty).ToLowerInvariant()) {
                case "package":
                    break;

                case "source":
                    break;

                case "install":
                    request.YieldDynamicOption("Scope", "String", false, new[] {"CurrentUser", "AllUsers"});
                    request.YieldDynamicOption("SkipHashValidation", Constants.OptionType.Switch, false);
                    break;
            }
        }

        public void ResolvePackageSources(PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "ResolvePackageSources");

            var selectedSources = request.SelectedSources;

            try {
                foreach (var source in selectedSources) {
                    request.Debug(Resources.Messages.YieldingPackageSource, PackageProviderName, source);
                    request.YieldPackageSource(source.Name, source.Location, source.Trusted, source.IsRegistered, source.IsValidated);
                }
            } catch (Exception e) {
                e.Dump(request);
            }

            request.Debug(Resources.Messages.DebugInfoReturnCall, PackageProviderName, "ResolvePackageSources");
        }

        /// <summary>
        /// This is called when the user is adding (or updating) a package source
        ///
        /// If this PROVIDER doesn't support user-defined package sources, remove this method.
        /// </summary>
        /// <param name="name">The name of the package source. If this parameter is null or empty the PROVIDER should use the location as the name (if the PROVIDER actually stores names of package sources)</param>
        /// <param name="location">The location (ie, directory, URL, etc) of the package source. If this is null or empty, the PROVIDER should use the name as the location (if valid)</param>
        /// <param name="trusted">A boolean indicating that the user trusts this package source. Packages returned from this source should be marked as 'trusted'</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void AddPackageSource(string name, string location, bool trusted, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            try {

                request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "AddPackageSource - ProviderName = '{0}', name='{1}', location='{2}', trusted='{3}'", PackageProviderName, name, location, trusted);

                // Error out if a user does not provide package source Name
                if (string.IsNullOrWhiteSpace(name)) {
                    request.WriteError(ErrorCategory.InvalidArgument, Constants.Parameters.Name, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Name);
                    return;
                }

                if (string.IsNullOrWhiteSpace(location)) {
                    request.WriteError(ErrorCategory.InvalidArgument, Constants.Parameters.Location, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Location);
                    return;
                }

                // Set-PackageSource will update the existing package source. In that case IsUpdate = true.
                var isUpdate = request.GetOptionValue(Constants.Parameters.IsUpdate).IsTrue();

                request.Debug(Resources.Messages.VariableCheck, "IsUpdate", isUpdate);


                // check first that we're not clobbering an existing source, unless this is an update
                request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "FindRegisteredSource -name'{0}'", name));

                var src = request.FindRegisteredSource(name);
                if (src != null && !isUpdate) {
                    // tell the user that there's one here already
                    request.WriteError(ErrorCategory.InvalidArgument, name, Constants.Messages.PackageSourceExists, name);
                    return;
                }

                // conversely, if it didn't find one, and it is an update, that's bad too:
                if (src == null && isUpdate)
                {
                    // you can't find that package source? Tell that to the user
                    request.WriteError(ErrorCategory.ObjectNotFound, name, Constants.Messages.UnableToResolveSource, name);
                    return;
                }

                // ok, we know that we're ok to save this source
                // next we check if the location is valid (if we support that kind of thing)
                var validated = false;
                validated = request.ValidateSourceLocation(location);
                if (!validated) {
                     request.WriteError(ErrorCategory.InvalidData, name, Constants.Messages.SourceLocationNotValid, location);
                     return;
                }
                else
                { 
                    request.Verbose(Resources.Messages.SuccessfullyValidated, name);
                }

                bool force = request.GetOptionValue("Force") != null;
                //if source is UNC location/ copy it to local path;
                Uri uri;
                if (Uri.TryCreate(location, UriKind.Absolute, out uri))
                {
                    if (uri.IsFile && uri.IsUnc)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(location);
                        string directory = Path.GetDirectoryName(location);
                        string catalogFilePath = Path.Combine(directory, fileName+".cat");
                        if (!File.Exists(catalogFilePath))
                        {
                            request.WriteError(ErrorCategory.InvalidData, location, Resources.Messages.CatalogFileMissing, location);
                            return;
                        }
                        if (!TestCatalogFile(location, catalogFilePath, request))
                        {
                            return;
                        }
                        if (force || request.ShouldContinue(Resources.Messages.QueryDownloadPackageSourceList.format(location), Resources.Messages.PackageSourceListNotTrusted))
                        {                            
                            string destination = Path.Combine(_pslDirLocation, Path.GetFileName(uri.LocalPath));
                            if (File.Exists(destination))
                            {
                                if (force || request.ShouldContinue(Resources.Messages.OverwriteFile, Resources.Messages.FileExists))
                                {
                                    File.Copy(location, destination, true);
                                    location = destination;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else {
                                File.Copy(location, destination);
                                location = destination;
                            }                  
                        }
                        else
                        {
                            return;
                        }
                    }
                }                   

                // it's good to check just before you actually write something to see if the user has cancelled the operation
                if (request.IsCanceled) {
                    return;
                }

                // looking good -- store the package source.
                request.AddPackageSource(name, location, trusted, validated);

                // Yield the package source back to the caller.
                request.YieldPackageSource(name, location, trusted, true /*since we just registered it*/, validated);
            } catch (Exception e) {
                e.Dump(request);
            }
        }

        /// <summary>
        /// Removes/Unregisters a package source
        /// </summary>
        /// <param name="name">The name or location of a package source to remove.</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void RemovePackageSource(string name, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "RemovePackageSource");

            var src = request.FindRegisteredSource(name);
            if (src == null) {
                request.Warning(Constants.Messages.UnableToResolveSource, Constants.ProviderName, name);
                return;
            }

            request.RemovePackageSource(src.Name);
            request.YieldPackageSource(src.Name, src.Location, src.Trusted, false, src.IsValidated);
        }


        /// <summary>
        /// Searches package sources given name and version information
        ///
        /// Package information must be returned using <c>request.YieldPackage(...)</c> function.
        /// </summary>
        /// <param name="name">a name or partial name of the package(s) requested</param>
        /// <param name="requiredVersion">A specific version of the package. Null or empty if the user did not specify</param>
        /// <param name="minimumVersion">A minimum version of the package. Null or empty if the user did not specify</param>
        /// <param name="maximumVersion">A maximum version of the package. Null or empty if the user did not specify</param>
        /// <param name="id">if this is greater than zero (and the number should have been generated using <c>StartFind(...)</c>, the core is calling this multiple times to do a batch search request. The operation can be delayed until <c>CompleteFind(...)</c> is called</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "FindPackage' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", name, requiredVersion, minimumVersion, maximumVersion));

            // no package name or the name with wildcard
            if (string.IsNullOrWhiteSpace(name) || WildcardPattern.ContainsWildcardCharacters(name))
            {
                // In the case of the package name is null or contains wildcards, error out if a user puts version info
                if (!string.IsNullOrWhiteSpace(requiredVersion) || !string.IsNullOrWhiteSpace(minimumVersion) || !string.IsNullOrWhiteSpace(maximumVersion))
                {
                    request.Warning(Resources.Messages.WildCardCharsAreNotSupported, name);
                    return;
                }
       
                var packages = request.GetPackages(name);
                if (request.GetOptionValue("AllVersions").IsTrue())
                {
                    // if version is specified then name can not be empty or with wildcard. in this case the cmdlet has been errored out already.
                    // here we just return all packages we can find
                    if (request.FilterOnVersion(packages, requiredVersion, minimumVersion, maximumVersion, minInclusive: true, maxInclusive: true, latest: false).OrderBy(p => p.Name).Any(p => !request.YieldFromSwidtag(p, p.Name)))
                    {
                        return;
                    }
                    return;
                }

                //return the latest version
                if (packages.GroupBy(p => p.Name)
                        .Select(each => each.OrderByDescending(pp => pp.Version).FirstOrDefault()).OrderBy(p=>p.Name).Any( item =>!request.YieldFromSwidtag(item, item.Name)))
                {
                    return;
                }
                
            } else {
 
                // a user specifies name
                // asked for a specific version?
                if (!string.IsNullOrWhiteSpace(requiredVersion))
                {
                    request.YieldFromSwidtag(request.GetPackage(name, requiredVersion), name);
                    return;
                }

                var allVersion = request.GetOptionValue("AllVersions").IsTrue();
                // asked for a version range?
                if (!string.IsNullOrWhiteSpace(minimumVersion) || !string.IsNullOrEmpty(maximumVersion) || allVersion)
                {
                    var packages = request.GetPackagesWithinVersionRange(name, minimumVersion, maximumVersion);
                    if (allVersion)
                    {
                        if (packages.Any(p => !request.YieldFromSwidtag(p, name)))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (request.YieldFromSwidtag(packages.FirstOrDefault(), name))
                        {
                            return;
                        }
                    }

                    return;
                }

                // just return by name
                request.YieldFromSwidtag(request.GetPackage(name), name);
            }
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
        public void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "GetInstalledPackages' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", name, requiredVersion, minimumVersion, maximumVersion));

            IEnumerable<PackageJson> packagesDefinedInJsonSpec;

            if (string.IsNullOrWhiteSpace(name) || WildcardPattern.ContainsWildcardCharacters(name)) {
                // In the case of the package name is null or contains wildcards, error out if a user puts version info
                if (!string.IsNullOrWhiteSpace(requiredVersion) || !string.IsNullOrWhiteSpace(minimumVersion) || !string.IsNullOrWhiteSpace(maximumVersion))
                {
                    request.Warning(Resources.Messages.WildCardCharsAreNotSupported, name);
                    return;
                }
                packagesDefinedInJsonSpec = request.GetPackages(name).ToArray();
 
            } else {
                //return all installed
                packagesDefinedInJsonSpec = request.GetPackages(name, requiredVersion, minimumVersion, maximumVersion).ToArray();
            }
            _fastPackReftable.Clear();

            if (!packagesDefinedInJsonSpec.Any())
            {
                request.Verbose(Resources.Messages.NoPackageFound, Constants.ProviderName);
                return;
            }

            foreach (var package in packagesDefinedInJsonSpec) {
                switch (package.Type.ToLowerInvariant()) {

                    case Constants.MediaType.AppxPackage:
                        //TODO for future
                        break;
                    case Constants.MediaType.PsArtifacts:
                        PowerShellArtifactInstaller.GeInstalledPowershellArtifacts(package, requiredVersion, minimumVersion,maximumVersion, _fastPackReftable, request);
                        break;
                    case Constants.MediaType.ExePackage:
                        //program provider can handle get-package git for git.exe
                        ExePackageInstaller.GetInstalledExePackages(package, requiredVersion, minimumVersion, minimumVersion, request);
                        break;
                    case Constants.MediaType.MsiPackage:
                        //msi provider can handle get-package node.js for node.js.msi                       
                        GetMsiInstalledPackage(name, package, requiredVersion, minimumVersion, maximumVersion, request);
                        break;
                    case Constants.MediaType.ZipPackage:
                        ZipPackageInstaller.GetInstalledZipPackage(package, request);
                        break;
                    case Constants.MediaType.NuGetPackage:
                        NupkgInstaller.GeInstalledNuGetPackages(package, requiredVersion, minimumVersion, maximumVersion, _fastPackReftable, request);
                        break;

                } //switch
            }
        }

        private void GetMsiInstalledPackage(string name, PackageJson package, string requiredVersion, string minimumVersion, string maximumVersion, PackageSourceListRequest request) {

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "GetMsiInstalledPackage' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", name, requiredVersion, minimumVersion, maximumVersion));

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, request);
            if (provider != null) {
                var packagesInstalled = provider.GetInstalledPackages(package.Name, requiredVersion, minimumVersion, maximumVersion, request);

                if (!packagesInstalled.Any()) {
                    packagesInstalled = provider.GetInstalledPackages(package.DisplayName, requiredVersion, minimumVersion, maximumVersion, request);
                }
                foreach (var i in packagesInstalled)
                {
                    request.Debug("found a package '{0}.{1}' installed from '{2}'", i.Name, i.Version, i.Source);
 
                    var info = PackageSourceListRequest.MakeFastPathComplex(i.Source, name, (package.DisplayName?? ""), i.Version, i.FastPackageReference);

                    _fastPackReftable.AddOrSet(i.FastPackageReference, i);

                    // check if the installed version matches with the one specified in the PSL.json.
                    // If so, we choose PSL.json.
                    var version = i.Version.CompareVersion(package.Version) ? package.Version : i.Version;
                    //we use displayname here because msi provider uses the displayname. 
                    request.YieldSoftwareIdentity(info, package.DisplayName, version, i.VersionScheme, i.Summary, package.Source, i.SearchKey, i.FullPath, i.PackageFilename);
                    return;
                }           
            }
        }
        private void UnInstallMsiPackage(PackageSourceListRequest request,  string fastPath, PackageJson package)
        {
            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "UnInstallMsiPackage' - name='{0}', fastPath='{1}'", package.Name, fastPath));

            string sourceLocation;
            string id;
            string displayName;
            string version;
            string fastPackageReference;

            if (!request.TryParseFastPathComplex(fastPath: fastPath, regex: PackageSourceListRequest.RegexFastPathComplex, location: out sourceLocation, id: out id, displayname: out displayName, version: out version, fastpath: out fastPackageReference))
            {
                //we don't need to error out even if fastpath is not correct because msi provider is expected to handle the uninstall-package.
                request.Verbose(Resources.Messages.UnsupportMSIUninstall, Constants.ProviderName, package.Name);
                return;
            }

            // Normally uninstall-package will be handled by MSI provider. Here we added a special case for handling uninstall-package nodejs
            // which msi provider unable to deal with (node.js only for msi)
            if (id != null && id.EqualsIgnoreCase("nodejs"))
            {

                var provider = PackageSourceListRequest.FindProvider(request, Constants.ProviderNames.Msi, request);

                if (provider != null)
                {

                    if (!_fastPackReftable.ContainsKey(fastPackageReference))
                    {
                        request.WriteError(ErrorCategory.InvalidData, fastPackageReference, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPackageReference);
                        return;
                    }

                    request.Verbose(Resources.Messages.UninstallingPackage, Constants.ProviderName, package.Name);

                    var p = _fastPackReftable[fastPackageReference];

                    var installing = provider.UninstallPackage(p, request);

                    foreach (var i in installing)
                    {
                        request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme,
                            i.Summary, package.Source, i.SearchKey, i.FullPath, i.PackageFilename);
                        if (request.IsCanceled)
                        {
                            installing.Cancel();
                        }
                        return;
                    }
                }
            }
            else
            {
                //no-op for uninstalling the msi packages. only install-package nodejs is supported because msi can not handle it
                request.Verbose(Resources.Messages.UnsupportMSIUninstall, Constants.ProviderName, package.Name);
                return;
            }
           
        }


        /// <summary>
        /// Uninstalls a package
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void UninstallPackage(string fastPackageReference, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, fastPackageReference);

            var package = request.GetFastReferenceComplex(fastPackageReference);
            if (package == null)
            {
                request.WriteError(ErrorCategory.InvalidData, fastPackageReference, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPackageReference);
                return;
            }

            switch (package.Type.ToLowerInvariant()) {

                case Constants.MediaType.AppxPackage:
                    //TODO for the future
                    break;
                case Constants.MediaType.ExePackage:
                    ExePackageInstaller.UninstallExePackage(fastPackageReference, request);
                    break;
                case Constants.MediaType.MsiPackage:
                    
                    UnInstallMsiPackage(request, fastPackageReference, package);
                    break;
                case Constants.MediaType.ZipPackage:
                    ZipPackageInstaller.UnInstallZipPackage(request, fastPackageReference);
                    break;
                case Constants.MediaType.NuGetPackage:
                    NupkgInstaller.UninstallNuGetPackage(package, fastPackageReference, request, _fastPackReftable);                   
                    break;
                case Constants.MediaType.PsArtifacts:
                    PowerShellArtifactInstaller.UninstallPowershellArtifacts(package, fastPackageReference, request, _fastPackReftable);
                    break;
                default:
                    request.WriteError(ErrorCategory.InvalidData, fastPackageReference, Resources.Messages.UnknownMediaType, package.Name, package.Source, package.Type);
                    break;
            }
        }


        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void DownloadPackage(string fastPath, string location, PackageSourceListRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "DownloadPackage' - fastReference='{0}', location='{1}'", fastPath, location));

            var package = request.GetPackageByFastPath(fastPath);

            if (package == null)
            {
                request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPath);
                return;
            }
            
            switch (package.Type.ToLowerInvariant()) {
                case Constants.MediaType.MsiPackage:
                    //No-op as the msi provider does not support save-package
                    break;
                case Constants.MediaType.MsuPackage:
                    //No-op as the msu provider does not support save-package
                    break;
                case Constants.MediaType.AppxPackage:
                    //TODO for future whenever needed to support appx packages
                    break;
                case Constants.MediaType.NuGetPackage:
                    NupkgInstaller.DownloadNuGetPackage(fastPath, location, request);
                    break;
                case Constants.MediaType.ZipPackage:
                    //TODO
                    ZipPackageInstaller.DownloadZipPackage(fastPath, location, request);
                    break;
                case Constants.MediaType.ExePackage:
                    //TODO
                    ExePackageInstaller.DownloadExePackage(fastPath, location, request);
                    break;
                case Constants.MediaType.PsArtifacts:
                    //TODO
                    break;

                default:
                    request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.UnknownMediaType, package.Name, package.Source, package.Type);
                    return;
            }          
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Plugin requirement.")]
        public void InstallPackage(string fastPath, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (!ValidatePackageManagementVersion(request))
            {
                return;
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "InstallPackage' - fastReference='{0}'", fastPath));

            var package = request.GetPackageByFastPath(fastPath);
            if (package == null) 
            {
                request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPath);
                return;
            }

            InstallProviderFromInstaller(package, fastPath, request);
        }

        internal static void InstallProviderFromInstaller(PackageJson package, string fastPath, PackageSourceListRequest request) {
           
            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "InstallProviderFromInstaller' - name='{0}', fastPath='{1}'", package.Name, fastPath));

            //install any dependency packages
            InstallDependencies(package, request);

            switch (package.Type.ToLowerInvariant()) {
                case Constants.MediaType.MsiPackage:
                case Constants.MediaType.MsuPackage:
                    InstallPackageFile(package, fastPath, request);
                    break;
                case Constants.MediaType.AppxPackage:
                    //TODO for future whenever needed to support appx packages
                    break;
                case Constants.MediaType.NuGetPackage:
                    NupkgInstaller.InstallNuGetPackage(package, fastPath, request);
                    break;
                case Constants.MediaType.ZipPackage:
                    ZipPackageInstaller.InstallZipPackage(package, fastPath, request);
                    break;
                case Constants.MediaType.ExePackage:
                    ExePackageInstaller.InstallExePackage(package, fastPath, request);
                    break;               
                case Constants.MediaType.PsArtifacts:
                    PowerShellArtifactInstaller.InstallPowershellArtifacts(package, fastPath, request);
                    break;

                default:
                    request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.UnknownMediaType, package.Name, package.Source, package.Type);
                   break;
            }
            return;
        }

        private static void InstallPackageFile(PackageJson package, string fastPath, PackageSourceListRequest request)
        {
            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "InstallPackageFile' - name='{0}', fastPath='{1}'", package.Name, fastPath));

            // get a temp file name, msi or msu
            var providerType = package.Type.ToLowerInvariant();
            var destination = Path.ChangeExtension(Path.GetTempFileName(), providerType);

            //download the msi package to the temp file
            WebDownloader.DownloadFile(package.Source, destination, request, null);
            
            if (!File.Exists(destination))
            {
                return;
            }


            // validate the file
            if (!WebDownloader.VerifyHash(destination,package, request))
            {
                return;
            }
           
            if (!package.IsTrustedSource)
            {
                if (!request.ShouldContinueWithUntrustedPackageSource(package.Name, package.Source))
                {
                    request.Warning(Constants.Messages.UserDeclinedUntrustedPackageInstall, package.Name);
                    return;
                }
            }

            var installRequest = PackageSourceListRequest.ExtendRequest(new Dictionary<string, string[]>
                           {
                                {"Source", new[] {package.Source ?? ""}}
                           }, new[] { package.Source ?? "" }, package.IsTrustedSource, request);

            request.Verbose(Resources.Messages.CallMsiForInstall, package.Name);
            if (request.ProviderServices.Install(destination, "", installRequest))
            {
                // it installed ok!exit
                request.YieldFromSwidtag(package, fastPath);
                return;
            }
            else
            {
                request.WriteError(ErrorCategory.InvalidOperation, Constants.ProviderName, Resources.Messages.PackageFailedInstall, package.Name);
            }
        }

        internal static void InstallDependencies(PackageJson packageJson, PackageSourceListRequest request)
        {
            //TODO dependency chain check

            //let's install dependency first in case it is needed for installing the actual package
            if (packageJson.DependencyObjects != null)
            {
                var dependencies = GetDependencies(packageJson, request);

                if (dependencies != null)
                {
                    foreach (var dep in dependencies.Where(dep => (dep != null) && !dep.IsCommonDefinition))
                    {
                        //dependency source trusty follows its parent
                        dep.IsTrustedSource |= packageJson.IsTrustedSource;
                        var id = PackageSourceListRequest.CreateCanonicalId(dep, dep.Name);
                        InstallProviderFromInstaller(dep, id, request);
                    }
                }
            }
        }

        private static IEnumerable<PackageJson> GetDependencies(PackageJson packageJson, PackageSourceListRequest request) {

            if (packageJson.DependencyObjects == null) {
                yield break;
            }

            bool force = request.GetOptionValue("Force") != null;
            foreach (var dep in packageJson.DependencyObjects.Where(dep => (dep != null) && !dep.IsCommonDefinition)) {
                if (!force) {
                    var provider = PackageSourceListRequest.FindProvider(request, dep.Type, request, true);

                    if(provider == null)
                    {
                        //FindProvider logged an error already
                        break;
                    }
                    //Check whether the dependency package is installed
                    var installedPackages = provider.GetInstalledPackages(dep.Name, requiredVersion: null, minimumVersion: dep.Version, maximumVersion: null, requestObject: request);

                    if (installedPackages == null || !installedPackages.Any()) {
                        request.Verbose(Resources.Messages.DependencyNotInstalled, dep.Name);
                        yield return dep;
                    } else {
                        request.Verbose(Resources.Messages.DependencyInstalled, dep.Name);
                    }
                } else {
                    yield return dep;
                }
            }
        }
      
        private bool ValidatePackageManagementVersion(PackageSourceListRequest request)
        {
            var userSpecifiedProvider = request.GetOptionValue("ProviderName") ?? request.GetOptionValue("Provider");

            if (_currentPackageManagementVersion == null)
            {
                var moduleInfo = GetModule("PackageManagement");
                              
                if(moduleInfo == null || !moduleInfo.Any())
                {
                    request.Verbose(Resources.Messages.CannotFindPackageManagementVersion);
                    return false;
                }
              
                _currentPackageManagementVersion = moduleInfo.OrderByDescending(each => each.Version).FirstOrDefault().Version;
                if (_currentPackageManagementVersion < new Version(RequiredPackageManagementVersion))
                {
                    _doesPackageManagementVersionMatch = false;
                }
                else
                {
                    _doesPackageManagementVersionMatch = true;
                }
            }

            if (!_doesPackageManagementVersionMatch)
            {
                if (userSpecifiedProvider != null && userSpecifiedProvider.IndexOf(Constants.ProviderName, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    request.WriteError(ErrorCategory.InvalidOperation, Constants.ProviderName, Resources.Messages.MininumVersonCheck, Constants.ProviderName, RequiredPackageManagementVersion, _currentPackageManagementVersion);
                }
                else
                {
                    request.Verbose(Resources.Messages.MininumVersonCheck, Constants.ProviderName, RequiredPackageManagementVersion, _currentPackageManagementVersion);
                }
            }

            return _doesPackageManagementVersionMatch;
        }

        internal static IEnumerable<PSModuleInfo> GetModule(string moduleName)
        {
  
            using (PowerShell powershell = PowerShell.Create())
            {
                if (powershell != null)
                {
                    return powershell
                        .AddCommand("Get-Module")
                        .AddParameter("Name", moduleName)
                        .AddParameter("ListAvailable")
                        .Invoke<PSModuleInfo>();
                }
            }

            return Enumerable.Empty<PSModuleInfo>();

        }

        internal static bool TestCatalogFile(string jsonFile, string catalogFile, PackageSourceListRequest request)
        {
            try
            {
                PSObject result = null;
                using (PowerShell powershell = PowerShell.Create())
                {
                    if (powershell != null)
                    {
                        result = powershell
                        .AddCommand("Test-FileCatalog")
                        .AddParameter("CatalogFilePath", catalogFile)
                        .AddParameter("Path", jsonFile)
                        .Invoke().FirstOrDefault();
                    }
                    if (result.ToString().EqualsIgnoreCase("Valid"))
                        return true;
                }
            }
            catch(Exception ex) 
            {
                request.WriteError(ErrorCategory.InvalidData, catalogFile, Resources.Messages.CatalogFileVerificationFailedWithError, catalogFile, ex.Message.ToString());
                return false;
            }

            request.WriteError(ErrorCategory.InvalidData, catalogFile, Resources.Messages.CatalogFileVerificationFailed, jsonFile);
            return false;

        }

    }
}

#endif