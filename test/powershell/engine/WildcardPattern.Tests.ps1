# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WildcardPattern.ToRegex Tests" -Tags "CI" {
    Context "Basic wildcard to regex conversion" {
        It "Converts asterisk wildcard to regex" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("*.txt")
            $regex = $pattern.ToRegex()
            $regex | Should -Match "\.txt"
        }

        It "Converts question mark wildcard to regex" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("test?.log")
            $regex = $pattern.ToRegex()
            # ? converts to . (matches exactly one character), not .*
            $regex | Should -BeExactly '^test.\.log$'
        }

        It "Converts bracket expression to regex" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("file[0-9].txt")
            $regex = $pattern.ToRegex()
            $regex | Should -Match "\[0-9\]"
        }

        It "Escapes regex special characters" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("test.log")
            $regex = $pattern.ToRegex()
            $regex | Should -Match "test\\\.log"
        }
    }

    Context "Wildcard options affect regex conversion" {
        It "Respects IgnoreCase option" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("TEST", [System.Management.Automation.WildcardOptions]::IgnoreCase)
            $regex = $pattern.ToRegex()
            $regex | Should -Not -BeNullOrEmpty
        }

        It "Respects CultureInvariant option" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("test", [System.Management.Automation.WildcardOptions]::CultureInvariant)
            $regex = $pattern.ToRegex()
            $regex | Should -Not -BeNullOrEmpty
        }
    }

    Context "Regex can be used for matching" {
        It "Generated regex can match strings" {
            $wildcardPattern = [System.Management.Automation.WildcardPattern]::new("*.txt")
            $regexPattern = $wildcardPattern.ToRegex()
            $regex = [regex]::new($regexPattern)

            $regex.IsMatch("file.txt") | Should -BeTrue
            $regex.IsMatch("document.txt") | Should -BeTrue
            $regex.IsMatch("file.log") | Should -BeFalse
        }

        It "Generated regex matches same strings as WildcardPattern.IsMatch" {
            $testCases = @(
                @{ Pattern = "*.txt"; TestString = "file.txt"; Expected = $true }
                @{ Pattern = "*.txt"; TestString = "file.log"; Expected = $false }
                @{ Pattern = "test?.log"; TestString = "test1.log"; Expected = $true }
                @{ Pattern = "test?.log"; TestString = "test12.log"; Expected = $false }
                @{ Pattern = "file[0-9].txt"; TestString = "file5.txt"; Expected = $true }
                @{ Pattern = "file[0-9].txt"; TestString = "fileA.txt"; Expected = $false }
            )

            foreach ($testCase in $testCases) {
                $wildcardPattern = [System.Management.Automation.WildcardPattern]::new($testCase.Pattern)
                $regexPattern = $wildcardPattern.ToRegex()
                $regex = [regex]::new($regexPattern)

                $wildcardMatch = $wildcardPattern.IsMatch($testCase.TestString)
                $regexMatch = $regex.IsMatch($testCase.TestString)

                $wildcardMatch | Should -Be $testCase.Expected -Because "WildcardPattern should match expectation"
                $regexMatch | Should -Be $testCase.Expected -Because "Regex should match expectation"
                $wildcardMatch | Should -Be $regexMatch -Because "Wildcard and Regex should match the same"
            }
        }
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

    Context "Complex patterns" {
        It "Handles multiple wildcards" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("*test*file*.txt")
            $regex = $pattern.ToRegex()
            $regex | Should -Not -BeNullOrEmpty

            $regexObj = [regex]::new($regex)
            $regexObj.IsMatch("mytestmyfile123.txt") | Should -BeTrue
        }

        It "Handles pattern with multiple bracket expressions" {
            $pattern = [System.Management.Automation.WildcardPattern]::new("file[0-9][a-z].txt")
            $regex = $pattern.ToRegex()
            $regex | Should -Not -BeNullOrEmpty

            $regexObj = [regex]::new($regex)
            $regexObj.IsMatch("file5a.txt") | Should -BeTrue
            $regexObj.IsMatch("file55.txt") | Should -BeFalse
        }
    }
}
