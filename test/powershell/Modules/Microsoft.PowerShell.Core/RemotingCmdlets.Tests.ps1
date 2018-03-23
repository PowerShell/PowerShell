# Copyright (c) Microsoft Corporation. All rights reserved.
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
        { Invoke-Command -HostName localhost -UserName User -KeyFilePath NoKeyFile -ScriptBlock {1} } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.InvokeCommandCommand"
    }
}
