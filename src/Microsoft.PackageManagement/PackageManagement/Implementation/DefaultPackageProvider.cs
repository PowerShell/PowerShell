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


namespace Microsoft.PackageManagement.Internal.Implementation
{
    using System;
    using PackageManagement.Internal.Packaging;
    using Providers;

    /// <summary>
    /// This DefaultPackageProvider type is mainly used for the PowerShell console output.
    /// </summary>
    public class DefaultPackageProvider : Swidtag, IPackageProvider {

        private readonly string _providerName;
        private readonly string _providerVersion;

        public DefaultPackageProvider(string name, string version) {
            _providerVersion = version;
            _providerName = name;
        }

        public void InitializeProvider(Api.IRequest requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            requestObject.Debug("Called DummyProvider InitializeProvider");
        }

        internal bool IsLoaded {set; get;}

        public string ProviderPath {set; get;}


        public string GetPackageProviderName() {
            return _providerName;
        }

        public string GetProviderVersion() {
            return _providerVersion;
        }

        public void AddPackageSource(string name, string location, bool trusted, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void ResolvePackageSources(Api.IRequest requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            requestObject.Debug("Called DummyProvider ResolvePackageSources");
        }

        public void GetDynamicOptions(string category, Api.IRequest requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            requestObject.Debug("Called DummyProvider GetDynamicOptions");
        }

        public void RemovePackageSource(string source, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void FindPackageByFile(string filePath, int id, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void FindPackageByUri(Uri uri, int id, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void DownloadPackage(string fastPath, string location, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void GetPackageDetails(string fastPath, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void InstallPackage(string fastPath, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public void UninstallPackage(string fastPath, Api.IRequest requestObject) {
            throw new NotImplementedException();
        }


        public void GetFeatures(Api.IRequest requestObject) {
            throw new NotImplementedException();
        }

        public bool IsMethodImplemented(string methodName) {
            throw new NotImplementedException();
        }

        public void OnUnhandledException(string methodName, Exception exception) {
            throw new NotImplementedException();
        }
    }
}


