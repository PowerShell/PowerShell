
namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Net;
    using System.IO;

    internal class PackageSource
    {
        private IPackageRepository _repository;

        //Parameters must be filled in during the instantiation.

        internal string Name { get; set; }

        internal string Location { get; set; }

        internal bool Trusted { get; set; }

        internal bool IsRegistered { get; set; }

        internal bool IsValidated { get; set; }

        internal NuGetRequest Request { get; set; }

        internal IPackageRepository Repository
        {
            get
            {
                if (!IsSourceAFile)
                {
                    return _repository ?? (_repository = PackageRepositoryFactory.Default.CreateRepository(Location, Request));
                }
                return null;
            }
        }

        internal bool IsSourceAFile
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(Location) && ((!Uri.IsWellFormedUriString(Location, UriKind.Absolute) || new Uri(Location).IsFile) && File.Exists(Location)))
                    {
                        return true;
                    }
                }
                catch
                {
                    // no worries.
                }
                return false;
            }
        }

        internal string Serialized
        {
            get
            {
                return Location.ToBase64();
            }
        }
    }
}