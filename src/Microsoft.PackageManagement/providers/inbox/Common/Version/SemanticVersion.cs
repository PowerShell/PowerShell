using MMicrosoft.PackageManagement.Provider.Utility;

namespace Microsoft.PackageManagement.Provider.Utility
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// 
    /// Example: EntityFramework 6.1.3-beta1 
    /// Version: 6.1.3
    /// SpecialVersion: -beta1 
    /// OriginalString: 6.1.3-beta1 
    /// 
    /// </summary>    
    [TypeConverter(typeof(SemanticVersionTypeConverter))]
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private const RegexOptions _flags =  RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
        internal const string AllowFourPartsVersion = @"(?<Version>\d+(\s*\.\s*\d+){0,3})";
        internal const string ThreePartsVersion = @"(?<Version>\d+(\.\d+){2})";
        // the pre release regex is of the form -<pre-release version> where <pre-release> version is set of identifier delimited by ".". Each identifer can be any characters in [A-z0-9a-z-]
        internal const string ReleasePattern = @"(?<Release>-[A-Z0-9a-z\-]+(\.[A-Z0-9a-z\-]+)*)?";
        // The build regex is of the same form except with a + instead of -
        internal const string BuildPattern = @"(?<Build>\+[A-Z0-9a-z\-]+(\.[A-Z0-9a-z\-]+)*)?";
        internal static string SemanticVersionPattern = String.Concat("^", AllowFourPartsVersion, ReleasePattern, BuildPattern, "$");

        private static readonly Regex _semanticVersionRegex = new Regex(SemanticVersionPattern, _flags);
        private static readonly Regex _strictSemanticVersionRegex = new Regex(@"^(?<Version>\d+(\.\d+){2})(?<Release>-[A-Z0-9a-z\-]+(\.[A-Z0-9a-z\-]+)*)?(?<Build>\+[A-Z0-9a-z\-]+(\.[A-Z0-9a-z\-]+)*)?$", _flags);
        private readonly string _originalString;

        public SemanticVersion(string version)  
            : this(Parse(version))
        {
            // The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it. 
            // The original string represents the original form in which the version is represented to be used when printing.
            _originalString = version;
        }

        public SemanticVersion(int major, int minor, int build, int revision)
            : this(new Version(major, minor, build, revision))
        {
        }

        public SemanticVersion(int major, int minor, int build, string specialVersion)
            : this(new Version(major, minor, build), specialVersion)
        {
        }

        public SemanticVersion(Version version)
            : this(version, String.Empty)
        {
        }

        public SemanticVersion(Version version, string specialVersion)
            : this(version, specialVersion, null)
        {
        }

        private SemanticVersion(Version version, string specialVersion, string originalString)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? String.Empty;
            _originalString = String.IsNullOrWhiteSpace(originalString) ? (version + (!String.IsNullOrWhiteSpace(specialVersion) ? '-' + specialVersion : null)) : originalString;
        }

        public SemanticVersion(SemanticVersion semVer)
        {
            if (semVer == null) {
                throw new ArgumentNullException("semVer");
            }
            _originalString = semVer.ToString();
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
        }

        /// <summary>
        /// Gets the normalized version portion.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the optional special version.
        /// </summary>
        public string SpecialVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// Get an array of the major, minor, build, and revision.
        /// </summary>
        /// <returns></returns>
        public string[] GetOriginalVersionComponents()
        {
            if (!String.IsNullOrWhiteSpace(_originalString))
            {
                string original;

                // search the start of the SpecialVersion part, if any
                int dashIndex = _originalString.IndexOf('-');
                if (dashIndex != -1)
                {
                    // remove the SpecialVersion part
                    original = _originalString.Substring(0, dashIndex);
                }
                else
                {
                    original = _originalString;
                }

                return SplitAndPadVersionString(original);
            }
            else
            {
                return SplitAndPadVersionString(Version.ToString());
            }
        }

        private static string[] SplitAndPadVersionString(string version)
        {
            string[] a = version.Split('.');
            if (a.Length == 4)
            {
                return a;
            }
            else
            {
                // if 'a' has less than 4 elements, we pad the '0' at the end 
                // to make it 4.
                var b = new string[4] { "0", "0", "0", "0" };
                Array.Copy(a, 0, b, 0, a.Length);
                return b;
            }
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static SemanticVersion Parse(string version)
        {
            if (String.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentNullException("version");
            }

            SemanticVersion semVer;
            if (!TryParse(version, out semVer))
            {
                return null;
            }

            return semVer;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _semanticVersionRegex, out value);
        }

        /// <summary>
        /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
        /// </summary>
        public static bool TryParseStrict(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _strictSemanticVersionRegex, out value);
        }

        private static bool TryParseInternal(string version, Regex regex, out SemanticVersion semVer)
        {
            semVer = null;
            if (String.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            var match = regex.Match(version.Trim());
            Version versionValue;
            if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out versionValue))
            {
                return false;
            }

            semVer = new SemanticVersion(NormalizeVersionValue(versionValue), match.Groups["Release"].Value.TrimStart('-'), version.Replace(" ", ""));
            return true;
        }

        private static Version NormalizeVersionValue(Version version)
        {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 1;
            }
            SemanticVersion other = obj as SemanticVersion;
            if (other == null)
            {
                throw new ArgumentNullException("obj");                    
            }
            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0)
            {
                return result;
            }

            bool empty = String.IsNullOrWhiteSpace(SpecialVersion);
            bool otherEmpty = String.IsNullOrWhiteSpace(other.SpecialVersion);
            if (empty && otherEmpty)
            {
                return 0;
            }
            else if (empty)
            {
                return 1;
            }
            else if (otherEmpty)
            {
                return -1;
            }
            return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2)
        {
            if (ReferenceEquals(version1, null))
            {
                return ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
        {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
        {
            return (version1 == version2) || (version1 > version2);
        }

        public override string ToString()
        {
            return _originalString;
        }

        public bool Equals(SemanticVersion other)
        {
            if (other == null)
            {
                return ReferenceEquals(Version, null);
            }

            return Version.Equals(other.Version) &&
                   SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            SemanticVersion semVer = obj as SemanticVersion;
            return !ReferenceEquals(null, semVer) && Equals(semVer);
        }

        public override int GetHashCode()
        {
            int hashCode = Version.GetHashCode();
            if (SpecialVersion != null)
            {
                hashCode = hashCode * 4567 + SpecialVersion.GetHashCode();
            }

            return hashCode;
        }
    }
}
