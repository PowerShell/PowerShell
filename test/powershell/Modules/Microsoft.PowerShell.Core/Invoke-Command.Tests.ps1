# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Invoke-Command" -Tags "CI" {
    Context "StrictModeVersion tests" {
        BeforeAll {
            $errorMessage = "The variable '`$InvokeCommand__Test' cannot be retrieved because it has not been set."
            If (Test-Path Variable:InvokeCommand__Test) {
                Remove-Item Variable:InvokeCommand__Test
            }
        }

        It "Tests Invoke-Command with -StrictModeVersion parameter using uninitialized variable" {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
        }

        It "Tests Invoke-Command with -StrictModeVersion parameter using initialized variable" {
            $InvokeCommand__Test = 'Something'
            Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} | Should -Be 'Something'
            Remove-Item Variable:InvokeCommand__Test
        }

        It "Tests Invoke-Command with -StrictModeVersion parameter sets StrictMode back to original state" {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
            { Invoke-Command {$InvokeCommand__Test} } | Should -Not -Throw
        }
    }
}
