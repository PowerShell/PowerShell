Describe "Export-Csv" -Tags "CI" {
    BeforeAll {
        $testObject = @("test","object","array")
        $testCsv = Join-Path -Path $TestDrive -ChildPath "output.csv"
    }

    AfterEach {
        Remove-Item $testCsv -Force -ErrorAction SilentlyContinue
    }

    It "Should be able to be called without error" {
        { $testObject | Export-Csv $testCsv -ErrorAction Stop } | Should Not Throw
    }

    It "Should throw if an output file isn't specified" {
        { $testObject | Export-Csv -ErrorAction Stop } | ShouldBeErrorId "CannotSpecifyPathAndLiteralPath,Microsoft.PowerShell.Commands.ExportCsvCommand"
    }

    It "Should be a string when exporting via pipe" {
        $testObject | Export-Csv $testCsv -IncludeTypeInformation

        $piped = Get-Content $testCsv

        $piped[0] | Should BeExactly "#TYPE System.String"
    }

    It "Should be an object when exporting via the inputObject switch" {
        Export-Csv -InputObject $testObject -Path $testCsv -IncludeTypeInformation

        $switch = Get-Content $testCsv 

        $switch[0] | Should BeExactly "#TYPE System.Object[]" 
    }

    It "Should output a csv file containing a string of all the lengths of each element when piped input is used" {
        $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation

        $first    = "`"" + $testObject[0].Length.ToString() + "`""
        $second   = "`"" + $testObject[1].Length.ToString() + "`""
        $third    = "`"" + $testObject[2].Length.ToString() + "`""
        $expected = @("#TYPE System.String", "`"Length`"", $first , $second, $third)

        for ($i = 0; $i -lt $expected.Count; $i++) {
            $(Get-Content $testCsv)[$i] | Should Be $expected[$i]
        }
    }

    It "Does not include type information by default" {
        $testObject | Export-Csv -Path $testCsv

        $(Get-Content $testCsv)[0] | Should Not Match ([regex]::Escape("System.String"))
        $(Get-Content $testCsv)[0] | Should Not Match ([regex]::Escape("#TYPE"))
    }

    It "Does not include type information with -NoTypeInformation" {
        $testObject | Export-Csv -Path $testCsv -NoTypeInformation

        $(Get-Content $testCsv)[0] | Should Not Match ([regex]::Escape("System.String"))
        $(Get-Content $testCsv)[0] | Should Not Match ([regex]::Escape("#TYPE"))
    }

    It "Includes type information when -IncludeTypeInformation is supplied" {
        $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation

        $(Get-Content $testCsv)[0] | Should BeExactly "#TYPE System.String"
    }

    It "Does not support -IncludeTypeInformation and -NoTypeInformation at the same time" {
        { $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation -NoTypeInformation } | 
            ShouldBeErrorId "CannotSpecifyIncludeTypeInformationAndNoTypeInformation,Microsoft.PowerShell.Commands.ExportCsvCommand"
    }
}

Describe "Export-Csv DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $filePath = Join-Path $TestDrive -ChildPath "test.csv"
        $newLine = [environment]::NewLine
    }

    It "Test basic function works well" {
        $input = [pscustomobject]@{ "P1" = "V11"; "P2" = "V12"; "P3" = "V13" }
        $input | Export-Csv -Path $filePath -NoTypeInformation
        $results = Import-Csv $filePath
        $results.P1 | Should Be "V11"
        $results.P2 | Should Be "V12"
        $results.P3 | Should Be "V13"
    }

    It "Test if it works with special character" {
        $v3 = "abc" + $newLine + "foo"
        $input = [pscustomobject]@{ "P1" = "  "; "P2" = "abc,foo"; "P3" = $v3}
        $input | Export-Csv -Path $filePath -NoTypeInformation
        $results = Import-Csv $filePath
        $results.P1 | Should Be "  "
        $results.P2 | Should Be "abc,foo"
        $results.P3 | Should Be $v3
    }

    It "Test force switch works well" {
        $input = [pscustomobject]@{ "P1" = "first" }
        $input | Export-Csv -Path $filePath

        $input =  [pscustomobject]@{ "P2" = "second" }
        $input | Export-Csv -Path $filePath -Force
        $results = Import-Csv $filePath

        $results.P2 | Should be "second"
        $property = $results | Get-Member | Where-Object { $_.MemberType -eq "NoteProperty" } | ForEach-Object { $_.Name }
        $property | should not be P1
    }

    It "Test export-csv with a useculture flag" {
        $outputFilesDir = Join-Path $TestDrive -ChildPath "Monad"
        $fileToGenerate = Join-Path $outputFilesDir -ChildPath "CSVTests.csv"
        $delimiter = (Get-Culture).TextInfo.ListSeparator
        New-Item -Path $outputFilesDir -ItemType Directory -Force
        Get-Item -Path $outputFilesDir| Export-Csv -Path $fileToGenerate -UseCulture -NoTypeInformation
        $contents = Get-Content -Path $fileToGenerate
        $contents.Count | Should Be 2
        $contents[0].Contains($delimiter) | Should Be $true
        $contents[1].Contains($delimiter) | Should Be $true
    }
}
