namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using Resources;
    using Microsoft.PackageManagement.Provider.Utility;
 
    /// <summary>
    /// Utility that can be used to handle the packaged related actions. 
    /// </summary>
    internal static class PackageUtility
    {

        // the pre release regex is of the form -<pre-release version> where <pre-release> version is set of identifier delimited by ".". Each identifer can be any characters in [A-z0-9a-z-]
        // The build regex is of the same form except with a + instead of -
        private static string VersionPattern = String.Concat(@"\.", SemanticVersion.AllowFourPartsVersion, SemanticVersion.ReleasePattern, SemanticVersion.BuildPattern, "$");

        /// <summary>
        /// Get the package name without version info
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        internal static string GetPackageNameWithoutVersionInfo(string packageName)
        {
            //Input: JQuery.2.1.3
            //output: JQuery
            // An unsupported scenario is if the package has name MyModule2.2 and version 2.0 then we will return version as 2.2.2.0 and packagename as mymodule

            string version = String.Empty;

            // Check for version
            var m1 = Regex.Match(packageName, VersionPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (m1.Success) {
                // We have a version
                version = m1.ToString();
            }

            // Return name without the version
            return packageName.Substring(0, packageName.Length - version.Length);
        }

 
        internal static IEnumerable<PackageBase> ProcessPackagesFeed(Stream stream)
        {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }

            XDocument document = XmlUtility.LoadSafe(stream, ignoreWhiteSpace: true);

            var entries = document.Root.ElementsNoNamespace("entry");

            if (entries == null)
            {

                var message = string.Format(Messages.ManifestRequiredXmlElementMissing, "entry");
                throw new InvalidDataException(message);
            }

            foreach (XElement entry in entries)
            {
                var package = new PackageBase();

                ReadEntryElement(ref package, entry);

                yield return package;
            }
        }

        /// <summary>
        /// Parse the 'entry' xml element.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="xElement"></param>
        internal static void ReadEntryElement(ref PackageBase package, XElement xElement)
        {          
            var node = xElement.FirstNode;

            //iterate each <entry> element from the feed, e.g.
            //http://www.nuget.org/api/v2/FindPackagesById()?id='Jquery'
            //
            while (node != null)
            {
                var element = node as XElement;

                if (element != null)
                {
                    ReadEntryChildNode(ref package, element);
                }
                node = node.NextNode;
            }
        }
       
        /// <summary>
        /// Parse the children of the 'entry' tag.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="element"></param>
        private static void ReadEntryChildNode(ref PackageBase package, XElement element)
        {

            var value = element.Value.SafeTrim();

            switch (element.Name.LocalName.ToLowerInvariant())
            {
                case "id":
                    //In manifest, <id>http://www.nuget.org/api/v2/Packages(Id='jQuery',Version='1.10.1')</id>
                    //we here only need Id=Jquery
                    package.Id = ExtractId(value);
                    break;
                case "version":
                    package.Version = value;                   
                    break;
                case "minclientversion":
                    package.MinClientVersion = string.IsNullOrWhiteSpace(value) ? null : new Version(value);
                    break;
                case "author":
                    package.Authors = value;
                    break;
                case "owners":
                    package.Owners = value;
                    break;
                case "licenseurl":
                    package.LicenseUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "projecturl":
                    package.ProjectUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "iconurl":
                    package.IconUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "gallerydetailsurl":
                    package.GalleryDetailsUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "requirelicenseacceptance":
                    package.RequireLicenseAcceptance = XmlConvert.ToBoolean(value);
                    break;
                case "developmentdependency":
                    package.DevelopmentDependency = XmlConvert.ToBoolean(value);
                    break;
                case "description":
                    package.Description = value;
                    break;
                case "summary":
                    package.Summary = value;
                    break;
                case "content":
                    var srcAttribute = element.Attributes(XName.Get("src")).FirstOrDefault();
                    if (srcAttribute != null) {
                        package.ContentSrcUrl = srcAttribute.Value;
                    }
                    break;
                case "releasenotes":
                    package.ReleaseNotes = value;
                    break;
                case "copyright":
                    package.Copyright = value;
                    break;
                case "language":
                    package.Language = value;
                    break;
                case "title":
                    package.Title = value;
                    break;
                case "tags":
                    package.Tags = value;
                    break;
                case "islatestversion":
                    package.IsLatestVersion = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    break;
                case "isabsolutelatestversion":
                    package.IsAbsoluteLatestVersion = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    break;
                case "published":
                    package.Published = GetDateTime(value);
                    break;
                case "created":
                    package.Created = GetDateTime(value);
                    break;
                case "lastupdated":
                    package.LastUpdated = GetDateTime(value);
                    break;
                case "lastedited":
                    package.LastEdited = GetDateTime(value);
                    break;
                case "licensereporturl":
                    package.LicenseReportUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "reportabuseurl":
                    package.ReportAbuseUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "downloadcount":
                    package.DownloadCount = XmlConvert.ToInt64(value);
                    break;
                case "versiondownloadcount":
                    package.VersionDownloadCount = XmlConvert.ToInt64(value);
                    break;
                case "packagesize":
                    package.PackageSize = XmlConvert.ToInt64(value);
                    break;
                case "properties":
                    var node = element.FirstNode;
                    while (node != null)
                    {
                        var propertyElement = node as XElement;
                        if (propertyElement != null)
                        {
                            ReadEntryChildNode(ref package, propertyElement);
                        }
                        node = node.NextNode;
                    }
                    break;
                case "dependencies":
                    package.DependencySetList = ParseDependencySet(value);
                    break;
                case "packagehashalgorithm":
                    package.PackageHashAlgorithm = value;
                    break;
                case "packagehash":
                    package.PackageHash = value;
                    break;
                /* case "frameworkAssemblies":
                     package.FrameworkAssemblies = ReadFrameworkAssemblies(element);
                     break;
                 case "references":
                     package.ReferenceSets = ReadReferenceSets(element);
                     break;*/
                default:
                    if (!String.IsNullOrWhiteSpace(value) && !String.IsNullOrWhiteSpace(element.Name.LocalName))
                    {
                        package.AdditionalProperties.AddOrSet(element.Name.LocalName, value);
                    }
                    break;
            }
        }

        internal static DateTimeOffset? GetDateTime(string dateTime)
        {
            if (string.IsNullOrWhiteSpace(dateTime))
            {
                return null as DateTimeOffset?;
            }

            DateTime result;

            if (DateTime.TryParse(dateTime, out result))
            {
                return result;
            }
            else
            {
                return null as DateTimeOffset?;
            }
        }

        /// <summary>
        /// Parse the dependency set from string value (returned from nuget api call)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static List<PackageDependencySet> ParseDependencySet(string value)
        {
            var dependencySets = new List<PackageDependencySet>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return dependencySets;
            }

            // Dependencies are of the form "dep1|dep2"
            // Split them up and process each dependency with ParseDependency function
            var dependencies = value.Split('|').Select(ParseDependency);

            // group the dependencies by target framework
            var groups = dependencies.GroupBy(d => d.Item3);

            dependencySets.AddRange(
                // Construct dependencySets by targetframework
                groups.Select(g => new PackageDependencySet
                {
                    TargetFramework = g.Key,
                    // Construct the DependencyList for this framework
                    Dependencies = g.Where(pair => !String.IsNullOrEmpty(pair.Item1))       // the Id is empty when a group is empty.
                                    .Select(pair => new PackageDependency
                                    {
                                        Id = pair.Item1,
                                        DependencyVersion = pair.Item2
                                    }).ToList()
                }));
            return dependencySets;
        }

        /// <summary>
        /// Parses a dependency from the feed in the format:
        /// id or id:versionSpec, or id:versionSpec:targetFramework
        /// </summary>
        private static Tuple<string, DependencyVersion, string> ParseDependency(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // IMPORTANT: Do not pass StringSplitOptions.RemoveEmptyEntries to this method, because it will break 
            // if the version spec is null, for in that case, the Dependencies string sent down is "<id>::<target framework>".
            // We do want to preserve the second empty element after the split.
            string[] tokens = value.Trim().Split(new[] { ':' });

            if (tokens.Length == 0)
            {
                return null;
            }

            // Trim the id
            string id = tokens[0].Trim();

            // Parse the dependency version
            DependencyVersion depVer = new DependencyVersion();
            if (tokens.Length > 1)
            {
                // Parse the version
                depVer = DependencyVersion.ParseDependencyVersion(tokens[1]);
            }

            // Get the target framework, returns empty string if none exists.
            var targetFramework = (tokens.Length > 2 && !String.IsNullOrEmpty(tokens[2])) ? tokens[2] : String.Empty;

            return Tuple.Create(id, depVer, targetFramework);
        }

        /// <summary>
        /// Extract the package id.
        /// From the feed, the package id is equal to something like this "http://www.nuget.org/api/v2/Packages(Id='jQuery',Version='2.1.3')".
        /// But we need id='jQuery'. 
        /// </summary>
        /// <param name="longId"></param>
        /// <returns></returns>
        private static string ExtractId(string longId)
        {
            String pattern = @"([^']+?)\+?'(?<PackageId>[^']+?)\'";
                     
            Match m = Regex.Match(longId, pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (m.Success)
            {
                return m.Groups["PackageId"].ToString();
            }

            return longId;
        }

        /// <summary>
        /// Utility to parse the nuspec file and produces the package object.
        /// </summary>
        /// <param name="nuspecFileFullPath">The full path to the .nuspec file </param>
        /// <returns></returns>
        internal static PackageBase ProcessNuspec(string nuspecFileFullPath)
        {

            if (string.IsNullOrWhiteSpace(nuspecFileFullPath))
            {
                throw new ArgumentNullException(nuspecFileFullPath);
            }

            if (!File.Exists(nuspecFileFullPath))
            {
                throw new FileNotFoundException(nuspecFileFullPath);
            }

            //Make sure it's .nuspec
            string fileExtension = Path.GetExtension(nuspecFileFullPath);

            if (fileExtension != ".nuspec")
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Messages.InvalidFileExtension, nuspecFileFullPath));
            }

            XDocument document = XmlUtility.LoadSafe(nuspecFileFullPath);
            return ProcessNuspec(document);
        }


        /// <summary>
        /// Utility to parse the nuspec file and produces the package object.
        /// </summary>
        /// <param name="rootDocument">XDocument object pointing to a .nuspec xml file </param>
        /// <returns></returns>
        private static PackageBase ProcessNuspec(XDocument rootDocument)
        {
            if (rootDocument == null)
            {
                throw new ArgumentNullException("rootDocument");
            }

            var pkg = new PackageBase();

            var metadataElement = rootDocument.Root.ElementsNoNamespace("metadata").FirstOrDefault();

            if (metadataElement == null)
            {
                var message = string.Format(CultureInfo.InvariantCulture, Messages.NuspecRequiredXmlElementMissing, "metadata");
                throw new InvalidDataException(message);
            }

            ReadPackageMetadata(ref pkg, metadataElement);

            return pkg;
        }

        /// <summary>
        /// A help for processing the metadata tag
        /// </summary>
        /// <param name="package"></param>
        /// <param name="xElement"></param>
        private static void ReadPackageMetadata(ref PackageBase package, XElement xElement)
        {
            var node = xElement.FirstNode;

            if (node == null) {

                //Check for the tags within metadata tag
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, Messages.NuspecRequiredXmlElementMissing, "id, version, authors, or description"));
            }

            while (node != null)
            {
                var element = node as XElement;
                if (element != null)
                {
                    ReadPackageMetadataElement(ref package, element);
                }
                node = node.NextNode;
            }
            
            // now check for required elements, which include <id>, <version>, <authors> and <description>
            EnsureRequiredXmlTags(package);
        }

        /// <summary>
        /// A helper method for processing the children of the 'metadata' tag in the .nuspec file
        /// </summary>
        /// <param name="package"></param>
        /// <param name="element"></param>
        private static void ReadPackageMetadataElement(ref PackageBase package, XElement element)
        {
            var value = element.Value.SafeTrim();

            // since we put all the name to lower case, each word in case should all be in lower case
            switch (element.Name.LocalName.ToLowerInvariant())
            {
                case "id":
                    package.Id = value;
                    break;
                case "version":
                    package.Version = value;
                    break;
                case "authors":
                    package.Authors = value;
                    break;
                case "owners":
                    package.Owners = value;
                    break;
                case "licenseurl":
                    package.LicenseUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "projecturl":
                    package.ProjectUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "iconurl":
                    package.IconUrl = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
                    break;
                case "requirelicenseacceptance":
                    package.RequireLicenseAcceptance = XmlConvert.ToBoolean(value);
                    break;
                case "developmentdependency":
                    package.DevelopmentDependency = XmlConvert.ToBoolean(value);
                    break;
                case "description":
                    package.Description = value;
                    break;
                case "summary":
                    package.Summary = value;
                    break;
                case "releasenotes":
                    package.ReleaseNotes = value;
                    break;
                case "copyright":
                    package.Copyright = value;
                    break;
                case "language":
                    package.Language = value;
                    break;
                case "title":
                    package.Title = value;
                    break;
                case "tags":
                    package.Tags = value;
                    break;
                case "dependencies":
                    package.DependencySetList = ReadDependencyList(element);
                    break;
                /* case "frameworkAssemblies":
                     manifestMetadata.FrameworkAssemblies = ReadFrameworkAssemblies(element);
                     break;
                 case "references":
                     manifestMetadata.ReferenceSets = ReadReferenceSets(element);
                     break;*/
            }
        }

        /// <summary>
        /// Parse dependencies from the xelement and returns a list of PackageDependencySet
        /// </summary>
        /// <param name="dependenciesElement"></param>
        /// <returns></returns>
        private static List<PackageDependencySet> ReadDependencyList(XElement dependenciesElement)
        {
            // No elements so return empty list
            if (!dependenciesElement.HasElements)
            {
                return new List<PackageDependencySet>();
            }

            // Direct child of dependenciesElement with tag <group>
            var groups = dependenciesElement.ElementsNoNamespace("group");

            // It is an error for <dependencies> element to contain both <dependency> and <group> child elements
            if (dependenciesElement.ElementsNoNamespace("dependency").Any() && groups.Any())
            {
                throw new InvalidDataException(Messages.DependencyHasBothElements);
            }

            var dependencies = ReadDependencies(dependenciesElement);

            if (!groups.Any())
            {
                // since there is no group, we are encountering
                // old format, <dependency> is direct child of <dependencies>
                var dependencySet = new PackageDependencySet
                {
                    Dependencies = dependencies
                };

                return new List<PackageDependencySet> { dependencySet };
            }
            else
            {
                // new format, with <group> as child of <dependencies>
                // Project each group into a packagedependencyset
                return groups.Select(group =>
                    new PackageDependencySet {
                        TargetFramework = group.GetOptionalAttributeValue("targetFramework").SafeTrim(),
                        Dependencies = ReadDependencies(group)
                    }).ToList();
            }
        }

        /// <summary>
        /// Read the dependencies from xelement. Returns a list of dependencies
        /// </summary>
        /// <param name="containerElement"></param>
        /// <returns></returns>
        private static List<PackageDependency> ReadDependencies(XElement containerElement)
        {
            // list of dependency
            var dependencies = containerElement.ElementsNoNamespace("dependency");
            
            return (from element in containerElement.ElementsNoNamespace("dependency")                    
                    let idElement = element.Attribute("id")
                    // Check that id is not null or empty
                    where idElement != null && !String.IsNullOrEmpty(idElement.Value)
                    // Project a PackageDependency based on that
                    select new PackageDependency
                    {
                        Id = idElement.Value.SafeTrim(),
                        DependencyVersion = DependencyVersion.ParseDependencyVersion(element.GetOptionalAttributeValue("version").SafeTrim())
                    }).ToList();
        }

        /// <summary>
        /// Make sure the Nuget required xml tags exist within the metadata tag in the nuspec file.
        /// </summary>
        /// <param name="package"></param>
        private static void EnsureRequiredXmlTags(PackageBase package)
        {
            //As a .nupec, id", "version", "authors", "description" tags are required.
            //Check if they exists

            if (string.IsNullOrWhiteSpace(package.Version) ||
                string.IsNullOrWhiteSpace(package.Id) ||
                string.IsNullOrWhiteSpace(package.Authors) ||
                string.IsNullOrWhiteSpace(package.Description))
            {
                //bad .nuspec xml format
                string message = string.Format(CultureInfo.InvariantCulture, "{0}: '{1}', '{2}, '{3}', '{4}'", Messages.NuspecRequiredXmlElementMissing, "Version", "Id", "authors", "description");

                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Unzip the file
        /// </summary>
        /// <param name="fullZipPath">The file to be unzipped.</param>
        /// <returns></returns>
        internal static string DecompressFile(string fullZipPath) {

            var extractedFolder = Path.Combine(Path.GetDirectoryName(fullZipPath), Path.GetFileNameWithoutExtension(fullZipPath));

            //this possible race condition if someone else is creating it.
            if (Directory.Exists(extractedFolder)) {
                Directory.Delete(extractedFolder, true);
            }

            ZipFile.ExtractToDirectory(fullZipPath, extractedFolder);

            //Delete the default files that come with the packages _rels, [Content_Types].xml, and packages folder            
            FileUtility.DeleteDirectory(Path.Combine(extractedFolder, "_rels"), true, false);

            //var files = Directory.GetFiles(extractedFolder, "[Content_Types].xml", SearchOption.TopDirectoryOnly);
            FileUtility.DeleteFile(Path.Combine(extractedFolder, "[Content_Types].xml"), false);

            //e.g ..\package\services\metadata\core-properties\341424.psmdcp
            var metadataFile = Path.Combine(extractedFolder, "package", "services", "metadata", "core-properties");
            if (Directory.Exists(metadataFile)) {
                FileUtility.DeleteDirectory(Path.Combine(extractedFolder, "package"), true, false);
            }
          
            return extractedFolder;
        }
        
        /// <summary>
        /// Unzip the .nupkg file and read the .nuspec manifest.
        /// </summary>
        /// <param name="nupkgFilePath">The nupkg file</param>
        /// <param name="packageName">Package name</param>
        /// <returns></returns>
        internal static PackageBase DecompressFile(string nupkgFilePath, string packageName)
        {
            if (!File.Exists(nupkgFilePath))
            {
                throw new FileNotFoundException(nupkgFilePath);
            }

            XDocument root = ReadNuSpecFromNuPkg(nupkgFilePath);

            return ProcessNuspec(root);             
        }

        /// <summary>
        /// Read .nuspec manifest from the .nupkg package file.
        /// </summary>
        /// <param name="nupkgPath">The nupkg file path.</param>
        /// <returns></returns>
        private static XDocument ReadNuSpecFromNuPkg(string nupkgPath)
        {
            if (nupkgPath == null)
            {
                throw new ArgumentNullException("nupkgPath");
            }

            // We set FileAccess.Read here to ensure a user only needs tne read access permission for find packages.
            // By default, FileStream needs ReadWrite access that’s why 
            // System.UnauthorizedAccessException is thrown if a user does not have write access.
            using (FileStream zipToOpen = new FileStream(nupkgPath, FileMode.Open, FileAccess.Read))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    var entry = archive.Entries.FirstOrDefault(each => each.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
                        each.FullName.IndexOf("/", StringComparison.OrdinalIgnoreCase) == -1);
                    if (entry != null) {
                        return XDocument.Load(entry.Open());
                    }
                }
            }
            return null;
        }

    }  
}
