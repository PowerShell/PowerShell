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
        $bp.Enabled | should be $false
        Enable-PSBreakpoint $bp
        $bp.Enabled | Should be $true
    }
    It "Disable-PSBreakpoint should disable the breakpoint" {
        Enable-PSBreakpoint $bp
        $bp.Enabled | Should be $true
        Disable-PSBreakpoint $bp
        $bp.Enabled | Should be $false
    }
}
