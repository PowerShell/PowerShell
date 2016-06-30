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

namespace Microsoft.PackageManagement.Internal.Utility.Platform {
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Linq;

    internal static class Manifest {
        private static readonly byte[] _utf = {0xef, 0xbb, 0xbf};

#if !LINUX
        public static IEnumerable<XElement> LoadFrom(string filename) {
            var manifests = new List<XElement>();

            using (DisposableModule dll = NativeMethods.LoadLibraryEx(filename, Unused.Nothing, LoadLibraryFlags.AsImageResource | LoadLibraryFlags.AsDatafile)) {
                // if we get back a valid module handle
                if (!dll.IsInvalid) {
                    // search all the 'manifest' resources
                    if (NativeMethods.EnumResourceNamesEx(dll, ResourceType.Manifest, (m, type, id, param) => {
                        // for each manifest, check the language
                        NativeMethods.EnumResourceLanguagesEx(m, type, id, (m1, resourceType, resourceId, language, unused) => {
                            // find the specific resource
                            var resource = NativeMethods.FindResourceEx(m1, resourceType, resourceId, language);
                            if (!resource.IsInvalid) {
                                // get a handle to the resource data
                                var resourceData = NativeMethods.LoadResource(m1, resource);
                                if (!resourceData.IsInvalid) {
                                    // copy the resource text out of the resource data
                                    try {
                                        var dataSize = NativeMethods.SizeofResource(m1, resource);
                                        var dataPointer = NativeMethods.LockResource(resourceData);

                                        // make sure that the pointer and size are legit.
                                        if (dataSize > 0 && dataPointer != IntPtr.Zero) {
                                            var data = new byte[dataSize];
                                            Marshal.Copy(dataPointer, data, 0, data.Length);
                                            var bomPresent = (data.Length >= 3 && data[0] == _utf[0] && data[1] == _utf[1] && data[2] == _utf[2]);

                                            // create an XElement for the data returned.
                                            // IIRC, manifests are always UTF-8, n'est-ce pas?
                                            manifests.Add(XElement.Parse(Encoding.UTF8.GetString(data, bomPresent ? 3 : 0, bomPresent ? data.Length - 3 : data.Length)));
                                        }
                                    } catch {
                                        // skip it if it doesn't load.
                                    }
                                }
                            }
                            return true;
                        }, Unused.Nothing, ResourceEnumFlags.None, LanguageId.None);

                        return true;
                    }, Unused.Nothing, ResourceEnumFlags.None, 0)) {
                    }
                }
            }
            return manifests;
        }
#endif

    }
}
