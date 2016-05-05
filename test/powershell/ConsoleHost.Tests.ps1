Describe "ConsoleHost unit tests" {
    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

    Context "Command-line parsing" {

        foreach ($x in "--help", "-help", "-h", "-?", "--he", "-hel", "--HELP", "-hEl") {
            It "Accepts '$x' as a parameter for help" {
                & $powershell -noprofile $x | ?{ $_ -match "PowerShell[.exe] -Help | -? | /?" } | Should Not BeNullOrEmpty
            }
        }

        It "Should accept a Base64 encoded command" {
            $commandString = "Get-Location"
            $encodedCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($commandString))
            # We don't compare to `Get-Location` directly because object and formatted output comparisons are difficult
            $expected = & $powershell -noprofile -command $commandString
            $actual = & $powershell -noprofile -EncodedCommand $encodedCommand
            $actual | Should Be $expected
        }
    }
}
