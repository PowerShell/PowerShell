Describe "CredSSP cmdlet tests" -Tags 'Feature','RequireAdminOnWindows' {

    It "Error returned if invalid parameters: <description>" -Skip:(!$IsWindows) -TestCases @(
        @{params=@{Role="Client"};Description="Client role, no DelegateComputer"},
        @{params=@{Role="Server";DelegateComputer="."};Description="Server role w/ DelegateComputer"}
    ) {
        param ($params)
        { Enable-WSManCredSSP @params } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.WSMan.Management.EnableWSManCredSSPCommand"
    }

    It "Enable-WSManCredSSP works: <description>" -Skip:(!$IsWindows) -TestCases @(
        @{params=@{Role="Client";DelegateComputer="*"};description="client"},
        @{params=@{Role="Server"};description="server"}
    ) {
        param ($params)
        $c = Enable-WSManCredSSP @params -Force
        $c.CredSSP | Should Be $true

        $c = Get-WSManCredSSP
        if ($params.Role -eq "Client")
        {
            $c[0] | Should Match "The machine is configured to allow delegating fresh credentials to the following target\(s\):wsman/\*"
        }
        else
        {
            $c[1] | Should Match "This computer is configured to receive credentials from a remote client computer"
        }
    }

    It "Disable-WSManCredSSP works: <role>" -Skip:(!$IsWindows) -TestCases @(
        @{Role="Client"},
        @{Role="Server"}
    ) {
        param ($role)
        Disable-WSManCredSSP -Role $role | Should BeNullOrEmpty

        $c = Get-WSManCredSSP
        if ($role -eq "Client")
        {
            $c[0] | Should Match "The machine is not configured to allow delegating fresh credentials."
        }
        else
        {
            $c[1] | Should Match "This computer is not configured to receive credentials from a remote client computer"
        }
    }
}

