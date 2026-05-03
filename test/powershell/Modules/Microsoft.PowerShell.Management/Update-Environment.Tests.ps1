# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Update-Environment" -Tag "CI" -Skip:(-not $IsWindows) {

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

        It "Should merge Path entries from User scope while preserving process-only segments" {
            # Arrange
            $originalUserPath = [Environment]::GetEnvironmentVariable("Path", "User")
            $userPathSegment = "C:\UpdateEnvironmentUserPath_$([guid]::NewGuid().Guid)"
            $processOnlyPathSegment = "C:\UpdateEnvironmentProcessOnlyPath_$([guid]::NewGuid().Guid)"
            $pathSeparator = [IO.Path]::PathSeparator

            # Inject artificial process segment
            $env:PATH = "$processOnlyPathSegment$pathSeparator$env:PATH"

            # Inject User target segment
            if ([string]::IsNullOrEmpty($originalUserPath)) {
                [Environment]::SetEnvironmentVariable("Path", $userPathSegment, "User")
            }
            else {
                [Environment]::SetEnvironmentVariable("Path", "$originalUserPath$pathSeparator$userPathSegment", "User")
            }

            try {
                # Sanity checks before the merge
                $processPathBeforeUpdate = [Environment]::GetEnvironmentVariable("Path", "Process") -split [string]$pathSeparator
                $processPathBeforeUpdate | Should -Contain $processOnlyPathSegment
                $processPathBeforeUpdate | Should -Not -Contain $userPathSegment

                # Act
                Update-Environment -User

                # Assert
                $processPathAfterUpdate = [Environment]::GetEnvironmentVariable("Path", "Process") -split [string]$pathSeparator
                # The user segment should have been pulled in
                $processPathAfterUpdate | Should -Contain $userPathSegment
                # The process segment should NOT have been erased
                $processPathAfterUpdate | Should -Contain $processOnlyPathSegment
            }
            finally {
                [Environment]::SetEnvironmentVariable("Path", $originalUserPath, "User")
            }
        }
    }
}
