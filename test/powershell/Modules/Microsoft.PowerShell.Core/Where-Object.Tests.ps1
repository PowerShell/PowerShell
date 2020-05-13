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
}
