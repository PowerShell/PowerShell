# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-UICulture" -Tags "CI" {
    It "Should have $ PsUICulture variable be equivalent to Get-UICulture object" {
        $result = Get-UICulture
        $result.Name | Should -Be $PSUICulture
        $result | Should -BeOfType CultureInfo
    }
}
