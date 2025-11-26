# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WildcardPattern.ToRegex Tests" -Tags "CI" {
    It "Converts '<Pattern>' to '<Expected>'" -TestCases @(
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
        $wildcardPattern.ToRegex() | Should -BeExactly $Expected
    }

    It "Converts '<Pattern>' with <OptionName> option" -TestCases @(
        @{ Pattern = 'TEST'; OptionName = 'IgnoreCase'; Option = [System.Management.Automation.WildcardOptions]::IgnoreCase; Expected = '^TEST$' }
        @{ Pattern = 'test'; OptionName = 'CultureInvariant'; Option = [System.Management.Automation.WildcardOptions]::CultureInvariant; Expected = '^test$' }
    ) {
        param($Pattern, $OptionName, $Option, $Expected)
        $wildcardPattern = [System.Management.Automation.WildcardPattern]::new($Pattern, $Option)
        $wildcardPattern.ToRegex() | Should -BeExactly $Expected
    }

    It "Regex from '<Pattern>' matches '<TestString>': <ShouldMatch>" -TestCases @(
        @{ Pattern = '*test*file*.txt'; TestString = 'mytestmyfile123.txt'; ShouldMatch = $true }
        @{ Pattern = 'file[0-9][a-z].txt'; TestString = 'file5a.txt'; ShouldMatch = $true }
        @{ Pattern = 'file[0-9][a-z].txt'; TestString = 'file55.txt'; ShouldMatch = $false }
    ) {
        param($Pattern, $TestString, $ShouldMatch)
        $regex = [System.Management.Automation.WildcardPattern]::new($Pattern).ToRegex()
        [regex]::new($regex).IsMatch($TestString) | Should -Be $ShouldMatch
    }

    Context "Edge cases" {
        It "Handles empty pattern" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("")
            $regex = $pattern.ToRegex()
            $regex | Should -Be "^$"
        }

        It "Handles pattern with only asterisk" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("*")
            $regex = $pattern.ToRegex()
            # Pattern with just * returns empty string (for backward compatibility)
            # but empty regex pattern matches everything
            $regex | Should -BeExactly ""
            $regexObj = [regex]::new($regex)
            $regexObj.IsMatch("anything") | Should -BeTrue
            $regexObj.IsMatch("") | Should -BeTrue
        }

        It "Handles escaped wildcard characters" {
            # Use single quotes to preserve the backtick escape character
            $pattern = [System.Management.Automation.WildcardPattern]::new('file`*.txt')
            $regex = $pattern.ToRegex()
            # The backtick-escaped asterisk should become a literal asterisk in the regex (escaped as \*)
            $regex | Should -BeExactly '^file\*\.txt$'
        }
    }
}
