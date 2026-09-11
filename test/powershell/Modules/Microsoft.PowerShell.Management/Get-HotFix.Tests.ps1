# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-HotFix Tests" -Tag CI -Skip:(-not $IsWindows) {
    BeforeDiscovery {
        $hotfixSkip = $false
        if ($IsWindows -and (Test-IsWindowsArm64)) {
            # Win32Exception: Failed to load required native library 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\wminet_utils.dll'.
            Write-Verbose "needed provider not on ARM64, skipping tests" -Verbose
            $hotfixSkip = $true
        }
    }

    BeforeAll {
        $skip = $false
        if (Test-IsWindowsArm64) {
            $skip = $true
        }
        else {
            # skip the tests if there are no hotfixes returned
            $qfe = Get-CimInstance Win32_QuickFixEngineering
            if ($qfe.Count -eq 0) {
                $skip = $true
            }
        }
        $script:qfe = $qfe
        $script:hotfixRuntimeSkip = $skip
    }

    It "Get-HotFix will enumerate all QFEs" -Skip:$hotfixSkip {
        if ($script:hotfixRuntimeSkip) { Set-ItResult -Skipped -Because 'no hotfixes returned by provider' }
        $hotfix = Get-HotFix
        $hotfix.Count | Should -Be $script:qfe.Count
    }

    It "Get-HotFix can filter on -Id" -Skip:$hotfixSkip {
        if ($script:hotfixRuntimeSkip) { Set-ItResult -Skipped -Because 'no hotfixes returned by provider' }
        $testQfe = $script:qfe[0]

        $hotfix = Get-HotFix -Id $testQfe.HotFixID
        $hotfix.HotFixID | Should -BeExactly $testQfe.HotFixID
        $hotfix.Description | Should -BeExactly $testQfe.Description
    }

    It "Get-HotFix can filter on -Description" -Skip:$hotfixSkip {
        if ($script:hotfixRuntimeSkip) { Set-ItResult -Skipped -Because 'no hotfixes returned by provider' }
        $testQfes = $script:qfe | Where-Object { $_.Description -eq 'Update' }
        if ($testQfes.Count -gt 0) {
            $hotfixes = Get-HotFix -Description 'Update'
        }
        elseif ($script:qfe.Count -gt 0) {
            $description = $script:qfe[0].Description
            $testQfes = $script:qfe | Where-Object { $_.Description -eq $description }
            $hotfixes = Get-HotFix -Desscription $description
        }

        # if no applicable qfes are found on test system, this test will still
        # pass as both will have count 0

        $hotfixes.Count | Should -Be $testQfes.Count
    }

    It "Get-HotFix can use -ComputerName" -Skip:$hotfixSkip {
        if ($script:hotfixRuntimeSkip) { Set-ItResult -Skipped -Because 'no hotfixes returned by provider' }
        $hotfixes = Get-HotFix -ComputerName localhost
        $hotfixes.Count | Should -Be $script:qfe.Count
    }

    It "Get-Hotfix can accept ComputerName via pipeline" -Skip:$hotfixSkip {
        if ($script:hotfixRuntimeSkip) { Set-ItResult -Skipped -Because 'no hotfixes returned by provider' }
        { [PSCustomObject]@{ComputerName = 'UnavailableComputer'} | Get-HotFix } | Should -Throw -ErrorId '*,Microsoft.PowerShell.Commands.GetHotFixCommand'
        [PSCustomObject]@{ComputerName = 'localhost'} | Get-HotFix | Should -Not -BeNullOrEmpty
    }
}
