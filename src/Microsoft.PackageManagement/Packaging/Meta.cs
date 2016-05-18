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
    using System.Xml.Linq;

    /// <summary>
    ///     An intermediate base class for elements in the swidtag that support
    ///     arbitrary attributes in the default namespace.
    ///     From the swidtag schema:
    ///     An open-ended collection of key/value data related to this SWID.
    ///     Permits any user-defined attributes in Meta tags
    /// </summary>
    public class Meta : BaseElement {
        internal Meta(XElement element)
            : base(element) {
            // we don't do an Element type validation here, since this will get called from
            // child classes too.
            // and that's ok, because Meta doesn't directly have any predefined fields anyway.
        }

        internal Meta()
            : base(new XElement(Iso19770_2.Elements.Meta)) {
        }

        public string this[string key] {
            get {
                return Attributes[key];
            }
        }

        public override string ToString() {
            return Attributes != null ? Attributes.ToString() : string.Empty;
        }

        /// <summary>
        ///     Determines if the element contains an attribute with the given name.
        /// </summary>
        /// <param name="key">the attribute to find</param>
        /// <returns>True, if the element contains the attribute.</returns>
        public bool ContainsKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) {
                return false;
            }

            return Element.Attribute(key) != null;
        }
    }
}