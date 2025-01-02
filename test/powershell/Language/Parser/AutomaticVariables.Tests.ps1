# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Automatic variable $input' -Tags "CI" {
    # $input type in advanced functions
    It '$input Type should be arraylist and object array' {
        function from_begin { [cmdletbinding()]param() begin { Write-Output -NoEnumerate $input } }
        function from_process { [cmdletbinding()]param() process { Write-Output -NoEnumerate $input } }
        function from_end { [cmdletbinding()]param() end { Write-Output -NoEnumerate $input } }

        (from_begin) -is [System.Collections.ArrayList] | Should -BeTrue
        (from_process) -is [System.Collections.ArrayList] | Should -BeTrue
        (from_end) -is [System.Object[]] | Should -BeTrue
    }

    It 'Empty $input really is empty' {
        & { @($input).Count } | Should -Be 0
        & { [cmdletbinding()]param() begin { @($input).Count } } | Should -Be 0
        & { [cmdletbinding()]param() process { @($input).Count } } | Should -Be 0
        & { [cmdletbinding()]param() end { @($input).Count } } | Should -Be 0
    }
}
