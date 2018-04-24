Describe "Get-SubString Unit Tests" -Tag "CI" {
	It "Should have the designed for parameters & sets" {
		(Get-Command Get-SubString).ParameterSets.Name | Should -Be "substring", "trim", "trimpartial"
		(Get-Command Get-SubString).Parameters.Keys | Should -Be "Trim", "TrimStart", "TrimEnd", "Start", "Length", "InputString", "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ErrorVariable", "WarningVariable", "InformationVariable", "OutVariable", "OutBuffer", "PipelineVariable"
	}
	
	It "Should properly select strings" {
		"abcdefghijklmno" | Get-SubString 2 5 | Should -Be "cdefg"
		"abcdefghijklmno" | Get-SubString -Trim abcmno | Should -Be "defghijkl"
		"abcdefghijklmno" | Get-SubString -TrimStart abcm | Should -Be "defghijklmno"
		"abcdefghijklmno" | Get-SubString -TrimEnd abno | Should -Be "abcdefghijklm"
		"abcdefghijklmno" | Get-SubString -TrimStart abcm -TrimEnd dmno | Should -Be "defghijkl"
	}
}