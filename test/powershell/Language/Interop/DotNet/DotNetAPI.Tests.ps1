# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "DotNetAPI" -Tags "CI" {
    $posh_E  = 2.718281828459045
    $posh_pi = 3.14159265358979

    It "Should be able to use static .NET classes and get a constant" {
	[System.Math]::E  | Should -Match $posh_E.ToString()
	[System.Math]::PI | Should -Match $posh_pi.ToString()
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
        [System.Console]::TreatControlCAsInput | Should -BeFalse
    }
}
