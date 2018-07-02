# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Enable and Disable PSBreakpoints" -Tag "CI" {
    BeforeAll {
        $path = Setup -F testbp.ps1 -content "get-command`nget-date`nget-location" -pass
        $bp = set-psbreakpoint -script $path -line 2
        disable-psbreakpoint $bp
    }
    AfterAll {
        $bp | remove-psbreakpoint
    }
    It "Enable-PSBreakpoint should enable the breakpoint" {
        $bp.Enabled | Should -BeFalse
        Enable-PSBreakpoint $bp
        $bp.Enabled | Should -BeTrue
    }
    It "Disable-PSBreakpoint should disable the breakpoint" {
        Enable-PSBreakpoint $bp
        $bp.Enabled | Should -BeTrue
        Disable-PSBreakpoint $bp
        $bp.Enabled | Should -BeFalse
    }
}
