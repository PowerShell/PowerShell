Describe "Set-PSBreakpoint DRT Unit Tests" -Tags DRT{
    #Set up test script
    $scriptFileName = Join-Path $TestDrive -ChildPath breakpointTestScript.ps1
    $powershell = Join-Path $PSHOME -ChildPath powershell.exe
    
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


    It "Should be able to set psbreakpoints for line" {
        $lineNum = 13
        $columnNum = 1
        $action = "break"

        $brk = Set-PSBreakpoint -Line $lineNum -Script $scriptFileName
        $brk.Line | Should Be $lineNum
        Remove-PSBreakPoint -Id $brk.Id
        

        $brk = set-psbreakpoint -line $lineNum -column $columnNum -script $scriptFileName
        $brk.Line | Should Be $lineNum
        $brk.Column | Should Be $columnNum
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint -line $lineNum -action {{ break; }} -script $scriptFileName
        $brk.Line | Should Be $lineNum
        $brk.Action | Should Match $action
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint -line $lineNum -column $columnNum -action {{ break; }} -script $scriptFileName
        $brk.Line | Should Be $lineNum
        $brk.Column | Should Be $columnNum
        $brk.Action | Should Match $action
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-script and -line can take multiple items" {
        { sbp -line 11,12,13 -column 1 -script $scriptFileName,$scriptFileName } | Should Not Throw
        Get-PSBreakpoint -Script $scriptFileName | Remove-PSBreakpoint
    }


    It "-script, -line and -column are positional" {
        { sbp $scriptFileName 13 } | Should Not Throw
        Get-PSBreakpoint -Script $scriptFileName | Remove-PSBreakpoint

        { sbp $scriptFileName 13 1 } | Should Not Throw
        Get-PSBreakpoint -Script $scriptFileName | Remove-PSBreakpoint
    }

    It "Should be throw Exception when missing mandatory parameter -line" -Skip:($IsLinux -Or $IsOSX) {
        try {
            powershell.exe -noninteractive -command 'sbp -column 1' -script $scriptFileName
            Throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should Be "MissingMandatoryParameter,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
        }
    }

    It "Should be throw Exception when missing mandatory parameter -script" -Skip:($IsLinux -Or $IsOSX) {
        try {
            powershell.exe -noninteractive -command 'sbp -line 1' 
            Throw "Execution OK" 
        }
        catch {
            $_.FullyQualifiedErrorId | Should Be "MissingMandatoryParameter,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
        }
    }

        
    It "Should be throw Exception when BUG 104306:	New-PSBreakpoint -Column should not allow numbers less than 1" {
        try {
            set-psbreakpoint -line 1 -column -1 -script $scriptFileName 
            Throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
        }

    }

    It "Should be throw Exception when BUG 104304:	New-PSBreakpoint -Line should not allow numbers less than 1" {
        try {
            set-psbreakpoint -line -1 -script $scriptFileName
            Throw "Execution OK" 
        }
        catch {
            $_.FullyQualifiedErrorId | Should Be "SetPSBreakpoint:LineLessThanOne,Microsoft.PowerShell.Commands.SetPSBreakpointCommand"
        }
    }
    

    It "Should be able to set psbreakpoints for command" {
        $brk = set-psbreakpoint -command "write-host" 
        $brk.Command | Should Be "write-host"
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint -command "write-host" -script $scriptFileName 
        $brk.Command | Should Be "write-host"
        Remove-PSBreakPoint -Id $brk.Id     

        $brk = set-psbreakpoint -command "write-host" -action {{ break; }} -script $scriptFileName 
        $brk.Action | Should Match "break"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-Command can take multiple items" {
        $brk = set-psbreakpoint -command "write-host,Hello"
        $brk.Command | Should Be "write-host,Hello"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "-Script is positional" {
        $brk = set-psbreakpoint -command "Hello" $scriptFileName
        $brk.Command | Should Be "Hello" 
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint $scriptFileName -command "Hello" 
        $brk.Command | Should Be "Hello"
        Remove-PSBreakPoint -Id $brk.Id
    }

    It "Should be able to set breakpoints on functions" {
        $brk = set-psbreakpoint -command "Hello"
        $brk.Command | Should Be "Hello"
        Remove-PSBreakPoint -Id $brk.Id 

        $brk = set-psbreakpoint -command "Hello" -script $scriptFileName 
        $brk.Command | Should Be "Hello"
        Remove-PSBreakPoint -Id $brk.Id

        $brk = set-psbreakpoint -command "Hello,Goodbye"
        $brk.Command | Should Be "Hello,Goodbye" 
        Remove-PSBreakPoint -Id $brk.Id

    }

    Remove-Item -Path $scriptFileName -Force
}

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
