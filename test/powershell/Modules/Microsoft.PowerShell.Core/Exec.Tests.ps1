# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Switch-Process tests for Unix' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not [ExperimentalFeature]::IsEnabled('PSExec') -or $IsWindows)
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Exec alias should map to Switch-Process' {
        $alias = Get-Command exec
        $alias | Should -BeOfType [System.Management.Automation.AliasInfo]
        $alias.Definition | Should -BeExactly 'Switch-Process'
    }

    It 'Exec by itself does nothing' {
        exec | Should -BeNullOrEmpty
    }

    It 'Exec given a cmdlet should fail' {
        { exec Get-Command } | Should -Throw -ErrorId 'CommandNotFound,Microsoft.PowerShell.Commands.SwitchProcessCommand'
    }

    It 'Exec given an exe should work' {
        $id, $uname = pwsh -noprofile -noexit -outputformat text -command { $pid; exec uname }
        { Get-Process -Id $id -ErrorAction Stop } | Should -Throw -ErrorId 'NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand'
        $uname | Should -BeExactly (uname)
    }

    It 'Exec given an exe and arguments should work' {
        $id, $uname = pwsh -noprofile -noexit -outputformat text -command { $pid; exec uname -a }
        { Get-Process -Id $id -ErrorAction Stop } | Should -Throw -ErrorId 'NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand'
        $uname | Should -BeExactly (uname -a)
    }
}

Describe 'Switch-Process for Windows' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not $IsWindows)
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Switch-Process should not be available' {
        { Get-Command -Name Switch-Process } | Should -Throw -ErrorId 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
    }

    It 'Exec alias should not be available' {
        { Get-Alias -Name exec } | Should -Throw -ErrorId 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
    }
}
