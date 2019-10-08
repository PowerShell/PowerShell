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

    It 'Resolve-ErrorRecord -Newest works' {
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

        $out = Resolve-ErrorRecord -Newest 2
        $out.Count | Should -Be 2
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
}
