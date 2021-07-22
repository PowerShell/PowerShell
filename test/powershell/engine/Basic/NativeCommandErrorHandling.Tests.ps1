# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Functional tests to verify that errors (non-terminating and terminating) appropriately
# when $PSNativeCommandUseErrorActionPreference is $true

Describe 'Native command error handling tests' -Tags 'CI' {
    Context 'PSNativeCommandUseErrorActionPreference is $true' {
        BeforeAll {
            $pwsh = "$PSHOME/pwsh"
        }
        BeforeEach {
            $PSNativeCommandUseErrorActionPreference = $true
            $Error.Clear()
        }

        It 'Non-zero exit code throws teminating error when $ErrorActionPreference = Stop' {
            $ErrorActionPreference = 'Stop'

            { & $pwsh -noprofile -noninteractive -c "exit 1" } | Should -Throw -ErrorId 'ProgramFailedToComplete'

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
        }

        It 'Non-zero exit code writes a non-teminating error when $ErrorActionPreference = Continue' {
            $ErrorActionPreference = 'Continue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
            $stderr.Exception.Message | Should -Be 'Program "pwsh.exe" ended with non-zero exit code 1.'
        }

        It 'Non-zero exit code generates a non-teminating error when $ErrorActionPreference = SilentlyContinue' {
            $ErrorActionPreference = 'SilentlyContinue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
            $stderr | Should -BeNullOrEmpty
        }

        It 'Non-zero exit code does not generates an error record when $ErrorActionPreference = Ignore' {
            $ErrorActionPreference = 'Ignore'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $LASTEXITCODE | Should -Be 1
            $error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }

        It 'Zero exit code generates no error when $ErrorActionPreference = Stop' {
            $ErrorActionPreference = 'Stop'

            { & $pwsh -noprofile -noninteractive -c "exit 0" } | Should -Not -Throw

            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
        }

        It 'Zero exit code generates no error when $ErrorActionPreference = Continue' {
            $ErrorActionPreference = 'Continue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 0" 2>&1

            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }

        It 'Zero exit code generates no error when $ErrorActionPreference = SilentlyContinue' {
            $ErrorActionPreference = 'SilentlyContinue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 0" 2>&1

            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }

        It 'Zero exit code generates no error when $ErrorActionPreference = Ignore' {
            $ErrorActionPreference = 'Ignore'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 0" 2>&1

            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }
    }

    Context 'PSNativeCommandUseErrorActionPreference is $false' {
        BeforeAll {
            $pwsh = "$PSHOME/pwsh"
        }
        BeforeEach {
            $PSNativeCommandUseErrorActionPreference = $false
            $Error.Clear()
        }

        It 'Non-zero exit code generates no error when $ErrorActionPreference = Stop' {
            $ErrorActionPreference = 'Stop'

            { & $pwsh -noprofile -noninteractive -c "exit 1" } | Should -Not -Throw

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
        }

        It 'Non-zero exit code generates no error when $ErrorActionPreference = Continue' {
            $ErrorActionPreference = 'Continue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }

        It 'Non-zero exit code generates no error when $ErrorActionPreference = SilentlyContinue' {
            $ErrorActionPreference = 'SilentlyContinue'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }

        It 'Non-zero exit code generates no error when $ErrorActionPreference = Ignore' {
            $ErrorActionPreference = 'Ignore'

            $stderr = & $pwsh -noprofile -noninteractive -c "exit 1" 2>&1

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
            $stderr | Should -BeNullOrEmpty
        }
    }
}
