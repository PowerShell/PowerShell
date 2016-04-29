namespace Microsoft.PackageManagement.NuGetProvider 
{
    
    public interface IPackageName
    {
        string Id { get; }
        SemanticVersion Version { get; }      
    }
}
