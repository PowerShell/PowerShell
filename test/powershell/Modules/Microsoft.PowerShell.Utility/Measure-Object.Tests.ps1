# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Measure-Object" -Tags "CI" {
    BeforeAll {
        $testObject = 1,3,4
        $testObject2 = 1..100
    }

    It "Should be able to be called without error" {
        { Measure-Object | Out-Null } | Should -Not -Throw
    }

    It "Should be able to call on piped input" {
        { $testObject | Measure-Object } | Should -Not -Throw
    }

    It "Should be able to count the number of objects input to it" {
        $($testObject | Measure-Object).Count | Should -Be $testObject.Length
    }

    It "Should calculate Standard Deviation" {
        $actual = ($testObject | Measure-Object -StandardDeviation)
        # We check this way since .StandardDeviation returns a double value
        # 1.52752523165195 was calculated outside powershell using formula from
        # http://mathworld.wolfram.com/StandardDeviation.html
        [Math]::abs($actual.StandardDeviation - 1.52752523165195) | Should -BeLessThan .00000000000001
    }


    It "Should calculate Standard Deviation" {
        $actual = ($testObject2 | Measure-Object -StandardDeviation)
        # We check this way since .StandardDeviation returns a double value
        # 29.011491975882 was calculated outside powershell using formula from
        # http://mathworld.wolfram.com/StandardDeviation.html
        [Math]::abs($actual.StandardDeviation - 29.011491975882) | Should -BeLessThan .0000000000001
    }

    It "Should calculate Standard Deviation with -Sum" {
        $actual = ($testObject | Measure-Object -Sum -StandardDeviation)
        # We check this way since .StandardDeviation returns a double value
        $actual.Sum | Should Be 8
        # 1.52752523165195 was calculated outside powershell using formula from
        # http://mathworld.wolfram.com/StandardDeviation.html
        [Math]::abs($actual.StandardDeviation - 1.52752523165195) | Should -BeLessThan .00000000000001
    }

    It "Should calculate Standard Deviation with -Average" {
        $actual = ($testObject | Measure-Object -Average -StandardDeviation)
        # We check this way since .StandardDeviation returns a double value
        [Math]::abs($actual.Average - 2.66666666666667) | Should -BeLessThan .00000000000001
        # 1.52752523165195 was calculated outside powershell using formula from
        # http://mathworld.wolfram.com/StandardDeviation.html
        [Math]::abs($actual.StandardDeviation - 1.52752523165195) | Should -BeLessThan .00000000000001
    }

    It "Should calculate Standard Deviation with -Sum -Average" {
        $actual = ($testObject2 | Measure-Object -Sum -Average -StandardDeviation)
        # We check this way since .StandardDeviation returns a double value
        $actual.Sum | Should Be 5050
        $actual.Average | Should Be 50.5
        # 29.011491975882 was calculated outside powershell using formula from
        # http://mathworld.wolfram.com/StandardDeviation.html
        [Math]::abs($actual.StandardDeviation - 29.011491975882) | Should -BeLessThan .0000000000001
    }

    It "Should be able to count using the Property switch" {
        $expected = $(Get-ChildItem $TestDrive).Length
        $actual   = $(Get-ChildItem $TestDrive | Measure-Object -Property Length).Count

        $actual | Should -Be $expected
    }

    It "Should be able to use wildcards for the Property argument" {
        $data = [pscustomobject]@{ A1 = 1; A2 = 2; C3 = 3 },
                [pscustomobject]@{ A1 = 1; A2 = 2; A3 = 3 }
        $actual = $data | Measure-Object -Property A* -Sum
        $actual.Count       | Should -Be 3
        $actual[0].Property | Should -Be A1
        $actual[0].Sum      | Should -Be 2
        $actual[0].Count    | Should -Be 2
        $actual[1].Property | Should -Be A2
        $actual[1].Sum      | Should -Be 4
        $actual[1].Count    | Should -Be 2
        $actual[2].Property | Should -Be A3
        $actual[2].Sum      | Should -Be 3
        $actual[2].Count    | Should -Be 1
    }

    Context "Numeric tests" {
        It "Should be able to sum" {
            $actual   = $testObject | Measure-Object -Sum
            $expected = 0

            foreach ( $obj in $testObject )
            {
                $expected += $obj
            }

            $actual.Sum | Should -Be $expected
        }

        It "Should be able to average" {
            $actual   = $testObject | Measure-Object -Average
            $expected = 0

            foreach ( $obj in $testObject )
            {
                $expected += $obj
            }

            $expected /= $testObject.length

            $actual.Average | Should -Be $expected
        }

        It "Should be able to return a minimum" {
            $actual   = $testObject | Measure-Object -Minimum
            $expected = $testObject[0]

            for ($i=0; $i -lt $testObject.length; $i++)
            {
                if ( $testObject[$i] -lt $expected )
                {
                    $expected = $testObject[$i]
                }
            }

            $actual.Minimum | Should -Be $expected
        }

        It "Should be able to return a minimum when multiple objects are the minimum" {
            $testMinimum = 1,1,2,4
            $actual      = $testMinimum | Measure-Object -Minimum
            $expected    = $testMinimum[0]

            for ($i=1; $i -lt $testMinimum.length; $i++)
            {
                if ( $testMinimum[$i] -lt $expected )
                {
                    $expected = $testMinimum[$i]
                }
            }

            $actual.Minimum | Should -Be $expected
        }

        It "Should be able to return a maximum" {
            $actual   = $testObject | Measure-Object -Maximum
            $expected = $testObject[0]

            for ($i=1; $i -lt $testObject.length; $i++)
            {
                if ( $testObject[$i] -gt $expected )
                {
                    $expected = $testObject[$i]
                }
            }

            $actual.Maximum | Should -Be $expected
        }

        It "Should be able to return a maximum when multiple objects are the maximum" {
            $testMaximum = 1,3,5,5
            $actual      = $testMaximum | Measure-Object -Maximum
            $expected    = $testMaximum[0]

            for ($i=1; $i -lt $testMaximum.length; $i++)
            {
                if ( $testMaximum[$i] -gt $expected )
                {
                    $expected = $testMaximum[$i]
                }
            }

            $actual.Maximum | Should -Be $expected
        }

        It "Should be able to return all the statitics for given values" {
            $result = 1..10  | Measure-Object -AllStats
            $result.Count    | Should -Be 10
            $result.Average  | Should -Be 5.5
            $result.Sum      | Should -Be 55
            $result.Minimum  | Should -Be 1
            $result.Maximum  | Should -Be 10
            ($result.StandardDeviation).ToString()  | Should -Be '3.02765035409749'
        }
    }

    Context "String tests" {
        BeforeAll {
            $nl = [Environment]::NewLine
            $testString = "HAD I the heavens' embroidered cloths,$nl Enwrought with golden and silver light,$nl The blue and the dim and the dark cloths$nl Of night and light and the half light,$nl I would spread the cloths under your feet:$nl But I, being poor, have only my dreams;$nl I have spread my dreams under your feet;$nl Tread softly because you tread on my dreams."
        }

        It "Should be able to count the number of words in a string" {
            $expectedLength = $testString.Replace($nl,"").Split().length
            $actualLength   = $testString | Measure-Object -Word

            $actualLength.Words | Should -Be $expectedLength
        }

        It "Should be able to count the number of characters in a string" {
            $expectedLength = $testString.length
            $actualLength   = $testString | Measure-Object -Character

            $actualLength.Characters | Should -Be $expectedLength
        }

        It "Should be able to count the number of lines in a string" {
            $expectedLength = $testString.Split($nl, [System.StringSplitOptions]::RemoveEmptyEntries).length
            $actualLength   = $testString | Measure-Object -Line

            $actualLength.Lines | Should -Be $expectedLength
        }
    }
}

Describe "Measure-Object DRT basic functionality" -Tags "CI" {
    BeforeAll {
        if(-not ([System.Management.Automation.PSTypeName]'TestMeasureGeneric').Type)
        {
            Add-Type -TypeDefinition @"
    [System.Flags]
    public enum TestMeasureGeneric : uint
    {
        TestSum = 1,
        TestAverage = 2,
        TestMax = 4,
        TestMin = 8
    }
"@
        }

        if(-not ([System.Management.Automation.PSTypeName]'TestMeasureText').Type)
        {
            Add-Type -TypeDefinition @"
    [System.Flags]
    public enum TestMeasureText : uint
    {
        TestIgnoreWS = 1,
        TestCharacter = 2,
        TestWord = 4,
        TestLine = 8
    }
"@
        }

        $employees = [pscustomobject]@{"FirstName"="joseph"; "LastName"="smith"; "YearsInMS"=15},
                            [pscustomobject]@{"FirstName"="paul"; "LastName"="smith"; "YearsInMS"=15},
                            [pscustomobject]@{"FirstName"="mary jo"; "LastName"="soe"; "YearsInMS"=5},
                            [pscustomobject]@{"FirstName"="edmund`todd `n"; "LastName"="bush"; "YearsInMS"=9}
    }

    It "Measure-Object with Generic enum value options combination should work"{
        $flags = [TestMeasureGeneric]0
        $property = "FirstName"
        $testSum = ($flags -band [TestMeasureGeneric]::TestSum) -gt 0
        $testAverage = ($flags -band [TestMeasureGeneric]::TestAverage) -gt 0
        $testMax = ($flags -band [TestMeasureGeneric]::TestMax) -gt 0
        $testMin = ($flags -band [TestMeasureGeneric]::TestMin) -gt 0
        $result = $employees | Measure-Object -Sum:$testSum -Average:$testAverage -Max:$testMax -Min:$testMin -Prop $property
        $result.Count   | Should -Be 4
        $result.Sum     | Should -BeNullOrEmpty
        $result.Average | Should -BeNullOrEmpty
        $result.Max     | Should -BeNullOrEmpty
        $result.Min     | Should -BeNullOrEmpty
        for ($i = 1; $i -lt 8 * 2; $i++)
        {
            $flags = [TestMeasureGeneric]$i
            $property = "YearsInMS"
            $testSum = ($flags -band [TestMeasureGeneric]::TestSum) -gt 0
            $testAverage = ($flags -band [TestMeasureGeneric]::TestAverage) -gt 0
            $testMax = ($flags -band [TestMeasureGeneric]::TestMax) -gt 0
            $testMin = ($flags -band [TestMeasureGeneric]::TestMin) -gt 0
            $result = $employees | Measure-Object -Sum:$testSum -Average:$testAverage -Max:$testMax -Min:$testMin -Prop $property
            $result.Count | Should -Be 4
            if($testSum)
            {
                $result.Sum | Should -Be 44
            }
            else
            {
                $result.Sum | Should -BeNullOrEmpty
            }

            if($testAverage)
            {
                $result.Average | Should -Be 11
            }
            else
            {
                $result.Average | Should -BeNullOrEmpty
            }

            if($testMax)
            {
                $result.Maximum | Should -Be 15
            }
            else
            {
                $result.Maximum | Should -BeNullOrEmpty
            }

            if($testMin)
            {
                $result.Minimum | Should -Be 5
            }
            else
            {
                $result.Minimum | Should -BeNullOrEmpty
            }
        }
    }

    It "Measure-Object with Text combination should work"{
        for ($i = 1; $i -lt 8 * 2; $i++)
        {
            $flags = [TestMeasureText]$i
            $property = "FirstName"
            $testIgnoreWS = ($flags -band [TestMeasureText]::TestIgnoreWS) -gt 0
            $testCharacter = ($flags -band [TestMeasureText]::TestCharacter) -gt 0
            $testWord = ($flags -band [TestMeasureText]::TestWord) -gt 0
            $testLine = ($flags -band [TestMeasureText]::TestLine) -gt 0
            $result = $employees | Measure-Object -IgnoreWhiteSpace:$testIgnoreWS -Character:$testCharacter -Word:$testWord -Line:$testLine -Prop $property

            if($testCharacter)
            {
                if($testIgnoreWS)
                {
                    $result.Characters | Should -Be 25
                }
                else
                {
                    $result.Characters | Should -Be 29
                }
            }
            else
            {
                $result.Characters | Should -BeNullOrEmpty
            }

            if($testWord)
            {
                $result.Words | Should -Be 6
            }
            else
            {
                $result.Words | Should -BeNullOrEmpty
            }

            if($testLine)
            {
                $result.Lines | Should -Be 4
            }
            else
            {
                $result.Lines | Should -BeNullOrEmpty
            }
        }
    }

    It "Measure-Object with ScriptBlock properties should work" {
        $result = 1..10 | Measure-Object -Sum -Average -Minimum -Maximum -Property {$_ * 10}
        $result.Count    | Should -Be 10
        $result.Average  | Should -Be 55
        $result.Sum      | Should -Be 550
        $result.Minimum  | Should -Be 10
        $result.Maximum  | Should -Be 100
        $result.Property | Should -Be '$_ * 10'
    }

    It "Measure-Object with ScriptBlock properties should work with -word" {
        $result = "a,b,c" | Measure-Object -Word  {$_ -split ','}
        $result.Words | Should -Be 3
    }

    It "Measure-Object ScriptBlock properties should be able to transform input" {
        $map = @{ one = 1; two = 2; three = 3 }
        $result = "one", "two", "three" | Measure-Object -Sum {$map[$_]}
        $result.Sum | Should -Be 6
    }

    It "Measure-Object should handle hashtables as objects" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum fo*
        $result.Sum | Should -Be 14
    }

    It "Measure-Object should handle hashtables as objects with ScriptBlock properties" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum {$_.foo * 10 }
        $result.Sum | Should -Be 140
    }

    #
    # Since PSPropertyExtression is now a public type, this function is used to test its
    # operation as a parameter on a PowerShell function, independent of Measure-Object
    #
    function Test-PSPropertyExpression {
        [CmdletBinding()]
        param (
            [Parameter(Mandatory,Position=0)]
            [PSPropertyExpression]
                $pe,
            [Parameter(ValueFromPipeline)]
                $InputObject
        )
        begin { $sum = 0}
        process { $sum += $pe.GetValues($InputObject).result }
        end { $sum }
    }

    It "Test-PropertyExpression function with a wildcard property expression should sum numbers" {
        $result = (1..10).ForEach{@{value = $_}} | Test-PSPropertyExpression val*
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should sum numbers" {
        $result = 1..10 | Test-PSPropertyExpression {$_}
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should be able to transform input" {
        # Count the number of 'e's in the words.
        $result = "one", "two", "three", "four", "five" | Test-PSPropertyExpression {($_.ToCharArray() -match 'e').Count}
        $result | Should -Be 4
    }
    It "Measure-Object with multiple lines should work"{
        $result = "123`n4" | Measure-Object -Line
        $result.Lines | Should -Be 2
    }

    It "Measure-Object with ScriptBlock properties should work" {
        $result = 1..10 | Measure-Object -Sum -Average -Minimum -Maximum -Property {$_ * 10}
        $result.Count    | Should -Be 10
        $result.Average  | Should -Be 55
        $result.Sum      | Should -Be 550
        $result.Minimum  | Should -Be 10
        $result.Maximum  | Should -Be 100
        $result.Property | Should -Be '$_ * 10'
    }

    It "Measure-Object with ScriptBlock properties should work with -word" {
        $result = "a,b,c", "d,e" | Measure-Object -Word  {$_ -split ','}
        $result.Words | Should -Be 5
    }

    It "Measure-Object ScriptBlock properties should be able to transform input" {
        $map = @{ one = 1; two = 2; three = 3 }
        $result = "one", "two", "three" | Measure-Object -Sum {$map[$_]}
        $result.Sum | Should -Be 6
    }

    It "Measure-Object should handle hashtables as objects" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum fo*
        $result.Sum | Should -Be 14
    }

    It "Measure-Object should handle hashtables as objects with ScriptBlock properties" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum {$_.foo * 10 }
        $result.Sum | Should -Be 140
    }
}

# Since PSPropertyExpression is now a public type, it can be tested
# directly, independent of the Measure-Object cmdlet
Describe "Directly test the PSPropertyExpression type" -Tags "CI" {
    # this function is used to test the use of PSPropertyExpression
    # as a parameter in script,
    function Test-PSPropertyExpression {
        [CmdletBinding()]
        param (
            [Parameter(Mandatory,Position=0)]
            [PSPropertyExpression]
                $pe,
            [Parameter(ValueFromPipeline)]
                $InputObject
        )
        begin { $sum = 0}
        process { $sum += $pe.GetValues($InputObject).result }
        end { $sum }
    }

    It "Test-PropertyExpression function with a wildcard property expression should sum numbers" {
        $result = (1..10).ForEach{@{value = $_}} | Test-PSPropertyExpression val*
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should sum numbers" {
        $result = 1..10 | Test-PSPropertyExpression {$_}
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should be able to transform input" {
        # Count the number of 'e's in the words.
        $result = "one", "two", "three", "four", "five" | Test-PSPropertyExpression {($_.ToCharArray() -match 'e').Count}
        $result | Should -Be 4
    }

    It "Measure-Object with multiple lines should work"{
        $result = "123`n4" | Measure-Object -Line
        $result.Lines | Should -Be 2
    }

    It "Measure-Object with ScriptBlock properties should work" {
        $result = 1..10 | Measure-Object -Sum -Average -Minimum -Maximum -Property {$_ * 10}
        $result.Count    | Should -Be 10
        $result.Average  | Should -Be 55
        $result.Sum      | Should -Be 550
        $result.Minimum  | Should -Be 10
        $result.Maximum  | Should -Be 100
        $result.Property | Should -Be '$_ * 10'
    }

    It "Measure-Object with ScriptBlock properties should work with -word" {
        $result = "a,b,c", "d,e" | Measure-Object -Word  {$_ -split ','}
        $result.Words | Should -Be 5
    }

    It "Measure-Object ScriptBlock properties should be able to transform input" {
        $map = @{ one = 1; two = 2; three = 3 }
        $result = "one", "two", "three" | Measure-Object -Sum {$map[$_]}
        $result.Sum | Should -Be 6
    }

    It "Measure-Object should handle hashtables as objects" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum fo*
        $result.Sum | Should -Be 14
    }

    It "Measure-Object should handle hashtables as objects with ScriptBlock properties" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum {$_.foo * 10 }
        $result.Sum | Should -Be 140
    }
}

# Since PSPropertyExpression is now a public type, it can be tested
# directly, independent of the Measure-Object cmdlet
Describe "Directly test the PSPropertyExpression type" -Tags "CI" {
    # this function is used to test the use of PSPropertyExpression
    # as a parameter in script,
    function Test-PSPropertyExpression {
        [CmdletBinding()]
        param (
            [Parameter(Mandatory,Position=0)]
            [PSPropertyExpression]
                $pe,
            [Parameter(ValueFromPipeline)]
                $InputObject
        )
        begin { $sum = 0}
        process { $sum += $pe.GetValues($InputObject).result }
        end { $sum }
    }

    It "Test-PropertyExpression function with a wildcard property expression should sum numbers" {
        $result = (1..10).ForEach{@{value = $_}} | Test-PSPropertyExpression val*
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should sum numbers" {
        $result = 1..10 | Test-PSPropertyExpression {$_}
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should be able to transform input" {
        # Count the number of 'e's in the words.
        $result = "one", "two", "three", "four", "five" | Test-PSPropertyExpression {($_.ToCharArray() -match 'e').Count}
        $result | Should -Be 4
    }
    It "Measure-Object with multiple lines should work"{
        $result = "123`n4" | Measure-Object -Line
        $result.Lines | Should -Be 2
    }

    It "Measure-Object with ScriptBlock properties should work" {
        $result = 1..10 | Measure-Object -Sum -Average -Minimum -Maximum -Property {$_ * 10}
        $result.Count    | Should -Be 10
        $result.Average  | Should -Be 55
        $result.Sum      | Should -Be 550
        $result.Minimum  | Should -Be 10
        $result.Maximum  | Should -Be 100
        $result.Property | Should -Be '$_ * 10'
    }

    It "Measure-Object with ScriptBlock properties should work with -word" {
        $result = "a,b,c", "d,e" | Measure-Object -Word  {$_ -split ','}
        $result.Words | Should -Be 5
    }

    It "Measure-Object ScriptBlock properties should be able to transform input" {
        $map = @{ one = 1; two = 2; three = 3 }
        $result = "one", "two", "three" | Measure-Object -Sum {$map[$_]}
        $result.Sum | Should -Be 6
    }

    It "Measure-Object should handle hashtables as objects" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum fo*
        $result.Sum | Should -Be 14
    }

    It "Measure-Object should handle hashtables as objects with ScriptBlock properties" {
        $htables = @{foo = 1}, @{foo = 3}, @{foo = 10}
        $result = $htables | Measure-Object -Sum {$_.foo * 10 }
        $result.Sum | Should -Be 140
    }
}

# Since PSPropertyExpression is now a public type, it can be tested
# directly, independent of the Measure-Object cmdlet
Describe "Directly test the PSPropertyExpression type" -Tags "CI" {
    # this function is used to test the use of PSPropertyExpression
    # as a parameter in script,
    function Test-PSPropertyExpression {
        [CmdletBinding()]
        param (
            [Parameter(Mandatory,Position=0)]
            [PSPropertyExpression]
                $pe,
            [Parameter(ValueFromPipeline)]
                $InputObject
        )
        begin { $sum = 0}
        process { $sum += $pe.GetValues($InputObject).result }
        end { $sum }
    }

    It "Test-PropertyExpression function with a wildcard property expression should sum numbers" {
        $result = (1..10).ForEach{@{value = $_}} | Test-PSPropertyExpression val*
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should sum numbers" {
        $result = 1..10 | Test-PSPropertyExpression {$_}
        $result | Should -Be 55
    }

    It "Test-PropertyExpression function with a scriptblock property expression should be able to transform input" {
        # Count the number of 'e's in the words.
        $result = "one", "two", "three", "four", "five" | Test-PSPropertyExpression {($_.ToCharArray() -match 'e').Count}
        $result | Should -Be 4
    }
}
