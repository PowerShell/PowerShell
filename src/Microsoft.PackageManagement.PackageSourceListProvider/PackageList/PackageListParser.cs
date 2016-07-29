#if !UNIX

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Management.Automation;
using Microsoft.PackageManagement.Provider.Utility;
using System.Reflection;
using System.Globalization;

using SemanticVersion = Microsoft.PackageManagement.Provider.Utility.SemanticVersion;

namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    internal static class JsonParser
    {
        public static System.Management.Automation.PowerShell _powershell = System.Management.Automation.PowerShell.Create();

        public static object ConvertObjectToType(object objectToBeConverted, Type typeOfObject)
        {
            if (typeOfObject.GetTypeInfo().IsPrimitive || typeOfObject == typeof(string))
            {
                return objectToBeConverted;
            }

            PSObject psObject = objectToBeConverted as PSObject;

            if (psObject == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.CannotConvertObject, objectToBeConverted.GetType()));
            }

            var returnValue = Activator.CreateInstance(typeOfObject);

            foreach (var property in typeOfObject.GetProperties())
            {
                var propertyTypeInfo = property.PropertyType.GetTypeInfo();

                var matchingProperty = psObject.Properties.FirstOrDefault(psObjectProperty => psObjectProperty.Name.EqualsIgnoreCase(property.Name));

                if (matchingProperty != null)
                {
                    // primitive type, just cast
                    if (propertyTypeInfo.IsPrimitive || property.PropertyType == typeof(string))
                    {
                        property.SetValue(returnValue, matchingProperty.Value);
                    }
                    else if (propertyTypeInfo.IsGenericType)
                    {
                        // maybe list so check that we have 1 generic arguments
                        var genericArguments = propertyTypeInfo.GetGenericArguments();

                        if (genericArguments.Count() == 1)
                        {
                            Type genericArgumentType = genericArguments[0];
                            Type genericListType = typeof(List<>).MakeGenericType(genericArgumentType);

                            // it's a list so check that the value we got is an array
                            if (matchingProperty.Value.GetType().GetTypeInfo().IsArray)
                            {
                                var newList = Activator.CreateInstance(genericListType);

                                var objectList = matchingProperty.Value as object[];

                                var addMethod = genericListType.GetMethod("Add");

                                // populate the array
                                foreach (var entry in objectList)
                                {
                                    addMethod.Invoke(newList, new object[] { ConvertObjectToType(entry, genericArgumentType) });
                                }

                                property.SetValue(returnValue, newList);
                            }
                        }
                        else
                        {
                            throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.CannotConvertGenericTypes));
                        }
                    }
                    else if (propertyTypeInfo.IsClass && matchingProperty.Value is PSObject)
                    {
                        // create class using this same function and assign that to the property
                        property.SetValue(returnValue, ConvertObjectToType(matchingProperty.Value as PSObject, property.PropertyType));
                    }
                }

            }

            return returnValue;
        }

        public static Dictionary<string, List<PackageJson>> ReadPackageSpec(PackageSource packageSource, string fileToBeProcessed)
        {
            string packageSpecPath = string.IsNullOrWhiteSpace(fileToBeProcessed) ? packageSource.Location : fileToBeProcessed;

            var jsonFileContent = File.ReadAllText(packageSpecPath);

            PSObject result;

            lock (_powershell)
            {
                _powershell.Commands.Clear();
                _powershell.AddCommand("ConvertFrom-Json").AddParameter("InputObject", jsonFileContent);
                result = _powershell.Invoke().FirstOrDefault();
            }

            var packageListEntries = PopulatePackageSourceList(result, packageSource, null);

            //process dependencies
            return ProcessDependencies(packageListEntries);
        }

        public static Dictionary<string, List<PackageJson>> ProcessDependencies(Dictionary<string, List<PackageJson>> packages)
        {
            if (packages == null)
            {
                return packages;
            }

            List<PackageJson> dependencyList = new List<PackageJson>();

            var PackagesWithDependencies = packages.Values.SelectMany(each => each).Where(item => item.Dependencies != null);

            foreach (var package in PackagesWithDependencies)
            {
                dependencyList.Clear();

                foreach (var dep in package.Dependencies)
                {
                    //TODO check name and version
                    if (packages.ContainsKey(dep.Name))
                    {
                        var depObject = packages[dep.Name].Where(each => (each.Version == dep.Version) && !each.IsCommonDefinition).FirstOrDefault();
                        if (depObject != null)
                        {
                            dependencyList.Add(depObject);
                        }
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("'{0}' is not referencd but not defined in the file '{1}'", dep.Name, package.FilePath));
                    }

                }
                package.DependencyObjects = dependencyList;

            }
            return packages;         
        }

        public static Dictionary<string, List<PackageJson>> PopulatePackageSourceList(PSObject psObjectEntries, PackageSource packageSource, string key)
        {
            var packageSpecPath = packageSource.Location;

            //read access only, and allow others to read at the same time
            Dictionary<string, List<PackageJson>> packages = new Dictionary<string, List<PackageJson>>(StringComparer.OrdinalIgnoreCase);
            ICollection<PackageJson> list = new List<PackageJson>();

            foreach (var package in psObjectEntries.Properties)
            {
                // if this is an array
                if (package.Value.GetType().GetTypeInfo().IsArray)
                {
                    var result = PopulateFromArray(package.Value as object[], packageSpecPath, key);

                    // check that we got a valid array of entries
                    if (result != null && result.Count > 0)
                    {
                        var common = result.Select(each => each.Common).Where(p => p.Name != null).FirstOrDefault();

                        foreach (var item in result)
                        {
                            if (item.Common != null)
                            {
                                item.IsCommonDefinition = true;
                            }
                            else
                            {
                                item.Common = common;
                                item.FilePath = packageSpecPath;

                                if (string.IsNullOrWhiteSpace(item.Version))
                                {
                                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.VersionNotFound, item.Name));
                                }
                            }

                            item.PackageSource = packageSource;
                        }

                        packages.Add(package.Name, result);
                    }
                }
                else if (package.Value is PSObject)
                {
                    var convertedPackage = ConvertObjectToType(package.Value, typeof(PackageJson)) as PackageJson;

                    if (convertedPackage != null)
                    {
                        if (convertedPackage.Common != null)
                        {
                            convertedPackage.IsCommonDefinition = true;
                        }
                        else
                        {
                            convertedPackage.Common = new CommonDefinition();

                            if (String.IsNullOrWhiteSpace(convertedPackage.Version))
                            {
                                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.VersionNotFound, convertedPackage.Name, packageSpecPath));
                            }
                        }

                        convertedPackage.FilePath = packageSpecPath;
                        convertedPackage.PackageSource = packageSource;
                        packages.Add(package.Name, new List<PackageJson>() { convertedPackage });
                    }
                }
                else
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentUICulture, Resources.Messages.InvalidPackageListFormat, packageSpecPath));
                }
            }

            return packages;
        }

        private static List<PackageJson> PopulateFromArray(object[] listOfPackages, string packageSpecPath, string key)
        {
            List<PackageJson> result = new List<PackageJson>();

            if (listOfPackages == null)
            {
                throw new ArgumentNullException(nameof(listOfPackages));
            }

            foreach (var package in listOfPackages)
            {
                var packageEntry = ConvertObjectToType(package, typeof(PackageJson)) as PackageJson;

                if (packageEntry != null)
                {
                    packageEntry.FilePath = packageSpecPath;
                    result.Add(packageEntry);
                }
            }

            return result;
        }  
    }

 
    public class OSRequirement
    {
        public List<string> architecture { get; set; }
        public string minimunVersion { get; set; }
        public List<string> installationOption { get; set; }
    }

    public class PackageHash
    {
        public string algorithm { get; set; }
        public string hashCode { get; set; }

    }

    public class CommonDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }

        public string InstallArguments { get; set; }
        public string UnInstallAdditionalArguments { get; set; }
        public bool IsPackageProvider { get; set; }
        public string Type { get; set; }
   
        public string Summary { get; set; }
        public string License { get; set; }
        public string Author { get; set; }
        public bool IsTrustedSource { get; set; }
        public OSRequirement OS { get; set; }
        public string Destination { get; set; }
        public List<Dependencies> Dependencies { get; set; }
    }

    public class Dependencies
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
    }

    public class PackageJson
    {
        private string _license;
        private string _name;
        private string _displayName;
        private string _summary;
        private string _author;
        private string _destination;
        private List<Dependencies> _dependencies;
        private List<PackageJson> _dependencyObjects;
        private OSRequirement _osRequirement;        
        private string _type;
        private string _isTrustedSource;
        private bool _isPackageProvider;
        private string _installArguments;
        private string _unInstallAdditionalArguments;
        private string _source;

        public CommonDefinition Common { get; set; }
        public string Version { get; set; }


        public PackageHash Hash { get; set; }

        public SemanticVersion SemVer
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
        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                if (value != null)
                {
                    Uri uri;

                    // check whether source is http instead of https
                    if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.UnsuportedUriFormat, Constants.ProviderName, value));
                    }

                    if (!uri.IsFile)
                    {
                        if (uri.Scheme != "https")
                        {
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Messages.UriSchemeNotSupported, uri.Scheme, "https"));
                        }
                    }

                    _source = value;
                }
            }
        }

        public string Name
        {
            get
            {
                return _name ?? (Common != null ? Common.Name : null);
            }

            set { _name = value; }
        }

        

        public string DisplayName
        {
            get
            {
                return _displayName ?? (Common != null ? Common.DisplayName : null);
            }

            set { _displayName = value; }
        }

        public string InstallArguments
        {
            get
            {
                return _installArguments ?? (Common != null ? Common.InstallArguments : null);
            }

            set { _installArguments = value; }
        }

        public string UnInstallAdditionalArguments
        {
            get
            {
                return _unInstallAdditionalArguments ?? (Common != null ? Common.UnInstallAdditionalArguments : null);
            }

            set { _unInstallAdditionalArguments = value; }
        }
 
        public bool IsPackageProvider
        {
            // either the Common or Package definition has the IsPackageProvider property defined?
            get { return _isPackageProvider || (Common != null && Common.IsPackageProvider); }

            set { _isPackageProvider = value; }
        }


        public string Type
        {
            get
            {
                return _type ?? (Common != null ? Common.Type : null);
            }
            set { _type = value; }
        }

        public string Summary
        {
            get
            {
                return _summary ?? (Common != null ? Common.Summary : null);
            }
            set { _summary = value; }
        }
        public string License {
            get
            {
                return _license ?? (Common != null ? Common.License : null);
            }
            set { _license = value; }
        }
        public string Author
        {
            get
            {
                return _author ?? (Common != null ? Common.Author : null);
            }
            set { _author = value; }
        }
        public string Destination
        {
            get
            {
                return _destination ?? (Common != null ? Common.Destination : null);
            }
            set { _destination = value; }
        }

        public bool IsTrustedSource
        {
            get
            {
                string istrusted = _isTrustedSource ?? (Common != null ? Common.IsTrustedSource.ToString().ToLowerInvariant() : null);
                return istrusted.EqualsIgnoreCase("true");
            }
            set { _isTrustedSource = value.ToString().ToLowerInvariant(); }
        }

        internal PackageSource PackageSource { get; set; }

        public List<Dependencies> Dependencies
        {
            get
            {
                return _dependencies ?? (Common != null ? Common.Dependencies : null);
            }
            set { _dependencies = value; }
        }
        
        public OSRequirement OS
        {
            get
            {
                return _osRequirement ?? (Common != null ? Common.OS : null);
            }
            set { _osRequirement = value; }
        }
        public bool IsCommonDefinition { get; set; }

        public string FilePath { get; set; }
        public string VersionScheme { get; set; }

        public List<PackageJson> DependencyObjects
        {
            get
            {
                return _dependencyObjects;
            }
            set { _dependencyObjects = value; }
        }
    } 
}

#endif