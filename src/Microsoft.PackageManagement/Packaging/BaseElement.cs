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
    using System.Xml;
    using System.Xml.Linq;
    using Utility.Extensions;

    /// <summary>
    ///     The base element that is common to all elements in a Swidtag.
    ///     Swidtag classes are intended to be constructed, but are not mutable
    ///     (ie, can't be created and have values modified or removed)
    /// </summary>
    public class BaseElement {
        private AttributeIndexer _attributeIndexer;
        private string _uniqueId;
        protected internal XElement Element;

        protected internal BaseElement(XElement element) {
            Element = element;
        }

        protected internal BaseElement(XDocument element) {
            if (element != null) {
                Element = element.Root;
            }
        }

        internal string ElementUniqueId {
            get {
                return _uniqueId ?? (_uniqueId = PathToElement(Element));
            }
        }

        /// <summary>
        ///     All Swidtag elements can explicitly support the xml:lang attribute.
        /// </summary>
        public string Culture {
            get {
                var xmlLang =  GetAttribute(Iso19770_2.Attributes.XmlLang);               
                if (string.IsNullOrWhiteSpace(xmlLang))
                {
                    xmlLang = GetAttribute(Iso19770_2.Attributes.Lang);
                }
                return xmlLang;               
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.XmlLang, value);
            }
        }

        public AttributeIndexer Attributes {
            get {
                return _attributeIndexer ?? (_attributeIndexer = new AttributeIndexer(Element));
            }
        }

        protected static bool IsMetaElement(XElement element) {
            return Iso19770_2.Elements.MetaElements.Contains(element.Name);
        }

        private static string PathToElement(string parent, XElement element) {
            return "{0}/{1}[{2}]".format(parent, element.Name.LocalName, element.ElementsBeforeSelf(element.Name).Count() + 1);
        }

        protected static string PathToElement(XElement element) {
            if (element.Parent != null) {
                return PathToElement(PathToElement(element.Parent), element);
            }
            return "/" + element.Name.LocalName;
        }

        private static XElement FindChildElementViaPath(string parentPath, XElement element, string elementId) {
            foreach (var each in element.Elements()) {
                var eId = PathToElement(parentPath, each);
                if (elementId == eId) {
                    return each;
                }

                if (elementId.IndexOf(eId, StringComparison.CurrentCulture) == 0)
                {
                    return FindChildElementViaPath(eId, each, elementId);
                }
            }
            return null;
        }

        protected virtual XElement FindElementWithUniqueId(string elementId) {
            if (elementId == ElementUniqueId) {
                return Element;
            }
            return FindChildElementViaPath(ElementUniqueId, Element, elementId);
        }

        /// <summary>
        ///     Internal method to gain access to attributes.
        ///     Returns Null if the attribute is not present.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        internal string GetAttribute(XName attribute) {
            return Element.GetAttribute(attribute);
        }

        /// <summary>
        ///     Adds an attribute to this element.
        /// </summary>
        /// <param name="attribute">name of the attribute</param>
        /// <param name="value">value of the attribute</param>
        /// <returns>The current element (enables fluent usage)</returns>
        internal XElement AddAttribute(XName attribute, string value) {
            return Element.AddAttribute(attribute, value);
        }

        /// <summary>
        ///     Adds a new element as a child to the current element
        /// </summary>
        /// <param name="swidElement">the swid element to add</param>
        /// <returns>The newly added element</returns>
        internal T AddElement<T>(T swidElement) where T : BaseElement {
            Element.Add(swidElement.Element);
            return swidElement;
        }

        /// <summary>
        ///     Checks if the element contains an attribute with the given key
        /// </summary>
        /// <param name="key">the key to find</param>
        /// <returns>True if the element contains the key.</returns>
        public bool ContainsKey(XName key) {
            if (key == null || string.IsNullOrWhiteSpace(key.LocalName)) {
                return false;
            }
            return Element.Attribute(key) != null;
        }
    }
}