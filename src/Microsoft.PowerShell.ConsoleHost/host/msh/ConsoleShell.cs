// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// This class provides an entry point which is called by minishell's main
    /// to transfer control to Msh console host implementation.
    /// </summary>

    public static
    class ConsoleShell
    {
        /// <summary>Entry point in to ConsoleShell. This method is called by main of minishell.</summary>
        /// <param name="bannerText">Banner text to be displayed by ConsoleHost.</param>
        /// <param name="helpText">Help text for minishell. This is displayed on 'minishell -?'.</param>
        /// <param name="args">Commandline parameters specified by user.</param>
        /// <returns>An integer value which should be used as exit code for the process.</returns>
        public static int Start(string bannerText, string helpText, string[] args)
        {
            if (args == null)
            {
                throw PSTraceSource.NewArgumentNullException("args");
            }

            return ConsoleHost.Start(bannerText, helpText, args);
        }
    }
}

