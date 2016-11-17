##
## SSH Remoting cmdlet tests
## Copyright (c) Microsoft Corporation, 2016
##

Describe "SSHTransport switch parameter value" -Tags 'CI' {

    It "New-PSSession SSHTransport parameter cannot have false value" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            New-PSSession -HostName localhost -UserName UserA -SSHTransport:$false
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "ParameterArgumentValidationError"
    }

    It "Enter-PSSession SSHTransport parameter cannot have false value" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            Enter-PSSession -HostName localhost -UserName UserA -SSHTransport:$false
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "ParameterArgumentValidationError"
    }

    It "Invoke-Command SSHTransport parameter cannot have false value" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            Invoke-Command -ScriptBlock {"Hello"} -HostName localhost -UserName UserA -SSHTransport:$false
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "ParameterArgumentValidationError"
    }
}

Describe "SSHConnection parameter hashtable error conditions" -Tags 'CI' {

    It "SSHConnection parameter hashtable cannot contain empty parameter names" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            New-PSSession -SSHConnection @{ ComputerName = 'localhost'; "" = 'noParameter' }
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }

    It "SSHConnection parameter hashtable cannot contain empty parameter values" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            New-PSSession -SSHConnection @{ HostName = $null }
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }

    It "SSHConnection parameter hashtable cannot contain unknown parameter names" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            New-PSSession -SSHConnection @{ ComputerName = 'localhost'; UnknownParameter = 'Hello' }
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }

    It "SSHConnection parmeter hashtable must contain the ComputerName parameter" {

        [System.Management.Automation.ErrorRecord] $paramError = $null
        try
        {
            New-PSSession -SSHConnection @{ UserName = 'UserName'; KeyFilePath = 'path' }
        }
        catch { $paramError = $_ }

        $paramError.FullyQualifiedErrorId | Should Match "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }
}
