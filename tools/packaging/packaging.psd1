@{
GUID="41857994-4283-4757-a932-0b0edb104913"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation. All rights reserved."
ModuleVersion="1.0.0"
PowerShellVersion="5.0"
CmdletsToExport=@()
FunctionsToExport=@(
    'Expand-PSSignedBuild'
    'New-DotnetSdkContainerFxdPackage'
    'New-MSIPatch'
    'New-PSSignedBuildZip'
    'New-UnifiedNugetPackage'
    'Publish-NugetToMyGet'
    'Start-PSPackage'
)
RootModule="packaging.psm1"
RequiredModules = @("build")
}
