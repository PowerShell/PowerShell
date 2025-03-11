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
}
