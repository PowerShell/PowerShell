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

namespace Microsoft.PackageManagement.MetaProvider.PowerShell {
    public class Link {
        public string HRef {get; set;}
        public string Relationship {get; set;}
        public string MediaType {get; set;}
        public string Ownership {get; set;}
        public string Use {get; set;}
        public string AppliesToMedia {get; set;}
        public string Artifact {get; set;}
    }
}
