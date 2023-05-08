# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Group-Object DRT Unit Tests" -Tags "CI" {
    It "Test for CaseSensitive switch" {
        $testObject = 'aA', 'aA', 'AA', 'AA'
        $results = $testObject | Group-Object -CaseSensitive
        $results.Count | Should -Be 2
        $results.Name.Count | Should -Be 2
        $results.Group.Count | Should -Be 4
        $results.Name | Should -Be aA,AA
        $results.Group | Should -Be aA,aA,AA,AA
        ,$results | Should -BeOfType System.Array
    }
}

Describe "Group-Object" -Tags "CI" {

    BeforeAll {
        $testObject = Get-ChildItem
    }

    It "Should be called using an object as piped without error with no switches" {
        {$testObject | Group-Object } | Should -Not -Throw
    }

    It "Should be called using the InputObject without error with no other switches" {
        { Group-Object -InputObject $testObject } | Should -Not -Throw
    }

    It "Should return three columns- count, name, and group" {
        $actual = Group-Object -InputObject $testObject

        $actual.Count       | Should -BeGreaterThan 0
        $actual.Name.Count  | Should -BeGreaterThan 0
        $actual.Group.Count | Should -BeGreaterThan 0
    }

    It "Should use the group alias" {
        { Group-Object -InputObject $testObject } | Should -Not -Throw
    }

    It "Should create a collection when the inputObject parameter is used" {
        $actualParam = Group-Object -InputObject $testObject
        $actualParam.Group.Gettype().Name | Should -Be 'Collection`1'
    }

    It "Should return object of 'GroupInfo' type" {
        $actualParam = Group-Object -InputObject $testObject
        $actualParam | Should -BeOfType Microsoft.PowerShell.Commands.GroupInfo
    }

    It "Should output an array when piped input is used" {
        $actual = $testObject | Group-Object

        ,$actual | Should -BeOfType System.Array
    }

    It "Should have the same output between the group alias and the group-object cmdlet" {
        $actualAlias = Group-Object -InputObject $testObject
        $actualCmdlet = Group-Object -InputObject $testObject

        $actualAlias.Name[0] | Should -Be $actualCmdlet.Name[0]
        $actualAlias.Group[0] | Should -Be $actualCmdlet.Group[0]
    }

    It "Should be able to use the property switch without error" {
        { $testObject | Group-Object -Property Attributes } | Should -Not -Throw

        $actual = $testObject | Group-Object -Property Attributes

        $actual.Group.Count | Should -BeGreaterThan 0
    }

    It "Should be able to use the property switch on multiple properties without error" {
        { $testObject | Group-Object -Property Attributes, Length }

        $actual = $testObject | Group-Object -Property Attributes, Length

        $actual.Group.Count | Should -BeGreaterThan 0
    }

    It "Should be able to omit members of a group using the NoElement switch without error" {
        { $testObject | Group-Object -NoElement } | Should -Not -Throw

        ($testObject | Group-Object -NoElement).Group | Should -BeNullOrEmpty
    }

    It "Should be able to output a hashtable datatype" {
        $actual = $testObject | Group-Object -AsHashTable

        $actual | Should -Not -BeNullOrEmpty
        $actual | Should -BeOfType System.Collections.Hashtable
    }

    It "Should be able to access when output as hash table" {
        $actual = $testObject | Group-Object -AsHashTable

        $actual.Keys | Should -Not -BeNullOrEmpty
    }

    It "Should throw when attempting to use AsString without AsHashTable" {
        { $testObject | Group-Object -AsString } | Should -Throw
    }

    It "Should not throw error when using AsString when the AsHashTable was added" {
        { $testObject | Group-Object -AsHashTable -AsString } | Should -Not -Throw
    }

    It "Should be able to retrieve objects by key when using -AsHashTable without -AsString" {
        $testObject = [pscustomobject] @{a="one"; b=2}, [pscustomobject] @{a="two"; b=10}
        $result = $testObject | Group-Object -AsHashTable -Property a
        $result.one.b | Should -Be 2
        $result["two"].b | Should -Be 10
    }

    It "User's scenario should work (see issue #6933 for link to stackoverflow question)" {
        # Sort numbers into two groups even succeeded, odd failed.
        $result = 1..9 | ForEach-Object {[PSCustomObject]@{ErrorMessage = if ($_ % 2) {'SomeError'} else {''}}} |
            Group-Object -Property {if ($_.ErrorMessage) {'Failed'} else {'Successful'}} -AsHashTable

        $result['Failed'].ErrorMessage.Count | Should -Be 5
        $result['Failed'].ErrorMessage[0] | Should -Be 'SomeError'
        $result['Successful'].ErrorMessage.Count | Should -Be 4
        $result['Successful'].ErrorMessage[0] | Should -Be ''
    }

    It "Should understand empty NoteProperty" {
        $result = "dummy" | Select-Object -Property @{Name = 'X'; Expression = {}} | Group-Object X
        $result.Count | Should -Be 1
        $result[0].Name | Should -Be ""
        $result[0].Group | Should -Be '@{X=}'
    }
}

Describe "Check 'Culture' parameter in order object cmdlets (Group-Object, Sort-Object, Compare-Object)" -Tags "CI" {

    BeforeAll {

        $testObject = Get-ChildItem

    }

    It "Should accept a culture by name" {

        if ( (Get-Culture).Name -eq "ru-Ru" ) {
            $testCulture = "ru-UA"
        }
        else {
            $testCulture = "ru-RU"
        }

        {$testObject | Group-Object -Culture $testCulture } | Should -Not -Throw
    }

    It "Should accept a culture by hex string LCID" {

        if ( (Get-Culture).LCID -eq 1049 ) {
            $testCulture = "0x1000"
        }
        else {
            $testCulture = "0x419"
        }

        {$testObject | Group-Object -Culture $testCulture } | Should -Not -Throw
    }

    It "Should accept a culture by int string LCID" {

        if ( (Get-Culture).LCID -eq 1049 ) {
            $testCulture = "4096"
        }
        else {
            $testCulture = "1049"
        }

        { $testObject | Group-Object -Culture $testCulture } | Should -Not -Throw
    }

    It 'should not throw a key duplication error with -CaseSensitive -AsHashtable' {
        $capitonyms = @(
            [PSCustomObject]@{
                Capitonym = 'Bill'
            }
            [PSCustomObject]@{
                Capitonym = 'bill'
            }
        )

        $Result = $capitonyms | Group-Object -Property Capitonym -AsHashTable -CaseSensitive
        $Result | Should -BeOfType HashTable
        $Result.Keys | Should -BeIn @( 'Bill', 'bill' )
    }
}
