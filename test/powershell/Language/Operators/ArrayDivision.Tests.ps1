# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Array Division Operator Tests" -Tags "CI" {

    Context "Basic Array Division" {
        It "Should divide array @(1,2,3,4,5,6) / 2 into two equal groups" {
            $result = @(1,2,3,4,5,6) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 3
            $result[1].Count | Should -Be 3
            $result[0] -join ',' | Should -Be "1,2,3"
            $result[1] -join ',' | Should -Be "4,5,6"
        }

        It "Should divide array @(1,2,3,4,5,6,7,8,9) / 3 into three equal groups" {
            $result = @(1,2,3,4,5,6,7,8,9) / 3
            $result.Count | Should -Be 3
            $result[0].Count | Should -Be 3
            $result[1].Count | Should -Be 3
            $result[2].Count | Should -Be 3
            $result[0] -join ',' | Should -Be "1,2,3"
            $result[1] -join ',' | Should -Be "4,5,6"
            $result[2] -join ',' | Should -Be "7,8,9"
        }

        It "Should handle uneven division @(1,2,3,4,5) / 2 correctly" {
            $result = @(1,2,3,4,5) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 3  # First group gets extra element
            $result[1].Count | Should -Be 2
            $result[0] -join ',' | Should -Be "1,2,3"
            $result[1] -join ',' | Should -Be "4,5"
        }

        It "Should handle division by 1 returning original array" {
            $result = @(1,2,3,4,5) / 1
            $result.Count | Should -Be 1
            $result[0].Count | Should -Be 5
            $result[0] -join ',' | Should -Be "1,2,3,4,5"
        }
    }

    Context "Array Division with Custom Sizes" {
        It "Should divide array @(1,2,3,4,5,6,7,8) / @(2,3,2) with custom sizes" {
            $result = @(1,2,3,4,5,6,7,8) / @(2,3,2)
            $result.Count | Should -Be 4  # 3 specified groups + 1 remainder
            $result[0].Count | Should -Be 2
            $result[1].Count | Should -Be 3
            $result[2].Count | Should -Be 2
            $result[3].Count | Should -Be 1  # Remainder
            $result[0] -join ',' | Should -Be "1,2"
            $result[1] -join ',' | Should -Be "3,4,5"
            $result[2] -join ',' | Should -Be "6,7"
            $result[3] -join ',' | Should -Be "8"
        }

        It "Should divide array @(1,2,3,4,5,6) / @(3,3) exactly with no remainder" {
            $result = @(1,2,3,4,5,6) / @(3,3)
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 3
            $result[1].Count | Should -Be 3
            $result[0] -join ',' | Should -Be "1,2,3"
            $result[1] -join ',' | Should -Be "4,5,6"
        }

        It "Should handle array division with sizes larger than remaining elements" {
            $result = @(1,2,3) / @(5)
            $result.Count | Should -Be 1
            $result[0].Count | Should -Be 3  # Gets all remaining elements
            $result[0] -join ',' | Should -Be "1,2,3"
        }
    }

    Context "Mixed Data Types" {
        It "Should handle mixed data types @('hello', 123, `$true, 'world', 456) / 2" {
            $result = @('hello', 123, $true, 'world', 456) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 3
            $result[1].Count | Should -Be 2
            $result[0][0] | Should -Be 'hello'
            $result[0][1] | Should -Be 123
            $result[0][2] | Should -Be $true
            $result[1][0] | Should -Be 'world'
            $result[1][1] | Should -Be 456
        }

        It "Should handle array with null values" {
            $result = @(1, $null, 3, $null, 5) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 3
            $result[1].Count | Should -Be 2
            $result[0][1] | Should -BeNullOrEmpty
            $result[1][1] | Should -BeNullOrEmpty
        }
    }

    Context "Edge Cases" {
        It "Should handle empty array division" {
            $result = @() / 2
            $result.Count | Should -Be 0
        }

        It "Should handle single element array division" {
            $result = @(42) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 1
            $result[1].Count | Should -Be 0
            $result[0][0] | Should -Be 42
        }

        It "Should throw error for zero divisor" {
            { @(1,2,3) / 0 } | Should -Throw "*positive divisor*"
        }

        It "Should throw error for negative divisor" {
            { @(1,2,3) / -1 } | Should -Throw "*positive divisor*"
        }

        It "Should throw error for empty array divisor" {
            { @(1,2,3) / @() } | Should -Throw "*positive integer sizes*"
        }

        It "Should throw error for array divisor with non-positive values" {
            { @(1,2,3) / @(2, 0, 1) } | Should -Throw "*positive integer sizes*"
        }
    }

    Context "Type Conversion" {
        It "Should handle string divisor that converts to integer" {
            $result = @(1,2,3,4) / "2"
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 2
            $result[1].Count | Should -Be 2
        }

        It "Should handle double divisor that converts to integer" {
            $result = @(1,2,3,4) / 2.0
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 2
            $result[1].Count | Should -Be 2
        }
    }
}
