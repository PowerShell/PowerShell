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
        /// Get the path to the user's PowerShell Help directory (a 'Help' subdirectory of the PSContentPath).
        /// </summary>
        /// <remarks>
        /// This path is cached for performance, but updated if the PSContentPath changes.
        /// This ensures it reflects changes to the PSContentPath experimental feature or config.
        /// </remarks>
        internal static string GetUserHomeHelpSearchPath()
        {
            string expectedPath = Path.Combine(Utils.GetPSContentPath(), "Help");
            
            // Update cache if path changed or not yet initialized
            if (userHomeHelpPath != expectedPath)
            {
                userHomeHelpPath = expectedPath;
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
            // If the scope is AllUsers, then the help goes under moduleBase.

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
