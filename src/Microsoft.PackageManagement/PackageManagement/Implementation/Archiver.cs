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

namespace Microsoft.PackageManagement.Internal.Implementation {
    using System.Collections.Generic;
    using Api;
    using Providers;

    internal class Archiver : ProviderBase<IArchiver> {
        private string _name;

        internal Archiver(IArchiver provider)
            : base(provider) {
        }

        public override string ProviderName {
            get {
                return _name ?? (_name = Provider.GetArchiverName());
            }
        }

        public override bool IsSupportedFile(string localFilename) {
            return base.IsSupportedFile(localFilename) && Provider.IsSupportedFile(localFilename);
        }

        public IEnumerable<string> UnpackArchive(string localFilename, string destinationFolder, IHostApi host) {
            return new FuncRequestObject<IEnumerable<string>>(this, host, (request) => Provider.UnpackArchive(localFilename, destinationFolder, request)).Value;
        }
    }
}