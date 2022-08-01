@{
GUID = "A94C8C7E-9810-47C0-B8AF-65089C13A35A"
Author = "PowerShell"
CompanyName = "Microsoft Corporation"
Copyright = "Copyright (c) Microsoft Corporation."
ModuleVersion = "7.0.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion = "3.0"
FunctionsToExport = @()
CmdletsToExport = "Get-Acl", "Set-Acl", "Get-PfxCertificate", "Get-Credential", "Get-ExecutionPolicy", "Set-ExecutionPolicy", "Get-AuthenticodeSignature", "Set-AuthenticodeSignature", "ConvertFrom-SecureString", "ConvertTo-SecureString", "Get-CmsMessage", "Unprotect-CmsMessage", "Protect-CmsMessage" , "New-FileCatalog" , "Test-FileCatalog"
AliasesToExport = @()
NestedModules = "Microsoft.PowerShell.Security.dll"
# 'Security.types.ps1xml' refers to types from 'Microsoft.PowerShell.Security.dll' and thus requiring to load the assembly before processing the type file.
# We declare 'Microsoft.PowerShell.Security.dll' in 'RequiredAssemblies' so as to make sure it's loaded before the type file processing.
RequiredAssemblies = "Microsoft.PowerShell.Security.dll"
TypesToProcess = "Security.types.ps1xml"
HelpInfoURI = 'https://aka.ms/powershell73-help'
}
