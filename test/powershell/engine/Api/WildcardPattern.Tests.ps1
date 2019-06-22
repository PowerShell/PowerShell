# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "WildcardPattern Escape - Experimental-Feature-Disabled" -Tags "CI" {

    BeforeAll {
        $testName = 'PSWildcardEscapeEscape'
        $skipTest = $EnabledExperimentalFeatures.Contains($testName)

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature '$testName' to be disabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It "Unescaping '<escapedStr>' which escaped from '<inputStr>' should get the original" -TestCases @(
        @{inputStr = '*This'; escapedStr = '`*This'}
        @{inputStr = 'Is?'; escapedStr = 'Is`?'}
        @{inputStr = 'Real[ly]'; escapedStr = 'Real`[ly`]'}
        @{inputStr = 'Ba`sic'; escapedStr = 'Ba`sic'}
        @{inputStr = 'Test `[more`]?'; escapedStr = 'Test ``[more``]`?'}
    ) {
        param($inputStr, $escapedStr)

        [WildcardPattern]::Escape($inputStr) | Should -BeExactly $escapedStr
        [WildcardPattern]::Unescape($escapedStr) | Should -BeExactly $inputStr
    }
}

Describe "WildcardPattern Escape - Experimental-Feature-Enabled" -Tags "CI" {

    BeforeAll {
        $testName = 'PSWildcardEscapeEscape'
        $skipTest = -not $EnabledExperimentalFeatures.Contains($testName)

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature '$testName' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It "Unescaping '<escapedStr>' which escaped from '<inputStr>' should get the original" -TestCases @(
        @{inputStr = '*This'; escapedStr = '`*This'}
        @{inputStr = 'Is?'; escapedStr = 'Is`?'}
        @{inputStr = 'Real[ly]'; escapedStr = 'Real`[ly`]'}
        @{inputStr = 'Ba`sic'; escapedStr = 'Ba``sic'}
        @{inputStr = 'Test `[more`]?'; escapedStr = 'Test ```[more```]`?'}
    ) {
        param($inputStr, $escapedStr)

        [WildcardPattern]::Escape($inputStr) | Should -BeExactly $escapedStr
        [WildcardPattern]::Unescape($escapedStr) | Should -BeExactly $inputStr
    }
}
