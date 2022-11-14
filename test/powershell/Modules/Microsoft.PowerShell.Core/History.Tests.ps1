# Copyright (c) Microsoft Corporation.
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

    Context 'Conversions and Culture tests' {

        BeforeAll {
            $cultureTestCases = @(
                @{
                    Culture   = 'en-us'
                    StartTime = '08/18/2021 16:43:50'
                    EndTime   = '08/18/2021 16:44:50'
                }
                @{
                    Culture   = 'en-au'
                    StartTime = '18/08/2021 16:43:50'
                    EndTime   = '18/08/2021 16:44:50'
                }
            )

            $oldCulture = [cultureinfo]::CurrentCulture
        }

        AfterEach {
            [cultureinfo]::CurrentCulture = $oldCulture
        }

        It "respects current culture settings when handling datetime conversions" -TestCases $cultureTestCases {
            param($Culture, $StartTime, $EndTime)

            [cultureinfo]::CurrentCulture = [cultureinfo]::GetCultureInfo($Culture)

            $history = [PSCustomObject] @{
                CommandLine        = "test-command"
                ExecutionStatus    = [Management.Automation.Runspaces.PipelineState]::Completed
                StartExecutionTime = $StartTime
                EndExecutionTime   = $EndTime
            }

            { $history | Add-History -ErrorAction Stop } | Should -Not -Throw -Because 'the datetime should be converted according to the current culture'
        }

        It "throws an error when asked to convert a date format that doesn't match the current culture" {
            [cultureinfo]::CurrentCulture = [cultureinfo]::GetCultureInfo('en-au')
            $history = [PSCustomObject] @{
                CommandLine        = "test-command"
                ExecutionStatus    = [Management.Automation.Runspaces.PipelineState]::Completed
                StartExecutionTime = '08/18/2021 16:43:50'
                EndExecutionTime   = '08/18/2021 16:44:50'
            }

            $errorMessage = 'Cannot add history because the input object has a format that is not valid.'
            { $history | Add-History -ErrorAction Stop } | Should -Throw -ExpectedMessage $errorMessage
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

        $InformationPreference = "Continue"
        $DebugPreference = "Continue"
        $VerbosePreference = "Continue"
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
        $null = $ps.AddScript("Get-Process -id $PID")
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
        $h = Get-History -Count 1
        $h.Duration | Should -Be $duration
    }

    It "Simple recursive invocation of 'Invoke-History' can be detected" {
        Set-Content -Path $TestDrive/history.csv -Value @'
#TYPE Microsoft.PowerShell.Commands.HistoryInfo
"Id","CommandLine","ExecutionStatus","StartExecutionTime","EndExecutionTime","Duration"
"1","Invoke-History","Completed","7/16/2020 4:33:43 PM","7/16/2020 4:33:43 PM","00:00:00.0724719"
'@
        try {
            $ps = [PowerShell]::Create()
            $ps.AddScript("Import-Csv $TestDrive/history.csv | Add-History").Invoke() > $null
            $ps.Commands.Clear()
            $ps.AddCommand("Invoke-History").Invoke() > $null

            $ps.Streams.Error.Count | Should -BeExactly 1
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'InvokeHistoryLoopDetected,Microsoft.PowerShell.Commands.InvokeHistoryCommand'
        }
        finally {
            $ps.Dispose()
        }
    }

    It "Nested recursive invocation of 'Invoke-History' can be detected" {
        Set-Content -Path $TestDrive/history.csv -Value @'
#TYPE Microsoft.PowerShell.Commands.HistoryInfo
"Id","CommandLine","ExecutionStatus","StartExecutionTime","EndExecutionTime","Duration"
"1","Invoke-History 2","Completed","7/16/2020 9:54:45 PM","7/16/2020 9:54:45 PM","00:00:00.0859151"
"2","tt 1","Completed","7/16/2020 9:54:50 PM","7/16/2020 9:54:50 PM","00:00:00.1687306"
'@
        try {
            $ps = [PowerShell]::Create()
            $ps.AddScript("Import-Csv $TestDrive/history.csv | Add-History; Set-Alias -Name tt -Value Invoke-History").Invoke() > $null
            $ps.Commands.Clear()
            $ps.AddCommand("Invoke-History").Invoke() > $null

            $ps.Streams.Error.Count | Should -BeExactly 1
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly 'InvokeHistoryLoopDetected,Microsoft.PowerShell.Commands.InvokeHistoryCommand'
        }
        finally {
            $ps.Dispose()
        }
    }
}
