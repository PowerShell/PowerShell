Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

Describe "SSH Remoting Cmdlet Tests" -Tags "Feature" {

    It "Enter-PSSession HostName parameter set should throw error for invalid key path" {

        { Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.EnterPSSessionCommand"
    }

    It "New-PSSession HostName parameter set should throw error for invalid key path" {

        { New-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }

    It "Invoke-Command HostName parameter set should throw error for invalid key path" {

        { Invoke-Command -HostName localhost -UserName User -KeyFilePath NoKeyFile -ScriptBlock {1} } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.InvokeCommandCommand"
    }
}
