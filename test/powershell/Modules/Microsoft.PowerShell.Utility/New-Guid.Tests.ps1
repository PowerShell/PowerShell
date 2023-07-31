# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-Guid" -Tags "CI" {

    It "Returns a new guid" {
        $guid = New-Guid
        $guid | Should -BeOfType System.Guid
    }

    It "Should not be all zeros" {
        $guid = New-Guid
        $guid.ToString() | Should -Not -BeExactly "00000000-0000-0000-0000-000000000000"
    }

    It "Should be all zeros" {
        $guid = New-Guid -Empty
        $guid.ToString() | Should -BeExactly "00000000-0000-0000-0000-000000000000"
    }

    It "Should convert a string to a guid" {
        $guid1 = New-Guid
        $guid2 = New-Guid -FromString $guid1.ToString()
        $guid3 = New-Guid ($guid1.ToString())
        $guid2 | Should -BeOfType System.Guid
        $guid1.ToString() | Should -BeExactly $guid2.ToString()
        $guid1.ToString() | Should -BeExactly $guid3.ToString()
    }

    It "Should return different guids with each call" {
        $guid1 = New-Guid
        $guid2 = New-Guid
        $guid1.ToString() | Should -Not -Be $guid2.ToString()
    }
}

