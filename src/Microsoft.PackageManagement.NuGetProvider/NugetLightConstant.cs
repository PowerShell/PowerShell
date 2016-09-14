
namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;

    public static class NuGetConstant
    {
        /// <summary>
        /// Represents the ".nupkg" extension.
        /// </summary>
        public static readonly string PackageExtension = ".nupkg";
        
        /// <summary>
        /// Represents the ".nuspec" extension.
        /// </summary>
        public static readonly string ManifestExtension = ".nuspec";
   
        /// <summary>
        /// Represents the default nuget uri
        /// </summary>
        public static readonly string NugetSite = "www.nuget.org";

        /// <summary>
        /// Name of this provider
        /// </summary>
        public static readonly string ProviderName = "NuGet";

        /// <summary>
        /// Version of this provider
        /// </summary>
        public static readonly string ProviderVersion = "2.8.5.206";     

        /// <summary>
        /// Represents a method that the V2 web service supports to find a package
        /// </summary>
        public static readonly string FindPackagesById= "/FindPackagesById()?id='{0}'";

        /// <summary>
        /// Represents how many packages to be returned by an api call
        /// </summary>
        public static readonly string SkipAndTop = "&$skip={0}&$top={1}";

        /// <summary>
        /// Represents a method that the V2 web service supports to search packages
        /// </summary>
        public static readonly string SearchTerm = "&searchTerm='{0}'&targetFramework=''&includePrerelease={1}";

        /// <summary>
        /// Represents a method that the V2 web service supports to search packages with filtering
        /// </summary>
        public static readonly string SearchFilterAllVersions = "Search()?$orderby=DownloadCount%20desc,Id";

        /// <summary>
        /// Represents a method that the V2 web service supports to search packages with filtering. The result returned is already
        /// sorted by download count.
        /// </summary>
        public static readonly string SearchFilter = "Search()?$filter=IsLatestVersion";


        /// <summary>
        /// Config file storing the info as a result of registering a package source
        /// </summary>
        public static readonly string SettingsFileName = "nuget.config";
        
        /// <summary>
        /// The magic unpublished date is 1900-01-01T00:00:00. This is from NugetCore
        /// </summary>
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));        
    }
}