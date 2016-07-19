Describe "Get-RunspaceDebug" {

    Context "Check return types of RunspaceDebug" {

	It -skip:($IsCore) "Should return Microsoft.Powershell.Commands.PSRunspaceDebug as the return type" {
	    $rs = Get-RunspaceDebug
	    $rs[0].GetType().Name | Should Be PSRunspaceDebug
	}
    }
}
