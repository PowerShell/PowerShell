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
                @{ Value = { $null } }
                @{ Value = { [DBNull]::Value } }
                @{ Value = { [NullString]::Value } }
                @{ Value = { [AutomationNull]::Value } }
            )
        }

        It '<Value> should cast to $false' -TestCases $TestValues {
            param($Value)

            [bool]($Value.InvokeReturnAsIs()) | Should -BeFalse
        }

        It '-not <Value> should be $true' -TestCases $TestValues {
            param($Value)

            -not $Value.InvokeReturnAsIs() | Should -BeTrue
        }

        It '<Value> should be treated as $false by Where-Object' -TestCases $TestValues {
            param($Value)

            100 | Where-Object { $Value.InvokeReturnAsIs() } | Should -BeNullOrEmpty
        }
    }

    Context 'Collection Comparisons' {
        BeforeAll {
            $NullArray = $null, $null, [DBNull]::Value, $null, $null, [NullString]::Value
        }

        It '<Value> should correctly filter the array and return <ExpectedCount> results' {
            param($Value, $ExpectedCount)

            $NullArray -eq $Value | Should -HaveCount $ExpectedCount
        } -TestCases @(
            @{ Value = $null; ExpectedCount = 6 }
            @{ Value = [DBNull]::Value; ExpectedCount = 5 }
            @{ Value = [NullString]::Value; ExpectedCount = 5 }
        )
    }
}
