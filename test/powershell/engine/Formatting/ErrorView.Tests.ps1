# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Tests for `$ErrorView" -Tag CI {

    It "`$ErrorView is an enum" {
        $ErrorView | Should -BeOfType [System.Management.Automation.ErrorView]
    }

    It "`$ErrorView should have correct default value" {
        $expectedDefault = "NormalView"

        if ((Get-ExperimentalFeature -Name PSErrorView).IsEnabled) {
            $expectedDefault = "ConciseView"
        }

        $ErrorView | Should -BeExactly $expectedDefault
    }

    Context "ConciseView tests" {

        It "Cmdlet error should be one line of text" {
            Get-Item (New-Guid) -ErrorVariable e
            ($e | Out-String).Trim().Count | Should -Be 1
        }

        It "Script error should contain path to script and line for error" {
            $testScript = @'
                $a = 1
                123)
                $b = 2
'@

            $testScriptPath = Join-Path -Path $TestDrive -ChildPath "test.ps1"
            Set-Content -Path $testScriptPath -Value $testScriptPath
            & $testScriptPath | Out-String | Should -Contain $testScriptPath
        }
    }
}
