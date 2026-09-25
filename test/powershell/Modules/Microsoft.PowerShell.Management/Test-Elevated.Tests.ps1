# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Test-Elevated cmdlet and $IsElevated variable' -Tags "CI" {
    BeforeAll {
        # Save the expected value
        $expectedElevated = [System.Environment]::IsPrivilegedProcess
    }

    Context '$IsElevated automatic variable' {
        It 'Should exist' {
            { Get-Variable IsElevated -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should be a boolean' {
            $IsElevated | Should -BeOfType [bool]
        }

        It 'Should be constant' {
            $var = Get-Variable IsElevated
            $var.Options | Should -Match 'Constant'
        }

        It 'Should be AllScope' {
            $var = Get-Variable IsElevated
            $var.Options | Should -Match 'AllScope'
        }

        It 'Should have the correct value' {
            $IsElevated | Should -Be $expectedElevated
        }

        It 'Should not be modifiable' {
            { Set-Variable IsElevated -Value $false -Force -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'Test-Elevated cmdlet' {
        It 'Should exist' {
            { Get-Command Test-Elevated -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should return a boolean' {
            $result = Test-Elevated
            $result | Should -BeOfType [bool]
        }

        It 'Should match $IsElevated variable' {
            $result = Test-Elevated
            $result | Should -Be $IsElevated
        }

        It 'Should have correct output type' {
            $cmd = Get-Command Test-Elevated
            $cmd.OutputType.Type.Name | Should -Be 'Boolean'
        }

        It 'Should work without parameters' {
            { Test-Elevated } | Should -Not -Throw
        }

        It 'Should have correct verb' {
            $cmd = Get-Command Test-Elevated
            $cmd.Verb | Should -Be 'Test'
        }

        It 'Should have correct noun' {
            $cmd = Get-Command Test-Elevated
            $cmd.Noun | Should -Be 'Elevated'
        }
    }

    Context 'Integration tests' {
        It 'Both should return the same value' {
            $cmdletResult = Test-Elevated
            $variableValue = $IsElevated
            $cmdletResult | Should -Be $variableValue
        }

        It 'Both should match the expected value' {
            $cmdletResult = Test-Elevated
            $variableValue = $IsElevated
            $cmdletResult | Should -Be $expectedElevated
            $variableValue | Should -Be $expectedElevated
        }
    }
}
