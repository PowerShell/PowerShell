# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties in an enabled test host' -Tag 'CI' {
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
        $PS.ActionPreference.Error | Should -BeOfType ([System.Management.Automation.ActionPreference])
        $PS.Style.Progress.View | Should -BeOfType ([System.Management.Automation.ProgressView])
    }

    It 'shares typed values between the facade and legacy preference variable' {
        & {
            $ErrorActionPreference = 'sIlEnTlYcOnTiNuE'
            $PS.ActionPreference.Error | Should -Be ([System.Management.Automation.ActionPreference]::SilentlyContinue)
            $ErrorActionPreference | Should -BeOfType ([System.Management.Automation.ActionPreference])

            $PS.ActionPreference.Error = 'Continue'
            (Get-Variable ErrorActionPreference).Value | Should -Be ([System.Management.Automation.ActionPreference]::Continue)

            { $PS.ActionPreference.Error = 'invalid preference' } | Should -Throw
            $ErrorActionPreference | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
        }
    }

    It 'resets a child preference to the inherited value' {
        & {
            $PS.ActionPreference.Error = 'Continue'
            & {
                $ErrorActionPreference = 'SilentlyContinue'
                $PS.ActionPreference.Error | Should -Be ([System.Management.Automation.ActionPreference]::SilentlyContinue)
                $PS.Reset('ActionPreference.Error')
                $PS.ActionPreference.Error | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
            }
            $ErrorActionPreference | Should -Be ([System.Management.Automation.ActionPreference]::Continue)
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
