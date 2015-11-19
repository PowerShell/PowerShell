Describe "Get-RunspaceDebug" {

    Context "Check return types of RunspaceDebug" {

        It "Should return Microsoft.Powershell.Commands.PSRunspaceDebug as the return type" {
            (Get-RunspaceDebug).GetType() | Should Be Microsoft.Powershell.Commands.PSRunspaceDebug
        }    
    }
}
