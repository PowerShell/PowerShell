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

 	    It "Should tee the output to Verbose" {
	        $out = Write-Output teeobjecttest4  | Tee-Object -Stream Verbose -Verbose 4>&1
	        $out | Where-Object {$_ -is [System.Management.Automation.VerboseRecord]} | Select-Object -ExpandProperty Message |
                   Should -BeExactly 'teeobjecttest4'
	    }

        It "Should tee the output to Debug" {
	        $out = Write-Output teeobjecttest4  | Tee-Object -Stream Debug 5>&1 -debug
	        $out | Where-Object {$_ -is [System.Management.Automation.DebugRecord]} | Select-Object -ExpandProperty Message |
                   Should -BeExactly 'teeobjecttest4'
	    }

        It "Should tee the output to Error" {
	        $null = Write-Output teeobjecttest5  | Tee-Object -Stream Error -ErrorVariable errorOutput -ErrorAction SilentlyContinue
	        $errorOutput[0].TargetObject | Should -BeExactly 'teeobjecttest5'
	    }

        It "Should tee the output to Warning" {
	        $null = Write-Output teeobjecttest5  | Tee-Object -Stream Warning -WarningVariable warningOutput -WarningAction SilentlyContinue
	        $warningOutput[0].Message | Should -BeExactly 'teeobjecttest5'
	    }
        It "Should tee the output to Information" {
	        $null = Write-Output teeobjecttest6  | Tee-Object -Stream Information -InformationVariable informationOutput -InformationAction SilentlyContinue
	        $informationOutput[0].MessageData | Should -BeExactly 'teeobjecttest6'
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

    It "Positive Verbose Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Stream Verbose -Verbose 4>&1
        $results.Length | Should -Be 6
        $outputResults=$results | Where-Object {-not ($_ -is [System.Management.Automation.VerboseRecord])}
        $outputresults | Should -Be $expected
        $verboseResults =$results | Where-Object {$_ -is [System.Management.Automation.VerboseRecord]}
        $verboseResults.Message | Should -Be $expected
    }

    It "Positive Debug Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Stream Debug -Debug 5>&1
        $results.Length | Should -Be 6
        $outputResults=$results | Where-Object {-not ($_ -is [System.Management.Automation.DebugRecord])}
        $outputresults | Should -Be $expected
        $debugResults =$results | Where-Object {$_ -is [System.Management.Automation.DebugRecord]}
        $debugResults.Message | Should -Be $expected
    }

    It "Positive Warning Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Stream Warning -WarningVariable wa -WarningAction SilentlyContinue
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $wa.count | Should -Be 3
        $wa.Message | Should -Be $expected
    }

    It "Positive Information Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Stream Information -InformationVariable info -InformationAction SilentlyContinue
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $info.count | Should -Be 3
        $info.MessageData | Should -Be $expected
    }

    It "Positive Error Stream Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Stream Error -ErrorVariable err -ErrorAction SilentlyContinue
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $err.count | Should -Be 3
        $err.TargetObject | Should -Be $expected
    }


}
