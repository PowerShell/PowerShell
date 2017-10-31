/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Application.Test
{
    public sealed class Logic
    {
        /// <summary>
        /// Start the actual logic
        /// </summary>
        public static int Start(string[] args)
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
