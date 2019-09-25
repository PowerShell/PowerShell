# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'NullConditionalOperations' -tag 'CI' {

    Context "Null conditional assignment operator ??=" {
        BeforeAll {
            $someGuid = New-Guid

            $typesTests = @(
                @{ name = 'string'; valueToSet = 'hello' }
                @{ name = 'dotnetType'; valueToSet = $someGuid }
                @{ name = 'byte'; valueToSet = [byte]0x94 }
                @{ name = 'intArray'; valueToSet = 1..2 }
                @{ name = 'stringArray'; valueToSet = 'a'..'c' }
                @{ name = 'emptyArray'; valueToSet = @(1, 2, 3) }
            )

        }

        It 'Variable doesnot exist' {
            Remove-Variable variableDoesNotExist -ErrorAction SilentlyContinue -Force

            $variableDoesNotExist ??= 1
            $variableDoesNotExist | Should -Be 1

            $variableDoesNotExist ??= 2
            $variableDoesNotExist | Should -Be 1
        }

        It 'Variable exists and is null' {
            $variableDoesNotExist = $null

            $variableDoesNotExist ??= 2
            $variableDoesNotExist | Should -Be 2
        }

        It 'Validate types - <name> can be set' -TestCases $typesTests {
            param ($name, $valueToSet)

            $x = $null
            $x ??= $valueToSet
            $x | Should -Be $valueToSet
        }

        It 'Validate hashtable can be set' {
            $x = $null
            $x ??= @{ 1 = '1' }
            $x.Keys | Should -Be @(1)
        }

        It 'Validate lhs is returned' {
            $x = 100
            $x ??= 200
            $x | Should -Be 100
        }

        It 'Error case' {
            $e = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput('1 ??= 100', [ref] $null, [ref] $e)
            $e[0].ErrorId | Should -BeExactly 'InvalidLeftHandSide'
        }
    }

    Context 'Null coalesce operator ??' {
        BeforeEach {
            $x = $null
        }

        It 'Variable does not exist' {
            $variableDoesNotExist ?? 100 | Should -Be 100
        }

        It 'Variable exists but is null' {
            $x = $null
            $x ?? 100 | Should -Be 100
        }

        It 'Lhs is not null' {
            $x = 100
            $x ?? 200 | Should -Be 100
        }

        It 'Lhs is a non-null constant' {
            1 ?? 2 | Should -Be 1
        }

        It 'Lhs is `$null' {
            $null ?? 'string value' | Should -BeExactly 'string value'
        }

        It 'Check precedence of ?? expression resolution' {
            $x ?? $null ?? 100 | Should -Be 100
            $null ?? $null ?? 100 | Should -Be 100
            $null ?? $null ?? $null | Should -Be $null
            $x ?? 200 ?? $null | Should -Be 200
            $x ?? 200 ?? 300 | Should -Be 200
            100 ?? $x ?? 200 | Should -Be 100
            $null ?? 100 ?? $null ?? 200 | Should -Be 100
        }
    }

    Context 'Combined usage of null conditional operators' {

        It '?? and ??= used together' {
            $x ??= 100 ?? 200
            $x | Should -Be 100

            $y ??= 100 ?? 200
            $y | Should -Be 100
        }
    }
}
