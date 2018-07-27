@{
GUID="CA046F10-CA64-4740-8FF9-2565DBA61A4F"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation. All rights reserved."
ModuleVersion="6.1.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion="3.0"
FunctionsToExport = @()
CmdletsToExport="Get-WinEvent", "New-WinEvent" # Counter CmdLets Disabled #4272: "Get-Counter", "Import-Counter", "Export-Counter"
AliasesToExport = @()
NestedModules="Microsoft.PowerShell.Commands.Diagnostics.dll"
TypesToProcess="GetEvent.types.ps1xml"
FormatsToProcess="Event.format.ps1xml", "Diagnostics.format.ps1xml"
HelpInfoURI = 'https://go.microsoft.com/fwlink/?linkid=855954'
}
