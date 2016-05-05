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
    using System.Linq;
    using System.Xml.Linq;
    using Utility.Collections;

    public class ResourceCollection : BaseElement {
        internal ResourceCollection(XElement element)
            : base(element) {
        }

        /// <summary>
        ///     An enumeration of the child directories in the element.
        /// </summary>
        public IEnumerable<Directory> Directories {
            get {
                return Element.Elements(Iso19770_2.Elements.Directory).Select(each => new Directory(each)).ReEnumerable();
            }
        }

        /// <summary>
        ///     An enumeration of the Files contained in the element
        /// </summary>
        public IEnumerable<File> Files {
            get {
                return Element.Elements(Iso19770_2.Elements.File).Select(each => new File(each)).ReEnumerable();
            }
        }

        /// <summary>
        ///     An enumeration of the Process elements in the element.
        /// </summary>
        public IEnumerable<Process> Processes {
            get {
                return Element.Elements(Iso19770_2.Elements.Process).Select(each => new Process(each)).ReEnumerable();
            }
        }

        /// <summary>
        ///     An enumeration of the resource elements in the element.
        /// </summary>
        public IEnumerable<Resource> Resources {
            get {
                return Element.Elements(Iso19770_2.Elements.Resource).Select(each => new Resource(each)).ReEnumerable();
            }
        }

        /// <summary>
        ///     Adds a child directory element.
        /// </summary>
        /// <returns>The newly created directory element</returns>
        internal Directory AddDirectory(string directoryName) {
            return AddElement(new Directory(directoryName));
        }

        /// <summary>
        ///     Adds a file element.
        /// </summary>
        /// <returns>The newly created file element</returns>
        internal File AddFile(string filename) {
            return AddElement(new File(filename));
        }

        /// <summary>
        ///     Adds a new Process element to the element
        /// </summary>
        /// <returns>the newly created process element</returns>
        internal Process AddProcess(string name) {
            return AddElement(new Process(name));
        }

        /// <summary>
        ///     Adds a new Resource element to the element
        /// </summary>
        /// <returns>the newly created Resource element</returns>
        internal Resource AddResource(string type) {
            return AddElement(new Resource(type));
        }
    }
}