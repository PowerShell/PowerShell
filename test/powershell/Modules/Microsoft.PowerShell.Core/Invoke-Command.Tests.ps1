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

        It "Setting -StrictModeVersion parameter with uninitialized variable throws error" {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
        }

        It "Setting -StrictModeVersion parameter with initialized variable does not throw error" {
            $InvokeCommand__Test = 'Something'
            Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} | Should -Be 'Something'
            Remove-Item Variable:InvokeCommand__Test
        }

        It "-StrictModeVersion parameter sets StrictMode back to original state after process completes" {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
            { Invoke-Command {$InvokeCommand__Test} } | Should -Not -Throw
        }

        It "-StrictModeVersion applies for piped input" {
            "There" | Invoke-Command -ScriptBlock { "Hello $input" } -StrictModeVersion Latest
        }
    }
}
