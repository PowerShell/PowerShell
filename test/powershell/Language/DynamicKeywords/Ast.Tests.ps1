Import-Module $PSScriptRoot\DynamicKeywordTestSupport.psm1

Describe "Creates the correct AST for keywords" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $testCases = @(
            @{ keyword = "CommandBodyKeyword"; body = "NONE" },
            @{ keyword = "HashtableBodyKeyword"; body = [hashtable] },
            @{ keyword = "ScriptBlockBodyKeyword"; body = [scriptblock] }
        )

        $command = {
            $keywords = $args[0] -split ","

            $bindingFlags = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
            $bodyProperty = [System.Management.Automation.Language.DynamicKeywordStatementAst].GetProperty("BodyExpression", $bindingFlags)

            $results = @{}

            [scriptblock]::Create("using module BodyModeDsl").Invoke()

            foreach ($keyword in $keywords)
            {
                $ast = [scriptblock]::Create("$keyword { }").Ast
                $ast = $ast.Find({
                    $args[0] -is [System.Management.Automation.Language.DynamicKeywordStatementAst]
                }, $true)
                $results += @{ $keyword = $bodyProperty.GetValue($ast) }
            }

            $results
        }

        $keywords = ($testCases | ForEach-Object { $_.keyword }) -join ","
        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "BodyModeDsl" -ScriptBlock $command -Arguments $keywords
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "<keyword> has body type of <body>" -TestCases $testCases {
        param($keyword, $body)

        if ($body -eq "NONE")
        {
            $expected = $null
        }
        else
        {
            $expected = $body.ToString()
        }

        $results.$keyword.StaticType | Should Be $expected
    }
}
