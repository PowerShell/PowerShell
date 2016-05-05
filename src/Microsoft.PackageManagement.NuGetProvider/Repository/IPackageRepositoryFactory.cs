namespace Microsoft.PackageManagement.NuGetProvider 
{

    public interface IPackageRepositoryFactory
    {
        IPackageRepository CreateRepository(string packageSource, NuGetRequest request);
    }
}
