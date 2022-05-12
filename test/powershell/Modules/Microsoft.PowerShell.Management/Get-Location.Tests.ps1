# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Location" -Tags "CI" {
    $currentDirectory=[System.IO.Directory]::GetCurrentDirectory()
    BeforeEach {
	Push-Location $currentDirectory
    }

    AfterEach {
	Pop-Location
    }

    It "Should list the output of the current working directory" {

	(Get-Location).Path | Should -BeExactly $currentDirectory
    }

    It "Should do exactly the same thing as its alias" {
	(pwd).Path | Should -BeExactly (Get-Location).Path
    }
}
