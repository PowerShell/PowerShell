Get-Command -Module PowerShellGet

Get-PSRepository

Register-PSRepository -Name PSGalleryINT -SourceLocation https://dtlgalleryint.cloudapp.net -InstallationPolicy Trusted

Get-PSRepository

Find-Module
Find-Module ContosoServer

Save-Module ContosoServer -Path /tmp
Get-ChildItem -Path /tmp/ContosoServer -Recurse

Install-Module -Name ContosoServer -RequiredVersion 1.5 -Scope CurrentUser
Get-InstalledModule
Get-Module -ListAvailable ContosoServer
Get-InstalledScript ContosoServer | Format-List *

Install-Module -Name ContosoClient -RequiredVersion 1.0 -Scope CurrentUser
Get-InstalledModule ContosoClient
Get-Module -ListAvailable ContosoClient

Update-Module ContosoClient -RequiredVersion 1.5
Get-InstalledModule ContosoClient
Get-Module -ListAvailable ContosoClient

Update-Module -WhatIf
Update-Module
Get-InstalledModule

Get-InstalledModule ContosoClient -AllVersions
Uninstall-Module ContosoClient -RequiredVersion 1.0
Get-InstalledModule ContosoClient -AllVersions


Find-Script
Find-Script -Name Fabrikam-ServerScript
Save-Script Fabrikam-ServerScript -Path /tmp
Get-ChildItem -Path /tmp/Fabrikam-ServerScript.ps1

'/tmp/Fabrikam-ServerScript.ps1'

Find-Script -Name Fabrikam-ServerScript -RequiredVersion 2.0 | Install-Script -Scope CurrentUser
Get-InstalledScript

Install-Script Fabrikam-ServerClient -RequiredVersion 1.0 -Scope CurrentUser
Get-InstalledScript

Get-InstalledScript Fabrikam-ServerClient | Format-List *

Update-Script -WhatIf
Update-Script
Get-InstalledScript

Uninstall-Script Fabrikam-ServerClient -Verbose



