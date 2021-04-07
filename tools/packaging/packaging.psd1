@{
    GUID              = "41857994-4283-4757-a932-0b0edb104913"
    Author            = "PowerShell"
    CompanyName       = "Microsoft Corporation"
    Copyright         = "Copyright (c) Microsoft Corporation."
    ModuleVersion     = "1.0.0"
    PowerShellVersion = "5.0"
    CmdletsToExport   = @()
    FunctionsToExport = @(
        'Expand-ExePackageEngine'
        'Expand-PSSignedBuild'
        'Compress-ExePackageEngine'
        'New-DotnetSdkContainerFxdPackage'
        'New-ExePackage'
        'New-GlobalToolNupkg'
        'New-ILNugetPackage'
        'New-MSIPatch'
        'New-PSBuildZip'
        'New-PSSignedBuildZip'
        'Publish-NugetToMyGet'
        'Start-PSPackage'
        'Update-PSSignedBuildFolder'
    )
    RootModule        = "packaging.psm1"
    RequiredModules   = @("build")
}
