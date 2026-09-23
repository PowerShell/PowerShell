# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "PSDiagnostics cmdlets tests." -Tag "CI", "RequireAdminOnWindows" {
    BeforeAll {
        $LogType = 'Analytic'
        $OriginalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not $IsWindows) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else{
            $LogSettingBak = Get-LogProperties -Name PowerShellCore/$LogType
        }
    }
    AfterAll {
        if ($IsWindows) {
            Set-LogProperties -LogDetails $LogSettingBak -Force
        }
        $Global:PSDefaultParameterValues = $OriginalDefaultParameterValues
    }

    Context "Test for Enable-PSTrace and Disable-PSTrace cmdlets." {
        It "Should enable $LogType logs for PowerShellCore." {
            [XML]$CurrentSetting = & wevtutil gl PowerShellCore/$LogType /f:xml
            if($CurrentSetting.Channel.Enabled -eq 'true'){
                & wevtutil sl PowerShellCore/$LogType /e:false /q
            }

            Enable-PSTrace -Force

            [XML]$ExpectedOutput = & wevtutil gl PowerShellCore/$LogType /f:xml

            $ExpectedOutput.Channel.enabled | Should -BeExactly 'true'
        }

        It "Should disable $LogType logs for PowerShellCore." {
            [XML]$CurrentState = & wevtutil gl PowerShellCore/$LogType /f:xml
            if($CurrentState.channel.enabled -eq 'false'){
                & wevtutil sl PowerShellCore/$LogType /e:true /q
            }
            Disable-PSTrace

            [XML]$ExpectedOutput = & wevtutil gl PowerShellCore/$LogType /f:xml

            $ExpectedOutput.Channel.enabled | Should -Be 'false'
        }
    }

    Context "Test for Get-LogProperties cmdlet." {
        It "Should return properties of $LogType logs for 'PowerShellCore'." {
            [XML]$ExpectedOutput = wevtutil gl PowerShellCore/$LogType /f:xml

            $LogProperty = Get-LogProperties -Name PowerShellCore/$LogType

            $LogProperty.Name       | Should -Be $ExpectedOutput.channel.Name
            $LogProperty.Enabled    | Should -Be $ExpectedOutput.channel.Enabled
            $LogProperty.Retention  | Should -Be $ExpectedOutput.channel.Logging.Retention
            $LogProperty.AutoBackup | Should -Be $ExpectedOutput.channel.Logging.AutoBackup
            $LogProperty.MaxLogSize | Should -Be $ExpectedOutput.channel.Logging.MaxSize

            #Verifying the property count. Adding 2 to count from the wevtutil output as the Enabled and Name property as the count is taken only for Logging property.
            ($LogProperty | Get-Member -MemberType Property | Measure-Object).Count |
            Should -Be (($ExpectedOutput.Channel.Logging | Get-Member -MemberType Property | Measure-Object).Count + 2)
        }
    }

    Context "Test for Set-LogProperties cmdlet." {
        BeforeAll {
            if ($IsWindows) {
                [XML]$WevtUtilBefore = wevtutil gl PowerShellCore/$LogType /f:xml
                $LogPropertyToSet = [Microsoft.PowerShell.Diagnostics.LogDetails]::new($WevtUtilBefore.channel.Name,
                    [bool]::Parse($WevtUtilBefore.channel.Enabled),
                    $LogType,
                    [bool]::Parse($WevtUtilBefore.channel.Logging.Retention),
                    [bool]::Parse($WevtUtilBefore.channel.Logging.AutoBackup),
                    $WevtUtilBefore.channel.Logging.MaxSize -as [int]
                )
            }
        }

        It "Should invert AutoBackup setting of $LogType logs for 'PowerShellCore'." {
            $LogPropertyToSet.AutoBackup = -not $LogPropertyToSet.AutoBackup
            Set-LogProperties -LogDetails $LogPropertyToSet -Force

            [XML]$ExpectedOutput = & wevtutil gl PowerShellCore/$LogType /f:xml
            (Get-LogProperties -Name PowerShellCore/$LogType).AutoBackup | Should -Be ([bool]::Parse($ExpectedOutput.Channel.Logging.AutoBackup))
        }

        It "Should throw exception for invalid LogName." {
            {Set-LogProperties -LogDetails 'Foo' -Force } | Should -Throw -ErrorId 'ParameterArgumentTransformationError'
        }
    }
}
