@{
RootModule = 'PSModule.psm1'
ModuleVersion = '1.0.0.1'
GUID = '1d73a601-4a6c-43c5-ba3f-619b18bbb404'
Author = 'Microsoft Corporation'
CompanyName = 'Microsoft Corporation'
Copyright = '(c) Microsoft Corporation. All rights reserved.'
Description = 'PowerShell module with commands for discovering, installing, updating and publishing the PowerShell artifacts like Modules, DSC Resources, Role Capabilities and Scripts.'
PowerShellVersion = '3.0'
FormatsToProcess = 'PSGet.Format.ps1xml'
FunctionsToExport = @('Install-Module',
                      'Find-Module',
                      'Save-Module',
                      'Update-Module',
                      'Publish-Module', 
                      'Get-InstalledModule',
                      'Uninstall-Module',
                      'Find-Command', 
                      'Find-DscResource', 
                      'Find-RoleCapability',
                      'Install-Script',
                      'Find-Script',
                      'Save-Script',
                      'Update-Script',
                      'Publish-Script', 
                      'Get-InstalledScript',
                      'Uninstall-Script',
                      'Test-ScriptFileInfo',
                      'New-ScriptFileInfo',
                      'Update-ScriptFileInfo',
                      'Get-PSRepository',
                      'Set-PSRepository',                      
                      'Register-PSRepository',
                      'Unregister-PSRepository',
                      'Update-ModuleManifest')
VariablesToExport = "*"
AliasesToExport = @('inmo',
                    'fimo',
                    'upmo',
                    'pumo')
FileList = @('PSModule.psm1',
             'PSGet.Format.ps1xml',
             'PSGet.Resource.psd1')
RequiredModules = @(@{ModuleName='PackageManagement';ModuleVersion='1.0.0.1'})
PrivateData = @{
                "PackageManagementProviders" = 'PSModule.psm1'
                "SupportedPowerShellGetFormatVersions" = @('1.x')
    PSData = @{
        Tags = @('Packagemanagement',
                 'Provider',
                 'PSEdition_Desktop',
                 'PSEdition_Core',
                 'Linux',
                 'Mac')
        ProjectUri = 'https://go.microsoft.com/fwlink/?LinkId=828955'
        LicenseUri = 'https://go.microsoft.com/fwlink/?LinkId=829061'
        ReleaseNotes = @'
- PowerShellCore support
- Security enhancements including the enforcement of catalog-signed modules during installation
- Authenticated Repository support
- Proxy Authentication support
- Responses to a number of user requests and issues
'@
    }
}

HelpInfoURI = 'https://go.microsoft.com/fwlink/?LinkId=393271'
}
