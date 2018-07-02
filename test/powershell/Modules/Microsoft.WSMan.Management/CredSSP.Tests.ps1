# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "CredSSP cmdlet tests" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        $powershell = Join-Path $PSHOME "pwsh"
        $notEnglish = $false
        $IsToBeSkipped = !$IsWindows;

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( $IsToBeSkipped )
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            if ([System.Globalization.CultureInfo]::CurrentCulture.Name -ne "en-US")
            {
                $notEnglish = $true
            }
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    BeforeEach {
        if ( ! $IsToBeSkipped )
        {
            $errtxt = "$testdrive/error.txt"
            Remove-Item $errtxt -Force -ErrorAction SilentlyContinue
            $donefile = "$testdrive/done"
            Remove-Item $donefile -Force -ErrorAction SilentlyContinue
        }
    }

    It "Error returned if invalid parameters: <description>" -TestCases @(
        @{params=@{Role="Client"};Description="Client role, no DelegateComputer"},
        @{params=@{Role="Server";DelegateComputer="."};Description="Server role w/ DelegateComputer"}
    ) {
        param ($params)
        { Enable-WSManCredSSP @params } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.WSMan.Management.EnableWSManCredSSPCommand"
    }

    It "Enable-WSManCredSSP works: <description>" -Skip:($NotEnglish -or $IsToBeSkipped) -TestCases @(
        @{params=@{Role="Client";DelegateComputer="*"};description="client"},
        @{params=@{Role="Server"};description="server"}
    ) {
        param ($params)
        $c = Enable-WSManCredSSP @params -Force
        $c.CredSSP | Should -Be $true

        $c = Get-WSManCredSSP
        if ($params.Role -eq "Client")
        {
            $c[0] | Should -Match "The machine is configured to allow delegating fresh credentials to the following target\(s\):wsman/\*"
        }
        else
        {
            $c[1] | Should -Match "This computer is configured to receive credentials from a remote client computer"
        }
    }

    It "Disable-WSManCredSSP works: <role>" -Skip:($NotEnglish -or $IsToBeSkipped) -TestCases @(
        @{Role="Client"},
        @{Role="Server"}
    ) {
        param ($role)
        Disable-WSManCredSSP -Role $role | Should -BeNullOrEmpty

        $c = Get-WSManCredSSP
        if ($role -eq "Client")
        {
            $c[0] | Should -Match "The machine is not configured to allow delegating fresh credentials."
        }
        else
        {
            $c[1] | Should -Match "This computer is not configured to receive credentials from a remote client computer"
        }
    }

    It "Call cmdlet as API" {
        $credssp = [Microsoft.WSMan.Management.EnableWSManCredSSPCommand]::new()
        $credssp.Role = "Client"
        $credssp.Role | Should -BeExactly "Client"
        $credssp.DelegateComputer = "foo", "bar"
        $credssp.DelegateComputer -join ',' | Should -Be "foo,bar"
        $credssp.Force = $true
        $credssp.Force | Should -Be $true

        $credssp = [Microsoft.WSMan.Management.DisableWSManCredSSPCommand]::new()
        $credssp.Role = "Server"
        $credssp.Role | Should -BeExactly "Server"
    }
}

Describe "CredSSP cmdlet error cases tests" -Tags 'Feature' {

    It "Error returned if runas non-admin: <cmdline>" -Skip:(!$IsWindows) -TestCases @(
        @{cmdline = "Enable-WSManCredSSP -Role Server -Force"; cmd = "EnableWSManCredSSPCommand"},
        @{cmdline = "Disable-WSManCredSSP -Role Server"; cmd = "DisableWSManCredSSPCommand"},
        @{cmdline = "Get-WSManCredSSP"; cmd = "GetWSmanCredSSPCommand"}
    ) {
        param ($cmdline, $cmd)

        $scriptBlock = [scriptblock]::Create($cmdline)
        $scriptBlock | should -Throw -ErrorId "System.InvalidOperationException,Microsoft.WSMan.Management.$cmd"
    }
}
