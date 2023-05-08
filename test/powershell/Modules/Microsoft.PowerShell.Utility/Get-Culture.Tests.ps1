# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Culture" -Tags "CI" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

        $culture = Get-Culture
        $culture | Should -BeOfType CultureInfo
        ($culture).EnglishName | Should -BeExactly $Host.CurrentCulture.EnglishName

        Get-Culture -NoUserOverrides | Should -BeOfType CultureInfo
    }

    It "Should have (Get-Culture).Name variable be equivalent to `$PSCulture" {

        (Get-Culture).Name | Should -BeExactly $PSCulture
    }

    It "Should return the specified culture with '-Name' parameter" {

        $ci = Get-Culture -Name ru-RU
        $ci | Should -BeOfType CultureInfo
        $ci.Name | Should -BeExactly "ru-RU"

        $ci = Get-Culture -Name ru-RU -NoUserOverrides
        $ci | Should -BeOfType CultureInfo
        $ci.Name | Should -BeExactly "ru-RU"
    }

    It "Should return specified cultures with '-Name' parameter" {

        $ciArray = Get-Culture "", "ru-RU"
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType CultureInfo
        $ciArray[0].EnglishName | Should -BeExactly "Invariant Language (Invariant Country)"

        $ciArray[1] | Should -BeOfType CultureInfo
        $ciArray[1].Name | Should -BeExactly "ru-RU"
        $ciArray[1].EnglishName | Should -BeExactly "Russian (Russia)"
    }

    It "Should accept values from a pipeline for '-Name' parameter" {

        $ciArray = "", "ru-RU" | Get-Culture
        $ciArray | Should -HaveCount 2
        $ciArray[0] | Should -BeOfType CultureInfo
        $ciArray[0].EnglishName | Should -BeExactly "Invariant Language (Invariant Country)"
        $ciArray[1] | Should -BeOfType CultureInfo
        $ciArray[1].Name | Should -BeExactly "ru-RU"
        $ciArray[1].EnglishName | Should -BeExactly "Russian (Russia)"
    }

    It "Should return the culture array with '-ListAvailable' parameter" {

        $ciArray = Get-Culture -ListAvailable
        $ciArray.Count | Should -BeGreaterThan 0
        $ciArray[0] | Should -BeOfType CultureInfo
    }

    It "Should write an error on unsupported culture name" {

        { Get-Culture -Name "abcdefghijkl" -ErrorAction Stop } | Should -PassThru -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetCultureCommand"
    }
}

Describe "`$PSCulture" -Tags "CI" {

    It "`$PSCulture is the current thread culture" {
        $PSCulture | Should -BeExactly $([System.Threading.Thread]::CurrentThread.CurrentCulture.Name)
    }

    It "`$PSUICulture is the current thread culture" {
        $PSUICulture | Should -BeExactly $([System.Threading.Thread]::CurrentThread.CurrentUICulture.Name)
    }

    It "`$PSCulture follows the current thread culture" {
        $oldCulture = [CultureInfo]::CurrentCulture
        $newCulture = "ru-RU"

        # Workaround to pass tests locally
        if ($oldCulture -eq "ru-RU") {
            $newCulture = "fr-FR"
        }

        try {
            [CultureInfo]::currentculture = $newCulture
            $PSCulture | Should -BeExactly $newCulture
            $PSCulture | Should -BeExactly $([System.Threading.Thread]::CurrentThread.CurrentCulture.Name)
        } finally {
            [CultureInfo]::CurrentCulture = $oldCulture
        }
    }

    It "`$PSUICulture follows the current thread culture" {
        $oldUICulture = [CultureInfo]::CurrentUICulture
        $newUICulture = "ru-RU"

        if ($oldUICulture -eq "ru-RU") {
            $newUICulture = "fr-FR"
        }

        try {
            [CultureInfo]::CurrentUICulture = $newUICulture
            $PSUICulture | Should -BeExactly $newUICulture
            $PSUICulture | Should -BeExactly $([System.Threading.Thread]::CurrentThread.CurrentUICulture.Name)
        } finally {
            [CultureInfo]::CurrentUICulture = $oldUICulture
        }
    }
}
