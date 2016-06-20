namespace Microsoft.PackageManagement.NuGetProvider 
{
    using Microsoft.PackageManagement.Provider.Utility;
    public interface IPackageName
    {
        string Id { get; }
        SemanticVersion Version { get; }      
    }
}
