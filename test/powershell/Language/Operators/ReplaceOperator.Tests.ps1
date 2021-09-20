# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Replace Operator" -Tags CI {
    Context "Replace operator" {
        It "Replace operator can replace string values using regular expressions" {
            $res = "Get-Process" -replace "Get", "Stop"
            $res | Should -BeExactly "Stop-Process"

            $res = "image.gif" -replace "\.gif$",".jpg"
            $res | Should -BeExactly "image.jpg"
        }

        It "Replace operator can convert an substitution object to string" {
            $substitution = Get-Process -Id $pid
            $res = "!3!" -replace "3",$substitution
            $res | Should -BeExactly "!System.Diagnostics.Process (pwsh)!"
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

        It "Replace operator can take an enumerable as first argument, a mandatory pattern, and an optional substitution" {
            $res = "PowerPoint1","PowerPoint2" -replace "Point","Shell"
            $res.Count | Should -Be 2
            $res[0] | Should -BeExactly "PowerShell1"
            $res[1] | Should -BeExactly "PowerShell2"

            $res = "PowerPoint1","PowerPoint2" -replace "Point"
            $res.Count | Should -Be 2
            $res[0] | Should -BeExactly "Power1"
            $res[1] | Should -BeExactly "Power2"
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

    Describe "Culture-invariance tests for -split and -replace" -Tags CI {
        BeforeAll {
            $prevCulture = [cultureinfo]::CurrentCulture
            # The French culture uses "," as the decimal mark.
            [cultureinfo]::CurrentCulture = 'fr'
        }

        AfterAll {
            [cultureinfo]::CurrentCulture = $prevCulture
        }

        It "-split: LHS stringification is not culture-sensitive" {
          1.2 -split ',' | Should -Be '1.2'
        }

        It "-replace: LHS stringification is not culture-sensitive" {
          1.2 -replace ',' | Should -Be '1.2'
        }
    }
}
