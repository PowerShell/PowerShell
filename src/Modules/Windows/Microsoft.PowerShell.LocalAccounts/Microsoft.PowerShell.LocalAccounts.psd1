@{
RootModule = 'Microsoft.Powershell.LocalAccounts'
GUID = '8e362604-2c0b-448f-a414-a6a690a644e2'
Author = 'PowerShell'
CompanyName = 'Microsoft Corporation'
Copyright = 'Copyright (c) Microsoft Corporation.'
ModuleVersion = '7.0.0.0'
CompatiblePSEditions = @("Core")
PowerShellVersion = '3.0'
Description = 'Provides cmdlets to work with local users and local groups'
FormatsToProcess = @('LocalAccounts.format.ps1xml')
CmdletsToExport = @(
    'Add-LocalGroupMember',
    'Disable-LocalUser',
    'Enable-LocalUser',
    'Get-LocalGroup',
    'Get-LocalGroupMember',
    'Get-LocalUser',
    'New-LocalGroup',
    'New-LocalUser',
    'Remove-LocalGroup',
    'Remove-LocalGroupMember',
    'Remove-LocalUser',
    'Rename-LocalGroup',
    'Rename-LocalUser',
    'Set-LocalGroup',
    'Set-LocalUser'
    )
AliasesToExport= @( "algm", "dlu", "elu", "glg", "glgm", "glu", "nlg", "nlu", "rlg", "rlgm", "rlu", "rnlg", "rnlu", "slg", "slu")
HelpInfoURI = 'https://go.microsoft.com/fwlink/?LinkId=717973'
}
