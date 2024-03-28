// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.GlobalTool.Shim
{
    /// <summary>
    /// Shim layer to chose the appropriate runtime for PowerShell DotNet Global tool.
    /// </summary>
    public static class EntryPoint
    {
        private const string PwshDllName = "pwsh.dll";

        private const string WinFolderName = "win";

        private const string UnixFolderName = "unix";

        /// <summary>
        /// Entry point for the global tool.
        /// </summary>
        /// <param name="args">Arguments passed to the global tool.</param>'
        /// <returns>Exit code returned by pwsh.</returns>
        public static int Main(string[] args)
        {
            var currentPath = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            var isWindows = OperatingSystem.IsWindows();

            string platformFolder = isWindows ? WinFolderName : UnixFolderName;

            var arguments = new List<string>(args.Length + 1);
            var pwshPath = Path.Combine(currentPath, platformFolder, PwshDllName);
            arguments.Add(pwshPath);
            arguments.AddRange(args);

            if (File.Exists(pwshPath))
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                };

                var process = System.Diagnostics.Process.Start("dotnet", arguments);
                process.WaitForExit();
                return process.ExitCode;
            }
            else
            {
                throw new FileNotFoundException(pwshPath);
            }
        }
    }
}
