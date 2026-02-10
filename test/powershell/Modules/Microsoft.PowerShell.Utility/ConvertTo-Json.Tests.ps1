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
        It 'Should serialize <TypeName> value <Value> correctly via Pipeline and InputObject' -TestCases @(
            # Byte types
            @{ TypeName = 'byte'; Value = [byte]0; Expected = '0' }
            @{ TypeName = 'byte'; Value = [byte]255; Expected = '255' }
            @{ TypeName = 'sbyte'; Value = [sbyte]-128; Expected = '-128' }
            @{ TypeName = 'sbyte'; Value = [sbyte]127; Expected = '127' }
            # Short types
            @{ TypeName = 'short'; Value = [short]-32768; Expected = '-32768' }
            @{ TypeName = 'short'; Value = [short]32767; Expected = '32767' }
            @{ TypeName = 'ushort'; Value = [ushort]0; Expected = '0' }
            @{ TypeName = 'ushort'; Value = [ushort]65535; Expected = '65535' }
            # Integer types
            @{ TypeName = 'int'; Value = 42; Expected = '42' }
            @{ TypeName = 'int'; Value = -42; Expected = '-42' }
            @{ TypeName = 'int'; Value = 0; Expected = '0' }
            @{ TypeName = 'int'; Value = [int]::MaxValue; Expected = '2147483647' }
            @{ TypeName = 'int'; Value = [int]::MinValue; Expected = '-2147483648' }
            @{ TypeName = 'uint'; Value = [uint]0; Expected = '0' }
            @{ TypeName = 'uint'; Value = [uint]::MaxValue; Expected = '4294967295' }
            # Long types
            @{ TypeName = 'long'; Value = [long]::MaxValue; Expected = '9223372036854775807' }
            @{ TypeName = 'long'; Value = [long]::MinValue; Expected = '-9223372036854775808' }
            @{ TypeName = 'ulong'; Value = [ulong]0; Expected = '0' }
            @{ TypeName = 'ulong'; Value = [ulong]::MaxValue; Expected = '18446744073709551615' }
            # Floating-point types
            @{ TypeName = 'float'; Value = [float]3.14; Expected = '3.14' }
            @{ TypeName = 'float'; Value = [float]::NaN; Expected = '"NaN"' }
            @{ TypeName = 'float'; Value = [float]::PositiveInfinity; Expected = '"Infinity"' }
            @{ TypeName = 'float'; Value = [float]::NegativeInfinity; Expected = '"-Infinity"' }
            @{ TypeName = 'double'; Value = 3.14159; Expected = '3.14159' }
            @{ TypeName = 'double'; Value = -3.14159; Expected = '-3.14159' }
            @{ TypeName = 'double'; Value = 0.0; Expected = '0.0' }
            @{ TypeName = 'double'; Value = [double]::NaN; Expected = '"NaN"' }
            @{ TypeName = 'double'; Value = [double]::PositiveInfinity; Expected = '"Infinity"' }
            @{ TypeName = 'double'; Value = [double]::NegativeInfinity; Expected = '"-Infinity"' }
            @{ TypeName = 'decimal'; Value = 123.456d; Expected = '123.456' }
            # BigInteger
            @{ TypeName = 'BigInteger'; Value = 18446744073709551615n; Expected = '18446744073709551615' }
            # Boolean
            @{ TypeName = 'bool'; Value = $true; Expected = 'true' }
            @{ TypeName = 'bool'; Value = $false; Expected = 'false' }
            # Null
            @{ TypeName = 'null'; Value = $null; Expected = 'null' }
        ) {
            param($TypeName, $Value, $Expected)
            $jsonPipeline = $Value | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $Value -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        It 'Should include ETS properties on <TypeName>' -TestCases @(
            @{ TypeName = 'int'; Value = 42; Expected = '{"value":42,"MyProp":"test"}' }
            @{ TypeName = 'double'; Value = 3.14; Expected = '{"value":3.14,"MyProp":"test"}' }
        ) {
            param($TypeName, $Value, $Expected)
            $valueWithEts = Add-Member -InputObject $Value -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $valueWithEts | ConvertTo-Json -Compress
            $json | Should -BeExactly $Expected
        }
    }

    Context 'String scalar types' {
        It 'Should serialize string <Description> correctly via Pipeline and InputObject' -TestCases @(
            @{ Description = 'regular'; Value = 'hello'; Expected = '"hello"' }
            @{ Description = 'empty'; Value = ''; Expected = '""' }
            @{ Description = 'with spaces'; Value = 'hello world'; Expected = '"hello world"' }
            @{ Description = 'with newline'; Value = "line1`nline2"; Expected = '"line1\nline2"' }
            @{ Description = 'with tab'; Value = "col1`tcol2"; Expected = '"col1\tcol2"' }
            @{ Description = 'with quotes'; Value = 'say "hello"'; Expected = '"say \"hello\""' }
            @{ Description = 'with backslash'; Value = 'c:\path'; Expected = '"c:\\path"' }
            @{ Description = 'unicode'; Value = '???'; Expected = '"???"' }
            @{ Description = 'emoji'; Value = '??'; Expected = '"??"' }
        ) {
            param($Description, $Value, $Expected)
            $jsonPipeline = $Value | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $Value -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        It 'Should ignore ETS properties on string' {
            $str = Add-Member -InputObject 'hello' -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $str | ConvertTo-Json -Compress
            $json | Should -BeExactly '"hello"'
        }
    }

    Context 'DateTime and related types' {
        It 'Should serialize DateTime with UTC kind via Pipeline and InputObject' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc)
            $jsonPipeline = $dt | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dt -Compress
            $jsonPipeline | Should -BeExactly '"2024-06-15T10:30:00Z"'
            $jsonInputObject | Should -BeExactly '"2024-06-15T10:30:00Z"'
        }

        It 'Should serialize DateTime with Local kind' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Local)
            $json = $dt | ConvertTo-Json -Compress
            $offset = $dt.ToString('zzz')
            $expected = '"2024-06-15T10:30:00' + $offset + '"'
            $json | Should -BeExactly $expected
        }

        It 'Should serialize DateTime with Unspecified kind via Pipeline and InputObject' {
            $dt = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Unspecified)
            $jsonPipeline = $dt | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dt -Compress
            $jsonPipeline | Should -BeExactly '"2024-06-15T10:30:00"'
            $jsonInputObject | Should -BeExactly '"2024-06-15T10:30:00"'
        }

        It 'Should serialize DateTimeOffset correctly via Pipeline and InputObject' {
            $dto = [DateTimeOffset]::new(2024, 6, 15, 10, 30, 0, [TimeSpan]::FromHours(9))
            $jsonPipeline = $dto | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dto -Compress
            $jsonPipeline | Should -BeExactly '"2024-06-15T10:30:00+09:00"'
            $jsonInputObject | Should -BeExactly '"2024-06-15T10:30:00+09:00"'
        }

        It 'Should serialize DateOnly as object with properties' {
            $d = [DateOnly]::new(2024, 6, 15)
            $json = $d | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"Year":2024,"Month":6,"Day":15,"DayOfWeek":6,"DayOfYear":167,"DayNumber":739051}'
        }

        It 'Should serialize TimeOnly as object with properties' {
            $t = [TimeOnly]::new(10, 30, 45)
            $json = $t | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"Hour":10,"Minute":30,"Second":45,"Millisecond":0,"Microsecond":0,"Nanosecond":0,"Ticks":378450000000}'
        }

        It 'Should serialize TimeSpan as object with properties' {
            $ts = [TimeSpan]::new(1, 2, 3, 4, 5)
            $json = $ts | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"Ticks":937840050000,"Days":1,"Hours":2,"Milliseconds":5,"Microseconds":0,"Nanoseconds":0,"Minutes":3,"Seconds":4,"TotalDays":1.0854630208333333,"TotalHours":26.0511125,"TotalMilliseconds":93784005.0,"TotalMicroseconds":93784005000.0,"TotalNanoseconds":93784005000000.0,"TotalMinutes":1563.06675,"TotalSeconds":93784.005}'
        }

        It 'Should ignore ETS properties on DateTime' {
            $dt = [DateTime]::new(2024, 6, 15, 0, 0, 0, [DateTimeKind]::Utc)
            $dt = Add-Member -InputObject $dt -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $dt | ConvertTo-Json -Compress
            $json | Should -BeExactly '"2024-06-15T00:00:00Z"'
        }

        It 'Should include ETS properties on DateTimeOffset' {
            $dto = [DateTimeOffset]::new(2024, 6, 15, 10, 30, 0, [TimeSpan]::Zero)
            $dto = Add-Member -InputObject $dto -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $dto | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"value":"2024-06-15T10:30:00+00:00","MyProp":"test"}'
        }

        It 'Should include ETS properties on DateOnly' {
            $d = [DateOnly]::new(2024, 6, 15)
            $d = Add-Member -InputObject $d -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $d | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"Year":2024,"Month":6,"Day":15,"DayOfWeek":6,"DayOfYear":167,"DayNumber":739051,"MyProp":"test"}'
        }

        It 'Should include ETS properties on TimeOnly' {
            $t = [TimeOnly]::new(10, 30, 45)
            $t = Add-Member -InputObject $t -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $t | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"Hour":10,"Minute":30,"Second":45,"Millisecond":0,"Microsecond":0,"Nanosecond":0,"Ticks":378450000000,"MyProp":"test"}'
        }
    }

    Context 'Guid type' {
        It 'Should serialize Guid as string via InputObject' {
            $guid = [Guid]::new('12345678-1234-1234-1234-123456789abc')
            $json = ConvertTo-Json -InputObject $guid -Compress
            $json | Should -BeExactly '"12345678-1234-1234-1234-123456789abc"'
        }

        It 'Should serialize Guid with Extended properties via Pipeline' {
            $guid = [Guid]::new('12345678-1234-1234-1234-123456789abc')
            $json = $guid | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"value":"12345678-1234-1234-1234-123456789abc","Guid":"12345678-1234-1234-1234-123456789abc"}'
        }

        It 'Should serialize empty Guid correctly via InputObject' {
            $json = ConvertTo-Json -InputObject ([Guid]::Empty) -Compress
            $json | Should -BeExactly '"00000000-0000-0000-0000-000000000000"'
        }

        It 'Should include ETS properties on Guid via Pipeline' {
            $guid = [Guid]::new('12345678-1234-1234-1234-123456789abc')
            $guid = Add-Member -InputObject $guid -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $guid | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"value":"12345678-1234-1234-1234-123456789abc","MyProp":"test","Guid":"12345678-1234-1234-1234-123456789abc"}'
        }
    }

    Context 'Uri type' {
        It 'Should serialize Uri <Description> correctly via Pipeline and InputObject' -TestCases @(
            @{ Description = 'http'; UriString = 'http://example.com'; Expected = '"http://example.com"' }
            @{ Description = 'https with path'; UriString = 'https://example.com/path'; Expected = '"https://example.com/path"' }
            @{ Description = 'with query'; UriString = 'https://example.com/search?q=test'; Expected = '"https://example.com/search?q=test"' }
            @{ Description = 'file'; UriString = 'file:///c:/temp/file.txt'; Expected = '"file:///c:/temp/file.txt"' }
        ) {
            param($Description, $UriString, $Expected)
            $uri = [Uri]$UriString
            $jsonPipeline = $uri | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $uri -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        It 'Should include ETS properties on Uri' {
            $uri = [Uri]'https://example.com'
            $uri = Add-Member -InputObject $uri -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $uri | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"value":"https://example.com","MyProp":"test"}'
        }
    }

    Context 'Enum types' {
        It 'Should serialize enum <EnumType>::<Value> as <Expected> via Pipeline and InputObject' -TestCases @(
            @{ EnumType = 'System.DayOfWeek'; Value = 'Sunday'; Expected = '0' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Monday'; Expected = '1' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Saturday'; Expected = '6' }
            @{ EnumType = 'System.ConsoleColor'; Value = 'Red'; Expected = '12' }
            @{ EnumType = 'System.IO.FileAttributes'; Value = 'ReadOnly'; Expected = '1' }
            @{ EnumType = 'System.IO.FileAttributes'; Value = 'Hidden'; Expected = '2' }
        ) {
            param($EnumType, $Value, $Expected)
            $enumValue = [Enum]::Parse($EnumType, $Value)
            $jsonPipeline = $enumValue | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $enumValue -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        It 'Should serialize enum as "<Expected>" with -EnumsAsStrings' -TestCases @(
            @{ EnumType = 'System.DayOfWeek'; Value = 'Sunday'; Expected = 'Sunday' }
            @{ EnumType = 'System.DayOfWeek'; Value = 'Monday'; Expected = 'Monday' }
            @{ EnumType = 'System.ConsoleColor'; Value = 'Red'; Expected = 'Red' }
        ) {
            param($EnumType, $Value, $Expected)
            $enumValue = [Enum]::Parse($EnumType, $Value)
            $json = $enumValue | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly "`"$Expected`""
        }

        It 'Should serialize flags enum correctly via Pipeline and InputObject' {
            $flags = [System.IO.FileAttributes]::ReadOnly -bor [System.IO.FileAttributes]::Hidden
            $jsonPipeline = $flags | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $flags -Compress
            $jsonPipeline | Should -BeExactly '3'
            $jsonInputObject | Should -BeExactly '3'
        }

        It 'Should serialize flags enum as string with -EnumsAsStrings' {
            $flags = [System.IO.FileAttributes]::ReadOnly -bor [System.IO.FileAttributes]::Hidden
            $json = $flags | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '"ReadOnly, Hidden"'
        }

        It 'Should include ETS properties on Enum' {
            $enum = Add-Member -InputObject ([DayOfWeek]::Monday) -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $enum | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"value":1,"MyProp":"test"}'
        }
    }

    Context 'IPAddress type' {
        It 'Should serialize IPAddress v4 correctly via InputObject' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $json = ConvertTo-Json -InputObject $ip -Compress
            $json | Should -BeExactly '{"AddressFamily":2,"ScopeId":null,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":16885952}'
        }

        It 'Should serialize IPAddress v4 correctly via Pipeline' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $json = $ip | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"AddressFamily":2,"ScopeId":null,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":16885952,"IPAddressToString":"192.168.1.1"}'
        }

        It 'Should serialize IPAddress v6 correctly via InputObject' {
            $ip = [System.Net.IPAddress]::Parse('::1')
            $json = ConvertTo-Json -InputObject $ip -Compress
            $json | Should -BeExactly '{"AddressFamily":23,"ScopeId":0,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":null}'
        }

        It 'Should serialize IPAddress v6 correctly via Pipeline' {
            $ip = [System.Net.IPAddress]::Parse('::1')
            $json = $ip | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"AddressFamily":23,"ScopeId":0,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":null,"IPAddressToString":"::1"}'
        }

        It 'Should include ETS properties on IPAddress' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $ip = Add-Member -InputObject $ip -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = $ip | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"AddressFamily":2,"ScopeId":null,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":16885952,"MyProp":"test","IPAddressToString":"192.168.1.1"}'
        }
    }

    Context 'Scalars as elements of arrays' {
        It 'Should serialize array of <TypeName> correctly via Pipeline and InputObject' -TestCases @(
            @{ TypeName = 'int'; Values = @(1, 2, 3); Expected = '[1,2,3]' }
            @{ TypeName = 'string'; Values = @('a', 'b', 'c'); Expected = '["a","b","c"]' }
            @{ TypeName = 'double'; Values = @(1.1, 2.2, 3.3); Expected = '[1.1,2.2,3.3]' }
        ) {
            param($TypeName, $Values, $Expected)
            $jsonPipeline = $Values | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $Values -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        # Note: bool array test uses InputObject only because $true/$false are singletons
        # and ETS properties added in other tests would affect Pipeline serialization
        It 'Should serialize array of bool correctly via InputObject' {
            $bools = @($true, $false, $true)
            $json = ConvertTo-Json -InputObject $bools -Compress
            $json | Should -BeExactly '[true,false,true]'
        }

        It 'Should serialize array of Guid with Extended properties via Pipeline' {
            $guids = @(
                [Guid]'11111111-1111-1111-1111-111111111111',
                [Guid]'22222222-2222-2222-2222-222222222222'
            )
            $json = $guids | ConvertTo-Json -Compress
            $json | Should -BeExactly '[{"value":"11111111-1111-1111-1111-111111111111","Guid":"11111111-1111-1111-1111-111111111111"},{"value":"22222222-2222-2222-2222-222222222222","Guid":"22222222-2222-2222-2222-222222222222"}]'
        }

        It 'Should serialize array of enum correctly via Pipeline and InputObject' {
            $enums = @([DayOfWeek]::Monday, [DayOfWeek]::Wednesday, [DayOfWeek]::Friday)
            $jsonPipeline = $enums | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $enums -Compress
            $jsonPipeline | Should -BeExactly '[1,3,5]'
            $jsonInputObject | Should -BeExactly '[1,3,5]'
        }

        It 'Should serialize array of enum as strings with -EnumsAsStrings' {
            $enums = @([DayOfWeek]::Monday, [DayOfWeek]::Wednesday, [DayOfWeek]::Friday)
            $json = $enums | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '["Monday","Wednesday","Friday"]'
        }

        # Note: mixed array test uses InputObject only due to $true singleton issue
        It 'Should serialize mixed type array correctly via InputObject' {
            $mixed = @(1, 'two', $true, 3.14)
            $json = ConvertTo-Json -InputObject $mixed -Compress
            $json | Should -BeExactly '[1,"two",true,3.14]'
        }

        It 'Should serialize array with null elements correctly' {
            $arr = @(1, $null, 'three')
            $json = $arr | ConvertTo-Json -Compress
            $json | Should -BeExactly '[1,null,"three"]'
        }

        It 'Should include ETS properties on array via InputObject' {
            $arr = @(1, 2, 3)
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '{"value":[1,2,3],"MyProp":"test"}'
        }
    }

    Context 'Scalars as values in hashtables and PSCustomObject' {
        It 'Should serialize hashtable with scalar values correctly via Pipeline and InputObject' {
            $hash = [ordered]@{
                intVal = 42
                strVal = 'hello'
                boolVal = $true
                doubleVal = 3.14
                nullVal = $null
            }
            $expected = '{"intVal":42,"strVal":"hello","boolVal":true,"doubleVal":3.14,"nullVal":null}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize PSCustomObject with scalar values correctly via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                intVal = 42
                strVal = 'hello'
                boolVal = $true
                doubleVal = 3.14
            }
            $expected = '{"intVal":42,"strVal":"hello","boolVal":true,"doubleVal":3.14}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize hashtable with <TypeName> value correctly' -TestCases @(
            @{ TypeName = 'DateTime'; Value = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc); Expected = '{"val":"2024-06-15T10:30:00Z"}' }
            @{ TypeName = 'Guid'; Value = [Guid]'12345678-1234-1234-1234-123456789abc'; Expected = '{"val":"12345678-1234-1234-1234-123456789abc"}' }
            @{ TypeName = 'Enum'; Value = [DayOfWeek]::Monday; Expected = '{"val":1}' }
            @{ TypeName = 'Uri'; Value = [Uri]'https://example.com'; Expected = '{"val":"https://example.com"}' }
            @{ TypeName = 'BigInteger'; Value = 18446744073709551615n; Expected = '{"val":18446744073709551615}' }
        ) {
            param($TypeName, $Value, $Expected)
            $hash = @{ val = $Value }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $Expected
            $jsonInputObject | Should -BeExactly $Expected
        }

        It 'Should serialize hashtable with enum as string correctly' {
            $hash = @{ day = [DayOfWeek]::Monday }
            $json = $hash | ConvertTo-Json -Compress -EnumsAsStrings
            $json | Should -BeExactly '{"day":"Monday"}'
        }

        It 'Should include ETS properties on hashtable via InputObject' {
            $hash = @{ a = 1 }
            $hash = Add-Member -InputObject $hash -MemberType NoteProperty -Name MyProp -Value 'test' -PassThru
            $json = ConvertTo-Json -InputObject $hash -Compress
            $json | Should -BeExactly '{"a":1,"MyProp":"test"}'
        }
    }

    #endregion Comprehensive Scalar Type Tests (Phase 1)

    #region Comprehensive Array and Dictionary Tests (Phase 2)
    # Test coverage for ConvertTo-Json array and dictionary serialization
    # Covers: Pipeline vs InputObject, ETS vs no ETS, nested structures

    Context 'Array basic serialization' {
        It 'Should serialize empty array correctly via Pipeline and InputObject' {
            $arr = @()
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[]'
            $jsonInputObject | Should -BeExactly '[]'
        }

        It 'Should serialize single element array correctly via Pipeline and InputObject' {
            $arr = @(42)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[42]'
            $jsonInputObject | Should -BeExactly '[42]'
        }

        It 'Should serialize multi-element array correctly via Pipeline and InputObject' {
            $arr = @(1, 2, 3, 4, 5)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[1,2,3,4,5]'
            $jsonInputObject | Should -BeExactly '[1,2,3,4,5]'
        }

        It 'Should serialize string array correctly via Pipeline and InputObject' {
            $arr = @('apple', 'banana', 'cherry')
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '["apple","banana","cherry"]'
            $jsonInputObject | Should -BeExactly '["apple","banana","cherry"]'
        }

        It 'Should serialize typed array correctly via Pipeline and InputObject' {
            [int[]]$arr = @(10, 20, 30)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[10,20,30]'
            $jsonInputObject | Should -BeExactly '[10,20,30]'
        }

        It 'Should serialize array with single null element correctly via Pipeline and InputObject' {
            $arr = @($null)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[null]'
            $jsonInputObject | Should -BeExactly '[null]'
        }

        It 'Should serialize array with multiple null elements correctly via Pipeline and InputObject' {
            $arr = @($null, $null, $null)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[null,null,null]'
            $jsonInputObject | Should -BeExactly '[null,null,null]'
        }
    }

    Context 'Nested arrays' {
        It 'Should serialize 2D array correctly via Pipeline and InputObject' {
            $arr = @(@(1, 2), @(3, 4))
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[[1,2],[3,4]]'
            $jsonInputObject | Should -BeExactly '[[1,2],[3,4]]'
        }

        It 'Should serialize 3D array correctly via Pipeline and InputObject' {
            $arr = @(@(@(1, 2), @(3, 4)), @(@(5, 6), @(7, 8)))
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[[[1,2],[3,4]],[[5,6],[7,8]]]'
            $jsonInputObject | Should -BeExactly '[[[1,2],[3,4]],[[5,6],[7,8]]]'
        }

        It 'Should serialize jagged array correctly via Pipeline and InputObject' {
            $arr = @(@(1), @(2, 3), @(4, 5, 6))
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[[1],[2,3],[4,5,6]]'
            $jsonInputObject | Should -BeExactly '[[1],[2,3],[4,5,6]]'
        }

        It 'Should serialize array containing empty arrays correctly via Pipeline and InputObject' {
            $arr = @(@(), @(1), @())
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[[],[1],[]]'
            $jsonInputObject | Should -BeExactly '[[],[1],[]]'
        }

        It 'Should serialize deeply nested array with Depth limit using ToString via Pipeline and InputObject' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $arr = ,(,(,(,($ip))))
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress -Depth 2
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress -Depth 2
            $jsonPipeline | Should -BeExactly '[[["192.168.1.1"]]]'
            $jsonInputObject | Should -BeExactly '[[["192.168.1.1"]]]'
        }

        It 'Should serialize deeply nested array with sufficient Depth as full object via Pipeline and InputObject' {
            $ip = [System.Net.IPAddress]::Parse('192.168.1.1')
            $arr = ,(,(,(,($ip))))
            $expected = '[[[[{"AddressFamily":2,"ScopeId":null,"IsIPv6Multicast":false,"IsIPv6LinkLocal":false,"IsIPv6SiteLocal":false,"IsIPv6Teredo":false,"IsIPv6UniqueLocal":false,"IsIPv4MappedToIPv6":false,"Address":16885952}]]]]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress -Depth 10
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress -Depth 10
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Array with mixed content types' {
        It 'Should serialize array with mixed scalars correctly via Pipeline and InputObject' {
            $arr = @(1, 'two', 3.14, $true, $null)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[1,"two",3.14,true,null]'
            $jsonInputObject | Should -BeExactly '[1,"two",3.14,true,null]'
        }

        It 'Should serialize array with nested array and scalars correctly via Pipeline and InputObject' {
            $arr = @(1, @(2, 3), 4)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[1,[2,3],4]'
            $jsonInputObject | Should -BeExactly '[1,[2,3],4]'
        }

        It 'Should serialize array with PSCustomObject elements correctly via Pipeline and InputObject' {
            $arr = @([PSCustomObject]@{x = 1}, [PSCustomObject]@{y = 2})
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[{"x":1},{"y":2}]'
            $jsonInputObject | Should -BeExactly '[{"x":1},{"y":2}]'
        }

        It 'Should serialize array with DateTime elements correctly via Pipeline and InputObject' {
            $date1 = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc)
            $date2 = [DateTime]::new(2024, 12, 25, 0, 0, 0, [DateTimeKind]::Utc)
            $arr = @($date1, $date2)
            $expected = '["2024-06-15T10:30:00Z","2024-12-25T00:00:00Z"]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize array with Guid elements correctly via Pipeline and InputObject' {
            $guid1 = [Guid]'12345678-1234-1234-1234-123456789abc'
            $guid2 = [Guid]'87654321-4321-4321-4321-cba987654321'
            $arr = @($guid1, $guid2)
            $expected = '["12345678-1234-1234-1234-123456789abc","87654321-4321-4321-4321-cba987654321"]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize array with enum elements correctly via Pipeline and InputObject' {
            $arr = @([DayOfWeek]::Monday, [DayOfWeek]::Friday)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '[1,5]'
            $jsonInputObject | Should -BeExactly '[1,5]'
        }

        It 'Should serialize array with enum as string correctly via Pipeline and InputObject' {
            $arr = @([DayOfWeek]::Monday, [DayOfWeek]::Friday)
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress -EnumsAsStrings
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress -EnumsAsStrings
            $jsonPipeline | Should -BeExactly '["Monday","Friday"]'
            $jsonInputObject | Should -BeExactly '["Monday","Friday"]'
        }
    }

    Context 'Array ETS properties' {
        It 'Should include ETS properties on array via Pipeline and InputObject' {
            $arr = @(1, 2, 3)
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name ArrayName -Value 'MyArray' -PassThru
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '{"value":[1,2,3],"ArrayName":"MyArray"}'
            $jsonInputObject | Should -BeExactly '{"value":[1,2,3],"ArrayName":"MyArray"}'
        }

        It 'Should include multiple ETS properties on array via Pipeline and InputObject' {
            $arr = @('a', 'b')
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name Prop1 -Value 'val1' -PassThru
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name Prop2 -Value 'val2' -PassThru
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly '{"value":["a","b"],"Prop1":"val1","Prop2":"val2"}'
            $jsonInputObject | Should -BeExactly '{"value":["a","b"],"Prop1":"val1","Prop2":"val2"}'
        }
    }

    Context 'Hashtable basic serialization' {
        It 'Should serialize empty hashtable correctly via Pipeline and InputObject' {
            $hash = @{}
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{}'
            $jsonInputObject | Should -BeExactly '{}'
        }

        It 'Should serialize single key hashtable correctly via Pipeline and InputObject' {
            $hash = @{ key = 'value' }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"key":"value"}'
            $jsonInputObject | Should -BeExactly '{"key":"value"}'
        }

        It 'Should serialize hashtable with null value correctly via Pipeline and InputObject' {
            $hash = @{ nullKey = $null }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"nullKey":null}'
            $jsonInputObject | Should -BeExactly '{"nullKey":null}'
        }

        It 'Should serialize hashtable with various scalar types correctly via Pipeline and InputObject' {
            $hash = [ordered]@{
                intKey = 42
                strKey = 'hello'
                boolKey = $true
                doubleKey = 3.14
            }
            $expected = '{"intKey":42,"strKey":"hello","boolKey":true,"doubleKey":3.14}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'OrderedDictionary serialization' {
        It 'Should preserve order in OrderedDictionary via Pipeline and InputObject' {
            $ordered = [ordered]@{
                z = 1
                a = 2
                m = 3
            }
            $expected = '{"z":1,"a":2,"m":3}'
            $jsonPipeline = $ordered | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $ordered -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize large OrderedDictionary preserving order via Pipeline and InputObject' {
            $ordered = [ordered]@{}
            1..5 | ForEach-Object { $ordered["key$_"] = $_ }
            $expected = '{"key1":1,"key2":2,"key3":3,"key4":4,"key5":5}'
            $jsonPipeline = $ordered | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $ordered -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Nested dictionaries' {
        It 'Should serialize nested hashtable correctly via Pipeline and InputObject' {
            $hash = @{
                outer = @{
                    inner = 'value'
                }
            }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"outer":{"inner":"value"}}'
            $jsonInputObject | Should -BeExactly '{"outer":{"inner":"value"}}'
        }

        It 'Should serialize deeply nested hashtable correctly via Pipeline and InputObject' {
            $hash = @{
                level1 = @{
                    level2 = @{
                        level3 = 'deep'
                    }
                }
            }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"level1":{"level2":{"level3":"deep"}}}'
            $jsonInputObject | Should -BeExactly '{"level1":{"level2":{"level3":"deep"}}}'
        }

        It 'Should serialize nested hashtable with Depth limit via Pipeline and InputObject' {
            $hash = @{
                level1 = @{
                    level2 = @{
                        level3 = 'deep'
                    }
                }
            }
            $jsonPipeline = $hash | ConvertTo-Json -Compress -Depth 1
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress -Depth 1
            $jsonPipeline | Should -BeExactly '{"level1":{"level2":"System.Collections.Hashtable"}}'
            $jsonInputObject | Should -BeExactly '{"level1":{"level2":"System.Collections.Hashtable"}}'
        }

        It 'Should serialize hashtable with array value correctly via Pipeline and InputObject' {
            $hash = @{ arr = @(1, 2, 3) }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"arr":[1,2,3]}'
            $jsonInputObject | Should -BeExactly '{"arr":[1,2,3]}'
        }

        It 'Should serialize hashtable with nested array of hashtables correctly via Pipeline and InputObject' {
            $hash = @{
                items = @(
                    @{ id = 1 },
                    @{ id = 2 }
                )
            }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"items":[{"id":1},{"id":2}]}'
            $jsonInputObject | Should -BeExactly '{"items":[{"id":1},{"id":2}]}'
        }
    }

    Context 'Dictionary key types' {
        It 'Should serialize hashtable with string keys correctly via Pipeline and InputObject' {
            $hash = @{ 'string-key' = 'value' }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"string-key":"value"}'
            $jsonInputObject | Should -BeExactly '{"string-key":"value"}'
        }

        It 'Should serialize hashtable with special character keys correctly via Pipeline and InputObject' {
            $hash = [ordered]@{
                'key with space' = 1
                'key-with-dash' = 2
                'key_with_underscore' = 3
            }
            $expected = '{"key with space":1,"key-with-dash":2,"key_with_underscore":3}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize hashtable with unicode keys correctly via Pipeline and InputObject' {
            $hash = @{ "`u{65E5}`u{672C}`u{8A9E}" = 'Japanese' }
            $expected = "{`"`u{65E5}`u{672C}`u{8A9E}`":`"Japanese`"}"
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize hashtable with empty string key correctly via Pipeline and InputObject' {
            $hash = @{ '' = 'empty key' }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"":"empty key"}'
            $jsonInputObject | Should -BeExactly '{"":"empty key"}'
        }
    }

    Context 'Dictionary with complex values' {
        It 'Should serialize hashtable with DateTime value correctly via Pipeline and InputObject' {
            $hash = @{ date = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc) }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"date":"2024-06-15T10:30:00Z"}'
            $jsonInputObject | Should -BeExactly '{"date":"2024-06-15T10:30:00Z"}'
        }

        It 'Should serialize hashtable with Guid value correctly via Pipeline and InputObject' {
            $hash = @{ guid = [Guid]'12345678-1234-1234-1234-123456789abc' }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"guid":"12345678-1234-1234-1234-123456789abc"}'
            $jsonInputObject | Should -BeExactly '{"guid":"12345678-1234-1234-1234-123456789abc"}'
        }

        It 'Should serialize hashtable with enum value correctly via Pipeline and InputObject' {
            $hash = @{ day = [DayOfWeek]::Monday }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"day":1}'
            $jsonInputObject | Should -BeExactly '{"day":1}'
        }

        It 'Should serialize hashtable with enum as string correctly via Pipeline and InputObject' {
            $hash = @{ day = [DayOfWeek]::Monday }
            $jsonPipeline = $hash | ConvertTo-Json -Compress -EnumsAsStrings
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress -EnumsAsStrings
            $jsonPipeline | Should -BeExactly '{"day":"Monday"}'
            $jsonInputObject | Should -BeExactly '{"day":"Monday"}'
        }

        It 'Should serialize hashtable with PSCustomObject value correctly via Pipeline and InputObject' {
            $hash = @{ obj = [PSCustomObject]@{ prop = 'value' } }
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"obj":{"prop":"value"}}'
            $jsonInputObject | Should -BeExactly '{"obj":{"prop":"value"}}'
        }
    }

    Context 'Dictionary ETS properties' {
        It 'Should include ETS properties on hashtable via Pipeline and InputObject' {
            $hash = @{ a = 1 }
            $hash = Add-Member -InputObject $hash -MemberType NoteProperty -Name ETSProp -Value 'ets' -PassThru
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
            $jsonInputObject | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
        }

        It 'Should include ETS properties on OrderedDictionary via Pipeline and InputObject' {
            $ordered = [ordered]@{ a = 1 }
            $ordered = Add-Member -InputObject $ordered -MemberType NoteProperty -Name ETSProp -Value 'ets' -PassThru
            $jsonPipeline = $ordered | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $ordered -Compress
            $jsonPipeline | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
            $jsonInputObject | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
        }
    }

    Context 'Generic Dictionary types' {
        It 'Should serialize Generic Dictionary correctly via Pipeline and InputObject' {
            $dict = [System.Collections.Generic.Dictionary[string,int]]::new()
            $dict['one'] = 1
            $dict['two'] = 2
            $jsonPipeline = $dict | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dict -Compress
            $jsonPipeline | Should -Match '"one":1'
            $jsonPipeline | Should -Match '"two":2'
            $jsonInputObject | Should -Match '"one":1'
            $jsonInputObject | Should -Match '"two":2'
        }

        It 'Should serialize SortedDictionary correctly via Pipeline and InputObject' {
            $dict = [System.Collections.Generic.SortedDictionary[string,int]]::new()
            $dict['b'] = 2
            $dict['a'] = 1
            $dict['c'] = 3
            $jsonPipeline = $dict | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $dict -Compress
            $jsonPipeline | Should -BeExactly '{"a":1,"b":2,"c":3}'
            $jsonInputObject | Should -BeExactly '{"a":1,"b":2,"c":3}'
        }
    }

    #endregion Comprehensive Array and Dictionary Tests (Phase 2)

    #region Comprehensive Depth Truncation and Multilevel Composition Tests (Phase 4)
    # Test coverage for ConvertTo-Json depth truncation and complex nested structures
    # Covers: -Depth parameter behavior, multilevel type compositions

    Context 'Depth parameter basic behavior' {
        It 'Should use default depth of 2 via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = [PSCustomObject]@{
                    L1 = [PSCustomObject]@{
                        L2 = [PSCustomObject]@{
                            L3 = 'deep'
                        }
                    }
                }
            }
            $expected = '{"L0":{"L1":{"L2":"@{L3=deep}"}}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should truncate at Depth 0 via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = [PSCustomObject]@{ L1 = 1 }
            }
            $expected = '{"L0":"@{L1=1}"}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 0
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 0
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should truncate at Depth 1 via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = [PSCustomObject]@{
                    L1 = [PSCustomObject]@{
                        L2 = 'deep'
                    }
                }
            }
            $expected = '{"L0":{"L1":"@{L2=deep}"}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 1
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 1
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize fully with sufficient Depth via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = [PSCustomObject]@{
                    L1 = [PSCustomObject]@{
                        L2 = [PSCustomObject]@{
                            L3 = 'very deep'
                        }
                    }
                }
            }
            $expected = '{"L0":{"L1":{"L2":{"L3":"very deep"}}}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 10
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 10
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should handle Depth 100 for deeply nested structures via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{ L0 = [PSCustomObject]@{ L1 = [PSCustomObject]@{ L2 = [PSCustomObject]@{ L3 = [PSCustomObject]@{ L4 = 'deep' } } } } }
            $expected = '{"L0":{"L1":{"L2":{"L3":{"L4":"deep"}}}}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 100
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 100
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should throw on Depth 101 exceeding maximum via Pipeline and InputObject' {
            { [PSCustomObject]@{ L0 = 1 } | ConvertTo-Json -Depth 101 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.ConvertToJsonCommand'
            { ConvertTo-Json -InputObject ([PSCustomObject]@{ L0 = 1 }) -Depth 101 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.ConvertToJsonCommand'
        }
    }

    Context 'Depth truncation with arrays' {
        It 'Should truncate nested array at Depth limit via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Arr = ,(,(1, 2, 3))
            }
            $expected = '{"Arr":["System.Object[]"]}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 1
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 1
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize nested array fully with sufficient Depth via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Arr = ,(,(1, 2, 3))
            }
            $expected = '{"Arr":[[[1,2,3]]]}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 10
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 10
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should truncate array of objects at Depth limit via Pipeline and InputObject' {
            $arr = @(
                [PSCustomObject]@{ Inner = [PSCustomObject]@{ Value = 1 } }
            )
            $expected = '[{"Inner":"@{Value=1}"}]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress -Depth 1
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress -Depth 1
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Depth truncation with hashtables' {
        It 'Should truncate nested hashtable at Depth limit via Pipeline and InputObject' {
            $hash = @{
                L0 = @{
                    L1 = @{
                        L2 = 'deep'
                    }
                }
            }
            $expected = '{"L0":{"L1":"System.Collections.Hashtable"}}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress -Depth 1
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress -Depth 1
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize nested hashtable fully with sufficient Depth via Pipeline and InputObject' {
            $hash = @{
                L0 = @{
                    L1 = @{
                        L2 = 'deep'
                    }
                }
            }
            $expected = '{"L0":{"L1":{"L2":"deep"}}}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress -Depth 10
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress -Depth 10
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Depth truncation string representation' {
        It 'Should convert PSCustomObject to @{...} string when truncated via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Child = [PSCustomObject]@{ A = 1; B = 2 }
            }
            $expected = '{"Child":"@{A=1; B=2}"}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 0
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 0
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should convert Hashtable to type name when truncated via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Child = @{ Key = 'Value' }
            }
            $expected = '{"Child":"System.Collections.Hashtable"}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 0
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 0
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should convert Array to space-separated string when truncated via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Child = @(1, 2, 3)
            }
            $expected = '{"Child":"1 2 3"}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 0
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 0
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Multilevel composition: Array containing Dictionary' {
        It 'Should serialize array of hashtables correctly via Pipeline and InputObject' {
            $arr = @(@{ a = 1 }, @{ b = 2 }, @{ c = 3 })
            $expected = '[{"a":1},{"b":2},{"c":3}]'
            $jsonPipeline = $arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize array of ordered dictionaries correctly via Pipeline and InputObject' {
            $arr = @(
                [ordered]@{ x = 1; y = 2 },
                [ordered]@{ x = 3; y = 4 }
            )
            $expected = '[{"x":1,"y":2},{"x":3,"y":4}]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize nested array of hashtables correctly via Pipeline and InputObject' {
            $arr = @(
                @{
                    Items = @(
                        @{ Value = 1 },
                        @{ Value = 2 }
                    )
                }
            )
            $expected = '[{"Items":[{"Value":1},{"Value":2}]}]'
            $jsonPipeline = ,$arr | ConvertTo-Json -Compress -Depth 3
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress -Depth 3
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Multilevel composition: Dictionary containing Array' {
        It 'Should serialize dictionary with array values correctly via Pipeline and InputObject' {
            $hash = [ordered]@{
                numbers = @(1, 2, 3)
                strings = @('a', 'b', 'c')
            }
            $expected = '{"numbers":[1,2,3],"strings":["a","b","c"]}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize dictionary with nested array values correctly via Pipeline and InputObject' {
            $hash = @{
                matrix = @(@(1, 2), @(3, 4))
            }
            $expected = '{"matrix":[[1,2],[3,4]]}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize dictionary with empty array value correctly via Pipeline and InputObject' {
            $hash = @{ empty = @() }
            $expected = '{"empty":[]}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize dictionary with array of dictionaries correctly via Pipeline and InputObject' {
            $hash = @{
                Items = @(
                    @{ X = 1 },
                    @{ X = 2 }
                )
            }
            $expected = '{"Items":[{"X":1},{"X":2}]}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Multilevel composition: PSCustomObject with mixed types' {
        It 'Should serialize PSCustomObject with array and hashtable properties via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                List = @(1, 2, 3)
                Config = @{ Key = 'Value' }
                Name = 'Test'
            }
            $expected = '{"List":[1,2,3],"Config":{"Key":"Value"},"Name":"Test"}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize PSCustomObject with nested PSCustomObject and array via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Child = [PSCustomObject]@{
                    Items = @(1, 2, 3)
                }
            }
            $expected = '{"Child":{"Items":[1,2,3]}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize array of PSCustomObject with mixed properties via Pipeline and InputObject' {
            $arr = @(
                [PSCustomObject]@{ Type = 'A'; Data = @(1, 2) },
                [PSCustomObject]@{ Type = 'B'; Data = @{ Key = 'Val' } }
            )
            $expected = '[{"Type":"A","Data":[1,2]},{"Type":"B","Data":{"Key":"Val"}}]'
            $jsonPipeline = $arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Multilevel composition: PowerShell class in complex structures' {
        BeforeAll {
            class ItemClass {
                [int]$Id
                [string]$Name
            }

            class ContainerClass {
                [string]$Type
                [ItemClass]$Item
            }
        }

        It 'Should serialize array of PowerShell class correctly via Pipeline and InputObject' {
            $arr = @(
                [ItemClass]@{ Id = 1; Name = 'First' },
                [ItemClass]@{ Id = 2; Name = 'Second' }
            )
            $expected = '[{"Id":1,"Name":"First"},{"Id":2,"Name":"Second"}]'
            $jsonPipeline = $arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize hashtable containing PowerShell class correctly via Pipeline and InputObject' {
            $item = [ItemClass]@{ Id = 1; Name = 'Test' }
            $hash = @{ Item = $item }
            $expected = '{"Item":{"Id":1,"Name":"Test"}}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize nested PowerShell classes correctly via Pipeline and InputObject' {
            $item = [ItemClass]@{ Id = 1; Name = 'Inner' }
            $container = [ContainerClass]@{ Type = 'Outer'; Item = $item }
            $expected = '{"Type":"Outer","Item":{"Id":1,"Name":"Inner"}}'
            $jsonPipeline = $container | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $container -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize PSCustomObject containing PowerShell class correctly via Pipeline and InputObject' {
            $item = [ItemClass]@{ Id = 1; Name = 'Test' }
            $obj = [PSCustomObject]@{
                Label = 'Container'
                Content = $item
            }
            $expected = '{"Label":"Container","Content":{"Id":1,"Name":"Test"}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should truncate nested PowerShell class at Depth limit via Pipeline and InputObject' {
            $item = [ItemClass]@{ Id = 1; Name = 'Test' }
            $container = [ContainerClass]@{ Type = 'Outer'; Item = $item }
            $itemString = $item.ToString()
            $expected = "{`"Type`":`"Outer`",`"Item`":`"$itemString`"}"
            $jsonPipeline = $container | ConvertTo-Json -Compress -Depth 0
            $jsonInputObject = ConvertTo-Json -InputObject $container -Compress -Depth 0
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'Complex multilevel compositions' {
        It 'Should serialize 3-level mixed composition correctly via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                Users = @(
                    [PSCustomObject]@{
                        Name = 'Alice'
                        Roles = @('Admin', 'User')
                    },
                    [PSCustomObject]@{
                        Name = 'Bob'
                        Roles = @('User')
                    }
                )
            }
            $expected = '{"Users":[{"Name":"Alice","Roles":["Admin","User"]},{"Name":"Bob","Roles":["User"]}]}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 3
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 3
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize dictionary with nested mixed types correctly via Pipeline and InputObject' {
            $hash = [ordered]@{
                Meta = [PSCustomObject]@{ Version = '1.0' }
                Data = @(
                    ([ordered]@{ Key = 'A'; Values = @(1, 2) }),
                    ([ordered]@{ Key = 'B'; Values = @(3, 4) })
                )
            }
            $expected = '{"Meta":{"Version":"1.0"},"Data":[{"Key":"A","Values":[1,2]},{"Key":"B","Values":[3,4]}]}'
            $jsonPipeline = $hash | ConvertTo-Json -Compress -Depth 3
            $jsonInputObject = ConvertTo-Json -InputObject $hash -Compress -Depth 3
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should handle deeply nested mixed types with sufficient Depth via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = @{
                    L1 = [PSCustomObject]@{
                        L2 = @(
                            [PSCustomObject]@{ L3 = 'deep' }
                        )
                    }
                }
            }
            $expected = '{"L0":{"L1":{"L2":[{"L3":"deep"}]}}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 10
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 10
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should truncate deeply nested mixed types at Depth limit via Pipeline and InputObject' {
            $obj = [PSCustomObject]@{
                L0 = @{
                    L1 = [PSCustomObject]@{
                        L2 = @(
                            [PSCustomObject]@{ L3 = 'deep' }
                        )
                    }
                }
            }
            $expected = '{"L0":{"L1":{"L2":""}}}'
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 2
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 2
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    #endregion Comprehensive Depth Truncation and Multilevel Composition Tests (Phase 4)

    #region Comprehensive PowerShell Class Tests (Phase 5)
    # Test coverage for ConvertTo-Json PowerShell class serialization
    # Covers: Pipeline vs InputObject, ETS vs no ETS, nested structures, inheritance

    Context 'PowerShell class serialization' {
        BeforeAll {
            class SimpleClass {
                [string]$StringVal
                [int]$IntVal
                [bool]$BoolVal
                [double]$DoubleVal
                [bigint]$BigIntVal
                [guid]$GuidVal
                [ipaddress]$IPVal
                [object[]]$ArrayVal
                [hashtable]$DictVal
                hidden [string]$HiddenVal
            }
        }

        It 'Should serialize PowerShell class with various property types including ETS via Pipeline and InputObject' {
            $obj = [SimpleClass]::new()
            $obj.StringVal = 'hello'
            $obj.IntVal = 42
            $obj.BoolVal = $true
            $obj.DoubleVal = 3.14
            $obj.BigIntVal = [bigint]::Parse('99999999999999999999')
            $obj.GuidVal = [guid]'12345678-1234-1234-1234-123456789abc'
            $obj.IPVal = [ipaddress]::Parse('192.168.1.1')
            $obj.ArrayVal = @(1, 'two', $true)
            $obj.DictVal = @{ Key = 'Value'; Nested = @{ Inner = 1 } }
            $obj.HiddenVal = 'secret'
            $obj | Add-Member -MemberType NoteProperty -Name ETSNote -Value 'note'
            $obj | Add-Member -MemberType ScriptProperty -Name ETSScript -Value { $this.StringVal.Length }
            $jsonPipeline = $obj | ConvertTo-Json -Compress -Depth 3
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress -Depth 3
            $jsonPipeline | Should -Match '"StringVal":"hello"'
            $jsonPipeline | Should -Match '"IntVal":42'
            $jsonPipeline | Should -Match '"BoolVal":true'
            $jsonPipeline | Should -Match '"DoubleVal":3\.14'
            $jsonPipeline | Should -Match '"BigIntVal":99999999999999999999'
            $jsonPipeline | Should -Match '"GuidVal":"12345678-1234-1234-1234-123456789abc"'
            $jsonPipeline | Should -Match '"IPVal":\{'
            $jsonPipeline | Should -Match '"ArrayVal":\[1,"two",true\]'
            $jsonPipeline | Should -Match '"Key":"Value"'
            $jsonPipeline | Should -Match '"Inner":1'
            $jsonPipeline | Should -Match '"HiddenVal":"secret"'
            $jsonPipeline | Should -Match '"ETSNote":"note"'
            $jsonPipeline | Should -Match '"ETSScript":5'
            $jsonInputObject | Should -Match '"StringVal":"hello"'
            $jsonInputObject | Should -Match '"IntVal":42'
            $jsonInputObject | Should -Match '"BoolVal":true'
            $jsonInputObject | Should -Match '"DoubleVal":3\.14'
            $jsonInputObject | Should -Match '"BigIntVal":99999999999999999999'
            $jsonInputObject | Should -Match '"GuidVal":"12345678-1234-1234-1234-123456789abc"'
            $jsonInputObject | Should -Match '"IPVal":\{'
            $jsonInputObject | Should -Match '"ArrayVal":\[1,"two",true\]'
            $jsonInputObject | Should -Match '"Key":"Value"'
            $jsonInputObject | Should -Match '"Inner":1'
            $jsonInputObject | Should -Match '"HiddenVal":"secret"'
            $jsonInputObject | Should -Not -Match 'ETSNote'
            $jsonInputObject | Should -Not -Match 'ETSScript'
        }

        It 'Should serialize PowerShell class with default values via Pipeline and InputObject' {
            $obj = [SimpleClass]::new()
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -Match '"StringVal":null'
            $jsonPipeline | Should -Match '"IntVal":0'
            $jsonPipeline | Should -Match '"BoolVal":false'
            $jsonPipeline | Should -Match '"DoubleVal":0\.0'
            $jsonPipeline | Should -Match '"BigIntVal":0'
            $jsonPipeline | Should -Match '"GuidVal":"00000000-0000-0000-0000-000000000000"'
            $jsonPipeline | Should -Match '"IPVal":null'
            $jsonPipeline | Should -Match '"ArrayVal":null'
            $jsonPipeline | Should -Match '"DictVal":null'
            $jsonPipeline | Should -Match '"HiddenVal":null'
            $jsonInputObject | Should -BeExactly $jsonPipeline
        }
    }

    Context 'Nested PowerShell class' {
        BeforeAll {
            class InnerClass {
                [string]$Inner
            }

            class OuterClass {
                [string]$Outer
                [InnerClass]$Child
            }

            class DeepClass {
                [string]$Name
                [OuterClass]$Nested
            }
        }

        It 'Should serialize deeply nested PowerShell class via Pipeline and InputObject' {
            $inner = [InnerClass]@{ Inner = 'deep' }
            $outer = [OuterClass]@{ Outer = 'middle'; Child = $inner }
            $deep = [DeepClass]@{ Name = 'top'; Nested = $outer }
            $expected = '{"Name":"top","Nested":{"Outer":"middle","Child":{"Inner":"deep"}}}'
            $jsonPipeline = $deep | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $deep -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }

        It 'Should serialize nested PowerShell class with null child via Pipeline and InputObject' {
            $outer = [OuterClass]@{ Outer = 'outer value'; Child = $null }
            $expected = '{"Outer":"outer value","Child":null}'
            $jsonPipeline = $outer | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $outer -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    Context 'PowerShell class inheritance' {
        BeforeAll {
            class BaseClass {
                [string]$BaseProp
            }

            class ChildClass : BaseClass {
                [string]$ChildProp
            }

            class GrandChildClass : ChildClass {
                [string]$GrandChildProp
            }
        }

        It 'Should serialize derived class with base properties via Pipeline and InputObject' {
            $obj = [ChildClass]@{ BaseProp = 'base'; ChildProp = 'child' }
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -Match '"BaseProp":"base"'
            $jsonPipeline | Should -Match '"ChildProp":"child"'
            $jsonInputObject | Should -Match '"BaseProp":"base"'
            $jsonInputObject | Should -Match '"ChildProp":"child"'
        }

        It 'Should serialize multi-level inherited class via Pipeline and InputObject' {
            $obj = [GrandChildClass]@{
                BaseProp = 'base'
                ChildProp = 'child'
                GrandChildProp = 'grandchild'
            }
            $jsonPipeline = $obj | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $obj -Compress
            $jsonPipeline | Should -Match '"BaseProp":"base"'
            $jsonPipeline | Should -Match '"ChildProp":"child"'
            $jsonPipeline | Should -Match '"GrandChildProp":"grandchild"'
            $jsonInputObject | Should -Match '"BaseProp":"base"'
            $jsonInputObject | Should -Match '"ChildProp":"child"'
            $jsonInputObject | Should -Match '"GrandChildProp":"grandchild"'
        }

    }

    Context 'Mixed PSCustomObject and PowerShell class' {
        BeforeAll {
            class MixedClass {
                [string]$ClassName
            }
        }

        It 'Should serialize array with mixed types via Pipeline and InputObject' {
            $classObj = [MixedClass]@{ ClassName = 'class' }
            $customObj = [PSCustomObject]@{ CustomName = 'custom' }
            $arr = @($classObj, $customObj)
            $expected = '[{"ClassName":"class"},{"CustomName":"custom"}]'
            $jsonPipeline = $arr | ConvertTo-Json -Compress
            $jsonInputObject = ConvertTo-Json -InputObject $arr -Compress
            $jsonPipeline | Should -BeExactly $expected
            $jsonInputObject | Should -BeExactly $expected
        }
    }

    #endregion Comprehensive PowerShell Class Tests (Phase 5)

}
