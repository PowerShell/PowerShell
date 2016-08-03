/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// PSInstaller is a class for facilitating installation
    /// of monad engine and monad PSSnapin's. 
    /// 
    /// This class implements installer api from CLR. At install
    /// time, installation utilities (like InstallUtil.exe) will 
    /// call api implementation functions in this class automatically. 
    /// This includes functions like Install, Uninstall, Rollback
    /// and Commit. 
    /// 
    /// This class is an abstract class for handling installation needs
    /// that are common for all monad components, which include, 
    /// 
    ///     1. accessing system registry
    ///     2. support of additional command line parameters. 
    ///     3. writing registry files
    ///     4. automatically extract informaton like vender, version, etc.
    /// 
    /// Different monad component will derive from this class. Two common
    /// components that need install include, 
    /// 
    ///     1. PSSnapin. Installation of PSSnapin will require information 
    ///        about PSSnapin assembly, version, vendor, etc to be 
    ///        written to registry.
    /// 
    ///     2. Engine. Installation of monad engine will require information
    ///        about engine assembly, version, CLR information to be
    ///        written to registry. 
    /// 
    /// </summary>
    /// 
    /// <remarks>
    /// This is an abstract class to be derived by monad engine and PSSnapin installers
    /// only. Developer should not directly derive from this class. 
    /// </remarks>
    public abstract class PSInstaller : Installer
    {
        private static string[] MshRegistryRoots
        {
            get
            {
                // For 3.0 PowerShell, we use "3" as the registry version key only for Engine
                // related data like ApplicationBase.
                // For 3.0 PowerShell, we still use "1" as the registry version key for 
                // Snapin and Custom shell lookup/discovery. 
                return new string[] {
                    "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\PowerShell\\" + PSVersionInfo.RegistryVersion1Key + "\\"
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal abstract string RegKey
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        internal abstract Dictionary<String, object> RegValues
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateSaver"></param>
        public sealed override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            WriteRegistry();

            return;
        }

        private void WriteRegistry()
        {
            foreach (string root in MshRegistryRoots)
            {
                RegistryKey key = GetRegistryKey(root + RegKey);

                foreach (var pair in RegValues)
                {
                    key.SetValue(pair.Key, pair.Value);
                }
            }
        }

        private RegistryKey GetRegistryKey(string keyPath)
        {
            RegistryKey root = GetRootHive(keyPath);

            if (root == null)
                return null;

            return root.CreateSubKey(GetSubkeyPath(keyPath));
        }

        private static string GetSubkeyPath(string keyPath)
        {
            int index = keyPath.IndexOf('\\');
            if (index > 0)
            {
                return keyPath.Substring(index + 1);
            }

            return null;
        }

        private static RegistryKey GetRootHive(string keyPath)
        {
            string root;

            int index = keyPath.IndexOf('\\');
            if (index > 0)
            {
                root = keyPath.Substring(0, index);
            }
            else
            {
                root = keyPath;
            }

            switch (root.ToUpperInvariant())
            {
                case "HKEY_CURRENT_USER":
                    return Registry.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                    return Registry.LocalMachine;
                case "HKEY_CLASSES_ROOT":
                    return Registry.ClassesRoot;
                case "HKEY_CURRENT_CONFIG":
                    return Registry.CurrentConfig;
                case "HKEY_PERFORMANCE_DATA":
                    return Registry.PerformanceData;
                case "HKEY_USERS":
                    return Registry.Users;
            }

            return null;
        }

        /// <summary>
        /// Uninstall this msh component
        /// </summary>
        /// <param name="savedState"></param>
        public sealed override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            if (this.Context != null && this.Context.Parameters != null && this.Context.Parameters.ContainsKey("RegFile"))
            {
                string regFile = this.Context.Parameters["RegFile"];

                // If regfile is specified. Don't uninstall.
                if (!String.IsNullOrEmpty(regFile))
                    return;
            }

            string keyName;
            string parentKey;

            int index = RegKey.LastIndexOf('\\');
            if (index >= 0)
            {
                parentKey = RegKey.Substring(0, index);
                keyName = RegKey.Substring(index + 1);
            }
            else
            {
                parentKey = "";
                keyName = RegKey;
            }

            foreach (string root in MshRegistryRoots)
            {
                RegistryKey key = GetRegistryKey(root + parentKey);

                key.DeleteSubKey(keyName);
            }

            return;
        }

        /// <summary>
        /// Rollback this msh component
        /// </summary>
        /// <param name="savedState"></param>
        public sealed override void Rollback(IDictionary savedState)
        {
            Uninstall(savedState);
        }
    }
}
