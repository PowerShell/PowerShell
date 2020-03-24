# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-TraceSource" -Tags "Feature" {
    It "Should output data sorted by name" {
        $expected = (Get-TraceSource | Sort-Object Name)
        Get-TraceSource | Should -Be $expected
    }
}
