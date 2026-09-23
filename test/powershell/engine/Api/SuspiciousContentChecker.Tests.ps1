# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'SuspiciousContentChecker verification' -Tags "CI" {
    BeforeAll {
        $type = [psobject].Assembly.GetType('System.Management.Automation.ScriptBlock+SuspiciousContentChecker')

        $testCases = @(
            @{ id = 'Should Detect (1)'; text = "add-TyPe"; expected = "Add-Type" }
            @{ id = 'Should Detect (2)'; text = "GetDelegateForFunctionPointer"; expected = "GetDelegateForFunctionPointer" }
            @{ id = 'Should Detect (3)'; text = "ZeroFreeGlobalAllocUnicode"; expected = "ZeroFreeGlobalAllocUnicode" }
            @{ id = 'Should Detect (4)'; text = "Hello world emit new"; expected = "Emit" }
            @{ id = 'Should Detect (5)'; text = "xxxx yyyyMakeByRefTypecccc Type 'help' to get help"; expected = "MakeByRefType" }
            @{ id = 'Should Detect (6)'; text = "emjt TypeHandlebegood"; expected = "TypeHandle" }
            @{ id = 'Should Detect (7)'; text = "emit*&)(@~>-type"; expected = "Emit" }
            @{ id = 'Should Detect (8)'; text = "*&)(@~>-typeemit"; expected = "Emit" }
            @{ id = 'Should Detect (9)'; text = "Type`u{48}andle`u{2122}"; expected = "TypeHandle" }
            @{ id = 'Should Detect (10)'; text = "`u{2122}Type`u{48}andle`u{2122}"; expected = "TypeHandle" }
            @{ id = 'Should Detect (11)'; text = "`u{D83D}`u{DE00}Type`u{48}andle`u{D83D}`u{DE00}"; expected = "TypeHandle" } ## use surrogate pairs in the string.
            @{ id = 'Should Detect (12)'; text = "xx`u{48}`u{48}xx()xxx--xx[]xx;'xpox?/xxemit"; expected = "Emit" } ## suspicious string starts at the index 29.

            @{ id = 'Should NOT Detect (1)'; text = "PowerShell Preview Extension v2022.11.2"; expected = $null }
            @{ id = 'Should NOT Detect (2)'; text = "add-typu"; expected = $null }
            @{ id = 'Should NOT Detect (3)'; text = "GetDelegateForFunctionPointfr"; expected = $null }
            @{ id = 'Should NOT Detect (4)'; text = "emjt TypeHandlfe"; expected = $null }
            @{ id = 'Should NOT Detect (5)'; text = "Get*&)(@~>-Types"; expected = $null }
        )
    }

    It "Smoke testing the suspicious content detection - <id>" -TestCases $testCases {
        param($text, $expected)

        $type::Match($text) | Should -BeExactly $expected
    }
}
