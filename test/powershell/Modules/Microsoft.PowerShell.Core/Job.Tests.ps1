Describe "Job Cmdlet Tests" -Tag "CI" {
    Context "Simple Jobs" {
        AfterEach {
            Get-Job | Remove-Job
        }
        BeforeEach {
            $j = start-job -scriptblock { 1 + 1 }
        }
        It "Start-Job produces a job object" {
            $j.gettype().fullname | should be "System.Management.Automation.PSRemotingJob"
        }
        It "Get-Job retrieves a job object" {
            (Get-Job -id $j.id).gettype().fullname | should be "System.Management.Automation.PSRemotingJob"
        }
        It "Remove-Job can remove a job" {
            remove-job $j
            try {
                get-job $j -ea Stop
                throw "Execution OK"
            }
            catch {
                $_.FullyQualifiedErrorId | should be "JobWithSpecifiedNameNotFound,Microsoft.PowerShell.Commands.GetJobCommand"
            }
        }
        It "Receive-Job can retrieve job results" -pending:(!$IsWindows) {
            $result = receive-job -id $j.id
            $result.id | Should be 2
        }
    }
    Context "jobs which take time" {
        BeforeEach {
            $j = start-job -scriptblock { Start-Sleep 5 }
        }
        AfterEach {
            Get-Job | Remove-Job
        }
        It "Wait-Job will wait for a job" {
            $result = wait-Job $j
            $result | should be $j
        }
        It "Stop-Job will stop a job" -pending:(!$IsWindows) {
            Stop-Job -id $j.id
            $result = get-job -id $j.id
            $result.Status |Should be Stopped
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
    It "Debug-Job will break into debugger" -pending:(!$IsWindows) {
        $ps.AddScript('$job = start-job { 1..300 | % { sleep 1 } }').Invoke()
        $ps.Commands.Clear()
        $ps.Runspace.Debugger.GetCallStack() | Should BeNullOrEmpty
        start-sleep 3
        $asyncResult = $ps.AddScript('debug-job $job').BeginInvoke()
        $ps.commands.clear()
        start-sleep 2
        $result = $ps.runspace.Debugger.GetCallStack()
        $result.Command | Should be "<ScriptBlock>"
    }
}
