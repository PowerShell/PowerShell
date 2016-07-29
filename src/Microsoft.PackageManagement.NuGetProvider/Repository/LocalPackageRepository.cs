namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.PackageManagement.Provider.Utility;

    /// <summary>
    /// Package repository for downloading data from file repositories
    /// </summary>
    internal class LocalPackageRepository : IPackageRepository {
        private readonly string _path;

        /// <summary>
        /// Ctor's
        /// </summary>
        /// <param name="request"></param>
        /// <param name="physicalPath"></param>
        public LocalPackageRepository(string physicalPath, NuGetRequest request) {
            _path = physicalPath;
        }

        /// <summary>
        /// Package source location
        /// </summary>
        public string Source {
            get {
                return _path;
            }
        }

        /// <summary>
        /// Finding the packages in the file repository
        /// </summary>
        /// <param name="openPackage">Delegate function which is actually finding a package</param>
        /// <param name="packageId">Package Id</param>
        /// <param name="packagePaths">File repository path</param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static IEnumerable<IPackage> GetPackages(Func<string, Request, IPackage> openPackage, 
            string packageId, 
            IEnumerable<string> packagePaths, 
            Request request) 
        {
            request.Debug(Resources.Messages.DebugInfoCallMethod3, "LocalPackageRepository", "GetPackages", packageId);

            foreach (var path in packagePaths) 
            {
                IPackage package = null;
                try {
                    package = GetPackage(openPackage, path, request);
                } catch (InvalidOperationException ex) {
                    // ignore error for unzipped packages (nuspec files).
                    if (!string.Equals(NuGetConstant.ManifestExtension, Path.GetExtension(path), StringComparison.OrdinalIgnoreCase)) {
                        request.Verbose(ex.Message);
                        throw;
                    }
                }

                if (package != null && package.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)) {
                    yield return package;
                }
            }
        }

        /// <summary>
        /// Finding the package in the file repository
        /// </summary>
        /// <param name="openPackage">Delegate function which is actually finding a package</param>
        /// <param name="path">File repository path</param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static IPackage GetPackage(Func<string, Request, IPackage> openPackage, string path, Request request)
        {           
            //Create the package
            IPackage package = openPackage(path, request);

            return package;
        }

        /// <summary>
        /// A delegate used for finding the package.
        /// </summary>
        /// <param name="path">File repository path</param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static IPackage OpenPackage(string path, Request request) {

            request.Debug(Resources.Messages.DebugInfoCallMethod3, "LocalPackageRepository", "OpenPackage", path);

            if (!File.Exists(path)) {
                request.Warning(Resources.Messages.FileNotFound, path,"LocalPackageRepository::OpenPackage");
                return null;
            }

            //deal with .nupkg
            if (string.Equals(Path.GetExtension(path), NuGetConstant.PackageExtension, StringComparison.OrdinalIgnoreCase)) {
                PackageBase package;
                try {
                    package = ProcessZipPackage(path);
                } catch (Exception ex) {
                    request.Verbose(ex.Message);
                    throw;
                }

                // Set the last modified date on the package
                package.Published = FileUtility.GetLastModified(path);

                // We assume local files in the local repository are all latest version
                package.IsAbsoluteLatestVersion = true;
                package.IsLatestVersion = true;
                package.FullFilePath = path;

                return package;
            }

            return null;
        }

        /// <summary>
        /// Unzip the package and create PackageImpl object
        /// </summary>
        /// <param name="nupkgPath">The .nupkg file path</param>
        /// <returns></returns>
        private static PackageBase ProcessZipPackage(string nupkgPath) {

            string packageName = Path.GetFileNameWithoutExtension(nupkgPath);
            packageName = PackageUtility.GetPackageNameWithoutVersionInfo(packageName);
            PackageBase package = PackageUtility.DecompressFile(nupkgPath, packageName);

            return package;
        }

        /// <summary>
        /// Get the .nupkg files
        /// </summary>
        /// <param name="filter">The file filter to be applied while finding the .nupkg files</param>
        /// <returns></returns>
        private IEnumerable<string> GetPackageFiles(string filter = null) {
            filter = filter ?? "*" + NuGetConstant.PackageExtension;

            // Check for package files one level deep. We use this at package install time
            // to determine the set of installed packages. Installed packages are copied to 
            // {id}.{version}\{packagefile}.{extension}.
            foreach (var dir in FileUtility.GetDirectories(_path))
                //foreach (var dir in FileSystem.GetDirectories(String.Empty))
            {
                foreach (var path in FileUtility.GetFiles(dir, filter, recursive: false)) {
                    yield return path;
                }
            }

            // Check top level directory
            foreach (var path in FileUtility.GetFiles(_path, filter, recursive: false)) {
                yield return path;
            }
        }

        /// <summary>
        /// Find-Package
        /// </summary>
        /// <param name="packageId">Package id</param>
        /// <param name="version">Package Name</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual IPackage FindPackage(string packageId, SemanticVersion version, NuGetRequest request)
        {
            return FindPackage(OpenPackage, packageId, version, request);
        }

        /// <summary>
        /// Find-Package
        /// </summary>
        /// <param name="openPackage">Delegate function which is actually finding a package</param>
        /// <param name="packageId">Package Id</param>
        /// <param name="version">Package version</param>
        /// <param name="nugetRequest"></param>
        /// <returns></returns>
        private IPackage FindPackage(Func<string, Request, IPackage> openPackage, string packageId, SemanticVersion version, NuGetRequest nugetRequest)
        {
            if (nugetRequest == null) {
                return null;
            }

            nugetRequest.Debug(Resources.Messages.SearchingRepository, "FindPackage", packageId);

            var lookupPackageName = new PackageName(packageId, version);

            //handle file cache here if we want to support local package cache later

            // Lookup files which start with the name "<Id>." and attempt to match it with all possible version string combinations (e.g. 1.2.0, 1.2.0.0) 
            // before opening the package. To avoid creating file name strings, we attempt to specifically match everything after the last path separator
            // which would be the file name and extension.
            return (from path in GetPackageLookupPaths(packageId, version)
                    let package = GetPackage(openPackage, path, nugetRequest)
                where lookupPackageName.Equals(new PackageName(package.Id, package.Version))
                select package).FirstOrDefault();
        }

        /// <summary>
        /// Find the package files (.nupkg or .nuspec). 
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private IEnumerable<string> GetPackageLookupPaths(string packageId, SemanticVersion version) {
            // Files created by the path resolver. This would take into account the non-side-by-side scenario 
            // and we do not need to match this for id and version.
            //var packageFileName = packageId + version + Constant.PackageExtension;

            var packageFileName = FileUtility.MakePackageFileName(false, packageId, version.ToString(), NuGetConstant.PackageExtension);

            // var packageFileName = PathResolver.GetPackageFileName(packageId, version);
            var manifestFileName = Path.ChangeExtension(packageFileName, NuGetConstant.ManifestExtension);

            var filesMatchingFullName = GetPackageFiles(packageFileName).Concat(GetPackageFiles(manifestFileName));

            if (version.Version.Revision < 1) {
                // If the build or revision number is not set, we need to look for combinations of the format
                // * Foo.1.2.nupkg
                // * Foo.1.2.3.nupkg
                // * Foo.1.2.0.nupkg
                // * Foo.1.2.0.0.nupkg
                // To achieve this, we would look for files named 1.2*.nupkg if both build and revision are 0 and
                // 1.2.3*.nupkg if only the revision is set to 0.
                string partialName = version.Version.Build < 1 ?
                    String.Join(".", packageId, version.Version.Major, version.Version.Minor) :
                    String.Join(".", packageId, version.Version.Major, version.Version.Minor, version.Version.Build);

                string partialManifestName = partialName + "*" + NuGetConstant.ManifestExtension;

                partialName += "*" + NuGetConstant.PackageExtension;

                // Partial names would result is gathering package with matching major and minor but different build and revision. 
                // Attempt to match the version in the path to the version we're interested in.
                var partialNameMatches = GetPackageFiles(partialName).Where(path => FileNameMatchesPattern(packageId, version, path));

                var partialManifestNameMatches = GetPackageFiles(partialManifestName).Where(
                    path => FileNameMatchesPattern(packageId, version, path));

                filesMatchingFullName = filesMatchingFullName.Concat(partialNameMatches).Concat(partialManifestNameMatches);
            }

            // cannot find matching files, we should try to search for just packageid.nupkg
            if (filesMatchingFullName.Count() == 0)
            {
                // exclude version
                var packageWithoutVersionName = FileUtility.MakePackageFileName(true, packageId, null, NuGetConstant.PackageExtension);
                var packageWithoutVersionManifest = Path.ChangeExtension(packageWithoutVersionName, NuGetConstant.ManifestExtension);

                return GetPackageFiles(packageWithoutVersionName).Concat(GetPackageFiles(packageWithoutVersionManifest));
            }

            return filesMatchingFullName;
        }

        /// <summary>
        /// True if the the file contains the right id and version.
        /// </summary>
        /// <param name="packageId">package id</param>
        /// <param name="version">package version</param>
        /// <param name="path">File path</param>
        /// <returns></returns>
        private static bool FileNameMatchesPattern(string packageId, SemanticVersion version, string path) 
        {
            var name = Path.GetFileNameWithoutExtension(path);
            SemanticVersion parsedVersion;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // When matching by pattern, we will always have a version token. Packages without versions would be matched early on by the version-less path resolver 
            // when doing an exact match.
            return name.Length > packageId.Length &&
                   SemanticVersion.TryParse(name.Substring(packageId.Length + 1), out parsedVersion) &&
                   parsedVersion == version;
        }

        /// <summary>
        /// Find-Package based the given Id
        /// </summary>
        /// <param name="packageId">Package Id</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerable<IPackage> FindPackagesById(string packageId, NuGetRequest request)
        {
            return FindPackagesById(OpenPackage, packageId, request);
        }

        /// <summary>
        /// Find-Package based the given Id
        /// </summary>
        /// <param name="openPackage">Delegate function which is actually finding a package</param>
        /// <param name="packageId">Package version</param>
        /// <param name="request"></param>
        /// <returns></returns>
        private IEnumerable<IPackage> FindPackagesById(Func<string, Request, IPackage> openPackage, string packageId, Request request) 
        {          
            request.Debug(Resources.Messages.DebugInfoCallMethod3, "LocalPackageRepository", "FindPackagesById", packageId);

            // get packages in .nupkg or .nuspec files
            // add "*" to avoid parsing the version (id: packageName+Version, e.g. jQuery.1.10.0)
            return GetPackages(
                openPackage,
                packageId,
                GetPackageFiles(packageId + "*" + NuGetConstant.PackageExtension),
                request).Union(
                    GetPackages(
                        openPackage,
                        packageId,
                        GetPackageFiles(packageId + "*" + NuGetConstant.ManifestExtension),
                        request));

        }

        /// <summary>
        /// True if the packagesource is a file repository
        /// </summary>
        public bool IsFile {
            get {
                //true because this is not a local file repository
                return true;
            }
        }

        /// <summary>
        /// Search the entire repository for the case when a user does not provider package name or uses wildcards in the name.
        /// </summary>
        /// <param name="searchTerm">The Searchterm</param>
        /// <param name="nugetRequest"></param>
        /// <returns></returns>
        public IEnumerable<IPackage> Search(string searchTerm, NuGetRequest nugetRequest)
        {
            var packages = SearchImpl(searchTerm, nugetRequest);
            if (packages == null) {
                return Enumerable.Empty<IPackage>();
            }

            if (nugetRequest != null && nugetRequest.AllVersions.Value) {
                //return whatever we can find
                return packages;
            }

            //return the lastest version
            return packages.GroupBy(p => p.Id).Select(each => each.OrderByDescending(pp => pp.Version).FirstOrDefault());
        }

        private IEnumerable<IPackage> SearchImpl(string searchTerm, NuGetRequest nugetRequest)
        {
            if (nugetRequest == null) {
                yield break;
            }

            nugetRequest.Debug(Resources.Messages.SearchingRepository, "LocalPackageRepository", Source);
       
            var files = Directory.GetFiles(Source);

            foreach (var package in nugetRequest.FilterOnTags(files.Select(nugetRequest.GetPackageByFilePath).Where(pkgItem => pkgItem != null).Select(pkg => pkg.Package)))
            {
                yield return package;
            }

            // look in the package source location for directories that contain nupkg files.
            var subdirs = Directory.EnumerateDirectories(Source, "*", SearchOption.AllDirectories);        
            foreach (var subdir in subdirs) {
                var nupkgs = Directory.EnumerateFileSystemEntries(subdir, "*.nupkg", SearchOption.TopDirectoryOnly);

                foreach (var package in nugetRequest.FilterOnTags(nupkgs.Select(nugetRequest.GetPackageByFilePath).Where(pkgItem => pkgItem != null).Select(pkg => pkg.Package)))
                {
                    yield return package;
                }
            }
        }
    }
}
