# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for Wait-Debugger' -Tags "CI" {
    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context 'Wait-Debugger should break on the statement containing the Wait-Debugger command' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    Wait-Debugger
                    'The debugger should break on the previous line, not on this line.'
                }
                Test-Break
            }

            $results = @(Test-Debugger -Scriptblock $testScript)
        }

        It 'Should show 1 debugger command was invoked' {
            # There is always an implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 1
        }

        It 'The breakpoint should be the statement containing Wait-Debugger' {
            $results[0] | ShouldHaveExtent -Line 5 -FromColumn 21 -ToColumn 34
        }
    }
}
