# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$powershell = Join-Path -Path $PsHome -ChildPath "pwsh"

function Wait-JobPid {
    param (
        $Job
    )

    # This is to prevent hanging in the test.
    $startTime = [DateTime]::Now
    $TimeoutInMilliseconds = 10000

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

    $sb = {
        # use $i as an incrementally growing pause based on the attempt number
        # so that it's more likely to succeed.
        $commandStr = @'
Start-Sleep -Seconds {0}
Enter-PSHostProcess {1} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $i, $ArgumentString

        ($commandStr | & $powershell -c -) -eq $Id
    }

    $result = $false
    $failures = 0
    foreach ($i in 1..$Retry) {
        if ($sb.Invoke()) {
            $result = $true
            break
        }

        $failures++
    }

    if($failures) {
        Write-Warning "Enter-PSHostProcess script failed $i out of $Retry times."
    }

    $result
}

Describe "Enter-PSHostProcess tests" -Tag Feature {
    Context "By Process Id" {

        BeforeEach {
            # Start a normal job where the first thing it does is return $pid. After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $pwshJob = Start-Job {
                $pid
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
            Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $pwshId) }

            # This will enter and exit another process
            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$pwshId'."

            # Re-enter and exit the other process
            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to re-enter another process and grab the pid of '$pwshId'."
        }

        It "Can enter, exit, and re-enter another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            # Start a Windows PowerShell job where the first thing it does is return $pid. After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $powershellJob = Start-Job -PSVersion 5.1 {
                $pid
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            $powershellId = Wait-JobPid $powershellJob

            try {
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $powershellId) }

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
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $pwshId) }

                $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwshId)
                $rs = [runspacefactory]::CreateRunspace($npInfo)
                $rs.Open()
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('$pid').Invoke() | Should -Be $pwshId
            } finally {
                $rs.Dispose()
                $ps.Dispose()
            }
        }
    }

    Context "By CustomPipeName" {

        It "Can enter, exit, and re-enter using CustomPipeName" {
            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath -PipeName $pipeName

            # Start a job where the first thing it does is set the custom pipe name, then return $pid.
            # After that, spin forever.
            # We will use this job as the target process for Enter-PSHostProcess
            $pwshJob = Start-Job -ArgumentList $pipeName {
                [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($args[0])
                $pid
                while ($true) { Start-Sleep -Seconds 30 | Out-Null }
            }

            $pwshId = Wait-JobPid $pwshJob

            try {
                Wait-UntilTrue { Test-Path $pipePath }

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
