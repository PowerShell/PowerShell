Describe 'New-WinEvent' -Tags "CI" {

    Context "New-WinEvent tests" {
        
        BeforeAll {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            if ( ! $IsWindows ) {
                $PSDefaultParameterValues["it:skip"] = $true
            }
        }
        
        AfterAll {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }

        It 'Simple New-WinEvent' {
            New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 40962 -Version 1 # simple event without any payload
            $filter = @{ ProviderName = 'Microsoft-Windows-PowerShell'; Id = 40962}
            (Get-WinEvent -filterHashtable $filter).Count | Should BeGreaterThan 0
        }
        
        It 'No provider found error' {
            { New-WinEvent -ProviderName NonExistingProvider -Id 0 } | ShouldBeErrorId 'System.ArgumentException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }
        
        It 'EmptyProviderName error' {
            { New-WinEvent -ProviderName $null -Id 0 } | ShouldBeErrorId 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventId error' {
            { New-WinEvent Microsoft-Windows-PowerShell -Id 999999 } | ShouldBeErrorId 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventVersion error' {
            { New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 40962 -Version 99 } | ShouldBeErrorId 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }
        
        It 'PayloadMismatch error' {
            $logPath = join-path $TestDrive 'testlog1.txt'
            # this will print the warning with expected event template to the file
            New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 32868 *> $logPath
            Get-Content $logPath -Raw | Should Match 'data name="FragmentPayload"'
        }
    }
}
