
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

namespace Microsoft.PackageManagement.Internal.Implementation {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.AccessControl;
    using Api;
    using PackageManagement.Implementation;
    using PackageManagement.Packaging;
    using Packaging;
    using Providers;
    using Utility.Collections;
    using Utility.Extensions;
    using Utility.Platform;
    using Utility.Plugin;
    using Utility.Versions;
    using Win32;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
#if CORECLR
    using System.Management.Automation;
#endif

    /// <summary>
    ///     The Client API is designed for use by installation hosts:
    ///     - PackageManagement Powershell Cmdlets
    ///     The Client API provides high-level consumer functions to support SDII functionality.
    /// </summary>
    internal class PackageManagementService : IPackageManagementService {
       
        private static int _lastCallCount;
        private static HashSet<string> _providersTriedThisCall;
        private string[] _bootstrappableProviderNames;
        private bool _initialized;
        
        // well known, built in provider assemblies.
        private readonly string[] _defaultProviders = {
            Path.GetFullPath(CurrentAssemblyLocation), // load the providers from this assembly
            "Microsoft.PackageManagement.MetaProvider.PowerShell.dll"
        };

        private readonly object _lockObject = new object();
        private readonly IDictionary<string, IMetaProvider> _metaProviders = new Dictionary<string, IMetaProvider>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, PackageProvider> _packageProviders = new Dictionary<string, PackageProvider>(StringComparer.OrdinalIgnoreCase);
        internal readonly IDictionary<string, Archiver> Archivers = new Dictionary<string, Archiver>(StringComparer.OrdinalIgnoreCase);
        internal readonly IDictionary<string, Downloader> Downloaders = new Dictionary<string, Downloader>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PackageProvider>> _providerCacheTable = new Dictionary<string, List<PackageProvider>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, byte[]> _providerFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private string _baseDir;
        internal bool InternalPackageManagementInstallOnly = false;
        private readonly string _nuget ="NuGet";

        internal enum ProviderOption
        {
            AllProvider = 0,
            LatestVersion = 1,
        }

        internal Dictionary<string, List<PackageProvider>> ProviderCacheTable
        {
            get
            {
                return _providerCacheTable;
            }
        }

        internal static string CurrentAssemblyLocation
        {
            get
            {
#if !CORECLR
                return Assembly.GetExecutingAssembly().Location;
#else
                return typeof(PackageManagementService).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
#endif
            }
        }

        internal string BaseDir {
            get {
                return _baseDir ?? (_baseDir = Path.GetDirectoryName(CurrentAssemblyLocation));
            }
        }

        internal string[] BootstrappableProviderNames {
            get {
                return _bootstrappableProviderNames ?? new string[0];
            }
            set {
                if (_bootstrappableProviderNames.IsNullOrEmpty()) {
                    _bootstrappableProviderNames = value;
                }
            }
        }

        internal IEnumerable<string> AutoLoadedAssemblyLocations {
            get
            {
                var folder = Path.GetDirectoryName(CurrentAssemblyLocation);
                if (!string.IsNullOrWhiteSpace(folder) && folder.DirectoryExists()) {
                    yield return folder;
                }
            }
        }
        internal IEnumerable<string> ProviderAssembliesLocation {
            get {
                var folder = SystemAssemblyLocation;
                if (!string.IsNullOrWhiteSpace(folder) && folder.DirectoryExists()) {
                    yield return folder;
                }

                folder = UserAssemblyLocation;
                if (!string.IsNullOrWhiteSpace(folder) && folder.DirectoryExists()) {
                    yield return folder;
                }
            }
        }
#if CORECLR
        private IEnumerable<string> PowerShellModulePath {
            get {
                var psModulePath = Environment.GetEnvironmentVariable("PSModulePath") ?? "";
                var paths = psModulePath.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToArray();
                return paths;
            }
        }
#endif
        internal string UserAssemblyLocation {
            get {
                try {
#if !CORECLR
                    var basepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#else
                    var basepath = System.Environment.GetEnvironmentVariable("LocalAppData");
#endif
                    if (string.IsNullOrWhiteSpace(basepath)) {
                        return null;
                    }

                    var path = Path.Combine(basepath, @"PackageManagement\ProviderAssemblies");
                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                } catch {
                    // if it can't be created, it's not the end of the world.
                }
                return null;
            }
        }

        internal string SystemAssemblyLocation {
            get {
                try {
#if !CORECLR
                    var basepath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
#else
                    var basepath = System.Environment.GetEnvironmentVariable("ProgramFiles");
#endif
                    if (string.IsNullOrWhiteSpace(basepath)) {
                        return null;
                    }
                    var path = Path.Combine(basepath, @"PackageManagement\ProviderAssemblies");

                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                } catch {
                    // ignore non-existant directory for now.
                }
                return null;
            }
        }

        public IEnumerable<PackageProvider> PackageProviders {
            get {
                return _packageProviders.Values;
            }
        }

        public bool Initialize(IHostApi request) {
            lock (_lockObject) {
                if (!_initialized) {
                    LoadProviders(request);
                    _initialized = true;
                }
            }
            return _initialized;
        }

        public int Version {
            get {
                return Constants.PackageManagementVersion;
            }
        }

        public IEnumerable<string> ProviderNames {
            get {
                return _packageProviders.Keys;
            }
        }

        public IEnumerable<string> AllProviderNames {
            get {
                if (BootstrappableProviderNames.IsNullOrEmpty()) {
                    return _packageProviders.Keys;
                }

               return _packageProviders.Where(p => p.Value != null && (p.Value.Features == null || !p.Value.Features.ContainsKey(Constants.Features.AutomationOnly)))
                    .Select(each => each.Key).Concat(BootstrappableProviderNames).Distinct(StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<PackageProvider> SelectProvidersWithFeature(string featureName) {
            return _packageProviders.Values.Where(each => each.Features.ContainsKey(featureName));
        }

        public IEnumerable<PackageProvider> SelectProvidersWithFeature(string featureName, string value) {
            return _packageProviders.Values.Where(each => each.Features.ContainsKey(featureName) && each.Features[featureName].Contains(value));
        }

        public IEnumerable<PackageProvider> SelectProviders(string providerName, IHostApi hostApi) {
            if (!string.IsNullOrWhiteSpace(providerName)) {
                // match with wildcards
                var results = _packageProviders.Values.Where(each => each.ProviderName.IsWildcardMatch(providerName)).ReEnumerable();
                if (results.Any()) {
                    return results;
                }

                // If the provider is installed but not imported, let's import it 
                // we don't want import package provider via name to write errors, because in that case the subsequent call to bootstrapper provider will get cancelled.
                var provider = ImportPackageProviderHelper(hostApi, providerName, null, null, null, false, false, false).ToArray();
                if (provider.Any()) {
                    return provider;
                }

                if (hostApi != null && !providerName.ContainsWildcards()) {
                    // if the end user requested a provider that's not there. perhaps the bootstrap provider can find it.
                    if (RequirePackageProvider(null, providerName, Constants.MinVersion, hostApi)) {
                        // seems to think we found it.
                        if (_packageProviders.ContainsKey(providerName)) {
                            return _packageProviders[providerName].SingleItemAsEnumerable();
                        }
                    }

                    // SelectProviders() is iterating through the loaded provider list. As we still need to go through the
                    // unloaded provider list, we should not warn users yet at this point of time.
                    // If the provider is not found, eventually we will error out in SelectProviders()/cmdletbase.cs(). 

                    //hostApi.Warn(hostApi.FormatMessageString(Constants.Messages.UnknownProvider, providerName));                   
                }
                return Enumerable.Empty<PackageProvider>();
            } else {
                // If a user does not specify -provider or -provider name, we will bootstrap the nuget provider if it does not exist.                
                //Only find, install, uninstall, and save cmdlets requires the bootstrap.                   
                var bootstrapNuGet = hostApi.GetOptionValues(Constants.BootstrapNuGet).FirstOrDefault();

                if ((bootstrapNuGet != null) && bootstrapNuGet.EqualsIgnoreCase("true")) {
                    //check if the NuGet provider is already loaded                   
                    if (!_packageProviders.Keys.Any(each => each.EqualsIgnoreCase(_nuget))) {
                        //we'll bootstrap NuGet provider under the following cases:
                        //case 1: on a clean VM, type install-package foobar
                        //case 2: on a existing VM, if the nuget provider does not exist and type install-package foobar
                        //case 3: An existing VM has a old version of the NuGet installed, no bootstrap will occur. This means there is no changes
                        //        to the user, unless he does 'install-packageprovider -name nuget -force'.
                        if (RequirePackageProvider(null, _nuget, Constants.MinVersion, hostApi)) {
                            // seems to think we found it.
                            if (_packageProviders.ContainsKey(_nuget)) {
                                return PackageProviders.Concat(_packageProviders[_nuget].SingleItemAsEnumerable());
                            }
                        }
                    }
                }
            }

            return PackageProviders;
        }

        public IEnumerable<SoftwareIdentity> FindPackageByCanonicalId(string packageId, IHostApi hostApi) {
            Uri pkgId;
            if (Uri.TryCreate(packageId, UriKind.Absolute, out pkgId)) {
                var segments = pkgId.Segments;
                if (segments.Length > 0) {
                    var provider = SelectProviders(pkgId.Scheme, hostApi).FirstOrDefault();
                    if (provider != null) {
                        var name = Uri.UnescapeDataString(segments[0].Trim('/', '\\'));
                        var version = (segments.Length > 1) ? Uri.UnescapeDataString(segments[1]) : null;
                        var source = pkgId.Fragment.TrimStart('#');
                        var sources = (string.IsNullOrWhiteSpace(source) ? hostApi.Sources : Uri.UnescapeDataString(source).SingleItemAsEnumerable()).ToArray();

                        var host = new object[] {
                            new {
                                GetSources = new Func<IEnumerable<string>>(() => sources),
                                GetOptionValues = new Func<string, IEnumerable<string>>(key => key.EqualsIgnoreCase("FindByCanonicalId") ? new[] {"true"} : hostApi.GetOptionValues(key)),
                                GetOptionKeys = new Func<IEnumerable<string>>(() => hostApi.OptionKeys.ConcatSingleItem("FindByCanonicalId")),
                            },
                            hostApi,
                        }.As<IHostApi>();

                        return provider.FindPackage(name, version, null, null, host).Select(each => {
                            each.Status = Constants.PackageStatus.Dependency;
                            return each;
                        }).ReEnumerable();
                    }
                }
            }
            return new SoftwareIdentity[0];
        }

        public bool RequirePackageProvider(string requestor, string packageProviderName, string minimumVersion, IHostApi hostApi) {
            // check if the package provider is already installed
            if (_packageProviders.ContainsKey(packageProviderName)) {
                var current = _packageProviders[packageProviderName].Version;
                if (current >= minimumVersion) {
                    return true;
                }
            }

            var currentCallCount = hostApi.CallCount;

            if (_lastCallCount >= currentCallCount) {
                // we've already been here this call.

                // are they asking for the same provider again?
                if (_providersTriedThisCall.Contains(packageProviderName)) {
                    hostApi.Debug("Skipping RequirePackageProvider -- tried once this call previously.");
                    return false;
                }
                // remember this in case we come back again.
                _providersTriedThisCall.Add(packageProviderName);
            } else {
                _lastCallCount = currentCallCount;
                _providersTriedThisCall = new HashSet<string> {
                    packageProviderName
                };
            }

            if (!hostApi.IsInteractive) {
                hostApi.Debug("Skipping RequirePackageProvider due to not interactive");
                // interactive indicates that the host can respond to queries -- this doesn't happen
                // in powershell during tab-completion.
                return false;
            }

            // no?
            // ask the bootstrap provider if there is a package provider with that name available.
            if (!_packageProviders.ContainsKey("Bootstrap")) {
                return false;
            }
            
            var bootstrap = _packageProviders["Bootstrap"];
            if (bootstrap == null) {
                hostApi.Debug("Skipping RequirePackageProvider due to missing bootstrap provider");
                return false;
            }

            var pkg = bootstrap.FindPackage(packageProviderName, null, minimumVersion, null, hostApi).OrderByDescending(p =>  p, SoftwareIdentityVersionComparer.Instance).GroupBy(package => package.Name).ToArray();
            if (pkg.Length == 1) {
                // Yeah? Install it.
                var package = pkg[0].FirstOrDefault();
                var metaWithProviderType = package.Meta.FirstOrDefault(each => each.ContainsKey("providerType"));
                var providerType = metaWithProviderType == null ? "unknown" : metaWithProviderType.GetAttribute("providerType");
                var destination = providerType == "assembly" ? (AdminPrivilege.IsElevated ? SystemAssemblyLocation : UserAssemblyLocation) : string.Empty;
                var link = package.Links.FirstOrDefault(each => each.Relationship == "installationmedia");
                var location = string.Empty;
                if (link != null) {
                    location = link.HRef.ToString();
                }

                // what can't find an installationmedia link?
                // todo: what should we say here?
                if (hostApi.ShouldBootstrapProvider(requestor, package.Name, package.Version, providerType, location, destination)) {
                    var newRequest = hostApi.Extend<IHostApi>(new {
                        GetOptionValues = new Func<string, IEnumerable<string>>(key => {
                            if (key == "DestinationPath") {
                                return new[] {
                                    destination
                                };
                            }
                            return new string[0];
                        })
                    });
                    var packagesInstalled = bootstrap.InstallPackage(package, newRequest).LastOrDefault();
                    if (packagesInstalled == null) {
                        // that's sad.
                        hostApi.Error(Constants.Messages.FailedProviderBootstrap, ErrorCategory.InvalidOperation.ToString(), package.Name, hostApi.FormatMessageString(Constants.Messages.FailedProviderBootstrap, package.Name));
                        return false;
                    }
                    // so it installed something
                    // we must tell the plugin loader to reload the plugins again.
                    LoadProviders(hostApi);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all available providers. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="providerNames">providers to be loaded.</param>
        public IEnumerable<PackageProvider> GetAvailableProviders(IHostApi request, string[] providerNames) {

            //Handling two cases
            //1. Both "-Name" and "-Listavailable" exist
            //2. "-Listavailable" only. 

            return providerNames.IsNullOrEmpty() ?
                GetAvailableProvider(request, String.Empty) :
                    providerNames.SelectMany(each => GetAvailableProvider(request, each));
        }

        /// <summary>
        /// Get available provider. It handles "Get-Packageprovider -Name -ListAvailable" and "Get-Packageprovider -ListAvailable"
        /// </summary>
        /// <param name="request"></param>
        /// <param name="providerName">Name of the provider to be loaded.</param>
        private IEnumerable<PackageProvider> GetAvailableProvider(IHostApi request, string providerName) {

            //This method is called when get-packageprovider -ListAvailable
            //We will return whatever we can find
            ScanForAvailableProviders(request, providerName, null, null, null);

            //Check if the provider is in the cache
            var packageProviders = GetPackageProviderFromCacheTable(providerName).ReEnumerable();

            return packageProviders.Any() ? packageProviders.Where(p => p.Features == null || !p.Features.ContainsKey(Constants.Features.AutomationOnly))
                : Enumerable.Empty<PackageProvider>();
        }


        private IEnumerable<PackageProvider> GetPackageProviderFromCacheTable(string providerName)
        {
            // latest version of the providers will be displayed first
            var cacheList = (string.IsNullOrWhiteSpace(providerName)) ? _providerCacheTable.SelectMany(each => each.Value.OrderByDescending(provider => provider.Version)).WhereNotNull()
                 : _providerCacheTable.Where(each => each.Key.IsWildcardMatch(providerName)).SelectMany(each => each.Value.OrderByDescending(provider => provider.Version)).WhereNotNull();

            return cacheList;
        }

        private void ScanForAvailableProviders(IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool shouldRefreshCache = false,
            bool logWarning = true) {

            ResetProviderCachetable();

            //search assemblies from the well-known locations and update the internal provider cache table 
            var providerAssemblies = ScanAllProvidersFromProviderAssembliesLocation(request, providerName, requiredVersion, minimumVersion, maximumVersion, ProviderOption.AllProvider).ToArray();

            //find out which one are from root directory. Because we cannot tell its version and provider name
            //we need to load it.

            var files = providerAssemblies.Where(each => ProviderAssembliesLocation.ContainsIgnoreCase(Path.GetDirectoryName(each))).ReEnumerable();

            //after the cache table gets cleaned, we need to load these assemblies sitting at the top level folder
            files.ParallelForEach(providerAssemblyName => {
                lock (_providerFiles) {
                    if (_providerFiles.ContainsKey(providerAssemblyName)) {
                        //remove the same file from the _providerFiles if any, so it gets reentered
                        //to the cache table.
                        _providerFiles.Remove(providerAssemblyName);
                    }
                }
                LoadProviderAssembly(request, providerAssemblyName, false);
            });

            var powerShellMetaProvider = GetMetaProviderObject(request);
            if (powerShellMetaProvider == null) {
                return;
            }

            //Get available powershell providers
            powerShellMetaProvider.RefreshProviders(request.As<IRequest>(), providerName, requiredVersion, minimumVersion, maximumVersion, logWarning);
        }

        /// <summary>
        /// Import a package provider.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="providerName">Provider name or file path</param>
        /// <param name="requiredVersion">The provider version to be loaded</param>
        /// <param name="minimumVersion">The minimum version of the provider to be loaded</param>
        /// <param name="maximumVersion">The maximum version of the provider to be loaded</param>
        /// <param name="isPathRooted">Whether the 'providerName' is path or name</param>
        /// <param name="force">Whether -force is specified</param>
        /// <returns></returns>
        public IEnumerable<PackageProvider> ImportPackageProvider(IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool isPathRooted,
            bool force) {
 
            return ImportPackageProviderHelper(request, providerName, requiredVersion, minimumVersion, maximumVersion, isPathRooted, force, true);
        }        

        /// <summary>
        /// Import a package provider.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="providerName">Provider name or file path</param>
        /// <param name="requiredVersion">The provider version to be loaded</param>
        /// <param name="minimumVersion">The minimum version of the provider to be loaded</param>
        /// <param name="maximumVersion">The maximum version of the provider to be loaded</param>
        /// <param name="isPathRooted">Whether the 'providerName' is path or name</param>
        /// <param name="force">Whether -force is specified</param>
        /// <param name="throwErrorWhenImportWithName">if true then we use write error when there is an
        /// error when importing with name</param>
        /// <returns></returns>
        private IEnumerable<PackageProvider> ImportPackageProviderHelper(IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool isPathRooted,
            bool force,
            bool throwErrorWhenImportWithName) {

            request.Debug(string.Format(CultureInfo.CurrentCulture, "Calling ImportPackageProvider. providerName = '{0}', requiredVersion='{1}', minimumVersion = '{2}', maximumVersion='{3}'",
                providerName, requiredVersion, minimumVersion, maximumVersion));

            if (string.IsNullOrWhiteSpace(providerName)) {
                return Enumerable.Empty<PackageProvider>();
            }
            if (providerName.ContainsWildcards()) {
                request.Error(Constants.Messages.InvalidParameter, ErrorCategory.InvalidData.ToString(), providerName, string.Format(CultureInfo.CurrentCulture, Resources.Messages.InvalidParameter, "Import-PackageProvider"));      
                return Enumerable.Empty<PackageProvider>();
            }
           
            if (isPathRooted) {

                if (!File.Exists(providerName)) {
                    request.Error(Constants.Messages.InvalidFilename, ErrorCategory.InvalidData.ToString(), providerName, string.Format(CultureInfo.CurrentCulture, Resources.Messages.FileNotFound, providerName));
                    return Enumerable.Empty<PackageProvider>();
                }

                //Check if the file type is supported: .dll, .exe, or .psm1
                if (!Constants.SupportedAssemblyTypes.Any(each => each.EqualsIgnoreCase(Path.GetExtension(providerName)))) {
                    var fileTypes = Constants.SupportedAssemblyTypes.Aggregate(string.Empty, (current, each) => current + " " + each);
                    request.Error(Constants.Messages.InvalidFilename, ErrorCategory.InvalidData.ToString(), providerName, string.Format(CultureInfo.CurrentCulture, Resources.Messages.InvalidFileType, providerName, fileTypes));
                    return Enumerable.Empty<PackageProvider>();
                }
            }

            var providers = isPathRooted ? ImportPackageProviderViaPath(request, providerName, requiredVersion, minimumVersion, maximumVersion, force)
                : ImportPackageProviderViaName(request, providerName, requiredVersion, minimumVersion, maximumVersion, force, throwErrorWhenImportWithName);
          
            return providers;
        }

        private IEnumerable<PackageProvider> ImportPackageProviderViaPath(IHostApi request,
            string providerPath,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool force) {
            request.Debug(string.Format(CultureInfo.CurrentCulture, "Calling ImportPackageProviderViaPath. providerName = '{0}', requiredVersion='{1}', minimumVersion = '{2}', maximumVersion='{3}'",
                providerPath, requiredVersion, minimumVersion, maximumVersion));

            var extension = Path.GetExtension(providerPath);

            if (extension != null && extension.EqualsIgnoreCase(".psm1")) {
                //loading the PowerShell provider
                request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.LoadingPowerShellModule, providerPath));
                return ImportPowerShellProvider(request, providerPath, requiredVersion, force);
            }

            //loading non-PowerShell providers
            request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.LoadingAssembly, providerPath));

            var loaded = LoadProviderAssembly(request, providerPath, force);

            if (loaded) {
                return _packageProviders.Where(p => p.Value.ProviderPath.EqualsIgnoreCase(providerPath)).Select(each => each.Value);
            }

            return Enumerable.Empty<PackageProvider>();
        }

        private IEnumerable<PackageProvider> ImportPackageProviderViaName(IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool force,
            bool throwErrorWhenImportWithName) {
            request.Debug(string.Format(CultureInfo.CurrentCulture, "Calling ImportPackageProviderViaName. providerName = '{0}', requiredVersion='{1}', minimumVersion = '{2}', maximumVersion='{3}'",
                providerName, requiredVersion, minimumVersion, maximumVersion));

            //Check if the module or assembly is already loaded
            //key = path, value = version
            HashSet<KeyValuePair<string, FourPartVersion>> refreshingProvidersPaths = new HashSet<KeyValuePair<string, FourPartVersion>>();
            foreach (var provider in _packageProviders) {
                if (provider.Key.IsWildcardMatch(providerName)) {
                    //found the provider with the same name is already loaded                  

                    if (force) {
                        // if -force is present and required version is specified, we will enforce that the loaded provider version must match the required version
                        if ((requiredVersion != null && provider.Value.Version == (FourPartVersion)requiredVersion)
                            // if -force is specified and no version information is provided, we will re-import directly from the path of the loaded provider
                            ||(requiredVersion == null && maximumVersion == null && minimumVersion == null))
                        {
                            refreshingProvidersPaths.Add(new KeyValuePair<string, FourPartVersion>(_packageProviders[provider.Key].ProviderPath, _packageProviders[provider.Key].Version));
                        }
                        request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ReImportProvider, provider.Key));
                    } else {
                        request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ProviderImportedAlready, provider.Key));
                        return Enumerable.Empty<PackageProvider>();
                    }
                }
            }

            //reload the assembly
            foreach (var providerPath in refreshingProvidersPaths) {
                var providers = ImportPackageProviderViaPath(request, providerPath.Key, providerPath.Value, minimumVersion, maximumVersion, force);
                return providers;

            }

            IEnumerable<PackageProvider> results = null;

            // if user doesn't have all the available providers in the cache,
            // then there is a chance that we will miss the latest version of the provider
            // so we will only try to search from the cache table without refreshing it
            // if the user does not provide -force and either maximum or minimum version.
            if (!force || (maximumVersion == null && minimumVersion == null))
            {
                //check if the provider is in the cache table
                results = FindMatchedProvidersFromInternalCacheTable(request, providerName, requiredVersion, minimumVersion, maximumVersion, force).ToArray();
                if (results.Any())
                {
                    return results;
                }
            }

            //If the provider is not in the cache list, rescan for providers                
            ScanForAvailableProviders(request, providerName, requiredVersion, minimumVersion, maximumVersion, true, false);
            results = FindMatchedProvidersFromInternalCacheTable(request, providerName, requiredVersion, minimumVersion, maximumVersion, force).ToArray();
            if (!results.Any()) {
                if (throwErrorWhenImportWithName)
                {
                    request.Error(Constants.Messages.NoMatchFoundForCriteria, ErrorCategory.InvalidData.ToString(),
                        providerName, string.Format(CultureInfo.CurrentCulture, Resources.Messages.NoMatchFoundForCriteria, providerName));
                }
                else
                {
                    request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.NoMatchFoundForCriteria, providerName));
                }
            } else {
                return results;
            }

            return Enumerable.Empty<PackageProvider>();
        }


        private IEnumerable<PackageProvider> FindMatchedProvidersFromInternalCacheTable(
            IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            bool force) {

            //Search from the internal table to see if we can the matched provider
            //check if the provider name matches
            var providersTable = _providerCacheTable.Where(each => each.Key.IsWildcardMatch(providerName))
                .Select(each => each.Value).ToArray();
            //check if version matches
            var providers = providersTable.Select(list => list.Where(each => {
                bool foundMatch = true;
                if (requiredVersion != null) {
                    return each.Version.Equals(requiredVersion);
                }

                if (minimumVersion != null) {
                    foundMatch = each.Version >= (FourPartVersion)minimumVersion;
                }
                if (maximumVersion != null) {
                    foundMatch &= each.Version <= (FourPartVersion)maximumVersion;
                }

                return foundMatch;
            }).Select(each => each)).ToArray();

            var selectedProviders = providers.Select(each => each.OrderByDescending(p => p.Version).FirstOrDefault()).WhereNotNull();
            
            foreach (var instance in selectedProviders) {
                if (instance.IsLoaded) {
                    //Initialize the provider
                    instance.Initialize(request);

                    //Add it to the provider list that all imported and in use
                    _packageProviders.AddOrSet(instance.ProviderName, instance);
                    request.Verbose(string.Format(Resources.Messages.ImportPackageProvider, instance.ProviderName));                   
                    yield return instance;
                } else {
                    if (Path.GetExtension(instance.ProviderPath).EqualsIgnoreCase(".psm1")) {

                        //it's a powershell provider
                        var psProviders = ImportPowerShellProvider(request, instance.ProviderPath, instance.Version, shouldRefreshCache: force);
                        foreach (var p in psProviders) {
                            yield return p;
                        }

                    } else {
                        LoadProviderAssembly(request, instance.ProviderPath, shouldRefreshCache: force);
                        var foo = _packageProviders.Where(each => each.Key.IsWildcardMatch(providerName));
                        foreach (var p in foo) {
                            yield return p.Value;
                        }
                    }
                }
            }
        }


        private IEnumerable<PackageProvider> ImportPowerShellProvider(IHostApi request, string modulePath, Version requiredVersion, bool shouldRefreshCache)
        {
            request.Debug(string.Format(CultureInfo.CurrentCulture, "Calling ImportPowerShellProvider. providerName = '{0}', requiredVersion='{1}'", 
                modulePath, requiredVersion));

            var powerShellMetaProvider = GetMetaProviderObject(request);
            if (powerShellMetaProvider == null) {
                yield break;
            }

            //providerName can be a file path or name.
            var instances = powerShellMetaProvider.LoadAvailableProvider(request.As<IRequest>(), modulePath, requiredVersion, shouldRefreshCache).ReEnumerable();

            if (!instances.Any()) {
                //A provider is not found
                request.Error(Constants.Messages.UnknownProvider, ErrorCategory.InvalidOperation.ToString(),
                    modulePath, string.Format(Resources.Messages.UnknownProvider, modulePath));
                yield break;

            }

            foreach (var instance in instances) {
                //Register the provider
                var provider = instance.As<PackageProvider>();
                if (provider != null) {
                    //initialize the actual powershell package provider
                    if (provider.Provider == null) {
                        continue;
                    }
                    provider.Provider.InitializeProvider(request.As<IRequest>());

                    AddToProviderCacheTable(provider.ProviderName, provider);

                    //initialize the wrapper package provider
                    provider.Initialize(request);

                    // addOrSet locks the collection anyway.
                    _packageProviders.AddOrSet(provider.ProviderName, provider);

                    yield return provider;
                }
            }
        }

        private IMetaProvider GetMetaProviderObject(IHostApi request)
        {
            //retrieve the powershell metaprovider object
            if (_metaProviders.ContainsKey("PowerShell")) {
                var powerShellMetaProvider = _metaProviders["PowerShell"];
                if (powerShellMetaProvider != null) {
                    return powerShellMetaProvider;
                }
            } 

            request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.FailedPowerShellMetaProvider));
            return null;
        }

        private bool CompareProvider(PackageProvider p1, PackageProvider p2) {
            if (p1 == null && p2 == null) {
                return true;
            }

            if (p1 == null || p2 == null) {
                return false;
            }

            if ((p1.Name != null) && (p1.Name.EqualsIgnoreCase(p2.Name)) && (p1.ProviderName != null && p1.ProviderName.EqualsIgnoreCase(p2.ProviderName) && p1.Version == p2.Version)) {
                return true;
            }
            return false;
        }

        internal void AddToProviderCacheTable(string name, PackageProvider provider) {
            lock (_providerCacheTable) {
                if (_providerCacheTable.ContainsKey(name)) {
                    var list = _providerCacheTable[name];

                    var index = list.FindIndex(each => CompareProvider(each, provider));
                    if (index != -1) {
                        //overwrite the cache only if the provider is loaded but the existing one not loaded
                        if (!list[index].IsLoaded && provider.IsLoaded) {
                            list[index] = provider;
                        }
                    } else {
                        _providerCacheTable[name].Add(provider);
                    }
                } else {
                    var entry = new List<PackageProvider> {
                        provider
                    };
                    _providerCacheTable.Add(name, entry);
                }
            }
        }

        private void ResetProviderCachetable() {

            foreach (var list in _providerCacheTable.Values.WhereNotNull()) {
                list.Clear();
            }
            _providerCacheTable.Clear();

            _packageProviders.ParallelForEach(each => AddToProviderCacheTable(each.Key, each.Value));
        }

        //Scan through the well-known providerAssemblies folder to find the providers that met the condition.
        internal IEnumerable<string> ScanAllProvidersFromProviderAssembliesLocation(
            IHostApi request,
            string providerName,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            ProviderOption providerOption = ProviderOption.LatestVersion) {

#if PORTABLE
            return Enumerable.Empty<string>();
#else
            //We don't need to scan provider assemblies on corepowershell.

            //if provider is installed in providername\version format
            var providerFolder = ProviderAssembliesLocation.Distinct(new PathEqualityComparer(PathCompareOption.Full)).SelectMany(Directory.EnumerateDirectories);

            foreach (var providerNameFolder in providerFolder) {

                var name = Path.GetFileName(providerNameFolder);

                //check the providername folder
                if (!string.IsNullOrWhiteSpace(providerName)) {

                    if (string.IsNullOrWhiteSpace(providerNameFolder)) {
                        continue;
                    }
                   
                    if (string.IsNullOrWhiteSpace(name) || !name.IsWildcardMatch(providerName)) {
                        continue;
                    }
                }

                var selectedVersions = Directory.EnumerateDirectories(providerNameFolder).Select(versionFolder => {
                    //check if the version is in a valid format. Ver will be 0 if TryParse fails and it won't be selected
                    Version ver;
                    if (System.Version.TryParse(Path.GetFileName(versionFolder), out ver)) {
                        return new {
                            folder = versionFolder,
                            version = (FourPartVersion)ver
                        };
                    }
                    return null;
                }).Where(each => each != null && each.version > 0L);

                selectedVersions = selectedVersions.Where(eachVersion => {
                    if ((requiredVersion == null) || eachVersion.version == (FourPartVersion)requiredVersion) {
                        if ((minimumVersion == null) || eachVersion.version >= (FourPartVersion)minimumVersion) {
                            if ((maximumVersion == null) || eachVersion.version <= (FourPartVersion)maximumVersion) {
                                return true;
                            }
                        }
                    }
                    return false;
                });

                //Get the version folders
                var versionFolders = (providerOption == ProviderOption.AllProvider) ?
                    selectedVersions.Select(each => each.folder).Where(Directory.Exists) :
                    new[] {selectedVersions.OrderByDescending(each => each.version).Select(each => each.folder).FirstOrDefault(Directory.Exists)};

                foreach (var assemblyFolder in versionFolders.WhereNotNull()) {
                    //we reached the provider assembly file path now

                    var files = Directory.EnumerateFiles(assemblyFolder)
                        .Where(file => (file != null) && (Path.GetExtension(file).EqualsIgnoreCase(".dll") || Path.GetExtension(file).EqualsIgnoreCase(".exe"))
                                        // we only check for dll that has manifest attached to it. (In case there are supporting assemblies in this folder)
                                        && Manifest.LoadFrom(file).Any(manifest => Swidtag.IsSwidtag(manifest) && new Swidtag(manifest).IsApplicable(new Hashtable())))
                        .ToArray();

                    //if found more than one dll with manifest is installed under a version folder, this is not allowed. warning here as enumerating for providers should continue
                    if (files.Any() && files.Count() > 1) {
                        request.Warning(string.Format(CultureInfo.CurrentCulture, Resources.Messages.SingleAssemblyAllowed, files.JoinWithComma()));
                        continue;
                    }

                    // find modules that have the provider manifests
                    var filelist = files.Where(each => Manifest.LoadFrom(each).Any(manifest => Swidtag.IsSwidtag(manifest) && new Swidtag(manifest).IsApplicable(new Hashtable())));

                    if (!filelist.Any()) {
                        continue;
                    }
                    var version = Path.GetFileName(assemblyFolder);
                    var defaultPkgProvider = new DefaultPackageProvider(name, version);
                    var providerPath = files.FirstOrDefault();

                    var pkgProvider = new PackageProvider(defaultPkgProvider)
                    {
                        ProviderPath = providerPath,
                        Version = version,
                        IsLoaded = false
                    };

                    pkgProvider.SetSwidTag(providerPath);

                    AddToProviderCacheTable(name, pkgProvider);
                    yield return providerPath;
                }
            }

            //check if assembly is installed at the top leverl folder.
            var providerFiles = ProviderAssembliesLocation.Distinct(new PathEqualityComparer(PathCompareOption.Full)).SelectMany(Directory.EnumerateFiles)
                .Where(each => each.FileExists() && (Path.GetExtension(each).EqualsIgnoreCase(".dll") || Path.GetExtension(each).EqualsIgnoreCase(".exe"))).ReEnumerable();

            // found the assemblies at the top level folder.
            // if a user is looking for a specific version & provider name, we are not be able to know the provider name or version without loading it.
            // Thus, for the top level providers, we just need to load them for the backward compatibility. 
            if (providerFiles.Any()) {
                request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ProviderNameAndVersionNotAvailableFromFilePath, providerFiles.JoinWithComma()));
                foreach (var provider in providerFiles) {
                    //the provider is installed at the top level.

                    // find modules that have the provider manifests
                    if (Manifest.LoadFrom(provider).Any(manifest => Swidtag.IsSwidtag(manifest) && new Swidtag(manifest).IsApplicable(new Hashtable()))) {

                        yield return provider;
                    }
                }
            }
#endif
        }

        //Return all providers under the providerAssemblies folder
        internal IEnumerable<string> AllProvidersFromProviderAssembliesLocation(IHostApi request) {
#if !PORTABLE
            // don't need this for core powershell
            try {

                return ScanAllProvidersFromProviderAssembliesLocation(request, null, null, null, null, ProviderOption.AllProvider).WhereNotNull().ToArray();
            } catch (Exception ex) {
                request.Debug(ex.Message);
            }
#endif
            return Enumerable.Empty<string>();
        }


        //return the providers with latest version under the providerAssemblies folder
        //This method only gets called during the initialization, i.e. LoadProviders().
        private IEnumerable<string> ProvidersWithLatestVersionFromProviderAssembliesLocation(IHostApi request) {
#if !PORTABLE
            // don't need this for core powershell
            try {
                var providerPaths = ScanAllProvidersFromProviderAssembliesLocation(request, null, null, null, null, ProviderOption.LatestVersion).WhereNotNull().ToArray();

                var notRootAssemblies = providerPaths.Where(each => !ProviderAssembliesLocation.ContainsIgnoreCase(Path.GetDirectoryName(each))).ToArray();
                var rootAssemblies = providerPaths.Where(each => ProviderAssembliesLocation.ContainsIgnoreCase(Path.GetDirectoryName(each))).ToArray();

                var equalityComparer = new PathEqualityComparer(PathCompareOption.File);
                //return the assemblies that are installed not directly under ProviderAssemblies root folder.
                //For the assemblies under the root directory, we need to check further if the provider that has the later version 
                //installed under providername\version folder
                //Convention: the providers are installed under providerassemblies\providername\version folder has the later version
                //than those at the top root folder.
                var assembliesUnderRootFolder = rootAssemblies.Where(rootPath => !notRootAssemblies.Any(element => equalityComparer.Equals(element, rootPath)));

                //for these assemblies not under the providerassemblies root folders but they have the same provider names, we return the latest version               
                var assembliesUnderVersionFolder = notRootAssemblies.GroupBy(Path.GetFileName).Select(
                    each => each.OrderByDescending(file => {
                        var versionFolder = Path.GetDirectoryName(file);
                        Version ver;
                        return !System.Version.TryParse(Path.GetFileName(versionFolder), out ver) ? new Version("0.0") : ver;
                    }).FirstOrDefault()).WhereNotNull();

                //filter out the old nuget-anycpu if exists
                assembliesUnderRootFolder = assembliesUnderRootFolder.Where(rootPath => !assembliesUnderVersionFolder.Any(element => new PathEqualityComparer(PathCompareOption.Nuget).Equals(element, rootPath)));

                return assembliesUnderVersionFolder.Concat(assembliesUnderRootFolder);

            }
            catch (Exception ex)
            {
                request.Debug(ex.Message);
            }
#endif

            return Enumerable.Empty<string>();
        }

        /// <summary>
        ///     This initializes the provider registry with the list of package providers.
        ///     (currently a hardcoded list, soon, registry driven)
        /// </summary>
        /// <param name="request"></param>
        internal void LoadProviders(IHostApi request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            //looks like registry needs to be here for supporting .msi packages
            var providerAssemblies = (_initialized ? Enumerable.Empty<string>() : _defaultProviders)
                .Concat(GetProvidersFromRegistry(Registry.LocalMachine, "SOFTWARE\\MICROSOFT\\PACKAGEMANAGEMENT"))
                .Concat(GetProvidersFromRegistry(Registry.CurrentUser, "SOFTWARE\\MICROSOFT\\PACKAGEMANAGEMENT"));

#if !COMMUNITY_BUILD
            // todo: these should just be strong-named references. for now, just load them from the same directory.
            providerAssemblies = providerAssemblies.Concat(new[] {
                Path.Combine(BaseDir, "Microsoft.PackageManagement.MetaProvider.PowerShell.dll"),
                Path.Combine(BaseDir, "Microsoft.PackageManagement.ArchiverProviders.dll"),
                Path.Combine(BaseDir, "Microsoft.PackageManagement.CoreProviders.dll"),
                Path.Combine(BaseDir, "Microsoft.PackageManagement.NuGetProvider.dll"),
                Path.Combine(BaseDir, "Microsoft.PackageManagement.PackageSourceListProvider.dll"),
#if !CORECLR
                Path.Combine(BaseDir, "Microsoft.PackageManagement.MsuProvider.dll"),
                Path.Combine(BaseDir, "Microsoft.PackageManagement.MsiProvider.dll")
#endif
            });
#endif

            providerAssemblies = providerAssemblies.OrderByDescending(each => {
                try {
                    // try to get a version from the file first
                    return (ulong)(FourPartVersion)FileVersionInfo.GetVersionInfo(each);
                } catch {
                    // otherwise we can't make a distinction.
                    return (ulong)0;
                }
            });

            providerAssemblies = providerAssemblies.Distinct(new PathEqualityComparer(PathCompareOption.FileWithoutExtension));

            // there is no trouble with loading providers concurrently.
#if DEEP_DEBUG
            providerAssemblies.SerialForEach(providerAssemblyName => {
#else
            providerAssemblies.ParallelForEach(providerAssemblyName => {
#endif
                LoadProviderAssembly(request, providerAssemblyName, false);
            });
#if DEEP_DEBUG
            WaitForDebugger();
#endif
        }

#if DEEP_DEBUG
        internal void WaitForDebugger() {
            if (!System.Diagnostics.Debugger.IsAttached) {
                Console.Beep(500, 2000);
                while (!System.Diagnostics.Debugger.IsAttached) {
                    System.Threading.Thread.Sleep(1000);
                    Console.Beep(500, 200);
                }
            }
        }
#endif
        /// <summary>
        /// Dynamic providers are the ones that are not installed with the core itself.
        /// </summary>
        internal IEnumerable<PackageProvider> DynamicProviders {
            get {
                return _packageProviders.Values.Where(each => !each.ProviderPath.StartsWith(BaseDir, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static IEnumerator<string> GetProvidersFromRegistry(RegistryKey registryKey, string p) {
            RegistryKey key;
            try {
#if !CORECLR
                key = registryKey.OpenSubKey(p, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
#else
                key = registryKey.OpenSubKey(p);
#endif
            } catch {
                yield break;
            }

            if (key == null) {
                yield break;
            }

            foreach (var name in key.GetValueNames()) {
                yield return key.GetValue(name).ToString();
            }
        }

        public IEnumerable<PackageSource> GetAllSourceNames(IHostApi request) {
            return _packageProviders.Values.SelectMany(each => each.ResolvePackageSources(request));
        }


        /// <summary>
        ///     Searches for the assembly, interrogates it for it's providers and then proceeds to load
        /// </summary>
        /// <param name="request"></param>
        /// <param name="providerAssemblyName"></param>
        /// <param name="shouldRefreshCache"></param>
        /// <returns></returns>
        internal bool LoadProviderAssembly(IHostApi request, string providerAssemblyName, bool shouldRefreshCache) {
            request.Debug(request.FormatMessageString("Trying provider assembly: {0}", providerAssemblyName));
            var assemblyPath = FindAssembly(providerAssemblyName);

            if (assemblyPath != null) {

                try {
                    byte[] hash = null;
                    using (var stream = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        hash = System.Security.Cryptography.MD5.Create().ComputeHash(stream);
                    }
                    lock (_providerFiles) {
                        if (_providerFiles.ContainsKey(assemblyPath)) {
                            // have we tried this file before?
                             //if the exactly same provider is loaded already, skip the processed assembly. 

                            if (_providerFiles[assemblyPath].SequenceEqual(hash) && !shouldRefreshCache) {
                                // and it's the exact same file?
                                request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.SkippingProcessedAssembly, assemblyPath));
                                return false;
                            }
                            // it's a different file in the same path? -force exists?
                            // we're gonna let it try the new file. 
                            _providerFiles.Remove(assemblyPath);                            
                        }

                        request.Debug(request.FormatMessageString("Attempting loading of assembly: {0}", assemblyPath)); 
                        // record that this file is being loaded.
                        _providerFiles.Add(assemblyPath, hash);
                    }
                    if (AcquireProviders(assemblyPath, request, shouldRefreshCache)) {
                        request.Debug(request.FormatMessageString("SUCCESS provider assembly: {0}", providerAssemblyName));
                        return true;
                    }
                } catch (Exception e) {
                    e.Dump();

                    lock (_providerFiles) {
                        // can't create hash from file? 
                        // we're not going to try and load this.
                        // all we can do is record the name.
                        if (!_providerFiles.ContainsKey(assemblyPath)) {
                            _providerFiles.Add(assemblyPath, new byte[0]);
                        }
                    }
                }
            }
            request.Debug(request.FormatMessageString("FAILED provider assembly: {0}", providerAssemblyName));
            return false;
        }

        /// <summary>
        ///     PROTOTYPE -- extremely simplified assembly locator.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private string FindAssembly(string assemblyName) {
            try {
                string fullPath;
                // is the name given a strong name?
                if (assemblyName.Contains(',')) {
                    // looks like a strong name
                    // todo: not there yet...
                    return null;
                }

                // is it a path?
                if (assemblyName.Contains('\\') || assemblyName.Contains('/') || assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
                    fullPath = Path.GetFullPath(assemblyName);
                    if (File.Exists(fullPath)) {
                        return fullPath;
                    }
                    if (File.Exists(fullPath + ".dll")) {
                        return fullPath;
                    }

                    // lets see if the assembly name is in the same directory as the current assembly...
                    try {
                        fullPath = Path.Combine(Path.GetDirectoryName(CurrentAssemblyLocation), assemblyName);
                        if (File.Exists(fullPath)) {
                            return fullPath;
                        }
                        if (File.Exists(fullPath + ".dll")) {
                            return fullPath;
                        }
                    } catch {
                    }
                }
                // must be just just a plain name.

                // todo: search the GAC too?

                // search the local folder.
                fullPath = Path.GetFullPath(assemblyName + ".dll");
                if (File.Exists(fullPath)) {
                    return fullPath;
                }

                // try next to where we are.
                fullPath = Path.Combine(Path.GetDirectoryName(CurrentAssemblyLocation), assemblyName + ".dll");
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            } catch (Exception e) {
                e.Dump();
            }
            return null;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Justification = "This is a plugin loader. It *needs* to do that.")]
        internal bool AcquireProviders(string assemblyPath, IHostApi request, bool shouldRefreshCache) {
            var found = false;
            try {

                Assembly assembly = null;

#if !CORECLR
                assembly = Assembly.LoadFrom(assemblyPath);
#else
                assembly = Microsoft.PowerShell.CoreCLR.AssemblyExtensions.LoadFrom(assemblyPath);
#endif
                if (assembly == null) {
                    return false;
                }

                var asmVersion = GetAssemblyVersion(assembly);
                request.Debug("Acquiring providers for assembly" + assemblyPath);

                assembly.FindCompatibleTypes<IMetaProvider>().AsyncForEach(metaProviderClass => {
                    request.Debug("Registering providers via metaproviders for assembly " + metaProviderClass);
                    found |= RegisterProvidersViaMetaProvider(metaProviderClass.Create<IMetaProvider>(), asmVersion, request);
                })
                    .Concat(assembly.FindCompatibleTypes<IPackageProvider>().AsyncForEach(packageProviderClass => {

                        try {
                            //Handling C# based providers
                            var packageProvider = RegisterPackageProvider(packageProviderClass.Create<IPackageProvider>(), asmVersion, request, shouldRefreshCache);
                            if (packageProvider != null) {
                                found = true;
                                packageProvider.ProviderPath = assemblyPath;
                                packageProvider.SetSwidTag(assemblyPath);
                            }

                        } catch (Exception ex) {
                            request.Debug(ex.Message);
                        }
                    }))

                    .Concat(assembly.FindCompatibleTypes<IArchiver>().AsyncForEach(serviceProviderClass => {
                        var archiver = RegisterArchiver(serviceProviderClass.Create<IArchiver>(), asmVersion, request);
                        if (archiver != null) {
                            found = true;
                            archiver.ProviderPath = assemblyPath;
                            archiver.SetSwidTag(assemblyPath);
                        }

                    }))
                    .Concat(assembly.FindCompatibleTypes<IDownloader>().AsyncForEach(serviceProviderClass => {
                        var downloader = RegisterDownloader(serviceProviderClass.Create<IDownloader>(), asmVersion, request);
                        if (downloader != null) {
                            found = true;
                            downloader.ProviderPath = assemblyPath;
                            downloader.SetSwidTag(assemblyPath);
                        }

                    })).WaitAll();

            } catch (Exception e) {
                request.Debug(e.Message);
                request.Debug(e.StackTrace);
            }
            return found;
        }

        private static FourPartVersion GetAssemblyVersion(Assembly asm) {
            FourPartVersion result = 0;

            result = asm.GetName().Version;

            if (result == 0) {
                // what? No assembly version?
                // fallback to the file version of the assembly
#if !CORECLR
                var assemblyLocation = asm.Location;
#else
                var assemblyLocation = asm.ManifestModule.FullyQualifiedName;
#endif
                if (!string.IsNullOrWhiteSpace(assemblyLocation) && File.Exists(assemblyLocation)) {
                    result = FileVersionInfo.GetVersionInfo(assemblyLocation);
                    if (result == 0) {
                        // no file version either?
                        // use the date I guess.
                        try {
                            result = new FileInfo(assemblyLocation).LastWriteTime;
                        } catch {
                        }
                    }
                }

                if (result == 0) {
                    // still no version?
                    // I give up. call it 0.0.0.1
                    result = "0.0.0.1";
                }
            }
            return result;
        }

        /// <summary>
        /// Register the package provider
        /// </summary>
        /// <param name="provider">Package provider object</param>
        /// <param name="asmVersion">assembly version info</param>
        /// <param name="request"></param>
        /// <param name="shouldRefreshCache">should refresh the internal provider list</param>
        /// <returns></returns>
        private PackageProvider RegisterPackageProvider(IPackageProvider provider,
            FourPartVersion asmVersion,
            IHostApi request,
            bool shouldRefreshCache) {

            string name = null;
            try {
                if (provider == null) {
                    return null;
                }
                FourPartVersion ver = provider.GetProviderVersion();
                var version = ver == 0 ? asmVersion : ver;
                name = provider.GetPackageProviderName();
                if (string.IsNullOrWhiteSpace(name)) {
                    return null;
                }

                // Initialize the provider before locking the collection
                // that way we're not blocking others on non-deterministic actions.
                request.Debug("Initializing provider '{0}'".format(name));
                provider.InitializeProvider(request.As<IRequest>());
                request.Debug("Provider '{0}' Initialized".format(name));

                lock (_packageProviders) {
                    //Check if the provider is loaded already.
                    if (_packageProviders.ContainsKey(name)) {
                        //if no -force, do nothing
                        if (!shouldRefreshCache) {
                            request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.SkipPreviousProcessedProvider, name));

                            //add the provider to the list
                            var pkgprovider = new PackageProvider(provider) {
                                Version = version,
                                IsLoaded = true
                            };
                            AddToProviderCacheTable(name, pkgprovider);
                            return pkgprovider;
                        } else {
                            //looks like -force is used, we need to remove the old provider first.
                            // this won't remove the plugin domain and unload the code yet
                            _packageProviders.Remove(name);
                        }
                    }
                }

                request.Debug("Using Package Provider {0}".format(name));
                var packageProvider = new PackageProvider(provider) {
                    Version = version
                };

                //initialize the package provider
                packageProvider.Initialize(request);

                // addOrSet locks the collection anyway.
                _packageProviders.AddOrSet(name, packageProvider);
                packageProvider.IsLoaded = true;
                request.Debug("The provider '{0}' is imported".format(name));

                //add the provider to the list
                AddToProviderCacheTable(name, packageProvider);
                return packageProvider;
            } catch (Exception e) {
                request.Debug("Provider '{0}' Failed to import".format(name));
                e.Dump();
            }
            return null;
        }

        private Archiver RegisterArchiver(IArchiver provider, FourPartVersion asmVersion, IHostApi request) {
            string name = null;
            try {
                FourPartVersion ver = provider.GetProviderVersion();
                var version = ver == 0 ? asmVersion : ver;
                name = provider.GetArchiverName();
                if (string.IsNullOrWhiteSpace(name)) {
                    return null;
                }

                // Initialize the provider before locking the collection
                // that way we're not blocking others on non-deterministic actions.
                request.Debug("Initializing provider '{0}'".format(name));
                provider.InitializeProvider(request.As<IRequest>());
                request.Debug("Provider '{0}' Initialized".format(name));

                lock (Archivers) {
                    if (Archivers.ContainsKey(name)) {
                        if (version > Archivers[name].Version) {
                            // remove the old provider first.
                            // todo: this won't remove the plugin domain and unload the code yet
                            // we'll have to do that later.

                            Archivers.Remove(name);
                        }
                        else {
                            return null;
                        }
                    }
                    request.Debug("Using Archiver Provider {0}".format(name));
                    var archiver = new Archiver(provider) {
                        Version = version,
                        IsLoaded = true
                    };

                    archiver.Initialize(request);
                    Archivers.AddOrSet(name, archiver);
                    return archiver;
                }
            }
            catch (Exception e) {
                request.Debug("Provider '{0}' Failed".format(name));
                e.Dump();
            }
            return null;
        }

        private Downloader RegisterDownloader(IDownloader provider, FourPartVersion asmVersion, IHostApi request) {
            string name = null;
            try {
                FourPartVersion ver = provider.GetProviderVersion();
                var version = ver == 0 ? asmVersion : ver;
                name = provider.GetDownloaderName();
                if (string.IsNullOrWhiteSpace(name)) {
                    return null;
                }

                // Initialize the provider before locking the collection
                // that way we're not blocking others on non-deterministic actions.
                request.Debug("Initializing provider '{0}'".format(name));
                provider.InitializeProvider(request.As<IRequest>());
                request.Debug("Provider '{0}' Initialized".format(name));

                lock (Downloaders) {
                    if (Downloaders.ContainsKey(name)) {
                        if (version > Downloaders[name].Version) {
                            // remove the old provider first.
                            // todo: this won't remove the plugin domain and unload the code yet
                            // we'll have to do that later.

                            Downloaders.Remove(name);
                        } else {
                            return null;
                        }
                    }
                    request.Debug("Using Downloader Provider {0}".format(name));

                    var downloader = new Downloader(provider) {
                        Version = version,
                        IsLoaded = true
                    };

                    downloader.Initialize(request);
                    Downloaders.AddOrSet(name, downloader);
                    return downloader;
                }
            } catch (Exception e) {
                request.Debug("Provider '{0}' Failed".format(name));
                e.Dump();
            }
            return null;
        }

        internal bool TryLoadProviderViaMetaProvider(string metaproviderName, string providerNameOrPath, IHostApi request ) {
            if (_metaProviders.ContainsKey(metaproviderName)) {
                var metaProvider = _metaProviders[metaproviderName];

                request.Debug("Using MetaProvider '{0}' to attempt to load provider from '{1}'".format(metaproviderName, providerNameOrPath));

                return LoadViaMetaProvider( _metaProviders[metaproviderName], providerNameOrPath, metaProvider.GetProviderVersion(),request);
            }
            request.Debug("MetaProvider '{0}' is not recognized".format(metaproviderName));
            return false;
        }

        internal bool RegisterProvidersViaMetaProvider(IMetaProvider provider, FourPartVersion asmVersion, IHostApi request) {
            request.Debug("Trying to register metaprovider");
            var found = false;
            var metaProviderName = provider.GetMetaProviderName();

            lock (_metaProviders) {
                if (!_metaProviders.ContainsKey(metaProviderName)) {
                    // Meta Providers can't be replaced at this point
                    _metaProviders.AddOrSet(metaProviderName, provider);
                }
            }

            try {
                provider.InitializeProvider(request.As<IRequest>());
                provider.GetProviderNames().ParallelForEach(name => {
                    found = LoadViaMetaProvider(provider, name, asmVersion, request);
                });
            } catch (Exception e) {
                e.Dump();
            }
            return found;
        }

        private bool LoadViaMetaProvider(IMetaProvider metaProvider, string name, FourPartVersion asmVersion, IHostApi request) {
            var found = false;

            var instance = metaProvider.CreateProvider(name);
            if (instance != null) {
                // check if it's a Package Provider
                if (typeof (IPackageProvider).CanDynamicCastFrom(instance)) {
                    try {
                        var packageProvider = RegisterPackageProvider(instance.As<IPackageProvider>(), asmVersion, request, false);
                        if (packageProvider != null) {
                            found = true;
                            packageProvider.IsLoaded = true;
                            packageProvider.ProviderPath = metaProvider.GetProviderPath(name);
                            packageProvider.SetSwidTag(packageProvider.ProviderPath);
                        }

                    } catch (Exception e) {
                        e.Dump();
                    }
                }

                // check if it's a Services Provider
                if (typeof (IArchiver).CanDynamicCastFrom(instance)) {
                    try {
                        var archiver = RegisterArchiver(instance.As<IArchiver>(), asmVersion, request);
                        if (archiver != null) {
                            found = true;
                            archiver.IsLoaded = true;
                            archiver.ProviderPath = metaProvider.GetProviderPath(name);
                            archiver.SetSwidTag(archiver.ProviderPath);
                        }

                    } catch (Exception e) {
                        e.Dump();
                    }
                }

                if (typeof (IDownloader).CanDynamicCastFrom(instance)) {
                    try {
                        var downloader = RegisterDownloader(instance.As<IDownloader>(), asmVersion, request);
                        if (downloader != null) {
                            found = true;
                            downloader.ProviderPath = metaProvider.GetProviderPath(name);
                            downloader.SetSwidTag(downloader.ProviderPath);
                            downloader.IsLoaded = true;
                        }

                    } catch (Exception e) {
                        e.Dump();
                    }
                }
            }
            return found;
        }
    }
}
