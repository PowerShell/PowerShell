# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'SuspiciousContentChecker verification' -Tags "CI" {
    BeforeAll {
        $type = [psobject].Assembly.GetType('System.Management.Automation.ScriptBlock+SuspiciousContentChecker')

        $testCases = @(
            @{ number = 1; text = "add-TyPe"; expected = "Add-Type" }
            @{ number = 2; text = "GetDelegateForFunctionPointer"; expected = "GetDelegateForFunctionPointer" }
            @{ number = 3; text = "ZeroFreeGlobalAllocUnicode"; expected = "ZeroFreeGlobalAllocUnicode" }
            @{ number = 4; text = "Hello world emit new"; expected = "Emit" }
            @{ number = 5; text = "xxxx yyyyMakeByRefTypecccc Type 'help' to get help"; expected = "MakeByRefType" }
            @{ number = 9; text = "emjt TypeHandlebegood"; expected = "TypeHandle" }

            @{ number = 6; text = "PowerShell Preview Extension v2022.11.2"; expected = $null }
            @{ number = 7; text = "add-typu"; expected = $null }
            @{ number = 8; text = "GetDelegateForFunctionPointfr"; expected = $null }
            @{ number = 9; text = "emjt TypeHandlfe"; expected = $null }
        )
    }

    It "Smoke testing the suspicious content detection - <number>" -TestCases $testCases {
        param($text, $expected)

        $type::Match($text) | Should -BeExactly $expected
    }
}
