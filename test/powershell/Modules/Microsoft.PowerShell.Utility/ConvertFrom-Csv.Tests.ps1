# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ConvertFrom-Csv" -Tags "CI" {

    BeforeAll {
        $testObject = "a", "1"
        $testcsv = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath TestCsv2.csv
        $testName = "Zaphod BeebleBrox"
        $testColumns = @"
a,b,c
1,2,3
"@
        $testTypeData = @"
#TYPE My.Custom.Object
a,b,c
1,2,3
"@
    }

    It "Should be able to be called" {
        { ConvertFrom-Csv -InputObject $testObject } | Should -Not -Throw
    }

    It "Should be able to pipe" {
        { $testObject | ConvertFrom-Csv } | Should -Not -Throw
    }

    It "Should have expected results when using piped inputs" {
        $csvContent   = Get-Content $testcsv
        $actualresult = $csvContent | ConvertFrom-Csv

        ,$actualresult | Should -BeOfType System.Array
        $actualresult[0] | Should -BeOfType PSCustomObject

        #Should have a name property in the result
        $actualresult[0].Name | Should -Be $testName
    }

    It "Should be able to set a delimiter" {
        { $testcsv | ConvertFrom-Csv -Delimiter ";" } | Should -Not -Throw
    }

    It "Should actually delimit the output" {
        $csvContent   = Get-Content $testcsv
        $actualresult = $csvContent | ConvertFrom-Csv -Delimiter ";"

        ,$actualresult | Should -BeOfType System.Array
        $actualresult[0] | Should -BeOfType PSCustomObject

        # ConvertFrom-Csv takes the first line of the input as a header by default
        $actualresult.Length | Should -Be $($csvContent.Length - 1)
    }

    It "Should be able to have multiple columns" {
        $actualData   = $testColumns | ConvertFrom-Csv

        $actualLength = $($( $actualData | Get-Member) | Where-Object {$_.MemberType -eq "NoteProperty" }).Length

        $actualLength | Should -Be 3
    }

    It "Should Contain the Imported Type data" {
        $actualData = $testTypeData | ConvertFrom-Csv
        $actualData.PSObject.TypeNames.Count | Should -Be 2
        $actualData.PSObject.TypeNames[0] | Should -BeExactly "My.Custom.Object"
        $actualData.PSObject.TypeNames[1] | Should -BeExactly "CSV:My.Custom.Object"
    }
}

Describe "ConvertFrom-Csv DRT Unit Tests" -Tags "CI" {
    It "Test ConvertFrom-Csv with pipelined InputObject and Header" {
        $inputObject = [pscustomobject]@{ First = 1; Second = 2 }
        $res = $inputObject | ConvertTo-Csv
        $result = $res | ConvertFrom-Csv -Header "Header1","Header2"

        $result[0].Header1 | Should -BeExactly "First"
        $result[0].Header2 | Should -BeExactly "Second"
        $result[1].Header1 | Should -BeExactly "1"
        $result[1].Header2 | Should -BeExactly "2"
    }
}

Describe "ConvertFrom-Csv with empty and null values" {

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

        It 'ConvertFrom-Csv correctly deserializes input CSV' {
            foreach ($testCase in $testCases) {
                $expectedResult = $testCase.Expected | ConvertTo-Csv
                $actualResult   = $testCase.InputCsv | ConvertFrom-Csv | ConvertTo-Csv

                $actualResult | Should -BeExactly $expectedResult
            }
        }
    }

    Context 'Header Only Scenarios' {
        It 'Should handle header only without newline' {
            $result = 'P1,P2' | ConvertFrom-Csv
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle header with one empty newline' {
            $result = @'
P1,P2

'@ | ConvertFrom-Csv
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle header followed by multiple empty lines' {
            $result = @'
P1,P2



'@ | ConvertFrom-Csv
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Empty Input with -Header Parameter' {
        It 'Should handle empty input with -Header specified' {
            $result = '' | ConvertFrom-Csv -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle one empty line with -Header specified' {
            $result = @'

'@ | ConvertFrom-Csv -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle multiple empty lines with -Header specified' {
            $result = @'



'@ | ConvertFrom-Csv -Header P1, P2
            $result | Should -BeNullOrEmpty
        }

        It 'Should handle mixed content with -Header parameter' {
            $result = @'
,
A1,A2
B1,B2
,
'@ | ConvertFrom-Csv -Header P1, P2

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

        It 'Should handle whitespace-only fields' {
            $result = @'
P1,P2,P3
" ","  ","   "
,,
"","",""
'@ | ConvertFrom-Csv

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
            $result = @'
P1,P2
"Line1
Line2","Value2"
"Value3","Line1
Line2"
'@ | ConvertFrom-Csv

            $expectedValue = "Line1$([Environment]::NewLine)Line2"
            $result.Count | Should -Be 2
            $result[0].P1 | Should -Be $expectedValue
            $result[0].P2 | Should -Be 'Value2'
            $result[1].P1 | Should -Be 'Value3'
            $result[1].P2 | Should -Be $expectedValue
        }

        It 'Should handle escaped quotes within fields' {
            $result = @'
P1,P2
"Value with ""quotes""","Normal value"
"Another ""quoted"" value",""
'@ | ConvertFrom-Csv

            $result.Count | Should -Be 2
            $result[0].P1 | Should -Be 'Value with "quotes"'
            $result[0].P2 | Should -Be 'Normal value'
            $result[1].P1 | Should -Be 'Another "quoted" value'
            $result[1].P2 | Should -Be ''
        }

        It 'Should handle tab delimiter with empty fields' {
            $result = @"
P1	P2	P3
A1
B1
C1
"@ | ConvertFrom-Csv -Delimiter "`t"

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -BeNullOrEmpty
            $result[0].P3 | Should -BeNullOrEmpty
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -BeNullOrEmpty
            $result[2].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle comma delimiter with empty fields' {
            $result = @'
P1,P2,P3
A1,,
B1,,
C1,,
'@ | ConvertFrom-Csv -Delimiter ','

            $result.Count | Should -Be 3
            $result[0].P1 | Should -Be 'A1'
            $result[0].P2 | Should -Be ''
            $result[0].P3 | Should -Be ''
            $result[2].P1 | Should -Be 'C1'
            $result[2].P2 | Should -Be ''
            $result[2].P3 | Should -BeNullOrEmpty
        }

        It 'Should handle custom delimiter that appears in data' {
            $result = @'
P1;P2;P3
A1
B1
C1
'@ | ConvertFrom-Csv -Delimiter ';'

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
            $result = @'
P1,P2,P3
A1
B1
C1
'@ | ConvertFrom-Csv

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
}
