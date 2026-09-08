# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Arithmetic overflow promotion" -Tags "CI","RequireAdminOnWindows" {
    BeforeAll {
        $IsSkipped = (-not $IsCoreCLR) -or ($PSVersionTable.PSVersion -lt [version]'7.4.0')
    }

    It "Promotes Int64 overflow to Double" -Skip:$IsSkipped {
        $result = [Int64]::MaxValue + [Int64]::MaxValue
        $result.GetType().Name | Should -Be 'Double'
    }

    It "Promotes Int128 overflow to Double" -Skip:$IsSkipped {
        $result = [Int128]::MaxValue + [Int128]::MaxValue
        $result.GetType().Name | Should -Be 'Double'
    }

    It "Keeps non-overflow Int128 addition in Int128" -Skip:$IsSkipped {
        $result = [Int128]1 + [Int128]2
        $result.GetType().Name | Should -Be 'Int128'
        $result | Should -Be ([Int128]3)
    }

    It "Promotes UInt128 overflow to Double" -Skip:$IsSkipped {
        $result = [UInt128]::MaxValue + [UInt128]::MaxValue
        $result.GetType().Name | Should -Be 'Double'
    }
}
