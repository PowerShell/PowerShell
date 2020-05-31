@{
GUID="41857994-4283-4757-a932-0b0edb104913"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation."
ModuleVersion="1.0.0"
PowerShellVersion="5.0"
CmdletsToExport=@()
FunctionsToExport=@('Start-PSPackage','New-PSSignedBuildZip', 'New-PSBuildZip', 'New-MSIPatch', 'Expand-PSSignedBuild', 'Publish-NugetToMyGet', 'New-DotnetSdkContainerFxdPackage', 'New-GlobalToolNupkg', 'New-ILNugetPackage', 'Update-PSSignedBuildFolder')
RootModule="packaging.psm1"
RequiredModules = @("build")
}
