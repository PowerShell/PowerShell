Describe "New-PSSession basic test" -Tag @("CI") {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:skip'] = $true
    }
    It "New-PSSession should not crash powershell" {
        try {
    	New-PSSession -ComputerName nonexistcomputer -Authentication Basic
    	throw "New-PSSession should throw"
        } catch {
    	$_.FullyQualifiedErrorId | Should Be "InvalidOperation,Microsoft.PowerShell.Commands.NewPSSessionCommand"
        }
    }
    $PSDefaultParameterValues.remove('it:skip')
}
