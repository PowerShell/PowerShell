# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Wait-Event" -Tags "CI" {
    Context "Validate Wait-Event is waiting for events" {
		It "Should time out when it does not receive a FakeEvent" {
			# Don't depend on Measure-Command
			$stopwatch = [System.Diagnostics.Stopwatch]::startNew()
			# Testing the timeout, so wait for an event that will never be
			# raised because it is fake
			Wait-Event -Timeout 1 -SourceIdentifier "FakeEvent"
			$stopwatch.Stop()
			$stopwatch.ElapsedMilliseconds | Should -BeGreaterThan 500
			$stopwatch.ElapsedMilliseconds | Should -BeLessThan 1500
		}

		It "Should be able to wait for an arbitrary -TimeSpan" {
			# Don't depend on Measure-Command
			$stopwatch = [System.Diagnostics.Stopwatch]::startNew()
			# Testing the timeout, so wait for an event that will never be
			# raised because it is fake
			Wait-Event -TimeSpan ([Timespan]'00:00:00.25') -SourceIdentifier "FakeEvent"
			$stopwatch.Stop()
			$stopwatch.ElapsedMilliseconds | Should -BeGreaterThan 200
			$stopwatch.ElapsedMilliseconds | Should -BeLessThan 300
		}
	}


}
