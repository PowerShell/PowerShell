# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Out-String DRT Unit Tests" -Tags "CI" {

    It "check display of properties with names containing wildcard characters" {
        $results = New-Object psobject | Add-Member -PassThru noteproperty 'name with square brackets: [0]' 'myvalue' | Out-String
        $results.Length | Should -BeGreaterThan 1
        $results | Should -BeOfType System.String
        $results.Contains("myvalue") | Should -BeTrue
        $results.Contains("name with square brackets: [0]") | Should -BeTrue
    }

}

Describe "Out-String" -Tags "CI" {

    BeforeAll {
        $nl = [Environment]::NewLine
    }

    It "Should accumulate the strings and returns them as a single string" {
        $testArray = "a", " b"

        $testArray | Out-String | Should -BeExactly "a$nl b$nl"
        ,$($testArray | Out-String) | Should -BeOfType System.String
    }

    It "Should be able to return an array of strings using the stream switch" {
        $testInput = "a", "b"

        ,$($testInput | Out-String) | Should -BeOfType System.String
        ,$($testInput | Out-String -Stream) | Should -BeOfType System.Array
    }

    It "Should send all objects through a pipeline when not using the stream switch" {
	$testInput = "a", "b"
	$streamoutputlength = $($testInput | Out-String -Stream).Length
	$nonstreamoutputlength = $($testInput | Out-String).Length

	$nonstreamoutputlength | Should -BeGreaterThan $streamoutputlength
    }

    It "Should send a single object through a pipeline when the stream switch is used" {
	$testInput = "a", "b"
	$streamoutputlength = $($testInput | Out-String -Stream).Length
	$nonstreamoutputlength = $($testInput | Out-String).Length

	$streamoutputlength | Should -BeLessThan $nonstreamoutputlength
    }

    It "Should not print a newline when the nonewline switch is used" {
        $testArray = "a", "b"
        $testArray | Out-String -NoNewLine | Should -BeExactly "ab"
    }

    It "Should preserve embedded newline when the nonewline switch is used" {
        $testArray = "a$nl", "b"
        $testArray | Out-String -NoNewLine | Should -BeExactly "a${nl}b"
    }

    It "Should throw error when NoNewLine and Stream are used together" {
        $testArray = "a", "b"
        { $testArray | Out-String -NoNewLine -Stream } | Should -Throw -ErrorId  "AmbiguousParameterSet,Microsoft.PowerShell.Commands.OutStringCommand"
    }
}
