# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for formatted number parsing' -Tag 'CI' {

    It 'Can parse a number with commas in en-US culture' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("en-US")
        $formattedNumber = "1,000"
        $result = 0
        $parsed = [bigint]::TryParse($formattedNumber, [System.Globalization.NumberStyles]::Number, [System.Globalization.CultureInfo]::CurrentCulture, [ref]$result)
        $parsed | Should -Be $true
        $result | Should -Be 1000
    }

    It 'Can parse a number with commas in hi-IN culture' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("hi-IN")
        $formattedNumber = "1,00,000"
        $result = 0
        $parsed = [bigint]::TryParse($formattedNumber, [System.Globalization.NumberStyles]::Number, [System.Globalization.CultureInfo]::CurrentCulture, [ref]$result)
        $parsed | Should -Be $true
        $result | Should -Be 100000
    }

    It 'Can parse a number in ru-RU culture' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("ru-RU")
        $formattedNumber = "1 000"
        $result = 0
        $parsed = [bigint]::TryParse($formattedNumber, [System.Globalization.NumberStyles]::Number, [System.Globalization.CultureInfo]::CurrentCulture, [ref]$result)
        $parsed | Should -Be $true
        $result | Should -Be 1000
    }
}
