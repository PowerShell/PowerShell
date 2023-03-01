# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-HotFix Tests" -Tag CI {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        $skip = $false
        if (!$IsWindows) {
            $skip = $true
        }
        elseif (Test-IsWindowsArm64) {
            # Win32Exception: Failed to load required native library 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\wminet_utils.dll'.
            Write-Verbose "needed provider not on ARM64, skipping tests" -Verbose
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }
        else {
            # skip the tests if there are no hotfixes returned
            $qfe = Get-CimInstance Win32_QuickFixEngineering
            if ($qfe.Count -eq 0) {
                $skip = $true
            }
        }

        $PSDefaultParameterValues["it:skip"] = $skip
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Get-HotFix will enumerate all QFEs" {
        $hotfix = Get-HotFix
        $hotfix.Count | Should -Be $qfe.Count
    }

    It "Get-HotFix can filter on -Id" {
        $testQfe = $qfe[0]

        $hotfix = Get-HotFix -Id $testQfe.HotFixID
        $hotfix.HotFixID | Should -BeExactly $testQfe.HotFixID
        $hotfix.Description | Should -BeExactly $testQfe.Description
    }

    It "Get-HotFix can filter on -Description" {
        $testQfes = $qfe | Where-Object { $_.Description -eq 'Update' }
        if ($testQfes.Count -gt 0) {
            $hotfixes = Get-HotFix -Description 'Update'
        }
        elseif ($qfe.Count -gt 0) {
            $description = $qfe[0].Description
            $testQfes = $qfe | Where-Object { $_.Description -eq $description }
            $hotfixes = Get-HotFix -Desscription $description
        }

        # if no applicable qfes are found on test system, this test will still
        # pass as both will have count 0

        $hotfixes.Count | Should -Be $testQfes.Count
    }

    It "Get-HotFix can use -ComputerName" {
        $hotfixes = Get-HotFix -ComputerName localhost
        $hotfixes.Count | Should -Be $qfe.Count
    }

    It "Get-Hotfix can accept ComputerName via pipeline" {
        { [PSCustomObject]@{ComputerName = 'UnavailableComputer'} | Get-HotFix } | Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.GetHotFixCommand'
        [PSCustomObject]@{ComputerName = 'localhost'} | Get-HotFix | Should -Not -BeNullOrEmpty
    }
}
