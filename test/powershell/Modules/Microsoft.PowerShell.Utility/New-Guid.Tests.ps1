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

    It "Should respect explicit false value for -Empty" {
        $guid = New-Guid -Empty:$false
        $guid.ToString() | Should -Not -BeExactly "00000000-0000-0000-0000-000000000000"
    }

    It "Should convert a string to a guid" {
        $guid1 = New-Guid
        $guid2 = New-Guid -InputObject $guid1.ToString()
        $guid3 = New-Guid $guid1.ToString()
        $guid2 | Should -BeOfType System.Guid
        $guid1.ToString() | Should -BeExactly $guid2.ToString()
        $guid1.ToString() | Should -BeExactly $guid3.ToString()
    }

    It "Should convert a string to a guid, value from pipeline" {
        $guids = '11c43ee8-b9d3-4e51-b73f-bd9dda66e29c','0f8fad5bd9cb469fa16570867728950e' | New-Guid
        $guids[0].ToString() | Should -BeExactly '11c43ee8-b9d3-4e51-b73f-bd9dda66e29c'
        $guids[1].ToString() | Should -BeExactly '0f8fad5b-d9cb-469f-a165-70867728950e'
    }

    It "Should accept pipeline input" {
        $guids = 1..10 | foreach-object { [guid]::newguid() }
        $observed = $guids.Foreach({$_.ToString()}) | New-Guid
        $observed | Should -Be $guids
    }

    It "Should return different guids with each call" {
        $guid1 = New-Guid
        $guid2 = New-Guid
        $guid1.ToString() | Should -Not -Be $guid2.ToString()
    }
}

