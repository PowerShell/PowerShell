# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WildcardPattern.ToRegex Tests" -Tags "CI" {
    It "Converts '<Pattern>' to regex pattern '<Expected>'" -TestCases @(
        @{ Pattern = '*.txt'; Expected = '\.txt$' }
        @{ Pattern = 'test?.log'; Expected = '^test.\.log$' }
        @{ Pattern = 'file[0-9].txt'; Expected = '^file[0-9]\.txt$' }
        @{ Pattern = 'test.log'; Expected = '^test\.log$' }
        @{ Pattern = '*test*file*.txt'; Expected = 'test.*file.*\.txt$' }
        @{ Pattern = 'file[0-9][a-z].txt'; Expected = '^file[0-9][a-z]\.txt$' }
        @{ Pattern = 'test*'; Expected = '^test' }
        @{ Pattern = '*test*'; Expected = 'test' }
    ) {
        param($Pattern, $Expected)
        $wildcardPattern = [System.Management.Automation.WildcardPattern]::new($Pattern)
        $regex = $wildcardPattern.ToRegex()
        $regex | Should -BeOfType ([regex])
        $regex.ToString() | Should -BeExactly $Expected
    }

    It "Converts '<Pattern>' with <OptionName> option" -TestCases @(
        @{ Pattern = 'TEST'; OptionName = 'IgnoreCase'; Option = [System.Management.Automation.WildcardOptions]::IgnoreCase; Expected = '^TEST$'; ExpectedRegexOptions = 'IgnoreCase, Singleline'; TestString = 'test'; ExpectedMatch = $true }
        @{ Pattern = 'test'; OptionName = 'CultureInvariant'; Option = [System.Management.Automation.WildcardOptions]::CultureInvariant; Expected = '^test$'; ExpectedRegexOptions = 'Singleline, CultureInvariant'; TestString = 'test'; ExpectedMatch = $true }
        @{ Pattern = 'test*'; OptionName = 'Compiled'; Option = [System.Management.Automation.WildcardOptions]::Compiled; Expected = '^test'; ExpectedRegexOptions = 'Compiled, Singleline'; TestString = 'testing'; ExpectedMatch = $true }
    ) {
        param($Pattern, $OptionName, $Option, $Expected, $ExpectedRegexOptions, $TestString, $ExpectedMatch)
        $wildcardPattern = [System.Management.Automation.WildcardPattern]::new($Pattern, $Option)
        $regex = $wildcardPattern.ToRegex()
        $regex | Should -BeOfType ([regex])
        $regex.ToString() | Should -BeExactly $Expected
        $regex.Options.ToString() | Should -BeExactly $ExpectedRegexOptions
        $regex.IsMatch($TestString) | Should -Be $ExpectedMatch
    }

    It "Regex from '<Pattern>' matches '<TestString>': <ShouldMatch>" -TestCases @(
        @{ Pattern = '*test*file*.txt'; TestString = 'mytestmyfile123.txt'; ShouldMatch = $true }
        @{ Pattern = 'file[0-9][a-z].txt'; TestString = 'file5a.txt'; ShouldMatch = $true }
        @{ Pattern = 'file[0-9][a-z].txt'; TestString = 'file55.txt'; ShouldMatch = $false }
    ) {
        param($Pattern, $TestString, $ShouldMatch)
        $regex = [System.Management.Automation.WildcardPattern]::new($Pattern).ToRegex()
        $regex.IsMatch($TestString) | Should -Be $ShouldMatch
    }

    Context "Edge cases" {
        It "Handles empty pattern" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("")
            $regex = $pattern.ToRegex()
            $regex | Should -BeOfType ([regex])
            $regex.ToString() | Should -Be "^$"
        }

        It "Handles pattern with only asterisk" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("*")
            $regex = $pattern.ToRegex()
            $regex | Should -BeOfType ([regex])
            $regex.ToString() | Should -BeExactly ""
            $regex.IsMatch("anything") | Should -BeTrue
            $regex.IsMatch("") | Should -BeTrue
        }

        It "Handles escaped '<Char>' wildcard character" -TestCases @(
            @{ Char = '*'; Pattern = 'file`*.txt'; Expected = '^file\*\.txt$' }
            @{ Char = '?'; Pattern = 'file`?.txt'; Expected = '^file\?\.txt$' }
            @{ Char = '['; Pattern = 'file`[.txt'; Expected = '^file\[\.txt$' }
            @{ Char = ']'; Pattern = 'file`].txt'; Expected = '^file]\.txt$' }
        ) {
            param($Char, $Pattern, $Expected)
            $wildcardPattern = [System.Management.Automation.WildcardPattern]::new($Pattern)
            $regex = $wildcardPattern.ToRegex()
            $regex | Should -BeOfType ([regex])
            $regex.ToString() | Should -BeExactly $Expected
        }

    }
}
