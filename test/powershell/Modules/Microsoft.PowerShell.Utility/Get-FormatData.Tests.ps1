# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {

        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            ,$result | Should -BeOfType "System.Object[]"
        }
    }

    Context "Check for error on invalid type as argument" {

        It "Should throw error on invalid type as argument" {
            { Get-FormatData "NoSuch.Type.Exists.Or.IsLoaded" -ErrorAction Stop } | Should -Throw -ErrorId "SpecifiedTypeNotFound,Microsoft.PowerShell.Commands.GetFormatDataCommand"
        }
    }
}
