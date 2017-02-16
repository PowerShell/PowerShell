Describe "Get-Item" -Tags "CI" {
    It "Should list all the items in the current working directory when asterisk is used" {
        $items = Get-Item (Join-Path -Path $PSScriptRoot -ChildPath "*")
        ,$items | Should BeOfType 'System.Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
        $item = Get-Item $PSScriptRoot
        $item | Should BeOfType 'System.IO.DirectoryInfo'
        $item.Name | Should Be (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        $rootitem = Get-Item $PSScriptRoot
        $rootitem | Should BeOfType 'System.IO.DirectoryInfo'
        $childitem = (Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1))
        $childitem | Should BeOfType 'System.IO.FileInfo'
    }

    It "Should have mode flags set" {
        Get-ChildItem $PSScriptRoot | foreach-object { $_.Mode | Should Not BeNullOrEmpty }
    }
}
