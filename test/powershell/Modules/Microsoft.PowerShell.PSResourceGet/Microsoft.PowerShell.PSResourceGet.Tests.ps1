# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'PSGallery'
$ACRRepositoryName = "ACRRepo"
$ACRRepoUri = "https://psresourcegettest.azurecr.io/"
$TestModule = 'newTestModule'
$TestScript = 'TestTestScript'
$ACRTestModule = 'newTestMod'

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

    $acrTests = $env:ACRTESTS -eq 'true'

    if ($acrTests)
    {
        if ($null -eq $env:TENANTID)
        {
            Write-Error "The TENANTID environment variable must be set for ACR tests."
            return
        }

        $psCredInfo = New-Object Microsoft.PowerShell.PSResourceGet.UtilClasses.PSCredentialInfo ("SecretStore", "$env:TENANTID")
        Register-PSResourceRepository -Name $ACRRepositoryName -ApiVersion 'ContainerRegistry' -Uri $ACRRepoUri -CredentialInfo $psCredInfo -Verbose -Trusted -Force
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

        if (!$IsMacOS) {
            $installedModuleInfo | Should -Not -BeNullOrEmpty
            $installedModuleInfo.Name | Should -Be $TestModule
            $installedModuleInfo.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue

            $module = Get-Module $TestModule -ListAvailable
            $module.Name | Should -Be $TestModule
            $module.ModuleBase.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
        }
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

        if (!$IsMacOS)
        {
            $installedScriptInfo | Should -Not -BeNullOrEmpty
            $installedScriptInfo.Name | Should -Be $TestScript
            $installedScriptInfo.InstalledLocation.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
        }
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
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

Describe "PSResourceGet - ACR tests" -tags "Feature" {

    BeforeAll {
        if ($env:ACRTESTS -ne 'true') {
            return
        }

        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }
    }

    BeforeEach {
        if ($env:ACRTESTS -ne 'true') {
            return
        }

        Remove-InstalledModules
    }

    It "Should find a module correctly" {
        $isSkipped = $env:ACRTESTS -ne 'true'

        Write-Verbose -Verbose "Test - Skipping = $isSkipped"

        if ($isSkipped) {
            Set-ItResult -Skipped -Because "The tests require the ACRTESTS environment variable to be set to 'true' for ACR authentication."
        }

        $psgetModuleInfo = Find-PSResource -Name $ACRTestModule -Repository $ACRRepositoryName
        $psgetModuleInfo.Name | Should -Be $ACRTestModule
        $psgetModuleInfo.Repository | Should -Be $ACRRepositoryName
    }

    It "Should install a module correctly to the required location with default CurrentUser scope" {
        $isSkipped = $env:ACRTESTS -ne 'true'

        if ($isSkipped) {
            Set-ItResult -Skipped:$isSkipped -Because "The tests require the ACRTESTS environment variable to be set to 'true' for ACR authentication."
        }

        Install-PSResource -Name $ACRTestModule -Repository $ACRRepositoryName
        $installedModuleInfo = Get-InstalledPSResource -Name $ACRTestModule

        if (!$IsMacOS) {
            $installedModuleInfo | Should -Not -BeNullOrEmpty
            $installedModuleInfo.Name | Should -Be $ACRTestModule
            $installedModuleInfo.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue

            $module = Get-Module $ACRTestModule -ListAvailable
            $module.Name | Should -Be $ACRTestModule
            $module.ModuleBase.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
        }
    }

    AfterAll {
        if ($env:ACRTESTS -ne 'true') {
            return
        }

        Remove-InstalledModules
    }
}
