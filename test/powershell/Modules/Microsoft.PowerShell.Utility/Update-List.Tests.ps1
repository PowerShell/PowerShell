# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Update-List Tests" -Tag CI {
    BeforeEach {
        $testObj = [pscustomobject]@{a = [System.Collections.ArrayList]::new(@(1,2,3))}
    }


    It "-Add works" {
        $testObj | Update-List -Property a -Add 4
        $testObj.A | Should -Be 1,2,3,4
    }

    It "-Remove works" {
        Update-List -InputObject $testObj -Property a -Remove 2
        $testObj.A | Should -Be 1,3
    }

    It "-Add with -Remove works" {
        $testObj | Update-List -Property a -Remove 2 -Add 4
        $testObj.A | Should -Be 1,3,4
    }

    It "-Replace works" {
        $testObj | Update-List -Property a -Replace 2,4
        $testObj.A | Should -Be 2,4
    }
}
