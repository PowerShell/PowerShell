Describe "ConvertTo-Csv" {
    $Name = "Hello"; $Data = "World";
    $testObject = New-Object psobject -Property @{ FirstColumn = $Name; SecondColumn = $Data }


    It "Should Be able to be called without error" {
	{ $testObject | ConvertTo-Csv } | Should Not Throw
    }

    It "Should output an array of objects" {
	$result = $testObject | ConvertTo-Csv

	$result.GetType().BaseType.Name | Should Be "Array"
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
