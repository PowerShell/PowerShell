# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Switch-Process tests for Unix' -Tags 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ($IsWindows)
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Exec function should map to Switch-Process' {
        $func = Get-Command exec
        $func | Should -BeOfType [System.Management.Automation.CommandInfo]
        $func.Definition | Should -Not -BeNullOrEmpty
    }

    It 'Exec by itself does nothing' {
        exec | Should -BeNullOrEmpty
    }

    It 'Exec given a cmdlet should fail' {
        { exec Get-Command } | Should -Throw -ErrorId 'CommandNotFound,Microsoft.PowerShell.Commands.SwitchProcessCommand'
    }

    It 'Exec given an exe should work' {
        $id, $uname = pwsh -noprofile -noexit -outputformat text -command { $pid; exec uname }
        Get-Process -Id $id -ErrorAction Ignore| Should -BeNullOrEmpty
        $uname | Should -BeExactly (uname)
    }

    It 'Exec given an exe and arguments should work' {
        $id, $uname = pwsh -noprofile -noexit -outputformat text -command { $pid; exec uname -a }
        Get-Process -Id $id -ErrorAction Ignore| Should -BeNullOrEmpty
        $uname | Should -BeExactly (uname -a)
    }

    It 'Exec will replace the process' {
        $sleep = Get-Command sleep -CommandType Application | Select-Object -First 1
        $p = Start-Process pwsh -ArgumentList "-noprofile -command exec $($sleep.Source) 90" -PassThru
        Wait-UntilTrue {
            ($p | Get-Process).Name -eq 'sleep'
        } -timeout 60000 -interval 100 | Should -BeTrue
    }

    It 'Error is returned if target command is not found' {
        $invalidCommand = 'doesNotExist'
        $e = { Switch-Process $invalidCommand } | Should -Throw -ErrorId 'CommandNotFound,Microsoft.PowerShell.Commands.SwitchProcessCommand' -PassThru
        $e.Exception.Message | Should -BeLike "*'$invalidCommand'*"
        $e.TargetObject | Should -BeExactly $invalidCommand
    }

    It 'The environment will be copied to the child process' {
        $env = pwsh -noprofile -outputformat text -command { $env:TEST_FOO='my test = value'; Switch-Process bash -c 'echo $TEST_FOO' }
        $env | Should -BeExactly 'my test = value'
    }

    It 'The command can include a -w parameter' {
        $out = pwsh -noprofile -outputformat text -command { exec /bin/echo 1 -w 2 }
        $out | Should -BeExactly '1 -w 2'
    }
}

Describe 'Switch-Process for Windows' -Tag 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (!$IsWindows)
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Switch-Process should not be available' {
        Get-Command -Name Switch-Process -ErrorAction Ignore | Should -BeNullOrEmpty
    }

    It 'Exec alias should not be available' {
        Get-Alias -Name exec -ErrorAction Ignore | Should -BeNullOrEmpty
    }
}
