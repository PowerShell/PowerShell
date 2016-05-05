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

namespace Microsoft.PackageManagement.Internal.Packaging {
    using System;
    using System.Collections.Generic;
    using PackageManagement.Packaging;
    using Utility.Extensions;

    public class SoftwareIdentityVersionComparer : IComparer<SoftwareIdentity> {
        public static SoftwareIdentityVersionComparer Instance = new SoftwareIdentityVersionComparer();

        public int Compare(SoftwareIdentity x, SoftwareIdentity y) {
            if (x == null || y == null) {
                // can't compare vs null.
                return 0;
            }
            var xVersionScheme = x.VersionScheme ?? string.Empty;
            var yVersionScheme = y.VersionScheme ?? string.Empty;

            if (!x.VersionScheme.EqualsIgnoreCase(yVersionScheme)) {
                // can't compare versions between different version schemes
                return 0;
            }

            return CompareVersions(xVersionScheme, x.Version, y.Version);
        }

        public static int CompareVersions(string versionScheme, string xVersion, string yVersion) {
            xVersion = (xVersion ?? string.Empty).Trim();
            yVersion = (yVersion ?? string.Empty).Trim();

            // regardless of type, if the strings are equal, it's the same version.
            // if we have two nulls or blanks, this works the same
            if (xVersion.Equals(yVersion)) {
                return 0;
            }

            if (xVersion.IsNullOrEmpty()) {
                // yVersion has a value and is always therefore greater than not a value
                return -1;
            }

            if (yVersion.IsNullOrEmpty()) {
                // xVersion has a value and is always therefore greater than not a value
                return 1;
            }

            switch ((versionScheme ?? "unknown").ToLowerInvariant()) {
                case Iso19770_2.VersionScheme.Alphanumeric:
                    // string sort
                    return String.Compare(xVersion, yVersion, StringComparison.Ordinal);

                case Iso19770_2.VersionScheme.Decimal:
                    double xDouble;
                    double yDouble;
                    if (double.TryParse(xVersion, out xDouble) && double.TryParse(yVersion, out yDouble)) {
                        return xDouble.CompareTo(yDouble);
                    }
                    return 0;

                case Iso19770_2.VersionScheme.MultipartNumeric:
                    return CompareMultipartNumeric(xVersion, yVersion);

                case Iso19770_2.VersionScheme.MultipartNumericPlusSuffix:
                    return CompareMultipartNumericSuffix(xVersion, yVersion);

                case Iso19770_2.VersionScheme.SemVer:
                    return CompareSemVer(xVersion, yVersion);

                case Iso19770_2.VersionScheme.Unknown:
                    return GuessComparison(xVersion, yVersion);

                default:
                    return GuessComparison(xVersion, yVersion);
            }
        }

        private static int GuessComparison(string xVersion, string yVersion) {
            // if either one starts with a dot, prepend a zero
            if (xVersion[0] == '.') {
                xVersion = "0" + xVersion;
            }
            if (yVersion[0] == '.') {
                yVersion = "0" + yVersion;
            }

            // if they both start with numbers, we're going to treat them as multipart numeric with suffix.
            if (xVersion.IndexWhere(ch => ch >= '0' && ch <= '9') == 0 && yVersion.IndexWhere(ch => ch >= '0' && ch <= '9') == 0) {
                return CompareMultipartNumericSuffix(xVersion, yVersion);
            }

            // otherwise, we really don't know
            return 0;
        }

        private static int CompareMultipartNumeric(string xVersion, string yVersion) {
            var xs = xVersion.Split('.');
            var ys = yVersion.Split('.');
            var len = Math.Max(xs.Length, ys.Length);
            for (var i = 0; i < len; i++) {
                ulong xLong;
                ulong yLong;

                if (ulong.TryParse(xs.Length > i ? xs[i] : "0", out xLong) && ulong.TryParse(ys.Length > i ? ys[i] : "0", out yLong)) {
                    var compare = xLong.CompareTo(yLong);
                    if (compare != 0) {
                        return compare;
                    }
                    continue;
                }
                return 0;
            }
            return 0;
        }

        private static int CompareMultipartNumericSuffix(string xVersion, string yVersion) {
            var xPos = IndexOfNonNumericWithDots(xVersion);
            var yPos = IndexOfNonNumericWithDots(yVersion);
            var xMulti = xPos == -1 ? xVersion : xVersion.Substring(0, xPos);
            var yMulti = yPos == -1 ? yVersion : yVersion.Substring(0, yPos);
            var compare = CompareMultipartNumeric(xMulti, yMulti);
            if (compare != 0) {
                return compare;
            }

            if (xPos == -1 && yPos == -1) {
                // no suffixes?
                return 0;
            }

            if (xPos == -1) {
                // x has no suffix, y does
                // y is later.
                return -1;
            }

            if (yPos == -1) {
                // x has suffix, y doesn't
                // x is later.
                return 1;
            }

            return String.Compare(xVersion.Substring(xPos), yVersion.Substring(yPos), StringComparison.Ordinal);
        }

        private static int CompareSemVer(string xVersion, string yVersion) {
            var xPos = IndexOfNonNumericWithDots(xVersion);
            var yPos = IndexOfNonNumericWithDots(yVersion);
            var xMulti = xPos == -1 ? xVersion : xVersion.Substring(0, xPos);
            var yMulti = yPos == -1 ? yVersion : yVersion.Substring(0, yPos);
            var compare = CompareMultipartNumeric(xMulti, yMulti);
            if (compare != 0) {
                return compare;
            }

            if (xPos == -1 && yPos == -1) {
                // no suffixes?
                return 0;
            }

            if (xPos == -1) {
                // x has no suffix, y does
                // x is later.
                return 1;
            }

            if (yPos == -1) {
                // x has suffix, y doesn't
                // y is later.
                return -1;
            }

            return String.Compare(xVersion.Substring(xPos), yVersion.Substring(yPos), StringComparison.Ordinal);
        }

        private static int IndexOfNonNumericWithDots(string version) {
            return string.IsNullOrEmpty(version) ? -1 : version.IndexWhere(ch => (ch < '0' || ch > '9') && ch != '.');
        }
    }
}