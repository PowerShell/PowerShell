# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "BooleanParameterDCR Tests" -tags "CI" {
    BeforeAll {
        Function ParserTestSwitchCmdlet
        {
          [CmdletBinding()]
          param([switch]$switchParam)
          return $switchParam
        }

        Function ParserTestBoolCmdlet2
        {
          [CmdletBinding()]
          param([bool]$First=$false)
          return $First
        }
    }

    $tests = @(
            @{ inputTest = 0;    expected = $false; iteration = 1 },
            @{ inputTest = 000;  expected = $false; iteration = 2 },
            @{ inputTest = 0x00; expected = $false; iteration = 3 },
            @{ inputTest = 0.00; expected = $false; iteration = 4 }
    )
    It "Test <iteration> that passing zero works as the value for a Switch parameter, inputTest:<inputTest>,expect:<expected>" -TestCases $tests {
            param ( $inputTest, $expected )
            [bool]$switchTestParam = $inputTest
            $result = ParserTestSwitchCmdlet -switchParam:$switchTestParam
            $result | Should -Be $expected
    }

    $tests = @(
            @{ inputTest = $(1 -eq 1); expected = $true; iteration = 1},
            @{ inputTest = $true;      expected = $true; iteration = 2},
            @{ inputTest = $TRUE;      expected = $true; iteration = 3}
    )
    It "Test <iteration> that $true is accepted as a true value for Switch parameters, inputTest:<inputTest>,expect:<expected>" -TestCases $tests {
            param ( $inputTest, $expected )
            [bool]$switchTestParam = $inputTest
            $result = ParserTestSwitchCmdlet -switchParam:$switchTestParam
            $result | Should -Be $expected
    }

    It "Test that a nullable boolean is accepted for a boolean parameter." {
        [System.Nullable[System.Int32]] $nullBoolVar = $false
        $result = ParserTestBoolCmdlet2 $nullBoolVar
        $result | Should -BeFalse
        $result = ParserTestBoolCmdlet2 -First:$nullBoolVar
        $result | Should -BeFalse
        $result = ParserTestBoolCmdlet2 -First $nullBoolVar
        $result | Should -BeFalse
    }
}
