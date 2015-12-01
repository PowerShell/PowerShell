$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Describe "Set-PSBreakpoint" {
    New-Variable -Name script -Value $here/assets/testablescript.ps1 -Scope Global -Force

    It "Should be able to called with script and line parameters without error " {
        { Set-PSBreakpoint -Script $script -Line 1 } | Should Not Throw
    }

    It "Should be able to be called using the command switch" {
        # Note- we don't actually require the command to exist in the script- it will simply not get a breakpoint
        { Set-PSBreakpoint -Script $script -Command aoeuaoeu } | Should Not Throw
    }
}
