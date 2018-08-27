# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "History cmdlet test cases" -Tags "CI" {
    Context "Simple History Tests" {
        BeforeEach {
            $setting = [system.management.automation.psinvocationsettings]::New()
            $setting.AddToHistory = $true
            $ps = [PowerShell]::Create("NewRunspace")
            # we need to be sure that history is added, so use the proper
            # Invoke variant
            $null = $ps.addcommand("Get-Date").Invoke($null, $setting)
            $ps.commands.clear()
            $null = $ps.addscript("1+1").Invoke($null, $setting)
            $ps.commands.clear()
            $null = $ps.addcommand("Get-Location").Invoke($null, $setting)
            $ps.commands.clear()
        }
        AfterEach {
            $ps.Dispose()
        }
        It "Get-History returns proper history" {
            # for this case, we'll *not* add to history
            $result = $ps.AddCommand("Get-History").Invoke()
            $result.Count | Should -Be 3
            $result[0].CommandLine | Should -BeExactly "Get-Date"
            $result[1].CommandLine | Should -Be "1+1"
            $result[2].CommandLine | Should -BeExactly "Get-Location"
        }
        It "Invoke-History invokes proper command" {
            $result = $ps.AddScript("Invoke-History 2").Invoke()
            $result | Should -Be 2
        }
        It "Clear-History removes history" {
            $ps.AddCommand("Clear-History").Invoke()
            $ps.commands.clear()
            $result = $ps.AddCommand("Get-History").Invoke()
            $result | Should -BeNullOrEmpty
        }
        It "Add-History actually adds to history" {
            # add this invocation to history
            $ps.AddScript("Get-History|Add-History").Invoke($null, $setting)
            # that's 4 history lines * 2
            $ps.Commands.Clear()
            $result = $ps.AddCommand("Get-History").Invoke()
            $result.Count | Should -Be 8
            for ($i = 0; $i -lt 4; $i++) {
                $result[$i + 4].CommandLine | Should -BeExactly $result[$i].CommandLine
            }
        }
    }

    It "Tests Invoke-History on a cmdlet that generates output on all streams" {
        $streamSpammer = '
        function StreamSpammer
        {
            [CmdletBinding()]
            param()

            Write-Debug "Debug"
            Write-Error "Error"
            Write-Information "Information"
            Write-Progress "Progress"
            Write-Verbose "Verbose"
            Write-Warning "Warning"
            "Output"
        }

        $informationPreference = "Continue"
        $debugPreference = "Continue"
        $verbosePreference = "Continue"
        '

        $invocationSettings = New-Object System.Management.Automation.PSInvocationSettings
        $invocationSettings.AddToHistory = $true
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript($streamSpammer).Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("StreamSpammer");
        $null = $ps.Invoke($null, $invocationSettings)
        $ps.Commands.Clear()
        $null = $ps.AddScript("Invoke-History -id 1")
        $result = $ps.Invoke($null, $invocationSettings)
        $outputCount = $(
            $ps.Streams.Error;
            $ps.Streams.Progress;
            $ps.Streams.Verbose;
            $ps.Streams.Debug;
            $ps.Streams.Warning;
            $ps.Streams.Information).Count
        $ps.Dispose()

        ## Twice per stream - once for the original invocation, and once for the re-invocation
        $outputCount | Should -Be 12
    }

    It "Tests Invoke-History on a private command" {

        $invocationSettings = New-Object System.Management.Automation.PSInvocationSettings
        $invocationSettings.AddToHistory = $true
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("(Get-Command Get-Process).Visibility = 'Private'").Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("Get-Process -id $pid")
        $null = $ps.Invoke($null, $invocationSettings)
        $ps.Commands.Clear()
        $null = $ps.AddScript("Invoke-History -id 1")
        $result = $ps.Invoke($null, $invocationSettings)
        $errorResult = $ps.Streams.Error[0].FullyQualifiedErrorId
        $ps.Dispose()

        $errorResult | Should -BeExactly 'CommandNotFoundException'
    }

    It "HistoryInfo calculates Duration" {
        $start = [datetime]::new(2001, 01, 01, 10, 01, 01)
        $duration = [timespan] "1:2:21"
        $end = $start + $duration
        $history = [PSCustomObject] @{
            CommandLine        = "command"
            ExecutionStatus    = [Management.Automation.Runspaces.PipelineState]::Completed
            StartExecutionTime = $start
            EndExecutionTime   = $end
        }
        $history | Add-History
        $h = Get-History -count 1
        $h.Duration | Should -Be $duration
    }
}
