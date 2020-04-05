# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "DotNetAPI" -Tags "CI" {

    It "Should be able to use static .NET classes and get a constant" {
        [System.Math]::E  | Should -Be 2.718281828459045
        [System.Math]::PI | Should -Be 3.141592653589793
    }

    It "Should be able to invoke a method" {
        [System.Environment]::GetEnvironmentVariable("PATH") | Should -Be $env:PATH
    }

    It "Should not require 'system' in front of static classes" {
        [Environment]::CommandLine | Should -Be ([System.Environment]::CommandLine)
        [Math]::E | Should -Be ([System.Math]::E)
    }

    It "Should be able to create a new instance of a .Net object" {
        [System.Guid]$guidVal = [System.Guid]::NewGuid()
        $guidVal | Should -BeOfType Guid
    }

    It "Should access types in System.Console" {
        $type = "System.Console" -as [type]
        $type.GetTypeInfo().FullName | Should -BeExactly "System.Console"
    }
}
