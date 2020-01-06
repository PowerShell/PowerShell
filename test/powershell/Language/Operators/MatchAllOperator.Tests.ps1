# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Scalar LHS MatchAll Operator" -Tags CI {
    Context "Case insensitive regular expression matching" {
        It "Should produce one match from an exact string pattern" {
            $pattern = "foo"
            $string = "foo"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 1
            $match.Value | Should -Be "foo"
        }

        It "Should produce two matches from a substring that matches twice (different case)" {
            $pattern = "O"
            $string = "foO"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 2

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1
        }

        It "Should produce two matches from a pattern with regex syntax" {
            $pattern = "(.)"
            $string = "foo"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 3

            $match.Value[0] | Should -Be "f"
            $match.Index[0] | Should -Be 0
            $match.Length[0] | Should -Be 1

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1
        }
    }

    Context "Case sensitive regular expression matching" {
        It "Should produce no matches from an exact string pattern with different case" {
            $pattern = "FOo"
            $string = "foo"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 0
        }

        It "Should produce one match from an exact string pattern with the same casing" {
            $pattern = "foo"
            $string = "foo"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 1
            $match.Value | Should -Be "foo"
        }

        It "Should produce two matches from a pattern with regex syntax" {
            $pattern = "(.)"
            $string = "fOo"
            $match = $string -matchall $pattern
            $match.Count | Should -Be 3

            $match.Value[0] | Should -Be "f"
            $match.Index[0] | Should -Be 0
            $match.Length[0] | Should -Be 1

            $match.Value[0] | Should -Be "o"
            $match.Index[0] | Should -Be 1
            $match.Length[0] | Should -Be 1

            $match.Value[1] | Should -Be "o"
            $match.Index[1] | Should -Be 2
            $match.Length[1] | Should -Be 1
        }
    }
}

Describe "Array LHS MatchAll operator" -Tags CI {
    Context "Case insensitive regular expression matching" {
        It "Should produce different matches from different strings in the array" {
            $strings = 'foo', 'baa'
            $pattern = 'o|a'
            $match = $strings -matchall $pattern
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
    }

    Context "Case sensitive regular expression matching" {
        It "Should exclude the matches with differnt casing" {
            It "Should produce different matches from different strings in the array" {
                $strings = 'foo', 'baa'
                $pattern = 'o|A'
                $match = $strings -matchall $pattern
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
}
