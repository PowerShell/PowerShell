@{
RootModule = 'OpenCover.psm1'
ModuleVersion = '1.1.0.0'
GUID = '4eedcffd-26e8-4172-8aad-9b882c13d370'
Author = 'PowerShell'
CompanyName = 'Microsoft Corporation'
Copyright = 'Copyright (c) Microsoft Corporation.'
Description = 'Module to install OpenCover and run Powershell tests to collect code coverage'
DotNetFrameworkVersion = 4.5
TypesToProcess = @('OpenCover.Types.ps1xml')
FormatsToProcess = @('OpenCover.Format.ps1xml')
FunctionsToExport = @('Get-CodeCoverage','Compare-CodeCoverage', 'Compare-FileCoverage', 'Install-OpenCover', 'Invoke-OpenCover','Format-FileCoverage')
CmdletsToExport = @()
VariablesToExport = @()
AliasesToExport = @()
}

