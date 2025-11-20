# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Add-TestDynamicType

Describe "Where-Object" -Tags "CI" {
    BeforeAll {
        $Computers = @(
            [PSCustomObject]@{
                ComputerName = "SPC-1234"
                IPAddress = "192.168.0.1"
                NumberOfCores = 1
                Drives = 'C','D'
            },
            [PSCustomObject]@{
                ComputerName = "BGP-5678"
                IPAddress = ""
                NumberOfCores = 2
                Drives = 'C','D','E'
            },
            [PSCustomObject]@{
                ComputerName = "MGC-9101"
                NumberOfCores = 3
                Drives = 'C'
            }
        )
    }

    It "Where-Object -Not Prop" {
        $Result = $Computers | Where-Object -Not 'IPAddress'
        $Result | Should -HaveCount 2
    }

    It 'Where-Object -FilterScript {$true -ne $_.Prop}' {
        $Result = $Computers | Where-Object -FilterScript {$true -ne $_.IPAddress}
        $Result | Should -HaveCount 2
    }

    It "Where-Object Prop" {
        $Result = $Computers | Where-Object 'IPAddress'
        $Result | Should -HaveCount 1
    }

    It 'Where-Object -FilterScript {$true -eq $_.Prop}' {
        $Result = $Computers | Where-Object -FilterScript {$true -eq $_.IPAddress}
        $Result | Should -HaveCount 1
    }

    It 'Where-Object -FilterScript {$_.Prop -contains Value}' {
        $Result = $Computers | Where-Object -FilterScript {$_.Drives -contains 'D'}
        $Result | Should -HaveCount 2
    }

    It 'Where-Object Prop -contains Value' {
        $Result = $Computers | Where-Object Drives -Contains 'D'
        $Result | Should -HaveCount 2
    }

    It 'Where-Object -FilterScript {$_.Prop -in $Array}' {
        $Array = 'SPC-1234','BGP-5678'
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -in $Array}
        $Result | Should -HaveCount 2
    }

    It 'Where-Object $Array -in Prop' {
        $Array = 'SPC-1234','BGP-5678'
        $Result = $Computers | Where-Object ComputerName -In $Array
        $Result | Should -HaveCount 2
    }

    It 'Where-Object -FilterScript {$_.Prop -ge 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -ge 2}
        $Result | Should -HaveCount 2
    }

    It 'Where-Object Prop -ge 2' {
        $Result = $Computers | Where-Object NumberOfCores -GE 2
        $Result | Should -HaveCount 2
    }

    It 'Where-Object -FilterScript {$_.Prop -gt 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -gt 2}
        $Result | Should -HaveCount 1
    }

    It 'Where-Object Prop -gt 2' {
        $Result = $Computers | Where-Object NumberOfCores -GT 2
        $Result | Should -HaveCount 1
    }

    It 'Where-Object -FilterScript {$_.Prop -le 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -le 2}
        $Result | Should -HaveCount 2
    }

    It 'Where-Object Prop -le 2' {
        $Result = $Computers | Where-Object NumberOfCores -LE 2
        $Result | Should -HaveCount 2
    }

    It 'Where-Object -FilterScript {$_.Prop -lt 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -lt 2}
        $Result | Should -HaveCount 1
    }

    It 'Where-Object Prop -lt 2' {
        $Result = $Computers | Where-Object NumberOfCores -LT 2
        $Result | Should -HaveCount 1
    }

    It 'Where-Object -FilterScript {$_.Prop -Like Value}' {
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -like 'MGC-9101'}
        $Result | Should -HaveCount 1
    }

    It 'Where-Object Prop -like Value' {
        $Result = $Computers | Where-Object ComputerName -Like 'MGC-9101'
        $Result | Should -HaveCount 1
    }

    It 'Where-Object -FilterScript {$_.Prop -Match Pattern}' {
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -match '^MGC.+'}
        $Result | Should -HaveCount 1
    }

    It 'Where-Object Prop -like Value' {
        $Result = $Computers | Where-Object ComputerName -Match '^MGC.+'
        $Result | Should -HaveCount 1
    }

    It 'Where-Object should handle dynamic (DLR) objects' {
        $dynObj = [TestDynamic]::new()
        $Result = $dynObj, $dynObj | Where-Object FooProp -EQ 123
        $Result | Should -HaveCount 2
        $Result[0] | Should -Be $dynObj
        $Result[1] | Should -Be $dynObj
    }

    It 'Where-Object should handle dynamic (DLR) objects, even without property name hint' {
        $dynObj = [TestDynamic]::new()
        $Result = $dynObj, $dynObj | Where-Object HiddenProp -EQ 789
        $Result | Should -HaveCount 2
        $Result[0] | Should -Be $dynObj
        $Result[1] | Should -Be $dynObj
    }

    Context "Switch parameter explicit `$false handling" {
        BeforeAll {
            $testData = @(
                [PSCustomObject]@{ Name = 'Alice'; Age = 25; City = 'New York' }
                [PSCustomObject]@{ Name = 'Bob'; Age = 30; City = 'Boston' }
                [PSCustomObject]@{ Name = 'Charlie'; Age = 35; City = 'Chicago' }
            )
        }

        It "-EQ:`$false should behave like default boolean evaluation" {
            # With -EQ:$false, the parameter is not specified, so default boolean evaluation applies
            # "... | Where-Object Name" is equivalent to "... | Where-Object {$true -eq $_.Name}"
            # Non-empty strings are truthy
            $result = $testData | Where-Object Name -EQ:$false
            $result | Should -HaveCount 3
        }

        It "-GT:`$false should behave like default boolean evaluation" {
            # With -GT:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Age -GT:$false
            $result | Should -HaveCount 3
        }

        It "-LT:`$false should behave like default boolean evaluation" {
            # With -LT:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Age -LT:$false
            $result | Should -HaveCount 3
        }

        It "-NE:`$false should behave like default boolean evaluation" {
            # With -NE:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -NE:$false
            $result | Should -HaveCount 3
        }

        It "-GE:`$false should behave like default boolean evaluation" {
            # With -GE:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Age -GE:$false
            $result | Should -HaveCount 3
        }

        It "-LE:`$false should behave like default boolean evaluation" {
            # With -LE:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Age -LE:$false
            $result | Should -HaveCount 3
        }

        It "-NotLike:`$false should behave like default boolean evaluation" {
            # With -NotLike:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object City -NotLike:$false
            $result | Should -HaveCount 3
        }

        It "-NotMatch:`$false should behave like default boolean evaluation" {
            # With -NotMatch:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -NotMatch:$false
            $result | Should -HaveCount 3
        }

        It "-NotContains:`$false should behave like default boolean evaluation" {
            # With -NotContains:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -NotContains:$false
            $result | Should -HaveCount 3
        }

        It "-NotIn:`$false should behave like default boolean evaluation" {
            # With -NotIn:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -NotIn:$false
            $result | Should -HaveCount 3
        }

        It "-IsNot:`$false should behave like default boolean evaluation" {
            # With -IsNot:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -IsNot:$false
            $result | Should -HaveCount 3
        }

        It "-Like:`$false should behave like default boolean evaluation" {
            # With -Like:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object City -Like:$false
            $result | Should -HaveCount 3
        }

        It "-Match:`$false should behave like default boolean evaluation" {
            # With -Match:$false, the parameter is not specified, so default boolean evaluation applies
            $result = $testData | Where-Object Name -Match:$false
            $result | Should -HaveCount 3
        }

        It "-Contains:`$false should behave like default boolean evaluation" {
            # With -Contains:$false, the parameter is not specified, so default boolean evaluation applies
            # "... | Where-Object Name" checks if Name property is truthy
            $result = $testData | Where-Object Name -Contains:$false
            $result | Should -HaveCount 3
        }

        It "-In:`$false should behave like default boolean evaluation" {
            # With -In:$false, the parameter is not specified, so default boolean evaluation applies
            # "... | Where-Object Name" checks if Name property is truthy
            $result = $testData | Where-Object Name -In:$false
            $result | Should -HaveCount 3
        }

        It "-Is:`$false should behave like default boolean evaluation" {
            # With -Is:$false, the parameter is not specified, so default boolean evaluation applies
            # "... | Where-Object Name" checks if Name property is truthy
            $result = $testData | Where-Object Name -Is:$false
            $result | Should -HaveCount 3
        }

        It "-Not:`$false should behave like default boolean evaluation" {
            $testData2 = @(
                [PSCustomObject]@{ Name = ''; Value = 1 }
                [PSCustomObject]@{ Name = 'Test'; Value = 2 }
            )
            # With -Not:$false, the parameter is not specified, so default boolean evaluation applies
            # "... | Where-Object Name" returns objects where Name is truthy (non-empty)
            $result = $testData2 | Where-Object Name -Not:$false
            $result | Should -HaveCount 1
            $result.Name | Should -Be 'Test'
        }

        It "-GT:`$false with -Value should throw an error requiring an operator" {
            # When a switch parameter is set to $false with -Value specified,
            # it should throw an error because no valid operator is active
            { $testData | Where-Object Age -GT:$false -Value 25 } | Should -Throw -ErrorId 'OperatorNotSpecified,Microsoft.PowerShell.Commands.WhereObjectCommand'
        }

        It "-EQ:`$false with -Value should throw an error requiring an operator" {
            # Similar behavior for -EQ:$false with -Value
            { $testData | Where-Object Name -EQ:$false -Value 'Alice' } | Should -Throw -ErrorId 'OperatorNotSpecified,Microsoft.PowerShell.Commands.WhereObjectCommand'
        }

        It "-Like:`$false with -Value should throw an error requiring an operator" {
            # Similar behavior for -Like:$false with -Value
            { $testData | Where-Object Name -Like:$false -Value 'A*' } | Should -Throw -ErrorId 'OperatorNotSpecified,Microsoft.PowerShell.Commands.WhereObjectCommand'
        }
    }
}
