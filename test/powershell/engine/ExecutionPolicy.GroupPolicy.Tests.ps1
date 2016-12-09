##
## PowerShell ExecutionPolicy GroupPolicy tests
## These are Windows only tests
##

Describe "User group policy execution policy should work" -Tags 'Feature' {

    BeforeAll {

        if (!$IsWindows)
        {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        # Add User group policy execution policy RemoteSigned
        $regHKCUKey = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\PowerShell"
        Remove-Item $regHKCUKey -Force -ErrorAction SilentlyContinue
        New-Item -Path $regHKCUKey -Force
        Set-ItemProperty -Path $regHKCUKey -Name "EnableScripts" -Value 1
        Set-ItemProperty -Path $regHKCUKey -Name "ExecutionPolicy" -Value "RemoteSigned"

        $powershell = Join-Path -Path $PSHOME -ChildPath "powershell"
    }

    AfterAll {

        if (!$IsWindows) 
        { 
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return 
        }

        # Remove User group policy
        Remove-Item $regHKCUKey -Force -ErrorAction SilentlyContinue
    }

    It "Sets User GroupPolicy ExecutionPolicy to RemoteSigned" {

        $command = @'
        return Get-ExecutionPolicy -List | ? { ($_.Scope -eq 'UserPolicy') -and ($_.ExecutionPolicy -eq 'RemoteSigned') }
'@

        $results = & $powershell -c $command -noprofile
        $results -join "," | Should Not BeNullOrEmpty
    }
}
