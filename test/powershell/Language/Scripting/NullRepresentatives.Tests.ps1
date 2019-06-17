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
        BeforeAll {
            $TestValues = @(
                @{ LHS = { [AutomationNull]::Value }; RHS = { [DBNull]::Value } }
                @{ LHS = { [AutomationNull]::Value }; RHS = { [NullString]::Value } }
                @{ LHS = { [DBNull]::Value }; RHS = { [NullString]::Value } }
            )
        }

        It '<LHS> should not be equal to <RHS>' -TestCases $TestValues {
            param($LHS, $RHS)

            $LHS.InvokeReturnAsIs() -eq $RHS.InvokeReturnAsIs() | Should -BeFalse
            $RHS.InvokeReturnAsIs() -eq $RHS.InvokeReturnAsIs() | Should -BeFalse
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
