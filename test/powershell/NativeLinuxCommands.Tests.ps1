Describe "NativeLinuxCommands" {
    It "Should return a type of System.Object for hostname cmdlet" {
    (hostname).GetType().BaseType | Should Be 'System.Object'
	(hostname).GetType().Name | Should Be String
    }

    It "Should have not empty Name flags set for ps object" {
	ps | foreach-object { $_.ProcessName | Should Not BeNullOrEmpty }
    }
	
	It "Should be able to call using the touch, cat, rm cmdlet" {
	{ touch /NativeLinuxCommandsTestFile } | Should Not Throw
	{ cat /NativeLinuxCommandsTestFile } | Should Not Throw
	{ rm /NativeLinuxCommandsTestFile } | Should Not Throw
    }
	
	It "Should be able to call using the mkdir, ls, rm cmdlet" {
	{ mkdir /NativeLinuxCommandsTestFolder } | Should Not Throw
	{ ls /NativeLinuxCommandsTestFolder } | Should Not Throw
	{ rm /NativeLinuxCommandsTestFolder } | Should Not Throw
    }
}
