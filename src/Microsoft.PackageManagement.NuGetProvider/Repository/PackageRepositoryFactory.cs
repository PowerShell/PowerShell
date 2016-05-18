namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Net;

    public class PackageRepositoryFactory : IPackageRepositoryFactory
    {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();

        public static PackageRepositoryFactory Default
        {
            get { return _default;}
        }

        public virtual IPackageRepository CreateRepository(string packageSource, NuGetRequest request)
        {
            if (packageSource == null)
            {
                throw new ArgumentNullException("packageSource");
            }

            Uri uri = new Uri(packageSource);

            if (uri.IsFile)
            {
                return new LocalPackageRepository(uri.LocalPath, request);
            }

            return new HttpClientPackageRepository(packageSource, request);
        }
    }
}
