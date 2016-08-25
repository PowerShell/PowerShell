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

    public interface IPackageProvider : IProvider {
        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <remarks>
        ///     May be implemented as a property: <code>string PackageProviderName { get; } </code>
        /// </remarks>
        /// <returns>the name of the package provider</returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use Get-PackageProviderName as the function name 
        /// </remarks>
        [Required]
        string GetPackageProviderName();

        /// <summary>
        ///     This is called when the user is adding (or updating) a package source
        ///     If this PROVIDER doesn't support user-defined package sources, remove this method.
        /// </summary>
        /// <param name="name">
        ///     The name of the package source. If this parameter is null or empty the PROVIDER should use the
        ///     location as the name (if the PROVIDER actually stores names of package sources)
        /// </param>
        /// <param name="location">
        ///     The location (ie, directory, URL, etc) of the package source. If this is null or empty, the
        ///     PROVIDER should use the name as the location (if valid)
        /// </param>
        /// <param name="trusted">
        ///     A boolean indicating that the user trusts this package source. Packages returned from this source
        ///     should be marked as 'trusted'
        /// </param>
        /// <param name="requestObject">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        /// <remarks>
        ///     Make 'trusted' a dynamic parameter.
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Add-PackageSource' as the function name.
        ///         - $request object is not passed as a parameter, but inserted as a variable before the call.
        ///         - Values can be returned using Write-Object for objects returned from the New-PackageSource function
        /// </remarks>
#if NEW_SIGNATURES
        void AddPackageSource(string name, string location, IRequest requestObject);
#endif

#if !REMOVE_DEPRECATED_FUNCTIONS
        void AddPackageSource(string name, string location, bool trusted, IRequest requestObject);
#endif
        /// <summary>
        /// Resolves and returns package sources.
        /// 
        /// Expected behavior:
        ///     If request.Sources is null or empty, the provider should return the list of registered sources.
        ///     Otherwise, the provider should return package sources that match the strings in the request.Sources  
        ///     collection. If the string doesn't match a registered source (either by name or by location) and the 
        ///     string can be construed as a package source (ie, passing in a valid URL to a provider that uses URLs 
        ///     for their package sources), then the provider should return a package source for that URL, but marked
        ///     as 'unregistered' (and 'untrusted')
        /// </summary>
        /// <returns>
        ///     Data is returned using the request.YieldPackageSource(...) function.
        /// </returns>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use Resolve-PackageSource as the function name 
        ///         - $request object is not passed as a parameter, but inserted as a variable before the ca New-PackageSource.
        ///         - Values can be returned using Write-Object for objects returned from the function
        /// </remarks>
        void ResolvePackageSources(IRequest requestObject);

        /// <summary>
        /// Removes a registered package source.
        /// 
        /// Matching of the package source should be done on via the name or location against the specified string.
        /// </summary>
        /// <param name="source"> the name or location of the source to be removed.</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Remove-PackageSource' as the function name.
        ///         - request object is not passed as a parameter, but inserted as a variable before the call.
        /// </remarks> 
        void RemovePackageSource(string source, IRequest requestObject);

        /// <summary>
        ///     Finds packages given a set of criteria.
        /// 
        ///     If request.Sources is null or empty, the provider should use all the registered sources specified.
        /// 
        ///     If the provider doesn't support package sources, and the request.Sources isn't null or empty, 
        ///     the provider should return nothing and exit immediately.
        /// 
        ///     If request.Sources contains one or more elements, the repository should scope the search to just repositories 
        ///     that match sources (either by name or location) in the list of specified strings.
        /// </summary>
        /// <param name="name">matches against the name of a package. If this is null or empty, should return all matching packages.</param>
        /// <param name="requiredVersion">an exact version of the package to match. If this is specified, minimum and maximum should be ignored.</param>
        /// <param name="minimumVersion">the minimum version of the package to match. If requiredVersion is specified, this should be ignored.</param>
        /// <param name="maximumVersion">the maximum version of the package to match. If requiredVersion is specified, this should be ignored.</param>
        /// <param name="id">the batch id. If the provider supports batch searches, and this is non-zero,
        ///                 the provider should queue up the search to be executed when CompleteFind is called</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Find-Package' as the function name.
        ///         - If the provider supports batch operations, the name should be a string array
        ///         - Values can be returned using Write-Object for objects returned from the New-SoftwareIdentity function
        /// </remarks>
#if NEW_SIGNATURES
        void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, IRequest requestObject);
        void FindPackage(string[] names, string requiredVersion, string minimumVersion, string maximumVersion, IRequest requestObject);
#endif 

#if !REMOVE_DEPRECATED_FUNCTIONS
        void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion,int id, IRequest requestObject);
#endif

        /// <summary>
        /// Returns SoftwareIdentities given a local filename.
        /// </summary>
        /// <param name="filePath">the full path to the file to evaluate as a package</param>
        /// <param name="id">the batch id. If the provider supports batch searches, and this is non-zero,
        ///                 the provider should queue up the search to be executed when CompleteFind is called</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Find-PackageByFile' as the function name.
        ///         - If the provider supports batch operations, the file parameter should be a string array
        ///         - Values can be returned using Write-Object for objects returned from the New-SoftwareIdentity function
        /// </remarks>
#if NEW_SIGNATURES
        void FindPackageByFile(string filePath, IRequest requestObject);
        void FindPackageByFile(string[] filePaths, IRequest requestObject);
#endif 

#if !REMOVE_DEPRECATED_FUNCTIONS
        void FindPackageByFile(string filePath, int id, IRequest requestObject);
#endif

        /// <summary>
        /// Returns SoftwareIdentities given a URI. 
        /// </summary>
        /// <param name="uri">the URI to search for packages</param>
        /// <param name="id">the batch id. If the provider supports batch searches, and this is non-zero,
        ///                 the provider should queue up the search to be executed when CompleteFind is called</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Find-PackageByUri' as the function name.
        ///         - If the provider supports batch operations, the uri parameter should be an array
        ///         - Values can be returned using Write-Object for objects returned from the New-SoftwareIdentity function
        /// </remarks>
#if NEW_SIGNATURES
        void FindPackageByUri(Uri uri, IRequest requestObject);
        void FindPackageByUri(Uri[] uris, IRequest requestObject);
#endif

#if !REMOVE_DEPRECATED_FUNCTIONS
        void FindPackageByUri(Uri uri, int id, IRequest requestObject);
#endif

        /// <summary>
        /// Returns Software Identities for installed packages
        /// </summary>
        /// <param name="name">matches against the name of a package. If this is null or empty, should return all matching packages.</param>
        /// <param name="requiredVersion">an exact version of the package to match. If this is specified, minimum and maximum should be ignored.</param>
        /// <param name="minimumVersion">the minimum version of the package to match. If requiredVersion is specified, this should be ignored.</param>
        /// <param name="maximumVersion">the maximum version of the package to match. If requiredVersion is specified, this should be ignored.</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Get-InstalledPackage' as the function name.
        ///         - Values can be returned using Write-Object for objects returned from the New-SoftwareIdentity function
        /// </remarks>
        void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, IRequest requestObject);

        /// <summary>
        /// If supported, this should download a package file to the location provided.
        /// </summary>
        /// <param name="fastPath"></param>
        /// <param name="location"></param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - may use 'Download-Package' as the function name.
        /// </remarks>
        void DownloadPackage(string fastPath, string location, IRequest requestObject);

        /// <summary>
        /// (not currently used)
        /// Will be used to allow packages to return full details on demand, rather than at find/get time.
        /// </summary>
        /// <param name="fastPath">the round-tripped (from FindPackageXXX or GetInstalledPackages ) identity of the package</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - May use 'Get-PackageDetail' as the function name.
        /// </remarks>
        void GetPackageDetails(string fastPath, IRequest requestObject);

        /// <summary>
        /// Installs a package.
        /// </summary>
        /// <param name="fastPath">the round-tripped (from FindPackageXXX) identity of the package to be installed</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - May use 'Install-Package' as the function name.
        /// </remarks>
        void InstallPackage(string fastPath, IRequest requestObject);

        /// <summary>
        /// Uninstalls a package.
        /// </summary>
        /// <param name="fastPath">the round-tripped (from GetInstalledPackages) identity of the package to be uninstalled</param>
        /// <param name="requestObject">The request context passed to the provider.</param>
        /// <returns>
        /// Returns Software Identities thru the use of request.YieldSoftwareIdentity(), and extended data via:
        ///     AddMetadata(...)    -- adds key/value pairs to an element
        ///     AddMeta(...)        -- adds a new Meta element child to an element
        ///     AddEntity(...)      -- adds a new Entity to an element
        ///     AddLink(...)        -- adds a new Link to a SoftwareIdentity
        ///     AddDependency(...)  -- adds a new 'requires' Link to a SoftwareIdentity
        ///     AddPayload(...)     -- adds a Payload (or returns the existing one) to a SoftwareIdentity
        ///     AddEvidence(...)    -- adds an Evidence (or returns the existing one) to a SoftwareIdentity
        ///     AddDirectory(...)   -- adds a Directory to a Payload, Evidence or Directory
        ///     AddFile(...)        -- adds a File to a Payload, Evidence or Directory
        ///     AddProcess(...)     -- adds a Process to a Payload or Evidence 
        ///     AddResource(...)    -- adds a Resource to a Payload or Evidence 
        /// </returns>
        /// <remarks>
        ///     PowerShellMetaProvider notes:
        ///         - May use 'Uninstall-Package' as the function name.
        /// </remarks>
        void UninstallPackage(string fastPath, IRequest requestObject);
    }
}