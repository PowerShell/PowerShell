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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Linq;
    using Utility.Collections;
    using Utility.Extensions;

    public class MetadataIndexer {
        private readonly Swidtag _swidtag;

        public MetadataIndexer(Swidtag swidtag) {
            _swidtag = swidtag;
        }

        /// <summary>
        ///     Returns the string value of a given attribute.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>the string value of a given attribute. If the attribute does not exist, returns null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers", Justification = "It's a collection of attributes.")]
        public IEnumerable<string> this[string key] {
            get {
                if (string.IsNullOrWhiteSpace(key)) {
                    return null;
                }
                return _swidtag.Meta.Where(each => {
                    try {
                        return (each.Attributes[key] != null);
                    } catch {
                        // happens when key is not a legal attribute name
                    }
                    return false;
                }).Select(each => each.Attributes[key]).ReEnumerable();
            }
        }

        /// <summary>
        ///     An enumeration of all the attributes in this element.
        /// </summary>
        public IEnumerable<XName> Keys {
            get {
                return _swidtag.Meta.SelectMany(each => each.Attributes.Keys).ReEnumerable();
            }
        }

        /// <summary>
        ///     An enumeration of all the attribute values in this element.
        /// </summary>
        public IEnumerable<string> Values {
            get {
                return _swidtag.Meta.SelectMany(each => each.Attributes.Values).ReEnumerable();
            }
        }

        /// <summary>
        ///     The count of the attributes in this element
        /// </summary>
        public int Count {
            get {
                return Values.Count();
            }
        }

        public override string ToString() {
            return "{{{0}}}".format(Keys.Select(each => each.ToString()).JoinWithComma());
        }
    }
}