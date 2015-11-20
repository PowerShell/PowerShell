Describe "Wait-Event" {

    Context "Validate Wait-Event is waiting for events" {
	It "Should verify Wait-Event is waiting for at least a second" {
	    $waiteventtime = Measure-Command { Wait-Event -timeout 1 }
            $waiteventtime.Seconds | Should BeGreaterThan 0
	}
    }
}
