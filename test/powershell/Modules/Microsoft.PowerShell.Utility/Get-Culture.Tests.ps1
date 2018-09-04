# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Culture" -Tags "CI" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

        Get-Culture | Should -BeOfType [CultureInfo]

    }

    It "Should have $ culture variable be equivalent to (Get-Culture).Name" {

        (Get-Culture).Name | Should -Be $PsCulture

    }

    It "Should return the specified culture with '-Name' parameter" {

        $ci = Get-Culture -Name ru-RU
        $ci | Should -BeOfType [CultureInfo]
        $ci.Name | Should -BeExactly "ru-RU"
    }

    It "Should return the culture array with '-ListAvailable' parameter" {

        $ciArray = Get-Culture -ListAvailable
        ,$ciArray | Should -BeOfType [System.Array]
        $ciArray.Count | Should -BeGreaterThan 0
        $ciArray[0] | Should -BeOfType [CultureInfo]
    }
}
