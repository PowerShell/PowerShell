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

    It 'Should serialize special floating-point values as strings' {
        [double]::PositiveInfinity | ConvertTo-Json | Should -BeExactly '"Infinity"'
        [double]::NegativeInfinity | ConvertTo-Json | Should -BeExactly '"-Infinity"'
        [double]::NaN | ConvertTo-Json | Should -BeExactly '"NaN"'
    }

    It 'Should return null for empty array' {
        @() | ConvertTo-Json | Should -BeNull
    }

    It 'Should serialize SwitchParameter as object with IsPresent property' {
        @{ flag = [switch]$true } | ConvertTo-Json -Compress | Should -BeExactly '{"flag":{"IsPresent":true}}'
        @{ flag = [switch]$false } | ConvertTo-Json -Compress | Should -BeExactly '{"flag":{"IsPresent":false}}'
    }

    It 'Should serialize Uri correctly' {
        $uri = [uri]"https://example.com/path"
        $json = $uri | ConvertTo-Json -Compress
        $json | Should -BeExactly '"https://example.com/path"'
    }

    It 'Should serialize enums <description>' -TestCases @(
        @{ description = 'as numbers by default'; params = @{}; expected = '1' }
        @{ description = 'as strings with -EnumsAsStrings'; params = @{ EnumsAsStrings = $true }; expected = '"Monday"' }
    ) {
        param($description, $params, $expected)
        [System.DayOfWeek]::Monday | ConvertTo-Json @params | Should -BeExactly $expected
    }

    It 'Should serialize nested PSCustomObject correctly' {
        $obj = [pscustomobject]@{
            name = "test"
            child = [pscustomobject]@{
                value = 42
            }
        }
        $json = $obj | ConvertTo-Json -Compress
        $json | Should -BeExactly '{"name":"test","child":{"value":42}}'
    }

    It 'Should not escape HTML tag characters by default' {
        $json = @{ text = '<>&' } | ConvertTo-Json -Compress
        $json | Should -BeExactly '{"text":"<>&"}'
    }

    It 'Should escape <description> with -EscapeHandling <EscapeHandling>' -TestCases @(
        @{ description = 'HTML tag characters'; inputText = '<>&'; EscapeHandling = 'EscapeHtml'; pattern = '\\u003C.*\\u003E.*\\u0026' }
        @{ description = 'non-ASCII characters'; inputText = '日本語'; EscapeHandling = 'EscapeNonAscii'; pattern = '\\u' }
    ) {
        param($description, $inputText, $EscapeHandling, $pattern)
        $json = @{ text = $inputText } | ConvertTo-Json -Compress -EscapeHandling $EscapeHandling
        $json | Should -Match $pattern
    }

    It 'Depth over 100 should throw' {
        { ConvertTo-Json -InputObject @{a=1} -Depth 101 } | Should -Throw
    }

    Context 'Nested raw object serialization' {
        BeforeAll {
            class TestClassWithFileInfo {
                [System.IO.FileInfo]$File
            }

            $script:testFilePath = Join-Path $PSHOME 'pwsh.dll'
            if (-not (Test-Path $script:testFilePath)) {
                $script:testFilePath = Join-Path $PSHOME 'System.Management.Automation.dll'
            }
        }

        It 'Typed property with raw FileInfo should serialize Base properties only' {
            $obj = [TestClassWithFileInfo]::new()
            $obj.File = [System.IO.FileInfo]::new($script:testFilePath)

            $json = $obj | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -Be 17
        }

        It 'Typed property loses PSObject wrapper from Get-Item' {
            $obj = [TestClassWithFileInfo]::new()
            $obj.File = Get-Item $script:testFilePath

            $json = $obj | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -Be 17
        }

        It 'PSCustomObject with Get-Item preserves Extended properties' {
            $obj = [PSCustomObject]@{
                File = Get-Item $script:testFilePath
            }

            $json = $obj | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -BeGreaterThan 17
            $parsed.File.PSObject.Properties.Name | Should -Contain 'PSPath'
        }

        It 'PSCustomObject with raw FileInfo should serialize Base properties only' {
            $obj = [PSCustomObject]@{
                File = [System.IO.FileInfo]::new($script:testFilePath)
            }

            $json = $obj | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -Be 17
            $parsed.File.PSObject.Properties.Name | Should -Not -Contain 'PSPath'
        }

        It 'Hashtable with raw FileInfo should serialize Base properties only' {
            $hash = @{
                File = [System.IO.FileInfo]::new($script:testFilePath)
            }

            $json = $hash | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -Be 17
        }

        It 'Hashtable with Get-Item preserves Extended properties' {
            $hash = @{
                File = Get-Item $script:testFilePath
            }

            $json = $hash | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed.File.PSObject.Properties.Name.Count | Should -BeGreaterThan 17
        }

        It 'Array of raw FileInfo should serialize with Adapted properties' {
            $arr = @([System.IO.FileInfo]::new($script:testFilePath))

            $json = $arr | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed[0].PSObject.Properties.Name.Count | Should -Be 24
        }

        It 'Array of Get-Item FileInfo preserves Extended properties' {
            $arr = @(Get-Item $script:testFilePath)

            $json = $arr | ConvertTo-Json -Depth 2
            $parsed = $json | ConvertFrom-Json

            $parsed[0].PSObject.Properties.Name.Count | Should -BeGreaterThan 24
        }
    }
}
