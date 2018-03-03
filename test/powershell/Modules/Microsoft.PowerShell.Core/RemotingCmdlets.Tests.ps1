# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "SSH Remoting Cmdlet Tests" -Tags "Feature" {

    It "Enter-PSSession HostName parameter set should throw error for invalid key path" {

        try
        {
            Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile
            throw "Enter-PSSession did not throw expected PathNotFound exception."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should -Be "PathNotFound,Microsoft.PowerShell.Commands.EnterPSSessionCommand"
        }
    }

    It "New-PSSession HostName parameter set should throw error for invalid key path" {

        try
        {
            New-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile
            throw "New-PSSession did not throw expected PathNotFound exception."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should -Be "PathNotFound,Microsoft.PowerShell.Commands.NewPSSessionCommand"
        }
    }

    It "Invoke-Command HostName parameter set should throw error for invalid key path" {

        try
        {
            Invoke-Command -HostName localhost -UserName User -KeyFilePath NoKeyFile -ScriptBlock {1}
            throw "Invoke-Command did not throw expected PathNotFound exception."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should -Be "PathNotFound,Microsoft.PowerShell.Commands.InvokeCommandCommand"
        }
    }
}
