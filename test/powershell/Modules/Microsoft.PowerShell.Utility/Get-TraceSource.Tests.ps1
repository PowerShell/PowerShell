# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-TraceSource" -Tags "Feature" {
    It "Should output data sorted by name" {
        $expected = (Get-TraceSource | Sort-Object Name)
        Get-TraceSource | Should -Be $expected
    }
}
