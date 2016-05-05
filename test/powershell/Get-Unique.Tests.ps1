Describe "Get-Unique DRT Unit Tests" -Tags DRT{
    It "Command get-unique works with AsString switch" {
        $inputArray = "aa","aa","Aa","ba","BA","BA"
        $results = $inputArray | Get-Unique -AsString 
        
        $results.Length | Should Be 4 
               
        $results[0] | Should Be "aa"
        $results[1] | Should Be "Aa"
        $results[2] | Should Be "ba"
        $results[3] | Should Be "BA"

        $results[0].GetType().FullName | Should be System.String
        $results[1].GetType().FullName | Should be System.String
        $results[2].GetType().FullName | Should be System.String
        $results[3].GetType().FullName | Should be System.String
    }
}

Describe "Get-Unique" {
    $sortedList1 = 1,2,2,3,3,4,5
    It "Should be able to use the Get-Unique cmdlet without error with inputObject switch" {
	{ Get-Unique -InputObject $sortedList1 } | Should Not Throw
    }

    It "Should be able to uset the gu alias without error" {
	{ gu -InputObject $sortedList1 } | Should Not Throw
    }

    It "Should output an array" {
	$(Get-Unique -InputObject $sortedList1).GetType().BaseType | Should Be Array
    }

    It "Should output an array of unchanged items when the InputObject switch is used" {
	$actual   = Get-Unique -InputObject $sortedList1

	$actual[0] | Should Be $sortedList1[0]
	$actual[1] | Should Be $sortedList1[1]
	$actual[2] | Should Be $sortedList1[2]
	$actual[3] | Should Be $sortedList1[3]
	$actual[4] | Should Be $sortedList1[4]
	$actual[5] | Should Be $sortedList1[5]
	$actual[6] | Should Be $sortedList1[6]

	$actual.Length | Should Be 7
    }

    It "Should accept piped input" {
	{ $actualOutput = $sortedList1 | Get-Unique } | Should Not Throw
    }

    It "Should have the expected output when piped input is used" {
	$actualOutput   = $sortedList1 | Get-Unique
	$expectedOutput = 1,2,3,4,5

	$actualOutput.Length | Should Be $expectedOutput.Length

	$actualOutput[0] | Should Be $expectedOutput[0]
	$actualOutput[1] | Should Be $expectedOutput[1]
	$actualOutput[2] | Should Be $expectedOutput[2]
	$actualOutput[3] | Should Be $expectedOutput[3]
	$actualOutput[4] | Should Be $expectedOutput[4]
    }

    It "Should be able to input a collection in the inputObject switch" {
	$collection = "a", "b", "b", "d"
	$actual     = Get-Unique -InputObject $collection

	$actual.Length | Should Be $collection.Length

	$actual[0] | Should Be $collection[0]
	$actual[1] | Should Be $collection[1]
	$actual[2] | Should Be $collection[2]
	$actual[3] | Should Be $collection[3]
    }

    It "Should get the unique items when piped collection input is used" {
	$collection     = "a", "b", "b", "d"
	$expectedOutput = "a", "b", "d"

	$actual = $collection | Get-Unique

	$actual.Length | Should Be $expectedOutput.Length

	$actual[0] | Should Be $expectedOutput[0]
	$actual[1] | Should Be $expectedOutput[1]
	$actual[2] | Should Be $expectedOutput[2]
    }
}
