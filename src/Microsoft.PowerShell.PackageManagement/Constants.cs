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

namespace Microsoft.PowerShell.PackageManagement {
    using System.Management.Automation;
    using Utility;

    internal static class Constants {
        internal const int DefaultTimeout = 60*60; // 60 minutes
        // todo: setting responsiveness to 15 minutes until we're sure
        // that it works right
        internal const int DefaultResponsiveness = 15 * 60 ; // 30 seconds
        // internal const int DefaultResponsiveness = 30; // 30 seconds

        // cmdlet naming/etc

        internal class ParameterSets {

            // To avoid the FxCop complains: CA1812: Avoid uninstantiated internal 
            private ParameterSets() {
            }

            internal const string PackageBySearchSet = "PackageBySearch";
            internal const string PackageByInputObjectSet = "PackageByInputObject";
            internal const string SourceByInputObjectSet = "SourceByInputObject";
            internal const string SourceBySearchSet = "SourceBySearch";

            internal const string DestinationPathSet = "DestinationPath";
            internal const string LiteralPathSet = "LiteralPath";
        }

        internal const string AssemblyProviderType = "assembly";
        internal static object[] NoParameters = new object[0];
        internal const string InstallPackageTrace = "Microsoft.Windows.PowerShell.PackageManagement_InstallPackage";
        internal const string UnInstallPackageTrace = "Microsoft.Windows.PowerShell.PackageManagement_UnInstallPackage";
        internal const string SavePackageTrace = "Microsoft.Windows.PowerShell.PackageManagement_InstallPackage";

        internal const string PowerShellProviderName = "Microsoft-Windows-PowerShell";
        internal const string NewWinEvent = @"Microsoft.PowerShell.Diagnostics\New-WinEvent";

        // messages

        // Implementation Note:
        // Because PackageManagement allows the application layer closest to the user (host) to be in ultimate
        // control of specifying messages to the end user, and falls back up the chain of responsibility
        // when resolving Messages from resources, we have prefixed the constants with MSG: in order
        // to *know* when we're trying to resolve a message.

        // As an optimization step, if the MSG: isn't present, then the application layer need not bother
        // resolving the message (nor asking up the chain) since it's not a message id, but rather an
        // already resolved string.

        internal static class Errors {
            // ReSharper disable InconsistentNaming
            public static ErrorMessage DestinationOrLiteralPathNotSpecified = new ErrorMessage(Messages.DestinationOrLiteralPathRequired, ErrorCategory.InvalidArgument);
            public static ErrorMessage DestinationPathInvalid = new ErrorMessage(Messages.DestinationPathInvalid, ErrorCategory.InvalidArgument);
            public static ErrorMessage DisambiguateForInstall = new ErrorMessage(Messages.DisambiguateForInstall, ErrorCategory.InvalidArgument);
            public static ErrorMessage DisambiguateForUninstall = new ErrorMessage(Messages.DisambiguateForUninstall, ErrorCategory.InvalidArgument);
            public static ErrorMessage InstallationFailure = new ErrorMessage(Messages.InstallationFailure, ErrorCategory.InvalidOperation);
            public static ErrorMessage MatchesMultipleProviders = new ErrorMessage(Messages.MatchesMultipleProviders, ErrorCategory.InvalidArgument);
            public static ErrorMessage MustSpecifyCriteria = new ErrorMessage(Messages.MustSpecifyCriteria, ErrorCategory.InvalidArgument);
            public static ErrorMessage NameOrLocationRequired = new ErrorMessage(Messages.NameOrLocationRequired, ErrorCategory.InvalidArgument);
            public static ErrorMessage NoMatchForProvidersAndSources = new ErrorMessage(Messages.NoMatchForProvidersAndSources, ErrorCategory.InvalidArgument);
            public static ErrorMessage NoMatchFound = new ErrorMessage(Messages.NoMatchFound, ErrorCategory.ObjectNotFound);
            public static ErrorMessage NoMatchFoundForCriteria = new ErrorMessage(Messages.NoMatchFoundForCriteria, ErrorCategory.ObjectNotFound);
            public static ErrorMessage NoPackagesFoundForProvider = new ErrorMessage(Messages.NoPackagesFoundForProvider, ErrorCategory.ObjectNotFound);
            public static ErrorMessage PackageInstallRequiresOption = new ErrorMessage(Messages.PackageInstallRequiresOption, ErrorCategory.InvalidArgument);
            public static ErrorMessage PackageSourceExists = new ErrorMessage(Messages.PackageSourceExists, ErrorCategory.ResourceExists);
            public static ErrorMessage SourceFoundInMultipleProviders = new ErrorMessage(Messages.SourceFoundInMultipleProviders, ErrorCategory.InvalidArgument);
            public static ErrorMessage SourceNotFound = new ErrorMessage(Messages.SourceNotFound, ErrorCategory.ObjectNotFound);
            public static ErrorMessage UnableToFindProviderForSource = new ErrorMessage(Messages.UnableToFindProviderForSource, ErrorCategory.ObjectNotFound);
            public static ErrorMessage UninstallationFailure = new ErrorMessage(Messages.UninstallationFailure, ErrorCategory.InvalidOperation);
            public static ErrorMessage UnknownProvider = new ErrorMessage(Microsoft.PackageManagement.Internal.Constants.Messages.UnknownProvider, ErrorCategory.ObjectNotFound);
            public static ErrorMessage UnknownProviderFromActivatedList = new ErrorMessage(Messages.UnknownProviderFromActivatedList, ErrorCategory.ObjectNotFound);
            public static ErrorMessage UnknownProviders = new ErrorMessage(Messages.UnknownProviders, ErrorCategory.InvalidArgument);
            public static ErrorMessage PackageFileExists = new ErrorMessage(Messages.PackageFileExists, ErrorCategory.InvalidArgument);
            public static ErrorMessage ProviderFailToDownloadFile = new ErrorMessage(Messages.ProviderFailToDownloadFile, ErrorCategory.InvalidOperation);
            public static ErrorMessage UnableToOverwrite = new ErrorMessage(Messages.UnableToOverwrite, ErrorCategory.InvalidResult);
            public static ErrorMessage FilePathMustBeFileSystemPath = new ErrorMessage(Messages.FilePathMustBeFileSystemPath, ErrorCategory.ObjectNotFound);
            public static ErrorMessage SavePackageError = new ErrorMessage(Messages.SavePackageError, ErrorCategory.InvalidArgument);
            public static ErrorMessage UnableToFindDependencyPackage = new ErrorMessage(Messages.UnableToFindDependencyPackage, ErrorCategory.ObjectNotFound);
            public static ErrorMessage ProviderNameNotSpecified = new ErrorMessage(Messages.ProviderNameNotSpecified, ErrorCategory.NotSpecified);
            public static ErrorMessage SpecifiedProviderMissingRequiredOption = new ErrorMessage(Messages.SpecifiedProviderMissingRequiredOption, ErrorCategory.InvalidArgument);
            public static ErrorMessage FileNotFound = new ErrorMessage(Messages.FileNotFound, ErrorCategory.ObjectNotFound);
            public static ErrorMessage FolderNotFound = new ErrorMessage(Messages.FolderNotFound, ErrorCategory.ObjectNotFound);
            public static ErrorMessage UnhandledException = new ErrorMessage(Messages.UnhandledException, ErrorCategory.InvalidOperation);
            public static ErrorMessage MoreThanOneFolderMatched = new ErrorMessage(Messages.MoreThanOneFolderMatched, ErrorCategory.ObjectNotFound);
            public static ErrorMessage MoreThanOneFileMatched = new ErrorMessage(Messages.MoreThanOneFileMatched, ErrorCategory.ObjectNotFound);
            public static ErrorMessage VersionRangeAndRequiredVersionCannotBeSpecifiedTogether = new ErrorMessage(Messages.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether, ErrorCategory.InvalidArgument);
            public static ErrorMessage AllVersionsCannotBeUsedWithOtherVersionParameters = new ErrorMessage(Messages.AllVersionsCannotBeUsedWithOtherVersionParameters, ErrorCategory.InvalidArgument);
            public static ErrorMessage InvalidVersion = new ErrorMessage(Messages.InvalidVersion, ErrorCategory.InvalidArgument);
            public static ErrorMessage MultipleNamesWithVersionNotAllowed = new ErrorMessage(Messages.MultipleNamesWithVersionNotAllowed, ErrorCategory.InvalidArgument);
            public static ErrorMessage WildCardCharsAreNotSupported = new ErrorMessage(Messages.WildCardCharsAreNotSupported, ErrorCategory.InvalidArgument);            
            public static ErrorMessage WhitespacesAreNotSupported = new ErrorMessage(Messages.WhitespacesAreNotSupported, ErrorCategory.InvalidArgument);            
            public static ErrorMessage MinimumVersionMustBeLessThanMaximumVersion = new ErrorMessage(Messages.MinimumVersionMustBeLessThanMaximumVersion, ErrorCategory.InvalidArgument);
            public static ErrorMessage FullProviderFilePathVersionNotAllowed = new ErrorMessage(Messages.FullProviderFilePathVersionNotAllowed, ErrorCategory.InvalidArgument);
            public static ErrorMessage RegisterPackageSourceRequired = new ErrorMessage(Messages.RegisterPackageSourceRequired, ErrorCategory.InvalidArgument);
            public static ErrorMessage NoMatchFoundForProvider = new ErrorMessage(Messages.NoMatchFoundForProvider, ErrorCategory.InvalidArgument);
            public static ErrorMessage MethodNotImplemented = new ErrorMessage(Messages.MethodNotImplemented, ErrorCategory.NotImplemented);
            public static ErrorMessage InstallRequiresCurrentUserScopeParameterForNonAdminUser = new ErrorMessage(Messages.InstallRequiresCurrentUserScopeParameterForNonAdminUser, ErrorCategory.NotImplemented);
            public static ErrorMessage TooManyPackages = new ErrorMessage(Messages.TooManyPackages, ErrorCategory.InvalidArgument);
            
            // ReSharper restore InconsistentNaming
        }

        internal static class Messages {
#if DEBUG
            internal const string NotImplemented = "MSG:NotImplemented";
#endif
            internal const string InstallRequiresCurrentUserScopeParameterForNonAdminUser = "MSG:InstallRequiresCurrentUserScopeParameterForNonAdminUser";
            internal const string MoreThanOneFileMatched = "MSG:MoreThanOneFileMatched";
            internal const string FileNotFound = "MSG:FileNotFound";
            internal const string UnhandledException = "MSG:UnhandledException";
            internal const string RegisterPackageSourceRequired = "MSG:RegisterPackageSourceRequired";
            internal const string NoMatchFoundForProvider = "MSG:NoMatchFoundForProvider";            
            internal const string FullProviderFilePathVersionNotAllowed = "MSG:FullProviderFilePathVersionNotAllowed";
            internal const string MultipleNamesWithVersionNotAllowed = "MSG:MultipleNamesWithVersionNotAllowed";
            internal const string WildCardCharsAreNotSupported = "MSG:WildCardCharsAreNotSupported";
            internal const string WhitespacesAreNotSupported = "MSG:WhitespacesAreNotSupported";           
            internal const string MinimumVersionMustBeLessThanMaximumVersion = "MSG:MinimumVersionMustBeLessThanMaximumVersion";
            internal const string FolderNotFound = "MSG:FolderNotFound";
            internal const string MoreThanOneFolderMatched = "MSG:MoreThanOneFolderMatched";
            internal const string ActionInstallPackage = "MSG:ActionInstallPackage";
            internal const string ActionRegisterPackageSource = "MSG:ActionRegisterPackageSource";
            internal const string ActionReplacePackageSource = "MSG:ActionReplacePackageSource";
            internal const string ActionUninstallPackage = "MSG:ActionUninstallPackage";
            internal const string ActionUnregisterPackageSource = "MSG:ActionUnregisterPackageSource";
            internal const string BootstrapManualAssembly = "MSG:BootstrapManualAssembly";
            internal const string BootstrapManualInstall = "MSG:BootstrapManualInstall";
            internal const string BootstrapProvider = "MSG:BootstrapProvider";
            internal const string BootstrapProviderProviderRequested = "MSG:BootstrapProviderProviderRequested";
            internal const string BootstrapProviderUserRequested = "MSG:BootstrapProviderUserRequested";
            internal const string CaptionPackageContainsInstallationScript = "MSG:CaptionPackageContainsInstallationScript";
            internal const string CaptionPackageContainsUninstallationScript = "MSG:CaptionPackageContainsUninstallationScript";
            internal const string CaptionPackageInstallFailure = "MSG:CaptionPackageInstallFailure";
            internal const string CaptionPackageNotTrusted = "MSG:CaptionPackageNotTrusted";
            internal const string CaptionSourceNotTrusted = "MSG:CaptionSourceNotTrusted";
            internal const string CaptionPackageUninstallFailure = "MSG:CaptionPackageUninstallFailure";
            internal const string DestinationOrLiteralPathRequired = "MSG:DestinationOrLiteralPathRequired";
            internal const string ProviderNameNotSpecified = "MSG:ProviderNameNotSpecified";
            internal const string DestinationPathInvalid = "MSG:DestinationPathInvalid";
            internal const string DisambiguateForInstall = "MSG:DisambiguateForInstall";
            internal const string DisambiguateForUninstall = "MSG:DisambiguateForUninstall";
            internal const string FileNotRecognized = "MSG:FileNotRecognized";
            internal const string FilePathMustBeFileSystemPath = "MSG:FilePathMustBeFileSystemPath";
            internal const string InstallationFailure = "MSG:InstallationFailure";
            internal const string InstalledPackageMultiple = "MSG:InstalledPackageMultiple";
            internal const string InstallingPackageMultiple = "MSG:InstallingPackageMultiple";
            internal const string InstallingPackagesCount = "MSG:InstallingPackagesCount";
            internal const string MethodNotImplemented = "MSG:MethodNotImplemented";
            internal const string MatchesMultiplePackages = "MSG:MatchesMultiplePackages";
            internal const string MatchesMultipleProviders = "MSG:MatchesMultipleProviders";
            internal const string MustSpecifyCriteria = "MSG:MustSpecifyCriteria";
            internal const string NameOrLocationRequired = "MSG:NameOrLocationRequired";
            internal const string NoMatchesForWildcard = "MSG:NoMatchesForWildcard";
            internal const string NoMatchForProvidersAndSources = "MSG:NoMatchForProvidersAndSources";
            internal const string NoMatchFound = "MSG:NoMatchFound";
            internal const string NoMatchFoundForCriteria = "MSG:NoMatchFoundForCriteria";
            internal const string NoPackagesFoundForProvider = "MSG:NoPackagesFoundForProvider";
            internal const string OverwritingPackageSource = "MSG:OverwritingPackageSource";
            internal const string PackageInstallRequiresOption = "MSG:PackageInstallRequiresOption";
            internal const string PackageSourceExists = "MSG:PackageSourceExists";
            internal const string QueryBootstrap = "MSG:QueryBootstrap";
            internal const string QueryContinueInstallingAfterFailing = "MSG:QueryContinueInstallingAfterFailing";
            internal const string QueryContinueUninstallingAfterFailing = "MSG:QueryContinueUninstallingAfterFailing";
            internal const string QueryInstallUntrustedPackage = "MSG:QueryInstallUntrustedPackage";
            internal const string QueryShouldThePackageScriptAtBeProcessed = "MSG:QueryShouldThePackageScriptAtBeProcessed";
            internal const string QueryShouldThePackageUninstallScriptAtBeProcessed = "MSG:QueryShouldThePackageUninstallScriptAtBeProcessed";
            internal const string SavePackage = "MSG:SavePackage";
            internal const string SavePackageError = "MSG:SavePackageError";
            internal const string ShouldContinueWithUntrustedPackageSource = "MSG:ShouldContinueWithUntrustedPackageSource";
            internal const string SkippedInstalledPackage = "MSG:SkippedInstalledPackage";
            internal const string SkippedInstalledPackageMultiple = "MSG:SkippedInstalledPackageMultiple";
            internal const string SkippedProviderMissingRequiredOption = "MSG:SkippedProviderMissingRequiredOption";
            internal const string SourceFoundInMultipleProviders = "MSG:SourceFoundInMultipleProviders";
            internal const string SourceNotFound = "MSG:SourceNotFound";
            internal const string SourceNotFoundForLocation = "MSG:SourceNotFoundForLocation";
            internal const string SourceNotFoundForNameAndLocation = "MSG:SourceNotFoundForNameAndLocation";
            internal const string SourceNotFoundNoCriteria = "MSG:SourceNotFoundNoCriteria";
            internal const string SpecifiedProviderMissingRequiredOption = "MSG:SpecifiedProviderMissingRequiredOption";
            internal const string TargetPackage = "MSG:TargetPackage";
            internal const string TargetPackageVersion = "MSG:TargetPackageVersion";
            internal const string TargetPackageSource = "MSG:TargetPackageSource";
            internal const string UnableToFindProviderForSource = "MSG:UnableToFindProviderForSource";
            internal const string UninstallationFailure = "MSG:UninstallationFailure";
            internal const string UnknownProviders = "MSG:UnknownProviders";
            internal const string UnknownProviderFromActivatedList = "MSG:UnknownProviderFromActivatedList";
            internal const string PackageFileExists = "MSG:PackageFileExists";
            internal const string UnableToOverwrite = "MSG:UnableToOverwrite";
            internal const string ProviderFailToDownloadFile = "MSG:ProviderFailToDownloadFile";
            internal const string UnableToFindDependencyPackage = "MSG:UnableToFindDependencyPackage";
            internal const string VersionRangeAndRequiredVersionCannotBeSpecifiedTogether = "MSG:VersionRangeAndRequiredVersionCannotBeSpecifiedTogether";
            internal const string AllVersionsCannotBeUsedWithOtherVersionParameters = "MSG:AllVersionsCannotBeUsedWithOtherVersionParameters";
            internal const string InvalidVersion = "MSG:InvalidVersion";
            internal const string TooManyPackages = "MSG:TooManyPackages";
        }

        internal static class Nouns {
            internal const string PackageNoun = "Package";
            internal const string PackageSourceNoun = "PackageSource";
            internal const string PackageProviderNoun = "PackageProvider";
        }

        internal static class Methods {
            internal const string StopProcessingAsyncMethod = "StopProcessingAsync";
            internal const string ProcessRecordAsyncMethod = "ProcessRecordAsync";
            internal const string GenerateDynamicParametersMethod = "GenerateDynamicParameters";
            internal const string BeginProcessingAsyncMethod = "BeginProcessingAsync";
            internal const string EndProcessingAsyncMethod = "EndProcessingAsync";
        }

        internal static class Parameters {
            internal const string ConfirmParameter = "Confirm";
            internal const string WhatIfParameter = "WhatIf";
        }

    }
}
