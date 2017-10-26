/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Management.Automation.Internal;
using Microsoft.Win32;

namespace System.Management.Automation
{
    /// <summary>
    /// Encapsulates $PSVersionTable.
    /// </summary>
    internal class PSVersionInfo
    {
        internal const string PSVersionTableName = "PSVersionTable";
        internal const string PSRemotingProtocolVersionName = "PSRemotingProtocolVersion";
        internal const string PSVersionName = "PSVersion";
        internal const string PSEditionName = "PSEdition";
        internal const string PSGitCommitIdName = "GitCommitId";
        internal const string PSCompatibleVersionsName = "PSCompatibleVersions";
        internal const string PSPlatformName = "Platform";
        internal const string PSOSName = "OS";
        internal const string SerializationVersionName = "SerializationVersion";
        internal const string WSManStackVersionName = "WSManStackVersion";
        private static PSVersionHashTable s_psVersionTable = null;

        /// <summary>
        /// A constant to track current PowerShell Version.
        /// </summary>
        /// <remarks>
        /// We can't depend on assembly version for PowerShell version.
        ///
        /// This is why we hard code the PowerShell version here.
        ///
        /// For each later release of PowerShell, this constant needs to
        /// be updated to reflect the right version.
        /// </remarks>
        private static Version s_psV1Version = new Version(1, 0);
        private static Version s_psV2Version = new Version(2, 0);
        private static Version s_psV3Version = new Version(3, 0);
        private static Version s_psV4Version = new Version(4, 0);
        private static Version s_psV5Version = new Version(5, 0);
        private static Version s_psV51Version = new Version(5, 1, NTVerpVars.PRODUCTBUILD, NTVerpVars.PRODUCTBUILD_QFE);
        private static SemanticVersion s_psV6Version;

        /// <summary>
        /// A constant to track current PowerShell Edition
        /// </summary>
        internal const string PSEditionValue = "Core";

        // Static Constructor.
        static PSVersionInfo()
        {
            s_psVersionTable = new PSVersionHashTable(StringComparer.OrdinalIgnoreCase);

            string assemblyPath = typeof(PSVersionInfo).Assembly.Location;
            string productVersion = FileVersionInfo.GetVersionInfo(assemblyPath).ProductVersion;

            // Get 'GitCommitId' and 'PSVersion' from the 'productVersion' assembly attribute.
            //
            // The strings can be one of the following format examples:
            //    when powershell is built from a commit:
            //      productVersion = '6.0.0-beta.7 Commits: 29 SHA: 52c6b...' convert to GitCommitId = 'v6.0.0-beta.7-29-g52c6b...'
            //                                                                           PSVersion   = '6.0.0-beta.7'
            //    when powershell is built from a release tag:
            //      productVersion = '6.0.0-beta.7 SHA: f1ec9...'             convert to GitCommitId = 'v6.0.0-beta.7'
            //                                                                           PSVersion   = '6.0.0-beta.7'
            //    when powershell is built from a release tag for RTM:
            //      productVersion = '6.0.0 SHA: f1ec9...'                    convert to GitCommitId = 'v6.0.0'
            //                                                                           PSVersion   = '6.0.0'
            string rawGitCommitId;
            string mainVersion = productVersion.Substring(0, productVersion.IndexOf(' '));

            if (productVersion.Contains(" Commits: "))
            {
                rawGitCommitId = "v" + productVersion.Replace(" Commits: ", "-").Replace(" SHA: ", "-g");
            }
            else
            {
                rawGitCommitId = "v" + mainVersion;
            }

            s_psV6Version = new SemanticVersion(mainVersion);

            s_psVersionTable[PSVersionInfo.PSVersionName] = s_psV6Version;
            s_psVersionTable[PSVersionInfo.PSEditionName] = PSEditionValue;
            s_psVersionTable[PSGitCommitIdName] = rawGitCommitId;
            s_psVersionTable[PSCompatibleVersionsName] = new Version[] { s_psV1Version, s_psV2Version, s_psV3Version, s_psV4Version, s_psV5Version, s_psV51Version, s_psV6Version };
            s_psVersionTable[PSVersionInfo.SerializationVersionName] = new Version(InternalSerializer.DefaultVersion);
            s_psVersionTable[PSVersionInfo.PSRemotingProtocolVersionName] = RemotingConstants.ProtocolVersion;
            s_psVersionTable[PSVersionInfo.WSManStackVersionName] = GetWSManStackVersion();
            s_psVersionTable[PSPlatformName] = Environment.OSVersion.Platform.ToString();
            s_psVersionTable[PSOSName] = Runtime.InteropServices.RuntimeInformation.OSDescription.ToString();
        }

        internal static PSVersionHashTable GetPSVersionTable()
        {
            return s_psVersionTable;
        }

        internal static Hashtable GetPSVersionTableForDownLevel()
        {
            var result = (Hashtable)s_psVersionTable.Clone();
            // Downlevel systems don't support SemanticVersion, but Version is most likely good enough anyway.
            result[PSVersionInfo.PSVersionName] = (Version)(SemanticVersion)s_psVersionTable[PSVersionInfo.PSVersionName];
            return result;
        }

        #region Private helper methods

        // Gets the current WSMan stack version from the registry.
        private static Version GetWSManStackVersion()
        {
            Version version = null;

#if !UNIX
            try
            {
                using (RegistryKey wsManStackVersionKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\WSMAN"))
                {
                    if (wsManStackVersionKey != null)
                    {
                        object wsManStackVersionObj = wsManStackVersionKey.GetValue("ServiceStackVersion");
                        string wsManStackVersion = (wsManStackVersionObj != null) ? (string)wsManStackVersionObj : null;
                        if (!string.IsNullOrEmpty(wsManStackVersion))
                        {
                            version = new Version(wsManStackVersion.Trim());
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }
#endif

            return version ?? System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STACK_VERSION;
        }

        #endregion

        #region Programmer APIs

        internal static Version PSVersion
        {
            get
            {
                return (SemanticVersion)GetPSVersionTable()[PSVersionInfo.PSVersionName];
            }
        }

        internal static string GitCommitId
        {
            get
            {
                return (string)GetPSVersionTable()[PSGitCommitIdName];
            }
        }

        internal static Version[] PSCompatibleVersions
        {
            get
            {
                return (Version[])GetPSVersionTable()[PSCompatibleVersionsName];
            }
        }

        internal static string PSEdition
        {
            get
            {
                return (string)GetPSVersionTable()[PSVersionInfo.PSEditionName];
            }
        }

        internal static Version SerializationVersion
        {
            get
            {
                return (Version)GetPSVersionTable()[SerializationVersionName];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// For 2.0 PowerShell, we still use "1" as the registry version key.
        /// For >=3.0 PowerShell, we still use "1" as the registry version key for
        /// Snapin and Custom shell lookup/discovery.
        /// </remarks>
        internal static string RegistryVersion1Key
        {
            get
            {
                return "1";
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// For 3.0 PowerShell, we use "3" as the registry version key only for Engine
        /// related data like ApplicationBase.
        /// For 3.0 PowerShell, we still use "1" as the registry version key for
        /// Snapin and Custom shell lookup/discovery.
        /// </remarks>
        internal static string RegistryVersionKey
        {
            get
            {
                // PowerShell >=4 is compatible with PowerShell 3 and hence reg key is 3.
                return "3";
            }
        }


        internal static string GetRegistryVersionKeyForSnapinDiscovery(string majorVersion)
        {
            int tempMajorVersion = 0;
            LanguagePrimitives.TryConvertTo<int>(majorVersion, out tempMajorVersion);

            if ((tempMajorVersion >= 1) && (tempMajorVersion <= PSVersionInfo.PSVersion.Major))
            {
                // PowerShell version 3 took a dependency on CLR4 and went with:
                // SxS approach in GAC/Registry and in-place upgrade approach for
                // FileSystem.
                // For >=3.0 PowerShell, we still use "1" as the registry version key for
                // Snapin and Custom shell lookup/discovery.
                return "1";
            }

            return null;
        }

        internal static string FeatureVersionString
        {
            get
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", PSVersionInfo.PSVersion.Major, PSVersionInfo.PSVersion.Minor);
            }
        }

        internal static bool IsValidPSVersion(Version version)
        {
            if (version.Major == s_psV6Version.Major)
            {
                return version.Minor == s_psV6Version.Minor;
            }
            if (version.Major == s_psV5Version.Major)
            {
                return (version.Minor == s_psV5Version.Minor || version.Minor == s_psV51Version.Minor);
            }
            if (version.Major == s_psV4Version.Major)
            {
                return (version.Minor == s_psV4Version.Minor);
            }
            else if (version.Major == s_psV3Version.Major)
            {
                return version.Minor == s_psV3Version.Minor;
            }
            else if (version.Major == s_psV2Version.Major)
            {
                return version.Minor == s_psV2Version.Minor;
            }
            else if (version.Major == s_psV1Version.Major)
            {
                return version.Minor == s_psV1Version.Minor;
            }

            return false;
        }

        internal static Version PSV4Version
        {
            get { return s_psV4Version; }
        }

        internal static Version PSV5Version
        {
            get { return s_psV5Version; }
        }

        internal static Version PSV51Version
        {
            get { return s_psV51Version; }
        }

        internal static SemanticVersion PSV6Version
        {
            get { return s_psV6Version; }
        }

        #endregion
    }

    /// <summary>
    /// Represents an implementation of '$PSVersionTable' variable.
    /// The implementation contains ordered 'Keys' and 'GetEnumerator' to get user-friendly output.
    /// </summary>
    public sealed class PSVersionHashTable : Hashtable, IEnumerable
    {
        private static readonly PSVersionTableComparer s_keysComparer = new PSVersionTableComparer();
        internal PSVersionHashTable(IEqualityComparer equalityComparer) : base(equalityComparer)
        {
        }

        /// <summary>
        /// Returns ordered collection with Keys of 'PSVersionHashTable'
        /// We want see special order:
        ///     1. PSVersionName
        ///     2. PSEditionName
        ///     3. Remaining properties in alphabetical order
        /// </summary>
        public override ICollection Keys
        {
            get
            {
                ArrayList keyList = new ArrayList(base.Keys);
                keyList.Sort(s_keysComparer);
                return keyList;
            }
        }

        private class PSVersionTableComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                string xString = (string)LanguagePrimitives.ConvertTo(x, typeof(string), CultureInfo.CurrentCulture);
                string yString = (string)LanguagePrimitives.ConvertTo(y, typeof(string), CultureInfo.CurrentCulture);
                if (PSVersionInfo.PSVersionName.Equals(xString, StringComparison.OrdinalIgnoreCase))
                {
                    return -1;
                }
                else if (PSVersionInfo.PSVersionName.Equals(yString, StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
                else if (PSVersionInfo.PSEditionName.Equals(xString, StringComparison.OrdinalIgnoreCase))
                {
                    return -1;
                }
                else if (PSVersionInfo.PSEditionName.Equals(yString, StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
                else
                {
                    return String.Compare(xString, yString, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator for 'PSVersionHashTable'.
        /// The enumeration is ordered (based on ordered version of 'Keys').
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (object key in Keys)
            {
                yield return new DictionaryEntry(key, this[key]);
            }
        }
    }

    /// <summary>
    /// An implementation of semantic versioning (http://semver.org)
    /// that can be converted to/from <see cref="System.Version"/>.
    ///
    /// When converting to <see cref="Version"/>, a PSNoteProperty is
    /// added to the instance to store the semantic version label so
    /// that it can be recovered when creating a new SemanticVersion.
    /// </summary>
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        /// <summary>
        /// Construct a SemanticVersion from a string.
        /// </summary>
        /// <param name="version">The version to parse</param>
        /// <exception cref="PSArgumentException"></exception>
        /// <exception cref="ValidationMetadataException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="OverflowException"></exception>
        public SemanticVersion(string version)
        {
            var v = SemanticVersion.Parse(version);

            Major = v.Major;
            Minor = v.Minor;
            Patch = v.Patch;
            Label = v.Label;
        }

        /// <summary>
        /// Construct a SemanticVersion.
        /// </summary>
        /// <param name="major">The major version</param>
        /// <param name="minor">The minor version</param>
        /// <param name="patch">The minor version</param>
        /// <param name="label">The label for the version</param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="major"/>, <paramref name="minor"/>, or <paramref name="patch"/> is less than 0.
        /// </exception>
        /// <exception cref="PSArgumentNullException">
        /// If <paramref name="label"/> is null or an empty string.
        /// </exception>
        public SemanticVersion(int major, int minor, int patch, string label)
            : this(major, minor, patch)
        {
            if (string.IsNullOrEmpty(label)) throw PSTraceSource.NewArgumentNullException(nameof(label));

            Label = label;
        }

        /// <summary>
        /// Construct a SemanticVersion.
        /// </summary>
        /// <param name="major">The major version</param>
        /// <param name="minor">The minor version</param>
        /// <param name="patch">The minor version</param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="major"/>, <paramref name="minor"/>, or <paramref name="patch"/> is less than 0.
        /// </exception>
        public SemanticVersion(int major, int minor, int patch)
        {
            if (major < 0) throw PSTraceSource.NewArgumentException(nameof(major));
            if (minor < 0) throw PSTraceSource.NewArgumentException(nameof(minor));
            if (patch < 0) throw PSTraceSource.NewArgumentException(nameof(patch));

            Major = major;
            Minor = minor;
            Patch = patch;
            Label = null;
        }

        /// <summary>
        /// Construct a SemanticVersion.
        /// </summary>
        /// <param name="major">The major version</param>
        /// <param name="minor">The minor version</param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="major"/> or <paramref name="minor"/> is less than 0.
        /// </exception>
        public SemanticVersion(int major, int minor) : this(major, minor, 0) {}

        /// <summary>
        /// Construct a SemanticVersion.
        /// </summary>
        /// <param name="major">The major version</param>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="major"/> is less than 0.
        /// </exception>
        public SemanticVersion(int major) : this(major, 0, 0) {}

        private const string LabelPropertyName = "PSSemanticVersionLabel";

        /// <summary>
        /// Construct a <see cref="SemanticVersion"/> from a <see cref="Version"/>,
        /// copying the NoteProperty storing the label if the expected property exists.
        /// </summary>
        /// <param name="version">The version.</param>
        public SemanticVersion(Version version)
        {
            if (version.Revision > 0) throw PSTraceSource.NewArgumentException(nameof(version));

            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build == -1 ? 0 : version.Build;
            var psobj = new PSObject(version);
            var labelNote = psobj.Properties[LabelPropertyName];
            if (labelNote != null)
            {
                Label = labelNote.Value as string;
            }
        }

        /// <summary>
        /// Convert a <see cref="SemanticVersion"/> to a <see cref="Version"/>.
        /// If there is a <see cref="Label"/>, it is added as a NoteProperty to the
        /// result so that you can round trip back to a <see cref="SemanticVersion"/>
        /// without losing the label.
        /// </summary>
        /// <param name="semver"></param>
        public static implicit operator Version(SemanticVersion semver)
        {
            var result = new Version(semver.Major, semver.Minor, semver.Patch);

            if (!string.IsNullOrEmpty(semver.Label))
            {
                var psobj = new PSObject(result);
                psobj.Properties.Add(new PSNoteProperty(LabelPropertyName, semver.Label));
            }

            return result;
        }

        /// <summary>
        /// The major version number, never negative.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// The minor version number, never negative.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// The patch version, -1 if not specified.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// The last component in a SemanticVersion - may be null if not specified.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Parse <paramref name="version"/> and return the result if it is a valid <see cref="SemanticVersion"/>, otherwise throws an exception.
        /// </summary>
        /// <param name="version">The string to parse</param>
        /// <returns></returns>
        /// <exception cref="PSArgumentException"></exception>
        /// <exception cref="ValidationMetadataException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="OverflowException"></exception>
        public static SemanticVersion Parse(string version)
        {
            if (version == null) throw PSTraceSource.NewArgumentNullException(nameof(version));

            var r = new VersionResult();
            r.Init(true);
            TryParseVersion(version, ref r);

            return r._parsedVersion;
        }

        /// <summary>
        /// Parse <paramref name="version"/> and return true if it is a valid <see cref="SemanticVersion"/>, otherwise return false.
        /// No exceptions are raised.
        /// </summary>
        /// <param name="version">The string to parse</param>
        /// <param name="result">The return value when the string is a valid <see cref="SemanticVersion"/></param>
        public static bool TryParse(string version, out SemanticVersion result)
        {
            if (version != null)
            {
                var r = new VersionResult();
                r.Init(false);

                if (TryParseVersion(version, ref r))
                {
                    result = r._parsedVersion;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryParseVersion(string version, ref VersionResult result)
        {
            var dashIndex = version.IndexOf('-');

            // Empty label?
            if (dashIndex == version.Length - 1)
            {
                result.SetFailure(ParseFailureKind.ArgumentException);
                return false;
            }

            var versionSansLabel = (dashIndex < 0) ? version : version.Substring(0, dashIndex);
            string[] parsedComponents = versionSansLabel.Split(Utils.Separators.Dot);
            if (parsedComponents.Length > 3)
            {
                result.SetFailure(ParseFailureKind.ArgumentException);
                return false;
            }

            int major = 0, minor = 0, patch = 0;
            if (!TryParseComponent(parsedComponents[0], "major", ref result, out major))
            {
                return false;
            }

            if (parsedComponents.Length >= 2 && !TryParseComponent(parsedComponents[1], "minor", ref result, out minor))
            {
                return false;
            }

            if (parsedComponents.Length == 3 && !TryParseComponent(parsedComponents[2], "patch", ref result, out patch))
            {
                return false;
            }

            result._parsedVersion = dashIndex < 0
                ? new SemanticVersion(major, minor, patch)
                : new SemanticVersion(major, minor, patch, version.Substring(dashIndex + 1));
            return true;
        }

        private static bool TryParseComponent(string component, string componentName, ref VersionResult result, out int parsedComponent)
        {
            if (!Int32.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent))
            {
                result.SetFailure(ParseFailureKind.FormatException, component);
                return false;
            }

            if (parsedComponent < 0)
            {
                result.SetFailure(ParseFailureKind.ArgumentOutOfRangeException, componentName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            if (Patch < 0)
            {
                return string.IsNullOrEmpty(Label)
                    ? StringUtil.Format("{0}.{1}", Major, Minor)
                    : StringUtil.Format("{0}.{1}-{2}", Major, Minor, Label);
            }

            return string.IsNullOrEmpty(Label)
                ? StringUtil.Format("{0}.{1}.{2}", Major, Minor, Patch)
                : StringUtil.Format("{0}.{1}.{2}-{3}", Major, Minor, Patch, Label);
        }

        /// <summary>
        /// Implement <see cref="IComparable.CompareTo"/>
        /// </summary>
        public int CompareTo(object version)
        {
            if (version == null)
            {
                return 1;
            }

            var v = version as SemanticVersion;
            if (v == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(version));
            }

            return CompareTo(v);
        }

        /// <summary>
        /// Implement <see cref="IComparable{T}.CompareTo"/>
        /// </summary>
        public int CompareTo(SemanticVersion value)
        {
            if ((object)value == null)
                return 1;

            if (Major != value.Major)
                return Major > value.Major ? 1 : -1;

            if (Minor != value.Minor)
                return Minor > value.Minor ? 1 : -1;

            if (Patch != value.Patch)
                return Patch > value.Patch ? 1 : -1;

            if (Label == null)
                return value.Label == null ? 0 : 1;

            if (value.Label == null)
                return -1;

            if (!string.Equals(Label, value.Label, StringComparison.Ordinal))
                return string.Compare(Label, value.Label, StringComparison.Ordinal);

            return 0;
        }

        /// <summary>
        /// Override <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersion);
        }

        /// <summary>
        /// Implement <see cref="IEquatable{T}.Equals(T)"/>
        /// </summary>
        public bool Equals(SemanticVersion other)
        {
            return other != null &&
                   (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch) &&
                   string.Equals(Label, other.Label, StringComparison.Ordinal);
        }

        /// <summary>
        /// Override <see cref="object.GetHashCode()"/>
        /// </summary>
        public override int GetHashCode()
        {
            return Utils.CombineHashCodes(
                Major.GetHashCode(),
                Minor.GetHashCode(),
                Patch.GetHashCode(),
                Label == null ? 0 : Label.GetHashCode());
        }

        /// <summary>
        /// Overloaded == operator
        /// </summary>
        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (object.ReferenceEquals(v1, null))
            {
                return object.ReferenceEquals(v2, null);
            }

            return v1.Equals(v2);
        }

        /// <summary>
        /// Overloaded != operator
        /// </summary>
        public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
        {
            return !(v1 == v2);
        }

        /// <summary>
        /// Overloaded &lt; operator
        /// </summary>
        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            if ((object)v1 == null) throw PSTraceSource.NewArgumentException(nameof(v1));
            return (v1.CompareTo(v2) < 0);
        }

        /// <summary>
        /// Overloaded &lt;= operator
        /// </summary>
        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            if ((object)v1 == null) throw PSTraceSource.NewArgumentException(nameof(v1));
            return (v1.CompareTo(v2) <= 0);
        }

        /// <summary>
        /// Overloaded &gt; operator
        /// </summary>
        public static bool operator >(SemanticVersion v1, SemanticVersion v2)
        {
            return (v2 < v1);
        }

        /// <summary>
        /// Overloaded &gt;= operator
        /// </summary>
        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            return (v2 <= v1);
        }

        internal enum ParseFailureKind
        {
            ArgumentException,
            ArgumentOutOfRangeException,
            FormatException
        }

        internal struct VersionResult
        {
            internal SemanticVersion _parsedVersion;
            internal ParseFailureKind _failure;
            internal string _exceptionArgument;
            internal bool _canThrow;

            internal void Init(bool canThrow)
            {
                _canThrow = canThrow;
            }

            internal void SetFailure(ParseFailureKind failure)
            {
                SetFailure(failure, String.Empty);
            }

            internal void SetFailure(ParseFailureKind failure, string argument)
            {
                _failure = failure;
                _exceptionArgument = argument;
                if (_canThrow)
                {
                    throw GetVersionParseException();
                }
            }

            internal Exception GetVersionParseException()
            {
                switch (_failure)
                {
                    case ParseFailureKind.ArgumentException:
                        return PSTraceSource.NewArgumentException("version");
                    case ParseFailureKind.ArgumentOutOfRangeException:
                        throw new ValidationMetadataException("ValidateRangeTooSmall",
                            null, Metadata.ValidateRangeSmallerThanMinRangeFailure,
                            _exceptionArgument, "0");
                    case ParseFailureKind.FormatException:
                        // Regenerate the FormatException as would be thrown by Int32.Parse()
                        try
                        {
                            Int32.Parse(_exceptionArgument, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException e)
                        {
                            return e;
                        }
                        catch (OverflowException e)
                        {
                            return e;
                        }
                        break;
                }
                return PSTraceSource.NewArgumentException("version");
            }
        }
    }
}
