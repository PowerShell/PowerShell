# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'ConvertTo-Json PSJsonSerializerV2 specific behavior' -Tags "CI" {

    BeforeAll {
        $skipTest = -not $EnabledExperimentalFeatures.Contains('PSJsonSerializerV2')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'PSJsonSerializerV2' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It 'Should serialize dictionary with integer keys' {
        $dict = @{ 1 = "one"; 2 = "two" }
        $json = $dict | ConvertTo-Json -Compress
        $json | Should -BeIn @('{"1":"one","2":"two"}', '{"2":"two","1":"one"}')
    }

    It 'Should serialize dictionary with non-string keys converting keys to strings' {
        $dict = [System.Collections.Hashtable]::new()
        $dict.Add(1, "one")
        $dict.Add([guid]"12345678-1234-1234-1234-123456789abc", "guid-value")
        $json = $dict | ConvertTo-Json -Compress
        $json | Should -Match '"1":\s*"one"'
        $json | Should -Match '"12345678-1234-1234-1234-123456789abc":\s*"guid-value"'
    }

    It 'Should not serialize hidden properties in PowerShell class' {
        class TestHiddenClass {
            [string] $Visible
            hidden [string] $Hidden
        }
        $obj = [TestHiddenClass]::new()
        $obj.Visible = "yes"
        $obj.Hidden = "no"
        $json = $obj | ConvertTo-Json -Compress
        $json | Should -Match 'Visible'
        $json | Should -Not -Match 'Hidden'
    }

}
