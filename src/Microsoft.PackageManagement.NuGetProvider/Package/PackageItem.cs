namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class PackageItem
    {
        private string _fullPath;

        internal IPackage Package { get; set; }     

        internal PackageSource PackageSource { get; set; }

        internal string FastPath { get; set; }

        internal bool IsPackageFile { get; set; }

        internal string[] Sources { get; set; }

        internal string Id
        {
            get
            {
                if (Package != null)
                {
                    return Package.Id;
                }
                return null;
            }

        }

        internal string InstalledDirectory
        {
            get
            {
                try
                {
                    // if this package file is in a folder with the same name,
                    // we'll consider that 'installed'
                    if (IsPackageFile)
                    {
                        var dir = Path.GetDirectoryName(PackageSource.Location);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            var dirName = Path.GetFileName(dir);
                            var name = Path.GetFileNameWithoutExtension(PackageSource.Location);

                            //There is a case where we keep .nuspec
                            //c:\...\jQuery.2.1.4
                            //     jQuery.nuspec
                            //dirName = jQuery.2.1.4
                            //name = jQuery
                            //conduct case insensitive comparison between name and dirName                           
                            if (!string.IsNullOrEmpty(name) && (dirName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) && Directory.Exists(dir))
                            {
                                return dir;
                            }
                        }
                    }
                }
                catch
                {
                }
                return null;
            }
        }

        internal bool IsInstalled
        {
            get
            {
                return !string.IsNullOrWhiteSpace(InstalledDirectory);
            }
        }

        internal string Version
        {
            get
            {
                if ((Package != null) && (Package.Version != null))
                {
                    return Package.Version.ToString();
                }
                return null;
            }
        }

        //internal string FullName
        //{
        //    get
        //    {
        //        if (Package != null)
        //        {
        //            return Package.GetFullName();
        //        }

        //        return string.Format("{0}.{1}", Id, Version);
        //    }
        //}

        internal string PackageFilename
        {
            get
            {
                if (IsPackageFile)
                {
                    return Path.GetFileName(PackageSource.Location);
                }

                return Id + "." + Version + NuGetConstant.PackageExtension;
            }
        }

        internal string FullPath
        {
            get
            {
                if (IsPackageFile)
                {
                    return Path.GetFileName(PackageSource.Location);
                }
                return _fullPath;
            }
            set
            {
                _fullPath = value;
            }
        }
    }
    
    //Comparer for packages using version and id
    internal class PackageItemComparer : IEqualityComparer<PackageItem>
    {
        public bool Equals(PackageItem packageOne, PackageItem packageTwo)
        {
            return String.Equals(packageOne.Id, packageTwo.Id, StringComparison.OrdinalIgnoreCase) && String.Equals(packageOne.Version, packageTwo.Version, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(PackageItem obj)
        {
            return (String.IsNullOrWhiteSpace(obj.Id) ? String.Empty : obj.Id).GetHashCode() * 31
                + (String.IsNullOrWhiteSpace(obj.Version) ? String.Empty : obj.Version).GetHashCode();
        }
    }
}