Describe 'New-WinEvent' -Tags "CI" {

    Context "New-WinEvent tests" {
        
        It 'Simple New-WinEvent' -Skip:(-not $IsWindows) {
            New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 40962 -Version 1 # simple event without any payload
            $filter = @{ ProviderName = 'Microsoft-Windows-PowerShell'; Id = 40962}
            (Get-WinEvent -filterHashtable $filter).Count -gt 0 | Should be $true
        }
        
        It 'No provider found error' -Skip:(-not $IsWindows) {
            { New-WinEvent -ProviderName NonExistingProvider -Id 0 } | ShouldBeErrorID 'System.ArgumentException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }
        
        It 'EmptyProviderName error' -Skip:(-not $IsWindows) {
            { New-WinEvent -ProviderName $null -Id 0 } | ShouldBeErrorID 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventId error' -Skip:(-not $IsWindows) {
            { New-WinEvent Microsoft-Windows-PowerShell -Id 999999 } | ShouldBeErrorID 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }

        It 'IncorrectEventVersion error' -Skip:(-not $IsWindows) {
            { New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 40962 -Version 99 } | ShouldBeErrorID 'Microsoft.PowerShell.Commands.EventWriteException,Microsoft.PowerShell.Commands.NewWinEventCommand'
        }
        
        It 'PayloadMismatch error' -Skip:(-not $IsWindows) {
            $logPath = join-path $TestDrive 'testlog1.txt'
            New-WinEvent -ProviderName Microsoft-Windows-PowerShell -Id 32868 *> $logPath # this will print the warning with expected event template to the file
            Get-Content $logPath -Raw | Should Match 'data name="FragmentPayload"'
        }
    }
}
