# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Formatting out of band data tests' -Tags 'CI' {

    Context 'Outputting out of band data renders properly' {
        
        BeforeAll {
            $formatCommandTestCases = @(
                @{
                    FormatCommand = 'Format-Table'
                    DataType      = 'TableRowEntry'
                },
                @{
                    FormatCommand = 'Format-List'
                    DataType      = 'ListViewEntry'
                },
                @{
                    FormatCommand = 'Format-Wide'
                    DataType      = 'WideViewEntry'
                },
                @{
                    FormatCommand = 'Format-Custom'
                    DataType      = 'ComplexViewEntry'
                }
            )
            $oobTypes = 'Error', 'Warning', 'Verbose', 'Debug', 'Information'
            $formatCommandWithOobTypeTestCases = foreach ($oobType in $oobTypes) {
                foreach ($item in $formatCommandTestCases) {
                    @{
                        OobType       = $oobType
                        FormatCommand = $item.FormatCommand
                        DataType      = $item.DataType
                    }
                }
            }

            function Test-OutOfBandFormatting {
                [CmdletBinding()]
                [OutputType([System.Diagnostics.Process])]
                param(
                    [Parameter(Mandatory)]
                    [ValidateSet('Error', 'Warning', 'Verbose', 'Debug', 'Information', 'Number', 'String')]
                    [string]
                    $OobType
                )
                $ErrorActionPreference = $WarningPreference = $VerbosePreference = $DebugPreference = $InformationPreference = [System.Management.Automation.ActionPreference]::Continue
                $process = Get-Process -Id $PID

                $process

                switch ($OobType) {
                    'Number' {
                        42
                        break
                    }
                    'String' {
                        'Made it'
                        break
                    }
                    default {
                        . "Write-${OobType}" "This is an out of band $($OobType.ToLower()) message."
                    }
                }
        
                $process
            }

        }

        It 'Formats <OobType>Record message data mid-stream in <FormatCommand> formatting as out of band' -TestCases $formatCommandWithOobTypeTestCases {
            param(
                [string]$OobType,
                [string]$FormatCommand
            )
            $formattedData = Test-OutOfBandFormatting -OobType $OobType *>&1 | . $FormatCommand
            $formattedData | Should -HaveCount 7
            $formattedData[3].outOfBand | Should -BeTrue
            $formattedData[3].formatEntryInfo.GetType().FullName | Should -BeExactly 'Microsoft.PowerShell.Commands.Internal.Format.ComplexViewEntry'
        }

        It 'Formats captured <OobType>Record message data in <FormatCommand> using the requested format' -TestCases $formatCommandWithOobTypeTestCases {
            param(
                [string]$OobType,
                [string]$FormatCommand,
                [string]$DataType
            )
            $message = "This is an out of band $($OobType.ToLower()) message."
            $messageData = if ($OobType -eq 'Error') {
                [System.Management.Automation.ErrorRecord]::new($message, 'Fully qualified error id', [System.Management.Automation.ErrorCategory]::InvalidOperation, 'Target name')
            } elseif ($OobType -eq 'Information') {
                [System.Management.Automation.InformationRecord]::new($message, 'The source of the message')
            } else {
                New-Object -TypeName "System.Management.Automation.${OobType}Record" -ArgumentList $message
            }
            $formattedData = $messageData | . $FormatCommand
            $formattedData | Should -HaveCount 5
            $formattedData[2].outOfBand | Should -Not -BeTrue
            $formattedData[2].formatEntryInfo.GetType().FullName | Should -BeExactly "Microsoft.PowerShell.Commands.Internal.Format.${DataType}"
        }

        It 'Formats numbers mid-stream in <FormatCommand> formatting as out of band' -TestCases $formatCommandTestCases {
            param(
                [string]$FormatCommand
            )
            $formattedData = Test-OutOfBandFormatting -OobType 'Number' | . $FormatCommand
            $formattedData | Should -HaveCount 7
            $formattedData[3].outOfBand | Should -BeTrue
            $formattedData[3].formatEntryInfo.GetType().FullName | Should -BeExactly 'Microsoft.PowerShell.Commands.Internal.Format.RawTextFormatEntry'
        }

        It 'Formats strings mid-stream in <FormatCommand> formatting as out of band' -TestCases $formatCommandTestCases {
            param(
                [string]$FormatCommand
            )
            $formattedData = Test-OutOfBandFormatting -OobType 'String' | . $FormatCommand
            $formattedData | Should -HaveCount 7
            $formattedData[3].outOfBand | Should -BeTrue
            $formattedData[3].formatEntryInfo.GetType().FullName | Should -BeExactly 'Microsoft.PowerShell.Commands.Internal.Format.RawTextFormatEntry'
        }

        It 'Formats exceptions in <FormatCommand> using the requested format' -TestCases $formatCommandTestCases {
            param(
                [string]$FormatCommand,
                [string]$DataType
            )
            $error.Clear()
            try {
                1 / 0
            } catch {
                # Do nothing
            }
            $formattedData = $error[0].Exception | . $FormatCommand
            $formattedData | Should -HaveCount 5
            $formattedData[2].outOfBand | Should -Not -BeTrue
            $formattedData[2].formatEntryInfo.GetType().FullName | Should -BeExactly "Microsoft.PowerShell.Commands.Internal.Format.${DataType}"
        }

        It 'Formats script blocks in <FormatCommand> using the requested format' -TestCases $formatCommandTestCases {
            param(
                [string]$FormatCommand,
                [string]$DataType
            )
            $formattedData = {'This is an out of band script block.'} | . $FormatCommand
            $formattedData | Should -HaveCount 5
            $formattedData[2].outOfBand | Should -Not -BeTrue
            $formattedData[2].formatEntryInfo.GetType().FullName | Should -BeExactly "Microsoft.PowerShell.Commands.Internal.Format.${DataType}"
        }
    }
}
