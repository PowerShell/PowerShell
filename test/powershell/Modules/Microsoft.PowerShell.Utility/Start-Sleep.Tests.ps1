Describe "Start-Sleep DRT Unit Tests" -Tags DRT{
    It "Should be works properly when sleeping with Second" {
        $dtStart = [DateTime]::Now
        Start-Sleep -Seconds 1
        $dtEnd = [DateTime]::Now
        $millseconds = (New-TimeSpan -Start $dtStart -End $dtEnd).TotalMilliseconds
        $millseconds | Should BeGreaterThan 1000
    }

    It "Should be works properly when sleeping with Milliseconds" {
        $dtStart = [DateTime]::Now
        Start-Sleep -Milliseconds 1000
        $dtEnd = [DateTime]::Now
        $millseconds = (New-TimeSpan -Start $dtStart -End $dtEnd).TotalMilliseconds
        $millseconds | Should BeGreaterThan 1000
    }

}

Describe "Start-Sleep" {

    Context "Validate Start-Sleep works properly" {
	It "Should only sleep for at least 1 second" {
	    $result = Measure-Command { Start-Sleep -s 1 }
	    $result.TotalSeconds | Should BeGreaterThan 0.25
	}
    }
}
