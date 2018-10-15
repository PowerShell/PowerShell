# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Join-String" -Tags "CI" {

    BeforeAll {
        $testObject = Get-ChildItem
    }

    It "Should be called using the InputObject without error with no other switches" {
        { Join-String -InputObject $testObject } | Should -Not -Throw
    }

    It "Should return a single string" {
        $actual = $testObject | Join-String

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType System.String
    }

    It "Should join property values with default separator" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-String -Property Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values positionally with default separator" {
        $expected = $testObject.Name -join $ofs
        $actual = $testObject | Join-String Name
        $actual | Should -BeExactly $expected
    }

    It "Should join property values with custom Separator" {
        $expected = $testObject.Name -join "; "
        $actual = $testObject | Join-String -Property Name -Separator "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join property values SingleQuoted" {
        $expected = ($testObject.Name).Foreach{"'$_'"} -join "; "
        $actual = $testObject | Join-String -Property Name -Separator "; " -SingleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should join property values DoubleQuoted" {
        $expected = ($testObject.Name).Foreach{"""$_"""} -join "; "
        $actual = $testObject | Join-String -Property Name -Separator "; " -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should join property values Formatted" {
        $expected = ($testObject.Name).Foreach{"[$_]"} -join "; "
        $actual = $testObject | Join-String -Property Name -Separator "; " -Format "[{0}]"
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with default separator" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join $ofs
        $actual = $testObject | Join-String -Property $sb
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with custom separator" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb) -join "; "
        $actual = $testObject | Join-String -Property $sb -Separator "; "
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results SingleQuoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"'$_'"} -join $ofs
        $actual = $testObject | Join-String -Property $sb -SingleQuote
        $actual | Should -BeExactly $expected
    }
    It "Should join script block results DoubleQuoted" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"""$_"""} -join $ofs
        $actual = $testObject | Join-String -Property $sb -DoubleQuote
        $actual | Should -BeExactly $expected
    }

    It "Should join script block results with Format and separator" {
        $sb = {$_.Name + $_.Length}
        $expected = ($testObject | ForEach-Object $sb).Foreach{"[{0}]" -f $_} -join "; "
        $actual = $testObject | Join-String -Property $sb -Separator "; " -Format "[{0}]"
        $actual | Should -BeExactly $expected
    }

    It "Should Handle OutputPrefix and OutputSuffix" {
        $ofs = ','
        $expected = "A 1,2,3 B"
        $actual = 1..3 | Join-String -OutputPrefix "A " -OutputSuffix " B"
        $actual | Should -BeExactly $expected
    }

    It "Should handle null separator" {
        $expected = -join 'hello'.tochararray()
        $actual = "hello" | Join-String -separator $null
        $actual | Should -BeExactly $expected
    }

    It "Should tabcomplete InputObject properties" {
        $cmd = '[io.fileinfo]::new("c:\temp") | Join-String -Property '
        $res = tabexpansion2 $cmd $cmd.length
        $completionTexts = $res.CompletionMatches.CompletionText
        $Propertys = [io.fileinfo]::new($PSScriptRoot).psobject.properties.Name
        foreach ($n in $Propertys) {
            $n -in $completionTexts | Should -BeTrue
        }
    }

}
