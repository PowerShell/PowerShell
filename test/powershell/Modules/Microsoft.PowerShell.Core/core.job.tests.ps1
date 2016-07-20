Describe "Job cmdlets" -Tags 'innerloop', 'DRT' {
    Context "Start-Job" {
        It "should be able to load by definitionName and type" {            
            $jobname = "StartJobShouldBeAbleLoadByDefinitionNameAndType"
            try
            {
                $scheduledjob = Get-ScheduledJob -Name $jobname -ErrorAction SilentlyContinue

                if (!$scheduledjob) 
                {
                    $scheduledjob = Register-ScheduledJob -Name $jobname -ScriptBlock {echo $args[0]} -ArgumentList ($jobname)
                }

                $job = Start-Job -DefinitionName $jobname -Type "*ScheduledJob*"
                $actual = $job | Wait-Job | Receive-Job
                $actual | Should Be $jobname
            }
            finally
            {
                Remove-Job -Name $jobname -Force -ErrorAction SilentlyContinue
                Unregister-ScheduledJob -Name $jobname -Force -ErrorAction SilentlyContinue
            }
        }

        It "no recurse should not return result from child jobs" {
            $message = "StartJobNoRecurseShouldNotReturnTheResultsFromAnyChildJobs"
            try
            {
                $job = Start-Job {echo $args[0]} -ArgumentList ($message)
                $result = $job | Wait-Job | Receive-Job -NoRecurse
                $result | Should BeNullOrEmpty

                $result = $job.ChildJobs | Receive-Job
                $result | Should Be $message
            }
            finally
            {
                $job | Remove-Job -Force
            }
        }
    }
}