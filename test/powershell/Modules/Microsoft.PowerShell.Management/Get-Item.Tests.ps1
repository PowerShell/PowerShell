Describe "Get-Item" {
    It "Should list all the items in the current working directory when asterisk is used" {
	(Get-Item (Join-Path -Path $PSScriptRoot -ChildPath "*")).GetType().BaseType | Should Be 'array'
	(Get-Item (Join-Path -Path $PSScriptRoot -ChildPath "*")).GetType().Name | Should Be 'Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
	(Get-Item $PSScriptRoot).GetType().BaseType | Should Be 'System.IO.FileSystemInfo'
	(Get-Item $PSScriptRoot).Name | Should Be (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
	(Get-Item $PSScriptRoot).GetType().Name | Should Be 'DirectoryInfo'
	(Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1)).GetType().Name | Should Be 'FileInfo'
    }

    It "Should have mode flags set" {
	Get-ChildItem $PSScriptRoot | foreach-object { $_.Mode | Should Not BeNullOrEmpty }
    }
}
