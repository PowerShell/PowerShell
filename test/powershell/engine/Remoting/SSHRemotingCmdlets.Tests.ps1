##
## SSH Remoting cmdlet tests
##

Describe "SSHTransport switch parameter value" -Tags 'Feature' {

    BeforeAll {

        $TestCasesSSHTransport = @(
            @{ scriptBlock = {New-PSSession -HostName localhost -UserName UserA -SSHTransport:$false};
               testName = 'New-PSSession SSHTransport parameter cannot have false value';
               ErrorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.NewPSSessionCommand"
            }
            @{ scriptBlock = {Enter-PSSession -HostName localhost -UserName UserA -SSHTransport:$false};
               testName = 'Enter-PSSession SSHTransport parameter cannot have false value';
               ErrorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.EnterPSSessionCommand"
            }
            @{ scriptBlock = {Invoke-Command -ScriptBlock {"Hello"} -HostName localhost -UserName UserA -SSHTransport:$false};
               testName = 'Invoke-Command SSHTransport parameter cannot have false value';
               ErrorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeCommandCommand"
            }
        )
    }

    It "<testName>" -TestCases $TestCasesSSHTransport {
        param($scriptBlock, $ErrorId)

        $scriptBlock | ShouldBeErrorId $ErrorId
    }
}

Describe "SSHConnection parameter hashtable error conditions" -Tags 'Feature' {

    BeforeAll {

        $TestCasesSSHConnection = @(
            @{scriptBlock = {New-PSSession -SSHConnection @{ ComputerName = "localhost"; "" = "noParameter" }}; testName = 'SSHConnection parameter hashtable cannot contain empty parameter names'}
            @{scriptBlock = {New-PSSession -SSHConnection @{ HostName = $null }}; testName = 'SSHConnection parameter hashtable cannot contain empty parameter values'}
            @{scriptBlock = {New-PSSession -SSHConnection @{ ComputerName = "localhost"; UnknownParameter = "Hello" }}; testName = 'SSHConnection parameter hashtable cannot contain unknown parameter names'}
            @{scriptBlock = {New-PSSession -SSHConnection @{ UserName = "UserName"; KeyFilePath = "path" }}; testName = 'SSHConnection parmeter hashtable must contain the ComputerName parameter'}
            @{scriptBlock = {New-PSSession -SSHConnection @{ ComputerName = "computerA"; hostname = "computerB" }}; testName = 'SSHConnection parameter hashtable cannot contain both ComputerName and HostName parameters' }
            @{scriptBlock = {New-PSSession -SSHConnection @{ keyfilepath = "pathA"; IdentityFilePath = "pathB" }}; testName = 'SSHConnection parameter hashtable cannot contain both KeyFilePath and IdentityFilePath parameters' }
        )
    }

    It "<testName>" -TestCases $TestCasesSSHConnection {
        param ($scriptBlock)

        $scriptBlock | ShouldBeErrorId "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }
}
