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

    AfterAll {
        Remove-Job $job -Force
    }
}