Import-Module $PSScriptRoot/DynamicKeywordTestSupport.psm1

Describe "Updating PowerShell type data using a DSL" -Tags "CI" {
    BeforeAll {
        $savedModulePath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -PathString $env:PSMODULEPATH -ModulePath $TestDrive

        $testCases = @(
            @{ name = "ScriptMethod"; expression = '@(9, 3, 12).Sum()'; expected = 24 },
            @{ name = "CodeMethod"; expression = '@(13, 5, 18).First()'; expected = 13 },
            @{ name = "ScriptProperty"; expression = '@{x = 1; y = 2}.TwiceCount'; expected = 4 },
            @{ name = "AliasProperty"; expression = '@{x = 1; y = 2}.NumElements'; expected = 2 },
            @{ name = "NoteProperty"; expression = '@{x = 1; y = 2}.Greeting'; expected = "Hello" },
            @{ name = "CodeProperty"; expression = '@{x = 1; y = 2}.TheValueOfX'; expected = 1 }
        )

        $testInput = ($testCases | ForEach-Object { $_.name,$_.expression -join "!" }) -join "::"
        $typeModulePath = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath "assets") -ChildPath "AddTypes.ps1"
        $serializedArgs = $typeModulePath,$testInput -join "%"

        $command = {
            $testInput = $args -split "%"
            $typeModulePath = $testInput[0]
            $testCases = $testInput[1]

            . $typeModulePath

            $tests = $testCases -split "::"
            $result = @{}
            foreach ($test in $tests)
            {
                $testParts = $test -split "!"
                $name = $testParts[0]
                $expr = $testParts[1]
                $result += @{ $name = [scriptblock]::Create($expr).Invoke() }
            }
            $result
        }

        $result = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "TypesDsl" -Arguments $serializedArgs -ScriptBlock $command
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModulePath
    }

    It "Evaluates the given expression correctly using the <name> type data" -TestCases $testCases {
        param($name, $expression, $expected)

        $result.$name | Should Be $expected
    }
}