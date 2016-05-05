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

    public struct TwoPartVersion : IComparable, IComparable<TwoPartVersion>, IEquatable<TwoPartVersion> {
        private uint _version;

        public UInt16 Major {
            get {
                return (UInt16)((_version >> 16) & 0xFFFF);
            }
        }

        public UInt16 Minor {
            get {
                return (UInt16)(_version & 0xFFFF);
            }
        }

        public int CompareTo(object obj) {
#if CORECLR
            uint compare = 0;
            if (obj is TwoPartVersion)
            {
                compare = ((TwoPartVersion)obj)._version;
            }
            else if (obj is uint)
            {
                compare = (uint)obj;
            }
            else if (obj is string)
            {
                compare = ((TwoPartVersion)(string)obj)._version;
            }

            if (_version < compare)
            {
                return -1;
            }
            else if (_version > compare)
            {
                return 1;
            }
            else
            {
                return 0;
            }
#else
            return obj is TwoPartVersion
                ? _version.CompareTo(((TwoPartVersion)obj)._version)
                : obj is FourPartVersion
                    ? _version.CompareTo(((ulong)(FourPartVersion)obj))
                    : obj is ulong
                        ? _version.CompareTo((ulong)obj)
                        : obj is uint
                            ? _version.CompareTo((uint)obj)
                            : obj is string
                                ? _version.CompareTo(((TwoPartVersion)(string)obj)._version)
                                : 0;
#endif
        }

        public int CompareTo(TwoPartVersion other) {
            return _version.CompareTo(other._version);
        }

        public bool Equals(TwoPartVersion other) {
            return other._version == _version;
        }

        public override string ToString() {
            return UIntToString(_version);
        }

        public static implicit operator uint(TwoPartVersion version) {
            return version._version;
        }

        public static implicit operator string(TwoPartVersion version) {
            return version.ToString();
        }

        public static implicit operator TwoPartVersion(uint version) {
            return new TwoPartVersion {
                _version = version
            };
        }

        public static implicit operator TwoPartVersion(string version) {
            return new TwoPartVersion {
                _version = StringToUInt(version)
            };
        }

        private static string UIntToString(uint version) {
            return String.Format(CultureInfo.CurrentCulture, "{0}.{1}", (version >> 16) & 0xFFFF, (version) & 0xFFFF);
        }

        private static uint StringToUInt(string version) {
            if (string.IsNullOrWhiteSpace(version)) {
                return 0;
            }

            var vers = version.Split('.');
            var major = vers.Length > 0 ? vers[0].ToInt32(0) : 0;
            var minor = vers.Length > 1 ? vers[1].ToInt32(0) : 0;

            return (((uint)major) << 16) + (uint)minor;
        }

        public static implicit operator FourPartVersion(TwoPartVersion version) {
            return ((ulong)version) << 32;
        }

        public static bool operator ==(TwoPartVersion a, TwoPartVersion b) {
            return a._version == b._version;
        }

        public static bool operator !=(TwoPartVersion a, TwoPartVersion b) {
            return a._version != b._version;
        }

        public static bool operator <(TwoPartVersion a, TwoPartVersion b) {
            return a._version < b._version;
        }

        public static bool operator >(TwoPartVersion a, TwoPartVersion b) {
            return a._version > b._version;
        }

        public static bool operator <=(TwoPartVersion a, TwoPartVersion b) {
            return a._version <= b._version;
        }

        public static bool operator >=(TwoPartVersion a, TwoPartVersion b) {
            return a._version >= b._version;
        }

        public override bool Equals(object o) {
            return o is TwoPartVersion && Equals((TwoPartVersion)o);
        }

        public override int GetHashCode() {
            return (int)_version;
        }

        public static implicit operator TwoPartVersion(FileVersionInfo versionInfo) {
            return new TwoPartVersion {
                _version = ((uint)versionInfo.FileMajorPart << 16) | (uint)versionInfo.FileMinorPart
            };
        }

        public static TwoPartVersion Parse(string input) {
            return new TwoPartVersion {
                _version = StringToUInt(input)
            };
        }

        public static bool TryParse(string input, out TwoPartVersion ret) {
            ret._version = StringToUInt(input);
            return true;
        }
    }
}