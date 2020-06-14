# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Stream writer tests" -Tags "CI" {

    Context "Redirect Stream Tests" {
        # These tests validate that a stream is actually being written to by redirecting the output of that stream

        BeforeAll {
            function Write-Messages {
                [CmdletBinding()]

                param()

                Write-Verbose "Verbose message"

                Write-Debug "Debug message"

            }
        }

        BeforeEach {
            $targetfile = [System.IO.Path]::GetTempFileName()
        }

        AfterEach {
            if (Test-Path -LiteralPath $targetfile) {
                Remove-Item $targetfile
            }
        }

        It "Should write warnings to the warning stream" {
            Write-Warning "Test Warning" 3>&1 > $targetfile

            Get-Content $targetfile | Should -BeExactly "Test Warning"
        }

        It "Should write error messages to the error stream" {
            Write-Error "Testing Error" 2>&1 > $targetfile
            # The contents of the error stream should contain the expected text
            $targetfile | Should -FileContentMatch "Testing Error"
        }

        It "Should write debug messages to the debug stream" {
            Write-Messages -Debug -ErrorAction SilentlyContinue 5>&1 > $targetfile
            # The contents of the debug stream should contain the expected text
            $targetfile | Should -FileContentMatch "Debug Message"
        }

        It "Should write messages to the verbose stream" {
            Write-Messages -Verbose 4>&1 > $targetfile
            # The contents of the debug stream should contain the expected text
            $targetfile | Should -FileContentMatch "Verbose Message"
        }
    }

    Context "Error automatic variable" {
        It "Should write error messages to the `$error automatic variable" {
            Write-Error "Test Error Message" -ErrorAction SilentlyContinue

            $error[0] | Should -Match "Test Error Message"
        }
    }

    Context "Write-Information cmdlet" {
        BeforeEach {
            $ps = [powershell]::Create()
        }

        AfterEach {
            $ps.Dispose()
        }

       It "Write-Information outputs an information object" {
            # redirect the streams is sufficient
            $result = Write-Information "Test Message" *>&1
            $result.NativeThreadId | Should -Not -Be 0
            $result.ProcessId | Should -Be $PID
            $result | Should -BeOfType System.Management.Automation.InformationRecord

            # Use Match instead of Be so we can avoid dealing with a potential domain name
            $result.Computer | Should -Match "^($([environment]::MachineName)){1}(\.[a-zA-Z0-9]+)*$|^localhost$"
            if ($IsWindows)
            {
                $result.User | Should -Match ".*${env:USERNAME}"
            }
            else
            {
                $result.User | Should -Be $(whoami)
            }

            "$result" | Should -BeExactly "Test Message"
       }

       It "Write-Information accept objects from pipe" {
            $ps.AddScript("'teststring',12345 | Write-Information -InformationAction Continue").Invoke()
            $result = $ps.Streams.Information
            $result.Count | Should -Be 2
            $result[0].MessageData | Should -BeExactly "teststring"
            $result[1].MessageData | Should -BeExactly "12345"
       }

        $testInfoData = @(
            @{ Name = 'defaults'; Command = "Write-Information TestMessage"; returnCount = 1; returnValue = 'TestMessage' }
            @{ Name = '-Object'; Command = "Write-Information -MessageData TestMessage"; returnCount = 1; returnValue = 'TestMessage' }
            @{ Name = '-Message'; Command = "Write-Information -Message TestMessage"; returnCount = 1; returnValue = 'TestMessage' }
            @{ Name = '-Msg'; Command = "Write-Information -Msg TestMessage"; returnCount = 1; returnValue = 'TestMessage' }
            @{ Name = '-Tag'; Command = "Write-Information TestMessage -Tag Test"; returnCount = 1; returnValue = 'TestMessage' }
        )

        It "Write-Information works with <Name>" -TestCases $testInfoData {
            param($Command, $returnCount, $returnValue)
            $ps.AddScript($Command).Invoke()

            $result = $ps.Streams.Information

            $result.Count | Should -Be $returnCount
            (Compare-Object $result $returnValue -SyncWindow 0).length | Should -Be 0
        }

        It "Write-Information accepts `$null" {
            $streamPath = Join-Path $testdrive information.txt
            $null | Write-Information -Tags myTag -ErrorAction Stop -InformationAction SilentlyContinue -InformationVariable i
            $i.Tags | Should -BeExactly "myTag"
            $i.MessageData | Should -Be $null
        }
    }

    Context 'Stream Common Parameter Tests' {
        # These tests validate the *Variable and *Action common parameters

        $streams = if ($EnabledExperimentalFeatures.Contains('PSNewCommonParameters')) {
            @('Error', 'Warning', 'Verbose', 'Debug', 'Information', 'Progress')
        } else {
            @('Error', 'Warning', 'Information')
        }
        $streamTestCases = foreach ($stream in $streams) {
            @{
                Stream = $stream
            }
        }

        BeforeAll {
            function Test-StreamData {
                [CmdletBinding()]
                param()
                Write-Progress -Activity 'Warming up' -PercentComplete ([Math]::Round(0 / 6 * 100))
                Write-Progress -Activity 'Writing output' -Status 'Outputting an error' -PercentComplete ([Math]::Round(1 / 6 * 100))
                Write-Error -Message 'Error'
                Write-Progress -Activity 'Writing output' -Status 'Outputting a warning' -PercentComplete ([Math]::Round(2 / 6 * 100))
                Write-Warning -Message 'Warning'
                Write-Progress -Activity 'Writing output' -Status 'Outputting a verbose message' -PercentComplete ([Math]::Round(3 / 6 * 100))
                Write-Verbose -Message 'Verbose'
                Write-Progress -Activity 'Writing output' -Status 'Outputting a debug message' -PercentComplete ([Math]::Round(4 / 6 * 100))
                Write-Debug -Message 'Debug'
                Write-Progress -Activity 'Writing output' -Status 'Outputting an information message' -PercentComplete ([Math]::Round(5 / 6 * 100))
                Write-Information -MessageData 'Information'
                Write-Progress -Activity 'Cooling down' -Completed -PercentComplete ([Math]::Round(6 / 6 * 100))
            }
        }

        It 'Should be able to capture messages written to the <Stream> stream' -TestCases $streamTestCases {
            param($Stream)

            $streamData = @()
            $parameters = @{
                "${Stream}Variable" = 'streamData'
            }
            Test-StreamData @parameters *> $null

            ,$streamData | Should -BeOfType [System.Collections.ArrayList]
            $streamData.Count | Should -BeGreaterThan 0
            $streamData | Should -BeOfType "System.Management.Automation.${Stream}Record"
        }

        It 'Should be able to capture messages written to the <Stream> stream when the action is set to SilentlyContinue' -TestCases $streamTestCases {
            param($Stream)

            $streamData = @()
            $parameters = @{
                "${Stream}Variable" = 'streamData'
            }
            foreach ($streamName in $streams) {
                $parameters["${streamName}Action"] = [System.Management.Automation.ActionPreference]::SilentlyContinue
            }
            Test-StreamData @parameters *> $null

            , $streamData | Should -BeOfType [System.Collections.ArrayList]
            $streamData.Count | Should -BeGreaterThan 0
            $streamData | Should -BeOfType "System.Management.Automation.${Stream}Record"
        }

        # We only check the error stream here because the others are capturable when ignored right now (see Issue #10248)
        It 'Should not be able to capture messages written to the <Stream> stream when the action is set to Ignore' -TestCases $streamTestCases.where{ $_.Values[0] -eq 'Error' } {
            param($Stream)

            $streamData = @()
            $parameters = @{
                "${Stream}Variable" = 'streamData'
            }
            foreach ($streamName in $streams) {
                $parameters["${streamName}Action"] = [System.Management.Automation.ActionPreference]::Ignore
            }
            Test-StreamData @parameters *> $null

            , $streamData | Should -BeOfType [System.Collections.ArrayList]
            $streamData.Count | Should -Be 0
        }

        # We skip the progress stream here because it is not redirectable
        It 'Should be able to ignore all streams except for the <Stream> stream' -TestCases $streamTestCases.where{ $_.Values[0] -ne 'Progress' } {
            param($Stream)
            $parameters = @{}
            foreach ($streamName in $streams) {
                $parameters["${streamName}Action"] = if ($streamName -eq $Stream) {
                    [System.Management.Automation.ActionPreference]::Continue
                } else {
                    [System.Management.Automation.ActionPreference]::Ignore
                }
            }
            $streamData = @(Test-StreamData @parameters *>&1)

            ,$streamData | Should -BeOfType [System.Object[]]
            $streamData.Count | Should -BeGreaterThan 0
            $streamData | Should -BeOfType "System.Management.Automation.${Stream}Record"
        }

        It 'Should prefer -VerboseAction over -Verbose when both are provided' -Skip:$(-not $EnabledExperimentalFeatures.Contains('PSNewCommonParameters')) {
            $parameters = @{
                'Verbose' = $true
            }
            foreach ($streamName in $streams) {
                $parameters["${streamName}Action"] = [System.Management.Automation.ActionPreference]::Ignore
            }
            $streamData = @()
            $streamData = @(Test-StreamData @parameters *>&1)

            , $streamData | Should -BeOfType [System.Object[]]
            $streamData.Count | Should -Be 0
        }

        It 'Should prefer -DebugAction over -Debug when both are provided' -Skip:$(-not $EnabledExperimentalFeatures.Contains('PSNewCommonParameters')) {
            $parameters = @{
                'Debug' = $true
            }
            foreach ($streamName in $streams) {
                $parameters["${streamName}Action"] = [System.Management.Automation.ActionPreference]::Ignore
            }
            $streamData = @()
            $streamData = @(Test-StreamData @parameters *>&1)

            , $streamData | Should -BeOfType [System.Object[]]
            $streamData.Count | Should -Be 0
        }
    }

    Context 'Stream API tests' {
        BeforeAll {
            # Define a function to test stream output
            function Test-StreamOutput {
                [CmdletBinding()]
                param()
                $PSCmdlet.WriteError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Management.Automation.CommandNotFoundException]::new('Before'),
                        'FirstError',
                        'InvalidOperation',
                        $null))
                $ErrorActionPreference = 'Continue'
                $PSCmdlet.WriteError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Management.Automation.CommandNotFoundException]::new('After'),
                        'SecondError',
                        'InvalidOperation',
                        $null))
                $PSCmdlet.WriteWarning('Before')
                $WarningPreference = 'Continue'
                $PSCmdlet.WriteWarning('After')
                $PSCmdlet.WriteVerbose('Before')
                $VerbosePreference = 'Continue'
                $PSCmdlet.WriteVerbose('After')
                $PSCmdlet.WriteDebug('Before')
                $DebugPreference = 'Continue'
                $PSCmdlet.WriteDebug('After')
                $PSCmdlet.WriteInformation('Before', $null)
                $InformationPreference = 'Continue'
                $PSCmdlet.WriteInformation('After', $null)
                $PSCmdlet.WriteProgress(
                    [System.Management.Automation.ProgressRecord]::new(
                        1,
                        'Testing progress stream',
                        'Starting test'
                    )
                )
                $ProgressPreference = 'Continue'
                $PSCmdlet.WriteProgress(
                    [System.Management.Automation.ProgressRecord]::new(
                        1,
                        'Testing progress stream',
                        'Finished test'
                    )
                )
            }
        }

        It 'Should not output anything from stream APIs when all streams are ignored at invocation time' {
            & {
                # Ignore all messages in a child scope so that they get reset afterwards
                $ErrorActionPreference = $WarningPreference = $VerbosePreference = $DebugPreference = $InformationPreference = $ProgressPreference = 'Ignore'

                # Invoke the function and verify no stream data was returned
                Test-StreamOutput *>&1 | Should -BeNullOrEmpty
            }
        }
    }
}
