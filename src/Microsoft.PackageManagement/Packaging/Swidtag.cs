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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Utility.Collections;
    using Utility.Extensions;

    public class Swidtag : BaseElement {
        internal XDocument _swidTag;

        public Swidtag(XDocument document)
            : base(document) {
            _swidTag = document;
            ChangeCurrentNameSpace();
        }

        public Swidtag()
            : this(new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(Iso19770_2.Elements.SoftwareIdentity))) {
        }

        public Swidtag(XElement xmlDocument)
            : this(new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                xmlDocument)) {
        }

        internal void SetSwidTag(XDocument swidTag)
        {
            _swidTag = swidTag;
            ChangeCurrentNameSpace();
            Element = _swidTag.Root;
        }

        /// <summary>
        /// This method is called to change namespace from Iso19770_2_Current to Iso19770_2
        /// </summary>
        private void ChangeCurrentNameSpace()
        {
            if (_swidTag.Root.GetDefaultNamespace() == Iso19770_2.Namespace.Iso19770_2_Current)
            {
                // change namespace of the document
                // this is based on https://msdn.microsoft.com/en-us/library/bb943914.aspx

                foreach (XElement el in _swidTag.Root.DescendantsAndSelf())
                {

                    el.Name = Iso19770_2.Namespace.Iso19770_2.GetName(el.Name.LocalName);

                    List<XAttribute> atList = el.Attributes().ToList();

                    el.Attributes().Remove();
                    
                    foreach (XAttribute at in atList)
                    {
                        el.Add(new XAttribute(Iso19770_2.Namespace.Iso19770_2.GetName(at.Name.LocalName), at.Value));
                    }
                }
            }
        }

        public string SwidTagText {
            get {
                var stringBuilder = new StringBuilder();

                var settings = new XmlWriterSettings {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    NewLineOnAttributes = true,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates
                };

                using (var xmlWriter = XmlWriter.Create(stringBuilder, settings)) {
                    _swidTag.Save(xmlWriter);
                }

                return stringBuilder.ToString();
            }
        }

        public static bool IsSwidtag(XElement xmlDocument) {
            return xmlDocument.Name == Iso19770_2.Elements.SoftwareIdentity || xmlDocument.Name == Iso19770_2.Elements.SoftwareIdentityCurrent;
        }

        public bool IsApplicable(Hashtable environment) {
            return MediaQuery.IsApplicable(AppliesToMedia, environment);
        }

        public IEnumerable<string> Dependencies
        {
            get
            {
                return Links.Where(each => Iso19770_2.Relationship.Requires == each.Relationship).Select(each => each.HRef).WhereNotNull().Select(each => Uri.UnescapeDataString(each.ToString())).ReEnumerable();
            }
        }

        #region Attributes

        public bool? IsCorpus {
            get {
                return GetAttribute(Iso19770_2.Attributes.Corpus).IsTruePreserveNull();
            }
            internal set {
                if (value != null) {
                    AddAttribute(Iso19770_2.Attributes.Corpus, value.ToString());
                }
            }
        }

        public virtual string Name {
            get {
                return GetAttribute(Iso19770_2.Attributes.Name);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Name, value);
            }
        }

        public string Version {
            get {
                return GetAttribute(Iso19770_2.Attributes.Version);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Version, value);
            }
        }

        public string VersionScheme {
            get {
                return GetAttribute(Iso19770_2.Attributes.VersionScheme);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.VersionScheme, value);
            }
        }

        public string TagVersion {
            get {
                return GetAttribute(Iso19770_2.Attributes.TagVersion);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.TagVersion, value);
            }
        }

        public string TagId {
            get {
                return GetAttribute(Iso19770_2.Attributes.TagId);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.TagId, value);
            }
        }

        public bool? IsPatch {
            get {
                return GetAttribute(Iso19770_2.Attributes.Patch).IsTruePreserveNull();
            }
            internal set {
                if (value != null) {
                    AddAttribute(Iso19770_2.Attributes.Patch, value.ToString());
                }
            }
        }

        public bool? IsSupplemental {
            get {
                return GetAttribute(Iso19770_2.Attributes.Supplemental).IsTruePreserveNull();
            }
            internal set {
                if (value != null) {
                    AddAttribute(Iso19770_2.Attributes.Supplemental, value.ToString());
                }
            }
        }

        public string AppliesToMedia {
            get {
                return GetAttribute(Iso19770_2.Attributes.Media);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Media, value);
            }
        }

        #endregion

        #region Elements

        public IEnumerable<SoftwareMetadata> Meta {
            get {
                return Element.Elements(Iso19770_2.Elements.Meta).Select(each => new SoftwareMetadata(each)).ReEnumerable();
            }
        }

        internal SoftwareMetadata AddMeta() {
            return AddElement(new SoftwareMetadata());
        }

        public IEnumerable<Link> Links {
            get {
                return Element.Elements(Iso19770_2.Elements.Link).Select(each => new Link(each)).ReEnumerable();
            }
        }

        internal Link AddLink(Uri referenceUri, string relationship) {
            return AddElement(new Link(referenceUri, relationship));
        }

        public IEnumerable<Entity> Entities {
            get {
                return Element.Elements(Iso19770_2.Elements.Entity).Select(each => new Entity(each)).ReEnumerable();
            }
        }

        internal Entity AddEntity(string name, string regId, string role) {
            return AddElement(new Entity(name, regId, role));
        }

        /// <summary>
        ///     A ResourceCollection for the 'Payload' of the SwidTag.
        ///     This value is null if the swidtag does not contain a Payload element.
        ///     from swidtag XSD:
        ///     The items that may be installed on a device when the software is
        ///     installed.  Note that Payload may be a superset of the items
        ///     installed and, depending on optimization systems for a device,
        ///     may or may not include every item that could be created or
        ///     executed on a device when software is installed.
        ///     In general, payload will be used to indicate the files that
        ///     may be installed with a software product and will often be a
        ///     superset of those files (i.e. if a particular optional
        ///     component is not installed, the files associated with that
        ///     component may be included in payload, but not installed on
        ///     the device).
        /// </summary>
        public Payload Payload {
            get {
                return Element.Elements(Iso19770_2.Elements.Payload).Select(each => new Payload(each)).FirstOrDefault();
            }
        }

        /// <summary>
        ///     Adds a Payload resource collection element.
        /// </summary>
        /// <returns>The ResourceCollection added. If the Payload already exists, returns the current Payload.</returns>
        internal Payload AddPayload() {
            // should we just detect and add the evidence element when a provider is adding items to the evidence
            // instead of requiring someone to explicitly add the element?
            if (Element.Elements(Iso19770_2.Elements.Payload).Any()) {
                return Payload;
            }
            return AddElement(new Payload());
        }

        /// <summary>
        ///     An Evidence element representing the discovered for a swidtag.
        ///     This value is null if the swidtag does not contain an Evidence element
        ///     from swidtag XSD:
        ///     The element is used to provide results from a scan of a system
        ///     where software that does not have a SWID tag is discovered.
        ///     This information is not provided by the software creator, and
        ///     is instead created when a system is being scanned and the
        ///     evidence for why software is believed to be installed on the
        ///     device is provided in the Evidence element.
        /// </summary>
        public Evidence Evidence {
            get {
                return Element.Elements(Iso19770_2.Elements.Evidence).Select(each => new Evidence(each)).FirstOrDefault();
            }
        }

        /// <summary>
        ///     Adds an Evidence element.
        /// </summary>
        /// <returns>The added Evidence element. If the Evidence element already exists, returns the current element.</returns>
        internal Evidence AddEvidence() {
            // should we just detect and add the evidence element when a provider is adding items to the evidence
            // instead of requiring someone to explicitly add the element?
            if (Element.Elements(Iso19770_2.Elements.Evidence).Any()) {
                return Evidence;
            }
            return AddElement(new Evidence());
        }

        #endregion
    }
}