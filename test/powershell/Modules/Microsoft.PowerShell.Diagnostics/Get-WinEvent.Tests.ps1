Describe 'Get-WinEvent' -Tags "CI" {

    # Get-WinEvent works only on windows
    It 'can query a System log' -Skip:(-not $IsWindows) {
        Get-WinEvent -LogName System -MaxEvents 1 | Should Not Be $null
    }
}
