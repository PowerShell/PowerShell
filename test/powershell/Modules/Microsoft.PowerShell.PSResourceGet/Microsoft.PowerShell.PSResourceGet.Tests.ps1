# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'PSGallery'
$ACRRepositoryName = "ACRRepo"
$ACRRepoUri = "https://psresourcegettest.azurecr.io/"
$LocalRepoName = 'LocalRepo'
$LocalRepoUri = '.\temp'
$TestModule = 'newTestModule'
$TestScript = 'TestTestScript'
$ACRTestModule = 'newTestMod'
$TempTestPublish = 'tempTestPublish'
$TestPublishModule = 'TestPublishModule'
$TestPublishModuleNupkgName = "$TestPublishModule.nupkg"
$testPublishModuleNupkgPath = Join-Path -Path $TempTestPublish -ChildPath $TestPublishModuleNupkgName
$TestPublishModuleLocalPath =  Microsoft.PowerShell.Management\Join-Path -Path $TempTestPublish -ChildPath $TestPublishModule
$TestPublishScript = 'TestPublishScript'
$TestPublishScriptPath = Join-Path -Path $TestPublishScript -ChildPath "$TestPublishScript.ps1"
$TestPublishScriptNupkgName = "$TestPublishScript.nupkg"
$TestPublishScriptNupkgPath = Join-Path -Path $TempTestPublish -ChildPath $TestPublishScriptNupkgName


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

function Register-LocalRepo
{
    if (!(Test-Path $LocalRepoUri)) {
        New-Item -Path $LocalRepoUri -ItemType Directory
    }

    Register-PSResourceRepository -Name $LocalRepoName -Uri $LocalRepoUri -Trusted -Force
}

#endregion

function Remove-InstalledModules
{
    Get-InstalledPSResource -Name $TestModule -Version '*' -ErrorAction SilentlyContinue | Microsoft.PowerShell.PSResourceGet\Uninstall-PSResource
}

function New-TestPackages
{
    Write-Output "New-TestPackages"
    if (!(Test-Path $TestPublishModuleLocalPath)) {
        New-Item $TestPublishModuleLocalPath -ItemType Directory
    }

    Write-Output "Generating new test module manifest"
    New-ModuleManifest (Join-Path $TestPublishModuleLocalPath -ChildPath "$TestPublishModule.psd1") -Description "Test module for PowerShell CI"

    Write-Output "Generating new test script file"
    New-ScriptFileInfo -Path $TestPublishScriptPath -Description "Test script for PowerShell CI"
}

Describe "PSResourceGet - Module tests" -tags "Feature" {

    BeforeAll {
        if ($script:Initialized -eq $false) {
            Initialize
            $script:Initialized = $true
        }

        Write-Verbose -Verbose "Registering local repository"
        Register-LocalRepo
        New-TestPackages
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

    It "Should publish a module" {
        Publish-PSResource -Path $TestPublishModuleLocalPath -Repository $LocalRepoName -Verbose -Debug

        $foundModuleInfo = Find-PSResource $TestPublishModule -Repository $LocalRepoName
        $foundModuleInfo | Should -Not -BeNullOrEmpty
        $foundModuleInfo.Count | Should -Be 1
        $foundModuleInfo.Name | Should -Be $TestPublishModule
    }

    It "Should compress a module into a .nupkg" {
        Compress-PSResource -Path $TestPublishModuleLocalPath -DestinationPath $testPublishModuleNupkgPath

        $modulePublished = Get-ChildItem $TestPublishModuleNupkgPath
        $modulePublished | Should -Not -BeNullOrEmpty
        $modulePublished.Name | Should -Be $nupkgName
    }

    It "Should publish compressed .nupkg" {
        Compress-PSResource -Path $TestPublishModuleLocalPath -DestinationPath $testPublishModuleNupkgPath

        Publish-PSResource -NupkgPath $testPublishModuleNupkgPath -Repository $LocalRepoName -NupkgPath

        $foundModuleInfo = Find-PSResource $TestPublishModule -Repository $LocalRepoName
        $foundModuleInfo | Should -Not -BeNullOrEmpty
        $foundModuleInfo.Count | Should -Be 1
        $foundModuleInfo.Name | Should -Be $TestPublishModule
    }

    AfterEach {
        if (Test-Path $LocalRepoUri)
        {
            Remove-Item -Path $LocalRepoUri -Recurse -Force
        }
        if (Test-Path $TempTestPublish)
        {
            Remove-Item -Path $TempTestPublish -Recurse -Force
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

        Register-LocalRepo
        New-TestPackages
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

    It "Should publish a script" {
        Publish-PSResource -Path $TestPublishScriptPath -Repository $LocalRepoName

        $foundScriptInfo = Find-PSResource $TestPublishScript -Repository $LocalRepoName
        $foundScriptInfo | Should -Not -BeNullOrEmpty
        $foundScriptInfo.Count | Should -Be 1
        $foundScriptInfo.Name | Should -Be $TestPublishScript
    }

    It "Should compress a script into a .nupkg" {
        Compress-PSResource -Path $TestPublishScriptPath -DestinationPath $TestPublishScriptNupkgPath

        $scriptPublished = Get-ChildItem $TestPublishScriptNupkgPath
        $scriptPublished | Should -Not -BeNullOrEmpty
        $scriptPublished.Name | Should -Be $TestPublishScriptNupkgName
    }

    It "Should publish compressed script .nupkg" {
        Compress-PSResource -Path $TestPublishScriptPath -DestinationPath $TestPublishScriptNupkgPath

        Publish-PSResource -NupkgPath $TestPublishScriptNupkgPath -Repository $LocalRepoName -NupkgPath

        $foundScriptInfo = Find-PSResource $TestPublishScript -Repository $LocalRepoName
        $foundScriptInfo | Should -Not -BeNullOrEmpty
        $foundScriptInfo.Count | Should -Be 1
        $foundScriptInfo.Name | Should -Be $TestPublishScript
    }

    AfterEach {
        if (Test-Path $LocalRepoUri)
        {
            Remove-Item -Path $LocalRepoUri -Recurse -Force
        }

        if (Test-Path $TempTestPublish)
        {
            Remove-Item -Path $TempTestPublish -Recurse -Force
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
