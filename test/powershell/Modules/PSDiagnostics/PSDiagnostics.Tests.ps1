# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe -Name "PSDiagnostics cmdlets tests" -Tag "CI","RequireAdminOnWindows" {
    Context -Name "Test for Enable-PSTrace cmdlet" {
        it -Name "Should enable Analytic logs for Microsoft-Windows-PowerShell." -Skip:(-not $IsWindows) {
            Enable-PSTrace -Force

            [XML]$WevtUtilOutput = & wevtutil gl Microsoft-Windows-PowerShell/Analytic /f:xml

            $WevtUtilOutput.Channel.enabled | Should -Be 'true'
        }
    }

    Context -Name "Test for Disable-PSTrace cmdlet" {
        it -Name "Should disable Analytic logs for Microsoft-Windows-PowerShell." -Skip:(-not $IsWindows) {
            Disable-PSTrace

            [XML]$WevtUtilOutput = & wevtutil gl Microsoft-Windows-PowerShell/Analytic /f:xml

            $WevtUtilOutput.Channel.enabled | Should -Be 'false'
        }
    }

    Context -Name "Test for Get-LogProperties cmdlet" {
        it -Name "Should show properties of Admin logs for 'Microsoft-Windows-PowerShell'." -Skip:(-not $IsWindows) {
            [XML]$WevtUtilOutput = wevtutil gl Microsoft-Windows-PowerShell/Admin /f:xml

            $LogProperty         = Get-LogProperties -Name Microsoft-Windows-PowerShell/Admin

            $LogProperty.Name       | Should -Be $WevtUtilOutput.channel.Name
            $LogProperty.Enabled    | Should -Be $WevtUtilOutput.channel.Enabled
            $LogProperty.Retention  | Should -Be $WevtUtilOutput.channel.Logging.Retention
            $LogProperty.AutoBackup | Should -Be $WevtUtilOutput.channel.Logging.AutoBackup
            $LogProperty.MaxLogSize | Should -Be $WevtUtilOutput.channel.Logging.MaxSize
        }
    }

    Context -Name "Test for Set-LogProperties cmdlet" {
        BeforeAll {
            $LogType = 'Analytic'
            [XML]$WevtUtilBefore = wevtutil gl Microsoft-Windows-PowerShell/$LogType /f:xml
            $LogPropertyToSet    = [Microsoft.PowerShell.Diagnostics.LogDetails]::new($WevtUtilBefore.channel.Name,
                                                                        $WevtUtilBefore.channel.Enabled -as [bool],
                                                                        $LogType,
                                                                        $WevtUtilBefore.channel.Logging.Retention -as [bool],
                                                                        $WevtUtilBefore.channel.Logging.AutoBackup -as [bool],
                                                                        $WevtUtilBefore.channel.Logging.MaxSize -as [int]
                                                                        )
        }

        it -Name "Should invert AutoBackup setting of $LogType logs for 'Microsoft-Windows-PowerShell'." -Skip:(-not $IsWindows) {
            $LogPropertyToSet.AutoBackup = -not $LogPropertyToSet.AutoBackup
            $LogProperty                 = Set-LogProperties -LogDetails $LogPropertyToSet -Force

            [XML]$WevtUtilOutput         = & wevtutil gl Microsoft-Windows-PowerShell/$LogType /f:xml

            $LogPropertyToSet.AutoBackup | Should -Be $WevtUtilOutput.Channel.Logging.AutoBackup
        }

        it -Name "Should throw excpetion for invalid LogName." -Skip:(-not $IsWindows) {
            {Set-LogProperties -LogDetails 'Foo' -Force } | Should -Throw -ErrorId 'ParameterArgumentTransformationError'
        }
    }
}
