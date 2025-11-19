@{
GUID="8B2D3C45-6E7F-4A8B-9C1D-2E3F4A5B6C7D"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation."
ModuleVersion="1.0.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion="7.0"
FunctionsToExport = @()
CmdletsToExport=@(
    "Get-ProjectContext",
    "Start-DevCommand",
    "Get-DevCommandStatus",
    "Wait-DevCommand",
    "Stop-DevCommand",
    "Receive-DevCommandOutput",
    "Register-CliTool",
    "Get-CliTool",
    "Unregister-CliTool",
    "Invoke-CliTool",
    "Format-ForAI",
    "Get-AIErrorContext"
)
AliasesToExport = @(
    "gpc",      # Get-ProjectContext
    "devcmd",   # Start-DevCommand
    "fai"       # Format-ForAI
)
NestedModules="Microsoft.PowerShell.Development.dll"
HelpInfoURI = 'https://aka.ms/powershell75-help'
Description = 'PowerShell cmdlets for AI-assisted software development and CLI tool integration'
}
