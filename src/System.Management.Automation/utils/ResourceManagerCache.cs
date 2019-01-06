// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Collections.Generic;
using System.Resources;

namespace System.Management.Automation
{
    /// <summary>
    /// </summary>
    internal static class ResourceManagerCache
    {
        /// <summary>
        /// Maintains a cache of ResourceManager objects. This is a dictionary that is keyed based on the path
        /// to the default resource assembly. The value is another dictionary that is keyed based on the base
        /// name for the resource that is being retrieved. The value for this dictionary is the ResourceManager.
        /// </summary>
        private static Dictionary<string, Dictionary<string, ResourceManager>> s_resourceManagerCache =
            new Dictionary<string, Dictionary<string, ResourceManager>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Used to synchronize access to the ResourceManagerCache.
        /// </summary>
        private static object s_syncRoot = new Object();

        /// <summary>
        /// Gets the ResourceManager from the cache or gets an instance of the ResourceManager
        /// and returns it if it isn't already present in the cache.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to be used as the base for resource lookup.
        /// </param>
        /// <param name="baseName">
        /// The base name of the resources to get the ResourceManager for.
        /// </param>
        /// <returns>
        /// A ResourceManager instance for the assembly and base name that were specified.
        /// </returns>
        internal static ResourceManager GetResourceManager(
            Assembly assembly,
            string baseName)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException("assembly");
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentException("baseName");
            }

            // Check to see if the manager is already in the cache

            ResourceManager manager = null;
            Dictionary<string, ResourceManager> baseNameCache;

            string assemblyManifestFileLocation = assembly.Location;
            lock (s_syncRoot)
            {
                // First do the lookup based on the assembly location

                if (s_resourceManagerCache.TryGetValue(assemblyManifestFileLocation, out baseNameCache) && baseNameCache != null)
                {
                    // Now do the lookup based on the resource base name
                    baseNameCache.TryGetValue(baseName, out manager);
                }
            }

            // If it's not in the cache, create it an add it.
            if (manager == null)
            {
                manager = InitRMWithAssembly(baseName, assembly);

                // Add the new resource manager to the hash

                if (baseNameCache != null)
                {
                    lock (s_syncRoot)
                    {
                        // Since the assembly is already cached, we just have
                        // to cache the base name entry

                        baseNameCache[baseName] = manager;
                    }
                }
                else
                {
                    // Since the assembly wasn't cached, we have to create base name
                    // cache entry and then add it into the cache keyed by the assembly
                    // location

                    var baseNameCacheEntry = new Dictionary<string, ResourceManager>();

                    baseNameCacheEntry[baseName] = manager;

                    lock (s_syncRoot)
                    {
                        s_resourceManagerCache[assemblyManifestFileLocation] = baseNameCacheEntry;
                    }
                }
            }

            Diagnostics.Assert(
                manager != null,
                "If the manager was not already created, it should have been dynamically created or an exception should have been thrown");

            return manager;
        }

        /// <summary>
        /// Design For Testability -- assert on failed resource lookup.
        /// </summary>
        private static bool s_DFT_monitorFailingResourceLookup = true;
        internal static bool DFT_DoMonitorFailingResourceLookup
        {
            get { return ResourceManagerCache.s_DFT_monitorFailingResourceLookup; }

            set { ResourceManagerCache.s_DFT_monitorFailingResourceLookup = value; }
        }

        /// <summary>
        /// Gets the string from the resource manager based on the assembly,
        /// base name, resource ID, and culture specified.
        /// </summary>
        /// <param name="assembly">
        /// The base assembly from which to get the resources from.
        /// </param>
        /// <param name="baseName">
        /// The base name of the resource to retrieve the string from.
        /// </param>
        /// <param name="resourceId">
        /// Resource ID for which the localized string needs to be retrieved
        /// </param>
        /// <returns>
        /// Localized String, or null if the string does not exist
        /// </returns>
        /// <remarks>
        /// The current thread's UI culture is used.
        /// </remarks>
        /// <throws>
        /// ArgumentException if <paramref name="baseName"/> or <paramref name="resourceId"/>
        ///     are null or empty..
        /// InvalidOperationException if the value of the specified resource is not a string
        /// MissingManifestResourceException if no usable set of resources have been found, and
        ///     there are no neutral culture resources.
        /// </throws>
        internal static string GetResourceString(
            Assembly assembly,
            string baseName,
            string resourceId)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException("assembly");
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentException("baseName");
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentException("resourceId");
            }

            ResourceManager resourceManager = null;
            string text = string.Empty;

            // For a non-existing resource defined by {assembly,baseName,resourceId}
            // MissingManifestResourceException is thrown only at the time when resource retrieval method
            // such as ResourceManager.GetString or ResourceManager.GetObject is called,
            // not when you instantiate a ResourceManager object.
            try
            {
                // try with original baseName first
                // if it fails then try with alternative resource path format
                resourceManager = GetResourceManager(assembly, baseName);
                text = resourceManager.GetString(resourceId);
            }
            catch (MissingManifestResourceException)
            {
                const string resourcesSubstring = ".resources.";
                int resourcesSubstringIndex = baseName.IndexOf(resourcesSubstring);
                string newBaseName = string.Empty;
                if (resourcesSubstringIndex != -1)
                {
                    newBaseName = baseName.Substring(resourcesSubstringIndex + resourcesSubstring.Length); // e.g.  "FileSystemProviderStrings"
                }
                else
                {
                    newBaseName = string.Concat(assembly.GetName().Name, resourcesSubstring, baseName); // e.g. "System.Management.Automation.resources.FileSystemProviderStrings"
                }

                resourceManager = GetResourceManager(assembly, newBaseName);
                text = resourceManager.GetString(resourceId);
            }

            if (string.IsNullOrEmpty(text) && s_DFT_monitorFailingResourceLookup)
            {
                Diagnostics.Assert(false,
                    "Lookup failure: baseName " + baseName + " resourceId " + resourceId);
            }

            return text;
        }

        /// <summary>
        /// Creates a Resource manager instance based on the assembly specified.
        /// </summary>
        /// <param name="baseName">
        /// The root name of the resources.
        /// For example, the root name for the resource file
        /// named "MyResource.en-US.resources" is "MyResource".
        /// </param>
        /// <param name="assemblyToUse">
        /// The main Assembly for the resources
        /// </param>
        /// <returns>Resource Manager instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the resource manager instance could not be created
        /// </exception>
        private static ResourceManager InitRMWithAssembly(string baseName, Assembly assemblyToUse)
        {
            ResourceManager rm = null;

            if (baseName != null && assemblyToUse != null)
            {
                rm = new ResourceManager(baseName, assemblyToUse);
            }
            else
            {
                // 2004/10/11-JonN Do we need a better error message?  I don't think so,
                // since this is private.
                throw PSTraceSource.NewArgumentException("assemblyToUse");
            }

            return rm;
        }
    }
}

