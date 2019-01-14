# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ps = Join-Path -Path $PsHome -ChildPath "pwsh"

Describe "Set-PSBreakpoint DRT Unit Tests" -Tags "CI" {
    #Set up
    $scriptFileName = Join-Path $TestDrive -ChildPath breakpointTestScript.ps1
    $scriptFileNameBug = Join-Path -Path $TestDrive -ChildPath SetPSBreakpointTests.ExposeBug154112.ps1

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

    $contentsBug = @"
set-psbreakpoint -variable foo
set-psbreakpoint -command foo
"@

    $contents > $scriptFileName
    $contentsBug > $scriptFileNameBug

    It "Should be able to set psbreakpoints for -Line" {
        $brk = Set-PSBreakpoint -Line 13 -Script $scriptFileName
        $brk.Line | Should -Be 13
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set psbreakpoints for -Line and -column" {
        $brk = set-psbreakpoint -line 13 -column 1 -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set psbreakpoints for -Line and -action" {
        $brk = set-psbreakpoint -line 13 -action {{ break; }} -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Action | Should -Match "break"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set psbreakpoints for -Line, -column and -action" {
        $brk = set-psbreakpoint -line 13 -column 1 -action {{ break; }} -script $scriptFileName
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
        $brk.Action | Should -Match "break"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-script and -line can take multiple items" {
        $brk = Set-PSBreakpoint -line 11,12,13 -column 1 -script $scriptFileName,$scriptFileName
        $brk.Line | Should -BeIn 11,12,13
        $brk.Column | Should -BeIn 1
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-script and -line are positional" {
        $brk = Set-PSBreakpoint $scriptFileName 13
        $brk.Line | Should -Be 13
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-script, -line and -column are positional" {
        $brk = Set-PSBreakpoint $scriptFileName 13 1
        $brk.Line | Should -Be 13
        $brk.Column | Should -Be 1
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should throw Exception when missing mandatory parameter -line" -Pending {
         $output = & $ps -noninteractive -command "sbp -column 1 -script $scriptFileName"
         [system.string]::Join(" ", $output) | Should -Match "MissingMandatoryParameter,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
    }

    It "Should throw Exception when missing mandatory parameter" -Pending {
         $output = & $ps -noprofile -noninteractive -command "sbp -line 1"
         [system.string]::Join(" ", $output) | Should -Match "MissingMandatoryParameter,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
    }

    It "Should be able to set psbreakpoints for -command" {
        $brk = set-psbreakpoint -command "write-host"
        $brk.Command | Should -BeExactly "write-host"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set psbreakpoints for -command, -script" {
        $brk = set-psbreakpoint -command "write-host" -script $scriptFileName
        $brk.Command | Should -BeExactly "write-host"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set psbreakpoints for -command, -action and -script" {
        $brk = set-psbreakpoint -command "write-host" -action {{ break; }} -script $scriptFileName
        $brk.Action | Should -Match "break"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-Command can take multiple items" {
        $brk = set-psbreakpoint -command write-host,Hello
        $brk.Command | Should -Be write-host,Hello
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-Script is positional" {
        $brk = set-psbreakpoint -command "Hello" $scriptFileName
        $brk.Command | Should -BeExactly "Hello"
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint $scriptFileName -command "Hello"
        $brk.Command | Should -BeExactly "Hello"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set breakpoints on functions" {
        $brk = set-psbreakpoint -command Hello,Goodbye -script $scriptFileName
        $brk.Command | Should -Be Hello,Goodbye
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be throw Exception when Column number less than 1" {
        { set-psbreakpoint -line 1 -column -1 -script $scriptFileName } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
    }

    It "Should be throw Exception when Line number less than 1" {
        $ErrorActionPreference = "Stop"
        { set-psbreakpoint -line -1 -script $scriptFileName } | Should -Throw -ErrorId "SetPSBreakpoint:LineLessThanOne,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
        $ErrorActionPreference = "SilentlyContinue"
    }

    It "Remove implicit script from 'set-psbreakpoint -script'" {
        & $ps -noprofile $scriptFileNameBug

        $breakpoint = Get-PSBreakpoint -Script $scriptFileNameBug
        $breakpoint | Should -BeNullOrEmpty
    }

    It "Fail to set psbreakpoints when script is a file of wrong type" {
        $tempFile = [System.IO.Path]::GetTempFileName()
        $ErrorActionPreference = "Stop"
        {
            Set-PSBreakpoint -Script $tempFile -Line 1
        } | Should -Throw
        $ErrorActionPreference = "SilentlyContinue"
        Remove-Item $tempFile -Force
    }

    It "Fail to set psbreakpoints when script file does not exist" {
        $ErrorActionPreference = "Stop"
        ${script.ps1} = 10
        {
            Set-PSBreakpoint -Script variable:\script.ps1 -Line 1
        } | Should -Throw
        $ErrorActionPreference = "SilentlyContinue"
    }

    # clean up
    Remove-Item -Path $scriptFileName -Force
    Remove-Item -Path $scriptFileNameBug -Force
}

Describe "Set-PSBreakpoint" -Tags "CI" {
    # Set up test script
    $testScript = Join-Path -Path $PSScriptRoot -ChildPath psbreakpointtestscript.ps1

    "`$var = 1 " > $testScript

    It "Should be able to set a psbreakpoint on a line" {
        $lineNumber = 1
        $brk = Set-PSBreakpoint -Line $lineNumber -Script $testScript
        $brk.Line | Should -Be $lineNumber
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should throw when a string is entered for a line number" {
        {
            $lineNumber = "one"
            Set-PSBreakpoint -Line $lineNumber -Script $testScript

        } | Should -Throw
    }

    It "Should be able to set a psbreakpoint on a Command" {
        $command = "theCommand"
        $brk = Set-PSBreakpoint -Command $command -Script $testScript
        $brk.Command | Should -Be $command
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set a psbreakpoint on a variable" {
        $var = "theVariable"
        $brk = Set-PSBreakpoint -Command $var -Script $testScript
        $brk.Command | Should -Be $var
        Remove-PSBreakPoint -Id $brk.Id
    }

    # clean up after ourselves
    Remove-Item -Path $testScript
}
