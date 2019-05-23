# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Resolve types in additional referenced assemblies" -Tag CI {
    It "Will resolve DirectoryServices type <name>" -TestCases @(
        @{ typename = "[System.DirectoryServices.AccountManagement.AdvancedFilters]"; name = "AdvancedFilters" }
    ){
        param ($typename, $name)
        pwsh -noprofile -command "$typename.Name" | Should -BeExactly $name
    }
}
