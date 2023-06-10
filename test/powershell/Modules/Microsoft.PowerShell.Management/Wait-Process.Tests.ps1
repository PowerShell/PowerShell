# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Wait-Process" {

    BeforeAll {
        $isNanoServer = [System.Management.Automation.Platform]::IsNanoServer
        $isIot = [System.Management.Automation.Platform]::IsIoT
        $isFullWin = $IsWindows -and !$isNanoServer -and !$isIot

        $pingCommandPath = (Get-Command -CommandType Application ping)[0].Definition

        $startProcessArgs = @{
            FilePath = $pingCommandPath
            PassThru = $true
        }
        if ($isFullWin) {
            $startProcessArgs.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        }
        $nc = $IsWindows ? "-n" : "-c"

        function longPing { Start-Process @startProcessArgs -ArgumentList "$nc 10 localhost" }
        function shortPing { Start-Process @startProcessArgs -ArgumentList "$nc 2 localhost" }
    }

    BeforeEach {
        $Processes = @( 1..3 | ForEach-Object { longPing } ) + (shortPing)
    }

    AfterEach {
        Stop-Process -InputObject $Processes
    }

    It "Should wait until all processes have exited" {
        Wait-Process -InputObject $Processes

        $Processes.Where({$_.HasExited -eq $true}).Count  | Should -Be $Processes.Count
    }

    It "Should wait until one process has exited" {
        Wait-Process -InputObject $Processes -Any

        $Processes.Where({$_.HasExited -eq $true}).Count     | Should -Be 1
        $Processes.Where({$_.HasExited -eq $false}).Count    | Should -Be ($Processes.Count - 1)
    }

    It "Should passthru all processes when all processes have exited" {
        $PassThruProcesses = Wait-Process -InputObject $Processes -PassThru

        $PassThruProcesses.Where({$_.HasExited -eq $true}).Count    | Should -Be $Processes.Count
    }

    It "Should passthru all processes when one process has exited" {
        $PassThruProcesses = Wait-Process -InputObject $Processes -Any -PassThru

        $PassThruProcesses.Where({$_.HasExited -eq $true}).Count     | Should -Be 1
        $PassThruProcesses.Where({$_.HasExited -eq $false}).Count    | Should -Be ($Processes.Count - 1)
    }
}
