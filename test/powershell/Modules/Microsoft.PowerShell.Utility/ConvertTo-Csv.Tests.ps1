# Copyright (c) Microsoft Corporation. All rights reserved.
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
        #The default value is ','
        $returnObject = $inputObject | ConvertTo-Csv -UseCulture -IncludeTypeInformation
        $returnObject.Count | Should -Be 3
        $returnObject[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should -BeExactly "`"First`",`"Second`""
        $returnObject[2] | Should -BeExactly "`"1`",`"2`""
    }

    It "Test convertto-csv with Delimiter" {
        #The default value is ','
        $returnObject = $inputObject | ConvertTo-Csv -Delimiter ";" -IncludeTypeInformation
        $returnObject.Count | Should -Be 3
        $returnObject[0] | Should -BeExactly "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should -BeExactly "`"First`";`"Second`""
        $returnObject[2] | Should -BeExactly "`"1`";`"2`""
    }
}

Describe "ConvertTo-Csv" -Tags "CI" {
    $Name = "Hello"; $Data = "World";
    $testObject = New-Object psobject -Property @{ FirstColumn = $Name; SecondColumn = $Data }

    It "Should Be able to be called without error" {
	{ $testObject | ConvertTo-Csv } | Should -Not -Throw
    }

    It "Should output an array of objects" {
        $result = $testObject | ConvertTo-Csv
        ,$result | Should -BeOfType "System.Array"
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

    It "Does not support -IncludeTypeInformation and -NoTypeInformation at the same time" {
        { $testObject | ConvertTo-Csv -IncludeTypeInformation -NoTypeInformation } | 
            ShouldBeErrorId "CannotSpecifyIncludeTypeInformationAndNoTypeInformation,Microsoft.PowerShell.Commands.ConvertToCsvCommand"
    }

}
