# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Tests for (error, warning, etc) action preference" -Tags "CI" {
        BeforeAll {
            $orgin = $GLOBAL:errorActionPreference
        }

        AfterAll {
            if ($GLOBAL:errorActionPreference -ne $orgin)
            {
                $GLOBAL:errorActionPreference = $orgin
            }
        }
        Context 'Setting ErrorActionPreference to stop prevents user from getting the error exception' {
            $err = $null
            try
            {
                get-childitem nosuchfile.nosuchextension -ErrorAction stop -ErrorVariable err
            }
            catch {}

            It '$err.Count' { $err.Count | Should -Be 1 }
            It '$err[0] should not be $null' { $err[0] | Should -Not -BeNullOrEmpty }
            It '$err[0].GetType().Name' { $err[0] | Should -BeOfType "System.Management.Automation.ActionPreferenceStopException" }
            It '$err[0].ErrorRecord' { $err[0].ErrorRecord | Should -Not -BeNullOrEmpty }
            It '$err[0].ErrorRecord.Exception.GetType().Name' { $err[0].ErrorRecord.Exception | Should -BeOfType "System.Management.Automation.ItemNotFoundException" }
        }

        It 'ActionPreference Ignore Works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ignore

            $error.Count | Should -BeExactly $errorCount
        }

        It 'action preference of Ignore cannot be set as a preference variable' {
            $e = {
                $GLOBAL:errorActionPreference = "Ignore"
                Get-Process -Name asdfasdfasdf
            } | Should -Throw -ErrorId 'System.NotSupportedException' -PassThru
            $e.CategoryInfo.Reason | Should -BeExactly 'NotSupportedException'

            $GLOBAL:errorActionPreference = $orgin
        }

        It 'action preference of Suspend cannot be set as a preference variable' {
            $e = {
                $GLOBAL:errorActionPreference = "Suspend"
                Get-Process -Name asdfasdfasdf
            } | Should -Throw -ErrorId 'RuntimeException' -PassThru
            $e.CategoryInfo.Reason | Should -BeExactly 'ArgumentTransformationMetadataException'

            $GLOBAL:errorActionPreference = $orgin
        }

        It 'enum disambiguation works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

            $error.Count | Should -BeExactly $errorCount
        }

        It 'ErrorAction = Suspend works on Workflow' -Skip:$IsCoreCLR {
           . .\TestsOnWinFullOnly.ps1
            Run-TestOnWinFull "ActionPreference:ErrorAction=SuspendOnWorkflow"
        }

        It 'ErrorAction = Suspend does not work on functions' {
            function MyHelperFunction {
                [CmdletBinding()]
                param()
                "Hello"
            }

            { MyHelperFunction -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,MyHelperFunction"
        }

        It 'ErrorAction = Suspend does not work on cmdlets' {
            { Get-Process -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        It 'WarningAction = Suspend does not work' {
            { Get-Process -WarningAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        #issue 2076
        It 'ErrorAction and WarningAction are the only action preferences do not support suspend' -Pending{
            $params = [System.Management.Automation.Internal.CommonParameters].GetProperties().Name | Select-String Action

            $suspendErrors = $null
            $num=0

            $params | ForEach-Object {
                        $input=@{'InputObject' = 'Test';$_='Suspend'}
                        { Write-Output @input } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand"
                    }
        }

        It '<switch> does not take precedence over $ErrorActionPreference' -TestCases @(
            @{switch="Verbose"},
            @{switch="Debug"}
        ) {
            param($switch)
            $ErrorActionPreference = "SilentlyContinue"
            $params = @{
                ItemType = "File";
                Path = "$testdrive\test.txt";
                Confirm = $false
            }
            New-Item @params > $null
            $params += @{$switch=$true}
            { New-Item @params } | Should -Not -Throw
            $ErrorActionPreference = "Stop"
            { New-Item @params } | Should -Throw -ErrorId "NewItemIOError,Microsoft.PowerShell.Commands.NewItemCommand"
            Remove-Item "$testdrive\test.txt" -Force
        }
}

Describe 'ActionPreference.Break tests' -tag 'CI' {

    BeforeAll {
        Register-DebuggerHandler
    }

    AfterAll {
        Unregister-DebuggerHandler
    }

    Context '-ErrorAction Break should break on a non-terminating error' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        # Generate a non-terminating error
                        Write-Error 'This is a non-terminating error.'
                        # Do something afterwards
                        'This should still run'
                    } catch {
                        'Do nothing'
                    } finally {
                        'This finally runs'
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 'v', 'v')
        }

        It 'Should show 3 debugger commands were invoked' {
            # There is always an implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 3
        }

        It 'The breakpoint should be the statement that generated the non-terminating error' {
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 71
        }

        It 'The second statement should be the statement after that which generated the non-terminating error' {
            $results[1] | ShouldHaveExtent -Line 9 -FromColumn 25 -ToColumn 48
        }

        It 'The third statement should be the statement in the finally block' {
            $results[2] | ShouldHaveExtent -Line 13 -FromColumn 25 -ToColumn 44
        }
    }

    Context '-ErrorAction Break should break on a terminating error' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        # Generate a terminating error
                        Get-Process -TheAnswer 42
                        # Do something afterwards
                        'This should not run'
                    } catch {
                        'Do nothing'
                    } finally {
                        'This finally runs'
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -ScriptBlock $testScript -CommandQueue 'v', 'v')
        }

        It 'Should show 3 debugger commands were invoked' {
            # There is always an implicit 'c' command that keeps the debugger automation moving
            $results.Count | Should -Be 3
        }

        It 'The breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 7 -FromColumn 25 -ToColumn 50
        }

        It 'The second statement should be the statement in the catch block where the terminating error is caught' {
            $results[1] | ShouldHaveExtent -Line 11 -FromColumn 25 -ToColumn 37
        }

        It 'The third statement should be the statement in the finally block' {
            $results[2] | ShouldHaveExtent -Line 13 -FromColumn 25 -ToColumn 44
        }
    }

    Context '-ErrorAction Break should not break on a naked rethrow' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        try {
                            # Generate a terminating error
                            Get-Process -TheAnswer 42
                        } catch {
                            throw
                        }
                    } catch {
                        # Swallow the exception here
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -ScriptBlock $testScript)
        }

        It 'Should show 1 debugger command was invoked' {
            # ErrorAction break should only trigger on the initial terminating error
            $results.Count | Should -Be 1
        }

        It 'The breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 8 -FromColumn 29 -ToColumn 54
        }
    }

    Context '-ErrorAction Break should break when throwing a specific error or object' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    try {
                        try {
                            # Generate a terminating error
                            Get-Process -TheAnswer 42
                        } catch {
                            throw $_
                        }
                    } catch {
                        # Swallow the exception here
                    }
                }
                Test-Break -ErrorAction Break
            }

            $results = @(Test-Debugger -ScriptBlock $testScript)
        }

        It 'Should show 2 debugger commands were invoked' {
            # ErrorAction break should trigger on the initial terminating error and the throw
            # since it throws a "new" error (throwing anything is considered a new terminating
            # error)
            $results.Count | Should -Be 2
        }

        It 'The first breakpoint should be the statement that generated the terminating error' {
            $results[0] | ShouldHaveExtent -Line 8 -FromColumn 29 -ToColumn 54
        }

        It 'The second breakpoint should be the statement that threw $_' {
            $results[1] | ShouldHaveExtent -Line 10 -FromColumn 29 -ToColumn 37
        }
    }

    Context 'Other message types should break on their corresponding messages when requested' {
        BeforeAll {
            $testScript = {
                function Test-Break {
                    [CmdletBinding()]
                    param()
                    Write-Warning -Message 'This is a warning message'
                    Write-Verbose -Message 'This is a verbose message'
                    Write-Debug -Message 'This is a debug message'
                    Write-Information -MessageData 'This is an information message'
                    Write-Progress -Activity 'This shows progress'
                }
                Test-Break -WarningAction Break -InformationAction Break *>$null
                $WarningPreference = $VerbosePreference = $DebugPreference = $InformationPreference = $ProgressPreference = [System.Management.Automation.ActionPreference]::Break
                Test-Break *>$null
            }

            $results = @(Test-Debugger -ScriptBlock $testScript)
        }

        It 'Should show 7 debugger commands were invoked' {
            # When no debugger commands are provided, 'c' is invoked every time a breakpoint is hit
            $results.Count | Should -Be 7
        }

        It 'Write-Warning should trigger a breakpoint from -WarningAction Break' {
            $results[0] | ShouldHaveExtent -Line 5 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Information should trigger a breakpoint from -InformationAction Break' {
            $results[1] | ShouldHaveExtent -Line 8 -FromColumn 21 -ToColumn 84
        }

        It 'Write-Warning should trigger a breakpoint from $WarningPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[2] | ShouldHaveExtent -Line 5 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Verbose should trigger a breakpoint from $VerbosePreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[3] | ShouldHaveExtent -Line 6 -FromColumn 21 -ToColumn 71
        }

        It 'Write-Debug should trigger a breakpoint from $DebugPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[4] | ShouldHaveExtent -Line 7 -FromColumn 21 -ToColumn 67
        }

        It 'Write-Information should trigger a breakpoint from $InformationPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[5] | ShouldHaveExtent -Line 8 -FromColumn 21 -ToColumn 84
        }

        It 'Write-Progress should trigger a breakpoint from $ProgressPreference = [System.Management.Automation.ActionPreference]::Break' {
            $results[6] | ShouldHaveExtent -Line 9 -FromColumn 21 -ToColumn 67
        }
    }

    Context 'ActionPreference.Break in jobs' {

        BeforeAll {
            $job = Start-Job {
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Break
                Get-Process -TheAnswer 42
            }
        }

        AfterAll {
            Remove-Job -Job $job -Force
        }

        It 'ActionPreference.Break should break in a running job' {
            Wait-UntilTrue -sb { $job.State -eq 'AtBreakpoint' } -TimeoutInMilliseconds (10 * 1000) -IntervalInMilliseconds 100 | Should -BeTrue
        }
    }
}
