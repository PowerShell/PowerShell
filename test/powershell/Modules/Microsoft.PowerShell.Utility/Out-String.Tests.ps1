Describe "Out-String DRT Unit Tests" -Tags "CI" {

    It "check display of properties with names containing wildcard characters" {
        $results = new-object psobject | add-member -passthru noteproperty 'name with square brackets: [0]' 'myvalue' | out-string
        $results.Length | Should BeGreaterThan 1
        $results | Should BeOfType "System.String"
        $results.Contains("myvalue") | Should Be $true
        $results.Contains("name with square brackets: [0]") | Should Be $true
    }

}

Describe "Out-String" -Tags "CI" {

    BeforeAll {
        $nl = [Environment]::NewLine
    }

    It "Should accumulate the strings and returns them as a single string" {
        $testArray = "a", " b"

        $testArray | Out-String | Should Be "a$nl b$nl"
        ,$($testArray | Out-String) | Should BeOfType "System.String"
    }

    It "Should be able to return an array of strings using the stream switch" {
        $testInput = "a", "b"

        ,$($testInput | Out-String) | Should BeOfType "System.String"
        ,$($testInput | Out-String -Stream) | Should BeOfType "System.Array"
    }

    It "Should send all objects through a pipeline when not using the stream switch" {
	$testInput = "a", "b"
	$streamoutputlength = $($testInput | Out-String -Stream).Length
	$nonstreamoutputlength = $($testInput | Out-String).Length

	$nonstreamoutputlength| Should BeGreaterThan $streamoutputlength
    }

    It "Should send a single object through a pipeline when the stream switch is used" {
	$testInput = "a", "b"
	$streamoutputlength = $($testInput | Out-String -Stream).Length
	$nonstreamoutputlength = $($testInput | Out-String).Length

	$streamoutputlength | Should BeLessThan $nonstreamoutputlength
    }
}
