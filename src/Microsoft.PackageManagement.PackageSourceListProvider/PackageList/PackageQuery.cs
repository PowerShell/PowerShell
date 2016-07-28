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
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Provider.Utility;
    using System.Globalization;
    internal class PackageQuery
    {
        private Dictionary<string, List<PackageJson>> _PackageSourceList;

        private PackageQuery() { }

        internal virtual bool IsValid
        {
            get
            {
                return _PackageSourceList != null;
            }
        }
        internal PackageQuery(PackageSource packageSource, PackageSourceListRequest request)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException("packageSource");
            }

            if(string.IsNullOrWhiteSpace(packageSource.Location) || !System.IO.File.Exists(packageSource.Location))
            {
                request.Warning(Resources.Messages.PackageSourceManifestNotFound, packageSource.Location, Constants.ProviderName);
                return;
            }

            Uri uri;

            if (Uri.TryCreate(packageSource.Location, UriKind.Absolute, out uri))
            {
                if (uri.IsFile)
                {
                    try
                    {
                        //process the file
                        _PackageSourceList = JsonParser.ReadPackageSpec(packageSource, null);
                    }
                    catch (Exception ex)
                    {
                        request.Warning(ex.Message);
                        while (ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            request.Warning(ex.Message);
                        }
                        request.Warning(string.Format(CultureInfo.CurrentCulture, Resources.Messages.InvalidPackageListFormat, uri.AbsolutePath));
                        ex.Dump(request);
                    }
                }

                // Uri?
                //TODO: ask a user whether go ahead for downloading an sample PSL.json
            }
            else
            {
                //TODO: Check with GP, DSC Settings, 
                
                request.Verbose(Resources.Messages.UnsupportedPackageSource, packageSource.Location);
                return;
            }
        }

        internal PackageQuery(PackageSource packageSource, string file, PackageSourceListRequest request)
        {
            try
            {
                //process the file
                _PackageSourceList = JsonParser.ReadPackageSpec(packageSource, file);
            }
            catch (Exception ex)
            {
                request.Warning(ex.Message);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    request.Warning(ex.Message);
                }
                request.Warning(string.Format(CultureInfo.CurrentCulture, Resources.Messages.InvalidPackageListFormat, file));
                ex.Dump(request);
            }
        }

        internal IEnumerable<PackageJson> Query()
        {
            return _PackageSourceList.SelectMany(each => each.Value).Where(item => !item.IsCommonDefinition); ;

        }

        internal IEnumerable<PackageJson> Query(string name)
        {
            return
                _PackageSourceList.SelectMany(each => each.Value)
                    .Where(item => (item.Name.EqualsIgnoreCase(name) || (!string.IsNullOrWhiteSpace(item.DisplayName) && item.DisplayName.EqualsIgnoreCase(name))) && !item.IsCommonDefinition);

        }

        internal IEnumerable<PackageJson> Query(string name, string version)
        {
            return string.IsNullOrWhiteSpace(version)
                ? _PackageSourceList.SelectMany(each => each.Value).Where(item => (item.Name.EqualsIgnoreCase(name) ||
                                                                              (!string.IsNullOrWhiteSpace(item.DisplayName) && item.DisplayName.EqualsIgnoreCase(name))) && !item.IsCommonDefinition)
                : _PackageSourceList.SelectMany(each => each.Value)
                    .Where(item =>
                            (item.Name.EqualsIgnoreCase(name) || (!string.IsNullOrWhiteSpace(item.DisplayName) && item.DisplayName.EqualsIgnoreCase(name)))
                            && !item.IsCommonDefinition
                            && (string.IsNullOrWhiteSpace(item.Version) || new SemanticVersion(item.Version) == new SemanticVersion(version)));
        }

        internal IEnumerable<PackageJson> Query(string name, string minimumVersion, string maximumVersion)
        {
            if (string.IsNullOrEmpty(minimumVersion) && string.IsNullOrEmpty(maximumVersion))
            {
                return Query(name);
            }

            if (!IsValid || string.IsNullOrEmpty(name))
            {
                return Enumerable.Empty<PackageJson>();
            }

            var packages = _PackageSourceList.SelectMany(each => each.Value).Where(item => (item.Name.EqualsIgnoreCase(name) ||
                                                                           (!string.IsNullOrWhiteSpace(item.DisplayName) && item.DisplayName.EqualsIgnoreCase(name))) && !item.IsCommonDefinition);

            return packages.Where(pkg => {

                if (!string.IsNullOrWhiteSpace(minimumVersion))
                {
                    if (SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, minimumVersion) < 0)
                    {
                        // a minimum version was specified, but the package version is less than the specified minimumversion.
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(maximumVersion))
                {
                    if (SoftwareIdentityVersionComparer.CompareVersions(pkg.VersionScheme, pkg.Version, maximumVersion) > 0)
                    {
                        // a maximum version was specified, but the package version is more than the specified maximumversion.
                        return false;
                    }
                }

                // the version is in the range asked for.
                return true;
            });                           
      }
    }
}

#endif