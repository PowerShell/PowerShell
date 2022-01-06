# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Invoke-Command" -Tags "CI" {
    Context "StrictMode tests" {
        BeforeAll {
            $skipTest = !($EnabledExperimentalFeatures -contains "PSStrictModeAssignment");
            If (Test-Path Variable:InvokeCommand__Test) {
                Remove-Item Variable:InvokeCommand__Test
            }
        }

        It "Setting -StrictMode parameter with uninitialized variable throws error"  -skip:$skipTest {
            { Invoke-Command -StrictMode 3.0 {$InvokeCommand__Test} } | Should -Throw -ErrorId 'VariableIsUndefined'
        }

        It "Setting -StrictMode parameter with initialized variable does not throw error" -skip:$skipTest {
            $InvokeCommand__Test = 'Something'
            Invoke-Command -StrictMode 3.0 {$InvokeCommand__Test} | Should -Be 'Something'
            Remove-Item Variable:InvokeCommand__Test
        }

        It "-StrictMode parameter sets StrictMode back to original state after process completes" -skip:$skipTest {
            { Invoke-Command -StrictMode 3.0 {$InvokeCommand__Test} } | Should -Throw -ErrorId 'VariableIsUndefined'
            { Invoke-Command {$InvokeCommand__Test} } | Should -Not -Throw
        }

        It "-StrictMode parameter works on piped input" -skip:$skipTest {
            "There" | Invoke-Command -ScriptBlock { "Hello $input" } -StrictMode 3.0 | Should -Be 'Hello There'
            { "There" | Invoke-Command -ScriptBlock { "Hello $InvokeCommand__Test" } -StrictMode 3.0 } | Should -Throw -ErrorId 'VariableIsUndefined'
        }

        It "-StrictMode latest works" -skip:$skipTest {
            { Invoke-Command -StrictMode latest {$InvokeCommand__Test} } | Should -Throw -ErrorId 'VariableIsUndefined'
        }

        It "-StrictMode off works" -skip:$skipTest {
            { Invoke-Command -StrictMode off {$InvokeCommand__Test} } | Should -Not -Throw
        }
    }
}
