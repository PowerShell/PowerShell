# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-UICulture" -Tags "CI" {
    It "Should have $ PsUICulture variable be equivalent to Get-UICulture object" {
        $result = Get-UICulture
        $result.Name | Should -Be $PsUICulture
        $result | Should -BeOfType CultureInfo
    }
}
