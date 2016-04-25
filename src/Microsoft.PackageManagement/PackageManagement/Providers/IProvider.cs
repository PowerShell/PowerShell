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
    using PackageManagement.Internal.Api;
    using PackageManagement.Internal.Utility.Plugin;

    public interface IProvider {
        /// <summary>
        ///     Allows the Provider to do one-time initialization.
        ///     This is called after the Provider is instantiated .
        /// </summary>
        /// <param name="requestObject">Object implementing some or all IRequest methods</param>
        [Required]
        void InitializeProvider(IRequest requestObject);

        /// <summary>
        ///     Gets the features advertized from the provider
        /// </summary>
        /// <param name="requestObject"></param>
        void GetFeatures(IRequest requestObject);

        /// <summary>
        ///     Gets dynamically defined options from the provider
        /// </summary>
        /// <param name="category"></param>
        /// <param name="requestObject"></param>
        void GetDynamicOptions(string category, IRequest requestObject);

        /// <summary>
        ///     Allows runtime examination of the implementing class to check if a given method is implemented.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        bool IsMethodImplemented(string methodName);

        /// <summary>
        ///     Returns the version of the provider.
        ///     This is expected to be in multipart numeric format.
        /// </summary>
        /// <returns>The version of the provider</returns>
        string GetProviderVersion();


        /// <summary>
        /// By declaring this, the DuckTyper will write isolation code that ensures that functions can't throw exceptions, instead
        /// Exceptions can be directed to the OnUnhandledException function.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="exception"></param>
        void OnUnhandledException(string methodName, Exception exception);
   }
}