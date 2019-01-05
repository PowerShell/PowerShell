# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

using namespace System.Environment
using namespace System.Management.Automation
using namespace Newtonsoft.Json.Linq

Describe 'ConvertTo-Json' -tags 'CI' {
    It 'Newtonsoft.Json.Linq.Jproperty should be converted to Json properly' {
        $object = [pscustomobject]@{
            HashTable = [hashtable]@{
                JObject = [JObject]::FromObject( @{
                        TestValue1 = 123456;
                        TestValue2 = 78910;
                        TestValue3 = 99999
                    } );
                StrObject = 'This is a string Object'
            };
            RandomString = 'A quick brown fox jumped over the lazy dog'
        }

        $jsonFormat = ConvertTo-Json -InputObject $object
        $jsonFormat | Should -Match '"TestValue1": 123456'
        $jsonFormat | Should -Match '"TestValue2": 78910'
        $jsonFormat | Should -Match '"TestValue3": 99999'
    }

    It 'StopProcessing should succeed' {
        $ps = [powershell]::Create()
        [void]$ps.AddScript( {
                $obj = [pscustomobject]@{P1 = ''; P2 = ''; P3 = ''; P4 = ''; P5 = ''; P6 = ''}
                $obj.P1 = $obj.P2 = $obj.P3 = $obj.P4 = $obj.P5 = $obj.P6 = $obj
                (1..100).ForEach{
                    ConvertTo-Json -InputObject $obj -Depth 10
                }
                throw 'ConvertTo-Json finished processing before it could be stopped.'
            } )

        [void]$ps.BeginInvoke()

        # Wait for instance to start.
        if (-not (Wait-UntilTrue { $ps.InvocationStateInfo.State -eq [PSInvocationState]::Running } -TimeoutInMilliseconds 1000 -IntervalInMilliseconds 10)) {
             throw 'PowerShell instance did not start.'
        }

        # Not using synchronous Stop() to avoid blocking Pester.
        [void]$ps.BeginStop($null, $null)

        # Instance should stop.
        Wait-UntilTrue { $ps.InvocationStateInfo.State -eq [PSInvocationState]::Stopped } -TimeoutInMilliseconds 1000 -IntervalInMilliseconds 10 | Should -BeTrue
        $ps.Dispose()
    }

    It 'The result string is packed in an array symbols when AsArray parameter is used.' {
        $output = ConvertTo-Json -InputObject 1 -AsArray
        $output | Should -BeLike '`[*1*]'

        $output = ConvertTo-Json -InputObject (1, 2) -AsArray
        $output | Should -BeLike '`[*1*2*]'
    }

    It 'The result string is not packed in the array symbols when there is only one input object and AsArray parameter is not used.' {
        $output = ConvertTo-Json -InputObject 1
        $output | Should -BeExactly '1'
    }

    It 'The result string should <Name>.' -TestCases @(
        $nl = [Environment]::NewLine
        @{name = 'be not escaped by default.';                     params = @{};                              expected = "{$nl  ""abc"": ""'def'""$nl}" }
        @{name = 'be not escaped with "-EscapeHandling Default".'; params = @{EscapeHandling = 'Default'};    expected = "{$nl  ""abc"": ""'def'""$nl}" }
        @{name = 'be escaped with "-EscapeHandling EscapeHtml".';  params = @{EscapeHandling = 'EscapeHtml'}; expected = "{$nl  ""abc"": ""\u0027def\u0027""$nl}" }
    ) {
        param ($name, $params, $expected)

        @{ 'abc' = "'def'" } | ConvertTo-Json @params | Should -BeExactly $expected
    }
}
