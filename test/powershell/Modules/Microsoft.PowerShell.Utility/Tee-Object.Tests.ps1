# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tee-Object" -Tags "CI" {

    Context "Validate Tee-Object is correctly forking output" {

	$testfile = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath testfile.txt

	    It "Should return the output to the screen and to the variable" {
	        $teefile = $testfile
	        Write-Output teeobjecttest1 | Tee-Object -Variable teeresults
	        $teeresults         | Should -BeExactly "teeobjecttest1"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
	    }

	    It "Should tee the output to a file" {
	        $teefile = $testfile
	        Write-Output teeobjecttest3  | Tee-Object $teefile
	        Get-Content $teefile | Should -BeExactly "teeobjecttest3"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
        }

        It "Should tee output to file using selected encoding" -TestCases @(
            @{ Encoding = "ascii" },
            @{ Encoding = "bigendianunicode" },
            @{ Encoding = "default" },
            @{ Encoding = "latin1" },
            @{ Encoding = "unicode" },
            @{ Encoding = "utf32" },
            @{ Encoding = "utf7" },
            @{ Encoding = "utf8" }
        ) {
            param($Encoding)
            $contentSets =
                @(@('a1','aa2','aaa3','aaaa4','aaaaa5'), # utf-8
                @('€1','€€2','€€€3','€€€€4','€€€€€5'), # utf-16
                @('𐍈1','𐍈𐍈2','𐍈𐍈𐍈3','𐍈𐍈𐍈𐍈4','𐍈𐍈𐍈𐍈𐍈5')) # utf-32
            ForEach ($content in $contentSets){
                $teefile = $testfile
                Write-Output -InputObject content  | Tee-Object -FilePath $teefile -Encoding $Encoding
                Get-Content -Path $teefile -Encoding $Encoding | Should -BeExactly "teeobjecttest3"
                Remove-Item -Path $teefile -ErrorAction SilentlyContinue
            }
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
