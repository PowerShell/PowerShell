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
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;
    using Microsoft.PackageManagement.Packaging;
    using Microsoft.PackageManagement.Provider.Utility;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using System.Globalization;
    using SemanticVersion = Microsoft.PackageManagement.Provider.Utility.SemanticVersion;

    internal static class NupkgInstaller {

        internal static void GeInstalledNuGetPackages(PackageJson package, string requiredVersion, string minimumVersion, string maximumVersion, Dictionary<string, SoftwareIdentity> fastPackReftable, PackageSourceListRequest request)
        { 
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "GeInstalledNuGetPackages' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", package.Name, requiredVersion, minimumVersion, maximumVersion));

            //clone the request
            //nuget provider may not know the location of the package gets installed.
            //we need to pass around the destination path to nuget provider
            var installedRequest = PackageSourceListRequest.ExtendRequest(
                   new Dictionary<string, string[]> {
                        {"Destination", new[] { package.Destination ?? ""}}
                   }, null, package.IsTrustedSource, request);

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, request);
            if (provider != null)
            {
                //calling NuGet provider
                var packagesInstalled = provider.GetInstalledPackages(package.Name, requiredVersion, minimumVersion, maximumVersion, installedRequest);
                if (packagesInstalled != null)
                {
                    foreach (var i in packagesInstalled)
                    {
                        request.Debug("Found an installed package '{0}.{1} from {2}' ", i.Name, i.Version, i.Source);
                        var info = PackageSourceListRequest.MakeFastPathComplex(i.Source, i.Name, "", i.Version, "");

                        fastPackReftable.AddOrSet(info, i);

                        // make it semver because in find-package we use semver
                        var version = i.Version.CompareVersion(package.Version) ? package.Version : i.Version;
                        request.YieldSoftwareIdentity(info, i.Name, version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);

                    }
                }
            }
        }

        /// <summary>
        /// Uninstalls a package
        /// </summary>
        /// <param name="package">package defined in the PackageSourceList</param>
        /// <param name="fastPackageReference"></param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param>
        /// <param name="fastPackReftable"></param> 
        internal static void UninstallNuGetPackage(PackageJson package, string fastPackageReference, PackageSourceListRequest request, Dictionary<string, SoftwareIdentity> fastPackReftable)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "UninstallNuGetPackage' - fastReference='{0}'", fastPackageReference));

            var unInstallRequest = PackageSourceListRequest.ExtendRequest(
                new Dictionary<string, string[]>
                {
                    {"Destination", new[] {package.Destination ?? ""}}
                }, null, package.IsTrustedSource, request);

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, request, true);
            if (provider != null)
            {
                request.Debug("{0}: Using the provider '{1} to uninstall the package '{2}'", Constants.ProviderName, provider.Name, package.Name);

                if (!fastPackReftable.ContainsKey(fastPackageReference))
                {
                    request.WriteError(ErrorCategory.InvalidData, fastPackageReference, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPackageReference);                   
                    return;
                }

                var p = fastPackReftable[fastPackageReference];

                //calling NuGet for uninstall
                var installing = provider.UninstallPackage(p, unInstallRequest);

                foreach (var i in installing)
                {
                    request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);

                    if (request.IsCanceled)
                    {
                        installing.Cancel();
                    }
                }
            } 
        }

        internal static void DownloadNuGetPackage(string fastPath, string location, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName,
                string.Format(CultureInfo.InvariantCulture, "DownloadNuGetPackage' - fastReference='{0}', location='{1}'", fastPath, location));

            var package = request.GetPackageByFastPath(fastPath);
            if (package == null)
            {
                request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPath);
                return;
            }

            // let the core figure out how to save this package 
            var canonicalId = PackageSourceListRequest.CreateCanonicalId(package, Constants.ProviderNames.NuGet); // "nuget:jquery/2.1.0#http://nuget.org/api/v2";
            var pkgs = request.PackageManagementService.FindPackageByCanonicalId(canonicalId, request.As<IHostApi>())
                        .Where(each => string.IsNullOrWhiteSpace(package.Version) ||
                        (new SemanticVersion(each.Version) == new SemanticVersion(package.Version))).ToArray();

            switch (pkgs.Length)
            {
                case 0:
                    request.Warning(Resources.Messages.CannotFindPackage, Constants.ProviderName, canonicalId);
                    return;

                case 1:
                    var provider = request.PackageManagementService.GetAvailableProviders(request, new[] {"NuGet"}).FirstOrDefault();
                    if (provider != null)
                    {                      
                        var downloadrequest = PackageSourceListRequest.ExtendRequest(
                            new Dictionary<string, string[]>
                            {
                                {"Destination", new[] {package.Destination ?? ""}}
                            }, 
                            new[] {package.Source ?? ""}, 
                            package.IsTrustedSource, 
                            request);

                        var downloading = provider.DownloadPackage(pkgs[0], location, downloadrequest);

                        foreach (var i in downloading)
                        {
                            request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);

                            if (request.IsCanceled)
                            {
                                downloading.Cancel();
                            }
                        }
                    }
                    break;
                default:
                    request.Warning(Resources.Messages.FoundMorePackages, Constants.ProviderName, pkgs.Length, canonicalId);
                    return;
            }

        }

        internal static void InstallNuGetPackage(PackageJson package, string fastPath, PackageSourceListRequest request)
        {
            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "InstallNuGetPackage' - name='{0}', fastPath='{1}'", package.Name, fastPath));

            var canonicalId = PackageSourceListRequest.CreateCanonicalId(package, Constants.ProviderNames.NuGet); // "nuget:jquery/2.1.0#http://nuget.org/api/v2";
            var pkgs = request.PackageManagementService.FindPackageByCanonicalId(canonicalId, request.As<IHostApi>())
                        .Where(each => string.IsNullOrWhiteSpace(package.Version) ||
                        (new SemanticVersion(each.Version) == new SemanticVersion(package.Version))).ToArray();

            switch (pkgs.Length)
            {
                case 0:
                    request.Warning(Resources.Messages.CannotFindPackage, Constants.ProviderName, canonicalId);
                    return;
                case 1:
                    InstallPackageReference(package, request, pkgs);
                    return;

                default:
                    request.Warning(Resources.Messages.FoundMorePackages, Constants.ProviderName, pkgs.Length, canonicalId);
                    return;
            }
        }

        private static void InstallPackageReference(PackageJson package, PackageSourceListRequest request, SoftwareIdentity[] packages)
        {
            var installRequest = PackageSourceListRequest.ExtendRequest(
                           new Dictionary<string, string[]>
                           {
                                {"Destination", new[] {package.Destination ?? ""}}
                           },
                           new[] { package.Source ?? "" },
                           package.IsTrustedSource,
                           request);

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, installRequest, true);   
            if (provider == null)
            {
                return;
            }

            var installing = provider.InstallPackage(packages[0], installRequest);
            foreach (var i in installing)
            {
                request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);
                if (request.IsCanceled)
                {
                    installing.Cancel();
                }
            }
        }
    }
}

#endif