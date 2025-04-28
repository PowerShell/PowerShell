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

    It 'Correctly parses large numbers with separators' {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo("fr-FR")
        $formattedNumber = "1 000"
        $convertedValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo($formattedNumber, [bigint])
        $convertedValue | Should -Be 1000
    }
}
