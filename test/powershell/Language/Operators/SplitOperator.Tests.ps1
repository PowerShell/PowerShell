# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Split Operator" -Tags CI {
    Context "Binary split operator" {
        It "Binary split operator can split array of value" {
            $res = "a b", "c d" -split " "
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"
        }

        It "Binary split operator can split a string" {
            $res = "a b c d" -split " "
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"
        }

        It "Binary split operator can works with max substring limit" {
            $res = "a b c d" -split " ", 2
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b c d"

            $res = "a b c d" -split " ", 0
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"

            $res = "a b c d" -split " ", -2
            $res.count | Should -Be 2
            $res[0] | Should -Be "a b c"
            $res[1] | Should -Be "d"

            $res = "a b c d" -split " ", -1
            $res.count | Should -Be 1
            $res[0] | Should -Be "a b c d"
        }

        It "Binary split operator can work with different delimeter than split string" {
            $res = "a b c d" -split " ",8
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"

            $res = "a b c d" -split " ",-8
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"

            $res = " " -split " ",-2
            $res.count | Should -Be 2
            $res[0] | Should -Be ""
            $res[1] | Should -Be ""
        }

        It "Binary split operator with predicate can work with negative numbers" {
            $res = "a b c d" -split {$_ -like ' '},-2
            $res.count | Should -Be 2
            $res[0] | Should -Be "a b c"
            $res[1] | Should -Be "d"

            $res = "a b c d" -split {$_ -like ' '},-4
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"

            $res = "a b c d" -split {$_ -like ' '},-8
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"

            $res = " " -split {$_ -like ' '},-4
            $res.count | Should -Be 2
            $res[0] | Should -Be ""
            $res[1] | Should -Be ""

            $res = "folder/path/to/file" -split {$_ -like '/'}, -2
            $res.count | Should -Be 2
            $res[0] | Should -Be "folder/path/to"
            $res[1] | Should -Be "file"
        }

        It "Binary split operator can work with regex expression" {
            $res = "a2b3c4d" -split '\d+',2
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b3c4d"

            $res = "a2b3c4d" -split '\d+',-2
            $res.count | Should -Be 2
            $res[0] | Should -Be "a2b3c"
            $res[1] | Should -Be "d"
        }

        It "Binary split operator can works with freeform delimiter" {
            $res = "a::b::c::d" -split "::"
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"
        }

        It "Binary split operator can preserve delimiter" {
            $res = "a1:b1:c1:d" -split "(1:)"
            $res.count | Should -Be 7
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "1:"
            $res[2] | Should -Be "b"
            $res[3] | Should -Be "1:"
            $res[4] | Should -Be "c"
            $res[5] | Should -Be "1:"
            $res[6] | Should -Be "d"

            $res = "a1:b1:c1:d" -split "1(:)"
            $res.count | Should -Be 7
            $res[0] | Should -Be "a"
            $res[1] | Should -Be ":"
            $res[2] | Should -Be "b"
            $res[3] | Should -Be ":"
            $res[4] | Should -Be "c"
            $res[5] | Should -Be ":"
            $res[6] | Should -Be "d"
        }

        It "Binary split operator can be case-insensitive and case-sensitive" {
            $res = "abcBd" -split "B"
            $res.count | Should -Be 3
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "c"
            $res[2] | Should -Be "d"

            $res = "abcBd" -isplit "B"
            $res.count | Should -Be 3
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "c"
            $res[2] | Should -Be "d"

            $res = "abcBd" -csplit "B"
            $res.count | Should -Be 2
            $res[0] | Should -Be "abc"
            $res[1] | Should -Be "d"

            $res = "abcBd" -csplit "B", 0 , 'IgnoreCase'
            $res.count | Should -Be 3
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "c"
            $res[2] | Should -Be "d"
        }

        It "Binary split operator can works with script block" {
            $res = "a::b::c::d" -split {$_ -eq "b" -or $_ -eq "C"}
            $res.count | Should -Be 3
            $res[0] | Should -Be "a::"
            $res[1] | Should -Be "::"
            $res[2] | Should -Be "::d"
        }

    }

    Context "Binary split operator options" {
        BeforeAll {
            # Add '%' in testText2 in order to second line doesn't start with 'b'.
            $testCases = @(
                @{ Name = '`n';   testText = "a12a`nb34b`nc56c`nd78d";       testText2 = "a12a`n%b34b`nc56c`nd78d";       newLine = "`n" }
                @{ Name = '`r`n'; testText = "a12a`r`nb34b`r`nc56c`r`nd78d"; testText2 = "a12a`r`n%b34b`r`nc56c`r`nd78d"; newLine = "`r`n" }
                )
        }

        It "Binary split operator has no Singleline and no Multiline by default (new line = '<Name>')" -TestCases $testCases {
            param($testText, $testText2, $newLine)
            # Multiline isn't default
            $res = $testText -split '^b'
            $res.count | Should -Be 1

            # Singleline isn't default
            $res = $testText -split 'b.+c'
            $res.count | Should -Be 1
        }

        It "Binary split operator works with Singleline (new line = '<Name>')" -TestCases $testCases {
            param($testText, $testText2, $newLine)
            $res = $testText -split 'b.+c', 0, 'Singleline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)"
            $res[1] | Should -Be "$($newLine)d78d"

            $res = $testText2 -split 'b.+c', 0, 'Singleline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)%"
            $res[1] | Should -Be "$($newLine)d78d"
        }

        It "Binary split operator works with Multiline (new line = '<Name>')" -TestCases $testCases {
            param($testText, $testText2, $newLine)
            $res = $testText -split '^b', 0, 'Multiline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)"
            $res[1] | Should -Be "34b$($newLine)c56c$($newLine)d78d"
        }

        It "Binary split operator works with Singleline,Multiline (new line = '<Name>')" -TestCases $testCases {
            param($testText, $testText2, $newLine)
            $res = $testText -split 'b.+c', 0, 'Singleline,Multiline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)"
            $res[1] | Should -Be "$($newLine)d78d"

            $res = $testText2 -split 'b.+c', 0, 'Singleline,Multiline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)%"
            $res[1] | Should -Be "$($newLine)d78d"

            $res = $testText -split '^b.+c', 0, 'Singleline,Multiline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a12a$($newLine)"
            $res[1] | Should -Be "$($newLine)d78d"

            $res = $testText2 -split '^b.+c', 0, 'Singleline,Multiline'
            $res.count | Should -Be 1
        }

        It "Binary split operator works with IgnorePatternWhitespace" {
            $res = "a: b:c" -split ': '
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b:c"

            $res = "a: b:c" -split ': ',0,'IgnorePatternWhitespace'
            $res.count | Should -Be 3
            $res[0] | Should -Be "a"
            $res[1] | Should -Be " b"
            $res[2] | Should -Be "c"
        }

        It "Binary split operator works with ExplicitCapture" {
            $res = "a:b" -split "(:)"
            $res.count | Should -Be 3
            $res[0] | Should -Be "a"
            $res[1] | Should -Be ":"
            $res[2] | Should -Be "b"

            $res = "a:b" -split "(:)", 0, 'ExplicitCapture'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
        }

        It "Binary split operator works with SimpleMatch" {
            $res = "abc" -split "B", 0, 'SimpleMatch,IgnoreCase'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "c"
        }

        It "Binary split operator works with RegexMatch" {
            $res = "abc" -split "B", 0, 'RegexMatch,Singleline'
            $res.count | Should -Be 2
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "c"
        }

        It "Binary split operator doesn't works with RegexMatch,SimpleMatch" {
            { "abc" -split "B", 0, 'RegexMatch,SimpleMatch' } | Should -Throw -ErrorId "InvalidSplitOptionCombination"
        }
    }

    Context "Unary split operator" {
        It "Unary split operator has higher precedence than a comma" {
            $res = -split "a b", "c d"
            $res.count | Should -Be 2
            $res[0][0] | Should -Be "a"
            $res[0][1] | Should -Be "b"
            $res[1] | Should -Be "c d"
        }

        It "Unary split operator can split array of values" {
            $res = -split ("a b", "c d")
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"
        }

        It "Unary split operator can split a string" {
            $res = -split "a b c d"
            $res.count | Should -Be 4
            $res[0] | Should -Be "a"
            $res[1] | Should -Be "b"
            $res[2] | Should -Be "c"
            $res[3] | Should -Be "d"
        }
    }
}
