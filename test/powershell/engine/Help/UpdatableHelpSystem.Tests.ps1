# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# Test Settings:
# This is the list of PowerShell Core modules for which we test update-help
$powershellCoreModules = @(
    "CimCmdlets"
    <#
    This scenario is broken due to issue # https://github.com/PowerShell/platyPS/issues/241
    Re-enable when issue is fixed.
    "Microsoft.PowerShell.Archive"
    #>
    "Microsoft.PowerShell.Host"
    "Microsoft.PowerShell.Core"
    "Microsoft.PowerShell.Diagnostics"
    "Microsoft.PowerShell.Management"
    # "Microsoft.PowerShell.LocalAccounts" removed due to #4274
    "Microsoft.PowerShell.Security"
    "Microsoft.PowerShell.Utility"
    "Microsoft.WsMan.Management"
    "PackageManagement"
#    "PowershellGet"
    "PSReadline"
)

# The file extension for the help content on the Download Center.
# For Linux we use zip, and on Windows we use $extension.
$extension = ".zip"

if ([System.Management.Automation.Platform]::IsWindows)
{
    $extension = ".cab"
}

# This is the list of test cases -- each test case represents a PowerShell Core module.
$testCases = @{

    "CimCmdlets" = @{
        HelpFiles            = "Microsoft.Management.Infrastructure.CimCmdlets.dll-help.xml"
        HelpInfoFiles        = "CimCmdlets_fb6cc51d-c096-4b38-b78d-0fed6277096a_HelpInfo.xml"
        CompressedFiles      = "CimCmdlets_fb6cc51d-c096-4b38-b78d-0fed6277096a_en-US_HelpContent$extension"
        HelpInstallationPath = "$pshome\Modules\CimCmdlets\en-US"
    }

<#
    This scenario is broken due to issue # https://github.com/PowerShell/platyPS/issues/241
    Re-enable when issue is fixed.
    "Microsoft.PowerShell.Archive" = @{
        HelpFiles            = "Microsoft.PowerShell.Archive.psm1-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Archive_eb74e8da-9ae2-482a-a648-e96550fb8733_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Archive_eb74e8da-9ae2-482a-a648-e96550fb8733_en-US_HelpContent$extension"
        HelpInstallationPath = "$pshome\Modules\Microsoft.PowerShell.Archive\en-US"
    }
#>

    "Microsoft.PowerShell.Core" = @{
        HelpFiles            = "System.Management.Automation.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Core_00000000-0000-0000-0000-000000000000_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Core_00000000-0000-0000-0000-000000000000_en-US_HelpContent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.PowerShell.Diagnostics" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Diagnostics.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Diagnostics_ca046f10-ca64-4740-8ff9-2565dba61a4f_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Diagnostics_ca046f10-ca64-4740-8ff9-2565dba61a4f_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.PowerShell.Host" = @{
        HelpFiles            = "Microsoft.PowerShell.ConsoleHost.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Host_56d66100-99a0-4ffc-a12d-eee9a6718aef_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Host_56d66100-99a0-4ffc-a12d-eee9a6718aef_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.PowerShell.LocalAccounts" = @{
        HelpFiles            = "Microsoft.Powershell.LocalAccounts.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.LocalAccounts_8e362604-2c0b-448f-a414-a6a690a644e2_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.LocalAccounts_8e362604-2c0b-448f-a414-a6a690a644e2_en-US_HelpContent$extension"
        HelpInstallationPath = "$pshome\Modules\Microsoft.PowerShell.LocalAccounts\en-US"
    }

    "Microsoft.PowerShell.Management" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Management.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Management_eefcb906-b326-4e99-9f54-8b4bb6ef3c6d_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Management_eefcb906-b326-4e99-9f54-8b4bb6ef3c6d_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.PowerShell.Security" = @{
        HelpFiles            = "Microsoft.PowerShell.Security.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Security_a94c8c7e-9810-47c0-b8af-65089c13a35a_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Security_a94c8c7e-9810-47c0-b8af-65089c13a35a_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.PowerShell.Utility" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Utility.dll-Help.xml", "Microsoft.PowerShell.Utility-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Utility_1da87e53-152b-403e-98dc-74d7b4d63d59_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Utility_1da87e53-152b-403e-98dc-74d7b4d63d59_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "Microsoft.WSMan.Management" = @{
        HelpFiles            = "Microsoft.WSMan.Management.dll-help.xml"
        HelpInfoFiles        = "Microsoft.WsMan.Management_766204A6-330E-4263-A7AB-46C87AFC366C_HelpInfo.xml"
        CompressedFiles      = "Microsoft.WsMan.Management_766204A6-330E-4263-A7AB-46C87AFC366C_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\en-US"
    }

    "PackageManagement" = @{
        HelpFiles            = "Microsoft.PowerShell.PackageManagement.dll-help.xml"
        HelpInfoFiles        = "PackageManagement_4ae9fd46-338a-459c-8186-07f910774cb8_HelpInfo.xml"
        CompressedFiles      = "PackageManagement_4ae9fd46-338a-459c-8186-07f910774cb8_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\Modules\PackageManagement\*\en-US"
    }

    "PowershellGet" = @{
        HelpFiles            = "PSGet.psm1-help.xml"
        HelpInfoFiles        = "PowershellGet_1d73a601-4a6c-43c5-ba3f-619b18bbb404_HelpInfo.xml"
        CompressedFiles      = "PowershellGet_1d73a601-4a6c-43c5-ba3f-619b18bbb404_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\Modules\PowershellGet\*\en-US"
    }

    "PSReadline" = @{
        HelpFiles            = "Microsoft.PowerShell.PSReadline.dll-help.xml"
        HelpInfoFiles        = "PSReadline_5714753b-2afd-4492-a5fd-01d9e2cff8b5_HelpInfo.xml"
        CompressedFiles      = "PSReadline_5714753b-2afd-4492-a5fd-01d9e2cff8b5_en-US_helpcontent$extension"
        HelpInstallationPath = "$pshome\Modules\PSReadLine\en-US"
    }
}

# These are the inbox modules.
$modulesInBox = @("Microsoft.PowerShell.Core"
                  Get-Module -ListAvailable | ForEach-Object{$_.Name}
)

function GetFiles
{
    param (
        [string]$fileType = "*help.xml",
        [ValidateNotNullOrEmpty()]
        [string]$path
    )

    Get-ChildItem $path -Include $fileType -Recurse -ea SilentlyContinue | Select-Object -ExpandProperty FullName
}

function ValidateInstalledHelpContent
{
    param (
        [ValidateNotNullOrEmpty()]
        [string]$moduleName
    )

    $helpFilesInstalled = @(GetFiles -path $testCases[$moduleName].HelpInstallationPath | ForEach-Object {Split-Path $_ -Leaf})
    $expectedHelpFiles = @($testCases[$moduleName].HelpFiles)
    $helpFilesInstalled.Count | Should Be $expectedHelpFiles.Count

    foreach ($fileName in $expectedHelpFiles)
    {
        $helpFilesInstalled -contains $fileName | Should Be $true
    }
}

function RunUpdateHelpTests
{
    param (
        [string]$tag = "CI",
        [switch]$useSourcePath,
        [switch]$Pending
    )

    foreach ($moduleName in $modulesInBox)
    {
        if ($powershellCoreModules -contains $moduleName)
        {

            It "Validate Update-Help for module '$moduleName'" -Pending:$Pending {

                # If the help file is already installed, delete it.
                Get-ChildItem $testCases[$moduleName].HelpInstallationPath -Include @("*help.xml") -Recurse -ea SilentlyContinue |
                    Remove-Item -Force -ErrorAction SilentlyContinue

                if ((Get-UICulture).Name -ne "en-Us")
                {
                    if ($useSourcePath)
                    {
                        Update-Help -Module $moduleName -Force -UICulture en-US -SourcePath "$PSScriptRoot\assets"
                    }
                    else
                    {
                        Update-Help -Module $moduleName -Force -UICulture en-US
                    }
                }
                else
                {
                    if ($useSourcePath)
                    {
                        Update-Help -Module $moduleName -Force -SourcePath "$PSScriptRoot\assets"
                    }
                    else
                    {
                        Update-Help -Module $moduleName -Force
                    }
                }

                ValidateInstalledHelpContent -moduleName $moduleName
            }

            if ($tag -eq "CI")
            {
                break
            }
        }
    }
}

function RunSaveHelpTests
{
    param (
        [string]$tag = "CI",
        [switch]$Pending
    )

    foreach ($moduleName in $modulesInBox)
    {
        if ($powershellCoreModules -contains $moduleName)
        {
            try
            {
                $saveHelpFolder = Join-Path $TestDrive (Get-Random).ToString()
                New-Item  $saveHelpFolder -Force -ItemType Directory

                It "Validate Save-Help for the '$moduleName' module" -Pending:$Pending {

                    if ((Get-UICulture).Name -ne "en-Us")
                    {
                        Save-Help -Module $moduleName -Force -UICulture en-US -DestinationPath $saveHelpFolder
                    }
                    else
                    {
                        Save-Help -Module $moduleName -Force -DestinationPath $saveHelpFolder
                    }

                    ValidateSaveHelp -moduleName $moduleName -path $saveHelpFolder
                }

                if ($tag -eq "CI")
                {
                    break
                }
            }
            finally
            {
                Remove-Item $saveHelpFolder -Force -ea SilentlyContinue -Recurse
            }
        }
    }
}

function ValidateSaveHelp
{
    param (
        [string]$moduleName,
        [string]$path
    )

    $compressedFile = GetFiles -fileType "*$extension" -path $path | ForEach-Object {Split-Path $_ -Leaf}
    $expectedCompressedFile = $testCases[$moduleName].CompressedFiles
    $expectedCompressedFile | Should Be $compressedFile

    $helpInfoFile = GetFiles -fileType "*HelpInfo.xml" -path $path | ForEach-Object {Split-Path $_ -Leaf}
    $expectedHelpInfoFile = $testCases[$moduleName].HelpInfoFiles
    $expectedHelpInfoFile | Should Be $helpInfoFile
}

Describe "Validate Update-Help from the Web for one PowerShell Core module." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -tag "CI" -Pending
}

Describe "Validate Update-Help from the Web for all PowerShell Core modules." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -tag "Feature"
}

Describe "Validate Update-Help -SourcePath for one PowerShell Core module." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -tag "CI" -useSourcePath
}

Describe "Validate Update-Help -SourcePath for all PowerShell Core modules." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -tag "Feature" -useSourcePath
}

Describe "Validate 'Save-Help -DestinationPath for one PowerShell Core modules." -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }
    RunSaveHelpTests -tag "CI" -Pending
}

Describe "Validate 'Save-Help -DestinationPath for all PowerShell Core modules." -Tags @('Feature', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }
    RunSaveHelpTests -tag "Feature"
}
