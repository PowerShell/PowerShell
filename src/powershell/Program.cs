/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point for the .NET CLI "powershell" app
    /// </summary>
    public sealed class ManagedPSEntry
    {
        /// <summary>
        /// Starts the managed MSH
        /// </summary>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
        public static int Main(string[] args)
        {
#if CORECLR
            // Open PowerShell has to set the ALC here, since we don't own the native host
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(System.AppContext.BaseDirectory);
            var consoleHost = PowerShellAssemblyLoadContextInitializer.PSAsmLoadContext.LoadFromAssemblyName(new AssemblyName("Microsoft.PowerShell.ConsoleHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
            var unmanagedPSEntry = consoleHost.GetType("Microsoft.PowerShell.UnmanagedPSEntry", true);
            var start = unmanagedPSEntry.GetMethod("Start");
            return (int)start.Invoke(null, new object[] { string.Empty, args, args.Length });
#else
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
#endif
        }
    }
}
