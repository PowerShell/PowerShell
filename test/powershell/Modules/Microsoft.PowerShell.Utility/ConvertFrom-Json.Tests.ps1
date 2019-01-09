# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe 'ConvertFrom-Json' -tags "CI" {

    BeforeAll {
        $testCasesWithAndWithoutAsHashtableSwitch = @(
            @{ AsHashtable = $true  }
            @{ AsHashtable = $false }
        )

        $testCasesJsonDepthWithAndWithoutAsHashtableSwitch = @(
            @{ Depth = 2;    AsHashtable = $true  }
            @{ Depth = 2;    AsHashtable = $false }
            @{ Depth = 200;  AsHashtable = $true  }
            @{ Depth = 200;  AsHashtable = $false }
            @{ Depth = 2000; AsHashtable = $true  }
            @{ Depth = 2000; AsHashtable = $false }
        )

        function GenerateNestedJson {
            Param(
                [int] $Depth
            )

            $nestedJson = "null"

            1..$Depth | ForEach-Object {
                $nestedJson = '{"' + $_ + '":' + $nestedJson + '}'
            }

            return $nestedJson
        }
    }

    It 'Can convert a single-line object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        ('{"a" : "1"}' | ConvertFrom-Json -AsHashtable:$AsHashtable).a | Should -Be 1
    }

    It 'Can convert one string-per-object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $json = @('{"a" : "1"}', '{"a" : "x"}') | ConvertFrom-Json -AsHashtable:$AsHashtable
        $json.Count | Should -Be 2
        $json[1].a | Should -Be 'x'
        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
    }

    It 'Can convert multi-line object with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $json = @('{"a" :', '"x"}') | ConvertFrom-Json -AsHashtable:$AsHashtable
        $json.a | Should -Be 'x'
        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
    }

    It 'Can convert an object with Newtonsoft.Json metadata properties with AsHashtable switch set to <AsHashtable>' -TestCase $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $id = 13
        $type = 'Calendar.Months.December'
        $ref = 1989

        $json = '{"$id":' + $id + ', "$type":"' + $type + '", "$ref":' + $ref + '}' | ConvertFrom-Json -AsHashtable:$AsHashtable

        $json.'$id' | Should -Be $id
        $json.'$type' | Should -Be $type
        $json.'$ref' | Should -Be $ref

        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
    }

    It 'Can convert an object of depth 1024 by default with AsHashtable switch set to <AsHashtable>' -TestCases $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $nestedJson = GenerateNestedJson -Depth:1024

        $json = $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable

        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
        else
        {
            $json | Should -BeOfType PSCustomObject
        }
    }

    It 'Fails to convert an object of depth higher than 1024 by default with AsHashtable switch set to <AsHashtable>' -TestCases $testCasesWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable)
        $nestedJson = GenerateNestedJson -Depth:1989

        { $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable } |
            Should -Throw -ErrorId "System.ArgumentException,Microsoft.PowerShell.Commands.ConvertFromJsonCommand"
    }

    It 'Can convert an object with depth less than Depth param set to <Depth> and AsHashtable switch set to <AsHashtable>' -TestCases $testCasesJsonDepthWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable, $Depth)
        $nestedJson = GenerateNestedJson -Depth:($Depth - 1)

        $json = $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable -Depth:$Depth

        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
        else
        {
            $json | Should -BeOfType PSCustomObject
        }
    }

    It 'Can convert an object with depth equal to Depth param set to <Depth> and AsHashtable switch set to <AsHashtable>' -TestCases $testCasesJsonDepthWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable, $Depth)
        $nestedJson = GenerateNestedJson -Depth:$Depth

        $json = $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable -Depth:$Depth

        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
        else
        {
            $json | Should -BeOfType PSCustomObject
        }
    }

    It 'Can convert an object with depth equal to <Depth> when Depth param is set to 0 and AsHashtable switch set to <AsHashtable>' -TestCases $testCasesJsonDepthWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable, $Depth)
        $nestedJson = GenerateNestedJson -Depth:$Depth

        $json = $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable -Depth:0

        if ($AsHashtable)
        {
            $json | Should -BeOfType Hashtable
        }
        else
        {
            $json | Should -BeOfType PSCustomObject
        }
    }

    It 'Fails to convert an object with greater depth than Depth param set to <Depth> and AsHashtable switch set to <AsHashtable>' -TestCases $testCasesJsonDepthWithAndWithoutAsHashtableSwitch {
        Param($AsHashtable, $Depth)
        $nestedJson = GenerateNestedJson -Depth:($Depth + 1)

        { $nestedJson | ConvertFrom-Json -AsHashtable:$AsHashtable -Depth:$Depth } |
            Should -Throw -ErrorId "System.ArgumentException,Microsoft.PowerShell.Commands.ConvertFromJsonCommand"
    }
}
