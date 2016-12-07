##
## PowerShell ExecutionPolicy GroupPolicy tests
## These are Windows only tests
##

Describe "User group policy execution policy should work" -Tags 'Feature' {

    BeforeAll {

        if (!$IsWindows) { return }

        # Add User group policy execution policy RemoteSigned
        $regHKCUKey = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\PowerShell"
        Remove-Item $regHKCUKey -Force -ErrorAction SilentlyContinue
        New-Item -Path $regHKCUKey -Force
        Set-ItemProperty -Path $regHKCUKey -Name "EnableScripts" -Value 1
        Set-ItemProperty -Path $regHKCUKey -Name "ExecutionPolicy" -Value "RemoteSigned"
    }

    AfterAll {

        if (!$IsWindows) { return }

        # Remove User group policy
        Remove-Item $regHKCUKey -Force -ErrorAction SilentlyContinue
    }

    It "Sets User GroupPolicy ExecutionPolicy to RemoteSigned" -Skip:(!$IsWindows) {

        $command = @'
        return Get-ExecutionPolicy -List | ? { ($_.Scope -eq 'UserPolicy') -and ($_.ExecutionPolicy -eq 'RemoteSigned') }
'@

        $psPath = Join-Path -Path $PSHOME -ChildPath "powershell.exe"
        $results = & $psPath -c $command
        $results -join "," | Should Not BeNullOrEmpty
    }
}
