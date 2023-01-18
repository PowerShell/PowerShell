# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Automatic variable $input' -Tags "CI" {
    It 'Empty $input really is empty' {
        & { @($input).Count } | Should -Be 0
        & { [cmdletbinding()]param() begin { @($input).Count } } | Should -Be 0
        & { [cmdletbinding()]param() process { @($input).Count } } | Should -Be 0
        & { [cmdletbinding()]param() end { @($input).Count } } | Should -Be 0
    }
}
