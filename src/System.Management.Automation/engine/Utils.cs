// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

using TypeTable = System.Management.Automation.Runspaces.TypeTable;

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Management.Automation
{
    /// <summary>
    /// helper fns
    /// </summary>
    internal static class Utils
    {
        // From System.Web.Util.HashCodeCombiner
        internal static int CombineHashCodes(int h1, int h2)
        {
            return unchecked(((h1 << 5) + h1) ^ h2);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), h3);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), CombineHashCodes(h3, h4));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), h5);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7, h8));
        }

        /// <summary>
        /// Allowed PowerShell Editions
        /// </summary>
        internal static string[] AllowedEditionValues = { "Desktop", "Core" };

        /// <summary>
        /// helper fn to check byte[] arg for null.
        /// </summary>
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///<returns> Does not return a value </returns>
        internal static void CheckKeyArg(byte[] arg, string argName)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(argName);
            }
            //
            // we use AES algorithm which supports key
            // lengths of 128, 192 and 256 bits.
            // We throw ArgumentException if the key is
            // of any other length
            //
            else if (!((arg.Length == 16) ||
                       (arg.Length == 24) ||
                       (arg.Length == 32)))
            {
                throw PSTraceSource.NewArgumentException(argName, Serialization.InvalidKeyLength, argName);
            }
        }

        /// <summary>
        /// helper fn to check arg for empty or null.
        /// Throws ArgumentNullException on either condition.
        /// </summary>
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///<returns> Does not return a value </returns>
        internal static void CheckArgForNullOrEmpty(string arg, string argName)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(argName);
            }
            else if (arg.Length == 0)
            {
                throw PSTraceSource.NewArgumentException(argName);
            }
        }

        /// <summary>
        /// helper fn to check arg for null.
        /// Throws ArgumentNullException on either condition.
        /// </summary>
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///<returns> Does not return a value </returns>
        internal static void CheckArgForNull(object arg, string argName)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(argName);
            }
        }

        /// <summary>
        /// helper fn to check arg for null.
        /// </summary>
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///<returns> Does not return a value </returns>
        internal static void CheckSecureStringArg(SecureString arg, string argName)
        {
            if (arg == null)
            {
                throw PSTraceSource.NewArgumentNullException(argName);
            }
        }

        [ArchitectureSensitive]
        internal static string GetStringFromSecureString(SecureString ss)
        {
            IntPtr p = IntPtr.Zero;
            string s = null;

            try
            {
                p = Marshal.SecureStringToCoTaskMemUnicode(ss);
                s = Marshal.PtrToStringUni(p);
            }
            finally
            {
                if (p != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(p);
                }
            }

            return s;
        }

        /// <summary>
        /// Gets TypeTable by querying the ExecutionContext stored in
        /// Thread-Local-Storage. This will return null if ExecutionContext
        /// is not available.
        /// </summary>
        /// <returns></returns>
        internal static TypeTable GetTypeTableFromExecutionContextTLS()
        {
            ExecutionContext ecFromTLS = Runspaces.LocalPipeline.GetExecutionContextFromTLS();
            if (ecFromTLS == null)
            {
                return null;
            }

            return ecFromTLS.TypeTable;
        }

#if !UNIX
        private static string s_pshome = null;

        /// <summary>
        /// Get the application base path of the shell from registry
        /// </summary>
        internal static string GetApplicationBaseFromRegistry(string shellId)
        {
            bool wantPsHome = (object)shellId == (object)DefaultPowerShellShellID;
            if (wantPsHome && s_pshome != null)
                return s_pshome;

            string engineKeyPath = RegistryStrings.MonadRootKeyPath + "\\" +
                PSVersionInfo.RegistryVersionKey + "\\" + RegistryStrings.MonadEngineKey;

            using (RegistryKey engineKey = Registry.LocalMachine.OpenSubKey(engineKeyPath))
            {
                if (engineKey != null)
                {
                    var result = engineKey.GetValue(RegistryStrings.MonadEngine_ApplicationBase) as string;
                    result = Environment.ExpandEnvironmentVariables(result);
                    if (wantPsHome)
                        Interlocked.CompareExchange(ref s_pshome, null, result);

                    return result;
                }
            }

            return null;
        }
#endif

        internal static string DefaultPowerShellAppBase => GetApplicationBase(DefaultPowerShellShellID);
        internal static string GetApplicationBase(string shellId)
        {
            // Use the location of SMA.dll as the application base.
            Assembly assembly = typeof(PSObject).Assembly;
            return Path.GetDirectoryName(assembly.Location);
        }

        private static string[] s_productFolderDirectories;

        /// <summary>
        /// Specifies the per-user configuration settings directory in a platform agnostic manner.
        /// </summary>
        /// <returns>The current user's configuration settings directory</returns>
        internal static string GetUserConfigurationDirectory()
        {
#if UNIX
            return Platform.SelectProductNameForDirectory(Platform.XDG_Type.CONFIG);
#else
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return IO.Path.Combine(basePath, Utils.ProductNameForDirectory);
#endif
        }

        private static string[] GetProductFolderDirectories()
        {
            if (s_productFolderDirectories == null)
            {
                List<string> baseDirectories = new List<string>();

                // Retrieve the application base from the registry
                string appBase = Utils.DefaultPowerShellAppBase;
                if (!string.IsNullOrEmpty(appBase))
                {
                    baseDirectories.Add(appBase);
                }
#if !UNIX
                // Win8: 454976
                // Now add the two variations of System32
                baseDirectories.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
                string systemX86 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                if (!string.IsNullOrEmpty(systemX86))
                {
                    baseDirectories.Add(systemX86);
                }
#endif
                // And built-in modules
                string progFileDir;
                // TODO: #1184 will resolve this work-around
                // Side-by-side versions of PowerShell use modules from their application base, not
                // the system installation path.
                progFileDir = Path.Combine(appBase, "Modules");

                if (!string.IsNullOrEmpty(progFileDir))
                {
                    baseDirectories.Add(Path.Combine(progFileDir, "PackageManagement"));
                    baseDirectories.Add(Path.Combine(progFileDir, "PowerShellGet"));
                    baseDirectories.Add(Path.Combine(progFileDir, "Pester"));
                    baseDirectories.Add(Path.Combine(progFileDir, "PSReadLine"));
                }
                Interlocked.CompareExchange(ref s_productFolderDirectories, baseDirectories.ToArray(), null);
            }

            return s_productFolderDirectories;
        }

        /// <summary>
        /// Checks if the filePath represents a file under product folder
        /// ie., PowerShell ApplicationBase or $env:windir\system32 or
        /// $env:windir\syswow64.
        /// </summary>
        /// <returns>
        /// true: if the filePath is under product folder
        /// false: otherwise
        /// </returns>
        internal static bool IsUnderProductFolder(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            string filename = fileInfo.FullName;

            var productFolderDirectories = GetProductFolderDirectories();
            for (int i = 0; i < productFolderDirectories.Length; i++)
            {
                string applicationBase = productFolderDirectories[i];
                if (filename.StartsWith(applicationBase, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the current process is using WOW
        /// </summary>
        internal static bool IsRunningFromSysWOW64()
        {
            return DefaultPowerShellAppBase.Contains("SysWOW64");
        }

        /// <summary>
        /// Checks if host machine is WinPE
        /// </summary>
        internal static bool IsWinPEHost()
        {
#if !UNIX
            RegistryKey winPEKey = null;

            try
            {
                // The existence of the following registry confirms that the host machine is a WinPE
                // HKLM\System\CurrentControlSet\Control\MiniNT
                winPEKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\MiniNT");

                return winPEKey != null;
            }
            catch (ArgumentException) { }
            catch (SecurityException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                if (winPEKey != null)
                {
                    winPEKey.Dispose();
                }
            }
#endif
            return false;
        }

        #region Versioning related methods

        /// <summary>
        /// returns current major version of monad ( that is running ) in a string
        /// format.
        /// </summary>
        /// <returns>string</returns>
        /// <remarks>
        /// Cannot return a Version object as minor number is a requirement for
        /// version object.
        /// </remarks>
        internal static string GetCurrentMajorVersion()
        {
            return PSVersionInfo.PSVersion.Major.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Coverts a string to version format.
        /// If the string is of the format x (ie., no dots), then ".0" is appended
        /// to the string.
        /// Version.TryParse will be used to convert the string to a Version
        /// object.
        /// </summary>
        /// <param name="versionString">string representing version</param>
        /// <returns>A Version Object.</returns>
        internal static Version StringToVersion(string versionString)
        {
            // max of 1 dot is allowed in version
            if (string.IsNullOrEmpty(versionString))
            {
                return null;
            }

            int dotCount = 0;
            foreach (char c in versionString)
            {
                if (c == '.')
                {
                    dotCount++;
                    if (dotCount > 1)
                    {
                        break;
                    }
                }
            }
            // Version.TryParse expects the string to be in format: major.minor[.build[.revision]]
            if (dotCount == 0)
            {
                versionString += ".0";
            }

            Version result = null;
            if (Version.TryParse(versionString, out result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Checks whether current monad session supports version specified
        /// by ver.
        /// </summary>
        /// <param name="ver">Version to check</param>
        /// <returns>true if supported, false otherwise</returns>
        internal static bool IsPSVersionSupported(string ver)
        {
            // Convert version to supported format ie., x.x
            Version inputVersion = StringToVersion(ver);
            return IsPSVersionSupported(inputVersion);
        }

        /// <summary>
        /// Checks whether current monad session supports version specified
        /// by checkVersion.
        /// </summary>
        /// <param name="checkVersion">Version to check</param>
        /// <returns>true if supported, false otherwise</returns>
        internal static bool IsPSVersionSupported(Version checkVersion)
        {
            if (checkVersion == null)
            {
                return false;
            }

            foreach (Version compatibleVersion in PSVersionInfo.PSCompatibleVersions)
            {
                if (checkVersion.Major == compatibleVersion.Major && checkVersion.Minor <= compatibleVersion.Minor)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether current PowerShell session supports edition specified
        /// by checkEdition.
        /// </summary>
        /// <param name="checkEdition">Edition to check</param>
        /// <returns>true if supported, false otherwise</returns>
        internal static bool IsPSEditionSupported(string checkEdition)
        {
            return PSVersionInfo.PSEdition.Equals(checkEdition, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check whether the current PowerShell session supports any of the specified editions.
        /// </summary>
        /// <param name="editions">The PowerShell editions to check compatibility with.</param>
        /// <returns>True if the edition is supported by this runtime, false otherwise.</returns>
        internal static bool IsPSEditionSupported(IEnumerable<string> editions)
        {
            string currentPSEdition = PSVersionInfo.PSEdition;
            foreach (string edition in editions)
            {
                if (currentPSEdition.Equals(edition, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the specified edition value is allowed.
        /// </summary>
        /// <param name="editionValue">Edition value to check</param>
        /// <returns>true if allowed, false otherwise</returns>
        internal static bool IsValidPSEditionValue(string editionValue)
        {
            return AllowedEditionValues.Contains(editionValue, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// String representing the Default shellID.
        /// </summary>
        internal const string DefaultPowerShellShellID = "Microsoft.PowerShell";

        /// <summary>
        /// This is used to construct the profile path.
        /// </summary>
        internal static string ProductNameForDirectory = Platform.IsInbox ? "WindowsPowerShell" : "PowerShell";

        /// <summary>
        /// The subdirectory of module paths
        /// e.g. ~\Documents\WindowsPowerShell\Modules and %ProgramFiles%\WindowsPowerShell\Modules
        /// </summary>
        internal static string ModuleDirectory = Path.Combine(ProductNameForDirectory, "Modules");

        internal readonly static ConfigScope[] SystemWideOnlyConfig = new[] { ConfigScope.SystemWide };
        internal readonly static ConfigScope[] CurrentUserOnlyConfig = new[] { ConfigScope.CurrentUser };
        internal readonly static ConfigScope[] SystemWideThenCurrentUserConfig = new[] { ConfigScope.SystemWide, ConfigScope.CurrentUser };
        internal readonly static ConfigScope[] CurrentUserThenSystemWideConfig = new[] { ConfigScope.CurrentUser, ConfigScope.SystemWide };

        internal static T GetPolicySetting<T>(ConfigScope[] preferenceOrder) where T : PolicyBase, new()
        {
            T policy = null;
#if !UNIX
            // On Windows, group policy settings from registry take precedence.
            // If the requested policy is not defined in registry, we query the configuration file.
            policy = GetPolicySettingFromGPO<T>(preferenceOrder);
            if (policy != null) { return policy; }
#endif
            policy = GetPolicySettingFromConfigFile<T>(preferenceOrder);
            return policy;
        }

        private readonly static ConcurrentDictionary<ConfigScope, PowerShellPolicies> s_cachedPoliciesFromConfigFile =
            new ConcurrentDictionary<ConfigScope, PowerShellPolicies>();

        /// <summary>
        /// Get a specific kind of policy setting from the configuration file.
        /// </summary>
        private static T GetPolicySettingFromConfigFile<T>(ConfigScope[] preferenceOrder) where T : PolicyBase, new()
        {
            foreach (ConfigScope scope in preferenceOrder)
            {
                PowerShellPolicies policies;
                if (InternalTestHooks.BypassGroupPolicyCaching)
                {
                    policies = PowerShellConfig.Instance.GetPowerShellPolicies(scope);
                }
                else if (!s_cachedPoliciesFromConfigFile.TryGetValue(scope, out policies))
                {
                    // Use lock here to reduce the contention on accessing the configuration file
                    lock (s_cachedPoliciesFromConfigFile)
                    {
                        policies = s_cachedPoliciesFromConfigFile.GetOrAdd(scope, PowerShellConfig.Instance.GetPowerShellPolicies);
                    }
                }

                if (policies != null)
                {
                    PolicyBase result = null;
                    switch (typeof(T).Name)
                    {
                        case nameof(ScriptExecution):             result = policies.ScriptExecution; break;
                        case nameof(ScriptBlockLogging):          result = policies.ScriptBlockLogging; break;
                        case nameof(ModuleLogging):               result = policies.ModuleLogging; break;
                        case nameof(ProtectedEventLogging):       result = policies.ProtectedEventLogging; break;
                        case nameof(Transcription):               result = policies.Transcription; break;
                        case nameof(UpdatableHelp):               result = policies.UpdatableHelp; break;
                        case nameof(ConsoleSessionConfiguration): result = policies.ConsoleSessionConfiguration; break;
                        default: Diagnostics.Assert(false, "Should be unreachable code. Update this switch block when new PowerShell policy types are added."); break;
                    }
                    if (result != null) { return (T) result; }
                }
            }

            return null;
        }

#if !UNIX
        private static readonly Dictionary<string, string> GroupPolicyKeys = new Dictionary<string, string>
        {
            {nameof(ScriptExecution), @"Software\Policies\Microsoft\PowerShellCore"},
            {nameof(ScriptBlockLogging), @"Software\Policies\Microsoft\PowerShellCore\ScriptBlockLogging"},
            {nameof(ModuleLogging), @"Software\Policies\Microsoft\PowerShellCore\ModuleLogging"},
            {nameof(ProtectedEventLogging), @"Software\Policies\Microsoft\Windows\EventLog\ProtectedEventLogging"},
            {nameof(Transcription), @"Software\Policies\Microsoft\PowerShellCore\Transcription"},
            {nameof(UpdatableHelp), @"Software\Policies\Microsoft\PowerShellCore\UpdatableHelp"},
            {nameof(ConsoleSessionConfiguration), @"Software\Policies\Microsoft\PowerShellCore\ConsoleSessionConfiguration"}
        };
        private readonly static ConcurrentDictionary<Tuple<ConfigScope, string>, PolicyBase> s_cachedPoliciesFromRegistry =
            new ConcurrentDictionary<Tuple<ConfigScope, string>, PolicyBase>();

        /// <summary>
        /// The implementation of fetching a specific kind of policy setting from the given configuration scope.
        /// </summary>
        private static T GetPolicySettingFromGPOImpl<T>(ConfigScope scope) where T : PolicyBase, new()
        {
            Type tType = typeof(T);
            // SystemWide scope means 'LocalMachine' root key when query from registry
            RegistryKey rootKey = (scope == ConfigScope.SystemWide) ? Registry.LocalMachine : Registry.CurrentUser;

            GroupPolicyKeys.TryGetValue(tType.Name, out string gpoKeyPath);
            Diagnostics.Assert(gpoKeyPath != null, StringUtil.Format("The GPO registry key path should be pre-defined for {0}", tType.Name));

            using (RegistryKey gpoKey = rootKey.OpenSubKey(gpoKeyPath))
            {
                // If the corresponding GPO key doesn't exist, return null
                if (gpoKey == null) { return null; }

                // The corresponding GPO key exists, then create an instance of T
                // and populate its properties with the settings
                object tInstance = Activator.CreateInstance(tType, nonPublic: true);
                var properties = tType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                bool isAnyPropertySet = false;

                string[] valueNames = gpoKey.GetValueNames();
                string[] subKeyNames = gpoKey.GetSubKeyNames();
                var valueNameSet = valueNames.Length > 0 ? new HashSet<string>(valueNames, StringComparer.OrdinalIgnoreCase) : null;
                var subKeyNameSet = subKeyNames.Length > 0 ? new HashSet<string>(subKeyNames, StringComparer.OrdinalIgnoreCase) : null;

                foreach (var property in properties)
                {
                    string settingName = property.Name;
                    object rawRegistryValue = null;

                    // Get the raw value from registry.
                    if (valueNameSet != null && valueNameSet.Contains(settingName))
                    {
                        rawRegistryValue = gpoKey.GetValue(settingName);
                    }
                    else if (subKeyNameSet != null && subKeyNameSet.Contains(settingName))
                    {
                        using (RegistryKey subKey = gpoKey.OpenSubKey(settingName))
                        {
                            if (subKey != null) { rawRegistryValue = subKey.GetValueNames(); }
                        }
                    }

                    // Get the actual property value based on the property type.
                    // If the final property value is not null, then set the property.
                    if (rawRegistryValue != null)
                    {
                        Type propertyType = property.PropertyType;
                        object propertyValue = null;

                        switch (propertyType)
                        {
                            case var _ when propertyType == typeof(bool?):
                                if (rawRegistryValue is int rawIntValue)
                                {
                                    if (rawIntValue == 1) { propertyValue = true; }
                                    else if (rawIntValue == 0) { propertyValue = false; }
                                }
                                break;
                            case var _ when propertyType == typeof(string):
                                if (rawRegistryValue is string rawStringValue)
                                {
                                    propertyValue = rawStringValue;
                                }
                                break;
                            case var _ when propertyType == typeof(string[]):
                                if (rawRegistryValue is string[] rawStringArrayValue)
                                {
                                    propertyValue = rawStringArrayValue;
                                }
                                else if (rawRegistryValue is string stringValue)
                                {
                                    propertyValue = new string[] { stringValue };
                                }
                                break;
                            default:
                                Diagnostics.Assert(false, "Should be unreachable code. Update this switch block when properties of new types are added to PowerShell policy types.");
                                break;
                        }

                        // Set the property if the value is not null
                        if (propertyValue != null)
                        {
                            property.SetValue(tInstance, propertyValue);
                            isAnyPropertySet = true;
                        }
                    }
                }

                // If no property is set, then we consider this policy as undefined
                return isAnyPropertySet ? (T) tInstance : null;
            }
        }

        /// <summary>
        /// Get a specific kind of policy setting from the group policy registry key.
        /// </summary>
        private static T GetPolicySettingFromGPO<T>(ConfigScope[] preferenceOrder) where T : PolicyBase, new()
        {
            PolicyBase policy = null;
            foreach (ConfigScope scope in preferenceOrder)
            {
                if (InternalTestHooks.BypassGroupPolicyCaching)
                {
                    policy = GetPolicySettingFromGPOImpl<T>(scope);
                }
                else
                {
                    var key = Tuple.Create(scope, typeof(T).Name);
                    if (!s_cachedPoliciesFromRegistry.TryGetValue(key, out policy))
                    {
                        lock (s_cachedPoliciesFromRegistry)
                        {
                            policy = s_cachedPoliciesFromRegistry.GetOrAdd(key, tuple => GetPolicySettingFromGPOImpl<T>(tuple.Item1));
                        }
                    }
                }

                if (policy != null) { return (T) policy; }
            }

            return null;
        }
#endif

        /// <summary>
        /// Scheduled job module name.
        /// </summary>
        internal const string ScheduledJobModuleName = "PSScheduledJob";

        internal static void EnsureModuleLoaded(string module, ExecutionContext context)
        {
            if (context != null && !context.AutoLoadingModuleInProgress.Contains(module))
            {
                List<PSModuleInfo> loadedModules = context.Modules.GetModules(new string[] { module }, false);

                if ((loadedModules == null) || (loadedModules.Count == 0))
                {
                    CommandInfo commandInfo = new CmdletInfo("Import-Module", typeof(Microsoft.PowerShell.Commands.ImportModuleCommand),
                                                             null, null, context);
                    var importModuleCommand = new System.Management.Automation.Runspaces.Command(commandInfo);

                    context.AutoLoadingModuleInProgress.Add(module);

                    PowerShell ps = null;

                    try
                    {
                        ps = PowerShell.Create(RunspaceMode.CurrentRunspace)
                            .AddCommand(importModuleCommand)
                            .AddParameter("Name", module)
                            .AddParameter("Scope", StringLiterals.Global)
                            .AddParameter("ErrorAction", ActionPreference.Ignore)
                            .AddParameter("WarningAction", ActionPreference.Ignore)
                            .AddParameter("InformationAction", ActionPreference.Ignore)
                            .AddParameter("Verbose", false)
                            .AddParameter("Debug", false)
                            .AddParameter("PassThru");

                        ps.Invoke<PSModuleInfo>();
                    }
                    catch (Exception)
                    {
                        // Call-out to user code, catch-all OK
                    }
                    finally
                    {
                        context.AutoLoadingModuleInProgress.Remove(module);
                        if (ps != null)
                        {
                            ps.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns modules (either loaded or in available) that match pattern <paramref name="module"/>.
        /// Uses Get-Module -ListAvailable cmdlet.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="context"></param>
        /// <returns>
        /// List of PSModuleInfo's or Null.
        /// </returns>
        internal static List<PSModuleInfo> GetModules(string module, ExecutionContext context)
        {
            // first look in the loaded modules and then append the modules from gmo -Listavailable
            // Reason: gmo -li looks only the PSModulepath. There may be cases where a module
            // is imported directly from a path (that is not in PSModulePath).
            List<PSModuleInfo> result = context.Modules.GetModules(new string[] { module }, false);

            CommandInfo commandInfo = new CmdletInfo("Get-Module", typeof(Microsoft.PowerShell.Commands.GetModuleCommand),
                                                     null, null, context);
            var getModuleCommand = new System.Management.Automation.Runspaces.Command(commandInfo);

            PowerShell ps = null;
            try
            {
                ps = PowerShell.Create(RunspaceMode.CurrentRunspace)
                        .AddCommand(getModuleCommand)
                        .AddParameter("Name", module)
                        .AddParameter("ErrorAction", ActionPreference.Ignore)
                        .AddParameter("WarningAction", ActionPreference.Ignore)
                        .AddParameter("Verbose", false)
                        .AddParameter("Debug", false)
                        .AddParameter("ListAvailable");

                Collection<PSModuleInfo> gmoOutPut = ps.Invoke<PSModuleInfo>();
                if (gmoOutPut != null)
                {
                    if (result == null)
                    {
                        result = gmoOutPut.ToList<PSModuleInfo>();
                    }
                    else
                    {
                        // append to result
                        foreach (PSModuleInfo temp in gmoOutPut)
                        {
                            result.Add(temp);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Call-out to user code, catch-all OK
            }
            finally
            {
                if (ps != null)
                {
                    ps.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Returns modules (either loaded or in available) that match FullyQualifiedName <paramref name="fullyQualifiedName"/>.
        /// Uses Get-Module -ListAvailable cmdlet.
        /// </summary>
        /// <param name="fullyQualifiedName"></param>
        /// <param name="context"></param>
        /// <returns>
        /// List of PSModuleInfo's or Null.
        /// </returns>
        internal static List<PSModuleInfo> GetModules(ModuleSpecification fullyQualifiedName, ExecutionContext context)
        {
            // first look in the loaded modules and then append the modules from gmo -Listavailable
            // Reason: gmo -li looks only the PSModulepath. There may be cases where a module
            // is imported directly from a path (that is not in PSModulePath).
            List<PSModuleInfo> result = context.Modules.GetModules(new[] { fullyQualifiedName }, false);
            CommandInfo commandInfo = new CmdletInfo("Get-Module", typeof(GetModuleCommand),
                                                     null, null, context);
            var getModuleCommand = new Runspaces.Command(commandInfo);

            PowerShell ps = null;
            try
            {
                ps = PowerShell.Create(RunspaceMode.CurrentRunspace)
                        .AddCommand(getModuleCommand)
                        .AddParameter("FullyQualifiedName", fullyQualifiedName)
                        .AddParameter("ErrorAction", ActionPreference.Ignore)
                        .AddParameter("WarningAction", ActionPreference.Ignore)
                        .AddParameter("InformationAction", ActionPreference.Ignore)
                        .AddParameter("Verbose", false)
                        .AddParameter("Debug", false)
                        .AddParameter("ListAvailable");

                Collection<PSModuleInfo> gmoOutput = ps.Invoke<PSModuleInfo>();
                if (gmoOutput != null)
                {
                    if (result == null)
                    {
                        result = gmoOutput.ToList();
                    }
                    else
                    {
                        // append to result
                        result.AddRange(gmoOutput);
                    }
                }
            }
            catch (Exception)
            {
                // Call-out to user code, catch-all OK
            }
            finally
            {
                if (ps != null)
                {
                    ps.Dispose();
                }
            }

            return result;
        }

        internal static bool IsAdministrator()
        {
            // Porting note: only Windows supports the SecurityPrincipal API of .NET. Due to
            // advanced privilege models, the correct approach on Unix is to assume the user has
            // permissions, attempt the task, and error gracefully if the task fails due to
            // permissions. To fit into PowerShell's existing model of preemptively checking
            // permissions (which cannot be assumed on Unix), we "assume" the user is an
            // administrator by returning true, thus nullifying this check on Unix.
#if UNIX
            return true;
#else
            System.Security.Principal.WindowsIdentity currentIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(currentIdentity);

            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#endif
        }

        internal static void NativeEnumerateDirectory(string directory, out List<string> directories, out List<string> files)
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            NativeMethods.WIN32_FIND_DATA findData;

            files = new List<string>();
            directories = new List<string>();

            IntPtr findHandle;

            findHandle = NativeMethods.FindFirstFile(directory + "\\*", out findData);
            if (findHandle != INVALID_HANDLE_VALUE)
            {
                do
                {
                    if ((findData.dwFileAttributes & NativeMethods.FileAttributes.Directory) != 0)
                    {
                        if ((!String.Equals(".", findData.cFileName, StringComparison.OrdinalIgnoreCase)) &&
                            (!String.Equals("..", findData.cFileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            directories.Add(directory + "\\" + findData.cFileName);
                        }
                    }
                    else
                    {
                        files.Add(directory + "\\" + findData.cFileName);
                    }
                }
                while (NativeMethods.FindNextFile(findHandle, out findData));
                NativeMethods.FindClose(findHandle);
            }
        }

        internal static bool IsReservedDeviceName(string destinationPath)
        {
#if !UNIX
            string[] reservedDeviceNames = { "CON", "PRN", "AUX", "CLOCK$", "NUL",
                                             "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                                             "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            string compareName = Path.GetFileName(destinationPath);
            string noExtensionCompareName = Path.GetFileNameWithoutExtension(destinationPath);

            // See if it's the correct length. If it's shorter than CON, AUX, etc, it can't be a device name.
            // Likewise, if it's longer than 'CLOCK$', it can't be a device name.
            if (((compareName.Length < 3) || (compareName.Length > 6)) &&
                ((noExtensionCompareName.Length < 3) || (noExtensionCompareName.Length > 6)))
            {
                return false;
            }

            foreach (string deviceName in reservedDeviceNames)
            {
                if (
                    String.Equals(deviceName, compareName, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(deviceName, noExtensionCompareName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
#endif
            return false;
        }

        internal static bool PathIsUnc(string path)
        {
#if UNIX
            return false;
#else
            Uri uri;
            return !string.IsNullOrEmpty(path) && Uri.TryCreate(path, UriKind.Absolute, out uri) && uri.IsUnc;
#endif
        }

        internal class NativeMethods
        {
            private static string EnsureLongPathPrefixIfNeeded(string path)
            {
                if (path.Length >= MAX_PATH && !path.StartsWith(@"\\?\", StringComparison.Ordinal))
                    return @"\\?\" + path;

                return path;
            }

            [DllImport(PinvokeDllNames.GetFileAttributesDllName, EntryPoint = "GetFileAttributesW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern int GetFileAttributesPrivate(string lpFileName);

            internal static int GetFileAttributes(string fileName)
            {
                fileName = EnsureLongPathPrefixIfNeeded(fileName);
                return GetFileAttributesPrivate(fileName);
            }

            [Flags]
            internal enum FileAttributes
            {
                Hidden = 0x0002,
                Directory = 0x0010
            }

            public const int MAX_PATH = 260;
            public const int MAX_ALTERNATE = 14;

            [StructLayout(LayoutKind.Sequential)]
            public struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct WIN32_FIND_DATA
            {
                public FileAttributes dwFileAttributes;
                public FILETIME ftCreationTime;
                public FILETIME ftLastAccessTime;
                public FILETIME ftLastWriteTime;
                public uint nFileSizeHigh; //changed all to uint, otherwise you run into unexpected overflow
                public uint nFileSizeLow;  //|
                public uint dwReserved0;   //|
                public uint dwReserved1;   //v
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                public string cFileName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
                public string cAlternate;
            }

            [DllImport(PinvokeDllNames.FindFirstFileDllName, CharSet = CharSet.Unicode)]
            public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

            [DllImport(PinvokeDllNames.FindNextFileDllName, CharSet = CharSet.Unicode)]
            public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

            [DllImport(PinvokeDllNames.FindCloseDllName, CharSet = CharSet.Unicode)]
            public static extern bool FindClose(IntPtr hFindFile);
        }

        internal static readonly string PowerShellAssemblyStrongNameFormat =
            "{0}, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

        internal static readonly HashSet<string> PowerShellAssemblies =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "microsoft.powershell.commands.diagnostics",
                    "microsoft.powershell.commands.management",
                    "microsoft.powershell.commands.utility",
                    "microsoft.powershell.consolehost",
                    "microsoft.powershell.scheduledjob",
                    "microsoft.powershell.security",
                    "microsoft.wsman.management",
                    "microsoft.wsman.runtime",
                    "system.management.automation"
                };

        internal static bool IsPowerShellAssembly(string assemblyName)
        {
            if (!String.IsNullOrWhiteSpace(assemblyName))
            {
                // Remove the '.dll' if it's there...
                var fixedName = assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                ? Path.GetFileNameWithoutExtension(assemblyName)
                                : assemblyName;

                if ((fixedName != null) && PowerShellAssemblies.Contains(fixedName))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string GetPowerShellAssemblyStrongName(string assemblyName)
        {
            if (!String.IsNullOrWhiteSpace(assemblyName))
            {
                // Remove the '.dll' if it's there...
                string fixedName = assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                ? Path.GetFileNameWithoutExtension(assemblyName)
                                : assemblyName;

                if ((fixedName != null) && PowerShellAssemblies.Contains(fixedName))
                {
                    return string.Format(CultureInfo.InvariantCulture, PowerShellAssemblyStrongNameFormat, fixedName);
                }
            }

            return assemblyName;
        }

        /// <summary>
        /// If a mutex is abandoned, in our case, it is ok to proceed
        /// </summary>
        /// <param name="mutex">The mutex to wait on. If it is null, a new one will be created</param>
        /// <param name="initializer">The initializer to use to recreate the mutex.</param>
        /// <returns>A working mutex. If the mutex was abandoned, a new one is created to replace it</returns>
        internal static Mutex SafeWaitMutex(Mutex mutex, MutexInitializer initializer)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
                // If the Mutex has been abandoned, then the process protecting the critical section
                // is no longer valid. We need to release to continue normal operations.
                mutex.ReleaseMutex();
                ((IDisposable)mutex).Dispose();

                // Try again, throw if it still fails
                mutex = initializer();
                mutex.WaitOne();
            }

            return mutex;
        }
        internal delegate Mutex MutexInitializer();

        internal static bool Succeeded(int hresult)
        {
            return hresult >= 0;
        }

        // Attempt to determine the existing encoding
        internal static Encoding GetEncoding(string path)
        {
            if (!File.Exists(path))
            {
                return ClrFacade.GetDefaultEncoding();
            }

            byte[] initialBytes = new byte[100];
            int bytesRead = 0;

            try
            {
                using (FileStream stream = System.IO.File.OpenRead(path))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        bytesRead = reader.Read(initialBytes, 0, 100);
                    }
                }
            }
            catch (IOException)
            {
                return ClrFacade.GetDefaultEncoding();
            }

            // Test for four-byte preambles
            string preamble = null;
            Encoding foundEncoding = ClrFacade.GetDefaultEncoding();

            if (bytesRead > 3)
            {
                preamble = String.Join("-", initialBytes[0], initialBytes[1], initialBytes[2], initialBytes[3]);

                if (encodingMap.TryGetValue(preamble, out foundEncoding))
                {
                    return foundEncoding;
                }
            }

            // Test for three-byte preambles
            if (bytesRead > 2)
            {
                preamble = String.Join("-", initialBytes[0], initialBytes[1], initialBytes[2]);
                if (encodingMap.TryGetValue(preamble, out foundEncoding))
                {
                    return foundEncoding;
                }
            }

            // Test for two-byte preambles
            if (bytesRead > 1)
            {
                preamble = String.Join("-", initialBytes[0], initialBytes[1]);
                if (encodingMap.TryGetValue(preamble, out foundEncoding))
                {
                    return foundEncoding;
                }
            }

            // Check for binary
            string initialBytesAsAscii = System.Text.Encoding.ASCII.GetString(initialBytes, 0, bytesRead);
            if (initialBytesAsAscii.IndexOfAny(nonPrintableCharacters) >= 0)
            {
                return Encoding.Unicode;
            }

            return Encoding.ASCII;
        }

        // BigEndianUTF32 encoding is possible, but requires creation
        internal static Encoding BigEndianUTF32Encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
        // [System.Text.Encoding]::GetEncodings() | Where-Object { $_.GetEncoding().GetPreamble() } |
        //     Add-Member ScriptProperty Preamble { $this.GetEncoding().GetPreamble() -join "-" } -PassThru |
        //     Format-Table -Auto
        internal static Dictionary<String, Encoding> encodingMap =
            new Dictionary<string, Encoding>()
            {
                { "255-254", Encoding.Unicode },
                { "254-255", Encoding.BigEndianUnicode },
                { "255-254-0-0", Encoding.UTF32 },
                { "0-0-254-255", BigEndianUTF32Encoding },
                { "239-187-191", Encoding.UTF8 },
            };

        internal static char[] nonPrintableCharacters = {
            (char) 0, (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8,
            (char) 11, (char) 12, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20,
            (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 28, (char) 29, (char) 30,
            (char) 31, (char) 127, (char) 129, (char) 141, (char) 143, (char) 144, (char) 157 };

        internal static readonly UTF8Encoding utf8NoBom =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

#if !CORECLR // TODO:CORECLR - WindowsIdentity.Impersonate() is not available. Use WindowsIdentity.RunImpersonated to replace it.
        /// <summary>
        /// Queues a CLR worker thread with impersonation of provided Windows identity.
        /// </summary>
        /// <param name="identityToImpersonate">Windows identity to impersonate or null.</param>
        /// <param name="threadProc">Thread procedure for thread.</param>
        /// <param name="state">Optional state for thread procedure.</param>
        internal static void QueueWorkItemWithImpersonation(
            WindowsIdentity identityToImpersonate,
            WaitCallback threadProc,
            object state)
        {
            object[] args = new object[3];
            args[0] = identityToImpersonate;
            args[1] = threadProc;
            args[2] = state;
            Threading.ThreadPool.QueueUserWorkItem(WorkItemCallback, args);
        }

        private static void WorkItemCallback(object callBackArgs)
        {
            object[] args = callBackArgs as object[];
            WindowsIdentity identityToImpersonate = args[0] as WindowsIdentity;
            WaitCallback callback = args[1] as WaitCallback;
            object state = args[2];

            WindowsImpersonationContext impersonationContext = null;
            if ((identityToImpersonate != null) &&
                (identityToImpersonate.ImpersonationLevel == TokenImpersonationLevel.Impersonation))
            {
                impersonationContext = identityToImpersonate.Impersonate();
            }
            try
            {
                callback(state);
            }
            finally
            {
                if (impersonationContext != null)
                {
                    try
                    {
                        impersonationContext.Undo();
                        impersonationContext.Dispose();
                    }
                    catch (System.Security.SecurityException) { }
                }
            }
        }
#endif

        /// <summary>
        /// If the command name is fully qualified then it is split into its component parts
        /// E.g., moduleName\commandName
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="moduleName"></param>
        /// <returns>Command name and as appropriate Module name in out parameter</returns>
        internal static string ParseCommandName(string commandName, out string moduleName)
        {
            var names = commandName.Split(Separators.Backslash, 2);
            if (names.Length == 2)
            {
                moduleName = names[0];
                return names[1];
            }

            moduleName = null;
            return commandName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T[] EmptyArray<T>()
        {
            return EmptyArrayHolder<T>._instance;
        }

        internal static ReadOnlyCollection<T> EmptyReadOnlyCollection<T>()
        {
            return EmptyReadOnlyCollectionHolder<T>._instance;
        }

        private static class EmptyArrayHolder<T>
        {
            internal static readonly T[] _instance = new T[0];
        }

        private static class EmptyReadOnlyCollectionHolder<T>
        {
            internal static readonly ReadOnlyCollection<T> _instance =
                new ReadOnlyCollection<T>(EmptyArray<T>());
        }

        internal static class Separators
        {
            internal static readonly char[] Backslash = new char[] { '\\' };
            internal static readonly char[] Directory = new char[] { '\\', '/' };
            internal static readonly char[] DirectoryOrDrive = new char[] { '\\', '/', ':' };

            internal static readonly char[] Colon = new char[] { ':' };
            internal static readonly char[] Dot = new char[] { '.' };
            internal static readonly char[] Pipe = new char[] { '|' };
            internal static readonly char[] Comma = new char[] { ',' };
            internal static readonly char[] Semicolon = new char[] { ';' };
            internal static readonly char[] StarOrQuestion = new char[] { '*', '?' };
            internal static readonly char[] ColonOrBackslash = new char[] { '\\', ':' };
            internal static readonly char[] PathSeparator = new char[] { Path.PathSeparator };

            internal static readonly char[] QuoteChars = new char[] { '\'', '"' };
            internal static readonly char[] Space = new char[] { ' ' };
            internal static readonly char[] QuotesSpaceOrTab = new char[] { ' ', '\t', '\'', '"' };
            internal static readonly char[] SpaceOrTab = new char[] { ' ', '\t' };
            internal static readonly char[] Newline = new char[] { '\n' };
            internal static readonly char[] CrLf = new char[] { '\r', '\n' };

            // (Copied from System.IO.Path so we can call TrimEnd in the same way that Directory.EnumerateFiles would on the search patterns).
            // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
            // String.WhitespaceChars will trim aggressively than what the underlying FS does (for ex, NTFS, FAT).
            internal static readonly char[] PathSearchTrimEnd = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };
        }

        /// <summary>
        /// A COM object could be directly of the type 'System.__ComObject', or it could be a strongly typed RWC,
        /// whose specific type derives from 'System.__ComObject'.
        /// A strongly typed RWC can be created via the 'new' operation with a Primary Interop Assembly (PIA).
        /// For example, with the PIA 'Microsoft.Office.Interop.Excel', you can write the following code:
        ///    var excelApp = new Microsoft.Office.Interop.Excel.Application();
        ///    Type type = excelApp.GetType();
        ///    Type comObjectType = typeof(object).Assembly.GetType("System.__ComObject");
        ///    Console.WriteLine("excelApp type: {0}", type.FullName);
        ///    Console.WriteLine("Is __ComObject assignable from? {0}", comObjectType.IsAssignableFrom(type));
        /// and the results are:
        ///    excelApp type: Microsoft.Office.Interop.Excel.ApplicationClass
        ///    Is __ComObject assignable from? True
        /// </summary>
        internal static bool IsComObject(object obj)
        {
#if UNIX
            return false;
#else
            return obj != null && Marshal.IsComObject(obj);
#endif
        }

        #region Implicit Remoting Batching

        // Commands allowed to run on target remote session along with implicit remote commands
        private static readonly HashSet<string> AllowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ForEach-Object",
            "Measure-Command",
            "Measure-Object",
            "Sort-Object",
            "Where-Object"
        };

        // Determines if the typed command invokes implicit remoting module proxy functions in such
        // a way as to allow simple batching, to reduce round trips between client and server sessions.
        // Requirements:
        //  a. All commands must be implicit remoting module proxy commands targeted to the same remote session
        //  b. Except for *allowed* commands that can be safely run on remote session rather than client session
        //  c. Commands must be in a simple pipeline
        internal static bool TryRunAsImplicitBatch(string command, Runspace runspace)
        {
            try
            {
                var scriptBlock = ScriptBlock.Create(command);
                var scriptBlockAst = scriptBlock.Ast as ScriptBlockAst;
                if (scriptBlockAst == null)
                {
                    return false;
                }

                // Make sure that this is a simple pipeline
                string errorId;
                string errorMsg;
                scriptBlockAst.GetSimplePipeline(true, out errorId, out errorMsg);
                if (errorId != null)
                {
                    return false;
                }

                // Run checker
                var checker = new PipelineForBatchingChecker { ScriptBeingConverted = scriptBlockAst };
                scriptBlockAst.InternalVisit(checker);

                // If this is just a single command, there is no point in batching it
                if (checker.Commands.Count < 2)
                {
                    return false;
                }

                // We have a valid batching candidate
                using (var ps = System.Management.Automation.PowerShell.Create())
                {
                    ps.Runspace = runspace;

                    // Check commands
                    if (!TryGetCommandInfoList(ps, checker.Commands, out Collection<CommandInfo> cmdInfoList))
                    {
                        return false;
                    }

                    // All command modules must be implicit remoting modules from the same PSSession
                    var success = true;
                    var psSessionId = Guid.Empty;
                    foreach (var cmdInfo in cmdInfoList)
                    {
                        // Check for allowed command
                        string cmdName = (cmdInfo is AliasInfo aliasInfo) ? aliasInfo.ReferencedCommand.Name : cmdInfo.Name;
                        if (AllowedCommands.Contains(cmdName))
                        {
                            continue;
                        }

                        // Commands must be from implicit remoting module
                        if (cmdInfo.Module == null || string.IsNullOrEmpty(cmdInfo.ModuleName))
                        {
                            success = false;
                            break;
                        }

                        // Commands must be from modules imported into the same remote session
                        if (cmdInfo.Module.PrivateData is System.Collections.Hashtable privateData)
                        {
                            var sessionIdString = privateData["ImplicitSessionId"] as string;
                            if (string.IsNullOrEmpty(sessionIdString))
                            {
                                success = false;
                                break;
                            }

                            var sessionId = new Guid(sessionIdString);
                            if (psSessionId == Guid.Empty)
                            {
                                psSessionId = sessionId;
                            }
                            else if (psSessionId != sessionId)
                            {
                                success = false;
                                break;
                            }
                        }
                        else
                        {
                            success = false;
                            break;
                        }
                    }

                    if (success)
                    {
                        //
                        // Invoke command pipeline as entire pipeline on remote session
                        //

                        // Update script to declare variables via Using keyword
                        if (checker.ValidVariables.Count > 0)
                        {
                            foreach (var variableName in checker.ValidVariables)
                            {
                                command = command.Replace(variableName, ("Using:" + variableName), StringComparison.OrdinalIgnoreCase);
                            }

                            scriptBlock = ScriptBlock.Create(command);
                        }

                        // Retrieve the PSSession runspace in which to run the batch script on
                        ps.Commands.Clear();
                        ps.Commands.AddCommand("Get-PSSession").AddParameter("InstanceId", psSessionId);
                        var psSession = ps.Invoke<System.Management.Automation.Runspaces.PSSession>().FirstOrDefault();
                        if (psSession == null || (ps.Streams.Error.Count > 0) || (psSession.Availability != RunspaceAvailability.Available))
                        {
                            return false;
                        }

                        // Create and invoke implicit remoting command pipeline
                        ps.Commands.Clear();
                        ps.AddCommand("Invoke-Command").AddParameter("Session", psSession).AddParameter("ScriptBlock", scriptBlock).AddParameter("HideComputerName", true)
                            .AddCommand("Out-Default");

                        try
                        {
                            ps.Invoke();
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(ex, "ImplicitRemotingBatchExecutionTerminatingError", ErrorCategory.InvalidOperation, null);

                            ps.Commands.Clear();
                            ps.AddCommand("Write-Error").AddParameter("InputObject", errorRecord).Invoke();
                        }

                        return true;
                    }
                }
            }
            catch (Exception) { }

            return false;
        }

        private const string WhereObjectCommandAlias = "?";
        private static bool TryGetCommandInfoList(PowerShell ps, HashSet<string> commandNames, out Collection<CommandInfo> cmdInfoList)
        {
            if (commandNames.Count == 0)
            {
                cmdInfoList = null;
                return false;
            }

            bool specialCaseWhereCommandAlias = commandNames.Contains(WhereObjectCommandAlias);
            if (specialCaseWhereCommandAlias)
            {
                commandNames.Remove(WhereObjectCommandAlias);
            }

            // Use Get-Command to collect CommandInfo from candidate commands, with correct precedence so
            // that implicit remoting proxy commands will appear when available.
            ps.Commands.Clear();
            ps.Commands.AddCommand("Get-Command").AddParameter("Name", commandNames.ToArray());
            cmdInfoList = ps.Invoke<CommandInfo>();
            if (ps.Streams.Error.Count > 0)
            {
                return false;
            }

            // For special case '?' alias don't use Get-Command to retrieve command info, and instead
            // use the GetCommand API.
            if (specialCaseWhereCommandAlias)
            {
                var cmdInfo = ps.Runspace.ExecutionContext.SessionState.InvokeCommand.GetCommand(WhereObjectCommandAlias, CommandTypes.Alias);
                if (cmdInfo == null)
                {
                    return false;
                }
                cmdInfoList.Add(cmdInfo);
            }

            return true;
        }

        #endregion
    }

    #region ImplicitRemotingBatching

    // A visitor to walk an AST and validate that it is a candidate for implicit remoting batching.
    // Based on ScriptBlockToPowerShellChecker.
    internal class PipelineForBatchingChecker : AstVisitor
    {
        internal readonly HashSet<string> ValidVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal readonly HashSet<string> Commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal ScriptBlockAst ScriptBeingConverted { get; set; }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            if (!variableExpressionAst.VariablePath.IsAnyLocal())
            {
                ThrowError(
                        new ImplicitRemotingBatchingNotSupportedException(
                            "VariableTypeNotSupported"),
                        variableExpressionAst);
            }

            if (variableExpressionAst.VariablePath.UnqualifiedPath != "_")
            {
                ValidVariables.Add(variableExpressionAst.VariablePath.UnqualifiedPath);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst)
        {
            if (pipelineAst.PipelineElements[0] is CommandExpressionAst)
            {
                // If the first element is a CommandExpression, this pipeline should be the value
                // of a parameter. We want to avoid a scriptblock that contains only a pure expression.
                // The check "pipelineAst.Parent.Parent == ScriptBeingConverted" guarantees we throw
                // error on that kind of scriptblock.

                // Disallow pure expressions at the "top" level, but allow them otherwise.
                // We want to catch:
                //     1 | echo
                // But we don't want to error out on:
                //     echo $(1)
                // See the comment in VisitCommand on why it's safe to check Parent.Parent, we
                // know that we have at least:
                //     * a NamedBlockAst (the end block)
                //     * a ScriptBlockAst (the ast we're comparing to)
                if (pipelineAst.GetPureExpression() == null || pipelineAst.Parent.Parent == ScriptBeingConverted)
                {
                    ThrowError(
                        new ImplicitRemotingBatchingNotSupportedException(
                            "PipelineStartingWithExpressionNotSupported"),
                        pipelineAst);
                }
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            if (commandAst.InvocationOperator == TokenKind.Dot)
            {
                ThrowError(
                    new ImplicitRemotingBatchingNotSupportedException(
                        "DotSourcingNotSupported"),
                    commandAst);
            }

            /*
            // Up front checking ensures that we have a simple script block,
            // so we can safely assume that the parents are:
            //     * a PipelineAst
            //     * a NamedBlockAst (the end block)
            //     * a ScriptBlockAst (the ast we're comparing to)
            // If that isn't the case, the conversion isn't allowed.  It
            // is also safe to assume that we have at least 3 parents, a script block can't be simpler.
            if (commandAst.Parent.Parent.Parent != ScriptBeingConverted)
            {
                ThrowError(
                    new ImplicitRemotingBatchingNotSupportedException(
                        "CantConvertWithCommandInvocations not supported"),
                    commandAst);
            }
            */

            if (commandAst.CommandElements[0] is ScriptBlockExpressionAst)
            {
                ThrowError(
                    new ImplicitRemotingBatchingNotSupportedException(
                        "ScriptBlockInvocationNotSupported"),
                    commandAst);
            }

            var commandName = commandAst.GetCommandName();
            if (commandName != null)
            {
                Commands.Add(commandName);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst)
        {
            if (redirectionAst.ToStream != RedirectionStream.Output)
            {
                ThrowError(
                    new ImplicitRemotingBatchingNotSupportedException(
                        "MergeRedirectionNotSupported"),
                    redirectionAst);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst)
        {
            ThrowError(
                new ImplicitRemotingBatchingNotSupportedException(
                    "FileRedirectionNotSupported"),
                redirectionAst);

            return AstVisitAction.Continue;
        }

        /*
        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            ThrowError(new ImplicitRemotingBatchingNotSupportedException(
                           "ScriptBlocks not supported"),
                       scriptBlockExpressionAst);

            return AstVisitAction.SkipChildren;
        }
        */

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // Using expressions are not expected in Implicit remoting commands.
            ThrowError(new ImplicitRemotingBatchingNotSupportedException(
                "UsingExpressionNotSupported"),
                usingExpressionAst);

            return AstVisitAction.SkipChildren;
        }

        internal static void ThrowError(ImplicitRemotingBatchingNotSupportedException ex, Ast ast)
        {
            InterpreterError.UpdateExceptionErrorRecordPosition(ex, ast.Extent);
            throw ex;
        }
    }

    internal class ImplicitRemotingBatchingNotSupportedException : Exception
    {
        internal string ErrorId
        {
            get;
            private set;
        }

        internal ImplicitRemotingBatchingNotSupportedException(string errorId) : base(
            ParserStrings.ImplicitRemotingPipelineBatchingNotSupported)
        {
            ErrorId = errorId;
        }
    }

    #endregion
}

namespace System.Management.Automation.Internal
{
    /// <summary>This class is used for internal test purposes.</summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public static class InternalTestHooks
    {
        internal static bool BypassGroupPolicyCaching;
        internal static bool ForceScriptBlockLogging;
        internal static bool UseDebugAmsiImplementation;
        internal static bool BypassAppLockerPolicyCaching;
        internal static bool BypassOnlineHelpRetrieval;
        internal static bool ForcePromptForChoiceDefaultOption;

        // Stop/Restart/Rename Computer tests
        internal static bool TestStopComputer;
        internal static bool TestWaitStopComputer;
        internal static bool TestRenameComputer;
        internal static int  TestStopComputerResults;
        internal static int  TestRenameComputerResults;

        // It's useful to test that we don't depend on the ScriptBlock and AST objects and can use a re-parsed version.
        internal static bool IgnoreScriptBlockCache;
        // Simulate 'System.Diagnostics.Stopwatch.IsHighResolution is false' to test Get-Uptime throw
        internal static bool StopwatchIsNotHighResolution;
        internal static bool DisableGACLoading;
        internal static bool SetConsoleWidthToZero;

        // A location to test PSEdition compatibility functionality for Windows PowerShell modules with
        // since we can't manipulate the System32 directory in a test
        internal static string TestWindowsPowerShellPSHomeLocation;

        internal static bool ShowMarkdownOutputBypass;

        /// <summary>This member is used for internal test purposes.</summary>
        public static void SetTestHook(string property, object value)
        {
            var fieldInfo = typeof(InternalTestHooks).GetField(property, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, value);
            }
        }

        /// <summary>
        /// Test hook used to test implicit remoting batching.  A local runspace must be provided that has imported a 
        /// remote session, i.e., has run the Import-PSSession cmdlet.  This hook will return true if the provided commandPipeline
        /// is successfully batched and run in the remote session, and false if it is rejected for batching.
        /// </summary>
        /// <param name="commandPipeline">Command pipeline to test</param>
        /// <param name="runspace">Runspace with imported remote session</param>
        /// <returns>True if commandPipeline is batched successfully</returns>
        public static bool TestImplicitRemotingBatching(string commandPipeline, System.Management.Automation.Runspaces.Runspace runspace)
        {
            return Utils.TryRunAsImplicitBatch(commandPipeline, runspace);
        }
    }

    /// <summary>
    /// Provides undo/redo functionality by using 2 instances of <seealso cref="BoundedStack{T}"/>.
    /// </summary>
    internal class HistoryStack<T>
    {
        private readonly BoundedStack<T> _boundedUndoStack;
        private readonly BoundedStack<T> _boundedRedoStack;

        internal HistoryStack(uint capacity)
        {
            _boundedUndoStack = new BoundedStack<T>(capacity);
            _boundedRedoStack = new BoundedStack<T>(capacity);
        }

        internal void Push(T item)
        {
            _boundedUndoStack.Push(item);
            if (RedoCount >= 0)
            {
                _boundedRedoStack.Clear();
            }
        }

        /// <summary>
        /// Handles bounded history stacks by pushing the current item to the redoStack and returning the item from the popped undoStack.
        /// </summary>
        internal T Undo(T currentItem)
        {
            T previousItem = _boundedUndoStack.Pop();
            _boundedRedoStack.Push(currentItem);
            return previousItem;
        }

        /// <summary>
        /// Handles bounded history stacks by pushing the current item to the undoStack and returning the item from the popped redoStack.
        /// </summary>
        internal T Redo(T currentItem)
        {
            var nextItem = _boundedRedoStack.Pop();
            _boundedUndoStack.Push(currentItem);
            return nextItem;
        }

        internal int UndoCount => _boundedUndoStack.Count;

        internal int RedoCount => _boundedRedoStack.Count;
    }

    /// <summary>
    /// A bounded stack based on a linked list.
    /// </summary>
    internal class BoundedStack<T> : LinkedList<T>
    {
        private readonly uint _capacity;

        /// <summary>
        /// Lazy initialisation, i.e. it sets only its limit but does not allocate the memory for the given capacity.
        /// </summary>
        /// <param name="capacity"></param>
        internal BoundedStack(uint capacity)
        {
            _capacity = capacity;
        }

        /// <summary>
        /// Push item.
        /// </summary>
        /// <param name="item"></param>
        internal void Push(T item)
        {
            this.AddFirst(item);

            if (this.Count > _capacity)
            {
                this.RemoveLast();
            }
        }

        /// <summary>
        /// Pop item.
        /// </summary>
        /// <returns></returns>
        internal T Pop()
        {
            if (this.First == null)
            {
                throw new InvalidOperationException(SessionStateStrings.BoundedStackIsEmpty);
            }

            var item = this.First.Value;
            try
            {
                this.RemoveFirst();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(SessionStateStrings.BoundedStackIsEmpty);
            }
            return item;
        }
    }
}
