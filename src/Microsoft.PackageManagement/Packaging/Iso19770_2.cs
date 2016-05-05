// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Packaging {
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Utility.Extensions;

    internal static class Iso19770_2 {

        internal static XAttribute SwidtagNamespace {
            get {
                return new XAttribute(XNamespace.Xmlns + "swid", Namespace.Iso19770_2);
            }
        }

        /// <summary>
        ///     Gets the attribute value for a given element.
        /// </summary>
        /// <param name="element">the element that possesses the attribute</param>
        /// <param name="attribute">the attribute to find</param>
        /// <returns>the string value of the element. Returns null if the element or attribute does not exist.</returns>
        internal static string GetAttribute(this XElement element, XName attribute) {
            if (element == null || attribute == null || string.IsNullOrWhiteSpace(attribute.ToString())) {
                return null;
            }

            XAttribute result;

            // no name space, just check local name
            if (string.IsNullOrWhiteSpace(attribute.Namespace.NamespaceName))
            {
                result = element.Attributes().Where(attr => attr != null && string.Equals(attr.Name.LocalName, attribute.LocalName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            else
            {
                result = element.Attribute(attribute);
            }

            return result == null ? null : result.Value;
        }

        /// <summary>
        ///     Adds a new attribute to the element
        ///     Does not permit modification of an existing attribute.
        ///     Does not add empty or null attributes or values.
        /// </summary>
        /// <param name="element">The element to add the attribute to</param>
        /// <param name="attribute">The attribute to add</param>
        /// <param name="value">the value of the attribute to add</param>
        /// <returns>The element passed in. (Permits fluent usage)</returns>
        internal static XElement AddAttribute(this XElement element, XName attribute, string value) {
            if (element == null) {
                return null;
            }

            // we quietly ignore attempts to add empty data or attributes.
            if (string.IsNullOrWhiteSpace(value) || attribute == null || string.IsNullOrWhiteSpace(attribute.ToString())) {
                return element;
            }

            // Swidtag attributes can be added but not changed -- if it already exists, that's not permitted.
            var current = element.GetAttribute(attribute);
            if (!string.IsNullOrWhiteSpace(current)) {
                if (value != current) {
                    throw new Exception("Attempt to change Attribute '{0}' present in element '{1}'".format(attribute.LocalName, element.Name.LocalName));
                }

                // if the value was set to that already, don't worry about it.
                return element;
            }

            element.SetAttributeValue(attribute, value);

            return element;
        }

        internal static class Attributes {
            internal static readonly XName Name = "name";
            internal static readonly XName Patch = "patch";
            internal static readonly XName Media = "media";
            internal static readonly XName Supplemental = "supplemental";
            internal static readonly XName TagVersion = "tagVersion";
            internal static readonly XName TagId = "tagId";
            internal static readonly XName Version = "version";
            internal static readonly XName VersionScheme = "versionScheme";
            internal static readonly XName Corpus = "corpus";
            internal static readonly XName Summary = "summary";
            internal static readonly XName Description = "description";
            internal static readonly XName ActivationStatus = "activationStatus";
            internal static readonly XName ChannelType = "channelType";
            internal static readonly XName ColloquialVersion = "colloquialVersion";
            internal static readonly XName Edition = "edition";
            internal static readonly XName EntitlementDataRequired = "entitlementDataRequired";
            internal static readonly XName EntitlementKey = "entitlementKey";
            internal static readonly XName Generator = "generator";
            internal static readonly XName PersistentId = "persistentId";
            internal static readonly XName Product = "product";
            internal static readonly XName ProductFamily = "productFamily";
            internal static readonly XName Revision = "revision";
            internal static readonly XName UnspscCode = "unspscCode";
            internal static readonly XName UnspscVersion = "unspscVersion";
            internal static readonly XName RegId = "regId";
            internal static readonly XName Role = "role";
            internal static readonly XName Thumbprint = "thumbprint";
            internal static readonly XName HRef = "href";
            internal static readonly XName Relationship = "rel";
            internal static readonly XName MediaType = "type";
            internal static readonly XName Ownership = "ownership";
            internal static readonly XName Use = "use";
            internal static readonly XName Artifact = "artifact";
            internal static readonly XName Type = "type";
            internal static readonly XName Key = "key";
            internal static readonly XName Root = "root";
            internal static readonly XName Location = "location";
            internal static readonly XName Size = "size";
            internal static readonly XName Pid = "pid";
            internal static readonly XName Date = "date";
            internal static readonly XName DeviceId = "deviceId";
            internal static readonly XName XmlLang = Namespace.Xml + "lang";
            internal static readonly XName Lang = "lang";
        }

        internal static class Discovery {
            internal static readonly XName Name = Namespace.Discovery + "name";

            // Feed Link Extended attributes: 
            internal static readonly XName MinimumName = Namespace.Discovery + "min-name";
            internal static readonly XName MaximumName = Namespace.Discovery + "max-name";
            internal static readonly XName MinimumVersion = Namespace.Discovery + "min-version";
            internal static readonly XName MaximumVersion = Namespace.Discovery + "max-version";
            internal static readonly XName Keyword = Namespace.Discovery + "keyword";

            // Package Link Extended Attributes 
            internal static readonly XName Version = Namespace.Discovery + "version";
            internal static readonly XName Latest = Namespace.Discovery + "latest";
            internal static readonly XName TargetFilename = Namespace.Discovery + "targetFilename";
            internal static readonly XName Type = Namespace.Discovery + "type";

        }

        internal static class Hash {
            internal static readonly XName Hash512 = Namespace.Sha512 + "hash";
            internal static readonly XName Hash256 = Namespace.Sha256 + "hash";
            internal static readonly XName Md5 = Namespace.Md5 + "hash";
            
        }

        internal static class HashAlgorithm
        {
            internal static readonly string Sha512 = "sha512";
            internal static readonly string Sha256 = "sha256";
            internal static readonly string Md5 = "md5";
        }


        internal static class MediaType {
            internal const string PackageReference = "application/vnd.packagemanagement-canonicalid";

            internal const string SwidTagXml = "application/swid-tag+xml";
            internal const string SwidTagJsonLd = "application/swid-tag+json-ld";

            internal const string MsiPackage = "application/vnd.ms.msi-package";
            internal const string MsuPackage  = "application/vnd.ms.msu-package";

            internal const string ExePackage = "application/vnd.packagemanagement.exe-package";
            internal const string ZipPackage = "application/epub+zip";
            internal const string NuGetPackage = "application/vnd.packagemanagement.nuget-package";
            internal const string ChocolateyPackage = "application/vnd.packagemanagement.chocolatey-package";

        }

        internal static class Relationship {
            internal const string Requires = "requires";
            internal const string InstallationMedia = "installationmedia";
            internal const string Component = "component";
            internal const string Supplemental = "supplemental";
            internal const string Parent = "parent";
            internal const string Ancestor = "ancestor";
            
            // Package Discovery Relationships:
            internal const string Feed = "feed";        // should point to a swidtag the represents a feed of packages
            internal const string Package = "package";  // should point ot a swidtag that represents an installation package
        }

        internal static class Role {
            internal const string Aggregator = "aggregator";
            internal const string Distributor = "distributor";
            internal const string Licensor = "licensor";
            internal const string SoftwareCreator = "softwareCreator";
            internal const string Author = "author";
            internal const string Contributor = "contributor";
            internal const string Publisher = "publisher";
            internal const string TagCreator = "tagCreator";
        }

        internal static class Use {
            internal const string Required = "required";
            internal const string Recommended = "recommended";
            internal const string Optional = "optional";
        }

        internal static class VersionScheme {
            internal const string Alphanumeric = "alphanumeric";
            internal const string Decimal = "decimal";
            internal const string MultipartNumeric = "multipartnumeric";
            internal const string MultipartNumericPlusSuffix = "multipartnumeric+suffix";
            internal const string SemVer = "semver";
            internal const string Unknown = "unknown";
        }

        internal static class Ownership {
            internal const string Abandon = "abandon";
            internal const string Private = "private";
            internal const string Shared = "shared";
        }

        internal static class Namespace {
            internal static readonly XNamespace Iso19770_2 = XNamespace.Get("http://standards.iso.org/iso/19770/-2/2015/schema.xsd");
            internal static readonly XNamespace Iso19770_2_Current = XNamespace.Get("http://standards.iso.org/iso/19770/-2/2015-current/schema.xsd");
            internal static readonly XNamespace Discovery = XNamespace.Get("http://packagemanagement.org/discovery");
            internal static readonly XNamespace OneGet = XNamespace.Get("http://oneget.org/packagemanagement");
            internal static readonly XNamespace Xml = XNamespace.Get("http://www.w3.org/XML/1998/namespace");
            internal static XNamespace XmlDsig = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");
            internal static readonly XNamespace Sha512 = XNamespace.Get("http://www.w3.org/2001/04/xmlenc#sha512");
            internal static readonly XNamespace Sha256 = XNamespace.Get("http://www.w3.org/2001/04/xmlenc#sha256");
            internal static readonly XNamespace Md5 = XNamespace.Get("http://www.w3.org/2001/04/xmldsig-more#md5");            
        }

        internal static class Elements {
            internal static readonly XName SoftwareIdentity = Namespace.Iso19770_2 + "SoftwareIdentity";
            internal static readonly XName SoftwareIdentityCurrent = Namespace.Iso19770_2_Current + "SoftwareIdentity";
            internal static readonly XName Entity = Namespace.Iso19770_2 + "Entity";
            internal static readonly XName Link = Namespace.Iso19770_2 + "Link";
            internal static readonly XName Evidence = Namespace.Iso19770_2 + "Evidence";
            internal static readonly XName Payload = Namespace.Iso19770_2 + "Payload";
            internal static readonly XName Meta = Namespace.Iso19770_2 + "Meta";
            internal static readonly XName Directory = Namespace.Iso19770_2 + "Directory";
            internal static readonly XName File = Namespace.Iso19770_2 + "File";
            internal static readonly XName Process = Namespace.Iso19770_2 + "Process";
            internal static readonly XName Resource = Namespace.Iso19770_2 + "Resource";

            internal static readonly XName[] MetaElements = {
                Meta, Directory, File, Process, Resource
            };
        }
    }
}