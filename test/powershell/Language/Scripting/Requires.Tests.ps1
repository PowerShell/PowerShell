Describe "Requires tests" -Tags "CI" {
    Context "Parser error" {

        $testcases = @(
                        @{command = "#requiresappID`r`n$foo = 1; $foo" ; testname = "appId with newline"}
                        @{command = "#requires -version A `r`n$foo = 1; $foo" ; testname = "version as character"}
                        @{command = "#requires -version 2b `r`n$foo = 1; $foo" ; testname = "alphanumeric version"}
                        @{command = "#requires -version 1. `r`n$foo = 1; $foo" ; testname = "version with dot"}
                        @{command = "#requires -version '' `r`n$foo = 1; $foo" ; testname = "empty version"}
                        @{command = "#requires -version 1.0. `r`n$foo = 1; $foo" ; testname = "version with two dots"}
                        @{command = "#requires -version 1.A `r`n$foo = 1; $foo" ; testname = "alphanumeric version with dots"}
                    )

        It "throws ParserException - <testname>" -TestCases $testcases {
            param($command)
            try
            {
                [scriptblock]::Create($command)
                throw "'$command' should have thrown ParserError"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "ParseException"
            }
        }
    }

}