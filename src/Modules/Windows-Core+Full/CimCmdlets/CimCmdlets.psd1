@{
GUID="{Fb6cc51d-c096-4b38-b78d-0fed6277096a}"
Author="Microsoft Corporation"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation. All rights reserved."
ModuleVersion="1.0.0.0"
PowerShellVersion="3.0"
CLRVersion="4.0"
RootModule="Microsoft.Management.Infrastructure.CimCmdlets"
RequiredAssemblies="Microsoft.Management.Infrastructure.CimCmdlets.dll","Microsoft.Management.Infrastructure.Dll"
CmdletsToExport= "Get-CimAssociatedInstance", "Get-CimClass", "Get-CimInstance", "Get-CimSession", "Invoke-CimMethod",
    "New-CimInstance","New-CimSession","New-CimSessionOption","Register-CimIndicationEvent","Remove-CimInstance",
    "Remove-CimSession","Set-CimInstance",
    "Export-BinaryMiLog","Import-BinaryMiLog"
AliasesToExport = "gcim","scim","ncim", "rcim","icim","gcai","rcie","ncms","rcms","gcms","ncso","gcls"
FunctionsToExport = @()
HelpInfoUri="https://go.microsoft.com/fwlink/?linkid=855946"
}
