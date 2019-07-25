# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Parallel foreach syntax" -Tags "CI" {

    Context 'Generates an error on an arbitrary parameter' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            'foreach -bogus ($input in $bar) { }', [ref]$null, [ref]$errors)
        It '$errors.Count' { $errors.Count | Should -BeGreaterOrEqual 1 }
        It '$errors[0].ErrorId' { $errors[0].ErrorId | Should -BeExactly 'MissingOpenParenthesisAfterKeyword' }
    }

    Context 'Generate an error on -parallel' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            'foreach -parallel ($input in $bar) { }', [ref]$null, [ref]$errors)
        It '$errors.Count' { $errors.Count | Should -BeGreaterOrEqual 1 }
        It '$errors[0].ErrorId' { $errors[0].ErrorId | Should -BeExactly 'MissingOpenParenthesisAfterKeyword' }
    }
}