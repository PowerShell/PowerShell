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
    using System.Globalization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.PackageManagement.Internal.Implementation;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using Internal.Utility.Plugin;
    using System.Management.Automation;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Provider.Utility;
    using SemanticVersion = Microsoft.PackageManagement.Provider.Utility.SemanticVersion;

    public abstract class PackageSourceListRequest : Request {

        private IEnumerable<PackageQuery> _packageQuery;
        private static IDictionary<string, PackageSource> _registeredPackageSources;
        private string _configurationFileLocation;
        private XDocument _config;
        private string _defaultConfig;
        private string PowerShellSourceURL = @"https://go.microsoft.com/fwlink/?LinkID=821777&clcid=0x409";
        private string PowerShellNanoSourceURL = @"https://go.microsoft.com/fwlink/?LinkID=821783&clcid=0x409";
        private string PowerShellSourceCatalogURL = @"https://go.microsoft.com/fwlink/?LinkID=823093&clcid=0x409";
        private string PowerShellNanoSourceCatalogURL = @"https://go.microsoft.com/fwlink/?LinkID=823094&clcid=0x409";
        private IEnumerable<string> _packageSources;
        private const string _PackageSourceListRequest = "PackageSourceListRequest";
        private HttpClient _httpClient;
        //private HttpClient _httpClientWithoutAcceptHeader;

        private static IDictionary<string, PackageSource> _checkedUnregisteredPackageSources = new ConcurrentDictionary<string, PackageSource>();
        private static Dictionary<string, PackageProvider> _packageProviders = new Dictionary<string, PackageProvider>();

        private static readonly Regex RegexFastPath = new Regex(@"\$(?<id>[\w,\+,\/,=]*)\\(?<version>[\w,\+,\/,=]*)");
        internal static readonly Regex RegexFastPathComplex = new Regex(@"\$(?<location>[\w,\+,\/,=]*)#(?<id>[\w,\+,\/,=]*)#(?<displayname>[\w,\+,\/,=]*)#(?<version>[\w,\+,\/,=]*)#(?<fastpath>[\w,\+,\/,=]*)");

        internal Lazy<bool> SkipValidate;
        internal readonly Lazy<bool> AllVersions;
        internal readonly Lazy<string[]> Headers;
        internal Lazy<bool> SkipHashValidation; 

        internal const WildcardOptions WildcardOptions = System.Management.Automation.WildcardOptions.CultureInvariant | System.Management.Automation.WildcardOptions.IgnoreCase;

        internal const string DefaultConfigDefinition = @"<?xml version=""1.0""?>
<configuration>
  <packageSources>
    <add key=""PSL"" value=""##"" />
  </packageSources>
</configuration>";

        internal const string EmptyConfig = @"<?xml version=""1.0""?>
<configuration>
  <packageSources>    
  </packageSources>
</configuration>";

        /// <summary>
        /// Ctor required by the PackageManagement Platform
        /// </summary>
        protected PackageSourceListRequest()
        {           
            AllVersions = new Lazy<bool>(() => GetOptionValue("AllVersions").IsTrue());
            SkipValidate = new Lazy<bool>(() => GetOptionValue("SkipValidate").IsTrue());
            Headers = new Lazy<string[]>(() => (GetOptionValues("Headers") ?? new string[0]).ToArray());
            SkipHashValidation = new Lazy<bool>(() => GetOptionValue("SkipHashValidation").IsTrue());
        }

        internal string DefaultJSONFileLocation
        {
            get
            {
                return Path.Combine(Environment.GetEnvironmentVariable("appdata"), Constants.ProviderName, Constants.JSONFileName);
            }
        }

        internal string DefaultCatalogFileLocation
        {
            get
            {
                return Path.Combine(Environment.GetEnvironmentVariable("appdata"), Constants.ProviderName, Constants.CatFileName);
            }
        }

        internal string DefaultJSONSourceLocation
        {
            get
            {
#if CORECLR
                    return PowerShellNanoSourceURL;                         
#else
                return PowerShellSourceURL;
#endif
            }
        }

        internal string DefaultJSONCatalogFileLocation
        {
            get
            {
#if CORECLR
                    return PowerShellNanoSourceCatalogURL;                         
#else
                return PowerShellSourceCatalogURL;
#endif
            }
        }

        internal string DefaultConfig
        {
            get
            {
                if (_defaultConfig == null)
                {
                    _defaultConfig = DefaultConfigDefinition.Replace("##", DefaultJSONFileLocation);
                }
                return _defaultConfig;
            }
        }


        internal IEnumerable<string> PackageSources
        {
            get
            {
                return _packageSources ?? (_packageSources = (Sources ?? new string[0]).ToArray());
            }
        }
        private IEnumerable<PackageQuery> Packages
        {
            get
            {
                if (_packageQuery == null)
                {
                    try
                    {
                        _packageQuery = SelectedSources.AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered).SelectMany(source =>
                        (new PackageQuery(source, this)).SingleItemAsEnumerable()).Where(each=>each.IsValid);                     
                    }
                    catch (Exception ex)
                    {
                        ex.Dump(this);
                    }
                }
                return _packageQuery;
            }
        }

        internal virtual bool IsValid
        {
            get
            {
                return _packageQuery != null;
            }
        }

        internal IEqualityComparer<PackageJson> PackageEqualityComparer { get; private set; }
        
        /// <summary>
        /// Supported package source schemes by this provider
        /// </summary>
        internal static IEnumerable<string> SupportedSchemes
        {
            get
            {
                return PackageSourceListProvider.Features[Constants.Features.SupportedSchemes];
            }
        }
        private string GetValue(string name) {
            // get the value from the request
            return (GetOptionValues(name) ?? Enumerable.Empty<string>()).LastOrDefault();
        }

        internal PackageJson GetPackage(string name) {
            return Packages.SelectMany(each => each.Query(name)).OrderByDescending(p => p.Version).FirstOrDefault();
        }

        internal PackageJson GetPackage(string name, string requiredVersion)
        {
            if (string.IsNullOrWhiteSpace(requiredVersion))
            {
                return Packages.SelectMany(each => each.Query(name)).FirstOrDefault();
            }

            return Packages.SelectMany(each => each.Query(name, requiredVersion)).FirstOrDefault();
        }

        internal IEnumerable<PackageJson> GetPackagesWithinVersionRange(string name, string minimumversion, string maximumversion)
        {
            return Packages.SelectMany(each => each.Query(name, minimumversion, maximumversion))
                .OrderByDescending(p => p.Version);
        }

        internal IEnumerable<PackageJson> GetPackages(string name)
        {
            var wildcardPattern = new WildcardPattern(name, WildcardOptions);

            return Packages.SelectMany(each => each.Query()).Distinct(PackageEqualityComparer)
                 .Where(p => !string.IsNullOrWhiteSpace(p.Name) && (string.IsNullOrWhiteSpace(name) || (wildcardPattern.IsMatch(p.Name) || wildcardPattern.IsMatch(p.DisplayName))));            
        }

        internal IEnumerable<PackageJson> GetPackages(string name, string requiredVersion, string minimumversion, string maximumversion)
        {
            if (!string.IsNullOrWhiteSpace(requiredVersion))
            {
                return Packages.SelectMany(each => each.Query(name, requiredVersion));
            }

            return Packages.SelectMany(each => each.Query(name, minimumversion, maximumversion))
                .OrderByDescending(p => p.Version);
        }

        internal static PackageProvider FindProvider(PackageSourceListRequest request, string providerType, IHostApi hostApi, bool logError = false) {

            string providerName = null;
            switch (providerType.ToLowerInvariant()) {
                case Constants.MediaType.MsiPackage:
                    providerName = Constants.ProviderNames.Msi;
                    break;
                case Constants.MediaType.MsuPackage:
                    providerName = Constants.ProviderNames.Msu;
                    break;
                case Constants.MediaType.AppxPackage:
                    //TODO for future whenever needed to support appx packages
                    break;
                case Constants.MediaType.NuGetPackage:
                    providerName = Constants.ProviderNames.NuGet;
                    break;
                case Constants.MediaType.ZipPackage:
                case Constants.MediaType.ExePackage:
                    providerName = Constants.ProviderNames.PSL;
                    break;
                case Constants.MediaType.PsArtifacts:
                    providerName = Constants.ProviderNames.PowerShellGet;
                    break;

                default:
                    request.Warning(Resources.Messages.UnsupportedProviderType, providerType);
                    break;
            }

            if (string.IsNullOrWhiteSpace(providerName)) {
                return null;
            }

            PackageProvider provider;
            if (_packageProviders.ContainsKey(providerName)) {
                provider = _packageProviders[providerName];
            } else {
                provider = request.PackageManagementService.SelectProviders(providerName, request).FirstOrDefault();
                if (provider != null) {
                    _packageProviders.AddOrSet(providerName, provider);
                }
            }

            if (provider != null) {
                return provider;
            }

            request.Verbose(Resources.Messages.ProviderNotFound, providerName);

            if (logError) {
                request.Error(ErrorCategory.InvalidOperation, providerName, Resources.Messages.ProviderNotFound, providerName);
            }
            return null;
        }

        internal static string CreateCanonicalId(PackageJson package, string providerName)
        {
            // example: "nuget:jquery/2.1.0#http://nuget.org/api/v2"
            if (package == null || string.IsNullOrEmpty(package.Name) || string.IsNullOrEmpty(providerName))
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(package.Version) && string.IsNullOrWhiteSpace(package.Source))
            {
                return "{0}:{1}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(providerName), package.Name);
            }
            if (string.IsNullOrWhiteSpace(package.Source))
            {
                return "{0}:{1}/{2}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(providerName), package.Name, package.Version);
            }

            Uri pkgId;
            var source = package.Source;
            if (Uri.TryCreate(package.Source, UriKind.Absolute, out pkgId))
            {
                source = pkgId.AbsoluteUri;
            }

            if (string.IsNullOrWhiteSpace(package.Version))
            {
                "{0}:{1}#{2}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(providerName), package.Name, source);
            }

            return "{0}:{1}/{2}#{3}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(providerName), package.Name, package.Version, source);
        }


        internal string MakeFastPath(PackageJson package)
        {
            return string.Format(CultureInfo.InvariantCulture, @"${0}\{1}", package.Name.ToBase64(), package.Version.ToBase64());
        }
        private bool TryParseFastPath(string fastPath, Regex regex, out string id, out string version)
        {
            var match = regex.Match(fastPath);
            id = match.Success ? match.Groups["id"].Value.FromBase64() : null;
            version = match.Success ? match.Groups["version"].Value.FromBase64() : null;
            return match.Success;
        }
      
        /// <summary>
        /// Get the PackageItem object from the fast path
        /// </summary>
        /// <param name="fastPath"></param>
        /// <returns></returns>
        internal PackageJson GetPackageByFastPath(string fastPath)
        {
            string id;
            string version;

            if (TryParseFastPath(fastPath, RegexFastPath, out id, out version))
            {
                return GetPackage(id, version);
            }

            return null;
        }


        internal static string MakeFastPathComplex(string source, string name, string displayName, string version, string fastPackageReference)
        {
            return string.Format(CultureInfo.InvariantCulture, @"${0}#{1}#{2}#{3}#{4}", (source ?? "").ToBase64(), (name ?? "").ToBase64(), (displayName ?? "").ToBase64(), (version ?? "").ToBase64(), (fastPackageReference ?? "").ToBase64());
        }

        internal bool TryParseFastPathComplex(string fastPath, Regex regex, out string location, out string id, out string displayname, out string version, out string fastpath)
        {
            var match = regex.Match(fastPath);
            location = match.Success ? match.Groups["location"].Value.FromBase64() : null;
            id = match.Success ? match.Groups["id"].Value.FromBase64() : null;
            displayname = match.Success ? match.Groups["displayname"].Value.FromBase64() : null;
            version = match.Success ? match.Groups["version"].Value.FromBase64() : null;
            fastpath = match.Success ? match.Groups["fastpath"].Value.FromBase64() : null;

            return match.Success;
        }


        internal PackageJson GetFastReferenceComplex(string fastPath)
        {
            string sourceLocation;
            string id;
            string displayname;
            string version;
            string fastreference;

            if (TryParseFastPathComplex(fastPath: fastPath, regex: RegexFastPathComplex, location: out sourceLocation, id: out id, displayname: out displayname, version: out version, fastpath: out fastreference))
            {               
                var ver = (new SemanticVersion(version)).ToString();
                return GetPackage(id, ver) ?? GetPackage(displayname, ver);
            }

            return null;
        }
      

        internal IEnumerable<PackageJson> FilterOnVersion(IEnumerable<PackageJson> pkgs, string requiredVersion, string minimumVersion, string maximumVersion, bool minInclusive = true, bool maxInclusive = true, bool latest=true)
        {
            if (!string.IsNullOrWhiteSpace(requiredVersion))
            {
                pkgs = pkgs.Where(each => each.SemVer == new SemanticVersion(requiredVersion));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(minimumVersion))
                {
                    // if minInclusive, then use >= else use >
                    if (minInclusive)
                    {
                        pkgs = pkgs.Where(each => each.SemVer >= new SemanticVersion(minimumVersion));
                    }
                    else
                    {
                        pkgs = pkgs.Where(each => each.SemVer > new SemanticVersion(minimumVersion));
                    }
                }

                if (!String.IsNullOrWhiteSpace(maximumVersion))
                {
                    // if maxInclusive, then use < else use <=
                    if (maxInclusive)
                    {
                        pkgs = pkgs.Where(each => each.SemVer <= new SemanticVersion(maximumVersion));
                    }
                    else
                    {
                        pkgs = pkgs.Where(each => each.SemVer < new SemanticVersion(maximumVersion));
                    }
                }
            }

            return latest ? pkgs.OrderByDescending(each => each.Version).FirstOrDefault().SingleItemAsEnumerable() : pkgs;
        }

        internal bool YieldFromSwidtag(PackageJson package, string searchKey)
        {
            if (package == null)
            {
                return !IsCanceled;
            }

            var targetFilename = package.FilePath;
            var summary = package.Summary;

            var fastPath = MakeFastPath(package);

            // As 'nodejs' become 'node.js' after the install, GetInstalledPackage() will return node.js. To skip installing node.js  
            // if already installed, we need to return the node.js i.e., use display name for msi provider.
            if (package.Type.EqualsIgnoreCase(Constants.MediaType.MsiPackage))
            {
                if (!string.IsNullOrWhiteSpace(package.DisplayName))
                {
                    package.Name = package.DisplayName;
                }
            }

            if (YieldSoftwareIdentity(fastPath, package.Name, package.SemVer.ToString(), package.VersionScheme, summary, package.Source, searchKey, null, targetFilename) != null)
            {
                //this is a trusted source
                if (AddMetadata(fastPath, "FromTrustedSource", "true") == null)
                {
                    return !IsCanceled;
                }

                if (AddMetadata(fastPath, "Source", package.Source) == null)
                {
                    return !IsCanceled;
                }
                if (AddMetadata(fastPath, "Type", package.Type) == null)
                {
                    return !IsCanceled;
                }

                if (package.Dependencies != null)
                {
                    //iterate thru the dependencies and add them to the software identity.
                    foreach (var dependency in package.Dependencies)
                    {
                        // foreach (var pkg in dependency)
                        {
                            AddDependency("PSL", dependency.Name, dependency.Version, null, null);
                        }
                    }
                }
            }
            return !IsCanceled;
        }

        /// <summary>
        /// Unregister the package source
        /// </summary>
        /// <param name="id">package source id or name</param>
        internal void RemovePackageSource(string id)
        {
            Debug(Resources.Messages.DebugInfoCallMethod3, Constants.ProviderName, "RemovePackageSource", id);
            var config = Config;
            if (config == null)
            {
                return;
            }

            try
            {

                XElement configuration = config.ElementsNoNamespace("configuration").FirstOrDefault();
                if (configuration == null)
                {
                    return;
                }

                XElement packageSources = configuration.ElementsNoNamespace("packageSources").FirstOrDefault();
                if (packageSources == null)
                {
                    return;
                }

                var nodes = packageSources.Elements("add");
                if (nodes == null)
                {
                    return;
                }

                foreach (XElement node in nodes)
                {

                    if (node.Attribute("key") != null && String.Equals(node.Attribute("key").Value, id, StringComparison.OrdinalIgnoreCase))
                    {
                        // remove itself
                        node.Remove();
                        Config = config;
                        Verbose(Resources.Messages.RemovedPackageSource, id);
                        break;
                    }

                }

                if (_registeredPackageSources.ContainsKey(id))
                {
                    _registeredPackageSources.Remove(id);
                }

                if (_checkedUnregisteredPackageSources.ContainsKey(id))
                {
                    _checkedUnregisteredPackageSources.Remove(id);
                }


                //var source = config.SelectNodes("/configuration/packageSources/add").Cast<XmlNode>().FirstOrDefault(node => String.Equals(node.Attributes["key"].Value, id, StringComparison.CurrentCultureIgnoreCase));

                //if (source != null)
                //{
                //    source.ParentNode.RemoveChild(source);
                //    Config = config;
                //    Verbose(Resources.Messages.RemovedPackageSource, id);
                //}
            }
            catch (Exception ex)
            {
                ex.Dump(this);
            }
        }

        /// <summary>
        /// Register the package source
        /// </summary>
        /// <param name="name">package source name</param>
        /// <param name="location">package source location</param>
        /// <param name="isTrusted">is the source trusted</param>
        /// <param name="isValidated">need validate before storing the information to config file</param>
        internal void AddPackageSource(string name, string location, bool isTrusted, bool isValidated)
        {

            Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "AddPackageSource - name= {0}, location={1}", name, location));
            try
            {
                // here the source is already validated by the caller
                var config = Config;
                if (config == null)
                {
                    return;
                }

                XElement source = null;
                XElement packageSources = null;
                // Check whether there is an existing node with the same name
                var configuration = config.ElementsNoNamespace("configuration").FirstOrDefault();
                if (configuration != null)
                {
                    packageSources = configuration.ElementsNoNamespace("packageSources").FirstOrDefault();
                    if (packageSources != null)
                    {
                        source = packageSources.Elements("add").FirstOrDefault(node =>
                            node.Attribute("key") != null && String.Equals(node.Attribute("key").Value, name, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    // create configuration node if it does not exist
                    configuration = new XElement("configuration");
                    // add that to the config
                    config.Add(configuration);
                }

                // There is no existing node with the same name. So we have to create one.
                if (source == null)
                {
                    // if packagesources is null we have to create that too
                    if (packageSources == null)
                    {
                        // create packagesources node
                        packageSources = new XElement("packageSources");
                        // add that to the config
                        configuration.Add(packageSources);
                    }

                    // Create new source
                    source = new XElement("add");
                    // Add that to packagesource
                    packageSources.Add(source);
                }

                // Now set the source node properties
                source.SetAttributeValue("key", name);
                source.SetAttributeValue("value", location);
                if (isValidated)
                {
                    source.SetAttributeValue("validated", true.ToString());
                }
                if (isTrusted)
                {
                    source.SetAttributeValue("trusted", true.ToString());
                }

                // Write back to the config file
                Config = config;

                // Add or set the source node from the dictionary depends on whether it was there
                if (_registeredPackageSources.ContainsKey(name))
                {
                    var packageSource = _registeredPackageSources[name];
                    packageSource.Name = name;
                    packageSource.Location = location;
                    packageSource.Trusted = isTrusted;
                    packageSource.IsRegistered = true;
                    packageSource.IsValidated = isValidated;
                }
                else
                {
                    _registeredPackageSources.Add(name, new PackageSource
                    {
                        Request = this,
                        Name = name,
                        Location = location,
                        Trusted = isTrusted,
                        IsRegistered = true,
                        IsValidated = isValidated,
                    });
                }

            }
            catch (Exception ex)
            {
                ex.Dump(this);
            }
        }

        /// <summary>
        /// Return the package source object
        /// </summary>
        /// <param name="name">The package source name to search for</param>
        /// <returns>package source object</returns>
        internal PackageSource FindRegisteredSource(string name)
        {

            Debug(Resources.Messages.DebugInfoCallMethod3, Constants.ProviderName, "FindRegisteredSource", name);
            var srcs = RegisteredPackageSources;

            if (srcs == null)
            {
                return null;
            }
            if (srcs.ContainsKey(name))
            {
                return srcs[name];
            }

            var src = srcs.Values.FirstOrDefault(each => LocationCloseEnoughMatch(name, each.Location));
            return src;
        }

        private bool LocationCloseEnoughMatch(string givenLocation, string knownLocation)
        {
            if (givenLocation.Equals(knownLocation, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // make trailing slashes consistent
            if (givenLocation.TrimEnd('/').Equals(knownLocation.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // and trailing backslashes
            if (givenLocation.TrimEnd('\\').Equals(knownLocation.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private IDictionary<string, PackageSource> RegisteredPackageSources
        {
            get
            {
                if (_registeredPackageSources == null || !_registeredPackageSources.Any())
                {
                    _registeredPackageSources = new ConcurrentDictionary<string, PackageSource>(StringComparer.OrdinalIgnoreCase);

                    try
                    {
                        Debug(Resources.Messages.DebugInfoCallMethod3, Constants.ProviderName, "RegisteredPackageSources", ConfigurationFileLocation);

                        var config = Config;
                        if (config != null)
                        {
                            // get the configuration node
                            var configuration = config.ElementsNoNamespace("configuration").FirstOrDefault();
                            if (configuration != null)
                            {
                                // get the packageSources node
                                var packageSources = configuration.ElementsNoNamespace("packageSources").FirstOrDefault();

                                if (packageSources != null)
                                {
                                    _registeredPackageSources = packageSources.Elements("add")
                                        .Where(each => each.Attribute("key") != null && each.Attribute("value") != null)
                                        .ToDictionaryNicely(each => each.Attribute("key").Value, each =>
                                            new PackageSource
                                            {
                                                Request = this,
                                                Name = each.Attribute("key").Value,
                                                Location = each.Attribute("value").Value,
                                                Trusted = each.Attribute("trusted") != null && each.Attribute("trusted").Value.IsTrue(),
                                                IsRegistered = true,
                                                IsValidated = each.Attribute("validated") != null && each.Attribute("validated").Value.IsTrue(),
                                            }, StringComparer.OrdinalIgnoreCase);

                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        _registeredPackageSources = new ConcurrentDictionary<string, PackageSource>();
                    }
                }

                return _registeredPackageSources;
            }
        }

        /// <summary>
        /// Return all registered package sources
        /// </summary>
        internal IEnumerable<PackageSource> SelectedSources
        {
            get
            {
                if (IsCanceled)
                {
                    yield break;
                }

                //get sources from user's input
                var sourcesFromUser = (PackageSources ?? Enumerable.Empty<string>()).ToArray();
                //get sources from config file that registered earlier 
                var pkgSources = RegisteredPackageSources;

                Debug(Resources.Messages.RegisteredSources, pkgSources.Count, Constants.ProviderName);

                //If a user does not provide -source, we use the registered ones
                if (sourcesFromUser.Length == 0)
                {
                    // return them all.
                    foreach (var src in pkgSources.Values)
                    {
                        Debug(src.Name);
                        yield return src;
                    }
                    yield break;
                }
                var userSpecifiesArrayOfSources =  sourcesFromUser.Length > 1;

                // otherwise, return package sources that match the items given.
                foreach (var src in sourcesFromUser)
                {
                    // Check whether we've already processed this item before
                    if (_checkedUnregisteredPackageSources.ContainsKey(src))
                    {
                        _checkedUnregisteredPackageSources[src].Request = this;
                        yield return _checkedUnregisteredPackageSources[src];
                        continue;
                    }

                    // check to see if we have a source with either that name
                    // or that URI first.
                    if (pkgSources.ContainsKey(src))
                    {
                        Debug(Resources.Messages.FoundRegisteredSource, src, Constants.ProviderName);
                        _checkedUnregisteredPackageSources.Add(src, pkgSources[src]);
                        yield return pkgSources[src];
                        continue;
                    }

                    var srcLoc = src;
                    var found = false;
                    foreach (var byLoc in pkgSources.Values)
                    {
                        // srcLoc does not match byLoc.Location, try to check for srcLoc with "/" appended at the end
                        if (!string.Equals(byLoc.Location, srcLoc, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(byLoc.Location, string.Concat(srcLoc, "/"), StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(string.Concat(byLoc.Location, "/"), srcLoc, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        _checkedUnregisteredPackageSources.Add(srcLoc, byLoc);
                        yield return byLoc;
                        found = true;
                    }
                    if (found)
                    {
                        continue;
                    }

                    Debug(Resources.Messages.NotFoundRegisteredSource, src, Constants.ProviderName);

                    // is it a file path?
                    if (System.IO.Directory.Exists(src))
                    {
                        Debug(Resources.Messages.SourceIsADirectory, src);
                        PackageSource newSource = new PackageSource
                        {
                            Request = this,
                            Location = src,
                            Name = src,
                            Trusted = true,
                            IsRegistered = false,
                            IsValidated = true,
                        };
                        yield return newSource;
                    }
                    else if (File.Exists(src) && (Path.GetExtension(src).EqualsIgnoreCase(".json")) )
                    {                     
                        PackageSource newSource = new PackageSource
                        {
                            Request = this,
                            Location = src,
                            Name = src,
                            Trusted = true,
                            IsRegistered = false,
                            IsValidated = true,
                        };
                        yield return newSource;
                    }
                    else
                    {
                        // Not a valid location?
                        if (userSpecifiesArrayOfSources)
                        {
                            Verbose(Constants.Messages.UnableToResolveSource, Constants.ProviderName, src);
                        }
                        else
                        {
                            Warning(Constants.Messages.UnableToResolveSource, Constants.ProviderName, src);
                        }
                    }
                }
            }
        }

        private XDocument Config
        {
            get
            {
                if (_config == null)
                {
                    Debug(Resources.Messages.LoadingConfigurationFile, ConfigurationFileLocation);

                    XDocument doc = null;

                    try
                    {
                        using (FileStream fs = new FileStream(ConfigurationFileLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            // load the xdocument from the file stream
                            doc = XmlUtility.LoadSafe(fs, ignoreWhiteSpace: true);
                        }

                        Debug(Resources.Messages.LoadedConfigurationFile, ConfigurationFileLocation);

                        if (doc.Root != null && doc.Root.Name != null && String.Equals(doc.Root.Name.LocalName, "configuration", StringComparison.OrdinalIgnoreCase))
                        {
                            _config = doc;
                            return _config;
                        }
                        Warning(Resources.Messages.MissingConfigurationElement, ConfigurationFileLocation);
                    }
                    catch (Exception e)
                    {
                        // a bad xml doc or a folder gets deleted somehow
                        Warning(e.Message);
                    }
                }

                return _config;
            }
            set
            {

                if (value == null)
                {
                    Debug(Resources.Messages.SettingConfigurationToNull);
                    return;
                }

                _config = value;

                Verbose(Resources.Messages.SavingConfigurationWithFile, ConfigurationFileLocation);
                var stringBuilder = new System.Text.StringBuilder();

                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    NewLineOnAttributes = true,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates
                };

                using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
                {
                    _config.Save(xmlWriter);
                    System.IO.File.WriteAllText(ConfigurationFileLocation, _config.ToString());
                }
            }
        }

        private string ConfigurationFileLocation
        {
            get
            {
            if (string.IsNullOrWhiteSpace(_configurationFileLocation))
            {      
                    var appdataFolder = Environment.GetEnvironmentVariable("appdata");
                    _configurationFileLocation = Path.Combine(appdataFolder, Constants.ProviderName, Constants.SettingsFileName);

                    //create directory if does not exist
                    string dir = Path.GetDirectoryName(_configurationFileLocation);
                    if (dir != null && !System.IO.Directory.Exists(dir))
                    {
                        Debug(Resources.Messages.CreateDirectory, dir);
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    //create place holder config file
                    if (!System.IO.File.Exists(_configurationFileLocation))
                    {
                        Debug(Resources.Messages.CreateFile, _configurationFileLocation);
                        bool addDefaultConfig = false;
                        if (System.IO.File.Exists(DefaultJSONFileLocation))
                        {
                            addDefaultConfig = true;
                        }
                        else
                        {
                            bool force = this.GetOptionValue("Force") != null;
                            if (force || this.ShouldContinue(Resources.Messages.QueryDownloadPackageSourceList.format(DefaultJSONSourceLocation), Resources.Messages.PackageSourceListNotFound.format(DefaultJSONFileLocation)))
                            {
                                WebDownloader.DownloadFile(DefaultJSONSourceLocation, DefaultJSONFileLocation, this, null);
                                WebDownloader.DownloadFile(DefaultJSONCatalogFileLocation, DefaultCatalogFileLocation, this, null);
                                if (System.IO.File.Exists(DefaultJSONFileLocation) && System.IO.File.Exists(DefaultCatalogFileLocation) && 
                                    PackageSourceListProvider.TestCatalogFile(DefaultJSONFileLocation, DefaultCatalogFileLocation, this))
                                {
                                   addDefaultConfig = true;
                                }                            
                            }
                        }

                        if(addDefaultConfig)
                            System.IO.File.WriteAllText(_configurationFileLocation, DefaultConfig);
                        else
                            System.IO.File.WriteAllText(_configurationFileLocation, EmptyConfig);
                    }
                }
                return _configurationFileLocation;
            }
        }

        internal bool WriteError(Internal.ErrorCategory category, string targetObjectValue, string messageText, params object[] args)
        {
            return Error(messageText, category.ToString(), targetObjectValue, base.FormatMessageString(messageText, args));
        }

        /// <summary>
        /// Check if the package source location is valid
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        internal bool ValidateSourceLocation(string location)
        {
            Debug(Resources.Messages.DebugInfoCallMethod3, _PackageSourceListRequest, "ValidateSourceLocation", location);
            if (File.Exists(location) && (Path.GetExtension(location).EqualsIgnoreCase(".json")))
            {
                return true;
            }            
            return false;
        }

        internal static IRequest ExtendRequest(Dictionary<string, string[]> options, string[] sources, bool isTrusted, PackageSourceListRequest request) {
            var srcs = sources ?? new List<string>().ToArray();
            var opts = options ?? new Dictionary<string, string[]>();

            return (new object[]
            {
                new {
                    GetOptionKeys = new Func<IEnumerable<string>>(() => opts.Keys.Any() ? request.OptionKeys.Concat(opts.Keys) : request.OptionKeys),
                    GetOptionValues = new Func<string, IEnumerable<string>>((key) =>
                    {
                        if (opts.ContainsKey(key))
                        {
                            return opts[key];
                        }
                        return request.GetOptionValues(key);
                    }),

                    GetSources = new Func<IEnumerable<string>>(() => srcs),
                    //ShouldContinueWithUntrustedPackageSource = new Func<string, string, bool>((pkgName, pkgSource) => isTrusted),
                },
                request
            }).As<IRequest>(); 
        }

        /// <summary>
        /// HttpClient with Accept-CharSet and Accept-Encoding Header
        /// We want to reuse HttpClient
        /// </summary>
        internal HttpClient Client
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = PathUtility.GetHttpClientHelper(CredentialUsername, CredentialPassword, WebProxy);

                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "UTF-8");
                    // Request for gzip and deflate encoding to make the response lighter.
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip,deflate");

                    // needed for downloading package like node.js.msi
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text / html, application / xhtml + xml, image / jxr, */*");

                    foreach (var header in Headers.Value)
                    {
                        // header is in the format "A=B" because OneGet doesn't support Dictionary parameters
                        if (!string.IsNullOrEmpty(header))
                        {
                            var headerSplit = header.Split(new string[] { "=" }, 2, StringSplitOptions.RemoveEmptyEntries);

                            // ignore wrong entries
                            if (headerSplit.Count() == 2)
                            {
                                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(headerSplit[0], headerSplit[1]);
                            }
                            else
                            {
                                Warning(Resources.Messages.HeaderIgnored, header);
                            }
                        }
                    }
                }

                return _httpClient;
            }
        }
    }
}

#endif