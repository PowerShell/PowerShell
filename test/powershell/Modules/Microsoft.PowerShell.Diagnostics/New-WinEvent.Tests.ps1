# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'New-WinEvent' -Tags "CI" {

    Context "New-WinEvent tests" {

        BeforeAll {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            if ( ! $IsWindows ) {
                $PSDefaultParameterValues["it:skip"] = $true
            }

            $ProviderName = 'Microsoft-Windows-PowerShell'
            $SimpleEventId = 40962
            $ComplexEventId = 32868
        }

        AfterAll {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }

        It 'Simple New-WinEvent without any payload' {
            New-WinEvent -ProviderName $ProviderName -Id $SimpleEventId -Version 1
            $filter = @{ ProviderName = $ProviderName; Id = $SimpleEventId}
            (Get-WinEvent -FilterHashtable $filter).Count | Should -BeGreaterThan 0
        }

        It 'No provider found error' {
            { New-WinEvent -ProviderName NonExistingProvider -Id 0 } | Should -Throw -ErrorId 'System.ArgumentException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'EmptyProviderName error' {
            { New-WinEvent -ProviderName $null -Id 0 } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventId error' {
            { New-WinEvent $ProviderName -Id 999999 } | Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventVersion error' {
            { New-WinEvent -ProviderName $ProviderName -Id $SimpleEventId -Version 99 } | Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'PayloadMismatch error' {
            $logPath = Join-Path $TestDrive 'testlog1.txt'
            # this will print the warning with expected event template to the file
            New-WinEvent -ProviderName $ProviderName -Id $ComplexEventId *> $logPath
            Get-Content $logPath -Raw | Should -Match 'data name="FragmentPayload"'
        }
    }
}
