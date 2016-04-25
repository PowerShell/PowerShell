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

    /// <summary>
    /// These functions are implemented by the CORE and passed to the PROVIDER so data can be returned from a given call.
    /// </summary>
    public interface IResponseApi {
        /// <summary>
        ///     Used by a provider to return fields for a SoftwareIdentity.
        /// </summary>
        /// <param name="fastPath"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="versionScheme"></param>
        /// <param name="summary"></param>
        /// <param name="source"></param>
        /// <param name="searchKey"></param>
        /// <param name="fullPath"></param>
        /// <param name="packageFileName"></param>
        /// <returns></returns>
        string YieldSoftwareIdentity(string fastPath, string name, string version, string versionScheme, string summary, string source, string searchKey, string fullPath, string packageFileName);

        /// <summary>
        /// Used by a provider to return a swidtag in the form of an xml
        /// Provider has the option to commit immediately
        /// </summary>
        /// <param name="xmlSwidTag"></param>
        /// <param name="commitImmediately"></param>
        /// <returns></returns>
        string YieldSoftwareIdentityXml(string xmlSwidTag, bool commitImmediately);

        /// <summary>
        /// Adds a tagId to a SoftwareIdentity object
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        string AddTagId(string tagId);

        /// <summary>
        /// Adds a xml language attribute to a SoftwareIdentity object
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        string AddCulture(string culture);

        /// <summary>
        /// Adds an arbitrary key/value pair of metadata to a SoftwareIdentity
        /// 
        /// This adds the metadata to the first Meta element in the swidtag.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>a string to a Meta element path. If null, this function did not succeed.</returns>
        string AddMetadata(string name, string value);

        /// <summary>
        /// Adds arbitrary key/value pair of metadata to a specific element in a Swidtag.
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>a string to a Meta element path. If null, this function did not succeed.</returns>
        string AddMetadata(string elementPath, string name, string value);

        /// <summary>
        /// Adds arbitrary key/value pair of metadata to a specific element in a Swidtag, using a specific namespace.
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="namespace"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>a string to a Meta element path. If null, this function did not succeed.</returns>
        string AddMetadata(string elementPath, Uri @namespace, string name, string value);

        /// <summary>
        /// Adds a new Meta Element to the Swidtag. 
        /// </summary>
        /// <param name="elementPath"></param>
        /// <returns>a string to the newly created Meta element. If null, this function did not succeed.</returns>
        string AddMeta(string elementPath);

        /// <summary>
        /// Adds a new Entity element to the Swidtag
        /// </summary>
        /// <param name="name"></param>
        /// <param name="regid"></param>
        /// <param name="role"></param>
        /// <param name="thumbprint"></param>
        /// <returns>a string to a Entity element path. If null, this function did not succeed.</returns>
        string AddEntity(string name, string regid, string role, string thumbprint);

        /// <summary>
        /// Adds a new Link element to the swidtag
        /// </summary>
        /// <param name="referenceUri"></param>
        /// <param name="relationship"></param>
        /// <param name="mediaType"></param>
        /// <param name="ownership"></param>
        /// <param name="use"></param>
        /// <param name="appliesToMedia"></param>
        /// <param name="artifact"></param>
        /// <returns>a string to a Link element path. If null, this function did not succeed.</returns>
        string AddLink(Uri referenceUri, string relationship, string mediaType, string ownership, string use, string appliesToMedia, string artifact);

        /// <summary>
        /// Adds a new Dependency (ie, Link, rel="requires") to the swidtag
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="packageName"></param>
        /// <param name="version"></param>
        /// <param name="source"></param>
        /// <param name="appliesTo"></param>
        /// <returns>a string to a Link element path. If null, this function did not succeed.</returns>
        string AddDependency(string providerName, string packageName, string version, string source, string appliesTo);


        /// <summary>
        /// Adds a Payload element to the Swidtag
        /// </summary>
        /// <returns>a string to a new (or the existing one) Payload element</returns>
        string AddPayload();

        /// <summary>
        /// Adds an Evidence element to the Swidtag
        /// </summary>
        /// <param name="date"></param>
        /// <param name="deviceId"></param>
        /// <returns>a string to a new (or the existing one) Evidence element</returns>
        string AddEvidence(DateTime date, string deviceId);

        /// <summary>
        /// Adds a Directory element to a Payload, Evidence or Directory element
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="directoryName"></param>
        /// <param name="location"></param>
        /// <param name="root"></param>
        /// <param name="isKey"></param>
        /// <returns>a string to a new Directory element. If null, this function did not succeed.</returns>
        string AddDirectory(string elementPath, string directoryName, string location, string root, bool isKey);

        /// <summary>
        /// Adds a File element to a Payload, Evidence or Directory element
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="fileName"></param>
        /// <param name="location"></param>
        /// <param name="root"></param>
        /// <param name="isKey"></param>
        /// <param name="size"></param>
        /// <param name="version"></param>
        /// <returns>a string to a new File element. If null, this function did not succeed.</returns>
        string AddFile(string elementPath, string fileName, string location, string root, bool isKey, long size, string version);

        /// <summary>
        /// Adds a Process element to a Payload or Evidence element
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="processName"></param>
        /// <param name="pid"></param>
        /// <returns>a string to a new Process element. If null, this function did not succeed.</returns>
        string AddProcess(string elementPath, string processName, int pid);

        /// <summary>
        /// Adds a Resource element to a Payload or Evidence element
        /// </summary>
        /// <param name="elementPath"></param>
        /// <param name="type"></param>
        /// <returns>a string to a new Resource element. If null, this function did not succeed.</returns>
        string AddResource(string elementPath, string type);

        /// <summary>
        ///     Used by a provider to return fields for a package source (repository)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="location"></param>
        /// <param name="isTrusted"></param>
        /// <param name="isRegistered"></param>
        /// <param name="isValidated"></param>
        /// <returns>A boolean indicating the IsCanceled state. (if the result is false, the provider should exit quickly)</returns>
        bool YieldPackageSource(string name, string location, bool isTrusted, bool isRegistered, bool isValidated);

        /// <summary>
        ///     Used by a provider to return the fields for a Metadata Definition
        ///     The cmdlets can use this to supply tab-completion for metadata to the user.
        /// </summary>
        /// <param name="name">the provider-defined name of the option</param>
        /// <param name="expectedType"> one of ['string','int','path','switch']</param>
        /// <param name="isRequired">if the parameter is mandatory</param>
        /// <returns>A boolean indicating the IsCanceled state. (if the result is false, the provider should exit quickly)</returns>
        bool YieldDynamicOption(string name, string expectedType, bool isRequired);

        /// <summary>
        /// Yields a key/value pair 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>A boolean indicating the IsCanceled state. (if the result is false, the provider should exit quickly)</returns>
        bool YieldKeyValuePair(string key, string value);

        /// <summary>
        /// Yields a value 
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A boolean indicating the IsCanceled state. (if the result is false, the provider should exit quickly)</returns>
        bool YieldValue(string value);
   }
}