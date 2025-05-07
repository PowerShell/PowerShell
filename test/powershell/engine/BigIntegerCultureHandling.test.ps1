# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PowerShell Type Conversion - BigInteger Parsing' -Tag 'CI' {

    It 'Handles large numbers with thousands separators that previously failed' {
        $formattedNumber = "9223372036854775,807"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 9223372036854775807
    }

    It 'Handles extremely large numbers to verify precision' {
        $formattedNumber = "99999999999999999999999999999"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 99999999999999999999999999999
    }

    It 'Parses mixed separators correctly' {
        $formattedNumber = "1,0000,00"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 1000000
    }

    It 'Parses a number string using the invariant culture, irrespective of the current culture' {
        $originalCulture = [cultureinfo]::CurrentCulture
        try {
            [cultureinfo]::CurrentCulture = [cultureinfo]::GetCultureInfo("de-DE")
            $formattedNumber = "1.000"  # in de-DE this means 1000
            $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
            # since [bigint] uses invariant culture, this will be parsed as 1
            $convertedValue | Should -Be 1
        }
        finally {
            [cultureinfo]::CurrentCulture = $originalCulture
        }
    }

    It 'Casts from floating-point number string to BigInteger using fallback' {
        $formattedNumber = "1.2"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 1
    }
}
