# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'INTGallery'
$SourceLocation = 'https://dtlgalleryint.cloudapp.net'
$RegisteredINTRepo = $false
$ContosoServer = 'ContosoServer'
$FabrikamServerScript = 'Fabrikam-ServerScript'

#region Utility functions

function IsInbox { $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase) }
function IsWindows { $PSVariable = Get-Variable -Name IsWindows -ErrorAction Ignore; return (-not $PSVariable -or $PSVariable.Value) }
function IsCoreCLR { $PSVariable = Get-Variable -Name IsCoreCLR -ErrorAction Ignore; return ($PSVariable -and $PSVariable.Value) }

#endregion

#region Install locations for modules and scripts

if(IsInbox)
{
    $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath "WindowsPowerShell"
}
else
{
    $script:ProgramFilesPSPath = $PSHome
}

if(IsInbox)
{
    try
    {
        $script:MyDocumentsFolderPath = [Environment]::GetFolderPath("MyDocuments")
    }
    catch
    {
        $script:MyDocumentsFolderPath = $null
    }

    $script:MyDocumentsPSPath = if($script:MyDocumentsFolderPath)
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsFolderPath -ChildPath "WindowsPowerShell"
                                }
                                else
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $env:USERPROFILE -ChildPath "Documents\WindowsPowerShell"
                                }
}
elseif(IsWindows)
{
    $script:MyDocumentsPSPath = Microsoft.PowerShell.Management\Join-Path -Path $HOME -ChildPath 'Documents\PowerShell'
}
else
{
    $script:MyDocumentsPSPath = Microsoft.PowerShell.Management\Join-Path -Path $HOME -ChildPath '.local/share/powershell'
}

$script:ProgramFilesModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Modules'
$script:MyDocumentsModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Modules'

$script:ProgramFilesScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath 'Scripts'
$script:MyDocumentsScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath 'Scripts'

#endregion

#region Register a test repository

# Check if the PackageManagement works in the base-oS or PowerShellCore
$PSHome
$PSVersionTable
$env:PSModulePath

Get-Module -ListAvailable -Name PackageManagement, PowerShellGet
Import-Module PackageManagement -verbose
Get-PackageProvider -ListAvailable

$repo = Get-PSRepository -ErrorAction SilentlyContinue |
            Where-Object {$_.SourceLocation.StartsWith($SourceLocation, [System.StringComparison]::OrdinalIgnoreCase)}
if($repo)
{
    $RepositoryName = $repo.Name
}
else
{
    Register-PSRepository -Name $RepositoryName -SourceLocation $SourceLocation -InstallationPolicy Trusted
    $RegisteredINTRepo = $true
}

#endregion

function Remove-InstalledModules
{
    Get-InstalledModule -Name $ContosoServer -AllVersions -ErrorAction SilentlyContinue | Uninstall-Module -Force
}

Describe "PowerShellGet - Module tests" -tags "Feature" {

    BeforeEach {
        Remove-InstalledModules
    }

    It "Should find a module correctly" {
        $psgetModuleInfo = Find-Module -Name $ContosoServer -Repository $RepositoryName
        $psgetModuleInfo.Name | Should Be $ContosoServer
        $psgetModuleInfo.Repository | Should Be $RepositoryName
    }

    It "Should install a module correctly to the required location with CurrentUser scope" {
        Install-Module -Name $ContosoServer -Repository $RepositoryName -Scope CurrentUser
        $installedModuleInfo = Get-InstalledModule -Name $ContosoServer

        $installedModuleInfo | Should Not Be $null
        $installedModuleInfo.Name | Should Be $ContosoServer
        $installedModuleInfo.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should Be $true

        $module = Get-Module $ContosoServer -ListAvailable
        $module.Name | Should be $ContosoServer
        $module.ModuleBase | Should Be $installedModuleInfo.InstalledLocation
    }

    AfterAll {
        Remove-InstalledModules
    }
}

Describe "PowerShellGet - Module tests (Admin)" -tags @('Feature', 'RequireAdminOnWindows') {

    BeforeEach {
        Remove-InstalledModules
    }

    It "Should install a module correctly to the required location with default AllUsers scope" {
        Install-Module -Name $ContosoServer -Repository $RepositoryName
        $installedModuleInfo = Get-InstalledModule -Name $ContosoServer

        $installedModuleInfo | Should Not Be $null
        $installedModuleInfo.Name | Should Be $ContosoServer
        $installedModuleInfo.InstalledLocation.StartsWith($script:programFilesModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should Be $true

        $module = Get-Module $ContosoServer -ListAvailable
        $module.Name | Should be $ContosoServer
        $module.ModuleBase | Should Be $installedModuleInfo.InstalledLocation
    }

    AfterAll {
        Remove-InstalledModules
    }
}

function Remove-InstalledScripts
{
    Get-InstalledScript -Name $FabrikamServerScript -ErrorAction SilentlyContinue | Uninstall-Script -Force
}

Describe "PowerShellGet - Script tests" -tags "Feature" {

    BeforeEach {
        Remove-InstalledScripts
    }

    It "Should find a script correctly" {
        $psgetScriptInfo = Find-Script -Name $FabrikamServerScript -Repository $RepositoryName
        $psgetScriptInfo.Name | Should Be $FabrikamServerScript
        $psgetScriptInfo.Repository | Should Be $RepositoryName
    }

    It "Should install a script correctly to the required location with CurrentUser scope" {
        Install-Script -Name $FabrikamServerScript -Repository $RepositoryName -Scope CurrentUser -NoPathUpdate
        $installedScriptInfo = Get-InstalledScript -Name $FabrikamServerScript

        $installedScriptInfo | Should Not Be $null
        $installedScriptInfo.Name | Should Be $FabrikamServerScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should Be $true
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

Describe "PowerShellGet - Script tests (Admin)" -tags @('Feature', 'RequireAdminOnWindows') {

    BeforeEach {
        Remove-InstalledScripts
    }

    It "Should install a script correctly to the required location with default AllUsers scope" {
        Install-Script -Name $FabrikamServerScript -Repository $RepositoryName -NoPathUpdate
        $installedScriptInfo = Get-InstalledScript -Name $FabrikamServerScript

        $installedScriptInfo | Should Not Be $null
        $installedScriptInfo.Name | Should Be $FabrikamServerScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should Be $true
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

if($RegisteredINTRepo)
{
    Get-PSRepository -Name $RepositoryName -ErrorAction SilentlyContinue | Unregister-PSRepository
}
