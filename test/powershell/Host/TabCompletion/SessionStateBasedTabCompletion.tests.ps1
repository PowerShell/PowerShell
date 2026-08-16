# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using type MyArrayList = System.Collections.ArrayList
using namespace MySysCol = System.Collections

Describe "TabCompletion with session state info" -Tags CI {
    Context "Using statement related tests" {
        It 'Should complete <ScriptText>' -TestCases @(
            @{
                Expected = 'MyArrayList'
                ScriptText = '[MyArrayLi'
            }
            @{
                Expected = 'MySysCol.Generic.List'
                ScriptText = '[MySysCol.Generic.Lis'
            }
        ) -test {
            param ($Expected, $ScriptText)
            (TabExpansion2 -inputScript $ScriptText -cursorColumn $ScriptText.Length).CompletionMatches.CompletionText | Select-Object -First 1 | Should -Be $Expected
        }

        It 'Should not complete <ScriptText>' -TestCases @(
            @{ScriptText = 'using type MyCustomType = System.Collections.Generic.List[string];[MyArrayLi'}
            @{ScriptText = 'using namespace MyCustomNamespace = System.Collections.Generic;MySysCo'}
        ) -test {
            param ($ScriptText)
            (TabExpansion2 -inputScript $ScriptText -cursorColumn $ScriptText.Length).CompletionMatches.CompletionText | Should -BeNullOrEmpty
        }
    }
}