# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Export-Csv" -Tags "CI" {
    BeforeAll {
        $testObject = @("test","object","array")
        $testCsv = Join-Path -Path $TestDrive -ChildPath "output.csv"
        $newLine = [environment]::NewLine
        $P1 = [pscustomobject]@{"P1" = "first"}
        $P2 = [pscustomobject]@{"P2" = "second"}
        $P11 = [pscustomobject]@{"P1" = "eleventh"}
    }

    AfterEach {
        Remove-Item -Path $testCsv -Force -ErrorAction SilentlyContinue
    }

    It "Should be able to be called without error" {
        { $testObject | Export-Csv -Path $testCsv -ErrorAction Stop } | Should -Not -Throw
    }

    It "Should throw if an output file isn't specified" {
        { $testObject | Export-Csv -ErrorAction Stop } | Should -Throw -ErrorId "CannotSpecifyPathAndLiteralPath,Microsoft.PowerShell.Commands.ExportCsvCommand"
    }

    It "Should be a string when exporting via pipe" {
        $testObject | Export-Csv -Path  $testCsv -IncludeTypeInformation
        $results = Get-Content -Path $testCsv

        $results[0] | Should -BeExactly "#TYPE System.String"
    }

    It "Should be an object when exporting via the inputObject switch" {
        Export-Csv -InputObject $testObject -Path $testCsv -IncludeTypeInformation
        $results = Get-Content -Path $testCsv

        $results[0] | Should -BeExactly "#TYPE System.Object[]"
    }

    It "Should output a csv file containing a string of all the lengths of each element when piped input is used" {
        $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation

        $first    = "`"" + $testObject[0].Length.ToString() + "`""
        $second   = "`"" + $testObject[1].Length.ToString() + "`""
        $third    = "`"" + $testObject[2].Length.ToString() + "`""
        $expected = @("#TYPE System.String", "`"Length`"", $first , $second, $third)

        for ($i = 0; $i -lt $expected.Count; $i++) {
            $(Get-Content -Path $testCsv)[$i] | Should -Be $expected[$i]
        }
    }

    It "Does not include type information by default" {
        $testObject | Export-Csv -Path $testCsv
        $results = Get-Content -Path $testCsv

        $results[0] | Should -Not -Match ([regex]::Escape("System.String"))
        $results[0] | Should -Not -Match ([regex]::Escape("#TYPE"))
    }

    It "Does not include type information with -NoTypeInformation" {
        $testObject | Export-Csv -Path $testCsv -NoTypeInformation
        $results = Get-Content -Path $testCsv

        $results[0] | Should -Not -Match ([regex]::Escape("System.String"))
        $results[0] | Should -Not -Match ([regex]::Escape("#TYPE"))
    }

    It "Includes type information when -IncludeTypeInformation is supplied" {
        $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation
        $results = Get-Content -Path $testCsv

        $results[0] | Should -BeExactly "#TYPE System.String"
    }

    It "Does not support -IncludeTypeInformation and -NoTypeInformation at the same time" {
        { $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation -NoTypeInformation } | Should -Throw -ErrorId "CannotSpecifyIncludeTypeInformationAndNoTypeInformation,Microsoft.PowerShell.Commands.ExportCsvCommand"
    }

    It "Should support -LiteralPath parameter" {
        $testObject | Export-Csv -LiteralPath $testCsv
        $results = Import-Csv -Path  $testCsv

        $results | Should -HaveCount 3
    }

    It "Should overwrite file without -NoClobber parameter" {
        $P1 | Export-Csv -Path $testCsv
        $P2 | Export-Csv -Path $testCsv
        $results = Import-Csv -Path $testCsv

        $results.P2 | Should -BeExactly "second"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P2"
    }

    It "Should not overwrite file with -NoClobber parameter" {
        $P1 | Export-Csv -Path $testCsv
        { $P2 | Export-Csv -Path $testCsv -NoClobber} | Should -Throw -ErrorId "NoClobber,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "first"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P1"
    }

    It "Should not overwrite read-only file without -Force parameter" {
        $P1 | Export-Csv -Path $testCsv
        Set-ItemProperty -Path $testCsv -Name IsReadOnly -Value $true

        { $P2 | Export-Csv -Path $testCsv } | Should -Throw -ErrorId "FileOpenFailure,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "first"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P1"
    }

    It "Should overwrite read-only file with -Force parameter" {
        $P1 | Export-Csv -Path $testCsv
        Set-ItemProperty -Path $testCsv -Name IsReadOnly -Value $true

        $P2 | Export-Csv -Path $testCsv -Force
        $results = Import-Csv -Path $testCsv

        $results.P2 | Should -BeExactly "second"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P2"
    }

    It "Should not export to file if -WhatIf parameter specified" {
        $P1 | Export-Csv -Path $testCsv -WhatIf
        $testCsv | Should -Not -Exist
    }

    It "Should append to file if -Append parameter specified" {
        $P1 | Export-Csv -Path $testCsv
        $P11 | Export-Csv -Path $testCsv -Append
        $results = Import-Csv -Path $testCsv

        $results[0].P1 | Should -BeExactly "first"
        $results[1].P1 | Should -BeExactly "eleventh"
        $property = $results[0].PSObject.Properties.Name
        $property | Should -BeExactly "P1"
    }

    It "Should append to empty file if -Append parameter specified" {
        New-Item -Path $testCsv -ItemType File

        $P11 | Export-Csv -Path $testCsv -Append
        $results = Import-Csv -Path $testCsv

        $results[0].P1 | Should -BeExactly "eleventh"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P1"
    }

    It "Should throw when appended property does not exist in existing .csv file" {
        $P1 | Export-Csv -Path $testCsv
        { $P2 | Export-Csv -Path $testCsv -Append -ErrorAction Stop } | Should -Throw -ErrorId "CannotAppendCsvWithMismatchedPropertyNames,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results[0].P1 | Should -BeExactly "first"
        $property = $results.PSObject.Properties.Name
        $property | Should -BeExactly "P1"
    }

    It "Test basic function works well" {
        $in = [pscustomobject]@{ "P1" = "V11"; "P2" = "V12"; "P3" = "V13" }
        $in | Export-Csv -Path $testCsv -NoTypeInformation
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "V11"
        $results.P2 | Should -BeExactly "V12"
        $results.P3 | Should -BeExactly "V13"
    }

    It "Test if it works with special character" {
        $v3 = "abc" + $newLine + "foo"
        $in = [pscustomobject]@{ "P1" = "  "; "P2" = "abc,foo"; "P3" = $v3}
        $in | Export-Csv -Path $testCsv -NoTypeInformation
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "  "
        $results.P2 | Should -BeExactly "abc,foo"
        $results.P3 | Should -BeExactly $v3
    }

    It "Test export-csv with a useculture flag" {
        $outputFilesDir = Join-Path -Path $TestDrive -ChildPath "Monad"
        $fileToGenerate = Join-Path -Path $outputFilesDir -ChildPath "CSVTests.csv"
        $delimiter = (Get-Culture).TextInfo.ListSeparator
        New-Item -Path $outputFilesDir -ItemType Directory -Force
        Get-Item -Path $outputFilesDir | Export-Csv -Path $fileToGenerate -UseCulture -NoTypeInformation
        $contents = Get-Content -Path $fileToGenerate

        $contents.Count | Should -Be 2
        $contents[0].Contains($delimiter) | Should -BeTrue
        $contents[1].Contains($delimiter) | Should -BeTrue
    }
}
