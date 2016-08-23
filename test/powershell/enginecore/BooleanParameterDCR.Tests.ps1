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
            @{ inputTest = 0; expected = $false },
            @{ inputTest = 000; expected = $false },
            @{ inputTest = 0x00; expected = $false },
            @{ inputTest = 0.00; expected = $false }
    )
    It "Test that passing zero works as the value for a Switch parameter, inputTest:<inputTest>,expect:<expected>" -TestCases $tests {
            param ( $inputTest, $expected )
            [bool]$switchTestParam = $inputTest
            $result = ParserTestSwitchCmdlet -switchParam:$switchTestParam
            $result | should be $expected
    }
    
    $tests = @(
            @{ inputTest = $(1 -eq 1); expected = $true },
            @{ inputTest = $true; expected = $true },
            @{ inputTest = $TRUE; expected = $true }
    )
    It "Test that $true is accepted as a true value for Switch parameters, inputTest:<inputTest>,expect:<expected>" -TestCases $tests {
            param ( $inputTest, $expected )
            [bool]$switchTestParam = $inputTest
            $result = ParserTestSwitchCmdlet -switchParam:$switchTestParam
            $result | should be $expected
    }
    
    It "Test that a nullable boolean is accepted for a boolean parameter." {
        [System.Nullable[System.Int32]] $nullBoolVar = $false
        $result = ParserTestBoolCmdlet2 $nullBoolVar
        $result | should be $false
        $result = ParserTestBoolCmdlet2 -First:$nullBoolVar
        $result | should be $false
        $result = ParserTestBoolCmdlet2 -First $nullBoolVar
        $result | should be $false
    }
}
