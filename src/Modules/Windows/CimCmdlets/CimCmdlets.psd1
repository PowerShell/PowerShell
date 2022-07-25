@{
GUID="{Fb6cc51d-c096-4b38-b78d-0fed6277096a}"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation."
ModuleVersion="7.0.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion="3.0"
RootModule="Microsoft.Management.Infrastructure.CimCmdlets"
RequiredAssemblies="Microsoft.Management.Infrastructure.CimCmdlets.dll","Microsoft.Management.Infrastructure.Dll"
FunctionsToExport = @()
CmdletsToExport= "Get-CimAssociatedInstance", "Get-CimClass", "Get-CimInstance", "Get-CimSession", "Invoke-CimMethod",
    "New-CimInstance","New-CimSession","New-CimSessionOption","Register-CimIndicationEvent","Remove-CimInstance",
    "Remove-CimSession","Set-CimInstance",
    "Export-BinaryMiLog","Import-BinaryMiLog"
AliasesToExport = "gcim","scim","ncim", "rcim","icim","gcai","rcie","ncms","rcms","gcms","ncso","gcls"
HelpInfoUri="https://aka.ms/powershell73-help"
}
