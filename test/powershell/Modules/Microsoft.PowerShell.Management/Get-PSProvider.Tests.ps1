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

    Context 'ItemSeparator properties' {
        BeforeAll {
            $testCases = if ($IsWindows) {
                            @(
                                @{Provider = 'FileSystem'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Variable'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Function'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Alias'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Environment'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Certificate'; ItemSeparator = '\'; AltItemSeparator = '/'}
                                @{Provider = 'Registry'; ItemSeparator = '\'; AltItemSeparator = '\'}
                            )
                        }
                        else {
                            @(
                                @{Provider = 'FileSystem'; ItemSeparator = '/'; AltItemSeparator = '\'}
                                @{Provider = 'Variable'; ItemSeparator = '/'; AltItemSeparator = '\'}
                                @{Provider = 'Function'; ItemSeparator = '/'; AltItemSeparator = '\'}
                                @{Provider = 'Alias'; ItemSeparator = '/'; AltItemSeparator = '\'}
                                @{Provider = 'Environment'; ItemSeparator = '/'; AltItemSeparator = '\'}
                            )
                        }
        }

        It '<Provider> provider has ItemSeparator properties' -TestCases $testCases {
            param ($Provider, $ItemSeparator, $AltItemSeparator)

            (Get-PSProvider $Provider).ItemSeparator | Should -Be $ItemSeparator
            (Get-PSProvider $Provider).AltItemSeparator | Should -Be $AltItemSeparator
        }

        It 'ItemSeparator properties is read-only in <Provider> provider' -TestCases $testCases {
            param ($Provider, $ItemSeparator, $AltItemSeparator)

            { (Get-PSProvider $Provider).ItemSeparator = $null } | Should -Throw
            { (Get-PSProvider $Provider).AltItemSeparator = $null } | Should -Throw
        }
    }
}
