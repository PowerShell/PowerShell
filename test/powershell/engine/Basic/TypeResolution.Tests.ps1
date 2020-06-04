# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Resolve types in additional referenced assemblies" -Tag CI {
    It "Will resolve DirectoryServices type <name>" -TestCases @(
        @{ typename = "[System.DirectoryServices.AccountManagement.AdvancedFilters]"; name = "AdvancedFilters" }
    ){
        param ($typename, $name)
        & "$PSHOME/pwsh" -noprofile -command "$typename.Name" | Should -BeExactly $name
    }
}
