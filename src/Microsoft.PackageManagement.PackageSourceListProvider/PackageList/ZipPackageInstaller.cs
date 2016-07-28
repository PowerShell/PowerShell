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
    using System.IO;
    using System.Linq;   
    using System.IO.Compression;
    using System.Threading;
    using Microsoft.PackageManagement.Provider.Utility;   
    using Microsoft.PackageManagement.Internal;
    internal static class  ZipPackageInstaller {
        internal static void GetInstalledZipPackage(PackageJson package, PackageSourceListRequest request)
        {

            string path = Path.Combine(package.Destination, package.Name, package.Version);
            if (Directory.Exists(path))
            {
                if (Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any())
                {
                    var fp = PackageSourceListRequest.MakeFastPathComplex(package.Source, package.Name, (package.DisplayName ?? ""), package.Version, path);

                    //the directory exists and contain files, we think the package has been installed.
                    request.YieldSoftwareIdentity(fp, package.Name, package.Version, package.VersionScheme, package.Summary, path, package.Name, path, path);
                }
            }
        }
        internal static bool InstallZipPackage(PackageJson package, string fastPath, PackageSourceListRequest request)
        {
            // download the exe package
            var file = Path.ChangeExtension(Path.GetTempFileName(), "exe");

            WebDownloader.DownloadFile(package.Source, file, request, null);
            if (!File.Exists(file))
            {
                return false;
            }

            // validate the file
            if (!WebDownloader.VerifyHash(file, package,request))
            {                
                return false;
            }

            if (!package.IsTrustedSource)
            {
                if (!request.ShouldContinueWithUntrustedPackageSource(package.Name, package.Source))
                {
                    request.Warning(Constants.Messages.UserDeclinedUntrustedPackageInstall, package.Name);
                    return false;
                }
            }


            Timer timer = null;
            object timerLock = new object();
            bool cleanUp = false;

            ProgressTracker tracker = new ProgressTracker(request.StartProgress(0, "Installing Zip Package............"));
            double percent = tracker.StartPercent;
            Action cleanUpAction = () => {
                lock (timerLock)
                {
                    // check whether clean up is already done before or not
                    if (!cleanUp)
                    {
                        try
                        {
                            if (timer != null)
                            {
                                // stop timer
                                timer.Change(Timeout.Infinite, Timeout.Infinite);
                                timer.Dispose();
                                timer = null;
                            }
                        }
                        catch
                        {
                        }

                        cleanUp = true;
                    }
                }
            };
          
            // extracted folder
            string extractedFolder = string.Concat(file.GenerateTemporaryFilename());

            try
            {

                timer = new Timer(_ =>
                {
                    percent += 0.025;
                    var progressPercent = tracker.ConvertPercentToProgress(percent);
                    if (progressPercent < 90)
                    {
                        request.Progress(tracker.ProgressID, (int)progressPercent, string.Format(CultureInfo.CurrentCulture, "Copying files ..."));
                    }
                    if (request.IsCanceled)
                    {
                        cleanUpAction();
                    }
                }, null, 0, 1000);

                //unzip the file
                ZipFile.ExtractToDirectory(file, extractedFolder);

                if (Directory.Exists(extractedFolder))
                {

                    var versionFolder = Path.Combine(package.Destination, package.Name, package.Version);

                    // create the directory version folder if not exist
                    if (!Directory.Exists(versionFolder))
                    {
                        Directory.CreateDirectory(versionFolder);
                    }

                    try
                    {
                        FileUtility.CopyDirectory(extractedFolder, versionFolder, true);
                        request.YieldFromSwidtag(package, fastPath);
                    }
                    catch (Exception e)
                    {
                        request.CompleteProgress(tracker.ProgressID, false);
                        request.Debug(e.StackTrace);

                        if (!(e is UnauthorizedAccessException || e is IOException))
                        {
                            // something wrong, delete the version folder
                            versionFolder.TryHardToDelete();
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    request.Warning("Failed to download a Zip package {0} from {1}", package.Name, package.Source);
                }
            }
            finally
            {
                cleanUpAction();
                file.TryHardToDelete();
                extractedFolder.TryHardToDelete();
                request.CompleteProgress(tracker.ProgressID, true);

            }

            return false;
        }

        internal static bool UnInstallZipPackage(PackageSourceListRequest request, string fastPath)
        {
            string sourceLocation;
            string id;
            string displayName;
            string version;
            string path;

            var package = request.GetFastReferenceComplex(fastPath);

            if (!request.TryParseFastPathComplex(fastPath: fastPath, regex: PackageSourceListRequest.RegexFastPathComplex, location: out sourceLocation, id: out id, displayname: out displayName, version: out version, fastpath: out path))
            {
                request.Error(ErrorCategory.InvalidOperation, "package", Constants.Messages.UnableToUninstallPackage);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                path.TryHardToDelete();
                request.YieldFromSwidtag(package, path);
                return true;
            }
            else
            {
                request.Error(ErrorCategory.InvalidData, "folder {0} does not exist", path);
                return false;
            }
        }

        internal static void DownloadZipPackage(string fastPath, string location, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            //TODO
            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName,
                string.Format(CultureInfo.InvariantCulture, "DownloadZipPackage' - fastReference='{0}', location='{1}'", fastPath, location));
           
        }
    }
}

#endif