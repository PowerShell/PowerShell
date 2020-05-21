# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Tests for indexers' -Tags "CI" {
    It 'Indexer in dictionary' {

        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello"] | Should -BeExactly "There"
    }

    It 'Accessing a Indexed property of a dictionary that does not exist should return $null' {
        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello There"] | Should -BeNullOrEmpty
        }

    It 'CimClass implements an indexer' -Skip:(-not $IsWindows)  {

        $service = Get-CimClass -ClassName Win32_Service

        $service.CimClassProperties["DisplayName"].Name | Should -BeExactly 'DisplayName'
    }

    It 'Accessing a Indexed property of a CimClass that does not exist should return $null' -Skip:(-not $IsWindows) {

        $service = Get-CimClass -ClassName Win32_Service
        $service.CimClassProperties["Hello There"] | Should -BeNullOrEmpty
    }

    It 'ITuple implementations can be indexed' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[0] | Should -Be 10
        $tuple[1] | Should -BeExactly 'Hello'
    }

    It 'ITuple objects can be spliced' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[0..1] | Should -Be @(10, 'Hello')
    }

    It 'Index of -1 should return the last item for ITuple objects' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[-1] | Should -BeExactly 'Hello'
    }
}
