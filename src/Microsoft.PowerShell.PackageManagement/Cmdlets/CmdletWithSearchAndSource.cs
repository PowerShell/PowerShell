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
    using System.Globalization;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Language;
    using System.Security;
    using System.Threading;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;
    using Utility;
    using CollectionExtensions = Microsoft.PackageManagement.Internal.Utility.Extensions.CollectionExtensions;
    using Constants = PackageManagement.Constants;
    using DictionaryExtensions = Microsoft.PackageManagement.Internal.Utility.Extensions.DictionaryExtensions;
    using Directory = System.IO.Directory;
    using FilesystemExtensions = Microsoft.PackageManagement.Internal.Utility.Extensions.FilesystemExtensions;

    public abstract class CmdletWithSearchAndSource : CmdletWithSearch {
        internal readonly OrderedDictionary<string, List<SoftwareIdentity>> _resultsPerName = new OrderedDictionary<string, List<SoftwareIdentity>>();
        protected List<PackageProvider> _providersNotFindingAnything = new List<PackageProvider>();
#if CORECLR
        internal static readonly string[] ProviderFilters = new[] { "Packagemanagement", "Provider", "PSEdition_Core" };
#else
        internal static readonly string[] ProviderFilters = new[] { "Packagemanagement", "Provider" };
#endif
        protected const string Bootstrap = "Bootstrap";
        protected const string PowerShellGet = "PowerShellGet";
        internal static readonly string[] RequiredProviders = new[] { Bootstrap, PowerShellGet };
        private readonly HashSet<string> _sourcesTrusted = new HashSet<string>();
        private readonly HashSet<string> _sourcesDeniedTrust = new HashSet<string>();
        private bool _yesToAll = false;
        private bool _noToAll = false;

        protected CmdletWithSearchAndSource(OptionCategory[] categories)
            : base(categories) {
        }

        private string[] _sources;

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's a performance thing.")]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public virtual string[] Source
        {
            get
            {
                return _sources;
            }
            set
            {
                _sources = value;

                if (_sources != null && _sources.Length != 0) {
                    ProviderInfo providerInfo;

                    _sources = _sources.SelectMany(source => {
                        if (source.Equals(".")) {
                            source = ".\\";
                        }
                        //Need to resolve the path created via psdrive. 
                        //e.g., New-PSDrive -Name x -PSProvider FileSystem -Root \\foobar\myfolder. Here we are resolving x:\
                        try
                        {
                            if (FilesystemExtensions.LooksLikeAFilename(source))
                            {
                                var resolvedPaths = GetResolvedProviderPathFromPSPath(source, out providerInfo);

                                // Ensure the path is a single path from the file system provider
                                if ((providerInfo != null) && (resolvedPaths.Count == 1) && String.Equals(providerInfo.Name, "FileSystem", StringComparison.OrdinalIgnoreCase))
                                {
                                    return resolvedPaths[0].SingleItemAsEnumerable();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //allow to continue handling the cases other than file system       
                        }

                        return source.SingleItemAsEnumerable();
                    }).ToArray();
                }
            }
        }


        [Parameter]
        public virtual PSCredential Credential {get; set;}

        [Parameter]
        [ValidateNotNull()]
        public Uri Proxy { get; set; }

        [Parameter]
        [ValidateNotNull()]
        public PSCredential ProxyCredential { get; set; }

        public override IEnumerable<string> Sources
        {
            get
            {
                if (CollectionExtensions.IsNullOrEmpty(Source)) {
                    return Microsoft.PackageManagement.Internal.Constants.Empty;
                }

                return Source;
            }
        }

        /*
        protected override IEnumerable<PackageProvider> SelectedProviders {
            get {
                // filter on provider names  - if they specify a provider name, narrow to only those provider names.
                var providers = SelectProviders(ProviderName).ReEnumerable();

                // filter out providers that don't have the sources that have been specified (only if we have specified a source!)
                if (Source != null && Source.Length > 0) {
                    // sources must actually match a name or location. Keeps providers from being a bit dishonest

                    var potentialSources = providers.SelectMany(each => each.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing)).Where(source => Source.ContainsAnyOfIgnoreCase(source.Name, source.Location))).ReEnumerable();

                    // prefer registered sources
                    var registeredSources = potentialSources.Where(source => source.IsRegistered).ReEnumerable();

                    providers = registeredSources.Any() ? registeredSources.Select(source => source.Provider).Distinct().ReEnumerable() : potentialSources.Select(source => source.Provider).Distinct().ReEnumerable();
                }
                // filter on: dynamic options - if they specify any dynamic options, limit the provider set to providers with those options.
                return FilterProvidersUsingDynamicParameters(providers).ToArray();
            }
        }
        */

        /// <summary>
        /// Returns web proxy that provider can use
        /// Construct the webproxy using InternalWebProxy
        /// </summary>
        public override System.Net.IWebProxy WebProxy
        {
            get
            {
                if (Proxy != null)
                {
                    return new PackageManagement.Utility.InternalWebProxy(Proxy, ProxyCredential == null ? null : ProxyCredential.GetNetworkCredential());
                }

                return null;
            }
        }

        public override string CredentialUsername
        {
            get
            {
                return Credential != null ? Credential.UserName : null;
            }
        }

        public override SecureString CredentialPassword
        {
            get
            {
                return Credential != null ? Credential.Password : null;
            }
        }

        public override bool ProcessRecordAsync() {
            // record the names in the collection.
            if (!CollectionExtensions.IsNullOrEmpty(Name)) {
                foreach (var name in Name) {
                    DictionaryExtensions.GetOrAdd(_resultsPerName, name, () => null);
                }
            }

            // it's searching for packages.
            // we need to do the actual search for the packages now,
            // and hold the results until EndProcessingAsync()
            // where we can determine if we we have no ambiguity left.
            SearchForPackages();

            return true;
        }



        private MutableEnumerable<string> FindFiles(string path) {
            if (FilesystemExtensions.LooksLikeAFilename(path)) {
                ProviderInfo providerInfo;
                var paths = GetResolvedProviderPathFromPSPath(path, out providerInfo).ReEnumerable();
                return
                    paths.SelectMany(
                        each => FilesystemExtensions.FileExists(each) ? CollectionExtensions.SingleItemAsEnumerable(each) : FilesystemExtensions.DirectoryExists(each) ? Directory.GetFiles(each) : Microsoft.PackageManagement.Internal.Constants.Empty)
                        .ReEnumerable();
            }
            return Microsoft.PackageManagement.Internal.Constants.Empty.ReEnumerable();
        }


        protected bool SpecifiedMinimumOrMaximum
        {
            get
            {
                return !string.IsNullOrWhiteSpace(MaximumVersion) || !string.IsNullOrWhiteSpace(MinimumVersion);
            }
        }

        private List<Uri> _uris = new List<Uri>();
        private Dictionary<string, Tuple<List<string>, byte[]>> _files = new Dictionary<string, Tuple<List<string>, byte[]>>(StringComparer.OrdinalIgnoreCase);

        private IEnumerable<string> _names;

        private bool IsUri(string name) {
            Uri packageUri;
            if (Uri.TryCreate(name, UriKind.Absolute, out packageUri)) {
                // if it's an uri, then we search via uri or file!
                if (!packageUri.IsFile) {
                    _uris.Add(packageUri);
                    return true;
                }
            }
            return false;
        }


        private bool IsFile(string name) {
            var files = FindFiles(name);
            if (files.Any()) {
                foreach (var f in files) {
                    if (_files.ContainsKey(f)) {
                        // if we've got this file already by another parameter, just update it to
                        // keep track that we've somehow got it twice.
                        _files[f].Item1.Add(name);
                    } else {
                        // otherwise, lets' grab the first chunk of this file so we can check what providers
                        // can handle it (later)
                        DictionaryExtensions.Add(_files, f, new List<string> {
                            name
                        }, FilesystemExtensions.ReadBytes(f, 1024));
                    }
                }

                return true;
            }
            return false;
        }

        protected virtual IHostApi GetProviderSpecificOption(PackageProvider pv) {
            return this.ProviderSpecific(pv);
        }

        protected virtual bool EnsurePackageIsProvider(SoftwareIdentity package) {
            return true;
        }


        protected override string BootstrapNuGet
        {
            get
            {
                //find, install, save- inherits from this class. They all need bootstrap NuGet if does not exists.
                return "true";
            }
        }
        /// <summary>
        ///  Validate if the package is a provider package. 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        protected bool ValidatePackageProvider(SoftwareIdentity package) {
            //no need to filter on the packages from the bootstrap site as only providers will be published out there.
            if (package.ProviderName.EqualsIgnoreCase("Bootstrap")) {
                return true;
            }

            //get the tags info from the package's swid
            var tags = package.Metadata["tags"].ToArray();

            //Check if the provider has provider tags
            var found = false;
            foreach (var filter in ProviderFilters) {
                found = false;
                if (tags.Any(tag => tag.ContainsIgnoreCase(filter))) {
                    found = true;
                } else {
                    break;
                }
            }
            return found;
        }

        private void ProcessRequests(PackageProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
            {
                return;
            }

            var requests = providers.SelectMany(pv => {
                Verbose(Resources.Messages.SelectedProviders, pv.ProviderName);
                // for a given provider, if we get an error, we want just that provider to stop.
                var host = GetProviderSpecificOption(pv);

                var a = _uris.Select(uri => new {
                    query = new List<string> {
                            uri.AbsolutePath
                        },
                    provider = pv,
                    packages = pv.FindPackageByUri(uri, host).CancelWhen(CancellationEvent.Token)
                });

                var b = _files.Keys.Where(file => pv.IsSupportedFile(_files[file].Item2)).Select(file => new {
                    query = _files[file].Item1,
                    provider = pv,
                    packages = pv.FindPackageByFile(file, host)
                });

                var c = _names.Select(name => new {
                    query = new List<string> {
                            name
                        },
                    provider = pv,
                    packages = pv.FindPackage(name, RequiredVersion, MinimumVersion, MaximumVersion, (Sources != null && Sources.Count() > 1) ? host.SuppressErrorsAndWarnings(IsProcessing) : host)
                });

                return a.Concat(b).Concat(c);
            }).ToArray();

            Debug("Calling SearchForPackages After Select {0}", requests.Length);

            if (AllVersions || !SpecifiedMinimumOrMaximum) {
                // the user asked for every version or they didn't specify any version ranges
                // either way, that means that we can just return everything that we're finding.

                while (WaitForActivity(requests.Select(each => each.packages))) {

                    // keep processing while any of the the queries is still going.

                    foreach (var result in requests.Where(each => each.packages.HasData)) {
                        // look only at requests that have data waiting.

                        foreach (var package in result.packages.GetConsumingEnumerable()) {
                            // process the results for that set.
                            // check if the package is a provider package. If so we need to filter on the packages for the providers.
                            if (EnsurePackageIsProvider(package)) {
                                ProcessPackage(result.provider, result.query, package);
                            }
                        }
                    }

                    requests = requests.FilterWithFinalizer(each => each.packages.IsConsumed, each => each.packages.Dispose()).ToArray();
                }


            } else {
                // now this is where it gets a bit funny.
                // the user specified a min or max
                // and so we have to only return the highest one in the set for a given package.

                while (WaitForActivity(requests.Select(each => each.packages))) {
                    // keep processing while any of the the queries is still going.
                    foreach (var perProvider in requests.GroupBy(each => each.provider)) {
                        foreach (var perQuery in perProvider.GroupBy(each => each.query)) {
                            if (perQuery.All(each => each.packages.IsCompleted && !each.packages.IsConsumed)) {
                                foreach (var pkg in from p in perQuery.SelectMany(each => each.packages.GetConsumingEnumerable())
                                                    group p by new {
                                                        p.Name,
                                                        p.Source
                                                    }
                                                        // for a given name
                                                        into grouping
                                                        // get the latest version only
                                                        select grouping.OrderByDescending(pp => pp, SoftwareIdentityVersionComparer.Instance).First()) {

                                    if (EnsurePackageIsProvider(pkg)) {
                                        ProcessPackage(perProvider.Key, perQuery.Key, pkg);
                                    }
                                }
                            }
                        }
                    }
                    // filter out whatever we're done with.
                    requests = requests.FilterWithFinalizer(each => each.packages.IsConsumed, each => each.packages.Dispose()).ToArray();
                }
            }

            // dispose of any requests that didn't get cleaned up earlier.
            foreach (var i in requests) {
                i.packages.Dispose();
            }
        }

        protected virtual void SearchForPackages() {
            try {

                var providers = SelectedProviders.ToArray();

                // filter the items into three types of searches
                _names = CollectionExtensions.IsNullOrEmpty(Name) ? CollectionExtensions.SingleItemAsEnumerable(string.Empty) : Name.Where(each => !IsUri(each) && !IsFile(each)).ToArray();

                foreach (var n in _names) {
                    Debug("Calling SearchForPackages. Name='{0}'", n);
                }

                ProcessRequests(providers.Where(pv => string.Equals(pv.ProviderName, "bootstrap", StringComparison.OrdinalIgnoreCase)).ToArray());

                ProcessRequests(providers.Where(pv => !string.Equals(pv.ProviderName, "bootstrap", StringComparison.OrdinalIgnoreCase)).ToArray());

            } catch (Exception ex) {

                Debug(ex.ToString());
            }
        }

        protected virtual void ProcessPackage(PackageProvider provider, IEnumerable<string> searchKey, SoftwareIdentity package) {
            foreach (var key in searchKey) {

                _resultsPerName.GetOrSetIfDefault(key, () => new List<SoftwareIdentity>()).Add(package);
            }
        }

        protected bool CheckUnmatchedPackages() {
            var unmatched = _resultsPerName.Keys.Where(each => _resultsPerName[each] == null).ReEnumerable();
            var result = true;

            if (unmatched.Any()) {
                // whine about things not matched.
                foreach (var name in unmatched) {

                    Debug(string.Format(CultureInfo.CurrentCulture, "unmatched package name='{0}'", name));

                    if (name == string.Empty) {
                        // no name
                        result = false;
                        Error(Constants.Errors.NoPackagesFoundForProvider, _providersNotFindingAnything.Select(each => each.ProviderName).JoinWithComma());
                    } else {
                        if (WildcardPattern.ContainsWildcardCharacters(name)) {
                            Verbose(Constants.Messages.NoMatchesForWildcard, name);
                            result = false; // even tho' it's not an 'error' it is still enough to know not to actually install.
                        } else {
                            result = false;
                            NonTerminatingError(Constants.Errors.NoMatchFoundForCriteria, name);
                        }
                    }
                }
            }

            return result;
        }

         protected IEnumerable<SoftwareIdentity> CheckMatchedDuplicates() {
            // if there are overmatched packages we need to know why:
            // are they found across multiple providers?
            // are they found accross multiple sources?
            // are they all from the same source?
 
            foreach (var list in _resultsPerName.Values.Where(each => each != null && each.Any())) {
                if (list.Count == 1) {
                    //no overmatched
                    yield return list.FirstOrDefault();
                } else {
                    //process the overmatched case
                    SoftwareIdentity selectedPackage = null;

                    var providers = list.Select(each => each.ProviderName).Distinct().ToArray();
                    var sources = list.Select(each => each.Source).Distinct().ToArray();

                    //case: a user specifies -Source and multiple packages are found. In this case to determine which one should be installed,
                    //      We treat the user's package source array is in a priority order, i.e. the first package source has the highest priority.
                    //      Of course, these packages should not be from a single source with the same provider.
                    //      Example: install-package -Source @('src1', 'src2')
                    //               install-package -Source @('src1', 'src2') -Provider @('p1', 'p2')
                    if (Sources.Any() && (providers.Length != 1 || sources.Length != 1)) {
                        // let's use the first source as our priority.As long as we find a package, we exit the 'for' loop righ away
                        foreach (var source in Sources) {
                            //select all packages matched source
                            var pkgs = list.Where(package => source.EqualsIgnoreCase(package.Source) || (UserSpecifiedSourcesList.Keys.ContainsIgnoreCase(package.Source) && source.EqualsIgnoreCase(UserSpecifiedSourcesList[package.Source]))).ToArray();
                            if (pkgs.Length == 0) {
                                continue;
                            }
                            if (pkgs.Length == 1) {
                                //only one provider found the package
                                selectedPackage = pkgs[0];
                                break;
                            }
                            if (ProviderName == null) {
                                //user does not specify '-providerName' but we found multiple packages with a source, can not determine which one
                                //will error out
                                break;
                            }
                            if (pkgs.Length > 1) {
                                //case: multiple providers matched the same source. 
                                //need to process provider's priority order                               
                                var pkg = ProviderName.Select(p => pkgs.FirstOrDefault(each => each.ProviderName.EqualsIgnoreCase(p))).FirstOrDefault();
                                if (pkg != null) {
                                    selectedPackage = pkg;
                                    break;
                                }
                            }
                        }//inner foreach

                        //case: a user specifies -Provider array but no -Source and multiple packages are found. In this case to determine which one should be installed,
                        //      We treat the user's package provider array is in a priority order, i.e. the first provider in the array has the highest priority.
                        //      Of course, these packages should not be from a single source with the same provider.
                        //      Example: install-package -Provider @('p1', 'p2')
                    } else if (ProviderName != null && ProviderName.Any() && (providers.Length != 1 || sources.Length != 1)) {
                        foreach (var providerName in ProviderName) {

                            //select all packages matched with the provider name
                            var packages = list.Where(each => providerName.EqualsIgnoreCase(each.ProviderName)).ToArray();
                            if (packages.Length == 0) {
                                continue;
                            }
                            if (packages.Length == 1) {
                                //only one provider found the package, that's good
                                selectedPackage = packages[0];
                                break;
                            } else {
                                //user does not specify '-source' but we found multiple packages with one provider, we can not determine which one
                                //will error out 
                                break;
                            }
                        }
                    }

                    if (selectedPackage != null) {
                        yield return selectedPackage;
                    } else {
                        //error out for the overmatched case
                        var suggestion = "";
                        if (providers.Length == 1) {
                            // it's matching this package multiple times in the same provider.
                            if (sources.Length == 1) {
                                // matching it from a single source.
                                // be more exact on matching name? or version?
                                suggestion = Resources.Messages.SuggestRequiredVersion;
                            } else {
                                // it's matching the same package from multiple sources
                                // tell them to use -source
                                suggestion = Resources.Messages.SuggestSingleSource;
                            }
                        } else {
                            // found across multiple providers
                            // must specify -provider
                            suggestion = Resources.Messages.SuggestSingleProviderName;
                        }

                        string searchKey = null;

                        foreach (var pkg in list) {

                            Warning(Constants.Messages.MatchesMultiplePackages, pkg.SearchKey, pkg.ProviderName, pkg.Name, pkg.Version, GetPackageSourceNameOrLocation(pkg));
                            searchKey = pkg.SearchKey;
                        }
                        Error(Constants.Errors.DisambiguateForInstall, searchKey, GetMessageString(suggestion, suggestion));
                    }
                }
            }
        }     

        // Performs a reverse lookup from Package Source Location -> Source Name for a Provider
        private string GetPackageSourceNameOrLocation(SoftwareIdentity package)
        {
            // Default to Source Location Url
            string packageSourceName = package.Source;

            // Get the package provider object            
            var packageProvider = SelectProviders(package.ProviderName).ReEnumerable();

            if (!packageProvider.Any())
            {
                return packageSourceName;
            }

            
            // For any issues with reverse lookup (SourceLocation -> SourceName), return Source Location Url                        
            try
            {
                var packageSource = packageProvider.Select(each => each.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing)).Where(source => source.IsRegistered && (source.Location.EqualsIgnoreCase(package.Source)))).ReEnumerable();

                if (packageSource.Any()) {
                    var pkgSource = packageSource.FirstOrDefault();
                    if (pkgSource != null) {
                        var source = pkgSource.FirstOrDefault();
                        if (source != null) {
                            packageSourceName = source.Name;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.Dump();
            }                        

            return packageSourceName;
        }

        protected bool InstallPackages(params SoftwareIdentity[] packagesToInstall) {

            if(packagesToInstall == null || packagesToInstall.Length == 0)
            {
                return false;
            }
            // first, check to see if we have all the required dynamic parameters
            // for each package/provider
            foreach (var package in packagesToInstall) {
                var pkg = package;
                foreach (var parameter in DynamicParameterDictionary.Values.OfType<CustomRuntimeDefinedParameter>()
                    .Where(param => param.IsSet == false && param.Options.Any(option => option.ProviderName == pkg.ProviderName && option.Category == OptionCategory.Install && option.IsRequired))) {
                    // this is not good. there is a required parameter for the package
                    // and the user didn't specify it. We should return the error to the user
                    // and they can try again.
                    Error(Constants.Errors.PackageInstallRequiresOption, package.Name, package.ProviderName, parameter.Name);
                    Cancel();
                }
            }

            if (IsCanceled) {
                return false;
            }
            var progressId = 0;
            var triedInstallCount = 0;

            if (packagesToInstall.Length > 1) {
                progressId = StartProgress(0, Constants.Messages.InstallingPackagesCount, packagesToInstall.Length);
            }
            var n = 0;

            foreach (var pkg in packagesToInstall) {
                if (packagesToInstall.Length > 1) {
                    Progress(progressId, (n*100/packagesToInstall.Length) + 1, Constants.Messages.InstallingPackageMultiple, pkg.Name, ++n, packagesToInstall.Length);
                }
                var provider = SelectProviders(pkg.ProviderName).FirstOrDefault();
                if (provider == null) {
                    Error(Constants.Errors.UnknownProvider, pkg.ProviderName);
                    return false;
                }

                // quickly check to see if this package is already installed.
                var installedPkgs = provider.GetInstalledPackages(pkg.Name, pkg.Version, null, null, this.ProviderSpecific(provider)).CancelWhen(CancellationEvent.Token).ToArray();
                if (IsCanceled) {
                    // if we're stopping, just get out asap.
                    return false;
                }

                // todo: this is a terribly simplistic way to do this, we'd better rethink this soon
                if (!Force) {
                    if (installedPkgs.Any(each => each.Name.EqualsIgnoreCase(pkg.Name) && each.Version.EqualsIgnoreCase(pkg.Version))) {
                        // it looks like it's already installed.
                        // skip it.
                        Verbose(Constants.Messages.SkippedInstalledPackage, pkg.Name, pkg.Version);

                        if (packagesToInstall.Length > 1) {
                            Progress(progressId, (n*100/packagesToInstall.Length) + 1, Constants.Messages.SkippedInstalledPackageMultiple, pkg.Name, n, packagesToInstall.Length);
                        }
                        continue;
                    }
                }

                
                try {
                    if (ShouldProcessPackageInstall(pkg.Name, pkg.Version, pkg.Source)) {
                        foreach (var installedPkg in provider.InstallPackage(pkg, this).CancelWhen(CancellationEvent.Token)) {
                            if (IsCanceled) {
                                // if we're stopping, just get out asap.
                                return false;
                            }

                            WriteObject(AddPropertyToSoftwareIdentity(installedPkg));
                            LogEvent(EventTask.Install, EventId.Install,
                                Resources.Messages.PackageInstalled,
                                installedPkg.Name,
                                installedPkg.Version,
                                installedPkg.ProviderName,
                                installedPkg.Source ?? string.Empty,
                                installedPkg.Status ?? string.Empty,
                                installedPkg.InstallationPath ?? string.Empty);
                            TraceMessage(Constants.InstallPackageTrace, installedPkg);
                            triedInstallCount++;
                        }
                    }
                } catch (Exception e) {
                    e.Dump();
                    Error(Constants.Errors.InstallationFailure, pkg.Name);
                    return false;
                }
                if (packagesToInstall.Length > 1) {
                    Progress(progressId, (n*100/packagesToInstall.Length) + 1, Constants.Messages.InstalledPackageMultiple, pkg.Name, n, packagesToInstall.Length);
                }
                
            }

            return (triedInstallCount == packagesToInstall.Length);
        }

        protected bool ShouldProcessPackageInstall(string packageName, string version, string source) {
            try {
                //-force is already handled before reaching this stage
                return ShouldProcess(FormatMessageString(Constants.Messages.TargetPackage, packageName, version, source), FormatMessageString(Constants.Messages.ActionInstallPackage)).Result;
            } catch {
            }
            return false;
        }

        public override bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource) {
            try {
                if (_sourcesTrusted.Contains(packageSource) || Force || WhatIf || _yesToAll) {
                    return true;
                }
                if (_sourcesDeniedTrust.Contains(packageSource) || _noToAll) {
                    return false;
                }
                var shouldContinueResult = ShouldContinue(FormatMessageString(Constants.Messages.QueryInstallUntrustedPackage, package, packageSource), FormatMessageString(Constants.Messages.CaptionSourceNotTrusted), true).Result;
                _yesToAll = shouldContinueResult.yesToAll;
                _noToAll = shouldContinueResult.noToAll;
                if (shouldContinueResult.result) {
                    _sourcesTrusted.Add(packageSource);
                    return true;
                } else {
                    _sourcesDeniedTrust.Add(packageSource);
                }
            } catch {
            }
            return false;
        }
    }
}
