namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IPackageRepository
    {
        /// <summary>
        /// Package source location
        /// </summary>
        string Source { get; }

        /// <summary>
        /// True if a file repository.
        /// </summary>
        bool IsFile { get; }

        /// <summary>
        /// Finds packages that match the exact Id and version.
        /// </summary>
        /// <returns>The package if found, null otherwise.</returns>
        IPackage FindPackage(string packageId, SemanticVersion version, NuGetRequest request); 

        /// <summary>
        /// Returns a sequence of packages with the specified id.
        /// </summary>
        IEnumerable<IPackage> FindPackagesById(string packageId, NuGetRequest request);

        /// <summary>
        /// Nuget V2 metadata supports a method 'Search'. It takes three parameters, searchTerm, targetFramework, and includePrerelease.
        /// </summary>
        /// <param name="searchTerm">search uri</param>
        /// <param name="request"></param>
        /// <returns></returns>
        IEnumerable<IPackage> Search(string searchTerm,  NuGetRequest request);
    }
}