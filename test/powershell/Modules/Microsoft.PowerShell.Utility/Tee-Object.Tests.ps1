# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Tee-Object" -Tags "CI" {

    Context "Validate Tee-Object is correctly forking output" {

	$testfile = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath testfile.txt

	    It "Should return the output to the screen and to the variable" {
	        $teefile = $testfile
	        Write-Output teeobjecttest1 | Tee-Object -variable teeresults
	        $teeresults         | Should -BeExactly "teeobjecttest1"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
	    }

	    It "Should tee the output to a file" {
	        $teefile = $testfile
	        Write-Output teeobjecttest3  | Tee-Object $teefile
	        Get-Content $teefile | Should -BeExactly "teeobjecttest3"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
	    }
    }
}

Describe "Tee-Object DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $tempFile = Join-Path $TestDrive -ChildPath "TeeObjectTestsTempFile"
    }
    It "Positive File Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -FilePath $tempFile
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $content = Get-Content $tempFile
        $content | Should -Be $expected
    }

    It "Positive File Test with Path parameter alias" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Path $tempFile
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $content = Get-Content $tempFile
        $content | Should -Be $expected
    }

    It "Positive Variable Test" {
        $expected = "1", "2", "3"
        $varName = "teeObjectTestVar"
        $results = $expected | Tee-Object -Variable $varName
        $results.Length | Should -Be 3
        $results | Should -Be $expected

        $results = Get-Variable -Name $varName -ValueOnly
        $results.Length | Should -Be 3
        $results | Should -Be $expected
    }

}
