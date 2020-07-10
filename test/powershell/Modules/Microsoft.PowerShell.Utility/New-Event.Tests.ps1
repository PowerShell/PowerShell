# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-Event" -Tags "CI" {

    Context "Check return type of New-Event" {

        It "Should return PSEventArgs as return type of New-Event" {
            New-Event -SourceIdentifier a | Should -BeOfType System.Management.Automation.PSEventArgs
        }
    }

    Context "Check New-Event can register an event"{
	It "Should return PesterTestMessage as the MessageData" {
	    (New-Event -SourceIdentifier PesterTimer -Sender Windows.timer -MessageData "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).MessageData  | Should -BeExactly "PesterTestMessage"
	    Remove-Event -SourceIdentifier PesterTimer
	}

	It "Should return Sender as Windows.timer" {
	    (New-Event -SourceIdentifier PesterTimer -Sender Windows.timer -MessageData "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).Sender  | Should -Be Windows.timer
	    Remove-Event -SourceIdentifier PesterTimer
	}
    }
}
