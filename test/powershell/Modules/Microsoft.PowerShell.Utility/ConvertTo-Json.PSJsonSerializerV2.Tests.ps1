# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {
    # Check if V2 is enabled using Get-ExperimentalFeature
    $script:v2Feature = Get-ExperimentalFeature -Name PSJsonSerializerV2 -ErrorAction SilentlyContinue
    $script:isV2Enabled = $script:v2Feature -and $script:v2Feature.Enabled
}

Describe 'ConvertTo-Json with PSJsonSerializerV2' -Tags "CI" {
    Context "Default values and limits" {
        It "V2: Default depth should be 64" -Skip:(-not $script:isV2Enabled) {
            # Create a 64-level deep object
            $obj = @{ level = 0 }
            for ($i = 1; $i -le 63; $i++) {
                $obj = @{ level = $i; child = $obj }
            }
            # This should work without truncation at default depth 64
            $json = $obj | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Match '"level":63'
        }

        It "V2: Large depth values should be allowed" -Skip:(-not $script:isV2Enabled) {
            { ConvertTo-Json -InputObject @{a=1} -Depth 10000 } | Should -Not -Throw
        }

        It "V2: Depth -1 should work as unlimited" -Skip:(-not $script:isV2Enabled) {
            $obj = @{ level = 0 }
            for ($i = 1; $i -lt 200; $i++) {
                $obj = @{ level = $i; child = $obj }
            }
            $json = $obj | ConvertTo-Json -Depth -1 -Compress -WarningAction SilentlyContinue
            $json | Should -Match '"level":199'
        }

        It "V2: Negative depth other than -1 should throw" -Skip:(-not $script:isV2Enabled) {
            { ConvertTo-Json -InputObject @{a=1} -Depth -2 } | Should -Throw
        }

        It "Legacy: Depth over 100 should throw when V2 is disabled" -Skip:$script:isV2Enabled {
            { ConvertTo-Json -InputObject @{a=1} -Depth 101 } | Should -Throw
        }
    }

    Context "Depth exceeded warning" {
        It "V2: Should output warning when depth is exceeded" -Skip:(-not $script:isV2Enabled) {
            $a = @{ a = @{ b = @{ c = @{ d = 1 } } } }
            $json = $a | ConvertTo-Json -Depth 2 -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
        }

        It "V2: Should convert deep objects to string when depth exceeded" -Skip:(-not $script:isV2Enabled) {
            $inner = [pscustomobject]@{ value = "deep" }
            $outer = [pscustomobject]@{ child = $inner }
            $json = $outer | ConvertTo-Json -Depth 0 -Compress -WarningVariable warn -WarningAction SilentlyContinue
            # At depth 0, child should be converted to string
            $json | Should -Match '"child":'
            $warn | Should -Not -BeNullOrEmpty
        }
    }

    Context "Non-string dictionary keys (Issue #5749)" {
        It "V2: Should serialize dictionary with integer keys" -Skip:(-not $script:isV2Enabled) {
            $dict = @{ 1 = "one"; 2 = "two" }
            $json = $dict | ConvertTo-Json -Compress
            $json | Should -Match '"1":\s*"one"'
            $json | Should -Match '"2":\s*"two"'
        }

        It "V2: Should serialize Exception.Data with non-string keys" -Skip:(-not $script:isV2Enabled) {
            $ex = [System.Exception]::new("test")
            $ex.Data.Add(1, "value1")
            $ex.Data.Add("key", "value2")
            { $ex | ConvertTo-Json -Depth 1 } | Should -Not -Throw
        }
    }

    Context "JsonIgnoreAttribute and HiddenAttribute" {
        It "V2: Should not serialize hidden properties in PowerShell class" -Skip:(-not $script:isV2Enabled) {
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

    Context "Special types" {
        It "V2: Should serialize Uri correctly" -Skip:(-not $script:isV2Enabled) {
            $uri = [uri]"https://example.com/path"
            $json = $uri | ConvertTo-Json -Compress
            $json | Should -BeExactly '"https://example.com/path"'
        }

        It "V2: Should serialize Guid correctly" -Skip:(-not $script:isV2Enabled) {
            $guid = [guid]"12345678-1234-1234-1234-123456789abc"
            $json = ConvertTo-Json -InputObject $guid -Compress
            $json | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
        }

        It "V2: Should serialize BigInteger correctly" -Skip:(-not $script:isV2Enabled) {
            $big = [System.Numerics.BigInteger]::Parse("123456789012345678901234567890")
            $json = ConvertTo-Json -InputObject $big -Compress
            $json | Should -BeExactly '123456789012345678901234567890'
        }

        It "V2: Should serialize enums as numbers by default" -Skip:(-not $script:isV2Enabled) {
            $json = [System.DayOfWeek]::Monday | ConvertTo-Json
            $json | Should -BeExactly '1'
        }

        It "V2: Should serialize enums as strings with -EnumsAsStrings" -Skip:(-not $script:isV2Enabled) {
            $json = [System.DayOfWeek]::Monday | ConvertTo-Json -EnumsAsStrings
            $json | Should -BeExactly '"Monday"'
        }
    }

    Context "Null handling" {
        It "V2: Should serialize null correctly" -Skip:(-not $script:isV2Enabled) {
            $null | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "V2: Should serialize DBNull as null" -Skip:(-not $script:isV2Enabled) {
            [System.DBNull]::Value | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "V2: Should serialize NullString as null" -Skip:(-not $script:isV2Enabled) {
            [NullString]::Value | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "V2: Should handle ETS properties on DBNull" -Skip:(-not $script:isV2Enabled) {
            try {
                $p = Add-Member -InputObject ([System.DBNull]::Value) -MemberType NoteProperty -Name testprop -Value 'testvalue' -PassThru
                $json = $p | ConvertTo-Json -Compress
                $json | Should -Match '"value":null'
                $json | Should -Match '"testprop":"testvalue"'
            }
            finally {
                $p.psobject.Properties.Remove('testprop')
            }
        }
    }

    Context "Collections" {
        It "V2: Should serialize arrays correctly" -Skip:(-not $script:isV2Enabled) {
            $arr = @(1, 2, 3)
            $json = $arr | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,2,3]'
        }

        It "V2: Should serialize hashtable correctly" -Skip:(-not $script:isV2Enabled) {
            $hash = [ordered]@{ a = 1; b = 2 }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"a":1,"b":2}'
        }

        It "V2: Should serialize nested objects correctly" -Skip:(-not $script:isV2Enabled) {
            $obj = [pscustomobject]@{
                name = "test"
                child = [pscustomobject]@{
                    value = 42
                }
            }
            $json = $obj | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"name":"test","child":{"value":42}}'
        }
    }

    Context "EscapeHandling" {
        It "V2: Should not escape by default" -Skip:(-not $script:isV2Enabled) {
            $json = @{ text = "<>&" } | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"text":"<>&"}'
        }

        It "V2: Should escape HTML with -EscapeHandling EscapeHtml" -Skip:(-not $script:isV2Enabled) {
            $json = @{ text = "<>&" } | ConvertTo-Json -Compress -EscapeHandling EscapeHtml
            $json | Should -Match '\\u003C'
            $json | Should -Match '\\u003E'
            $json | Should -Match '\\u0026'
        }

        It "V2: Should escape non-ASCII with -EscapeHandling EscapeNonAscii" -Skip:(-not $script:isV2Enabled) {
            $json = @{ text = "日本語" } | ConvertTo-Json -Compress -EscapeHandling EscapeNonAscii
            $json | Should -Match '\\u'
        }
    }

    Context "Backward compatibility" {
        It "V2: Should still support Newtonsoft JObject" -Skip:(-not $script:isV2Enabled) {
            $jobj = New-Object Newtonsoft.Json.Linq.JObject
            $jobj.Add("key", [Newtonsoft.Json.Linq.JToken]::FromObject("value"))
            $json = @{ data = $jobj } | ConvertTo-Json -Compress -Depth 2
            $json | Should -Match '"key":\s*"value"'
        }

        It "V2: Depth parameter should work" -Skip:(-not $script:isV2Enabled) {
            $obj = @{ a = @{ b = 1 } }
            $json = $obj | ConvertTo-Json -Depth 2 -Compress
            $json | Should -BeExactly '{"a":{"b":1}}'
        }
    }
}
