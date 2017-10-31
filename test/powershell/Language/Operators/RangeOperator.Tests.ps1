Describe "Range Operator" -Tags CI {
    Context "Range integer operations" {
        It "Range operator generates arrays of integers" {
            $Range = 5..8
            $Range.count | Should Be 4
            $Range[0] | Should BeOfType [int]
            $Range[1] | Should BeOfType [int]
            $Range[2] | Should BeOfType [int]
            $Range[3] | Should BeOfType [int]

            $Range[0] | Should Be 5
            $Range[1] | Should Be 6
            $Range[2] | Should Be 7
            $Range[3] | Should Be 8
        }

        It "Range operator accepts negative integer values" {
            $Range = -8..-5
            $Range.count | Should Be 4
            $Range[0] | Should Be -8
            $Range[1] | Should Be -7
            $Range[2] | Should Be -6
            $Range[3] | Should Be -5
        }

        It "Range operator support single-item sequences" {
            $Range = 0..0
            $Range.count | Should Be 1
            $Range[0] | Should BeOfType [int]
            $Range[0] | Should Be 0
        }

        It "Range operator works in descending order" {
            $Range = 4..3
            $Range.count | Should Be 2
            $Range[0] | Should Be 4
            $Range[1] | Should Be 3
        }

        It "Range operator works for sequences of both negative and positive numbers" {
            $Range = -2..2
            $Range.count | Should Be 5
            $Range[0] | Should Be -2
            $Range[1] | Should Be -1
            $Range[2] | Should Be 0
            $Range[3] | Should Be 1
            $Range[4] | Should Be 2
        }
    }

    Context "Character expansion" {
        It "Range operator generates an array of [char] from single-character string operands" {
            $CharRange = 'A'..'E'
            $CharRange.count | Should Be 5
            $CharRange[0] | Should BeOfType [char]
            $CharRange[1] | Should BeOfType [char]
            $CharRange[2] | Should BeOfType [char]
            $CharRange[3] | Should BeOfType [char]
            $CharRange[4] | Should BeOfType [char]
        }

        It "Range operator works in ascending and descending order" {
            $CharRange = 'a'..'b'
            $CharRange.count | Should Be 2
            $CharRange[0] | Should Be ([char]'a')
            $CharRange[1] | Should Be ([char]'b')

            $CharRange = 'b'..'a'
            $CharRange.count | Should Be 2
            $CharRange[0] | Should Be ([char]'b')
            $CharRange[1] | Should Be ([char]'a')
        }

        It "Range operator works with 16-bit unicode characters" {
            $UnicodeRange = "`u{0110}".."`u{0114}"
            $UnicodeRange.count | Should Be 5
            $UnicodeRange[0] | Should Be "`u{0110}"[0]
            $UnicodeRange[1] | Should Be "`u{0111}"[0]
            $UnicodeRange[2] | Should Be "`u{0112}"[0]
            $UnicodeRange[3] | Should Be "`u{0113}"[0]
            $UnicodeRange[4] | Should Be "`u{0114}"[0]
            $UnicodeRange.Where({$_ -is [char]}).count | Should Be 5
        }
    }

    Context "Range operator operand types" {
        It "Range operator works on [decimal]" {
            $Range = 1.1d..3.9d
            $Range.count | Should Be 4
            $Range[0] | Should Be 1
            $Range[1] | Should Be 2
            $Range[2] | Should Be 3
            $Range[3] | Should Be 4
            $Range.Where({$_ -is [int]}).count | Should Be 4
        }
    }
}
