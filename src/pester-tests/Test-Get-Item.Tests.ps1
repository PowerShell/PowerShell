$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Get-Item" {
    It "Should list all the items in the current working directory when asterisk is used" {
        (Get-Item *).GetType().BaseType | Should Be 'array'
        (Get-Item *).GetType().Name | Should Be 'Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
        (Get-Item .).GetType().BaseType | Should Be 'System.IO.FileSystemInfo'
        (Get-Item .).Name | Should Be 'pester-tests'
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        (Get-Item .).GetType().Name | Should Be 'DirectoryInfo'
        (Get-Item .\Test-Get-Item.Tests.ps1).GetType().Name | Should Be 'FileInfo'
    }

    It "Should return a different directory when a path argument is used" {
        (Get-Item /usr/bin) | Should Not BeNullOrEmpty
        (Get-Item ..) | Should Not BeNullOrEmpty
    }
}
