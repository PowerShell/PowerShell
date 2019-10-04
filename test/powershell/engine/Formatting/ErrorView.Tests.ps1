# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Tests for $ErrorView' -Tag CI {

    It '$ErrorView is an enum' {
        $ErrorView | Should -BeOfType [System.Management.Automation.ErrorView]
    }

    It '$ErrorView should have correct default value' {
        $expectedDefault = 'NormalView'

        if ((Get-ExperimentalFeature -Name PSErrorView).Enabled) {
            $expectedDefault = 'ConciseView'
        }

        $ErrorView | Should -BeExactly $expectedDefault
    }

    Context 'ConciseView tests' {

        It 'Cmdlet error should be one line of text' {
            Get-Item (New-Guid) -ErrorVariable e -ErrorAction SilentlyContinue
            ($e | Out-String).Trim().Count | Should -Be 1
        }

        It 'Script error should contain path to script and line for error' {
            $testScript = @'
                [cmdletbinding()]
                param()
                $a = 1
                123)
                $b = 2
'@

            $testScriptPath = Join-Path -Path $TestDrive -ChildPath 'test.ps1'
            Set-Content -Path $testScriptPath -Value $testScript
            $e = { & $testScriptPath } | Should -Throw -ErrorId 'UnexpectedToken' -PassThru
            $e | Out-String | Should -BeLike "*$testScriptPath*"
            # validate line number is shown
            $e | Out-String | Should -BeLike '* 4 *'
        }

        It "Remote errors show up correctly" {
            Start-Job -ScriptBlock { get-item (new-guid) } | Wait-Job | Receive-Job -ErrorVariable e -ErrorAction SilentlyContinue
            ($e | Out-String).Trim().Count | Should -Be 1
        }
    }
}
