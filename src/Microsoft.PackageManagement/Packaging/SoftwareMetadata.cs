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
    using Utility.Extensions;

    public class SoftwareMetadata : Meta {
        internal SoftwareMetadata(XElement element) : base(element) {
            if (element.Name != Iso19770_2.Elements.Meta) {
                throw new ArgumentException("Element is not of type 'SoftwareMetadata'", "element");
            }
        }

        internal SoftwareMetadata()
            : base(new XElement(Iso19770_2.Elements.Meta)) {
        }

        public string ActivationStatus {
            get {
                return GetAttribute(Iso19770_2.Attributes.ActivationStatus);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.ActivationStatus, value);
            }
        }

        public string ChannelType {
            get {
                return GetAttribute(Iso19770_2.Attributes.ChannelType);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.ChannelType, value);
            }
        }

        public string Description {
            get {
                return GetAttribute(Iso19770_2.Attributes.Description);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Description, value);
            }
        }

        public string ColloquialVersion {
            get {
                return GetAttribute(Iso19770_2.Attributes.ColloquialVersion);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.ColloquialVersion, value);
            }
        }

        public string Edition {
            get {
                return GetAttribute(Iso19770_2.Attributes.Edition);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Edition, value);
            }
        }

        public string EntitlementKey {
            get {
                return GetAttribute(Iso19770_2.Attributes.EntitlementKey);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.EntitlementKey, value);
            }
        }

        public string Generator {
            get {
                return GetAttribute(Iso19770_2.Attributes.Generator);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Generator, value);
            }
        }

        public string PersistentId {
            get {
                return GetAttribute(Iso19770_2.Attributes.PersistentId);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.PersistentId, value);
            }
        }

        public string Product {
            get {
                return GetAttribute(Iso19770_2.Attributes.Product);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Product, value);
            }
        }

        public string ProductFamily {
            get {
                return GetAttribute(Iso19770_2.Attributes.ProductFamily);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.ProductFamily, value);
            }
        }

        public string Revision {
            get {
                return GetAttribute(Iso19770_2.Attributes.Revision);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.Revision, value);
            }
        }

        public string UnspscCode {
            get {
                return GetAttribute(Iso19770_2.Attributes.UnspscCode);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.UnspscCode, value);
            }
        }

        public string UnspscVersion {
            get {
                return GetAttribute(Iso19770_2.Attributes.UnspscVersion);
            }
            internal set {
                AddAttribute(Iso19770_2.Attributes.UnspscVersion, value);
            }
        }

        public bool? EntitlementDataRequired {
            get {
                return GetAttribute(Iso19770_2.Attributes.EntitlementDataRequired).IsTruePreserveNull();
            }
            internal set {
                if (value != null) {
                    AddAttribute(Iso19770_2.Attributes.EntitlementDataRequired, value.ToString());
                }
            }
        }

        public override string ToString() {
            return Attributes.ToString();
        }
    }
}