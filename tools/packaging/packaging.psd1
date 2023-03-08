@{
    GUID              = "41857994-4283-4757-a932-0b0edb104913"
    Author            = "PowerShell"
    CompanyName       = "Microsoft Corporation"
    Copyright         = "Copyright (c) Microsoft Corporation."
    ModuleVersion     = "1.0.0"
    PowerShellVersion = "5.0"
    CmdletsToExport   = @()
    FunctionsToExport = @(
        'Compress-ExePackageEngine'
        'Expand-ExePackageEngine'
        'Expand-PSSignedBuild'
        'Invoke-AzDevOpsLinuxPackageBuild'
        'Invoke-AzDevOpsLinuxPackageCreation'
        'New-DotnetSdkContainerFxdPackage'
        'New-ExePackage'
        'New-GlobalToolNupkg'
        'New-ILNugetPackageSource'
        'New-ILNugetPackageFromSource'
        'New-PSBuildZip'
        'New-PSSignedBuildZip'
        'Publish-NugetToMyGet'
        'Start-PSPackage'
        'Test-PackageManifest'
        'Update-PSSignedBuildFolder'
        'Test-Bom'
    )
    RootModule        = "packaging.psm1"
    RequiredModules   = @("build")
}
