# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Set-Item" -Tag "CI" {
    BeforeDiscovery {
        $testCases = @{ Path = "variable:SetItemTestCase"; Value = "TestData" },
            @{ Path = "alias:SetItemTestCase"; Value = "Get-Alias" },
            @{ Path = "function:SetItemTestCase"; Value = "{ 1 }" },
            @{ Path = "env:SetItemTestCase"; Value = "{ 1 }" }
    }

    BeforeAll {
        $validateAndReset = @{
            "variable:SetItemTestCase" = @{ Validate = { $SetItemTestCase | Should -Be "TestData" }; Reset = { Remove-Item variable:SetItemTestCase } }
            "alias:SetItemTestCase"    = @{ Validate = { (Get-Alias SetItemTestCase).Definition | Should -Be "Get-Alias" }; Reset = { Remove-Item alias:SetItemTestCase } }
            "function:SetItemTestCase" = @{ Validate = { SetItemTestCase | Should -Be 1 }; Reset = { Remove-Item function:SetItemTestCase } }
            "env:SetItemTestCase"      = @{ Validate = { $env:SetItemTestCase | Should -Be 1 }; Reset = { Remove-Item env:SetItemTestCase } }
        }
    }

    It "Set-Item should be able to handle <Path>" -TestCase $testCases {
        param ( $Path, $Value )
        $actualValue = if ($Value -match '^\{.*\}$') { [scriptblock]::Create($Value.Trim('{ }')) } else { $Value }
        Set-Item -Path $Path -Value $actualValue
        try {
            & $validateAndReset[$Path].Validate
        }
        finally {
            & $validateAndReset[$Path].Reset
        }
    }
}
