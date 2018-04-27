Describe "Join-String Unit Tests" -Tag "CI" {
	It "Should have the designed for parameters & sets" {
		(Get-Command Join-String).ParameterSets.Name | Should -Be "__AllParameterSets"
		(Get-Command Join-String).Parameters.Keys | Should -Be "Separator", "Count", "InputString", "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ErrorVariable", "WarningVariable", "InformationVariable", "OutVariable", "OutBuffer", "PipelineVariable"
	}
	
	It "Should properly join strings" {
		1 .. 4 | Join-String "." | Should -Be "1.2.3.4"
		1 .. 4 | Join-String "__" | Should -Be "1__2__3__4"
		1 .. 4 | Join-String "." -Count 2 | Should -Be "1.2", "3.4"
	}
}