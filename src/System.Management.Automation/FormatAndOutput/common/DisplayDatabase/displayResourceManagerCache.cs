// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;
using System.Reflection;
using System.Resources;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class DisplayResourceManagerCache
    {
        internal enum LoadingResult { NoError, AssemblyNotFound, ResourceNotFound, StringNotFound }

        internal enum AssemblyBindingStatus { NotFound, FoundInGac, FoundInPath }

        internal string GetTextTokenString(TextToken tt)
        {
            if (tt.resource != null)
            {
                string resString = this.GetString(tt.resource);
                if (resString != null)
                    return resString;
            }

            return tt.text;
        }

        internal void VerifyResource(StringResourceReference resourceReference, out LoadingResult result, out AssemblyBindingStatus bindingStatus)
        {
            GetStringHelper(resourceReference, out result, out bindingStatus);
        }

        private string GetString(StringResourceReference resourceReference)
        {
            LoadingResult result;
            AssemblyBindingStatus bindingStatus;
            return GetStringHelper(resourceReference, out result, out bindingStatus);
        }

        private string GetStringHelper(StringResourceReference resourceReference, out LoadingResult result, out AssemblyBindingStatus bindingStatus)
        {
            result = LoadingResult.AssemblyNotFound;
            bindingStatus = AssemblyBindingStatus.NotFound;

            AssemblyLoadResult loadResult = null;
            // try first to see if we have an assembly reference in the cache
            if (_resourceReferenceToAssemblyCache.Contains(resourceReference))
            {
                loadResult = _resourceReferenceToAssemblyCache[resourceReference] as AssemblyLoadResult;
                bindingStatus = loadResult.status;
            }
            else
            {
                loadResult = new AssemblyLoadResult();
                // we do not have an assembly, we try to load it
                bool foundInGac;
                loadResult.a = LoadAssemblyFromResourceReference(resourceReference, out foundInGac);
                if (loadResult.a == null)
                {
                    loadResult.status = AssemblyBindingStatus.NotFound;
                }
                else
                {
                    loadResult.status = foundInGac ? AssemblyBindingStatus.FoundInGac : AssemblyBindingStatus.FoundInPath;
                }

                // add to the cache even if null
                _resourceReferenceToAssemblyCache.Add(resourceReference, loadResult);
            }

            bindingStatus = loadResult.status;

            if (loadResult.a == null)
            {
                // we failed the assembly loading
                result = LoadingResult.AssemblyNotFound;
                return null;
            }
            else
            {
                resourceReference.assemblyLocation = loadResult.a.Location;
            }

            // load now the resource from the resource manager cache
            try
            {
                string val = ResourceManagerCache.GetResourceString(loadResult.a, resourceReference.baseName, resourceReference.resourceId);
                if (val == null)
                {
                    result = LoadingResult.StringNotFound;
                    return null;
                }
                else
                {
                    result = LoadingResult.NoError;
                    return val;
                }
            }
            catch (InvalidOperationException)
            {
                result = LoadingResult.ResourceNotFound;
            }
            catch (MissingManifestResourceException)
            {
                result = LoadingResult.ResourceNotFound;
            }
            catch (Exception e) // will rethrow
            {
                Diagnostics.Assert(false, "ResourceManagerCache.GetResourceString unexpected exception " + e.GetType().FullName);
                throw;
            }

            return null;
        }

        /// <summary>
        /// Get a reference to an assembly object by looking up the currently loaded assemblies.
        /// </summary>
        /// <param name="resourceReference">the string resource reference object containing
        /// the name of the assembly to load</param>
        /// <param name="foundInGac"> true if assembly was found in the GAC. NOTE: the current
        /// implementation always return FALSE</param>
        /// <returns></returns>
        private Assembly LoadAssemblyFromResourceReference(StringResourceReference resourceReference, out bool foundInGac)
        {
            // NOTE: we keep the function signature as and the calling code is able do deal
            // with dynamically loaded assemblies. If this functionality is implemented, this
            // method will have to be changed accordingly
            foundInGac = false; // it always be false, since we return already loaded assemblies
            return _assemblyNameResolver.ResolveAssemblyName(resourceReference.assemblyName);
        }

        private sealed class AssemblyLoadResult
        {
            internal Assembly a;
            internal AssemblyBindingStatus status;
        }

        /// <summary>
        /// Helper class to resolve an assembly name to an assembly reference
        /// The class caches previous results for faster lookup.
        /// </summary>
        private sealed class AssemblyNameResolver
        {
            /// <summary>
            /// Resolve the assembly name against the set of loaded assemblies.
            /// </summary>
            /// <param name="assemblyName"></param>
            /// <returns></returns>
            internal Assembly ResolveAssemblyName(string assemblyName)
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    return null;
                }

                // look up the cache first
                if (_assemblyReferences.Contains(assemblyName))
                {
                    return (Assembly)_assemblyReferences[assemblyName];
                }

                // not found, scan the loaded assemblies

                // first look for the full name
                Assembly retVal = ResolveAssemblyNameInLoadedAssemblies(assemblyName, true) ??
                                  ResolveAssemblyNameInLoadedAssemblies(assemblyName, false);
                // NOTE: we cache the result (both for success and failure)

                // Porting note: this won't be hit in normal usage, but can be hit with bad build setup
                Diagnostics.Assert(retVal != null, "AssemblyName resolution failed, a resource file might be broken");

                _assemblyReferences.Add(assemblyName, retVal);
                return retVal;
            }

            private static Assembly ResolveAssemblyNameInLoadedAssemblies(string assemblyName, bool fullName)
            {
                Assembly result = null;

#if false
                // This should be re-enabled once the default assembly list contains the
                // assemblies referenced by the S.M.A.dll.

                // First we need to get the execution context from thread-local storage.

                ExecutionContext context = System.Management.Automation.Runspaces.LocalPipeline.GetExecutionContextFromTLS();

                if (context != null)
                {
                    context.AssemblyCache.GetAtKey(assemblyName, out result);
                }
#else
                foreach (Assembly a in ClrFacade.GetAssemblies())
                {
                    AssemblyName aName = null;
                    try
                    {
                        aName = a.GetName();
                    }
                    catch (System.Security.SecurityException)
                    {
                        continue;
                    }

                    string nameToCompare = fullName ? aName.FullName : aName.Name;

                    if (string.Equals(nameToCompare, assemblyName, StringComparison.Ordinal))
                    {
                        return a;
                    }
                }
#endif
                return result;
            }

            private readonly Hashtable _assemblyReferences = new Hashtable(StringComparer.OrdinalIgnoreCase);
        }

        private readonly AssemblyNameResolver _assemblyNameResolver = new AssemblyNameResolver();
        private readonly Hashtable _resourceReferenceToAssemblyCache = new Hashtable();
    }
}
