# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "MatchAll/IMatchAll Operator" -Tags CI {
    Context "Scalar LHS regular expression matching" {
        It "Should produce one match from an exact string pattern" -TestCases @(
            @{ string = 'foo'; pattern = "foo" },
            @{ string = 'FOO'; pattern = "FOO" },
            @{ string = 'FOo'; pattern = "foo" },
            @{ string = 'foo'; pattern = "FOo" }
        ) {
            param($string, $pattern)
            $match = $string -matchall $pattern
            $match.Count | Should -Be 1
            $match.Value | Should -Be $string

            $match = $string -imatchall $pattern
            $match.Count | Should -Be 1
            $match.Value | Should -Be $string
        }

        It "Should produce two matches from a pattern with regex syntax" -TestCases @(
            @{ string = 'foo'; pattern = "(.)" },
            @{ string = 'foO'; pattern = "(.)" }
        ) {
            param($string, $pattern)
            $pattern = "(.)"
            $string = "foo"
            $match = $string -matchall $pattern
            $imatch = $string -imatchall $pattern
            $match.Count | Should -Be $string.Length
            $imatch.Count | Should -Be $string.Length
            For ($i = 0; i -lt $string.Length; $i++)
            {
                $match.Value[$i] | Should -Be $string[$i]
                $match.Index[$i] | Should -Be $i
                $match.Length[$i] | Should -Be 1

                $imatch.Value[$i] | Should -Be $string[$i]
                $imatch.Index[$i] | Should -Be $i
                $imatch.Length[$i] | Should -Be 1
            }
        }
    }

    Context "Array LHS regular expression matching" {
        It "Should produce different matches from different strings in the array" -TestCases @(
            @{ strings = 'foo', 'baa'; $pattern = 'o|a' },
            @{ strings = 'foo', 'baa'; $pattern = 'o|A' },
            @{ strings = 'foO', 'baa'; $pattern = 'o|a' }
        ) {
            param($strings, $pattern)
            $match = $strings -matchall $pattern
            $imatch = $strings -imatchall $pattern
            $match.Count | Should -Be 4
            $imatch.Count | Should -Be 4

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1

            $match.Value[2] | Should -Be "a"
            $match.Index[2] | Should -Be 1
            $match.Length[2] | Should -Be 1

            $match.Value[3] | Should -Be "a"
            $match.Index[3] | Should -Be 2
            $match.Length[3] | Should -Be 1

            $imatch.Value[0] | Should -Be "o"
            $imatch.Index[0] | Should -Be 1
            $imatch.Length[0] | Should -Be 1

            $imatch.Value[1] | Should -Be "o"
            $imatch.Index[1] | Should -Be 2
            $imatch.Length[1] | Should -Be 1

            $imatch.Value[2] | Should -Be "a"
            $imatch.Index[2] | Should -Be 1
            $imatch.Length[2] | Should -Be 1

            $imatch.Value[3] | Should -Be "a"
            $imatch.Index[3] | Should -Be 2
            $imatch.Length[3] | Should -Be 1
        }
    }
}

Describe "CMatchAll operator" -Tags CI {
    Context "Scalar LHS regular expression matching" {
        It "All characters are in string and pattern lowercase" {
            $string = 'foo'
            $pattern = 'o'
            $match = $string -cmatchall $pattern
            $match.Count | Should -Be 2

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1

        }

        It "Pattern is all uppercase and string is all lowercase" {
            $string = 'foo'
            $pattern = 'O'
            $match = $string -cmatchall $pattern
            $match.Count | Should -Be 0
        }
    }

    Context "Array LHS regular expression matching" {
        It "All characters are in string and pattern lowercase" {
            $strings = 'foo', 'baa'
            $pattern = 'o|a'
            $match = $strings -cmatchall $pattern
            $match.Count | Should -Be 4

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1

            $match.Value[2] | Should -Be "a"
            $match.Index[2] | Should -Be 1
            $match.Length[2] | Should -Be 1

            $match.Value[3] | Should -Be "a"
            $match.Index[3] | Should -Be 2
            $match.Length[3] | Should -Be 1
        }

        It "One character the pattern is uppercase and all characters in the strings are lowercase" {
            $strings = 'foo', 'baa'
            $pattern = 'o|A'
            $match = $strings -cmatchall $pattern
            $match.Count | Should -Be 2

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1
        }
    }
}
