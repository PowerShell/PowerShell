# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Join-Object" -Tags "CI" {

    BeforeAll {
        $testObject = Get-ChildItem
    }

    It "Should be called using an object as piped without error with no switches" {
        {$testObject | Join-Object } | Should -Not -Throw
    }

    It "Should be called using the InputObject without error with no other switches" {
        { Join-Object -InputObject $testObject } | Should -Not -Throw
    }

    It "Should return a single string" {
        $actual = $testObject | Join-Object

        $actual.Count       | Should -Be 1
        $actual | Should -BeOfType System.String
    }

    It "Should join property values with default delimiter" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-Object -PropertyName Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values positionally with default delimiter" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-Object Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values with custom delimiter" {
        $expected = $testObject.Name -join "; "
        $actual = $testObject | Join-Object -PropertyName Name -Delimiter "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join property values Quoted" {
        $expected = ($testObject.Name).Foreach{"'$_'"} -join "; "
        $actual = $testObject | Join-Object -PropertyName Name -Delimiter "; " -Quote
        $actual | Should -BeExactly $expected
    }

    It "Should join property values DoubleQuoted" {
        $expected = ($testObject.Name).Foreach{"""$_"""} -join "; "
        $actual = $testObject | Join-Object -PropertyName Name -Delimiter "; " -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with default delimiter" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join $ofs
        $actual = $testObject | Join-Object -PropertyName $sb
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with custom delimiter" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join "; "
        $actual = $testObject | Join-Object -PropertyName $sb -Delimiter "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results Quoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"'$_'"} -join $ofs
        $actual = $testObject | Join-Object -PropertyName $sb -Quote
        $actual | Should -BeExactly $expected
    }
    It "Should join script block results DoubleQuoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"""$_"""} -join $ofs
        $actual = $testObject | Join-Object -PropertyName $sb -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should Handle PreScript and PostScript" {
        $ofs = ','
        $expected = "A 1,2,3 B"
        $actual = 1..3 | Join-Object -prescript "A " -PostScript " B"
        $actual | Should -BeExactly $expected
    }
}
