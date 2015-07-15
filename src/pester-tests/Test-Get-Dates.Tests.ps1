$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Get-Date" {
<#
    1. foreach
    2.
#>
    It "Should return a DateTime object upon being called" {
        (Get-Date).GetType().Name.Equals('DateTime') | Should Be $true
    }
    
    It "Should filter properly when displayhint switch is used" {
        (Get-Date -DisplayHint Time).ToString().Contains(":") |Should be $true
        (Get-Date -DisplayHint Date).ToString().Contains(":") | Should be $false
    }

    It "Should be able to pipe the output to another cmdlet" {
        $timestamp = Get-Date -Format o | foreach {$_ -replace ":", "."}
        $timestamp.ToString().Contains(":") | Should be $false
    }
}
