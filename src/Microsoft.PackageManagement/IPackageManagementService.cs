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
    using System;
    using System.Collections.Generic;
    using Api;
    using Implementation;
    using PackageManagement.Implementation;
    using PackageManagement.Packaging;

    /// <summary>
    ///     The current Package Management Service Interface
    ///     Binding directly to this is discouraged, as the interface is expected to be incrementally
    ///     expanded over time.
    ///     In order to access the interface, the Host (client) is encouraged to copy this interface
    ///     into their own project and use the <code>PackageManagementService.GetInstance<![CDATA[<>]]></code>
    ///     method to dynamically generate a matching implementation at load time.
    /// </summary>
    public interface IPackageManagementService {
        int Version {get;}

        IEnumerable<string> ProviderNames {get;}

        IEnumerable<string> AllProviderNames { get; }

        IEnumerable<PackageProvider> PackageProviders {get;}

        IEnumerable<PackageProvider> GetAvailableProviders(IHostApi requestObject, string[] names);

        IEnumerable<PackageProvider> ImportPackageProvider(IHostApi requestObject, string providerName, Version requiredVersion,
            Version minimumVersion, Version maximumVersion, bool isRooted, bool force);

        bool Initialize(IHostApi requestObject);

        IEnumerable<PackageProvider> SelectProvidersWithFeature(string featureName);

        IEnumerable<PackageProvider> SelectProvidersWithFeature(string featureName, string value);

        IEnumerable<PackageProvider> SelectProviders(string providerName, IHostApi requestObject);

        IEnumerable<SoftwareIdentity> FindPackageByCanonicalId(string packageId, IHostApi requestObject);

        bool RequirePackageProvider(string requestor, string packageProviderName, string minimumVersion, IHostApi requestObject);
    }
}
