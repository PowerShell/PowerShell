# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Functional tests to verify basic conditions for IO to the powershell.config.json files
# The properties files are supported on non-Windows OSes, but the tests are specific to
# Windows so that file IO can be verified using supported cmdlets.
#

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
            # Config now defaults to LocalAppData instead of Documents
            $userSettingsDir = [System.IO.Path]::Combine($env:LOCALAPPDATA, $productName)
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
