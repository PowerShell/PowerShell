// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace System.Management.Automation
{
    internal class HelpUtils
    {
        private static string userScopeRootPath = null;
        private static string userHomeHelpPath = null;


        /// <summary>
        /// Get the path to $HOME
        /// </summary>
        internal static string GetUserHomeHelpSearchPath()
        {
            if (userScopeRootPath == null)
            {
#if UNIX
                userScopeRootPath = Platform.SelectProductNameForDirectory(Platform.XDG_Type.USER_MODULES);
#else
                userScopeRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PowerShell");
#endif
            }

            if (userHomeHelpPath == null)
            {
                userHomeHelpPath = Path.Combine(userScopeRootPath, "Help");
            }

            return userHomeHelpPath;
        }
    }
}
