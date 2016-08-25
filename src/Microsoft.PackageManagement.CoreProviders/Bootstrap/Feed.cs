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
    using System.Collections.Generic;
    using System.Linq;
    using PackageManagement.Internal.Packaging;
    using PackageManagement.Internal.Utility.Extensions;

    internal class Feed : Swid {
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Feed(BootstrapRequest request, Swidtag swidtag)
            : base(request, swidtag) {
        }

        internal Feed(BootstrapRequest request, IEnumerable<Link> mirrors)
            : base(request, mirrors) {
        }

        internal Feed(BootstrapRequest request, IEnumerable<Uri> mirrors)
            : base(request, mirrors) {
        }

        /// <summary>
        ///     Follows the feed to find all the *declared latest* versions of packages
        /// </summary>
        /// <returns>A set of packages</returns>
        internal IEnumerable<Package> Query() {
            if (!IsValid) {
                return Enumerable.Empty<Package>();
            }

            // first get all the packages that are marked as the latest version.
            var packages = Packages.Select(packageGroup => new Package(_request, packageGroup.Where(link => link.Attributes[Iso19770_2.Discovery.Latest].IsTrue())));

            // then follow any supplemental links to more declared latest packages.
            var morePackages = More.SelectMany(nextGroup => new Feed(_request, nextGroup).Query());

            // We do not follow to other feeds to find more, because declared latest packages should be in this feed (or a supplemental).
            return packages.Concat(morePackages);
        }

        /// <summary>
        ///     Follows the feed to find all versions of package matching 'name'
        /// </summary>
        /// <param name="name">the name or partial name of a package to find</param>
        /// <returns>A set of packages</returns>
        internal IEnumerable<Package> Query(string name) {
            if (!IsValid || string.IsNullOrEmpty(name)) {
                return Enumerable.Empty<Package>();
            }

            // first get all the packages that are in this feed with a matched name
            var packages = PackagesFilteredByName(name).Select(packageGroup => new Package(_request, packageGroup)).Where(package => package.IsValid && package.Name.EqualsIgnoreCase(name));

            // then follow any supplemental links to more declared latest packages.
            var morePackages = More.SelectMany(nextGroup => new Feed(_request, nextGroup).Query(name));

            // let's search child feeds that declare that the name of the package in the feed matches the given name
            var packagesByName = Feeds.Where(feedGroup => feedGroup.Any(link => name.EqualsIgnoreCase(link.Attributes[Iso19770_2.Discovery.Name]))).SelectMany(feed => new Feed(_request, feed).Query(name));

            // and search child feeds that the name would be in their range.
            var packagesByNameRange = Feeds.Where(feedGroup => feedGroup.Any(link => {
                var minName = link.Attributes[Iso19770_2.Discovery.MinimumName];
                var maxName = link.Attributes[Iso19770_2.Discovery.MaximumName];
                if (string.IsNullOrEmpty(minName) || string.IsNullOrEmpty(maxName)) {
                    return false;
                }
                return (String.Compare(minName, name, StringComparison.OrdinalIgnoreCase) <= 0 && String.Compare(name, maxName, StringComparison.OrdinalIgnoreCase) <= 0);
            })).SelectMany(feed => new Feed(_request, feed).Query(name));

            return packages.Concat(morePackages).Concat(packagesByName).Concat(packagesByNameRange);
        }

        /// <summary>
        ///     Follows the feed to find the specific version of a package matching 'name'
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>A set of packages</returns>
        internal IEnumerable<Package> Query(string name, string version) {
            if (string.IsNullOrEmpty(version)) {
                return Query(name);
            }

            if (!IsValid || string.IsNullOrEmpty(name)) {
                return Enumerable.Empty<Package>();
            }

            // first get all the packages that are in this feed with a matched name and version
            var packages = PackagesFilteredByName(name).Select(packageGroup => new Package(_request, packageGroup))
                .Where(package => package.IsValid && package.Name.EqualsIgnoreCase(name) && SoftwareIdentityVersionComparer.CompareVersions(package.VersionScheme, package.Version, version) == 0);

            // then follow any supplemental links to more declared latest packages.
            var morePackages = More.SelectMany(nextGroup => new Feed(_request, nextGroup).Query(name, version));

            // let's search child feeds that declare that the name of the package in the feed matches the given name
            // and the version is either in the specified range of the link, or there is no specified version.
            var packagesByName = Feeds.Where(feedGroup => feedGroup.Any(link => {
                if (name.EqualsIgnoreCase(link.Attributes[Iso19770_2.Discovery.Name])) {
                    var minVer = link.Attributes[Iso19770_2.Discovery.MinimumVersion];
                    if (!string.IsNullOrEmpty(minVer)) {
                        // since we don't know the version scheme at this point, so we just have to guess.
                        if (SoftwareIdentityVersionComparer.CompareVersions(Iso19770_2.VersionScheme.Unknown, minVer, version) > 0) {
                            // the minimum version in the feed is greater than the specified version.
                            return false;
                        }
                    }
                    var maxVer = link.Attributes[Iso19770_2.Discovery.MaximumVersion];
                    if (!string.IsNullOrEmpty(maxVer)) {
                        // since we don't know the version scheme at this point, so we just have to guess.
                        if (SoftwareIdentityVersionComparer.CompareVersions(Iso19770_2.VersionScheme.Unknown, version, maxVer) > 0) {
                            // the given version is greater than the maximum version in the feed.
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            })).SelectMany(feed => new Feed(_request, feed).Query(name, version));

            // and search child feeds that the name would be in their range. 
            // (version matches have to wait till we Query() that feed, since name ranges and version ranges shouldn't be on the same link.)
            var packagesByNameRange = Feeds.Where(feedGroup => feedGroup.Any(link => {
                var minName = link.Attributes[Iso19770_2.Discovery.MinimumName];
                var maxName = link.Attributes[Iso19770_2.Discovery.MaximumName];
                if (string.IsNullOrEmpty(minName) || string.IsNullOrEmpty(maxName)) {
                    return false;
                }
                return (String.Compare(minName, name, StringComparison.OrdinalIgnoreCase) <= 0 && String.Compare(name, maxName, StringComparison.OrdinalIgnoreCase) <= 0);
            })).SelectMany(feed => new Feed(_request, feed).Query(name, version));

            return packages.Concat(morePackages).Concat(packagesByName).Concat(packagesByNameRange);
        }

        /// <summary>
        ///     Follows the feed to find the all versions of a package matching 'name', in the given range
        /// </summary>
        /// <param name="name"></param>
        /// <param name="minimumVersion"></param>
        /// <param name="maximumVersion"></param>
        /// <returns>A set of packages</returns>
        internal IEnumerable<Package> Query(string name, string minimumVersion, string maximumVersion) {
            if (string.IsNullOrEmpty(minimumVersion) && string.IsNullOrEmpty(maximumVersion)) {
                return Query(name);
            }

            if (!IsValid || string.IsNullOrEmpty(name)) {
                return Enumerable.Empty<Package>();
            }

            // first get all the packages that are in this feed with a matched name and version
            var packages = PackagesFilteredByName(name).Select(packageGroup => new Package(_request, packageGroup)).Where(package => {
                if (package.IsValid && package.Name.EqualsIgnoreCase(name)) {
                    if (!string.IsNullOrWhiteSpace(minimumVersion)) {
                        if (SoftwareIdentityVersionComparer.CompareVersions(package.VersionScheme, package.Version, minimumVersion) < 0) {
                            // a minimum version was specified, but the package version is less than the specified minimumversion.
                            return false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(maximumVersion)) {
                        if (SoftwareIdentityVersionComparer.CompareVersions(package.VersionScheme, package.Version, maximumVersion) > 0) {
                            // a maximum version was specified, but the package version is more than the specified maximumversion.
                            return false;
                        }
                    }

                    // the version is in the range asked for.
                    return true;
                }
                // not a valid package, or incorrect name.
                return false;
            });

            // then follow any supplemental links to more declared latest packages.
            var morePackages = More.SelectMany(nextGroup => new Feed(_request, nextGroup).Query(name, minimumVersion, maximumVersion));

            // let's search child feeds that declare that the name of the package in the feed matches the given name
            // and the version is either in the specified range of the link, or there is no specified version.
            var packagesByName = Feeds.Where(feedGroup => feedGroup.Any(link => {
                if (name.EqualsIgnoreCase(link.Attributes[Iso19770_2.Discovery.Name])) {
                    // first, ensure that the requested minimum version is lower than the maximum version found in the feed.
                    var maxVer = link.Attributes[Iso19770_2.Discovery.MaximumVersion];
                    if (!string.IsNullOrEmpty(maxVer)) {
                        // since we don't know the version scheme at this point, so we just have to guess.
                        if (SoftwareIdentityVersionComparer.CompareVersions(Iso19770_2.VersionScheme.Unknown, minimumVersion, maxVer) <= 0) {
                            // the minimum version is greater than the maximum version in the feed.
                            return false;
                        }
                    }

                    // and then ensure that the requested maximum version is greater than the minimum version found in the feed.
                    var minVer = link.Attributes[Iso19770_2.Discovery.MinimumVersion];
                    if (!string.IsNullOrEmpty(minVer)) {
                        // since we don't know the version scheme at this point, so we just have to guess.
                        if (SoftwareIdentityVersionComparer.CompareVersions(Iso19770_2.VersionScheme.Unknown, maximumVersion, minVer) >= 0) {
                            // the maximum version less than the minimum version in the feed.
                            return false;
                        }
                    }

                    return true;
                }
                return false;
            })).SelectMany(feed => new Feed(_request, feed).Query(name, minimumVersion, maximumVersion));

            // and search child feeds that the name would be in their range. 
            // (version matches have to wait till we Query() that feed, since name ranges and version ranges shouldn't be on the same link.)
            var packagesByNameRange = Feeds.Where(feedGroup => feedGroup.Any(link => {
                var minName = link.Attributes[Iso19770_2.Discovery.MinimumName];
                var maxName = link.Attributes[Iso19770_2.Discovery.MaximumName];
                if (string.IsNullOrEmpty(minName) || string.IsNullOrEmpty(maxName)) {
                    return false;
                }
                return (String.Compare(minName, name, StringComparison.OrdinalIgnoreCase) <= 0 && String.Compare(name, maxName, StringComparison.OrdinalIgnoreCase) <= 0);
            })).SelectMany(feed => new Feed(_request, feed).Query(name, minimumVersion, maximumVersion));

            return packages.Concat(morePackages).Concat(packagesByName).Concat(packagesByNameRange);
        }
    }
}