# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Split-String Unit Tests" -Tag "CI" {
	It "Should have the designed for parameters & sets" {
		(Get-Command Split-String).ParameterSets.Name | Should -Be "regex", "simple"
		(Get-Command Split-String).Parameters.Keys | Should -Be "Separator", "DoNotUseRegex", "Options", "Count", "InputString", "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ErrorVariable", "WarningVariable", "InformationVariable", "OutVariable", "OutBuffer", "PipelineVariable"
	}
	
	It "Should properly replace in strings" {
		"abc,def" | Split-String "," | Should -Be "abc", "def"
		"abc,def" | Split-String "\W" | Should -Be "abc", "def"
		"abc,def" | Split-String "\W" -DoNotUseRegex | Should -Be "abc,def"
		"abc (def) ghi" | Split-String "(def)" | Should -Be "abc (", "def", ") ghi"
		"abc (def) ghi" | Split-String "(def)" -DoNotUseRegex | Should -Be "abc ", " ghi"
		"aBcbd" | Split-String "b" | Should -Be "a", "c", "d"
		"aBcbd" | Split-String "b" -Options None | Should -Be "aBc", "d"
		"a,b,c,d,e" | Split-String "," -Count 2 | Should -Be "a", "b,c,d,e"
		"a,b,c,d,e" | Split-String "," -Count 4 | Should -Be "a", "b", "c", "d,e"
		"a,b,c,d,e" | Split-String "," -Count 2 -DoNotUseRegex | Should -Be "a", "b,c,d,e"
		"a,b,c,d,e" | Split-String "," -Count 4 -DoNotUseRegex | Should -Be "a", "b", "c", "d,e"
	}
}