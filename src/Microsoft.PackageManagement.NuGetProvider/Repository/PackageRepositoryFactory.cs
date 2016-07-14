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

            // we cannot call new uri on file path on linux because it will error out
            if (System.IO.Directory.Exists(packageSource))
            {
                return new LocalPackageRepository(packageSource, request);
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
