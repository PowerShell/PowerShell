# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Parallel foreach syntax' -Tags 'CI' {

    Context 'Should be able to retrieve AST of parallel foreach' {
        BeforeAll {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'foreach -parallel ($foo in $bar) {}', [ref]$null, [ref]$null)
        }

        It '$ast.EndBlock.Statements[0].Flags' {
            $ast.EndBlock.Statements[0].Flags | Should -BeExactly 'Parallel'
        }
    }

    Context 'Supports newlines before and after' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                "foreach `n-parallel `n(`$foo in `$bar) {}", [ref] $null, [ref] $null)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 0
        }

        It '$ast.EndBlock.Statements[0].Flags' {
            $ast.EndBlock.Statements[0].Flags | Should -BeExactly 'Parallel'
        }
    }

    Context 'Generates an error on invalid parameter' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'foreach -bogus ($input in $bar) { }', [ref]$null, [ref]$errors)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 1
        }

        It '$errors[0].ErrorId' {
            $errors[0].ErrorId | Should -BeExactly 'InvalidForeachFlag'
        }
    }

    Context 'Generate an error on -parallel' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'foreach -parallel ($input in $bar) { }', [ref]$null, [ref]$errors)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 1
        }

        It '$errors[0].ErrorId' {
            $errors[0].ErrorId | Should -Be 'KeywordParameterReservedForFutureUse'
        }
    }

    Context 'Generate an error on -throttlelimit' {
        BeforeAll {
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                'foreach -throttlelimit 2 ($input in $bar) { }', [ref]$null, [ref]$errors)
        }

        It '$errors.Count' {
            $errors.Count | Should -Be 2
        }

        It '$errors[0].ErrorId' {
            $errors[0].ErrorId | Should -Be 'KeywordParameterReservedForFutureUse'
        }

        It '$errors[1].ErrorId' {
            $errors[1].ErrorId | Should -Be 'ThrottleLimitRequiresParallelFlag'
        }
    }
}
