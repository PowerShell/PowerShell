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
    internal partial class PowerShellAssemblyLoadContext
    {
        #region Resource_Strings

        // We cannot use a satellite resources.dll to store resource strings for Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll. This is because when retrieving resource strings, ResourceManager
        // tries to load the satellite resources.dll using a probing approach, which will cause an infinite loop to PowerShellAssemblyLoadContext.Load(AssemblyName).
        // Take the 'en-US' culture as an example. When retrieving resource string to construct an exception, ResourceManager calls Assembly.Load(..) in the following order to load the resource dll:
        //     1. Load assembly with culture 'en-US' (Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.resources, Version=3.0.0.0, Culture=en-US, PublicKeyToken=31bf3856ad364e35)
        //     2. Load assembly with culture 'en'    (Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35)
        // When the first attempt fails, we again need to retrieve the resource string to construct another exception, which ends up with an infinite loop.
        private const string BaseFolderDoesNotExist = "The base directory '{0}' does not exist.";
        private const string ManifestDefinitionDoesNotMatch = "Could not load file or assembly '{0}' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference.";
        private const string AssemblyPathDoesNotExist = "Could not load file or assembly '{0}' or one of its dependencies. The system cannot find the file specified.";
        private const string InvalidAssemblyExtensionName = "Could not load file or assembly '{0}' or one of its dependencies. The file specified is not a DLL file.";
        private const string AbsolutePathRequired = "Absolute path information is required.";
        private const string SingletonAlreadyInitialized = "The singleton of PowerShellAssemblyLoadContext has already been initialized.";
        private const string UseResolvingEventHandlerOnly = "PowerShellAssemblyLoadContext was initialized to use its 'Resolving' event handler only.";

        #endregion Resource_Strings

        #region Constructor

        /// <summary>
        /// Initialize a singleton of PowerShellAssemblyLoadContext
        /// </summary>
        internal static PowerShellAssemblyLoadContext InitializeSingleton(string basePaths)
        {
            lock (s_syncObj)
            {
                if (Instance != null)
                    throw new InvalidOperationException(SingletonAlreadyInitialized);

                Instance = new PowerShellAssemblyLoadContext(basePaths);
                return Instance;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'. They will be the default paths to probe assemblies.
        /// The passed-in argument could be null or an empty string, in which case there is no default paths to probe assemblies.
        /// </param>
        private PowerShellAssemblyLoadContext(string basePaths)
        {
            #region Validation
            if (string.IsNullOrEmpty(basePaths))
            {
                _basePaths = Array.Empty<string>();
            }
            else
            {
                _basePaths = basePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < _basePaths.Length; i++)
                {
                    string basePath = _basePaths[i];
                    if (!Directory.Exists(basePath))
                    {
                        string message = string.Format(
                            CultureInfo.CurrentCulture,
                            BaseFolderDoesNotExist,
                            basePath);
                        throw new ArgumentException(message, "basePaths");
                    }
                    _basePaths[i] = basePath.Trim();
                }
            }
            #endregion Validation

            // FIRST: Add basePaths to probing paths
            _probingPaths = new List<string>(_basePaths);

            // NEXT: Initialize the CoreCLR type catalog dictionary [OrdinalIgnoreCase]
            _coreClrTypeCatalog = InitializeTypeCatalog();

            // LAST: Register 'Resolving' handler on the default load context.
            AssemblyLoadContext.Default.Resolving += Resolve;
        }

        #endregion Constructor

        #region Fields

        private readonly static object s_syncObj = new object();
        private readonly string[] _basePaths;
        // Initially, 'probingPaths' only contains psbase path. But every time we load an assembly through 'LoadFrom(string AssemblyPath)', we
        // add its parent path to 'probingPaths', so that we are able to support implicit loading of an assembly from the same place where the
        // requesting assembly is located.
        // We don't need to worry about removing any paths from 'probingPaths', because once an assembly is loaded, it won't be unloaded until
        // the current process exits, and thus the assembly itself and its parent folder cannot be deleted or renamed.
        private readonly List<string> _probingPaths;
        // CoreCLR type catalog dictionary
        //  - Key: namespace qualified type name (FullName)
        //  - Value: strong name of the TPA that contains the type represented by Key.
        private readonly Dictionary<string, string> _coreClrTypeCatalog;
        private readonly string[] _extensions = new string[] { ".ni.dll", ".dll" };

        /// <summary>
        /// Assembly cache across the AppDomain
        /// </summary>
        /// <remarks>
        /// We user the assembly short name (AssemblyName.Name) as the key.
        /// According to the Spec of AssemblyLoadContext, "in the context of a given instance of AssemblyLoadContext, only one assembly with
        /// a given name can be loaded. Attempt to load a second assembly with the same name and different MVID will result in an exception."
        ///
        /// MVID is Module Version Identifier, which is a guid. Its purpose is solely to be unique for each time the module is compiled, and
        /// it gets regenerated for every compilation. That means AssemblyLoadContext cannot handle loading two assemblies with the same name
        /// but different versions, not even two assemblies with the exactly same code and version but built by two separate compilations.
        ///
        /// Therefore, there is no need to use the full assembly name as the key. Short assembly name is sufficient.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, Assembly> s_assemblyCache =
            new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        #endregion Fields

        #region Properties

        /// <summary>
        /// Singleton instance of PowerShellAssemblyLoadContext
        /// </summary>
        internal static PowerShellAssemblyLoadContext Instance
        {
            get; private set;
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Assembly load event
        /// </summary>
        internal event Action<Assembly> AssemblyLoad;

        #endregion Events

        #region Internal_Methods

        /// <summary>
        /// Load an IL or NI assembly from its file path.
        /// </summary>
        internal Assembly LoadFrom(string assemblyPath)
        {
            ValidateAssemblyPath(assemblyPath, "assemblyPath");

            Assembly asmLoaded;
            AssemblyName assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);

            // Probe the assembly cache
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (s_syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the assembly through 'LoadFromNativeImagePath' or 'LoadFromAssemblyPath'
                asmLoaded = assemblyPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                    ? AssemblyLoadContext.Default.LoadFromNativeImagePath(assemblyPath, null)
                    : AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    s_assemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                    // Add its parent path to our probing paths
                    string parentPath = Path.GetDirectoryName(assemblyPath);
                    if (!_probingPaths.Contains(parentPath))
                    {
                        _probingPaths.Add(parentPath);
                    }
                }
            }

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
        }

        /// <summary>
        /// Load assembly from byte stream.
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
            lock (s_syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the assembly through 'LoadFromStream'
                asmLoaded = AssemblyLoadContext.Default.LoadFromStream(assembly);
                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    s_assemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                }
            }

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
        }

        /// <summary>
        /// Get the current loaded assemblies
        /// </summary>
        internal IEnumerable<Assembly> GetAssembly(string namespaceQualifiedTypeName)
        {
            // If 'namespaceQualifiedTypeName' is specified and it's a CoreCLR framework type,
            // then we only return that specific TPA assembly.
            if (!string.IsNullOrEmpty(namespaceQualifiedTypeName))
            {
                string tpaStrongName;
                if (_coreClrTypeCatalog.TryGetValue(namespaceQualifiedTypeName, out tpaStrongName))
                {
                    try
                    {
                        return new Assembly[] { GetTrustedPlatformAssembly(tpaStrongName) };
                    }
                    catch (FileNotFoundException)
                    {
                        // It's possible that the type catalog generated in OPS contains more entries than
                        // the one generated in windows build. This is because in OPS we have more freedom
                        // to control what packages to depend on, such as Json.NET.
                        // If we deploy the PSALC.dll generated from OPS to NanoServer, then it's possible
                        // that 'GetTrustedPlatformAssembly(tpaStrongName)' may fail for such entries. In
                        // this case, we ignore the exception and return our cached assemblies.
                    }
                }
            }

            // Otherwise, we return null
            return null;
        }

        /// <summary>
        /// Get the namespace-qualified type names of all available CoreCLR .NET types.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal IEnumerable<string> GetAvailableDotNetTypes()
        {
            return _coreClrTypeCatalog.Keys;
        }

        /// <summary>
        /// Set the profile optimization root on the appropriate load context
        /// </summary>
        /// <remarks>
        /// When using PS ALC as a full fledged ALC in OPS, we don't enable profile optimization.
        /// This is because PS assemblies will be recorded in the profile, and the next time OPS
        /// starts up, the default context will load the PS assemblies pretty early to ngen them
        /// in another CPU core, so our Load override won't track the loading of them, and thus
        /// OPS will fail to work.
        /// The root cause is that dotnet.exe put all PS assemblies in TPA list. If PS assemblies
        /// are not in TPA list, then we can enable profile optimization without a problem.
        /// </remarks>
        internal void SetProfileOptimizationRootImpl(string directoryPath)
        {
            AssemblyLoadContext.Default.SetProfileOptimizationRoot(directoryPath);
        }

        /// <summary>
        /// Start the profile optimization on the appropriate load context
        /// </summary>
        /// <remarks>
        /// When using PS ALC as a full fledged ALC in OPS, we don't enable profile optimization.
        /// This is because PS assemblies will be recorded in the profile, and the next time OPS
        /// starts up, the default context will load the PS assemblies pretty early to ngen them
        /// in another CPU core, so our Load override won't track the loading of them, and thus
        /// OPS will fail to work.
        /// The root cause is that dotnet.exe put all PS assemblies in TPA list. If PS assemblies
        /// are not in TPA list, then we can enable profile optimization without a problem.
        /// </remarks>
        internal void StartProfileOptimizationImpl(string profile)
        {
            AssemblyLoadContext.Default.StartProfileOptimization(profile);
        }

        #endregion Internal_Methods

        #region Private_Methods

        /// <summary>
        /// The handler for the Resolving event
        /// </summary>
        private Assembly Resolve(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            // Probe the assembly cache
            Assembly asmLoaded;
            if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                return asmLoaded;

            // Prepare to load the assembly
            lock (s_syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Search the specified assembly in probing paths, and load it through 'LoadFromAssemblyPath' if the file exists and matches the requested AssemblyName.
                // If the CultureName of the requested assembly is not NullOrEmpty, then it's a resources.dll and we need to search corresponding culture sub-folder.
                bool isAssemblyFileFound = false, isAssemblyFileMatching = false;
                string asmCultureName = assemblyName.CultureName ?? string.Empty;
                string asmFilePath = null;

                for (int i = 0; i < _probingPaths.Count; i++)
                {
                    string probingPath = _probingPaths[i];
                    string asmCulturePath = Path.Combine(probingPath, asmCultureName);
                    for (int k = 0; k < _extensions.Length; k++)
                    {
                        string asmFileName = assemblyName.Name + _extensions[k];
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

                // We failed to find the assembly file; or we found the file, but the assembly file doesn't match the request.
                // In this case, return null so that other Resolving event handlers can kick in to resolve the request.
                if (!isAssemblyFileFound || !isAssemblyFileMatching)
                {
                    return null;
                }

                asmLoaded = asmFilePath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                                ? loadContext.LoadFromNativeImagePath(asmFilePath, null)
                                : loadContext.LoadFromAssemblyPath(asmFilePath);
                if (asmLoaded != null)
                {
                    // Add the loaded assembly to the cache
                    s_assemblyCache.TryAdd(assemblyName.Name, asmLoaded);
                }
            }

            // Raise AssemblyLoad event
            OnAssemblyLoaded(asmLoaded);
            return asmLoaded;
        }

        /// <summary>
        /// Handle the AssemblyLoad event
        /// </summary>
        private void OnAssemblyLoaded(Assembly assemblyLoaded)
        {
            Action<Assembly> assemblyLoadHandler = AssemblyLoad;
            if (assemblyLoaded != null && assemblyLoadHandler != null)
            {
                try
                {
                    assemblyLoadHandler(assemblyLoaded);
                }
                catch
                {
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
            if (s_assemblyCache.TryGetValue(assemblyName.Name, out asmLoaded))
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
            // We always depend on the default context to load the TPAs that are recorded in
            // the type catalog.
            //   - If the requested TPA is already loaded, then 'Assembly.Load' will just get
            //     it back from the cache of default context.
            //   - If the requested TPA is not loaded yet, then 'Assembly.Load' will make the
            //     default context to load it
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
    /// This is the managed entry point for Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll.
    /// </summary>
    public class PowerShellAssemblyLoadContextInitializer
    {
        /// <summary>
        /// Create a singleton of PowerShellAssemblyLoadContext.
        /// Then register to the Resolving event of the load context that loads this assembly.
        /// </summary>
        /// <remarks>
        /// This method is to be used by native host whose TPA list doesn't include PS assemblies, such as the
        /// in-box Nano powershell.exe, the PS remote WinRM plugin, in-box Nano DSC and in-box Nano SCOM agent.
        /// </remarks>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'.
        /// They will be the default paths to probe assemblies.
        /// </param>
        public static void SetPowerShellAssemblyLoadContext([MarshalAs(UnmanagedType.LPWStr)]string basePaths)
        {
            if (string.IsNullOrEmpty(basePaths))
                throw new ArgumentNullException("basePaths");

            PowerShellAssemblyLoadContext.InitializeSingleton(basePaths);
        }
    }
}

#endif

