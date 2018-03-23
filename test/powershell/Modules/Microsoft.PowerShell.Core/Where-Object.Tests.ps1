# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

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
        $Result.Count | Should -Be 2
    }

    It 'Where-Object -FilterScript {$true -ne $_.Prop}' {
        $Result = $Computers | Where-Object -FilterScript {$true -ne $_.IPAddress}
        $Result.Count | Should -Be 2
    }

    It "Where-Object Prop" {
        $Result = $Computers | Where-Object 'IPAddress'
        $Result.Count | Should -Be 1
    }

    It 'Where-Object -FilterScript {$true -eq $_.Prop}' {
        $Result = $Computers | Where-Object -FilterScript {$true -eq $_.IPAddress}
        $Result.Count | Should -Be 1
    }

    It 'Where-Object -FilterScript {$_.Prop -contains Value}' {
        $Result = $Computers | Where-Object -FilterScript {$_.Drives -contains 'D'}
        $Result.Count | Should -Be 2
    }

    It 'Where-Object Prop -contains Value' {
        $Result = $Computers | Where-Object Drives -contains 'D'
        $Result.Count | Should -Be 2
    }

    It 'Where-Object -FilterScript {$_.Prop -in $Array}' {
        $Array = 'SPC-1234','BGP-5678'
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -in $Array}
        $Result.Count | Should -Be 2
    }

    It 'Where-Object $Array -in Prop' {
        $Array = 'SPC-1234','BGP-5678'
        $Result = $Computers | Where-Object ComputerName -in $Array
        $Result.Count | Should -Be 2
    }

    It 'Where-Object -FilterScript {$_.Prop -ge 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -ge 2}
        $Result.Count | Should -Be 2
    }

    It 'Where-Object Prop -ge 2' {
        $Result = $Computers | Where-Object NumberOfCores -ge 2
        $Result.Count | Should -Be 2
    }

    It 'Where-Object -FilterScript {$_.Prop -gt 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -gt 2}
        $Result.Count | Should -Be 1
    }

    It 'Where-Object Prop -gt 2' {
        $Result = $Computers | Where-Object NumberOfCores -gt 2
        $Result.Count | Should -Be 1
    }

    It 'Where-Object -FilterScript {$_.Prop -le 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -le 2}
        $Result.Count | Should -Be 2
    }

    It 'Where-Object Prop -le 2' {
        $Result = $Computers | Where-Object NumberOfCores -le 2
        $Result.Count | Should -Be 2
    }

    It 'Where-Object -FilterScript {$_.Prop -lt 2}' {
        $Result = $Computers | Where-Object -FilterScript {$_.NumberOfCores -lt 2}
        $Result.Count | Should -Be 1
    }

    It 'Where-Object Prop -lt 2' {
        $Result = $Computers | Where-Object NumberOfCores -lt 2
        $Result.Count | Should -Be 1
    }

    It 'Where-Object -FilterScript {$_.Prop -Like Value}' {
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -like 'MGC-9101'}
        $Result.Count | Should -Be 1
    }

    It 'Where-Object Prop -like Value' {
        $Result = $Computers | Where-Object ComputerName -like 'MGC-9101'
        $Result.Count | Should -Be 1
    }

    It 'Where-Object -FilterScript {$_.Prop -Match Pattern}' {
        $Result = $Computers | Where-Object -FilterScript {$_.ComputerName -match '^MGC.+'}
        $Result.Count | Should -Be 1
    }

    It 'Where-Object Prop -like Value' {
        $Result = $Computers | Where-Object ComputerName -match '^MGC.+'
        $Result.Count | Should -Be 1
    }
}
