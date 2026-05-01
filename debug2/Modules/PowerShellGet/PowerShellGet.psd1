@{
    RootModule        = 'PSModule.psm1'
    ModuleVersion     = '2.2.5'
    GUID              = '1d73a601-4a6c-43c5-ba3f-619b18bbb404'
    Author            = 'Microsoft Corporation'
    CompanyName       = 'Microsoft Corporation'
    Copyright         = '(c) Microsoft Corporation. All rights reserved.'
    Description       = 'PowerShell module with commands for discovering, installing, updating and publishing the PowerShell artifacts like Modules, DSC Resources, Role Capabilities and Scripts.'
    PowerShellVersion = '3.0'
    FormatsToProcess  = 'PSGet.Format.ps1xml'
FunctionsToExport = @(
	'Find-Command',
	'Find-DSCResource',
	'Find-Module',
	'Find-RoleCapability',
	'Find-Script',
	'Get-CredsFromCredentialProvider',
	'Get-InstalledModule',
	'Get-InstalledScript',
	'Get-PSRepository',
	'Install-Module',
	'Install-Script',
	'New-ScriptFileInfo',
	'Publish-Module',
	'Publish-Script',
	'Register-PSRepository',
	'Save-Module',
	'Save-Script',
	'Set-PSRepository',
	'Test-ScriptFileInfo',
	'Uninstall-Module',
	'Uninstall-Script',
	'Unregister-PSRepository',
	'Update-Module',
	'Update-ModuleManifest',
	'Update-Script',
	'Update-ScriptFileInfo')

    VariablesToExport = 'PSGetPath'
    AliasesToExport   = @('inmo', 'fimo', 'upmo', 'pumo')
    FileList          = @('PSModule.psm1',
        'PSGet.Format.ps1xml',
        'PSGet.Resource.psd1')
    RequiredModules   = @(@{ModuleName = 'PackageManagement'; ModuleVersion = '1.4.4' })
    PrivateData       = @{
        "PackageManagementProviders"           = 'PSModule.psm1'
        "SupportedPowerShellGetFormatVersions" = @('1.x', '2.x')
        PSData                                 = @{
            Tags         = @('Packagemanagement',
                'Provider',
                'PSEdition_Desktop',
                'PSEdition_Core',
                'Linux',
                'Mac')
            ProjectUri   = 'https://go.microsoft.com/fwlink/?LinkId=828955'
            LicenseUri   = 'https://go.microsoft.com/fwlink/?LinkId=829061'
            ReleaseNotes = @'
### 2.2.5
- Security patch for code injection bug

### 2.2.4.1
- Remove catalog file

### 2.2.3
- Update `HelpInfoUri` to point to the latest content (#560)
- Improve discovery of usable nuget.exe binary (Thanks bwright86!) (#558)

### 2.2.2
Bug Fix

- Update casing of DscResources output

### 2.2.1
Bug Fix

- Allow DscResources to work on case sensitive platforms (#521)
- Fix for failure to return credential provider when using private feeds (#521)

## 2.2
Bug Fix

- Fix for prompting for credentials when passing in -Credential parameter when using Register-PSRepository

## 2.1.5
New Features

- Add and remove nuget based repositories as a nuget source when nuget client tool is installed (#498)

Bug Fix

- Fix for 'Failed to publish module' error thrown when publishing modules (#497)

## 2.1.4
- Fixed hang while publishing some packages (#478)

## 2.1.3
New Features

- Added -Scope parameter to Update-Module (Thanks @lwajswaj!) (#471)
- Added -Exclude parameter to Publish-Module (Thanks @Benny1007!) (#191)
- Added -SkipAutomaticTags parameter to Publish-Module (Thanks @awickham10!) (#452)

Bug Fix

- Fixed issue with finding modules using macOS and .NET Core 3.0

## 2.1.2

New Feature

- Added support for registering repositories with special characters

## 2.1.1

- Fix DSC resource folder structure

## 2.1.0

Breaking Change

- Default installation scope for Update-Module and Update-Script has changed to match Install-Module and Install-Script. For Windows PowerShell (version 5.1 or below), the default scope is AllUsers when running in an elevated session, and CurrentUser at all other times.
  For PowerShell version 6.0.0 and above, the default installation scope is always CurrentUser. (#421)

Bug Fixes

- Update-ModuleManifest no longer clears FunctionsToExport, AliasesToExport, nor NestModules (#415 & #425) (Thanks @pougetat and @tnieto88!)
- Update-Module no longer changes repository URL (#407)
- Update-ModuleManifest no longer preprends 'PSGet_' to module name (#403) (Thanks @ThePoShWolf)
- Update-ModuleManifest now throws error and fails to update when provided invalid entries (#398) (Thanks @pougetat!)
- Ignore files no longer being included when uploading modules (#396)

New Features

- New DSC resource, PSRepository (#426) (Thanks @johlju!)
- Piping of PS respositories (#420)
- utf8 support for .nuspec (#419)

## 2.0.4

Bug Fix
* Remove PSGallery availability checks (#374)

## 2.0.3

Bug fixes and Improvements
* Fix CommandAlreadyAvailable error for PackageManagement module (#333)
* Remove trailing whitespace when value is not provided for Get-PSScriptInfoString (#337) (Thanks @thomasrayner)
* Expanded aliases for improved readability (#338) (Thanks @lazywinadmin)
* Improvements for Catalog tests (#343)
* Fix Update-ScriptInfoFile to preserve PrivateData (#346) (Thanks @tnieto88)
* Import modules with many commands faster (#351)

New Features
* Tab completion for -Repository parameter (#339) and for Publish-Module -Name (#359) (Thanks @matt9ucci)

## 2.0.1

Bug fixes
- Resolved Publish-Module doesn't report error but fails to publish module (#316)
- Resolved CommandAlreadyAvailable error while installing the latest version of PackageManagement module (#333)

## 2.0.0

Breaking Change
- Default installation scope for Install-Module, Install-Script, and Install-Package has changed. For Windows PowerShell (version 5.1 or below), the default scope is AllUsers when running in an elevated session, and CurrentUser at all other times.
  For PowerShell version 6.0.0 and above, the default installation scope is always CurrentUser.

## 1.6.7

Bug fixes
- Resolved Install/Save-Module error in PSCore 6.1.0-preview.4 on Ubuntu 18.04 OS (WSL/Azure) (#313)
- Updated error message in Save-Module cmdlet when the specified path is not accessible (#313)
- Added few additional verbose messages (#313)

## 1.6.6

Dependency Updates
* Add dependency on version 4.1.0 or newer of NuGet.exe
* Update NuGet.exe bootstrap URL to https://aka.ms/psget-nugetexe

Build and Code Cleanup Improvements
* Improved error handling in network connectivity tests.

Bug fixes
- Change Update-ModuleManifest so that prefix is not added to CmdletsToExport.
- Change Update-ModuleManifest so that parameters will not reset to default values.
- Specify AllowPrereleseVersions provider option only when AllowPrerelease is specified on the PowerShellGet cmdlets.

## 1.6.5

New features
* Allow Pester/PSReadline installation when signed by non-Microsoft certificate (#258)
  - Whitelist installation of non-Microsoft signed Pester and PSReadline over Microsoft signed Pester and PSReadline.

Build and Code Cleanup Improvements
* Splitting of functions (#229) (Thanks @Benny1007)
  - Moves private functions into respective private folder.
  - Moves public functions as defined in PSModule.psd1 into respective public folder.
  - Removes all functions from PSModule.psm1 file.
  - Dot sources the functions from PSModule.psm1 file.
  - Uses Export-ModuleMember to export the public functions from PSModule.psm1 file.

* Add build step to construct a single .psm1 file (#242) (Thanks @Benny1007)
  - Merged public and private functions into one .psm1 file to increase load time performance.

Bug fixes
- Fix null parameter error caused by MinimumVersion in Publish-PackageUtility (#201)
- Change .ExternalHelp link from PSGet.psm1-help.xml to PSModule-help.xml in PSModule.psm1 file (#215)
- Change Publish-* to allow version comparison instead of string comparison (#219)
- Ensure Get-InstalledScript -RequiredVersion works when versions have a leading 0 (#260)
- Add positional path to Save-Module and Save-Script (#264, #266)
- Ensure that Get-AuthenticodePublisher verifies publisher and that installing or updating a module checks for approprite catalog signature (#272)
- Update HelpInfoURI to 'http://go.microsoft.com/fwlink/?linkid=855963' (#274)


## 1.6.0

New features
* Prerelease Version Support (#185)
  - Implemented prerelease versions functionality in PowerShellGet cmdlets.
  - Enables publishing, discovering, and installing the prerelease versions of modules and scripts from the PowerShell Gallery.
  - [Documentation](https://docs.microsoft.com/en-us/powershell/gallery/psget/module/PrereleaseModule)

* Enabled publish cmdlets on PWSH and Nano Server (#196)
  - Dotnet command version 2.0.0 or newer should be installed by the user prior to using the publish cmdlets on PWSH and Windows Nano Server.
  - Users can install the dotnet command by following the instructions specified at https://aka.ms/dotnet-install-script.
  - On Windows, users can install the dotnet command by running *Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '.\dotnet-install.ps1'; & '.\dotnet-install.ps1' -Channel Current -Version '2.0.0'*
  - Publish cmdlets on Windows PowerShell supports using the dotnet command for publishing operations.

Breaking Change
- PWSH: Changed the installation location of AllUsers scope to the parent of $PSHOME instead of $PSHOME. It is the SHARED_MODULES folder on PWSH.

Bug fixes
- Update HelpInfoURI to 'https://go.microsoft.com/fwlink/?linkid=855963' (#195)
- Ensure MyDocumentsPSPath path is correct (#179) (Thanks @lwsrbrts)


## 1.5.0.0

New features
* Added support for modules requiring license acceptance (#150)
  - [Documentation](https://docs.microsoft.com/en-us/powershell/gallery/psget/module/RequireLicenseAcceptance)

* Added version for REQUIREDSCRIPTS (#162)
  - Enabled following scenarios for REQUIREDSCRIPTS
    - [1.0] - RequiredVersion
    - [1.0,2.0] - Min and Max Version
    - (,1.0] - Max Version
    - 1.0 - Min Version

Bug fixes
* Fixed empty version value in nuspec (#157)


## 1.1.3.2
* Disabled PowerShellGet Telemetry on PS Core as PowerShell Telemetry APIs got removed in PowerShell Core beta builds. (#153)
* Fixed for DateTime format serialization issue. (#141)
* Update-ModuleManifest should add ExternalModuleDependencies value as a collection. (#129)

## 1.1.3.1

New features
* Added `PrivateData` field to ScriptFileInfo. (#119)

Bug fixes

## 1.1.2.0

Bug fixes

## 1.1.1.0

Bug fixes

## 1.1.0.0

* Initial release from GitHub.
* PowerShellCore support.

## For full history of release notes see changelog:
https://github.com/PowerShell/PowerShellGet/blob/master/CHANGELOG.md
'@
        }
    }

    HelpInfoURI       = 'http://go.microsoft.com/fwlink/?linkid=2113539'
}


# SIG # Begin signature block
# MIIjkQYJKoZIhvcNAQcCoIIjgjCCI34CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCRNShWem0qs5De
# OoNTMpUMZmnlgRrJHCT4bl+44h9Jy6CCDYEwggX/MIID56ADAgECAhMzAAABh3IX
# chVZQMcJAAAAAAGHMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTEwHhcNMjAwMzA0MTgzOTQ3WhcNMjEwMzAzMTgzOTQ3WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQDOt8kLc7P3T7MKIhouYHewMFmnq8Ayu7FOhZCQabVwBp2VS4WyB2Qe4TQBT8aB
# znANDEPjHKNdPT8Xz5cNali6XHefS8i/WXtF0vSsP8NEv6mBHuA2p1fw2wB/F0dH
# sJ3GfZ5c0sPJjklsiYqPw59xJ54kM91IOgiO2OUzjNAljPibjCWfH7UzQ1TPHc4d
# weils8GEIrbBRb7IWwiObL12jWT4Yh71NQgvJ9Fn6+UhD9x2uk3dLj84vwt1NuFQ
# itKJxIV0fVsRNR3abQVOLqpDugbr0SzNL6o8xzOHL5OXiGGwg6ekiXA1/2XXY7yV
# Fc39tledDtZjSjNbex1zzwSXAgMBAAGjggF+MIIBejAfBgNVHSUEGDAWBgorBgEE
# AYI3TAgBBggrBgEFBQcDAzAdBgNVHQ4EFgQUhov4ZyO96axkJdMjpzu2zVXOJcsw
# UAYDVR0RBEkwR6RFMEMxKTAnBgNVBAsTIE1pY3Jvc29mdCBPcGVyYXRpb25zIFB1
# ZXJ0byBSaWNvMRYwFAYDVQQFEw0yMzAwMTIrNDU4Mzg1MB8GA1UdIwQYMBaAFEhu
# ZOVQBdOCqhc3NyK1bajKdQKVMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly93d3cu
# bWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY0NvZFNpZ1BDQTIwMTFfMjAxMS0w
# Ny0wOC5jcmwwYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8vd3d3
# Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY0NvZFNpZ1BDQTIwMTFfMjAx
# MS0wNy0wOC5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAgEAixmy
# S6E6vprWD9KFNIB9G5zyMuIjZAOuUJ1EK/Vlg6Fb3ZHXjjUwATKIcXbFuFC6Wr4K
# NrU4DY/sBVqmab5AC/je3bpUpjtxpEyqUqtPc30wEg/rO9vmKmqKoLPT37svc2NV
# BmGNl+85qO4fV/w7Cx7J0Bbqk19KcRNdjt6eKoTnTPHBHlVHQIHZpMxacbFOAkJr
# qAVkYZdz7ikNXTxV+GRb36tC4ByMNxE2DF7vFdvaiZP0CVZ5ByJ2gAhXMdK9+usx
# zVk913qKde1OAuWdv+rndqkAIm8fUlRnr4saSCg7cIbUwCCf116wUJ7EuJDg0vHe
# yhnCeHnBbyH3RZkHEi2ofmfgnFISJZDdMAeVZGVOh20Jp50XBzqokpPzeZ6zc1/g
# yILNyiVgE+RPkjnUQshd1f1PMgn3tns2Cz7bJiVUaqEO3n9qRFgy5JuLae6UweGf
# AeOo3dgLZxikKzYs3hDMaEtJq8IP71cX7QXe6lnMmXU/Hdfz2p897Zd+kU+vZvKI
# 3cwLfuVQgK2RZ2z+Kc3K3dRPz2rXycK5XCuRZmvGab/WbrZiC7wJQapgBodltMI5
# GMdFrBg9IeF7/rP4EqVQXeKtevTlZXjpuNhhjuR+2DMt/dWufjXpiW91bo3aH6Ea
# jOALXmoxgltCp1K7hrS6gmsvj94cLRf50QQ4U8Qwggd6MIIFYqADAgECAgphDpDS
# AAAAAAADMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJVUzETMBEGA1UECBMK
# V2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0
# IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9vdCBDZXJ0aWZpY2F0
# ZSBBdXRob3JpdHkgMjAxMTAeFw0xMTA3MDgyMDU5MDlaFw0yNjA3MDgyMTA5MDla
# MH4xCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMT
# H01pY3Jvc29mdCBDb2RlIFNpZ25pbmcgUENBIDIwMTEwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCr8PpyEBwurdhuqoIQTTS68rZYIZ9CGypr6VpQqrgG
# OBoESbp/wwwe3TdrxhLYC/A4wpkGsMg51QEUMULTiQ15ZId+lGAkbK+eSZzpaF7S
# 35tTsgosw6/ZqSuuegmv15ZZymAaBelmdugyUiYSL+erCFDPs0S3XdjELgN1q2jz
# y23zOlyhFvRGuuA4ZKxuZDV4pqBjDy3TQJP4494HDdVceaVJKecNvqATd76UPe/7
# 4ytaEB9NViiienLgEjq3SV7Y7e1DkYPZe7J7hhvZPrGMXeiJT4Qa8qEvWeSQOy2u
# M1jFtz7+MtOzAz2xsq+SOH7SnYAs9U5WkSE1JcM5bmR/U7qcD60ZI4TL9LoDho33
# X/DQUr+MlIe8wCF0JV8YKLbMJyg4JZg5SjbPfLGSrhwjp6lm7GEfauEoSZ1fiOIl
# XdMhSz5SxLVXPyQD8NF6Wy/VI+NwXQ9RRnez+ADhvKwCgl/bwBWzvRvUVUvnOaEP
# 6SNJvBi4RHxF5MHDcnrgcuck379GmcXvwhxX24ON7E1JMKerjt/sW5+v/N2wZuLB
# l4F77dbtS+dJKacTKKanfWeA5opieF+yL4TXV5xcv3coKPHtbcMojyyPQDdPweGF
# RInECUzF1KVDL3SV9274eCBYLBNdYJWaPk8zhNqwiBfenk70lrC8RqBsmNLg1oiM
# CwIDAQABo4IB7TCCAekwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFEhuZOVQ
# BdOCqhc3NyK1bajKdQKVMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1Ud
# DwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFHItOgIxkEO5FAVO
# 4eqnxzHRI4k0MFoGA1UdHwRTMFEwT6BNoEuGSWh0dHA6Ly9jcmwubWljcm9zb2Z0
# LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcmwwXgYIKwYBBQUHAQEEUjBQME4GCCsGAQUFBzAChkJodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcnQwgZ8GA1UdIASBlzCBlDCBkQYJKwYBBAGCNy4DMIGDMD8GCCsGAQUFBwIB
# FjNodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2RvY3MvcHJpbWFyeWNw
# cy5odG0wQAYIKwYBBQUHAgIwNB4yIB0ATABlAGcAYQBsAF8AcABvAGwAaQBjAHkA
# XwBzAHQAYQB0AGUAbQBlAG4AdAAuIB0wDQYJKoZIhvcNAQELBQADggIBAGfyhqWY
# 4FR5Gi7T2HRnIpsLlhHhY5KZQpZ90nkMkMFlXy4sPvjDctFtg/6+P+gKyju/R6mj
# 82nbY78iNaWXXWWEkH2LRlBV2AySfNIaSxzzPEKLUtCw/WvjPgcuKZvmPRul1LUd
# d5Q54ulkyUQ9eHoj8xN9ppB0g430yyYCRirCihC7pKkFDJvtaPpoLpWgKj8qa1hJ
# Yx8JaW5amJbkg/TAj/NGK978O9C9Ne9uJa7lryft0N3zDq+ZKJeYTQ49C/IIidYf
# wzIY4vDFLc5bnrRJOQrGCsLGra7lstnbFYhRRVg4MnEnGn+x9Cf43iw6IGmYslmJ
# aG5vp7d0w0AFBqYBKig+gj8TTWYLwLNN9eGPfxxvFX1Fp3blQCplo8NdUmKGwx1j
# NpeG39rz+PIWoZon4c2ll9DuXWNB41sHnIc+BncG0QaxdR8UvmFhtfDcxhsEvt9B
# xw4o7t5lL+yX9qFcltgA1qFGvVnzl6UJS0gQmYAf0AApxbGbpT9Fdx41xtKiop96
# eiL6SJUfq/tHI4D1nvi/a7dLl+LrdXga7Oo3mXkYS//WsyNodeav+vyL6wuA6mk7
# r/ww7QRMjt/fdW1jkT3RnVZOT7+AVyKheBEyIXrvQQqxP/uozKRdwaGIm1dxVk5I
# RcBCyZt2WwqASGv9eZ/BvW1taslScxMNelDNMYIVZjCCFWICAQEwgZUwfjELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEoMCYGA1UEAxMfTWljcm9z
# b2Z0IENvZGUgU2lnbmluZyBQQ0EgMjAxMQITMwAAAYdyF3IVWUDHCQAAAAABhzAN
# BglghkgBZQMEAgEFAKCBrjAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgor
# BgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQg3TxELXso
# oRSF8xKPexxNhyDHV3uwUf4mRHTftOyVvf4wQgYKKwYBBAGCNwIBDDE0MDKgFIAS
# AE0AaQBjAHIAbwBzAG8AZgB0oRqAGGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbTAN
# BgkqhkiG9w0BAQEFAASCAQC2MdGNwwt17U4YHP6Gn/7I/Z5/XKA08BGacJ6NSvXC
# 7BK55Looz5q9JuOjKgY0J5Zv3s3cBCBiABOF/E+kfjMqCtK1x9xC0dz+ZXGjKyFb
# MFuuzueIFHKnZWjLXueiAXmXuhQEV1lmw2X2Cv5lCV7fhoUyYP47yJvS+nHk5c2u
# 8OSZ4iVgBTcazWLWZTDu4zbVKk2g2HawdlGEwsCBn21gwhyqIelNUa/Cs4ab+a30
# Ach8Jd12YE/ZmrSsoUV1hVDFA+/Z7oeZfuENa2mUUlrZ7Y+uLpbw8GZ4vge7TO23
# r2cFGEV5g6q1/ykxFisUUheyYfjfsWqPVhXe6+9Mvk94oYIS8DCCEuwGCisGAQQB
# gjcDAwExghLcMIIS2AYJKoZIhvcNAQcCoIISyTCCEsUCAQMxDzANBglghkgBZQME
# AgEFADCCAVQGCyqGSIb3DQEJEAEEoIIBQwSCAT8wggE7AgEBBgorBgEEAYRZCgMB
# MDEwDQYJYIZIAWUDBAIBBQAEIFoqO98TuKz1Zz96t7blTSp0BuwKzzxkArukjb8S
# 0+wRAgZfYPq2eAYYEjIwMjAwOTIyMjIxOTUwLjkzWjAEgAIB9KCB1KSB0TCBzjEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWlj
# cm9zb2Z0IE9wZXJhdGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBU
# U1MgRVNOOjc4ODAtRTM5MC04MDE0MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1T
# dGFtcCBTZXJ2aWNloIIORDCCBPUwggPdoAMCAQICEzMAAAEooA6B4TbVT8IAAAAA
# ASgwDQYJKoZIhvcNAQELBQAwfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIwMTAw
# HhcNMTkxMjE5MDExNTAwWhcNMjEwMzE3MDExNTAwWjCBzjELMAkGA1UEBhMCVVMx
# EzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoT
# FU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWljcm9zb2Z0IE9wZXJh
# dGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBUU1MgRVNOOjc4ODAt
# RTM5MC04MDE0MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNl
# MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnZGx1vdU24Y+zb8OClz2
# C3vssbQk+QPhVZOUQkuSrOdMmX5Ghl+I7A3qJZ8+7iT+SPyfBjum8uzU6wHLj3jK
# 6yDiscvAc1Qk+3DVNzngw4uB1yiwDg3GSLvd8PKpbAO2M52TofuQ1zME+oAMPoH3
# yi3vv/BIAIEkjGb2oBS52q5Ll9zMIXT75pZRq8O7jpTdy/ocSMh1XZl0lNQqDhZQ
# h1NgxBcjTzb6pKzjlYFmNwr3z+0h/Hy6ryrySxYX37NSMZMWIxooeGftxIKgSPsT
# W1WZbTwhKlLrvxYU/b4DQ5DBpZwko0AIr4n4trsvPZsa6kKJ04bPlcN7BzWUP2cs
# 9wIDAQABo4IBGzCCARcwHQYDVR0OBBYEFITi8oPxfrU3m9QBw050f1AEy6byMB8G
# A1UdIwQYMBaAFNVjOlyKMZDzQ3t8RhvFM2hahW1VMFYGA1UdHwRPME0wS6BJoEeG
# RWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Rp
# bVN0YVBDQV8yMDEwLTA3LTAxLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUH
# MAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljVGltU3Rh
# UENBXzIwMTAtMDctMDEuY3J0MAwGA1UdEwEB/wQCMAAwEwYDVR0lBAwwCgYIKwYB
# BQUHAwgwDQYJKoZIhvcNAQELBQADggEBAItfZkcYhQuAOT+JxwdZTLCMPICwEeWG
# Sa2YGniWV3Avd02jRtdlkeJJkH5zYrO8+pjrgGUQKNL8+q6vab1RpPU3QF5SjBEd
# BPzzB3N33iBiopeYsNtVHzJ5WAGRw/8mJVZtd1DNzPURMeBauH67MDwHBSABocnD
# 6ddhxwi4OA8kzVRN42X1Hk69/7rNHYTlkjgOsiq9LiMfhCygw9OfbsCM3tVm3hqa
# hHEwsRxABLu89PUlRRpEWkUeaRRhWWfVgyzD///r3rxpG/LdyYKVLji7GSRogtuG
# HWHT16NmMeGsSf6T0xxWRaK5jvbiMn/nu3KUzsD+PMhY2PUXxWWGTLIwggZxMIIE
# WaADAgECAgphCYEqAAAAAAACMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9v
# dCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAxMDAeFw0xMDA3MDEyMTM2NTVaFw0y
# NTA3MDEyMTQ2NTVaMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
# MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwMIIBIjAN
# BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqR0NvHcRijog7PwTl/X6f2mUa3RU
# ENWlCgCChfvtfGhLLF/Fw+Vhwna3PmYrW/AVUycEMR9BGxqVHc4JE458YTBZsTBE
# D/FgiIRUQwzXTbg4CLNC3ZOs1nMwVyaCo0UN0Or1R4HNvyRgMlhgRvJYR4YyhB50
# YWeRX4FUsc+TTJLBxKZd0WETbijGGvmGgLvfYfxGwScdJGcSchohiq9LZIlQYrFd
# /XcfPfBXday9ikJNQFHRD5wGPmd/9WbAA5ZEfu/QS/1u5ZrKsajyeioKMfDaTgaR
# togINeh4HLDpmc085y9Euqf03GS9pAHBIAmTeM38vMDJRF1eFpwBBU8iTQIDAQAB
# o4IB5jCCAeIwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFNVjOlyKMZDzQ3t8
# RhvFM2hahW1VMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIB
# hjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFNX2VsuP6KJcYmjRPZSQW9fO
# mhjEMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9w
# a2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dF8yMDEwLTA2LTIzLmNybDBaBggr
# BgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNv
# bS9wa2kvY2VydHMvTWljUm9vQ2VyQXV0XzIwMTAtMDYtMjMuY3J0MIGgBgNVHSAB
# Af8EgZUwgZIwgY8GCSsGAQQBgjcuAzCBgTA9BggrBgEFBQcCARYxaHR0cDovL3d3
# dy5taWNyb3NvZnQuY29tL1BLSS9kb2NzL0NQUy9kZWZhdWx0Lmh0bTBABggrBgEF
# BQcCAjA0HjIgHQBMAGUAZwBhAGwAXwBQAG8AbABpAGMAeQBfAFMAdABhAHQAZQBt
# AGUAbgB0AC4gHTANBgkqhkiG9w0BAQsFAAOCAgEAB+aIUQ3ixuCYP4FxAz2do6Eh
# b7Prpsz1Mb7PBeKp/vpXbRkws8LFZslq3/Xn8Hi9x6ieJeP5vO1rVFcIK1GCRBL7
# uVOMzPRgEop2zEBAQZvcXBf/XPleFzWYJFZLdO9CEMivv3/Gf/I3fVo/HPKZeUqR
# UgCvOA8X9S95gWXZqbVr5MfO9sp6AG9LMEQkIjzP7QOllo9ZKby2/QThcJ8ySif9
# Va8v/rbljjO7Yl+a21dA6fHOmWaQjP9qYn/dxUoLkSbiOewZSnFjnXshbcOco6I8
# +n99lmqQeKZt0uGc+R38ONiU9MalCpaGpL2eGq4EQoO4tYCbIjggtSXlZOz39L9+
# Y1klD3ouOVd2onGqBooPiRa6YacRy5rYDkeagMXQzafQ732D8OE7cQnfXXSYIghh
# 2rBQHm+98eEA3+cxB6STOvdlR3jo+KhIq/fecn5ha293qYHLpwmsObvsxsvYgrRy
# zR30uIUBHoD7G4kqVDmyW9rIDVWZeodzOwjmmC3qjeAzLhIp9cAvVCch98isTtoo
# uLGp25ayp0Kiyc8ZQU3ghvkqmqMRZjDTu3QyS99je/WZii8bxyGvWbWu3EQ8l1Bx
# 16HSxVXjad5XwdHeMMD9zOZN+w2/XU/pnR4ZOC+8z1gFLu8NoFA12u8JJxzVs341
# Hgi62jbb01+P3nSISRKhggLSMIICOwIBATCB/KGB1KSB0TCBzjELMAkGA1UEBhMC
# VVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNV
# BAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWljcm9zb2Z0IE9w
# ZXJhdGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBUU1MgRVNOOjc4
# ODAtRTM5MC04MDE0MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2
# aWNloiMKAQEwBwYFKw4DAhoDFQAxPUsb8oASPReyIv2fubGZfVp9m6CBgzCBgKR+
# MHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJjAkBgNVBAMT
# HU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwMA0GCSqGSIb3DQEBBQUAAgUA
# 4xSzVjAiGA8yMDIwMDkyMjIxMzEwMloYDzIwMjAwOTIzMjEzMTAyWjB3MD0GCisG
# AQQBhFkKBAExLzAtMAoCBQDjFLNWAgEAMAoCAQACAiF/AgH/MAcCAQACAhGdMAoC
# BQDjFgTWAgEAMDYGCisGAQQBhFkKBAIxKDAmMAwGCisGAQQBhFkKAwKgCjAIAgEA
# AgMHoSChCjAIAgEAAgMBhqAwDQYJKoZIhvcNAQEFBQADgYEAIIiMzPTf2R4++LT5
# BIQAAZ7XoAkwHZm+GsArFZSjJmmdGtYNmLO7vA433YPn1av4tVK9wasdh+Uxa3Qo
# YD4D+LR+E8bwdZR+JGQZKDkxjmOXwPITowyRtYQPCa9mTO4iJDf/gixpGBzu8sgM
# zXRdLiDn1P/Vo8FpWdAKfaLz4P4xggMNMIIDCQIBATCBkzB8MQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGlt
# ZS1TdGFtcCBQQ0EgMjAxMAITMwAAASigDoHhNtVPwgAAAAABKDANBglghkgBZQME
# AgEFAKCCAUowGgYJKoZIhvcNAQkDMQ0GCyqGSIb3DQEJEAEEMC8GCSqGSIb3DQEJ
# BDEiBCDvMyofVUL0InlfJG07Yi8ZRMCLjc3rNDrU8nhvTqbq+DCB+gYLKoZIhvcN
# AQkQAi8xgeowgecwgeQwgb0EILxFaouvBVJ379wbEN8GpLhvW09eGg8WsLrXm9XW
# 6BTaMIGYMIGApH4wfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24x
# EDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlv
# bjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIwMTACEzMAAAEo
# oA6B4TbVT8IAAAAAASgwIgQgRO6TlKRXX+wSVoBIMaqor080XnPuS/BnRzhuQ8oN
# BCQwDQYJKoZIhvcNAQELBQAEggEAZM529j3yYoEo0YZVvE7ZwiKlil8xSVU3JNZE
# gHE391SCUFpeLu012jhMt3ikeJjApyUuhuQpyWkU1pkIMJHP78SOq4VejIMhm57l
# iIUo1txBy4P6NZcNmIihQa/R484b4k1Zz1GP7jx+Wmvq65FHPv3SFlmJOs6GuE1R
# lIhZdc8VNW7DsyGShMOIKx5lYb68+rsUES5bRExAxAj0ykGPpmiQt3QRmBNlDEgc
# 7GF2FGWFD7ZdAn6+W66UmqxkU7MpBrVwggRi92YXMbZI2A6ts8UR8G8JeJZbaHRz
# hnAaK3NPwhZhfD/2oRN172Rr7uRQtH9FesspG0TlfkjEe6P9zw==
# SIG # End signature block
