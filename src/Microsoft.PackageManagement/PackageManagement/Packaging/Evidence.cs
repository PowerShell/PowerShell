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
    using System.Globalization;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    ///     From the schema:
    ///     The element is used to provide results from a scan of a system
    ///     where software that does not have a SWID tag is discovered.
    ///     This information is not provided by the software creator, and
    ///     is instead created when a system is being scanned and the
    ///     evidence for why software is believed to be installed on the
    ///     device is provided in the Evidence element.
    /// </summary>
    public class Evidence : ResourceCollection {
        internal Evidence(XElement element)
            : base(element) {
            if (element.Name != Iso19770_2.Elements.Evidence) {
                throw new ArgumentException("Element is not of type 'Evidence'", "element");
            }
        }

        internal Evidence()
            : base(new XElement(Iso19770_2.Elements.Evidence)) {
        }

        /// <summary>
        ///     Date the evidence was gathered.
        /// </summary>
        public DateTime? Date {
            get {
                var v = GetAttribute(Iso19770_2.Attributes.Date);
                if (v != null) {
                    try {
                        return XmlConvert.ToDateTime(v, XmlDateTimeSerializationMode.Utc);
                    } catch {
                    }
                }
                return null;
            }
            internal set {
                if (value == null) {
                    return;
                }
                var v = (DateTime)value;

                AddAttribute(Iso19770_2.Attributes.Date, v.ToUniversalTime().ToString("o", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        ///     Identifier for the device the evidence was gathered from.
        /// </summary>
        public string DeviceId {
            get {
                return GetAttribute(Iso19770_2.Attributes.DeviceId);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.DeviceId, value);
            }
        }
    }
}