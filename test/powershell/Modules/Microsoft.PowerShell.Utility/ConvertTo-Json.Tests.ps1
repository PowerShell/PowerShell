# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'ConvertTo-Json' -tags "CI" {
    BeforeAll {
        $newline = [System.Environment]::NewLine
    }

    It 'Newtonsoft.Json.Linq.Jproperty should be converted to Json properly' {
        $EgJObject = New-Object -TypeName Newtonsoft.Json.Linq.JObject
        $EgJObject.Add("TestValue1", "123456")
        $EgJObject.Add("TestValue2", "78910")
        $EgJObject.Add("TestValue3", "99999")
        $dict = @{}
        $dict.Add('JObject', $EgJObject)
        $dict.Add('StrObject', 'This is a string Object')
        $properties = @{'DictObject' = $dict; 'RandomString' = 'A quick brown fox jumped over the lazy dog'}
        $object = New-Object -TypeName psobject -Property $properties
        $jsonFormat = ConvertTo-Json -InputObject $object
        $jsonFormat | Should -Match '"TestValue1": 123456'
        $jsonFormat | Should -Match '"TestValue2": 78910'
        $jsonFormat | Should -Match '"TestValue3": 99999'
    }

	It "StopProcessing should succeed" -Pending:$true {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript({
            $obj = [PSCustomObject]@{P1 = ''; P2 = ''; P3 = ''; P4 = ''; P5 = ''; P6 = ''}
            $obj.P1 = $obj.P2 = $obj.P3 = $obj.P4 = $obj.P5 = $obj.P6 = $obj
            1..100 | ForEach-Object { $obj } | ConvertTo-Json -Depth 10 -Verbose
            # the conversion is expected to take some time, this throw is in case it doesn't
            throw "Should not have thrown exception"
        })
        $null = $ps.BeginInvoke()
        # wait for verbose message from ConvertTo-Json to ensure cmdlet is processing
        Wait-UntilTrue { $ps.Streams.Verbose.Count -gt 0 } | Should -BeTrue
        $null = $ps.BeginStop($null, $null)
        # wait a bit to ensure state has changed, not using synchronous Stop() to avoid blocking Pester
        Start-Sleep -Milliseconds 100
        $ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
        $ps.Dispose()
    }

    It "Should accept minimum depth as 0." {
        $ComplexObject = [PSCustomObject] @{
            FirstLevel1  = @{
                Child1_1 = 0
                Bool = $True
            }
            FirstLevel2 = @{
                Child2_1 = 'Child_2_1_Value'
                Child2_2 = @{
                     ChildOfChild2_2= @(1,2,3)
                    }
                    Float = 1.2
                }
                Integer = 10
                Bool = $False
            }

        $ExpectedOutput = '{
  "FirstLevel1": "System.Collections.Hashtable",
  "FirstLevel2": "System.Collections.Hashtable",
  "Integer": 10,
  "Bool": false
}'

        $output = $ComplexObject | ConvertTo-Json -Depth 0
        $output | Should -Be $ExpectedOutput
    }

    It "The result string is packed in an array symbols when AsArray parameter is used." {
        $output = 1 | ConvertTo-Json -AsArray
        $output | Should -BeLike "``[*1*]"

        $output = 1,2 | ConvertTo-Json -AsArray
        $output | Should -BeLike "``[*1*2*]"
    }

    It "The result string is not packed in the array symbols when there is only one input object and AsArray parameter is not used." {
        $output = 1 | ConvertTo-Json
        $output | Should -BeExactly '1'
    }

    It "The result string should <Name>." -TestCases @(
        @{name = "be not escaped by default.";                     params = @{};                              expected = "{$newline  ""abc"": ""'def'""$newline}" }
        @{name = "be not escaped with '-EscapeHandling Default'."; params = @{EscapeHandling = 'Default'};    expected = "{$newline  ""abc"": ""'def'""$newline}" }
        @{name = "be escaped with '-EscapeHandling EscapeHtml'.";  params = @{EscapeHandling = 'EscapeHtml'}; expected = "{$newline  ""abc"": ""\u0027def\u0027""$newline}" }
    ) {
        param ($name, $params ,$expected)

        @{ 'abc' = "'def'" } | ConvertTo-Json @params | Should -BeExactly $expected
    }

    It "Should handle null" {
        [pscustomobject] @{ prop=$null } | ConvertTo-Json -Compress | Should -BeExactly '{"prop":null}'
        $null | ConvertTo-Json -Compress | Should -Be 'null'
        ConvertTo-Json -Compress $null | Should -Be 'null'
        1, $null, 2 | ConvertTo-Json -Compress | Should -Be '[1,null,2]'
    }

    It "Should handle 'AutomationNull.Value' and 'NullString.Value' correctly" {
        [ordered]@{
            a = $null;
            b = [System.Management.Automation.Internal.AutomationNull]::Value;
            c = [System.DBNull]::Value;
            d = [NullString]::Value
        } | ConvertTo-Json -Compress | Should -BeExactly '{"a":null,"b":null,"c":null,"d":null}'

        ConvertTo-Json ([System.Management.Automation.Internal.AutomationNull]::Value) | Should -BeExactly 'null'
        ConvertTo-Json ([NullString]::Value) | Should -BeExactly 'null'

        ConvertTo-Json -Compress @(
            $null,
            [System.Management.Automation.Internal.AutomationNull]::Value,
            [System.DBNull]::Value,
            [NullString]::Value
        ) | Should -BeExactly '[null,null,null,null]'
    }

    It "Should handle the ETS properties added to 'DBNull.Value' and 'NullString.Value'" {
        try
        {
            $p1 = Add-Member -InputObject ([System.DBNull]::Value) -MemberType NoteProperty -Name dbnull -Value 'dbnull' -PassThru
            $p2 = Add-Member -InputObject ([NullString]::Value) -MemberType NoteProperty -Name nullstr -Value 'nullstr' -PassThru

            $p1, $p2 | ConvertTo-Json -Compress | Should -BeExactly '[{"value":null,"dbnull":"dbnull"},{"value":null,"nullstr":"nullstr"}]'
        }
        finally
        {
            $p1.psobject.Properties.Remove('dbnull')
            $p2.psobject.Properties.Remove('nullstr')
        }
    }

    It 'Should not serialize ETS properties added to DateTime' {
        $date = "2021-06-24T15:54:06.796999-07:00"
        $d = [DateTime]::Parse($date)

        # need to use wildcard here due to some systems may be configured with different culture setting showing time in different format
        $d | ConvertTo-Json -Compress | Should -BeLike '"2021-06-24T*'
        $d | ConvertTo-Json | ConvertFrom-Json | Should -Be $d
    }

    It 'Should not serialize ETS properties added to String' {
        $text = "Hello there"
        $t = Add-Member -InputObject $text -MemberType NoteProperty -Name text -Value $text -PassThru
        $t | ConvertTo-Json -Compress | Should -BeExactly "`"$text`""
    }

    It 'Should serialize BigInteger values' {
        $obj = [Ordered]@{
            Positive = 18446744073709551615n
            Negative = -18446744073709551615n
        }

        $actual = ConvertTo-Json -Compress -InputObject $obj
        $actual | Should -Be '{"Positive":18446744073709551615,"Negative":-18446744073709551615}'
    }
    #region Comprehensive Scalar Type Tests (Phase 1)
    # Test coverage for ConvertTo-Json scalar serialization
    # Covers: Pipeline vs InputObject, ETS vs no ETS, all primitive and special types

    Context 'Primitive scalar types' {
        It 'Should serialize <TypeName> value <Value> correctly' -TestCases @(
            # Integer types
            @{ TypeName = 'int'; Value = 42; Expected = '42' }
            @{ TypeName = 'int'; Value = -42; Expected = '-42' }
            @{ TypeName = 'int'; Value = 0; Expected = '0' }
            @{ TypeName = 'int'; Value = [int]::MaxValue; Expected = '2147483647' }
            @{ TypeName = 'int'; Value = [int]::MinValue; Expected = '-2147483648' }
            @{ TypeName = 'long'; Value = 9223372036854775807L; Expected = '9223372036854775807' }
            @{ TypeName = 'long'; Value = -9223372036854775808L; Expected = '-9223372036854775808' }
            # Floating-point types
            @{ TypeName = 'double'; Value = 3.14159; Expected = '3.14159' }
            @{ TypeName = 'double'; Value = -3.14159; Expected = '-3.14159' }
            @{ TypeName = 'double'; Value = 0.0; Expected = '0.0' }
            @{ TypeName = 'float'; Value = [float]3.14; Expected = '3.14' }
            @{ TypeName = 'decimal'; Value = 123.456d; Expected = '123.456' }
            # Boolean
            @{ TypeName = 'bool'; Value = $true; Expected = 'true' }
            @{ TypeName = 'bool'; Value = $false; Expected = 'false' }
        ) {
            param($TypeName, $Value, $Expected)
            $Value | ConvertTo-Json -Compress | Should -BeExactly $Expected
        }
    }

    Context 'String scalar types' {
        It 'Should serialize string <Description> correctly' -TestCases @(
            @{ Description = 'regular'; Value = 'hello'; Expected = '"hello"' }
            @{ Description = 'empty'; Value = ''; Expected = '""' }
            @{ Description = 'with spaces'; Value = 'hello world'; Expected = '"hello world"' }
            @{ Description = 'with newline'; Value = "line1`nline2"; Expected = '"line1\nline2"' }
            @{ Description = 'with tab'; Value = "col1`tcol2"; Expected = '"col1\tcol2"' }
            @{ Description = 'with quotes'; Value = 'say "hello"'; Expected = '"say \"hello\""' }
            @{ Description = 'with backslash'; Value = 'c:\path'; Expected = '"c:\\path"' }
            @{ Description = 'unicode'; Value = '日本語'; Expected = '"日本語"' }
            @{ Description = 'emoji'; Value = '😀'; Expected = '"😀"' }
        ) {
            param($Description, $Value, $Expected)
            $Value | ConvertTo-Json -Compress | Should -BeExactly $Expected
        }
    }

    Context 'DateTime and related types' {
        It 'Should serialize DateTime with UTC kind' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc)
            $json = $dt | ConvertTo-Json -Compress
            $json | Should -BeExactly '"2024-06-15T10:30:00Z"'
        }

        It 'Should serialize DateTime with Local kind' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Local)
            $json = $dt | ConvertTo-Json -Compress
            $json | Should -Match '^"2024-06-15T10:30:00'
        }

        It 'Should serialize DateTime with Unspecified kind' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Unspecified)
            $json = $dt | ConvertTo-Json -Compress
            $json | Should -BeExactly '"2024-06-15T10:30:00"'
        }

        It 'Should serialize DateTimeOffset correctly' {
            $dto = [DateTimeOffset]::new(2024, 6, 15, 10, 30, 0, [TimeSpan]::FromHours(9))
            $json = $dto | ConvertTo-Json -Compress
            $json | Should -BeExactly '"2024-06-15T10:30:00+09:00"'
        }

        It 'Should serialize TimeSpan as object with properties' {
            $ts = [TimeSpan]::new(1, 2, 3, 4, 5)
            $json = $ts | ConvertTo-Json -Compress
            # TimeSpan is serialized as object with all properties
            $json | Should -Match '"Ticks":'
            $json | Should -Match '"Days":1'
            $json | Should -Match '"Hours":2'
            $json | Should -Match '"Minutes":3'
            $json | Should -Match '"Seconds":4'
        }
    }

    Context 'Guid type' {
        It 'Should serialize Guid as string via InputObject' {
            $guid = [Guid]::new('12345678-1234-1234-1234-123456789abc')
            $json = ConvertTo-Json -InputObject $guid -Compress
            $json | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
        }

        It 'Should serialize Guid as object with Extended properties via Pipeline' {
            $guid = [Guid]::new('12345678-1234-1234-1234-123456789abc')
            $json = $guid | ConvertTo-Json -Compress
            # Pipeline adds Extended property (Guid)
            $json | Should -Match '"value":"12345678-1234-1234-1234-123456789abc"'
            $json | Should -Match '"Guid":"12345678-1234-1234-1234-123456789abc"'
        }

        It 'Should serialize empty Guid correctly via InputObject' {
            $json = ConvertTo-Json -InputObject ([Guid]::Empty) -Compress
            $json | Should -BeExactly '"00000000-0000-0000-0000-000000000000"'
        }
    }

    Context 'Uri type' {
        It 'Should serialize Uri <Description> correctly' -TestCases @(
            @{ Description = 'http'; UriString = 'http://example.com'; Expected = '"http://example.com"' }
            @{ Description = 'https with path'; UriString = 'https://example.com/path'; Expected = '"https://example.com/path"' }
            @{ Description = 'with query'; UriString = 'https://example.com/search?q=test'; Expected = '"https://example.com/search?q=test"' }
            @{ Description = 'file'; UriString = 'file:///c:/temp/file.txt'; Expected = '"file:///c:/temp/file.txt"' }
        ) {
            param($Description, $UriString, $Expected)
            $uri = [Uri]$UriString
            $json = $uri | ConvertTo-Json -Compress
            $json | Should -BeExactly $Expected
        }
    }

    Context 'Enum types' {
        It 'Should serialize enum <EnumType>::<Value> as <Expected>' -TestCases @(
            @{ EnumType = 'System.DayOfWeek'; Value = 'Sunday'; Expected = '0' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Monday'; Expected = '1' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Saturday'; Expected = '6' }
            @{ EnumType = 'System.ConsoleColor'; Value = 'Red'; Expected = '12' }
            @{ EnumType = 'System.IO.FileAttributes'; Value = 'ReadOnly'; Expected = '1' }
            @{ EnumType = 'System.IO.FileAttributes'; Value = 'Hidden'; Expected = '2' }
        ) {
            param($EnumType, $Value, $Expected)
            $enumValue = [Enum]::Parse($EnumType, $Value)
            $json = $enumValue | ConvertTo-Json -Compress
            $json | Should -BeExactly $Expected
        }

        It 'Should serialize enum <EnumType>::<Value> as "<Expected>" with -EnumsAsStrings' -TestCases @(
            @{ EnumType = 'System.DayOfWeek'; Value = 'Sunday'; Expected = 'Sunday' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Monday'; Expected = 'Monday' }
            @{ EnumType = 'System.ConsoleColor'; Value = 'Red'; Expected = 'Red' }
        ) {
            param($EnumType, $Value, $Expected)
            $enumValue = [Enum]::Parse($EnumType, $Value)
            $json = $enumValue | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly "`"$Expected`""
        }

        It 'Should serialize flags enum correctly' {
            $flags = [System.IO.FileAttributes]::ReadOnly -bor [System.IO.FileAttributes]::Hidden
            $json = $flags | ConvertTo-Json -Compress
            $json | Should -BeExactly '3'
        }

        It 'Should serialize flags enum as string with -EnumsAsStrings' {
            $flags = [System.IO.FileAttributes]::ReadOnly -bor [System.IO.FileAttributes]::Hidden
            $json = $flags | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '"ReadOnly, Hidden"'
        }
    }

    Context 'IPAddress type' {
        It 'Should serialize IPAddress v4 correctly via InputObject' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $json = ConvertTo-Json -InputObject $ip -Compress
            # Raw object serializes base properties
            $json | Should -Not -Match 'IPAddressToString'
        }

        It 'Should serialize IPAddress v6 correctly via InputObject' {
            $ip = [System.Net.IPAddress]::Parse('::1')
            $json = ConvertTo-Json -InputObject $ip -Compress
            $json | Should -Not -Match 'IPAddressToString'
        }

        It 'Should serialize IPAddress with Extended properties via Pipeline' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $json = $ip | ConvertTo-Json -Compress
            # Pipeline wraps in PSObject, adding Extended property
            $json | Should -Match 'IPAddressToString'
        }
    }

    Context 'Pipeline vs InputObject for scalars' {
        It 'Should serialize <TypeName> identically via Pipeline and InputObject' -TestCases @(
            @{ TypeName = 'int'; Value = 42 }
            @{ TypeName = 'string'; Value = 'hello' }
            @{ TypeName = 'bool'; Value = $true }
            @{ TypeName = 'double'; Value = 3.14 }
            # Note: Guid differs between Pipeline and InputObject (Guid adds Extended properties via Pipeline)
        ) {
            param($TypeName, $Value)
            $jsonPipeline = $Value | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $Value -Compress
            $jsonPipeline | Should -BeExactly $jsonInputObject
        }

        It 'Should serialize DateTime identically via Pipeline and InputObject' {
            $dt = [DateTime]::new(2024, 1, 15, 10, 30, 0, [DateTimeKind]::Utc)
            $jsonPipeline = $dt | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dt -Compress
            $jsonPipeline | Should -BeExactly $jsonInputObject
        }

        It 'Should serialize DateTimeOffset identically via Pipeline and InputObject' {
            $dto = [DateTimeOffset]::new(2024, 1, 15, 10, 30, 0, [TimeSpan]::Zero)
            $jsonPipeline = $dto | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dto -Compress
            $jsonPipeline | Should -BeExactly $jsonInputObject
        }

        It 'Should serialize Uri identically via Pipeline and InputObject' {
            $uri = [Uri]'https://example.com/path'
            $jsonPipeline = $uri | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $uri -Compress
            $jsonPipeline | Should -BeExactly $jsonInputObject
        }
    }

    Context 'Scalars as elements of arrays' {
        It 'Should serialize array of <TypeName> correctly' -TestCases @(
            @{ TypeName = 'int'; Values = @(1, 2, 3); Expected = '[1,2,3]' }
            @{ TypeName = 'string'; Values = @('a', 'b', 'c'); Expected = '["a","b","c"]' }
            @{ TypeName = 'bool'; Values = @($true, $false, $true); Expected = '[true,false,true]' }
            @{ TypeName = 'double'; Values = @(1.1, 2.2, 3.3); Expected = '[1.1,2.2,3.3]' }
        ) {
            param($TypeName, $Values, $Expected)
            $json = $Values | ConvertTo-Json -Compress
            $json | Should -BeExactly $Expected
        }

        It 'Should serialize array of Guid with Extended properties via Pipeline' {
            $guids = @(
                [Guid]'11111111-1111-1111-1111-111111111111',
                [Guid]'22222222-2222-2222-2222-222222222222'
            )
            $json = $guids | ConvertTo-Json -Compress
            # Pipeline adds Extended properties to each Guid
            $json | Should -Match '"value":"11111111-1111-1111-1111-111111111111"'
            $json | Should -Match '"value":"22222222-2222-2222-2222-222222222222"'
        }

        It 'Should serialize array of enum correctly' {
            $enums = @([DayOfWeek]::Monday, [DayOfWeek]::Wednesday, [DayOfWeek]::Friday)
            $json = $enums | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,3,5]'
        }

        It 'Should serialize array of enum as strings with -EnumsAsStrings' {
            $enums = @([DayOfWeek]::Monday, [DayOfWeek]::Wednesday, [DayOfWeek]::Friday)
            $json = $enums | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '["Monday","Wednesday","Friday"]'
        }

        It 'Should serialize mixed type array correctly' {
            $mixed = @(1, 'two', $true, 3.14)
            $json = $mixed | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,"two",true,3.14]'
        }

        It 'Should serialize array with null elements correctly' {
            $arr = @(1, $null, 'three')
            $json = $arr | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,null,"three"]'
        }
    }

    Context 'Scalars as values in hashtables and PSCustomObject' {
        It 'Should serialize hashtable with scalar values correctly' {
            $hash = [ordered]@{
                intVal = 42
                strVal = 'hello'
                boolVal = $true
                doubleVal = 3.14
                nullVal = $null
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"intVal":42,"strVal":"hello","boolVal":true,"doubleVal":3.14,"nullVal":null}'
        }

        It 'Should serialize PSCustomObject with scalar values correctly' {
            $obj = [PSCustomObject]@{
                intVal = 42
                strVal = 'hello'
                boolVal = $true
                doubleVal = 3.14
            }
            $json = $obj | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"intVal":42,"strVal":"hello","boolVal":true,"doubleVal":3.14}'
        }

        It 'Should serialize hashtable with DateTime value correctly' {
            $hash = @{ dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc) }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"dt":"2024-06-15T10:30:00Z"}'
        }

        It 'Should serialize hashtable with Guid value correctly' {
            $hash = @{ id = [Guid]'12345678-1234-1234-1234-123456789abc' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"id":"12345678-1234-1234-1234-123456789abc"}'
        }

        It 'Should serialize hashtable with enum value correctly' {
            $hash = @{ day = [DayOfWeek]::Monday }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"day":1}'
        }

        It 'Should serialize hashtable with enum as string correctly' {
            $hash = @{ day = [DayOfWeek]::Monday }
            $json = $hash | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '{"day":"Monday"}'
        }

        It 'Should serialize hashtable with Uri value correctly' {
            $hash = @{ url = [Uri]'https://example.com' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"url":"https://example.com"}'
        }

        It 'Should serialize hashtable with BigInteger value correctly' {
            $hash = @{ big = 18446744073709551615n }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"big":18446744073709551615}'
        }
    }

    Context 'ETS properties on scalar types' {
        It 'Should ignore ETS properties on string' {
            $str = 'hello'
            $str = Add-Member -InputObject $str -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $str | ConvertTo-Json -Compress
            $json | Should -BeExactly '"hello"'
            $json | Should -Not -Match 'MyProp'
        }

        It 'Should ignore ETS properties on DateTime' {
            $dt = [DateTime]::new(2024, 6, 15, 0, 0, 0, [DateTimeKind]::Utc)
            $dt = Add-Member -InputObject $dt -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $dt | ConvertTo-Json -Compress
            $json | Should -Match '^"2024-06-15'
            $json | Should -Not -Match 'MyProp'
        }

        It 'Should include ETS properties on Uri' {
            $uri = [Uri]'https://example.com'
            $uri = Add-Member -InputObject $uri -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $uri | ConvertTo-Json -Compress
            $json | Should -Match 'MyProp'
            $json | Should -Match 'value.*https://example.com'
        }

        It 'Should include ETS properties on Guid' {
            $guid = [Guid]::NewGuid()
            $guid = Add-Member -InputObject $guid -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $guid | ConvertTo-Json -Compress
            $json | Should -Match 'MyProp'
        }
    }
}
