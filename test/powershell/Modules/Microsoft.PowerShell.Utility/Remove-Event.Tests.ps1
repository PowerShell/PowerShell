# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Remove-Event" -Tags "CI" {

    BeforeEach {
	New-Event -SourceIdentifier PesterTimer  -Sender Windows.timer -MessageData "PesterTestMessage"
    }

    AfterEach {
	Remove-Event -SourceIdentifier PesterTimer -ErrorAction SilentlyContinue
    }

    Context "Check Remove-Event can validly remove events" {

	It "Should remove an event given a sourceidentifier" {
	    { Remove-Event -SourceIdentifier PesterTimer }
	    { Get-Event -ErrorAction SilentlyContinue | Should -Not FileMatchContent PesterTimer }
	}

	It "Should remove an event given an event identifier" {
	    { $events = Get-Event -SourceIdentifier PesterTimer }
	    { $events = $events.EventIdentifier }
	    { Remove-Event -EventIdentifier $events }
	    { $events = Get-Event -ErrorAction SilentlyContinue}
	    { $events.SourceIdentifier | Should -Not FileMatchContent "PesterTimer" }
	}

	It "Should be able to remove an event given a pipe from Get-Event" {
	    { Get-Event -SourceIdentifier PesterTimer | Remove-Event }
	    { Get-Event -ErrorAction SilentlyContinue | Should -Not FileMatchContent "PesterTimer" }

	}

	It "Should NOT remove an event given the whatif flag" {
	    { Remove-Event -SourceIdentifier PesterTimer -WhatIf }
	    { $events = Get-Event }
	    { $events.SourceIdentifier  | Should -FileContentMatch "PesterTimer" }
	}
    }
}
