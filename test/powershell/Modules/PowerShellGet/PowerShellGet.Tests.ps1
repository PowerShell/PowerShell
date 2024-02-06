# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# no progress output during these tests
$ProgressPreference = "SilentlyContinue"

$RepositoryName = 'PSGallery'
$PSGalleryURL = 'https://www.powershellgallery.com'
$SourceLocation = 'https://www.powershellgallery.com/api/v2'
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
    # Cleaned up commands whose output to console by deleting or piping to Out-Null
    $repo = Get-PSRepository -ErrorAction SilentlyContinue |
                Where-Object {$_.Uri.AbsoluteUri.StartsWith($PSGalleryURL, [System.StringComparison]::OrdinalIgnoreCase)}
    if($repo)
    {
        $script:RepositoryName = $repo.Name
    }
    else
    {
        Register-PSRepository -Name $RepositoryName -SourceLocation $SourceLocation -InstallationPolicy Trusted
    }
}

#endregion

function Remove-InstalledModules
{
    try {
        $mod = Get-InstalledModule -Name $TestModule -AllVersions -ErrorAction SilentlyContinue
        if ($null -eq $mod) {
            return
        }

        if (Get-Module -Name $TestModule -ErrorAction Ignore) {
            Remove-Module -Force -Name $TestModule
        }

        $installedPath = $mod.InstalledLocation
        if (Test-Path $installedPath) {
            Remove-Item -Force -Recurse $installedPath -ErrorAction Ignore
        }
    }
    catch {
        Write-Warning "Remove-InstalledModules: $_"
    }
}

Describe "PowerShellGet - Module tests" -tags "Feature" {

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
        $psgetModuleInfo = Find-Module -Name $TestModule -Repository $RepositoryName
        $psgetModuleInfo.Name | Should -Be $TestModule
        $psgetModuleInfo.Repository | Should -Be $RepositoryName
    }

    It "Should install a module correctly to the required location with default CurrentUser scope" {
        Install-Module -Name $TestModule -Repository $RepositoryName
        $module = Get-Module -Name $TestModule -ListAvailable
        $module | Should -Not -BeNullOrEmpty
        $module.Name | Should -Be $TestModule
        $module.ModuleBase.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
    }

    AfterAll {
        Remove-InstalledModules
    }
}

Describe "PowerShellGet - Module tests (Admin)" -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {

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
        Uninstall-PSResource  -Name $TestModule -SkipDependencyCheck
        Install-Module -Name $TestModule -Repository $RepositoryName -Scope AllUsers

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
    $installedScript = Get-InstalledScript -Name $TestScript -ErrorAction SilentlyContinue
    if ($null -eq $installedScript) {
        return
    }

	$scriptPath = Join-Path ${installedScript}.InstalledLocation "${TestScript}.ps1"
	if (test-Path -Type Leaf -Path $scriptPath) {
		Remove-Item -Force -Path $scriptPath -ErrorAction Ignore
	}

	$xmlPath = Join-Path ${installedScript}.InstalledLocation InstalledScriptInfos "${TestScript}_InstalledScriptInfo.xml"
	if (test-Path -Type Leaf -Path $xmlPath) {
		Remove-Item -Force -Path $xmlPath -ErrorAction Ignore
	}
}

Describe "PowerShellGet - Script tests" -tags "Feature" {

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
        $psgetScriptInfo = Find-Script -Name $TestScript -Repository $RepositoryName
        $psgetScriptInfo.Name | Should -Be $TestScript
        $psgetScriptInfo.Repository | Should -Be $RepositoryName
    }

    It "Should install a script correctly to the required location with default CurrentUser scope" {
        Install-Script -Name $TestScript -Repository $RepositoryName -NoPathUpdate
        $installedScriptInfo = Get-InstalledScript -Name $TestScript

        $installedScriptInfo | Should -Not -BeNullOrEmpty
        $installedScriptInfo.Name | Should -Be $TestScript
        $installedScriptInfo.InstalledLocation.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

Describe "PowerShellGet - Script tests (Admin)" -Tags @('Feature', 'RequireAdminOnWindows', 'RequireSudoOnUnix') {

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
        Uninstall-PSResource  -Name $TestScript -SkipDependencyCheck
        Install-Script -Name $TestScript -Repository $RepositoryName -NoPathUpdate -Scope AllUsers
        $installedScriptInfo = Get-InstalledScript -Name $TestScript

        $installedScriptInfo | Should -Not -BeNullOrEmpty
        $installedScriptInfo.Name | Should -Be $TestScript
        Write-Output ("InstalledLocation: " + $installedScriptInfo.InstalledLocation)
        $installedScriptInfo.InstalledLocation.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase) | Should -BeTrue
    }

    AfterAll {
        Remove-InstalledScripts
    }
}

Describe 'PowerShellGet Type tests' -tags @('CI') {
    BeforeAll {
        Import-Module PowerShellGet -Force
    }

    It 'Ensure PowerShellGet Types are available' {
        $PowerShellGetNamespace = 'Microsoft.PowerShell.Commands.PowerShellGet'
        $PowerShellGetTypeDetails = @{
            InternalWebProxy = @('GetProxy', 'IsBypassed')
        }

        if((IsWindows)) {
            $PowerShellGetTypeDetails['CERT_CHAIN_POLICY_PARA'] = @('cbSize','dwFlags','pvExtraPolicyPara')
            $PowerShellGetTypeDetails['CERT_CHAIN_POLICY_STATUS'] = @('cbSize','dwError','lChainIndex','lElementIndex','pvExtraPolicyStatus')
            $PowerShellGetTypeDetails['InternalSafeHandleZeroOrMinusOneIsInvalid'] = @('IsInvalid')
            $PowerShellGetTypeDetails['InternalSafeX509ChainHandle'] = @('CertFreeCertificateChain','ReleaseHandle','InvalidHandle')
            $PowerShellGetTypeDetails['Win32Helpers'] = @('CertVerifyCertificateChainPolicy', 'CertDuplicateCertificateChain', 'IsMicrosoftCertificate')
        }

        if('Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI' -as [Type]) {
            $PowerShellGetTypeDetails['Telemetry'] = @('TraceMessageArtifactsNotFound', 'TraceMessageNonPSGalleryRegistration')
        }

        $PowerShellGetTypeDetails.GetEnumerator() | ForEach-Object {
            $ClassName = $_.Name
            $Type = "$PowerShellGetNamespace.$ClassName" -as [Type]
            $Type | Select-Object -ExpandProperty Name | Should -Be $ClassName
            $_.Value | ForEach-Object { $Type.DeclaredMembers.Name -contains $_ | Should -BeTrue }
        }
    }
}
