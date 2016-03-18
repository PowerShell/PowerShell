Describe "Get-PSBreakpoint" {

    $scriptName = "Get-PSBreakpoint.Tests.ps1"

    AfterEach {
        Get-PSBreakpoint -Script "$PSScriptRoot\$scriptName" | Remove-PSBreakpoint 
    }

    It "should be able to get PSBreakpoint with using Id switch" {
        Set-PSBreakpoint -Script "$PSScriptRoot\$scriptName" -Line 1
        
        { Get-PSBreakpoint -Script "$PSScriptRoot\$scriptName" } | Should Not Throw

        $Id = (Get-PSBreakpoint -Script "$PSScriptRoot\$scriptName").Id
        $Id | Should be 0

    }

    It "sshould be able to get PSBreakpoint with using Variable switch" {
        Set-PSBreakpoint -Script "$PSScriptRoot\$scriptName" -Variable "$scriptName"
        
        { Get-PSBreakpoint -Variable "$scriptName" -Script "$PSScriptRoot\$scriptName" } | Should Not Throw

        $Id = (Get-PSBreakpoint -Variable "$scriptName" -Script "$PSScriptRoot\$scriptName").Variable
        $Id | Should be $scriptName

    }
}

