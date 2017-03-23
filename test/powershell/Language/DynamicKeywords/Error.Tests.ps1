Import-Module $PSScriptRoot/DynamicKeywordTestSupport.psm1

Describe "DynamicKeyword metadata reader error checking" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH += [System.IO.Path]::PathSeparator + $TestDrive

        $testCases = @(
            @{ dslName = "ErrorDoubleDefinitionDsl"; expectedErrorId = "DynamicKeywordMetadataKeywordAlreadyDefinedInScope" },
            @{ dslName = "ErrorGlobalBadUseModeDsl"; expectedErrorId = "DynamicKeywordMetadataGlobalKeywordsMustBeOptionalMany" },
            @{ dslName = "ErrorNestedKeywordInCommandDsl"; expectedErrorId = "DynamicKeywordMetadataCommandKeywordHasNestedKeywords" },
            @{ dslName = "ErrorNoInheritKeywordDsl"; expectedErrorId = "DynamicKeywordMetadataKeywordDoesNotInheritKeyword" },
            @{ dslName = "ErrorNonCommandHasParametersDsl"; expectedErrorId = "DynamicKeywordMetadataNonCommandKeywordHasParameters" },
            @{ dslName = "ErrorNoZeroArgCtorDsl"; expectedErrorId = "DynamicKeywordMetadataNoZeroArgCtor" },
            @{ dslName = "ErrorPropertyOnNonHashtableDsl"; expectedErrorId = "DynamicKeywordMetadataNonHashtableKeywordHasProperties" }
        )

        $command = {
            $serializedTests = $args[0]

            $results = @{}

            foreach ($test in $serializedTests -split ';')
            {
                $dslName = ($test -split ',')[0]

                try
                {
                    [scriptblock]::Create("using module $dslName").Invoke()
                }
                catch
                {
                    $results += @{ $dslName = $_.Exception.InnerException.Errors[0].ErrorId }
                }
            }
            $results
        }

        $testInput = Convert-TestCasesToSerialized -TestCases $testCases -Keys "dslName","expectedErrorId"

        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName ($testCases | ForEach-Object {$_.dslName}) -ScriptBlock $command -Arguments $testInput
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "throws <expectedErrorId> when reading the metadata for <dslName>" -TestCases $testCases {
        param($dslName, $expectedErrorId)

        $results.$dslName | Should Be $expectedErrorId
    }
}
