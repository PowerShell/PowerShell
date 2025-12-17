# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {
    $script:isV2Enabled = $EnabledExperimentalFeatures.Contains('PSJsonSerializerV2')
}

Describe 'ConvertTo-Json with PSJsonSerializerV2' -Tags "CI" {
    Context "Default values and limits" {
        It "V2: Default depth should be unlimited" -Skip:(-not $script:isV2Enabled) {
            # Create a 200-level deep object - should work with unlimited default depth
            $obj = @{ level = 0 }
            for ($i = 1; $i -lt 200; $i++) {
                $obj = @{ level = $i; child = $obj }
            }
            # This should work without truncation at unlimited default depth
            $json = $obj | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Match '"level":199'
            $warn | Should -BeNullOrEmpty
        }

        It "V2: Large depth values should be allowed" -Skip:(-not $script:isV2Enabled) {
            { ConvertTo-Json -InputObject @{a=1} -Depth 10000 } | Should -Not -Throw
        }

        It "V2: Depth 0 or negative should throw" -Skip:(-not $script:isV2Enabled) {
            { ConvertTo-Json -InputObject @{a=1} -Depth 0 } | Should -Throw
            { ConvertTo-Json -InputObject @{a=1} -Depth -1 } | Should -Throw
            { ConvertTo-Json -InputObject @{a=1} -Depth -2 } | Should -Throw
        }

        It "V2: Minimum depth of 1 should work" -Skip:(-not $script:isV2Enabled) {
            $obj = @{ a = @{ b = 1 } }
            $json = $obj | ConvertTo-Json -Depth 1 -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Match '"a":'
            $warn | Should -Not -BeNullOrEmpty  # depth exceeded warning
        }

        It "Legacy: Depth over 100 should throw when V2 is disabled" -Skip:$script:isV2Enabled {
            { ConvertTo-Json -InputObject @{a=1} -Depth 101 } | Should -Throw
        }
    }

    Context "Circular reference detection" {
        It "V2: Should detect self-referencing object" -Skip:(-not $script:isV2Enabled) {
            $obj = [pscustomobject]@{ Name = "Test"; Self = $null }
            $obj.Self = $obj
            $json = $obj | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
            $warn | Should -Match 'Circular reference'
        }

        It "V2: Should detect circular reference in nested objects" -Skip:(-not $script:isV2Enabled) {
            $parent = [pscustomobject]@{ Name = "Parent"; Child = $null }
            $child = [pscustomobject]@{ Name = "Child"; Parent = $null }
            $parent.Child = $child
            $child.Parent = $parent
            $json = $parent | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
            $warn | Should -Match 'Circular reference'
        }

        It "V2: Should detect circular reference in hashtable" -Skip:(-not $script:isV2Enabled) {
            $hash = @{ Name = "Test" }
            $hash.Self = $hash
            $json = $hash | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
            $warn | Should -Match 'Circular reference'
        }

        It "V2: Should detect circular reference in array" -Skip:(-not $script:isV2Enabled) {
            $arr = @(1, 2, $null)
            $arr[2] = $arr
            $json = ConvertTo-Json -InputObject $arr -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
            $warn | Should -Match 'Circular reference'
        }

        It "V2: Should handle same object appearing multiple times (not circular)" -Skip:(-not $script:isV2Enabled) {
            $shared = @{ value = 42 }
            $obj = @{ first = $shared; second = $shared }
            # Same object appearing in different branches is fine, not circular
            $json = $obj | ConvertTo-Json -Compress -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Match '"value":42'
            # Note: This may or may not produce a warning depending on implementation
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
            $json = $outer | ConvertTo-Json -Depth 1 -Compress -WarningVariable warn -WarningAction SilentlyContinue
            # At depth 1, child should be converted to string
            $json | Should -Match '"child":'
            $warn | Should -Not -BeNullOrEmpty
        }
    }

    Context "Non-string dictionary keys" {
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
        It "Should still support Newtonsoft JObject" {
            $jobj = New-Object Newtonsoft.Json.Linq.JObject
            $jobj.Add("key", [Newtonsoft.Json.Linq.JToken]::FromObject("value"))
            $json = @{ data = $jobj } | ConvertTo-Json -Compress -Depth 2
            $json | Should -Match '"key":\s*"value"'
        }

        It "Depth parameter should work" {
            $obj = @{ a = @{ b = 1 } }
            $json = $obj | ConvertTo-Json -Depth 2 -Compress
            $json | Should -BeExactly '{"a":{"b":1}}'
        }

        It "AsArray parameter should work" {
            $json = @{a=1} | ConvertTo-Json -AsArray -Compress
            $json | Should -BeExactly '[{"a":1}]'
        }

        It "Multiple objects from pipeline should be serialized as array" {
            $json = 1, 2, 3 | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,2,3]'
        }

        It "Multiple objects from pipeline with AsArray should work" {
            $json = @{a=1}, @{b=2} | ConvertTo-Json -AsArray -Compress
            $json | Should -Match '^\[.*\]$'
        }
    }
}
