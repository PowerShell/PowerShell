Describe "Start-Sleep DRT Unit Tests" -Tags "CI" {
    It "Should work properly when sleeping with Second" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Seconds 1
        $watch.Stop()
        $watch.ElapsedMilliseconds -ge 999 | Should be $true
    }

    It "Should work properly when sleeping with Milliseconds" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Milliseconds 1000
        $watch.Stop()
        $watch.ElapsedMilliseconds -ge 999 | Should be $true
    }

}

Describe "Start-Sleep" -Tags "CI" {

    Context "Validate Start-Sleep works properly" {
	It "Should only sleep for at least 1 second" {
	    $result = Measure-Command { Start-Sleep -s 1 }
	    $result.TotalSeconds | Should BeGreaterThan 0.25
	}
    }
}
