# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Policy-gated console startup" -Tag 'CI', 'NotWinPE' {
    BeforeAll {
        $canSimulatePolicy = $false
        if ($IsWindows) {
            $canSimulatePolicy = [System.Management.Automation.Security.SystemPolicy]::GetSystemLockdownPolicy().ToString() -eq 'None'
        }

        if ($canSimulatePolicy) {
            $hookAssembly = Join-Path $TestDrive 'StartupHook.dll'
            $hookSource = Join-Path $PSScriptRoot '..\..\tools\StartupPolicy\StartupHook.cs'
            Add-Type -Path $hookSource -OutputAssembly $hookAssembly -ErrorAction Stop
        }
    }

    It "Waits for <Policy> discovery before selecting the startup language modes" -TestCases @(
        @{ Policy = 'Enforce'; ExpectedInitialMode = 'ConstrainedLanguage'; ExpectedCommandMode = 'ConstrainedLanguage' }
        @{ Policy = 'Audit'; ExpectedInitialMode = 'FullLanguage'; ExpectedCommandMode = 'ConstrainedLanguage' }
        @{ Policy = 'None'; ExpectedInitialMode = 'FullLanguage'; ExpectedCommandMode = 'FullLanguage' }
    ) {
        param($Policy, $ExpectedInitialMode, $ExpectedCommandMode)

        if (-not $IsWindows) {
            Set-ItResult -Skipped -Because 'System lockdown policy is Windows-specific.'
            return
        }
        if (-not $canSimulatePolicy) {
            Set-ItResult -Skipped -Because 'Synthetic policy tests must not override an active machine policy.'
            return
        }

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new((Join-Path $PSHOME 'pwsh.exe'))
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardInput = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.Environment['DOTNET_STARTUP_HOOKS'] = $hookAssembly
        $startInfo.Environment['PWSH_POLICY_TEST_HOME'] = $PSHOME
        $startInfo.Environment['PWSH_POLICY_TEST_MODE'] = $Policy
        $startInfo.Environment['POWERSHELL_TELEMETRY_OPTOUT'] = '1'
        $startInfo.Environment['DOTNET_MultiCoreJitMinNumCpus'] = '1000'
        [void]$startInfo.Environment.Remove('__PSLockdownPolicy')
        foreach ($argument in @(
            '-nologo', '-noprofile', '-command',
            '"$env:PWSH_POLICY_TEST_COMPLETE|$($ExecutionContext.SessionState.LanguageMode)"'
        )) {
            $startInfo.ArgumentList.Add($argument)
        }

        $child = [System.Diagnostics.Process]::Start($startInfo)
        try {
            $errorTask = $child.StandardError.ReadToEndAsync()
            $heldTask = $child.StandardOutput.ReadLineAsync()
            $heldTask.Wait(30000) | Should -BeTrue
            $heldTask.GetAwaiter().GetResult() | Should -BeExactly '__POLICY_HELD__'

            $nextOutput = $child.StandardOutput.ReadLineAsync()
            $nextOutput.Wait(2000) | Should -BeFalse -Because 'the first command must remain blocked while policy discovery is held'

            $child.StandardInput.WriteLine('release')
            $child.StandardInput.Flush()
            $nextOutput.Wait(30000) | Should -BeTrue
            $nextOutput.GetAwaiter().GetResult() | Should -BeExactly '__POLICY_RELEASED__'

            $outputs = foreach ($index in 1..2) {
                $outputTask = $child.StandardOutput.ReadLineAsync()
                $outputTask.Wait(30000) | Should -BeTrue
                $outputTask.GetAwaiter().GetResult()
            }
            $outputs | Should -Contain "__INITIAL_LANGUAGE_MODE__|$ExpectedInitialMode"
            # Audit keeps FullLanguage built-ins, while the runtime uses audit-only ConstrainedLanguage.
            $outputs | Should -Contain "1|$ExpectedCommandMode"
            $child.WaitForExit(30000) | Should -BeTrue
            $child.ExitCode | Should -Be 0
            $errorTask.GetAwaiter().GetResult() | Should -BeNullOrEmpty
            [System.Management.Automation.Security.SystemPolicy]::GetSystemLockdownPolicy().ToString() | Should -BeExactly 'None'
        }
        finally {
            if (-not $child.HasExited) {
                $child.Kill($true)
                $child.WaitForExit()
            }
            $child.Dispose()
        }
    }
}
