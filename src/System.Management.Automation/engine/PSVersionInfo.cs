/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using Microsoft.Win32;

namespace System.Management.Automation
{
    /// <summary>
    /// Encapsulates $PSVersionTable.
    /// </summary>
    internal class PSVersionInfo
    {
        internal const string PSVersionTableName = "PSVersionTable";
        internal const string PSRemotingProtocolVersionName = "PSRemotingProtocolVersion";
        internal const string PSVersionName = "PSVersion";
        internal const string SerializationVersionName = "SerializationVersion";
        internal const string WSManStackVersionName = "WSManStackVersion";
        static private Hashtable _psVersionTable = null;

        /// <summary>
        /// A constant to track current PowerShell Version.
        /// </summary>
        /// <remarks>
        /// We can't depend on assembly version for PowerShell version.
        /// 
        /// This is why we hard code the PowerShell version here. 
        /// 
        /// For each later relase of PowerShell, this constant needs to 
        /// be updated to reflect the right version. 
        /// </remarks>
        static Version _psV1Version  = new Version(1, 0);
        static Version _psV2Version  = new Version(2, 0);
        static Version _psV3Version  = new Version(3, 0);
        static Version _psV4Version  = new Version(4, 0);
        static Version _psV5Version  = new Version(5, 0);
        static Version _psV51Version = new Version(5, 1, NTVerpVars.PRODUCTBUILD, NTVerpVars.PRODUCTBUILD_QFE);

        /// <summary>
        /// A constant to track current PowerShell Edition
        /// </summary>
        /// <remarks>
        /// Desktop -- "full" PowerShell that runs on Server and Desktop SKUs. Contains all features.
        /// Core -- Covers Nano Server and IoT SKUs since they are identical from a built-in feature and CLR perspective.
        /// Linux -- All PS on Linux flavors. This may need to be subdivided based on compatibility between distros.
        /// </remarks>
#if !CORECLR
        internal const string PSEditionValue = "WindowsPowerShell";
#else
        internal const string PSEditionValue = "PowerShellCore";
#endif

        // Static Constructor.
        static PSVersionInfo()
        {
            _psVersionTable = new Hashtable(StringComparer.OrdinalIgnoreCase);

            _psVersionTable[PSVersionInfo.PSVersionName] = _psV51Version;
            _psVersionTable["PSEdition"] = PSEditionValue;
            _psVersionTable["BuildVersion"] = GetBuildVersion();
            _psVersionTable["GitCommitId"] = GetCommitInfo();
            _psVersionTable["PSCompatibleVersions"] = new Version[] { _psV1Version, _psV2Version, _psV3Version, _psV4Version, _psV5Version, _psV51Version };
            _psVersionTable[PSVersionInfo.SerializationVersionName] = new Version(InternalSerializer.DefaultVersion);
            _psVersionTable[PSVersionInfo.PSRemotingProtocolVersionName] = RemotingConstants.ProtocolVersion;
            _psVersionTable[PSVersionInfo.WSManStackVersionName] = GetWSManStackVersion();
#if CORECLR
            _psVersionTable["CLRVersion"] = null;
#else
            _psVersionTable["CLRVersion"] = Environment.Version;
#endif
        }

        static internal Hashtable GetPSVersionTable()
        {
            return _psVersionTable;
        }

        static internal Version GetBuildVersion()
        {
            string assemblyPath = typeof(PSVersionInfo).GetTypeInfo().Assembly.Location;
            string buildVersion = FileVersionInfo.GetVersionInfo(assemblyPath).FileVersion;
            return new Version(buildVersion);
        }

        // Get the commit id from the powershell.version file. If the powershell.version file doesn't exist, use the string "N/A"
        static internal string GetCommitInfo()
        {
            try {
                string assemblyPath = IO.Path.GetDirectoryName(typeof(PSVersionInfo).GetTypeInfo().Assembly.Location);
                return (IO.File.ReadAllLines(IO.Path.Combine(assemblyPath,"powershell.version"))[0]);
            }
            catch (Exception e){
                return e.Message;
            }
        }

        #region Private helper methods

        // Gets the current WSMan stack version from the registry.
        private static Version GetWSManStackVersion()
        {
            Version version = null;

#if !UNIX
            try
            {
                using (RegistryKey wsManStackVersionKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\WSMAN"))
                {
                    if (wsManStackVersionKey != null)
                    {
                        object wsManStackVersionObj = wsManStackVersionKey.GetValue("ServiceStackVersion");
                        string wsManStackVersion = (wsManStackVersionObj != null) ? (string)wsManStackVersionObj : null;
                        if (!string.IsNullOrEmpty(wsManStackVersion))
                        {
                            version = new Version(wsManStackVersion.Trim());
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }
#endif

            return version ?? System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STACK_VERSION;
        }

        #endregion

        #region Programmer APIs

        static internal Version PSVersion
        {
            get
            {
                return (Version) GetPSVersionTable()[PSVersionInfo.PSVersionName];
            }
        }

        static internal Version CLRVersion
        {
            get
            {
                return (Version) GetPSVersionTable()["CLRVersion"];
            }
        }

        static internal Version BuildVersion
        {
            get
            {
                return (Version) GetPSVersionTable()["BuildVersion"];
            }
        }

        static internal Version[] PSCompatibleVersions
        {
            get
            {
                return (Version[]) GetPSVersionTable()["PSCompatibleVersions"];
            }
        }

        static internal string PSEdition
        {
            get
            {
                return (string)GetPSVersionTable()["PSEdition"];
            }
        }

        static internal Version SerializationVersion
        {
            get
            {
                return (Version) GetPSVersionTable()["SerializationVersion"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// For 2.0 PowerShell, we still use "1" as the registry version key.
        /// For >=3.0 PowerShell, we still use "1" as the registry version key for 
        /// Snapin and Custom shell lookup/discovery.
        /// </remarks>
        static internal string RegistryVersion1Key
        {
            get
            {
                return "1";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// For 3.0 PowerShell, we use "3" as the registry version key only for Engine
        /// related data like ApplicationBase.
        /// For 3.0 PowerShell, we still use "1" as the registry version key for 
        /// Snapin and Custom shell lookup/discovery. 
        /// </remarks>
        static internal string RegistryVersionKey
        {
            get
            {
                // PowerShell >=4 is compatible with PowerShell 3 and hence reg key is 3.
                return "3";
            }
        }


        static internal string GetRegisteryVersionKeyForSnapinDiscovery(string majorVersion)
        {
            int tempMajorVersion = 0;
            LanguagePrimitives.TryConvertTo<int>(majorVersion, out tempMajorVersion);

            if ((tempMajorVersion >= 1) && (tempMajorVersion <= PSVersionInfo.PSVersion.Major))
            {
                // PowerShell version 3 took a dependency on CLR4 and went with:
                // SxS approach in GAC/Registry and in-place upgrade approach for
                // FileSystem.
                // For >=3.0 PowerShell, we still use "1" as the registry version key for
                // Snapin and Custom shell lookup/discovery.
                return "1";
            }

            return null;
        }

        static internal string FeatureVersionString
        {
            get
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", PSVersionInfo.PSVersion.Major, PSVersionInfo.PSVersion.Minor);
            }
        }

        static internal bool IsValidPSVersion(Version version)
        {
            if (version.Major == _psV5Version.Major)
            {
                return (version.Minor == _psV5Version.Minor || version.Minor == _psV51Version.Minor);
            }
            if (version.Major == _psV4Version.Major)
            {
                return (version.Minor == _psV4Version.Minor);
            }
            else if (version.Major == _psV3Version.Major)
            {
                return version.Minor == _psV3Version.Minor;
            }
            else if (version.Major == _psV2Version.Major)
            {
                return version.Minor == _psV2Version.Minor;
            }
            else if (version.Major == _psV1Version.Major)
            {
                return version.Minor == _psV1Version.Minor;
            }

            return false;
        }

        static internal Version PSV4Version
        {
            get { return _psV4Version; }
        }

        static internal Version PSV5Version
        {
            get { return _psV5Version; }
        }

        static internal Version PSV51Version
        {
            get { return _psV51Version; }
        }

        #endregion

    }
}
