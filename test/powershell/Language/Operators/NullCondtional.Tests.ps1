# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'NullConditionalAssginmentOperator' -tag 'CI' {

    BeforeAll {
        $someGuid = New-Guid

        $typesTests = @(
            @{ name = 'string'; valueToSet = 'hello'}
            @{ name = 'dotnetType'; valueToSet = $someGuid}
            @{ name = 'byte'; valueToSet = [byte]0x94}
            @{ name = 'intArray'; valueToSet = 1..2}
            @{ name = 'stringArray'; valueToSet = 'a'..'c'}
            @{ name = 'emptyArray'; valueToSet = @(1,2,3)}
        )

    }

    It 'Variable doesnot exist' {
        Remove-Variable variableDoesNotExist -ErrorAction SilentlyContinue -Force

        $variableDoesNotExist ?= 1
        $variableDoesNotExist | Should -Be 1

        $variableDoesNotExist ?= 2
        $variableDoesNotExist | Should -Be 1
    }

    It 'Variable exists and is null' {
        $variableDoesNotExist = $null

        $variableDoesNotExist ?= 2
        $variableDoesNotExist | Should -Be 2
    }

    It 'Validate types - <name> can be set' -TestCases $typesTests {
        param ($name, $valueToSet)

        $x = $null
        $x ?= $valueToSet
        $x | Should -Be $valueToSet
    }

    It 'Validate hashtable can be set' {
        $x = $null
        $x ?= @{ 1 = '1'}
        $x.Keys | Should -Be @(1)
    }
}
