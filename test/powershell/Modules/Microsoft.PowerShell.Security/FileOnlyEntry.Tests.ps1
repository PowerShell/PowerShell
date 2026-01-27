using namespace System.Diagnostics.CodeAnalysis

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## ----------
## Test Note:
## ----------
## Since these tests change session and system state (constrained language and system lockdown)
## they will all use try/finally blocks instead of Pester AfterEach/AfterAll to ensure session
## and system state is restored.
## Pester AfterEach, AfterAll is not reliable when the session is constrained language or locked down.
##

Import-Module HelpersSecurity

try
{
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:Skip"] = !$IsWindows

    Describe "File Only Entry throws for interactive or non-file scenarios" -Tags 'CI','RequireAdminOnWindows' {

        BeforeAll {
            function MakeTestCase {
                param([Parameter(ValueFromRemainingArguments)] [string[]] $ArgumentList)
                end {
                    @{
                        Arguments = $ArgumentList
                        TestName = 'With args "{0}"' -f ($ArgumentList -join ' ')
                    }
                }
            }

            [SuppressMessage('PSUseDeclaredVarsMoreThanAssignments', 'PwshParameterTestCases')]
            $PwshParameterTestCases = @(
                MakeTestCase -NoExit -Command Get-ChildItem
                MakeTestCase -Command Get-ChildItem
                # File validation should come after `FileOnlyEntry` check, so
                # this file should not need to exist for us to get the error
                # we expect.
                MakeTestCase -NoExit -File this_file_does_not_exist.ps1
                MakeTestCase -File -
            )
        }

        It "<TestName>" -TestCases $PwshParameterTestCases {
            param($Arguments)

            $results = $null
            try {
                Invoke-LanguageModeTestingSupportCmdlet -SetFileOnlyEntry
                if ($Arguments[-1] -eq '-') {
                    $results = 'Get-ChildItem' | & "$PSHOME\pwsh.exe" @Arguments 2>&1
                } else {
                    $results = & "$PSHOME\pwsh.exe" @Arguments 2>&1
                }
            } finally {
                Invoke-LanguageModeTestingSupportCmdlet -RevertFileOnlyEntry
            }

            $results.Exception.Message | Should -Be 'The parameter "-File" is required by policy.'
        }
    }
}
finally
{
    if ($null -ne $defaultParamValues)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
