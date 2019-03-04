// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.GlobalTool.Shim
{
    /// <summary>
    /// Shim layer to chose the appropriate runtime for PowerShell Core DotNet Global tool.
    /// </summary>
    public class EntryPoint
    {
        private const string PwshDllName = "pwsh.dll";

        private const string WinFolderName = "win";

        private const string UnixFolderName = "unix";

        /// <summary>
        /// Entry point for the global tool.
        /// </summary>
        /// <param name="args">Arguments passed to the global tool.</param>
        public static void Main(string[] args)
        {
            var currentPath = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string platformFolder = isWindows ? WinFolderName : UnixFolderName;

            string argsString = args.Length > 0 ? string.Join(" ", args) : null;
            var pwshPath = Path.Combine(currentPath, platformFolder, PwshDllName);
            string processArgs = string.IsNullOrEmpty(argsString) ? $"{pwshPath}" : $"{pwshPath} -c {argsString}";

            if (File.Exists(pwshPath))
            {
                System.Diagnostics.Process.Start("dotnet", processArgs).WaitForExit();
            }
            else
            {
                throw new FileNotFoundException(pwshPath);
            }
        }
    }
}
