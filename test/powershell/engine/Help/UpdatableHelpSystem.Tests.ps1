# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

# Test Settings:
# This is the list of PowerShell modules for which we test update-help
[string[]] $powershellCoreModules = @(
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
)

# The file extension for the help content on the Download Center.
# For Linux we use zip, and on Windows we use $extension.
[string] $extension = ".zip"

if ([System.Management.Automation.Platform]::IsWindows)
{
    $extension = ".cab"
}

[string] $userHelpRoot = $null

if([System.Management.Automation.Platform]::IsWindows)
{
    # To the reader: the explicit parameter names below are required by a brainless code checker.
    $userHelpRoot = Join-Path -Path:$HOME -ChildPath:Documents -AdditionalChildPath:PowerShell, Help
}
else
{
    [string] $userModulesRoot = [System.Management.Automation.Platform]::SelectProductNameForDirectory([System.Management.Automation.Platform+XDG_Type]::USER_MODULES)
    $userHelpRoot = Join-Path -Path:$userModulesRoot -ChildPath:.. -AdditionalChildPath:Help
}

# default values for system modules
[string] $myUICulture = 'en-US'   
[string] $HelpInstallationPath = Join-Path $PSHOME $myUICulture
[string] $HelpInstallationPathHome = Join-Path $userHelpRoot $myUICulture

# This is the list of test cases -- each test case represents a PowerShell module.
[hashtable] $testCases = @{

    "CimCmdlets" = @{
        HelpFiles            = "Microsoft.Management.Infrastructure.CimCmdlets.dll-help.xml"
        HelpInfoFiles        = "CimCmdlets_fb6cc51d-c096-4b38-b78d-0fed6277096a_HelpInfo.xml"
        CompressedFiles      = "CimCmdlets_fb6cc51d-c096-4b38-b78d-0fed6277096a_en-US_HelpContent$extension"
        HelpInstallationPath = Join-Path -Path:$PSHOME -ChildPath:Modules -AdditionalChildPath:CimCmdlets, $myUICulture
        HelpInstallationPathHome = Join-Path -Path:$userHelpRoot -ChildPath:CimCmdlets -AdditionalChildPath:$myUICulture
    }

    "Microsoft.PowerShell.Archive" = @{
        HelpFiles            = "Microsoft.PowerShell.Archive-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Archive_eb74e8da-9ae2-482a-a648-e96550fb8733_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Archive_eb74e8da-9ae2-482a-a648-e96550fb8733_en-US_HelpContent$extension"
        HelpInstallationPath = Join-Path -Path:$PSHOME -ChildPath:Modules -AdditionalChildPath:Microsoft.PowerShell.Archive, $myUICulture
    }

    "Microsoft.PowerShell.Core" = @{
        HelpFiles            = "System.Management.Automation.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Core_00000000-0000-0000-0000-000000000000_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Core_00000000-0000-0000-0000-000000000000_en-US_HelpContent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.PowerShell.Diagnostics" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Diagnostics.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Diagnostics_ca046f10-ca64-4740-8ff9-2565dba61a4f_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Diagnostics_ca046f10-ca64-4740-8ff9-2565dba61a4f_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.PowerShell.Host" = @{
        HelpFiles            = "Microsoft.PowerShell.ConsoleHost.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Host_56d66100-99a0-4ffc-a12d-eee9a6718aef_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Host_56d66100-99a0-4ffc-a12d-eee9a6718aef_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.PowerShell.LocalAccounts" = @{
        HelpFiles            = "Microsoft.Powershell.LocalAccounts.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.LocalAccounts_8e362604-2c0b-448f-a414-a6a690a644e2_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.LocalAccounts_8e362604-2c0b-448f-a414-a6a690a644e2_en-US_HelpContent$extension"
        HelpInstallationPath = Join-Path -Path:$PSHOME Modules Microsoft.PowerShell.LocalAccounts $myUICulture
        HelpInstallationPathHome = Join-Path -Path:$userHelpRoot -ChildPath:Microsoft.PowerShell.LocalAccounts -AdditionalChildPath:$myUICulture
    }

    "Microsoft.PowerShell.Management" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Management.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Management_eefcb906-b326-4e99-9f54-8b4bb6ef3c6d_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Management_eefcb906-b326-4e99-9f54-8b4bb6ef3c6d_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.PowerShell.Security" = @{
        HelpFiles            = "Microsoft.PowerShell.Security.dll-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Security_a94c8c7e-9810-47c0-b8af-65089c13a35a_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Security_a94c8c7e-9810-47c0-b8af-65089c13a35a_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.PowerShell.Utility" = @{
        HelpFiles            = "Microsoft.PowerShell.Commands.Utility.dll-Help.xml", "Microsoft.PowerShell.Utility-help.xml"
        HelpInfoFiles        = "Microsoft.PowerShell.Utility_1da87e53-152b-403e-98dc-74d7b4d63d59_HelpInfo.xml"
        CompressedFiles      = "Microsoft.PowerShell.Utility_1da87e53-152b-403e-98dc-74d7b4d63d59_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "Microsoft.WSMan.Management" = @{
        HelpFiles            = "Microsoft.WSMan.Management.dll-help.xml"
        HelpInfoFiles        = "Microsoft.WsMan.Management_766204A6-330E-4263-A7AB-46C87AFC366C_HelpInfo.xml"
        CompressedFiles      = "Microsoft.WsMan.Management_766204A6-330E-4263-A7AB-46C87AFC366C_en-US_helpcontent$extension"
        HelpInstallationPath = $HelpInstallationPath
        HelpInstallationPathHome = $HelpInstallationPathHome
    }

    "PackageManagement" = @{
        HelpFiles            = "Microsoft.PowerShell.PackageManagement.dll-help.xml"
        HelpInfoFiles        = "PackageManagement_4ae9fd46-338a-459c-8186-07f910774cb8_HelpInfo.xml"
        CompressedFiles      = "PackageManagement_4ae9fd46-338a-459c-8186-07f910774cb8_en-US_helpcontent$extension"
        HelpInstallationPath = Join-Path -Path:$PSHOME -ChildPath:Modules -AdditionalChildPath:PackageManagement, $myUICulture
        HelpInstallationPathHome = Join-Path -Path:$userHelpRoot -ChildPath:PackageManagement -AdditionalChildPath:$myUICulture
    }

    "PowershellGet" = @{
        HelpFiles            = "PSGet.psm1-help.xml"
        HelpInfoFiles        = "PowershellGet_1d73a601-4a6c-43c5-ba3f-619b18bbb404_HelpInfo.xml"
        CompressedFiles      = "PowershellGet_1d73a601-4a6c-43c5-ba3f-619b18bbb404_en-US_helpcontent$extension"
        HelpInstallationPath = Join-Path -Path:$PSHOME -ChildPath:Modules -AdditionalChildPath:PowershellGet, $myUICulture
        HelpInstallationPathHome = Join-Path -Path:$userHelpRoot -ChildPath:PackageManagement -AdditionalChildPath:$myUICulture
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

    Get-ChildItem $path -Include $fileType -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
}

function ValidateInstalledHelpContent
{
    param (
        [ValidateNotNullOrEmpty()]
        [string]$moduleName,
        [switch]$UserScope
    )

    [string] $pathProperty = $(if ($UserScope) { 'HelpInstallationPathHome' } else { 'HelpInstallationPath' })
    [System.IO.FileInfo[]] $helpFilesInstalled = Get-ChildItem -Path:($testCases[$moduleName][$pathProperty]) -Filter:*help.xml -Recurse

    [string[]] $expectedHelpFiles = $testCases[$moduleName].HelpFiles
    $helpFilesInstalled.Count | Should -Be ($expectedHelpFiles.Count)

    foreach ($fileName in $expectedHelpFiles)
    {
        [string[]] $helpFilesInstalled.Name -eq $fileName | Should -Be $fileName
    }
}

function RunUpdateHelpTests
{
    param (
        [string]$tag = "CI",
        [switch]$useSourcePath,
        [switch]$userscope
    )

    foreach ($moduleName in $modulesInBox)
    {
        if ($powershellCoreModules -contains $moduleName)
        {

            [string] $moduleHelpPath = $null
            [string] $updateScope = $null
            if ($userscope)
            {
                $moduleHelpPath = $testCases[$moduleName]['HelpInstallationPathHome']
                $updateScope = 'CurrentUser'
            }
            else
            {
                $moduleHelpPath = $testCases[$moduleName]['HelpInstallationPath']
                $updateScope = 'AllUsers'
            }

            It ('Validate Update-Help for module ''{0}'' in {1}' -F $moduleName, [PSCustomObject] $updateScope) -Skip:(!(Test-CanWriteToPsHome) -and $userscope -eq $false) {

                # Delete the whole help directory
                Remove-Item ($moduleHelpPath) -Recurse
 
                [hashtable] $UICultureParam = $(if ((Get-UICulture).Name -ne $myUICulture) { @{ UICulture = $myUICulture } } else { @{} })
                [hashtable] $sourcePathParam = $(if ($useSourcePath) { @{ SourcePath = Join-Path $PSScriptRoot assets } } else { @{} })
                Update-Help -Module:$moduleName -Force @UICultureParam @sourcePathParam -Scope:$updateScope

                [hashtable] $userScopeParam = $(if ($userscope) { @{ UserScope = $true } } else { @{} })
                ValidateInstalledHelpContent -moduleName:$moduleName @userScopeParam

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
        [string]$tag = "CI"
    )

    foreach ($moduleName in $modulesInBox)
    {
        if ($powershellCoreModules -contains $moduleName)
        {
            try
            {
                $saveHelpFolder = Join-Path $TestDrive (Get-Random).ToString()
                New-Item  $saveHelpFolder -Force -ItemType Directory > $null

                ## Save help has intermittent connectivity issues for downloading PackageManagement help content.
                ## Hence the test has been marked as Pending.
                if($moduleName -eq 'PackageManagement')
                {
                    $pending = $true
                }

                It "Validate Save-Help for the '$moduleName' module" -Pending:$pending {

                    if ((Get-UICulture).Name -ne $myUICulture)
                    {
                        Save-Help -Module $moduleName -Force -UICulture $myUICulture -DestinationPath $saveHelpFolder
                    }
                    else
                    {
                        Save-Help -Module $moduleName -Force -DestinationPath $saveHelpFolder
                    }

                    ValidateSaveHelp -moduleName $moduleName -path $saveHelpFolder
                }

                ## Reset pending state.
                if($pending)
                {
                    $pending = $false
                }

                if ($tag -eq "CI")
                {
                    break
                }
            }
            finally
            {
                Remove-Item $saveHelpFolder -Force -ErrorAction SilentlyContinue -Recurse
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
    $expectedCompressedFile | Should -Not -BeNullOrEmpty -Because "Test data (expectedCompressedFile) should never be null"
    $compressedFile | Should -Be $expectedCompressedFile -Because "Save-Help for $module should download '$expectedCompressedFile'"

    $helpInfoFile = GetFiles -fileType "*HelpInfo.xml" -path $path | ForEach-Object {Split-Path $_ -Leaf}
    $expectedHelpInfoFile = $testCases[$moduleName].HelpInfoFiles
    $expectedHelpInfoFile | Should -Not -BeNullOrEmpty -Because "Test data (expectedHelpInfoFile) should never be null"
    $helpInfoFile | Should -Be $expectedHelpInfoFile -Because "Save-Help for $module should download '$expectedHelpInfoFile'"
}

Describe "Validate Update-Help from the Web for one PowerShell module." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "CI"
}

Describe "Validate Update-Help from the Web for one PowerShell module for user scope." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "CI" -UserScope
}

Describe "Validate Update-Help from the Web for all PowerShell modules." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "Feature"
}

Describe "Validate Update-Help from the Web for all PowerShell modules for user scope." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "Feature" -UserScope
}

Describe "Validate Update-Help -SourcePath for one PowerShell module." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "CI" -useSourcePath
}

Describe "Validate Update-Help -SourcePath for one PowerShell module for user scope." -Tags @('CI', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "CI" -useSourcePath -UserScope
}

Describe "Validate Update-Help -SourcePath for all PowerShell modules." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "Feature" -useSourcePath
}

Describe "Validate Update-Help -SourcePath for all PowerShell modules for user scope." -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunUpdateHelpTests -Tag "Feature" -useSourcePath -UserScope
}

Describe "Validate 'Update-Help' shows 'HelpCultureNotSupported' when thrown" -Tags @('Feature') {
    BeforeAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ThrowHelpCultureNotSupported', $true)
    }
    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ThrowHelpCultureNotSupported', $false)
    }

    It 'Shows error if help culture does not match: <name>' -TestCases @(
        @{ 'name' = 'implicit culture'; 'culture' = $null }
        @{ 'name' = 'explicit culture en-GB'; 'culture' = 'en-GB' }
        @{ 'name' = 'explicit culture de-DE'; 'culture' = 'de-DE' }
    ) {
        param ($name, $culture)

        # if running in Linux as an invariant culture => force Spanish
        if ($IsLinux && $culture -eq $null && (Get-Culture).LCID -eq 127 ){
            $culture = 'es-ES'
        }

        # Cannot pass null, have to splat to skip argument entirely
        $cultureArg = $culture ? @{ 'UICulture' = $culture } : @{}
        $cultureUsed = $culture ?? (Get-Culture)

        $ErrorVariable = $null
        $VerboseOutput = New-TemporaryFile
        Update-Help @cultureArg -ErrorVariable ErrorVariable -ErrorAction SilentlyContinue -Verbose 4>$VerboseOutput
        $ErrorVariable | Should -Match "No UI culture was found that matches the following pattern: ${cultureUsed}"
        if (-not $culture) {
            Get-Content -Raw $VerboseOutput | Should -Match 'Postponing error and trying fallback cultures'
        }
    }
}

Describe "Validate 'Save-Help -DestinationPath for one PowerShell modules." -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }
    RunSaveHelpTests -Tag "CI"
}

Describe "Validate 'Save-Help -DestinationPath for all PowerShell modules." -Tags @('Feature', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }
    RunSaveHelpTests -Tag "Feature"
}
