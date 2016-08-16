#if !UNIX

namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    public static class Constants
    {
         /// <summary>
        /// Name of this provider
        /// </summary>
        public const string ProviderName = "PSL";

        /// <summary>
        /// Version of this provider
        /// </summary>
        public static readonly string ProviderVersion = "1.0.0.210";

        /// <summary>
        /// Config file storing the info as a result of registering a package source
        /// </summary>
        public static readonly string SettingsFileName = "PSL.config";

        /// <summary>
        /// Sample JSON file containing powershell entry
        /// </summary>
        public static readonly string JSONFileName = "PSL.json";
        public static readonly string CatFileName = "PSL.cat";

        internal static class MediaType
        {
            public const string MsiPackage = "msi";
            public const string MsuPackage = "msu";

            public const string ExePackage = "exe";
            public const string ZipPackage = "zip";
            public const string NuGetPackage = "nupkg";
            public const string AppxPackage = "appx";
            public const string PsArtifacts = "psartifacts";

        }

        internal static class ProviderNames
        {
            public const string PowerShellGet = "PowerShellGet";
            public const string NuGet = "NuGet";
            public const string Msi = "MSI";
            public const string Msu = "MSU";
            public const string Programs = "Programs";
            public const string PSL = ProviderName;

        }

        #region copy common-constants-implementation /internal/public

        internal static string[] Empty = new string[0];
        internal const string MinVersion = "0.0.0.1";
        internal const string MSGPrefix = "MSG:";
        internal const string Download = "Download";
        internal const string Install = "Install";
        public readonly static string[] FeaturePresent = new string[0];
        internal const string CurrentUser = "CurrentUser";
        internal const string AllUsers = "AllUsers";


        internal static class Features
        {
            public const string AutomationOnly = "automation-only";
            public const string MagicSignatures = "magic-signatures";
            public const string SupportedExtensions = "file-extensions";
            public const string SupportedSchemes = "uri-schemes";
            public const string PackageManagementMinimumVersion = "packagemanagement-minimum-version";
            public const string SupportsPowerShellModules = "supports-powershell-modules";
            public const string SupportsRegexSearch = "supports-regex-search";
            public const string SupportsSubstringSearch = "supports-substring-search";
            public const string SupportsWildcardSearch = "supports-wildcard-search";
        }

        internal static class Messages
        {
            public const string UnableToFindDependencyPackage = "MSG:UnableToFindDependencyPackage";
            public const string MissingRequiredParameter = "MSG:MissingRequiredParameter";
            public const string PackageFailedInstallOrDownload = "MSG:PackageFailedInstallOrDownload";
            public const string PackageSourceExists = "MSG:PackageSourceExists";
            public const string ProtocolNotSupported = "MSG:ProtocolNotSupported";
            public const string ProviderPluginLoadFailure = "MSG:ProviderPluginLoadFailure";
            public const string ProviderSwidtagUnavailable = "MSG:ProviderSwidtagUnavailable";
            public const string RemoveEnvironmentVariableRequiresElevation = "MSG:RemoveEnvironmentVariableRequiresElevation";
            public const string SchemeNotSupported = "MSG:SchemeNotSupported";
            public const string SourceLocationNotValid = "MSG:SourceLocationNotValid_Location";         
            public const string UnableToCopyFileTo = "MSG:UnableToCopyFileTo";
            public const string UnableToCreateShortcutTargetDoesNotExist = "MSG:UnableToCreateShortcutTargetDoesNotExist";
            public const string UnableToDownload = "MSG:UnableToDownload";
            public const string UnableToOverwriteExistingFile = "MSG:UnableToOverwriteExistingFile";
            public const string UnableToRemoveFile = "MSG:UnableToRemoveFile";
            public const string UnableToResolvePackage = "MSG:UnableToResolvePackage";
            public const string UnableToResolveSource = "MSG:UnableToResolveSource";
            public const string UnableToUninstallPackage = "MSG:UnableToUninstallPackage";
            public const string UnknownFolderId = "MSG:UnknownFolderId";
            public const string UnknownProvider = "MSG:UnknownProvider";
            public const string UnsupportedArchive = "MSG:UnsupportedArchive";
            public const string UnsupportedProviderType = "MSG:UnsupportedProviderType";
            public const string UriSchemeNotSupported = "MSG:UriSchemeNotSupported";
            public const string UserDeclinedUntrustedPackageInstall = "MSG:UserDeclinedUntrustedPackageInstall";
            public const string HashNotFound = "MSG:HashNotFound";
            public const string HashNotMatch = "MSG:HashNotMatch";
            public const string HashNotSupported = "MSG:HashNotSupported";
            public const string DependencyLoopDetected = "MSG:DependencyLoopDetected";
            public const string CouldNotGetResponseFromQuery = "MSG:CouldNotGetResponseFromQuery";
            public const string SkippedDownloadedPackage = "MSG:SkippedDownloadedPackage";
            public const string InstallRequiresCurrentUserScopeParameterForNonAdminUser = "MSG:InstallRequiresCurrentUserScopeParameterForNonAdminUser";
            
        }

        internal static class OptionType {
            public const string String = "String";
            public const string StringArray = "StringArray";
            public const string Int = "Int";
            public const string Switch = "Switch";
            public const string Folder = "Folder";
            public const string File = "File";
            public const string Path = "Path";
            public const string Uri = "Uri";
            public const string SecureString = "SecureString";
        }

        internal static class Status
        {
            internal const string TimedOut = "TimedOut";
        }

 

        internal static class PackageStatus
        {
            public const string Available = "Available";
            public const string Dependency = "Dependency";
            public const string Installed = "Installed";
            public const string Uninstalled = "Uninstalled";
        }

        internal static class Parameters
        {
            public const string IsUpdate = "IsUpdatePackageSource";
            public const string Name = "Name";
            public const string Location = "Location";
        }

        internal static class Signatures
        {
            public const string Cab = "4D534346";
            public const string OleCompoundDocument = "D0CF11E0A1B11AE1";
            public const string Zip = "504b0304";
            //public static string[] ZipVariants = new[] {Zip, /* should have EXEs? */};
        }

        internal static class SwidTag
        {
            public const string SoftwareIdentity = "SoftwareIdentity";
        }

        #endregion
    }
}

#endif
