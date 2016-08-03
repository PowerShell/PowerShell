/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Management.Automation.Language;
using Microsoft.Management.Infrastructure;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines generic utilities and helper methods for PowerShell
    /// </summary>
    internal static class PsUtils
    {
        internal static string ArmArchitecture = "ARM";

        /// <summary>
        /// Safely retrieves the MainModule property of a
        /// process. Version 2.0 and below of the .NET Framework are 
        /// impacted by a Win32 API usability knot that throws an
        /// exception if API tries to enumerate the process' modules
        /// while it is still loading them. This generates the error
        /// message: Only part of a ReadProcessMemory or
        /// WriteProcessMemory request was completed.
        /// The BCL fix in V3 was to just try more, so we do the same
        /// thing.
        ///
        /// Note: If you attempt to retrieve the MainModule of a 64-bit
        /// process from a WOW64 (32-bit) process, the Win32 API has a fatal
        /// flaw that causes this to return the same error.
        ///
        /// If you need the MainModule of a 64-bit process from a WOW64
        /// process, you will need to write the P/Invoke yourself.
        /// </summary>
        ///
        /// <param name="targetProcess">The process from which to
        /// retrieve the MainModule</param>
        /// <exception cref="NotSupportedException">
        /// You are trying to access the MainModule property for a process that is running 
        /// on a remote computer. This property is available only for processes that are 
        /// running on the local computer.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The process Id is not available (or) The process has exited. 
        /// </exception>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// 
        /// </exception>
        internal static ProcessModule GetMainModule(Process targetProcess)
        {
            int caughtCount = 0;
            ProcessModule mainModule = null;

            while (mainModule == null)
            {
                try
                {
                    mainModule = targetProcess.MainModule;
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    // If this is an Access Denied error (which can happen with thread impersonation)
                    // then re-throw immediately.
                    if (e.NativeErrorCode == 5)
                        throw;

                    // Otherwise retry to ensure module is loaded.
                    caughtCount++;
                    System.Threading.Thread.Sleep(100);
                    if (caughtCount == 5)
                        throw;
                }
            }

            return mainModule;
        }

        /// <summary>
        /// Retrieve the parent process of a process.
        /// 
        /// This is an extremely expensive operation, as WMI
        /// needs to work with an ugly Win32 API. The Win32 API
        /// creates a snapshot of every process in the system, which
        /// you then need to iterate through to find your process and
        /// its parent PID.
        ///
        /// Also, since this is PID based, this API is only reliable
        /// when the process has not yet exited.
        /// </summary>
        ///
        /// <param name="current">The process we want to find the
        /// parent of</param>
        internal static Process GetParentProcess(Process current)
        {
            string wmiQuery = String.Format(CultureInfo.CurrentCulture,
                                            "Select * From Win32_Process Where Handle='{0}'",
                                            current.Id);

            using (CimSession cimSession = CimSession.Create(null))
            {
                IEnumerable<CimInstance> processCollection =
                    cimSession.QueryInstances("root/cimv2", "WQL", wmiQuery);

                int parentPid =
                    processCollection.Select(
                        cimProcess =>
                        Convert.ToInt32(cimProcess.CimInstanceProperties["ParentProcessId"].Value,
                                        CultureInfo.CurrentCulture)).FirstOrDefault();

                if (parentPid == 0)
                    return null;

                try
                {
                    Process returnProcess = Process.GetProcessById(parentPid);

                    // Ensure the process started before the current
                    // process, as it could have gone away and had the
                    // PID recycled.
                    if (returnProcess.StartTime <= current.StartTime)
                        return returnProcess;
                    else
                        return null;
                }
                catch (ArgumentException)
                {
                    // GetProcessById throws an ArgumentException when
                    // you reach the top of the chain -- Explorer.exe
                    // has a parent process, but you cannot retrieve it.
                    return null;
                }
            }
        }

#if !CORECLR // .NET Frmework Version is not applicable to CoreCLR
        /// <summary>
        /// Detects the installation of Frmework Versions 1.1, 2.0, 3.0 and 3.5 and 4.0 through
        /// the official registry instalation keys.
        /// </summary>
        internal static class FrameworkRegistryInstallation
        {
            /// <summary>
            /// Gets the three registry names allowing for framework installation and service pack checks based on the
            /// majorVersion and minorVersion version numbers.
            /// </summary>
            /// <param name="majorVersion">Major version of .NET required, for .NET 3.5 this is 3.</param>
            /// <param name="minorVersion">Minor version of .NET required, for .NET 3.5 this is 5.</param>
            /// <param name="installKeyName">name of the key containing installValueName</param>
            /// <param name="installValueName">name of the registry key indicating the SP has been installed</param>
            /// <param name="spKeyName">name of the key containing the SP value with SP version</param>
            /// <param name="spValueName">name of the value containing the SP value with SP version</param>
            /// <returns>true if the majorVersion and minorVersion correspond the versions we can check for, false otherwise.</returns>
            private static bool GetRegistryNames(int majorVersion, int minorVersion, out string installKeyName, out string installValueName, out string spKeyName, out string spValueName)
            {
                installKeyName = null;
                spKeyName = null;
                installValueName = null;
                spValueName = "SP";


                const string v1_1KeyName = "v1.1.4322";
                const string v2KeyName = "v2.0.50727";
                const string v3KeyName = "v3.0";
                const string v3_5KeyName = "v3.5";

                // There are two registry keys "Client" and "Full" corresponding to the Client and Full .NET 4 Profiles.
                // Client is a subset of the assemblies in Full (identical assemblies, smaller set), having most of the
                // .NET 4's features.
                // Here is some information on the client profile: http://msdn.microsoft.com/en-us/library/cc656912.aspx
                // For now, we are picking Client, because it has most of .NET features and is the only version available
                // in Server Core. If, in the future, PowerShell needs to depend on Full, we might have to revisit this
                // decision.
                const string v4KeyName = @"v4\Client";
                const string install = "Install";
                const string oneToThreePointFivePrefix = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\";

                // In .NET 4.5, there is no concept of Client and Full. There is only the full redistributable package available 
                // http://msdn.microsoft.com/en-us/library/cc656912(VS.110).aspx
                const string v45KeyName = @"v4\Full";
                const string v45ReleaseKeyName = "Release";

                if (majorVersion == 1 && minorVersion == 1)
                {
                    // http://msdn.microsoft.com/en-us/library/ms994402.aspx
                    installKeyName = oneToThreePointFivePrefix + v1_1KeyName;
                    spKeyName = installKeyName;
                    installValueName = install;
                    return true;
                }
                if (majorVersion == 2 && minorVersion == 0)
                {
                    // http://msdn.microsoft.com/en-us/library/aa480243.aspx
                    installKeyName = oneToThreePointFivePrefix + v2KeyName;
                    spKeyName = installKeyName;
                    installValueName = install;
                    return true;
                }
                if (majorVersion == 3 && minorVersion == 0)
                {
                    // http://msdn.microsoft.com/en-us/library/aa480173.aspx
                    installKeyName = oneToThreePointFivePrefix + v3KeyName + @"\Setup";
                    spKeyName = oneToThreePointFivePrefix + v3KeyName;
                    installValueName = "InstallSuccess";
                    return true;
                }
                if (majorVersion == 3 && minorVersion == 5)
                {
                    // http://msdn.microsoft.com/en-us/library/cc160716.aspx
                    installKeyName = oneToThreePointFivePrefix + v3_5KeyName;
                    spKeyName = installKeyName;
                    installValueName = install;
                    return true;
                }
                if (majorVersion == 4 && minorVersion == 0)
                {
                    // http://msdn.microsoft.com/library/ee942965(v=VS.100).aspx
                    installKeyName = oneToThreePointFivePrefix + v4KeyName;
                    spKeyName = installKeyName;
                    installValueName = install;
                    spValueName = "Servicing";
                    return true;
                }
                if (majorVersion == 4 && minorVersion == 5)
                {
                    // http://msdn.microsoft.com/en-us/library/ee942965(VS.110).aspx
                    installKeyName = oneToThreePointFivePrefix + v45KeyName;
                    installValueName = v45ReleaseKeyName;
                    return true;
                }

                // To add v1.0 in the future note that
                // http://msdn.microsoft.com/en-us/library/ms994395.aspx does not mention 
                // NDP keys since they were not introduced until later.
                // There were no official setup keys,  but this blog suggests an alternative for finding out
                // about the service pack information:
                // http://blogs.msdn.com/astebner/archive/2004/09/14/229802.aspx
                return false;
            }

            /// <summary>
            /// Tries to read the valueName from the registry key returning null if
            /// the it was not found, if it is not an integer or if or an exception was thrown.
            /// </summary>
            /// <param name="key">Key containing valueName</param>
            /// <param name="valueName">Name of value to be returned</param>
            /// <returns>The value or null if it could not be retrieved</returns>
            private static int? GetRegistryKeyValueInt(RegistryKey key, string valueName)
            {
                try
                {
                    object keyValue = key.GetValue(valueName);
                    if (keyValue is int)
                    {
                        return (int)keyValue;
                    }
                    return null;
                }
                catch (ObjectDisposedException)
                {
                    return null;
                }
                catch (SecurityException)
                {
                    return null;
                }
                catch (IOException)
                {
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }

            /// <summary>
            /// Tries to read the keyName from the registry key returning null if
            /// the key was not found or an exception was thrown.
            /// </summary>
            /// <param name="key">Key containing subKeyName</param>
            /// <param name="subKeyName">NAme of sub key to be returned</param>
            /// <returns>The subkey or null if it could not be retrieved</returns>
            private static RegistryKey GetRegistryKeySubKey(RegistryKey key, string subKeyName)
            {
                try
                {
                    return key.OpenSubKey(subKeyName);
                }
                catch (ObjectDisposedException)
                {
                    return null;
                }
                catch (SecurityException)
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            // based on Table in http://support.microsoft.com/kb/318785
            private static Version V4_0 = new Version(4, 0, 30319, 0);
            private static Version V3_5 = new Version(3, 5, 21022, 8);
            private static Version V3_5sp1 = new Version(3, 5, 30729, 1);
            private static Version V3_0 = new Version(3, 0, 4506, 30);
            private static Version V3_0sp1 = new Version(3, 0, 4506, 648);
            private static Version V3_0sp2 = new Version(3, 0, 4506, 2152);
            private static Version V2_0 = new Version(2, 0, 50727, 42);
            private static Version V2_0sp1 = new Version(2, 0, 50727, 1433);
            private static Version V2_0sp2 = new Version(2, 0, 50727, 3053);
            private static Version V1_1 = new Version(1, 1, 4322, 573);
            private static Version V1_1sp1 = new Version(1, 1, 4322, 2032);
            private static Version V1_1sp1Server = new Version(1, 1, 4322, 2300);

            // Original versions without build or revision numbers
            private static Version V4_5_00 = new Version(4, 5, 0, 0);
            private static Version V4_0_00 = new Version(4, 0, 0, 0);
            private static Version V3_5_00 = new Version(3, 5, 0, 0);
            private static Version V3_0_00 = new Version(3, 0, 0, 0);
            private static Version V2_0_00 = new Version(2, 0, 0, 0);
            private static Version V1_1_00 = new Version(1, 1, 0, 0);

            // Dictionary holding compatible .NET framework versions
            // This is used in verifying the .NET framework version for loading module manifest
            internal static Dictionary<Version, HashSet<Version>> CompatibleNetFrameworkVersions = new Dictionary<Version, HashSet<Version>>() {
                {V1_1_00, new HashSet<Version> {V4_5_00, V4_0_00, V3_5_00, V3_0_00, V2_0_00}},
                {V2_0_00, new HashSet<Version> {V4_5_00, V4_0_00, V3_5_00, V3_0_00}},
                {V3_0_00, new HashSet<Version> {V4_5_00, V4_0_00, V3_5_00 }},
                {V3_5_00, new HashSet<Version> {V4_5_00, V4_0_00 }},
                {V4_0_00, new HashSet<Version> {V4_5_00}},
                {V4_5_00, new HashSet<Version> ()},
            };

            // .NET 4.5 is the highest known .NET version for PowerShell 3.0
            internal static Version KnownHighestNetFrameworkVersion = new Version(4, 5);

            /// <summary>
            /// Returns true if IsFrameworkInstalled will be able to check for this framework version.
            /// </summary>
            /// <param name="version">version to be checked</param>
            /// <param name="majorVersion">Major version of .NET required, for .NET 3.5 this is 3.</param>
            /// <param name="minorVersion">Minor version of .NET required, for .NET 3.5 this is 5.</param>
            /// <param name="minimumSpVersion">Minimum SP version number corresponding to <paramref name="version"/>.</param>
            /// <returns>true if IsFrameworkInstalled will be able to check for this framework version</returns>
            internal static bool CanCheckFrameworkInstallation(Version version, out int majorVersion, out int minorVersion, out int minimumSpVersion)
            {
                // based on Table in http://support.microsoft.com/kb/318785
                majorVersion = -1;
                minorVersion = -1;
                minimumSpVersion = -1;

                if (version == V4_5_00)
                {
                    majorVersion = 4;
                    minorVersion = 5;
                    minimumSpVersion = 0;
                    return true;
                }

                if (version == V4_0 || version == V4_0_00)
                {
                    majorVersion = 4;
                    minorVersion = 0;
                    minimumSpVersion = 0;
                    return true;
                }
                if (version == V3_5 || version == V3_5_00)
                {
                    majorVersion = 3;
                    minorVersion = 5;
                    minimumSpVersion = 0;
                    return true;
                }
                if (version == V3_5sp1)
                {
                    majorVersion = 3;
                    minorVersion = 5;
                    minimumSpVersion = 1;
                    return true;
                }
                else if (version == V3_0 || version == V3_0_00)
                {
                    majorVersion = 3;
                    minorVersion = 0;
                    minimumSpVersion = 0;
                    return true;
                }
                else if (version == V3_0sp1)
                {
                    majorVersion = 3;
                    minorVersion = 0;
                    minimumSpVersion = 1;
                    return true;
                }
                else if (version == V3_0sp2)
                {
                    majorVersion = 3;
                    minorVersion = 0;
                    minimumSpVersion = 2;
                    return true;
                }
                else if (version == V2_0 || version == V2_0_00)
                {
                    majorVersion = 2;
                    minorVersion = 0;
                    minimumSpVersion = 0;
                    return true;
                }
                else if (version == V2_0sp1)
                {
                    majorVersion = 2;
                    minorVersion = 0;
                    minimumSpVersion = 1;
                    return true;
                }
                else if (version == V2_0sp2)
                {
                    majorVersion = 2;
                    minorVersion = 0;
                    minimumSpVersion = 2;
                    return true;
                }
                else if (version == V1_1 || version == V1_1_00)
                {
                    majorVersion = 1;
                    minorVersion = 1;
                    minimumSpVersion = 0;
                    return true;
                }
                else if (version == V1_1sp1 || version == V1_1sp1Server)
                {
                    majorVersion = 1;
                    minorVersion = 1;
                    minimumSpVersion = 1;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Check if the given version if the framework is installed
            /// </summary>
            /// <param name="version">version to check. 
            /// for .NET Framework 3.5 and any service pack this can be new Version(3,5) or new Version(3, 5, 21022, 8).
            /// for .NET 3.5 with SP1 this should be new Version(3, 5, 30729, 1).
            /// For other versions please check the table at http://support.microsoft.com/kb/318785.
            /// </param>
            /// <returns></returns>
            internal static bool IsFrameworkInstalled(Version version)
            {
                int minorVersion, majorVersion, minimumSPVersion;
                if (!FrameworkRegistryInstallation.CanCheckFrameworkInstallation(
                        version,
                        out majorVersion,
                        out minorVersion,
                        out minimumSPVersion))
                {
                    return false;
                }
                return IsFrameworkInstalled(majorVersion, minorVersion, minimumSPVersion);
            }

            /// <summary>
            /// Check if the given version if the framework is installed
            /// </summary>
            /// <param name="majorVersion">Major version of .NET required, for .NET 3.5 this is 3.</param>
            /// <param name="minorVersion">Minor version of .NET required, for .NET 3.5 this is 5.</param>
            /// <param name="minimumSPVersion">Minimum SP version required. 0 (Zero) or less means no SP requirement.</param>
            /// <returns>true if the framework is available. False if it is not available or that could not be determined.</returns>
            internal static bool IsFrameworkInstalled(int majorVersion, int minorVersion, int minimumSPVersion)
            {
                string installKeyName, installValueName, spKeyName, spValueName;
                if (!FrameworkRegistryInstallation.GetRegistryNames(majorVersion, minorVersion, out installKeyName, out installValueName, out spKeyName, out spValueName))
                {
                    return false;
                }

                RegistryKey installKey = FrameworkRegistryInstallation.GetRegistryKeySubKey(Registry.LocalMachine, installKeyName);
                if (installKey == null)
                {
                    return false;
                }

                int? installValue = FrameworkRegistryInstallation.GetRegistryKeyValueInt(installKey, installValueName);
                if (installValue == null)
                {
                    return false;
                }
                // The detection logic for .NET 4.5 is to check for the existence of a DWORD key named Release under HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full folder in the registry.
                // For .NET 4.5, the value of this key is the release number and not 1 (Install = 1 for .NET 3.5, .NET 4.0) . So, we need to bypasss the check below
                if ((majorVersion != 4 && minorVersion != 5) && (installValue != 1))
                {
                    Debug.Assert(PSVersionInfo.CLRVersion.Major == 4, "This check is valid only for CLR Version 4.0 and .NET Version 4.5");
                    return false;
                }

                if (minimumSPVersion > 0)
                {
                    RegistryKey spKey = FrameworkRegistryInstallation.GetRegistryKeySubKey(Registry.LocalMachine, spKeyName);
                    if (spKey == null)
                    {
                        return false;
                    }
                    int? spValue = FrameworkRegistryInstallation.GetRegistryKeyValueInt(spKey, spValueName);
                    if (spValue == null)
                    {
                        return false;
                    }
                    if (spValue < minimumSPVersion)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
#endif

        /// <summary>
        /// Returns processor architecture for the current process.
        /// If powershell is running inside Wow64, then <see cref="ProcessorArchitecture.X86"/> is returned.
        /// </summary>
        /// <returns>processor architecture for the current process</returns>
        internal static ProcessorArchitecture GetProcessorArchitecture(out bool isRunningOnArm)
        {
            var sysInfo = new NativeMethods.SYSTEM_INFO();
            NativeMethods.GetSystemInfo(ref sysInfo);
            ProcessorArchitecture result;
            isRunningOnArm = false;
            switch (sysInfo.wProcessorArchitecture)
            {
                case NativeMethods.PROCESSOR_ARCHITECTURE_IA64:
                    result = ProcessorArchitecture.IA64;
                    break;
                case NativeMethods.PROCESSOR_ARCHITECTURE_AMD64:
                    result = ProcessorArchitecture.Amd64;
                    break;
                case NativeMethods.PROCESSOR_ARCHITECTURE_INTEL:
                    result = ProcessorArchitecture.X86;
                    break;
                case NativeMethods.PROCESSOR_ARCHITECTURE_ARM:
                    result = ProcessorArchitecture.None;
                    isRunningOnArm = true;
                    break;

                default:
                    result = ProcessorArchitecture.None;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Return true/false to indicate whether the processor architecture is ARM
        /// </summary>
        /// <returns></returns>
        internal static bool IsRunningOnProcessorArchitectureARM()
        {
#if CORECLR
            Architecture arch = RuntimeInformation.OSArchitecture;
            if (arch == Architecture.Arm || arch == Architecture.Arm64)
            {
                return true;
            }
            else
            {
                return false;
            }
#else
            // Important:
            // this functiona has a clone in Workflow.ServiceCore in admin\monad\src\m3p\product\ServiceCore\WorkflowCore\WorkflowRuntimeCompilation.cs
            // if you are making any changes specific to this function then update the clone as well.

            var sysInfo = new NativeMethods.SYSTEM_INFO();
            NativeMethods.GetSystemInfo(ref sysInfo);
            return sysInfo.wProcessorArchitecture == NativeMethods.PROCESSOR_ARCHITECTURE_ARM;
#endif
        }

        internal static string GetHostName()
        {
            // Note: non-windows CoreCLR does not support System.Net yet
            if (Platform.IsWindows)
            {
                return WinGetHostName();
            }
            else
            {
                return Platform.NonWindowsGetHostName();
            }
        }

        internal static string WinGetHostName()
        {
            System.Net.NetworkInformation.IPGlobalProperties ipProperties =
                System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

            string hostname = ipProperties.HostName;
            if (!String.IsNullOrEmpty(ipProperties.DomainName))
            {
                hostname = hostname + "." + ipProperties.DomainName;
            }

            return hostname;
        }

        internal static uint GetNativeThreadId()
        {
            if (Platform.IsWindows)
            {
                return WinGetNativeThreadId();
            }
            else
            {
                return Platform.NonWindowsGetThreadId();
            }
        }

        internal static uint WinGetNativeThreadId()
        {
            return NativeMethods.GetCurrentThreadId();
        }

        private static class NativeMethods
        {
            // Important:
            // this clone has a clone in SMA in admin\monad\src\m3p\product\ServiceCore\WorkflowCore\WorkflowRuntimeCompilation.cs
            // if you are making any changes specific to this class then update the clone as well.

            internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
            internal const ushort PROCESSOR_ARCHITECTURE_ARM = 5;
            internal const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
            internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
            internal const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

            [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO
            {
                public ushort wProcessorArchitecture;
                public ushort wReserved;
                public uint dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public UIntPtr dwActiveProcessorMask;
                public uint dwNumberOfProcessors;
                public uint dwProcessorType;
                public uint dwAllocationGranularity;
                public ushort wProcessorLevel;
                public ushort wProcessorRevision;
            };

            [DllImport(PinvokeDllNames.GetSystemInfoDllName)]
            internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

            [DllImport(PinvokeDllNames.GetCurrentThreadIdDllName)]
            internal static extern uint GetCurrentThreadId();
        }

        #region ASTUtils

        /// <summary>
        /// This method is to get the unique key for a UsingExpressionAst. The key is a base64 
        /// encoded string based on the text of the UsingExpressionAst.
        /// 
        /// This method is used when handling a script block that contains $using for Invoke-Command.
        /// 
        /// When run Invoke-Command targetting a machine that runs PSv3 or above, we pass a dictionary
        /// to the remote end that contains the key of each UsingExpressionAst and its value. This method
        /// is used to generate the key.
        /// </summary>
        /// <param name="usingAst">A using expression</param>
        /// <returns>Base64 encoded string as the key of the UsingExpressionAst</returns>
        internal static string GetUsingExpressionKey(Language.UsingExpressionAst usingAst)
        {
            Diagnostics.Assert(usingAst != null, "Caller makes sure the parameter is not null");

            // We cannot call ToLowerInvariant unconditionally, because usingAst might 
            // contain IndexExpressionAst in its SubExpression, such as
            //   $using:bar["AAAA"]
            // and the index "AAAA" might not get us the same value as "aaaa".
            //
            // But we do want a unique key to represent the same UsingExpressionAst's as much
            // as possible, so as to avoid sending redundant key-value's to remote machine. 
            // As a workaround, we call ToLowerInvariant when the SubExpression of usingAst 
            // is a VariableExpressionAst, because:
            //   (1) Variable name is case insensitive;
            //   (2) People use $using to refer to a variable most of the time.
            string usingAstText = usingAst.ToString();
            if (usingAst.SubExpression is Language.VariableExpressionAst)
            {
                usingAstText = usingAstText.ToLowerInvariant();
            }
            return StringToBase64Converter.StringToBase64String(usingAstText);
        }

        #endregion ASTUtils

        #region EvaluatePowerShellDataFile

        /// <summary>
        /// Evaluate a powershell data file as if it's a module manifest
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="psDataFilePath"></param>
        /// <param name="context"></param>
        /// <param name="skipPathValidation"></param>
        /// <returns></returns>
        internal static Hashtable EvaluatePowerShellDataFileAsModuleManifest(
                                     string parameterName,
                                     string psDataFilePath,
                                     ExecutionContext context,
                                     bool skipPathValidation)
        {
            // Use the same capabilities as the module manifest
            // e.g. allow 'PSScriptRoot' variable
            return EvaluatePowerShellDataFile(
                      parameterName,
                      psDataFilePath,
                      context,
                      Microsoft.PowerShell.Commands.ModuleCmdletBase.PermittedCmdlets,
                      new[] { "PSScriptRoot" },
                      allowEnvironmentVariables: true,
                      skipPathValidation: skipPathValidation);
        }

        /// <summary>
        /// Get a Hashtable object out of a PowerShell data file (.psd1)
        /// </summary>
        /// <param name="parameterName">
        /// Name of the parameter that takes the specified .psd1 file as a value
        /// </param>
        /// <param name="psDataFilePath">
        /// Path to the powershell data file
        /// </param>
        /// <param name="context">
        /// ExecutionContext to use
        /// </param>
        /// <param name="allowedCommands">
        /// Set of command names that are allowed to use in the .psd1 file
        /// </param>
        /// <param name="allowedVariables">
        /// Set of variable names that are allowed to use in the .psd1 file
        /// </param>
        /// <param name="allowEnvironmentVariables">
        /// If true, allow to use environment variables in the .psd1 file
        /// </param>
        /// <param name="skipPathValidation">
        /// If true, caller guarantees the path is valid
        /// </param>
        /// <returns></returns>
        internal static Hashtable EvaluatePowerShellDataFile(
                                     string parameterName,
                                     string psDataFilePath,
                                     ExecutionContext context,
                                     IEnumerable<string> allowedCommands,
                                     IEnumerable<string> allowedVariables,
                                     bool allowEnvironmentVariables,
                                     bool skipPathValidation)
        {
            if (!skipPathValidation && string.IsNullOrEmpty(parameterName)) { throw PSTraceSource.NewArgumentNullException("parameterName"); }
            if (string.IsNullOrEmpty(psDataFilePath)) { throw PSTraceSource.NewArgumentNullException("psDataFilePath"); }
            if (context == null) { throw PSTraceSource.NewArgumentNullException("context"); }

            string resolvedPath;
            if (skipPathValidation)
            {
                resolvedPath = psDataFilePath;
            }
            else
            {
                #region "ValidatePowerShellDataFilePath"

                bool isPathValid = true;

                // File extension needs to be .psd1
                string pathExt = Path.GetExtension(psDataFilePath);
                if (string.IsNullOrEmpty(pathExt) ||
                    !StringLiterals.PowerShellDataFileExtension.Equals(pathExt, StringComparison.OrdinalIgnoreCase))
                {
                    isPathValid = false;
                }

                ProviderInfo provider;
                var resolvedPaths = context.SessionState.Path.GetResolvedProviderPathFromPSPath(psDataFilePath, out provider);

                // ConfigPath should be resolved as FileSystem provider
                if (provider == null || !Microsoft.PowerShell.Commands.FileSystemProvider.ProviderName.Equals(provider.Name, StringComparison.OrdinalIgnoreCase))
                {
                    isPathValid = false;
                }

                // ConfigPath should be resolved to a single path
                if (resolvedPaths.Count != 1)
                {
                    isPathValid = false;
                }

                if (!isPathValid)
                {
                    throw PSTraceSource.NewArgumentException(
                             parameterName,
                             ParserStrings.CannotResolvePowerShellDataFilePath,
                             psDataFilePath);
                }

                resolvedPath = resolvedPaths[0];

                #endregion "ValidatePowerShellDataFilePath"
            }

            #region "LoadAndEvaluatePowerShellDataFile"

            object evaluationResult;
            try
            {
                // Create the scriptInfo for the .psd1 file
                string dataFileName = Path.GetFileName(resolvedPath);
                var dataFileScriptInfo = new ExternalScriptInfo(dataFileName, resolvedPath, context);
                ScriptBlock scriptBlock = dataFileScriptInfo.ScriptBlock;

                // Validate the scriptblock
                scriptBlock.CheckRestrictedLanguage(allowedCommands, allowedVariables, allowEnvironmentVariables);

                // Evaluate the scriptblock
                object oldPsScriptRoot = context.GetVariableValue(SpecialVariables.PSScriptRootVarPath);
                try
                {
                    // Set the $PSScriptRoot before the evaluation
                    context.SetVariable(SpecialVariables.PSScriptRootVarPath, Path.GetDirectoryName(resolvedPath));
                    evaluationResult = PSObject.Base(scriptBlock.InvokeReturnAsIs());
                }
                finally
                {
                    context.SetVariable(SpecialVariables.PSScriptRootVarPath, oldPsScriptRoot);
                }
            }
            catch (RuntimeException ex)
            {
                throw PSTraceSource.NewInvalidOperationException(
                         ex,
                         ParserStrings.CannotLoadPowerShellDataFile,
                         psDataFilePath,
                         ex.Message);
            }

            var retResult = evaluationResult as Hashtable;
            if (retResult == null)
            {
                throw PSTraceSource.NewInvalidOperationException(
                         ParserStrings.InvalidPowerShellDataFile,
                         psDataFilePath);
            }

            #endregion "LoadAndEvaluatePowerShellDataFile"

            return retResult;
        }

        #endregion EvaluatePowerShellDataFile

        internal static readonly string[] ManifestModuleVersionPropertyName = new[] { "ModuleVersion" };
        internal static readonly string[] ManifestGuidPropertyName = new[] { "GUID" };
        internal static readonly string[] FastModuleManifestAnalysisPropertyNames = new[] { "AliasesToExport", "CmdletsToExport", "FunctionsToExport", "NestedModules", "RootModule", "ModuleToProcess", "ModuleVersion" };

        internal static Hashtable GetModuleManifestProperties(string psDataFilePath, string[] keys)
        {
            string dataFileContents = ScriptAnalysis.ReadScript(psDataFilePath);
            ParseError[] parseErrors;
            var ast = (new Parser()).Parse(psDataFilePath, dataFileContents, null, out parseErrors, ParseMode.ModuleAnalysis);
            if (parseErrors.Length > 0)
            {
                var pe = new ParseException(parseErrors);
                throw PSTraceSource.NewInvalidOperationException(
                    pe,
                    ParserStrings.CannotLoadPowerShellDataFile,
                    psDataFilePath,
                    pe.Message);
            }

            string unused1;
            string unused2;
            var pipeline = ast.GetSimplePipeline(false, out unused1, out unused2);
            if (pipeline != null)
            {
                var hashtableAst = pipeline.GetPureExpression() as HashtableAst;
                if (hashtableAst != null)
                {
                    var result = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in hashtableAst.KeyValuePairs)
                    {
                        var key = pair.Item1 as StringConstantExpressionAst;
                        if (key != null && keys.Contains(key.Value, StringComparer.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var val = pair.Item2.SafeGetValue();
                                result[key.Value] = val;
                            }
                            catch
                            {
                                throw PSTraceSource.NewInvalidOperationException(
                                         ParserStrings.InvalidPowerShellDataFile,
                                         psDataFilePath);
                            }
                        }
                    }
                    return result;
                }
            }

            throw PSTraceSource.NewInvalidOperationException(
                     ParserStrings.InvalidPowerShellDataFile,
                     psDataFilePath);
        }
    }

    /// <summary>
    /// This class provides helper methods for converting to/fro from
    /// string to base64string
    /// </summary>
    internal static class StringToBase64Converter
    {
        /// <summary>
        /// Converts string to base64 encoded string
        /// </summary>
        /// <param name="input">string to encode</param>
        /// <returns>base64 encoded string</returns>
        internal static string StringToBase64String(string input)
        {
            // NTRAID#Windows Out Of Band Releases-926471-2005/12/27-JonN
            // shell crashes if you pass an empty script block to a native command
            if (null == input)
            {
                throw PSTraceSource.NewArgumentNullException("input");
            }
            string base64 = Convert.ToBase64String
                            (
                                Encoding.Unicode.GetBytes(input.ToCharArray())
                            );
            return base64;
        }

        /// <summary>
        /// Decodes base64 encoded string
        /// </summary>
        /// <param name="base64">base64 string to decode</param>
        /// <returns>decoded string</returns>
        internal static string Base64ToString(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                throw PSTraceSource.NewArgumentNullException("base64");
            }
            string output = new string(Encoding.Unicode.GetChars(Convert.FromBase64String(base64)));
            return output;
        }

        /// <summary>
        /// Decodes base64 encoded string in to args array
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        internal static object[] Base64ToArgsConverter(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                throw PSTraceSource.NewArgumentNullException("base64");
            }
            string decoded = new string(Encoding.Unicode.GetChars(Convert.FromBase64String(base64)));

            //Deserialize string
            XmlReader reader = XmlReader.Create(new StringReader(decoded), InternalDeserializer.XmlReaderSettingsForCliXml);
            object dso;
            Deserializer deserializer = new Deserializer(reader);
            dso = deserializer.Deserialize();
            if (deserializer.Done() == false)
            {
                //This helper function should move to host and it should provide appropriate
                //error message there.
                throw PSTraceSource.NewArgumentException(MinishellParameterBinderController.ArgsParameter);
            }

            PSObject mo = dso as PSObject;
            if (mo == null)
            {
                //This helper function should move the host. Provide appropriate error message.
                //Format of args parameter is not correct.
                throw PSTraceSource.NewArgumentException(MinishellParameterBinderController.ArgsParameter);
            }

            var argsList = mo.BaseObject as ArrayList;
            if (argsList == null)
            {
                //This helper function should move the host. Provide appropriate error message.
                //Format of args parameter is not correct.
                throw PSTraceSource.NewArgumentException(MinishellParameterBinderController.ArgsParameter);
            }

            return argsList.ToArray();
        }
    }

    #region ReferenceEqualityComparer

    /// <summary>
    /// Equality comparer based on Object Identity
    /// </summary>
    internal class ReferenceEqualityComparer : IEqualityComparer
    {
        bool IEqualityComparer.Equals(object x, object y)
        {
            return Object.ReferenceEquals(x, y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            // The Object.GetHashCode and RuntimeHelpers.GetHashCode methods are used in the following scenarios:
            //
            // Object.GetHashCode is useful in scenarios where you care about object value. Two strings with identical
            // contents will return the same value for Object.GetHashCode.
            //
            // RuntimeHelpers.GetHashCode is useful in scenarios where you care about object identity. Two strings with
            // identical contents will return different values for RuntimeHelpers.GetHashCode, because they are different 
            // string objects, although their contents are the same.

            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    #endregion
}

