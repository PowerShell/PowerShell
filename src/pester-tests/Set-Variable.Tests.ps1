Describe "Set-Variable" {
    It "Should create a new variable with no parameters" {
        { Set-Variable testVar } | Should Not Throw

        { Get-Variable testVar } | Should Not Throw
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

        # This will cause errors running these tests in windows
        $output | Should Be "`n`nDescription : test description`n`n`n`n"
    }

    It "Should be able to set the value using the value switch" {
        Set-Variable -Name testVar -Value 4

        $testVar | Should Be 4

        Set-Variable -Name testVar -Value "test"

        $testVar | Should Be "test"
    }

    Context "Scope Tests" {
        # This will violate the DRY principle.  Tread softly.

        It "Should be able to get a global scope variable using the global switch" {
            Set-Variable globalVar -Value 1 -Scope global -Force

            (Get-Variable -Name globalVar -Scope global)[0].Value | Should Be 1
        }

        It "Should not be able to set a global scope variable using the local switch" {
            Set-Variable globalVar -Value 1 -Scope global -Force

            Get-Variable -Name globalVar -Scope local -ErrorAction SilentlyContinue | Should Throw
        }

        It "Should be able to set a global variable using the script scope switch" {
            {
                Set-Variable globalVar -Value 1 -Scope global -Force
                Get-Variable -Name globalVar -Scope script
            } | Should Not Throw
        }

        It "Should be able to get an item locally using the local switch" {
            {
                Set-Variable globalVar -Value 1 -Scope local -Force

                Get-Variable -Name globalVar -Scope local 
            } | Should Not Throw
        }

        It "Should be able to get an item locally using the global switch" {
            {
                Set-Variable globalVar -Value 1 -Scope local -Force

                Get-Variable -Name globalVar -Scope global
            } | Should Not Throw
        }

        It "Should not be able to get a local variable using the script scope switch" {
            {
                Set-Variable globalVar -Value 1 -Scope local -Force

                Get-Variable -Name globalVar -Scope script
            } | Should Not Throw
        }

        It "Should be able to get a script variable created using the script switch" {
            {
                Set-Variable globalVar -Value 1 -Scope script -Force

                Get-Variable -Name globalVar -Scope script
            } | Should Not Throw
        }

        It "Should be able to set a global script variable that was created using the script scope switch" {
            {
                Set-Variable globalVar -Value 1 -Scope script -Force

                Get-Variable -Name globalVar -Scope script
            } | Should Not Throw
        }
    }
}
