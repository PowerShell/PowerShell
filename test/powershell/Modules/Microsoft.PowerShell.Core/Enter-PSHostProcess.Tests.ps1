# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function Get-PipePath {
    param (
        $PipeName
    )
    if ($IsWindows) {
        return "\\.\pipe\$PipeName"
    }
    "$([System.IO.Path]::GetTempPath())CoreFxPipe_$PipeName"
}

# Executes the Enter/Exit PSHostProcess script that returns the pid of the process that's started.
function Invoke-PSHostProcessScript {
    param (
        [string] $ArgumentString,
        [int] $Id,
        [int] $Retry = 5 # Default retry of 5 times
    )

    $sb = {
        $commandStr = @'
Start-Sleep -Seconds {0}
Enter-PSHostProcess {1} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $Retry, $ArgumentString

        ($commandStr | pwsh -c -) -eq $Id
    }

    $result = $false
    foreach ($i in 0..$Retry) {
        if ($sb.Invoke()) {
            $result = $true
            break
        }

        Write-Warning "Enter-PSHostProcess attempt '$i' failed. $($Retry - $i) attempts left."
    }

    $result
}

Describe "Enter-PSHostProcess tests" -Tag Feature {
    Context "By Process Id" {

        BeforeEach {
            # Start a normal job where the first thing it does is return $pid. After that, spin forever.
            $pwshJob = Start-Job {
                $pid
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            # This will receive the pid of the Job process and nothing more since that was the only thing written to the pipeline.
            do {
                Start-Sleep -Seconds 1
                $pwshId = Receive-Job $pwshJob
            } while (!$pwshId)
        }

        AfterEach {
            $pwshJob | Stop-Job -PassThru | Remove-Job
        }

        It "Can enter, exit, and re-enter another PSHost" {
            Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $pwshId) }

            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$pwshId'."

            Invoke-PSHostProcessScript -ArgumentString "-Id $pwshId" -Id $pwshId |
                Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$pwshId'."
        }

        It "Can enter and exit another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            # Start a Windows PowerShell job where the first thing it does is return $pid. After that, spin forever.
            $powershellJob = Start-Job -PSVersion 5.1 {
                $pid
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            # This will receive the pid of the Job process and nothing more since that was the only thing written to the pipeline.
            do {
                Start-Sleep -Seconds 1
                $powershellId = Receive-Job $powershellJob
            } while (!$powershellId)

            try {
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $powershellId) }

                Invoke-PSHostProcessScript -ArgumentString "-Id $powershellId" -Id $powershellId |
                    Should -BeTrue -Because "The script was able to enter another process and grab the pid of '$pwshId'."

            } finally {
                $powershellJob | Stop-Process -Force -ErrorAction SilentlyContinue
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
            }
        }
    }

    Context "By CustomPipeName" {

        It "Can enter, exit, and re-enter using CustomPipeName" {
            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath -PipeName $pipeName

            # Start a job where the first thing it does is set the custom pipe name, then return $pid.
            # After that, spin forever.
            $pwshJob = Start-Job -ArgumentList $pipeName {
                [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($args[0])
                $pid
                while ($true) { Start-Sleep -Seconds 30 | Out-Null }
            }

            # This will receive the pid of the Job process and nothing more since that was the only thing written to the pipeline.
            do {
                Start-Sleep -Seconds 1
                $pwshId = Receive-Job $pwshJob
            } while (!$pwshId)

            try {
                Wait-UntilTrue { Test-Path $pipePath }

                Invoke-PSHostProcessScript -ArgumentString "-CustomPipeName $pipeName" -Id $pwshId |
                    Should -BeTrue -Because "The script was able to enter another process and grab the pipe of '$pipeName'."

                Invoke-PSHostProcessScript -ArgumentString "-CustomPipeName $pipeName" -Id $pwshId |
                    Should -BeTrue -Because "The script was able to enter another process and grab the pipe of '$pipeName'."

            } finally {
                $pwshJob | Stop-Job -PassThru | Remove-Job
            }
        }

        It "Should throw if CustomPipeName does not exist" {
            { Enter-PSHostProcess -CustomPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with CustomPipeName: badpipename."
        }
    }
}
