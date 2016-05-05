namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PackageBase : IPackage
    {
        private long _downloadCount = -1;
        private long _versionDownloadCount = -1;
        private long _packageSize = -1;

        public PackageBase()
        {
            AdditionalProperties = new Dictionary<string, string>();
        }

        string IPackageName.Id
        {
            get
            {
                return Id;
            }
        }

        SemanticVersion IPackageName.Version
        {
            get
            {
                if (Version != null)
                {
                    return new SemanticVersion(Version);
                }
                return null;
            }
        }

        public bool IsAbsoluteLatestVersion { get; set; }

        public bool IsLatestVersion { get; set;}

        public bool Listed{ get; set;}

        public string FullFilePath { get; set; }

        public DateTimeOffset? Published { get; set;}

        public DateTimeOffset? Created { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public DateTimeOffset? LastEdited { get; set; }

        public string Title{ get; set;}

        public Uri IconUrl{ get; set;}
 
        public Uri LicenseUrl{ get; set;}

        public Uri ProjectUrl{ get; set;}

        public Uri GalleryDetailsUrl { get; set; }

        public bool RequireLicenseAcceptance{ get; set;}

        public bool DevelopmentDependency{ get; set;}

        public string Description{ get; set;}

        public string Summary{ get; set;}

        public string ContentSrcUrl { get; set; }

        public string ReleaseNotes{ get; set;}

        public string Language{ get; set;}

        public string Tags{ get; set;}
 
        public string Copyright{ get; set;}

        public string LicenseNames { get; set; }

        public Version MinClientVersion{ get; set;}

        public long DownloadCount
        {
            get
            {
                return _downloadCount;
            }
            set
            {
                _downloadCount = value;
            }
        }

        public Uri ReportAbuseUrl{ get; set; }

        public Uri LicenseReportUrl { get; set; }

        public long VersionDownloadCount
        {
            get
            {
                return _versionDownloadCount;
            }
            set
            {
                _versionDownloadCount = value;
            }
        }

        public long PackageSize
        {
            get
            {
                return _packageSize;
            }
            set
            {
                _packageSize = value;
            }
        }

        IEnumerable<string> IPackage.Authors
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Authors))
                {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<string> IPackage.Owners
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Owners))
                {
                    return Enumerable.Empty<string>();
                }
                return Owners.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        internal string Id { get; set; }

        internal string Version { get; set; }

        internal string Authors { get; set; }

        internal string Owners { get; set; }

        public string GetFullName()
        {
            return Id + " " + Version;
        }

        public List<PackageDependencySet> DependencySetList { get; set; }

        public string PackageHashAlgorithm { get; set; }

        public string PackageHash { get; set; }

        public Dictionary<string, string> AdditionalProperties { get; set; }
    }
}
