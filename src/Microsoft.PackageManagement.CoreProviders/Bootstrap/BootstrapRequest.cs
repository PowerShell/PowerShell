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
    using System.Collections;
    using System.Globalization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Security.Cryptography;
    using System.Management.Automation;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Packaging;
    using PackageManagement.Internal.Utility.Platform;
    using PackageManagement.Internal.Utility.Collections;
    using PackageManagement.Internal.Utility.Extensions;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using System.IO.Compression;
    using File = System.IO.File;
    using Directory = System.IO.Directory;

    public abstract class BootstrapRequest : Request {
        internal Uri[] _urls
        {
            get
            {
                string testfeed = Environment.GetEnvironmentVariable("BootstrapProviderTestfeedUrl");

                // if testfeed exists, use that
                if (!string.IsNullOrWhiteSpace(testfeed))
                {
                    Uri result = null;

                    // test whether the uri is valid
                    if (Uri.TryCreate(testfeed, UriKind.Absolute, out result))
                    {
                        return new Uri[] {
                            result
                        };
                    }
                }

                return new Uri[] {
    #if LOCAL_DEBUG
                new Uri("https://localhost:81/providers.swidtag"),
    #endif
    #if CORECLR
                new Uri("https://go.microsoft.com/fwlink/?LinkID=627340&clcid=0x409"),
                // starting in 2015/05 builds, we bootstrap from here:
    #else
                new Uri("https://go.microsoft.com/fwlink/?LinkID=627338&clcid=0x409"),
    #endif      
                };
            }
        }

        private IEnumerable<Feed> _feeds;
        private IEnumerable<string> _fileSource = null;

        private IEnumerable<Feed> Feeds {
            get {
                if (_feeds == null) {
                    if (LocalSource.Any()) {
                        Verbose(Resources.Messages.UseLocalSource, LocalSource.FirstOrDefault());                        
                        return Enumerable.Empty<Feed>();
                    }

#if !PORTABLE
                    // we don't do bootstrap on core powershell
                    if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) {
                        Warning(Constants.Messages.NetworkNotAvailable);
                        Warning(string.Format(CultureInfo.CurrentCulture, Resources.Messages.ProviderBootstrapFailed));
                    }

                    // right now, we only have one feed (can have many urls tho')
                    // so we just return a single feed in the collection
                    // but later, we can expand it to support multiple feeds.
                    var feed = new Feed(this, _urls);
                    if (feed.IsValid) {
                        _feeds = feed.SingleItemAsEnumerable().ReEnumerable();
                    } else {
                        Warning(Constants.Messages.ProviderSwidtagUnavailable);
                        return Enumerable.Empty<Feed>();
                    }
#endif
                }
                return _feeds;
            }
        }

        internal IEnumerable<string> LocalSource {
            get
            {
                if (_fileSource == null)
                {
                    if (Sources.IsNullOrEmpty())
                    {
                        _fileSource = Enumerable.Empty<string>();                       
                    } else {
                        _fileSource = Sources.Where(each => !string.IsNullOrWhiteSpace(each) && (System.IO.File.Exists(each) || System.IO.Directory.Exists(each))).WhereNotNull();
                    }
                }
                return _fileSource;
            }
        }

        internal string DestinationPath(Request request) {

            var pms = PackageManagementService as PackageManagementService;

            var scope = GetValue("Scope");
            if (!string.IsNullOrWhiteSpace(scope)) {
                if (scope.EqualsIgnoreCase("CurrentUser")) {
                    return pms.UserAssemblyLocation;
                }
                if (AdminPrivilege.IsElevated) {
                    return pms.SystemAssemblyLocation;
                } else {
                    //a user specifies 'AllUsers' that requires Admin privilege. However his console gets launched by non-elevated.
                    Error(ErrorCategory.InvalidOperation, ErrorCategory.InvalidOperation.ToString(),
                        PackageManagement.Resources.Messages.InstallRequiresCurrentUserScopeParameterForNonAdminUser, pms.SystemAssemblyLocation, pms.UserAssemblyLocation);
                    return null;
                }
            }

            var v = GetValue("DestinationPath");
            if (String.IsNullOrWhiteSpace(v)) {
                // use a well-known path.
                v = AdminPrivilege.IsElevated ? pms.SystemAssemblyLocation : pms.UserAssemblyLocation;
                if (String.IsNullOrWhiteSpace(v)) {
                    return null;
                }
            }
            return Path.GetFullPath(v);
        }

        internal IEnumerable<Package> Providers {
            get {
                return Feeds.SelectMany(feed => feed.Query());
            }
        }

        private string GetValue(string name) {
            // get the value from the request
            return (GetOptionValues(name) ?? Enumerable.Empty<string>()).LastOrDefault();
        }

        internal Package GetProvider(Uri uri) {
            return new Package(this, uri.SingleItemAsEnumerable());
        }

        internal Package GetProvider(string name) {
            return Feeds.SelectMany(feed => feed.Query(name)).FirstOrDefault();
        }

        internal Package GetProvider(string name, string version) {
            return Feeds.SelectMany(feed => feed.Query(name, version)).FirstOrDefault();
        }

        internal IEnumerable<Package> GetProviderAll(string name, string minimumversion, string maximumversion) {
            return Feeds.SelectMany(feed => feed.Query(name, minimumversion, maximumversion));
        }

        internal IEnumerable<Package> GetProvider(string name, string minimumversion, string maximumversion) {
            return new[] {
                GetProviderAll(name, minimumversion, maximumversion)
                    .OrderByDescending(each => SoftwareIdentityVersionComparer.Instance).FirstOrDefault()
            };
        }

        internal string DownloadAndValidateFile(Swidtag swidtag) {
            var file = DownLoadFileFromLinks(swidtag.Links.Where(each => each.Relationship == Iso19770_2.Relationship.InstallationMedia));
            if (string.IsNullOrWhiteSpace(file)) {
                return null;
            }

            var payload = swidtag.Payload;
            if (payload == null) {
                //We let the providers that are already posted in the public continue to be installed.
                return file;
            } else {
                //validate the file hash
                var valid = ValidateFileHash(file, payload);
                if (!valid) {
                    //if the hash does not match, delete the file in the temp folder
                    file.TryHardToDelete();
                    return null;
                }
                return file;
            }
        }

        /// <summary>
        /// Extract zipped package and return the unzipped folder
        /// </summary>
        /// <param name="zippedPackagePath"></param>
        /// <returns></returns>
        private string ExtractZipPackage(string zippedPackagePath)
        {
            if (zippedPackagePath != null && zippedPackagePath.FileExists())
            {
                // extracted folder
                string extractedFolder = FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory();

                try
                {
                    //unzip the file
                    ZipFile.ExtractToDirectory(zippedPackagePath, extractedFolder);

                    // extraction fails
                    if (!Directory.Exists(extractedFolder))
                    {
                        Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.FailToExtract, zippedPackagePath, extractedFolder));
                        return string.Empty;
                    }

                    // the zipped folder
                    var zippedDirectory = Directory.EnumerateDirectories(extractedFolder).FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(zippedDirectory) && Directory.Exists(zippedDirectory))
                    {
                        return zippedDirectory;
                    }
                }
                catch (Exception ex)
                {
                    Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.FailToInstallZipFolder, zippedPackagePath, ex.Message));
                    Debug(ex.StackTrace);

                    // remove the extracted folder
                    extractedFolder.TryHardToDelete();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Helper function to retry downloading a file.
        /// downloadFileFunction is the main function that is used to download the file when given a uri
        /// numberOfTry is how many times we can try to download it
        /// </summary>
        /// <param name="downloadFileFunction"></param>
        /// <param name="location"></param>
        /// <param name="numberOfTry"></param>
        /// <returns></returns>
        internal string RetryDownload(Func<Uri, string> downloadFileFunction, Uri location, uint numberOfTry = 3) {
            string file = null;

            // if scheme is not https, write warning and ignores this link
            if (!string.Equals(location.Scheme, "https")) {
                Warning(string.Format(CultureInfo.CurrentCulture, Resources.Messages.OnlyHttpsSchemeSupported, location.AbsoluteUri));
                return file;
            }

            // try 3 times to see whether we can download this
            int remainingTry = 3;

            // try to download the file for remainingTry times
            while (remainingTry > 0) {
                try {
                    file = downloadFileFunction(location);
                } finally {
                    if (file == null || !file.FileExists()) {
                        // file cannot be download
                        file = null;
                        remainingTry -= 1;
                        Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.RetryDownload, location.AbsoluteUri, remainingTry));
                    } else {
                        // file downloaded, no need to retry.
                        remainingTry = 0;
                    }
                }
           } 

            return file;
        }

        
        
        internal string DownLoadFileFromLinks(IEnumerable<Link> links) {
            string file = null;

            foreach (var link in links) {
                file = RetryDownload(
                    // the download function takes in a uri link and download it
                    (uri) => {
                        var tmpFile = FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory();
                        return ProviderServices.DownloadFile(uri, tmpFile, -1, true, this);
                    },
                    link.HRef);

                // got a valid file!
                if (file != null && file.FileExists()) {
                    // if file is zip, unpack it and return the unpacked folder
                    if (link.MediaType == Iso19770_2.MediaType.ZipPackage)
                    {
                        try
                        {
                            // let's extract the zipped file
                            return ExtractZipPackage(file);
                        }
                        finally
                        {
                            // delete the zipped file
                            file.TryHardToDelete();
                        }
                    }

                    return file;
                }
            }

            return file;
        }

        private bool ValidateFileHash(string fileFullPath, Payload payload) {

            Debug("BootstrapRequest::ValidateFileHash");
            /* format: 
             * <Payload>
             *   <File name="nuget-anycpu-2.8.5.205.exe"  sha512:hash="a314fc2dc663ae7a6b6bc6787594057396e6b3f569cd50fd5ddb4d1bbafd2b6a" />
             * </Payload>
             */

            if (payload == null || fileFullPath == null || !fileFullPath.FileExists()) {
                return false;
            }

            try {
                if ((payload.Files == null) || !payload.Files.Any()) {
                    Error(ErrorCategory.InvalidData, "Payload", Constants.Messages.MissingFileTag);
                    return false;
                }
                var fileTag = payload.Files.FirstOrDefault();

                if ((fileTag.Attributes == null) || (fileTag.Attributes.Keys == null)) {
                    Error(ErrorCategory.InvalidData, "Payload", Constants.Messages.MissingHashAttribute);
                    return false;
                }

                var hashtag = fileTag.Attributes.Keys.FirstOrDefault(each => each.LocalName.Equals("hash"));
                if (hashtag == null) {
                    Error(ErrorCategory.InvalidData, "Payload", Constants.Messages.MissingHashAttribute);
                    return false;
                }

                //Note we cannot use switch here because these xname like Iso19770_2.Hash.Hash512, is not compiler time constant
                string packageHash = null;
                HashAlgorithm hashAlgorithm = null;

                if (hashtag.Equals(Iso19770_2.Hash.Hash512)) {
                    hashAlgorithm = SHA512.Create();
                    packageHash = fileTag.GetAttribute(Iso19770_2.Hash.Hash512);
                } else if (hashtag.Equals(Iso19770_2.Hash.Hash256)) {
                    hashAlgorithm = SHA256.Create();
                    packageHash = fileTag.GetAttribute(Iso19770_2.Hash.Hash256);
                } else if (hashtag.Equals(Iso19770_2.Hash.Md5)) {
                    hashAlgorithm = MD5.Create();
                    packageHash = fileTag.GetAttribute(Iso19770_2.Hash.Md5);
                } else {
                    //hash algorithm not supported, we support 512, 256, md5 only 
                    Error(ErrorCategory.InvalidData, "Payload", Constants.Messages.UnsupportedHashAlgorithm, hashtag,
                        new[] {Iso19770_2.HashAlgorithm.Sha512, Iso19770_2.HashAlgorithm.Sha256, Iso19770_2.HashAlgorithm.Md5}.JoinWithComma());
                    return false;
                }

                if (string.IsNullOrWhiteSpace(packageHash) || hashAlgorithm == null) {
                    //missing hash content?
                    Error(ErrorCategory.InvalidData, "Payload", Constants.Messages.MissingHashContent);
                    return false;
                }

                // Verify the hash
                using (FileStream stream = System.IO.File.OpenRead(fileFullPath)) {
                    // compute the hash from the file
                    byte[] computedHash = hashAlgorithm.ComputeHash(stream);

                    try {
                        // convert the original hash we got from the payload tag
                        byte[] expectedHash = Convert.FromBase64String(packageHash);
                        //check if hash is equal
                        if (!computedHash.SequenceEqual(expectedHash)) {
                            // the file downloaded is not the same as expected. The file is modified.
                            Error(ErrorCategory.SecurityError, "Payload", Constants.Messages.HashNotEqual, packageHash, Convert.ToBase64String(computedHash));
                            return false;
                        }

                        return true;

                    } catch (FormatException ex) {
                        Warning(ex.Message);
                        Error(ErrorCategory.SecurityError, "Payload", Constants.Messages.InvalidHashFormat, packageHash);
                    }
                }
            } catch (Exception ex) {
                Warning(ex.Message);
            }
            return false;
        }

        internal bool YieldFromSwidtag(Package provider, string requiredVersion, string minimumVersion, string maximumVersion, string searchKey) {
            if (provider == null) {
                // if the provider isn't there, just return.
                return !IsCanceled;
            }

            if (AnyNullOrEmpty(provider.Name, provider.Version, provider.VersionScheme)) {
                Debug("Skipping yield on swid due to missing field \r\n", provider.ToString());
                return !IsCanceled;
            }

            if (!String.IsNullOrWhiteSpace(requiredVersion)) {
                if (provider.Version != requiredVersion) {
                    return !IsCanceled;
                }
            } else {
                if (!String.IsNullOrWhiteSpace(minimumVersion) && SoftwareIdentityVersionComparer.CompareVersions(provider.VersionScheme, provider.Version, minimumVersion) < 0) {
                    return !IsCanceled;
                }

                if (!String.IsNullOrWhiteSpace(maximumVersion) && SoftwareIdentityVersionComparer.CompareVersions(provider.VersionScheme, provider.Version, maximumVersion) > 0) {
                    return !IsCanceled;
                }
            }
            return YieldFromSwidtag(provider, searchKey);
        }


        internal bool YieldFromSwidtag(Package pkg, string searchKey) {
            if (pkg == null) {
                return !IsCanceled;
            }

            var provider = pkg._swidtag;
            var fastPackageReference = LocalSource.Any() ? pkg.Location.LocalPath : pkg.Location.AbsoluteUri;
            var source = pkg.Source ?? fastPackageReference;

            var summary = pkg.Name;
            var targetFileName = pkg.Name;

            if (!LocalSource.Any()) {
                summary = new MetadataIndexer(provider)[Iso19770_2.Attributes.Summary.LocalName].FirstOrDefault();
                targetFileName = provider.Links.Select(each => each.Attributes[Iso19770_2.Discovery.TargetFilename]).WhereNotNull().FirstOrDefault();
            }

            if (YieldSoftwareIdentity(fastPackageReference, provider.Name, provider.Version, provider.VersionScheme, summary, source, searchKey, null, targetFileName) != null) {
                // yield all the meta/attributes
                if (provider.Meta.Any(
                    m => {
                        var element = AddMeta(fastPackageReference);
                        var attributes = m.Attributes;
                        return attributes.Keys.Any(key => {
                            var nspace = key.Namespace.ToString();
                            if (String.IsNullOrWhiteSpace(nspace)) {
                                return AddMetadata(element, key.LocalName, attributes[key]) == null;
                            }

                            return AddMetadata(element, new Uri(nspace), key.LocalName, attributes[key]) == null;
                        });
                    })) {
                    return !IsCanceled;
                }

                if (provider.Links.Any(link => AddLink(link.HRef, link.Relationship, link.MediaType, link.Ownership, link.Use, link.Media, link.Artifact) == null)) {
                    return !IsCanceled;
                }

                if (provider.Entities.Any(entity => AddEntity(entity.Name, entity.RegId, entity.Role, entity.Thumbprint) == null)) {
                    return !IsCanceled;
                }

                //installing a package from bootstrap site needs to prompt a user. Only auto-bootstrap is not prompted.
                var pm = PackageManagementService as PackageManagementService;
                string isTrustedSource = pm.InternalPackageManagementInstallOnly ? "false" : "true";
                if (AddMetadata(fastPackageReference, "FromTrustedSource", isTrustedSource) == null) {
                    return !IsCanceled;
                }
            }
            return !IsCanceled;
        }

        private static bool AnyNullOrEmpty(params string[] args) {
            return args.Any(String.IsNullOrWhiteSpace);
        }

        /// <summary>
        /// Get a package provider from a given path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="suppressErrorsAndWarnings"></param>
        /// <param name="copyFileToTemp"></param>
        /// <returns></returns>
        internal Package GetProviderFromFile(string filePath, bool copyFileToTemp = false, bool suppressErrorsAndWarnings = false) {
            
#if PORTABLE
            // not supported on core powershell
            return null;
#else
            if (string.IsNullOrWhiteSpace(filePath) && !System.IO.File.Exists(filePath)) {
                Warning(Constants.Messages.FileNotFound, filePath);              
                return null;
            }

            // support providers with .dll file extension only
            if (!Path.GetExtension(filePath).EqualsIgnoreCase(".dll")) {
                if (!suppressErrorsAndWarnings)
                {
                    Warning(Resources.Messages.InvalidFileType, ".dll", filePath);
                }
                return null;
            }

            string tempFile = filePath;
            IEnumerable<XElement> manifests = Enumerable.Empty<XElement>();
            if (copyFileToTemp) {
                try {
                    // Manifest.LoadFrom() does not work with network share, so we need to copy the dll to temp location
                    tempFile = CopyToTempLocation(filePath);
                    if (string.IsNullOrWhiteSpace(tempFile) && !System.IO.File.Exists(tempFile))
                    {
                        Warning(Constants.Messages.FileNotFound, tempFile);
                        return null;
                    }

                    manifests = Manifest.LoadFrom(tempFile).ToArray();
                }
                finally {
                    if (!string.IsNullOrWhiteSpace(tempFile)) {
                        tempFile.TryHardToDelete();
                    }
                }
            } else {
                // providers have the provider manifest embeded?
                manifests = Manifest.LoadFrom(filePath).ToArray();
            }

            if (!manifests.Any()) {
                if (!suppressErrorsAndWarnings)
                {
                    Warning(Resources.Messages.MissingProviderManifest, tempFile);
                }
                return null;
            }

            var source = new Uri(filePath);
            foreach (var manifest in manifests) {
                var swidTagObject = new Swidtag(manifest);

                if (Swidtag.IsSwidtag(manifest) && swidTagObject.IsApplicable(new Hashtable())) {
                    
                    return new Package(this, swidTagObject) {
                        Location = source,
                        Source = source.LocalPath
                    };
                }
            }

            return null;
#endif
        }

        /// <summary>
        /// Find a package provider from a file path.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="requiredVersion"></param>
        /// <param name="minimumVersion"></param>
        /// <param name="maximumVersion"></param>
        internal void FindProviderFromFile(string name, string requiredVersion, string minimumVersion, string maximumVersion) {

            // find the providers from the given Source location
            var pkgs = FindProviderByNameFromFile(name).Where(each => FilterOnName(each, name) && FilterOnVersion(each, requiredVersion, minimumVersion, maximumVersion)).ReEnumerable();

            Debug("Total {0}  providers found".format(pkgs.Count()));

            //A user does not provide version info, we choose the latest
            if (!GetOptionValue("AllVersions").IsTrue() && (string.IsNullOrWhiteSpace(requiredVersion) && string.IsNullOrWhiteSpace(minimumVersion) && string.IsNullOrWhiteSpace(maximumVersion)))
            {
                pkgs = pkgs.GroupBy(p => p.Name).Select(each => each.OrderByDescending(pp => pp.Version).FirstOrDefault()).ReEnumerable();
            }
        
            foreach (var package in pkgs)
            {
                YieldFromSwidtag(package, name);
            }
        }

        private IEnumerable<Package> FindProviderByNameFromFile(string name) {
            foreach (var each in LocalSource) {
                // each can be file full path or folder directory
                var assemblies = System.IO.File.Exists(each) ? new[] {each} : System.IO.Directory.EnumerateFiles(each, "*.dll", SearchOption.AllDirectories);

                foreach (var item in assemblies) {
                    yield return GetProviderFromFile(item, true, false);               
                }
            }
        }

        private bool FilterOnName(Package pkg, string name)
        {
            if (pkg == null)
            {
                return false;
            }

            if(string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            if (WildcardPattern.ContainsWildcardCharacters(name))            
            {
                // Applying the wildcard pattern matching
                const WildcardOptions wildcardOptions = WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase;
                var wildcardPattern = new WildcardPattern(name, wildcardOptions);

                return wildcardPattern.IsMatch(pkg.Name);

            }
            else
            {
                return pkg.Name.EqualsIgnoreCase(name);
            }
        }

        private bool FilterOnVersion(Package pkg, string requiredVersion, string minimumVersion, string maximumVersion) {

            if(pkg == null) {
                return false;
            }

            if (string.IsNullOrWhiteSpace(requiredVersion) || (SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, requiredVersion) == 0))
            {
                if (string.IsNullOrWhiteSpace(minimumVersion) || (SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, minimumVersion) >= 0))
                {
                    if (string.IsNullOrWhiteSpace(maximumVersion) || (SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, maximumVersion) <= 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetTempFileFullPath(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                return filePath;
            }
            // get a temp location
            var file = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));

            if (System.IO.File.Exists(file)) {
                //if exists already, delete it
                file.TryHardToDelete();
            }

            // is that file still there?
            if (System.IO.File.Exists(file)) {
                //try it again if the generated file already exists
                file = GetTempFileFullPath(filePath);
            }

            return file;
        }

        private string CopyToTempLocation(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                return filePath;
            }

            var targetFile = GetTempFileFullPath(filePath);

            if (filePath.EqualsIgnoreCase(targetFile)) {
                return filePath;
            }

            Debug("Copying file '{0}' to '{1}'", filePath, targetFile);
            try {
                System.IO.File.Copy(filePath, targetFile);
                return targetFile;
            } catch (Exception ex) {
                Debug(ex.StackTrace);
                return string.Empty;
            }
        }
    }
}
