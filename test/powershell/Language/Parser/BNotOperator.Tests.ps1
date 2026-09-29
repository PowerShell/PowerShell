# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {
    $baseTypes = @{
        [SByte] = 'sbyte';     [Byte] = 'byte'
        [Int16] = 'short';     [UInt16] = 'ushort'
        [Int32] = 'int';       [UInt32] = 'uint'
        [Int64] = 'long';      [UInt64] = 'ulong'
    }

    $ns = [Guid]::NewGuid() -replace '-',''

    $typeDefinition = "namespace ns_$ns`n{"

    foreach ($bt in $baseTypes.Keys)
    {
        $baseTypeName = $baseTypes[$bt]
        $typeDefinition += @"
    public enum E_$baseTypeName : $baseTypeName
    {
        Min = $($bt::MinValue),
        MinPlus1 = $($bt::MinValue + 1),
        MaxMinus1 = $($bt::MaxValue - 1),
        Max = $($bt::MaxValue)
    }
"@
    }

    $typeDefinition += "`n}"

    $enumTypeNames = Add-Type $typeDefinition -PassThru

    $enumTypesForEach = [type[]]$enumTypeNames | ForEach-Object { @{ enumType = $_ } }
    $baseTypesForEach = $baseTypes.Keys | ForEach-Object { @{ baseType = $_ } }
}

Describe "bnot on enums" -Tags "CI" {
    Context "<enumType>" -ForEach $enumTypesForEach {
        It "max - 1" {
            $res = -bnot $enumType::MaxMinus1
            $res | Should -Be $enumType::MinPlus1
            $res | Should -BeOfType $enumType
        }

        It "min + 1" {
            $res = -bnot $enumType::MinPlus1
            $res | Should -Be $enumType::MaxMinus1
            $res | Should -BeOfType $enumType
        }

        It "Max" {
            $res = -bnot $enumType::Max
            $res | Should -Be $enumType::Min
            $res | Should -BeOfType $enumType
        }

        It "Min" {
            $res = -bnot $enumType::Min
            $res | Should -Be $enumType::Max
            $res | Should -BeOfType $enumType
        }
    }
}

Describe "bnot on integral types" -Tags "CI" {
    Context "<baseType>" -ForEach $baseTypesForEach {

        BeforeAll {
            $max = $baseType::MaxValue
            $maxMinus1 = $max - 1
            $min = $baseType::MinValue
            $minPlus1 = $min + 1

            if ([System.Runtime.InteropServices.Marshal]::SizeOf([type]$baseType) -lt 4)
            {
                $expectedResultType = [int]
            }
            else
            {
                $expectedResultType = $baseType
            }

            $isUnsignedSmall = ($baseType -eq [byte] -or $baseType -eq [uint16])
        }

        It "max - 1" {
            $res = -bnot $maxMinus1
            if ($isUnsignedSmall) {
                $res | Should -Be (-bnot [int]$maxMinus1)
            } else {
                $res | Should -Be $minPlus1
            }
            $res | Should -BeOfType $expectedResultType
        }

        It "min + 1" {
            $res = -bnot $minPlus1
            if ($isUnsignedSmall) {
                $res | Should -Be (-bnot [int]$minPlus1)
            } else {
                $res | Should -Be $maxMinus1
            }
            $res | Should -BeOfType $expectedResultType
        }

        It "max" {
            $res = -bnot $max
            if ($isUnsignedSmall) {
                $res | Should -Be (-bnot [int]$max)
            } else {
                $res | Should -Be $min
            }
            $res | Should -BeOfType $expectedResultType
        }

        It "min" {
            $res = -bnot $min
            if ($isUnsignedSmall) {
                $res | Should -Be (-bnot [int]$min)
            } else {
                $res | Should -Be $max
            }
            $res | Should -BeOfType $expectedResultType
        }
    }
}
