# first check to see which platform we're on. If we're on windows we should be able
# to be sure whether we're running elevated. If we're on Linux, we can use whoami to
# determine whether we're elevated
Describe "Set-Date" -Tag "CI" {
    BeforeAll {
        if ( $IsWindows ) {
            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
            $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $identity
            if ($windowsPrincipal.IsInRole("Administrators") -eq 1) { $IsElevated = $true } else { $IsElevated = $false }
        }
        else {
            if ( (whoami) -match "root" ) {
                $IsElevated = $true
            }
            else {
                $IsElevated = $false
            }
        }
    }
    It "Set-Date should be able to set the date in an elevated context" -Skip:(! $IsElevated) {
        { get-date | set-date } | Should not throw
    }
    It "Set-Date should produce an error in a non-elevated context" -Skip:($IsElevated) {
        { get-date |set-date} | should throw
    }
}
