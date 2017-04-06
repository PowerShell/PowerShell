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
    internal partial class PowerShellAssemblyLoadContext : AssemblyLoadContext
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
        internal static PowerShellAssemblyLoadContext InitializeSingleton(string basePaths, bool useResolvingHandlerOnly)
        {
            lock (s_syncObj)
            {
                if (Instance != null)
                    throw new InvalidOperationException(SingletonAlreadyInitialized);

                Instance = new PowerShellAssemblyLoadContext(basePaths, useResolvingHandlerOnly);
                return Instance;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'.
        /// They will be the default paths to probe assemblies.
        /// </param>
        /// <param name="useResolvingHandlerOnly">
        /// Indicate whether this instance is going to be used as a
        /// full fledged ALC, or only its 'Resolve' handler is going
        /// to be used.
        /// </param>
        /// <remarks>
        /// When <paramref name="useResolvingHandlerOnly"/> is true, we will register to the 'Resolving' event of the default
        /// load context with our 'Resolve' method, and depend on the default load context to resolve/load assemblies for PS.
        /// This mode is used when TPA list of the native host only contains .NET Core libraries.
        /// In this case, TPA binder will be consulted before hitting our resolving logic. The binding order of Assembly.Load is:
        ///     TPA binder --> Resolving event
        ///
        /// When <paramref name="useResolvingHandlerOnly"/> is false, we will use this instance as a full fledged load context
        /// to resolve/load assemblies for PS. This mode is used when TPA list of the native host contains both .NET Core libraries
        /// and PS assemblies.
        /// In this case, our Load override will kick in before consulting the TPA binder. The binding order of Assembly.Load is:
        ///     Load override --> TPA binder --> Resolving event
        /// </remarks>
        private PowerShellAssemblyLoadContext(string basePaths, bool useResolvingHandlerOnly)
        {
            #region Validation
            if (string.IsNullOrEmpty(basePaths))
            {
                throw new ArgumentNullException("basePaths");
            }

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
            #endregion Validation

            // FIRST: Add basePaths to probing paths
            _probingPaths = new List<string>(_basePaths);

            // NEXT: Initialize the CoreCLR type catalog dictionary [OrdinalIgnoreCase]
            _coreClrTypeCatalog = InitializeTypeCatalog();

            // LAST: Handle useResolvingHandlerOnly flag
            _useResolvingHandlerOnly = useResolvingHandlerOnly;
            _activeLoadContext = useResolvingHandlerOnly ? Default : this;
            if (useResolvingHandlerOnly)
            {
                Default.Resolving += Resolve;
            }
            else
            {
                var tempSet = new HashSet<string>(_coreClrTypeCatalog.Values, StringComparer.OrdinalIgnoreCase);
                _tpaSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.PowerShell.CoreCLR.AssemblyLoadContext" };
                foreach (string tpa in tempSet)
                {
                    string shortName = tpa.Substring(0, tpa.IndexOf(','));
                    _tpaSet.Add(shortName);
                }
            }
        }

        #endregion Constructor

        #region Fields

        private readonly bool _useResolvingHandlerOnly;
        private readonly AssemblyLoadContext _activeLoadContext;
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
        private readonly HashSet<string> _tpaSet;
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

        #region Protected_Internal_Methods

        /// <summary>
        /// Implement the AssemblyLoadContext.Load(AssemblyName). Search the requested assembly in probing paths.
        /// Search the file "[assemblyName.Name][.ni].dll" in probing paths. If the file is found and it matches the requested AssemblyName, load it with LoadFromAssemblyPath.
        /// </summary>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (_useResolvingHandlerOnly)
                throw new NotSupportedException(UseResolvingEventHandlerOnly);

            // We let the default context load the assemblies included in the type catalog as there
            // appears to be a bug in .NET with method resolution with system libraries loaded by our
            // context and not the default. We use the short name because some packages have inconsistent
            // versions between reference and runtime assemblies.
            if (_tpaSet.Contains(assemblyName.Name))
                return null;

            return Resolve(this, assemblyName);
        }

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
            lock (s_syncObj)
            {
                // Probe the cache again in case it's already loaded
                if (TryGetAssemblyFromCache(assemblyName, out asmLoaded))
                    return asmLoaded;

                // Load the assembly through 'LoadFromNativeImagePath' or 'LoadFromAssemblyPath'
                asmLoaded = assemblyPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                    ? _activeLoadContext.LoadFromNativeImagePath(assemblyPath, null)
                    : _activeLoadContext.LoadFromAssemblyPath(assemblyPath);

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
                asmLoaded = _activeLoadContext.LoadFromStream(assembly);
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
        internal IEnumerable<Assembly> GetAssemblies(string namespaceQualifiedTypeName)
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

            // Otherwise, we return all assemblies from the AssemblyCache
            return s_assemblyCache.Values;
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
            bool success = s_assemblyCache.TryAdd(asmName.Name, assembly);
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
            string[] metadataProbingPaths = _basePaths;
            if (useAdditionalSearchPath)
            {
                var searchPaths = new List<string>() { additionalSearchPath };
                searchPaths.AddRange(_basePaths);
                metadataProbingPaths = searchPaths.ToArray();
            }

            for (int i = 0; i < metadataProbingPaths.Length; i++)
            {
                string metadataProbingPath = metadataProbingPaths[i];
                for (int k = 0; k < _extensions.Length; k++)
                {
                    string asmFileName = assemblyShortName + _extensions[k];
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
            if (_useResolvingHandlerOnly)
                _activeLoadContext.SetProfileOptimizationRoot(directoryPath);
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
            if (_useResolvingHandlerOnly)
                _activeLoadContext.StartProfileOptimization(profile);
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
        private static object[] s_emptyArray = new object[0];

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

            PowerShellAssemblyLoadContext.InitializeSingleton(basePaths, useResolvingHandlerOnly: true);
        }

        /// <summary>
        /// Create a singleton of PowerShellAssemblyLoadContext.
        /// Then load System.Management.Automation and call the WSManPluginManagedEntryWrapper delegate.
        /// </summary>
        /// <remarks>
        /// This method is used by the native host of the PSRP plugin.
        /// </remarks>
        /// <param name="wkrPtrs">
        /// Passed to delegate.
        /// </param>
        public static int WSManPluginWrapper(IntPtr wkrPtrs)
        {
            string basePaths = System.IO.Path.GetDirectoryName(typeof(PowerShellAssemblyLoadContextInitializer).GetTypeInfo().Assembly.Location);
            string entryAssemblyName = "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            string entryTypeName = "System.Management.Automation.Remoting.WSManPluginManagedEntryWrapper";
            string entryMethodName = "InitPlugin";
            object[] args = { wkrPtrs };

            var psLoadContext = PowerShellAssemblyLoadContext.InitializeSingleton(basePaths, useResolvingHandlerOnly: false);
            var entryAssembly = psLoadContext.LoadFromAssemblyName(new AssemblyName(entryAssemblyName));
            var entryType = entryAssembly.GetType(entryTypeName, throwOnError: true, ignoreCase: true);
            var methodInfo = entryType.GetMethod(entryMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);

            return (int)methodInfo.Invoke(null, args);
        }

        /// <summary>
        /// Create a singleton of PowerShellAssemblyLoadContext.
        /// Then load the assembly containing the actual entry point using it.
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'.
        /// They will be the default paths to probe assemblies.
        /// </param>
        /// <param name="entryAssemblyName">
        /// Name of the assembly that contains the actual entry point.
        /// </param>
        /// <returns>
        /// The assembly that contains the actual entry point.
        /// </returns>
        public static Assembly InitializeAndLoadEntryAssembly(string basePaths, AssemblyName entryAssemblyName)
        {
            if (string.IsNullOrEmpty(basePaths))
                throw new ArgumentNullException("basePaths");

            if (entryAssemblyName == null)
                throw new ArgumentNullException("entryAssemblyName");

            var psLoadContext = PowerShellAssemblyLoadContext.InitializeSingleton(basePaths, useResolvingHandlerOnly: false);
            return psLoadContext.LoadFromAssemblyName(entryAssemblyName);
        }

        /// <summary>
        /// Create a singleton of PowerShellAssemblyLoadContext.
        /// Then call into the actual entry point based on the given assembly name, type name, method name and arguments.
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'.
        /// They will be the default paths to probe assemblies.
        /// </param>
        /// <param name="entryAssemblyName">
        /// Name of the assembly that contains the actual entry point.
        /// </param>
        /// <param name="entryTypeName">
        /// Name of the type that contains the actual entry point.
        /// </param>
        /// <param name="entryMethodName">
        /// Name of the actual entry point method.
        /// </param>
        /// <param name="args">
        /// An array of arguments passed to the entry point method.
        /// </param>
        /// <returns>
        /// The return value of running the entry point method.
        /// </returns>
        public static object InitializeAndCallEntryMethod(string basePaths, AssemblyName entryAssemblyName, string entryTypeName, string entryMethodName, object[] args)
        {
            if (string.IsNullOrEmpty(basePaths))
                throw new ArgumentNullException("basePaths");

            if (entryAssemblyName == null)
                throw new ArgumentNullException("entryAssemblyName");

            if (string.IsNullOrEmpty(entryTypeName))
                throw new ArgumentNullException("entryTypeName");

            if (string.IsNullOrEmpty(entryMethodName))
                throw new ArgumentNullException("entryMethodName");

            args = args ?? s_emptyArray;

            var psLoadContext = PowerShellAssemblyLoadContext.InitializeSingleton(basePaths, useResolvingHandlerOnly: false);
            var entryAssembly = psLoadContext.LoadFromAssemblyName(entryAssemblyName);
            var entryType = entryAssembly.GetType(entryTypeName, throwOnError: true, ignoreCase: true);
            var methodInfo = entryType.GetMethod(entryMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);

            return methodInfo.Invoke(null, args);
        }
    }
}

#endif

