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

    public class Process : Meta {
        internal Process(XElement element)
            : base(element) {
            if (element.Name != Iso19770_2.Elements.Process) {
                throw new ArgumentException("Element is not of type 'Process'", "element");
            }
        }

        internal Process(string processName)
            : base(new XElement(Iso19770_2.Elements.Process)) {
            Name = processName;
        }

        /// <summary>
        ///     From the swidtag schema:
        ///     The process name as it will be found in the devices process table.
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
        ///     From the swidtag schema:
        ///     The file size in bytes of the file
        /// </summary>
        public int? Pid {
            get {
                var sz = GetAttribute(Iso19770_2.Attributes.Size);
                if (sz != null) {
                    int result;
                    if (Int32.TryParse(sz, out result)) {
                        return result;
                    }
                }
                return null;
            }
            internal set {
                if (value != null) {
                    AddAttribute(Iso19770_2.Attributes.Pid, value.ToString());
                }
            }
        }
    }
}