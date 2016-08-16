/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
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
            // PowerShell has to set the ALC here, since we don't own the native host
            string appBase = System.IO.Path.GetDirectoryName(typeof(ManagedPSEntry).GetTypeInfo().Assembly.Location);
            return (int)PowerShellAssemblyLoadContextInitializer.
                           InitializeAndCallEntryMethod(
                               appBase,
                               new AssemblyName("Microsoft.PowerShell.ConsoleHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"),
                               "Microsoft.PowerShell.UnmanagedPSEntry",
                               "Start",
                               new object[] { string.Empty, args, args.Length });
#else
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
#endif
        }
    }
}
