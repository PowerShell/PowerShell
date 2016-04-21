Describe "CoreConsoleHost unit tests" {
    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

    Context "Command-line parsing" {

        foreach ($x in "--help", "-help", "-h", "-?", "--he", "-hel", "--HELP", "-hEl") {
            It "Accepts '$x' as a parameter for help" {
                & $powershell -noprofile $x | ?{ $_ -match "usage: powershell" } | Should Not BeNullOrEmpty
            }
        }
    }
}
