# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties in an enabled test host' -Tag 'CI' {
    $preferenceCases = @(
        @{ PreferenceName = 'Error'; VariableName = 'ErrorActionPreference'; DefaultPreference = 'Continue' }
        @{ PreferenceName = 'Progress'; VariableName = 'ProgressPreference'; DefaultPreference = 'Continue' }
        @{ PreferenceName = 'Warning'; VariableName = 'WarningPreference'; DefaultPreference = 'Continue' }
        @{ PreferenceName = 'Information'; VariableName = 'InformationPreference'; DefaultPreference = 'SilentlyContinue' }
        @{ PreferenceName = 'Verbose'; VariableName = 'VerbosePreference'; DefaultPreference = 'SilentlyContinue' }
        @{ PreferenceName = 'Debug'; VariableName = 'DebugPreference'; DefaultPreference = 'SilentlyContinue' }
    )

    $suspendCases = foreach ($preferenceCase in $preferenceCases) {
        $preferenceCase + @{ Value = [System.Management.Automation.ActionPreference]::Suspend; ValueKind = 'an enum' }
        $preferenceCase + @{ Value = 'Suspend'; ValueKind = 'a string' }
    }

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSScopedProperties')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'exposes the constant AllScope facade in the test host' {
        [ExperimentalFeature]::IsEnabled('PSScopedProperties') | Should -BeTrue
        (Get-Variable PS).Options | Should -Be ([System.Management.Automation.ScopedItemOptions]'Constant, AllScope')
        $PS.Style.Progress.View | Should -BeOfType ([System.Management.Automation.ProgressView])
    }

    It 'exposes the typed scalar preference defaults in the test host' {
        $PS.ActionPreference.Progress | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
        $PS.ShouldProcess.Confirm | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        $PS.ShouldProcess.WhatIf | Should -BeOfType ([bool])
        $PS.ShouldProcess.WhatIf | Should -BeFalse
        $ProgressPreference | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
        $ConfirmPreference | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        $WhatIfPreference | Should -BeOfType ([bool])
        $WhatIfPreference | Should -BeFalse
    }

    It 'exposes the typed <PreferenceName> default in a fresh runspace' -TestCases $preferenceCases {
        param($PreferenceName, $VariableName, $DefaultPreference)

        $shell = [powershell]::Create()
        try {
            $null = $shell.AddScript({
                param($name, $variableName)
                $PS.ActionPreference.$name
                (Get-Variable -Name $variableName).Value
            }).AddArgument($PreferenceName).AddArgument($VariableName)
            $values = $shell.Invoke()
            $shell.HadErrors | Should -BeFalse
            $values.Count | Should -Be 2
            foreach ($value in $values) {
                $value | Should -BeOfType ([System.Management.Automation.ActionPreference])
                $value | Should -Be ([System.Management.Automation.ActionPreference]$DefaultPreference)
            }
        } finally {
            $shell.Dispose()
        }
    }

    It 'shares typed values between the <PreferenceName> facade and legacy variable' -TestCases $preferenceCases {
        param($PreferenceName, $VariableName)

        & {
            Set-Variable -Name $VariableName -Value 'sIlEnTlYcOnTiNuE'
            $PS.ActionPreference.$PreferenceName | Should -Be ([System.Management.Automation.ActionPreference]::SilentlyContinue)
            (Get-Variable -Name $VariableName).Value | Should -BeOfType ([System.Management.Automation.ActionPreference])

            $PS.ActionPreference.$PreferenceName = 'Continue'
            (Get-Variable -Name $VariableName).Value | Should -Be ([System.Management.Automation.ActionPreference]::Continue)

            { $PS.ActionPreference.$PreferenceName = 'invalid preference' } | Should -Throw
            (Get-Variable -Name $VariableName).Value | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
        }
    }

    It 'resets a child <PreferenceName> preference to the inherited value' -TestCases $preferenceCases {
        param($PreferenceName, $VariableName)

        & {
            $PS.ActionPreference.$PreferenceName = 'Continue'
            & {
                Set-Variable -Name $VariableName -Value 'SilentlyContinue' -Scope Local
                $PS.ActionPreference.$PreferenceName | Should -Be ([System.Management.Automation.ActionPreference]::SilentlyContinue)
                $PS.Reset("aCtIoNpReFeReNcE.$PreferenceName")
                $PS.ActionPreference.$PreferenceName | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
                Get-Variable -Name $VariableName -Scope Local -ErrorAction Ignore | Should -BeNullOrEmpty
            }
            (Get-Variable -Name $VariableName).Value | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
        }
    }

    It 'rejects <PreferenceName> Suspend supplied as <ValueKind> during assignment' -TestCases $suspendCases {
        param($PreferenceName, $VariableName, $Value, $ValueKind)

        & {
            $originalPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
            Set-Variable -Name $VariableName -Value $originalPreference -Scope Local

            $reachedCommand = $false
            $record = try {
                Set-Variable -Name $VariableName -Value $Value -Scope Local -ErrorAction Stop
                $reachedCommand = $true
                & "Write-$PreferenceName" 'unexpected stream message' *>$null
            } catch {
                $_
            }

            $record.FullyQualifiedErrorId | Should -BeExactly 'InvalidActionPreference,Microsoft.PowerShell.Commands.SetVariableCommand'
            $record.CategoryInfo.Reason | Should -BeExactly 'PSInvalidCastException'
            $reachedCommand | Should -BeFalse
            (Get-Variable -Name $VariableName -Scope Local).Value | Should -BeExactly $originalPreference
            $PS.ActionPreference.$PreferenceName | Should -BeExactly $originalPreference
        }
    }

    It 'isolates progress overrides and shares them with PSStyle' {
        & {
            $PS.Style.Progress.View = 'Minimal'
            & {
                $PSStyle.Progress.View = 'Classic'
                $PS.Style.Progress.View | Should -Be ([System.Management.Automation.ProgressView]::Classic)
                $PS.Reset('Style.Progress.View')
                $PSStyle.Progress.View | Should -Be ([System.Management.Automation.ProgressView]::Minimal)
            }
            $PS.Style.Progress.View | Should -Be ([System.Management.Automation.ProgressView]::Minimal)
        }
    }
}
