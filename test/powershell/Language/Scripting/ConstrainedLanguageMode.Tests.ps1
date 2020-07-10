# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Test constrained language mode" -Tags "CI" {
    It "dynamic invocation on non-PowerShell thread should work" {
        $refAssemblies = @()
        if (!$IsCoreCLR) {
            $refAssemblies += "Microsoft.CSharp"
        }

        $t,$null = Add-Type -ReferencedAssemblies $refAssemblies -WarningAction Ignore -PassThru @"
        public class BinderBug$(Get-Date -Format FileDateTime)
        {
            public static object Test(System.Management.Automation.PSObject psobj)
            {
                // Invoke a method through PSObject, but with dynamic, so we get PowerShell's dynamic site binder involved
                // And we do this on a different thread so there is no ExecutionContext/runspace to check the language mode
                // The actual method called doesn't really matter.

                return System.Threading.Tasks.Task.Run(() => ((dynamic)psobj).AddCommand("Get-Command")).Result;
            }
        }
"@

        $o = [powershell]::Create()
        $t::Test($o) | Should -BeExactly $o

        try
        {
            # Set language mode to ConstrainedLanguage on a different runspace (so it doesn't affect this runspace)
            $ps = [powershell]::Create()
            $null = $ps.AddScript('$ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"')
            $null = $ps.Invoke()

            $t::Test($o) | Should -BeExactly $o
        }
        finally
        {
            $ps.Dispose()
        }
    }
}
