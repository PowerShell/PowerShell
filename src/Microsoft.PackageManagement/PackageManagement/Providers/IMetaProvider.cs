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

namespace Microsoft.PackageManagement.Internal.Providers {
    using System;
    using System.Collections.Generic;
    using Api;
    using PackageManagement.Internal.Utility.Plugin;

    public interface IMetaProvider : IProvider {
        #region declare MetaProvider-interface

        /* Synced/Generated code =================================================== */

        /// <summary>
        ///     Will instantiate an instance of a provider given it's name.
        /// 
        ///     If the name is a filename, this will ask the provider to attempt to load it.
        /// </summary>
        /// <param name="name">the name of the provider to create</param>
        /// <returns>an instance of the provider.</returns>
        [Required]
        object CreateProvider(string name);

        /// <summary>
        ///     Gets the name of this MetaProvider
        /// </summary>
        /// <returns>the name of the MetaProvider.</returns>
        [Required]
        string GetMetaProviderName();

        /// <summary>
        ///     Returns a collection of all the names of Providers this MetaProvider can create.
        /// </summary>
        /// <returns>a collection of all the names of Providers this MetaProvider can create</returns>
        [Required]
        IEnumerable<string> GetProviderNames();

        /// <summary>
        ///     Returns the path to the file that defines a given provider.
        /// </summary>
        /// <returns>the path of the provider</returns>
        [Required]
        string GetProviderPath(string name);

        /// <summary>
        ///     Return available providers that not loaded yet.
        /// </summary>
        /// <param name="request">Object inherits IRequest</param>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="requiredVersion">Retrieves only the specified version of the provider</param>
        /// <param name="minimumVersion">Retrieves only a version of the provider that is greater than or equal to the specified value</param>
        /// <param name="maximumVersion">Retrieves only a version of the provider that is less than the specified value</param>
        /// <returns></returns>
        [Required]
        void RefreshProviders(IRequest request, string providerName, Version requiredVersion, Version minimumVersion, Version maximumVersion);

        /// <summary>
        ///  Load a particular provider written in powershell module. 
        /// </summary>
        /// <param name="request">Object inherits IRequest</param>
        /// <param name="modulePath">The file path of the PowerShell module provider</param>
        /// <param name="requiredVersion">Retrieves only the specified version of the provider</param>
        /// <param name="force">Force to load a provider</param>
        /// <returns></returns>
        [Required]
        IEnumerable<object> LoadAvailableProvider(IRequest request, string modulePath, Version requiredVersion, bool force);

        #endregion
    }
}