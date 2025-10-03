# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ConvertTo-Csv DRT Unit Tests" -Tags "CI" {
    $inputObject = [pscustomobject]@{ First = 1; Second = 2 }

    It "Test convertto-csv with psobject pipelined" {
        $returnObject = $inputObject | ConvertTo-Csv -IncludeTypeInformation
        $returnObject.Count | Should -Be 3
        $returnObject[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should -BeExactly "`"First`",`"Second`""
        $returnObject[2] | Should -BeExactly "`"1`",`"2`""
    }

    It "Test convertto-csv with NoTypeInformation and psobject pipelined" {
        $returnObject = $inputObject | ConvertTo-Csv -NoTypeInformation
        $returnObject.Count | Should -Be 2
        $returnObject[0] | Should -BeExactly "`"First`",`"Second`""
        $returnObject[1] | Should -BeExactly "`"1`",`"2`""
    }

    It "Test convertto-csv with a useculture flag" {
        $delimiter = [CultureInfo]::CurrentCulture.TextInfo.ListSeparator
        $returnObject = $inputObject | ConvertTo-Csv -UseCulture -IncludeTypeInformation
        $returnObject.Count | Should -Be 3
        $returnObject[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should -BeExactly "`"First`"$($delimiter)`"Second`""
        $returnObject[2] | Should -BeExactly "`"1`"$($delimiter)`"2`""
    }

    It "Test convertto-csv with Delimiter" {
        $returnObject = $inputObject | ConvertTo-Csv -Delimiter ";" -IncludeTypeInformation
        $returnObject.Count | Should -Be 3
        $returnObject[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should -BeExactly "`"First`";`"Second`""
        $returnObject[2] | Should -BeExactly "`"1`";`"2`""
    }
}

Describe "ConvertTo-Csv" -Tags "CI" {
    BeforeAll {
        $Name = "Hello"; $Data = "World";
        $testObject = [pscustomobject]@{ FirstColumn = $Name; SecondColumn = $Data }
        $testNullObject = [pscustomobject]@{ FirstColumn = $Name; SecondColumn = $null }
    }

    It "Should Be able to be called without error" {
        { $testObject | ConvertTo-Csv } | Should -Not -Throw
    }

    It "Should output an array of objects" {
        $result = $testObject | ConvertTo-Csv
        $result.GetType() | Should -Be ([object[]])
    }

    It "Should return the type of data in the first element of the output array" {
        $result = $testObject | ConvertTo-Csv -IncludeTypeInformation

        $result[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
    }

    It "Should return the column info in the second element of the output array" {
        $result = $testObject | ConvertTo-Csv -IncludeTypeInformation

        $result[1] | Should -Match "`"FirstColumn`""
        $result[1] | Should -Match "`"SecondColumn`""
    }

    It "Should return the data as a comma-separated list in the third element of the output array" {
        $result = $testObject | ConvertTo-Csv -IncludeTypeInformation
        $result[2] | Should -Match "`"Hello`""
        $result[2] | Should -Match "`"World`""
    }

    It "Includes type information when -IncludeTypeInformation is supplied" {
        $result = $testObject | ConvertTo-Csv -IncludeTypeInformation

        ($result -split ([Environment]::NewLine))[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
    }

    It "Does not include type information by default" {
        $result = $testObject | ConvertTo-Csv

        $result | Should -Not -Match ([regex]::Escape('System.Management.Automation.PSCustomObject'))
        $result | Should -Not -Match ([regex]::Escape('#TYPE'))
    }

    It "Does not include type information with -NoTypeInformation" {
        $result = $testObject | ConvertTo-Csv -NoTypeInformation

        $result | Should -Not -Match ([regex]::Escape('System.Management.Automation.PSCustomObject'))
        $result | Should -Not -Match ([regex]::Escape('#TYPE'))
    }

    It "Does not include headers with -NoHeader" {
        $result = $testObject | ConvertTo-Csv -NoHeader
        $result | Should -BeExactly '"Hello","World"'
    }

    It "Does not support -UseQuotes and -QuoteFields at the same time" {
        { $testObject | ConvertTo-Csv -UseQuotes Always -QuoteFields "TestFieldName" } |
            Should -Throw -ErrorId "CannotSpecifyQuoteFieldsAndUseQuotes,Microsoft.PowerShell.Commands.ConvertToCsvCommand"
    }

    It "Does not support -IncludeTypeInformation and -NoTypeInformation at the same time" {
        { $testObject | ConvertTo-Csv -IncludeTypeInformation -NoTypeInformation } |
            Should -Throw -ErrorId "CannotSpecifyIncludeTypeInformationAndNoTypeInformation,Microsoft.PowerShell.Commands.ConvertToCsvCommand"
    }

    Context "QuoteFields parameter" {
        It "QuoteFields" {
            # Use 'FiRstCoLumn' to test case insensitivity
            $result = $testObject | ConvertTo-Csv -QuoteFields FiRstCoLumn -Delimiter ','

            $result[0] | Should -BeExactly "`"FirstColumn`",SecondColumn"
            $result[1] | Should -BeExactly "`"Hello`",World"

            $result = $testObject | ConvertTo-Csv -QuoteFields FiRstCoLumn, SeCondCoLumn -Delimiter ','

            $result[0] | Should -BeExactly "`"FirstColumn`",`"SecondColumn`""
            $result[1] | Should -BeExactly "`"Hello`",`"World`""
        }
    }

    Context "UseQuotes parameter" {
        It "UseQuotes Always" {
            $result = $testObject | ConvertTo-Csv -UseQuotes Always -Delimiter ','

            $result[0] | Should -BeExactly "`"FirstColumn`",`"SecondColumn`""
            $result[1] | Should -BeExactly "`"Hello`",`"World`""

            $result = $testNullObject | ConvertTo-Csv -UseQuotes Always -Delimiter ','

            $result[0] | Should -BeExactly "`"FirstColumn`",`"SecondColumn`""
            $result[1] | Should -BeExactly "`"Hello`","
        }

        It "UseQuotes Always is default" {
            $result = $testObject | ConvertTo-Csv -UseQuotes Always -Delimiter ','
            $result2 = $testObject | ConvertTo-Csv -Delimiter ','

            $result | Should -BeExactly $result2
        }

        It "UseQuotes Never" {
            $result = $testObject | ConvertTo-Csv -UseQuotes Never -Delimiter ','

            $result[0] | Should -BeExactly "FirstColumn,SecondColumn"
            $result[1] | Should -BeExactly "Hello,World"

            $result = $testNullObject | ConvertTo-Csv -UseQuotes Never -Delimiter ','

            $result[0] | Should -BeExactly "FirstColumn,SecondColumn"
            $result[1] | Should -BeExactly "Hello,"
        }

        It "UseQuotes AsNeeded" {
            $result = $testObject | ConvertTo-Csv -UseQuotes AsNeeded -Delimiter 'r'

            $result[0] | Should -BeExactly "`"FirstColumn`"rSecondColumn"
            $result[1] | Should -BeExactly "Hellor`"World`""

            $result = $testNullObject | ConvertTo-Csv -UseQuotes AsNeeded -Delimiter 'r'

            $result[0] | Should -BeExactly "`"FirstColumn`"rSecondColumn"
            $result[1] | Should -BeExactly "Hellor"
        }

        It "UseQuotes AsNeeded Escapes Delimiters" {
            $testDelimitersObject = [pscustomobject]@{ "FirstColumn" = "Hello,"; "Second,Column" = "World" };

            $result = $testDelimitersObject | ConvertTo-Csv -UseQuotes AsNeeded -Delimiter ','

            $result[0] | Should -BeExactly "FirstColumn,`"Second,Column`""
            $result[1] | Should -BeExactly "`"Hello,`",World"

            $result = $testDelimitersObject | ConvertTo-Csv -UseQuotes AsNeeded -Delimiter "r"

            $result[0] | Should -BeExactly "`"FirstColumn`"rSecond,Column"
            $result[1] | Should -BeExactly "Hello,r`"World`""
        }

        It "UseQuotes AsNeeded Escapes Newlines" {
            $testCRLFObject = [pscustomobject]@{ "First`r`nColumn" = "Hello`r`nWorld" };
            $testLFObject = [pscustomobject]@{ "First`nColumn" = "Hello`nWorld" };

            $result = $testCRLFObject | ConvertTo-Csv -UseQuotes AsNeeded

            $result[0] | Should -BeExactly "`"First`r`nColumn`""
            $result[1] | Should -BeExactly "`"Hello`r`nWorld`""

            $result = $testLFObject | ConvertTo-Csv -UseQuotes AsNeeded

            $result[0] | Should -BeExactly "`"First`nColumn`""
            $result[1] | Should -BeExactly "`"Hello`nWorld`""
        }

        It "UseQuotes AsNeeded Escapes Quotes" {
            $testQuotesObject = [pscustomobject]@{ "First`"Column" = "`"Hello`" World" };

            $result = $testQuotesObject | ConvertTo-Csv -UseQuotes AsNeeded

            $result[0] | Should -BeExactly "`"First`"`"Column`""
            $result[1] | Should -BeExactly "`"`"`"Hello`"`" World`""
        }

    }

    Context 'Converting IDictionary Objects' {
        BeforeAll {
            $Letters = 'A', 'B', 'C', 'D', 'E', 'F'
            $Items = 0..5 | ForEach-Object {
                [ordered]@{ Number = $_; Letter = $Letters[$_] }
            }
            $CsvString = $Items | ConvertTo-Csv
            $TestHashTable = [ordered]@{ 'first' = 'value1'; 'second' = $null; 'third' = 'value3' }
        }

        It 'should treat dictionary entries as properties' {
            $CsvString[0] | Should -MatchExactly ($Items[0].Keys -join '","')

            for ($i = 0; $i -lt $Items.Count; $i++) {
                # Index in the CSV strings will be +1 due to header line
                $ValuesPattern = $Items[$i].Values -join '","'
                $CsvString[$i + 1] | Should -MatchExactly $ValuesPattern
            }
        }

        It 'should ignore regular object properties' {
            $PropertyPattern = $Items[0].PSObject.Properties.Name -join '|'
            $CsvString[0] | Should -Not -Match $PropertyPattern
        }

        It 'should account for extended properties added deliberately' {
            $Items | Add-Member -MemberType NoteProperty -Name 'Extra' -Value 'Surprise!'
            $NewCsvString = $Items | ConvertTo-Csv
            $NewCsvString[0] | Should -MatchExactly 'Extra'
            $NewCsvString | Select-Object -Skip 1 | Should -MatchExactly 'Surprise!'
        }

        It 'should convert hashtable with null values without error'{
            { $TestHashTable | ConvertTo-Csv } | Should -Not -Throw
        }

        It 'should properly convert hashtable with null and non-null values'{
            $CsvResult = $TestHashTable | ConvertTo-Csv

            $CsvResult[0] | Should -BeExactly "`"first`",`"second`",`"third`""
            $CsvResult[1] | Should -BeExactly "`"value1`",$null,`"value3`""
        }
    }
}
