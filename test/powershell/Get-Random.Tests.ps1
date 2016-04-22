Describe "Get-Random DRT Unit Tests" -Tags DRT{
   
    # -minimum is always set to the actual low end of the range, details refer to closed issue #887
    It "Tests for random-numbers mode" {    
        # Test for Int32
        $results = get-random
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan ([int32]::MaxValue)

        $results = get-random 100
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan 100

        $x = 10000 
        $results = get-random $x
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan 10000

        $results = get-random -Minimum -100 -Maximum 0
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -101
        $results | Should BeLessThan 0

        $results = get-random -Minimum -100 -Maximum 100
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -101
        $results | Should BeLessThan 100

        $results = get-random -Minimum -200 -Maximum -100
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -201
        $results | Should BeLessThan -100

        $results = get-random -Minimum (-200) -Maximum (-100)
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -201
        $results | Should BeLessThan -100
        
        $results = get-random -Minimum 5 -Maximum '8'
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan 4
        $results | Should BeLessThan 8

        $results = get-random -Minimum '5' -Maximum 8
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan 4
        $results | Should BeLessThan 8
        
        $results = get-random -Minimum 0 -Maximum +100
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan 100

        $results = get-random -Minimum 0 -Maximum '+100'
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan 100

        $results = get-random -Minimum '-100' -Maximum '+100'
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -101
        $results | Should BeLessThan 100

        $results = get-random -Minimum 0 -Maximum '1kb'
        $results.GetType().FullName | Should Be System.Int32
        $results | Should BeGreaterThan -1
        $results | Should BeLessThan 1024

        #Test for Int64
        $results = get-random ([int64]::MaxValue)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-1)
        $results | Should BeLessThan ([int64]::MaxValue)

        $results = get-random ([int64]100)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-1)
        $results | Should BeLessThan ([int64]100)

        $results = get-random 100000000000
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-1)
        $results | Should BeLessThan ([int64]100000000000)

        $results = get-random -Minimum ([int64]-100) -Maximum ([int64]0)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-101)
        $results | Should BeLessThan ([int64]0)

        $results = get-random -Minimum ([int64]-100) -Maximum ([int64]100)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-101)
        $results | Should BeLessThan ([int64]100)

        $results = get-random -Minimum ([int64]-200) -Maximum ([int64]-100)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-201)
        $results | Should BeLessThan ([int64]-100)

        $results = get-random -Minimum ([int64](-200)) -Maximum ([int64](-100))
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-201)
        $results | Should BeLessThan ([int64]-100)

        
        $results = get-random -Minimum ([int64]5) -Maximum '8'
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]4)
        $results | Should BeLessThan ([int64]8)

        $results = get-random -Minimum '5' -Maximum ([int64]8)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]4)
        $results | Should BeLessThan ([int64]8)
        
        $results = get-random -Minimum ([int64]0) -Maximum +100
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-1)
        $results | Should BeLessThan ([int64]100)

        $results = get-random -Minimum ([int64]0) -Maximum '+100'
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-1)
        $results | Should BeLessThan ([int64]100)

        $results = get-random -Minimum '-100' -Maximum ([int64]0)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]-101)
        $results | Should BeLessThan ([int64]0)

        $results = get-random -Minimum '1mb' -Maximum ([int64]1048585)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]1048575)
        $results | Should BeLessThan ([int64]1048585)

        $results = get-random -Minimum '10mb' -Maximum '1tb'
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]10485759)
        $results | Should BeLessThan ([int64]1099511627776)

        $results = get-random -Minimum ([int64]::MinValue) -Maximum ([int64]::MaxValue)
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]::MinValue)
        $results | Should BeLessThan ([int64]::MaxValue)

        $results = get-random -Minimum ([int64](([int]::MaxValue)+10)) -Maximum ([int64](([int]::MaxValue)+15))
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]([int32]::MaxValue + 9))
        $results | Should BeLessThan ([int64]([int32]::MaxValue + 15))

        $results = get-random -Minimum ([int64](([int]::MaxValue)+100)) -Maximum ([int64](([int]::MaxValue)+150))
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]([int32]::MaxValue + 99))
        $results | Should BeLessThan ([int64]([int32]::MaxValue + 150))

        $results = get-random -Minimum 100000000001 -Maximum '100099000001'
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]100000000000)
        $results | Should BeLessThan ([int64]100099000001)

        $results = get-random -Minimum '100000002222' -Maximum 100000002230
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]100000002221)
        $results | Should BeLessThan ([int64]100000002230)

        $results = get-random -Minimum 4 -Maximum 90000000000
        $results.GetType().FullName | Should Be System.Int64
        $results | Should BeGreaterThan ([int64]3)
        $results | Should BeLessThan ([int64]90000000000)

        #Test for double
        $results = get-random 100.0
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -1.0
        $results | Should BeLessThan 100.0

        $results = get-random -Minimum -100.0 -Maximum 0.0
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -101.0
        $results | Should BeLessThan 0.0

        $results = get-random -Minimum -100.0 -Maximum 100.0
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -101.0
        $results | Should BeLessThan 100.0

        $results = get-random -Minimum 5 -Maximum 8.0
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan 4.0
        $results | Should BeLessThan 8.0

        $results = get-random -Minimum 5.0 -Maximum 8
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan 4.0
        $results | Should BeLessThan 8.0

        $results = get-random -Minimum 0.0 -Maximum 20.
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -1.0
        $results | Should BeLessThan 20.0

        $results = get-random -Minimum 0.0 -Maximum '20.'
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -1.0
        $results | Should BeLessThan 20.0

        $results = get-random -Minimum 0 -Maximum +100.0
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -1.0
        $results | Should BeLessThan 100.0

        $results = get-random -Minimum 0 -Maximum '+100.0'
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan -1.0
        $results | Should BeLessThan 100.0

        $results = get-random -Minimum 1.0e+100
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan 1.0e+99
        $results | Should BeLessThan ([double]::MaxValue)

        $results = get-random -minimum ([double]::MinValue) -maximum ([double]::MaxValue)
        $results.GetType().FullName | Should Be System.Double
        $results | Should BeGreaterThan ([double]::MinValue)
        $results | Should BeLessThan ([double]::MaxValue)

        #Verify Error
        { get-random -Minimum 20 -Maximum 10 } | Should Throw "The Minimum value (20) cannot be greater than or equal to the Maximum value (10)"
        { get-random -Minimum 20 -Maximum 20 } | Should Throw "The Minimum value (20) cannot be greater than or equal to the Maximum value (20)"
        { get-random -Minimum -10 -Maximum -20 } | Should Throw "The Minimum value (-10) cannot be greater than or equal to the Maximum value (-20)"
        { get-random -Minimum -20 -Maximum -20 } | Should Throw "The Minimum value (-20) cannot be greater than or equal to the Maximum value (-20)"
        { get-random -Minimum 20.0 -Maximum 10.0 } | Should Throw "The Minimum value (20) cannot be greater than or equal to the Maximum value (10)"
        { get-random -Minimum 20.0 -Maximum 20.0 } | Should Throw "The Minimum value (20) cannot be greater than or equal to the Maximum value (20)"
        { get-random -Minimum -10.0 -Maximum -20.0 } | Should Throw "The Minimum value (-10) cannot be greater than or equal to the Maximum value (-20)"
        { get-random -Minimum -20.0 -Maximum -20.0 } | Should Throw "The Minimum value (-20) cannot be greater than or equal to the Maximum value (-20)"
        { get-random -10 } | Should Throw "The Minimum value (0) cannot be greater than or equal to the Maximum value (-10)"
        { $x = -10; get-random $x } | Should Throw "The Minimum value (0) cannot be greater than or equal to the Maximum value (-10)"
    }

    It "Tests for setting the seed" {
        $result1 = get-random -SetSeed 123; get-random;
        $result2 = get-random -SetSeed 123; get-random;
        $result1 | Should Be $result2
    }
}

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
