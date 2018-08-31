# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Join-String" -Tags "CI" {

    BeforeAll {
        $testObject = Get-ChildItem
    }

    It "Should be called using an object as piped without error with no switches" {
        {$testObject | Join-String } | Should -Not -Throw
    }

    It "Should be called using the InputObject without error with no other switches" {
        { Join-String -InputObject $testObject } | Should -Not -Throw
    }

    It "Should return a single string" {
        $actual = $testObject | Join-String

        $actual.Count       | Should -Be 1
        $actual | Should -BeOfType System.String
    }

    It "Should join property values with default delimiter" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-String -Property Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values positionally with default delimiter" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-String Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values with custom delimiter" {
        $expected = $testObject.Name -join "; "
        $actual = $testObject | Join-String -Property Name -Delimiter "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join property values Quoted" {
        $expected = ($testObject.Name).Foreach{"'$_'"} -join "; "
        $actual = $testObject | Join-String -Property Name -Delimiter "; " -Quote
        $actual | Should -BeExactly $expected
    }

    It "Should join property values DoubleQuoted" {
        $expected = ($testObject.Name).Foreach{"""$_"""} -join "; "
        $actual = $testObject | Join-String -Property Name -Delimiter "; " -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with default delimiter" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join $ofs
        $actual = $testObject | Join-String -Property $sb
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with custom delimiter" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join "; "
        $actual = $testObject | Join-String -Property $sb -Delimiter "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results Quoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"'$_'"} -join $ofs
        $actual = $testObject | Join-String -Property $sb -Quote
        $actual | Should -BeExactly $expected
    }
    It "Should join script block results DoubleQuoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"""$_"""} -join $ofs
        $actual = $testObject | Join-String -Property $sb -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should Handle PreScript and PostScript" {
        $ofs = ','
        $expected = "A 1,2,3 B"
        $actual = 1..3 | Join-String -Prefix "A " -Suffix " B"
        $actual | Should -BeExactly $expected
    }

	It "Should tabcomplete InputObject properties" {
		$cmd = '[io.fileinfo]::new("c:\temp") | Join-String -Property '
		$res = tabexpansion2 $cmd $cmd.length
		$completionTexts = $res.CompletionMatches.CompletionText
		$Propertys = [io.fileinfo]::new($PSScriptRoot).psobject.properties.Name
		foreach($n in $Propertys) {
			$n -in $completionTexts | Should -BeTrue
		}
	}

}
