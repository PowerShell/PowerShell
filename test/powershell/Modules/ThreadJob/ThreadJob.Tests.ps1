# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Helper function to wait for job to reach a running or completed state
# Job state can go to "Running" before the underlying runspace thread is running
# so we always first wait 100 mSec before checking state.
function Wait-ForJobRunning
{
    param (
        $job
    )

    $iteration = 10
    Do
    {
        Start-Sleep -Milliseconds 100
    }
    Until (($job.State -match "Running|Completed|Failed") -or (--$iteration -eq 0))

    if ($job.State -notmatch "Running|Completed|Failed")
    {
        throw ("Cannot start job '{0}'. Job state is '{1}'" -f $job,$job.State)
    }
}

Describe 'Basic ThreadJob Tests' -Tags 'CI' {

    BeforeAll {

        $scriptFilePath1 = Join-Path $testdrive "TestThreadJobFile1.ps1"
        @'
        for ($i=0; $i -lt 10; $i++)
        {
            Write-Output "Hello $i"
        }
'@ > $scriptFilePath1

        $scriptFilePath2 = Join-Path $testdrive "TestThreadJobFile2.ps1"
        @'
        param ($arg1, $arg2)
        Write-Output $arg1
        Write-Output $arg2
'@ > $scriptFilePath2

        $scriptFilePath3 = Join-Path $testdrive "TestThreadJobFile3.ps1"
        @'
        $input | foreach {
            Write-Output $_
        }
'@ > $scriptFilePath3

        $scriptFilePath4 = Join-Path $testdrive "TestThreadJobFile4.ps1"
        @'
        Write-Output $using:Var1
        Write-Output $($using:Array1)[2]
        Write-Output @(,$using:Array1)
'@ > $scriptFilePath4

        $scriptFilePath5 = Join-Path $testdrive "TestThreadJobFile5.ps1"
        @'
        param ([string]$param1)
        Write-Output "$param1 $using:Var1 $using:Var2"
'@ > $scriptFilePath5

        $WaitForCountFnScript = @'
        function Wait-ForExpectedRSCount
        {
            param (
                $expectedRSCount
            )

            $iteration = 20
            while ((@(Get-Runspace).Count -ne $expectedRSCount) -and ($iteration-- -gt 0))
            {
                Start-Sleep -Milliseconds 100
            }
        }
'@
    }

    AfterEach {
        Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Remove-Job -Force
    }

    It 'ThreadJob with ScriptBlock' {

        $job = Start-ThreadJob -ScriptBlock { "Hello" }
        $results = $job | Receive-Job -Wait
        $results | Should -Be "Hello"
    }

    It 'ThreadJob with ScriptBlock and Initialization script' {

        $job = Start-ThreadJob -ScriptBlock { "Goodbye" } -InitializationScript { "Hello" }
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be "Hello"
        $results[1] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptBlock and Argument list' {

        $job = Start-ThreadJob -ScriptBlock { param ($arg1, $arg2) $arg1; $arg2 } -ArgumentList @("Hello","Goodbye")
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be "Hello"
        $results[1] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptBlock and piped input' {

        $job = "Hello","Goodbye" | Start-ThreadJob -ScriptBlock { $input | ForEach-Object { $_ } }
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be "Hello"
        $results[1] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptBlock and Using variables' {

        $Var1 = "Hello"
        $Var2 = "Goodbye"
        $Var3 = 102
        $Var4 = 1..5
        $global:GVar1 = "GlobalVar"
        $job = Start-ThreadJob -ScriptBlock {
            Write-Output $using:Var1
            Write-Output $using:Var2
            Write-Output $using:Var3
            Write-Output ($using:Var4)[1]
            Write-Output @(,$using:Var4)
            Write-Output $using:GVar1
        }

        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be $Var1
        $results[1] | Should -Be $Var2
        $results[2] | Should -Be $Var3
        $results[3] | Should -Be 2
        $results[4] | Should -Be $Var4
        $results[5] | Should -Be $global:GVar1
    }

    It 'ThreadJob with ScriptBlock and Using variables and argument list' {

        $Var1 = "Hello"
        $Var2 = 52
        $job = Start-ThreadJob -ScriptBlock {
            param ([string] $param1)

            "$using:Var1 $param1 $using:Var2"
        } -ArgumentList "There"

        $results = $job | Receive-Job -Wait
        $results | Should -Be "Hello There 52"
    }

    It 'ThreadJob with ScriptFile' {

        $job = Start-ThreadJob -FilePath $scriptFilePath1
        $results = $job | Receive-Job -Wait
        $results | Should -HaveCount 10
        $results[9] | Should -Be "Hello 9"
    }

    It 'ThreadJob with ScriptFile and Initialization script' {

        $job = Start-ThreadJob -FilePath $scriptFilePath1 -Initialization { "Goodbye" }
        $results = $job | Receive-Job -Wait
        $results | Should -HaveCount 11
        $results[0] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptFile and Argument list' {

        $job = Start-ThreadJob -FilePath $scriptFilePath2 -ArgumentList @("Hello","Goodbye")
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be "Hello"
        $results[1] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptFile and piped input' {

        $job = "Hello","Goodbye" | Start-ThreadJob -FilePath $scriptFilePath3
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be "Hello"
        $results[1] | Should -Be "Goodbye"
    }

    It 'ThreadJob with ScriptFile and Using variables' {

        $Var1 = "Hello!"
        $Array1 = 1..10

        $job = Start-ThreadJob -FilePath $scriptFilePath4
        $results = $job | Receive-Job -Wait
        $results[0] | Should -Be $Var1
        $results[1] | Should -Be 3
        $results[2] | Should -Be $Array1
    }

    It 'ThreadJob with ScriptFile and Using variables with argument list' {

        $Var1 = "There"
        $Var2 = 60
        $job = Start-ThreadJob -FilePath $scriptFilePath5 -ArgumentList "Hello"
        $results = $job | Receive-Job -Wait
        $results | Should -Be "Hello There 60"
    }

    It 'ThreadJob with terminating error' {

        $job = Start-ThreadJob -ScriptBlock { throw "MyError!" }
        $job | Wait-Job
        $job.JobStateInfo.Reason.Message | Should -Be "MyError!"
    }

    It 'ThreadJob and Error stream output' {

        $job = Start-ThreadJob -ScriptBlock { Write-Error "ErrorOut" } | Wait-Job
        $job.Error | Should -Be "ErrorOut"
    }

    It 'ThreadJob and Warning stream output' {

        $job = Start-ThreadJob -ScriptBlock { Write-Warning "WarningOut" } | Wait-Job
        $job.Warning | Should -Be "WarningOut"
    }

    It 'ThreadJob and Verbose stream output' {

        $job = Start-ThreadJob -ScriptBlock { $VerbosePreference = 'Continue'; Write-Verbose "VerboseOut" } | Wait-Job
        $job.Verbose | Should -Match "VerboseOut"
    }

    It 'ThreadJob and Verbose stream output' {

        $job = Start-ThreadJob -ScriptBlock { $DebugPreference = 'Continue'; Write-Debug "DebugOut" } | Wait-Job
        $job.Debug | Should -Be "DebugOut"
    }

    It 'ThreadJob ThrottleLimit and Queue' {

        try
        {
            # Start four thread jobs with ThrottleLimit set to two
            Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Remove-Job -Force
            $job1 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 } -ThrottleLimit 2
            $job2 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }
            $job3 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }
            $job4 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }

            # Allow jobs to start
            Wait-ForJobRunning $job2

            Get-Job | Where-Object { ($_.PSJobTypeName -eq "ThreadJob") -and ($_.State -eq "Running") } | Should -HaveCount 2
            Get-Job | Where-Object { ($_.PSJobTypeName -eq "ThreadJob") -and ($_.State -eq "NotStarted") } | Should -HaveCount 2
        }
        finally
        {
            Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Remove-Job -Force
        }

        Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Should -HaveCount 0
    }

    It 'ThreadJob Runspaces should be cleaned up at completion' {

        $script = $WaitForCountFnScript + @'
        $WarningPreference = 'SilentlyContinue'
        try
        {
            Get-Job | Where-Object PSJobTypeName -eq "ThreadJob" | Remove-Job -Force
            $rsStartCount = @(Get-Runspace).Count

            # Start four thread jobs with ThrottleLimit set to two
            $Job1 = Start-ThreadJob -ScriptBlock { "Hello 1!" } -ThrottleLimit 5
            $job2 = Start-ThreadJob -ScriptBlock { "Hello 2!" }
            $job3 = Start-ThreadJob -ScriptBlock { "Hello 3!" }
            $job4 = Start-ThreadJob -ScriptBlock { "Hello 4!" }

            $null = $job1,$job2,$job3,$job4 | Wait-Job

            # Allow for runspace clean up to happen
            Wait-ForExpectedRSCount $rsStartCount

            Write-Output (@(Get-Runspace).Count -eq $rsStartCount)
        }
        finally
        {
            Get-Job | Where-Object PSJobTypeName -eq "ThreadJob" | Remove-Job -Force
        }
'@

        $result = & "$PSHOME/pwsh" -c $script
        $result | Should -BeExactly "True"
    }

    It 'ThreadJob Runspaces should be cleaned up after job removal' {

    $script = $WaitForCountFnScript + @'
        $WarningPreference = 'SilentlyContinue'
        try {
            Get-Job | Where-Object PSJobTypeName -eq "ThreadJob" | Remove-Job -Force
            $rsStartCount = @(Get-Runspace).Count

            # Start four thread jobs with ThrottleLimit set to two
            $Job1 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 } -ThrottleLimit 2
            $job2 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }
            $job3 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }
            $job4 = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }

            Wait-ForExpectedRSCount ($rsStartCount + 4)
            Write-Output (@(Get-Runspace).Count -eq ($rsStartCount + 4))

            # Stop two jobs
            $job1 | Remove-Job -Force
            $job3 | Remove-Job -Force

            Wait-ForExpectedRSCount ($rsStartCount + 2)
            Write-Output (@(Get-Runspace).Count -eq ($rsStartCount + 2))
        }
        finally
        {
            Get-Job | Where-Object PSJobTypeName -eq "ThreadJob" | Remove-Job -Force
        }

        Wait-ForExpectedRSCount $rsStartCount
        Write-Output (@(Get-Runspace).Count -eq $rsStartCount)
'@

        $result = & "$PSHOME/pwsh" -c $script
        $result | Should -BeExactly "True","True","True"
    }

    It 'ThreadJob jobs should work with Receive-Job -AutoRemoveJob' {

        Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Remove-Job -Force

        $job1 = Start-ThreadJob -ScriptBlock { 1..2 | ForEach-Object { Start-Sleep -Milliseconds 100; "Output $_" } } -ThrottleLimit 5
        $job2 = Start-ThreadJob -ScriptBlock { 1..2 | ForEach-Object { Start-Sleep -Milliseconds 100; "Output $_" } }
        $job3 = Start-ThreadJob -ScriptBlock { 1..2 | ForEach-Object { Start-Sleep -Milliseconds 100; "Output $_" } }
        $job4 = Start-ThreadJob -ScriptBlock { 1..2 | ForEach-Object { Start-Sleep -Milliseconds 100; "Output $_" } }

        $null = $job1,$job2,$job3,$job4 | Receive-Job -Wait -AutoRemoveJob

        Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Should -HaveCount 0
    }

    It 'ThreadJob jobs should run in FullLanguage mode by default' {

        $result = Start-ThreadJob -ScriptBlock { $ExecutionContext.SessionState.LanguageMode } | Wait-Job | Receive-Job
        $result | Should -Be "FullLanguage"
    }
}

Describe 'Job2 class API tests' -Tags 'CI' {

    AfterEach {
        Get-Job | Where-Object PSJobTypeName -EQ "ThreadJob" | Remove-Job -Force
    }

    It 'Verifies StopJob API' {

        $job = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 } -ThrottleLimit 5
        Wait-ForJobRunning $job
        $job.StopJob($true, "No Reason")
        $job.JobStateInfo.State | Should -Be "Stopped"
    }

    It 'Verifies StopJobAsync API' {

        $job = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 } -ThrottleLimit 5
        Wait-ForJobRunning $job
        $job.StopJobAsync($true, "No Reason")
        Wait-Job $job
        $job.JobStateInfo.State | Should -Be "Stopped"
    }

    It 'Verifies StartJobAsync API' {

        $jobRunning = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 } -ThrottleLimit 1
        $jobNotRunning = Start-ThreadJob -ScriptBlock { Start-Sleep -Seconds 60 }

        $jobNotRunning.JobStateInfo.State | Should -Be "NotStarted"

        # StartJobAsync starts jobs synchronously for ThreadJob jobs
        $jobNotRunning.StartJobAsync()
        Wait-ForJobRunning $jobNotRunning
        $jobNotRunning.JobStateInfo.State | Should -Be "Running"
    }

    It 'Verifies JobSourceAdapter Get-Jobs' {

        $job = Start-ThreadJob -ScriptBlock { "Hello" } | Wait-Job

        $getJob = Get-Job -InstanceId $job.InstanceId 2> $null
        $getJob | Should -Be $job

        $getJob = Get-Job -Name $job.Name 2> $null
        $getJob | Should -Be $job

        $getJob = Get-Job -Command ' "hello" ' 2> $null
        $getJob | Should -Be $job

        $getJob = Get-Job -State $job.JobStateInfo.State 2> $null
        $getJob | Should -Be $job

        $getJob = Get-Job -Id $job.Id 2> $null
        $getJob | Should -Be $job

        # Get-Job -Filter is not supported
        $result = Get-Job -Filter @{Id = ($job.Id)} 3> $null
        $result | Should -BeNullOrEmpty
    }

    It 'Verifies terminating job error' {

        $job = Start-ThreadJob -ScriptBlock { throw "My Job Error!" } | Wait-Job
        $results = $job | Receive-Job 2>&1
        $results.ToString() | Should -Be "My Job Error!"
    }
}
