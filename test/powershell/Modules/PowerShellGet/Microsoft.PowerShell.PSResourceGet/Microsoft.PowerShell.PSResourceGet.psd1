# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

@{
    RootModule             = './Microsoft.PowerShell.PSResourceGet.dll'
    NestedModules          = @('./Microsoft.PowerShell.PSResourceGet.psm1')
    ModuleVersion          = '1.0.2'
    CompatiblePSEditions   = @('Core', 'Desktop')
    GUID                   = 'e4e0bda1-0703-44a5-b70d-8fe704cd0643'
    Author                 = 'Microsoft Corporation'
    CompanyName            = 'Microsoft Corporation'
    Copyright              = '(c) Microsoft Corporation. All rights reserved.'
    Description            = 'PowerShell module with commands for discovering, installing, updating and publishing the PowerShell artifacts like Modules, Scripts, and DSC Resources.'
    PowerShellVersion      = '5.1'
    DotNetFrameworkVersion = '2.0'
    CLRVersion             = '4.0.0'
    FormatsToProcess       = 'PSGet.Format.ps1xml'
    CmdletsToExport        = @(
        'Find-PSResource',
        'Get-InstalledPSResource',
        'Get-PSResourceRepository',
        'Get-PSScriptFileInfo',
        'Install-PSResource',
        'Register-PSResourceRepository',
        'Save-PSResource',
        'Set-PSResourceRepository',
        'New-PSScriptFileInfo',
        'Test-PSScriptFileInfo',
        'Update-PSScriptFileInfo',
        'Publish-PSResource',
        'Uninstall-PSResource',
        'Unregister-PSResourceRepository',
        'Update-PSModuleManifest',
        'Update-PSResource'
    )
    FunctionsToExport      = @(
        'Import-PSGetRepository'
    )
    VariablesToExport = 'PSGetPath'
    AliasesToExport = @(
        'Get-PSResource',
        'fdres',
        'isres',
        'pbres',
        'udres')
    PrivateData = @{
        PSData = @{
            #Prerelease   = ''
            Tags         = @('PackageManagement',
                'PSEdition_Desktop',
                'PSEdition_Core',
                'Linux',
                'Mac',
                'Windows')
            ProjectUri   = 'https://go.microsoft.com/fwlink/?LinkId=828955'
            LicenseUri   = 'https://go.microsoft.com/fwlink/?LinkId=829061'
            ReleaseNotes = @'
## 1.0.2

### Bug Fixes
- Bug fix for `Update-PSResource` not updating from correct repository (#1549)
- Bug fix for creating temp home directory on Unix (#1544)
- Bug fix for creating `InstalledScriptInfos` directory when it does not exist (#1542)
- Bug fix for `Update-ModuleManifest` throwing null pointer exception (#1538)
- Bug fix for `name` property not populating in `PSResourceInfo` object when using `Find-PSResource` with JFrog Artifactory (#1535)
- Bug fix for incorrect configuration of requests to JFrog Artifactory v2 endpoints (#1533 Thanks @sean-r-williams!)
- Bug fix for determining JFrog Artifactory repositories (#1532 Thanks @sean-r-williams!)
- Bug fix for v2 server repositories incorrectly adding script endpoint (1526)
- Bug fixes for null references (#1525)
- Typo fixes in message prompts in `Install-PSResource` (#1510 Thanks @NextGData!)
- Bug fix to add `NormalizedVersion` property to `AdditionalMetadata` only when it exists (#1503 Thanks @sean-r-williams!)
- Bug fix to verify whether `Uri` is a UNC path and set respective `ApiVersion` (#1479 Thanks @kborowinski!)

## 1.0.1

### Bug Fixes
- Bugfix to update Unix local user installation paths to be compatible with .NET 7 and .NET 8 (#1464)
- Bugfix for Import-PSGetRepository in Windows PowerShell (#1460)
- Bugfix for Test-PSScriptFileInfo to be less sensitive to whitespace (#1457)
- Bugfix to overwrite rels/rels directory on net472 when extracting nupkg to directory (#1456)
- Bugfix to add pipeline by property name support for Name and Repository properties for Find-PSResource (#1451 Thanks @ThomasNieto!)

## 1.0.0

### New Features
- Add `ApiVersion` parameter for `Register-PSResourceRepository` (#1431)

### Bug Fixes
- Automatically set the ApiVersion to v2 for repositories imported from PowerShellGet (#1430)
- Bug fix ADO v2 feed installation failures (#1429)
- Bug fix Artifactory v2 endpoint failures (#1428)
- Bug fix Artifactory v3 endpoint failures (#1427)
- Bug fix `-RequiredResource` silent failures (#1426)
- Bug fix for v2 repository returning extra packages for `-Tag` based search with `-Prerelease` (#1405) 

## 0.9.0-rc1

### Bug Fixes
- Bug fix for using `Import-PSGetRepository` in Windows PowerShell (#1390)
- Add error handling when searching for unlisted package versions (#1386)
- Bug fix for deduplicating dependencies found from `Find-PSResource` (#1382)
- Added support for non-PowerShell Gallery v2 repositories (#1380)
- Bug fix for setting 'unknown' repository `APIVersion` (#1377)
- Bug fix for saving a script with `-IncludeXML` parameter (#1375)
- Bug fix for v3 server logic to properly parse inner @id element (#1374)
- Bug fix to write warning instead of error when package is already installed (#1367)

## 0.5.24-beta24

### Bug Fixes
- Detect empty V2 server responses at ServerApiCall level instead of ResponseUtil level (#1358)
- Bug fix for finding all versions of a package returning correct results and incorrect "package not found" error (#1356)
- Bug fix for installing or saving a pkg found in lower priority repository (#1350)
- Ensure `-Prerelease` is not empty or whitespace for `Update-PSModuleManifest` (#1348)
- Bug fix for saving `Az` module dependencies (#1343)
- Bug fix for `Find-PSResource` repository looping to to return matches from all repositories (#1342)
- Update error handling for Tags, Commands, and DSCResources when searching across repositories (#1339)
- Update `Find-PSResource` looping and error handling to account for multiple package names (#1338)
- Update error handling for `Find-PSResource` using V2 server endpoint repositories (#1329)
- Bug fix for searching through multiple repositories when some repositories do not contain the specified package (#1328)
- Add parameters to `Install-PSResource` verbose message (#1327)
- Bug fix for parsing required modules when publishing (#1326)
- Bug fix for saving dependency modules in version range format (#1323)
- Bug fix for `Install-PSResource` failing to find prerelease dependencies (#1322)
- Bug fix for updating to a new version of a prerelease module (#1320)
- Fix for error message when DSCResource is not found (#1317)
- Add error handling for local repository pattern based searching (#1316)
- `Set-PSResourceRepository` run without `-ApiVersion` paramater no longer resets the property for the repository (#1310)


## 0.5.23-beta23

### Breaking Changes

### New Features
- *-PSResourceRepository -Uri now accepting PSPaths (#1269)
- Add aliases for Install-PSResource, Find-PSResource, Update-PSResource, Publish-PSResource (#1264)
- Add custom user agent string to API calls (#1260)
- Support install for NuGet.Server application hosted feed (#1253)
- Add support for NuGet.Server application hosted feeds (#1236)
- Add Import-PSGetRepository function to import existing v2 PSRepositories into PSResourceRepositories. (#1221)
- Add 'Get-PSResource' alias to 'Get-InstalledPSResource' (#1216)
- Add -ApiVersion parameter to Set-PSResourceRepository (#1207)
- Add support for FindNameGlobbing scenarios (i.e -Name az*) for MyGet server repository (V3) (#1202)


### Bug Fixes
- Better error handling for scenario where repo ApiVersion is unknown and allow for PSPaths as URI for registered repositories (#1288)
- Bugfix for Uninstall should be able to remove older versions of a package that are not a dependency (#1287)
- Bugfix for Publish finding prerelease dependency versions. (#1283)
- Fix Pagination for V3 search with globbing scenarios (#1277)
- Update message for -WhatIf in Install-PSResource, Save-PSResource, and Update-PSResource (#1274)
- Bug fix for publishing with ExternalModuleDependencies (#1271)
- Support Credential Persistence for Publish-PSResource (#1268)
- Update Save-PSResource -Path param so it defaults to the current working directory (#1265)
- Update dependency error message in Publish-PSResource (#1263)
- Bug fixes for script metadata (#1259)
- Fix error message for Publish-PSResource for MyGet.org feeds (#1256)
- Bug fix for version ranges with prerelease versions not returning the correct versions (#1255)
- Bug fix for file path version must match psd1 version error when publishing (#1254)
- Bug fix for searching through local repositories with -Type parameter (#1252)
- Allow environment variables in module manifests (#1249)
- Updating prerelease version should update to latest prerelease version (#1238)
- Fix InstallHelper call to GetEnvironmentVariable() on Unix (#1237)
- Update build script to resolve module loading error (#1234)
- Enable UNC Paths for local repositories, source directories and destination directories (#1229)
- Improve better error handling for -Path in Publish-PSResource (#1227)
- Bug fix for RequireLicenseAcceptance in Publish-PSResource (#1225)
- Provide clearer error handling for V3 Publish support (#1224)
- Fix bug with version parsing in Publish-PSResource (#1223)
- Improve error handling for Find-PSResource (#1222)
- Add error handling to Get-InstalledPSResource and Find-PSResource (#1217)
- Improve error handling in Uninstall-PSResource (#1215)
- Change resolved paths to use GetResolvedProviderPathFromPSPath (#1209)
- Bug fix for Get-InstalledPSResource returning type of scripts as module (#1198)
            

## 0.5.22-beta22

### Breaking Changes
- PowerShellGet is now PSResourceGet! (#1164)
- Update-PSScriptFile is now Update-PSScriptFileInfo (#1140)
- New-PSScriptFile is now New-PSScriptFileInfo (#1140)
- Update-ModuleManifest is now Update-PSModuleManifest (#1139)
- -Tags parameter changed to -Tag in New-PSScriptFile, Update-PSScriptFileInfo, and Update-ModuleManifest (#1123)
- Change the type of -InputObject from PSResource to PSResource[] for Install-PSResource, Save-PSResource, and Uninstall-PSResource (#1124)
- PSModulePath is no longer referenced when searching paths (#1154)

### New Features
- Support for Azure Artifacts, GitHub Packages, and Artifactory (#1167, #1180)

### Bug Fixes
- Filter out unlisted packages (#1172, #1161)
- Add paging for V3 server requests (#1170)
- Support for floating versions (#1117)
- Update, Save, and Install with wildcard gets the latest version within specified range (#1117)
- Add positonal parameter for -Path in Publish-PSResource (#1111)
- Uninstall-PSResource -WhatIf now shows version and path of package being uninstalled (#1116)
- Find returns packages from the highest priority repository only (#1155)
- Bug fix for PSCredentialInfo constructor (#1156)
- Bug fix for Install-PSResource -NoClobber parameter (#1121)
- Save-PSResource now searches through all repos when no repo is specified (#1125)
- Caching for improved performance in Uninstall-PSResource (#1175)
- Bug fix for parsing package tags from local repository (#1119)

See change log (CHANGELOG.md) at https://github.com/PowerShell/PSResourceGet
'@
        }
    }

    HelpInfoUri            = 'https://go.microsoft.com/fwlink/?linkid=2238183'
}
