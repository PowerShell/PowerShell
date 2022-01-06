# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Invoke-Command" -Tags "CI" {
    Context "StrictModeVersion tests" {
        BeforeAll {
            if ([ExperimentalFeature]::IsEnabled("PSStrictModeVersionAssignment")){
                $skipTest = $false
            }
            else {
                $skipTest = $true
            }
            $errorMessage = "The variable '`$InvokeCommand__Test' cannot be retrieved because it has not been set."
            If (Test-Path Variable:InvokeCommand__Test) {
                Remove-Item Variable:InvokeCommand__Test
            }
        }

        It "Setting -StrictModeVersion parameter with uninitialized variable throws error"  -skip:$skipTest {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
        }

        It "Setting -StrictModeVersion parameter with initialized variable does not throw error" -skip:$skipTest {
            $InvokeCommand__Test = 'Something'
            Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} | Should -Be 'Something'
            Remove-Item Variable:InvokeCommand__Test
        }

        It "-StrictModeVersion parameter sets StrictMode back to original state after process completes" -skip:$skipTest {
            { Invoke-Command -StrictModeVersion 3.0 {$InvokeCommand__Test} } | Should -Throw $errorMessage
            { Invoke-Command {$InvokeCommand__Test} } | Should -Not -Throw
        }

        It "-StrictModeVersion parameter works on piped input" -skip:$skipTest {
            "There" | Invoke-Command -ScriptBlock { "Hello $input" } -StrictModeVersion 3.0 | Should -Be 'Hello There'
            { "There" | Invoke-Command -ScriptBlock { "Hello $InvokeCommand__Test" } -StrictModeVersion 3.0 } | Should -Throw $errorMessage
        }

    }
}
