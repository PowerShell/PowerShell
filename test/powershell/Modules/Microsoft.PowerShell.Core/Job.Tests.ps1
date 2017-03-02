Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1
Describe "Job Cmdlet Tests" -Tag "CI" {
    Context "Simple Jobs" {
        BeforeEach {
            $j = Start-Job -ScriptBlock { 1 + 1 } -Name "My Job"
        }
        AfterEach {
            Get-Job | Remove-Job -Force
        }
        It "Start-Job produces a job object" {
            $j | Should BeOfType "System.Management.Automation.Job"
            $j.Name | Should Be "My Job"
        }
        It "Get-Job retrieves a job object" {
            (Get-Job -Id $j.Id) | Should BeOfType "System.Management.Automation.Job"
        }
        It "Get-Job retrieves an array of job objects" {
            Start-Job -ScriptBlock { 2 * 2 }
            $jobs = Get-Job
            $jobs.Count | Should Be 2
            foreach ($job in $jobs)
            {
                $job | Should BeOfType "System.Management.Automation.Job"
            }
        }
        It "Remove-Job can remove a job" {
            Remove-Job $j -Force
            { Get-Job $j -ErrorAction Stop } | ShouldBeErrorId "JobWithSpecifiedNameNotFound,Microsoft.PowerShell.Commands.GetJobCommand"
        }
        It "Receive-Job can retrieve job results" {
            Wait-Job -Timeout 60 -id $j.id | Should Not BeNullOrEmpty
            receive-job -id $j.id | Should be 2
        }
    }
    Context "Jobs with arguments" {
        It "Start-Job accepts arguments" {
            $sb = { Write-Output $args[1]; Write-Output $args[0] }
            $j = Start-Job -ScriptBlock $sb -ArgumentList "$TestDrive", 42
            Wait-job -Timeout (5 * 60) $j | Should Be $j
            $r = Receive-Job $j
            $r -Join "," | Should Be "42,$TestDrive"
        }
    }
    Context "jobs which take time" {
        BeforeEach {
            $j = Start-Job -ScriptBlock { Start-Sleep 15 }
        }
        AfterEach {
            Get-Job | Remove-Job -Force
        }
        It "Wait-Job will wait for a job" {
            $result = Wait-Job $j
            $result | Should Be $j
            $j.State | Should Be "Completed"
        }
        It "Wait-Job will timeout waiting for a job" {
            $result = Wait-Job -Timeout 2 $j
            $result | Should Be $null
        }
        It "Stop-Job will stop a job" {
            Stop-Job -Id $j.Id
            $j.State | Should Be "Stopped"
        }
        It "Remove-Job will not remove a running job" {
            $id = $j.Id
            Remove-Job $j -ErrorAction SilentlyContinue
            $job = Get-Job -Id $id
            $job | Should Be $j
        }
        It "Remove-Job -Force will remove a running job" {
            $id = $j.Id
            Remove-Job $j -Force
            $job = Get-Job -Id $id -ErrorAction SilentlyContinue
            $job | Should Be $null
        }
    }
    Context "Retrieving partial output from jobs" {
        BeforeAll {
            function GetResults($job, $n, $keep)
            {
                $results = @()

                # $count allows us to bail out after 5 minutes, avoiding an endless loop
                for ($count = 0; $results.Count -lt $n; $count++)
                {
                    if ($count -eq 1000)
                    {
                        # It's been 5 minutes and we still have collected enough results!
                        throw "Receive-Job behaves suspiciously: Cannot receive $n results in 5 minutes."
                    }

                    # sleep for 300 ms to allow data to be produced
                    Start-Sleep -Milliseconds 300

                    if ($keep)
                    {
                        $results = Receive-Job -Keep $job
                    }
                    else
                    {
                        $results += Receive-Job $job
                    }
                }

                return $results
            }

            function CheckContent($array)
            {
                for ($i=1; $i -lt $array.Count; $i++)
                {
                    if ($array[$i] -ne ($array[$i-1] + 1))
                    {
                        return $false
                    }
                }

                return $true
            }

        }
        BeforeEach {
            $j = Start-Job -ScriptBlock { $count = 1; while ($true) { Write-Output ($count++); Start-Sleep -Milliseconds 30 } }
        }
        AfterEach {
            Get-Job | Remove-Job -Force
        }

        It "Receive-Job will retrieve partial output" {
            $result1 = GetResults $j 5 $false
            $result2 = GetResults $j 5 $false
            CheckContent ($result1 + $result2) | Should Be $true
        }
        It "Receive-Job will retrieve partial output, including -Keep results" {
            $result1 = GetResults $j 5 $true
            $result2 = GetResults $j ($result1.Count + 5) $false
            Compare-Object -SyncWindow 0 -PassThru $result1 $result2[0..($result1.Count-1)] | Should Be $null
            $result2[$result1.Count - 1] + 1 | Should Be $result2[$result1.Count]
        }
    }
}
Describe "Debug-job test" -tag "Feature" {
    BeforeAll {
        $rs = [runspacefactory]::CreateRunspace()
        $rs.Open()
        $rs.Debugger.SetDebugMode([System.Management.Automation.DebugModes]::RemoteScript)
        $rs.Debugger.add_DebuggerStop({$true})
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
    }
    AfterAll {
        $rs.Dispose()
        $ps.Dispose()
    }
    # we check this via implication.
    # if we're debugging a job, then the debugger will have a callstack
    It "Debug-Job will break into debugger" -pending {
        $ps.AddScript('$job = start-job { 1..300 | % { sleep 1 } }').Invoke()
        $ps.Commands.Clear()
        $ps.Runspace.Debugger.GetCallStack() | Should BeNullOrEmpty
        Start-Sleep 3
        $asyncResult = $ps.AddScript('debug-job $job').BeginInvoke()
        $ps.commands.clear()
        Start-Sleep 2
        $result = $ps.runspace.Debugger.GetCallStack()
        $result.Command | Should be "<ScriptBlock>"
    }
}
