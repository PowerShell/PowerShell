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
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace System.Management.Automation
{
    /// <summary>
    /// The powershell custom AssemblyLoadContext implementation
    /// </summary>
    public partial class PowerShellAssemblyLoadContext : AssemblyLoadContext
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
        private const string AbsolutePathRequired = "Absolute path information is required.";

        #endregion Resource_Strings

        #region Constructor

        /// <summary>
        /// This constructor is for testability purpose only
        /// </summary>
        protected PowerShellAssemblyLoadContext()
        {
        }

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
            if (string.IsNullOrEmpty(basePaths))
            {
                throw new ArgumentNullException("basePaths");
            }

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
        }

        #endregion Constructor

        #region Fields

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

        #region Events

        /// <summary>
        /// Assembly load event
        /// </summary>
        internal event Action<Assembly> AssemblyLoad;

        #endregion Events

        #region Protected_Internal_Methods

        /// <summary>
        /// Implement the AssemblyLoadContext.Load(AssemblyName). Search the requested assembly in probing paths.
        /// Search the file "[assemblyName.Name][.ni].dll" in probing paths. If the file is found and it matches the requested AssemblyName, load it with LoadFromAssemblyPath.
        /// </summary>
        protected override Assembly Load(AssemblyName assemblyName)
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
                            AssemblyName asmNameFound = GetAssemblyName(asmFilePath);
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

                asmLoaded = asmFilePath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase) 
                                ? base.LoadFromNativeImagePath(asmFilePath, null) 
                                : base.LoadFromAssemblyPath(asmFilePath);
                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    AssemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                }
            }

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
        }

        /// <summary>
        /// Load an IL or NI assembly from its file path.
        /// </summary>
        internal Assembly LoadFrom(string assemblyPath)
        {
            ValidateAssemblyPath(assemblyPath, "assemblyPath");

            Assembly asmLoaded;
            AssemblyName assemblyName = GetAssemblyName(assemblyPath);

            // Probe the assembly cache
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the assembly through 'LoadFromNativeImagePath' or 'LoadFromAssemblyPath'
                asmLoaded = assemblyPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                    ? base.LoadFromNativeImagePath(assemblyPath, null)
                    : base.LoadFromAssemblyPath(assemblyPath);

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

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
        }

        /// <summary>
        /// Load assemlby from byte stream.
        /// </summary>
        internal Assembly LoadFrom(Stream assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            Assembly asmLoaded;
            AssemblyName assemblyName = GetAssemblyName(assembly);

            // Probe the assembly cache
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the assembly through 'base.LoadFromStream'
                asmLoaded = base.LoadFromStream(assembly);
                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    AssemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                }
            }

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
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
        /// <remarks>
        /// This is for adding a dynamic assembly to the cache.
        /// PowerShell generates dynamic assemblies by directly emitting IL, and this API
        /// is to add such assemblies to the cache so that types in them are discoverable.
        /// </remarks>
        internal bool TryAddAssemblyToCache(Assembly assembly)
        {
            AssemblyName asmName = assembly.GetName();
            bool success = AssemblyCache.TryAdd(asmName.Name, assembly);
            // Raise AssemblyLoad event
            if (success) { OnAssemblyLoaded(assembly); }
            return success;
        }

        /// <summary>
        /// Probe for the assembly file with the specified short name for metadata analysis purpose
        /// </summary>
        internal string ProbeAssemblyFileForMetadataAnalysis(string assemblyShortName, string additionalSearchPath)
        {
            if (string.IsNullOrEmpty(assemblyShortName))
            {
                throw new ArgumentNullException("assemblyShortName");
            }

            bool useAdditionalSearchPath = false;
            if (!string.IsNullOrEmpty(additionalSearchPath))
            {
                if (!Path.IsPathRooted(additionalSearchPath))
                {
                    throw new ArgumentException(AbsolutePathRequired, "additionalSearchPath");
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
        /// Handle the AssemblyLoad event
        /// </summary>
        private void OnAssemblyLoaded(Assembly assemblyLoaded)
        {
            Action<Assembly> assemblyLoadHandler = AssemblyLoad;
            if (assemblyLoaded != null && assemblyLoadHandler != null)
            {
                try {
                    assemblyLoadHandler(assemblyLoaded);
                }
                catch {
                    // Catch all exceptions, same behavior as AppDomain.AssemblyLoad
                }
            }
        }

        /// <summary>
        /// Validate assembly path value for the specified parameter
        /// </summary>
        private void ValidateAssemblyPath(string assemblyPath, string parameterName)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!Path.IsPathRooted(assemblyPath))
            {
                throw new ArgumentException(AbsolutePathRequired, parameterName);
            }
            
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
        }
        
        /// <summary>
        /// Get AssemblyName of an assembly stream
        /// </summary>
        private AssemblyName GetAssemblyName(Stream assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string strongAssemblyName = null;
            using (PEReader peReader = new PEReader(assembly, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata))
            {
                MetadataReader metadataReader = peReader.GetMetadataReader();
                strongAssemblyName = AssemblyMetadataHelper.GetAssemblyStrongName(metadataReader);
            }
            
            assembly.Seek(0, SeekOrigin.Begin);
            return new AssemblyName(strongAssemblyName);
        }

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
            // Load the specified TPA. If the TPA is already loaded, it will be somehow
            // cached in CoreCLR runtime, and thus calling 'Assembly.Load' again won't
            // cause any overhead.
            AssemblyName assemblyName = new AssemblyName(tpaStrongName);
            Assembly asmLoaded = Assembly.Load(assemblyName);
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
    public static class PowerShellAssemblyLoadContextInitializer
    {
        // Porting note: it's much easier to send an LPStr on Linux
        private const UnmanagedType stringType = 
            #if LINUX
            UnmanagedType.LPStr
            #else
            UnmanagedType.LPWStr
            #endif
            ;

        public static PowerShellAssemblyLoadContext PSAsmLoadContext;

        /// <summary>
        /// Set the default Assembly Load Context
        /// </summary>
        public static void SetPowerShellAssemblyLoadContext([MarshalAs(stringType)]string basePaths)
        {
            if (string.IsNullOrEmpty(basePaths))
            {
                throw new ArgumentNullException("basePaths");
            }

            if (PSAsmLoadContext == null)
            {
                PSAsmLoadContext = new PowerShellAssemblyLoadContext(basePaths);
            }
        }
    }
}

#endif
