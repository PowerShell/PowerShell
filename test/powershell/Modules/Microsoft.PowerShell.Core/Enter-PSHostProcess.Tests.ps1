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

@'
Start-Sleep -Seconds 1
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pwshId | pwsh -c - | Should -Be $pwshId

@'
Start-Sleep -Seconds 1
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pwshId | pwsh -c - | Should -Be $pwshId

        }

        It "Can enter and exit another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            # Start a Windows PowerShell job where the first thing it does is return $pid. After that, spin forever.
            $powershellJob = Start-Job -PSVersion 5.1 {
                $pid
                while ($true) {
                    Start-Sleep -Seconds 30 | Out-Null
                }
            }

            do {
                Start-Sleep -Seconds 1
                $powershellId = Receive-Job $powershellJob
            } while (!$powershellId)

            try {
                Wait-UntilTrue { [bool](Get-PSHostProcessInfo -Id $powershellId) }

@'
Start-Sleep -Seconds 1
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $powershellId | pwsh -c - | Should -Be $powershellId

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
                $pid;
                while ($true) { Start-Sleep -Seconds 30 | Out-Null }
            }

            do {
                Start-Sleep -Seconds 1
                $pwshId = Receive-Job $pwshJob
            } while (!$pwshId)

            try {
                Wait-UntilTrue { Test-Path $pipePath }

@'
Start-Sleep -Seconds 1
Enter-PSHostProcess -CustomPipeName {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pipeName | pwsh -c - | Should -Be $pwshId

@'
Start-Sleep -Seconds 1
Enter-PSHostProcess -CustomPipeName {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pipeName | pwsh -c - | Should -Be $pwshId

            } finally {
                $pwshJob | Stop-Job -PassThru | Remove-Job
            }
        }

        It "Should throw if CustomPipeName does not exist" {
            { Enter-PSHostProcess -CustomPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with CustomPipeName: badpipename."
        }
    }
}
