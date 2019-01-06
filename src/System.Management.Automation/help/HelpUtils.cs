// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Management.Automation.Help;
using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    internal class HelpUtils
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
            string newModuleBase = moduleBase;

            // In case of inbox modules, the help is put under $PSHOME/<current_culture>,
            // since the dlls are not published under individual module folders, but under $PSHome.
            // In case of other modules, the help is under moduleBase/<current_culture> or
            // under moduleBase/<Version>/<current_culture>.
            // The code below creates a similar layout for CurrentUser scope.
            // If the the scope is AllUsers, then the help goes under moduleBase.

            var userHelpPath = GetUserHomeHelpSearchPath();
            string moduleBaseParent = Directory.GetParent(moduleBase).Name;

            if (moduleBase.EndsWith(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                // This module is not an inbox module, so help goes under <userHelpPath>/<moduleName>
                newModuleBase = Path.Combine(userHelpPath, moduleName);
            }
            else if (string.Equals(moduleBaseParent, moduleName, StringComparison.OrdinalIgnoreCase))
            {
                // This module has version folder.
                var moduleVersion = Path.GetFileName(moduleBase);
                newModuleBase = Path.Combine(userHelpPath, moduleName, moduleVersion);
            }
            else
            {
                // This module is inbox module, help should be under <userHelpPath>
                newModuleBase = userHelpPath;
            }

            return newModuleBase;
        }
    }
}
