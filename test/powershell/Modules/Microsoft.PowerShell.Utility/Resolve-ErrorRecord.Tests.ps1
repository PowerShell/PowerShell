# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Resolve-ErrorRecord tests' -Tag CI {
    BeforeAll {
        $skipTest = -not $EnabledExperimentalFeatures.Contains('Microsoft.PowerShell.Utility.PSResolveErrorRecord')
        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'Microsoft.PowerShell.Utility.PSResolveErrorRecord' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It 'Resolve-ErrorRecord resolves $Error[0] and includes InnerException' {
        try {
            1/0
        }
        catch {
        }

        $out = Resolve-ErrorRecord | Out-String
        $out | Should -BeLikeExactly '*InnerException*'
    }

    It 'Resolve-ErrorRecord -Newest `<count>` works: <scenario>' -TestCases @(
        @{ scenario = 'less than total'; count = 1 }
        @{ scenario = 'equal to total'; count = 2 }
        @{ scenario = 'greater than total'; count = 99 }
    ){
        param ($count)

        try {
            1/0
        }
        catch {
        }

        try {
            get-item (new-guid) -ErrorAction SilentlyContinue
        }
        catch {
        }

        $out = Resolve-ErrorRecord -Newest $count

        $expected = $count
        if ($count -eq 99) {
            $expected = $error.Count
        }

        $out.Count | Should -Be $expected
    }

    It 'Resolve-ErrorRecord -Newest with invalid value `<value>` should fail' -TestCases @(
        @{ value = 0 }
        @{ value = -2 }
    ){
        param($value)

        { Resolve-ErrorRecord -Newest $value } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.ResolveErrorRecordCommand'
    }

    It 'Resolve-ErrorRecord will accept pipeline input' {
        try {
            1/0
        }
        catch {
        }

        $out = $error[0] | Resolve-ErrorRecord | Out-String
        $out | Should -BeLikeExactly '*-2146233087*'
    }

    It 'Resolve-ErrorRecord will handle Exceptions' {
        try {
            Invoke-Expression '1/d'
        }
        catch {
        }

        $out = Resolve-ErrorRecord | Out-String
        $out | Should -BeLikeExactly '*ExpectedValueExpression*'
        $out | Should -BeLikeExactly '*UnexpectedToken*'
    }
}
