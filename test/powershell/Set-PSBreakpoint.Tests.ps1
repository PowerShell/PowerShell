Describe "Set-PSBreakpoint" {
    # Set up test script
    $testScript = Join-Path -Path $PSScriptRoot -ChildPath psbreakpointtestscript.ps1

    "`$var = 1 " > $testScript

    It "Should be able to set a psbreakpoint on a line" {
        $lineNumber = 1
        $brk = Set-PSBreakpoint -Line $lineNumber -Script $testScript
        $brk.Line | Should Be $lineNumber
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should throw when a string is entered for a line number" {
        {
            $lineNumber = "one"
            Set-PSBreakpoint -Line $lineNumber -Script $testScript

        } | Should Throw
    }

    It "Should be able to set a psbreakpoint on a Command" {
        $command = "theCommand"
        $brk = Set-PSBreakpoint -Command $command -Script $testScript
        $brk.Command | Should Be $command
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set a psbreakpoint on a variable" {
        $var = "theVariable"
        $brk = Set-PSBreakpoint -Command $var -Script $testScript
        $brk.Command | Should Be $var
        Remove-PSBreakPoint -Id $brk.Id
    }

    # clean up after ourselves
    Remove-Item -Path $testScript
}
