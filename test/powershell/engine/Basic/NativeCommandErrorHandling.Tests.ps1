# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Functional tests to verify that native executables throw errors (non-terminating and terminating) appropriately
# when $PSNativeCommandUseErrorActionPreference is $true

Describe 'Native command error handling tests' -Tags 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $exeName = $IsWindows ? 'testexe.exe' : 'testexe'
        $exePath = @(Get-Command $exeName -Type Application)[0].Path

        $errorActionPrefTestCases = @(
            @{ ErrorActionPref = 'Stop' }
            @{ ErrorActionPref = 'Continue' }
            @{ ErrorActionPref = 'SilentlyContinue' }
            @{ ErrorActionPref = 'Ignore' }
        )
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
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

            { testexe -returncode 1 } | Should -Throw -ErrorId 'ProgramExitedWithNonZeroCode'

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -BeExactly 'ProgramExitedWithNonZeroCode'
            $error[0].TargetObject | Should -BeExactly $exePath
        }

        It 'Non-zero exit code outputs a non-teminating error for $ErrorActionPreference = ''Continue''' {
            $ErrorActionPreference = 'Continue'

            $stderr = testexe -returncode 1 2>&1

            $error[0].FullyQualifiedErrorId | Should -BeExactly 'ProgramExitedWithNonZeroCode'
            $error[0].TargetObject | Should -BeExactly $exePath
            $stderr[1].Exception.Message | Should -BeExactly ("Program `"$exeName`" ended with non-zero exit code: 1 ({0})." -f ($IsWindows ? '0x00000001' : '0x01'))
        }

        It "Non-boolean value should not cause type casting error when the native command exited with non-zero code" {
            $ErrorActionPreference = 'Continue'

            $PSNativeCommandUseErrorActionPreference = 'Yeah'
            $PSNativeCommandUseErrorActionPreference | Should -BeExactly 'Yeah'

            $stderr = testexe -returncode 1 2>&1

            $error[0].FullyQualifiedErrorId | Should -BeExactly 'ProgramExitedWithNonZeroCode'
            $error[0].TargetObject | Should -BeExactly $exePath
            $stderr[1].Exception.Message | Should -BeExactly ("Program `"$exeName`" ended with non-zero exit code: 1 ({0})." -f ($IsWindows ? '0x00000001' : '0x01'))
        }

        It 'Non-zero exit code generates a non-teminating error for $ErrorActionPreference = ''SilentlyContinue''' {
            $ErrorActionPreference = 'SilentlyContinue'

            testexe -returncode 1 > $null

            $error.Count | Should -Be 1
            $error[0].FullyQualifiedErrorId | Should -BeExactly 'ProgramExitedWithNonZeroCode'
            $error[0].TargetObject | Should -BeExactly $exePath
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

            $output = testexe -returncode 0

            $output | Should -BeExactly '0'
            $LASTEXITCODE | Should -Be 0
            $Error.Count | Should -Be 0
        }

        It 'Works as expected with a try/catch block when $ErrorActionPreference = ''<ErrorActionPref>''' -TestCase $errorActionPrefTestCases {
            param($ErrorActionPref)

            $ErrorActionPreference = $ErrorActionPref

            $threw = $false
            $continued = $false
            $hitFinally = $false
            try
            {
                testexe -returncode 17 2>&1 > $null
                $continued = $true
            }
            catch
            {
                $threw = $true
                $exception = $_.Exception
            }
            finally
            {
                $hitFinally = $true
            }

            $hitFinally | Should -BeTrue
            $continued  | Should -Be ($ErrorActionPreference -ne 'Stop')
            $threw      | Should -Be ($ErrorActionPreference -eq 'Stop')

            if ($threw)
            {
                $exception.Path      | Should -BeExactly (Get-Command -Name testexe -CommandType Application).Path
                $exception.ExitCode  | Should -Be $LASTEXITCODE
                $exception.ProcessId | Should -BeGreaterThan 0
            }
        }

        It 'Works with trap when $ErrorActionPreference = ''<ErrorActionPref>''' -TestCases $errorActionPrefTestCases {
            param($ErrorActionPref)

            $ErrorActionPreference = $ErrorActionPref

            trap
            {
                $hitTrap = $true
                $exception = $_
                continue
            }

            $hitTrap = $false

            # Expect this to be trapped
            testexe -returncode 17 2>&1 > $null

            if ($ErrorActionPreference -eq 'Stop')
            {
                $hitTrap             | Should -BeTrue
                $exception.ExitCode  | Should -Be $LASTEXITCODE
                $exception.Path      | Should -BeExactly (Get-Command -Name testexe -CommandType Application).Path
                $exception.ProcessId | Should -BeGreaterThan 0
            }
            else
            {
                $hitTrap   | Should -BeFalse
                $exception | Should -BeNullOrEmpty
            }
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

        It "Non-boolean value should not cause type casting error when the native command exited with non-zero code" {
            $ErrorActionPreference = 'Continue'

            $PSNativeCommandUseErrorActionPreference = 0
            $PSNativeCommandUseErrorActionPreference | Should -Be 0
            $PSNativeCommandUseErrorActionPreference | Should -BeOfType 'System.Int32'

            testexe -returncode 1 > $null

            $LASTEXITCODE | Should -Be 1
            $Error.Count | Should -Be 0
        }
    }
}
