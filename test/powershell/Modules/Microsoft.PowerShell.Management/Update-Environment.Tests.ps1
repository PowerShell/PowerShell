# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Update-Environment" -Tag "CI" {
    Context "Variable merging and blocklist" {

        It "Should not overwrite ignored dynamic variables like USERNAME" {
            # Arrange
            $originalUsername = $env:USERNAME

            # Act
            Update-Environment

            # Assert
            $env:USERNAME | Should -BeExactly $originalUsername
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
