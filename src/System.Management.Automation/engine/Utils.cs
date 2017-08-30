/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
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
        /// The existence of the following registry confirms that the host machine is a WinPE
        /// HKLM\System\CurrentControlSet\Control\MiniNT
        /// </summary>
        internal static string WinPEIdentificationRegKey = @"System\CurrentControlSet\Control\MiniNT";

        /// <summary>
        /// Allowed PowerShell Editions
        /// </summary>
        internal static string[] AllowedEditionValues = { "Desktop", "Core" };

        /// <summary>
        /// helper fn to check byte[] arg for null.
        /// </summary>
        ///
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///
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
        ///
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///
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
        ///
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///
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
        ///
        ///<param name="arg"> arg to check </param>
        ///<param name="argName"> name of the arg </param>
        ///
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

        internal static string DefaultPowerShellAppBase { get; } = GetApplicationBase(DefaultPowerShellShellID);

        internal static string GetApplicationBase(string shellId)
        {
            // Use the location of SMA.dll as the application base.
            Assembly assembly = typeof(PSObject).GetTypeInfo().Assembly;
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
                winPEKey = Registry.LocalMachine.OpenSubKey(WinPEIdentificationRegKey);

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
        /// Checks whether current monad session supports edition specified
        /// by checkEdition.
        /// </summary>
        /// <param name="checkEdition">Edition to check</param>
        /// <returns>true if supported, false otherwise</returns>
        internal static bool IsPSEditionSupported(string checkEdition)
        {
            return PSVersionInfo.PSEdition.Equals(checkEdition, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks whether the specified edition values is allowed.
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

        internal static string GetRegistryConfigurationPrefix()
        {
            // For 3.0 PowerShell, we still use "1" as the registry version key for
            // Snapin and Custom shell lookup/discovery.
            // For 3.0 PowerShell, we use "3" as the registry version key only for Engine
            // related data like ApplicationBase etc.
            return "SOFTWARE\\Microsoft\\PowerShell\\" + PSVersionInfo.RegistryVersion1Key + "\\ShellIds";
        }

        internal static string GetRegistryConfigurationPath(string shellID)
        {
            return GetRegistryConfigurationPrefix() + "\\" + shellID;
        }

        // Calling static members of 'Registry' on UNIX will raise 'PlatformNotSupportedException'
#if UNIX
        internal static RegistryKey[] RegLocalMachine = null;
        internal static RegistryKey[] RegCurrentUser = null;
        internal static RegistryKey[] RegLocalMachineThenCurrentUser = null;
        internal static RegistryKey[] RegCurrentUserThenLocalMachine = null;
#else
        internal static RegistryKey[] RegLocalMachine = new[] { Registry.LocalMachine };
        internal static RegistryKey[] RegCurrentUser = new[] { Registry.CurrentUser };
        internal static RegistryKey[] RegLocalMachineThenCurrentUser = new[] { Registry.LocalMachine, Registry.CurrentUser };
        internal static RegistryKey[] RegCurrentUserThenLocalMachine = new[] { Registry.CurrentUser, Registry.LocalMachine };
#endif

        internal static Dictionary<string, object> GetGroupPolicySetting(string settingName, RegistryKey[] preferenceOrder)
        {
            string groupPolicyBase = "Software\\Policies\\Microsoft\\Windows\\PowerShell";
            return GetGroupPolicySetting(groupPolicyBase, settingName, preferenceOrder);
        }

        // We use a static to avoid creating "extra garbage."
        private static Dictionary<string, object> s_emptyDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        internal static Dictionary<string, object> GetGroupPolicySetting(string groupPolicyBase, string settingName, RegistryKey[] preferenceOrder)
        {
#if UNIX
            return s_emptyDictionary;
#else
            lock (s_cachedGroupPolicySettings)
            {
                // Return cached information, if we have it
                Dictionary<string, object> settings;
                if ((s_cachedGroupPolicySettings.TryGetValue(settingName, out settings)) &&
                    !InternalTestHooks.BypassGroupPolicyCaching)
                {
                    return settings;
                }

                if (!String.Equals(".", settingName, StringComparison.OrdinalIgnoreCase))
                {
                    groupPolicyBase += "\\" + settingName;
                }

                settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                foreach (RegistryKey searchKey in preferenceOrder)
                {
                    try
                    {
                        // Look up the machine-wide group policy
                        using (RegistryKey key = searchKey.OpenSubKey(groupPolicyBase))
                        {
                            if (key != null)
                            {
                                foreach (string subkeyName in key.GetValueNames())
                                {
                                    // A null or empty subkey name string corresponds to a (Default) key.
                                    // If it is null, make it an empty string which the Dictionary can handle.
                                    string keyName = subkeyName ?? string.Empty;

                                    settings[keyName] = key.GetValue(keyName);
                                }

                                foreach (string subkeyName in key.GetSubKeyNames())
                                {
                                    // A null or empty subkey name string corresponds to a (Default) key.
                                    // If it is null, make it an empty string which the Dictionary can handle.
                                    string keyName = subkeyName ?? string.Empty;

                                    using (RegistryKey subkey = key.OpenSubKey(keyName))
                                    {
                                        if (subkey != null)
                                        {
                                            settings[keyName] = subkey.GetValueNames();
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }
                    catch (System.Security.SecurityException)
                    {
                        // User doesn't have access to open group policy key
                    }
                }

                // No group policy settings, then return null
                if (settings.Count == 0)
                {
                    settings = null;
                }

                // Cache the data
                if (!InternalTestHooks.BypassGroupPolicyCaching)
                {
                    s_cachedGroupPolicySettings[settingName] = settings;
                }

                return settings;
            }
#endif
        }
        private static ConcurrentDictionary<string, Dictionary<string, object>> s_cachedGroupPolicySettings =
            new ConcurrentDictionary<string, Dictionary<string, object>>();

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
                        if (null != ps)
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
                if (null != ps)
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
                if (null != ps)
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

        internal static bool NativeItemExists(string path)
        {
            bool unusedIsDirectory;
            Exception unusedException;

            return NativeItemExists(path, out unusedIsDirectory, out unusedException);
        }

        // This is done through P/Invoke since File.Exists and Directory.Exists pay 13% performance degradation
        // through the CAS checks, and are terribly slow for network paths.
        internal static bool NativeItemExists(string path, out bool isDirectory, out Exception exception)
        {
            exception = null;

            if (String.IsNullOrEmpty(path))
            {
                isDirectory = false;
                return false;
            }
#if UNIX
            isDirectory = Platform.NonWindowsIsDirectory(path);
            return Platform.NonWindowsIsFile(path);
#else

            if (IsReservedDeviceName(path))
            {
                isDirectory = false;
                return false;
            }

            int result = NativeMethods.GetFileAttributes(path);
            if (result == -1)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 5)
                {
                    // Handle "Access denied" specifically.
                    Win32Exception win32Exception = new Win32Exception(errorCode);
                    exception = new UnauthorizedAccessException(win32Exception.Message, win32Exception);
                }
                else if (errorCode == 32)
                {
                    // Errorcode 32 is 'ERROR_SHARING_VIOLATION' i.e.
                    // The process cannot access the file because it is being used by another process.
                    // GetFileAttributes may return INVALID_FILE_ATTRIBUTES for a system file or directory because of this error.
                    // GetFileAttributes function tries to open the file with FILE_READ_ATTRIBUTES access right but it fails if the
                    // sharing flag for the file is set to 0x00000000.This flag prevents it from opening a file for delete, read, or
                    // write access. For example: C:\pagefile.sys is always opened by OS with sharing flag 0x00000000.
                    // But FindFirstFile is still able to get attributes as this api retrieves the required information using a find
                    // handle generated with FILE_LIST_DIRECTORY access.
                    // Fall back to FindFirstFile to check if the file actually exists.
                    IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
                    NativeMethods.WIN32_FIND_DATA findData;
                    IntPtr findHandle = NativeMethods.FindFirstFile(path, out findData);
                    if (findHandle != INVALID_HANDLE_VALUE)
                    {
                        isDirectory = (findData.dwFileAttributes & NativeMethods.FileAttributes.Directory) != 0;
                        NativeMethods.FindClose(findHandle);
                        return true;
                    }
                }
                else if (errorCode == 53)
                {
                    // ERROR_BAD_NETPATH - The network path was not found.
                    Win32Exception win32Exception = new Win32Exception(errorCode);
                    exception = new IOException(win32Exception.Message, win32Exception);
                }

                isDirectory = false;
                return false;
            }

            isDirectory = (result & ((int)NativeMethods.FileAttributes.Directory)) ==
                ((int)NativeMethods.FileAttributes.Directory);

            return true;
#endif
        }

        // This is done through P/Invoke since we pay 13% performance degradation
        // through the CAS checks required by File.Exists and Directory.Exists
        internal static bool NativeFileExists(string path)
        {
            bool isDirectory;
            Exception ioException;

            bool itemExists = NativeItemExists(path, out isDirectory, out ioException);
            if (ioException != null)
            {
                throw ioException;
            }

            return (itemExists && (!isDirectory));
        }

        // This is done through P/Invoke since we pay 13% performance degradation
        // through the CAS checks required by File.Exists and Directory.Exists
        internal static bool NativeDirectoryExists(string path)
        {
            bool isDirectory;
            Exception ioException;

            bool itemExists = NativeItemExists(path, out isDirectory, out ioException);
            if (ioException != null)
            {
                throw ioException;
            }

            return (itemExists && isDirectory);
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

#if !UNIX
        // This is to reduce the runtime overhead of the feature query
        private static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");
#endif

        internal static bool IsComObject(PSObject psObject)
        {
#if UNIX
            return false;
#else
            if (psObject == null) { return false; }

            object obj = PSObject.Base(psObject);
            return IsComObject(obj);
#endif
        }

        internal static bool IsComObject(object obj)
        {
#if UNIX
            return false;
#else
            // We can't use System.Runtime.InteropServices.Marshal.IsComObject(obj) since it doesn't work in partial trust.
            //
            // There could be strongly typed RWCs whose type is not 'System.__ComObject', but the more specific type should
            // derive from 'System.__ComObject'. The strongly typed RWCs can be created with 'new' operation via the Primay
            // Interop Assembly (PIA).
            // For example, with the PIA 'Microsoft.Office.Interop.Excel', you can write the following code:
            //    var excelApp = new Microsoft.Office.Interop.Excel.Application();
            //    Type type = excelApp.GetType();
            //    Type comObjectType = typeof(object).Assembly.GetType("System.__ComObject");
            //    Console.WriteLine("excelApp type: {0}", type.FullName);
            //    Console.WriteLine("Is __ComObject assignable from? {0}", comObjectType.IsAssignableFrom(type));
            // and the results are:
            //    excelApp type: Microsoft.Office.Interop.Excel.ApplicationClass
            //    Is __ComObject assignable from? True
            return obj != null && ComObjectType.IsAssignableFrom(obj.GetType());
#endif
        }
    }
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

        /// <summary>This member is used for internal test purposes.</summary>
        public static void SetTestHook(string property, object value)
        {
            var fieldInfo = typeof(InternalTestHooks).GetField(property, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, value);
            }
        }
    }
}
