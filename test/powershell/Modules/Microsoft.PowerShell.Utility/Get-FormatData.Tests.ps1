# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {

        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            ,$result | Should -BeOfType "System.Object[]"
        }
    }
}
