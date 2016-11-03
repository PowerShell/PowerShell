Describe "Get-Uptime" -Tags "CI" {
    It "Get-Uptime return timespan" {
        $upt = Get-Uptime
    	$upt | Should Not Be $null
    	($upt).Gettype().Name | Should Be "Timespan"
    }
}
