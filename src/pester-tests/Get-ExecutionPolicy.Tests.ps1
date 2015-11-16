Describe "Get-ExecutionPolicy"{

    Context "Check return type of Get-ExecutionPolicy"{

	It "Should return Microsoft.Powershell.ExecutionPolicy PSObject"{
	    (Get-ExecutionPolicy).GetType() | Should Be Microsoft.Powershell.ExecutionPolicy	
	}

    } 
}
