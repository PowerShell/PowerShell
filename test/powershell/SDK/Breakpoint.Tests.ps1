# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Breakpoint SDK Unit Tests' -Tags 'CI' {

    BeforeAll {
        # Start a job; this will create a runspace in which we can manage breakpoints
        $job = Start-Job -Scriptblock {
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
        Wait-UntilTrue { $job.ChildJobs.Count -gt 0 -and $job.ChildJobs[0].State -eq 'AtBreakpoint' } -TimeoutInMilliseconds 60000 -IntervalInMilliseconds 250

        # Get the runspace for the running job
        $jobRunspace = $job.ChildJobs[0].Runspace
    }

    AfterAll {
        # Remove the running job forcibly (whether it has finished or not)
        Remove-Job -Job $job -Force
    }

    Context 'Managing breakpoints in the host runspace via the SDK' {

        AfterAll {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints()) {
                $Host.Runspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Can set command breakpoints' {
            $Host.Runspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist', $null, $null) | Should -BeOfType System.Management.Automation.CommandBreakpoint
        }

        It 'Can set variable breakpoints' {
            $Host.Runspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { continue }, $null) | Should -BeOfType System.Management.Automation.VariableBreakpoint
        }

        It 'Can set line breakpoints' {
            $Host.Runspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { continue }) | Should -BeOfType System.Management.Automation.LineBreakpoint
        }

        It 'Can get breakpoints' {
            $Host.Runspace.Debugger.GetBreakpoints() | Should -HaveCount 3
        }

        It 'Can disable breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints()) {
                $bp = $Host.Runspace.Debugger.DisableBreakpoint($bp)
                $bp.Enabled | Should -BeFalse
            }
        }

        It 'Can enable breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints()) {
                $bp = $Host.Runspace.Debugger.EnableBreakpoint($bp)
                $bp.Enabled | Should -BeTrue
            }
        }

        It 'Can remove breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints()) {
                $Host.Runspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Returns an empty collection when there are no breakpoints' {
            $Host.Runspace.Debugger.GetBreakpoints() | Should -HaveCount 0
        }

        It 'Can set multiple breakpoints' {
            $breakpoints = [System.Collections.Generic.List[System.Management.Automation.Breakpoint]] @(
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 1)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 2)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 3)
            )

            $Host.Runspace.Debugger.SetBreakpoints($breakpoints)
            $Host.Runspace.Debugger.GetBreakpoints() | Should -HaveCount 3
        }
    }

    Context 'Managing breakpoints in a remote runspace via the SDK' {

        AfterAll {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $jobRunspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Can set command breakpoints' {
            $jobRunspace.Debugger.SetCommandBreakpoint('Write-Verbose', { break }, $null) | Should -BeOfType System.Management.Automation.CommandBreakpoint
        }

        It 'Can set variable breakpoints' {
            $jobRunspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { break }, $null) | Should -BeOfType System.Management.Automation.VariableBreakpoint
        }

        It 'Can set line breakpoints' {
            $jobRunspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { break }) | Should -BeOfType System.Management.Automation.LineBreakpoint
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
            $Host.Runspace.Debugger.GetBreakpoints() | Should -BeNullOrEmpty
        }

        It 'Can remove breakpoints' {
            foreach ($bp in $jobRunspace.Debugger.GetBreakpoints()) {
                $jobRunspace.Debugger.RemoveBreakpoint($bp) | Should -BeTrue
            }
        }

        It 'Returns an empty collection when there are no breakpoints' {
            $jobRunspace.Debugger.GetBreakpoints() | Should -HaveCount 0
        }

        It 'Can set multiple breakpoints' {
            $breakpoints = [System.Collections.Generic.List[System.Management.Automation.Breakpoint]] @(
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 1)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 2)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 3)
            )

            $jobRunspace.Debugger.SetBreakpoints($breakpoints)
            $jobRunspace.Debugger.GetBreakpoints() | Should -HaveCount 3
        }
    }

    Context 'Handling empty collections and errors while managing breakpoints in the host runspace via the SDK' {

        BeforeAll {
            $bp = [System.Management.Automation.CommandBreakpoint]::new($TestDrive, $null, 'Test-ThisCommandDoesNotExist')
        }

        It 'Returns false when trying to disable a breakpoint that does not exist' {
            $Host.Runspace.Debugger.DisableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to enable a breakpoint that does not exist' {
            $Host.Runspace.Debugger.EnableBreakpoint($bp) | Should -Be $null
        }

        It 'Returns false when trying to remove a breakpoint that does not exist' {
            $Host.Runspace.Debugger.RemoveBreakpoint($bp) | Should -BeFalse
        }
    }

    Context 'Handling errors while managing breakpoints in a remote runspace via the SDK' {

        BeforeAll {
            $bp = $jobRunspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist', $null, $null)
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

    Context 'Manage breakpoints in another runspace' {
        BeforeAll {
            $runspace = [runspacefactory]::CreateRunspace()
            $runspace.Open()
        }

        AfterAll {
            $runspace.Close()
            $runspace.Dispose()
        }

        It 'Can set command breakpoints' {
            $Host.Runspace.Debugger.SetCommandBreakpoint('Test-ThisCommandDoesNotExist', $null, $null, $runspace.Id) | Should -BeOfType System.Management.Automation.CommandBreakpoint
        }

        It 'Can set variable breakpoints' {
            $Host.Runspace.Debugger.SetVariableBreakpoint('DebugPreference', 'ReadWrite', { continue }, $null, $runspace.Id) | Should -BeOfType System.Management.Automation.VariableBreakpoint
        }

        It 'Can set line breakpoints' {
            $Host.Runspace.Debugger.SetLineBreakpoint($PSCommandPath, 1, 1, { continue }, $runspace.Id) | Should -BeOfType System.Management.Automation.LineBreakpoint
        }

        It 'Can get breakpoints' {
            $Host.Runspace.Debugger.GetBreakpoints($runspace.Id) | Should -HaveCount 3
        }

        It 'Can disable breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints($runspace.Id)) {
                $bp = $Host.Runspace.Debugger.DisableBreakpoint($bp, $runspace.Id)
                $bp.Enabled | Should -BeFalse
            }
        }

        It 'Can enable breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints($runspace.Id)) {
                $bp = $Host.Runspace.Debugger.EnableBreakpoint($bp, $runspace.Id)
                $bp.Enabled | Should -BeTrue
            }
        }

        It 'Doesn''t manipulate any breakpoints in the default runspace' {
            $Host.Runspace.Debugger.GetBreakpoints() | Should -BeNullOrEmpty
        }

        It 'Can remove breakpoints' {
            foreach ($bp in $Host.Runspace.Debugger.GetBreakpoints($runspace.Id)) {
                $Host.Runspace.Debugger.RemoveBreakpoint($bp, $runspace.Id) | Should -BeTrue
            }
        }

        It 'Returns an empty collection when there are no breakpoints' {
            $Host.Runspace.Debugger.GetBreakpoints($runspace.Id) | Should -HaveCount 0
        }

        It 'Can set multiple breakpoints' {
            $breakpoints = [System.Collections.Generic.List[System.Management.Automation.Breakpoint]] @(
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 1)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 2)
                [System.Management.Automation.LineBreakpoint]::new("/Path/to/foo.ps1", 3)
            )

            $Host.Runspace.Debugger.SetBreakpoints($breakpoints, $runspace.Id)
            $Host.Runspace.Debugger.GetBreakpoints($runspace.Id) | Should -HaveCount 3
        }
    }
}
