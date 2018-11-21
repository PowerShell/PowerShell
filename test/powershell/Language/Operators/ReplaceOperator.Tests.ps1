# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Replace Operator" -Tags CI {
    Context "Replace operator" {
        It "Replace operator can replace string values using regular expressions" {
            $res = "Get-Process" -replace "Get", "Stop"
            $res | Should -BeExactly "Stop-Process"

            $res = "image.gif" -replace "\.gif$",".jpg"
            $res | Should -BeExactly "image.jpg"
        }

        It "Replace operator can be case-insensitive and case-sensitive" {
            $res = "book" -replace "B","C"
            $res | Should -BeExactly "Cook"

            $res = "book" -ireplace "B","C"
            $res | Should -BeExactly "Cook"

            $res = "book" -creplace "B","C"
            $res | Should -BeExactly "book"
        }

        It "Replace operator can take 2 arguments, a mandatory pattern, and an optional substitution" {
            $res = "PowerPoint" -replace "Point","Shell"
            $res | Should -BeExactly "PowerShell"

            $res = "PowerPoint" -replace "Point"
            $res | Should -BeExactly "Power"
        }
    }

    Context "Replace operator substitutions" {
        It "Replace operator supports numbered substitution groups using ```$n" {
            $res = "domain.example" -replace ".*\.(\w+)$","Tld of '`$0' is - '`$1'"
            $res | Should -BeExactly "Tld of 'domain.example' is - 'example'"
        }

        It "Replace operator supports named substitution groups using ```${name}" {
            $res = "domain.example" -replace ".*\.(?<tld>\w+)$","`${tld}"
            $res | Should -BeExactly "example"
        }

        It "Replace operator can take a ScriptBlock in place of a substitution string" {
            $res = "ID ABC123" -replace "\b[A-C]+", {return "X" * $_.Value.Length}
            $res | Should -BeExactly "ID XXX123"
        }

        It "Replace operator can take a MatchEvaluator in place of a substitution string" {
            $matchEvaluator = {return "X" * $args[0].Value.Length} -as [System.Text.RegularExpressions.MatchEvaluator]
            $res = "ID ABC123" -replace "\b[A-C]+", $matchEvaluator
            $res | Should -BeExactly "ID XXX123"
        }

        It "Replace operator can take a static PSMethod in place of a substitution string" {
            class R {
                static [string] Replace([System.Text.RegularExpressions.Match]$Match) {
                    return "X" * $Match.Value.Length
                }
            }
            $substitutionMethod = [R]::Replace
            $res = "ID 0000123" -replace "\b0+", $substitutionMethod
            $res | Should -BeExactly "ID XXXX123"
        }
    }
}
