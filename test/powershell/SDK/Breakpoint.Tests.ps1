# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Breakpoint SDK Unit Tests' -Tags 'CI' {

    BeforeAll {
        # Start a job; this will create a runspace in which we can manage breakpoints
        $job = Start-Job -ScriptBlock {
            Set-PSBreakpoint -Command Start-Sleep
            1..240 | ForEach-Object {
                Start-Sleep -Milliseconds 250
                $_
                Write-Error 'boo'
                Write-Verbose 'Verbose' -Verbose
                $DebugPreference = 'Continue'
                Write-Debug 'Debug'
                Write-Warning 'Warning'
            }
        }

        # Wait for the child job that gets created to hit the breakpoint. This is the
        # only safe way to know that the job has actually entered a running state and
        # that the remote runspace is listening for requests.
        Wait-UntilTrue { $job.ChildJobs.Count -gt 0 -and $job.ChildJobs[0].State -eq 'AtBreakpoint' } -TimeoutInMilliseconds 10000 -IntervalInMilliseconds 250

        # Get the runspace for the running job
        $jobRunspace = $job.ChildJobs[0].Runspace
    }

    AfterAll {
        # Remove the running job forcibly (whether it has finished or not)
        Remove-Job -Job $job -Force
    }

    Context 'Managing breakpoints in the host runspace via the SDK' {

        It 'Can set command breakpoints' {
            $host.Runspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist') | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
        }

        It 'Can set variable breakpoints' {
            $host.Runspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { continue }) | Should -BeOfType [System.Management.Automation.VariableBreakpoint]
        }

        It 'Can set line breakpoints' {
            $host.Runspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { continue }) | Should -BeOfType [System.Management.Automation.LineBreakpoint]
        }

        It 'Can get breakpoints' {
            $host.Runspace.Debugger.GetBreakpoints() | Should -HaveCount 3
        }

        It 'Can disable breakpoints' {
            foreach ($bp in $host.Runspace.Debugger.GetBreakpoints()) {
                $bp = $host.Runspace.Debugger.DisableBreakpoint($bp)                
                $bp.Enabled | Should -BeFalse
            }
        }

        It 'Can enable breakpoints' {
            foreach ($bp in $host.Runspace.Debugger.GetBreakpoints()) {
                $bp = $host.Runspace.Debugger.EnableBreakpoint($bp)                
                $bp.Enabled | Should -BeTrue
            }
        }

        It 'Can remove breakpoints' {
            foreach ($bp in $host.Runspace.Debugger.GetBreakpoints()) {
                $host.Runspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Returns an empty collection when there are no breakpoints' {
            $host.Runspace.Debugger.GetBreakpoints() | Should -HaveCount 0
        }
    }

    Context 'Managing breakpoints in a remote runspace via the SDK' {

        It 'Can set command breakpoints' {
            $jobRunspace.Debugger.SetCommandBreakpoint('Write-Verbose', { break }) | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
        }

        It 'Can set variable breakpoints' {
            $jobRunspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { break }) | Should -BeOfType [System.Management.Automation.VariableBreakpoint]
        }

        It 'Can set line breakpoints' {
            $jobRunspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { break }) | Should -BeOfType [System.Management.Automation.LineBreakpoint]
        }

        It 'Can get breakpoints' {
            # This is 4, not 3, because we set a breakpoint in our job script
            $jobRunspace.Debugger.GetBreakpoints() | Should -HaveCount 4
        }

        It 'Can disable breakpoints' {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $bp = $jobRunspace.Debugger.DisableBreakpoint($bp)                
                $bp.Enabled | Should -BeFalse
            }
        }

        It 'Can enable breakpoints' {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $bp = $jobRunspace.Debugger.EnableBreakpoint($bp)                
                $bp.Enabled | Should -BeTrue
            }
        }

        It 'Doesn''t manipulate any breakpoints in the default runspace' {
            # Issue https://github.com/PowerShell/PowerShell/issues/10167 fix:
            # Ensure that breakpoints were not created in the default runspace.
            # Prior to this issue being fixed, breakpoints with the same id
            # would be created or updated in the default runspace.
            $host.Runspace.Debugger.GetBreakpoints() | Should -BeNullOrEmpty
        }

        It 'Can remove breakpoints' {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $jobRunspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Returns an empty collection when there are no breakpoints' {
            $jobRunspace.Debugger.GetBreakpoints() | Should -HaveCount 0
        }
    }

    Context 'Handling empty collections and  errors while managing breakpoints in the host runspace via the SDK' {

        BeforeAll {
            $bp = $host.Runspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist')
            $host.Runspace.Debugger.RemoveBreakpoint($bp) > $null
        }

        It 'Returns false when trying to disable a breakpoint that does not exist' {
            $host.Runspace.Debugger.DisableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to enable a breakpoint that does not exist' {
            $host.Runspace.Debugger.EnableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to remove a breakpoint that does not exist' {
            $host.Runspace.Debugger.RemoveBreakpoint($bp) | Should -BeFalse
        }
    }

    Context 'Handling errors while managing breakpoints in a remote runspace via the SDK' {

        BeforeAll {
            $bp = $jobRunspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist')
            $jobRunspace.Debugger.RemoveBreakpoint($bp) > $null
        }

        It 'Returns false when trying to disable a breakpoint that does not exist' {
            $jobRunspace.Debugger.DisableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to enable a breakpoint that does not exist' {
            $jobRunspace.Debugger.EnableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to remove a breakpoint that does not exist' {
            $jobRunspace.Debugger.RemoveBreakpoint($bp) | Should -BeFalse
        }
    }
}
