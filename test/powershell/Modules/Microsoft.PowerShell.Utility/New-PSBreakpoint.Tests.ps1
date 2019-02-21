# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ps = Join-Path -Path $PsHome -ChildPath "pwsh"

Describe "New-PSBreakpoint DRT Unit Tests" -Tags "CI" {
    #Set up
    $scriptFileName = Join-Path $TestDrive -ChildPath breakpointTestScript.ps1

    $contents = @"
function Hello
{
    `$greeting = 'Hello, world!'
    write-host `$greeting
}

function Goodbye
{
    `$message = 'Good bye, cruel world!'
    write-host `$message
}

Hello
Goodbye

# The following 2 statements produce null tokens (needed to verify 105473)
#
`$table = @{}

return
"@

    $contents > $scriptFileName

    It "Should be able to set psbreakpoints for -Line" {
        $brk = New-PSBreakpoint -Line 13 -Script $scriptFileName
        $brk.Line | Should -Be 13
    }

    It "Should be able to set psbreakpoints for -Line and -column" {
        $brk = New-PSBreakpoint -line 13 -column 1 -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
    }

    It "Should be able to set psbreakpoints for -Line and -action" {
        $brk = New-PSBreakpoint -line 13 -action {{ break; }} -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Action | Should -Match "break"
    }

    It "Should be able to set psbreakpoints for -Line, -column and -action" {
        $brk = New-PSBreakpoint -line 13 -column 1 -action {{ break; }} -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
        $brk.Action | Should -Match "break"
    }

    It "-script and -line can take multiple items" {
        $brk = New-PSBreakpoint -line 11,12,13 -column 1 -script $scriptFileName,$scriptFileName
        $brk.Line | Should -BeIn 11,12,13
        $brk.Column | Should -BeIn 1
    }

    It "-script and -line are positional" {
        $brk = New-PSBreakpoint $scriptFileName 13
        $brk.Line | Should -Be 13
    }

    It "-script, -line and -column are positional" {
        $brk = New-PSBreakpoint $scriptFileName 13 1
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
    }

    It "Should throw Exception when missing mandatory parameter -line" -Pending {
         $output = & $ps -noninteractive -command "nbp -column 1 -script $scriptFileName"
         [system.string]::Join(" ", $output) | Should -Match "MissingMandatoryParameter,Microsoft.PowerShell.Commands.NewPSBreakpointCommand"
    }

    It "Should throw Exception when missing mandatory parameter" -Pending {
         $output = & $ps -noprofile -noninteractive -command "nbp -line 1"
         [system.string]::Join(" ", $output) | Should -Match "MissingMandatoryParameter,Microsoft.PowerShell.Commands.NewPSBreakpointCommand"
    }

    It "Should be able to set psbreakpoints for -command" {
        $brk = New-PSBreakpoint -command "write-host"
        $brk.Command | Should -BeExactly "write-host"
    }

    It "Should be able to set psbreakpoints for -command, -script" {
        $brk = New-PSBreakpoint -command "write-host" -script $scriptFileName
        $brk.Command | Should -BeExactly "write-host"
    }

    It "Should be able to set psbreakpoints for -command, -action and -script" {
        $brk = New-PSBreakpoint -command "write-host" -action {{ break; }} -script $scriptFileName
        $brk.Action | Should -Match "break"
    }

    It "-Command can take multiple items" {
        $brk = New-PSBreakpoint -command write-host,Hello
        $brk.Command | Should -Be write-host,Hello
    }

    It "-Script is positional" {
        $brk = New-PSBreakpoint -command "Hello" $scriptFileName
        $brk.Command | Should -BeExactly "Hello"

        $brk = New-PSBreakpoint $scriptFileName -command "Hello"
        $brk.Command | Should -BeExactly "Hello"
    }

    It "Should be able to set breakpoints on functions" {
        $brk = New-PSBreakpoint -command Hello,Goodbye -script $scriptFileName
        $brk.Command | Should -Be Hello,Goodbye
    }

    It "Should be throw Exception when Column number less than 1" {
        { New-PSBreakpoint -line 1 -column -1 -script $scriptFileName } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewPSBreakpointCommand"
    }

    It "Should be throw Exception when Line number less than 1" {
        $ErrorActionPreference = "Stop"
        { New-PSBreakpoint -line -1 -script $scriptFileName } | Should -Throw -ErrorId "NewPSBreakpoint:LineLessThanOne,Microsoft.PowerShell.Commands.NewPSBreakpointCommand"
        $ErrorActionPreference = "SilentlyContinue"
    }

    It "Fail to set psbreakpoints when script is a file of wrong type" {
        $tempFile = [System.IO.Path]::GetTempFileName()
        $ErrorActionPreference = "Stop"
        {
            New-PSBreakpoint -Script $tempFile -Line 1
        } | Should -Throw
        $ErrorActionPreference = "SilentlyContinue"
        Remove-Item $tempFile -Force
    }

    It "Fail to set psbreakpoints when script file does not exist" {
        $ErrorActionPreference = "Stop"
        ${script.ps1} = 10
        {
            New-PSBreakpoint -Script variable:\script.ps1 -Line 1
        } | Should -Throw
        $ErrorActionPreference = "SilentlyContinue"
    }

    # clean up
    Remove-Item -Path $scriptFileName -Force
}

Describe "New-PSBreakpoint" -Tags "CI" {
    # Set up test script
    $testScript = Join-Path -Path $PSScriptRoot -ChildPath psbreakpointtestscript.ps1

    "`$var = 1 " > $testScript

    It "Should be able to set a psbreakpoint on a line" {
        $lineNumber = 1
        $brk = New-PSBreakpoint -Line $lineNumber -Script $testScript
        $brk.Line | Should -Be $lineNumber
    }

    It "Should throw when a string is entered for a line number" {
        {
            $lineNumber = "one"
            New-PSBreakpoint -Line $lineNumber -Script $testScript

        } | Should -Throw
    }

    It "Should be able to set a psbreakpoint on a Command" {
        $command = "theCommand"
        $brk = New-PSBreakpoint -Command $command -Script $testScript
        $brk.Command | Should -Be $command
    }

    It "Should be able to set a psbreakpoint on a variable" {
        $var = "theVariable"
        $brk = New-PSBreakpoint -Command $var -Script $testScript
        $brk.Command | Should -Be $var
    }

    # clean up after ourselves
    Remove-Item -Path $testScript
}
