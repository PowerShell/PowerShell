Describe "New-Event" -Tags "CI" {

    Context "Check return type of New-Event" {

        It "Should return PSEventArgs as return type of New-Event" {
            New-Event -SourceIdentifier a | Should BeOfType System.Management.Automation.PSEventArgs
        }
    }

    Context "Check New-Event can register an event"{
	It "Should return PesterTestMessage as the MessageData" {
	    (New-Event -sourceidentifier PesterTimer -sender Windows.timer -messagedata "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).MessageData  | Should Be "PesterTestMessage"
	    Remove-Event -sourceidentifier PesterTimer
	}

	It "Should return Sender as Windows.timer" {
	    (New-Event -sourceidentifier PesterTimer -sender Windows.timer -messagedata "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).Sender  | Should be Windows.timer
	    Remove-Event -sourceIdentifier PesterTimer
	}
    }
}
