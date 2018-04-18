# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##
## SSH Remoting cmdlet tests
##

Describe "SSHTransport switch parameter value" -Tags 'Feature' {

    BeforeAll {

        $TestCasesSSHTransport = @(
            @{scriptBlock = {New-PSSession -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'New-PSSession SSHTransport parameter cannot have false value'}
            @{scriptBlock = {Enter-PSSession -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'Enter-PSSession SSHTransport parameter cannot have false value'}
            @{scriptBlock = {Invoke-Command -ScriptBlock {"Hello"} -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'Invoke-Command SSHTransport parameter cannot have false value'}
        )
    }

    It "<testName>" -TestCases $TestCasesSSHTransport {
        param($scriptBlock)
        { & $scriptBlock } | Should -Throw -ErrorId "ParameterArgumentValidationError"
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
        { & $scriptBlock } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }
}
