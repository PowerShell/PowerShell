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

    Context 'PathSeparators' {
        BeforeAll {
            $testCases = if ($IsWindows) {
                            @(
                                @{Provider = 'FileSystem'; Value = @("\", "/")}
                                @{Provider = 'Variable'; Value = @("\", "/")}
                                @{Provider = 'Function'; Value = @("\", "/")}
                                @{Provider = 'Alias'; Value = @("\", "/")}
                                @{Provider = 'Environment'; Value = @("\", "/")}
                                @{Provider = 'Certificate'; Value = @("\", "/")}
                                @{Provider = 'Registry'; Value = @("\")}
                            )
                        }
                        else {
                            @(
                                @{Provider = 'FileSystem'; Value = @("/", "\")}
                                @{Provider = 'Variable'; Value = @("/", "\")}
                                @{Provider = 'Function'; Value = @("/", "\")}
                                @{Provider = 'Alias'; Value = @("/", "\")}
                                @{Provider = 'Environment'; Value = @("/", "\")}
                                @{Provider = 'Certificate'; Value = @("/", "\")}
                            )
                        }
        }

        It '<Provider> provider has PathSeparator property' -TestCases $testCases {
            param ($Provider, $Value)

            (Get-PSProvider $Provider).PathSeparator | Should -Be $Value
        }

        It 'PathSeparator property is read-only in <Provider> provider' -TestCases $testCases {
            param ($Provider, $Value)

            { (Get-PSProvider $Provider).PathSeparator = $null } | Should -Throw
        }

        It 'cannot modify PathSeparator collection in <Provider> provider' -TestCases $testCases {
            param ($Provider, $Value)

            $separator = (Get-PSProvider $Provider).PathSeparator

            { $separator[0] = "w" } | Should -Throw
        }

        It 'copying and modifying values does not affect PathSeparator property in <Provider> provider' -TestCases $testCases {
            param ($Provider, $Value)

            $separator = (Get-PSProvider $Provider).PathSeparator

            # copying to a new object
            $copy = @("", "")
            $separator.CopyTo($copy, 0)

            $copy[0] = "w"

            (Get-PSProvider $Provider).PathSeparator | Should -Be $Value
        }
    }
}
