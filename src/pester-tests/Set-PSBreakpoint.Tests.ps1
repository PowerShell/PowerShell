$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Describe "Set-PSBreakpoint" {
    # Set up test script
    $testScript = "$here/psbreakpointtestscript.ps1"

    "`$var = 1 " > $testScript

    It "Should be able to set a psbreakpoint on a line" {
        $lineNumber = 1
        $(Set-PSBreakpoint -Line $lineNumber -Script $testScript).Line | Should Be $lineNumber
    }

    It "Should throw when a string is entered for a line number" {
        {
            $lineNumber = "one"
            $(Set-PSBreakpoint -Line $lineNumber -Script $testScript).Line 

        } | Should Throw
    }

    It "Should be able to set a psbreakpoint on a Command" {
        $command = "theCommand"
        $(Set-PSBreakpoint -Command $command -Script $testScript).Command | Should Be $command
    }

    It "Should be able to set a psbreakpoint on a variable" {
        $var = "theVariable"
        $(Set-PSBreakpoint -Command $var -Script $testScript).Command | Should Be $var
    }

    # clean up after ourselves
    Remove-Item -Path $testScript
}