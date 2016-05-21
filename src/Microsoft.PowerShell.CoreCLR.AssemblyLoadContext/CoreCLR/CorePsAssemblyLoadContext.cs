/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if CORECLR

using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Loader;

namespace System.Management.Automation
{
    /// <summary>
    /// The powershell custom assembly loader implementation
    /// </summary>
    internal partial class PowerShellAssemblyLoadContext
    {
        #region Resource_Strings

        // We cannot use a satellite resources.dll to store resource strings for Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll. This is because when retrieving resource strings, ResourceManager 
        // tries to load the satellite resources.dll using a probing approach, which will cause an infinite loop to PowerShellAssemblyLoadContext.Load(AssemblyName).
        // Take the 'en-US' culture as an example. When retrieving resource string to construct an exception, ResourceManager calls Assembly.Load(..) in the following order to load the resource dll:
        //     1. Load assembly with culture 'en-US' (Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.resources, Version=3.0.0.0, Culture=en-US, PublicKeyToken=31bf3856ad364e35)
        //     2. Load assembly with culture 'en'    (Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35) 
        // When the first attempt fails, we again need to retrieve the resouce string to construct another exception, which ends up with an infinite loop.
        private const string BaseFolderDoesNotExist = "The base directory '{0}' does not exist.";
        private const string CannotFindFileBasedOnAssemblyName = "Could not load file or assembly '{0}' or one of its dependencies. The system cannot find the file specified under any probing paths.";
        private const string ManifestDefinitionDoesNotMatch = "Could not load file or assembly '{0}' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference.";
        private const string AssemblyPathDoesNotExist = "Could not load file or assembly '{0}' or one of its dependencies. The system cannot find the file specified.";
        private const string InvalidAssemblyExtensionName = "Could not load file or assembly '{0}' or one of its dependencies. The file specified is not a DLL file.";

        #endregion Resource_Strings

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'.
        /// They will be the default paths to probe assemblies.
        /// </param>
        internal PowerShellAssemblyLoadContext(string basePaths)
        {
            #region Validation
            this.basePaths = basePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < this.basePaths.Length; i++)
            {
                string basePath = this.basePaths[i];
                if (!Directory.Exists(basePath))
                {
                    string message = string.Format(
                        CultureInfo.CurrentCulture,
                        BaseFolderDoesNotExist,
                        basePath);
                    throw new ArgumentException(message, "basePaths");
                }
                this.basePaths[i] = basePath.Trim();
            }
            #endregion Validation

            // FIRST: Add basePaths to probing paths
            this.probingPaths = new List<string>(this.basePaths);

            // NEXT: Initialize the CoreCLR type catalog dictionary [OrdinalIgnoreCase]
            //  - Key: namespace qualified type name (FullName)
            //  - Value: strong name of the TPA that contains the type represented by Key.
            coreClrTypeCatalog = InitializeTypeCatalog();

            this.loadContext = AssemblyLoadContext.Default;
            loadContext.Resolving += Resolve;
        }

        #endregion Constructor

        #region Fields

        // AssemblyLoadContext used by this loader
        private readonly AssemblyLoadContext loadContext;

        // Serialized type catalog file
        private readonly object syncObj = new object();
        private readonly string[] basePaths;
        // Initially, 'probingPaths' only contains psbase path. But every time we load an assembly through 'LoadFrom(string AssemblyPath)', we
        // add its parent path to 'probingPaths', so that we are able to support implicit loading of an assembly from the same place where the
        // requesting assembly is located.
        // We don't need to worry about removing any paths from 'probingPaths', because once an assembly is loaded, it won't be unloaded until
        // the current process exits, and thus the assembly itself and its parent folder cannot be deleted or renamed.
        private readonly List<string> probingPaths;
        // We use dictionary because the generated binary file by DataContractSerializer is about 39% smaller in size than using Hashtable.
        private readonly Dictionary<string, string> coreClrTypeCatalog;
        private readonly string[] extensions = new string[] { ".ni.dll", ".dll" };

        /// <summary>
        /// Assembly cache accross the AppDomain
        /// </summary>
        /// <remarks>
        /// We user the assembly short name (AssemblyName.Name) as the key.
        /// According to the Spec of AssemblyLoadContext, "in the context of a given instance of AssemblyLoadContext, only one assembly with 
        /// a given name can be loaded. Attempt to load a second assembly with the same name and different MVID will result in an exception."
        /// 
        /// MVID is Module Version Identifier, which is a guid. Its purpose is solely to be unique for each time the module is compiled, and
        /// it gets regenerated for every compilation. That means AssemblyLoadContext cannot handle loading two assemblies with the same name
        /// but different veresions, not even two asssemblies with the exactly same code and version but built by two separate compilations.
        /// 
        /// Therefore, there is no need to use the full assembly name as the key. Short assembly name is sufficient.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, Assembly> AssemblyCache =
            new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        #endregion Fields

        #region Protected_Internal_Methods

        /// <summary>
        /// The global instance of PowerShellAssemblyLoadContext
        /// </summary>
        internal static PowerShellAssemblyLoadContext Instance { get; set; }

        /// <summary>
        /// Implement the AssemblyLoadContext.Resolving event handler. Search the requested assembly in probing paths.
        /// Search the file "[assemblyName.Name][.ni].dll" in probing paths. If the file is found and it matches the requested AssemblyName, load it with LoadFromAssemblyPath.
        /// </summary>
        internal Assembly Resolve(AssemblyLoadContext sender, AssemblyName assemblyName)
        {
            // Probe the assembly cache
            Assembly asmLoaded;
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Search the specified assembly in probing paths, and load it through 'LoadFromAssemblyPath' if the file exists and matches the requested AssemblyName.
                // If the CultureName of the requested assembly is not NullOrEmpty, then it's a resources.dll and we need to search corresponding culture sub-folder.
                bool isAssemblyFileFound = false, isAssemblyFileMatching = false;
                string asmCultureName = assemblyName.CultureName ?? string.Empty;
                string asmFilePath = null;

                for (int i = 0; i < probingPaths.Count; i++)
                {
                    string probingPath = probingPaths[i];
                    string asmCulturePath = Path.Combine(probingPath, asmCultureName);
                    for (int k = 0; k < extensions.Length; k++)
                    {
                        string asmFileName = assemblyName.Name + extensions[k];
                        asmFilePath = Path.Combine(asmCulturePath, asmFileName);

                        if (File.Exists(asmFilePath))
                        {
                            isAssemblyFileFound = true;
                            AssemblyName asmNameFound = AssemblyLoadContext.GetAssemblyName(asmFilePath);
                            if (IsAssemblyMatching(assemblyName, asmNameFound))
                            {
                                isAssemblyFileMatching = true;
                                break;
                            }
                        }
                    }

                    if (isAssemblyFileFound && isAssemblyFileMatching)
                    {
                        break;
                    }
                }

                // We failed to find the file specified
                if (!isAssemblyFileFound)
                {
                    ThrowFileNotFoundException(
                        CannotFindFileBasedOnAssemblyName,
                        assemblyName.FullName);
                }

                // We found the file specified, but the found assembly doesn't match the request
                if (!isAssemblyFileMatching)
                {
                    ThrowFileLoadException(
                        ManifestDefinitionDoesNotMatch,
                        assemblyName.FullName);
                }

                try
                {
                    asmLoaded = asmFilePath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                        ? loadContext.LoadFromNativeImagePath(asmFilePath, null)
                        : loadContext.LoadFromAssemblyPath(asmFilePath);
                }
                // Since .NET CLI built versions of PowerShell have all the
                // built-in assemblies in the TPA list, the above will throw,
                // and we have to use Assembly.Load. However, we must try the
                // above first, otherwise assemblies that exist outside the TPA
                // list will go into a recursive loop.
                catch (System.IO.FileLoadException)
                {
                    asmLoaded = System.Reflection.Assembly.Load(assemblyName);
                }

                // If it loaded, add it to the cache
                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    AssemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                }
            }

            return asmLoaded;
        }

        /// <summary>
        /// Load an assembly from its name.
        /// </summary>
        internal Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        /// <summary>
        /// Load an assembly from its file path.
        /// </summary>
        internal Assembly LoadFrom(string assemblyPath)
        {
            #region Validation
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException("assemblyPath");
            }

            assemblyPath = Path.GetFullPath(assemblyPath);
            if (!File.Exists(assemblyPath))
            {
                ThrowFileNotFoundException(
                    AssemblyPathDoesNotExist, 
                    assemblyPath);
            }

            if (!string.Equals(Path.GetExtension(assemblyPath), ".DLL", StringComparison.OrdinalIgnoreCase))
            {
                ThrowFileLoadException(
                    InvalidAssemblyExtensionName, 
                    assemblyPath);
            }
            #endregion Validation

            Assembly asmLoaded;
            AssemblyName assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);

            // Probe the assembly cache
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                try
                {
                    // Load the assembly through 'LoadFromNativeImagePath' or 'LoadFromAssemblyPath'
                    asmLoaded = assemblyPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                        ? loadContext.LoadFromNativeImagePath(assemblyPath, null)
                        : loadContext.LoadFromAssemblyPath(assemblyPath);
                }
                // Since .NET CLI built versions of PowerShell have all the
                // built-in assemblies in the TPA list, the above will throw,
                // and we have to use Assembly.Load. However, we must try the
                // above first, otherwise assemblies that exist outside the TPA
                // list will go into a recursive loop.
                catch (System.IO.FileLoadException)
                {
                    asmLoaded = System.Reflection.Assembly.Load(assemblyName);
                }

                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    AssemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                    // Add the its parent path to our probing paths
                    string parentPath = Path.GetDirectoryName(assemblyPath);
                    if (!probingPaths.Contains(parentPath))
                    {
                        probingPaths.Add(parentPath);
                    }
                }
            }

            return asmLoaded;
        }

        /// <summary>
        /// Load assemlby from byte stream.
        /// </summary>
        internal Assembly LoadFrom(Stream assembly)
        {
            var asm = loadContext.LoadFromStream(assembly);
            TryAddAssemblyToCache(asm);
            return asm;
        }

        /// <summary>
        /// Get the current loaded assemblies
        /// </summary>
        internal IEnumerable<Assembly> GetAssemblies(string namespaceQualifiedTypeName)
        {
            // If 'namespaceQualifiedTypeName' is specified and it's a CoreCLR framework type,
            // then we only return that specific TPA assembly.
            if (!string.IsNullOrEmpty(namespaceQualifiedTypeName))
            {
                string tpaStrongName;
                if (coreClrTypeCatalog.TryGetValue(namespaceQualifiedTypeName, out tpaStrongName))
                {
                    return new Assembly[] { GetTrustedPlatformAssembly(tpaStrongName) };
                }
            }

            // Otherwise, we return all assemblies from the AssemblyCache
            return AssemblyCache.Values;
        }

        /// <summary>
        /// Try adding a new assembly to the cache
        /// </summary>
        internal bool TryAddAssemblyToCache(Assembly assembly)
        {
            AssemblyName asmName = assembly.GetName();
            return AssemblyCache.TryAdd(asmName.Name, assembly);
        }

        /// <summary>
        /// Probe for the assembly file with the specified short name for metadata analysis purpose
        /// </summary>
        internal string ProbeAssemblyFileForMetadataAnalysis(string assemblyShortName, string additionalSearchPath)
        {
            bool useAdditionalSearchPath = false;
            if (!string.IsNullOrWhiteSpace(additionalSearchPath))
            {
                if (!Path.IsPathRooted(additionalSearchPath))
                {
                    additionalSearchPath = Path.GetFullPath(additionalSearchPath);
                }
                useAdditionalSearchPath = Directory.Exists(additionalSearchPath);
            }

            // Construct the probing paths for searching the specified assembly file
            string[] metadataProbingPaths = this.basePaths;
            if (useAdditionalSearchPath)
            {
                var searchPaths = new List<string>() { additionalSearchPath };
                searchPaths.AddRange(this.basePaths);
                metadataProbingPaths = searchPaths.ToArray();
            }

            for (int i = 0; i < metadataProbingPaths.Length; i++)
            {
                string metadataProbingPath = metadataProbingPaths[i];
                for (int k = 0; k < extensions.Length; k++)
                {
                    string asmFileName = assemblyShortName + extensions[k];
                    string asmFilePath = Path.Combine(metadataProbingPath, asmFileName);
                    if (File.Exists(asmFilePath))
                    {
                        return asmFilePath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the namespace-qualified type names of all available CoreCLR .NET types.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal IEnumerable<string> GetAvailableDotNetTypes()
        {
            return coreClrTypeCatalog.Keys;
        }

        #endregion Protected_Internal_Methods

        #region Private_Methods

        /// <summary>
        /// Try to get the specified assembly from cache
        /// </summary>
        private bool TryGetAssemblyFromCache(AssemblyName assemblyName, out Assembly asmLoaded)
        {
            if (AssemblyCache.TryGetValue(assemblyName.Name, out asmLoaded))
            {
                // Check if loaded assembly matches the request
                if (IsAssemblyMatching(assemblyName, asmLoaded.GetName()))
                    return true;

                // In the context of a given instance of AssemblyLoadContext, only one assembly with the
                // same name can be loaded. So we throw exception if assembly doesn't match the request.
                ThrowFileLoadException(
                    ManifestDefinitionDoesNotMatch,
                    assemblyName.FullName);
            }

            return false;
        }

        /// <summary>
        /// Check if the loaded assembly matches the request
        /// </summary>
        /// <param name="requestedAssembly">AssemblyName of the requested assembly</param>
        /// <param name="loadedAssembly">AssemblyName of the loaded assembly</param>
        /// <returns></returns>
        private bool IsAssemblyMatching(AssemblyName requestedAssembly, AssemblyName loadedAssembly)
        {
            //
            // We use the same rules as CoreCLR loader to compare the requested assembly and loaded assembly:
            //  1. If 'Version' of the requested assembly is specified, then the requested version should be less or equal to the loaded version;
            //  2. If 'CultureName' of the requested assembly is specified (not NullOrEmpty), then the CultureName of the loaded assembly should be the same;
            //  3. If 'PublicKeyToken' of the requested assembly is specified (not Null or EmptyArray), then the PublicKenToken of the loaded assembly should be the same.
            //

            // Version of the requested assembly should be the same or before the version of loaded assembly
            if (requestedAssembly.Version != null && requestedAssembly.Version.CompareTo(loadedAssembly.Version) > 0)
            {
                return false;
            }

            // CultureName of requested assembly and loaded assembly should be the same
            string requestedCultureName = requestedAssembly.CultureName;
            if (!string.IsNullOrEmpty(requestedCultureName) && !requestedCultureName.Equals(loadedAssembly.CultureName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // PublicKeyToken should be the same, unless it's not specified in the requested assembly
            byte[] requestedPublicKeyToken = requestedAssembly.GetPublicKeyToken();
            byte[] loadedPublicKeyToken = loadedAssembly.GetPublicKeyToken();

            if (requestedPublicKeyToken != null && requestedPublicKeyToken.Length > 0)
            {
                if (loadedPublicKeyToken == null || requestedPublicKeyToken.Length != loadedPublicKeyToken.Length)
                    return false;

                for (int i = 0; i < requestedPublicKeyToken.Length; i++)
                {
                    if (requestedPublicKeyToken[i] != loadedPublicKeyToken[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the TPA that is represented by the specified assembly strong name.
        /// </summary>
        /// <param name="tpaStrongName">
        /// The assembly strong name of a CoreCLR Trusted_Platform_Assembly
        /// </param>
        private Assembly GetTrustedPlatformAssembly(string tpaStrongName)
        {
            AssemblyName assemblyName = new AssemblyName(tpaStrongName);

            // Probe the assembly cache
            Assembly asmLoaded;
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the TPA
            lock (syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the specified TPA
                asmLoaded = Assembly.Load(assemblyName);
                AssemblyCache.TryAdd(assemblyName.Name, asmLoaded);
            }

            return asmLoaded;
        }

        /// <summary>
        /// Throw FileLoadException
        /// </summary>
        private void ThrowFileLoadException(string errorTemplate, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, errorTemplate, args);
            throw new FileLoadException(message);
        }

        /// <summary>
        /// Throw FileNotFoundException
        /// </summary>
        private void ThrowFileNotFoundException(string errorTemplate, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, errorTemplate, args);
            throw new FileNotFoundException(message);
        }

        #endregion Private_Methods
    }

    /// <summary>
    /// Set an instance of PowerShellAssemblyLoadContext to be the default Assembly Load Context.
    /// This is the managed entry point for Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll.
    /// </summary>
    public class PowerShellAssemblyLoadContextInitializer
    {
        // Porting note: it's much easier to send an LPStr on Linux
        private const UnmanagedType stringType = 
            #if LINUX
            UnmanagedType.LPStr
            #else
            UnmanagedType.LPWStr
            #endif
            ;

        /// <summary>
        /// Set the default Assembly Load Context
        /// </summary>
        public static void SetPowerShellAssemblyLoadContext([MarshalAs(stringType)]string basePaths)
        {
            if (PowerShellAssemblyLoadContext.Instance == null)
            {
                PowerShellAssemblyLoadContext.Instance = new PowerShellAssemblyLoadContext(basePaths);
            }
        }
    }
}

#endif
