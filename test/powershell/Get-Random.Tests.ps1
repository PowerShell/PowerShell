Describe "Get-Random" {
    It "Should return a random number greater than -1 " {
        Get-Random | Should BeGreaterThan -1 
    }
    It "Should return a random number less than 100 " {
        Get-Random -Maximum 100 | Should BeLessThan 100 
        Get-Random -Maximum 100 | Should BeGreaterThan -1 
    }

    It "Should return a random number less than 100 and greater than -100 " {
        $randomNumber = Get-Random -Minimum -100 -Maximum 100
        $randomNumber | Should BeLessThan 100
        $randomNumber | Should BeGreaterThan -101 
    }

    It "Should return a random number less than 20.93 and greater than 10.7 " {
        $randomNumber = Get-Random -Minimum 10.7 -Maximum 20.93
        $randomNumber | Should BeLessThan 20.93
        $randomNumber | Should BeGreaterThan 10.7 
    }

    It "Should return same number for both Get-Random when switch SetSeed is used " {
        $firstRandomNumber = Get-Random -Maximum 100 -SetSeed 23
        $secondRandomNumber = Get-Random -Maximum 100 -SetSeed 23
        $firstRandomNumber | Should be $secondRandomNumber
    }

    It "Should return a number from 1,2,3,5,8,13 " {
        $randomNumber = Get-Random -InputObject 1, 2, 3, 5, 8, 13
        $randomNumber | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
    }

    It "Should return an array " {
        $randomNumber = Get-Random -InputObject 1, 2, 3, 5, 8, 13 -Count 3
        $randomNumber.GetType().BaseType | Should Be array
    }

    It "Should return three random numbers for array of 1,2,3,5,8,13 " {
        $randomNumber = Get-Random -InputObject 1, 2, 3, 5, 8, 13 -Count 3
        $randomNumber[0] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[1] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[2] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[3] | Should BeNullOrEmpty 
    }

    It "Should return all the numbers for array of 1,2,3,5,8,13 in no particular order" {
        $randomNumber = Get-Random -InputObject 1, 2, 3, 5, 8, 13 -Count ([int]::MaxValue)
        $randomNumber[0] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[1] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[2] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[3] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[4] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[5] | Should Be (1 -or 2 -or 3 -or 5 -or 8 -or 13)
        $randomNumber[6] | Should BeNullOrEmpty 
    }

    It "Should return for a string collection " { 
        $randomNumber = Get-Random -InputObject "red", "yellow", "blue"
        $randomNumber | Should Be ("red" -or "yellow" -or "blue")
    }

    It "Should return a number for hexdecimal " { 
        $randomNumber = Get-Random 0x07FFFFFFFFF
        $randomNumber | Should BeLessThan 549755813887
        $randomNumber | Should BeGreaterThan 0
    }

    It "Should return false, check two random numbers are not equal when not using the SetSeed switch " {
        $firstRandomNumber = Get-Random
        $secondRandomNumber = Get-Random
        $firstRandomNumber | Should Not Be $secondRandomNumber
    }

    It "Should return the same number for hexidemical number and regular number when the switch SetSeed it used " {
        $firstRandomNumber = Get-Random 0x07FFFFFFFF -SetSeed 20
        $secondRandomNumber = Get-Random 34359738367 -SetSeed 20
        $firstRandomNumber | Should Be @secondRandomNumber 
    }
    It "Should throw an error because the hexidecial number is to large " { 
        { Get-Random 0x07FFFFFFFFFFFFFFFF } | Should Throw "Value was either too large or too small for a UInt32"
    }
}
