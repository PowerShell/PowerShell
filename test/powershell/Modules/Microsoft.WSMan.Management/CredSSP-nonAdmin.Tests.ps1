Describe "CredSSP cmdlet tests non-admin" -Tags 'Feature' {

    $isAdmin = (New-Object Security.Principal.WindowsPrincipal ([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)

    It "Error returned if runas non-admin: <cmdline>" -Skip:(!$IsWindows -or $isAdmin) -TestCases @(
            @{cmdline = "Enable-WSManCredSSP -Role Server -Force";cmd = "EnableWSManCredSSPCommand"},
            @{cmdline = "Disable-WSManCredSSP -Role Server";cmd = "DisableWSManCredSSPCommand"},
            @{cmdline = "Get-WSManCredSSP";cmd = "GetWSmanCredSSPCommand"}
    ) {
        param ($cmdline, $cmd)
        { Invoke-Expression $cmdline } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.WSMan.Management.$cmd"
    }

    It "Call cmdlet as API" -Skip:(!$IsWindows) {
        $credssp = [Microsoft.WSMan.Management.EnableWSManCredSSPCommand]::new()
        $credssp.Role = "Client"
        $credssp.Role | Should BeExactly "Client"
        $credssp.DelegateComputer = "foo","bar"
        $credssp.DelegateComputer -join ',' | Should Be "foo,bar"
        $credssp.Force = $true
        $credssp.Force | Should Be $true

        $credssp = [Microsoft.WSMan.Management.DisableWSManCredSSPCommand]::new()
        $credssp.Role = "Server"
        $credssp.Role | Should BeExactly "Server"
    }
}
