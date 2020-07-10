# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Nullable Boolean DCR Tests" -Tags "CI" {
    BeforeAll {
        function ParserTestFunction
        {
            param([bool]$First) $PSBoundParameters
        }

        function parsertest-bool2
        {
            [CmdletBinding()]
            param (
            [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)] [bool]$First = $false
            )

            Process {
                return $PSBoundParameters
            }
        }

        $testCases = @(
            @{ Arg = $true; Expected = $true }
            @{ Arg = 1; Expected = $true }
            @{ Arg = 1.28; Expected = $true }
            @{ Arg = (-2.32); Expected = $true }

            @{ Arg = $false; Expected = $false }
            @{ Arg = 0; Expected = $false }
            @{ Arg = 0.00; Expected = $false }
            @{ Arg = (1 - 1); Expected = $false }
        )
    }

    It 'Test a boolean parameter accepts positional values, input:<Arg>, expect:<Expected>' -TestCases $testCases {
        param($Arg, $Expected)
        (parsertestfunction $Arg).Values[0] | Should -Be $Expected
        (parsertest-bool2 $Arg).Values[0] | Should -Be $Expected
    }

    It 'Test a boolean parameter accepts values specified with a colon, input:<Arg>, expect:<Expected>' -TestCases $testCases {
        param($Arg, $Expected)
        (parsertestfunction -First:$Arg).Values[0] | Should -Be $Expected
        (parsertest-bool2 -First:$Arg).Values[0] | Should -Be $Expected
    }

    It 'Test a boolean parameter accepts values specified with general format, input:<Arg>, expect:<Expected>' -TestCases $testCases {
        param($Arg, $Expected)
        (parsertestfunction -First $Arg).Values[0] | Should -Be $Expected
        (parsertest-bool2 -First $Arg).Values[0] | Should -Be $Expected
    }
}
