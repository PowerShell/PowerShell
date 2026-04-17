# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Pop-Location" -Tags "CI" {
    BeforeAll {
        $startDirectory = $(Get-Location).Path
    }

    BeforeEach { Set-Location $startDirectory }

    It "Should be able to be called without error" {
	{ Pop-Location } | Should -Not -Throw
    }

    It "Should not take a parameter" {
	{ Pop-Location .. } | Should -Throw
    }

    It "Should be able pop multiple times" {
	Push-Location ..
	Push-Location ..
	Push-Location ..

	Pop-Location
	Pop-Location
	Pop-Location

	$(Get-Location).Path | Should -Be $startDirectory

    }

    BeforeAll {
        Set-Location $startDirectory
    }
}
