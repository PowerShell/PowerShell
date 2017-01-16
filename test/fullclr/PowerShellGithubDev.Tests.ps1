$originalPSModulePath = $env:PSModulePath
try
{
    # load all modules only from $env:DEVPATH !!!
    $env:PSModulePath = "$($env:DEVPATH)\Modules"

    # this Describe makes sure we build all the dlls we want and load them from the right place
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

    # this Describe makes sure we binplace all the files, like psd1, psm1, ps1xml and load usable modules from them
    Describe 'Modules for the package' {
        Context '$env:DEVPATH Modules loading' {
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


            It 'loads PSWorkflowUtility' {
                try
                {
                    Import-Module PSWorkflowUtility -ErrorAction Stop
                    Invoke-AsWorkflow -Expression { 'foo' } | Should Be 'foo'
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue PSWorkflowUtility
                }
            }

            It 'loads PSWorkflow' {
                try
                {
                    Import-Module PSWorkflow -ErrorAction Stop
                    New-PSWorkflowExecutionOption | Should Not Be $null
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue PSWorkflow
                }
            }

            It 'loads CimCmdlets' {
                try
                {
                    Import-Module CimCmdlets -ErrorAction Stop
                    Get-CimClass -ClassName CIM_Error | Should Not Be $null
                    [Microsoft.Management.Infrastructure.CimCmdlets.AsyncResultType].Assembly.Location | Should Be (
                        Join-Path $env:DEVPATH Microsoft.Management.Infrastructure.CimCmdlets.dll)
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue CimCmdlets
                }
            }

            It 'loads Microsoft.WSMan.Management' {
                try
                {
                    Import-Module Microsoft.WSMan.Management -ErrorAction Stop
                    Test-WSMan | Should Not Be $null
                    [Microsoft.WSMan.Management.TestWSManCommand].Assembly.Location | Should Be (
                        Join-Path $env:DEVPATH Microsoft.WSMan.Management.dll)
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue Microsoft.WSMan.Management
                }
            }

            It 'loads Microsoft.PowerShell.Diagnostics' {
                try
                {
                    Import-Module Microsoft.PowerShell.Diagnostics -ErrorAction Stop
                    Get-WinEvent -LogName System -MaxEvents 1 | Should Not Be $null
                    [Microsoft.PowerShell.Commands.GetWinEventCommand].Assembly.Location | Should Be (
                        Join-Path $env:DEVPATH Microsoft.PowerShell.Commands.Diagnostics.dll)
                }
                finally
                {
                    Remove-Module -ErrorAction SilentlyContinue Microsoft.PowerShell.Diagnostics
                }
            }
        }
    }

}
finally
{
    $env:PSModulePath = $originalPSModulePath
}
