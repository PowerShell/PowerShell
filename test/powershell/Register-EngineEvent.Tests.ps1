Describe "Register-EngineEvent" {

    Context "Check return type of Register-ObjectEvent" {
	It "Should return System.Management.Automation.PSEventJob as return type of Register-EngineEvent" {
	    ( Register-EngineEvent -SourceIdentifier PesterTestRegister -Action {echo registerengineevent} ).GetType() | Should Be System.Management.Automation.PSEventJob
	    Unregister-Event -sourceidentifier PesterTestRegister
	}
    }
}
