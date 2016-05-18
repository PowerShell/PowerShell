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

namespace Microsoft.PackageManagement.Providers.Internal.Bootstrap {
    using System;
    using System.Collections.Generic;
    using PackageManagement.Internal.Packaging;
    using Packaging;

    internal class Package : Swid {
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Package(BootstrapRequest request, Swidtag swidtag)
            : base(request, swidtag) {
        }

        internal Package(BootstrapRequest request, IEnumerable<Link> mirrors) : base(request, mirrors) {
        }

        internal Package(BootstrapRequest request, IEnumerable<Uri> mirrors)
            : base(request, mirrors) {
        }

        internal string Name {
            get {
                if (IsValid) {
                    return _swidtag.Name;
                }
                return string.Empty;
            }
        }

        internal string Version {
            get {
                if (IsValid) {
                    return _swidtag.Version ?? "0";
                }
                return "0";
            }
        }

        internal string VersionScheme {
            get {
                if (IsValid) {
                    return _swidtag.VersionScheme ?? Iso19770_2.VersionScheme.Unknown;
                }
                return Iso19770_2.VersionScheme.Unknown;
            }
        }

        internal string Source {get; set;}
    }
}