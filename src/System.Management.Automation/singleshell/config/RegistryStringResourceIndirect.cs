/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security;
using Microsoft.Win32;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This class is responsible for loading resources using the PSSnapin dll and
    /// associated registry entries.
    /// </summary>
    /// 
    /// <remarks>
    /// The class creates an app-domain to load the resource assemblies in. Upon dispose
    /// the class unloads the app-domain to ensure the assemblies get unloaded. It uses
    /// ReflectionOnlyLoad and ReflectionOnlyLoadFrom to ensure that no code can execute
    /// and that dependencies are not loaded.  This allows us to load assemblies that were
    /// built with different version of the CLR.
    /// </remarks>
    /// 
    internal sealed class RegistryStringResourceIndirect : IDisposable
    {
        /// <summary>
        /// Creates an instance of the RegistryStringResourceIndirect class.
        /// </summary>
        /// 
        /// <returns>
        /// A new instance of the RegistryStringResourceIndirect class.
        /// </returns>
        /// 
        internal static RegistryStringResourceIndirect GetResourceIndirectReader()
        {
            return new RegistryStringResourceIndirect();
        }

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        /// 
        private bool _disposed;

        /// <summary>
        /// Dispose method unloads the app domain that was
        /// created in the constructor.
        /// </summary>
        /// 
        public void Dispose()
        {
            if (_disposed == false)
            {
                if (_domain != null)
                {
                    AppDomain.Unload(_domain);
                    _domain = null;
                    _resourceRetriever = null;
                }
            }
            _disposed = true;
        }

        #endregion IDisposable Members

        /// <summary>
        /// The app-domain in which the resources will be loaded.
        /// </summary>
        /// 
        private AppDomain _domain;

        /// <summary>
        /// The class that is created in the app-domain which does the resource loading.
        /// </summary>
        /// 
        private ResourceRetriever _resourceRetriever;

        /// <summary>
        /// Creates the app-domain and the instance of the ResourceRetriever and
        /// sets the private fields with the references.
        /// </summary>
        /// 
        private void CreateAppDomain()
        {
            if (_domain == null)
            {
                // Create an app-domain to load the resource assemblies in so that they can be
                // unloaded.

                _domain = AppDomain.CreateDomain("ResourceIndirectDomain");
                _resourceRetriever =
                    (ResourceRetriever)_domain.CreateInstanceAndUnwrap(
                        Assembly.GetExecutingAssembly().FullName,
                        "System.Management.Automation.ResourceRetriever");
            }
        }

        /// <summary>
        /// Retrieves a resource string based on a resource reference stored in the specified
        /// registry key. 
        /// </summary>
        /// 
        /// <param name="key">
        /// The key in which there is a value that contains the reference to the resource
        /// to retrieve.
        /// </param>
        /// 
        /// <param name="valueName">
        /// The name of the value in the registry key that contains the reference to the resource.
        /// </param>
        /// 
        /// <param name="assemblyName">
        /// The full name of the assembly from which to load the resource.
        /// </param>
        /// 
        /// <param name="modulePath">
        /// The full path of the assembly from which to load the resource.
        /// </param>
        /// 
        /// <returns>
        /// The resource string that was loaded or null if it could not be found.
        /// </returns>
        /// 
        /// <remarks>
        /// This method ensures that an appropriate registry entry exists and that it contains
        /// a properly formatted resource reference ("BaseName,ResourceID").  It then creates an
        /// app-domain (or uses and existing one if it already exists on the instance of the class)
        /// and an instance of the ResourceRetriever in that app-domain.  It then calls the ResourceRetriever
        /// to load the specified assembly and retrieve the resource.  The assembly is loaded using ReflectionOnlyLoad 
        /// or ReflectionOnlyLoadFrom using the assemblyName or moduleName (respectively) so that 
        /// no code can be executed.
        /// 
        /// The app-domain is unloaded when this class instance is disposed.
        /// </remarks>
        /// 
        internal string GetResourceStringIndirect(
            RegistryKey key,
            string valueName,
            string assemblyName,
            string modulePath)
        {
            if (_disposed)
            {
                throw PSTraceSource.NewInvalidOperationException(MshSnapinInfo.ResourceReaderDisposed);
            }

            if (key == null)
            {
                throw PSTraceSource.NewArgumentNullException("key");
            }

            if (String.IsNullOrEmpty(valueName))
            {
                throw PSTraceSource.NewArgumentException("valueName");
            }

            if (String.IsNullOrEmpty(assemblyName))
            {
                throw PSTraceSource.NewArgumentException("assemblyName");
            }

            if (String.IsNullOrEmpty(modulePath))
            {
                throw PSTraceSource.NewArgumentException("modulePath");
            }

            string result = null;

            do // false loop
            {
                // Read the resource reference from the registry
                string regValue = GetRegKeyValueAsString(key, valueName);

                if (regValue == null)
                {
                    break;
                }

                result = GetResourceStringIndirect(assemblyName, modulePath, regValue);
            } while (false);

            return result;
        }

        /// <summary>
        /// Retrieves a resource string based on a resource reference supplied in <paramref name="baseNameRIDPair"/>. 
        /// </summary>
        /// 
        /// <param name="assemblyName">
        /// The full name of the assembly from which to load the resource.
        /// </param>
        /// 
        /// <param name="modulePath">
        /// The full path of the assembly from which to load the resource.
        /// </param>
        /// 
        /// <param name="baseNameRIDPair">
        /// A comma separated basename and resource id pair.
        /// </param>
        /// 
        /// <returns>
        /// The resource string that was loaded or null if it could not be found.
        /// </returns>
        /// 
        /// <remarks>
        /// This method ensures that  <paramref name="baseNameRIDPair"/> is a properly formatted
        /// resource reference ("BaseName,ResourceID").  It then creates an app-domain (or uses 
        /// an existing one if it already exists on the instance of the class) and an instance 
        /// of the ResourceRetriever in that app-domain.  It then calls the ResourceRetriever
        /// to load the specified assembly and retrieve the resource.  The assembly is loaded using ReflectionOnlyLoad 
        /// or ReflectionOnlyLoadFrom using the assemblyName or moduleName (respectively) so that 
        /// no code can be executed.
        /// 
        /// The app-domain is unloaded when this class instance is disposed.
        /// </remarks>
        /// 
        internal string GetResourceStringIndirect(
            string assemblyName,
            string modulePath,
            string baseNameRIDPair)
        {
            if (_disposed)
            {
                throw PSTraceSource.NewInvalidOperationException(MshSnapinInfo.ResourceReaderDisposed);
            }

            if (String.IsNullOrEmpty(assemblyName))
            {
                throw PSTraceSource.NewArgumentException("assemblyName");
            }

            if (String.IsNullOrEmpty(modulePath))
            {
                throw PSTraceSource.NewArgumentException("modulePath");
            }

            if (String.IsNullOrEmpty(baseNameRIDPair))
            {
                throw PSTraceSource.NewArgumentException("baseNameRIDPair");
            }

            string result = null;

            do // false loop
            {
                // Initialize the app-domain and resource reader if not already initialized
                if (_resourceRetriever == null)
                {
                    CreateAppDomain();
                }

                // If the app-domain failed to load or the ResourceRetriever instance wasn't
                // created, then return null.

                if (_resourceRetriever == null)
                {
                    break;
                }

                // Parse the resource reference
                string[] resourceSplit = baseNameRIDPair.Split(Utils.Separators.Comma);
                if (resourceSplit.Length != 2)
                {
                    break;
                }

                string baseName = resourceSplit[0];
                string resourceID = resourceSplit[1];

                // Get the resource in the app-domain
                result = _resourceRetriever.GetStringResource(assemblyName, modulePath, baseName, resourceID);
            } while (false);

            return result;
        }

        /// <summary>
        /// Retrieves a string value from the registry
        /// </summary>
        /// 
        /// <param name="key">
        /// The key to retrieve the value from.
        /// </param>
        /// 
        /// <param name="valueName">
        /// The name of the value to retrieve.
        /// </param>
        /// 
        /// <returns>
        /// The string value of the registry key value.
        /// </returns>
        /// 
        private static string GetRegKeyValueAsString(RegistryKey key, string valueName)
        {
            string result = null;
            try
            {
                // Check the type of the value
                RegistryValueKind kind = key.GetValueKind(valueName);
                if (kind == RegistryValueKind.String)
                {
                    // Get the value since it is a string
                    result = key.GetValue(valueName) as string;
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (SecurityException)
            {
            }

            return result;
        }
    }

    /// <summary>
    /// This class is the worker class used by RegistryStringResourceIndirect to load the resource
    /// assemblies and retrieve the resources inside the alternate app-domain.
    /// </summary>
    /// 
    internal class ResourceRetriever : MarshalByRefObject
    {
        /// <summary>
        /// Loads the specified assembly in the app-domain and retrieves the specified resource string.
        /// </summary>
        /// 
        /// <param name="assemblyName">
        /// Full name of the assembly to retrieve the resource from.
        /// </param>
        /// 
        /// <param name="modulePath">
        /// Full path of the assembly to retrieve the resource from.
        /// </param>
        /// 
        /// <param name="baseName">
        /// The resource base name to retrieve.
        /// </param>
        /// 
        /// <param name="resourceID">
        /// The resource ID of the resource to retrieve.
        /// </param>
        /// 
        /// <returns>
        /// The value of the specified string resource or null if the resource could not be found or loaded.
        /// </returns>
        /// 
        internal string GetStringResource(string assemblyName, string modulePath, string baseName, string resourceID)
        {
            string result = null;

            do // false loop
            {
                // Load the resource assembly
                Assembly assembly = LoadAssembly(assemblyName, modulePath);

                if (assembly == null)
                {
                    break;
                }

                CultureInfo currentCulture = System.Globalization.CultureInfo.CurrentUICulture;
                Stream stream = null;

                // Get the resource stream from the manifest
                // Loop until we have reached the default culture (identified by an empty Name string in the 
                // CultureInfo). 
                do
                {
                    string resourceStream = baseName;
                    if (!String.IsNullOrEmpty(currentCulture.Name))
                        resourceStream += "." + currentCulture.Name;
                    resourceStream += ".resources";

                    stream = assembly.GetManifestResourceStream(resourceStream);

                    if (stream != null)
                    {
                        break;
                    }

                    if (String.IsNullOrEmpty(currentCulture.Name))
                    {
                        break;
                    }

                    currentCulture = currentCulture.Parent;
                } while (true);

                if (stream == null)
                {
                    break;
                }

                // Retrieve the string resource from the stream.
                result = GetString(stream, resourceID);
            } while (false);

            return result;
        }

        /// <summary>
        /// Loads the specified assembly using ReflectionOnlyLoad or ReflectionOnlyLoadFrom
        /// </summary>
        /// 
        /// <param name="assemblyName">
        /// The FullName of the assembly to load. This takes precedence over the modulePath and
        /// will be passed to as a parameter to the ReflectionOnlyLoad.
        /// </param>
        /// 
        /// <param name="modulePath">
        /// The full path of the assembly to load. This is used if the ReflectionOnlyLoad using the 
        /// assemblyName doesn't load the assembly. It is passed as a parameter to the ReflectionOnlyLoadFrom API.
        /// </param>
        /// 
        /// <returns>
        /// An loaded instance of the specified resource assembly or null if the assembly couldn't be loaded.
        /// </returns>
        /// 
        /// <remarks>
        /// Since the intent of this method is to load resource assemblies, the standard culture fallback rules
        /// apply.  If the assembly couldn't be loaded for the current culture we fallback to the parent culture
        /// until the neutral culture is reached or an assembly is loaded.
        /// </remarks>
        /// 
        private static Assembly LoadAssembly(string assemblyName, string modulePath)
        {
            Assembly assembly = null;
            AssemblyName assemblyNameObj = new AssemblyName(assemblyName);

            // Split the path up so we can add the culture directory.
            string moduleBase = Path.GetDirectoryName(modulePath);
            string moduleFile = Path.GetFileName(modulePath);

            CultureInfo currentCulture = System.Globalization.CultureInfo.CurrentUICulture;

            // Loop until we have reached the default culture (identified by an empty Name string in the 
            // CultureInfo). 
            do
            {
                assembly = LoadAssemblyForCulture(currentCulture, assemblyNameObj, moduleBase, moduleFile);

                if (assembly != null)
                {
                    break;
                }

                if (String.IsNullOrEmpty(currentCulture.Name))
                {
                    break;
                }
                currentCulture = currentCulture.Parent;
            } while (true);


            return assembly;
        }

        /// <summary>
        /// Attempts to load the assembly for the specified culture
        /// </summary>
        /// 
        /// <param name="culture">
        /// The culture for which the assembly should be loaded.
        /// </param>
        /// 
        /// <param name="assemblyName">
        /// The name of the assembly without culture information (or at least undefined culture information).
        /// </param>
        /// 
        /// <param name="moduleBase">
        /// The directory containing the neutral culture assembly.
        /// </param>
        /// 
        /// <param name="moduleFile">
        /// The name of the assembly file.
        /// </param>
        /// 
        /// <returns>
        /// An instance of the loaded resource assembly or null if the assembly could not be loaded.
        /// </returns>
        /// 
        private static Assembly LoadAssemblyForCulture(
            CultureInfo culture,
            AssemblyName assemblyName,
            string moduleBase,
            string moduleFile)
        {
            Assembly assembly = null;

            // Set the assembly FullName to contain the culture we are trying to load.
            assemblyName.CultureInfo = culture;

            try
            {
                assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
            }
            catch (FileLoadException)
            {
            }
            catch (BadImageFormatException)
            {
            }
            catch (FileNotFoundException)
            {
            }

            if (assembly != null)
                return assembly;

            // Try the resources DLL
            string oldAssemblyName = assemblyName.Name;
            try
            {
                assemblyName.Name = oldAssemblyName + ".resources";
                assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
            }
            catch (FileLoadException)
            {
            }
            catch (BadImageFormatException)
            {
            }
            catch (FileNotFoundException)
            {
            }

            if (assembly != null)
                return assembly;

            assemblyName.Name = oldAssemblyName;

            // Add the culture directory into the file path
            string modulePath = Path.Combine(moduleBase, culture.Name);
            modulePath = Path.Combine(modulePath, moduleFile);

            if (File.Exists(modulePath))
            {
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(modulePath);
                }
                catch (FileLoadException)
                {
                }
                catch (BadImageFormatException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }
            return assembly;
        }

        /// <summary>
        /// Retrieves the specified resource string from the resource stream.
        /// </summary>
        /// 
        /// <param name="stream">
        /// The resource stream containing the desired resource.
        /// </param>
        /// 
        /// <param name="resourceID">
        /// The identifier of the string resource to retrieve from the stream.
        /// </param>
        /// 
        /// <returns>
        /// The resource string or null if the resourceID could not be found.
        /// </returns>
        /// 
        private static string GetString(Stream stream, string resourceID)
        {
            string result = null;

            ResourceReader rr = new ResourceReader(stream);

            foreach (DictionaryEntry e in rr)
            {
                if (String.Equals(resourceID, (string)e.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result = (string)e.Value;
                    break;
                }
            }
            /* NTRAID#Windows Out Of Band Releases-920971-2005/09/30-JeffJon
             * Whidbey v2.0.50727 has a bug where GetResourceData throws an NullReferenceException if
             * the assembly used to get the ResourceReader was loaded with ReflectionOnlyLoad. This code
             * would be more efficient than the iteration in the foreach loop above and should be enabled
             * when we move to the RTM version of Whidbey.
             * 
                        string resourceType = null;
                        byte[] resourceData = null;
                        rr.GetResourceData(resourceID, out resourceType, out resourceData);
             */
            return result;
        }
    }
}


