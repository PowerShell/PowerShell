// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Management.Automation.Help;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    internal static class HelpUtils
    {
        private static string userHomeHelpPath = null;

        /// <summary>
        /// Get the path to $HOME.
        /// </summary>
        internal static string GetUserHomeHelpSearchPath()
        {
            if (userHomeHelpPath == null)
            {
#if UNIX
                var userModuleFolder = Platform.SelectProductNameForDirectory(Platform.XDG_Type.USER_MODULES);
                string userScopeRootPath = System.IO.Path.GetDirectoryName(userModuleFolder);
#else
                string userScopeRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PowerShell");
#endif
                userHomeHelpPath = Path.Combine(userScopeRootPath, "Help");
            }

            return userHomeHelpPath;
        }

        internal static string GetModuleBaseForUserHelp(string moduleBase, string moduleName)
        {
            return GetModuleBaseForHelp(moduleBase, moduleName, GetUserHomeHelpSearchPath());
        }

        private static string allUsersHelpPath = null;

        /// <summary>
        /// When running as a packaged MSIX app with the per-machine data store provisioned, returns the
        /// writable AllUsers help root (under the store); otherwise null. Cached.
        /// </summary>
        internal static string GetAllUsersHomeHelpSearchPath()
        {
            if (allUsersHelpPath == null)
            {
                string store = Utils.GetPackagedMachineDataStorePath();
                allUsersHelpPath = string.IsNullOrEmpty(store) ? string.Empty : Path.Combine(store, "Help");
            }

            return string.IsNullOrEmpty(allUsersHelpPath) ? null : allUsersHelpPath;
        }

        /// <summary>
        /// Gets the AllUsers help destination for a module. When running as a packaged app this is under
        /// the per-machine data store (writable); otherwise it is the module base (historical behavior).
        /// </summary>
        internal static string GetModuleBaseForAllUsersHelp(string moduleBase, string moduleName)
        {
            string allUsersRoot = GetAllUsersHomeHelpSearchPath();
            if (string.IsNullOrEmpty(allUsersRoot))
            {
                // Not packaged / no store: keep the historical behavior (help goes under moduleBase).
                return moduleBase;
            }

            return GetModuleBaseForHelp(moduleBase, moduleName, allUsersRoot);
        }

        private static string GetModuleBaseForHelp(string moduleBase, string moduleName, string helpRoot)
        {
            string newModuleBase;

            // In case of inbox modules, the help is put under $PSHOME/<current_culture>,
            // since the dlls are not published under individual module folders, but under $PSHome.
            // In case of other modules, the help is under moduleBase/<current_culture> or
            // under moduleBase/<Version>/<current_culture>.
            // The code below creates a similar layout under the supplied help root.

            string moduleBaseParent = Directory.GetParent(moduleBase).Name;

            if (moduleBase.EndsWith(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                // This module is not an inbox module, so help goes under <helpRoot>/<moduleName>
                newModuleBase = Path.Combine(helpRoot, moduleName);
            }
            else if (string.Equals(moduleBaseParent, moduleName, StringComparison.OrdinalIgnoreCase))
            {
                // This module has version folder.
                var moduleVersion = Path.GetFileName(moduleBase);
                newModuleBase = Path.Combine(helpRoot, moduleName, moduleVersion);
            }
            else
            {
                // This module is inbox module, help should be under <helpRoot>
                newModuleBase = helpRoot;
            }

            return newModuleBase;
        }
    }
}
