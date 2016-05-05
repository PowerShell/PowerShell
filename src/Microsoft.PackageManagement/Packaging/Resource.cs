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
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Linq;

    /// <summary>
    ///     Specifies an arbitrary or custom resource in a swidtag.
    ///     From the swidtag schema:
    ///     A element that can be used to provide arbitrary resource
    ///     information about an application installed on a device, or
    ///     evidence collected from a device.
    /// </summary>
    public class Resource : Meta {
        internal Resource(XElement element)
            : base(element) {
            if (element.Name != Iso19770_2.Elements.Resource) {
                throw new ArgumentException("Element is not of type 'Resource'", "element");
            }
        }

        internal Resource(string type)
            : base(new XElement(Iso19770_2.Elements.Resource)) {
            Type = type;
        }

        /// <summary>
        ///     Specifies the what type of a resource is being recorded.
        ///     From the swidtag schema:
        ///     The type of resource (ie, registrykey, port, rootUrl,etc..)
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethod", Justification = "ISO field name.")]
        public string Type {
            get {
                return GetAttribute(Iso19770_2.Attributes.Type);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Type, value);
            }
        }
    }
}