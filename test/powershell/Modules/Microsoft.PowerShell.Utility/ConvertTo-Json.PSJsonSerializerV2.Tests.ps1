# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {
    $script:isV2Enabled = $EnabledExperimentalFeatures.Contains('PSJsonSerializerV2')
}

Describe 'ConvertTo-Json with PSJsonSerializerV2' -Tags "CI" {
    Context "V1/V2 compatible - Special types" {
        It "Should serialize Uri correctly" {
            $uri = [uri]"https://example.com/path"
            $json = $uri | ConvertTo-Json -Compress
            $json | Should -BeExactly '"https://example.com/path"'
        }

        It "Should serialize BigInteger correctly" {
            $big = [System.Numerics.BigInteger]::Parse("123456789012345678901234567890")
            $json = ConvertTo-Json -InputObject $big -Compress
            $json | Should -BeExactly '123456789012345678901234567890'
        }

        It "Should serialize enums as numbers by default" {
            $json = [System.DayOfWeek]::Monday | ConvertTo-Json
            $json | Should -BeExactly '1'
        }

        It "Should serialize enums as strings with -EnumsAsStrings" {
            $json = [System.DayOfWeek]::Monday | ConvertTo-Json -EnumsAsStrings
            $json | Should -BeExactly '"Monday"'
        }
    }

    Context "V1/V2 compatible - Null handling" {
        It "Should serialize null correctly" {
            $null | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "Should serialize DBNull as null" {
            [System.DBNull]::Value | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "Should serialize NullString as null" {
            [NullString]::Value | ConvertTo-Json | Should -BeExactly 'null'
        }

        It "Should handle ETS properties on DBNull" {
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

    Context "V1/V2 compatible - Collections" {
        It "Should serialize arrays correctly" {
            $arr = @(1, 2, 3)
            $json = $arr | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,2,3]'
        }

        It "Should serialize hashtable correctly" {
            $hash = [ordered]@{ a = 1; b = 2 }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"a":1,"b":2}'
        }

        It "Should serialize nested objects correctly" {
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

    Context "V1/V2 compatible - EscapeHandling" {
        It "Should not escape by default" {
            $json = @{ text = "<>&" } | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"text":"<>&"}'
        }

        It "Should escape HTML with -EscapeHandling EscapeHtml" {
            $json = @{ text = "<>&" } | ConvertTo-Json -Compress -EscapeHandling EscapeHtml
            $json | Should -Match '\\u003C'
            $json | Should -Match '\\u003E'
            $json | Should -Match '\\u0026'
        }

        It "Should escape non-ASCII with -EscapeHandling EscapeNonAscii" {
            $json = @{ text = "日本語" } | ConvertTo-Json -Compress -EscapeHandling EscapeNonAscii
            $json | Should -Match '\\u'
        }
    }

    Context "V1/V2 compatible - Backward compatibility" {
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

    Context "V1 only - Legacy limits" {
        It "Depth over 100 should throw when V2 is disabled" -Skip:$script:isV2Enabled {
            { ConvertTo-Json -InputObject @{a=1} -Depth 101 } | Should -Throw
        }
    }

    Context "V2 only - Depth exceeded warning" {
        It "Should output warning when depth is exceeded" -Skip:(-not $script:isV2Enabled) {
            $a = @{ a = @{ b = @{ c = @{ d = 1 } } } }
            $json = $a | ConvertTo-Json -Depth 2 -WarningVariable warn -WarningAction SilentlyContinue
            $json | Should -Not -BeNullOrEmpty
            $warn | Should -Not -BeNullOrEmpty
        }
    }

    Context "V2 only - Non-string dictionary keys" {
        It "Should serialize dictionary with integer keys" -Skip:(-not $script:isV2Enabled) {
            $dict = @{ 1 = "one"; 2 = "two" }
            $json = $dict | ConvertTo-Json -Compress
            $json | Should -Match '"1":\s*"one"'
            $json | Should -Match '"2":\s*"two"'
        }

        It "Should serialize Exception.Data with non-string keys" -Skip:(-not $script:isV2Enabled) {
            $ex = [System.Exception]::new("test")
            $ex.Data.Add(1, "value1")
            $ex.Data.Add("key", "value2")
            { $ex | ConvertTo-Json -Depth 1 } | Should -Not -Throw
        }
    }

    Context "V2 only - HiddenAttribute" {
        It "Should not serialize hidden properties in PowerShell class" -Skip:(-not $script:isV2Enabled) {
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

    Context "V2 only - Guid serialization" {
        It "Should serialize Guid as string consistently" -Skip:(-not $script:isV2Enabled) {
            $guid = [guid]"12345678-1234-1234-1234-123456789abc"
            # Both methods should produce the same string output (unlike V1 which is inconsistent)
            $jsonPipeline = $guid | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $guid -Compress
            $jsonPipeline | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
            $jsonInputObject | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
        }
    }
}
