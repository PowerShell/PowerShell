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

        It 'Should serialize array with single null element correctly' {
            $arr = @($null)
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[null]'
        }

        It 'Should serialize array with multiple null elements correctly' {
            $arr = @($null, $null, $null)
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[null,null,null]'
        }
    }

    Context 'Nested arrays' {
        It 'Should serialize 2D array correctly via InputObject' {
            $arr = @(@(1, 2), @(3, 4))
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[[1,2],[3,4]]'
        }

        It 'Should serialize 3D array correctly via InputObject' {
            $arr = @(@(@(1, 2), @(3, 4)), @(@(5, 6), @(7, 8)))
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[[[1,2],[3,4]],[[5,6],[7,8]]]'
        }

        It 'Should serialize jagged array correctly via InputObject' {
            $arr = @(@(1), @(2, 3), @(4, 5, 6))
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[[1],[2,3],[4,5,6]]'
        }

        It 'Should serialize array containing empty arrays correctly' {
            $arr = @(@(), @(1), @())
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[[],[1],[]]'
        }

        It 'Should serialize deeply nested array with Depth limit' {
            $arr = ,(,(,(,(1))))
            $json = ConvertTo-Json -InputObject $arr -Compress -Depth 2
            $json | Should -BeExactly '[[["1"]]]'
        }

        It 'Should serialize deeply nested array with sufficient Depth' {
            $arr = ,(,(,(,(1))))
            $json = ConvertTo-Json -InputObject $arr -Compress -Depth 10
            $json | Should -BeExactly '[[[[1]]]]'
        }
    }

    Context 'Array with mixed content types' {
        It 'Should serialize array with mixed scalars correctly' {
            $arr = @(1, 'two', 3.14, $true, $null)
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[1,"two",3.14,true,null]'
        }

        It 'Should serialize array with nested array and scalars correctly' {
            $arr = @(1, @(2, 3), 4)
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[1,[2,3],4]'
        }

        It 'Should serialize array with hashtable elements correctly' {
            $arr = @(@{a = 1}, @{b = 2})
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[{"a":1},{"b":2}]'
        }

        It 'Should serialize array with PSCustomObject elements correctly' {
            $arr = @([PSCustomObject]@{x = 1}, [PSCustomObject]@{y = 2})
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[{"x":1},{"y":2}]'
        }
    }

    Context 'Array ETS properties' {
        It 'Should include ETS properties on array via InputObject' {
            $arr = @(1, 2, 3)
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name ArrayName -Value 'MyArray' -PassThru
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '{"value":[1,2,3],"ArrayName":"MyArray"}'
        }

        It 'Should include multiple ETS properties on array via InputObject' {
            $arr = @('a', 'b')
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name Prop1 -Value 'val1' -PassThru
            $arr = Add-Member -InputObject $arr -MemberType NoteProperty -Name Prop2 -Value 'val2' -PassThru
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '{"value":["a","b"],"Prop1":"val1","Prop2":"val2"}'
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

        It 'Should serialize hashtable with null value correctly' {
            $hash = @{ nullKey = $null }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"nullKey":null}'
        }

        It 'Should serialize hashtable with various scalar types correctly' {
            $hash = [ordered]@{
                intKey = 42
                strKey = 'hello'
                boolKey = $true
                doubleKey = 3.14
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"intKey":42,"strKey":"hello","boolKey":true,"doubleKey":3.14}'
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

        It 'Should serialize large OrderedDictionary preserving order' {
            $ordered = [ordered]@{}
            1..5 | ForEach-Object { $ordered["key$_"] = $_ }
            $json = $ordered | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"key1":1,"key2":2,"key3":3,"key4":4,"key5":5}'
        }
    }

    Context 'Nested dictionaries' {
        It 'Should serialize nested hashtable correctly' {
            $hash = @{
                outer = @{
                    inner = 'value'
                }
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"outer":{"inner":"value"}}'
        }

        It 'Should serialize deeply nested hashtable correctly' {
            $hash = @{
                level1 = @{
                    level2 = @{
                        level3 = 'deep'
                    }
                }
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"level1":{"level2":{"level3":"deep"}}}'
        }

        It 'Should serialize nested hashtable with Depth limit' {
            $hash = @{
                level1 = @{
                    level2 = @{
                        level3 = 'deep'
                    }
                }
            }
            $json = $hash | ConvertTo-Json -Compress -Depth 1
            $json | Should -BeExactly '{"level1":{"level2":"System.Collections.Hashtable"}}'
        }

        It 'Should serialize hashtable with array value correctly' {
            $hash = @{ arr = @(1, 2, 3) }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"arr":[1,2,3]}'
        }

        It 'Should serialize hashtable with nested array of hashtables correctly' {
            $hash = @{
                items = @(
                    @{ id = 1 },
                    @{ id = 2 }
                )
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"items":[{"id":1},{"id":2}]}'
        }
    }

    Context 'Dictionary key types' {
        It 'Should serialize hashtable with string keys correctly' {
            $hash = @{ 'string-key' = 'value' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"string-key":"value"}'
        }

        It 'Should serialize hashtable with special character keys correctly' {
            $hash = [ordered]@{
                'key with space' = 1
                'key-with-dash' = 2
                'key_with_underscore' = 3
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"key with space":1,"key-with-dash":2,"key_with_underscore":3}'
        }

        It 'Should serialize hashtable with unicode keys correctly' {
            $hash = @{ '日本語' = 'Japanese' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"日本語":"Japanese"}'
        }

        It 'Should serialize hashtable with empty string key correctly' {
            $hash = @{ '' = 'empty key' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"":"empty key"}'
        }
    }

    Context 'Dictionary with complex values' {
        It 'Should serialize hashtable with DateTime value correctly' {
            $hash = @{ date = [DateTime]::new(2024, 6, 15, 10, 30, 0, [DateTimeKind]::Utc) }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"date":"2024-06-15T10:30:00Z"}'
        }

        It 'Should serialize hashtable with Guid value correctly' {
            $hash = @{ guid = [Guid]'12345678-1234-1234-1234-123456789abc' }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"guid":"12345678-1234-1234-1234-123456789abc"}'
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

        It 'Should serialize hashtable with PSCustomObject value correctly' {
            $hash = @{ obj = [PSCustomObject]@{ prop = 'value' } }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"obj":{"prop":"value"}}'
        }
    }

    Context 'Dictionary ETS properties' {
        It 'Should include ETS properties on hashtable via InputObject' {
            $hash = @{ a = 1 }
            $hash = Add-Member -InputObject $hash -MemberType NoteProperty -Name ETSProp -Value 'ets' -PassThru
            $json = ConvertTo-Json -InputObject $hash -Compress
            $json | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
        }

        It 'Should include ETS properties on OrderedDictionary via InputObject' {
            $ordered = [ordered]@{ a = 1 }
            $ordered = Add-Member -InputObject $ordered -MemberType NoteProperty -Name ETSProp -Value 'ets' -PassThru
            $json = ConvertTo-Json -InputObject $ordered -Compress
            $json | Should -BeExactly '{"a":1,"ETSProp":"ets"}'
        }
    }

    Context 'Generic Dictionary types' {
        It 'Should serialize Generic Dictionary correctly' {
            $dict = [System.Collections.Generic.Dictionary[string,int]]::new()
            $dict['one'] = 1
            $dict['two'] = 2
            $json = $dict | ConvertTo-Json -Compress
            $json | Should -Match '"one":1'
            $json | Should -Match '"two":2'
        }

        It 'Should serialize SortedDictionary correctly' {
            $dict = [System.Collections.Generic.SortedDictionary[string,int]]::new()
            $dict['b'] = 2
            $dict['a'] = 1
            $dict['c'] = 3
            $json = $dict | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"a":1,"b":2,"c":3}'
        }
    }

    Context 'Array of dictionaries' {
        It 'Should serialize array of hashtables correctly via Pipeline' {
            $arr = @{ a = 1 }, @{ b = 2 }, @{ c = 3 }
            $json = $arr | ConvertTo-Json -Compress
            $json | Should -BeExactly '[{"a":1},{"b":2},{"c":3}]'
        }

        It 'Should serialize array of ordered dictionaries correctly via InputObject' {
            $arr = @(
                [ordered]@{ x = 1; y = 2 },
                [ordered]@{ x = 3; y = 4 }
            )
            $json = ConvertTo-Json -InputObject $arr -Compress
            $json | Should -BeExactly '[{"x":1,"y":2},{"x":3,"y":4}]'
        }
    }

    Context 'Dictionary with array values' {
        It 'Should serialize dictionary with array values correctly' {
            $hash = [ordered]@{
                numbers = @(1, 2, 3)
                strings = @('a', 'b', 'c')
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"numbers":[1,2,3],"strings":["a","b","c"]}'
        }

        It 'Should serialize dictionary with nested array values correctly' {
            $hash = @{
                matrix = @(@(1, 2), @(3, 4))
            }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"matrix":[[1,2],[3,4]]}'
        }

        It 'Should serialize dictionary with empty array value correctly' {
            $hash = @{ empty = @() }
            $json = $hash | ConvertTo-Json -Compress
            $json | Should -BeExactly '{"empty":[]}'
        }
    }

    #endregion Comprehensive Array and Dictionary Tests (Phase 2)
}
