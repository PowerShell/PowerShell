# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-SecureRandom DRT Unit Tests" -Tags "CI" {
    $testData = @(
        @{ Name = 'no params'; Maximum = $null; Minimum = $null; GreaterThan = -1; LessThan = ([int32]::MaxValue); Type = 'System.Int32' }
        @{ Name = 'only positive maximum number'; Maximum = 100; Minimum = $null; GreaterThan = -1; LessThan = 100; Type = 'System.Int32' }
        @{ Name = 'maximum set to 0, Minimum to a negative number'; Maximum = 0; Minimum = -100; GreaterThan = -101; LessThan = 0; Type = 'System.Int32' }
        @{ Name = 'positive maximum, negative Minimum'; Maximum = 100; Minimum = -100; GreaterThan = -101; LessThan = 100; Type = 'System.Int32' }
        @{ Name = 'both negative'; Maximum = -100; Minimum = -200; GreaterThan = -201; LessThan = -100; Type = 'System.Int32' }
        @{ Name = 'both negative with parentheses'; Maximum = (-100); Minimum = (-200); GreaterThan = -201; LessThan = -100; Type = 'System.Int32' }
        @{ Name = 'maximum enclosed in quote'; Maximum = '8'; Minimum = 5; GreaterThan = 4; LessThan = 8; Type = 'System.Int32' }
        @{ Name = 'minimum enclosed in quote'; Maximum = 8; Minimum = '5'; GreaterThan = 4; LessThan = 8; Type = 'System.Int32' }
        @{ Name = 'maximum with plus sign'; Maximum = +100; Minimum = 0; GreaterThan = -1; LessThan = 100; Type = 'System.Int32' }
        @{ Name = 'maximum with plus sign and quote'; Maximum = '+100'; Minimum = 0; GreaterThan = -1; LessThan = 100; Type = 'System.Int32' }
        @{ Name = 'both with quote'; Maximum = '+100'; Minimum = '-100'; GreaterThan = -101; LessThan = 100; Type = 'System.Int32' }
        @{ Name = 'maximum set to kb'; Maximum = '1kb'; Minimum = 0; GreaterThan = -1; LessThan = 1024; Type = 'System.Int32' }
        @{ Name = 'maximum is Int64.MaxValue'; Maximum = ([int64]::MaxValue); Minimum = $null; GreaterThan = ([int64]-1); LessThan = ([int64]::MaxValue); Type = 'System.Int64' }
        @{ Name = 'maximum is a 64-bit integer'; Maximum = ([int64]100); Minimum = $null; GreaterThan = ([int64]-1); LessThan = ([int64]100); Type = 'System.Int64' }
        @{ Name = 'maximum set to a large integer greater than int32.MaxValue'; Maximum = 100000000000; Minimum = $null; GreaterThan = ([int64]-1); LessThan = ([int64]100000000000); Type = 'System.Int64' }
        @{ Name = 'maximum set to 0, Minimum set to a negative 64-bit integer'; Maximum = ([int64]0); Minimum = ([int64]-100); GreaterThan = ([int64]-101); LessThan = ([int64]0); Type = 'System.Int64' }
        @{ Name = 'maximum set to positive 64-bit number, min set to negative 64-bit number'; Maximum = ([int64]100); Minimum = ([int64]-100); GreaterThan = ([int64]-101); LessThan = ([int64]100); Type = 'System.Int64' }
        @{ Name = 'both are negative 64-bit number'; Maximum = ([int64]-100); Minimum = ([int64]-200); GreaterThan = ([int64]-201); LessThan = ([int64]-100); Type = 'System.Int64' }
        @{ Name = 'both are negative 64-bit number with parentheses'; Maximum = ([int64](-100)); Minimum = ([int64](-200)); GreaterThan = ([int64]-201); LessThan = ([int64]-100); Type = 'System.Int64' }
        @{ Name = 'max is 32-bit, min is 64-bit integer'; Maximum = '8'; Minimum = ([int64]5); GreaterThan = ([int64]4); LessThan = ([int64]8); Type = 'System.Int64' }
        @{ Name = 'max is 64-bit, min is 32-bit integer'; Maximum = ([int64]8); Minimum = '5'; GreaterThan = ([int64]4); LessThan = ([int64]8); Type = 'System.Int64' }
        @{ Name = 'max set to a 32-bit integer, min set to [int64]0'; Maximum = +100; Minimum = ([int64]0); GreaterThan = ([int64]-1); LessThan = ([int64]100); Type = 'System.Int64' }
        @{ Name = 'max set to a 32-bit integer with quote'; Maximum = '+100'; Minimum = ([int64]0); GreaterThan = ([int64]-1); LessThan = ([int64]100); Type = 'System.Int64' }
        @{ Name = 'max is [int64]0, min is a 32-bit integer'; Maximum = ([int64]0); Minimum = '-100'; GreaterThan = ([int64]-101); LessThan = ([int64]0); Type = 'System.Int64' }
        @{ Name = 'min set to 1MB, max set to a 64-bit integer greater than min'; Maximum = ([int64]1048585); Minimum = '1mb'; GreaterThan = ([int64]1048575); LessThan = ([int64]1048585); Type = 'System.Int64' }
        @{ Name = 'max set to 1tb, min set to 10 mb'; Maximum = '1tb'; Minimum = '10mb'; GreaterThan = ([int64]10485759); LessThan = ([int64]1099511627776); Type = 'System.Int64' }
        @{ Name = 'max is int64.MaxValue, min is Int64.MinValue'; Maximum = ([int64]::MaxValue); Minimum = ([int64]::MinValue); GreaterThan = ([int64]::MinValue); LessThan = ([int64]::MaxValue); Type = 'System.Int64' }
        @{ Name = 'both are int64.MaxValue plus a 32-bit integer'; Maximum = ([int64](([int]::MaxValue)+15)); Minimum = ([int64](([int]::MaxValue)+10)); GreaterThan = ([int64](([int]::MaxValue)+9)); LessThan = ([int64](([int]::MaxValue)+15)); Type = 'System.Int64' }
        @{ Name = 'both are greater than int32.MaxValue without specified type, and max with quote'; Maximum = '100099000001'; Minimum = 100000000001; GreaterThan = ([int64]10000000000); LessThan = ([int64]100099000001); Type = 'System.Int64' }
        @{ Name = 'both are greater than int32.MaxValue without specified type, and min with quote'; Maximum = 100000002230; Minimum = '100000002222'; GreaterThan = ([int64]100000002221); LessThan = ([int64]100000002230); Type = 'System.Int64' }
        @{ Name = 'max is greater than int32.MaxValue without specified type'; Maximum = 90000000000; Minimum = 4; GreaterThan = ([int64]3); LessThan = ([int64]90000000000); Type = 'System.Int64' }
        @{ Name = 'max is a double-precision number'; Maximum = 100.0; Minimum = $null; GreaterThan = -1.0; LessThan = 100.0; Type = 'System.Double' }
        @{ Name = 'both are double-precision numbers, min is negative.'; Maximum = 0.0; Minimum = -100.0; GreaterThan = -101.0; LessThan = 0.0; Type = 'System.Double' }
        @{ Name = 'both are double-precision number, max is positive, min is negative.'; Maximum = 100.0; Minimum = -100.0; GreaterThan = -101.0; LessThan = 100.0; Type = 'System.Double' }
        @{ Name = 'max is a double-precision number, min is int32'; Maximum = 8.0; Minimum = 5; GreaterThan = 4.0; LessThan = 8.0; Type = 'System.Double' }
        @{ Name = 'min is a double-precision number, max is int32'; Maximum = 8; Minimum = 5.0; GreaterThan = 4.0; LessThan = 8.0; Type = 'System.Double' }
        @{ Name = 'max set to a special double number'; Maximum = 20.; Minimum = 0.0; GreaterThan = -1.0; LessThan = 20.0; Type = 'System.Double' }
        @{ Name = 'max is double with quote'; Maximum = '20.'; Minimum = 0.0; GreaterThan = -1.0; LessThan = 20.0; Type = 'System.Double' }
        @{ Name = 'max is double with plus sign'; Maximum = +100.0; Minimum = 0; GreaterThan = -1.0; LessThan = 100.0; Type = 'System.Double' }
        @{ Name = 'max is double with plus sign and enclosed in quote'; Maximum = '+100.0'; Minimum = 0; GreaterThan = -1.0; LessThan = 100.0; Type = 'System.Double' }
        @{ Name = 'both set to the special numbers as 1.0e+xx '; Maximum = $null; Minimum = 1.0e+100; GreaterThan = 1.0e+99; LessThan = ([double]::MaxValue); Type = 'System.Double' }
        @{ Name = 'max is Double.MaxValue, min is Double.MinValue'; Maximum = ([double]::MaxValue); Minimum = ([double]::MinValue); GreaterThan = ([double]::MinValue); LessThan = ([double]::MaxValue); Type = 'System.Double' }
    )

    $testDataForError = @(
        @{ Name = 'Min is greater than max and all are positive 32-bit integer'; Maximum = 10; Minimum = 20}
        @{ Name = 'Min and Max are same and all are positive 32-bit integer'; Maximum = 20; Minimum = 20}
        @{ Name = 'Min is greater than max and all are negative 32-bit integer'; Maximum = -20; Minimum = -10}
        @{ Name = 'Min and Max are same and all are negative 32-bit integer'; Maximum = -20; Minimum = -20}
        @{ Name = 'Min is greater than max and all are positive double-precision number'; Maximum = 10.0; Minimum = 20.0}
        @{ Name = 'Min and Max are same and all are positive double-precision number'; Maximum = 20.0; Minimum = 20.0}
        @{ Name = 'Min is greater than max and all are negative double-precision number'; Maximum = -20.0; Minimum = -10.0}
        @{ Name = 'Min and Max are same and all are negative double-precision number'; Maximum = -20.0; Minimum = -20.0}
        @{ Name = 'Max is a negative number, min is the default number '; Maximum = -10; Minimum = $null}
    )

    # minimum is always set to the actual low end of the range, details refer to closed issue #887.
    It "Should return a correct random number for '<Name>'" -TestCases $testData {
        param($maximum, $minimum, $greaterThan, $lessThan, $type)

        $result = Get-SecureRandom -Maximum $maximum -Minimum $minimum
        $result | Should -BeGreaterThan $greaterThan
        $result | Should -BeLessThan $lessThan
        $result | Should -BeOfType $type
    }

    It "Should return correct random numbers for '<Name>' with Count specified" -TestCases $testData {
        param($maximum, $minimum, $greaterThan, $lessThan, $type)

        $result = Get-SecureRandom -Maximum $maximum -Minimum $minimum -Count 1
        $result | Should -BeGreaterThan $greaterThan
        $result | Should -BeLessThan $lessThan
        $result | Should -BeOfType $type

        $result = Get-SecureRandom -Maximum $maximum -Minimum $minimum -Count 3
        foreach ($randomNumber in $result) {
            $randomNumber | Should -BeGreaterThan $greaterThan
            $randomNumber | Should -BeLessThan $lessThan
            $randomNumber | Should -BeOfType $type
        }
    }

    It "Should be able to throw error when '<Name>'" -TestCases $testDataForError {
        param($maximum, $minimum)
        { Get-SecureRandom -Minimum $minimum -Maximum $maximum } | Should -Throw -ErrorId "MinGreaterThanOrEqualMax,Microsoft.PowerShell.Commands.GetSecureRandomCommand"
    }
}

Describe "Get-SecureRandom" -Tags "CI" {
    It "Should return a random number greater than -1" {
        Get-SecureRandom | Should -BeGreaterThan -1
    }

    It "Should return a random number less than 100" {
        Get-SecureRandom -Maximum 100 | Should -BeLessThan 100
        Get-SecureRandom -Maximum 100 | Should -BeGreaterThan -1
    }

    It "Should return a random number less than 100 and greater than -100 " {
        $randomNumber = Get-SecureRandom -Minimum -100 -Maximum 100
        $randomNumber | Should -BeLessThan 100
        $randomNumber | Should -BeGreaterThan -101
    }

    It "Should return a random number less than 20.93 and greater than 10.7 " {
        $randomNumber = Get-SecureRandom -Minimum 10.7 -Maximum 20.93
        $randomNumber | Should -BeLessThan 20.93
        $randomNumber | Should -BeGreaterThan 10.7
    }

    It "Should return a number from 1,2,3,5,8,13 " {
        $randomNumber = Get-SecureRandom -InputObject 1, 2, 3, 5, 8, 13
        $randomNumber | Should -BeIn 1, 2, 3, 5, 8, 13
    }

    It "Should return an array " {
        $randomNumber = Get-SecureRandom -InputObject 1, 2, 3, 5, 8, 13 -Count 3
        $randomNumber.Count | Should -Be 3
        ,$randomNumber | Should -BeOfType System.Array
    }

    It "Should return three random numbers for array of 1,2,3,5,8,13 " {
        $randomNumber = Get-SecureRandom -InputObject 1, 2, 3, 5, 8, 13 -Count 3
        $randomNumber.Count | Should -Be 3
        $randomNumber[0] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[1] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[2] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[3] | Should -BeNullOrEmpty
    }

    It "Should return all the numbers for array of 1,2,3,5,8,13 in no particular order" {
        $randomNumber = Get-SecureRandom -InputObject 1, 2, 3, 5, 8, 13 -Count ([int]::MaxValue)
        $randomNumber.Count | Should -Be 6
        $randomNumber[0] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[1] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[2] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[3] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[4] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[5] | Should -BeIn 1, 2, 3, 5, 8, 13
        $randomNumber[6] | Should -BeNullOrEmpty
    }

    It "Should return all the numbers for array of 1,2,3,5,8,13 in randomized order when the Shuffle switch is used" {
        $randomNumber = Get-SecureRandom -InputObject 1, 2, 3, 5, 8, 13 -Shuffle
        $randomNumber.Count | Should -Be 6
        $randomNumber | Should -BeIn 1, 2, 3, 5, 8, 13
    }

    It "Should return for a string collection " {
        $randomNumber = Get-SecureRandom -InputObject "red", "yellow", "blue"
        $randomNumber | Should -Be ("red" -or "yellow" -or "blue")
    }

    It "Should return a number for hexadecimal " {
        $randomNumber = Get-SecureRandom 0x07FFFFFFFFF
        $randomNumber | Should -BeLessThan 549755813887
        $randomNumber | Should -BeGreaterThan 0
    }

    It "Should throw an error because the hexadecimal number is to large " {
        { Get-SecureRandom 0x07FFFFFFFFFFFFFFFF } | Should -Throw "Value was either too large or too small for a UInt32"
    }

    It "Should accept collection containing empty string for -InputObject" {
        1..10 | ForEach-Object {
            Get-SecureRandom -InputObject @('a','b','') | Should -BeIn 'a','b',''
        }
    }

    It "Should accept `$null in collection for -InputObject" {
        1..10 | ForEach-Object {
            Get-SecureRandom -InputObject @('a','b',$null) | Should -BeIn 'a','b',$null
        }
    }

    It 'Should not have a -SetSeed parameter' {
        (Get-Command Get-SecureRandom).Parameters['SetSeed'] | Should -BeNullOrEmpty
    }
}
