# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Set-String Unit Tests" -Tag "CI" {
	It "Should have the designed for parameters & sets" {
		(Get-Command Set-String).ParameterSets.Name | Should -Be "regex", "simple"
		(Get-Command Set-String).Parameters.Keys | Should -Be "OldValue", "NewValue", "DoNotUseRegex", "Options", "InputString", "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ErrorVariable", "WarningVariable", "InformationVariable", "OutVariable", "OutBuffer", "PipelineVariable"
	}
	
	It "Should properly replace in strings" {
		"abc def ghi" | Set-String "def" "ddd" | Should -Be "abc ddd ghi"
		"abc def ghi" | Set-String "d\w+" "ddd" | Should -Be "abc ddd ghi"
		"abc def ghi" | Set-String "(d)\w+" { $_.Groups[1].Value + "zz" } | Should -Be "abc dzz ghi"
		"abc def ghi" | Set-String "(d)\w+" '$1zz' | Should -Be "abc dzz ghi"
		"abc (def) ghi" | Set-String "(def)" "def" | Should -Be "abc (def) ghi"
		"abc (def) ghi" | Set-String "(def)" "def" -Simple | Should -Be "abc def ghi"
		"AaBb" | Set-String "[AB]" "z" | Should -Be "zzzz"
		"AaBb" | Set-String "[AB]" "z" -Options None | Should -Be "zazb"
	}
}