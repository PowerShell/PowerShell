# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Invoke-Expression" -Tags "CI" {

    Context "Should execute the invoked command validly" {

	It "Should return the echoed text" {
	    (Invoke-Expression -command "echo pestertest1") | Should -BeExactly "pestertest1"
	}

	It "Should return the echoed text from a script" {
	    $testfile = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath echoscript.ps1
	    $testcommand = "echo pestertestscript"
	    $testcommand | Add-Content -Path "$testfile"
	    (Invoke-Expression "& '$testfile'") | Should -BeExactly "pestertestscript"
	    Remove-Item "$testfile"
	}
    }
}
Describe "Invoke-Expression DRT Unit Tests" -Tags "CI" {
	It "Invoke-Expression should work"{
		$result=invoke-expression -Command 2+2
		$result|Should -Be 4
	}
}
