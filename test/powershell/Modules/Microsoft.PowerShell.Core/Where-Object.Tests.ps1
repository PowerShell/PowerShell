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

        $NullTestData = @(
            [PSCustomObject]@{
                Prop = $null
            }
            [PSCustomObject]@{
                Prop = [NullString]::Value
            }
            [PSCustomObject]@{
                Prop = [DBNull]::Value
            }
            [PSCustomObject]@{
                Prop = [System.Management.Automation.Internal.AutomationNull]::Value
            }
            [PSCustomObject]@{
                Prop = @()
            }
            [PSCustomObject]@{
                Prop = @($null)
            }
            [PSCustomObject]@{
                Prop = @('Some value')
            }
            [PSCustomObject]@{
                Prop = @(1, $null, 2, $null, 3)
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

    Context 'Where-Object Prop -is $null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -Is $null
        }

        It 'Should find all null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Should have found a $null match' {
            $Result[0].Prop -is $null | Should -BeTrue
            $Result[0].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }

        It 'Should have found a [NullString]::Value match' {
            $Result[1].Prop | Should -BeOfType [NullString]
        }

        It 'Should have found a [DBNull]::Value match' {
            $Result[2].Prop | Should -BeOfType [DBNull]
        }

        It 'Should have found a [S.M.A.Internal.AutomationNull]::Value match' {
            $Result[3].Prop -is $null | Should -BeTrue
            $Result[3].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }
    }

    Context 'Where-Object Prop -is -Value $null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -Is -Value $null
        }

        It 'Should find all null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Should have found a $null match' {
            $Result[0].Prop -is $null | Should -BeTrue
            $Result[0].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }

        It 'Should have found a [NullString]::Value match' {
            $Result[1].Prop | Should -BeOfType [NullString]
        }

        It 'Should have found a [DBNull]::Value match' {
            $Result[2].Prop | Should -BeOfType [DBNull]
        }

        It 'Should have found a [S.M.A.Internal.AutomationNull]::Value match' {
            $Result[3].Prop -is $null | Should -BeTrue
            $Result[3].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }
    }

    Context 'Where-Object Prop -is -Value:$null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -Is -Value:$null
        }

        It 'Should find all null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Should have found a $null match' {
            $Result[0].Prop -is $null | Should -BeTrue
            $Result[0].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }

        It 'Should have found a [NullString]::Value match' {
            $Result[1].Prop | Should -BeOfType [NullString]
        }

        It 'Should have found a [DBNull]::Value match' {
            $Result[2].Prop | Should -BeOfType [DBNull]
        }

        It 'Should have found a [S.M.A.Internal.AutomationNull]::Value match' {
            $Result[3].Prop -is $null | Should -BeTrue
            $Result[3].Prop | Get-Member -ErrorAction Ignore | Should -BeNullOrEmpty
        }
    }

    Context 'Where-Object NullProp -is $($null)' {
        BeforeAll {
            $testObject = [pscustomobject]@{
                NonNullProp = 'Hello'
                NullProp = $null
            }
        }

        It 'Should cause an error when the right-hand side is a null value, but not a null literal' {
            $testObject | Where-Object NullProp -Is $($null) -ErrorAction SilentlyContinue -ErrorVariable myError
            $myError.FullyQualifiedErrorId | Should -Be 'OperatorFailed,Microsoft.PowerShell.Commands.WhereObjectCommand'
        }
    }

    Context 'Where-Object Prop -isnot $null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -IsNot $null
        }

        It 'Should find all non-null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Each non-null match should be of type array' {
            foreach ($item in $Result) {
                ,$item.Prop | Should -BeOfType [array]
            }
        }
    }

    Context 'Where-Object Prop -isnot -Value $null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -IsNot -Value $null
        }

        It 'Should find all non-null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Each non-null match should be of type array' {
            foreach ($item in $Result) {
                ,$item.Prop | Should -BeOfType [array]
            }
        }
    }

    Context 'Where-Object Prop -isnot -Value:$null' {
        BeforeAll {
            $Result = $NullTestData | Where-Object Prop -IsNot -Value:$null
        }

        It 'Should find all non-null matches' {
            $Result | Should -HaveCount 4
        }

        It 'Each non-null match should be of type array' {
            foreach ($item in $Result) {
                ,$item.Prop | Should -BeOfType [array]
            }
        }
    }

    Context 'Where-Object NonNullProp -isnot $($null)' {
        BeforeAll {
            $testObject = [pscustomobject]@{
                NonNullProp = 'Hello'
                NullProp    = $null
            }
        }

        It 'Should cause an error when the right-hand side is a null value, but not a null literal' {
            $testObject | Where-Object NonNullProp -IsNot $($null) -ErrorAction SilentlyContinue -ErrorVariable myError
            $myError.FullyQualifiedErrorId | Should -Be 'OperatorFailed,Microsoft.PowerShell.Commands.WhereObjectCommand'
        }
    }

}
