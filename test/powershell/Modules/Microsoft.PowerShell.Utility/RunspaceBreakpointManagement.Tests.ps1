# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Runspace Breakpoint Unit Tests - Feature-Enabled' -Tags 'CI' {

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

    # Test the transformation attribute independently of PSBreakpoint cmdlet tests so that
    # those tests can focus on scenarios expected to work.
    Context 'Can transform a runspace name, id, or instanceid into a runspace' {

        function Test-RunspaceTransform {
            param(
                [ValidateNotNull()]
                [Runspace]
                [System.Management.Automation.Runspaces.Runspace()]
                $Runspace
            )
            $Runspace
        }

        It 'Transforms a valid runspace name into a runspace' {
            Test-RunspaceTransform -Runspace $host.Runspace.Name | Should -Be $host.Runspace
        }

        It 'Transforms a valid runspace ID into a runspace' {
            Test-RunspaceTransform -Runspace $host.Runspace.Id | Should -Be $host.Runspace
        }

        It 'Transforms a valid runspace instance ID into a runspace' {
            Test-RunspaceTransform -Runspace $host.Runspace.InstanceId | Should -Be $host.Runspace
        }

        It 'Reports an argument transformation error when given invalid input' {
            $e = { Test-RunspaceTransform -Runspace 'This is not a runspace name' } | Should -Throw -PassThru
            $e.Exception.GetType().Name | Should -Be 'ParameterBindingArgumentTransformationException'
        }

        It 'Passes through $null without transforming it' {
            $e = { Test-RunspaceTransform -Runspace $null } | Should -Throw -PassThru
            $e.Exception.GetType().Name | Should -Be 'ParameterBindingValidationException'
        }
    }

    Context 'Managing breakpoints in the host runspace' {

        It 'Can set breakpoints' {
            Set-PSBreakpoint -Command Test-ThisCommandDoesNotExist -Runspace $host.Runspace | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
        }

        It 'Can get breakpoints, and the result breakpoints do not show the runspace id because they are local' {
            foreach ($bp in Get-PSBreakpoint -Runspace $host.Runspace) {
                Get-Member -InputObject $bp -Name RunspaceId -ErrorAction Ignore | Should -Be $null
            }
        }

        It 'Can disable breakpoints in a pipeline' {
            foreach ($bp in Get-PSBreakpoint -Runspace $host.Runspace | Disable-PSBreakpoint -PassThru) {
                $bp.Enabled | Should -BeFalse
                # This ensures we're working in the right runspace
                Get-Member -InputObject $bp -Name RunspaceId -ErrorAction Ignore | Should -Be $null
            }
        }

        It 'Can enable breakpoints in a pipeline' {
            foreach ($bp in Get-PSBreakpoint -Runspace $host.Runspace | Enable-PSBreakpoint -PassThru) {
                $bp.Enabled | Should -BeTrue
                # This ensures we're working in the right runspace
                Get-Member -InputObject $bp -Name RunspaceId -ErrorAction Ignore | Should -Be $null
            }
        }

        It 'Can disable breakpoints by id' {
            foreach ($bp in Get-PSBreakpoint -Runspace $host.Runspace) {
                $bp = Disable-PSBreakpoint -Id $bp.Id -Runspace $host.Runspace -PassThru
                $bp.Enabled | Should -BeFalse
                # This ensures we're working in the right runspace
                Get-Member -InputObject $bp -Name RunspaceId -ErrorAction Ignore | Should -Be $null
            }
        }

        It 'Can enable breakpoints by id' {
            foreach ($bp in Get-PSBreakpoint -Runspace $host.Runspace) {
                $bp = Enable-PSBreakpoint -Id $bp.Id -Runspace $host.Runspace -PassThru
                $bp.Enabled | Should -BeTrue
                # This ensures we're working in the right runspace
                Get-Member -InputObject $bp -Name RunspaceId -ErrorAction Ignore | Should -Be $null
            }
        }

        It 'Can remove breakpoints' {
            Get-PSBreakpoint -Runspace $host.Runspace | Remove-PSBreakpoint
            Get-PSBreakpoint -Runspace $host.Runspace | Should -BeNull
        }
    }

    Context 'Managing breakpoints in a remote runspace' {

        AfterAll {
            # Get rid of any breakpoints that were created in the default runspace.
            # This is necessary due to a known bug that causes breakpoints with the
            # same id to be created or updated in the default runspace.
            Get-PSBreakpoint | Remove-PSBreakpoint
        }

        It 'Can set breakpoints' {
            Set-PSBreakpoint -Command Write-Verbose -Action { break } -Runspace $jobRunspace | Should -BeOfType [System.Management.Automation.CommandBreakpoint]
            Set-PSBreakpoint -Variable DebugPreference -Mode ReadWrite -Action { break } -Runspace $jobRunspace | Should -BeOfType [System.Management.Automation.VariableBreakpoint]
            Set-PSBreakpoint -Script $PSCommandPath -Line 1 -Column 1 -Action { break } -Runspace $jobRunspace | Should -BeOfType [System.Management.Automation.LineBreakpoint]
        }

        It 'Can get breakpoints, and the result breakpoints show the remote runspace id' {
            foreach ($bp in Get-PSBreakpoint -Runspace $jobRunspace) {
                $bp.RunspaceId | Should -Be $jobRunspace.InstanceId
            }
        }

        It 'Breakpoints are triggered by the remote debugger' {
            $startTime = [datetime]::UtcNow
            $maxTimeToWait = [TimeSpan]'00:00:20'
            while ($job.State -ne 'AtBreakpoint' -and ([datetime]::UtcNow - $startTime) -lt $maxTimeToWait) {
                Start-Sleep -Milliseconds 100 # Give the job a bit of time to hit a breakpoint
            }
            $job.State | Should -Be 'AtBreakpoint'
        }

        It 'Can disable breakpoints in a pipeline' {
            foreach ($bp in Get-PSBreakpoint -Runspace $jobRunspace | Disable-PSBreakpoint -PassThru) {
                $bp.Enabled | Should -BeFalse
                # This ensures we're working in the right runspace
                $bp.RunspaceId | Should -Be $jobRunspace.InstanceId
            }
        }

        It 'Can enable breakpoints in a pipeline' {
            foreach ($bp in Get-PSBreakpoint -Runspace $jobRunspace | Enable-PSBreakpoint -PassThru) {
                $bp.Enabled | Should -BeTrue
                # This ensures we're working in the right runspace
                $bp.RunspaceId | Should -Be $jobRunspace.InstanceId
            }
        }

        It 'Can disable breakpoints by id' {
            foreach ($bp in Get-PSBreakpoint -Runspace $jobRunspace) {
                $bp = Disable-PSBreakpoint -Id $bp.Id -Runspace $jobRunspace -PassThru
                $bp.Enabled | Should -BeFalse
                # This ensures we're working in the right runspace
                $bp.RunspaceId | Should -Be $jobRunspace.InstanceId
            }
        }

        It 'Can enable breakpoints in a pipeline' {
            foreach ($bp in Get-PSBreakpoint -Runspace $jobRunspace) {
                $bp = Enable-PSBreakpoint -Id $bp.Id -Runspace $jobRunspace -PassThru
                $bp.Enabled | Should -BeTrue
                # This ensures we're working in the right runspace
                $bp.RunspaceId | Should -Be $jobRunspace.InstanceId
            }
        }

        It 'Can remove breakpoints' {
            Get-PSBreakpoint -Runspace $jobRunspace | Remove-PSBreakpoint
            Get-PSBreakpoint -Runspace $jobRunspace | Should -BeNull
        }
    }
}
