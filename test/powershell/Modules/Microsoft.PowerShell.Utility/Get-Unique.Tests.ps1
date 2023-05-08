# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Unique DRT Unit Tests" -Tags "CI" {
    It "Command get-unique works with AsString switch" {
        $inputArray = "aa","aa","Aa","ba","BA","BA"
        $results = $inputArray | Get-Unique -AsString

        $results.Length | Should -Be 4

        $results[0] | Should -BeExactly "aa"
        $results[1] | Should -BeExactly "Aa"
        $results[2] | Should -BeExactly "ba"
        $results[3] | Should -BeExactly "BA"

        $results[0] | Should -BeOfType System.String
        $results[1] | Should -BeOfType System.String
        $results[2] | Should -BeOfType System.String
        $results[3] | Should -BeOfType System.String
    }
}

Describe "Get-Unique" -Tags "CI" {

    BeforeAll {
        $sortedList1 = 1,2,2,3,3,4,5
        $expectedOutput1 = 1,2,3,4,5
        $collection     = "a", "b", "b", "d"
        $expectedOutput2 = "a", "b", "d"
    }

    It "Should be able to use the Get-Unique cmdlet without error with inputObject switch" {
        { Get-Unique -InputObject $sortedList1 } | Should -Not -Throw
    }

    It "Should output an array" {
        $result = Get-Unique -InputObject $sortedList1
        $result | Should -Not -BeNullOrEmpty
        ,$result | Should -BeOfType System.Array
    }

    It "Should output an array of unchanged items when the InputObject switch is used" {
        $actual   = Get-Unique -InputObject $sortedList1
        $(Compare-Object $actual $sortedList1 -SyncWindow 0).Length | Should -Be 0
    }

    It "Should accept piped input" {
        { $actualOutput = $sortedList1 | Get-Unique } | Should -Not -Throw
    }

    It "Should have the expected output when piped input is used" {
        $actualOutput   = $sortedList1 | Get-Unique
        $(Compare-Object $actualOutput $expectedOutput1 -SyncWindow 0).Length | Should -Be 0
    }

    It "Should be able to input a collection in the inputObject switch" {
        $actual = Get-Unique -InputObject $collection
        $(Compare-Object $actual $collection -SyncWindow 0).Length | Should -Be 0
    }

    It "Should get the unique items when piped collection input is used" {
        $actual = $collection | Get-Unique
        $(Compare-Object $actual $expectedOutput2 -SyncWindow 0).Length | Should -Be 0
    }
}
