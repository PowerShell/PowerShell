# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Event" -Tags "CI" {

    BeforeEach {
	( New-Event -SourceIdentifier PesterTestEvent -Sender Windows.timer -MessageData "PesterTestMessage" )
    }

    AfterEach {
	( Remove-Event -SourceIdentifier PesterTestEvent -ErrorAction SilentlyContinue )
    }

    Context "Check return type of Get-Event" {

	It "Should return PSEventArgs as return type of Get-Event" {
	    Get-Event -SourceIdentifier PesterTestEvent | Should -BeOfType System.Management.Automation.PSEventArgs
	}
    }

    Context "Validate Get-Event can retrieve event" {

	It "Should return at least 1 event" {
	    { (Get-Event).Count | Should -BeGreaterThan 0 }
	}

	It "Should return PesterTestMessage as the MessageData" {
	    { (Get-Event -SourceIdentifier PesterTimer).MessageData  | Should -BeExactly "PesterTestMessage" }
	}

	It "Should return Sender as Windows.timer" {
	    { (Get-Event -SourceIdentifier PesterTimer).Sender  | Should -Be Windows.timer }
	}

    }
}
