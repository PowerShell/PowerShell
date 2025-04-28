# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PowerShell Type Conversion - BigInteger Parsing' -Tag 'CI' {

    It 'Can convert formatted numbers using PowerShell type system' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("en-US")
        $formattedNumber = "1,000"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 1000
    }

    It 'Handles formatted numbers correctly in hi-IN culture' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("hi-IN")
        $formattedNumber = "1,00,000"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 100000
    }

    It 'Handles large comma-separated numbers that previously failed' {
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
}
