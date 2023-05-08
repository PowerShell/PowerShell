# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Validate basic Variable provider cmdlets" -Tags "CI" {
    BeforeAll {
        $testVarName = "MyTestVarThatWontConflict"
        $testVarValue = 1234
        $testVarDescription = "This is a test variable for provider test purposes."
    }

    BeforeEach {
        New-Variable -Name $testVarName -Value $testVarValue -Description $testVarDescription
    }

    AfterEach {
        Remove-Variable -Name $testVarName -ErrorAction SilentlyContinue
    }

    It "Verify Get-Item" {
        $result = Get-Item "Variable:${testVarName}"
        $result.Name | Should -Be $testVarName
        $result.Value | Should -Be $testVarValue
    }

    It "Verify New-Item" {
        $result = New-Item -Name "MyTestVariable" -Value 5 -Path "Variable:"
        $result.Name | Should -Be "MyTestVariable"
        $result.Value | Should -Be 5
    }

    It "Verify Clear-Item" {
        $valueBefore = (Get-Item "Variable:${testVarName}").Value
        Clear-Item "Variable:${testVarName}"
        $valueAfter = (Get-Item "Variable:${testVarName}").Value
        $valueBefore | Should -Be $testVarValue
        $valueAfter | Should -BeNullOrEmpty
    }

    It "Verify Copy-Item" {
        Copy-Item -Path "Variable:${testVarName}" -Destination "Variable:${testVarName}_Copy"
        $original = Get-Item "Variable:${testVarName}"
        $copy = Get-Item "Variable:${testVarName}_Copy"
        $original.Name | Should -Be $testVarName
        $copy.Name | Should -Be "${testVarName}_Copy"
        $original.Value | Should -Be $copy.Value
    }

    It "Verify Remove-Item" {
        $existsBefore = Test-Path "Variable:${testVarName}"
        Remove-Item -Path "Variable:${testVarName}"
        $existsAfter = Test-Path "Variable:${testVarName}"
        $existsBefore | Should -BeTrue
        $existsAfter | Should -BeFalse
    }

    It "Verify Rename-Item" {
        $existsBefore = Test-Path "Variable:${testVarName}"
        Rename-Item -Path "Variable:${testVarName}" -NewName "${testVarName}_Rename"
        $existsAfter = Test-Path "Variable:${testVarName}"
        $result = Get-Item "Variable:${testVarName}_Rename"
        $existsBefore | Should -BeTrue
        $existsAfter | Should -BeFalse
        $result.Name | Should -Be "${testVarName}_Rename"
        $result.Value | Should -Be $testVarValue
    }
}

Describe "Validate basic negative test cases for Variable provider cmdlets" -Tags "Feature" {
    BeforeAll {
        $testVarName = "MyTestVarThatWontConflict"
        $testVarValue = 1234
        $testVarDescription = "This is a test variable for provider test purposes."
    }

    BeforeEach {
        New-Variable -Name $testVarName -Value $testVarValue -Description $testVarDescription
    }

    AfterEach {
        Remove-Variable -Name $testVarName -ErrorAction SilentlyContinue
    }

    It "Verify Negative New-Item" {
        { New-Item -Name $testVarName -Value 5 -Path "Variable:" -ErrorAction Stop } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.NewItemCommand"
    }

    It "Verify Negative Move-Item" {
        $alreadyExistsVar = 2
        { Move-Item -Path "Variable:${testVarName}" -Destination "Variable:alreadyExistsVar" -ErrorAction Stop } | Should -Throw -ErrorId "GetDynamicParametersException,Microsoft.PowerShell.Commands.MoveItemCommand"
    }

    It "Verify Negative Invoke-Item" {
        { Invoke-Item -Path "Variable:${testVarName}" -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.InvokeItemCommand"
    }

    It "Verify Negative Get-ItemPropertyValue" {
        { Get-ItemPropertyValue -Path "Variable:" -Name $testVarName -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.GetItemPropertyValueCommand"
    }
}
