@{
GUID="CA046F10-CA64-4740-8FF9-2565DBA61A4F"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation."
ModuleVersion="7.0.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion="3.0"
FunctionsToExport = @()
CmdletsToExport="Get-WinEvent", "New-WinEvent", "Get-Counter"
AliasesToExport = @()
NestedModules="Microsoft.PowerShell.Commands.Diagnostics.dll"
TypesToProcess="GetEvent.types.ps1xml"
FormatsToProcess="Event.format.ps1xml", "Diagnostics.format.ps1xml"
HelpInfoURI = 'https://aka.ms/powershell73-help'
}
