# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Simple debugger command tests' -tag 'CI' {

    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context 'Help (?, h) command should display the debugger help message' {
        BeforeAll {
            $testScript = {
                try {
                    $bp = Set-PSBreakpoint -Command Get-Process
                    & {
                        Get-Process -Id $PID
                    } > $null
                } finally {
                    Remove-PSBreakPoint -Breakpoint $bp
                }
            }

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue '?','h')
            $result? = if ($results.Count -gt 0) {$results[0].Output -join [Environment]::NewLine}
            $resulth = if ($results.Count -gt 1) {$results[1].Output -join [Environment]::NewLine}
        }

        It 'Should show 3 debugger commands were invoked' {
             # One extra for the implicit 'c' command that keeps the debugger automation moving
             $results.Count | Should -Be 3
        }

        It 'Should only have non-empty string output from the help command' {
            $results[0].Output | Should -BeOfType string
            $result? | Should -Match '\S'
        }

        It '''h'' and ''?'' should show identical help messages' {
            $result? | Should -BeExactly $resulth
        }

        It 'Should show help for stepInto' {$result? | Should -Match '\ss, stepInto\s+'}
        It 'Should show help for stepOver' {$result? | Should -Match '\sv, stepOver\s+'}
        It 'Should show help for stepOut' {$result? | Should -Match '\so, stepOut\s+'}
        It 'Should show help for continue' {$result? | Should -Match '\sc, continue\s+'}
        It 'Should show help for quit' {$result? | Should -Match '\sq, quit\s+'}
        It 'Should show help for detach' {$result? | Should -Match '\sd, detach\s+'}
        It 'Should show help for Get-PSCallStack' {$result? | Should -Match '\sk, Get-PSCallStack\s+'}
        It 'Should show help for list' {$result? | Should -Match '\sl, list\s+'}
        It 'Should show help for <enter>' {$result? | Should -Match '\s<enter>\s+'}
        It 'Should show help for help' {$result? | Should -Match '\s\?, h\s+'}
    }

    Context 'List (l, list) command should show the script and the current position' {
        BeforeAll {
            $testScript = {
                try {
                    $bp = Set-PSBreakpoint -Command Get-Process
                    & {
                        Get-Process -Id $PID
                    } > $null
                } finally {
                    Remove-PSBreakPoint -Breakpoint $bp
                }
            }

            $testScriptList = @'
    1:
    2:                  try {
    3:                      $bp = Set-PSBreakpoint -Command Get-Process
    4:                      & {
    5:*                         Get-Process -Id $PID
    6:                      } > $null
    7:                  } finally {
    8:                      Remove-PSBreakPoint -Breakpoint $bp
    9:                  }
   10:
'@

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 'l','list')
            $resultl = if ($results.Count -gt 0) {$results[0].Output -replace '\s+$' -join [Environment]::NewLine -replace "^[`r`n]+|[`r`n]+$"}
            $resultlist = if ($results.Count -gt 1) {$results[1].Output -replace '\s+$' -join [Environment]::NewLine -replace "^[`r`n]+|[`r`n]+$"}
        }

        It 'Should show 3 debugger commands were invoked' {
             # One extra for the implicit 'c' command that keeps the debugger automation moving
             $results.Count | Should -Be 3
        }

        It 'Should only have non-empty string output from the list command' {
            $results[0].Output | Should -BeOfType string
            $resultl | Should -Match '\S'
        }

        It '''l'' and ''list'' should show identical script listings' {
            $resultl | Should -BeExactly $resultlist
        }

        It 'Should show the entire script listing with the current position on line 5' {
            $resultl | Should -BeExactly $testScriptList
        }
    }

    Context 'Callstack (k, Get-PSCallStack) command should show the current call stack' {
        BeforeAll {
            $testScript = {
                try {
                    $bp = Set-PSBreakpoint -Command Get-Process
                    & {
                        Get-Process -Id $PID
                    } > $null
                } finally {
                    Remove-PSBreakPoint -Breakpoint $bp
                }
            }

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 'k','Get-PSCallStack')
            $resultk = if ($results.Count -gt 0) {$results[0].Output}
            $resultgcs = if ($results.Count -gt 1) {$results[1].Output}
        }

        It 'Should show 3 debugger commands were invoked' {
             # One extra for the implicit 'c' command that keeps the debugger automation moving
             $results.Count | Should -Be 3
        }

        It 'Should only have CallStackFrame output from the callstack command' {
            $results[0].Output | Should -BeOfType System.Management.Automation.CallStackFrame
        }

        It '''k'' and ''Get-PSCallStack'' should show identical script listings' {
            [string[]]$resultk -join [Environment]::NewLine | Should -BeExactly ([string[]]$resultgcs -join [Environment]::NewLine)
        }
    }

}

Describe 'Debugger stepping command tests' -tag 'CI' {

    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context 'StepInto steps into the current command if possible; otherwise it steps over the command' {
        BeforeAll {
            $testScript = {
                try {
                    $bp = Set-PSBreakpoint -Command ForEach-Object
                    $sb = {
                        'One fish, two fish'
                        'Red fish, blue fish'
                    }
                    & {
                        Get-Process -Id $PID | ForEach-Object -Process $sb
                    } *> $null
                } finally {
                    Remove-PSBreakPoint -Breakpoint $bp
                }
            }

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 's','s','s','s')
            $resultstepinto = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 'stepInto','stepInto','stepInto','stepInto')
        }

        It 'Should show 4 debugger commands were invoked twice' {
             # One extra for the implicit 'c' command that keeps the debugger automation moving
             $results.Count | Should -Be 5
             $resultstepinto.Count | Should -Be 5
        }

        It '''s'' and ''stepInto'' should have identical behaviour' {
            for ($i = 0; $i -lt 3; $i++) {
                Get-DebuggerExtent -DebuggerCommandResult $results[$i] | Should -Be (Get-DebuggerExtent -DebuggerCommandResult $resultstepinto[$i])
            }
        }

        It 'The first extent should be the statement containing ForEach-Object' {
            $results[0] | ShouldHaveExtent -Line 9 -FromColumn 25 -ToColumn 75
        }

        It 'The second extent should be in the nested scriptblock' {
            $results[1] | ShouldHaveExtent -Line 4 -FromColumn 27 -ToColumn 28
        }

        It 'The third extent should be on Write-Object' {
            $results[2] | ShouldHaveExtent -Line 5 -FromColumn 25 -ToColumn 45
        }

        It 'The fourth extent should be on ''Hello''' {
            $results[3] | ShouldHaveExtent -Line 6 -FromColumn 25 -ToColumn 46
        }
    }
}

        <#
        It '-ErrorAction Break enters the debugger on a terminating error' {
            $testScript = {
                & {
                    [CmdletBinding()]
                    param()
                    'Hello'
                    Get-Process -Id ([int]::MaxValue + 1)
                    'Goodbye'
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 1
            $results[0] | ShouldHaveExtent -Line 6 -FromColumn 21 -ToColumn 58
        }

        It '-ErrorAction Break does NOT enter the debugger on a naked rethrow' {
            $testScript = {
                & {
                    [CmdletBinding()]
                    param()
                    try {
                        'Hello'
                        Get-Process -Id ([int]::MaxValue) -ErrorAction Stop
                        'Goodbye'
                    } catch {
                        throw
                    }
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 1
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 76
        }

        It '-ErrorAction Break does enter the debugger on a new throw' {
            $testScript = {
                & {
                    [CmdletBinding()]
                    param()
                    try {
                        'Hello'
                        Get-Process -Id ([int]::MaxValue) -ErrorAction Stop
                        'Goodbye'
                    } catch {
                        throw $_
                    }
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 2
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 76
            $results[1] | ShouldHaveExtent -Line 10 -FromColumn 25 -ToColumn 33
        }

        It '-ErrorAction Break does NOT enter the debugger for errors inside a DebuggerHidden block' {
            $testScript = {
                & {
                    [System.Diagnostics.DebuggerHidden()]
                    [CmdletBinding()]
                    param()
                    1/0 # We shouldn't be able to break here with -ErrorAction Break
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 0
        }

        It '-ErrorAction Break does NOT enter the debugger for errors in a nested function context under a DebuggerHidden block' {
            $testScript = {
                & {
                    [System.Diagnostics.DebuggerHidden()]
                    [CmdletBinding()]
                    param()
                    & {
                        [CmdletBinding()]
                        param()
                        1/0 # We shouldn't be able to break here with -ErrorAction Break
                    }
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 0
        }

        It '-ErrorAction Break does NOT enter the debugger for errors inside a DebuggerStepThrough block' {
            $testScript = {
                & {
                    [System.Diagnostics.DebuggerStepThrough()]
                    [CmdletBinding()]
                    param()
                    1/0 # We shouldn't be able to break here with -ErrorAction Break
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 0
        }

        It '-ErrorAction Break does enter the debugger for in a nested function context under a DebuggerStepThrough block' {
            $testScript = {
                & {
                    [System.Diagnostics.DebuggerStepThrough()]
                    [CmdletBinding()]
                    param()
                    & {
                        [CmdletBinding()]
                        param()
                        1/0 # We should be able to break here with -ErrorAction Break
                    }
                } -ErrorAction Break
            }

            $results = Test-Debugger -ScriptBlock $testScript
            $results.Count | Should -Be 1
            $results[0] | ShouldHaveExtent -Line 9 -FromColumn 25 -ToColumn 28
        }
        #>
