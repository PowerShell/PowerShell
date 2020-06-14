# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Parallel switch syntax' -Tags 'CI' {

    Context 'Should be able to retrieve AST of parallel switch' {
        BeforeAll {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'switch -parallel ($foo) {1 {break}}', [ref] $null, [ref] $null)
        }

        It '$ast.EndBlock.Statements[0].Flags' {
            $ast.EndBlock.Statements[0].Flags | Should -BeExactly 'Parallel'
        }
    }

    Context 'Generates an error on invalid parameter' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'switch -bogus ($foo) {1 {break}}', [ref]$null, [ref]$errors)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 1
        }

        It '$errors[0].ErrorId' {
            $errors[0].ErrorId | Should -BeExactly 'InvalidSwitchFlag'
        }
    }

    Context 'Generate an error on -parallel' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'switch -parallel ($foo) {1 {break}}', [ref]$null, [ref]$errors)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 1
        }

        It '$errors[0].ErrorId' {
            $errors[0].ErrorId | Should -Be 'KeywordParameterReservedForFutureUse'
        }
    }
}
