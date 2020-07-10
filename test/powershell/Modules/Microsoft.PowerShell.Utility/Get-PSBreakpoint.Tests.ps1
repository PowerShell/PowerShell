# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-PSBreakpoint" -Tags "CI" {

    $scriptName = "Get-PSBreakpoint.Tests.ps1"
    $fullScriptPath = Join-Path -Path $PSScriptRoot -ChildPath $scriptName

    AfterEach {
        Get-PSBreakpoint -Script $fullScriptPath | Remove-PSBreakpoint
    }

    It "should be able to get PSBreakpoint with using Id switch" {
        Set-PSBreakpoint -Script $fullScriptPath -Line 1

        { Get-PSBreakpoint -Script $fullScriptPath } | Should -Not -Throw

        $Id = (Get-PSBreakpoint -Script $fullScriptPath).Id
        # if breakpoints have been set by other tests, the number may or may not be 0
        # so we can't check against a specific number
        # however, we can be sure that we're getting an int and that the int is
        # greater or equal to 0
        ([int]$Id) -ge 0 | Should -BeTrue
    }

    It "should be able to get PSBreakpoint with using Variable switch" {
        Set-PSBreakpoint -Script $fullScriptPath -Variable "$scriptName"

        { Get-PSBreakpoint -Variable "$scriptName" -Script $fullScriptPath } | Should -Not -Throw

        $Id = (Get-PSBreakpoint -Variable "$scriptName" -Script $fullScriptPath).Variable
        $Id | Should -Be $scriptName

    }
}

