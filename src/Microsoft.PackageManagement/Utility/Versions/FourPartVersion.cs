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

namespace Microsoft.PackageManagement.Internal.Utility.Versions {
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Extensions;

    public struct FourPartVersion : IComparable, IComparable<FourPartVersion>, IEquatable<FourPartVersion> {
        private ulong _version;

        public UInt16 Major {
            get {
                return (UInt16)((_version >> 48) & 0xFFFF);
            }
        }

        public UInt16 Minor {
            get {
                return (UInt16)((_version >> 32) & 0xFFFF);
            }
        }

        public UInt16 Build {
            get {
                return (UInt16)((_version >> 16) & 0xFFFF);
            }
        }

        public UInt16 Revision {
            get {
                return (UInt16)(_version & 0xFFFF);
            }
        }

        public int CompareTo(object obj) {
            return obj is FourPartVersion
                ? _version.CompareTo(((FourPartVersion)obj)._version)
                : obj is TwoPartVersion
                    ? _version.CompareTo(((TwoPartVersion)obj))
                    : obj is ulong
                        ? _version.CompareTo((ulong)obj)
                        : obj is uint
                            ? _version.CompareTo((uint)obj)
                            : obj is string
                                ? _version.CompareTo(((FourPartVersion)(string)obj)._version)
                                : 0;
        }

        public int CompareTo(FourPartVersion other) {
            return _version.CompareTo(other._version);
        }

        public bool Equals(FourPartVersion other) {
            return other._version == _version;
        }

        public override string ToString() {
            return ULongToString(_version);
        }

        public static implicit operator ulong(FourPartVersion version) {
            return version._version;
        }

        public ulong ToULong() {
            return _version;
        }

        public static implicit operator Version(FourPartVersion version) {
            return new Version((int)((version >> 48) & 0xFFFF), (int)((version >> 32) & 0xFFFF), (int)((version >> 16) & 0xFFFF), (int)((version) & 0xFFFF));
        }

        public static implicit operator string(FourPartVersion version) {
            return version.ToString();
        }

        public static implicit operator FourPartVersion(Version version) {
            return new FourPartVersion {
                _version = StringToULong(version.ToString())
               // _version = ((ulong)version.Major << 48) + ((ulong)version.Minor << 32) + ((ulong)version.Build << 16) + (ulong)version.Revision
            };
        }

        public static implicit operator FourPartVersion(ulong version) {
            return new FourPartVersion {
                _version = version
            };
        }

        public static implicit operator FourPartVersion(string version) {
            return new FourPartVersion {
                _version = StringToULong(version)
            };
        }

        public static implicit operator FourPartVersion(DateTime dateAsVersion) {
            return new FourPartVersion {
                _version = (((UInt64)dateAsVersion.Year) << 48) + (((UInt64)dateAsVersion.Month) << 32) + (((UInt64)dateAsVersion.Day) << 16) + (UInt64)dateAsVersion.TimeOfDay.TotalSeconds
            };
        }

        private static string ULongToString(ulong version) {
            return String.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}.{3}", (version >> 48) & 0xFFFF, (version >> 32) & 0xFFFF, (version >> 16) & 0xFFFF, (version) & 0xFFFF);
        }

        private static ulong StringToULong(string version) {
            if (string.IsNullOrWhiteSpace(version)) {
                return 0L;
            }

            var vers = version.Split('.');
            var major = vers.Length > 0 ? vers[0].ToInt32(0) : 0;
            var minor = vers.Length > 1 ? vers[1].ToInt32(0) : 0;
            var build = vers.Length > 2 ? vers[2].ToInt32(0) : 0;
            var revision = vers.Length > 3 ? vers[3].ToInt32(0) : 0;

            if (version.Length <= 3 && version.IndexOf('-') > -1) {
                return (((UInt64)major) << 48) + (((UInt64)minor) << 32) + (((UInt64)build) << 16) - 1;
            }

            return (((UInt64)major) << 48) + (((UInt64)minor) << 32) + (((UInt64)build) << 16) + (UInt64)revision;
        }

        public static implicit operator TwoPartVersion(FourPartVersion version) {
            return ((uint)(version >> 32));
        }

        public static bool operator ==(FourPartVersion a, FourPartVersion b) {
            return a._version == b._version;
        }

        public static bool operator !=(FourPartVersion a, FourPartVersion b) {
            return a._version != b._version;
        }

        public static bool operator <(FourPartVersion a, FourPartVersion b) {
            return a._version < b._version;
        }

        public static bool operator >(FourPartVersion a, FourPartVersion b) {
            return a._version > b._version;
        }

        public static bool operator <=(FourPartVersion a, FourPartVersion b) {
            return a._version <= b._version;
        }

        public static bool operator >=(FourPartVersion a, FourPartVersion b) {
            return a._version >= b._version;
        }

        public override bool Equals(object o) {
            return o is FourPartVersion && Equals((FourPartVersion)o);
        }

        public override int GetHashCode() {
            return _version.GetHashCode();
        }

        public static implicit operator FourPartVersion(FileVersionInfo versionInfo) {
            return new FourPartVersion {
                _version = (ulong)((ulong)(uint)versionInfo.FileMajorPart << 48) | (ulong)((ulong)(uint)versionInfo.FileMinorPart << 32) | (ulong)((ulong)(uint)versionInfo.FileBuildPart << 16) | (ulong)((ulong)(uint)versionInfo.FilePrivatePart)
            };
        }

        public static FourPartVersion FromFileVersionInfo(FileVersionInfo versionInfo) {
            return new FourPartVersion {
                _version = (ulong)((ulong)(uint)versionInfo.FileMajorPart << 48) | (ulong)((ulong)(uint)versionInfo.FileMinorPart << 32) | (ulong)((ulong)(uint)versionInfo.FileBuildPart << 16) | (ulong)((ulong)(uint)versionInfo.FilePrivatePart)
            };
        }

        public static FourPartVersion Parse(string input) {
            return new FourPartVersion {
                _version = StringToULong(input)
            };
        }

        public static bool TryParse(string input, out FourPartVersion ret) {
            ret._version = StringToULong(input);
            return true;
        }
    }
}