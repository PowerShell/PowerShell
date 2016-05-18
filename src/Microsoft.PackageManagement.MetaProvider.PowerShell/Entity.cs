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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;

    public class Entity {
        public string Name {get; set;}
        public string RegId {get; set;}
        public string Role {get; set;}

        public IEnumerable<string> Roles {
            get {
                return (string.IsNullOrWhiteSpace(Role) ? "unknown" : Role).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                var values = value == null ? new string[0] : value.ToArray();
                Role = ( values.Length == 0) ? "unknown" : value.JoinWith(" ");
            }
        }

        public string Thumbprint {get; set;}
    }
}
