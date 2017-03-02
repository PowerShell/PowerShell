Describe "ConvertTo-Csv DRT Unit Tests" -Tags "CI" {
    $inputObject = [pscustomobject]@{ First = 1; Second = 2 }

    It "Test convertto-csv with psobject pipelined" {
        $returnObject = $inputObject | ConvertTo-Csv
        $returnObject.Count | Should Be 3
        $returnObject[0] | Should Be "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should Be "`"First`",`"Second`""
        $returnObject[2] | Should Be "`"1`",`"2`""
    }

    It "Test convertto-csv with NoTypeInformation and psobject pipelined" {
        $returnObject = $inputObject | ConvertTo-Csv -NoTypeInformation
        $returnObject.Count | Should Be 2
        $returnObject[0] | Should Be "`"First`",`"Second`""
        $returnObject[1] | Should Be "`"1`",`"2`""
    }

    It "Test convertto-csv with a useculture flag" {
        #The default value is ','
        $returnObject = $inputObject | ConvertTo-Csv -UseCulture
        $returnObject.Count | Should Be 3
        $returnObject[0] | Should Be "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should Be "`"First`",`"Second`""
        $returnObject[2] | Should Be "`"1`",`"2`""
    }

    It "Test convertto-csv with Delimiter" {
        #The default value is ','
        $returnObject = $inputObject | ConvertTo-Csv -Delimiter ";"
        $returnObject.Count | Should Be 3
        $returnObject[0] | Should Be "#TYPE System.Management.Automation.PSCustomObject"
        $returnObject[1] | Should Be "`"First`";`"Second`""
        $returnObject[2] | Should Be "`"1`";`"2`""
    }
}

Describe "ConvertTo-Csv" -Tags "CI" {
    $Name = "Hello"; $Data = "World";
    $testObject = New-Object psobject -Property @{ FirstColumn = $Name; SecondColumn = $Data }


    It "Should Be able to be called without error" {
	{ $testObject | ConvertTo-Csv } | Should Not Throw
    }

    It "Should output an array of objects" {
        $result = $testObject | ConvertTo-Csv
        ,$result | Should BeOfType "System.Array"
    }

    It "Should return the type of data in the first element of the output array" {
	$result = $testObject | ConvertTo-Csv

	$result[0] | Should Be "#TYPE System.Management.Automation.PSCustomObject"
    }

    It "Should return the column info in the second element of the output array" {
	$result = $testObject | ConvertTo-Csv

	$result[1] | Should Match "`"FirstColumn`""
	$result[1] | Should Match "`"SecondColumn`""
    }

    It "Should return the data as a comma-separated list in the third element of the output array" {
	$result = $testObject | ConvertTo-Csv
	$result[2] | Should Match "`"Hello`""
	$result[2] | Should Match "`"World`""
    }

}
