Describe "Export-Csv" {
    $testObject = @("test","object","array")
    $testCsv = "output.csv"

    AfterEach {
	Remove-Item $testCsv -Force -ErrorAction SilentlyContinue
    }

    It "Should be able to be called without error" {
	{ $testObject | Export-Csv $testCsv } | Should Not Throw
    }

    It "Should throw if an output file isn't specified" {
	{ $testObject | Export-Csv -ErrorAction SilentlyContinue } | Should Throw
    }

    It "Should be a string when exporting via pipe" {
	$testObject | Export-Csv $testCsv

	$piped = Get-Content $testCsv

	$piped[0] | Should Match ".String"
    }

    It "Should be an object when exporting via the inputObject switch" {
	Export-Csv -InputObject $testObject -Path $testCsv

	$switch = Get-Content $testCsv

	$switch[0] | Should Match ".Object"
    }

    It "Should output a csv file containing a string of all the lengths of each element when piped input is used" {
	$testObject | Export-Csv -Path $testCsv

	$first    = "`"" + $testObject[0].Length.ToString() + "`""
	$second   = "`"" + $testObject[1].Length.ToString() + "`""
	$third    = "`"" + $testObject[2].Length.ToString() + "`""
	$expected = @("#TYPE System.String", "`"Length`"", $first , $second, $third)

	for ( $i = 0; $i -lt $testCsv.Length; $i++)
	{
	    $(Get-Content $testCsv)[$i] | Should Be $expected[$i]
	}
    }

    It "Should be able to use the epcsv alias without error" {
	{ $testObject | Export-Csv -Path $testCsv } | Should Not Throw
    }

    It "Should have the same information when using the alias vs the cmdlet" {
	$testObject | Export-Csv -Path $testCsv

	$aliasObject = "alias.csv"

	$testObject | epcsv -Path $aliasObject

	for ( $i = 0; $i -lt $testCsv.Length; $i++)
	{
	    $(Get-Content $testCsv)[$i] | Should Be $(Get-Content $aliasObject)[$i]
	}

	# Clean up after yourself
	Remove-Item $aliasObject -Force
    }
}
