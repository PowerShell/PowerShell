# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersSecurity

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

Describe "Validate scope in Variable provider cmdlets" -Tag "Feature" {
    BeforeAll {
        Set-Variable -Name Module -Value (New-Module -Name "Test-Variables" {
            $Script:GetVariable = $true
            $Script:SetVariable = "False"
            $Script:ClearVariable = "False"
            $Script:RemoveVariable = "False"
        })
        $Module.Name | Should be "Test-Variables"
    }
    It "Validate variable does not exist in current scope" {
        { Get-Variable -Name GetVariable -ErrorAction Stop } | Should -Throw
    }
    It "Verify Get-Variable" {
        Get-Variable -Scope $Module -Name GetVariable | Should be $true
    }
    It "Verify New-Variable" {
        New-Variable -Scope $Module -Name NewVariable -Value $true
        Get-Variable -Scope $Module -Name NewVariable | Should be $true
    }
    It "Verify Set-Variable" {
        Set-Variable -Scope $Module -Name SetVariable -Value $true
        Get-Variable -Scope $Module -Name SetVariable | Should be $true
    }
    It "Verify Clear-Variable" {
        Clear-Variable -Scope $Module -Name ClearVariable
        Get-Variable -Scope $Module -Name ClearVariable | ForEach-Object Value | Should BeNullOrEmpty
    }
    It "Verify Remove-Variable" {
        Remove-Variable -Scope $Module -Name RemoveVariable
        { Get-Variable -Scope $Module -Name RemoveVariable -ErrorAction Stop } | should -Throw
    }
}

Describe "Validate scope types in Variable provider cmdlets" -Tag "Feature" {
    BeforeAll {
        Set-Variable -Name Module -Value (New-Module -Name "Test-Variables" {
            $Script:GetVariable = $true
            $Script:SetVariable = "False"
            $Script:ClearVariable = "False"
            $Script:RemoveVariable = "False"
        })
        $Module.Name | Should be "Test-Variables"
    }
    It "Validate Scope Object Type [ModuleInfo]" {
        Get-Variable -Scope $Module -Name GetVariable | Should be $true
    }
    It "Validate Scope Object Type [SessionState]" {
        Get-Variable -Scope $Module.SessionState -Name GetVariable | Should be $true
    }
    It "Validate Scope Object Type [SessionStateScope]" {
        #Todo: This will get filled in soon Pending Completion of Command Get-CurrentScope
    }
    It "Validate Scope Object Type [PSObject]" {
        $TestPSObject = [PSObject]::AsPSObject($Module)
        Get-Variable -Scope $TestPSObject -Name GetVariable | Should be $true
    }
    It "Validate negative Scope Object Type [<NotSupported>]" {
        $TestNotSupported = [datetime]::Now
        { Get-Variable -Scope $TestNotSupported -Name GetVariable -ErrorAction Stop } | Should -Throw
    }
}

Describe "Validate Security on scope in Variable provider cmdlets" -Tag "Feature","RequireAdminOnWindows" {
    BeforeAll {
        Set-Variable -Name TestApprovedScope -Value $true
        Set-Variable -Name Module -Value (New-Module -Name "Test-Variables" {
            $Script:GetVariable = $true
            $Script:SetVariable = "False"
            $Script:ClearVariable = "False"
            $Script:RemoveVariable = "False"
        })
        $Module.Name | Should be "Test-Variables"
    }
    It "Test Security Approved Scope executed in constrained language mode" {
        try {
            $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
            Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
            Get-Variable -Scope 0 -Name TestApprovedScope | Should be $true
        }
        catch {
            throw $_
        }
        finally {
            Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode -RevertLockdownMode
        }
    }
    It "Test Security Scope executed in constrained language mode" {
        try {
            $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
            Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
            Get-Variable -Scope $Module -Name GetVariable | Should be $true
        }
        catch {
            return $true
        }
        finally {
            Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode -RevertLockdownMode
        }
        throw "Test failed"
    }
}