# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
function Run-TestOnWinFull
{
    [CmdletBinding()]
    param( [string]$name )

    switch ($name)
    {
        "ActionPreference:ErrorAction=SuspendOnWorkflow" {
            workflow TestErrorActionSuspend { "Hello" }

            $r = TestErrorActionSuspend -ErrorAction Suspend

            $r | Should -BeExactly 'Hello'
            break;   }

        "ForeachParallel:ASTOfParallelForeachOnWorkflow" {
            Import-Module PSWorkflow
            $errors = @()
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(
        'workflow foo { foreach -parallel ($foo in $bar) {} }', [ref] $null, [ref] $errors)
            $errors.Count | Should -Be 0
            $ast.EndBlock.Statements[0].Body.EndBlock.Statements[0].Flags | Should -BeExactly 'Parallel'
            break;
            }
        default {
            #do nothing
        }

    }
}
