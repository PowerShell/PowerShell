# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Set-Date for admin" -Tag @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    # Fails in VSTS Linux with Operation not permitted
    It "Set-Date should be able to set the date in an elevated context" -Skip:(Test-IsVstsLinux) {
        { Get-Date | Set-Date } | Should -Not -Throw
    }

    # Fails in VSTS Linux with Operation not permitted
    It "Set-Date should be able to set the date with -Date parameter" -Skip:(Test-IsVstsLinux) {
        $target = Get-Date
        $expected = $target
        Set-Date -Date $target | Should -Be $expected
    }
}

Describe "Set-Date" -Tag 'CI' {
    It "Set-Date should produce an error in a non-elevated context" {
        { Get-Date | Set-Date } | Should -Throw -ErrorId "System.ComponentModel.Win32Exception,Microsoft.PowerShell.Commands.SetDateCommand"
    }
}
