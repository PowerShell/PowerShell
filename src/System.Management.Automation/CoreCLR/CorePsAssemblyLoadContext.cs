// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using System.Text;
using System.Linq;

namespace System.Management.Automation
{
    /// <summary>
    /// The powershell custom AssemblyLoadContext implementation.
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
        private const string SingletonAlreadyInitialized = "The singleton of PowerShellAssemblyLoadContext has already been initialized.";

        #endregion Resource_Strings

        #region Constructor

        /// <summary>
        /// Initialize a singleton of PowerShellAssemblyLoadContext.
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
        /// Constructor.
        /// </summary>
        /// <param name="basePaths">
        /// Base directory paths that are separated by semicolon ';'. They will be the default paths to probe assemblies.
        /// The passed-in argument could be null or an empty string, in which case there is no default paths to probe assemblies.
        /// </param>
        private PowerShellAssemblyLoadContext(string basePaths)
        {
#if !UNIX
            // Set GAC related member variables to null
            _winDir = _gacPath32 = _gacPath64 = _gacPathMSIL = null;
#endif

            // FIRST: Validate and populate probing paths
            if (string.IsNullOrEmpty(basePaths))
            {
                _probingPaths = Array.Empty<string>();
            }
            else
            {
                _probingPaths = basePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < _probingPaths.Length; i++)
                {
                    string basePath = _probingPaths[i];
                    if (!Directory.Exists(basePath))
                    {
                        string message = string.Format(CultureInfo.CurrentCulture, BaseFolderDoesNotExist, basePath);
                        throw new ArgumentException(message, "basePaths");
                    }

                    _probingPaths[i] = basePath.Trim();
                }
            }

            // NEXT: Initialize the CoreCLR type catalog dictionary [OrdinalIgnoreCase]
            _coreClrTypeCatalog = InitializeTypeCatalog();
            _availableDotNetAssemblyNames = new Lazy<HashSet<string>>(
                    () => new HashSet<string>(_coreClrTypeCatalog.Values, StringComparer.Ordinal));

            // LAST: Register 'Resolving' handler on the default load context.
            AssemblyLoadContext.Default.Resolving += Resolve;
        }

        #endregion Constructor

        #region Fields

        private static readonly object s_syncObj = new object();
        private readonly string[] _probingPaths;
        private readonly string[] _extensions = new string[] { ".ni.dll", ".dll" };
        // CoreCLR type catalog dictionary
        //  - Key: namespace qualified type name (FullName)
        //  - Value: strong name of the TPA that contains the type represented by Key.
        private readonly Dictionary<string, string> _coreClrTypeCatalog;
        private readonly Lazy<HashSet<string>> _availableDotNetAssemblyNames;
        private readonly HashSet<string> _blackListedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase){
                "System.Windows.Forms"
            };

#if !UNIX
        private string _winDir;
        private string _gacPathMSIL;
        private string _gacPath32;
        private string _gacPath64;
#endif

        /// <summary>
        /// Assembly cache across the AppDomain.
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
        /// Singleton instance of PowerShellAssemblyLoadContext.
        /// </summary>
        internal static PowerShellAssemblyLoadContext Instance
        {
            get; private set;
        }

        /// <summary>
        /// Get the namespace-qualified type names of all available .NET Core types shipped with PowerShell Core.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal IEnumerable<string> AvailableDotNetTypeNames
        {
            get { return _coreClrTypeCatalog.Keys; }
        }

        /// <summary>
        /// Get the assembly names of all available .NET Core assemblies shipped with PowerShell Core.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal HashSet<string> AvailableDotNetAssemblyNames
        {
            get { return _availableDotNetAssemblyNames.Value; }
        }

        #endregion Properties

        #region Internal_Methods

        /// <summary>
        /// Get the current loaded assemblies.
        /// </summary>
        internal IEnumerable<Assembly> GetAssembly(string namespaceQualifiedTypeName)
        {
            // If 'namespaceQualifiedTypeName' is specified and it's a CoreCLR framework type,
            // then we only return that specific TPA assembly.
            if (!string.IsNullOrEmpty(namespaceQualifiedTypeName))
            {
                if (_coreClrTypeCatalog.TryGetValue(namespaceQualifiedTypeName, out string tpaStrongName))
                {
                    try
                    {
                        return new Assembly[] { GetTrustedPlatformAssembly(tpaStrongName) };
                    }
                    catch (FileNotFoundException) { }
                }
            }

            // Otherwise, we return null
            return null;
        }

        /// <summary>
        /// Set the profile optimization root on the appropriate load context.
        /// </summary>
        internal void SetProfileOptimizationRootImpl(string directoryPath)
        {
            AssemblyLoadContext.Default.SetProfileOptimizationRoot(directoryPath);
        }

        /// <summary>
        /// Start the profile optimization on the appropriate load context.
        /// </summary>
        internal void StartProfileOptimizationImpl(string profile)
        {
            AssemblyLoadContext.Default.StartProfileOptimization(profile);
        }

        #endregion Internal_Methods

        #region Private_Methods

        /// <summary>
        /// The handler for the Resolving event.
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

                for (int i = 0; i < _probingPaths.Length; i++)
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
#if !UNIX
                    // Try loading from GAC
                    if (!TryFindInGAC(assemblyName, out asmFilePath))
                    {
                         return null;
                    }
#else
                    return null;
#endif
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

            return asmLoaded;
        }

#if !UNIX
        // Try to find the assembly in GAC by looking up the directories in well know locations.
        // First try to find in GAC_MSIL, then depending on process bitness; GAC_64 or GAC32.
        // If there are multiple version of the assembly, load the latest.
        private bool TryFindInGAC(AssemblyName assemblyName, out string assemblyFilePath)
        {
            assemblyFilePath = null;
            if (_blackListedAssemblies.Contains(assemblyName.Name))
            {
                // DotNet catches and throws a new exception with no inner exception
                // We cannot change the message DotNet returns.
                return false;
            }

            if (Internal.InternalTestHooks.DisableGACLoading)
            {
                return false;
            }

            bool assemblyFound = false;
            char dirSeparator = IO.Path.DirectorySeparatorChar;

            if (string.IsNullOrEmpty(_winDir))
            {
                // cache value of '_winDir' folder in member variable.
                _winDir = Environment.GetEnvironmentVariable("winDir");
            }

            if (string.IsNullOrEmpty(_gacPathMSIL))
            {
                // cache value of '_gacPathMSIL' folder in member variable.
                _gacPathMSIL = $"{_winDir}{dirSeparator}Microsoft.NET{dirSeparator}assembly{dirSeparator}GAC_MSIL";
            }

            assemblyFound = FindInGac(_gacPathMSIL, assemblyName, out assemblyFilePath);

            if (!assemblyFound)
            {
                string gacBitnessAwarePath = null;

                if (Environment.Is64BitProcess)
                {
                    if (string.IsNullOrEmpty(_gacPath64))
                    {
                        // cache value of '_gacPath64' folder in member variable.
                        _gacPath64 = $"{_winDir}{dirSeparator}Microsoft.NET{dirSeparator}assembly{dirSeparator}GAC_64";
                    }

                    gacBitnessAwarePath = _gacPath64;
                }
                else
                {
                    if (string.IsNullOrEmpty(_gacPath32))
                    {
                        // cache value of '_gacPath32' folder in member variable.
                        _gacPath32 = $"{_winDir}{dirSeparator}Microsoft.NET{dirSeparator}assembly{dirSeparator}GAC_32";
                    }

                    gacBitnessAwarePath = _gacPath32;
                }

                assemblyFound = FindInGac(gacBitnessAwarePath, assemblyName, out assemblyFilePath);
            }

            return assemblyFound;
        }

        // Find the assembly under 'gacRoot' and select the latest version.
        private bool FindInGac(string gacRoot, AssemblyName assemblyName, out string assemblyPath)
        {
            bool assemblyFound = false;
            assemblyPath = null;

            char dirSeparator = IO.Path.DirectorySeparatorChar;
            string tempAssemblyDirPath = $"{gacRoot}{dirSeparator}{assemblyName.Name}";

            if (Directory.Exists(tempAssemblyDirPath))
            {
                // Enumerate all directories, sort by name and select the last. This selects the latest version.
                var chosenVersionDirectory = Directory.GetDirectories(tempAssemblyDirPath).OrderBy(d => d).LastOrDefault();

                if (!string.IsNullOrEmpty(chosenVersionDirectory))
                {
                    // Select first or default as the directory will contain only one assembly. If nothing then default is null;
                    var foundAssemblyPath = Directory.GetFiles(chosenVersionDirectory, $"{assemblyName.Name}*").FirstOrDefault();

                    if (!string.IsNullOrEmpty(foundAssemblyPath))
                    {
                        AssemblyName asmNameFound = AssemblyLoadContext.GetAssemblyName(foundAssemblyPath);
                        if (IsAssemblyMatching(assemblyName, asmNameFound))
                        {
                            assemblyPath = foundAssemblyPath;
                            assemblyFound = true;
                        }
                    }
                }
            }

            return assemblyFound;
        }
#endif

        /// <summary>
        /// Try to get the specified assembly from cache.
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
        /// Check if the loaded assembly matches the request.
        /// </summary>
        /// <param name="requestedAssembly">AssemblyName of the requested assembly.</param>
        /// <param name="loadedAssembly">AssemblyName of the loaded assembly.</param>
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
        /// Throw FileLoadException.
        /// </summary>
        private void ThrowFileLoadException(string errorTemplate, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, errorTemplate, args);
            throw new FileLoadException(message);
        }

        /// <summary>
        /// Throw FileNotFoundException.
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
        /// in-box Nano powershell, the PS remote WinRM plugin, in-box Nano DSC and in-box Nano SCOM agent.
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

