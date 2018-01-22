/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;

namespace Application.Test
{
    public class Program
    {
        /// <summary>
        /// Managed entry point shim, which starts the actual program
        /// </summary>
        public static int Main(string[] args)
        {
            using (PowerShell ps = PowerShell.Create())
            {
                Console.WriteLine("\nEvaluating 'Get-Command Write-Output' in PS Core Runspace\n");
                var results = ps.AddScript("Get-Command Write-Output").Invoke();
                Console.WriteLine(results[0].ToString());
            }
            return 0;
        }
    }
}
