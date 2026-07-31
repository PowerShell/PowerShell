# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Functional tests to verify basic conditions for IO to the powershell.config.json files
# The properties files are supported on non-Windows OSes, but the tests are specific to
# Windows so that file IO can be verified using supported cmdlets.
#

Import-Module HelpersCommon

Describe "User-Specific powershell.config.json Modifications" -Tags "CI" {

    BeforeAll {
        # Skip these tests when run against "InBox" PowerShell
        $IsInbox = $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase)
        $productName = "PowerShell"

        #skip all tests on non-windows platform
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $IsNotSkipped = ($IsWindows -and !$IsInbox) # Only execute for PowerShell on Windows
        $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped

        if ($IsNotSkipped) {
            # Discover the user-specific powershell.config.json file
            $userSettingsDir = [System.IO.Path]::Combine($env:USERPROFILE, "Documents", $productName)
            $userPropertiesFile = Join-Path $userSettingsDir "powershell.config.json"

            # Save the file for restoration after the tests are complete
            $backupPropertiesFile = ""
            if (Test-Path $userPropertiesFile) {
                $backupPropertiesFile = Join-Path $userSettingsDir "ORIGINAL_powershell.config.json"
                Copy-Item -Path $userPropertiesFile -Destination $backupPropertiesFile -Force -ErrorAction Continue
            }
            elseif (-not (Test-Path $userSettingsDir)) {
                # create the directory if it does not already exist
                $null = New-Item -Type Directory -Path $userSettingsDir -Force -ErrorAction SilentlyContinue
            }

            # Save the original Process ExecutionPolicy. The tests assume that it is Undefined
            $processExecutionPolicy = Get-ExecutionPolicy -Scope Process
            Set-ExecutionPolicy -Scope Process -ExecutionPolicy Undefined
        }
    }

    BeforeEach {
        if ($IsNotSkipped) {
            Set-Content -Path $userPropertiesFile -Value '{"Microsoft.PowerShell:ExecutionPolicy":"RemoteSigned"}'
        }
    }

    AfterAll {
        if ($IsNotSkipped) {
            if (-not $backupPropertiesFile)
            {
                # Remove powershell.config.json if it did not exist before the tests
                Remove-Item -Path $userPropertiesFile -Force -ErrorAction SilentlyContinue
            }
            else
            {
                # Restore the original powershell.config.json file if it existed before the test pass.
                Move-Item -Path $backupPropertiesFile -Destination $userPropertiesFile -Force -ErrorAction Continue
            }

            # Restore the original Process ExecutionPolicy
            Set-ExecutionPolicy -Scope Process -ExecutionPolicy $processExecutionPolicy
        }

        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Verify Queries to Missing File Return Default Value" {
        Remove-Item $userPropertiesFile -Force

        Get-ExecutionPolicy -Scope CurrentUser | Should -Be "Undefined"

        # Verify the file was not created during the test
        { $propFile = Get-Item $userPropertiesFile -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
    }

    It "Verify Queries for Non-Existant Properties Return Default Value" {
        # Create a valid file with no values
        Set-Content -Path $userPropertiesFile -Value "{}"

        Get-ExecutionPolicy -Scope CurrentUser | Should -Be "Undefined"
    }

    It "Verify Writes Update Properties" {
        Get-Content -Path $userPropertiesFile | Should -Be '{"Microsoft.PowerShell:ExecutionPolicy":"RemoteSigned"}'
        Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass
        Get-Content -Path $userPropertiesFile | Should -Be '{"Microsoft.PowerShell:ExecutionPolicy":"Bypass"}'
    }

    It "Verify Writes Create the File if Not Present" {
        Remove-Item $userPropertiesFile -Force
        Test-Path $userPropertiesFile | Should -BeFalse
        Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass
        Get-Content -Path $userPropertiesFile | Should -Be '{"Microsoft.PowerShell:ExecutionPolicy":"Bypass"}'
    }
}

Describe "MSIX system-wide powershell.config.json Modifications" -Tags @("Feature", "RequireAdminOnWindows") {
    It "Writes LocalMachine execution policy to the package-family ProgramData file" {
        $packageFamilyName = [System.Management.Automation.Internal.InternalTestHooks]::GetCurrentPackageFamilyName()
        if ([string]::IsNullOrEmpty($packageFamilyName)) {
            Set-ItResult -Skipped -Because "The test requires PowerShell to be running with MSIX package identity."
            return
        }

        if (-not (Test-IsElevated)) {
            Set-ItResult -Skipped -Because "Writing machine-wide MSIX configuration requires elevation."
            return
        }

        $programDataDirectory = [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData)
        $powerShellConfigDirectory = Join-Path $programDataDirectory "Microsoft\PowerShell"
        $machineConfigDirectory = Join-Path $powerShellConfigDirectory $packageFamilyName
        $machineConfigFile = Join-Path $machineConfigDirectory "powershell.config.json"
        $backupConfigFile = Join-Path $TestDrive "powershell.config.json"
        $powerShellConfigDirectoryExisted = Test-Path -LiteralPath $powerShellConfigDirectory
        $machineConfigDirectoryExisted = Test-Path -LiteralPath $machineConfigDirectory
        $configFileExisted = Test-Path -LiteralPath $machineConfigFile
        $originalPolicyExists = $false
        $originalPolicy = $null

        if ($configFileExisted) {
            Copy-Item -LiteralPath $machineConfigFile -Destination $backupConfigFile -ErrorAction Stop
            $originalConfig = Get-Content -LiteralPath $machineConfigFile -Raw | ConvertFrom-Json
            if ($null -ne $originalConfig) {
                $originalPolicyProperty = $originalConfig.PSObject.Properties["Microsoft.PowerShell:ExecutionPolicy"]
                if ($null -ne $originalPolicyProperty) {
                    $originalPolicyExists = $true
                    $originalPolicy = $originalPolicyProperty.Value
                }
            }
        }

        $testPolicy = if ($originalPolicy -eq "AllSigned") { "RemoteSigned" } else { "AllSigned" }
        $setLocalMachinePolicy = {
            param([string] $Policy)

            try {
                Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy $Policy -Force -ErrorAction Stop
            }
            catch {
                if ($_.FullyQualifiedErrorId -ne "ExecutionPolicyOverride,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand") {
                    throw
                }
            }
        }

        try {
            & $setLocalMachinePolicy -Policy $testPolicy

            $machineConfigFile | Should -Exist
            $config = Get-Content -LiteralPath $machineConfigFile -Raw | ConvertFrom-Json
            $config."Microsoft.PowerShell:ExecutionPolicy" | Should -Be $testPolicy

            (Get-Acl -LiteralPath $powerShellConfigDirectory).AreAccessRulesProtected | Should -BeTrue
            (Get-Acl -LiteralPath $machineConfigDirectory).AreAccessRulesProtected | Should -BeTrue

            $machineConfigFileAcl = Get-Acl -LiteralPath $machineConfigFile
            $machineConfigFileAcl.AreAccessRulesProtected | Should -BeFalse
            @($machineConfigFileAcl.Access | Where-Object { $_.IsInherited }).Count | Should -BeGreaterThan 0
        }
        finally {
            try {
                try {
                    if ($configFileExisted -or (Test-Path -LiteralPath $machineConfigFile)) {
                        $restorePolicy = if ($originalPolicyExists) { $originalPolicy } else { "Undefined" }
                        & $setLocalMachinePolicy -Policy $restorePolicy
                    }
                }
                finally {
                    if ($configFileExisted) {
                        Copy-Item -LiteralPath $backupConfigFile -Destination $machineConfigFile -Force -ErrorAction Stop
                    }
                    else {
                        Remove-Item -LiteralPath $machineConfigFile -Force -ErrorAction SilentlyContinue
                    }
                }
            }
            finally {
                if (-not $machineConfigDirectoryExisted) {
                    Remove-Item -LiteralPath $machineConfigDirectory -Force -ErrorAction SilentlyContinue
                }

                if (-not $powerShellConfigDirectoryExisted) {
                    Remove-Item -LiteralPath $powerShellConfigDirectory -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
}
