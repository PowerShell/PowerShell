# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Functional tests to verify that errors (non-terminating and terminating) appropriately
# when $PSNativeCommandUseErrorActionPreference is $true

Describe 'Native command error handling tests' -Tags 'CI' {
    BeforeAll {
        if (-not [ExperimentalFeature]::IsEnabled('PSNativeCommandErrorActionPreference'))
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }

        $exeName = $IsWindows ? 'testexe.exe' : 'testexe'

        $errorActionPrefTestCases = @(
            @{ ErrorActionPref = 'Stop' }
            @{ ErrorActionPref = 'Continue' }
            @{ ErrorActionPref = 'SilentlyContinue' }
            @{ ErrorActionPref = 'Ignore' }
        )
    }

    AfterAll {
        $PSDefaultParameterValues['It:Skip'] = $false
    }

    BeforeEach {
        $Error.Clear()
    }

    Context 'PSNativeCommandUseErrorActionPreference is $true' {
        BeforeEach {
            $PSNativeCommandUseErrorActionPreference = $true
        }

        It 'Non-zero exit code throws teminating error for $ErrorActionPreference = ''Stop''' {
            $ErrorActionPreference = 'Stop'

            { testexe -returncode 1 } | Should -Throw -ErrorId 'ProgramFailedToComplete'

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
        }

        It 'Non-zero exit code outputs a non-teminating error for $ErrorActionPreference = ''Continue''' {
            $ErrorActionPreference = 'Continue'

            $stderr = testexe -returncode 1 2>&1

            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
            $stderr[1].Exception.Message | Should -Be "Program `"$exeName`" ended with non-zero exit code 1."
        }

        It 'Non-zero exit code generrates a non-teminating error for $ErrorActionPreference = ''SilentlyContinue''' {
            $ErrorActionPreference = 'SilentlyContinue'

            testexe -returncode 1 > $null

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -Be 'ProgramFailedToComplete'
        }

        It 'Non-zero exit code does not generates an error record for $ErrorActionPreference = ''Ignore''' {
            $ErrorActionPreference = 'Ignore'

            testexe -returncode 1 > $null

            $LASTEXITCODE | Should -Be 1
            $error.Count | Should -Be 0
        }

        It 'Zero exit code generates no error for $ErrorActionPreference = ''<ErrorActionPref>''' -TestCases $errorActionPrefTestCases {
            param($ErrorActionPref)

            $ErrorActionPreference = $ErrorActionPref

            if ($ErrorActionPref -eq 'Stop') {
                { testexe -returncode 0 } | Should -Not -Throw
            }
            else {
                testexe -returncode 0 > $null
            }

            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
        }
    }

    Context 'PSNativeCommandUseErrorActionPreference is $false' {
        BeforeEach {
            $PSNativeCommandUseErrorActionPreference = $false
        }

        It 'Non-zero exit code generates no error for $ErrorActionPreference = ''<ErrorActionPref>''' -TestCases $errorActionPrefTestCases {
            param($ErrorActionPref)

            $ErrorActionPreference = $ErrorActionPref

            if ($ErrorActionPref -eq 'Stop') {
                { testexe -returncode 1 } | Should -Not -Throw
            }
            else {
                testexe -returncode 1 > $null
            }

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
        }
    }
}
