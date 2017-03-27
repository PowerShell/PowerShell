@{
RootModule = 'OpenCover.psm1'
ModuleVersion = '1.1.0.0'
GUID = '4eedcffd-26e8-4172-8aad-9b882c13d370'
Author = 'Microsoft Corporation'
CompanyName = 'Microsoft Corporation'
Copyright = '(c) Microsoft Corporation. All rights reserved.'
Description = 'Module to install OpenCover and run Powershell tests to collect code coverage'
DotNetFrameworkVersion = 4.5
FormatsToProcess = @('OpenCover.Format.ps1xml')
FunctionsToExport = @('Get-CodeCoverage','Compare-CodeCoverage', 'Install-OpenCover', 'Invoke-OpenCover')
CmdletsToExport = @()
VariablesToExport = @()
AliasesToExport = @()
}

