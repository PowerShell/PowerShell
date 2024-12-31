# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Wait-Process" {

    BeforeAll {
        $pingCommandPath = (Get-Command -CommandType Application ping)[0].Definition

        $startProcessArgs = @{
            FilePath = $pingCommandPath
            PassThru = $true
        }        
        $nc = $IsWindows ? "-n" : "-c"

        function longPing { Start-Process @startProcessArgs -ArgumentList "$nc 10 localhost" }
        function shortPing { Start-Process @startProcessArgs -ArgumentList "$nc 2 localhost" }
    }

    BeforeEach {
        $Processes = @( 1..3 | ForEach-Object { longPing } ) + ($shortPing = shortPing)
    }

    AfterEach {
        Stop-Process -InputObject $Processes
    }

    It "Should wait until all processes have exited" {
        Wait-Process -InputObject $Processes

        $Processes.Where({$_.HasExited -eq $true}).Count  | Should -Be $Processes.Count
    }

    It "Should return after all processes have exited, even if some exit before the wait starts." {
        Wait-UntilTrue -sb { $shortPing.HasExited -eq $true } -IntervalInMilliseconds 100
        Wait-Process -InputObject $Processes

        $Processes.Where({$_.HasExited -eq $true}).Count  | Should -Be $Processes.Count
    }

    It "Should return immediately if all processes have exited before the wait starts" {
        Wait-UntilTrue -sb { $Processes.HasExited -NotContains $false } -IntervalInMilliseconds 100
        Wait-Process -InputObject $Processes

        $Processes.Where({$_.HasExited -eq $true}).Count  | Should -Be $Processes.Count
    }

    It "Should return immediately if at least one process has exited before the wait starts" {
        Wait-UntilTrue -sb { $shortPing.HasExited -eq $true } -IntervalInMilliseconds 100
        Wait-Process -InputObject $Processes -Any

        $Processes.Where({$_.HasExited -eq $true}).Count   | Should -Be 1
        $Processes.Where({$_.HasExited -eq $false}).Count  | Should -Be ($Processes.Count - 1)
    }

    It "Should wait until any one process has exited" {
        Wait-Process -InputObject $Processes -Any

        $Processes.Where({$_.HasExited -eq $true}).Count   | Should -Be 1
        $Processes.Where({$_.HasExited -eq $false}).Count  | Should -Be ($Processes.Count - 1)
    }

    It "Should passthru all processes when all processes have exited" {
        $PassThruProcesses = Wait-Process -InputObject $Processes -PassThru

        $PassThruProcesses.Where({$_.HasExited -eq $true}).Count  | Should -Be $Processes.Count
    }

    It "Should passthru all processes when any one process has exited" {
        $PassThruProcesses = Wait-Process -InputObject $Processes -Any -PassThru

        $PassThruProcesses.Where({$_.HasExited -eq $true}).Count   | Should -Be 1
        $PassThruProcesses.Where({$_.HasExited -eq $false}).Count  | Should -Be ($Processes.Count - 1)
    }
}
