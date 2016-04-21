<#
  [DRT] for Win7:499005	- Using invalid new computer name in Rename-Computer will cause all subsequent Rename/Remove-Computer cmdlet to fail 
  
  Ported to Pester from TTest https://github.com/PowerShell/psl-monad/tree/master/monad/tests/monad/DRT/commands/management/UnitTests/Computer.cs
#>

Describe "Rename-Computer" {
    Context "Check Rename-Computer behavior" {
	  It "Should throw if 'NewName' is null or empty" -Pending:($IsLinux -Or $IsOSX) {
	    { Rename-Computer -NewName $null} | Should Throw "Cannot validate argument on parameter 'NewName'. The argument is null or empty."
	  }
	  
      It "Should throw if 'NewName' is the same as the 'OldName'" -Pending:($IsLinux -Or $IsOSX) {
	    { Rename-Computer -NewName $env:COMPUTERNAME -ea stop} | Should Throw ("Skip computer '"+$env:COMPUTERNAME+"' with new name '"+$env:COMPUTERNAME)
	  }

	  It "Should throw if 'NewName' contains invalid chars" -Pending:($IsLinux -Or $IsOSX) {
	    { Rename-Computer -NewName "a.invA?lid.com" -ea stop} | Should Throw "Standard names may contain letters (a-z, A-Z), numbers (0-9), and hyphens (-), but no spaces or periods"
	  }
	  	  
      It "Should throw if 'NewName' contains a period" -Pending:($IsLinux -Or $IsOSX) {
	    { Rename-Computer -NewName "a." -ea stop} | Should Throw "Standard names may contain letters (a-z, A-Z), numbers (0-9), and hyphens (-), but no spaces or periods"
	  }
  }
}
