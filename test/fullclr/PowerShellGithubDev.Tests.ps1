Describe 'build.psm1 and powershell.exe' {
    Context '$env:DEVPATH assemblies loading' {
        It 'has $env:DEVPATH set' {
            $env:DEVPATH | Should Not Be $null
        }

        It 'loads System.Management.Automation.dll' {
            [psobject].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH System.Management.Automation.dll)
        }

        It 'loads Microsoft.PowerShell.Commands.Management.dll' {
            [Microsoft.PowerShell.Commands.GetChildItemCommand].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH Microsoft.PowerShell.Commands.Management.dll)
        }

        It 'loads Microsoft.PowerShell.Commands.Utility.dll' {
            [Microsoft.PowerShell.Commands.UtilityResources].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH Microsoft.PowerShell.Commands.Utility.dll)
        }

        It 'loads Microsoft.PowerShell.ConsoleHost.dll' {
            [Microsoft.PowerShell.ConsoleShell].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH Microsoft.PowerShell.ConsoleHost.dll)
        }

        It 'loads Microsoft.PowerShell.Security.dll' {
            [Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH Microsoft.PowerShell.Security.dll)
        }
    }
}
