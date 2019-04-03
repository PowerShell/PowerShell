#
# Module manifest for module 'TestRemoting'
#

@{

RootModule = 'HelpersRemoting.psm1'

ModuleVersion = '1.0'

GUID = '7acf3c68-64f4-4550-bf14-b9361bfbfea3'

CompanyName = 'Microsoft Corporation'

Copyright = 'Copyright (c) Microsoft Corporation. All rights reserved.'

Description = 'Temporary module for remoting tests'

FunctionsToExport = 'New-RemoteRunspace', 'New-RemoteSession', 'Enter-RemoteSession', 'Invoke-RemoteCommand', 'Connect-RemoteSession', 'New-RemoteRunspacePool', 'Get-PipePath'

AliasesToExport = @()

CmdletsToExport = @()
}
