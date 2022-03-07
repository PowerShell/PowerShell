# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"

function Wait-JobPid {
    param (
        $Job
    )

    # This is to prevent hanging in the test.
    # Some test environments (such as raspberry_pi) require more time for background job to run.
    $startTime = [DateTime]::Now
    $TimeoutInMilliseconds = 60000

    # This will receive the pid of the Job process and nothing more since that was the only thing written to the pipeline.
    do {
        Start-Sleep -Seconds 1
        $pwshId = Receive-Job $Job

        if (([DateTime]::Now - $startTime).TotalMilliseconds -gt $timeoutInMilliseconds) {
            throw "Unable to receive PowerShell process id."
        }
    } while (!$pwshId)

    $pwshId
}

# Executes the Enter/Exit PSHostProcess script that returns the pid of the process that's started.
function Invoke-PSHostProcessScript {
    param (
        [string] $ArgumentString,
        [int] $Id,
        [int] $Retry = 5 # Default retry of 5 times
    )

    $commandStr = @'
Enter-PSHostProcess {0} -ErrorAction Stop
$PID
Exit-PSHostProcess
'@ -f $ArgumentString

    $result = $false
    foreach ($i in 1..$Retry) {
        # use $i as an incrementally growing pause based on the attempt number
        # so that it's more likely to succeed.
        Start-Sleep -Seconds $i

        $result = ($commandStr | & $powershell -noprofile -c -) -eq $Id
        if ($result) {
            break
        }
    }

    if ($i -gt 1) {
        Write-Verbose -Verbose "Enter-PSHostProcess script failed $i out of $Retry times."
    }

    $result
}

Describe "Enter-PSHostProcess tests" -Tag Feature {
    Context "By Process Id" {

        BeforeAll {
            $oldColor = $env:NO_COLOR
            $env:NO_COLOR = 1
        }

        AfterAll {
            $env:NO_COLOR = $oldColor
        }

        BeforeEach {
            # Start a normal job where the first thing it does is return $PID. After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $pwshJob = Start-Job {
                $PID
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            $pwshId = Wait-JobPid $pwshJob
        }

        AfterEach {
            $pwshJob | Stop-Job -PassThru | Remove-Job
        }

        It "Can enter, exit, and re-enter another PSHost" {
            Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $pwshId) } | Should -BeTrue

            # This will enter and exit another process
            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$pwshId'"

            # Re-enter and exit the other process
            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to re-enter another process and grab the pid of '$pwshId'."
        }

        It "Can enter, exit, and re-enter another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            # Start a PowerShell job where the first thing it does is return $PID. After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $powershellJob = Start-Job -PSVersion 5.1 {
                $PID
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            $powershellId = Wait-JobPid $powershellJob

            try {
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $powershellId) } | Should -BeTrue

                # This will enter and exit another process
                Invoke-PSHostProcessScript -ArgumentString "-Id $powershellId" -Id $powershellId |
                    Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$powershellId'."

                # Re-enter and exit the other process
                Invoke-PSHostProcessScript -ArgumentString "-Id $powershellId" -Id $powershellId |
                    Should -BeTrue -Because "The script was able to re-enter another process and grab the pid of '$powershellId'."

            } finally {
                $powershellJob | Stop-Job -PassThru | Remove-Job
            }
        }

        It "Can enter using NamedPipeConnectionInfo" {
            try {
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $pwshId) } | Should -BeTrue

                $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwshId)
                $rs = [runspacefactory]::CreateRunspace($npInfo)

                # Try to open the runspace while tracing.
                $splat = @{
                    Name = "RunspaceInit"
                    Expression = {$Input.Open()}
                    PSHost = $true
                    ListenerOption = [System.Diagnostics.TraceOptions]::Callstack
                    FilePath = "$TestDrive/$([System.IO.Path]::GetRandomFileName()).log"
                    InputObject = $rs
                }
                Trace-Command @splat

                # If opening the runspace fails, then print out the trace with the callstack
                Wait-UntilTrue { $rs.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opened } |
                    Should -BeTrue -Because (Get-Content $splat.FilePath -Raw)

                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('$PID')

                [int]$retry = 0
                $result = $null
                $errorMsg = "Exception: "
                while ($retry -lt 5 -and $result -eq $null) {
                    try {
                        $result = $ps.Invoke()
                    }
                    catch [System.Management.Automation.Runspaces.InvalidRunspaceStateException] {
                        $errorMsg += $_.Exception.InnerException.Message + "; "
                        $retry++
                        Start-Sleep -Milliseconds 100
                    }
                }

                $result | Should -Be $pwshId -Because $errorMsg
            } finally {
                # Clean up disposables
                if ($rs) {
                    $rs.Dispose()
                }

                if ($ps) {
                    $ps.Dispose()
                }
            }
        }
    }

    Context "By CustomPipeName" {

        BeforeAll {
            $oldColor = $env:NO_COLOR
            $env:NO_COLOR = 1
        }

        AfterAll {
            $env:NO_COLOR = $oldColor
        }

        It "Can enter, exit, and re-enter using CustomPipeName" {
            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath -PipeName $pipeName

            # Start a job where the first thing it does is set the custom pipe name, then return $PID.
            # After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $pwshJob = Start-Job -ArgumentList $pipeName {
                [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($args[0])
                $PID
                while ($true) { Start-Sleep -Seconds 30 | Out-Null }
            }

            $pwshId = Wait-JobPid $pwshJob

            try {
                Wait-UntilTrue { Test-Path $pipePath } | Should -BeTrue

                # This will enter and exit another process
                Invoke-PSHostProcessScript -ArgumentString "-CustomPipeName $pipeName" -Id $pwshId |
                    Should -BeTrue -Because "The script was able to enter another process and grab the pipe of '$pipeName'."

                # Re-enter and exit the other process
                Invoke-PSHostProcessScript -ArgumentString "-CustomPipeName $pipeName" -Id $pwshId |
                    Should -BeTrue -Because "The script was able to re-enter another process and grab the pipe of '$pipeName'."

            } finally {
                $pwshJob | Stop-Job -PassThru | Remove-Job
            }
        }

        It "Should throw if CustomPipeName does not exist" {
            { Enter-PSHostProcess -CustomPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with CustomPipeName: badpipename."
        }
    }
}
