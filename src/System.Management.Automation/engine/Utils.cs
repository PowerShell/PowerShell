/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
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
using System.Security.Principal;

using TypeTable = System.Management.Automation.Runspaces.TypeTable;
using PSUtils = System.Management.Automation.PsUtils;

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
            // lenghts of 128, 192 and 256 bits.
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
                p = ClrFacade.SecureStringToCoTaskMemUnicode(ss);
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

        private static string s_pshome = null;

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

        /// <summary>
        /// Gets the application base for current monad version
        /// </summary>
        /// <returns>
        /// applicationbase path for current monad version installation
        /// </returns>
        /// <exception cref="SecurityException">
        /// if caller doesn't have permission to read the key
        /// </exception>
        internal static string GetApplicationBase(string shellId)
        {
#if CORECLR 
            // Use the location of SMA.dll as the application base
            // Assembly.GetEntryAssembly and GAC are not in CoreCLR.
            Assembly assembly = typeof(PSObject).GetTypeInfo().Assembly;
            return Path.GetDirectoryName(assembly.Location);
#else
            // This code path applies to Windows FullCLR inbox deployments. All CoreCLR 
            // implementations should use the location of SMA.dll since it must reside in PSHOME.
            //
            // try to get the path from the registry first
            string result = GetApplicationBaseFromRegistry(shellId);
            if (result != null)
            {
                return result;
            }
            
            // The default keys aren't installed, so try and use the entry assembly to
            // get the application base. This works for managed apps like minishells...
            Assembly assem = Assembly.GetEntryAssembly();
            if (assem != null)
            {
                // For minishells, we just return the executable path. 
                return Path.GetDirectoryName(assem.Location);
            }

            // For unmanaged host apps, look for the SMA dll, if it's not GAC'ed then
            // use it's location as the application base...
            assem = typeof(PSObject).GetTypeInfo().Assembly;
            string gacRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.Net\\assembly");
            if (!assem.Location.StartsWith(gacRootPath, StringComparison.OrdinalIgnoreCase))
            {
                // For other hosts. 
                return Path.GetDirectoryName(assem.Location);
            }

            // otherwise, just give up...
            return "";
#endif
        }

        private static string[] s_productFolderDirectories;

        /// <summary>
        /// Specifies the per-user configuration settings directory in a platform agnostic manner.
        /// Windows Ex:
        ///     %LOCALAPPDATA%\PowerShell
        /// Non-Windows Ex:
        ///     ~/.config/PowerShell
        /// </summary>
        /// <returns>The current user's configuration settings directory</returns>
        internal static string GetUserSettingsDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath, "PowerShell");
        }

        private static string[] GetProductFolderDirectories()
        {
            if (s_productFolderDirectories == null)
            {
                List<string> baseDirectories = new List<string>();

                // Retrieve the application base from the registry
                string appBase = GetApplicationBase(DefaultPowerShellShellID);
                if (!string.IsNullOrEmpty(appBase))
                {
                    baseDirectories.Add(appBase);
                }

                // Win8: 454976
                // Now add the two variations of System32
                baseDirectories.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
                string systemX86 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                if (!string.IsNullOrEmpty(systemX86))
                {
                    baseDirectories.Add(systemX86);
                }

                // And built-in modules
                string progFileDir;
                // TODO: #1184 will resolve this work-around
                // Side-by-side versions of PowerShell use modules from their application base, not
                // the system installation path.
#if CORECLR
                progFileDir = Path.Combine(appBase, "Modules");
#else
                progFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell", "Modules");
#endif

                if (!string.IsNullOrEmpty(progFileDir))
                {
                    baseDirectories.Add(Path.Combine(progFileDir, "PackageManagement"));
                    baseDirectories.Add(Path.Combine(progFileDir, "PowerShellGet"));
                    baseDirectories.Add(Path.Combine(progFileDir, "Pester"));
                    baseDirectories.Add(Path.Combine(progFileDir, "PSReadLine"));
#if CORECLR
                    baseDirectories.Add(Path.Combine(progFileDir, "Json.Net"));
#endif // CORECLR
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
            return Utils.GetApplicationBase(Utils.DefaultPowerShellShellID).Contains("SysWOW64");
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

#if !CORECLR
        /// <summary>
        /// Checks whether current monad session supports NetFrameworkVersion specified
        /// by checkVersion. The specified version is treated as the the minimum required 
        /// version of .NET framework.
        /// </summary>
        /// <param name="checkVersion">Version to check</param>
        /// <param name="higherThanKnownHighestVersion">true if version to check is higher than the known highest version</param>
        /// <returns>true if supported, false otherwise</returns>
        internal static bool IsNetFrameworkVersionSupported(Version checkVersion, out bool higherThanKnownHighestVersion)
        {
            higherThanKnownHighestVersion = false;
            bool isSupported = false;

            if (checkVersion == null)
            {
                return false;
            }

            // Construct a temporary version number with build number and revision number set to 0. 
            // This is done so as to re-use the version specifications in PSUtils.FrameworkRegistryInstallation          
            Version tempVersion = new Version(checkVersion.Major, checkVersion.Minor, 0, 0);

            // Win8: 840038 - For any version above the highest known .NET version (4.5 for Windows 8), we can't make a call as to 
            // whether the requirement is satisfied or not because we can't detect that version of .NET.
            // We end up erring on the side of app compat by letting it through. 
            // We will write a message in the Verbose output saying that we cannot detect the specified version of the .NET Framework.
            if (checkVersion > PsUtils.FrameworkRegistryInstallation.KnownHighestNetFrameworkVersion)
            {
                isSupported = true;
                higherThanKnownHighestVersion = true;
            }
            // For a script to have a valid .NET version, the specified version or atleast one of its compatible versions must be installed on the machine.
            else if (PSUtils.FrameworkRegistryInstallation.CompatibleNetFrameworkVersions.ContainsKey(tempVersion))
            {
                if (PSUtils.FrameworkRegistryInstallation.IsFrameworkInstalled(tempVersion.Major, tempVersion.Minor, 0))
                {
                    // If the specified version is installed on the machine, then we return true.
                    isSupported = true;
                }
                else
                {
                    // If any of the compatible versions are installed on the machine, then we return true.
                    HashSet<Version> compatibleVersions = PSUtils.FrameworkRegistryInstallation.CompatibleNetFrameworkVersions[tempVersion];
                    foreach (Version compatibleVersion in compatibleVersions)
                    {
                        if (PSUtils.FrameworkRegistryInstallation.IsFrameworkInstalled(compatibleVersion.Major, compatibleVersion.Minor, 0))
                        {
                            isSupported = true;
                            break;
                        }
                    }
                }
            }

            return isSupported;
        }
#endif
        #endregion

        /// <summary>
        /// String representing the Default shellID.
        /// </summary>
        internal const string DefaultPowerShellShellID = "Microsoft.PowerShell";

        /// <summary>
        /// This is used to construct the profile path.
        /// </summary>
#if CORECLR
        internal static string ProductNameForDirectory = Platform.IsInbox ? "WindowsPowerShell" : "PowerShell";
#else
        internal const string ProductNameForDirectory = "WindowsPowerShell";
#endif

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

        // Retrieves group policy settings based on the preference order provided:
        // Dictionary<string, object> settings = GetGroupPolicySetting("Transcription", Registry.LocalMachine, Registry.CurrentUser);

        internal static RegistryKey[] RegLocalMachine = new[] { Registry.LocalMachine };
        internal static RegistryKey[] RegCurrentUser = new[] { Registry.CurrentUser };
        internal static RegistryKey[] RegLocalMachineThenCurrentUser = new[] { Registry.LocalMachine, Registry.CurrentUser };
        internal static RegistryKey[] RegCurrentUserThenLocalMachine = new[] { Registry.CurrentUser, Registry.LocalMachine };

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

        internal const string WorkflowType = "Microsoft.PowerShell.Workflow.AstToWorkflowConverter, Microsoft.PowerShell.Activities, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
        internal const string WorkflowModule = "PSWorkflow";

        internal static IAstToWorkflowConverter GetAstToWorkflowConverterAndEnsureWorkflowModuleLoaded(ExecutionContext context)
        {
            IAstToWorkflowConverter converterInstance = null;
            Type converterType = null;

            if (Utils.IsRunningFromSysWOW64())
            {
                throw new NotSupportedException(AutomationExceptions.WorkflowDoesNotSupportWOW64);
            }

            // If the current language mode is ConstrainedLanguage but the system lockdown mode is not,
            // then also block the conversion - since we can't validate the InlineScript, PowerShellValue,
            // etc.
            if ((context != null) &&
                (context.LanguageMode == PSLanguageMode.ConstrainedLanguage) &&
                (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Enforce))
            {
                throw new NotSupportedException(Modules.CannotDefineWorkflowInconsistentLanguageMode);
            }

            EnsureModuleLoaded(WorkflowModule, context);

            converterType = Type.GetType(WorkflowType);

            if (converterType != null)
            {
                converterInstance = (IAstToWorkflowConverter)converterType.GetConstructor(PSTypeExtensions.EmptyTypes).Invoke(EmptyArray<object>());
            }

            if (converterInstance == null)
            {
                string error = StringUtil.Format(AutomationExceptions.CantLoadWorkflowType, Utils.WorkflowType, Utils.WorkflowModule);
                throw new NotSupportedException(error);
            }

            return converterInstance;
        }

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
                    catch (Exception e)
                    {
                        // Call-out to user code, catch-all OK
                        CommandProcessorBase.CheckForSevereException(e);
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
            catch (Exception e)
            {
                // Call-out to user code, catch-all OK
                CommandProcessorBase.CheckForSevereException(e);
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
            catch (Exception e)
            {
                // Call-out to user code, catch-all OK
                CommandProcessorBase.CheckForSevereException(e);
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
            // permissions. To fit into PowerShell's existing model of pre-emptively checking
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
            [DllImport(PinvokeDllNames.GetFileAttributesDllName, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int GetFileAttributes(string lpFileName);

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
                    "microsoft.powershell.activities",
                    "microsoft.powershell.commands.diagnostics",
                    "microsoft.powershell.commands.management",
                    "microsoft.powershell.commands.utility",
                    "microsoft.powershell.consolehost",
                    "microsoft.powershell.core.activities",
                    "microsoft.powershell.diagnostics.activities",
                    "microsoft.powershell.editor",
                    "microsoft.powershell.gpowershell",
                    "microsoft.powershell.graphicalhost",
                    "microsoft.powershell.isecommon",
                    "microsoft.powershell.management.activities",
                    "microsoft.powershell.scheduledjob",
                    "microsoft.powershell.security.activities",
                    "microsoft.powershell.security",
                    "microsoft.powershell.utility.activities",
                    "microsoft.powershell.workflow.servicecore",
                    "microsoft.wsman.management.activities",
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

        internal static FileSystemCmdletProviderEncoding GetEncoding(string path)
        {
            if (!File.Exists(path))
            {
                return FileSystemCmdletProviderEncoding.Default;
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
                return FileSystemCmdletProviderEncoding.Default;
            }

            // Test for four-byte preambles
            string preamble = null;
            FileSystemCmdletProviderEncoding foundEncoding = FileSystemCmdletProviderEncoding.Default;

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
                return FileSystemCmdletProviderEncoding.Byte;
            }

            return FileSystemCmdletProviderEncoding.Ascii;
        }

        internal static Encoding GetEncodingFromEnum(FileSystemCmdletProviderEncoding encoding)
        {
            System.Text.Encoding result = System.Text.Encoding.Unicode;

            switch (encoding)
            {
                case FileSystemCmdletProviderEncoding.String:
                    result = new UnicodeEncoding();
                    break;

                case FileSystemCmdletProviderEncoding.Unicode:
                    result = new UnicodeEncoding();
                    break;

                case FileSystemCmdletProviderEncoding.BigEndianUnicode:
                    result = new UnicodeEncoding(true, false);
                    break;

                case FileSystemCmdletProviderEncoding.UTF8:
                    result = new UTF8Encoding();
                    break;

                case FileSystemCmdletProviderEncoding.UTF7:
                    result = new UTF7Encoding();
                    break;

                case FileSystemCmdletProviderEncoding.UTF32:
                    result = new UTF32Encoding();
                    break;

                case FileSystemCmdletProviderEncoding.BigEndianUTF32:
                    result = new UTF32Encoding(true, false);
                    break;

                case FileSystemCmdletProviderEncoding.Ascii:
                    result = new ASCIIEncoding();
                    break;

                case FileSystemCmdletProviderEncoding.Default:
                    result = ClrFacade.GetDefaultEncoding();
                    break;

                case FileSystemCmdletProviderEncoding.Oem:
                    result = ClrFacade.GetOEMEncoding();
                    break;

                default:
                    // Default to unicode encoding
                    result = new UnicodeEncoding();
                    break;
            }

            return result;
        } // GetEncodingFromEnum

        // [System.Text.Encoding]::GetEncodings() | ? { $_.GetEncoding().GetPreamble() } |
        //     Add-Member ScriptProperty Preamble { $this.GetEncoding().GetPreamble() -join "-" } -PassThru |
        //     Format-Table -Auto
        internal static Dictionary<String, FileSystemCmdletProviderEncoding> encodingMap =
            new Dictionary<string, FileSystemCmdletProviderEncoding>()
            {
                { "255-254", FileSystemCmdletProviderEncoding.Unicode },
                { "254-255", FileSystemCmdletProviderEncoding.BigEndianUnicode },
                { "255-254-0-0", FileSystemCmdletProviderEncoding.UTF32 },
                { "0-0-254-255", FileSystemCmdletProviderEncoding.BigEndianUTF32 },
                { "239-187-191", FileSystemCmdletProviderEncoding.UTF8 },
            };

        internal static char[] nonPrintableCharacters = {
            (char) 0, (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8,
            (char) 11, (char) 12, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20,
            (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 28, (char) 29, (char) 30,
            (char) 31, (char) 127, (char) 129, (char) 141, (char) 143, (char) 144, (char) 157 };

#if !CORECLR // TODO:CORECLR - WindowsIdentity.Impersonate() is not available. Use WindowsIdentity.RunImplemented to replace it.
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
        // It's useful to test that we don't depend on the ScriptBlock and AST objects and can use a re-parsed version.
        internal static bool IgnoreScriptBlockCache;

        /// <summary>This member is used for internal test purposes.</summary>
        public static void SetTestHook(string property, bool value)
        {
            var fieldInfo = typeof(InternalTestHooks).GetField(property, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, value);
            }
        }
    }
}
