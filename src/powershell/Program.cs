/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;

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
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(string.Empty);
#endif
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
        }
    }
}
