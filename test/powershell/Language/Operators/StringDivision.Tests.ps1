# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "String Division Operator Tests" -Tags "CI" {

    Context "Basic String Division" {
        It "Should divide string 'hello world' / 2 into two equal parts" {
            $result = 'hello world' / 2
            $result.Count | Should -Be 2
            $result[0] | Should -Be "hello "
            $result[1] | Should -Be "world"
        }

        It "Should divide string 'abcdefghijkl' / 3 into three equal parts" {
            $result = 'abcdefghijkl' / 3
            $result.Count | Should -Be 3
            $result[0] | Should -Be "abcd"
            $result[1] | Should -Be "efgh"
            $result[2] | Should -Be "ijkl"
        }

        It "Should handle uneven division 'abcdefg' / 3 correctly" {
            $result = 'abcdefg' / 3
            $result.Count | Should -Be 3
            $result[0] | Should -Be "abc"  # First group gets extra character
            $result[1] | Should -Be "de"
            $result[2] | Should -Be "fg"
        }

        It "Should handle division by 1 returning original string" {
            $result = 'hello' / 1
            $result.Count | Should -Be 1
            $result[0] | Should -Be "hello"
        }

        It "Should handle long string division" {
            $longString = "The quick brown fox jumps over the lazy dog"
            $result = $longString / 4
            $result.Count | Should -Be 4
            $result[0].Length | Should -Be 11  # First parts get extra characters
            $result[1].Length | Should -Be 11
            $result[2].Length | Should -Be 11
            $result[3].Length | Should -Be 10
            ($result -join '') | Should -Be $longString
        }
    }

    Context "String Division with Custom Sizes" {
        It "Should divide string 'abcdefghijklmno' / @(3,4,2) with custom sizes" {
            $result = 'abcdefghijklmno' / @(3,4,2)
            $result.Count | Should -Be 4  # 3 specified groups + 1 remainder
            $result[0] | Should -Be "abc"
            $result[1] | Should -Be "defg"
            $result[2] | Should -Be "hi"
            $result[3] | Should -Be "jklmno"  # Remainder
        }

        It "Should divide string 'abcdef' / @(3,3) exactly with no remainder" {
            $result = 'abcdef' / @(3,3)
            $result.Count | Should -Be 2
            $result[0] | Should -Be "abc"
            $result[1] | Should -Be "def"
        }

        It "Should handle string division with sizes larger than remaining characters" {
            $result = 'abc' / @(5)
            $result.Count | Should -Be 1
            $result[0] | Should -Be "abc"  # Gets all remaining characters
        }

        It "Should handle multiple custom sizes with exact fit" {
            $result = 'abcdefghij' / @(2,3,2,3)
            $result.Count | Should -Be 4
            $result[0] | Should -Be "ab"
            $result[1] | Should -Be "cde"
            $result[2] | Should -Be "fg"
            $result[3] | Should -Be "hij"
        }
    }

    Context "Special Characters and Unicode" {
        It "Should handle strings with spaces and punctuation" {
            $result = 'Hello, World! How are you?' / 3
            $result.Count | Should -Be 3
            ($result -join '') | Should -Be 'Hello, World! How are you?'
        }

        It "Should handle strings with newlines and tabs" {
            $result = "Line1`nLine2`tTabbed" / 2
            $result.Count | Should -Be 2
            ($result -join '') | Should -Be "Line1`nLine2`tTabbed"
        }

        It "Should handle Unicode characters" {
            $result = 'αβγδεζ' / 2
            $result.Count | Should -Be 2
            $result[0] | Should -Be "αβγ"
            $result[1] | Should -Be "δεζ"
        }

        It "Should handle emoji characters" {
            $result = '😀😃😄😁😆😅' / 3
            $result.Count | Should -Be 3
            $result[0] | Should -Be "😀😃"
            $result[1] | Should -Be "😄😁"
            $result[2] | Should -Be "😆😅"
        }
    }

    Context "Edge Cases" {
        It "Should handle empty string division" {
            $result = '' / 2
            $result.Count | Should -Be 2
            $result[0] | Should -Be ""
            $result[1] | Should -Be ""
        }

        It "Should handle single character string division" {
            $result = 'a' / 2
            $result.Count | Should -Be 2
            $result[0] | Should -Be "a"
            $result[1] | Should -Be ""
        }

        It "Should handle whitespace-only string" {
            $result = '   ' / 3
            $result.Count | Should -Be 3
            $result[0] | Should -Be " "
            $result[1] | Should -Be " "
            $result[2] | Should -Be " "
        }

        It "Should throw error for zero divisor" {
            { 'hello' / 0 } | Should -Throw "*positive divisor*"
        }

        It "Should throw error for negative divisor" {
            { 'hello' / -1 } | Should -Throw "*positive divisor*"
        }

        It "Should throw error for empty array divisor" {
            { 'hello' / @() } | Should -Throw "*positive integer sizes*"
        }

        It "Should throw error for array divisor with non-positive values" {
            { 'hello' / @(2, 0, 1) } | Should -Throw "*positive integer sizes*"
        }
    }

    Context "Type Conversion" {
        It "Should handle string divisor that converts to integer" {
            $result = 'abcd' / "2"
            $result.Count | Should -Be 2
            $result[0] | Should -Be "ab"
            $result[1] | Should -Be "cd"
        }

        It "Should handle double divisor that converts to integer" {
            $result = 'abcd' / 2.0
            $result.Count | Should -Be 2
            $result[0] | Should -Be "ab"
            $result[1] | Should -Be "cd"
        }

        It "Should throw error for non-numeric string divisor" {
            { 'hello' / 'world' } | Should -Throw
        }
    }

    Context "Concatenation Verification" {
        It "Should preserve original string when parts are joined" {
            $original = "PowerShell is awesome for automation and scripting tasks"
            $parts = $original / 5
            ($parts -join '') | Should -Be $original
        }

        It "Should preserve original string with custom sizes" {
            $original = "Testing string division with custom sizes functionality"
            $parts = $original / @(7, 6, 8, 4)
            ($parts -join '') | Should -Be $original
        }
    }
}
