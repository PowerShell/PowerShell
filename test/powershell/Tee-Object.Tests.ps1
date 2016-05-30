Describe "Tee-Object" {

    Context "Validate Tee-Object is correctly forking output" {

	$testfile = Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath testfile.txt

	    It "Should return the output to the screen and to the variable" {
	        $teefile = $testfile
	        echo teeobjecttest1 | Tee-Object -variable teeresults
	        $teeresults         | Should Be "teeobjecttest1"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
	    }

	    It "Should tee the output to a file" {
	        $teefile = $testfile
	        echo teeobjecttest3  | Tee-Object $teefile
	        Get-Content $teefile | Should Be "teeobjecttest3"
	        Remove-Item $teefile -ErrorAction SilentlyContinue
	    }
    }
}

Describe "Tee-Object DRT Unit Tests" -Tags DRT {
    $tempDirectory = Join-Path $TestDrive -ChildPath "TeeObjectTestsTempDir"
    $tempFile = "TeeObjectTestsTempFile"
    New-Item $tempDirectory -ItemType Directory -Force

    It "Positive File Test" {
        $expected = "1", "2", "3"
        $filePath = Join-Path $tempDirectory -ChildPath $tempFile
        $results = $expected | Tee-Object -FilePath $filePath
        $results.Length | Should be 3
        $results | Should Be $expected
        $content = Get-Content $filePath
        $content | Should Be $expected
    }

    It "Positive Var Test" {
        $expected = "1", "2", "3" 
        $varName = "teeObjectTestVar"
        $results = $expected | Tee-Object -Variable $varName
        $results.Length | Should be 3
        $results | Should Be $expected
        
        $results = Get-Variable -Name $varName -ValueOnly
        $results.Length | Should be 3
        $results | Should Be $expected
    }

}