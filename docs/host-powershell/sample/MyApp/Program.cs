// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Application.Test
{
    public static class Program
    {
        /// <summary>
        /// Managed entry point shim, which starts the actual program.
        /// </summary>
        public static int Main(string[] args)
        {
            using (PowerShell ps = PowerShell.Create())
            {
                Console.WriteLine("\nEvaluating 'Get-Command Write-Output' in PS Core Runspace\n");
                var results = ps.AddScript("Get-Command Write-Output").Invoke();
                Console.WriteLine(results[0].ToString());

                ps.Commands.Clear();

                Console.WriteLine("\nEvaluating '([S.M.A.ActionPreference], [S.M.A.AliasAttribute]).FullName' in PS Core Runspace\n");
                results = ps.AddScript("([System.Management.Automation.ActionPreference], [System.Management.Automation.AliasAttribute]).FullName").Invoke();
                foreach (dynamic result in results)
                {
                    Console.WriteLine(result.ToString());
                }
            }

            return 0;
        }
    }
}
