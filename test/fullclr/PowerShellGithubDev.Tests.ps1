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

        It 'loads Microsoft.PowerShell.Workflow.ServiceCore.dll' {
            workflow wfTest { Split-Path $pwd }
            wfTest | Should Not Be $null ## Also trigger the loading of ServiceCore.dll
            [Microsoft.PowerShell.Workflow.PSWorkflowJob].Assembly.Location | Should Be (
                Join-Path $env:DEVPATH Microsoft.PowerShell.Workflow.ServiceCore.dll)
        }
    }
}

Describe 'Modules for the packge' {
    Context '$env:DEVPATH Modules loading' {

        $originalPSModulePath = $env:PSModulePath 
        try 
        {
            # load all modules only from $env:DEVPATH !!!
            $env:PSModulePath = "$($env:DEVPATH)\Modules"

            It 'loads Microsoft.PowerShell.LocalAccounts' {
                try
                {
                    Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction Stop
                    Get-LocalUser | Should Not Be $null
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue Microsoft.PowerShell.LocalAccounts
                }
            }

            It 'loads Microsoft.PowerShell.Archive' {
                try
                {
                    Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction Stop
                    Set-Content -Path TestDrive:\1.txt -Value ''
                    Compress-Archive -Path TestDrive:\1.txt -DestinationPath TestDrive:\1.zip
                    Get-ChildItem -Path TestDrive:\1.zip | Should Not Be $null
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue Microsoft.PowerShell.Archive
                }
            }

            It 'loads PsScheduledJob' {
                try
                {
                    Import-Module PsScheduledJob -ErrorAction Stop
                    New-ScheduledJobOption | Should Not Be $null
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue PsScheduledJob
                }
            }            
        }
        finally
        {
            $env:PSModulePath = $originalPSModulePath
        }
    }
}

