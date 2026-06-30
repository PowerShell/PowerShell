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

Describe 'Automatic variable $PSProcessPath' -Tags "CI" {
    It '$PSProcessPath should return a non-empty string' {
        $PSProcessPath | Should -Not -BeNullOrEmpty
    }

    It '$PSProcessPath should be a string' {
        $PSProcessPath | Should -BeOfType [string]
    }

    It '$PSProcessPath should point to an existing file' {
        Test-Path -LiteralPath $PSProcessPath -PathType Leaf | Should -BeTrue
    }

    It '$PSProcessPath should be a constant variable' {
        $var = Get-Variable -Name PSProcessPath
        $var.Options | Should -Match 'Constant'
    }

    It '$PSProcessPath should be read-only (cannot be overwritten)' {
        { $PSProcessPath = 'something' } | Should -Throw
    }

    It '$PSProcessPath should match the current process path' {
        $PSProcessPath | Should -Be ([System.Environment]::ProcessPath)
    }
}
