# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'switch -regex' -Tags 'CI' {

    Context 'Populates $matches' {

        It 'Sets named groups' {
            switch -regex ('2026-09-04 ERROR boom') {
                '^(?<date>\S+) ERROR (?<msg>.*)$' {
                    $matches['date'] | Should -BeExactly '2026-09-04'
                    $matches['msg']  | Should -BeExactly 'boom'
                }
            }
        }

        It 'Sets numbered groups, including group 0' {
            switch -regex ('2026-09-04 ERROR boom') {
                '^(\S+) (ERROR) (.*)$' {
                    $matches[0] | Should -BeExactly '2026-09-04 ERROR boom'
                    $matches[1] | Should -BeExactly '2026-09-04'
                    $matches[2] | Should -BeExactly 'ERROR'
                    $matches[3] | Should -BeExactly 'boom'
                }
            }
        }

        It 'Exposes both named and numbered keys' {
            # .NET numbers the unnamed groups first, so (\d+) is group 1 and the
            # named group keeps its name -- keys are 0, 1 and 'letters'.
            switch -regex ('abc123') {
                '(?<letters>[a-z]+)(\d+)' {
                    ($matches.Keys | Sort-Object { "$_" }) -join ',' |
                        Should -BeExactly '0,1,letters'
                }
            }
        }

        It 'Leaves $matches from an earlier match untouched when nothing matches' {
            switch -regex ('abc123') { '(?<first>abc)' { } }
            $captured = $matches['first']

            switch -regex ('zzz') {
                'nomatch' { throw 'should not run' }
                default { }
            }

            $matches['first'] | Should -BeExactly $captured
        }
    }

    Context 'Case sensitivity' {

        It 'Matches case-insensitively by default' {
            $hit = $false
            switch -regex ('HELLO') { 'hello' { $hit = $true } }
            $hit | Should -BeTrue
        }

        It 'Honors -CaseSensitive' {
            $hit = $false
            switch -regex -casesensitive ('HELLO') { 'hello' { $hit = $true } }
            $hit | Should -BeFalse
        }
    }

    Context 'A [regex] instance as the clause condition' {
        BeforeAll {
            $re = [regex]::new('hello', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        }

        It 'Is used as-is under a case-insensitive switch' {
            $hit = $false
            switch -regex ('HELLO') { $re { $hit = $true } }
            $hit | Should -BeTrue
        }

        It 'Is rebuilt case-sensitively under -CaseSensitive' {
            $hit = $false
            switch -regex -casesensitive ('HELLO') { $re { $hit = $true } }
            $hit | Should -BeFalse
        }
    }

    Context 'Clause evaluation' {

        It 'Falls through to later clauses without continue' {
            $hits = @()
            switch -regex ('abc123') {
                '(?<letters>[a-z]+)' { $hits += "letters=$($matches.letters)" }
                '(?<digits>\d+)'     { $hits += "digits=$($matches.digits)" }
            }

            $hits -join '|' | Should -BeExactly 'letters=abc|digits=123'
        }

        It 'Throws InvalidRegularExpression on a malformed pattern' {
            { switch -regex ('x') { '[' { } } } |
                Should -Throw -ErrorId 'InvalidRegularExpression'
        }
    }

    Context 'More distinct clause patterns than the static regex cache holds' {
        # The engine caches switch clause patterns itself rather than relying on
        # [regex]::CacheSize, which defaults to 15. Exercise more distinct patterns
        # than that, repeatedly, so a caching regression shows up as a wrong result
        # rather than only as a slowdown.

        It 'Matches the correct clause across repeated passes' {
            $results = foreach ($i in 1..5) {
                foreach ($n in 1..24) {
                    $captured = $null
                    switch -regex ("value-$n-end") {
                        '^value-1-(?<tail>\w+)$'  { $captured = "01:$($matches.tail)"; continue }
                        '^value-2-(?<tail>\w+)$'  { $captured = "02:$($matches.tail)"; continue }
                        '^value-3-(?<tail>\w+)$'  { $captured = "03:$($matches.tail)"; continue }
                        '^value-4-(?<tail>\w+)$'  { $captured = "04:$($matches.tail)"; continue }
                        '^value-5-(?<tail>\w+)$'  { $captured = "05:$($matches.tail)"; continue }
                        '^value-6-(?<tail>\w+)$'  { $captured = "06:$($matches.tail)"; continue }
                        '^value-7-(?<tail>\w+)$'  { $captured = "07:$($matches.tail)"; continue }
                        '^value-8-(?<tail>\w+)$'  { $captured = "08:$($matches.tail)"; continue }
                        '^value-9-(?<tail>\w+)$'  { $captured = "09:$($matches.tail)"; continue }
                        '^value-10-(?<tail>\w+)$' { $captured = "10:$($matches.tail)"; continue }
                        '^value-11-(?<tail>\w+)$' { $captured = "11:$($matches.tail)"; continue }
                        '^value-12-(?<tail>\w+)$' { $captured = "12:$($matches.tail)"; continue }
                        '^value-13-(?<tail>\w+)$' { $captured = "13:$($matches.tail)"; continue }
                        '^value-14-(?<tail>\w+)$' { $captured = "14:$($matches.tail)"; continue }
                        '^value-15-(?<tail>\w+)$' { $captured = "15:$($matches.tail)"; continue }
                        '^value-16-(?<tail>\w+)$' { $captured = "16:$($matches.tail)"; continue }
                        '^value-17-(?<tail>\w+)$' { $captured = "17:$($matches.tail)"; continue }
                        '^value-18-(?<tail>\w+)$' { $captured = "18:$($matches.tail)"; continue }
                        '^value-19-(?<tail>\w+)$' { $captured = "19:$($matches.tail)"; continue }
                        '^value-20-(?<tail>\w+)$' { $captured = "20:$($matches.tail)"; continue }
                        '^value-21-(?<tail>\w+)$' { $captured = "21:$($matches.tail)"; continue }
                        '^value-22-(?<tail>\w+)$' { $captured = "22:$($matches.tail)"; continue }
                        '^value-23-(?<tail>\w+)$' { $captured = "23:$($matches.tail)"; continue }
                        '^value-24-(?<tail>\w+)$' { $captured = "24:$($matches.tail)"; continue }
                        default { $captured = 'none' }
                    }

                    $captured
                }
            }

            $expected = foreach ($i in 1..5) {
                foreach ($n in 1..24) { '{0:d2}:end' -f $n }
            }

            $results | Should -BeExactly $expected
        }
    }

    Context 'switch -regex -file' {
        BeforeAll {
            $file = Join-Path $TestDrive 'switch-regex.txt'
            Set-Content -Path $file -Value @('alpha 1', 'beta 2', 'gamma 3')
        }

        It 'Matches per line and populates $matches' {
            $acc = @()
            switch -regex -file $file {
                '^alpha (?<n>\d)$' { $acc += "a$($matches.n)"; continue }
                '^beta (?<n>\d)$'  { $acc += "b$($matches.n)"; continue }
                default            { $acc += 'other' }
            }

            $acc -join ',' | Should -BeExactly 'a1,b2,other'
        }
    }
}
