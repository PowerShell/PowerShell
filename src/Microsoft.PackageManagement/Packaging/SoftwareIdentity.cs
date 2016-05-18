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

namespace Microsoft.PackageManagement.Packaging {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using Implementation;
    using Internal.Api;
    using Internal.Packaging;
    using Internal.Utility.Collections;
    using Internal.Utility.Extensions;

    /// <summary>
    ///     This class represents a package (retrieved from Find-SoftwareIdentity or Get-SoftwareIdentity)
    ///     Will eventually also represent a swidtag.
    /// </summary>
    public class SoftwareIdentity : Swidtag {
        public SoftwareIdentity(XDocument document)
            : base(document) {
        }

        public SoftwareIdentity() {
        }

        public string FastPackageReference {get; set;}
        internal PackageProvider Provider {get; set;}

        public string ProviderName {
            get {
                return Provider != null ? Provider.ProviderName : null;
            }
        }

        public string Source {get; internal set;}
        public string Status {get; internal set;}
        public string SearchKey {get; internal set;}
        public string FullPath {get; internal set;}
        public string PackageFilename {get; internal set;}
        public bool FromTrustedSource {get; set;}

        public string Summary {
            get {

                return Element.Elements(Iso19770_2.Elements.Meta).Select(each => {
                    var summary = each.GetAttribute(Iso19770_2.Attributes.Summary);
                    if (string.IsNullOrWhiteSpace(summary)) {
                        summary = each.GetAttribute(Iso19770_2.Attributes.Description);
                    }
                    return summary;
                }).WhereNotNull().FirstOrDefault();
            }
            internal set {
                (Meta.FirstOrDefault() ?? AddMeta()).AddAttribute(Iso19770_2.Attributes.Summary, value);
            }
        }

        public IEnumerable<Swidtag> SwidTags {
            get {
                // todo: in the not-too-distant future, the SoftwareIdentity will support a collection of swidtags in addition to 'itself'
                // todo: at that time, we'll add the remaining logic to support collections of swidtags for a given SoftwareIdentity.
                yield return this;
            }
        }

        public string CanonicalId {
            get {
                return CreateCanonicalId(ProviderName, Name, Version, Source);
            }
        }

        public MetadataIndexer Metadata {
            get {
                return new MetadataIndexer(this);
            }
        }

        internal string InstallationPath
        {
            get
            {
                // no payload or directories, just returns empty string
                if (Payload == null || Payload.Directories == null)
                {
                    return string.Empty;
                }

                // find the installation directories based on the payload.
                var installationDirectories = Payload.Directories
                    // Use the directory if it is key, if its location is not null or white space and its name is not null or whitespace
                    .Where(directory => directory.IsKey.HasValue
                        && (bool)directory.IsKey
                        && !string.IsNullOrWhiteSpace(directory.Location)
                        && !string.IsNullOrWhiteSpace(directory.Name))
                    // combine location and name to get the directory
                    .Select(directory => System.IO.Path.Combine(directory.Location, directory.Name));

                // if there is installation directories, concat them with comma
                return installationDirectories.JoinWithComma();
            }
        }

        internal static string CreateCanonicalId(string provider, string name, string version, string source) {
            if (provider == null || name == null) {
                return null;
            }
            if (string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(source)) {
                return "{0}:{1}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(provider), name);
            }
            if (string.IsNullOrWhiteSpace(source)) {
                return "{0}:{1}/{2}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(provider), name, version);
            }
            if (string.IsNullOrWhiteSpace(version)) {
                "{0}:{1}#{2}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(provider), name, source);
            }

            return "{0}:{1}/{2}#{3}".format(CultureInfo.CurrentCulture.TextInfo.ToLower(provider), name, version, source);
        }

        public void FetchPackageDetails(IHostApi api) {
            Provider.GetPackageDetails(this, api);
        }

        /// <summary>
        ///     Sets a SoftwareMetadata value
        /// </summary>
        /// <param name="metaKey"></param>
        /// <param name="value"></param>
        internal void AddMetadataAttribute(string metaKey, string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                // we don't store empty values.
                return;
            }

            var metaElements = Element.Elements(Iso19770_2.Elements.Meta).ReEnumerable();
            var currentValue = metaElements.Select(each => each.GetAttribute(metaKey)).WhereNotNull().FirstOrDefault();

            // if the current value is already the value, then don't worry about it.
            if (currentValue == value) {
                return;
            }

            if (metaElements.Any() && currentValue == null) {
                // value has not been set (and we have at least one metadata element)
                // let's set it in the first meta element.
                Element.Elements(Iso19770_2.Elements.Meta).FirstOrDefault().AddAttribute(metaKey, value);
            } else {
                // add a new metadata object at the end and set the value in that element.
                AddElement(new Meta())
                    .AddAttribute(metaKey, value);
            }
        }

        protected override XElement FindElementWithUniqueId(string elementId) {
            if (elementId == FastPackageReference) {
                return Element;
            }
            return base.FindElementWithUniqueId(elementId);
        }

        internal string AddMeta(string elementPath) {
            var element = FindElementWithUniqueId(elementPath);
            if (element != null) {
                switch (element.Name.LocalName) {
                    case "SoftwareIdentity":
                        // adds a SoftwareMeta to the swidtag
                        return AddElement(new SoftwareMetadata()).ElementUniqueId;

                    case "Entity":
                        // adds a Meta to the entity
                        return new Entity(element).AddMeta().ElementUniqueId;
                }
            }
            return null;
        }

        internal string AddLink(Uri referenceUri, string relationship, string mediaType, string ownership, string use, string appliesToMedia, string artifact) {
            var link = AddLink(referenceUri, relationship);

            if (!string.IsNullOrWhiteSpace(mediaType)) {
                link.MediaType = mediaType;
            }

            if (!string.IsNullOrWhiteSpace(ownership)) {
                link.Ownership = ownership;
            }

            if (!string.IsNullOrWhiteSpace(use)) {
                link.Use = use;
            }

            if (!string.IsNullOrWhiteSpace(appliesToMedia)) {
                link.Media = appliesToMedia;
            }

            if (!string.IsNullOrWhiteSpace(artifact)) {
                link.Artifact = artifact;
            }

            return link.ElementUniqueId;
        }

        internal string AddEntity(string name, string regid, string role, string thumbprint) {
            var entity = AddEntity(name, regid, role);

            if (!string.IsNullOrWhiteSpace(thumbprint) && entity.Thumbprint == null) {
                entity.Thumbprint = thumbprint;
            }

            return entity.ElementUniqueId;
        }

        public string AddMetadataValue(string elementPath, Uri @namespace, string name, string value) {
            if (@namespace == null) {
                return null;
            }
            var element = FindElementWithUniqueId(elementPath);
            if (element != null) {
                element.AddAttribute(XNamespace.Get(@namespace.ToString()) + name, value);
                return PathToElement(element);
            }
            return null;
        }

        internal string AddMetadataValue(string elementPath, string name, string value) {
            var element = FindElementWithUniqueId(elementPath);
            if (element == null || string.IsNullOrWhiteSpace(name)) {
                return null;
            }

            if (element == Element) {
                // special case:
                if (name.EqualsIgnoreCase("FromTrustedSource")) {
                    FromTrustedSource = (value ?? string.Empty).IsTrue();
                    return FastPackageReference;
                }

                // metadata values on the swidtag go to the first Meta
                var meta = (Meta.FirstOrDefault() ?? AddMeta());
                meta.AddAttribute(name, value);
                return meta.ElementUniqueId;
            }

            if (element.Name == Iso19770_2.Elements.Entity) {
                // metadata values on entities go to the first Meta in the entity
                var entity = new Entity(element);
                var meta = (entity.Meta.FirstOrDefault() ?? entity.AddMeta());
                meta.AddAttribute(name, value);
                return meta.ElementUniqueId;
            }

            // for non-namespaced metadata values, the target element must be one that inherits from Meta
            if (IsMetaElement(element)) {
                var meta = new Meta(element);
                meta.AddAttribute(name, value);
                return meta.ElementUniqueId;
            }

            return null;
        }

        internal string AddResource(string elementPath, string type) {
            var element = FindElementWithUniqueId(elementPath);
            if (element == null || string.IsNullOrWhiteSpace(type)) {
                return null;
            }
            if (element.Name == Iso19770_2.Elements.Payload || element.Name == Iso19770_2.Elements.Evidence) {
                return new ResourceCollection(element).AddResource(type).ElementUniqueId;
            }
            return null;
        }

        internal string AddProcess(string elementPath, string processName, int pid) {
            var element = FindElementWithUniqueId(elementPath);
            if (element == null || string.IsNullOrWhiteSpace(processName)) {
                return null;
            }
            if (element.Name == Iso19770_2.Elements.Payload || element.Name == Iso19770_2.Elements.Evidence) {
                var process = new ResourceCollection(element).AddProcess(processName);
                if (pid != 0) {
                    process.Pid = pid;
                }
                return process.ElementUniqueId;
            }
            return null;
        }

        internal string AddFile(string elementPath, string fileName, string location, string root, bool isKey, long size, string version) {
            var element = FindElementWithUniqueId(elementPath);
            if (element == null || string.IsNullOrWhiteSpace(fileName)) {
                return null;
            }
            if (element.Name == Iso19770_2.Elements.Payload || element.Name == Iso19770_2.Elements.Evidence || element.Name == Iso19770_2.Elements.Directory) {
                var file = new ResourceCollection(element).AddFile(fileName);
                if (!string.IsNullOrWhiteSpace(location)) {
                    file.Location = location;
                }
                if (!string.IsNullOrWhiteSpace(root)) {
                    file.Root = root;
                }
                if (isKey) {
                    file.IsKey = true;
                }
                if (size > 0) {
                    file.Size = size;
                }
                if (!string.IsNullOrWhiteSpace(version)) {
                    file.Version = version;
                }
                return file.ElementUniqueId;
            }
            return null;
        }

        internal string AddDirectory(string elementPath, string directoryName, string location, string root, bool isKey) {
            var element = FindElementWithUniqueId(elementPath);
            if (element == null || string.IsNullOrWhiteSpace(directoryName)) {
                return null;
            }
            if (element.Name == Iso19770_2.Elements.Payload || element.Name == Iso19770_2.Elements.Evidence || element.Name == Iso19770_2.Elements.Directory) {
                var file = new ResourceCollection(element).AddDirectory(directoryName);
                if (!string.IsNullOrWhiteSpace(location)) {
                    file.Location = location;
                }
                if (!string.IsNullOrWhiteSpace(root)) {
                    file.Root = root;
                }
                if (isKey) {
                    file.IsKey = true;
                }
                return file.ElementUniqueId;
            }
            return null;
        }

        internal Evidence AddEvidence(DateTime date, string deviceId) {
            var evidence = AddEvidence();
            evidence.Date = date;
            evidence.DeviceId = deviceId;
            return evidence;
        }

        public string AddDependency(string providerName, string packageName, string version, string source, string appliesTo) {
            return AddLink(new Uri(CreateCanonicalId(providerName, packageName, version, source)), Iso19770_2.Relationship.Requires, null, null, null, appliesTo, null);
        }
    }
}