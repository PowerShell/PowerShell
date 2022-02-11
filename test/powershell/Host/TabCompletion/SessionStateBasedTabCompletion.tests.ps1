# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

class MyPsClass {}
enum MyPsEnum {}

Describe "TabCompletion with session state info" -Tags CI {
    Context "Loaded PSTypes" {
        It 'Should complete <ScriptText>' -TestCases @(
            @{
                Expected = 'MyPsClass'
                ScriptText = '[mypsclas'
            }
            @{
                Expected = 'MyPsEnum'
                ScriptText = '[mypsenu'
            }
        ) -test {
            param ($Expected, $ScriptText)
            (TabExpansion2 -inputScript $ScriptText -cursorColumn $ScriptText.Length).CompletionMatches.CompletionText | Select-Object -First 1 | Should -Be $Expected
        }
    }
}