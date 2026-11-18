# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Arithmetic operators for Int128 and UInt128" -Tags "CI" {

    Context "Conversion" {
        It "Converts an integer literal to Int128" {
            $result = [Int128]1
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]::One)
        }

        It "Converts an integer literal to UInt128" {
            $result = [UInt128]1
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]::One)
        }

        It "Converts a string to Int128 without losing range" {
            [Int128]'170141183460469231731687303715884105727' | Should -Be ([Int128]::MaxValue)
        }

        It "Converts a string to UInt128 without losing range" {
            [UInt128]'340282366920938463463374607431768211455' | Should -Be ([UInt128]::MaxValue)
        }

        It "Converts Int128 back to a narrower type" {
            $result = [int][Int128]42
            $result.GetType().Name | Should -Be 'Int32'
            $result | Should -Be 42
        }
    }

    Context "Addition" {
        It "Keeps non-overflowing Int128 addition in Int128" {
            $result = [Int128]1 + [Int128]2
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]3)
        }

        It "Promotes Int128 overflow to Double" {
            $result = [Int128]::MaxValue + [Int128]::MaxValue
            $result.GetType().Name | Should -Be 'Double'
        }

        It "Keeps non-overflowing UInt128 addition in UInt128" {
            $result = [UInt128]1 + [UInt128]2
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]3)
        }

        It "Promotes UInt128 overflow to Double" {
            $result = [UInt128]::MaxValue + [UInt128]::MaxValue
            $result.GetType().Name | Should -Be 'Double'
        }
    }

    Context "Subtraction" {
        It "Keeps non-overflowing Int128 subtraction in Int128" {
            $result = [Int128]5 - [Int128]3
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]2)
        }

        It "Promotes Int128 subtraction overflow to Double" {
            $result = [Int128]::MinValue - [Int128]1
            $result.GetType().Name | Should -Be 'Double'
        }

        It "Keeps non-underflowing UInt128 subtraction in UInt128" {
            $result = [UInt128]5 - [UInt128]3
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]2)
        }

        It "Promotes UInt128 subtraction underflow to Double" {
            $result = [UInt128]0 - [UInt128]1
            $result.GetType().Name | Should -Be 'Double'
            $result | Should -Be (-1.0)
        }
    }

    Context "Multiplication" {
        It "Keeps non-overflowing Int128 multiplication in Int128" {
            $result = [Int128]6 * [Int128]7
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]42)
        }

        It "Promotes Int128 multiplication overflow to Double" {
            $result = [Int128]::MaxValue * [Int128]2
            $result.GetType().Name | Should -Be 'Double'
        }

        It "Keeps non-overflowing UInt128 multiplication in UInt128" {
            $result = [UInt128]6 * [UInt128]7
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]42)
        }

        It "Promotes UInt128 multiplication overflow to Double" {
            $result = [UInt128]::MaxValue * [UInt128]2
            $result.GetType().Name | Should -Be 'Double'
        }
    }

    Context "Division" {
        It "Returns Int128 when the division is exact" {
            $result = [Int128]6 / [Int128]3
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]2)
        }

        It "Returns Double when the Int128 division is inexact" {
            $result = [Int128]7 / [Int128]2
            $result.GetType().Name | Should -Be 'Double'
            $result | Should -Be 3.5
        }

        It "Returns UInt128 when the division is exact" {
            $result = [UInt128]8 / [UInt128]4
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]2)
        }

        It "Returns Double when the UInt128 division is inexact" {
            $result = [UInt128]7 / [UInt128]2
            $result.GetType().Name | Should -Be 'Double'
            $result | Should -Be 3.5
        }

        It "Promotes Int128 MinValue divided by -1 to Double" {
            $result = [Int128]::MinValue / [Int128]-1
            $result.GetType().Name | Should -Be 'Double'
        }

        It "Throws a RuntimeException dividing Int128 by zero" {
            { [Int128]1 / [Int128]0 } | Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
        }

        It "Throws a RuntimeException dividing UInt128 by zero" {
            { [UInt128]1 / [UInt128]0 } | Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
        }
    }

    Context "Remainder" {
        It "Returns the Int128 remainder" {
            $result = [Int128]7 % [Int128]3
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]1)
        }

        It "Returns the UInt128 remainder" {
            $result = [UInt128]7 % [UInt128]3
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]1)
        }

        It "Returns zero for Int128 MinValue remainder -1" {
            $result = [Int128]::MinValue % [Int128]-1
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]::Zero)
        }

        It "Throws a RuntimeException for an Int128 zero divisor" {
            { [Int128]1 % [Int128]0 } | Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
        }

        It "Throws a RuntimeException for a UInt128 zero divisor" {
            { [UInt128]1 % [UInt128]0 } | Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
        }
    }

    Context "Mixed operand types" {
        It "Widens a narrower signed operand to Int128" {
            $result = [Int128]1 + [long]2
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]3)
        }

        It "Widens UInt64 to Int128" {
            $result = [Int128]1 + [ulong]2
            $result.GetType().Name | Should -Be 'Int128'
            $result | Should -Be ([Int128]3)
        }

        It "Keeps UInt128 with a non-negative signed operand in UInt128" {
            $result = [UInt128]10 + 5
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]15)
        }

        It "Keeps UInt128 with a non-negative Int128 operand in UInt128" {
            $result = [UInt128]1 + [Int128]2
            $result.GetType().Name | Should -Be 'UInt128'
            $result | Should -Be ([UInt128]3)
        }

        It "Promotes UInt128 with a negative operand to Double" {
            $result = [UInt128]10 - -5
            $result.GetType().Name | Should -Be 'Double'
            $result | Should -Be 15.0
        }

        It "Promotes Int128 with a floating point operand to Double" {
            $result = [Int128]1 + 2.5
            $result.GetType().Name | Should -Be 'Double'
            $result | Should -Be 3.5
        }

        It "Promotes overflow to Double when the operands have different widths" {
            $result = [Int128]::MaxValue + 1
            $result.GetType().Name | Should -Be 'Double'
        }
    }

    Context "Comparison" {
        It "Compares Int128 values" {
            [Int128]2 -eq [Int128]2 | Should -BeTrue
            [Int128]2 -ne [Int128]3 | Should -BeTrue
            [Int128]2 -lt [Int128]3 | Should -BeTrue
            [Int128]3 -gt [Int128]2 | Should -BeTrue
            [Int128]2 -le [Int128]2 | Should -BeTrue
            [Int128]2 -ge [Int128]2 | Should -BeTrue
        }

        It "Compares UInt128 values" {
            [UInt128]2 -eq [UInt128]2 | Should -BeTrue
            [UInt128]2 -ne [UInt128]3 | Should -BeTrue
            [UInt128]2 -lt [UInt128]3 | Should -BeTrue
            [UInt128]3 -gt [UInt128]2 | Should -BeTrue
            [UInt128]2 -le [UInt128]2 | Should -BeTrue
            [UInt128]2 -ge [UInt128]2 | Should -BeTrue
        }

        It "Compares Int128 against a narrower type" {
            [Int128]::MaxValue -gt 1 | Should -BeTrue
            1 -lt [Int128]::MaxValue | Should -BeTrue
        }
    }
}
