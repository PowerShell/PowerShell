# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Culture DRT Unit Tests" -Tags "CI" {
    It "Should works proper with get-culture" {
        $results = get-Culture
        $results -is "System.Globalization.CultureInfo" | Should -BeTrue
        $results[0].Name | Should -Be $PSCulture
    }
}

Describe "Get-Culture" -Tags "CI" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

	Get-Culture | Should -BeOfType CultureInfo

    }

    It "Should have $ culture variable be equivalent to (Get-Culture).Name" {

	(Get-Culture).Name | Should -Be $PsCulture

    }

}
