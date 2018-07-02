# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Remove-Event" -Tags "CI" {

    BeforeEach {
	New-Event -sourceidentifier PesterTimer  -sender Windows.timer -messagedata "PesterTestMessage"
    }

    AfterEach {
	Remove-Event -sourceidentifier PesterTimer -ErrorAction SilentlyContinue
    }

    Context "Check Remove-Event can validly remove events" {

	It "Should remove an event given a sourceidentifier" {
	    { Remove-Event -sourceidentifier PesterTimer }
	    { Get-Event -ErrorAction SilentlyContinue | Should -Not FileMatchContent PesterTimer }
	}

	It "Should remove an event given an event identifier" {
	    { $events = Get-Event -sourceidentifier PesterTimer }
	    { $events = $events.EventIdentifier }
	    { Remove-Event -EventIdentifier $events }
	    { $events = Get-Event -ErrorAction SilentlyContinue}
	    { $events.SourceIdentifier | Should -Not FileMatchContent "PesterTimer" }
	}

	It "Should be able to remove an event given a pipe from Get-Event" {
	    { Get-Event -sourceidentifier PesterTimer | Remove-Event }
	    { Get-Event -ErrorAction SilentlyContinue | Should -Not FileMatchContent "PesterTimer" }

	}

	It "Should NOT remove an event given the whatif flag" {
	    { Remove-Event -sourceidentifier PesterTimer -whatif }
	    { $events = Get-Event }
	    { $events.SourceIdentifier  | Should -FileContentMatch "PesterTimer" }
	}
    }
}
