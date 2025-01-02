# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Set-Date for admin" -Tag @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("SetDate", $true)
    }
    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("SetDate", $false)
    }

    It "Set-Date should be able to set the date in an elevated context" {
        { Get-Date | Set-Date } | Should -Not -Throw
    }

    # Check the individual properties as the types may be different
    It "Set-Date should be able to set the date with -Date parameter" {
        $target = Get-Date
        $expected = $target
        $observed = Set-Date -Date $target
        # do not test dayofweek because the number of the day is different between native and managed
        $observed.Day | Should -Be $expected.Day
        $observed.Hour | Should -Be $expected.Hour
        $observed.Minutes | Should -Be $expected.Minutes
        $observed.Month | Should -Be $expected.Month
        $observed.Second | Should -Be $expected.Second
        $observed.Year | Should -Be $expected.Year
    }
}

Describe "Set-Date" -Tag 'CI' {
    It "Set-Date should produce an error in a non-elevated context" {
        if (Test-IsElevated) {
            Set-ItResult -Skipped -Because "must NOT be run as admin"
        }

        { Get-Date | Set-Date } | Should -Throw -ErrorId "System.ComponentModel.Win32Exception,Microsoft.PowerShell.Commands.SetDateCommand"
    }
}
