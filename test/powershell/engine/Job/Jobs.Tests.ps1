Describe 'Basic Job Tests' -Tags 'CI' {

    BeforeAll {
        $job = Start-Job {1}
    }

    It 'Test job creation' {
        $job | should not be $null
    }

    It 'Test job State' {
        Wait-Job $job -Timeout 60
        $job.JobStateInfo.State -eq 'Completed' | should be $true
    }

    It 'Job output test' {
        Receive-Job $job -wait | should be 1
    }

    It "Create job with native command" {
        try {
            $nativeJob = Start-job { powershell -c 1+1 }
            $nativeJob | Wait-Job
            $nativeJob.State | Should BeExactly "Completed"
            $nativeJob.HasMoreData | Should Be $true
            Receive-Job $nativeJob | Should BeExactly 2
            Remove-Job $nativeJob
            { Get-Job $nativeJob -ErrorAction Stop } | ShouldBeErrorId "JobWithSpecifiedNameNotFound,Microsoft.PowerShell.Commands.GetJobCommand"
        }
        finally {
            Remove-Job $nativeJob -Force -ErrorAction SilentlyContinue
        }
    }

    AfterAll {
        Remove-Job $job -Force
    }
}