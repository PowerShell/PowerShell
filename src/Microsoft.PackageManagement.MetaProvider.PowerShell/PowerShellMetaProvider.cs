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

namespace Microsoft.PackageManagement.MetaProvider.PowerShell.Internal {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Versions;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using System.Globalization;
    using Implementation;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Providers;
    using PackageManagement.Internal.Utility.Plugin;
    using ErrorCategory = System.Management.Automation.ErrorCategory;
    using ProviderOption = PackageManagement.Internal.Implementation.PackageManagementService.ProviderOption;

    internal class ProviderItem {
        internal PSModuleInfo ModuleInfo;
        internal string ProviderPath;
    }

    /// <summary>
    ///     A  MetaProvider class that loads Providers implemented as a PowerShell Module.
    ///     It connects the functions in the PowerShell module to the expected functions that the
    ///     interface expects.
    /// </summary>
    public class PowerShellMetaProvider : IDisposable {
        private static readonly HashSet<string> _exclusionList = new HashSet<string> {
            "AppBackgroundTask",
            "AppLocker",
            "Appx",
            "AssignedAccess",
            "BitLocker",
            "BitsTransfer",
            "BranchCache",
            "CimCmdlets",
            "Defender",
            "DirectAccessClientComponents",
            "Dism",
            "DnsClient",
            "Hyper-V",
            "International",
            "iSCSI",
            "ISE",
            "Kds",
            "Microsoft.PowerShell.Diagnostics",
            "Microsoft.PowerShell.Host",
            "Microsoft.PowerShell.Management",
            "Microsoft.PowerShell.Security",
            "Microsoft.PowerShell.Utility",
            "Microsoft.WSMan.Management",
            "MMAgent",
            "MsDtc",
            "NetAdapter",
            "NetConnection",
            "NetEventPacketCapture",
            "NetLbfo",
            "NetNat",
            "NetQos",
            "NetSecurity",
            "NetSwitchTeam",
            "NetTCPIP",
            "NetWNV",
            "NetworkConnectivityStatus",
            "NetworkTransition",
            "PcsvDevice",
            "PKI",
            "PrintManagement",
            "PSDiagnostics",
            "PSScheduledJob",
            "PSWorkflow",
            "PSWorkflowUtility",
            "ScheduledTasks",
            "SecureBoot",
            "SmbShare",
            "SmbWitness",
            "StartScreen",
            "Storage",
            "TLS",
            "TroubleshootingPack",
            "TrustedPlatformModule",
            "VpnClient",
            "Wdac",
            "WindowsDeveloperLicense",
            "WindowsErrorReporting",
            "WindowsSearch",
            "PackageManagement", // dont' search ourselves.
            "OneGet", // dont' search ourselves.
            "OneGet-Edge" // dont' search ourselves.
        };

        private static string _baseFolder;
        private static string _powershellProviderFunctionsPath;
    
        //The reason of using 'object' instead of' PowerShellPackageProvider' is that PowerShellPackageProvider is a provider
        //that is not visible to the PackageManagement.
        private readonly IDictionary<string, object> _availableProviders = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, List<ProviderItem>> _psProviderCacheTable = new Dictionary<string, List<ProviderItem>>(StringComparer.OrdinalIgnoreCase);
        internal const string PowerShellGet = "PowerShellGet";
        private static readonly PowerShell _powershell = PowerShell.Create();
        private static bool _initialized = false;

        public PowerShellMetaProvider() {
           // _packageProviders.BlockingEnumerator = true;
        }

        internal static string BaseFolder {
            get {
                if (_baseFolder == null) {
#if CORECLR
                    _baseFolder = Path.GetDirectoryName(Path.GetFullPath(typeof(PowerShellMetaProvider).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName));
#else
                    _baseFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
#endif
                    if (_baseFolder == null || !Directory.Exists(_baseFolder)) {
                        throw new Exception(Resources.Messages.CantFindBasePowerShellModuleFolder);
                    }

                    _baseFolder = Path.Combine(_baseFolder, "Modules", "PackageManagement");
                }
                return _baseFolder;
            }
        }

        static PowerShellMetaProvider() {
            try {
                EnsurePowerShellInitialized();
            } catch {
                //we capture the exceptions here so that we won't fail to load the metaprovider assembly
                //exceptions can happen when execution-policy is set to restrict, for example.
            }
        }

        static void EnsurePowerShellInitialized() {
            if (!_initialized) {
                _powershell.ImportModule(PowerShellProviderFunctions);
                _initialized = true;
            }
        }

        internal static string PowerShellProviderFunctions {
            get {
                if (_powershellProviderFunctionsPath == null) {
                    // try the etc directory
                    _powershellProviderFunctionsPath = Path.Combine(BaseFolder, "etc", "PackageProviderFunctions.psm1");
                    if (!File.Exists(_powershellProviderFunctionsPath)) {
                        // fall back to the same directory.
                        _powershellProviderFunctionsPath = Path.Combine(BaseFolder, "PackageProviderFunctions.psm1");
                        if (!File.Exists(_powershellProviderFunctionsPath)) {
                            // oh-oh, no powershell functions file.
                            throw new Exception(String.Format(CultureInfo.CurrentCulture, Resources.Messages.UnableToFindPowerShellFunctionsFile, _powershellProviderFunctionsPath));
                        }
                    }
                }
                return _powershellProviderFunctionsPath;
            }
        }

        public IEnumerable<string> ProviderNames {
            get {
                //return _packageProviders.Select(each => each.GetPackageProviderName());;
                return _availableProviders.Keys;
            }
        }

        private void AddToTable(string name, ProviderItem provider) {
            //try to find if the provider is in the table already

            if (_psProviderCacheTable.ContainsKey(name)) {
                var list = _psProviderCacheTable[name];

                var index = list.FindIndex(each => (each.ModuleInfo.Version == provider.ModuleInfo.Version) && (each.ProviderPath.EqualsIgnoreCase(provider.ProviderPath)));
 
                if (index != -1) {
                    list[index] = provider;

                } else {
                    _psProviderCacheTable[name].Add(provider);
                }
            } else {
                var entry = new List<ProviderItem> {
                    provider
                };
                _psProviderCacheTable.Add(name, entry);
            }
        }

        /// <summary>
        ///     The name of this MetaProvider class
        /// </summary>
        public string MetaProviderName {
            get {
                return "PowerShell";
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string GetProviderPath(string providername) {
            return _availableProviders.Keys.Where(each => each.EqualsIgnoreCase(providername))
                .Select(each => ((PowerShellPackageProvider)_availableProviders[each]).ModulePath).FirstOrDefault();
        }

        private IEnumerable<KeyValuePair<string,PSModuleInfo>> ScanPrivateDataForProviders(PsRequest request, string baseFolder, Hashtable privateData, PSModuleInfo moduleInfo) {
            var providers = privateData.GetStringCollection("PackageManagementProviders").ReEnumerable();
            if (providers.Any()) {
                // found a module that is advertizing one or more  Providers.

                foreach (var provider in providers) {
                    var fullPath = provider;
                    try {
                        if (!Path.IsPathRooted(provider)) {
                            fullPath = Path.GetFullPath(Path.Combine(baseFolder, provider));
                        }
                    } catch {
                        // got an error from the path.
                        continue;
                    }
                    if (Directory.Exists(fullPath) || File.Exists(fullPath)) {
                        // looks like we have something that could definitely be a
                        // a module path.
                        var result = new KeyValuePair<string, PSModuleInfo>(fullPath, moduleInfo);
                        AddToPowerShellProviderCacheTable(result);
                        yield return result;
                    } else {
                        request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.FileNotFound, fullPath));
                    }
                }
            } else {
                request.Debug(string.Format(Resources.Messages.PackageManagementProvidersNotFound, baseFolder));
            }
        }

        private IEnumerable<KeyValuePair<string, PSModuleInfo>> GetPackageManagementModules(PsRequest request, PSModuleInfo module, Version requiredVersion, Version minimumVersion, Version maximumVersion) {
            // skip modules that we know don't contain any PM modules
            if (!_exclusionList.Contains(module.Name)) {
                var privateData = module.PrivateData as Hashtable;
                if (privateData != null) {
                    if (requiredVersion != null) {
                        if ((FourPartVersion)module.Version == (FourPartVersion)requiredVersion) {
                            return ScanPrivateDataForProviders(request, Path.GetDirectoryName(module.Path), privateData, module).ToArray();
                        } else {
                            return Enumerable.Empty<KeyValuePair<string, PSModuleInfo>>();
                        }
                    }

                    if ((minimumVersion != null) && ((FourPartVersion)module.Version < (FourPartVersion)minimumVersion)) {
                        return Enumerable.Empty<KeyValuePair<string, PSModuleInfo>>();
                    }

                    if ((maximumVersion != null) && ((FourPartVersion)module.Version > (FourPartVersion)maximumVersion)) {
                        return Enumerable.Empty<KeyValuePair<string, PSModuleInfo>>();
                    }

                    return ScanPrivateDataForProviders(request, Path.GetDirectoryName(module.Path), privateData, module).ToArray();
                }
            }
            return Enumerable.Empty<KeyValuePair<string, PSModuleInfo>>();
        }

        private void AddToPowerShellProviderCacheTable(KeyValuePair<string, PSModuleInfo> moduleInfo) {

            //some times when PrivateData in a provider's .psd1 meta file contains multiple providers (not recommended way), 
            //they all reside under the same module path. So we extract each file name and add to the table as dictionary key
            //to indicate they are actually different providers. 
            if (moduleInfo.Key != null) {
                var name = Path.GetFileNameWithoutExtension(moduleInfo.Key);
                if (string.IsNullOrWhiteSpace(name)) {
                    return;
                }

                // for the case where provider is a dll, we enforce that provider name must be the same as module name
                // so the name we add to the hash table will be the name of the module.
                // (for the case where provider is not a dll, we still allow multiple provider names for a module)
                if (string.Equals(Path.GetExtension(moduleInfo.Key), ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    name = moduleInfo.Value.Name;
                }

                AddToTable(name,
                    new ProviderItem {
                        ModuleInfo = moduleInfo.Value,
                        ProviderPath = moduleInfo.Key,
                    });
            }
        }

        private static PackageManagementService PackageManagementService {
            get {
                return PackageManager.Instance as PackageManagementService;
            }
        }

        //key = path, value = PSModuleInfo
        private IEnumerable<KeyValuePair<string, PSModuleInfo>> ScanForModules(
            PsRequest request,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            ProviderOption providerOption = ProviderOption.LatestVersion) {

            // two places we search for modules
            // 1. in this assembly's folder, look for all psd1 and psm1 files.
            //
            // 2. modules in the PSMODULEPATH
            //
            // Import each one of those, and check to see if they have a PackageManagementProviders section in their private data

            var allAvailableModules = _powershell
                .Clear()
                .AddCommand("Get-Module")
                .AddParameter("ListAvailable")
                .Invoke<PSModuleInfo>();

            return allAvailableModules.WhereNotNull()
                .SelectMany(each => GetPackageManagementModules(request, each, requiredVersion, minimumVersion, maximumVersion));
        }

        private IEnumerable<KeyValuePair<string, PSModuleInfo>> ScanForPowerShellGetModule(PsRequest request)
        {
            var psget = _powershell.GetModule("PowerShellGet").FirstOrDefault();
            return psget != null ? GetPackageManagementModules(request, psget, null, null, null) : Enumerable.Empty<KeyValuePair<string, PSModuleInfo>>();
        }

        private IEnumerable<string> AlternativeModuleScan(
            PsRequest request,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion,
            ProviderOption providerOption = ProviderOption.LatestVersion) {

            var psModulePath = Environment.GetEnvironmentVariable("PSModulePath") ?? "";

            IEnumerable<string> paths = psModulePath.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            // add assumed paths just in case the environment variable isn't really set.
            try {
#if CORECLR
                paths = paths.ConcatSingleItem(Path.Combine(Environment.GetEnvironmentVariable("windir"), "system32", @"WindowsPowerShell\v1.0\Modules"));
#else
                paths = paths.ConcatSingleItem(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\Modules"));
#endif
            } catch {
                // skip the folder if it's not valid
            }
            try {
#if CORECLR
                paths = paths.ConcatSingleItem(Path.Combine(Environment.GetEnvironmentVariable("userprofile"), "documents", @"WindowsPowerShell\Modules"));
#else
                paths = paths.ConcatSingleItem(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\Modules"));
#endif
            } catch {
                // skip the folder if it's not valid
            }

            if (!string.IsNullOrWhiteSpace(BaseFolder) && BaseFolder.DirectoryExists()) {
                paths = paths.ConcatSingleItem(BaseFolder);
            }

            var moduleFolders = paths.Distinct(new PathEqualityComparer(PathCompareOption.Full)).Where(each => each.DirectoryExists()).SelectMany(each => Directory.EnumerateDirectories(each).Where(dir => !_exclusionList.Contains(Path.GetFileName(dir))));

            foreach (var module in moduleFolders) {

                var moduleManifest = Path.Combine(module, Path.GetFileName(module) + ".psd1");
                if (File.Exists(moduleManifest)) {
                    //The version check is defered in the GetPackageManagementModules() because we do not know the version without looking into the content.
                    yield return moduleManifest;
                }

                //The following are the cases where there are multiple modules installed on the system. The file folder is the module version.
                var selectedVersions = Directory.EnumerateDirectories(module).Select(dir => new {
                    folder = dir,
                    ver = (FourPartVersion)Path.GetFileName(dir)
                }).Where(each => each.ver > 0L);

                if (requiredVersion != null) {
                    var version = selectedVersions.Where(each => each.ver == (FourPartVersion)requiredVersion).Select(each => each.folder).FirstOrDefault();
                    if (version != null) {
                        var file = Path.Combine(version, Path.GetFileName(Path.GetFileName(module)) + ".psd1");
                        if (File.Exists(file)) {
                            yield return file;
                        }
                    }
                }

                if (minimumVersion != null) {
                    selectedVersions = selectedVersions.Where(each => each.ver >= (FourPartVersion)minimumVersion);

                }
                if (maximumVersion != null) {
                    selectedVersions = selectedVersions.Where(each => each.ver <= (FourPartVersion)maximumVersion);
                }

                var results = (providerOption == PackageManagementService.ProviderOption.AllProvider) ?
                    selectedVersions.Select(version => Path.Combine(version.folder, Path.GetFileName(Path.GetFileName(module)) + ".psd1")).Where(File.Exists) :
                    new[] {
                        selectedVersions.OrderByDescending(each => each.ver)
                            .Select(version => Path.Combine(version.folder, Path.GetFileName(Path.GetFileName(module)) + ".psd1"))
                            .FirstOrDefault(File.Exists)
                    };

                foreach (var result in results.WhereNotNull()) {
                    yield return result;

                }
            }
        }

        public object CreateProvider(string name) {

            if (_availableProviders.ContainsKey(name)) {
                var provider = _availableProviders[name];

                if (provider != null) {
                    return provider;
                }
            }

            // it's possible that this is a path passed in. Let's see if it's a provider.
            if (!string.IsNullOrEmpty(name) && name.FileExists()) {
                // MUST DO: load provider from filepath.
            }
            // create the instance
            throw new Exception("No provider by name '{0}' registered.".format(name));
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _availableProviders.Clear();
                _psProviderCacheTable.Clear();
            }
        }

        private PowerShellPackageProvider Create(PsRequest request, string modulePath, string requiredVersion, bool force, bool logWarning) {
            var ps = PowerShell.Create();
            try {
                // load the powershell provider functions into this runspace.
                if (ps.ImportModule(PowerShellProviderFunctions, false) != null) {
                    var result = ps.ImportModule(modulePath, force);
                    if (result != null) {
                        try {
                            return new PowerShellPackageProvider(ps, result, requiredVersion);
                        } catch (Exception e) {
                            e.Dump();
                        }
                    }
                }
            } catch (Exception e) {
                // something didn't go well.
                // skip it.
                e.Dump();
                if (logWarning) {
                    request.Warning(e.Message);
                }
            }

            // didn't import correctly.
            ps.Dispose();
            return null;
        }

        public void InitializeProvider(PsRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Initializing PowerShell MetaProvider");

            //During the initialization, we load the PowerShellGet only to speeding up a little bit 
            var psModules = ScanForPowerShellGetModule(request).WhereNotNull();

            foreach (var psModule in psModules) {
                //Check if the PowerShellGet provider exists
                if ((psModule.Key != null) && (psModule.Value != null)) {
                    //load the PowerShellGet
                    AnalyzeModule(request, psModule.Key, psModule.Value.Version ?? new Version(0, 0), false, true, psModule.Value);
                }
            }

            if (_availableProviders.ContainsKey(PowerShellGet))
            {
                request.Debug("Loaded PowerShell Provider: PowerShellGet");
            }
            else
            {
                //if we can not load PowerShellGet, we do not fail the initialization
                request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ModuleNotFound, PowerShellGet));
            }
        }


        private void AddToPackageProviderCacheTable(PackageProvider provider)
        {
            PackageManagementService.AddToProviderCacheTable(provider.ProviderName, provider);
        }


        public void RefreshProviders(PsRequest request, string providerName, Version requiredVersion, Version minimumVersion, Version maximumVersion, bool logWarning) {
            //find and load the latest versions of the providers if only providerName exists, e.g., get-pp -name or import-pp -name 
            //find and load the particular provider if both providerName and version are provided
            _psProviderCacheTable.Clear();

            EnsurePowerShellInitialized();

            if (!string.IsNullOrWhiteSpace(providerName)) {
                //Get the list of available providers
                var modules = ScanForModules(request, requiredVersion, minimumVersion, maximumVersion, ProviderOption.AllProvider).ReEnumerable();

                var tasks = modules.AsyncForEach(modulePath => AnalyzeModule(request, modulePath.Key, modulePath.Value.Version ?? new Version(0, 0), false, logWarning, modulePath.Value));
                tasks.WaitAll();
            } else {
                //find all providers but only load the latest if no name nor version exists, e.g. get-pp -list 

                //Scan for the all available providers
                var results = ScanForModules(request, null, null, null, ProviderOption.AllProvider).ToArray();

                if (!_psProviderCacheTable.Any()) {
                    return;
                }

                foreach (var list in _psProviderCacheTable.Values.WhereNotNull()) {
                    var psInfo = list.OrderByDescending(each => each.ModuleInfo.Version).ToArray();
                    if (!psInfo.Any()) {
                        continue;
                    }

                    PackageProvider pkgProvider = null;
                    for (var index = 0; index < psInfo.Length; index++) {
                        var providerItem = psInfo[index];
                        if (providerItem == null) {
                            continue;
                        }

                        // if the provider is a dll, we will just provide a default provider, assuming the module name will be the name of the provider and add to the cache table (if it is not already loaded)
                        if (string.Equals(Path.GetExtension(providerItem.ProviderPath), ".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            // check whether it is already loaded or not
                            var loadedProvider = PackageManagementService.PackageProviders.FirstOrDefault(item => string.Equals(item.ProviderPath, providerItem.ProviderPath, StringComparison.OrdinalIgnoreCase));

                            // only provide default provider if it is not loaded
                            if (loadedProvider == null)
                            {
                                // here moduleinfo.providercategory is the module name.
                                // we are applying the guideline that module name is the same as provider name so we use that as the provider name
                                AddToPackageProviderCacheTable(new PackageProvider(new DefaultPackageProvider(providerItem.ModuleInfo.Name, providerItem.ModuleInfo.Version.ToString()))
                                {
                                    ProviderPath = providerItem.ProviderPath,
                                    Version = providerItem.ModuleInfo.Version,
                                    IsLoaded = false
                                });
                            }

                            continue;
                        }

                        //load the provider that has the latest version
                        if (pkgProvider == null) {
                            // analyze the module
                            pkgProvider = AnalyzeModule(request, providerItem.ProviderPath, providerItem.ModuleInfo.Version, false, logWarning, providerItem.ModuleInfo);
                        } else {
                            //the rest of providers under the same module will just create a provider object for the output but not loaded
                            var packageProvider = new PackageProvider(new DefaultPackageProvider(pkgProvider.ProviderName, providerItem.ModuleInfo.Version.ToString()))
                            {
                                ProviderPath = providerItem.ProviderPath,
                                Version = providerItem.ModuleInfo.Version,
                                IsLoaded = false
                            };
                            AddToPackageProviderCacheTable(packageProvider);
                        }
                    }
                }

                _psProviderCacheTable.Clear();
            }
        }

        private IEnumerable<PackageProvider> FindMatchedProvidersFromInternalCacheTable(PsRequest request, string providerPath) {
            //Search from the internal table to see if we can the matched provider
  
            var providers = PackageManagementService.ProviderCacheTable
                .Select(each => each.Value).Select(list => list
                .Where(each => each.ProviderPath.EqualsIgnoreCase(providerPath) && each.IsLoaded)
                .Select(each => each));

            return providers.SelectMany(list => {
                var packageProviders = list as PackageProvider[] ?? list.ToArray();
                return packageProviders;
            });
        }

        public IEnumerable<object> LoadAvailableProvider(PsRequest request, string modulePath, Version requiredVersion, bool force) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrEmpty(modulePath)) {
                throw new ArgumentNullException("modulePath");
            }

            EnsurePowerShellInitialized();

            //Check if it is already in the cache table
            var providersAlreadyImported = FindMatchedProvidersFromInternalCacheTable(request, modulePath).ToArray();

            if (providersAlreadyImported.Any() && !force) {
                return providersAlreadyImported;
            }

            //Trying to load it from the path directly
            var pkgProvider = AnalyzeModule(request, modulePath, requiredVersion ?? new Version(0, 0), force);
            if (pkgProvider != null) {
                return new[] { pkgProvider }.WhereNotNull();
            }

            request.Error(PackageManagement.Internal.Constants.Messages.FailedToImportProvider,
                ErrorCategory.InvalidOperation.ToString(), PowerShellGet, string.Format(CultureInfo.CurrentCulture, Resources.Messages.FailedToImportProvider, modulePath));
            return Enumerable.Empty<PackageProvider>();
        }

        private PackageProvider AnalyzeModule(PsRequest request, string modulePath, Version requiredVersion, bool force, bool logWarning =true, PSModuleInfo psModuleInfo = null)
        {
            if (string.IsNullOrWhiteSpace(modulePath)) {
                return null;
            }

            request.Debug("Attempting to load PowerShell Provider Module [{0}]", modulePath);

            if (string.Equals(Path.GetExtension(modulePath), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (psModuleInfo != null)
                {
                    // fake provider and returns it
                    var result = new PackageProvider(new DefaultPackageProvider(psModuleInfo.Name, psModuleInfo.Version.ToString()))
                    {
                        ProviderPath = modulePath,
                        Version = psModuleInfo.Version,
                        IsLoaded = false
                    };

                    AddToPackageProviderCacheTable(result);

                    return result;
                }
                else
                {
                    // psmoduleinfo is only null when this function is called in loadavailableprovider
                    // but in that case we want to load the provider directly anyway so we can do this
                    // if the path is a .dll then we ask packagemanagement to load it for us
                    // it will also register the dll
                    PackageManagementService.LoadProviderAssembly(request, modulePath, true);

                    // now let's checked whether we can find it in the list of loaded providers
                    foreach (var loadedProvider in PackageManagementService.PackageProviders)
                    {
                        // the one loaded should have the same path
                        if (string.Equals(loadedProvider.ProviderPath, modulePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return loadedProvider;
                        }
                    }

                    // if we reached here then we have failed to load the provider :(
                    return null;
                }                
            }

            string requiredVersionString = requiredVersion.ToString();
            var provider = Create(request, modulePath, requiredVersionString, force, logWarning);
            if (provider != null) {
                var providerName = provider.GetPackageProviderName();
                if (!string.IsNullOrWhiteSpace(providerName)) {
                    request.Debug(string.Format(CultureInfo.CurrentCulture, Resources.Messages.SuccessfullyLoadedModule, modulePath));
                    // looks good to me, let's add this to the list of modules this meta provider can create.

                    var packageProvider = new PackageProvider(provider.As<IPackageProvider>()) {
                        IsLoaded = true,
                        Version = provider.GetProviderVersion(),
                        ProviderPath = modulePath
                    };

                    // take out powershell get
                    var psgetprovider = PackageManagementService.PackageProviders.FirstOrDefault(pv => string.Equals(pv.ProviderName, PowerShellGet, StringComparison.OrdinalIgnoreCase));

                    if (psModuleInfo != null)
                    {
                        // add swidtag information using moduleinfo
                        // however, this won't give us as much information yet
                        // we may have to fill this up later
                        ProvideSwidTagInformation(packageProvider, psModuleInfo);
                    }
                    
                    AddToPackageProviderCacheTable(packageProvider);
                    _availableProviders.AddOrSet(providerName, provider);

                    return packageProvider;
                } else {
                    provider.Dispose();
                    provider = null;
                    request.Debug(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ProviderNameIsNullOrEmpty, modulePath));
                }
            }
            return null;
        }

        /// <summary>
        /// If we cannot use psget to get swidtag information, we will try to fill in some of the information
        /// </summary>
        /// <param name="packageProvider"></param>
        /// <param name="psModuleInfo"></param>
        private void ProvideSwidTagInformation(PackageProvider packageProvider, PSModuleInfo psModuleInfo)
        {
            if (packageProvider == null || psModuleInfo == null)
            {
                return;
            }

            packageProvider.VersionScheme = "MultiPartNumeric";

            Microsoft.PackageManagement.Internal.Packaging.SoftwareMetadata softwareMetadata = new Microsoft.PackageManagement.Internal.Packaging.SoftwareMetadata();
            bool changed = false;

            var type = psModuleInfo.GetType();

            // introduced in ps 2.0
            if (!string.IsNullOrWhiteSpace(psModuleInfo.Description))
            {
                softwareMetadata.Description = psModuleInfo.Description;
                changed = true;
            }

            // introduced in ps 3.0
            if (!string.IsNullOrWhiteSpace(psModuleInfo.Copyright))
            {
                softwareMetadata.AddAttribute("copyright", psModuleInfo.Copyright);
                changed = true;
            }

            // tags is introduced in ps 5.0
            var tagsProperty = type.GetProperty("Tags");
            bool isV5 = tagsProperty != null;

            if (isV5)
            {
                // introduced in ps 5.0
                var tags = tagsProperty.GetValue(psModuleInfo);

                // check that we have something in tags
                if (tags is IEnumerable<string> && (tags as IEnumerable<string>).Any())
                {
                    softwareMetadata.AddAttribute("tags", string.Join(" ", (tags as IEnumerable<string>).Distinct()));
                    changed = true;
                }

                var releaseNotes = type.GetProperty("ReleaseNotes").GetValue(psModuleInfo);

                // check that we have something in releasenotes
                if (releaseNotes is string && !string.IsNullOrWhiteSpace(type.GetProperty("ReleaseNotes").GetValue(psModuleInfo) as string))
                {
                    softwareMetadata.AddAttribute("tags", string.Join(" ", (tags as IEnumerable<string>).Distinct()));
                    changed = true;
                }
            }

            if (changed)
            {
                packageProvider.AddElement(softwareMetadata);
            }

            if (isV5)
            {
                var iconUri = type.GetProperty("IconUri").GetValue(psModuleInfo);

                // introduced in ps 5.0
                if (iconUri is Uri)
                {
                    packageProvider.AddLink(iconUri as Uri, "icon");
                }

                var licenseUri = type.GetProperty("LicenseUri").GetValue(psModuleInfo);

                // introduced in ps 5.0
                if (licenseUri is Uri)
                {
                    packageProvider.AddLink(licenseUri as Uri, "license");
                }

                var projectUri = type.GetProperty("ProjectUri").GetValue(psModuleInfo);

                // introduced in ps 5.0
                if (projectUri is Uri)
                {
                    packageProvider.AddLink(projectUri as Uri, "project");
                }

            }

            // introduced in ps 3.0
            if (!string.IsNullOrWhiteSpace(psModuleInfo.Author))
            {
                packageProvider.AddEntity(psModuleInfo.Author, null, "author");
            }

            // introduced in ps 3.0
            if (!string.IsNullOrWhiteSpace(psModuleInfo.CompanyName))
            {
                packageProvider.AddEntity(psModuleInfo.CompanyName, null, "owner");
            }        
        }
    }
}
