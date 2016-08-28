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
    using System.Globalization;   
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Packaging;
    using Microsoft.PackageManagement.Provider.Utility;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;
    internal static class PowerShellArtifactInstaller
    {
        internal static void InstallPowershellArtifacts(PackageJson package, string fastPath, PackageSourceListRequest request)
        {
            var provider1 = PackageSourceListRequest.FindProvider(request, package.Type, request, true);         
            if (provider1 == null) return;

            // As the PowerShellGet may access the -source via $request.Options or PackageSources,
            // so we need to fill in both options and sources parameters
            var installRequest = PackageSourceListRequest.ExtendRequest(new Dictionary<string, string[]>
                           {
                                {"Source", new[] {package.Source ?? ""}}
                           }, new[] { package.Source ?? "" }, package.IsTrustedSource, request);

            var psid = PackageSourceListRequest.CreateCanonicalId(package, Constants.ProviderNames.PowerShellGet);
            var pkgs = request.PackageManagementService.FindPackageByCanonicalId(psid, installRequest)
             .Where(each => new SemanticVersion(each.Version) == new SemanticVersion(package.Version)).ToArray();
            
            switch (pkgs.Length)
            {
                case 0:
                    request.Warning(Resources.Messages.CannotFindPackage, Constants.ProviderName, psid);
                    break;
                case 1:
                    InstallPackageViaPowerShellGet(package, request, pkgs);
                    break;

                default:
                    request.Warning(Resources.Messages.FoundMorePackages, Constants.ProviderName, pkgs.Length, psid);
                    break;
            }
            return;
        }
     
        private static void InstallPackageViaPowerShellGet(PackageJson packageJson, PackageSourceListRequest request, SoftwareIdentity[] packages)
        {

            var provider = PackageSourceListRequest.FindProvider(request, packageJson.Type, request, true);
            if (provider == null) return;
            
            IHostApi installRequest = request;

            if (provider.Name.EqualsIgnoreCase(Constants.ProviderNames.PowerShellGet) && !request.ProviderServices.IsElevated) {
                // if we're not elevated, we want powershellget to install to the user scope
                installRequest = PackageSourceListRequest.ExtendRequest(
                    new Dictionary<string, string[]> {
                        {"Scope", new[] {"CurrentUser"}}
                    }, null, packageJson.IsTrustedSource, request);

            } else {
                installRequest = PackageSourceListRequest.ExtendRequest(
                    new Dictionary<string, string[]> {
                        {"Destination", new[] {packageJson.Destination ?? ""}}
                    }, null, packageJson.IsTrustedSource, request);
            }

            request.Debug("Calling '{0}' provider to install the package '{1}.{2}'", provider.Name, packageJson.Name, packageJson.Version);

            var installing = provider.InstallPackage(packages[0], installRequest);
          
            if (installing == null || !installing.Any())
            {
                request.Verbose(Resources.Messages.NumberOfPackagesRecevied, 0, provider.Name, "InstallPackage");
                request.Warning(Resources.Messages.FailToInstallPackage, Constants.ProviderName, packages[0].Name);
                return;
            }

            int packagesReceived = 0;
            foreach (var i in installing)
            {
                request.YieldSoftwareIdentity(i.FastPackageReference, i.Name, i.Version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);

                if (request.IsCanceled)
                {
                    installing.Cancel();
                }
                else
                {
                    request.Verbose(Resources.Messages.SuccessfullyInstalled, "{0}.{1}".format(packageJson.Name, packageJson.Version));
                    //load provider
                    if (packageJson.IsPackageProvider)
                    {
                        //Per provider development guidance: provider name and module name should be the same otherwise we can not import it.
                        request.PackageManagementService.ImportPackageProvider(request, packageJson.Name, null, null, null, isRooted: false, force: false);
                    }
                }
                packagesReceived++;
            }

            request.Verbose(Resources.Messages.NumberOfPackagesRecevied, packagesReceived, provider.Name, "install-package");
        }

        internal static void GeInstalledPowershellArtifacts(PackageJson package, string requiredVersion, string minimumVersion, string maximumVersion, Dictionary<string, SoftwareIdentity> fastPackReftable, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "GeInstalledPowershellArtifacts' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", package.Name, requiredVersion, minimumVersion, maximumVersion));

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, request);
            if (provider == null) return;

            //calling the PowerShellGet provider
            request.Debug("Calling '{0}' provider to get installed packages '{1}.{2}'", provider.Name, package.Name, package.Version);

            var packagesInstalled = provider.GetInstalledPackages(package.Name, requiredVersion, minimumVersion, maximumVersion, request).ToArray();

            if (packagesInstalled == null || !packagesInstalled.Any())
            {
                request.Verbose(Resources.Messages.NumberOfPackagesRecevied, 0, provider.Name, "GetInstalledPackages");
                return;
            }          
            
            foreach (var i in packagesInstalled)
            {
                request.Debug("Found an installed package '{0}.{1} from {2}' ", i.Name, i.Version, i.Source);
                var info = PackageSourceListRequest.MakeFastPathComplex(i.Source, i.Name, "", i.Version, "");

                fastPackReftable.AddOrSet(info, i);

                // check if the installed version matches with the one specified in the PSL.json.
                // If so, we choose PSL.json.
                var version = i.Version.CompareVersion(package.Version) ? package.Version : i.Version;
                request.YieldSoftwareIdentity(info, i.Name, version, i.VersionScheme, i.Summary, i.Source, i.SearchKey, i.FullPath, i.PackageFilename);
            }                       
        }

        /// <summary>
        /// Uninstalls a package
        /// </summary>
        /// <param name="package">package defined in the PackageSourceList</param>
        /// <param name="fastPackageReference"></param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param>
        /// <param name="fastPackReftable"></param> 
        internal static void UninstallPowershellArtifacts(PackageJson package, string fastPackageReference, PackageSourceListRequest request, Dictionary<string, SoftwareIdentity> fastPackReftable)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(CultureInfo.InvariantCulture, "UninstallNuGetPackage' - fastReference='{0}'", fastPackageReference));

            var provider = PackageSourceListRequest.FindProvider(request, package.Type, request, true);
            if (provider != null)
            {
                request.Debug("{0}: Using the provider '{1} to uninstall the package '{2}'", Constants.ProviderName, provider.Name, package.Name);

                if (!fastPackReftable.ContainsKey(fastPackageReference))
                {
                    request.WriteError(Internal.ErrorCategory.InvalidData, fastPackageReference, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPackageReference);
                    return;
                }

                var p = fastPackReftable[fastPackageReference];

                //calling NuGet for uninstall
                var installing = provider.UninstallPackage(p, request);

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
}

#endif