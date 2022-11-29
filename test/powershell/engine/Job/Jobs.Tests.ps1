# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Basic Job Tests' -Tags 'Feature' {
    BeforeAll {
        # Make sure we do not have any jobs running
        Get-Job | Remove-Job -Force
        $timeBeforeStartedJob = Get-Date
        $startedJob = Start-Job -Name 'StartedJob' -Scriptblock { 1 + 1 } | Wait-Job
        $timeAfterStartedJob = Get-Date

        function script:ValidateJobInfo($job, $state, $hasMoreData, $command)
        {
            $job.State | Should -BeExactly $state
            $job.HasMoreData | Should -Be $hasMoreData

            if($command -ne $null)
            {
                $job.Command | Should -BeExactly $command
            }
        }
    }

    AfterAll {
        $startedJob | Remove-Job -Force -ErrorAction SilentlyContinue
    }

    Context 'Basic tests' {

        BeforeAll {
            $invalidPathTestCases = @(
                @{ path = "This is an invalid path"; case = "invalid path"; errorId = "DirectoryNotFoundException,Microsoft.PowerShell.Commands.StartJobCommand"}
                @{ path = ""; case = "empty string"; errorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.StartJobCommand"}
                @{ path = " "; case = "whitespace string (single space)"; errorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.StartJobCommand"}
            )
        }

        AfterEach {
            Get-Job | Where-Object { $_.Id -ne $startedJob.Id } | Remove-Job -ErrorAction SilentlyContinue -Force
        }

        It 'Can start, wait and receive a Job' {
            $job = Start-Job -ScriptBlock { 1 + 1 }
            $result = $job | Wait-Job | Receive-Job
            ValidateJobInfo -job $job -state 'Completed' -hasMoreData $false -command ' 1 + 1 '
            $result | Should -Be 2
        }

        It 'Can run nested jobs' {
            $job = Start-Job -ScriptBlock { Start-Job -ScriptBlock { 1 + 1 } | Wait-Job | Receive-Job }
            ValidateJobInfo -job $job -state 'Running' -hasMoreData $true -command ' Start-Job -ScriptBlock { 1 + 1 } | Wait-Job | Receive-Job '
            $result = $job | Wait-Job | Receive-Job
            $result | Should -Be 2
        }

        It 'Can get errors messages from job' {
            $job = Start-Job -ScriptBlock { throw 'MyError' } | Wait-Job
            Receive-Job -Job $job -ErrorVariable ev -ErrorAction SilentlyContinue
            $ev[0].Exception.Message | Should -BeExactly 'MyError'
        }

        It 'Can get warning messages from job' {
            $job = Start-Job -ScriptBlock { Write-Warning 'MyWarning' } | Wait-Job
            Receive-Job -Job $job -WarningVariable wv -WarningAction SilentlyContinue
            $wv | Should -BeExactly 'MyWarning'
        }

        It 'Can get verbose message from job' {
            $job = Start-Job -ScriptBlock { Write-Verbose -Verbose 'MyVerbose' } | Wait-Job
            $VerboseMsg = $job.ChildJobs[0].verbose.readall()
            $VerboseMsg | Should -BeExactly 'MyVerbose'
        }

        It 'Can get progress message from job' {
            $job = Start-Job -ScriptBlock { Write-Progress -Activity 1 -Status 2  } | Wait-Job
            $ProgressMsg = $job.ChildJobs[0].progress.readall()
            $ProgressMsg[0].Activity | Should -BeExactly 1
            $ProgressMsg[0].StatusDescription | Should -BeExactly 2
        }

        It 'Can use the user specified working directory parameter with whitespace' {
            $path = Join-Path -Path $TestDrive -ChildPath "My Dir"
            $null = New-Item -ItemType Directory -Path "$path"
            $job = Start-Job -ScriptBlock { $PWD } -WorkingDirectory $path | Wait-Job
            $jobOutput = Receive-Job $job
            $jobOutput | Should -BeExactly $path.ToString()
        }

        It 'Can specify the working directory with a PSPath' {
            try {
                Push-Location 'Temp:\'
                $job = Start-Job -ScriptBlock { $PWD } -WorkingDirectory $pwd.Path | Wait-Job
                $jobOutput = Receive-Job $job
                $jobOutput | Should -BeExactly $pwd.Path
            } finally {
                Pop-Location
            }
        }

        It 'Can use the user specified working directory parameter with quote' -Skip:($IsWindows) {
            $path = Join-Path -Path $TestDrive -ChildPath "My ""Dir"
            $null = New-Item -ItemType Directory -Path "$path"
            $job = Start-Job -ScriptBlock { $PWD } -WorkingDirectory $path | Wait-Job
            $jobOutput = Receive-Job $job
            $jobOutput | Should -BeExactly $path.ToString()
        }

        It 'Verifies the working directory parameter path with trailing backslash' -Skip:(! $IsWindows) {
            $job = Start-Job { $PWD } -WorkingDirectory '\' | Wait-Job
            $job.JobStateInfo.State | Should -BeExactly 'Completed'
        }

        It 'Throws an error when the working directory parameter is <case>' -TestCases $invalidPathTestCases {
            param($path, $case, $errorId)

            {Start-Job -ScriptBlock { 1 + 1 } -WorkingDirectory $path} | Should -Throw -ErrorId $errorId
        }

        It 'Verifies that the current working directory is preserved' {
            $job = Start-Job -ScriptBlock { $PWD }
            $location = $job | Wait-Job | Receive-Job
            $job | Remove-Job
            $location.Path | Should -BeExactly $PWD.Path
        }

        It "Create job with native command" {
            try {
                $nativeJob = Start-Job { & "$PSHOME/pwsh" -c 1+1 }
                $nativeJob | Wait-Job
                $nativeJob.State | Should -BeExactly "Completed"
                $nativeJob.HasMoreData | Should -BeTrue
                Receive-Job $nativeJob | Should -BeExactly 2
                Remove-Job $nativeJob
                { Get-Job $nativeJob -ErrorAction Stop } | Should -Throw -ErrorId "JobWithSpecifiedNameNotFound,Microsoft.PowerShell.Commands.GetJobCommand"
            }
            finally {
                Remove-Job $nativeJob -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context 'Wait-Job tests' {

        BeforeAll {
            $waitJobTestCases = @(
                @{ parameters = @{ Name = $startedJob.Name } ; property  = '-Name'},
                @{ parameters = @{ Id = $startedJob.Id } ; property  = '-Id'},
                @{ parameters = @{ Job = $startedJob } ; property  = '-Job'},
                @{ parameters = @{ InstanceId = $startedJob.InstanceId } ; property  = '-InstanceId'},
                @{ parameters = @{ State = $startedJob.State } ; property  = '-State'}
            )
        }

        AfterEach {
            Get-Job | Where-Object { $_.Id -ne $startedJob.Id } | Remove-Job -ErrorAction SilentlyContinue -Force
        }

        It 'Can wait for jobs to complete using <property>' -TestCases $waitJobTestCases {
            param($parameters)
            $job = Wait-Job @parameters
            ValidateJobInfo -job $job -state 'Completed' -hasMoreData $true -command ' 1 + 1 '
        }

        It 'Can wait for any job to complete' {
            $jobs = 1..3 | ForEach-Object { $seconds = $_ ; Start-Job -ScriptBlock { Start-Sleep -Seconds $using:seconds ; $using:seconds} }
            $waitedJob = Wait-Job -Job $jobs -Any
            ValidateJobInfo -job $waitedJob -state 'Completed' -hasMoreData $true -command ' Start-Sleep -Seconds $using:seconds ; $using:seconds'
            $result = $waitedJob | Receive-Job
            ## We check for $result to be less than 4 so that any of the jobs completing first will considered a success.
            $result | Should -BeLessThan 4
            ## Check none of the jobs threw errors.
            $jobs.Error | Should -BeNullOrEmpty
        }

        It 'Can timeout waiting for a job' {
            $job = Start-Job -ScriptBlock { Start-Sleep -Seconds 10 }
            $job | Wait-Job -TimeoutSec 1
            ValidateJobInfo -job $job -state 'Running' -hasMoreData $true -command ' Start-Sleep -Seconds 10 '
        }
    }

    Context 'Receive-job tests' {
        It 'Can Receive-Job with state change events' {
            $result = Start-Job -Name 'ReceiveWriteEventsJob' -ScriptBlock { 1 + 1 } | Receive-Job -Wait -WriteEvents
            $result.Count | Should -Be 3
            $result[0] | Should -Be 2
            $result[1].GetType().FullName | Should -BeExactly 'System.Management.Automation.JobStateEventArgs'
        }

        It 'Can Receive-Job with job object and result' {
            $result = Start-Job -ScriptBlock { 1 + 1 } | Receive-Job -Wait -WriteJobInResults
            $result.Count | Should -Be 2
            ValidateJobInfo -job $result[0] -command ' 1 + 1 ' -state 'Completed' -hasMoreData $false
            $result[1] | Should -Be 2
            $result[0] | Remove-Job -Force -ErrorAction SilentlyContinue
        }

        It 'Can Receive-Job and autoremove' {
            $result = Start-Job -Name 'ReceiveJobAutoRemove' -ScriptBlock { 1 + 1 } | Receive-Job -Wait -AutoRemoveJob
            $result | Should -Be 2
            { Get-Job -Name 'ReceiveJobAutoRemove' -ErrorAction Stop } | Should -Throw -ErrorId 'JobWithSpecifiedNameNotFound,Microsoft.PowerShell.Commands.GetJobCommand'
        }

        It 'Can Receive-Job and keep results' {
            $job = Start-Job -ScriptBlock { 1 + 1 } | Wait-Job
            $result = Receive-Job -Keep -Job $job
            $result | Should -Be 2
            $result2 = Receive-Job -Job $job
            $result2 | Should -Be 2
            $result3 = Receive-Job -Job $job
            $result3 | Should -BeNullOrEmpty
            $job | Remove-Job -Force -ErrorAction SilentlyContinue
        }

        It 'Can Receive-Job with NoRecurse' {
            $job = Start-Job -ScriptBlock { 1 + 1 }
            $result = Receive-Job -Wait -NoRecurse -Job $job
            $result | Should -BeNullOrEmpty
            $job | Remove-Job -Force -ErrorAction SilentlyContinue
        }

        It 'Can Receive-Job using ComputerName' {
            $jobName = 'ReceiveUsingComputerName'
            $job = Start-Job -ScriptBlock { 1 + 1 } -Name $jobName | Wait-Job
            $result = Receive-Job -ComputerName localhost -Job $job
            $result | Should -Be 2
            $job | Remove-Job -Force -ErrorAction SilentlyContinue
        }

        It 'Can Receive-Job using Location' {
            $jobName = 'ReceiveUsingLocation'
            $job = Start-Job -ScriptBlock { 1 + 1 } -Name $jobName | Wait-Job
            $result = Receive-Job -Location localhost -Job $job
            $result | Should -Be 2
            $job | Remove-Job -Force -ErrorAction SilentlyContinue
        }

        It 'Can receive a job with -wait switch' {
            $job = Start-Job -ScriptBlock { 1 + 1 }
            $result = $job | Receive-Job -Wait
            ValidateJobInfo -job $job -state 'Completed' -hasMoreData $false -command ' 1 + 1 '
            $result | Should -Be 2
        }
    }

    Context 'Get-Job tests' {
        BeforeAll {
            $getJobTestCases = @(
                @{ parameters = @{ Name = $startedJob.Name } ; property = 'Name'},
                @{ parameters = @{ Id = $startedJob.Id } ; property = 'Id'},
                @{ parameters = @{ InstanceId = $startedJob.InstanceId } ; property = 'InstanceId'},
                @{ parameters = @{ State = $startedJob.State } ; property = 'State'}
            )

            $getJobSwitches = @(
                @{ parameters = @{ Before = $timeAfterStartedJob }; property = '-Before'},
                @{ parameters = @{ After = $timeBeforeStartedJob }; property = '-After'},
                @{ parameters = @{ HasMoreData = $true }; property = '-HasMoreData'}
            )

            $getJobChildJobs = @(
                @{ parameters = @{ IncludeChildJob = $true }; property = '-IncludeChildJob'},
                @{ parameters = @{ ChildJobState = 'Completed' }; property = '-ChildJobState'}
            )
        }

        AfterEach {
            Get-Job | Where-Object { $_.Id -ne $startedJob.Id } | Remove-Job -ErrorAction SilentlyContinue -Force
        }

        It 'Can Get-Job with <property>' -TestCases $getJobTestCases {
            param($parameters)
            $job = Get-Job @parameters
            ValidateJobInfo -job $job -state 'Completed' -hasMoreData $true -command ' 1 + 1 '
        }

        It 'Can Get-Job with <property>' -TestCases $getJobSwitches {
            param($parameters)
            $job = Get-Job @parameters
            ValidateJobInfo -job $job -state 'Completed' -hasMoreData $true -Name 'StartedJob'
        }

        It 'Can Get-Job with <property>' -TestCases $getJobChildJobs {
            param($parameters)
            $jobs = Get-Job @parameters
            $jobs.Count | Should -Be 2
            ValidateJobInfo -job $jobs[0] -state 'Completed' -hasMoreData $true -Name 'StartedJob'
            ValidateJobInfo -job $jobs[1] -state 'Completed' -hasMoreData $true
        }
    }

    Context 'Remove-Job tests' {
        # The test pattern used here is different from other tests since there is a scoping issue in Pester.
        # If BeforeEach is used then $removeJobTestCases does not bind when the It is called.
        # This implementation works around the problem by using a BeforeAll and creating a job inside the It.
        BeforeAll {
            $removeJobTestCases = @(
                @{ property = 'Name'}
                @{ property = 'Id'}
                @{ property = 'InstanceId'}
                @{ property = 'State'}
            )
        }

        It 'Can Remove-Job with <property>' -TestCases $removeJobTestCases {
            param($property)
            $jobToRemove = Start-Job -ScriptBlock { 1 + 1 } -Name 'JobToRemove' | Wait-Job
            $splat = @{ $property = $jobToRemove.$property }
            Remove-Job @splat
            Get-Job $jobToRemove -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }

    Context 'Stop-Job tests' {
        # The test pattern used here is different from other tests since there is a scoping issue in Pester.
        # If BeforeEach is used then $stopJobTestCases does not bind when the It is called.
        # This implementation works around the problem by using a BeforeAll and creating a job inside the It.
        BeforeAll {
            $stopJobTestCases = @(
                @{ property = 'Name'}
                @{ property = 'Id'}
                @{ property = 'InstanceId'}
                @{ property = 'State'}
            )
        }

        BeforeEach {
            # 20 seconds is chosen to be large, so that the job is in running state when Stop-Job is called.
            $jobToStop = Start-Job -Scriptblock {
                1..80 | ForEach-Object {
                    Write-Output $_
                    Start-Sleep -Milliseconds 250
                }
            } -Name 'JobToStop'
            # Wait until the job is actually running and executing the script
            do {
                $data = Receive-Job -Job $jobToStop
            } while (($data.Count -eq 0) -and ($jobToStop.State -eq 'Running'))
        }

        AfterEach {
            Remove-Job $jobToStop -Force -ErrorAction SilentlyContinue
        }

        It 'Can Stop-Job with <property>' -TestCases $stopJobTestCases {
            param($property)
            $splat = @{ $property = $jobToStop.$property }
            Stop-Job @splat
            ValidateJobInfo -job $jobToStop -state 'Stopped' -hasMoreData $false
        }
    }

    Context 'Background pwsh process should terminate after job is done' {
        It "Can clean up background pwsh process after job is done" {
            $job = Start-Job { $pid }
            $processId = Receive-Job $job -Wait

            try {
                $process = Get-Process -Id $processId -ErrorAction Stop
                Wait-UntilTrue { $process.HasExited } -IntervalInMilliseconds 300 | Should -BeTrue
            } catch {
                $_.FullyQualifiedErrorId | Should -BeExactly 'NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand'
            }

            Remove-Job $job -Force
        }

        It "Can clean up background pwsh process when job is stopped" {
            $job = Start-Job { $pid; Start-Sleep -Second 10 }

            # Wait for the pid to be received.
            Wait-UntilTrue { [bool](Receive-Job $job -Keep) } | Should -BeTrue
            $processId = Receive-Job $job

            # Stop the job and wait for the cleanup to finish.
            Stop-Job $job

            try {
                $process = Get-Process -Id $processId -ErrorAction Stop
                Wait-UntilTrue { $process.HasExited } -IntervalInMilliseconds 300 | Should -BeTrue
            } catch {
                $_.FullyQualifiedErrorId | Should -BeExactly 'NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand'
            }

            Remove-Job $job -Force
        }

        It "Can clean up background pwsh process when job is removed" {
            $job = Start-Job { $pid; Start-Sleep -Second 10 }

            # Wait for the pid to be received.
            Wait-UntilTrue { [bool](Receive-Job $job -Keep) } | Should -BeTrue
            $processId = Receive-Job $job

            # Remove the job and wait for the cleanup to finish.
            Remove-Job $job -Force

            try {
                $process = Get-Process -Id $processId -ErrorAction Stop
                Wait-UntilTrue { $process.HasExited } -IntervalInMilliseconds 300 | Should -BeTrue
            } catch {
                $_.FullyQualifiedErrorId | Should -BeExactly 'NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand'
            }
        }
    }
}
