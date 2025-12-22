# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {
    $isV2Enabled = $EnabledExperimentalFeatures.Contains('PSJsonSerializerV2')
}

Describe 'ConvertTo-Json PSJsonSerializerV2 specific behavior' -Tags "CI" -Skip:(-not $isV2Enabled) {
    It 'Should output warning when depth is exceeded' {
        $a = @{ a = @{ b = @{ c = @{ d = 1 } } } }
        $json = $a | ConvertTo-Json -Depth 2 -WarningVariable warn -WarningAction SilentlyContinue
        $json | Should -Not -BeNullOrEmpty
        $warn | Should -Not -BeNullOrEmpty
    }

    It 'Should serialize dictionary with integer keys' {
        $dict = @{ 1 = "one"; 2 = "two" }
        $json = $dict | ConvertTo-Json -Compress
        $json | Should -Match '"1":\s*"one"'
        $json | Should -Match '"2":\s*"two"'
    }

    It 'Should serialize Exception.Data with non-string keys' {
        $ex = [System.Exception]::new("test")
        $ex.Data.Add(1, "value1")
        $ex.Data.Add("key", "value2")
        { $ex | ConvertTo-Json -Depth 1 } | Should -Not -Throw
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

    It 'Should serialize Guid as string consistently' {
        $guid = [guid]"12345678-1234-1234-1234-123456789abc"
        $jsonPipeline = $guid | ConvertTo-Json -Compress
        $jsonInputObject = ConvertTo-Json -InputObject $guid -Compress
        $jsonPipeline | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
        $jsonInputObject | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
    }
}
