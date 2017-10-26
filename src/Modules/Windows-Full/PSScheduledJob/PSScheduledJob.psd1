@{

ModuleToProcess = 'Microsoft.PowerShell.ScheduledJob.dll'

ModuleVersion = '1.1.0.0'

GUID = '50cdb55f-5ab7-489f-9e94-4ec21ff51e59'

Author = 'Microsoft Corporation'

CompanyName = 'Microsoft Corporation'

Copyright = 'Copyright (c) Microsoft Corporation. All rights reserved.'

PowerShellVersion = '3.0'

CLRVersion = '4.0'

TypesToProcess = 'PSScheduledJob.types.ps1xml'

FormatsToProcess="PSScheduledJob.Format.ps1xml"

CmdletsToExport = 'New-JobTrigger', 'Add-JobTrigger', 'Remove-JobTrigger',
               'Get-JobTrigger', 'Set-JobTrigger', 'Enable-JobTrigger',
               'Disable-JobTrigger', 'New-ScheduledJobOption', 'Get-ScheduledJobOption',
               'Set-ScheduledJobOption', 'Register-ScheduledJob', 'Get-ScheduledJob',
               'Set-ScheduledJob', 'Unregister-ScheduledJob', 'Enable-ScheduledJob',
               'Disable-ScheduledJob'
AliasesToExport = @()
FunctionsToExport = @()

HelpInfoURI = 'https://go.microsoft.com/fwlink/?linkid=390816'
}
