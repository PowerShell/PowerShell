# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'PSGallery'
$TestModule = 'newTestModule'
$TestScript = 'TestTestScript'
$Initialized = $false

#region Install locations for modules and scripts

if($IsWindows) {
    $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath 'PowerShell'
}
else {
    $script:ProgramFilesPSPath = Split-Path -Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory('SHARED_MODULES')) -Parent
}

try
{
    $script:MyDocumentsFolderPath = [Environment]::GetFolderPath("MyDocuments")
}
catch
{
    $script:MyDocumentsFolderPath = $null
}


if($IsWindows)
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


$script:ProgramFilesModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Modules'
$script:MyDocumentsModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Modules'

$script:ProgramFilesScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Scripts'
$script:MyDocumentsScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Scripts'

$script:ProgramFilesScriptsInfoPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesScriptsPath -ChildPath 'InstalledScriptInfos'
$script:MyDocumentsScriptsInfoPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsScriptsPath -ChildPath 'InstalledScriptInfos'

Write-Host "ProgramFilesScriptsInfoPath: $script:ProgramFilesScriptsInfoPath"
Write-Host "MyDocumentsScriptsInfoPath: $script:MyDocumentsScriptsInfoPath"

if (!(Test-Path $script:ProgramFilesScriptsInfoPath)) {
    New-Item -Path $script:ProgramFilesScriptsInfoPath -ItemType Directory
}

if (!(Test-Path $script:MyDocumentsScriptsPath)) {
    New-Item -Path $script:MyDocumentsScriptsInfoPath -ItemType Directory
}

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
    Get-InstalledPSResource -Name $TestModule -Version '*' -ErrorAction SilentlyContinue | Microsoft.PowerShell.PSResourceGet\Uninstall-PSResource
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
        Write-host "installedModuleInfo installed Location is: $($installedModuleInfo.InstalledLocation)"
        $installedModuleInfo.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue

        $module = Get-Module $TestModule -ListAvailable
        $module.Name | Should -Be $TestModule
        $module.ModuleBase.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
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

        $module = Get-Module $TestModule -ListAvailable
        $module.Name | Should -Be $TestModule
        $module.ModuleBase.StartsWith($script:programFilesModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
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
        Install-PSResource -Name $TestScript -Repository $RepositoryName -Verbose
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
        #$installedScriptInfo = Get-InstalledPSResource -Name $TestScript

        <#
        $installedScriptInfo | Should -Not -BeNullOrEmpty
        $installedScriptInfo.Name | Should -Be $TestScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
        #>
    }

    AfterAll {
        Remove-InstalledScripts
    }
}
