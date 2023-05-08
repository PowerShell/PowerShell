# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$assetsDir = Join-Path -Path $PSScriptRoot -ChildPath assets

Describe "Import-LocalizedData" -Tags "CI" {

    BeforeAll {
	$script = "localized.ps1"
	$sunday = "Sunday"
	$sundayInGerman = $sunday + " (in German)"
    }

    It "Should be able to import string using default culture" {
	# Set-Culture is broken, but let's verify that default culture is en-US
$culture = Get-Culture
$culture.Name     | Should -BeExactly "en-US"

$d = Import-LocalizedData -FileName $script -BaseDirectory $assetsDir
$d.d0             | Should -Be $sunday
}

It "Should be able to import string using en-US culture" {
    $d = Import-LocalizedData -FileName $script -BaseDirectory $assetsDir -UICulture en-US
    $d.d0             | Should -Be $sunday
}

It "Should be able to import string using de-DE culture" {
    $d = Import-LocalizedData -FileName $script -BaseDirectory $assetsDir -UICulture de-DE
    $d.d0             | Should -Be $sundayInGerman
}

It "Should be able to import string and store in binding variable" {
    Import-LocalizedData -FileName $script -BaseDirectory $assetsDir -UICulture de-DE -BindingVariable d
    $d.d0             | Should -Be $sundayInGerman
}

}
