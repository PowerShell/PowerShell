# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-PSProvider" -Tags "CI" {
    It "Should be able to call with no parameters without error" {
	{ Get-PSProvider } | Should -Not -Throw
    }

    It "Should be able to call the filesystem provider" {
	{ Get-PSProvider FileSystem } | Should -Not -Throw

	$actual = Get-PSProvider FileSystem

	$actual.Name | Should -BeExactly "FileSystem"

	$actual.Capabilities | Should -BeExactly "Filter, ShouldProcess, Credentials"
    }

    It "Should be able to call a provider with a wildcard expression" {
	{ Get-PSProvider File*m } | Should -Not -Throw
    }

    It "Should be able to pipe the output" {
	$actual = Get-PSProvider

	{ $actual | Format-List } | Should -Not -Throw
    }

    Context 'FileSystem provider' {
        It 'has PathSeparator property' {
            (Get-PSProvider FileSystem).PathSeparator | Should -Be @("\", "/")
        }
    }

    Context 'Registry provider' {
        It 'has PathSeparator property' {
            if ($IsWindows) {
                (Get-PSProvider Registry).PathSeparator | Should -Be @("\")
            }
        }
    }

    Context 'Variable provider' {
        It 'has PathSeparator property' {
            (Get-PSProvider Variable).PathSeparator | Should -Be @("\", "/")
        }
    }

    Context 'Function provider' {
        It 'has PathSeparator property' {
            (Get-PSProvider Function).PathSeparator | Should -Be @("\", "/")
        }
    }
}
