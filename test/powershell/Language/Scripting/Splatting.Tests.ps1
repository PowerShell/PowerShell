# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Tests for splatting' -Tags 'CI' {
    Context 'Splatting members' {
        BeforeAll {
            function Test-Splat {
                [CmdletBinding(DefaultParameterSetName = 'Default')]
                param(
                    [Parameter(ParameterSetName = 'Value')]
                    [ValidateNotNullOrEmpty()]
                    [string]
                    $Value
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
}
