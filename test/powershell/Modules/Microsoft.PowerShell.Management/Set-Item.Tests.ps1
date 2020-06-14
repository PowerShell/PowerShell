# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Set-Item" -Tag "CI" {
    $testCases = @{ Path = "variable:SetItemTestCase"; Value = "TestData"; Validate = { $SetItemTestCase | Should -Be "TestData" }; Reset = {Remove-Item variable:SetItemTestCase} },
        @{ Path = "alias:SetItemTestCase"; Value = "Get-Alias"; Validate = { (Get-Alias SetItemTestCase).Definition | Should -Be "Get-Alias"}; Reset = { Remove-Item alias:SetItemTestCase } },
        @{ Path = "function:SetItemTestCase"; Value = { 1 }; Validate = { SetItemTestCase | Should -Be 1 }; Reset = { Remove-Item function:SetItemTestCase } },
        @{ Path = "env:SetItemTestCase"; Value = { 1 }; Validate = { $env:SetItemTestCase | Should -Be 1 }; Reset = { Remove-Item env:SetItemTestCase } }

    It "Set-Item should be able to handle <Path>" -TestCase $testCases {
        param ( $Path, $Value, $Validate, $Reset )
        Set-Item -Path $path -Value $value
        try {
            & $Validate
        }
        finally {
            & $reset
        }
    }
}
