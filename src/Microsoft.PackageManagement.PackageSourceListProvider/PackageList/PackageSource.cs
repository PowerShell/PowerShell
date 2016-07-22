#if !UNIX

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


namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Provider.Utility;
    internal class PackageSource
    {
        //Parameters will be filled during the instantiation.
        internal string Name { get; set; }

        internal string Location { get; set; }

        internal bool Trusted { get; set; }

        internal bool IsRegistered { get; set; }

        internal bool IsValidated { get; set; }

        internal Request Request { get; set; }

        internal string Serialized
        {
            get
            {
                return Location.ToBase64();
            }
        }
    }
}

#endif