namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;

    public interface IPackage : IPackageName
    {
        //NugetClient:IPackage
        Uri ReportAbuseUrl { get; }
        Uri LicenseReportUrl { get; }
        long DownloadCount { get; }
        long PackageSize { get; }
        long VersionDownloadCount { get; }
        bool IsAbsoluteLatestVersion { get; }
        bool IsLatestVersion { get; }
        bool Listed { get; }
        DateTimeOffset? Published { get; }
        DateTimeOffset? Created { get; }
        DateTimeOffset? LastUpdated { get; }
        DateTimeOffset? LastEdited { get; }
        string FullFilePath { get; set; }

        //NugetClient:IPackageMetadata
        string Title { get; }
        IEnumerable<string> Authors { get; }
        IEnumerable<string> Owners { get; }
        Uri IconUrl { get; }
        Uri LicenseUrl { get;} 
        Uri ProjectUrl { get; }
        Uri GalleryDetailsUrl { get; }
        bool RequireLicenseAcceptance { get; }
        bool DevelopmentDependency { get; }
        string LicenseNames { get; }
        string Description { get; }
        string Summary { get; }
        string ContentSrcUrl { get;}
        string ReleaseNotes { get; }
        string Language { get; }
        string Tags { get; }
        string Copyright { get; }
        Version MinClientVersion { get; }
        List<PackageDependencySet> DependencySetList { get; }
        string PackageHash { get; }
        string PackageHashAlgorithm { get; }
    }
}