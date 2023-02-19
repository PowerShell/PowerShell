# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Import-Csv DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $fileToGenerate = Join-Path $TestDrive -ChildPath "importCSVTest.csv"
        $psObject = [pscustomobject]@{ "First" = "1"; "Second" = "2" }
    }

    It "Test import-csv with a delimiter parameter" {
        $delimiter = ';'
        $psObject | Export-Csv -Path $fileToGenerate -Delimiter $delimiter
        $returnObject = Import-Csv -Path $fileToGenerate -Delimiter $delimiter
        $returnObject.First | Should -Be 1
        $returnObject.Second | Should -Be 2
    }

    It "Test import-csv with UseCulture parameter" {
        $psObject | Export-Csv -Path $fileToGenerate -UseCulture
        $returnObject = Import-Csv -Path $fileToGenerate -UseCulture
        $returnObject.First | Should -Be 1
        $returnObject.Second | Should -Be 2
    }
}

Describe "Import-Csv Double Quote Delimiter" -Tags "CI" {
    BeforeAll {
        $emptyValueCsv = @'
        a1""a3
        v1"v2"v3
'@

        $withValueCsv = @'
        a1"a2"a3
        v1"v2"v3
'@

        $quotedCharacterCsv = @'
        a1,a2,a3
        v1,"v2",v3
'@
    }


    It "Should handle <name> and bind to LiteralPath from pipeline" -TestCases @(
        @{ name = "quote with empty value"  ; expectedHeader = "a1,H1,a3"; file = "EmptyValue.csv"      ; content = $emptyValueCsv       ; delimiter = '"' }
        @{ name = "quote with value"        ; expectedHeader = "a1,a2,a3"; file = "WithValue.csv"       ; content = $withValueCsv       ; delimiter = '"' }
        @{ name = "value enclosed in quote" ; expectedHeader = "a1,a2,a3"; file = "QuotedCharacter.csv" ; content = $quotedCharacterCsv ; delimiter = ',' }
        ){
        param($expectedHeader, $file, $content, $delimiter)

        $testPath = Join-Path $TestDrive $file
        Set-Content $testPath -Value $content

        $returnObject = Get-ChildItem -Path $testPath | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader

        $returnObject = $testPath | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader

        $returnObject = [pscustomobject]@{ LiteralPath = $testPath } | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader
    }

    It "Should handle <name> and bind to Path from pipeline" -TestCases @(
        @{ name = "quote with empty value"  ; expectedHeader = "a1,H1,a3"; file = "EmptyValue.csv"      ; content = $emptyValueCsv       ; delimiter = '"' }
        @{ name = "quote with value"        ; expectedHeader = "a1,a2,a3"; file = "WithValue.csv"       ; content = $withValueCsv       ; delimiter = '"' }
        @{ name = "value enclosed in quote" ; expectedHeader = "a1,a2,a3"; file = "QuotedCharacter.csv" ; content = $quotedCharacterCsv ; delimiter = ',' }
        ){
        param($expectedHeader, $file, $content, $delimiter)

        $testPath = Join-Path $TestDrive $file
        Set-Content $testPath -Value $content

        $returnObject = Get-ChildItem -Path $testPath | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader

        $returnObject = $testPath | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader

        $returnObject = [pscustomobject]@{ Path = $testPath } | Import-Csv -Delimiter $delimiter
        $actualHeader = $returnObject[0].psobject.Properties.name -join ','
        $actualHeader | Should -BeExactly $expectedHeader
    }
}

Describe "Import-Csv File Format Tests" -Tags "CI" {
    BeforeAll {
        # The file is w/o header
        $TestImportCsv_NoHeader = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv_NoHeader.csv
        # The file is with header
        $TestImportCsv_WithHeader = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv_WithHeader.csv
        # The file is W3C Extended Log File Format
        $TestImportCsv_W3C_ELF = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestImportCsv_W3C_ELF.csv

        $testCSVfiles = $TestImportCsv_NoHeader, $TestImportCsv_WithHeader, $TestImportCsv_W3C_ELF
        $originalHeader = "Column1","Column2","Column 3"
        $customHeader = "test1","test2","test3"
    }
    # Test set is the same for all file formats
    foreach ($testCsv in $testCSVfiles) {
       $FileName = (Get-ChildItem $testCsv).Name
        Context "Next test file: $FileName" {
            BeforeAll {
                $CustomHeaderParams = @{Header = $customHeader; Delimiter = ","}
                if ($FileName -eq "TestImportCsv_NoHeader.csv") {
                    # The file does not have header
                    # (w/o Delimiter here we get throw (bug?))
                    $HeaderParams = @{Header = $originalHeader; Delimiter = ","}
                } else {
                    # The files have header
                    $HeaderParams = @{Delimiter = ","}
                }

            }

            It "Should be able to import all fields" {
                $actual = Import-Csv -Path $testCsv @HeaderParams
                $actualfields = $actual[0].psobject.Properties.Name
                $actualfields | Should -Be $originalHeader
            }

            It "Should be able to import all fields with custom header" {
                $actual = Import-Csv -Path $testCsv @CustomHeaderParams
                $actualfields = $actual[0].psobject.Properties.Name
                $actualfields | Should -Be $customHeader
            }

            It "Should be able to import correct values" {
                $actual = Import-Csv -Path $testCsv @HeaderParams
                $actual.count         | Should -Be 4
                $actual[0].'Column1'  | Should -BeExactly "data1"
                $actual[0].'Column2'  | Should -BeExactly "1"
                $actual[0].'Column 3' | Should -BeExactly "A"
            }

        }
    }
}

Describe "Import-Csv #Type Tests" -Tags "CI" {
    BeforeAll {
        $testfile = Join-Path $TestDrive -ChildPath "testfile.csv"
        Remove-Item -Path $testfile -Force -ErrorAction SilentlyContinue
        $processlist = (Get-Process)[0..1]
        $processlist | Export-Csv -Path $testfile -Force -IncludeTypeInformation
        $expectedProcessTypes = "System.Diagnostics.Process","CSV:System.Diagnostics.Process"
    }

    It "Test import-csv import Object" {
        $importObjectList = Import-Csv -Path $testfile
        $processlist.Count | Should -Be $importObjectList.Count

        $importTypes = $importObjectList[0].psobject.TypeNames
        $importTypes.Count | Should -Be $expectedProcessTypes.Count
        $importTypes[0] | Should -Be $expectedProcessTypes[0]
        $importTypes[1] | Should -Be $expectedProcessTypes[1]
    }
}

Describe "Import-Csv with different newlines" -Tags "CI" {
    It "Test import-csv with '<name>' newline" -TestCases @(
        @{ name = "CR"; newline = "`r" }
        @{ name = "LF"; newline = "`n" }
        @{ name = "CRLF"; newline = "`r`n" }
        ) {
        param($newline)
        $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
        $delimiter = ','
        "h1,h2,h3$($newline)11,12,13$($newline)21,22,23$($newline)" | Out-File -FilePath $csvFile
        $returnObject = Import-Csv -Path $csvFile -Delimiter $delimiter
        $returnObject.Count | Should -Be 2
        $returnObject[0].h1 | Should -Be 11
        $returnObject[0].h2 | Should -Be 12
        $returnObject[0].h3 | Should -Be 13
        $returnObject[1].h1 | Should -Be 21
        $returnObject[1].h2 | Should -Be 22
        $returnObject[1].h3 | Should -Be 23
    }
}
