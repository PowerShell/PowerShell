# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Range Operator" -Tags CI {
    Context "Range integer operations" {
        It "Range operator generates arrays of integers" {
            $Range = 5..8
            $Range.count | Should -Be 4
            $Range[0] | Should -BeOfType int
            $Range[1] | Should -BeOfType int
            $Range[2] | Should -BeOfType int
            $Range[3] | Should -BeOfType int

            $Range[0] | Should -Be 5
            $Range[1] | Should -Be 6
            $Range[2] | Should -Be 7
            $Range[3] | Should -Be 8
        }

        It "Range operator accepts negative integer values" {
            $Range = -8..-5
            $Range.count | Should -Be 4
            $Range[0] | Should -Be -8
            $Range[1] | Should -Be -7
            $Range[2] | Should -Be -6
            $Range[3] | Should -Be -5
        }

        It "Range operator support single-item sequences" {
            $Range = 0..0
            $Range.count | Should -Be 1
            $Range[0] | Should -BeOfType int
            $Range[0] | Should -Be 0
        }

        It "Range operator works in descending order" {
            $Range = 4..3
            $Range.count | Should -Be 2
            $Range[0] | Should -Be 4
            $Range[1] | Should -Be 3
        }

        It "Range operator works for sequences of both negative and positive numbers" {
            $Range = -2..2
            $Range.count | Should -Be 5
            $Range[0] | Should -Be -2
            $Range[1] | Should -Be -1
            $Range[2] | Should -Be 0
            $Range[3] | Should -Be 1
            $Range[4] | Should -Be 2
        }

        It "Range operator enumerator works" {
            $Range = -2..2 | ForEach-Object { $_ }
            $Range.count | Should -Be 5
            $Range[0] | Should -Be -2
            $Range[1] | Should -Be -1
            $Range[2] | Should -Be 0
            $Range[3] | Should -Be 1
            $Range[4] | Should -Be 2
        }

        It "Range operator works with variables" {
            $var1 = -1
            $var2 = 1
            $Range = $var1..$var2
            $Range.count | Should -Be 3
            $Range[0] | Should -Be -1
            $Range[1] | Should -Be 0
            $Range[2] | Should -Be 1

            $Range = [int]$var2..[int]$var1
            $Range.count | Should -Be 3
            $Range[0] | Should -Be 1
            $Range[1] | Should -Be 0
            $Range[2] | Should -Be -1

            $Range = $var1..$var2 | ForEach-Object { $_ }
            $Range.count | Should -Be 3
            $Range[0] | Should -Be -1
            $Range[1] | Should -Be 0
            $Range[2] | Should -Be 1

            $Range = [int]$var2..[int]$var1 | ForEach-Object { $_ }
            $Range.count | Should -Be 3
            $Range[0] | Should -Be 1
            $Range[1] | Should -Be 0
            $Range[2] | Should -Be -1
        }
    }

    Context "Character expansion" {
        It "Range operator generates an array of [char] from single-character operands" {
            $CharRange = 'A'..'E'
            $CharRange.count | Should -Be 5
            $CharRange[0] | Should -BeOfType char
            $CharRange[1] | Should -BeOfType char
            $CharRange[2] | Should -BeOfType char
            $CharRange[3] | Should -BeOfType char
            $CharRange[4] | Should -BeOfType char
        }

        It "Range operator enumerator generates an array of [string] from single-character operands" {
            $CharRange = 'A'..'E' | ForEach-Object { $_ }
            $CharRange.count | Should -Be 5
            $CharRange[0] | Should -BeOfType char
            $CharRange[1] | Should -BeOfType char
            $CharRange[2] | Should -BeOfType char
            $CharRange[3] | Should -BeOfType char
            $CharRange[4] | Should -BeOfType char
        }

        It "Range operator works in ascending and descending order" {
            $CharRange = 'a'..'c'
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'a')
            $CharRange[1] | Should -BeExactly ([char]'b')
            $CharRange[2] | Should -BeExactly ([char]'c')

            $CharRange = 'C'..'A'
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'C')
            $CharRange[1] | Should -BeExactly ([char]'B')
            $CharRange[2] | Should -BeExactly ([char]'A')
        }

        It "Range operator works in ascending and descending order with [char] cast" {
            $CharRange = [char]'a'..[char]'c'
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'a')
            $CharRange[1] | Should -BeExactly ([char]'b')
            $CharRange[2] | Should -BeExactly ([char]'c')

            $CharRange = [char]"a".."c"
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'a')
            $CharRange[1] | Should -BeExactly ([char]'b')
            $CharRange[2] | Should -BeExactly ([char]'c')

            $CharRange = "a"..[char]"c"
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'a')
            $CharRange[1] | Should -BeExactly ([char]'b')
            $CharRange[2] | Should -BeExactly ([char]'c')

            # The same works in reverse order.
            $CharRange = [char]'C'..[char]'A'
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'C')
            $CharRange[1] | Should -BeExactly ([char]'B')
            $CharRange[2] | Should -BeExactly ([char]'A')

            $CharRange = [char]"C".."A"
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'C')
            $CharRange[1] | Should -BeExactly ([char]'B')
            $CharRange[2] | Should -BeExactly ([char]'A')

            $CharRange = "C"..[char]"A"
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly ([char]'C')
            $CharRange[1] | Should -BeExactly ([char]'B')
            $CharRange[2] | Should -BeExactly ([char]'A')
        }

        It "Range operator enumerator works in ascending and descending order" {
            $CharRange = 'a'..'c' | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "a"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "c"

            $CharRange = 'C'..'A' | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "C"
            $CharRange[1] | Should -BeExactly "B"
            $CharRange[2] | Should -BeExactly "A"
        }

        It "Range operator enumerator works in ascending and descending order with [char] cast" {
            $CharRange = [char]'a'..[char]'c' | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "a"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "c"

            $CharRange = [char]'C'..[char]'A' | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "C"
            $CharRange[1] | Should -BeExactly "B"
            $CharRange[2] | Should -BeExactly "A"
        }

        It "Range operator works with variables" {
            $var1 = 'a'
            $var2 = 'c'
            $CharRange = $var1..$var2
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "a"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "c"

            $CharRange = [char]$var2..[char]$var1
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "c"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "a"

            $CharRange = $var1..$var2 | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "a"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "c"

            $CharRange = [char]$var2..[char]$var1 | ForEach-Object { $_ }
            $CharRange.count | Should -Be 3
            $CharRange[0] | Should -BeExactly "c"
            $CharRange[1] | Should -BeExactly "b"
            $CharRange[2] | Should -BeExactly "a"
        }

        It "Range operator works with 16-bit unicode characters" {
            $UnicodeRange = "`u{0110}".."`u{0114}"
            $UnicodeRange.count | Should -Be 5
            $UnicodeRange[0] | Should -Be "`u{0110}"[0]
            $UnicodeRange[1] | Should -Be "`u{0111}"[0]
            $UnicodeRange[2] | Should -Be "`u{0112}"[0]
            $UnicodeRange[3] | Should -Be "`u{0113}"[0]
            $UnicodeRange[4] | Should -Be "`u{0114}"[0]
            $UnicodeRange.Where({$_ -is [char]}).count | Should -Be 5
        }

        It "Range operator with special ranges" {
            $SpecRange = "0".."9"
            $SpecRange.count | Should -Be 10
            $SpecRange.Where({$_ -is [int]}).count | Should -Be 10

            $SpecRange = '0'..'9'
            $SpecRange.count | Should -Be 10
            $SpecRange.Where({$_ -is [int]}).count | Should -Be 10

            $SpecRange = [char]'0'..[char]'9'
            $SpecRange.count | Should -Be 10
            $SpecRange.Where({$_ -is [char]}).count | Should -Be 10
        }
    }

    Context "Range operator operand types" {
        It "Range operator works on [decimal]" {
            $Range = 1.1d..3.9d
            $Range.count | Should -Be 4
            $Range[0] | Should -Be 1
            $Range[1] | Should -Be 2
            $Range[2] | Should -Be 3
            $Range[3] | Should -Be 4
            $Range.Where({$_ -is [int]}).count | Should -Be 4
        }
    }
}
