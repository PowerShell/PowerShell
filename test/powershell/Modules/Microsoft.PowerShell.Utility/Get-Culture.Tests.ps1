# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Culture" -Tags "CI" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

        $culture = Get-Culture
        $culture | Should -BeOfType [CultureInfo]
        ($culture).LCID | Should -Be $host.CurrentCulture.LCID

        Get-Culture -NoUserOverrides | Should -BeOfType [CultureInfo]
    }

    It "Should have `$PSCulture variable be equivalent to (Get-Culture).Name" {

        (Get-Culture).Name | Should -Be $PsCulture
    }

    It "Should return the specified culture with '-Name' parameter" {

        $ci = Get-Culture -Name ru-RU
        $ci | Should -BeOfType [CultureInfo]
        $ci.Name | Should -BeExactly "ru-RU"

        $ci = Get-Culture -Name ru-RU -NoUserOverrides
        $ci | Should -BeOfType [CultureInfo]
        $ci.Name | Should -BeExactly "ru-RU"
    }

    It "Should return specified cultures with '-Name' parameter" {

        $ciArray = Get-Culture "", "ru-RU"
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType [CultureInfo]
        $ciArray[0].LCID | Should -Be 127
        # Check that for empty name the cmdlet returns an invariant culture.
        $ciArray[0].DisplayName | Should -BeExactly "Invariant Language (Invariant Country)"
        $ciArray[1] | Should -BeOfType [CultureInfo]
        $ciArray[1].LCID | Should -Be 1049
    }

    It "Should accept values from a pipeline for '-Name' parameter" {

        $ciArray = "", "ru-RU" | Get-Culture
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType [CultureInfo]
        $ciArray[0].LCID | Should -Be 127
        $ciArray[1] | Should -BeOfType [CultureInfo]
        $ciArray[1].LCID | Should -Be 1049
    }

    It "Should return the culture array with '-ListAvailable' parameter" {

        $ciArray = Get-Culture -ListAvailable
        $ciArray.Count | Should -BeGreaterThan 0
        $ciArray[0] | Should -BeOfType [CultureInfo]
    }

    It "Should write an error on unsupported culture name" {

        { Get-Culture -Name "abcdefghijkl" -ErrorAction Stop } | Should -PassThru -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetCultureCommand"
    }
}
