Describe "Add-String Unit Tests" -Tag "CI" {
	It "Should have the designed for parameters & sets" {
		(Get-Command Add-String).ParameterSets.Name | Should -Be "wrap", "padLeft", "padRight"
		(Get-Command Add-String).Parameters.Keys | Should -Be "PadLeft", "PadRight", "PadWidth", "Before", "Behind", "InputString", "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ErrorVariable", "WarningVariable", "InformationVariable", "OutVariable", "OutBuffer", "PipelineVariable"
	}
	
	It "Should properly add strings in the 'wrap' parameterset" {
		"abc" | Add-String "o" "u" | Should -Be "oabcu"
		"abc" | Add-String "o" | Should -Be "oabc"
		"abc" | Add-String "" "u" | Should -Be "abcu"
		"abc" | Add-String "o" "u" | Should -Be "oabcu"
		"abc" | Add-String -Before "o" -Behind "u" | Should -Be "oabcu"
	}
	
	It "Should properly pad strings in the 'padLeft'/'padRight' parametersets" {
		"abc" | Add-String -PadLeft " " -PadWidth 8 | Should -Be "     abc"
		"abc" | Add-String -PadRight " " -PadWidth 8 | Should -Be "abc     "
	}
}