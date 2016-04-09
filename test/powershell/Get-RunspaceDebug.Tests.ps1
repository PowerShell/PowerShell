Describe "Get-RunspaceDebug" {

    Context "Check return types of RunspaceDebug" {

	It "Should return Microsoft.Powershell.Commands.PSRunspaceDebug as the return type" {
	    $rs = Get-RunspaceDebug
	    $rs[0].GetType().Name | Should Be PSRunspaceDebug
	}
    }
}
