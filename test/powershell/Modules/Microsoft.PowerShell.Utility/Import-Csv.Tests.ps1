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

    Context "Pipeline binding tests" {
        $testCases = @(
            @{ name = "quote with empty value"  ; expectedHeader = "a1,H1,a3"; file = "EmptyValue.csv"      ; contentVar = 'emptyValueCsv'     ; delimiter = '"' }
            @{ name = "quote with value"        ; expectedHeader = "a1,a2,a3"; file = "WithValue.csv"       ; contentVar = 'withValueCsv'      ; delimiter = '"' }
            @{ name = "value enclosed in quote" ; expectedHeader = "a1,a2,a3"; file = "QuotedCharacter.csv" ; contentVar = 'quotedCharacterCsv'; delimiter = ',' }
        )

        It 'Should handle CSV parsing with different delimiters' {
            foreach ($testCase in $testCases) {
                $content = Get-Variable -Name $testCase.contentVar -ValueOnly
                $testPath = Join-Path $TestDrive $testCase.file
                Set-Content $testPath -Value $content

                # Test LiteralPath binding
                $returnObject = Get-ChildItem -Path $testPath | Import-Csv -Delimiter $testCase.delimiter
                $actualHeader = $returnObject[0].psobject.Properties.name -join ','
                $actualHeader | Should -BeExactly $testCase.expectedHeader

                # Test Path binding
                $returnObject = $testPath | Import-Csv -Delimiter $testCase.delimiter
                $actualHeader = $returnObject[0].psobject.Properties.name -join ','
                $actualHeader | Should -BeExactly $testCase.expectedHeader

                # Test object pipeline binding
                $returnObject = [pscustomobject]@{ LiteralPath = $testPath } | Import-Csv -Delimiter $testCase.delimiter
                $actualHeader = $returnObject[0].psobject.Properties.name -join ','
                $actualHeader | Should -BeExactly $testCase.expectedHeader
            }
        }
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
        $orginalHeader = "Column1","Column2","Column 3"
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
                    $HeaderParams = @{Header = $orginalHeader; Delimiter = ","}
                } else {
                    # The files have header
                    $HeaderParams = @{Delimiter = ","}
                }

            }

            It "Should be able to import all fields" {
                $actual = Import-Csv -Path $testCsv @HeaderParams
                $actualfields = $actual[0].psobject.Properties.Name
                $actualfields | Should -Be $orginalHeader
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

Describe "Import-Csv with empty and null values" {

    Context 'Empty CSV Fields' {
        $testCases = @(
            @{
                Test     = '1a'
                Expected = [pscustomobject] @{ P1 = '' }
                InputCsv = @'
"P1"
""
'@
            }
            @{
                Test     = '1b'
                Expected = [pscustomobject] @{ P1 = '' }, [pscustomobject] @{ P1 = '' }, [pscustomobject] @{ P1 = '' }
                InputCsv = @'
"P1"
""
""
""
'@
            }
            @{
                Test     = '2a'
                Expected = [pscustomobject] @{ P1 = ''; P2 = $null }
                InputCsv = @'
"P1","P2"
"",
'@
            }
            @{
                Test     = '2b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }, [pscustomobject] @{ P1 = ''; P2 = $null }
                InputCsv = @'
"P1","P2"
"",
"",
'@
            }
            @{
                Test     = '3a'
                Expected = [pscustomobject] @{ P1 = ''; P2 = $null }
                InputCsv = @'
"P1","P2"
,
'@
            }
            @{
                Test     = '3b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }, [pscustomobject] @{ P1 = ''; P2 = $null }
                InputCsv = @'
"P1","P2"
,
,
'@
            }
            @{
                Test     = '4a'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }
                InputCsv = @'
"P1","P2"
,""
'@
            }
            @{
                Test     = '4b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }, [pscustomobject] @{ P1 = ''; P2 = '' }
                InputCsv = @'
"P1","P2"
,""
,""
'@
            }
            @{
                Test     = '5a'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }
                InputCsv = @'
"P1","P2"
"",""
'@
            }
            @{
                Test     = '5b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }, [pscustomobject] @{ P1 = ''; P2 = '' }
                InputCsv = @'
"P1","P2"
"",""
"",""
'@
            }
            @{
                Test     = '6a'
                Expected = [pscustomobject] @{ P1 = ''; P2 = ''; P3 = $null }
                InputCsv = @'
"P1","P2","P3"
,,
'@
            }
            @{
                Test     = '6b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = ''; P3 = '' }, [pscustomobject] @{ P1 = ''; P2 = ''; P3 = $null }
                InputCsv = @'
"P1","P2","P3"
,,
,,
'@
            }
            @{
                Test     = '7a'
                Expected = [pscustomobject] @{ P1 = '' }, [pscustomobject] @{ P1 = '' }
                InputCsv = @'
"P1"
""
""

'@
            }
            @{
                Test     = '7b'
                Expected = [pscustomobject] @{ P1 = ''; P2 = '' }, [pscustomobject] @{ P1 = 'A1'; P2 = 'A2' }, [pscustomobject] @{ P1 = 'B1'; P2 = 'B2' }, [pscustomobject] @{ P1 = ''; P2 = $null }
                InputCsv = @'
"P1","P2"
,
A1,A2
B1,B2
,
'@
            }
        )

        It 'Import-Csv correctly deserializes input CSV' {
            foreach ($testCase in $testCases) {
                $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
                $testCase.InputCsv | Set-Content -Path $csvFile -NoNewLine
                $actualResult   = Import-Csv -Path $csvFile | ConvertTo-Csv
                $expectedResult = $testCase.Expected | ConvertTo-Csv
                $actualResult | Should -BeExactly $expectedResult
            }
        }
    }

    Context 'Header Only Scenarios' {
        It 'Should handle header only without newline' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            'P1,P2' | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle header with one empty newline' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2

'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle header followed by multiple empty lines' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2



'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Empty Input with -Header Parameter' {
        It 'Should handle empty input with -Header specified' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            '' | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle one empty line with -Header specified' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'

'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle multiple empty lines with -Header specified' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'



'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle mixed content with -Header parameter' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
,
A1,A2
B1,B2
,
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Header P1, P2

            $result.Count | Should -Be 4
            $result[0].P1 | Should -Be ''
            $result[0].P2 | Should -Be ''
            $result[1].P1 | Should -Be 'A1'
            $result[1].P2 | Should -Be 'A2'
            $result[2].P1 | Should -Be 'B1'
            $result[2].P2 | Should -Be 'B2'
            $result[3].P1 | Should -Be ''
            $result[3].P2 | Should -BeNullOrEmpty
        }
    }

    Context 'Edge Cases with Whitespace and Special Characters' {
        It 'Should handle whitespace-only fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2,P3
" ","  ","   "
,,
"","",""
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be ' '
            $result[0].P2 | Should -Be '  '
            $result[0].P3 | Should -Be '   '
            $result[1].P1 | Should -Be ''
            $result[1].P2 | Should -Be ''
            $result[1].P3 | Should -Be ''
            $result[2].P1 | Should -Be ''
            $result[2].P2 | Should -Be ''
            $result[2].P3 | Should -Be ''
        }

        It 'Should handle newlines within quoted fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2
"Line1
Line2","Value2"
"Value3","Line1
Line2"
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $expectedValue = "Line1$([Environment]::NewLine)Line2"
            $result.Count | Should -Be 2
            $result[0].P1 | Should -Be $expectedValue
            $result[0].P2 | Should -Be 'Value2'
            $result[1].P1 | Should -Be 'Value3'
            $result[1].P2 | Should -Be $expectedValue
        }

        It 'Should handle escaped quotes within fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2
"Value with ""quotes""","Normal value"
"Another ""quoted"" value",""
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 2
            $result[0].P1 | Should -Be 'Value with "quotes"'
            $result[0].P2 | Should -Be 'Normal value'
            $result[1].P1 | Should -Be 'Another "quoted" value'
            $result[1].P2 | Should -Be ''
        }
    }

    Context 'Type Information Handling' {
        It 'Should preserve type information in edge cases' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
#TYPE Custom.Type
P1,P2
,
"",""
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 2
            $result[0].PSObject.TypeNames[0] | Should -Be 'Custom.Type'
            $result[0].PSObject.TypeNames[1] | Should -Be 'CSV:Custom.Type'
            $result[1].PSObject.TypeNames[0] | Should -Be 'Custom.Type'
            $result[1].PSObject.TypeNames[1] | Should -Be 'CSV:Custom.Type'
        }

        It 'Should handle type information with empty rows' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
#TYPE Custom.EmptyType
P1,P2



'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Delimiter Edge Cases' {
        It 'Should handle tab delimiter with empty fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @"
P1	P2	P3
A1
B1
C1
"@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Delimiter "`t"

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -BeNullOrEmpty
            $result[0].P3 | Should -BeNullOrEmpty
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -BeNullOrEmpty
            $result[2].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle delimiter with empty fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2,P3
A1,,
B1,,
C1,,
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Delimiter ','

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -Be ''
            $result[0].P3 | Should -Be ''
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -Be ''
            $result[2].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle custom delimiter that appears in data' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1;P2;P3
A1
B1
C1
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile -Delimiter ';'

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -BeNullOrEmpty
            $result[0].P3 | Should -BeNullOrEmpty
            $result[1].P1 | Should -Be 'B1'
            $result[1].P2 | Should -BeNullOrEmpty
            $result[1].P3 | Should -BeNullOrEmpty
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -BeNullOrEmpty
            $result[2].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle no delimiter with empty fields' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            @'
P1,P2,P3
A1
B1
C1
'@ | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -BeNullOrEmpty
            $result[0].P3 | Should -BeNullOrEmpty
            $result[1].P1 | Should -Be 'B1'
            $result[1].P2 | Should -BeNullOrEmpty
            $result[1].P3 | Should -BeNullOrEmpty
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -BeNullOrEmpty
            $result[2].P3 | Should -BeNullOrEmpty
        }
    }

    Context 'Large Data and Performance Edge Cases' {
        It 'Should handle large number of empty rows efficiently' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            $csvContent = "P1,P2,P3`n"

            for ($i = 0; $i -lt 100; $i++) {
                $csvContent += ",,`n"
            }
            $csvContent | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 100
            $result[0].P1 | Should -Be ''
            $result[0].P2 | Should -Be ''
            $result[0].P3 | Should -BeNullOrEmpty
            $result[99].P1 | Should -Be ''
            $result[99].P2 | Should -Be ''
            $result[99].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle many columns with empty values' {
            $csvFile = Join-Path $TestDrive -ChildPath $((New-Guid).Guid)
            $headers = 1..50 | ForEach-Object { "P$_" }
            $headerLine = $headers -join ','
            $emptyLine = ',' * 49

            $csvContent = "$headerLine`n$emptyLine`n"
            $csvContent | Set-Content -Path $csvFile -NoNewline
            $result = Import-Csv -Path $csvFile

            $result.Count | Should -Be 1
            $result[0].P1 | Should -Be ''
            $result[0].P25 | Should -Be ''
            $result[0].P50 | Should -BeNullOrEmpty
        }
    }
}
