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
    using System.Xml.Linq;

    /// <summary>
    ///     From the schema:
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
    public class Payload : ResourceCollection {
        internal Payload(XElement element)
            : base(element) {
            if (element.Name != Iso19770_2.Elements.Payload) {
                throw new ArgumentException("Element is not of type 'Payload'", "element");
            }
        }

        internal Payload()
            : base(new XElement(Iso19770_2.Elements.Payload)) {
        }
    }
}