# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##
## SSH Remoting cmdlet tests
##

Describe "SSHTransport switch parameter value" -Tags 'Feature' {

    BeforeAll {

        $TestCasesSSHTransport = @(
            @{scriptBlock = {New-PSSession -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'New-PSSession SSHTransport parameter cannot have false value'}
            @{scriptBlock = {Enter-PSSession -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'Enter-PSSession SSHTransport parameter cannot have false value'}
            @{scriptBlock = {Invoke-Command -Scriptblock {"Hello"} -HostName localhost -UserName UserA -SSHTransport:$false}; testName = 'Invoke-Command SSHTransport parameter cannot have false value'}
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

Describe "SSHConnection parameter hashtable type conversions" -Tags 'Feature', 'Slow' {

    BeforeAll {

        # Port 49151 is IANA reserved, so it should not be in use. If type conversion fails
        # then New-PSSession will throw before even attempting to open the session.
        $TestCasesSSHConnection = @(
            @{scriptBlock = {New-PSSession -ErrorAction Stop -SSHConnection @{ Port = 49151; ComputerName = [psobject]'localhost' }}; testName = 'SSHConnection can convert PSObject to string'}
            @{scriptBlock = {New-PSSession -ErrorAction Stop -SSHConnection @{ Port = [psobject]49151; ComputerName = 'localhost' }}; testName = 'SSHConnection can convert PSObject to int'}
        )
    }

    It "<testName>" -TestCases $TestCasesSSHConnection {
        param($scriptBlock)

        $err = $null
        try
        {
            & $scriptBlock
        }
        catch
        {
            $err = $_
        }
        # The exact returned error id string varies depending on platform,
        # but will always contain 'PSSessionOpenFailed'.
        $err.FullyQualifiedErrorId | Should -Match 'PSSessionOpenFailed'
    }
}

Describe "No hangs when host doesn't exist" -Tag "CI" {
    $testCases = @(
        @{
            Name = 'Verifies no hang for New-PSSession with non-existing host name'
            ScriptBlock = { New-PSSession -HostName "test-notexist" -UserName "test" -ErrorAction Stop }
            FullyQualifiedErrorId = 'PSSessionOpenFailed'
        },
        @{
            Name = 'Verifies no hang for Invoke-Command with non-existing host name'
            ScriptBlock = { Invoke-Command -HostName "test-notexist" -UserName "test" -ScriptBlock { 1 } -ErrorAction Stop }
            FullyQualifiedErrorId = 'PSSessionStateBroken'
        }
    )

    It "<Name>" -TestCases $testCases {
        param ($ScriptBlock, $FullyQualifiedErrorId)

        $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -ExceptionType 'System.Management.Automation.Remoting.PSRemotingTransportException'
    }
}
