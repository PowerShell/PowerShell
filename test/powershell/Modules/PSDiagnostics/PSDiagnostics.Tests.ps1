# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "PSDiagnostics cmdlets tests" -Tag "CI", "RequireAdminOnWindows" {
    BeforeAll {
        $OriginalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( -not $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    AfterAll {
        $Global:PSDefaultParameterValues = $OriginalDefaultParameterValues
    }

    Context "Test for Enable-PSTrace cmdlet" {
        it "Should enable Analytic logs for Microsoft-Windows-PowerShell." {
            Enable-PSTrace -Force

            [XML]$WevtUtilOutput = & wevtutil gl Microsoft-Windows-PowerShell/Analytic /f:xml

            $WevtUtilOutput.Channel.enabled | Should -Be 'true'
        }
    }

    Context "Test for Disable-PSTrace cmdlet" {
        it "Should disable Analytic logs for Microsoft-Windows-PowerShell." {
            Disable-PSTrace

            [XML]$WevtUtilOutput = & wevtutil gl Microsoft-Windows-PowerShell/Analytic /f:xml

            $WevtUtilOutput.Channel.enabled | Should -Be 'false'
        }
    }

    Context "Test for Get-LogProperties cmdlet" {
        it "Should show properties of Admin logs for 'Microsoft-Windows-PowerShell'." {
            [XML]$WevtUtilOutput = wevtutil gl Microsoft-Windows-PowerShell/Admin /f:xml

            $LogProperty = Get-LogProperties -Name Microsoft-Windows-PowerShell/Admin

            $LogProperty.Name       | Should -Be $WevtUtilOutput.channel.Name
            $LogProperty.Enabled    | Should -Be $WevtUtilOutput.channel.Enabled
            $LogProperty.Retention  | Should -Be $WevtUtilOutput.channel.Logging.Retention
            $LogProperty.AutoBackup | Should -Be $WevtUtilOutput.channel.Logging.AutoBackup
            $LogProperty.MaxLogSize | Should -Be $WevtUtilOutput.channel.Logging.MaxSize
        }
    }

    Context "Test for Set-LogProperties cmdlet" {
        BeforeAll {
            $LogType = 'Analytic'
            if ($IsWindows) {
                [XML]$WevtUtilBefore = wevtutil gl Microsoft-Windows-PowerShell/$LogType /f:xml
                $LogPropertyToSet = [Microsoft.PowerShell.Diagnostics.LogDetails]::new($WevtUtilBefore.channel.Name,
                    [bool]::Parse($WevtUtilBefore.channel.Enabled),
                    $LogType,
                    [bool]::Parse($WevtUtilBefore.channel.Logging.Retention),
                    [bool]::Parse($WevtUtilBefore.channel.Logging.AutoBackup),
                    $WevtUtilBefore.channel.Logging.MaxSize -as [int]
                )
            }
        }

        it "Should invert AutoBackup setting of $LogType logs for 'Microsoft-Windows-PowerShell'." {
            $LogPropertyToSet.AutoBackup = -not $LogPropertyToSet.AutoBackup
            Set-LogProperties -LogDetails $LogPropertyToSet -Force

            [XML]$WevtUtilOutput = & wevtutil gl Microsoft-Windows-PowerShell/$LogType /f:xml
            (Get-LogProperties -Name Microsoft-Windows-PowerShell/$LogType).AutoBackup | Should -Be ([bool]::Parse($WevtUtilOutput.Channel.Logging.AutoBackup))
        }

        it "Should throw excpetion for invalid LogName." {
            {Set-LogProperties -LogDetails 'Foo' -Force } | Should -Throw -ErrorId 'ParameterArgumentTransformationError'
        }
    }
}
