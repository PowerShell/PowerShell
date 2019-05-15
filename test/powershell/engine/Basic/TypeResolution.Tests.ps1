# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Resolve types in additional referenced assemblies" -Tag CI {
    It "Will resolve DirectoryServices type <name>" -TestCases @(
        @{ typename = "[System.DirectoryServices.AccountManagement.AdvancedFilters]"; name = "AdvancedFilters" }
        @{ typename = "[Markdig.Markdown]"; name = "Markdown"}
    ){
        param ($typename, $name)
        $type = & ([scriptblock]::Create($typename))
        $type.Name | Should -BeExactly $name
    }
}
