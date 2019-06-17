# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

using namespace System.Management.Automation.Internal

Describe 'Null Representatives' -Tags 'CI' {

    Context 'Comparisons with $null' {
        BeforeAll {
            $TestValues = @(
                @{ Value = { [AutomationNull]::Value } }
                @{ Value = { [DBNull]::Value } }
                @{ Value = { [NullString]::Value } }
            )
        }

        It '<Value> should be equivalent to $null (RHS: $null)' -TestCases $TestValues {
            param($Value)

            $Value.InvokeReturnAsIs() -eq $null | Should -BeTrue
        }

        It '$null should be equivalent to <Value> (LHS: $null)' -TestCases $TestValues {
            param($Value)

            $null -eq $Value.InvokeReturnAsIs() | Should -BeTrue
        }
    }

    Context 'Comparisons with other null representatives' {
        <#
            The only unequal null-representatives are NullString and DBNull.
            AutomationNull and $null are always considered equal already, so therefore NullString compares as
            true with both of them, as does DBNull.

            However, as NullString and DBNull have different purposes, it makes more sense to consider them unequal
            when directly compared with each other.
        #>
        It 'DBNull should not be equal to NullString' {
            [DBNull]::Value -eq [NullString]::Value | Should -BeFalse
            [NullString]::Value -eq [DBNull]::Value | Should -BeFalse
        }
    }

    Context 'Casting Behaviour' {
        BeforeAll {
            $TestValues = @(
                @{ Value = $null }
                @{ Value = [DBNull]::Value }
                @{ Value = [NullString]::Value }
                @{ Value = [System.Management.Automation.AutomationNull]::Value }
            )
        }

        It '<Value> should cast to $false' -TestCases $TestValues {
            param($Value)

            [bool]$Value | Should -BeFalse
        }

        It '<Value> should be treated as $false by Where-Object' -TestCases $TestValues {
            param($Value)

            100 | Where-Object { $Value } | Should -BeNullOrEmpty
        }
    }
}
