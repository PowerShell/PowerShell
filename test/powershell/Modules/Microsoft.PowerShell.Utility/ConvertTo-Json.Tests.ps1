# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'ConvertTo-Json' -tags "CI" {
    BeforeAll {
        $notNewConvertToJson = -not $EnabledExperimentalFeatures.Contains('Microsoft.PowerShell.Utility.NewConvertToJson')
        $newline = [System.Environment]::NewLine
    }

    It 'Newtonsoft.Json.Linq.Jproperty should be converted to Json properly' -Skip:(!$notNewConvertToJson) {
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

    It "StopProcessing should succeed" -Skip:(!$notNewConvertToJson) {
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

    It "The result string should be escaped: <Name>." -Skip:$notNewConvertToJson -TestCases @(
        @{name = "Default with HTML chars";             params = @{EscapeHandling = 'Default'};        source = "`",',\,<,>,&,+,``,`n";       expected = '"\",'',\\,<,>,&,+,`,\n"' }
        @{name = "EscapeHtml with HTML chars";          params = @{EscapeHandling = 'EscapeHtml'};     source = "`",',\,<,>,&,+,``,`n";       expected = '"\u0022,\u0027,\\,\u003C,\u003E,\u0026,\u002B,\u0060,\n"' }
        @{name = "EscapeNonAscii with HTML chars";      params = @{EscapeHandling = 'EscapeNonAscii'}; source = "`",',\,<,>,&,+,``,`n";       expected = '"\u0022,\u0027,\\,\u003C,\u003E,\u0026,\u002B,\u0060,\n"' }
        @{name = "Default with ASCII chars";            params = @{EscapeHandling = 'Default'};        source = "abcdefghijklmnopqrstuvwxyz"; expected = '"abcdefghijklmnopqrstuvwxyz"' }
        @{name = "EscapeHtml with ASCII chars";         params = @{EscapeHandling = 'EscapeHtml'};     source = "abcdefghijklmnopqrstuvwxyz"; expected = '"abcdefghijklmnopqrstuvwxyz"' }
        @{name = "EscapeNonAscii with ASCII chars";     params = @{EscapeHandling = 'EscapeNonAscii'}; source = "abcdefghijklmnopqrstuvwxyz"; expected = '"abcdefghijklmnopqrstuvwxyz"' }
        @{name = "Default with non-ASCII chars";        params = @{EscapeHandling = 'Default'};        source = "яб";                         expected = '"яб"' }
        @{name = "EscapeHtml with non-ASCII chars";     params = @{EscapeHandling = 'EscapeHtml'};     source = "яб";                         expected = '"\u044F\u0431"' }
        @{name = "EscapeNonAscii non-ASCII with chars"; params = @{EscapeHandling = 'EscapeNonAscii'}; source = "яб";                         expected = '"\u044F\u0431"' }
        @{name = "Default with empty string";           params = @{EscapeHandling = 'Default'};        source = "";                           expected = "`"`"" }
        @{name = "EscapeHtml with empty string";        params = @{EscapeHandling = 'EscapeHtml'};     source = "";                           expected = "`"`"" }
        @{name = "EscapeNonAscii with empty string";    params = @{EscapeHandling = 'EscapeNonAscii'}; source = "";                           expected = "`"`"" }
    ) {
        param ($name, $params , $source, $expected)

        $source | ConvertTo-Json @params | Should -BeExactly $expected
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

    It "Parameter works: -Depth <depth>" -Skip:$notNewConvertToJson {
        $a1 = [pscustomobject] @{ prop1=$null }
        $a2 = [pscustomobject] @{ prop2=$a1 }
        $a3 = [pscustomobject] @{ prop3=$a2 }
        $a4 = [pscustomobject] @{ prop4=$a3 }

        $a4 | ConvertTo-Json -Depth 5 -Compress | Should -BeExactly '{"prop4":{"prop3":{"prop2":{"prop1":null}}}}'
        $exc = { $a4 | ConvertTo-Json -Depth 4 -Compress } | Should -PassThru -Throw -ErrorId "System.Text.Json.JsonException,Microsoft.PowerShell.Commands.ConvertToJsonCommand"
        $exc.Exception.TargetSite.Name | Should -BeExactly "ThrowJsonException_SerializerCycleDetected"
    }

    It "Attrtibute works: JsonIgnoreAttribute and Hidden" -Skip:$notNewConvertToJson {
        class TestSerializationClass
         {
             [System.Text.Json.Serialization.JsonIgnoreAttribute()][string] $testName
             [string] $testFile
             hidden [string] $testHiddenValue
         }

        $testClass = @"
        using System;
        namespace Test.Serialization
        {
            public class Test
            {
                public string strValue { get; set; }
                public int intValue { get; set; }
                public DateTime dtValue { get; set; }
                [System.Text.Json.Serialization.JsonIgnoreAttribute]
                public string ignoreValue { get; set; }
            }
        }
"@
        Add-Type -TypeDefinition $testClass

        $testPowerShell = [TestSerializationClass]::new()
        $testCSharp = [Test.Serialization.Test]::new()

        # Pipeline convertes $testPowerShell to PSObject - PowerShell PSObject custom serializer is used.
        # InputObject accepts $InputObject 'as-is' without converting to PSObject - Core serializer is used.
        # For second 'Hidden' doesn't work - it is only PowerShell pseudo attribute.
        $testPowerShell | ConvertTo-Json -Compress | Should -BeExactly '{"testFile":null}'
        ConvertTo-Json -Compress -InputObject $testPowerShell | Should -BeExactly '{"testFile":null,"testHiddenValue":null}'

        $testCSharp | ConvertTo-Json -Compress | Should -BeExactly '{"strValue":null,"intValue":0,"dtValue":"0001-01-01T00:00:00"}'
        ConvertTo-Json -Compress -InputObject $testCSharp| Should -BeExactly '{"strValue":null,"intValue":0,"dtValue":"0001-01-01T00:00:00"}'
    }

    It "Enumerable works" -Skip:$notNewConvertToJson {
        $list=[array](1,2,3)

        $list | ConvertTo-Json -Compress | Should -BeExactly '[1,2,3]'
        ConvertTo-Json -Compress -InputObject $list | Should -BeExactly '[1,2,3]'
        ,$list | ConvertTo-Json -Compress | Should -BeExactly '[1,2,3]'

        Add-Member -InputObject $list -NotePropertyName test -NotePropertyValue 100

        ,$list | ConvertTo-Json -Compress | Should -BeExactly '{"value":[1,2,3],"test":100}'

    }

    It "DateTime works" -Skip:$notNewConvertToJson {
        $date = Get-Date -Year 2019 -Month 12 -Day 5 -Hour 1 -Minute 2 -Second 3 -Millisecond 4
        $expected = "{`"value`":`"2019-12-05T01:02:03.+`",`"DisplayHint`":2,`"DateTime`":`"$($date.DateTime)`"}"
        $date | ConvertTo-Json -Compress | Should -Match $expected
    }

    It "Uri works" -skip:$notNewConvertToJson {
        $uri = [uri]"https://google.com/"
        $expected = '"https://google.com/"'
        $uri | ConvertTo-Json -Compress | Should -BeExactly $expected
    }

    It "Cycle detection works" -Skip:$notNewConvertToJson {
        $Test = @{Guid = New-Guid}
        $Test.Parent = $Test

        $exc = { ConvertTo-Json -InputObject $test } | Should -PassThru -Throw -ErrorId "System.Text.Json.JsonException,Microsoft.PowerShell.Commands.ConvertToJsonCommand"
        $exc.Exception.TargetSite.Name | Should -BeExactly "ThrowJsonException_SerializerCycleDetected"
    }
}
