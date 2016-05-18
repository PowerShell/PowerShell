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

namespace Microsoft.PackageManagement.Internal.Api {
    using System;
    using System.Collections.Generic;
    using PackageManagement.Packaging;

    public interface IProviderServices {
        #region declare service-apis

        bool IsElevated {get;}

        IEnumerable<SoftwareIdentity> FindPackageByCanonicalId(string canonicalId, IRequest requestObject);

        string GetCanonicalPackageId(string providerName, string packageName, string version, string source);

        string ParseProviderName(string canonicalPackageId);

        string ParsePackageName(string canonicalPackageId);

        string ParsePackageVersion(string canonicalPackageId);

        string ParsePackageSource(string canonicalPackageId);

        string DownloadFile(Uri remoteLocation, string localFilename, IRequest requestObject);

        string DownloadFile(Uri remoteLocation, string localFilename,int timeoutMilliseconds, bool showProgress, IRequest requestObject);

        bool IsSupportedArchive(string localFilename, IRequest requestObject);

        IEnumerable<string> UnpackArchive(string localFilename, string destinationFolder, IRequest requestObject);

        bool Install(string fileName, string additionalArgs, IRequest requestObject);

        bool IsSignedAndTrusted(string filename, IRequest requestObject);

        int StartProcess(string filename, string arguments, bool requiresElevation, out string standardOutput, IRequest requestObject);

        #endregion
    }
}