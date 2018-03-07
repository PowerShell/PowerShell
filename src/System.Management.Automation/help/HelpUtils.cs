// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace System.Management.Automation
{
    internal class HelpUtils
    {
        /// <summary>
        /// Get the path to $HOME
        /// </summary>
        internal static string GetUserHomeHelpSearchPath()
        {
            string homeFolder = null;

            if (Platform.IsWindows)
            {
                homeFolder = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                homeFolder = Environment.GetEnvironmentVariable("HOME");
            }

            var homeHelpFolder = Path.Combine(homeFolder, "PowerShellHelp");

            return homeHelpFolder;
        }
    }
}
