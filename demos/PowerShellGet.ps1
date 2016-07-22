#region cleanup
Get-InstalledModule PSScriptAnalyzer -AllVersions -ErrorAction SilentlyContinue | Uninstall-Module
Get-InstalledModule xJea -AllVersions -ErrorAction SilentlyContinue | Uninstall-Module
Get-InstalledScript Fabrikam-Script -ErrorAction SilentlyContinue |  Uninstall-Script
Get-InstalledScript Start-Demo -ErrorAction SilentlyContinue |  Uninstall-Script
Remove-Item /tmp/PSScriptAnalyzer -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item /tmp/Start-Demot.ps1 -Force -ErrorAction SilentlyContinue

#endregion

# List of PowerShellGet commands
Get-Command -Module PowerShellGet

# Discover modules
Find-Module
Find-Module PSScriptAnalyzer

# Save a module to the local machine
Save-Module PSScriptAnalyzer -Repository PSGallery -Path /tmp
Get-ChildItem -Path /tmp/PSScriptAnalyzer/1.6.0/ -Recurse

# Install a module to the common modules location
Install-Module -Name PSScriptAnalyzer -RequiredVersion 1.0.2 -Scope CurrentUser -Repository PSGallery

# Discover the installed modules
Get-InstalledModule
Get-Module -ListAvailable PSScriptAnalyzer
Get-InstalledModule PSScriptAnalyzer | Format-List *

# Install xJea module
Install-Module -Name xJea -RequiredVersion 0.2 -Scope CurrentUser
Get-InstalledModule xJea
Get-Module -ListAvailable xJea

# Update a module
Update-Module xJea -RequiredVersion 0.2.5
Get-InstalledModule xJea
Get-Module -ListAvailable xJea

# Update all modules
Update-Module -WhatIf
Update-Module
Get-InstalledModule

# Uninstall a module version
Get-InstalledModule xJea -AllVersions
Uninstall-Module xJea
Get-InstalledModule xJea -AllVersions

# Discover PowerShell Scripts
Find-Script
Find-Script -Name Start-Demo

# Save scripts to a specified location
Save-Script Start-Demo -Repository PSGallery -Path /tmp
Get-ChildItem -Path /tmp/Start-Demo.ps1

# Install a script to the common scripts location
Find-Script -Name Start-Demo -Repository PSGallery  | Install-Script -Scope CurrentUser
Get-InstalledScript

Install-Script Fabrikam-Script -RequiredVersion 1.0 -Scope CurrentUser
Get-InstalledScript
Get-InstalledScript Fabrikam-Script | Format-List *

# Update the installed scripts
Update-Script -WhatIf
Update-Script
Get-InstalledScript

# Uninstall a script file
Uninstall-Script Fabrikam-Script -Verbose
