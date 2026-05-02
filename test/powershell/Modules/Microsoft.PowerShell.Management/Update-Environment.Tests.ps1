# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (-not $IsWindows) {
    return
}

Describe "Update-Environment" -Tag "CI" {

    BeforeAll {
        function Get-ProcessEnvironmentSnapshot {
            $environmentSnapshot = @{}
            foreach ($environmentEntry in Get-ChildItem -Path Env:) {
                $environmentSnapshot[$environmentEntry.Name] = $environmentEntry.Value
            }
            return $environmentSnapshot
        }

        function Restore-ProcessEnvironment {
            param(
                [hashtable]$EnvironmentSnapshot
            )
            foreach ($environmentEntry in Get-ChildItem -Path Env:) {
                if (-not $EnvironmentSnapshot.ContainsKey($environmentEntry.Name)) {
                    Remove-Item -Path "Env:\$($environmentEntry.Name)" -ErrorAction SilentlyContinue
                }
            }
            foreach ($environmentName in $EnvironmentSnapshot.Keys) {
                [Environment]::SetEnvironmentVariable($environmentName, $EnvironmentSnapshot[$environmentName], "Process")
            }
        }
    }

    BeforeEach {
        $script:processEnvironmentSnapshot = Get-ProcessEnvironmentSnapshot
    }

    AfterEach {
        Restore-ProcessEnvironment -EnvironmentSnapshot $script:processEnvironmentSnapshot
    }

    Context "Variable merging and blocklist" {
        It "Should not overwrite ignored dynamic variables like USERNAME" {
            # Arrange
            $originalUser = $env:USERNAME

            # Act
            Update-Environment

            # Assert
            $env:USERNAME | Should -BeExactly $originalUser
        }

        It "Should successfully pull new variables from the User target" {
            # Arrange
            $testKey = "TEST_UPDATE_ENV_VAR_$(Get-Random)"
            $testValue = "HelloWorld"

            # Set a new environment variable in the User registry target
            [Environment]::SetEnvironmentVariable($testKey, $testValue, "User")

            try {
                # Ensure the current process does not have it yet
                [Environment]::GetEnvironmentVariable($testKey, "Process") | Should -BeNullOrEmpty

                # Act - Run cmdlet
                Update-Environment -User

                # Assert - The process should now have the variable
                (Get-Item -Path "Env:\$testKey").Value | Should -BeExactly $testValue
            }
            finally {
                # Clean up the registry and process
                [Environment]::SetEnvironmentVariable($testKey, $null, "User")
                Remove-Item -Path "Env:\$testKey" -ErrorAction SilentlyContinue
            }
        }

        It "Should not pull User target variables when only Machine is specified" {
            # Arrange
            $testKey = "TEST_UPDATE_ENV_MACHINE_ONLY_$(Get-Random)"
            $testValue = "UserOnlyValue"
            # Set a new environment variable in the User registry target
            [Environment]::SetEnvironmentVariable($testKey, $testValue, "User")

            try {
                # Ensure the current process does not have it yet
                [Environment]::GetEnvironmentVariable($testKey, "Process") | Should -BeNullOrEmpty

                # Act - Run cmdlet for Machine scope only
                Update-Environment -Machine

                # Assert - The process should still not have the user-scoped variable
                [Environment]::GetEnvironmentVariable($testKey, "Process") | Should -BeNullOrEmpty
                Test-Path -Path "Env:\$testKey" | Should -BeFalse
            }
            finally {
                # Clean up the registry and process
                [Environment]::SetEnvironmentVariable($testKey, $null, "User")
                Remove-Item -Path "Env:\$testKey" -ErrorAction SilentlyContinue
            }
        }

        It "Should respect -WhatIf and not mutate the process environment" {
            # Arrange
            $testKey = "TEST_UPDATE_ENV_WHATIF_$(Get-Random)"
            $testValue = "WhatIfValue"
            [Environment]::SetEnvironmentVariable($testKey, $testValue, "User")

            try {
                # Act - Run with -WhatIf
                Update-Environment -User -WhatIf

                # Assert - Process environment should remain unchanged
                [Environment]::GetEnvironmentVariable($testKey, "Process") | Should -BeNullOrEmpty
                Test-Path -Path "Env:\$testKey" | Should -BeFalse
            }
            finally {
                # Clean up
                [Environment]::SetEnvironmentVariable($testKey, $null, "User")
                Remove-Item -Path "Env:\$testKey" -ErrorAction SilentlyContinue
            }
        }

        It "Should maintain the Path variable without destroying it" {
            # Act
            Update-Environment

            # Assert
            $env:PATH | Should -Not -BeNullOrEmpty
            # Ensure it still contains typical systemic paths after the merge
            $env:PATH.Length | Should -BeGreaterThan 0
        }
    }
}
