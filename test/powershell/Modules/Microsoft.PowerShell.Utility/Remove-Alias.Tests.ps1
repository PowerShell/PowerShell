# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Remove-Alias" -Tags "CI" {

    BeforeAll {
        $testAliasName = (New-Guid).Guid
    }

    It "Remove-Alias should remove a non-readonly alias"{
        {
            Set-Alias -Name $testAliasName -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name $testAliasName -ErrorAction Stop
            Get-Alias -Name $testAliasName -ErrorAction Stop
        } | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw on a readonly alias"{
        {
            Set-Alias -Name $testAliasName -Value "Remove-Alias" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name $testAliasName -ErrorAction Stop
        } | Should -Throw -ErrorId 'AliasNotRemovable,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should remove a non-readonly alias with force"{
        {
            Set-Alias -Name $testAliasName -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name $testAliasName -Force -ErrorAction Stop
            Get-Alias -Name $testAliasName -ErrorAction Stop
        } | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should remove a readonly alias with force"{
        {
            Set-Alias -Name $testAliasName -Value "Remove-Alias" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name $testAliasName -Force -ErrorAction Stop
            Get-Alias -Name $testAliasName -ErrorAction Stop
        } | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw if alias does not exist"{
        {
            Get-Alias -Name $testAliasName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
            Remove-Alias -Name $testAliasName -ErrorAction Stop
        } | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should remove multiple alias at once"{
        {
            Set-Alias -Name "$testAliasName" -Value "Remove-Alias" -ErrorAction Stop
            Set-Alias -Name "$testAliasName-2" -Value "Remove-Alias" -ErrorAction Stop
            Set-Alias -Name "$testAliasName-3" -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name "$testAliasName","$testAliasName-2","$testAliasName-3" -ErrorAction Stop
            Get-Alias -Name "$testAliasName" -ErrorAction Stop | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
            Get-Alias -Name "$testAliasName-2" -ErrorAction Stop | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
            Get-Alias -Name "$testAliasName-3" -ErrorAction Stop | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
        }
    }

    It "Remove-Alias should throw on out-of-range scope"{
        {
            Set-Alias -Name $testAliasName -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name $testAliasName -Scope 99999 -ErrorAction Stop
        } | Should -Throw -ErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.RemoveAliasCommand"
    }
}
