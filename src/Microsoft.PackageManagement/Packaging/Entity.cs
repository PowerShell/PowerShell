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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Utility.Collections;
    using Utility.Extensions;

    /// <summary>
    ///     From the schema:
    ///     Specifies the organizations related to the software component
    ///     referenced by this SWID tag.
    ///     This has a minOccurs of 1 because the spec declares that
    ///     you must have at least a Entity with role='tagCreator'
    /// </summary>
    public class Entity : BaseElement {
        /// <summary>
        ///     Construct an Entity object tied to an existing element in a document
        /// </summary>
        /// <param name="element"></param>
        internal Entity(XElement element) : base(element) {
            if (element.Name != Iso19770_2.Elements.Entity) {
                throw new ArgumentException("Element is not of type 'Entity'", "element");
            }
        }

        internal Entity(string name, string regId, string role)
            : base(new XElement(Iso19770_2.Elements.Entity)) {
            Name = name;
            RegId = string.IsNullOrWhiteSpace(regId) ? "invalid.unavailable" : regId;
            AddRole(role);
        }

        /// <summary>
        ///     The name of the organization claiming a particular role in the SWIDtag.
        /// </summary>
        public string Name {
            get {
                return GetAttribute(Iso19770_2.Attributes.Name);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Name, value);
            }
        }

        /// <summary>
        ///     An enumeration of the Roles for a given entity.
        ///     From the schema:
        ///     The relationship between this organization and this tag e.g. tag,
        ///     softwareCreator, licensor, tagCreator, etc.  The role of
        ///     tagCreator is required for every SWID tag.
        ///     Role may include any role value, but the pre-defined roles
        ///     include: aggregator, distributor, licensor, softwareCreator,
        ///     tagCreator
        ///     Other roles will be defined as the market uses the SWID tags.
        /// </summary>
        public IEnumerable<string> Roles {
            get {
                var attr = GetAttribute(Iso19770_2.Attributes.Role.LocalName);
                if (attr != null) {
                    return attr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                }
                return Enumerable.Empty<string>();
            }
        }

        public string Role {
            get {
                return GetAttribute(Iso19770_2.Attributes.Role.LocalName);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Role, value);
            }
        }

        /// <summary>
        ///     this value provides a hexadecimal string that contains a hash
        ///     (or thumbprint) of the signing entities certificate.
        /// </summary>
        public string Thumbprint {
            get {
                return GetAttribute(Iso19770_2.Attributes.Thumbprint.LocalName);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Thumbprint, value);
            }
        }

        /// <summary>
        ///     The regid of the organization.  If the regid is unknown, the
        ///     value "invalid.unavailable" is assumed by default (see
        ///     RFC 6761 for more details on the default value).
        /// </summary>
        public string RegId {
            get {
                return GetAttribute(Iso19770_2.Attributes.RegId.LocalName);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.RegId, value);
            }
        }

        /// <summary>
        ///     An enumeration of nested metadata for the Entity
        /// </summary>
        public IEnumerable<Meta> Meta {
            get {
                return Element.Elements(Iso19770_2.Elements.Meta).Select(each => new Meta(each)).ReEnumerable();
            }
        }

        /// <summary>
        ///     adds a role to the element.
        /// </summary>
        /// <param name="role">the role to add</param>
        internal void AddRole(string role) {
            if (string.IsNullOrWhiteSpace(role)) {
                role = string.Empty;
            }

            var attr = GetAttribute(Iso19770_2.Attributes.Role.LocalName);
            if (attr != null) {
                var roles = attr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ConcatSingleItem(role).Distinct();
                role = roles.JoinWith(" ");
            }

            // if there isn't an actual role added; set it to 'unknown'
            if (string.IsNullOrWhiteSpace(role)) {
                role = "unknown";
            }

            Element.SetAttributeValue(Iso19770_2.Attributes.Role, role.Trim());
        }

        /// <summary>
        ///     Adds a nested metadata element to the entity.
        /// </summary>
        /// <returns></returns>
        internal Meta AddMeta() {
            return AddElement(new Meta());
        }
    }
}