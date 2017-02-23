Import-Module $PSScriptRoot/DynamicKeywordTestSupport.psm1

Describe "Dynamic Keyword parse time delegate execution" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $testCases = @(
            @{ keyword = "SimplePreParse"; invocation = "SimplePreParseKeyword"; expectedError = "SuccessfulPreParse" },
            @{ keyword = "SimplePostParse"; invocation = "SimplePostParseKeyword"; expectedError = "SuccessfulPostParse" },
            @{ keyword = "SimpleSemanticCheck"; invocation = "SimpleSemanticCheckKeyword"; expectedError = "SuccessfulSemanticCheck" },
            @{ keyword = "AstManipulationPostParse"; invocation = 'AstManipulationPostParseKeyword { "Foo" }'; expectedError = "Foo" },
            @{ keyword = "AstManipulationSemanticCheck"; invocation = 'AstManipulationSemanticCheckKeyword { "Bar" }'; expectedError = "Bar" }
        )

        $command = {
            $tests = $args[0] -split ';'
            $results = @{}
            foreach ($test in $tests)
            {
                $testArgs = $test -split ','
                $keyword = $testArgs[0]
                $invocation = $testArgs[1]

                try
                {
                    [scriptblock]::Create("using module ParserDelegateDsl`n$invocation")
                }
                catch
                {
                    $results += @{ $keyword = $_.Exception.InnerException.Errors[0].ErrorId }
                }
            }
            $results
        }

        $testInput = Convert-TestCasesToSerialized -TestCases $TestCases -Keys "keyword","invocation","expectedError"

        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "ParserDelegateDsl" -ScriptBlock $command -Arguments $testInput
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "throws the error with ID <expectedError> when <keyword>Keyword is invoked" -TestCases $testCases {
        param($keyword, $invocation, $expectedError)

        $results.$keyword | Should Be $expectedError
    }

    It "adds the 'Greeting' parameter to the keyword when AstManipulationPreParseKeyword is invoked" {
        $preParseCommand = {
            [scriptblock]::Create("using module ParserDelegateDsl`nAstManipulationPreParseKeyword")

            $kw = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword("AstManipulationPreParseKeyword")
            $kw.Parameters
        }

        $result = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "ParserDelegateDsl" -ScriptBlock $preParseCommand

        $result.Greeting.Name | Should Be "Greeting"
    }
}

Describe "Extends runtime types using a dsl" -Tags "CI" {
    BeforeAll {
        $savedModulePath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $testCases = @(
            @{ invocation = "@(8,5,2).Sum()"; expected = 15 }
        )

        $command = {
            $results = @{}
        }
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModulePath
    }
}