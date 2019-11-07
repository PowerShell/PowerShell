# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Tests for splatting' -Tags 'CI' {
    Context 'Splatting members and indexed data' {
        BeforeAll {
            function Test-Splat {
                [CmdletBinding(DefaultParameterSetName = 'Default')]
                param(
                    [Parameter(ParameterSetName = 'Value')]
                    [ValidateNotNullOrEmpty()]
                    [string]
                    $Value,

                    [Parameter(ParameterSetName = 'Value2')]
                    [ValidateNotNullOrEmpty()]
                    [string]
                    $Value2
                )
                $PSCmdlet.ParameterSetName
            }
        }

        It 'Should splat property values properly' {
            $o = [pscustomobject]@{
                Parameters = @{
                    Value = 'Does it splat?'
                }
            }
            Test-Splat @o.Parameters | Should -BeExactly 'Value'
        }

        It 'Should splat method results properly' {
            $o = [pscustomobject]@{
                Parameters = @{
                    Value = 'Does it splat?'
                }
            }
            Add-Member -InputObject $o -Name GetParams -MemberType ScriptMethod -Value { $this.Parameters }
            Test-Splat @o.GetParams() | Should -BeExactly 'Value'
        }

        It 'Should splat indexed method results properly' {
            $o = [pscustomobject]@{
                Parameters = @(
                    @{
                        Value = 'Will this splat?'
                    }
                    @{
                        Value2 = 'Or this?'
                    }
                )
            }
            Add-Member -InputObject $o -Name GetParams -MemberType ScriptMethod -Value { $this.Parameters }
            Test-Splat @o.GetParams()[1] | Should -BeExactly 'Value2'
        }

        It 'Should splat indexed data properly' {
            $o = @(
                @{
                    Value = 'Will this splat?'
                }
                @{
                    Value2 = 'Or this?'
                }
            )
            Test-Splat @o[1] | Should -BeExactly 'Value2'
        }

        It 'Should splat multiple members properly' {
            $o = [pscustomobject]@{
                Parameters = @{
                    Nested = @{
                        Value = 'Does it splat?'
                    }
                }
            }
            Add-Member -InputObject $o -Name GetParams -MemberType ScriptMethod -Value { $this.Parameters }
            Test-Splat @o.GetParams().Nested | Should -BeExactly 'Value'
        }

        It 'Should splat $PSCmdlet.MyInvocation.BoundParameters properly' {
            & {
                [CmdletBinding()]
                param(
                    $Value
                )
                Test-Splat @PSCmdlet.MyInvocation.BoundParameters
            } -Value 'Hello' | Should -BeExactly 'Value'
        }
    }

    Context 'Splatting members and indexed data with the using statement' {
        BeforeAll {
            $skipTest = -not $EnabledExperimentalFeatures.Contains('PSForEachObjectParallel')
            if ($skipTest) {
                Write-Verbose "Tests Skipped. These tests require the experimental feature 'PSForEachObjectParallel' to be enabled." -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
            }
        }

        AfterAll {
            if ($skipTest) {
                $global:PSDefaultParameterValues = $originalDefaultParameterValues
            }
        }

        It 'Should splat indexed data and using: properly' {
            $o = @(
                @{
                    InputObject = 'First'
                }
                @{
                    InputObject = 'Second'
                }
            )
            Add-Member -InputObject $o -Name GetParams -MemberType ScriptMethod -Value { $this.Parameters[$args[0]] }
            1..1 | ForEach-Object -Parallel { Write-Output @using:o[1] } | Should -BeExactly 'Second'
        }

        It 'Should splat with properties and indexed data and using: properly' {
            $o = [pscustomobject]@{
                Parameters = @(
                    @{
                        InputObject = 'First'
                    }
                    @{
                        InputObject = 'Second'
                    }
                )
            }
            Add-Member -InputObject $o -Name GetParams -MemberType ScriptMethod -Value { $this.Parameters[$args[0]] }
            1..1 | ForEach-Object -Parallel { Write-Output @using:o.Parameters[1] } | Should -BeExactly 'Second'
        }
    }
}
