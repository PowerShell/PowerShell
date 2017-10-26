/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Management.Automation;
using System.Reflection;

namespace Application.Test
{
    public class Program
    {
        /// <summary>
        /// Managed entry point shim, which starts the actual program
        /// </summary>
        public static int Main(string[] args)
        {
            // Application needs to use PowerShell AssemblyLoadContext if it needs to create powershell runspace
            // PowerShell engine depends on PS ALC to provide the necessary assembly loading/searching support that is missing from .NET Core
            string appBase = System.IO.Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);
            System.Console.WriteLine("\nappBase: {0}", appBase);

            // Initialize the PS ALC and let it load 'Logic.dll' and start the execution
            return (int)PowerShellAssemblyLoadContextInitializer.
                           InitializeAndCallEntryMethod(
                               appBase,
                               new AssemblyName("Logic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                               "Application.Test.Logic",
                               "Start",
                               new object[] { args });
        }
    }
}
