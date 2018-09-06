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

    It "Should return specified cultures with '-Name' parameter" {

        $ciArray = Get-Culture "", "ru-RU"
        $ciArray | Should -BeOfType [CultureInfo]
        ,$ciArray | Should -BeOfType [System.Array]
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType [CultureInfo]
        $ciArray[0].LCID | Should -Be 127
        $ciArray[1] | Should -BeOfType [CultureInfo]
        $ciArray[1].LCID | Should -Be 1049
    }

    It "Should accept values from a pipeline for '-Name' parameter" {

        $ciArray = "", "ru-RU" | Get-Culture
        $ciArray | Should -BeOfType [CultureInfo]
        ,$ciArray | Should -BeOfType [System.Array]
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType [CultureInfo]
        $ciArray[0].LCID | Should -Be 127
        $ciArray[1] | Should -BeOfType [CultureInfo]
        $ciArray[1].LCID | Should -Be 1049
    }

    It "Should return the culture array with '-ListAvailable' parameter" {

        $ciArray = Get-Culture -ListAvailable
        ,$ciArray | Should -BeOfType [System.Array]
        $ciArray.Count | Should -BeGreaterThan 0
        $ciArray[0] | Should -BeOfType [CultureInfo]
    }

    It "Should write an error on unsupported culture name" {

        # The strange culture name come from the fact
        # that .Net Core behavior depend on the underlying OS
        # and can differ on different platforms.
        # See https://github.com/dotnet/corefx/issues/6374#issuecomment-418827420
        $ci = Get-Culture -Name "abcdefghijkl" -ErrorAction SilentlyContinue
        $e = $error[0]
        $e.FullyQualifiedErrorId | Should -BeExactly "ItemNotFoundException,Microsoft.PowerShell.Commands.GetCultureCommand"
        $e.Exception | Should -BeOfType [System.Globalization.CultureNotFoundException]
    }
}
