namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;

    public class PackageName : IPackageName, IEquatable<PackageName>
    {
        private readonly string _packageId;
        private readonly SemanticVersion _version;

        public PackageName(string packageId, SemanticVersion version)
        {
            _packageId = packageId;
            _version = version;
        }

        public string Id
        {
            get { return _packageId; }
        }

        public SemanticVersion Version
        {
            get
            {
                return _version;
            }
        }

        public string Name 
        {
            get 
            {
                return _packageId + "." + _version;
            }
        }

        public bool Equals(PackageName other)
        {
            if (other == null) {
                return ReferenceEquals(_packageId, null);
            }

            return _packageId.Equals(other._packageId, StringComparison.OrdinalIgnoreCase) &&
                   _version.Equals(other._version);
        }

        public override int GetHashCode()
        {
            return _packageId.GetHashCode() * 3137 + _version.GetHashCode();
        }

        public override string ToString()
        {
            return _packageId + " " + _version;
        }
    }
}
