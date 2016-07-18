namespace Microsoft.PackageManagement.NuGetProvider 
{
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Linq;
    using System.IO.Compression;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;
    using Microsoft.PackageManagement.Provider.Utility;
    using Microsoft.PackageManagement.NuGetProvider.Utility;

    /// <summary>
    /// Utility to handle the Find, Install, Uninstall-Package etc operations.
    /// </summary>
    internal static class NuGetClient
    {
        /// <summary>
        /// Find the package via the given uri query.
        /// </summary>
        /// <param name="query">A full Uri. A sample Uri looks like "http://www.nuget.org/api/v2/FindPackagesById()?id='Jquery'" </param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        /// <returns>Package objects</returns>
        internal static IEnumerable<PackageBase> FindPackage(string query, NuGetRequest request) {
            request.Debug(Messages.DebugInfoCallMethod, "NuGetClient", "FindPackage");

            request.Verbose(Messages.SearchingRepository, query, "");

            return HttpClientPackageRepository.SendRequest(query, request);
        }

        /// <summary>
        /// Download a package that matches the given version and name and install it on the local system.
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="version">Package version</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>  
        /// <param name="source">Package source</param>
        /// <param name="queryUrl">Full uri</param>
        /// <param name="packageHash">the hash of the package</param>
        /// <param name="packageHashAlgorithm">the hash algorithm of the package</param>
        /// <param name="progressTracker">progress tracker to help keep track of progressid, start and end of the progress</param>
        /// <returns>PackageItem object</returns>
        internal static PackageItem InstallPackage(
            string packageName,
            string version,
            NuGetRequest request,
            PackageSource source,
            string queryUrl,
            string packageHash,
            string packageHashAlgorithm,
            ProgressTracker progressTracker
            ) 
        {
            request.Debug(Messages.DebugInfoCallMethod, "NuGetClient", "InstallPackage");

            //If the destination folder does not exists, create it
            string destinationPath = request.Destination;
            request.Verbose(string.Format(CultureInfo.InvariantCulture, "InstallPackage' - name='{0}', version='{1}',destination='{2}'", packageName, version, destinationPath));

            string directoryToDeleteWhenFailed = string.Empty;
            bool needToDelete = false;
            string installFullPath = string.Empty;

            try
            {
                if (!Directory.Exists(destinationPath)) {
                    Directory.CreateDirectory(destinationPath);
                    // delete the destinationPath later on if we fail to install and if destinationPath did not exist before
                    directoryToDeleteWhenFailed = destinationPath;
                }

                //Create a folder under the destination path to hold the package
                string installDir = FileUtility.MakePackageDirectoryName(request.ExcludeVersion.Value, destinationPath, packageName, version);

                if (!Directory.Exists(installDir)) {
                    Directory.CreateDirectory(installDir);

                    // if directoryToDeleteWhenFailed is null then the destinationPath already exists before so we should not delete it
                    if (String.IsNullOrWhiteSpace(directoryToDeleteWhenFailed))
                    {
                        directoryToDeleteWhenFailed = installDir;
                    }
                }

                //Get the package file name based on the version and id
                string fileName = FileUtility.MakePackageFileName(request.ExcludeVersion.Value, packageName, version, NuGetConstant.PackageExtension);

                installFullPath = Path.Combine(installDir, fileName);

                // we assume downloading takes 70% of the progress
                int endProgressDownloading = progressTracker.ConvertPercentToProgress(0.7);

                //download to fetch the package
                DownloadPackage(packageName, version, installFullPath, queryUrl, request, source, new ProgressTracker(progressTracker.ProgressID, progressTracker.StartPercent, endProgressDownloading));

                // check that we have the file
                if (!File.Exists(installFullPath))
                {
                    needToDelete = true;
                    // error message is package failed to be downloaded
                    request.WriteError(ErrorCategory.ResourceUnavailable, installFullPath, Constants.Messages.PackageFailedInstallOrDownload, packageName,
                        CultureInfo.CurrentCulture.TextInfo.ToLower(Constants.Download));
                    return null;
                }

                #region verify hash
                //we don't enable checking for hash here because it seems like nuget provider does not
                //checks that there is hash. Otherwise we don't carry out the install
                
                if (string.IsNullOrWhiteSpace(packageHash))
                {
                    // if no hash (for example, vsts feed, install the package but log verbose message)
                    request.Verbose(string.Format(CultureInfo.CurrentCulture, Resources.Messages.HashNotFound, packageName));
                    //parse the package
                    var pkgItem = InstallPackageLocal(packageName, version, request, source, installFullPath, new ProgressTracker(progressTracker.ProgressID, endProgressDownloading, progressTracker.EndPercent));
                    return pkgItem;
                }

                // Verify the hash
                using (FileStream stream = File.OpenRead(installFullPath))
                {
                    HashAlgorithm hashAlgorithm = null;

                    switch (packageHashAlgorithm == null ? string.Empty : packageHashAlgorithm.ToLowerInvariant())
                    {
                        case "sha256":
                            hashAlgorithm = SHA256.Create();
                            break;

                        case "md5":
                            hashAlgorithm = MD5.Create();
                            break;

                        case "sha512":
                        // Flows to default case

                        // default to sha512 algorithm
                        default:
                            hashAlgorithm = SHA512.Create();
                            break;
                    }

                    if (hashAlgorithm == null)
                    {
                        // delete the file downloaded. VIRUS!!!
                        needToDelete = true;
                        request.WriteError(ErrorCategory.SecurityError, packageHashAlgorithm, Constants.Messages.HashNotSupported, packageHashAlgorithm);
                        return null;
                    }

                    // compute the hash
                    byte[] computedHash = hashAlgorithm.ComputeHash(stream);

                    // convert the original hash we got from the feed
                    byte[] downloadedHash = Convert.FromBase64String(packageHash);

                    // if they are not equal, just issue out verbose because there is a current bug in backend
                    // where editing the published module will result in a package with a different hash than the one
                    // provided on the feed
                    if (!Enumerable.SequenceEqual(computedHash, downloadedHash))
                    {
                        // delete the file downloaded. VIRUS!!!
                        request.Verbose(Constants.Messages.HashNotMatch, packageName);
                    }

                    //parse the package
                    var pkgItem = InstallPackageLocal(packageName, version, request, source, installFullPath, new ProgressTracker(progressTracker.ProgressID, endProgressDownloading, progressTracker.EndPercent));
                    return pkgItem;
                }

                #endregion

            }
            catch (Exception ex)
            {
                // the error will be package "packageName" failed to install because : "reason"
                ex.Dump(request);
                request.WriteError(ErrorCategory.InvalidResult, packageName, Resources.Messages.PackageFailedToInstallReason, packageName, ex.Message);
                needToDelete = true;
            }
            finally
            {
                if (needToDelete)
                {
                    // if the directory exists just delete it because it will contains the file as well
                    if (!String.IsNullOrWhiteSpace(directoryToDeleteWhenFailed) && Directory.Exists(directoryToDeleteWhenFailed))
                    {
                        try
                        {
                            FileUtility.DeleteDirectory(directoryToDeleteWhenFailed, true, isThrow: false);
                        }
                        catch { }
                    }

                    // if for some reason, we can't delete the directory or if we don't need to delete the directory
                    // then we have to delete installFullPath
                    if (File.Exists(installFullPath))
                    {
                        FileUtility.DeleteFile(installFullPath, isThrow: false);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Install a single package without checking for dependencies
        /// </summary>
        /// <param name="pkgItem"></param>
        /// <param name="request"></param>
        /// <param name="progressTracker"></param>
        /// <returns></returns>
        internal static bool InstallSinglePackage(PackageItem pkgItem, NuGetRequest request, ProgressTracker progressTracker)
        {
            PackageItem packageToBeInstalled;

            if (pkgItem == null || pkgItem.PackageSource == null || pkgItem.PackageSource.Repository == null)
            {
                return false;
            }

            // If the source location exists as a directory then we try to get the file location and provide to the packagelocal
            if (Directory.Exists(pkgItem.PackageSource.Location))
            {
                var fileLocation = pkgItem.PackageSource.Repository.FindPackage(pkgItem.Id, new SemanticVersion(pkgItem.Version), request).FullFilePath;
                packageToBeInstalled = NuGetClient.InstallPackageLocal(pkgItem.Id, pkgItem.Version, request, pkgItem.PackageSource, fileLocation, progressTracker);
            }
            else
            {             
                //V2 download package protocol:
                //sample url: http://www.nuget.org/api/v2/package/jQuery/2.1.3
                string append = String.Format(CultureInfo.InvariantCulture, "/package/{0}/{1}", pkgItem.Id, pkgItem.Version);
                string httpquery = PathUtility.UriCombine(pkgItem.PackageSource.Repository.Source, append);

                // wait for the result from installpackage
                packageToBeInstalled = NuGetClient.InstallPackage(pkgItem.Id, pkgItem.Version, request, pkgItem.PackageSource,
                    string.IsNullOrWhiteSpace(pkgItem.Package.ContentSrcUrl) ? httpquery : pkgItem.Package.ContentSrcUrl,
                    pkgItem.Package.PackageHash, pkgItem.Package.PackageHashAlgorithm, progressTracker);
            }

            // Package is installed successfully
            if (packageToBeInstalled != null)
            {
                // if this is a http repository, return metadata from online
                if (!pkgItem.PackageSource.Repository.IsFile)
                {
                    request.YieldPackage(pkgItem, packageToBeInstalled.PackageSource.Name, packageToBeInstalled.FullPath);
                }
                else
                {
                    request.YieldPackage(packageToBeInstalled, packageToBeInstalled.PackageSource.Name, packageToBeInstalled.FullPath);
                }

                request.Debug(Messages.DebugInfoReturnCall, "NuGetClient", "InstallSinglePackage");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Download a single package to destination without checking for dependencies
        /// </summary>
        /// <param name="pkgItem"></param>
        /// <param name="progressTracker"></param>
        /// <param name="request"></param>
        /// <param name="destLocation"></param>
        /// <returns></returns>
        internal static bool DownloadSinglePackage(PackageItem pkgItem, NuGetRequest request, string destLocation, ProgressTracker progressTracker)
        {
            if (string.IsNullOrWhiteSpace(pkgItem.PackageFilename) || pkgItem.PackageSource == null || pkgItem.PackageSource.Location == null
            || (pkgItem.PackageSource.IsSourceAFile && pkgItem.Package == null))
            {
                request.WriteError(ErrorCategory.ObjectNotFound, pkgItem.Id, Constants.Messages.UnableToResolvePackage, pkgItem.Id);
                return false;
            }

            // this is if the user says -force
            bool force = request.GetOptionValue("Force") != null;

            // combine the path and the file name
            destLocation = Path.Combine(destLocation, pkgItem.PackageFilename);

            // if the file already exists
            if (File.Exists(destLocation))
            {
                // if no force, just return
                if (!force)
                {
                    request.Verbose(Constants.Messages.SkippedDownloadedPackage, pkgItem.Id);
                    request.YieldPackage(pkgItem, pkgItem.PackageSource.Name);
                    return true;
                }

                // here we know it is forced, so delete
                FileUtility.DeleteFile(destLocation, isThrow: false);

                // if after we try delete, it is still there, tells the user we can't perform the action
                if (File.Exists(destLocation))
                {
                    request.WriteError(ErrorCategory.ResourceUnavailable, destLocation, Constants.Messages.UnableToOverwriteExistingFile, destLocation);
                    return false;
                }
            }

            bool downloadSuccessful = false;

            try
            {
                // if no repository, we can't do anything
                if (pkgItem.PackageSource.Repository == null)
                {
                    return false;
                }

                if (pkgItem.PackageSource.Repository.IsFile)
                {
                    using (var input = File.OpenRead(pkgItem.Package.FullFilePath))
                    {
                        using (var output = new FileStream(destLocation, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            input.CopyTo(output);
                        }
                    }
                }
                else
                {
                    //V2 download package protocol:
                    //sample url: http://www.nuget.org/api/v2/package/jQuery/2.1.3
                    string append = String.Format(CultureInfo.InvariantCulture, "/package/{0}/{1}", pkgItem.Id, pkgItem.Version);
                    string httpquery = PathUtility.UriCombine(pkgItem.PackageSource.Repository.Source, append);

                    downloadSuccessful = NuGetClient.DownloadPackage(pkgItem.Id, pkgItem.Version, destLocation,
                        string.IsNullOrWhiteSpace(pkgItem.Package.ContentSrcUrl) ? httpquery : pkgItem.Package.ContentSrcUrl, request, pkgItem.PackageSource, progressTracker);
                }
            }
            catch (Exception ex)
            {
                ex.Dump(request);
                return false;
            }

            if (downloadSuccessful)
            {
                request.Verbose(Resources.Messages.SuccessfullyDownloaded, pkgItem.Id);
                // provide the directory we save to to yieldpackage
                request.YieldPackage(pkgItem, pkgItem.PackageSource.Name, Path.GetDirectoryName(destLocation));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Install a single package. Also install any of its dependency if they are available (the dependency will be installed first).
        /// For dependencies, we will only get those that are not installed.
        /// Operation is either install or download
        /// installOrDownloadFunction is a function that takes in a packageitem and performs either install or download on it
        /// </summary>
        /// <param name="pkgItem"></param>
        /// <param name="request"></param>
        /// <param name="operation"></param>
        /// <param name="installOrDownloadFunction"></param>
        /// <returns></returns>
        internal static bool InstallOrDownloadPackageHelper(PackageItem pkgItem, NuGetRequest request, string operation,
            Func<PackageItem, ProgressTracker, bool> installOrDownloadFunction)
        {
            // pkgItem.Sources is the source that the user input. The request will try this source.
            request.OriginalSources = pkgItem.Sources;

            bool hasDependencyLoop = false;
            
            // Get the dependencies that are not already installed
            var dependencies = NuGetClient.GetPackageDependenciesToInstall(request, pkgItem, ref hasDependencyLoop).ToArray();

            // If there is a dependency loop. Warn the user and don't install the package
            if (hasDependencyLoop)
            {
                // package itself didn't install. Report error
                request.WriteError(ErrorCategory.DeadlockDetected, pkgItem.Id, Constants.Messages.DependencyLoopDetected, pkgItem.Id);
                return false;
            }

            // request may get canceled if there is a package dependencies missing
            if (request.IsCanceled)
            {
                return false;
            }

            int n = 0;
            int numberOfDependencies = dependencies.Count();

            // Start progress
            ProgressTracker progressTracker = ProgressTracker.StartProgress(null, string.Format(CultureInfo.InvariantCulture, Messages.InstallingOrDownloadingPackage, operation, pkgItem.Id), request);

            try
            {
                // check that this package has dependency and the user didn't want to skip dependencies
                if (numberOfDependencies > 0)
                {
                    // let's install dependencies
                    foreach (var dep in dependencies)
                    {
                        request.Progress(progressTracker.ProgressID, (n * 100 / (numberOfDependencies + 1)), string.Format(CultureInfo.InvariantCulture, Messages.InstallingOrDownloadingDependencyPackage, operation, dep.Id));

                        // start a subprogress bar for the dependent package
                        ProgressTracker subProgressTracker = ProgressTracker.StartProgress(progressTracker, string.Format(CultureInfo.InvariantCulture, Messages.InstallingOrDownloadingPackage, operation, dep.Id), request);
                        try
                        {
                            // Check that we successfully installed the dependency
                            if (!installOrDownloadFunction(dep, subProgressTracker))
                            {
                                request.WriteError(ErrorCategory.InvalidResult, dep.Id, Constants.Messages.DependentPackageFailedInstallOrDownload, dep.Id, CultureInfo.CurrentCulture.TextInfo.ToLower(operation));
                                return false;
                            }
                        }
                        finally
                        {
                            request.CompleteProgress(subProgressTracker.ProgressID, true);
                        }

                        n++;
                        request.Progress(progressTracker.ProgressID, (n * 100 / (numberOfDependencies + 1)), string.Format(CultureInfo.InvariantCulture, Messages.InstalledOrDownloadedDependencyPackage, operation, dep.Id));
                    }
                }

                // Now let's install the main package
                // the start progress should be where we finished installing the dependencies
                if (installOrDownloadFunction(pkgItem, new ProgressTracker(progressTracker.ProgressID, (n * 100 / (numberOfDependencies + 1)), 100)))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ex.Dump(request);
            }
            finally
            {
                // Report that we have completed installing the package and its dependency this does not mean there are no errors.
                // Just that it's completed.
                request.CompleteProgress(progressTracker.ProgressID, true);
            }
            
            // package itself didn't install. Report error
            request.WriteError(ErrorCategory.InvalidResult, pkgItem.Id, Constants.Messages.PackageFailedInstallOrDownload, pkgItem.Id, CultureInfo.CurrentCulture.TextInfo.ToLower(operation));

            return false;
        }

        /// <summary>
        /// Get the package dependencies that we need to installed. hasDependencyLoop is set to true if dependencyloop is detected.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="packageItem"></param>
        /// <param name="hasDependencyLoop"></param>
        /// <returns></returns>
        internal static IEnumerable<PackageItem> GetPackageDependenciesToInstall(NuGetRequest request, PackageItem packageItem, ref bool hasDependencyLoop)
        {
            request.Debug(Messages.DebugInfoCallMethod, "NuGetClient", "GetPackageDependencies");

            // No dependency
            if (packageItem.Package == null || packageItem.Package.DependencySetList == null)
            {
                request.Debug(Messages.DebugInfoReturnCall, "NuGetClient", "GetPackageDependencies");
                return Enumerable.Empty<PackageItem>();
            }

            // Returns list of dependency to be installed in the correct order that we should install them
            List<PackageItem> dependencyToBeInstalled = new List<PackageItem>();

            HashSet<PackageItem> permanentlyMarked = new HashSet<PackageItem>(new PackageItemComparer());
            HashSet<PackageItem> temporarilyMarked = new HashSet<PackageItem>(new PackageItemComparer());

            // checks that there are no dependency loop 
            hasDependencyLoop = !DepthFirstVisit(packageItem, temporarilyMarked, permanentlyMarked, dependencyToBeInstalled, new HashSet<string>(), request);

            if (!hasDependencyLoop)
            {
                request.Debug(Messages.DebugInfoReturnCall, "NuGetClient", "GetPackageDependencies");
                // remove the last item of the list because that is the package itself
                dependencyToBeInstalled.RemoveAt(dependencyToBeInstalled.Count - 1);
                return dependencyToBeInstalled;
            }

            // there are dependency loop. 
            request.Debug(Messages.DebugInfoReturnCall, "NuGetClient", "GetPackageDependencies");
            return Enumerable.Empty<PackageItem>();
        }

        /// <summary>
        /// Do a dfs visit. returns false if a cycle is encountered. Add the packageItem to the list at the end of each visit
        /// </summary>
        /// <param name="packageItem"></param>
        /// <param name="dependencyToBeInstalled"></param>
        /// <param name="permanentlyMarked"></param>
        /// <param name="temporarilyMarked"></param>
        /// <param name="processedDependencies"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        internal static bool DepthFirstVisit(PackageItem packageItem, HashSet<PackageItem> temporarilyMarked, HashSet<PackageItem> permanentlyMarked, List<PackageItem> dependencyToBeInstalled, HashSet<string> processedDependencies, NuGetRequest request)
        {
            // dependency loop detected because the element is temporarily marked
            if (temporarilyMarked.Contains(packageItem))
            {
                return false;
            }

            // this is permanently marked. So we don't have to visit it.
            // This is to resolve a case where we have: A->B->C and A->C. Then we need this when we visit C again from either B or A.
            if (permanentlyMarked.Contains(packageItem))
            {
                return true;
            }

            // Mark this node temporarily so we can detect cycle.
            temporarilyMarked.Add(packageItem);

            // Visit the dependency
            foreach (var dependency in GetPackageDependenciesHelper(packageItem, processedDependencies, request))
            {
                if (!DepthFirstVisit(dependency, temporarilyMarked, permanentlyMarked, dependencyToBeInstalled, processedDependencies, request))
                {
                    // if dfs returns false then we have encountered a loop
                    return false;
                }
                // otherwise visit the next dependency
            }

            // Add the package to the list so we can install later
            dependencyToBeInstalled.Add(packageItem);

            // Done with this node so mark it permanently
            permanentlyMarked.Add(packageItem);

            // Unmark it temporarily
            temporarilyMarked.Remove(packageItem);

            return true;
        }

        /// <summary>
        /// Returns the package dependencies of packageItem. We only return the dependencies that are not installed in the destination folder of request
        /// </summary>
        /// <param name="packageItem"></param>
        /// <param name="processedDependencies"></param>
        /// <param name="request"></param>
        private static IEnumerable<PackageItem> GetPackageDependenciesHelper(PackageItem packageItem, HashSet<string> processedDependencies, NuGetRequest request)
        {
            if (packageItem.Package.DependencySetList == null)
            {
                yield break;
            }

            bool force = request.GetOptionValue("Force") != null;
            foreach (var depSet in packageItem.Package.DependencySetList)
            {
                if (depSet.Dependencies == null)
                {
                    continue;
                }

                foreach (var dep in depSet.Dependencies)
                {
                    var depKey = string.Format(CultureInfo.InvariantCulture, "{0}!#!{1}", dep.Id, dep.DependencyVersion.ToStringSafe());

                    if (processedDependencies.Contains(depKey))
                    {
                        continue;
                    }

                    // Get the min dependencies version
                    string minVersion = dep.DependencyVersion.MinVersion.ToStringSafe();

                    // Get the max dependencies version
                    string maxVersion = dep.DependencyVersion.MaxVersion.ToStringSafe();

                    if (!force)
                    {
                        bool installed = false;

                        var installedPackages = request.InstalledPackages.Value;

                        if (request.InstalledPackages.Value.Count() > 0)
                        {
                            // check the installedpackages options passed in
                            foreach (var installedPackage in request.InstalledPackages.Value)
                            {
                                // if name not match, move on to the next entry
                                if (!string.Equals(installedPackage.Id, dep.Id, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                // if no version and if name matches, skip
                                if (string.IsNullOrWhiteSpace(installedPackage.Version))
                                {
                                    // skip this dependency
                                    installed = true;
                                    break;
                                }

                                SemanticVersion packageVersion = new SemanticVersion(installedPackage.Version);

                                // checks min and max
                                if (request.MinAndMaxVersionMatched(packageVersion, minVersion, maxVersion, dep.DependencyVersion.IsMinInclusive, dep.DependencyVersion.IsMaxInclusive))
                                {
                                    // skip this dependency
                                    installed = true;
                                    break;
                                }
                            }
                        }
                        // check whether package is installed at destination. only used this option if installedpackages not passed in
                        else if (request.GetInstalledPackages(dep.Id, null, minVersion, maxVersion, minInclusive: dep.DependencyVersion.IsMinInclusive, maxInclusive: dep.DependencyVersion.IsMaxInclusive, terminateFirstFound: true))
                        {
                            installed = true;
                        }

                        if (installed)
                        {
                            // already processed this so don't need to do this next time
                            processedDependencies.Add(dep.Id);
                            request.Verbose(String.Format(CultureInfo.CurrentCulture, Messages.AlreadyInstalled, dep.Id));
                            // already have a dependency so move on
                            continue;
                        }
                    }

                    // get all the packages that match this dependency
                    var dependentPackageItem = request.GetPackageById(dep.Id, request, minimumVersion: minVersion, maximumVersion: maxVersion, minInclusive: dep.DependencyVersion.IsMinInclusive, maxInclusive: dep.DependencyVersion.IsMaxInclusive).ToArray();

                    if (dependentPackageItem.Length == 0)
                    {
                        request.WriteError(ErrorCategory.ObjectNotFound, dep.Id, Constants.Messages.UnableToFindDependencyPackage, dep.Id);

                        break;
                    }

                    // Get the package that is the latest version
                    yield return dependentPackageItem.OrderByDescending(each => each.Version).FirstOrDefault();

                    processedDependencies.Add(depKey);
                }
            }
        }

        /// <summary>
        /// Download a package from a file repository that matches the given version and name and install it on the local system.
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="version">Package version</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>  
        /// <param name="source">Package source</param>
        /// <param name="sourceFilePath">File source path pointing to the package to be installed</param>
        /// <param name="progressTracker">progress tracker to help keep track of progressid, start and end of the progress</param>
        /// <returns>PackageItem object</returns>
        internal static PackageItem InstallPackageLocal(
            string packageName, 
            string version,
            NuGetRequest request, 
            PackageSource source,             
            string sourceFilePath,
            ProgressTracker progressTracker
            )
        {
            request.Debug(Messages.DebugInfoCallMethod, "NuGetClient", "InstallPackageLocal");

            string tempSourceFilePath = null;
            string tempSourceDirectory = null;

            string directoryToDeleteWhenFailed = String.Empty;
            bool needToDelete = false;

            try 
            {
                string destinationFilePath = request.Destination;
                request.Verbose(string.Format(CultureInfo.InvariantCulture, "InstallPackageLocal' - name='{0}', version='{1}',destination='{2}'", packageName, version, destinationFilePath));
                request.Debug(sourceFilePath);

                if (string.IsNullOrWhiteSpace(sourceFilePath))
                {
                    throw new ArgumentNullException(sourceFilePath);
                }

                if (!File.Exists(sourceFilePath)) {
                    throw new FileNotFoundException(sourceFilePath);
                }

                //Create the destination directory if it does not exist
                if (!Directory.Exists(destinationFilePath)) {
                    Directory.CreateDirectory(destinationFilePath);
                    directoryToDeleteWhenFailed = destinationFilePath;
                }

                //Make a temp folder in the user appdata temp directory 
                tempSourceFilePath = FileUtility.GetTempFileFullPath(fileExtension: NuGetConstant.PackageExtension);

                //Copy over the source file from  the folder repository to the temp folder
                File.Copy(sourceFilePath, tempSourceFilePath, true);

                request.Progress(progressTracker.ProgressID, progressTracker.StartPercent, string.Format(CultureInfo.CurrentCulture, Messages.Unzipping));
                //Unzip it
                tempSourceDirectory = PackageUtility.DecompressFile(tempSourceFilePath);

                //Get a package directory under the destination path to store the package
                string installedFolder = FileUtility.MakePackageDirectoryName(request.ExcludeVersion.Value, destinationFilePath, packageName, version);

                // if we did not set the directory before, then the destinationFilePath already exists, so we should not delete it
                if (string.IsNullOrWhiteSpace(directoryToDeleteWhenFailed))
                {
                    directoryToDeleteWhenFailed = installedFolder;
                }

                //File folder format of the Nuget packages looks like the following after installed:
                //Jquery.2.0.1
                //  - JQuery.2.0.1.nupkg
                //  - contents and other stuff

                // unzipping should take most of the time (assuming 70%)

                request.Progress(progressTracker.ProgressID, progressTracker.ConvertPercentToProgress(0.7), string.Format(CultureInfo.CurrentCulture, Messages.CopyUnzippedFiles, installedFolder));

                //Copy the unzipped files to under the package installed folder
                FileUtility.CopyDirectory(tempSourceDirectory, installedFolder, true);
                
                // copying should take another 15%
                // copy the nupkg file if it's not in
                var nupkgFilePath = Path.Combine(installedFolder, FileUtility.MakePackageFileName(request.ExcludeVersion.Value, packageName, version, NuGetConstant.PackageExtension));

                if (!File.Exists(nupkgFilePath))
                {
                    File.Copy(sourceFilePath, nupkgFilePath);
                }

                request.Progress(progressTracker.ProgressID, progressTracker.ConvertPercentToProgress(0.85), string.Format(CultureInfo.CurrentCulture, Messages.ReadingManifest));

                 //Read the package manifest and return the package object
                string nuspec = Path.Combine(installedFolder, packageName) + NuGetConstant.ManifestExtension;

                PackageBase package = PackageUtility.ProcessNuspec(nuspec);

                var pkgItem = new PackageItem {
                    Package = package,
                    PackageSource = source,
                    FastPath = request.MakeFastPath(source, package.Id, package.Version),
                    FullPath = installedFolder
                };

                // Delete the nuspec file
                //Get a package file path
                var nuspecFilePath = Path.Combine(installedFolder, packageName + NuGetConstant.ManifestExtension);

                if (File.Exists(nuspecFilePath))
                {
                    FileUtility.DeleteFile(nuspecFilePath, false);
                }

                request.Debug(Messages.DebugInfoReturnCall, "NuGetClient", "InstallPackageLocal");

                request.Progress(progressTracker.ProgressID, progressTracker.EndPercent, string.Format(CultureInfo.CurrentCulture, Messages.FinishInstalling, packageName));

                return pkgItem;

            } catch (Exception ex) {
                needToDelete = true;
                // the error will be package "packageName" failed to install because : "reason"
                ex.Dump(request);
                request.WriteError(ErrorCategory.InvalidResult, packageName, Resources.Messages.PackageFailedToInstallReason, packageName, ex.Message);
                throw;
            } finally {
                if (needToDelete && Directory.Exists(directoryToDeleteWhenFailed))
                {
                    FileUtility.DeleteDirectory(directoryToDeleteWhenFailed, true, isThrow: false);
                }

                FileUtility.DeleteFile(tempSourceFilePath, isThrow:false);
                FileUtility.DeleteDirectory(tempSourceDirectory, recursive: true, isThrow: false);
            }
        }

        /// <summary>
        /// Perform package uninstallation.
        /// </summary>
        /// <param name="request">Object given by the PackageManagement platform</param>
        /// <param name="pkg">PackageItem object</param>
        internal static void UninstallPackage(NuGetRequest request, PackageItem pkg)
        {
            request.Debug(Messages.DebugInfoCallMethod, "NuGetClient", "UninstallPackage");

            if (pkg == null)
            {
                throw new ArgumentNullException(paramName: "pkg");
            }

            var dir = pkg.InstalledDirectory;

            if (String.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                return;
            }

            FileUtility.DeleteDirectory(pkg.InstalledDirectory, recursive:true, isThrow:false);

            //Inform a user which package is deleted via the packageManagement platform
            request.Verbose(Messages.UninstalledPackage, "NuGetClient", pkg.Id);

            request.YieldPackage(pkg, pkg.Id);
        }

        /// <summary>
        /// Download a nuget package.
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="version">Package version</param>
        /// <param name="destination">Destination location to store the downloaded package</param>
        /// <param name="queryUrl">Uri to query the package</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>   
        /// <param name="pkgSource">source to download the package</param>
        /// <param name="progressTracker">Utility class to help track progress</param>
        /// 
        internal static bool DownloadPackage(string packageName, string version, string destination, string queryUrl, NuGetRequest request, PackageSource pkgSource, ProgressTracker progressTracker) 
        {
            try {                
                request.Verbose(string.Format(CultureInfo.InvariantCulture, "DownloadPackage' - name='{0}', version='{1}',destination='{2}', uri='{3}'", packageName, version, destination, queryUrl));

                if (new Uri(queryUrl).IsFile) {
                    throw new ArgumentException(Constants.Messages.UriSchemeNotSupported, queryUrl);
                }

                long result = 0;

                // Do not need to validate here again because the job is done by the httprepository that supplies the queryurl
                //Downloading the package
                //request.Verbose(httpquery);
                result = DownloadDataToFileAsync(destination, queryUrl, request, PathUtility.GetNetworkCredential(request.CredentialUsername, request.CredentialPassword), progressTracker).Result;                   

                if (result == 0 || !File.Exists(destination))
                {
                    request.Verbose(Messages.FailedDownloadPackage, packageName, queryUrl);
                    request.Warning(Constants.Messages.SourceLocationNotValid, queryUrl);
                    return false;
                } else {
                    request.Verbose(Messages.CompletedDownload, packageName);
                    return true;
                }
            } catch (Exception ex) {
                ex.Dump(request);
                request.Warning(Constants.Messages.PackageFailedInstallOrDownload, packageName, CultureInfo.CurrentCulture.TextInfo.ToLower(Constants.Download));
                throw;
            }
        }

        /// <summary>
        /// Returns the appropriate stream depending on the encoding
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static Stream GetStreamBasedOnEncoding(HttpResponseMessage response)
        {
            Stream result = response.Content.ReadAsStreamAsync().Result;
            // Gzip encoding so returns gzip stream
            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                return new GZipStream(result, CompressionMode.Decompress);
            }
            // Deflate encoding so returns deflate stream
            else if (response.Content.Headers.ContentEncoding.Contains("deflate"))
            {
                return new DeflateStream(result, CompressionMode.Decompress);
            }

            return result;
        }



        /// <summary>
        /// Download data from remote via uri query.
        /// </summary>
        /// <param name="query">Uri query</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>
        /// <returns></returns>
        internal static Stream DownloadDataToStream(string query, NuGetRequest request)
        {
            request.Debug(Messages.DownloadingPackage, query);

            var client = request.Client;

            var response = PathUtility.GetHttpResponse(client, query, (()=>request.IsCanceled),
                ((msg, num) => request.Verbose(Resources.Messages.RetryingDownload, msg, num)), (msg)=>request.Verbose(msg), (msg)=>request.Debug(msg));

            // Check that response was successful or throw exception
            if (response == null || !response.IsSuccessStatusCode)
            {
                request.Warning(Resources.Messages.CouldNotGetResponseFromQuery, query);
                return null;
            }

            // Read response and write out a stream
            var stream = GetStreamBasedOnEncoding(response);

            request.Debug(Messages.CompletedDownload, query);

            return stream;
        }

        /// <summary>
        /// Send an initial request to download data from the server.
        /// From the initial request, we may change the host of subsequent calls (if a redirection happens in this initial request)
        /// Also, if the initial request sends us less data than the amount we request, then we do not
        /// need to issue more requests
        /// </summary>
        /// <param name="query"></param>
        /// <param name="startPoint"></param>
        /// <param name="bufferSize"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        internal static Stream InitialDownloadDataToStream(UriBuilder query, int startPoint, int bufferSize, NuGetRequest request)
        {
            var uri = String.Format(CultureInfo.CurrentCulture, query.Uri.ToString(), startPoint, bufferSize);
            request.Debug(Messages.DownloadingPackage, uri);

            var client = request.Client;

            var response = PathUtility.GetHttpResponse(client, uri, (() => request.IsCanceled),
               ((msg, num) => request.Verbose(Resources.Messages.RetryingDownload, msg, num)), (msg) => request.Verbose(msg), (msg) => request.Debug(msg));


            // Check that response was successful or write error
            if (response == null || !response.IsSuccessStatusCode)
            {
                request.Warning(Resources.Messages.CouldNotGetResponseFromQuery, uri);
                return null;
            }

            // Read response and write out a stream
            var stream = GetStreamBasedOnEncoding(response);

            request.Debug(Messages.CompletedDownload, uri);

            // If the host from the response is different, change the host of the original query
            if (!String.Equals(response.RequestMessage.RequestUri.Host, query.Host, StringComparison.OrdinalIgnoreCase))
            {
                query.Host = response.RequestMessage.RequestUri.Host;
            }

            return stream;
        }

        /// <summary>
        /// Download data from remote via uri query.
        /// </summary>
        /// <param name="fileName">A file to store the downloaded data.</param>
        /// <param name="query">Uri query</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>   
        /// <param name="networkCredential">Credential to pass along to get httpclient</param>
        /// <param name="progressTracker">Utility class to help track progress</param>
        /// <returns></returns>
        internal static async Task<long> DownloadDataToFileAsync(string fileName, string query, NuGetRequest request,
            NetworkCredential networkCredential, ProgressTracker progressTracker)
        {
            request.Verbose(Messages.DownloadingPackage, query);

            var httpClient = request.Client;

            // try downloading for 3 times
            int remainingTry = 3;
            long totalDownloaded = 0;
            long totalBytesToReceive = 0;
            bool cleanUp = false;
            CancellationTokenSource cts = new CancellationTokenSource(); ;
            Stream input = null;
            Timer timer = null;
            FileStream output = null;
            object lockObject = new object();

            // function to perform cleanup
            Action cleanUpAction = () => {
                lock (lockObject)
                {
                    // if clean up is done before, don't need to do again
                    if (!cleanUp)
                    {
                        try
                        {
                            // dispose timer
                            if (timer != null)
                            {
                                timer.Change(Timeout.Infinite, Timeout.Infinite);
                                timer.Dispose();
                            }

                            // dispose cts token
                            if (cts != null)
                            {
                                cts.Cancel();
                                cts.Dispose();
                            }
                        }
                        catch { }

                        try
                        {
                            // dispose input and output stream
                            if (input != null)
                            {
                                input.Dispose();
                            }

                            // it is important that we dispose of the output here, otherwise we may not be able to delete the file
                            if (output != null)
                            {
                                output.Dispose();
                            }

                            // if the download didn't complete, log verbose message
                            if (totalBytesToReceive != totalDownloaded)
                            {
                                request.Verbose(string.Format(Resources.Messages.IncompleteDownload, totalDownloaded, totalBytesToReceive));
                            }

                            // if we couldn't download anything
                            if (totalDownloaded == 0 && File.Exists(fileName))
                            {
                                File.Delete(fileName);
                            }
                        }
                        catch { }

                        cleanUp = true;
                    }
                }
            };

            while (remainingTry > 0)
            {
                // if user cancel the request, no need to do anything
                if (request.IsCanceled)
                {
                    break;
                }

                input = null;
                output = null;
                totalDownloaded = 0;

                try
                {
                    // decrease try by 1
                    remainingTry -= 1;

                    // create new timer and cancellation token source
                    lock (lockObject)
                    {
                        // check every second to see whether request is cancelled
                        timer = new Timer(_ =>
                        {
                            if (request.IsCanceled)
                            {
                                cleanUpAction();
                            }
                        }, null, 500, 1000);

                        cts = new CancellationTokenSource();

                        cleanUp = false;
                    }

                    var response = await httpClient.GetAsync(query, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                    if (response.Content != null && response.Content.Headers != null)
                    {
                        totalBytesToReceive = response.Content.Headers.ContentLength ?? 0;
                        // the total amount of bytes we need to download in megabytes
                        double totalBytesToReceiveMB = (totalBytesToReceive / 1024f) / 1024f;

                        // Read response asynchronously and write out a file
                        // The return value is for the caller to wait for the async operation to complete.
                        input = await response.Content.ReadAsStreamAsync();

                        // buffer size of 64 KB, this seems to be preferable buffer size, not too small and not too big
                        byte[] bytes = new byte[1024 * 64];
                        output = File.Open(fileName, FileMode.OpenOrCreate);

                        int current = 0;
                        double lastPercent = 0;

                        // here we read content that we got from the http response stream into the bytes array
                        current = await input.ReadAsync(bytes, 0, bytes.Length, cts.Token);

                        // report initial progress
                        request.Progress(progressTracker.ProgressID, progressTracker.StartPercent,
                            string.Format(CultureInfo.CurrentCulture, Resources.Messages.DownloadingProgress, 0, (totalBytesToReceive / 1024f) / 1024f));

                        while (current > 0)
                        {
                            totalDownloaded += current;

                            // here we write the bytes array content into the file
                            await output.WriteAsync(bytes, 0, current, cts.Token);

                            double percent = totalDownloaded * 1.0 / totalBytesToReceive;

                            // don't want to report too often (slow down performance)
                            if (percent > lastPercent + 0.1)
                            {
                                lastPercent = percent;
                                // percent between startProgress and endProgress
                                var progressPercent = progressTracker.ConvertPercentToProgress(percent);

                                // report the progress
                                request.Progress(progressTracker.ProgressID, (int)progressPercent,
                                    string.Format(CultureInfo.CurrentCulture, Resources.Messages.DownloadingProgress, (totalDownloaded / 1024f) / 1024f, totalBytesToReceiveMB));
                            }

                            // here we read content that we got from the http response stream into the bytes array
                            current = await input.ReadAsync(bytes, 0, bytes.Length, cts.Token);
                        }

                        // check that we download everything
                        if (totalDownloaded == totalBytesToReceive)
                        {
                            // report that we finished with the download
                            request.Progress(progressTracker.ProgressID, progressTracker.EndPercent,
                                string.Format(CultureInfo.CurrentCulture, Resources.Messages.DownloadingProgress, totalDownloaded, totalBytesToReceive));

                            request.Verbose(Messages.CompletedDownload, query);

                            break;
                        }

                        // otherwise, we have to retry again
                    }

                    // if request is canceled, don't retry
                    if (request.IsCanceled)
                    {
                        break;
                    }

                    request.Verbose(Resources.Messages.RetryingDownload, query, remainingTry);
                }
                catch (Exception ex)
                {
                    request.Verbose(ex.Message);
                    request.Debug(ex.StackTrace);
                    // if there is exception, we will retry too
                    request.Verbose(Resources.Messages.RetryingDownload, query, remainingTry);
                }
                finally
                {
                    cleanUpAction();
                }
            }

            return totalDownloaded;
        }
    }
}
