Describe "Set-Variable" {
    ${nl} = [Environment]::Newline

    It "Should create a new variable with no parameters" {
        { Set-Variable testVar } | Should Not Throw
    }

    It "Should assign a value to a variable it has to create" {
        Set-Variable -Name testVar -Value 4

        Get-Variable testVar -ValueOnly | Should Be 4
    }

    It "Should change the value of an already existing variable" {
        $testVar=1

        $testVar | Should Not Be 2

        Set-Variable testVar -Value 2

        $testVar | Should Be 2
    }

    It "Should be able to be called with the set alias" {
        set testVar -Value 1

        $testVar | Should Be 1
    }

    It "Should be able to be called with the sv alias" {
        sv testVar -Value 2

        $testVar | Should Be 2
    }

    It "Should be able to set variable name using the Name parameter" {
        Set-Variable -Name testVar -Value 1

        $testVar | Should Be 1
    }

    It "Should be able to set the value of a variable by piped input" {
        $testValue = "piped input" 

        $testValue | Set-Variable -Name testVar

        $testVar | Should Be $testValue
    }

    It "Should be able to pipe object properties to output using the PassThru switch" {
        $in = Set-Variable -Name testVar -Value "test" -Description "test description" -PassThru

        $output = $in | Format-List -Property Description | Out-String

        # This will cause errors running these tests in Windows
        $output | Should Be "${nl}${nl}Description : test description${nl}${nl}${nl}${nl}"
    }

    It "Should be able to set the value using the value switch" {
        Set-Variable -Name testVar -Value 4

        $testVar | Should Be 4

        Set-Variable -Name testVar -Value "test"

        $testVar | Should Be "test"
    }

    Context "Scope Tests" {
        It "Should be able to set a global scope variable using the global switch" {
            { Set-Variable globalVar -Value 1 -Scope global -Force } | Should Not Throw
        }

        It "Should be able to set a global variable using the script scope switch" {
            { Set-Variable globalVar -Value 1 -Scope script -Force } | Should Not Throw
        }

        It "Should be able to set an item locally using the local switch" {
            { Set-Variable globalVar -Value 1 -Scope local -Force } | Should Not Throw
        }
    }
}
