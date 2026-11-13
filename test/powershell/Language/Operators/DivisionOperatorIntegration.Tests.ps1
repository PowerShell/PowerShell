# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Division Operator Integration Tests" -Tags "CI" {

    Context "Backward Compatibility" {
        It "Should maintain numeric division behavior" {
            $result = 10 / 2
            $result | Should -Be 5
        }

        It "Should maintain double division behavior" {
            $result = 10.5 / 2.5
            $result | Should -Be 4.2
        }

        It "Should handle mixed numeric types" {
            $result = 15 / 3.0
            $result | Should -Be 5.0
        }
    }

    Context "Type Precedence" {
        It "Should choose array division over numeric when left operand is array" {
            $result = @(1,2,3,4) / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 2
        }

        It "Should choose string division over numeric when left operand is string" {
            $result = "1234" / 2
            $result.Count | Should -Be 2
            $result[0] | Should -Be "12"
            $result[1] | Should -Be "34"
        }

        It "Should default to numeric division for other types" {
            $result = 8 / 4
            $result | Should -Be 2
        }
    }

    Context "Complex Scenarios" {
        It "Should handle nested array division results" {
            $original = @(1,2,3,4,5,6,7,8,9,10,11,12)
            $firstDivision = $original / 3  # Creates 3 groups of 4
            $secondDivision = $firstDivision[0] / 2  # Divide first group into 2

            $firstDivision.Count | Should -Be 3
            $secondDivision.Count | Should -Be 2
            $secondDivision[0] -join ',' | Should -Be "1,2"
            $secondDivision[1] -join ',' | Should -Be "3,4"
        }

        It "Should handle array of strings division" {
            $result = @("hello", "world", "test", "case") / 2
            $result.Count | Should -Be 2
            $result[0].Count | Should -Be 2
            $result[1].Count | Should -Be 2
            $result[0] -join ',' | Should -Be "hello,world"
            $result[1] -join ',' | Should -Be "test,case"
        }

        It "Should handle string division then join operations" {
            $original = "PowerShellArrayStringDivision"
            $parts = $original / 4
            $rejoined = $parts -join '-'
            $rejoined | Should -Be "PowerSh-ellArray-StringD-ivision"
        }
    }

    Context "Pipeline Operations" {
        It "Should work in pipeline with array division" {
            $result = 1..12 | ForEach-Object { $_ } | ForEach-Object { @($_) } | ForEach-Object { $_ / 1 }
            $result.Count | Should -Be 12
            foreach ($item in $result) {
                $item.Count | Should -Be 1
            }
        }

        It "Should work with Select-Object after division" {
            $result = @("apple", "banana", "cherry", "date") / 2 | Select-Object -First 1
            $result.Count | Should -Be 2
            $result -join ',' | Should -Be "apple,banana"
        }
    }

    Context "Variable Assignment and References" {
        It "Should work with variable assignment" {
            $array = @(1,2,3,4,5,6)
            $divisor = 3
            $result = $array / $divisor

            $result.Count | Should -Be 3
            $result[0] -join ',' | Should -Be "1,2"
            $result[1] -join ',' | Should -Be "3,4"
            $result[2] -join ',' | Should -Be "5,6"
        }

        It "Should work with array variable as divisor" {
            $sizes = @(2,3,1)
            $result = @(1,2,3,4,5,6) / $sizes

            $result.Count | Should -Be 3
            $result[0] -join ',' | Should -Be "1,2"
            $result[1] -join ',' | Should -Be "3,4,5"
            $result[2] -join ',' | Should -Be "6"
        }
    }

    Context "Error Handling Integration" {
        It "Should provide clear error messages for invalid operations" {
            { $null / 2 } | Should -Not -Throw  # Should handle null gracefully
        }

        It "Should handle type conversion errors gracefully" {
            { @(1,2,3) / "not_a_number" } | Should -Throw
        }

        It "Should maintain original PowerShell error behavior for unsupported operations" {
            { [System.DateTime]::Now / 2 } | Should -Throw
        }
    }

    Context "Performance and Memory" {
        It "Should handle large arrays efficiently" {
            $largeArray = 1..1000
            $result = $largeArray / 10

            $result.Count | Should -Be 10
            $result[0].Count | Should -Be 100
            $result[9].Count | Should -Be 100
        }

        It "Should handle long strings efficiently" {
            $longString = "a" * 10000
            $result = $longString / 100

            $result.Count | Should -Be 100
            $result[0].Length | Should -Be 100
            $result[99].Length | Should -Be 100
        }
    }
}
