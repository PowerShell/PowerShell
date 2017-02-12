Describe "Get-Item" -Tags "CI" {
    It "Should list all the items in the current working directory when asterisk is used" {
        $items = Get-Item (Join-Path -Path $PSScriptRoot -ChildPath "*")
        $items | Should Not BeNullOrEmpty
        $items.GetType().BaseType | Should Be 'array'
        $items.GetType().Name | Should Be 'Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
        $item = Get-Item $PSScriptRoot
        $item | Should Not BeNullOrEmpty
        $item.GetType().BaseType | Should Be 'System.IO.FileSystemInfo'
        $item.Name | Should Be (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        $rootitem = Get-Item $PSScriptRoot
        $rootitem | Should Not BeNullOrEmpty
        $rootitem.GetType().Name | Should Be 'DirectoryInfo'
        $childitem = (Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1))
        $childitem | Should Not BeNullOrEmpty
        $childitem.GetType().Name | Should Be 'FileInfo'
    }

    It "Should have mode flags set" {
        Get-ChildItem $PSScriptRoot | foreach-object { $_.Mode | Should Not BeNullOrEmpty }
    }
}
