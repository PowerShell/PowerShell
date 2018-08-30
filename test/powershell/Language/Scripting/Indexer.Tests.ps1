# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe 'Tests for indexers' -Tags "CI" {
    It 'Indexer in dictionary' {

        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello"] | Should -BeExactly "There"
    }

    It 'Accessing a Indexed property of a dictionary that does not exist should return $NULL' {
        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello There"] | Should -BeNullOrEmpty
        }

    It 'Wmi object implements an indexer' -Skip:$IsCoreCLR  {

        $service = Get-WmiObject -List -Amended Win32_Service

        $service.Properties["DisplayName"].Name | Should -BeExactly 'DisplayName'
    }

    It 'Accessing a Indexed property of a wmi object that does not exist should return $NULL' -skip:$IsCoreCLR {

        $service = Get-WmiObject -List -Amended Win32_Service
        $service.Properties["Hello There"] | Should -BeNullOrEmpty
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
