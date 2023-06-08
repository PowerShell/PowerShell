# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'PSGallery'
$SourceLocation = 'https://www.powershellgallery.com'
$TestModule = 'newTestModule'
$TestScript = 'TestTestScript'
$Initialized = $false

#region Utility functions

function IsInbox { $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase) }
function IsWindows { $PSVariable = Get-Variable -Name IsWindows -ErrorAction Ignore; return (-not $PSVariable -or $PSVariable.Value) }
function IsCoreCLR { $PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core' }

#endregion

#region Install locations for modules and scripts

if(IsInbox)
{
    $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath "WindowsPowerShell"
}
elseif(IsCoreCLR) {
    if(IsWindows) {
        $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath 'PowerShell'
    }
    else {
        $script:ProgramFilesPSPath = Split-Path -Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory('SHARED_MODULES')) -Parent
    }
}

try
{
    $script:MyDocumentsFolderPath = [Environment]::GetFolderPath("MyDocuments")
}
catch
{
    $script:MyDocumentsFolderPath = $null
}

if(IsInbox)
{
    $script:MyDocumentsPSPath = if($script:MyDocumentsFolderPath)
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsFolderPath -ChildPath "WindowsPowerShell"
                                }
                                else
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $env:USERPROFILE -ChildPath "Documents\WindowsPowerShell"
                                }
}
elseif(IsCoreCLR) {
    if(IsWindows)
    {
        $script:MyDocumentsPSPath = if($script:MyDocumentsFolderPath)
        {
            Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsFolderPath -ChildPath 'PowerShell'
        }
        else
        {
            Microsoft.PowerShell.Management\Join-Path -Path $HOME -ChildPath "Documents\PowerShell"
        }
    }
    else
    {
        $script:MyDocumentsPSPath = Split-Path -Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory('USER_MODULES')) -Parent
    }
}

$script:ProgramFilesModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Modules'
$script:MyDocumentsModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Modules'

$script:ProgramFilesScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Scripts'
$script:MyDocumentsScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Scripts'

#endregion

#region Register a test repository

function Initialize
{
    $repo = Get-PSResourceRepository $RepositoryName -ErrorAction SilentlyContinue
    if($repo)
    {
        Set-PSResourceRepository -Name $repo.Name -Trusted
    }
    else
    {
        Register-PSResourceRepository -PSGallery -Trusted
    }
}

#endregion

function Remove-InstalledModules
{
    Get-InstalledPSResource -Name $TestModule -Version '*' -ErrorAction SilentlyContinue | PSResourceGet\Uninstall-PSResource
}

Describe "PSResourceGet - Module tests" -tags "Feature" {

    BeforeAll {
        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }
    }

    BeforeEach {
        Remove-InstalledModules
    }

    It "Should find a module correctly" {
        $psgetModuleInfo = Find-PSResource -Name $TestModule -Repository $RepositoryName
        $psgetModuleInfo.Name | Should -Be $TestModule
        $psgetModuleInfo.Repository | Should -Be $RepositoryName
    }

    It "Should install a module correctly to the required location with default CurrentUser scope" {
        Install-PSResource -Name $TestModule -Repository $RepositoryName
        $installedModuleInfo = Get-InstalledPSResource -Name $TestModule

        $installedModuleInfo | Should -Not -BeNullOrEmpty
        $installedModuleInfo.Name | Should -Be $TestModule
        $installedModuleInfo.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue

        $module = Get-Module $TestModule -ListAvailable
        $module.Name | Should -Be $TestModule
        $module.ModuleBase | Should -Be $installedModuleInfo.InstalledLocation
    }

    AfterAll {
        Remove-InstalledModules
    }
}

Describe "PSResourceGet - Module tests (Admin)" -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {

    BeforeAll {
        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }
    }

    BeforeEach {
        Remove-InstalledModules
    }

    It "Should install a module correctly to the required location with AllUsers scope" {
        Install-PSResource -Name $TestModule -Repository $RepositoryName -Scope AllUsers
        $installedModuleInfo = Get-InstalledPSResource -Name $TestModule

        $installedModuleInfo | Should -Not -BeNullOrEmpty
        $installedModuleInfo.Name | Should -Be $TestModule
        $installedModuleInfo.InstalledLocation.StartsWith($script:programFilesModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue

        $module = Get-Module $TestModule -ListAvailable
        $module.Name | Should -Be $TestModule
        $module.ModuleBase | Should -Be $installedModuleInfo.InstalledLocation
    }

    AfterAll {
        Remove-InstalledModules
    }
}

function Remove-InstalledScripts
{
    Get-InstalledPSResource -Name $TestScript -ErrorAction SilentlyContinue | Uninstall-PSResource
}

Describe "PSResourceGet - Script tests" -tags "Feature" {

    BeforeAll {
        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }
    }

    BeforeEach {
        Remove-InstalledScripts
    }

    It "Should find a script correctly" {
        $psgetScriptInfo = Find-PSResource -Name $TestScript -Repository $RepositoryName
        $psgetScriptInfo.Name | Should -Be $TestScript
        $psgetScriptInfo.Repository | Should -Be $RepositoryName
    }

    It "Should install a script correctly to the required location with default CurrentUser scope" {
        Install-PSResource -Name $TestScript -Repository $RepositoryName
        $installedScriptInfo = Get-InstalledPSResource -Name $TestScript

        $installedScriptInfo | Should -Not -BeNullOrEmpty
        $installedScriptInfo.Name | Should -Be $TestScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

Describe "PSResourceGet - Script tests (Admin)" -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {

    BeforeAll {
        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }
    }

    BeforeEach {
        Remove-InstalledScripts
    }

    It "Should install a script correctly to the required location with AllUsers scope" {
        Install-PSResource -Name $TestScript -Repository $RepositoryName -Scope AllUsers
        $installedScriptInfo = Get-InstalledPSResource -Name $TestScript

        $installedScriptInfo | Should -Not -BeNullOrEmpty
        $installedScriptInfo.Name | Should -Be $TestScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
    }

    AfterAll {
        Remove-InstalledScripts
    }
}
