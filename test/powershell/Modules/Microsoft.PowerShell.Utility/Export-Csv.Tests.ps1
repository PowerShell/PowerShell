# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Export-Csv" -Tags "CI" {
    BeforeAll {
        $testObject = @("test","object","array")
        $testCsv = Join-Path -Path $TestDrive -ChildPath "output.csv"
        $newLine = [environment]::NewLine
        $P1 = [pscustomobject]@{"P1" = "first"}
        $P2 = [pscustomobject]@{"P2" = "second"}
        $P11 = [pscustomobject]@{"P1" = "eleventh"}
        $testHashTable = @{ 'first' = "value1"; 'second' = $null; 'third' = "value3" }
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

    It "Does not include headers with -NoHeader when exported and can imported with headers" {
        $P1 | Export-Csv -Path $testCsv -NoHeader
        $results = Get-Content -Path $testCsv
        $results | Should -BeExactly '"first"'
        $results = Import-Csv -Path $testCsv -Header "P1"
        $results[0].P1 | Should -BeExactly "first"
    }

    It "Does not include headers when imported with headers and exported using -NoHeader" {
        $P1 | Export-Csv -Path $testCsv
        (Import-Csv -Path $testCsv) | Export-Csv -Path $testCsv -NoHeader
        $results = Get-Content -Path $testCsv
        $results | Should -BeExactly '"first"'
    }

    It "Includes type information when -IncludeTypeInformation is supplied" {
        $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation
        $results = Get-Content -Path $testCsv

        $results[0] | Should -BeExactly "#TYPE System.String"
    }

    It "Does not support -IncludeTypeInformation and -NoTypeInformation at the same time" {
        { $testObject | Export-Csv -Path $testCsv -IncludeTypeInformation -NoTypeInformation } |
            Should -Throw -ErrorId "CannotSpecifyIncludeTypeInformationAndNoTypeInformation,Microsoft.PowerShell.Commands.ExportCsvCommand"
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
    }

    It "Should not overwrite file with -NoClobber parameter" {
        $P1 | Export-Csv -Path $testCsv
        { $P2 | Export-Csv -Path $testCsv -NoClobber} | Should -Throw -ErrorId "NoClobber,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "first"
    }

    It "Should not overwrite read-only file without -Force parameter" -Skip:(Test-IsRoot) {
        $P1 | Export-Csv -Path $testCsv
        Set-ItemProperty -Path $testCsv -Name IsReadOnly -Value $true

        { $P2 | Export-Csv -Path $testCsv } | Should -Throw -ErrorId "FileOpenFailure,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results.P1 | Should -BeExactly "first"
    }

    It "Should overwrite read-only file with -Force parameter" {
        $P1 | Export-Csv -Path $testCsv
        Set-ItemProperty -Path $testCsv -Name IsReadOnly -Value $true

        $P2 | Export-Csv -Path $testCsv -Force
        $results = Import-Csv -Path $testCsv

        $results.P2 | Should -BeExactly "second"
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
    }

    # This test is not a duplicate of the previous one, since it covers a separate branch in code.
    It "Should append to empty file if -Append parameter specified" {
        New-Item -Path $testCsv -ItemType File | Out-Null

        $P11 | Export-Csv -Path $testCsv -Append
        $results = Import-Csv -Path $testCsv

        $results[0].P1 | Should -BeExactly "eleventh"
    }

    It "Should throw when appended property does not exist in existing .csv file" {
        $P1 | Export-Csv -Path $testCsv
        { $P2 | Export-Csv -Path $testCsv -Append -ErrorAction Stop } | Should -Throw -ErrorId "CannotAppendCsvWithMismatchedPropertyNames,Microsoft.PowerShell.Commands.ExportCsvCommand"
        $results = Import-Csv -Path $testCsv

        $results[0].P1 | Should -BeExactly "first"
    }

    It "Should append existing properties, add missing properties with empty value, and skip extra properties" {
        $object1 = [PSCustomObject]@{first = 1; second = 2}
        $object2 = [PSCustomObject]@{first = 11; third = 13}

        $object1 | Export-Csv -Path $testCsv
        $object2 | Export-Csv -Path $testCsv -Append -Force

        $results = Import-Csv -Path $testCsv

        $results[0].first | Should -BeExactly "1"
        $results[0].second | Should -BeExactly "2"
        $results[1].first | Should -BeExactly "11"
        $results[1].second | Should -BeNullOrEmpty
        $results[1].PSObject.properties.Name | Should -Not -Contain 'third'
    }

    It "Should throw when -Append and -NoHeader are specified together" {
        { $P1 | Export-Csv -Path $testCsv -Append -NoHeader -ErrorAction Stop } | Should -Throw -ErrorId "CannotSpecifyBothAppendAndNoHeader,Microsoft.PowerShell.Commands.ExportCsvCommand"
    }

    It "First line should be #TYPE if -IncludeTypeInformation used and pstypenames object property is empty" {
        $object = [PSCustomObject]@{first = 1}
        $pstypenames = $object.pstypenames | ForEach-Object -Process {$_}
        $pstypenames | ForEach-Object -Process {$object.pstypenames.Remove($_)}
        $object | Export-Csv -Path $testCsv -IncludeTypeInformation
        $content = Get-Content -Path $testCsv

        $content[0] | Should -BeExactly '#TYPE'
    }

    # If type starts with CSV: Export-CSV should remove it. This would happen when you export
    # an imported object. Import-Csv adds CSV: prefix to the type.
    It "Should remove 'CSV:' from the type name" {
        $object = [PSCustomObject]@{first = 1}
        $object.pstypenames.Insert(0, "CSV:TheType")
        $object | Export-Csv -Path $testCsv -IncludeTypeInformation
        $result = Get-Content -Path $testCsv

        $result[0] | Should -BeExactly "#TYPE TheType"
    }

    It "Should escape double quote with another double quote" {
        $object = [PSCustomObject]@{first = 'Double quote " in the middle.'}
        $object | Export-Csv -Path $testCsv
        $result = Get-Content -Path $testCsv

        $result[1] | Should -BeExactly '"Double quote "" in the middle."'
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

    It "Should not throw when exporting hashtable with property that has null value"{
        { $testHashTable | Export-Csv -Path $testCsv } | Should -Not -Throw
    }

    It "Should not throw when exporting PSCustomObject with property that has null value"{
        $testObject = [pscustomobject]$testHashTable
        { $testObject | Export-Csv -Path $testCsv } | Should -Not -Throw
    }

    It "Export hashtable with null and non-null values"{
        $testHashTable | Export-Csv -Path $testCsv
        $result2 = Import-CSV -Path $testCsv

        $result2.first | Should -BeExactly "value1"
        $result2.second | Should -BeNullOrEmpty
        $result2.third | Should -BeExactly "value3"
    }

    It "Export hashtable with non-null values"{
        $testTable = @{ 'first' = "value1"; 'second' = "value2" }
        $testTable | Export-Csv -Path $testCsv
        $results = Import-CSV -Path $testCsv

        $results.first | Should -BeExactly "value1"
        $results.second | Should -BeExactly "value2"
    }

    Context "UseQuotes parameter" {

        # A minimum of tests. The rest are in ConvertTo-Csv.Tests.ps1

        BeforeAll {
            $Name = "Hello"; $Data = "World";
            $testOutputObject = [pscustomobject]@{ FirstColumn = $Name; SecondColumn = $Data }
            $testFile = Join-Path -Path $TestDrive -ChildPath "output.csv"
            $testFile2 = Join-Path -Path $TestDrive -ChildPath "output2.csv"
        }

        It "UseQuotes Always" {
            $testOutputObject | Export-Csv -Path $testFile -UseQuotes Always -Delimiter ','
            $result = Get-Content -Path $testFile

            $result[0] | Should -BeExactly "`"FirstColumn`",`"SecondColumn`""
            $result[1] | Should -BeExactly "`"Hello`",`"World`""
        }

        It "UseQuotes Always is default" {
            $testOutputObject | Export-Csv -Path $testFile -UseQuotes Always -Delimiter ','
            $result = Get-Content -Raw -Path $testFile
            $testOutputObject | Export-Csv -Path $testFile2 -Delimiter ','
            $result2 = Get-Content -Raw -Path $testFile2

            $result | Should -BeExactly $result2
        }
    }
}
