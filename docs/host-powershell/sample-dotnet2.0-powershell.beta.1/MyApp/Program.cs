// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Reflection;

namespace Application.Test
{
    public class Program
    {
        /// <summary>
        /// Managed entry point shim, which starts the actual program
        /// </summary>
        public static void Main(string[] args)
        {
            // Application needs to use PowerShell AssemblyLoadContext if it needs to create powershell runspace
            // PowerShell engine depends on PS ALC to provide the necessary assembly loading/searching support that is missing from .NET Core
            string appBase = System.IO.Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);
            System.Console.WriteLine("\nappBase: {0}", appBase);

            // Initialize the PS ALC and let it load 'Logic.dll' and start the execution
            System.Management.Automation.PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(appBase);
        }
    }
}
