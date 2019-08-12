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

        It 'Can set breakpoints' {
            $host.Runspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist') | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
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
            $host.Runspace.Debugger.GetBreakpoints().Count | Should -Be 0
        }
    }

    Context 'Managing breakpoints in a remote runspace via the SDK' {

        It 'Can set breakpoints' {
            $jobRunspace.Debugger.SetCommandBreakpoint('Write-Verbose', { break }) | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
            $jobRunspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { break }) | Should -BeOfType [System.Management.Automation.VariableBreakpoint]
            $jobRunspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { break }) | Should -BeOfType [System.Management.Automation.LineBreakpoint]
        }

        It 'Breakpoints are triggered by the remote debugger' {
            $startTime = [DateTime]::UtcNow
            $maxTimeToWait = [TimeSpan]'00:00:20'
            while ($job.State -ne 'AtBreakpoint' -and ([DateTime]::UtcNow - $startTime) -lt $maxTimeToWait) {
                Start-Sleep -Milliseconds 100 # Give the job a bit of time to hit a breakpoint
            }
            $job.State | Should -Be 'AtBreakpoint'
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

        It 'Can remove breakpoints' {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $jobRunspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
            $jobRunspace.Debugger.GetBreakpoints().Count | Should -Be 0
        }
    }
}
