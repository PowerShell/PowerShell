# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "SSH Remoting Cmdlet Tests" -Tags "Feature" {

    It "Enter-PSSession HostName parameter set should throw error for invalid key path" {
        { Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.EnterPSSessionCommand"
    }

    It "New-PSSession HostName parameter set should throw error for invalid key path" {
        { New-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }

    It "Invoke-Command HostName parameter set should throw error for invalid key path" {
        { Invoke-Command -HostName localhost -UserName User -KeyFilePath NoKeyFile -ScriptBlock { 1 } } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.InvokeCommandCommand"
    }

    It "Invoke-Command should support positional parameter ScriptBlock when using parameter set '<ParameterSetName>'" -TestCases @{ParameterSetName = 'SSHHost' }, @{ParameterSetName = 'SSHHostHashParam' } {
        param ([string]$ParameterSetName)
        $commandInfo = Get-Command -Name Invoke-Command
        $sshParameterSet = $commandInfo.ParameterSets | Where-Object { $_.Name -eq $ParameterSetName }
        $scriptBlockPosition = $sshParameterSet.Parameters | Where-Object { $_.Name -eq 'ScriptBlock' } | Select-Object -ExpandProperty Position
        $scriptBlockPosition | Should -Be 1
    }
}
