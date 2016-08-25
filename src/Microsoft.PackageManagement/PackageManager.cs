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

namespace Microsoft.PackageManagement.Internal {
    using Implementation;

    /// <summary>
    ///     The public interface to accessing the features of the Package Management Service
    ///     This offers two possible methods to get the instance of the PackageManagementService.
    ///     If the Host is consuming the PackageManagementService by linking to this assembly, then
    ///     the simplest access is just to use the <code>Instance</code> method.
    ///     If the Host has dynamically loaded this assembly, then it can request a dynamically-generated
    ///     instance of the PackageManagementService that implements an interface of their own choosing.
    ///     <example><![CDATA[
    ///    // Manually load the assembly
    ///    var asm = Assembly.Load("Microsoft.PackageManagement.Core.dll" )
    ///     // todo: insert reflection-based loading code.
    /// ]]>
    ///     </example>
    /// </summary>
    internal static class PackageManager {
        private static readonly object _lockObject = new object();
        internal static IPackageManagementService _instance;

        public static IPackageManagementService Instance {
            get {
                lock (_lockObject) {
                    if (_instance == null) {
                        _instance = new PackageManagementService();
                    }
                }
                return _instance;
            }
        }
    }
}
